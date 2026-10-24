using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using SoilShop.Api;
using SoilShop.Application;
using SoilShop.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ShopDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Shop") ?? throw new InvalidOperationException("ConnectionStrings:Shop is required.")));
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<ShopService>(); builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<IPasswordVerifier, PasswordVerifier>(); builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<ICalculationClient, CalculationClient>();
builder.Services.AddAutoMapper(cfg => { cfg.AddProfile<ShopProfile>(); if (builder.Configuration["AutoMapper:LicenseKey"] is string license) cfg.LicenseKey = license; });
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<ShopExceptionHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    var key = builder.Configuration["Jwt:Key"]!;
    o.TokenValidationParameters = new() { ValidateIssuer = true, ValidIssuer = "soilshop", ValidateAudience = true, ValidAudience = "soilshop-web", ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.FromSeconds(10) };
    o.Events = new JwtBearerEvents { OnTokenValidated = async context =>
    {
        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.Fail("Invalid account."); return; }
        var user = await context.HttpContext.RequestServices.GetRequiredService<IShopRepository>().UserAsync(id, context.HttpContext.RequestAborted);
        if (user is null || !user.IsActive || user.Role != context.Principal?.FindFirstValue(ClaimTypes.Role) || user.Version.ToString() != context.Principal?.FindFirstValue("version")) context.Fail("Account has changed.");
    }};
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new OpenApiInfo { Title = "Soil Shop API", Version = "v1" }); o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" }); });
if (builder.Configuration.GetValue("Messaging:Enabled", true)) { builder.Services.AddSingleton<RedisBus>(); builder.Services.AddHostedService<OutboxRelay>(); }
var app = builder.Build();
if (app.Configuration["Jwt:Key"] is not string signingKey || Encoding.UTF8.GetByteCount(signingKey) < 32) throw new InvalidOperationException("Configure Jwt:Key with at least 32 bytes.");
app.UseExceptionHandler();
app.Use(async (context, next) => { context.Response.Headers["X-Content-Type-Options"] = "nosniff"; context.Response.Headers["X-Frame-Options"] = "DENY"; await next(); });
app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.UseSwagger(options => options.RouteTemplate = "api/openapi/{documentName}.json");
app.UseSwaggerUI(options => options.SwaggerEndpoint("/api/openapi/v1.json", "Soil Shop API v1"));
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ready" }));
await using (var scope = app.Services.CreateAsyncScope()) await DatabaseSetup.InitializeAsync(scope.ServiceProvider.GetRequiredService<ShopDbContext>(), app.Configuration);
if (app.Configuration.GetValue("DemoSeed:Run", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    var result = await new DemoDataSeeder(scope.ServiceProvider.GetRequiredService<ShopDbContext>(), scope.ServiceProvider.GetRequiredService<ShopService>(), app.Configuration).RunAsync();
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
    return;
}
app.Run();
public partial class Program;

public sealed class ShopExceptionHandler(IProblemDetailsService problems, ILogger<ShopExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken ct)
    {
        var status = error switch { BusinessException b => b.Status, DbUpdateConcurrencyException => 409, DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "23503" } } => 409, PostgresException { SqlState: "40P01" or "40001" } => 409, _ => 500 };
        var detail = error is BusinessException business ? business.Message : status == 409 ? "Data changed or is referenced by another record. Reload and retry." : "The request could not be completed.";
        if (status == 500) logger.LogError(error, "Unhandled request failure {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = new() { Status = status, Title = detail, Extensions = { ["traceId"] = context.TraceIdentifier } } });
    }
}
