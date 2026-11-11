using Microsoft.EntityFrameworkCore;
using SoilShop.Infrastructure;
using StackExchange.Redis;
using System.Text.Json;

namespace SoilShop.Worker;

public sealed class InventoryConsumer(IServiceScopeFactory scopes, RedisBus redis, WorkerSettings settings, ILogger<InventoryConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = "inventory-" + Environment.MachineName;
        var nextReconciliation = DateTime.MinValue;
        RedisValue claimCursor = "0-0";
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Fresh database state makes delivery duplicates harmless and repairs broker data loss.
                if (DateTime.UtcNow >= nextReconciliation) { await ReconcileAsync(stoppingToken); nextReconciliation = DateTime.UtcNow.AddSeconds(settings.ReconcileSeconds); }
                var broker = await redis.DatabaseAsync();
                try { await broker.StreamCreateConsumerGroupAsync(RedisBus.Stream, RedisBus.Group, "0-0", createStream: true); }
                catch (RedisServerException ex) when (ex.Message.StartsWith("BUSYGROUP", StringComparison.Ordinal)) { }
                var claimed = await broker.StreamAutoClaimAsync(RedisBus.Stream, RedisBus.Group, consumer, settings.ReclaimSeconds * 1000L, claimCursor, count: settings.BatchSize);
                claimCursor = claimed.NextStartId;
                var fresh = await broker.StreamReadGroupAsync(RedisBus.Stream, RedisBus.Group, consumer, ">", count: settings.BatchSize);
                foreach (var entry in claimed.ClaimedEntries.Concat(fresh))
                {
                    try
                    {
                        await DispatchAsync(entry,stoppingToken);
                        await broker.StreamAcknowledgeAsync(RedisBus.Stream, RedisBus.Group, entry.Id);
                        logger.LogInformation("Consumed event {StreamId}", entry.Id);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        // One poison message cannot block the rest of the batch. Redis tracks delivery attempts.
                        try
                        {
                            var pending=await broker.StreamPendingMessagesAsync(RedisBus.Stream,RedisBus.Group,1,consumer,entry.Id,entry.Id);
                            var attempts=pending.Length==0 ? 0 : pending[0].DeliveryCount;
                            if(attempts>settings.MaxRetries)
                            {
                                const string move="local p=redis.call('XPENDING',KEYS[1],ARGV[1],ARGV[2],ARGV[2],1); if #p==0 then return 0 end; redis.call('XADD',KEYS[2],'*','sourceId',ARGV[2],'kind',ARGV[3],'payload',ARGV[4],'errorType',ARGV[5],'attempts',ARGV[6]); redis.call('XACK',KEYS[1],ARGV[1],ARGV[2]); return 1";
                                await broker.ScriptEvaluateAsync(move,[RedisBus.Stream,RedisBus.DeadLetterStream],
                                    [RedisBus.Group,entry.Id,Value(entry,"kind"),Value(entry,"payload"),ex.GetType().Name,attempts]);
                                logger.LogError("Quarantined event {StreamId} after {Attempts} deliveries: {ErrorType}",entry.Id,attempts,ex.GetType().Name);
                            }
                            else logger.LogWarning("Event {StreamId} failed delivery {Attempts}; will retry: {ErrorType}",entry.Id,attempts,ex.GetType().Name);
                        }
                        catch(Exception failure) when(failure is not OperationCanceledException)
                        { logger.LogWarning("Could not update failed event {StreamId}: {ErrorType}",entry.Id,failure.GetType().Name); }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Inventory worker will retry: {ErrorType}", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(settings.PollSeconds), stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
    private static string Value(StreamEntry entry,string name)=>entry.Values.FirstOrDefault(x=>x.Name==name).Value.ToString();
    private async Task DispatchAsync(StreamEntry entry,CancellationToken ct)
    {
        if(!Guid.TryParse(Value(entry,"id"),out _)) throw new FormatException("Invalid event identity.");
        using var payload=JsonDocument.Parse(Value(entry,"payload"));
        var root=payload.RootElement;
        switch(Value(entry,"kind"))
        {
            case "InventoryChanged":
                if(!root.TryGetProperty("skuId",out var sku) || !sku.TryGetInt32(out var id) || id<1) throw new FormatException("Invalid inventory event.");
                await ReconcileAsync(ct); break;
            case "SummaryRequested":
                if(!root.TryGetProperty("businessDate",out var date) || date.ValueKind!=JsonValueKind.String || !DateOnly.TryParseExact(date.GetString(),"yyyy-MM-dd",out _)) throw new FormatException("Invalid summary event.");
                break; // Durable queued database rows are processed by the independent summary loop.
            case "BackupRequested":
                if(!root.TryGetProperty("backupId",out var backup) || backup.ValueKind!=JsonValueKind.String || !Guid.TryParse(backup.GetString(),out _)) throw new FormatException("Invalid backup event.");
                break; // Durable queued database rows are processed by the independent backup loop.
            default: throw new InvalidOperationException("Unsupported event kind.");
        }
    }
    private async Task ReconcileAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(23201)", ct);
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE restock_suggestions r SET status='Resolved',updated_at=now(),version=r.version+1
            FROM purchase_receipts p WHERE r.receipt_id=p.id AND r.status='ReceiptDrafted' AND p.status IN ('Confirmed','Cancelled');
            UPDATE restock_suggestions r SET status='Resolved',updated_at=now(),version=r.version+1
            FROM skus s JOIN products p ON p.id=s.product_id
            WHERE r.sku_id=s.id AND r.status='Open' AND (NOT s.is_active OR NOT p.is_active OR s.stock_qty>s.low_stock_threshold);
            INSERT INTO restock_suggestions (sku_id,supplier_id,observed_qty,suggested_qty)
            SELECT s.id,s.preferred_supplier_id,s.stock_qty,GREATEST(s.target_stock-s.stock_qty,0)
            FROM skus s JOIN products p ON p.id=s.product_id
            WHERE s.is_active AND p.is_active AND s.stock_qty<=s.low_stock_threshold
            ON CONFLICT (sku_id) WHERE status IN ('Open','ReceiptDrafted') DO UPDATE SET
              observed_qty=EXCLUDED.observed_qty,suggested_qty=EXCLUDED.suggested_qty,
              supplier_id=EXCLUDED.supplier_id,updated_at=now(),version=restock_suggestions.version+1
            WHERE restock_suggestions.status='Open' AND
              (restock_suggestions.observed_qty,restock_suggestions.suggested_qty,restock_suggestions.supplier_id)
              IS DISTINCT FROM (EXCLUDED.observed_qty,EXCLUDED.suggested_qty,EXCLUDED.supplier_id);
            """, ct);
        await tx.CommitAsync(ct);
    }
}
