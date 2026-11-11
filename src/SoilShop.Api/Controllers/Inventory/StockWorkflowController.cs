using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoilShop.Application;
using SoilShop.Domain;
namespace SoilShop.Api;

[ApiController,Authorize,Route("api")]
public sealed class StockWorkflowController(StockWorkflowService service,TradingService trading) : ControllerBase
{
    private int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("stock-counts")] public Task<PageResult<StockCount>> Counts(CancellationToken ct,int page=1,int pageSize=20)=>service.CountsAsync(page,pageSize,ct);
    [HttpPost("stock-counts")] public Task<StockCount> Count(CountInput input,CancellationToken ct)=>service.CountAsync(input,Actor,ct);
    [HttpPost("stock-counts/{id:int}/review"),Authorize(Roles="Admin")] public Task<StockCount> Review(int id,CountReviewInput input,CancellationToken ct)=>service.ReviewAsync(id,input,Actor,ct);
    [HttpPost("stock-adjustments"),Authorize(Roles="Admin")] public Task<StockAdjustment> Adjust(AdjustmentInput input,CancellationToken ct)=>service.AdjustAsync(input,Actor,ct);
    [HttpPost("restock-suggestions/{id:int}/convert")] public async Task<TradeDto> Convert(int id,ConvertSuggestionInput input,CancellationToken ct)=>await trading.TradeAsync(true,await service.ConvertAsync(id,input,Actor,ct),ct);
}
