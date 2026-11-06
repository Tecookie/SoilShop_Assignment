
namespace SoilShop.Application;

public sealed record CalculationLine(int Quantity, decimal Price);

public interface ICalculationClient { Task<decimal> TotalAsync(IReadOnlyList<CalculationLine> lines, CancellationToken ct); }
