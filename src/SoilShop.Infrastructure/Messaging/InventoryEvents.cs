using System.Text.Json;
using SoilShop.Domain;
namespace SoilShop.Infrastructure;

internal static class InventoryEvents
{
    internal static void Add(ShopDbContext db, int skuId) => db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), Kind = "InventoryChanged", Payload = JsonSerializer.Serialize(new { skuId }) });
}
