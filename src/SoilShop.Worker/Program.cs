using Microsoft.EntityFrameworkCore;
using SoilShop.Infrastructure;
using SoilShop.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<ShopDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Shop") ?? throw new InvalidOperationException("ConnectionStrings:Shop is required.")));
builder.Services.AddSingleton<RedisBus>();
builder.Services.AddHostedService<InventoryConsumer>();
await builder.Build().RunAsync();
