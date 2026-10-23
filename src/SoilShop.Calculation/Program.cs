using Microsoft.AspNetCore.Server.Kestrel.Core;
using SoilShop.CalculationHost;

namespace SoilShop.CalculationHost;
public static class CalculationProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8082, listen => listen.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        var app = builder.Build();
        app.MapGrpcService<TotalsService>();
        app.Run();
    }
}
