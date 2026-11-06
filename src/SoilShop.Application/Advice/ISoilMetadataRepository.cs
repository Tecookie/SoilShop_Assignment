using SoilShop.Domain;

namespace SoilShop.Application;

public interface ISoilMetadataRepository
{
    Task<IReadOnlyList<string>> PlantGroupsAsync(int productId, CancellationToken ct);
    Task SaveMetadataAsync(Product product, IReadOnlyList<string>? groups, CancellationToken ct);
}
