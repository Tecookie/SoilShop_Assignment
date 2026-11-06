using SoilShop.Domain;

namespace SoilShop.Application;

public interface IAdviceCandidateRepository
{
    Task<IReadOnlyList<ProductAdviceCandidate>> CandidatesAsync(ProductAdviceInput input, CancellationToken ct);
}
