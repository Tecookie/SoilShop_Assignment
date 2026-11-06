using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

/// <summary>Opt-in fictional, classification-only advice fixtures. No scientific measurements.</summary>
public sealed class DemoAdviceSeeder(ShopDbContext db, ShopService service)
{
    private const string ReceiptCode = "ADVICE-V1-IN-001";
    private const string Source = "DỮ LIỆU GIẢ LẬP cho assignment: nhãn mẫu chỉ ghi nhóm cây sử dụng. Không có số đo, kiểm nghiệm hay bảo đảm phù hợp thực tế.";
    private static readonly (string Type, string Group, string Name)[] Samples =
    [
        ("GeneralPurpose", "General", "Đất trồng đa dụng [mẫu tư vấn]"),
        ("FoliageMix", "Foliage", "Đất trồng kiểng lá [mẫu tư vấn]"),
        ("FloweringMix", "Flowering", "Đất trồng cây hoa [mẫu tư vấn]"),
        ("SucculentMix", "Succulent", "Đất sen đá và xương rồng [mẫu tư vấn]"),
        ("OrchidMix", "Orchid", "Giá thể trồng lan [mẫu tư vấn]"),
        ("SeedStartingMix", "Seedling", "Giá thể ươm hạt [mẫu tư vấn]")
    ];
    public async Task RunAsync(CancellationToken ct = default)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(23203)", ct);
            try { await SeedAsync(ct); }
            finally { await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(23203)", CancellationToken.None); }
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
    private async Task SeedAsync(CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        if (await db.PurchaseReceipts.AnyAsync(x => x.Code == ReceiptCode && x.Status != "Draft", ct)) return;
        var actor = await db.Users.Where(x => x.IsActive && x.Role == "Admin").OrderBy(x => x.Id).Select(x => x.Id).FirstAsync(ct);
        PurchaseReceipt receipt;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Code == "ADVICE-V1-SUP", ct);
            if (supplier is null)
            {
                supplier = new Supplier { Code = "ADVICE-V1-SUP", Name = "Nhà cung cấp [mẫu tư vấn]", ContactNote = "Fictional assignment fixture." };
                db.Suppliers.Add(supplier); await db.SaveChangesAsync(ct);
            }
            var skus = new List<Sku>();
            for (var i = 0; i < Samples.Length; i++)
            {
                var sample = Samples[i]; var code = $"ADVICE-V1-P{i + 1:00}";
                var product = await db.Products.SingleOrDefaultAsync(x => x.Code == code, ct);
                if (product is null)
                {
                    // Explicit import curates only the fictional label's use tag; all measurements stay unknown.
                    product = new Product { Code = code, Name = sample.Name, ProductType = sample.Type, Brand = "Advice Demo", SourceReference = Source, ReviewedBy = actor, ReviewedAt = DateTime.UtcNow };
                    db.Products.Add(product); await db.SaveChangesAsync(ct);
                    var name = SoilOptions.PlantGroups[sample.Group];
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO plant_tags (code, name) VALUES ({sample.Group}, {name}) ON CONFLICT (code) DO NOTHING", ct);
                    var tag = await db.PlantTags.SingleAsync(x => x.Code == sample.Group, ct);
                    db.ProductPlantTags.Add(new ProductPlantTag { ProductId = product.Id, PlantTagId = tag.Id });
                }
                else if (product.Brand != "Advice Demo") throw new BusinessException(409, $"Reserved advice demo code {code} is already used. Existing data was preserved.");
                var sku = await db.Skus.SingleOrDefaultAsync(x => x.Code == code + "-5L", ct);
                if (sku is null)
                {
                    sku = new Sku { ProductId = product.Id, Code = code + "-5L", PackageAmount = 5, PackageUnit = "L", SalePriceVnd = 50000 + i * 5000, PreferredSupplierId = supplier.Id };
                    db.Skus.Add(sku); await db.SaveChangesAsync(ct);
                }
                if (sku.ProductId != product.Id || sku.PackageUnit != "L" || sku.PackageAmount != 5) throw new BusinessException(409, "Advice demo packaging code conflicts with existing data.");
                skus.Add(sku);
            }
            receipt = (await db.PurchaseReceipts.SingleOrDefaultAsync(x => x.Code == ReceiptCode, ct))!;
            if (receipt is null)
            {
                receipt = new PurchaseReceipt { Code = ReceiptCode, SupplierId = supplier.Id, CreatedBy = actor };
                db.PurchaseReceipts.Add(receipt); await db.SaveChangesAsync(ct);
                db.PurchaseReceiptLines.AddRange(skus.Select(x => new PurchaseReceiptLine { ReceiptId = receipt.Id, SkuId = x.Id, Quantity = 8, UnitCostVnd = 30000 }));
            }
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        var id = receipt.Id; var version = receipt.Version; db.ChangeTracker.Clear();
        await service.ConfirmAsync(true, id, new(new Guid("e7b6fdc9-35e3-4b1b-a1ce-a902164ec076"), version), actor, ct);
        db.ChangeTracker.Clear();
    }
}
