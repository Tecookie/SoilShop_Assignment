using AutoMapper;
namespace SoilShop.Application;

public sealed class InventoryService(IInventoryRepository repository, IMapper mapper)
{
    public Task<PageResult<MovementDto>> MovementsAsync(int? skuId, int page, int size, CancellationToken ct) { Pagination.Validate(page, size); return repository.MovementsAsync(skuId, page, size, ct); }

    public async Task<IReadOnlyList<SuggestionDto>> SuggestionsAsync(CancellationToken ct) => mapper.Map<List<SuggestionDto>>(await repository.SuggestionsAsync(ct));
}
