using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using SoilShop.Application;
using SoilShop.Contracts;

namespace SoilShop.Infrastructure;

public sealed class CalculationClient(IConfiguration config) : ICalculationClient, IDisposable
{
    private readonly GrpcChannel channel = GrpcChannel.ForAddress(config["Calculation:Address"] ?? "http://localhost:8082");
    public async Task<decimal> TotalAsync(IReadOnlyList<CalculationLine> lines, CancellationToken ct)
    {
        var request = new TotalsRequest();
        request.Lines.AddRange(lines.Select(x => new PriceLine { Quantity = x.Quantity, UnitPriceVnd = checked((long)x.Price) }));
        try
        {
            var reply = await new Calculation.CalculationClient(channel).CalculateTotalsAsync(request, deadline: DateTime.UtcNow.AddSeconds(3), cancellationToken: ct);
            return reply.TotalVnd;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument) { throw new BusinessException(400, "Document quantities or total exceed supported limits."); }
        catch (RpcException) { throw new BusinessException(503, "Calculation service is unavailable. Your draft is unchanged; retry shortly."); }
    }
    public void Dispose() => channel.Dispose();
}
