using Microsoft.EntityFrameworkCore;
using SoilShop.Infrastructure;
using SoilShop.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<ShopDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Shop") ?? throw new InvalidOperationException("ConnectionStrings:Shop is required.")));
builder.Services.AddSingleton<RedisBus>();
builder.Services.AddSingleton(WorkerSettings.Load(builder.Configuration));
builder.Services.AddHostedService<InventoryConsumer>();
builder.Services.AddHostedService<SummaryWorker>();
builder.Services.AddHostedService<BackupWorker>();
await builder.Build().RunAsync();
