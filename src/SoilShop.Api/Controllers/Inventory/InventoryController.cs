using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Authorize, Route("api")]
public sealed class InventoryController(InventoryService service) : ControllerBase
{
    [HttpGet("stock-movements")] public Task<PageResult<MovementDto>> Movements(CancellationToken ct, int? skuId = null, int page = 1, int pageSize = 20) => service.MovementsAsync(skuId, page, pageSize, ct);
    [HttpGet("restock-suggestions")] public Task<IReadOnlyList<SuggestionDto>> Suggestions(CancellationToken ct) => service.SuggestionsAsync(ct);
    [HttpGet("restock-suggestions/search")] public Task<PageResult<SuggestionDto>> SearchSuggestions(CancellationToken ct,string? search=null,int page=1,int pageSize=20)=>service.SuggestionsPageAsync(search,page,pageSize,ct);
}
