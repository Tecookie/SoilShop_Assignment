using SoilShop.Domain;

namespace SoilShop.Application;

public interface IProductAdviceClient
{
    Task<ProductAdviceRanking> RankAsync(ProductAdviceInput input, IReadOnlyList<ProductAdviceCandidate> candidates, CancellationToken ct);
}
