using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Authorize, Route("api/skus")]
public sealed class SkusController(CatalogueService service) : ControllerBase
{
    [HttpGet] public Task<PageResult<SkuDto>> List(CancellationToken ct, string? search = null, bool lowOnly = false, int? productId = null, int page = 1, int pageSize = 20, string? productType = null) => service.SkusAsync(search, lowOnly, productId, page, pageSize, ct, productType);
    [HttpGet("{id:int}")] public Task<SkuDto> Get(int id, CancellationToken ct) => service.SkuAsync(id, ct);
    [HttpPost, Authorize(Roles = "Admin")] public async Task<ActionResult<SkuDto>> Create(SkuInput input, CancellationToken ct) { var item = await service.SaveSkuAsync(null, input, ct); return Created($"/api/skus/{item.Id}", item); }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin")] public Task<SkuDto> Update(int id, SkuInput input, CancellationToken ct) => service.SaveSkuAsync(id, input, ct);
}
