using Grpc.Core;
using SoilShop.CalculationHost;
using SoilShop.Contracts;
using Xunit;

namespace SoilShop.Tests;
public sealed class CalculationTests
{
    [Fact] public void Totals_use_whole_vnd_without_rounding()
    {
        var request = new TotalsRequest();
        request.Lines.Add(new PriceLine { Quantity = 3, UnitPriceVnd = 45000 });
        request.Lines.Add(new PriceLine { Quantity = 2, UnitPriceVnd = 80000 });
        Assert.Equal(295000, TotalsService.Calculate(request));
    }
    [Fact] public void Nonpositive_quantity_is_rejected()
    {
        var request = new TotalsRequest(); request.Lines.Add(new PriceLine { Quantity = 0, UnitPriceVnd = 45000 });
        Assert.Throws<RpcException>(() => TotalsService.Calculate(request));
    }
    [Fact] public void Document_total_overflow_is_rejected()
    {
        var request = new TotalsRequest();
        request.Lines.AddRange(Enumerable.Range(0, 100).Select(_ => new PriceLine { Quantity = 1000000, UnitPriceVnd = 1000000000000 }));
        Assert.Throws<RpcException>(() => TotalsService.Calculate(request));
    }
}
