using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SoilShop.Application;
namespace SoilShop.Api;

public sealed class ShopExceptionHandler(IProblemDetailsService problems, ILogger<ShopExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken ct)
    {
        var status = error switch { BusinessException b => b.Status, DbUpdateConcurrencyException => 409, DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "23503" } } => 409, PostgresException { SqlState: "40P01" or "40001" } => 409, _ => 500 };
        var detail = error is BusinessException business ? business.Message : status == 409 ? "Data changed or is referenced by another record. Reload and retry." : "The request could not be completed.";
        if (status == 500) logger.LogError(error, "Unhandled request failure {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = status, Title = detail };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (error is QuoteConflictException conflict) problem.Extensions["quote"] = conflict.Quote;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
