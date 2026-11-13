using SoilShop.Domain;

namespace SoilShop.Application;

public interface IInventoryRepository
{
    Task<PageResult<MovementDto>> MovementsAsync(int? skuId, int page, int size, CancellationToken ct);
    Task<IReadOnlyList<RestockSuggestion>> SuggestionsAsync(CancellationToken ct);
    Task<PageResult<RestockSuggestion>> SuggestionsPageAsync(string? search,int page,int size,CancellationToken ct);
}
