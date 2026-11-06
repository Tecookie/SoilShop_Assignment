using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Authorize, Route("api/products/{id:int}/metadata")]
public sealed class SoilMetadataController(SoilMetadataService service) : ControllerBase
{
    [HttpGet] public Task<SoilMetadataDto> Get(int id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpPut, Authorize(Roles = "Admin")] public Task<SoilMetadataDto> Save(int id, SoilMetadataInput input, CancellationToken ct) => service.SaveAsync(id, input, ct);
    [HttpPost("review"), Authorize(Roles = "Admin")] public Task<SoilMetadataDto> Review(int id, ReviewMetadataInput input, CancellationToken ct) => service.ReviewAsync(id, input, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
}
