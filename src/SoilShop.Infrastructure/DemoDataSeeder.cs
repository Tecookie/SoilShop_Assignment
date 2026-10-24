using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed record DemoSeedResult(bool AlreadyApplied, int Products, int Skus, string Message);

/// <summary>Explicit fictional demo import. Never assigns opening stock directly.</summary>
public sealed class DemoDataSeeder(ShopDbContext db, ShopService service, IConfiguration configuration)
{
    private const string CompletionCode = "SEED-V1-OUT-003";
    private sealed record Sample(string Code, string Name, string Type, string Unit, int Small, int Large, decimal Price);
    private static readonly Sample[] Samples =
    [
        new("SEED-V1-P01", "Đất trồng đa dụng [mẫu]", "GeneralPurpose", "kg", 5, 10, 55000),
        new("SEED-V1-P02", "Đất trồng cây kiểng lá [mẫu]", "FoliageMix", "L", 5, 20, 65000),
        new("SEED-V1-P03", "Đất trồng cây hoa [mẫu]", "FloweringMix", "kg", 5, 10, 60000),
        new("SEED-V1-P04", "Đất trồng sen đá và xương rồng [mẫu]", "SucculentMix", "L", 2, 5, 35000),
        new("SEED-V1-P05", "Giá thể trồng lan [mẫu]", "OrchidMix", "L", 5, 10, 70000),
        new("SEED-V1-P06", "Giá thể ươm hạt [mẫu]", "SeedStartingMix", "L", 2, 5, 30000),
        new("SEED-V1-P07", "Đất trồng đa dụng nhẹ [mẫu]", "GeneralPurpose", "L", 10, 20, 90000),
        new("SEED-V1-P08", "Đất trồng kiểng lá trong nhà [mẫu]", "FoliageMix", "L", 5, 10, 75000)
    ];
    private static readonly int[] Inbound = [3, 14, 6, 30, 8, 12, 2, 10, 4, 18, 12, 24, 16, 30, 8, 10];

