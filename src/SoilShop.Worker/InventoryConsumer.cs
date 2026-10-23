using Microsoft.EntityFrameworkCore;
using SoilShop.Infrastructure;
using StackExchange.Redis;

namespace SoilShop.Worker;

public sealed class InventoryConsumer(IServiceScopeFactory scopes, RedisBus redis, ILogger<InventoryConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = "inventory-" + Environment.MachineName;
        var nextReconciliation = DateTime.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Fresh database state makes delivery duplicates harmless and repairs broker data loss.
                if (DateTime.UtcNow >= nextReconciliation) { await ReconcileAsync(stoppingToken); nextReconciliation = DateTime.UtcNow.AddMinutes(1); }
                var broker = await redis.DatabaseAsync();
                try { await broker.StreamCreateConsumerGroupAsync(RedisBus.Stream, RedisBus.Group, "0-0", createStream: true); }
                catch (RedisServerException ex) when (ex.Message.StartsWith("BUSYGROUP", StringComparison.Ordinal)) { }
                var claimed = await broker.StreamAutoClaimAsync(RedisBus.Stream, RedisBus.Group, consumer, 60000, "0-0", count: 25);
                var fresh = await broker.StreamReadGroupAsync(RedisBus.Stream, RedisBus.Group, consumer, ">", count: 25);
                foreach (var entry in claimed.ClaimedEntries.Concat(fresh))
                {
                    var kind = entry.Values.FirstOrDefault(x => x.Name == "kind").Value.ToString();
                    if (kind != "InventoryChanged") throw new InvalidOperationException("Unsupported event kind; leave pending for later implementation.");
                    await ReconcileAsync(stoppingToken);
                    await broker.StreamAcknowledgeAsync(RedisBus.Stream, RedisBus.Group, entry.Id);
                    logger.LogInformation("Consumed inventory event {StreamId}", entry.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Inventory worker will retry: {ErrorType}", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
    private async Task ReconcileAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(23201)", ct);
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE restock_suggestions r SET status='Resolved',updated_at=now(),version=r.version+1
            FROM skus s JOIN products p ON p.id=s.product_id
            WHERE r.sku_id=s.id AND r.status='Open' AND (NOT s.is_active OR NOT p.is_active OR s.stock_qty>s.low_stock_threshold);
            INSERT INTO restock_suggestions (sku_id,supplier_id,observed_qty,suggested_qty)
            SELECT s.id,s.preferred_supplier_id,s.stock_qty,GREATEST(s.target_stock-s.stock_qty,0)
            FROM skus s JOIN products p ON p.id=s.product_id
            WHERE s.is_active AND p.is_active AND s.stock_qty<=s.low_stock_threshold
            ON CONFLICT (sku_id) WHERE status IN ('Open','ReceiptDrafted') DO UPDATE SET
              observed_qty=EXCLUDED.observed_qty,suggested_qty=EXCLUDED.suggested_qty,
              supplier_id=EXCLUDED.supplier_id,updated_at=now(),version=restock_suggestions.version+1;
            """, ct);
        await tx.CommitAsync(ct);
    }
}
