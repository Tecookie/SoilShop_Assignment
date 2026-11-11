using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SoilShop.Api;
using SoilShop.Application;
using SoilShop.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ShopDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Shop") ?? throw new InvalidOperationException("ConnectionStrings:Shop is required.")));
builder.Services.AddScoped<ICatalogueRepository, CatalogueRepository>();
builder.Services.AddScoped<ITradingRepository, TradingRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IStockWorkflowRepository, StockWorkflowRepository>();
builder.Services.AddScoped<StockWorkflowService>();
builder.Services.AddScoped<IOperationsRepository, OperationsRepository>();
builder.Services.AddScoped<OperationsService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<IAdministrationRepository, AdministrationRepository>();
builder.Services.AddScoped<AdministrationService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISoilMetadataRepository, SoilMetadataRepository>(); builder.Services.AddScoped<SoilMetadataService>();
builder.Services.AddScoped<CatalogueService>(); builder.Services.AddScoped<TradingService>(); builder.Services.AddScoped<InventoryService>(); builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<IPasswordVerifier, PasswordVerifier>(); builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<CalculationClient>();
builder.Services.AddSingleton<ICalculationClient>(s => s.GetRequiredService<CalculationClient>());
builder.Services.AddSingleton<IProductAdviceClient>(s => s.GetRequiredService<CalculationClient>());
builder.Services.AddScoped<IAdviceCandidateRepository, AdviceCandidateRepository>(); builder.Services.AddScoped<ProductAdviceService>();
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
        var user = await context.HttpContext.RequestServices.GetRequiredService<IUserRepository>().UserAsync(id, context.HttpContext.RequestAborted);
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
    var result = await new DemoDataSeeder(scope.ServiceProvider.GetRequiredService<ShopDbContext>(), scope.ServiceProvider.GetRequiredService<TradingService>(), scope.ServiceProvider.GetRequiredService<CatalogueService>(), app.Configuration).RunAsync();
    if (app.Configuration.GetValue("DemoSeed:WithAdvice", false))
    {
        await new DemoAdviceSeeder(scope.ServiceProvider.GetRequiredService<ShopDbContext>(), scope.ServiceProvider.GetRequiredService<TradingService>()).RunAsync();
        Console.WriteLine("Fictional classification-only advice fixtures prepared; existing metadata preserved.");
    }
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
    return;
}
app.Run();
public partial class Program;
