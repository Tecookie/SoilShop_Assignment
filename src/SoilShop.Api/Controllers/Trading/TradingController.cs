using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Authorize, Route("api")]
public sealed class TradingController(TradingService service) : ControllerBase
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("sales")] public Task<PageResult<TradeDto>> Sales(CancellationToken ct, int page = 1, int pageSize = 20) => service.TradesAsync(false, page, pageSize, ct);
    [HttpGet("purchase-receipts")] public Task<PageResult<TradeDto>> Receipts(CancellationToken ct, int page = 1, int pageSize = 20) => service.TradesAsync(true, page, pageSize, ct);
    [HttpGet("sales/{id:int}")] public Task<TradeDto> Sale(int id, CancellationToken ct) => service.TradeAsync(false, id, ct);
    [HttpGet("purchase-receipts/{id:int}")] public Task<TradeDto> Receipt(int id, CancellationToken ct) => service.TradeAsync(true, id, ct);
    [HttpPost("sales")] public async Task<ActionResult<TradeDto>> CreateSale(TradeInput input, CancellationToken ct) { var item = await service.CreateTradeAsync(false, input, ActorId, ct); return Created($"/api/sales/{item.Id}", item); }
    [HttpPost("purchase-receipts")] public async Task<ActionResult<TradeDto>> CreateReceipt(TradeInput input, CancellationToken ct) { var item = await service.CreateTradeAsync(true, input, ActorId, ct); return Created($"/api/purchase-receipts/{item.Id}", item); }
    [HttpPut("sales/{id:int}")] public Task<TradeDto> UpdateSale(int id, UpdateTradeInput input, CancellationToken ct) => service.UpdateTradeAsync(false, id, input, ct);
    [HttpPut("purchase-receipts/{id:int}")] public Task<TradeDto> UpdateReceipt(int id, UpdateTradeInput input, CancellationToken ct) => service.UpdateTradeAsync(true, id, input, ct);
    [HttpDelete("sales/{id:int}")] public async Task<IActionResult> DeleteSale(int id, long expectedVersion, CancellationToken ct) { await service.DeleteTradeAsync(false, id, expectedVersion, ct); return NoContent(); }
    [HttpDelete("purchase-receipts/{id:int}")] public async Task<IActionResult> DeleteReceipt(int id, long expectedVersion, CancellationToken ct) { await service.DeleteTradeAsync(true, id, expectedVersion, ct); return NoContent(); }
    [HttpGet("sales/{id:int}/quote")] public Task<TradeQuote> SaleQuote(int id, CancellationToken ct) => service.QuoteAsync(false, id, ct);
    [HttpGet("purchase-receipts/{id:int}/quote")] public Task<TradeQuote> ReceiptQuote(int id, CancellationToken ct) => service.QuoteAsync(true, id, ct);
    [HttpPost("sales/{id:int}/confirm")] public Task<TradeDto> ConfirmSale(int id, ConfirmInput input, CancellationToken ct) => service.ConfirmAsync(false, id, input, ActorId, ct);
    [HttpPost("purchase-receipts/{id:int}/confirm")] public Task<TradeDto> ConfirmReceipt(int id, ConfirmInput input, CancellationToken ct) => service.ConfirmAsync(true, id, input, ActorId, ct);
    [HttpPost("sales/{id:int}/cancel"), Authorize(Roles = "Admin")] public Task<TradeDto> CancelSale(int id, CancelTradeInput input, CancellationToken ct) => service.CancelTradeAsync(false, id, input, ActorId, ct);
    [HttpPost("purchase-receipts/{id:int}/cancel"), Authorize(Roles = "Admin")] public Task<TradeDto> CancelReceipt(int id, CancelTradeInput input, CancellationToken ct) => service.CancelTradeAsync(true, id, input, ActorId, ct);
}
