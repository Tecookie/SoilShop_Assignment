using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoilShop.Application;
using SoilShop.Domain;
namespace SoilShop.Api;

public sealed record BackupRequest(Guid RequestKey);
[ApiController,Authorize,Route("api")]
public sealed class OperationsController(OperationsService service) : ControllerBase
{
    [HttpGet("summaries")] public Task<PageResult<DailySummary>> Summaries(CancellationToken ct,int page=1,int pageSize=20)=>service.SummariesAsync(page,pageSize,ct);
    [HttpGet("summaries/{date}")] public Task<SummaryResult> Summary(DateOnly date,CancellationToken ct)=>service.SummaryAsync(date,ct);
    [HttpPost("summaries/{date}/refresh")] public async Task<IActionResult> Refresh(DateOnly date,CancellationToken ct)=>Accepted(await service.QueueSummaryAsync(date,ct));
    [HttpGet("backups"),Authorize(Roles="Admin")] public Task<PageResult<BackupRun>> Backups(CancellationToken ct,int page=1,int pageSize=20)=>service.BackupsAsync(page,pageSize,ct);
    [HttpPost("backups"),Authorize(Roles="Admin")] public async Task<IActionResult> Backup(BackupRequest input,CancellationToken ct)=>Accepted(await service.QueueBackupAsync(input.RequestKey,int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),ct));
}

[ApiController,Authorize,Route("api/dashboard")]
public sealed class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet] public Task<DashboardResult> Get(DateOnly from,DateOnly to,CancellationToken ct,string groupBy="day")=>service.GetAsync(from,to,groupBy,ct);
}
