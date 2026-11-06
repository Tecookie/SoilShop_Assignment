using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Authorize, Route("api/product-advice")]
public sealed class ProductAdviceController(ProductAdviceService service) : ControllerBase
{
    [HttpPost] public Task<ProductAdviceResponse> Recommend(ProductAdviceInput input, CancellationToken ct) => service.RecommendAsync(input, ct);
}
