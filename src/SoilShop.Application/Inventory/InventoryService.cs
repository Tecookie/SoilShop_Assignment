using AutoMapper;
namespace SoilShop.Application;

public sealed class InventoryService(IInventoryRepository repository, IMapper mapper)
{
    public Task<PageResult<MovementDto>> MovementsAsync(int? skuId, int page, int size, CancellationToken ct) { Pagination.Validate(page, size); return repository.MovementsAsync(skuId, page, size, ct); }

    public async Task<IReadOnlyList<SuggestionDto>> SuggestionsAsync(CancellationToken ct) => mapper.Map<List<SuggestionDto>>(await repository.SuggestionsAsync(ct));
    public async Task<PageResult<SuggestionDto>> SuggestionsPageAsync(string? search,int page,int size,CancellationToken ct)
    {
        Pagination.Validate(page,size);
        var result=await repository.SuggestionsPageAsync(search,page,size,ct);
        return new(mapper.Map<List<SuggestionDto>>(result.Items),result.Total,page,size);
    }
}
