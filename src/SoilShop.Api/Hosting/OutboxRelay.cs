using Microsoft.EntityFrameworkCore;
using SoilShop.Infrastructure;
using StackExchange.Redis;

namespace SoilShop.Api;

public sealed class OutboxRelay(IServiceScopeFactory scopes, RedisBus redis, IConfiguration config, ILogger<OutboxRelay> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Messaging:Enabled", true)) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
                var messages = await db.OutboxMessages.Where(x => x.PublishedAt == null).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Take(50).ToListAsync(stoppingToken);
                if (messages.Count > 0)
                {
                    var broker = await redis.DatabaseAsync();
                    foreach (var message in messages)
                    {
                        await broker.StreamAddAsync(RedisBus.Stream, [new NameValueEntry("id", message.Id.ToString()), new NameValueEntry("kind", message.Kind), new NameValueEntry("payload", message.Payload)]);
                        message.PublishedAt = DateTime.UtcNow; message.Attempts++; message.LastError = null;
                        await db.SaveChangesAsync(stoppingToken);
                        logger.LogInformation("Published outbox {EventId} {Kind}", message.Id, message.Kind);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Outbox relay will retry: {ErrorType}", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
}
