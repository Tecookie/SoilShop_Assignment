using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Authorize, Route("api/suppliers")]
public sealed class SuppliersController(CatalogueService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<SupplierDto>> List(CancellationToken ct) => service.SuppliersAsync(ct);
    [HttpPost, Authorize(Roles = "Admin")] public async Task<ActionResult<SupplierDto>> Create(SupplierInput input, CancellationToken ct) { var item = await service.AddSupplierAsync(input, ct); return Created("/api/suppliers", item); }
}
