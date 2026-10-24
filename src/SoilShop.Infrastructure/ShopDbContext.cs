using Microsoft.EntityFrameworkCore;
using SoilShop.Domain;
namespace SoilShop.Infrastructure;
public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PlantTag> PlantTags => Set<PlantTag>();
    public DbSet<ProductPlantTag> ProductPlantTags => Set<ProductPlantTag>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<RestockSuggestion> RestockSuggestions => Set<RestockSuggestion>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();
    public DbSet<DailySkuSnapshot> DailySkuSnapshots => Set<DailySkuSnapshot>();
    public DbSet<BackupRun> BackupRuns => Set<BackupRun>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users", t =>
            {
                t.HasCheckConstraint("ck_users_1", "role IN ('Admin','Employee')");
                t.HasCheckConstraint("ck_users_2", "length(trim(username)) > 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.Username).HasColumnName("username").HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.DisplayName).HasColumnName("display_name").HasColumnType("varchar(150)").IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("text").IsRequired();
            e.Property(x => x.Role).HasColumnName("role").HasColumnType("varchar(16)").IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active").HasColumnType("boolean").IsRequired().HasDefaultValueSql("true");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasIndex(x => new { x.Username }).IsUnique().HasDatabaseName("uq_users_1");
        });
        modelBuilder.Entity<Supplier>(e =>
        {
            e.ToTable("suppliers", t =>
            {
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.Code).HasColumnName("code").HasColumnType("varchar(40)").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(200)").IsRequired();
            e.Property(x => x.ContactNote).HasColumnName("contact_note").HasColumnType("text");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasColumnType("boolean").IsRequired().HasDefaultValueSql("true");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("uq_suppliers_1");
        });
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products", t =>
            {
                t.HasCheckConstraint("ck_products_1", "drainage IS NULL OR drainage IN ('Low','Medium','High')");
                t.HasCheckConstraint("ck_products_2", "water_retention IS NULL OR water_retention IN ('Low','Medium','High')");
                t.HasCheckConstraint("ck_products_3", "(ph_min IS NULL AND ph_max IS NULL) OR (ph_min IS NOT NULL AND ph_max IS NOT NULL AND ph_min BETWEEN 0 AND 14 AND ph_max BETWEEN ph_min AND 14)");
                t.HasCheckConstraint("ck_products_4", "(reviewed_by IS NULL AND reviewed_at IS NULL) OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL AND source_reference IS NOT NULL AND length(trim(source_reference))>0)");
                t.HasCheckConstraint("ck_products_5", "product_type IN ('Unclassified','GeneralPurpose','FoliageMix','FloweringMix','SucculentMix','OrchidMix','SeedStartingMix')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.Code).HasColumnName("code").HasColumnType("varchar(40)").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(200)").IsRequired();
            e.Property(x => x.Brand).HasColumnName("brand").HasColumnType("varchar(150)");
            e.Property(x => x.Manufacturer).HasColumnName("manufacturer").HasColumnType("varchar(150)");
            e.Property(x => x.Ingredients).HasColumnName("ingredients").HasColumnType("text");
            e.Property(x => x.Drainage).HasColumnName("drainage").HasColumnType("varchar(12)");
            e.Property(x => x.WaterRetention).HasColumnName("water_retention").HasColumnType("varchar(12)");
            e.Property(x => x.PhMin).HasColumnName("ph_min").HasColumnType("numeric(4,2)");
            e.Property(x => x.PhMax).HasColumnName("ph_max").HasColumnType("numeric(4,2)");
            e.Property(x => x.StorageNote).HasColumnName("storage_note").HasColumnType("text");
            e.Property(x => x.SourceReference).HasColumnName("source_reference").HasColumnType("text");
            e.Property(x => x.ProductType).HasColumnName("product_type").HasColumnType("varchar(24)").IsRequired().HasDefaultValueSql("'Unclassified'");
            e.Property(x => x.ReviewedBy).HasColumnName("reviewed_by").HasColumnType("integer");
            e.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamptz");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasColumnType("boolean").IsRequired().HasDefaultValueSql("true");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("uq_products_1");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PlantTag>(e =>
        {
            e.ToTable("plant_tags", t =>
            {
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.Code).HasColumnName("code").HasColumnType("varchar(40)").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(100)").IsRequired();
            e.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("uq_plant_tags_1");
        });
        modelBuilder.Entity<ProductPlantTag>(e =>
        {
            e.ToTable("product_plant_tags", t =>
            {
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.ProductId).HasColumnName("product_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.PlantTagId).HasColumnName("plant_tag_id").HasColumnType("integer").IsRequired();
            e.HasIndex(x => new { x.ProductId, x.PlantTagId }).IsUnique().HasDatabaseName("uq_product_plant_tags_1");
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<PlantTag>().WithMany().HasForeignKey(x => x.PlantTagId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Sku>(e =>
        {
            e.ToTable("skus", t =>
            {
                t.HasCheckConstraint("ck_skus_1", "package_unit IN ('kg','L')");
                t.HasCheckConstraint("ck_skus_2", "package_amount > 0");
                t.HasCheckConstraint("ck_skus_3", "sale_price_vnd > 0");
                t.HasCheckConstraint("ck_skus_4", "reference_cost_vnd IS NULL OR reference_cost_vnd > 0");
                t.HasCheckConstraint("ck_skus_5", "stock_qty >= 0");
                t.HasCheckConstraint("ck_skus_6", "stock_version >= 0");
                t.HasCheckConstraint("ck_skus_7", "low_stock_threshold >= 0 AND target_stock > low_stock_threshold");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.ProductId).HasColumnName("product_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.Code).HasColumnName("code").HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.PackageAmount).HasColumnName("package_amount").HasColumnType("numeric(10,2)").IsRequired();
            e.Property(x => x.PackageUnit).HasColumnName("package_unit").HasColumnType("varchar(8)").IsRequired();
            e.Property(x => x.SalePriceVnd).HasColumnName("sale_price_vnd").HasColumnType("numeric(18,0)").IsRequired();
            e.Property(x => x.ReferenceCostVnd).HasColumnName("reference_cost_vnd").HasColumnType("numeric(18,0)");
            e.Property(x => x.StockQty).HasColumnName("stock_qty").HasColumnType("integer").IsRequired().HasDefaultValueSql("0");
            e.Property(x => x.StockVersion).HasColumnName("stock_version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("0");
            e.Property(x => x.LowStockThreshold).HasColumnName("low_stock_threshold").HasColumnType("integer").IsRequired().HasDefaultValueSql("5");
            e.Property(x => x.TargetStock).HasColumnName("target_stock").HasColumnType("integer").IsRequired().HasDefaultValueSql("30");
            e.Property(x => x.PreferredSupplierId).HasColumnName("preferred_supplier_id").HasColumnType("integer");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasColumnType("boolean").IsRequired().HasDefaultValueSql("true");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("uq_skus_1");
            e.HasIndex(x => new { x.ProductId, x.PackageAmount, x.PackageUnit }).IsUnique().HasDatabaseName("uq_skus_2");
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Supplier>().WithMany().HasForeignKey(x => x.PreferredSupplierId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PurchaseReceipt>(e =>
        {
            e.ToTable("purchase_receipts", t =>
            {
                t.HasCheckConstraint("ck_purchase_receipts_1", "status IN ('Draft','Confirmed','Cancelled')");
                t.HasCheckConstraint("ck_purchase_receipts_2", "total_vnd IS NULL OR total_vnd >= 0");
                t.HasCheckConstraint("ck_purchase_receipts_3", "(status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.Code).HasColumnName("code").HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.SupplierId).HasColumnName("supplier_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(16)").IsRequired().HasDefaultValueSql("'Draft'");
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("integer").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.ConfirmedBy).HasColumnName("confirmed_by").HasColumnType("integer");
            e.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at").HasColumnType("timestamptz");
            e.Property(x => x.ConfirmationKey).HasColumnName("confirmation_key").HasColumnType("uuid");
            e.Property(x => x.TotalVnd).HasColumnName("total_vnd").HasColumnType("numeric(18,0)");
            e.Property(x => x.CancelledBy).HasColumnName("cancelled_by").HasColumnType("integer");
            e.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamptz");
            e.Property(x => x.CancellationKey).HasColumnName("cancellation_key").HasColumnType("uuid");
            e.Property(x => x.CancelReason).HasColumnName("cancel_reason").HasColumnType("varchar(500)");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("uq_purchase_receipts_1");
            e.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ConfirmedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CancelledBy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PurchaseReceiptLine>(e =>
        {
            e.ToTable("purchase_receipt_lines", t =>
            {
                t.HasCheckConstraint("ck_purchase_receipt_lines_1", "quantity > 0");
                t.HasCheckConstraint("ck_purchase_receipt_lines_2", "unit_cost_vnd IS NULL OR unit_cost_vnd > 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.ReceiptId).HasColumnName("receipt_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.SkuId).HasColumnName("sku_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("integer").IsRequired();
            e.Property(x => x.UnitCostVnd).HasColumnName("unit_cost_vnd").HasColumnType("numeric(18,0)");
            e.HasIndex(x => new { x.ReceiptId, x.SkuId }).IsUnique().HasDatabaseName("uq_purchase_receipt_lines_1");
            e.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Sku>().WithMany().HasForeignKey(x => x.SkuId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Sale>(e =>
        {
            e.ToTable("sales", t =>
            {
                t.HasCheckConstraint("ck_sales_1", "status IN ('Draft','Confirmed','Cancelled')");
                t.HasCheckConstraint("ck_sales_2", "total_vnd IS NULL OR total_vnd >= 0");
                t.HasCheckConstraint("ck_sales_3", "(status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.Code).HasColumnName("code").HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(16)").IsRequired().HasDefaultValueSql("'Draft'");
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("integer").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.ConfirmedBy).HasColumnName("confirmed_by").HasColumnType("integer");
            e.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at").HasColumnType("timestamptz");
            e.Property(x => x.ConfirmationKey).HasColumnName("confirmation_key").HasColumnType("uuid");
            e.Property(x => x.TotalVnd).HasColumnName("total_vnd").HasColumnType("numeric(18,0)");
            e.Property(x => x.CancelledBy).HasColumnName("cancelled_by").HasColumnType("integer");
            e.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamptz");
            e.Property(x => x.CancellationKey).HasColumnName("cancellation_key").HasColumnType("uuid");
            e.Property(x => x.CancelReason).HasColumnName("cancel_reason").HasColumnType("varchar(500)");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("uq_sales_1");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ConfirmedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CancelledBy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SaleLine>(e =>
        {
            e.ToTable("sale_lines", t =>
            {
                t.HasCheckConstraint("ck_sale_lines_1", "quantity > 0");
                t.HasCheckConstraint("ck_sale_lines_2", "unit_price_vnd IS NULL OR unit_price_vnd > 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.SaleId).HasColumnName("sale_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.SkuId).HasColumnName("sku_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("integer").IsRequired();
            e.Property(x => x.UnitPriceVnd).HasColumnName("unit_price_vnd").HasColumnType("numeric(18,0)");
            e.Property(x => x.SkuLabelSnapshot).HasColumnName("sku_label_snapshot").HasColumnType("varchar(300)");
            e.HasIndex(x => new { x.SaleId, x.SkuId }).IsUnique().HasDatabaseName("uq_sale_lines_1");
            e.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Sku>().WithMany().HasForeignKey(x => x.SkuId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StockCount>(e =>
        {
            e.ToTable("stock_counts", t =>
            {
                t.HasCheckConstraint("ck_stock_counts_1", "expected_qty >= 0 AND actual_qty >= 0 AND expected_stock_version >= 0");
                t.HasCheckConstraint("ck_stock_counts_2", "status IN ('Pending','Approved','RecountRequired','Rejected')");
                t.HasCheckConstraint("ck_stock_counts_3", "length(trim(reason))>0");
                t.HasCheckConstraint("ck_stock_counts_4", "status <> 'Approved' OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.SkuId).HasColumnName("sku_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.ExpectedQty).HasColumnName("expected_qty").HasColumnType("integer").IsRequired();
            e.Property(x => x.ExpectedStockVersion).HasColumnName("expected_stock_version").HasColumnType("bigint").IsRequired();
            e.Property(x => x.ActualQty).HasColumnName("actual_qty").HasColumnType("integer").IsRequired();
            e.Property(x => x.Reason).HasColumnName("reason").HasColumnType("varchar(500)").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)").IsRequired().HasDefaultValueSql("'Pending'");
            e.Property(x => x.CountedBy).HasColumnName("counted_by").HasColumnType("integer").IsRequired();
            e.Property(x => x.CountedAt).HasColumnName("counted_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.ReviewedBy).HasColumnName("reviewed_by").HasColumnType("integer");
            e.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamptz");
            e.Property(x => x.ReviewNote).HasColumnName("review_note").HasColumnType("varchar(500)");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasOne<Sku>().WithMany().HasForeignKey(x => x.SkuId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CountedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StockAdjustment>(e =>
        {
            e.ToTable("stock_adjustments", t =>
            {
                t.HasCheckConstraint("ck_stock_adjustments_1", "delta_qty <> 0");
                t.HasCheckConstraint("ck_stock_adjustments_2", "length(trim(reason))>0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.RequestKey).HasColumnName("request_key").HasColumnType("uuid").IsRequired();
            e.Property(x => x.SkuId).HasColumnName("sku_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.StockCountId).HasColumnName("stock_count_id").HasColumnType("integer");
            e.Property(x => x.DeltaQty).HasColumnName("delta_qty").HasColumnType("integer").IsRequired();
            e.Property(x => x.Reason).HasColumnName("reason").HasColumnType("varchar(500)").IsRequired();
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("integer").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.RequestKey }).IsUnique().HasDatabaseName("uq_stock_adjustments_1");
            e.HasOne<Sku>().WithMany().HasForeignKey(x => x.SkuId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<StockCount>().WithMany().HasForeignKey(x => x.StockCountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StockMovement>(e =>
        {
            e.ToTable("stock_movements", t =>
            {
                t.HasCheckConstraint("ck_stock_movements_1", "delta_qty <> 0 AND balance_after >= 0 AND stock_version_after > 0");
                t.HasCheckConstraint("ck_stock_movements_2", "(kind IN ('Purchase','PurchaseCancel') AND receipt_id IS NOT NULL AND sale_id IS NULL AND adjustment_id IS NULL) OR (kind IN ('Sale','SaleCancel') AND sale_id IS NOT NULL AND receipt_id IS NULL AND adjustment_id IS NULL) OR (kind='Adjustment' AND adjustment_id IS NOT NULL AND sale_id IS NULL AND receipt_id IS NULL)");
                t.HasCheckConstraint("ck_stock_movements_3", "(kind IN ('Purchase','SaleCancel') AND delta_qty>0) OR (kind IN ('Sale','PurchaseCancel') AND delta_qty<0) OR kind='Adjustment'");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.SkuId).HasColumnName("sku_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.Kind).HasColumnName("kind").HasColumnType("varchar(16)").IsRequired();
            e.Property(x => x.DeltaQty).HasColumnName("delta_qty").HasColumnType("integer").IsRequired();
            e.Property(x => x.BalanceAfter).HasColumnName("balance_after").HasColumnType("integer").IsRequired();
            e.Property(x => x.StockVersionAfter).HasColumnName("stock_version_after").HasColumnType("bigint").IsRequired();
            e.Property(x => x.ReceiptId).HasColumnName("receipt_id").HasColumnType("integer");
            e.Property(x => x.SaleId).HasColumnName("sale_id").HasColumnType("integer");
            e.Property(x => x.AdjustmentId).HasColumnName("adjustment_id").HasColumnType("integer");
            e.Property(x => x.ActorId).HasColumnName("actor_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.SkuId, x.StockVersionAfter }).IsUnique().HasDatabaseName("uq_stock_movements_1");
            e.HasOne<Sku>().WithMany().HasForeignKey(x => x.SkuId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<StockAdjustment>().WithMany().HasForeignKey(x => x.AdjustmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RestockSuggestion>(e =>
        {
            e.ToTable("restock_suggestions", t =>
            {
                t.HasCheckConstraint("ck_restock_suggestions_1", "observed_qty >= 0 AND suggested_qty >= 0");
                t.HasCheckConstraint("ck_restock_suggestions_2", "status IN ('Open','ReceiptDrafted','Resolved')");
                t.HasCheckConstraint("ck_restock_suggestions_3", "status <> 'ReceiptDrafted' OR receipt_id IS NOT NULL");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.SkuId).HasColumnName("sku_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.SupplierId).HasColumnName("supplier_id").HasColumnType("integer");
            e.Property(x => x.ObservedQty).HasColumnName("observed_qty").HasColumnType("integer").IsRequired();
            e.Property(x => x.SuggestedQty).HasColumnName("suggested_qty").HasColumnType("integer").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)").IsRequired().HasDefaultValueSql("'Open'");
            e.Property(x => x.ReceiptId).HasColumnName("receipt_id").HasColumnType("integer");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasOne<Sku>().WithMany().HasForeignKey(x => x.SkuId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages", t =>
            {
                t.HasCheckConstraint("ck_outbox_messages_1", "kind IN ('InventoryChanged','BackupRequested','SummaryRequested')");
                t.HasCheckConstraint("ck_outbox_messages_2", "attempts >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
            e.Property(x => x.Kind).HasColumnName("kind").HasColumnType("varchar(24)").IsRequired();
            e.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.PublishedAt).HasColumnName("published_at").HasColumnType("timestamptz");
            e.Property(x => x.Attempts).HasColumnName("attempts").HasColumnType("integer").IsRequired().HasDefaultValueSql("0");
            e.Property(x => x.LastError).HasColumnName("last_error").HasColumnType("text");
        });
        modelBuilder.Entity<DailySummary>(e =>
        {
            e.ToTable("daily_summaries", t =>
            {
                t.HasCheckConstraint("ck_daily_summaries_1", "status IN ('Queued','Processing','Completed','Failed')");
                t.HasCheckConstraint("ck_daily_summaries_2", "revenue_vnd >= 0 AND sold_bags >= 0 AND sale_count >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.BusinessDate).HasColumnName("business_date").HasColumnType("date").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(16)").IsRequired().HasDefaultValueSql("'Queued'");
            e.Property(x => x.RevenueVnd).HasColumnName("revenue_vnd").HasColumnType("numeric(18,0)").IsRequired().HasDefaultValueSql("0");
            e.Property(x => x.SoldBags).HasColumnName("sold_bags").HasColumnType("bigint").IsRequired().HasDefaultValueSql("0");
            e.Property(x => x.SaleCount).HasColumnName("sale_count").HasColumnType("integer").IsRequired().HasDefaultValueSql("0");
            e.Property(x => x.SnapshotAt).HasColumnName("snapshot_at").HasColumnType("timestamptz");
            e.Property(x => x.LastError).HasColumnName("last_error").HasColumnType("text");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.Version).HasColumnName("version").HasColumnType("bigint").IsRequired().HasDefaultValueSql("1").IsConcurrencyToken();
            e.HasIndex(x => new { x.BusinessDate }).IsUnique().HasDatabaseName("uq_daily_summaries_1");
        });
        modelBuilder.Entity<DailySkuSnapshot>(e =>
        {
            e.ToTable("daily_sku_snapshots", t =>
            {
                t.HasCheckConstraint("ck_daily_sku_snapshots_1", "stock_qty >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").IsRequired().UseIdentityAlwaysColumn();
            e.Property(x => x.SummaryId).HasColumnName("summary_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.SkuId).HasColumnName("sku_id").HasColumnType("integer").IsRequired();
            e.Property(x => x.StockQty).HasColumnName("stock_qty").HasColumnType("integer").IsRequired();
            e.HasIndex(x => new { x.SummaryId, x.SkuId }).IsUnique().HasDatabaseName("uq_daily_sku_snapshots_1");
            e.HasOne<DailySummary>().WithMany().HasForeignKey(x => x.SummaryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Sku>().WithMany().HasForeignKey(x => x.SkuId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<BackupRun>(e =>
        {
            e.ToTable("backup_runs", t =>
            {
                t.HasCheckConstraint("ck_backup_runs_1", "kind IN ('Daily','Manual')");
                t.HasCheckConstraint("ck_backup_runs_2", "status IN ('Queued','Processing','Completed','Failed')");
                t.HasCheckConstraint("ck_backup_runs_3", "(kind='Daily' AND scheduled_date IS NOT NULL) OR (kind='Manual' AND scheduled_date IS NULL)");
                t.HasCheckConstraint("ck_backup_runs_4", "NOT retained OR (status='Completed' AND filename IS NOT NULL AND completed_at IS NOT NULL)");
                t.HasCheckConstraint("ck_backup_runs_5", "attempts >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
            e.Property(x => x.Kind).HasColumnName("kind").HasColumnType("varchar(12)").IsRequired();
            e.Property(x => x.ScheduledDate).HasColumnName("scheduled_date").HasColumnType("date");
            e.Property(x => x.RequestedBy).HasColumnName("requested_by").HasColumnType("integer");
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(16)").IsRequired().HasDefaultValueSql("'Queued'");
            e.Property(x => x.Filename).HasColumnName("filename").HasColumnType("varchar(200)");
            e.Property(x => x.Retained).HasColumnName("retained").HasColumnType("boolean").IsRequired().HasDefaultValueSql("false");
            e.Property(x => x.Attempts).HasColumnName("attempts").HasColumnType("integer").IsRequired().HasDefaultValueSql("0");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("now()");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
            e.Property(x => x.LastError).HasColumnName("last_error").HasColumnType("text");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sku>().HasIndex(x => new { x.ProductId }).HasDatabaseName("ix_skus_product");
        modelBuilder.Entity<Sku>().HasIndex(x => new { x.Id }).HasDatabaseName("ix_skus_low_stock").HasFilter("is_active AND stock_qty <= low_stock_threshold");
        modelBuilder.Entity<Sale>().HasIndex(x => new { x.ConfirmationKey }).IsUnique().HasDatabaseName("ux_sales_confirmation").HasFilter("confirmation_key IS NOT NULL");
        modelBuilder.Entity<Sale>().HasIndex(x => new { x.CancellationKey }).IsUnique().HasDatabaseName("ux_sales_cancellation").HasFilter("cancellation_key IS NOT NULL");
        modelBuilder.Entity<PurchaseReceipt>().HasIndex(x => new { x.ConfirmationKey }).IsUnique().HasDatabaseName("ux_receipt_confirmation").HasFilter("confirmation_key IS NOT NULL");
        modelBuilder.Entity<PurchaseReceipt>().HasIndex(x => new { x.CancellationKey }).IsUnique().HasDatabaseName("ux_receipt_cancellation").HasFilter("cancellation_key IS NOT NULL");
        modelBuilder.Entity<StockAdjustment>().HasIndex(x => new { x.StockCountId }).IsUnique().HasDatabaseName("ux_adjustment_count").HasFilter("stock_count_id IS NOT NULL");
        modelBuilder.Entity<StockMovement>().HasIndex(x => new { x.SaleId, x.SkuId, x.Kind }).IsUnique().HasDatabaseName("ux_movement_sale").HasFilter("sale_id IS NOT NULL");
        modelBuilder.Entity<StockMovement>().HasIndex(x => new { x.ReceiptId, x.SkuId, x.Kind }).IsUnique().HasDatabaseName("ux_movement_receipt").HasFilter("receipt_id IS NOT NULL");
        modelBuilder.Entity<StockMovement>().HasIndex(x => new { x.AdjustmentId }).IsUnique().HasDatabaseName("ux_movement_adjustment").HasFilter("adjustment_id IS NOT NULL");
        modelBuilder.Entity<RestockSuggestion>().HasIndex(x => new { x.SkuId }).IsUnique().HasDatabaseName("ux_restock_active").HasFilter("status IN ('Open','ReceiptDrafted')");
        modelBuilder.Entity<StockMovement>().HasIndex(x => new { x.SkuId, x.CreatedAt, x.Id }).HasDatabaseName("ix_movements_sku_time");
        modelBuilder.Entity<OutboxMessage>().HasIndex(x => new { x.CreatedAt }).HasDatabaseName("ix_outbox_pending").HasFilter("published_at IS NULL");
        modelBuilder.Entity<Sale>().HasIndex(x => new { x.ConfirmedAt }).HasDatabaseName("ix_sales_report").HasFilter("status='Confirmed'");
        modelBuilder.Entity<BackupRun>().HasIndex(x => new { x.ScheduledDate }).IsUnique().HasDatabaseName("ux_backup_daily").HasFilter("scheduled_date IS NOT NULL");
    }
}
