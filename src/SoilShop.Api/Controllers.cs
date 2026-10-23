using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login"), EnableRateLimiting("login")]
    public Task<LoginResult> Login(LoginInput input, CancellationToken ct) => auth.LoginAsync(input, ct);
    [HttpGet("me"), Authorize]
    public Task<UserDto> Me(CancellationToken ct) => auth.MeAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
}
[ApiController, Authorize, Route("api/products")]
public sealed class ProductsController(ShopService service) : ControllerBase
{
    [HttpGet] public Task<PageResult<ProductDto>> List(CancellationToken ct, string? search = null, bool? active = null, string sort = "name", int page = 1, int pageSize = 20) => service.ProductsAsync(search, active, sort, page, pageSize, ct);
    [HttpGet("{id:int}")] public Task<ProductDto> Get(int id, CancellationToken ct) => service.ProductAsync(id, ct);
    [HttpPost, Authorize(Roles = "Admin")] public async Task<ActionResult<ProductDto>> Create(ProductInput input, CancellationToken ct) { var item = await service.SaveProductAsync(null, input, ct); return Created($"/api/products/{item.Id}", item); }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin")] public Task<ProductDto> Update(int id, ProductInput input, CancellationToken ct) => service.SaveProductAsync(id, input, ct);
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { await service.DeleteProductAsync(id, ct); return NoContent(); }
}
[ApiController, Authorize, Route("api/skus")]
public sealed class SkusController(ShopService service) : ControllerBase
{
    [HttpGet] public Task<PageResult<SkuDto>> List(CancellationToken ct, string? search = null, bool lowOnly = false, int? productId = null, int page = 1, int pageSize = 20) => service.SkusAsync(search, lowOnly, productId, page, pageSize, ct);
    [HttpGet("{id:int}")] public Task<SkuDto> Get(int id, CancellationToken ct) => service.SkuAsync(id, ct);
    [HttpPost, Authorize(Roles = "Admin")] public async Task<ActionResult<SkuDto>> Create(SkuInput input, CancellationToken ct) { var item = await service.SaveSkuAsync(null, input, ct); return Created($"/api/skus/{item.Id}", item); }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin")] public Task<SkuDto> Update(int id, SkuInput input, CancellationToken ct) => service.SaveSkuAsync(id, input, ct);
}
[ApiController, Authorize, Route("api/suppliers")]
public sealed class SuppliersController(ShopService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<SupplierDto>> List(CancellationToken ct) => service.SuppliersAsync(ct);
    [HttpPost, Authorize(Roles = "Admin")] public async Task<ActionResult<SupplierDto>> Create(SupplierInput input, CancellationToken ct) { var item = await service.AddSupplierAsync(input, ct); return Created("/api/suppliers", item); }
}
[ApiController, Authorize, Route("api")]
public sealed class TradingController(ShopService service) : ControllerBase
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("sales")] public Task<PageResult<TradeDto>> Sales(CancellationToken ct, int page = 1, int pageSize = 20) => service.TradesAsync(false, page, pageSize, ct);
    [HttpGet("purchase-receipts")] public Task<PageResult<TradeDto>> Receipts(CancellationToken ct, int page = 1, int pageSize = 20) => service.TradesAsync(true, page, pageSize, ct);
    [HttpGet("sales/{id:int}")] public Task<TradeDto> Sale(int id, CancellationToken ct) => service.TradeAsync(false, id, ct);
    [HttpGet("purchase-receipts/{id:int}")] public Task<TradeDto> Receipt(int id, CancellationToken ct) => service.TradeAsync(true, id, ct);
    [HttpPost("sales")] public async Task<ActionResult<TradeDto>> CreateSale(TradeInput input, CancellationToken ct) { var item = await service.CreateTradeAsync(false, input, ActorId, ct); return Created($"/api/sales/{item.Id}", item); }
    [HttpPost("purchase-receipts")] public async Task<ActionResult<TradeDto>> CreateReceipt(TradeInput input, CancellationToken ct) { var item = await service.CreateTradeAsync(true, input, ActorId, ct); return Created($"/api/purchase-receipts/{item.Id}", item); }
    [HttpPost("sales/{id:int}/confirm")] public Task<TradeDto> ConfirmSale(int id, ConfirmInput input, CancellationToken ct) => service.ConfirmAsync(false, id, input, ActorId, ct);
    [HttpPost("purchase-receipts/{id:int}/confirm")] public Task<TradeDto> ConfirmReceipt(int id, ConfirmInput input, CancellationToken ct) => service.ConfirmAsync(true, id, input, ActorId, ct);
    [HttpGet("stock-movements")] public Task<PageResult<MovementDto>> Movements(CancellationToken ct, int? skuId = null, int page = 1, int pageSize = 20) => service.MovementsAsync(skuId, page, pageSize, ct);
    [HttpGet("restock-suggestions")] public Task<IReadOnlyList<SuggestionDto>> Suggestions(CancellationToken ct) => service.SuggestionsAsync(ct);
}
