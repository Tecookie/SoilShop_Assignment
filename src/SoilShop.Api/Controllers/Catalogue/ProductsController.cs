using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Authorize, Route("api/products")]
public sealed class ProductsController(CatalogueService service) : ControllerBase
{
    [HttpGet] public Task<PageResult<ProductDto>> List(CancellationToken ct, string? search = null, bool? active = null, string sort = "name", int page = 1, int pageSize = 20, string? productType = null) => service.ProductsAsync(search, active, sort, page, pageSize, ct, productType);
    [HttpGet("{id:int}")] public Task<ProductDto> Get(int id, CancellationToken ct) => service.ProductAsync(id, ct);
    [HttpPost, Authorize(Roles = "Admin")] public async Task<ActionResult<ProductDto>> Create(ProductInput input, CancellationToken ct) { var item = await service.SaveProductAsync(null, input, ct); return Created($"/api/products/{item.Id}", item); }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin")] public Task<ProductDto> Update(int id, ProductInput input, CancellationToken ct) => service.SaveProductAsync(id, input, ct);
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { await service.DeleteProductAsync(id, ct); return NoContent(); }
}