    public async Task<DemoSeedResult> RunAsync(CancellationToken ct = default)
    {
        // Session lock serializes repeated seed processes without holding stock row locks over gRPC.
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(23202)", ct);
            try { return await SeedAsync(ct); }
            finally { await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(23202)", CancellationToken.None); }
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
    private async Task<DemoSeedResult> SeedAsync(CancellationToken ct)
    {
        if (await db.Sales.AnyAsync(x => x.Code == CompletionCode && x.Status != "Draft", ct))
            return new(true, 8, 16, "Demo v1 already applied. Existing prices, stock, accounts and staff changes preserved.");
        var actor = await db.Users.Where(x => x.IsActive && x.Role == "Admin").OrderBy(x => x.Id).Select(x => x.Id).FirstAsync(ct);
        var suppliers = new List<Supplier>(); var skus = new List<Sku>();
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            for (var i = 1; i <= 3; i++)
            {
                var code = $"SEED-V1-SUP-0{i}";
                var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Code == code, ct);
                if (supplier is null)
                {
                    supplier = new Supplier { Code = code, Name = $"Demo Growing Media Supplier {i}", ContactNote = "Fictional supplier for assignment demonstration; no real contact information." };
                    db.Suppliers.Add(supplier); await db.SaveChangesAsync(ct);
                }
                suppliers.Add(supplier);
            }
            for (var i = 0; i < Samples.Length; i++)
            {
                var sample = Samples[i];
                var product = await db.Products.SingleOrDefaultAsync(x => x.Code == sample.Code, ct);
                if (product is null)
                {
                    product = new Product { Code = sample.Code, Name = sample.Name, ProductType = sample.Type, Brand = "Assignment Demo", StorageNote = "Follow the supplied package label. Demo data only.", SourceReference = "Fictional demo package description. Measurements and suitability are not verified." };
                    db.Products.Add(product); await db.SaveChangesAsync(ct);
                }
                foreach (var amount in new[] { sample.Small, sample.Large })
                {
                    var code = $"{sample.Code}-{amount}{sample.Unit.ToUpperInvariant()}";
                    var sku = await db.Skus.SingleOrDefaultAsync(x => x.Code == code, ct);
                    if (sku is null)
                    {
                        sku = new Sku { ProductId = product.Id, Code = code, PackageAmount = amount, PackageUnit = sample.Unit, SalePriceVnd = amount == sample.Small ? sample.Price : sample.Price * 2, ReferenceCostVnd = (amount == sample.Small ? sample.Price : sample.Price * 2) * 0.6m, PreferredSupplierId = suppliers[i % 3].Id };
                        db.Skus.Add(sku); await db.SaveChangesAsync(ct);
                    }
                    if (sku.ProductId != product.Id || sku.PackageAmount != amount || sku.PackageUnit != sample.Unit)
                        throw new BusinessException(409, $"Demo code {code} is already used for different packaging. Existing data was not replaced.");
                    skus.Add(sku);
                }
            }
            await AddEmployeeAsync(ct);
            await tx.CommitAsync(ct);
        }
        db.ChangeTracker.Clear();
        for (var group = 0; group < 2; group++)
        {
            var code = $"SEED-V1-IN-00{group + 1}";
            await EnsureReceiptAsync(code, suppliers[group].Id, actor, skus, Enumerable.Range(group * 8, 8).Select(i => (i, Inbound[i])), true, ct);
        }
        await EnsureReceiptAsync("SEED-V1-IN-DRAFT", suppliers[2].Id, actor, skus, [(0, 10), (2, 6)], false, ct);
        await EnsureSaleAsync("SEED-V1-OUT-001", actor, skus, [(0, 3), (1, 2), (2, 5), (3, 3)], true, ct);
        await EnsureSaleAsync("SEED-V1-OUT-002", actor, skus, [(4, 4), (5, 2), (6, 2), (8, 1)], true, ct);
        await EnsureSaleAsync("SEED-V1-OUT-DRAFT", actor, skus, [(11, 1), (13, 2)], false, ct);
        // Last packaging is inactive with retained historical stock; it was received while active.
        db.ChangeTracker.Clear();
        var lastId = skus[^1].Id;
        var last = await db.Skus.SingleAsync(x => x.Id == lastId, ct);
        if (last.IsActive)
            await service.SaveSkuAsync(last.Id, new(last.ProductId, last.Code, last.PackageAmount, last.PackageUnit, last.SalePriceVnd, last.LowStockThreshold, last.TargetStock, last.PreferredSupplierId, last.Version, false), ct);
        db.ChangeTracker.Clear();
        // This final confirmed document is the durable completion marker. Reruns never reset data.
        await EnsureSaleAsync(CompletionCode, actor, skus, [(9, 2), (10, 4), (12, 4), (14, 2)], true, ct);
        return new(false, 8, 16, "Seeded fictional catalogue, 2 confirmed/1 draft receipts and 3 confirmed/1 draft sales. Stock has a ledger trail.");
    }
    private async Task AddEmployeeAsync(CancellationToken ct)
    {
        var username = configuration["DemoSeed:EmployeeUsername"];
        var password = configuration["DemoSeed:EmployeePassword"];
        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100 || string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("Demo Employee requires a username and password of at least 12 characters.");
        username = username.Trim().ToLowerInvariant();
        var existing = await db.Users.SingleOrDefaultAsync(x => x.Username == username, ct);
        if (existing is not null)
        {
            if (existing.Role != "Employee") throw new BusinessException(409, "Demo employee username is used by another role.");
            return;
        }
        var user = new User { Username = username, DisplayName = "Demo shop employee", Role = "Employee" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user); await db.SaveChangesAsync(ct);
    }
    private async Task EnsureReceiptAsync(string code, int supplier, int actor, IReadOnlyList<Sku> skus, IEnumerable<(int Index, int Quantity)> lines, bool confirm, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        var doc = await db.PurchaseReceipts.SingleOrDefaultAsync(x => x.Code == code, ct);
        if (doc is null)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            doc = new PurchaseReceipt { Code = code, SupplierId = supplier, CreatedBy = actor };
            db.PurchaseReceipts.Add(doc); await db.SaveChangesAsync(ct);
            db.PurchaseReceiptLines.AddRange(lines.Select(x => new PurchaseReceiptLine { ReceiptId = doc.Id, SkuId = skus[x.Index].Id, Quantity = x.Quantity, UnitCostVnd = skus[x.Index].ReferenceCostVnd ?? skus[x.Index].SalePriceVnd * 0.6m }));
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        var id = doc.Id; var version = doc.Version; var status = doc.Status; db.ChangeTracker.Clear();
        if (confirm && status == "Draft") await service.ConfirmAsync(true, id, new(Key(code), version), actor, ct);
        db.ChangeTracker.Clear();
    }
    private async Task EnsureSaleAsync(string code, int actor, IReadOnlyList<Sku> skus, IEnumerable<(int Index, int Quantity)> lines, bool confirm, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        var doc = await db.Sales.SingleOrDefaultAsync(x => x.Code == code, ct);
        if (doc is null)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            doc = new Sale { Code = code, CreatedBy = actor }; db.Sales.Add(doc); await db.SaveChangesAsync(ct);
            db.SaleLines.AddRange(lines.Select(x => new SaleLine { SaleId = doc.Id, SkuId = skus[x.Index].Id, Quantity = x.Quantity }));
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        var id = doc.Id; var version = doc.Version; var status = doc.Status; db.ChangeTracker.Clear();
        if (confirm && status == "Draft") await service.ConfirmAsync(false, id, new(Key(code), version), actor, ct);
        db.ChangeTracker.Clear();
    }
    private static Guid Key(string code) => new(SHA256.HashData(Encoding.UTF8.GetBytes("soilshop-demo-v1/" + code)).AsSpan(0, 16));
}
