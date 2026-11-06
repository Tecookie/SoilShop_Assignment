using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SoilShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_summaries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "varchar(16)", nullable: false, defaultValueSql: "'Queued'"),
                    revenue_vnd = table.Column<decimal>(type: "numeric(18,0)", nullable: false, defaultValueSql: "0"),
                    sold_bags = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "0"),
                    sale_count = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    snapshot_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_summaries", x => x.id);
                    table.CheckConstraint("ck_daily_summaries_1", "status IN ('Queued','Processing','Completed','Failed')");
                    table.CheckConstraint("ck_daily_summaries_2", "revenue_vnd >= 0 AND sold_bags >= 0 AND sale_count >= 0");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "varchar(24)", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    published_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_1", "kind IN ('InventoryChanged','BackupRequested','SummaryRequested')");
                    table.CheckConstraint("ck_outbox_messages_2", "attempts >= 0");
                });

            migrationBuilder.CreateTable(
                name: "plant_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "varchar(40)", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plant_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "varchar(40)", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", nullable: false),
                    contact_note = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true"),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    username = table.Column<string>(type: "varchar(100)", nullable: false),
                    display_name = table.Column<string>(type: "varchar(150)", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "varchar(16)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true"),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_1", "role IN ('Admin','Employee')");
                    table.CheckConstraint("ck_users_2", "length(trim(username)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "backup_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "varchar(12)", nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    requested_by = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "varchar(16)", nullable: false, defaultValueSql: "'Queued'"),
                    filename = table.Column<string>(type: "varchar(200)", nullable: true),
                    retained = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_runs", x => x.id);
                    table.CheckConstraint("ck_backup_runs_1", "kind IN ('Daily','Manual')");
                    table.CheckConstraint("ck_backup_runs_2", "status IN ('Queued','Processing','Completed','Failed')");
                    table.CheckConstraint("ck_backup_runs_3", "(kind='Daily' AND scheduled_date IS NOT NULL) OR (kind='Manual' AND scheduled_date IS NULL)");
                    table.CheckConstraint("ck_backup_runs_4", "NOT retained OR (status='Completed' AND filename IS NOT NULL AND completed_at IS NOT NULL)");
                    table.CheckConstraint("ck_backup_runs_5", "attempts >= 0");
                    table.ForeignKey(
                        name: "FK_backup_runs_users_requested_by",
                        column: x => x.requested_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "varchar(40)", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", nullable: false),
                    brand = table.Column<string>(type: "varchar(150)", nullable: true),
                    manufacturer = table.Column<string>(type: "varchar(150)", nullable: true),
                    ingredients = table.Column<string>(type: "text", nullable: true),
                    drainage = table.Column<string>(type: "varchar(12)", nullable: true),
                    water_retention = table.Column<string>(type: "varchar(12)", nullable: true),
                    ph_min = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    ph_max = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    storage_note = table.Column<string>(type: "text", nullable: true),
                    source_reference = table.Column<string>(type: "text", nullable: true),
                    reviewed_by = table.Column<int>(type: "integer", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true"),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("ck_products_1", "drainage IS NULL OR drainage IN ('Low','Medium','High')");
                    table.CheckConstraint("ck_products_2", "water_retention IS NULL OR water_retention IN ('Low','Medium','High')");
                    table.CheckConstraint("ck_products_3", "(ph_min IS NULL AND ph_max IS NULL) OR (ph_min IS NOT NULL AND ph_max IS NOT NULL AND ph_min BETWEEN 0 AND 14 AND ph_max BETWEEN ph_min AND 14)");
                    table.CheckConstraint("ck_products_4", "(reviewed_by IS NULL AND reviewed_at IS NULL) OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL AND source_reference IS NOT NULL AND length(trim(source_reference))>0)");
                    table.ForeignKey(
                        name: "FK_products_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_receipts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "varchar(50)", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "varchar(16)", nullable: false, defaultValueSql: "'Draft'"),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    confirmed_by = table.Column<int>(type: "integer", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    confirmation_key = table.Column<Guid>(type: "uuid", nullable: true),
                    total_vnd = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    cancelled_by = table.Column<int>(type: "integer", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    cancellation_key = table.Column<Guid>(type: "uuid", nullable: true),
                    cancel_reason = table.Column<string>(type: "varchar(500)", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_receipts", x => x.id);
                    table.CheckConstraint("ck_purchase_receipts_1", "status IN ('Draft','Confirmed','Cancelled')");
                    table.CheckConstraint("ck_purchase_receipts_2", "total_vnd IS NULL OR total_vnd >= 0");
                    table.CheckConstraint("ck_purchase_receipts_3", "(status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)");
                    table.ForeignKey(
                        name: "FK_purchase_receipts_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipts_users_cancelled_by",
                        column: x => x.cancelled_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipts_users_confirmed_by",
                        column: x => x.confirmed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "varchar(50)", nullable: false),
                    status = table.Column<string>(type: "varchar(16)", nullable: false, defaultValueSql: "'Draft'"),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    confirmed_by = table.Column<int>(type: "integer", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    confirmation_key = table.Column<Guid>(type: "uuid", nullable: true),
                    total_vnd = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    cancelled_by = table.Column<int>(type: "integer", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    cancellation_key = table.Column<Guid>(type: "uuid", nullable: true),
                    cancel_reason = table.Column<string>(type: "varchar(500)", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.id);
                    table.CheckConstraint("ck_sales_1", "status IN ('Draft','Confirmed','Cancelled')");
                    table.CheckConstraint("ck_sales_2", "total_vnd IS NULL OR total_vnd >= 0");
                    table.CheckConstraint("ck_sales_3", "(status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)");
                    table.ForeignKey(
                        name: "FK_sales_users_cancelled_by",
                        column: x => x.cancelled_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_users_confirmed_by",
                        column: x => x.confirmed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_plant_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    plant_tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_plant_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_plant_tags_plant_tags_plant_tag_id",
                        column: x => x.plant_tag_id,
                        principalTable: "plant_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_plant_tags_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", nullable: false),
                    package_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    package_unit = table.Column<string>(type: "varchar(8)", nullable: false),
                    sale_price_vnd = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    reference_cost_vnd = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    stock_qty = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    stock_version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "0"),
                    low_stock_threshold = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "5"),
                    target_stock = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "30"),
                    preferred_supplier_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true"),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skus", x => x.id);
                    table.CheckConstraint("ck_skus_1", "package_unit IN ('kg','L')");
                    table.CheckConstraint("ck_skus_2", "package_amount > 0");
                    table.CheckConstraint("ck_skus_3", "sale_price_vnd > 0");
                    table.CheckConstraint("ck_skus_4", "reference_cost_vnd IS NULL OR reference_cost_vnd > 0");
                    table.CheckConstraint("ck_skus_5", "stock_qty >= 0");
                    table.CheckConstraint("ck_skus_6", "stock_version >= 0");
                    table.CheckConstraint("ck_skus_7", "low_stock_threshold >= 0 AND target_stock > low_stock_threshold");
                    table.ForeignKey(
                        name: "FK_skus_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_skus_suppliers_preferred_supplier_id",
                        column: x => x.preferred_supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_sku_snapshots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    summary_id = table.Column<int>(type: "integer", nullable: false),
                    sku_id = table.Column<int>(type: "integer", nullable: false),
                    stock_qty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_sku_snapshots", x => x.id);
                    table.CheckConstraint("ck_daily_sku_snapshots_1", "stock_qty >= 0");
                    table.ForeignKey(
                        name: "FK_daily_sku_snapshots_daily_summaries_summary_id",
                        column: x => x.summary_id,
                        principalTable: "daily_summaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_sku_snapshots_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_receipt_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    receipt_id = table.Column<int>(type: "integer", nullable: false),
                    sku_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost_vnd = table.Column<decimal>(type: "numeric(18,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_purchase_receipt_lines_1", "quantity > 0");
                    table.CheckConstraint("ck_purchase_receipt_lines_2", "unit_cost_vnd IS NULL OR unit_cost_vnd > 0");
                    table.ForeignKey(
                        name: "FK_purchase_receipt_lines_purchase_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "purchase_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_receipt_lines_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "restock_suggestions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sku_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    observed_qty = table.Column<int>(type: "integer", nullable: false),
                    suggested_qty = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValueSql: "'Open'"),
                    receipt_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restock_suggestions", x => x.id);
                    table.CheckConstraint("ck_restock_suggestions_1", "observed_qty >= 0 AND suggested_qty >= 0");
                    table.CheckConstraint("ck_restock_suggestions_2", "status IN ('Open','ReceiptDrafted','Resolved')");
                    table.CheckConstraint("ck_restock_suggestions_3", "status <> 'ReceiptDrafted' OR receipt_id IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_restock_suggestions_purchase_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "purchase_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_restock_suggestions_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_restock_suggestions_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    sku_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_vnd = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    sku_label_snapshot = table.Column<string>(type: "varchar(300)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_lines", x => x.id);
                    table.CheckConstraint("ck_sale_lines_1", "quantity > 0");
                    table.CheckConstraint("ck_sale_lines_2", "unit_price_vnd IS NULL OR unit_price_vnd > 0");
                    table.ForeignKey(
                        name: "FK_sale_lines_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sale_lines_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_counts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sku_id = table.Column<int>(type: "integer", nullable: false),
                    expected_qty = table.Column<int>(type: "integer", nullable: false),
                    expected_stock_version = table.Column<long>(type: "bigint", nullable: false),
                    actual_qty = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "varchar(500)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValueSql: "'Pending'"),
                    counted_by = table.Column<int>(type: "integer", nullable: false),
                    counted_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    reviewed_by = table.Column<int>(type: "integer", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    review_note = table.Column<string>(type: "varchar(500)", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_counts", x => x.id);
                    table.CheckConstraint("ck_stock_counts_1", "expected_qty >= 0 AND actual_qty >= 0 AND expected_stock_version >= 0");
                    table.CheckConstraint("ck_stock_counts_2", "status IN ('Pending','Approved','RecountRequired','Rejected')");
                    table.CheckConstraint("ck_stock_counts_3", "length(trim(reason))>0");
                    table.CheckConstraint("ck_stock_counts_4", "status <> 'Approved' OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_stock_counts_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_counts_users_counted_by",
                        column: x => x.counted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_counts_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    request_key = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<int>(type: "integer", nullable: false),
                    stock_count_id = table.Column<int>(type: "integer", nullable: true),
                    delta_qty = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "varchar(500)", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_adjustments", x => x.id);
                    table.CheckConstraint("ck_stock_adjustments_1", "delta_qty <> 0");
                    table.CheckConstraint("ck_stock_adjustments_2", "length(trim(reason))>0");
                    table.ForeignKey(
                        name: "FK_stock_adjustments_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_adjustments_stock_counts_stock_count_id",
                        column: x => x.stock_count_id,
                        principalTable: "stock_counts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_adjustments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sku_id = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "varchar(16)", nullable: false),
                    delta_qty = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    stock_version_after = table.Column<long>(type: "bigint", nullable: false),
                    receipt_id = table.Column<int>(type: "integer", nullable: true),
                    sale_id = table.Column<int>(type: "integer", nullable: true),
                    adjustment_id = table.Column<int>(type: "integer", nullable: true),
                    actor_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                    table.CheckConstraint("ck_stock_movements_1", "delta_qty <> 0 AND balance_after >= 0 AND stock_version_after > 0");
                    table.CheckConstraint("ck_stock_movements_2", "(kind IN ('Purchase','PurchaseCancel') AND receipt_id IS NOT NULL AND sale_id IS NULL AND adjustment_id IS NULL) OR (kind IN ('Sale','SaleCancel') AND sale_id IS NOT NULL AND receipt_id IS NULL AND adjustment_id IS NULL) OR (kind='Adjustment' AND adjustment_id IS NOT NULL AND sale_id IS NULL AND receipt_id IS NULL)");
                    table.CheckConstraint("ck_stock_movements_3", "(kind IN ('Purchase','SaleCancel') AND delta_qty>0) OR (kind IN ('Sale','PurchaseCancel') AND delta_qty<0) OR kind='Adjustment'");
                    table.ForeignKey(
                        name: "FK_stock_movements_purchase_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "purchase_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_stock_adjustments_adjustment_id",
                        column: x => x.adjustment_id,
                        principalTable: "stock_adjustments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backup_runs_requested_by",
                table: "backup_runs",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "ux_backup_daily",
                table: "backup_runs",
                column: "scheduled_date",
                unique: true,
                filter: "scheduled_date IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_daily_sku_snapshots_sku_id",
                table: "daily_sku_snapshots",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "uq_daily_sku_snapshots_1",
                table: "daily_sku_snapshots",
                columns: new[] { "summary_id", "sku_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_daily_summaries_1",
                table: "daily_summaries",
                column: "business_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending",
                table: "outbox_messages",
                column: "created_at",
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_plant_tags_1",
                table: "plant_tags",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_plant_tags_plant_tag_id",
                table: "product_plant_tags",
                column: "plant_tag_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_plant_tags_1",
                table: "product_plant_tags",
                columns: new[] { "product_id", "plant_tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_reviewed_by",
                table: "products",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "uq_products_1",
                table: "products",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipt_lines_sku_id",
                table: "purchase_receipt_lines",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "uq_purchase_receipt_lines_1",
                table: "purchase_receipt_lines",
                columns: new[] { "receipt_id", "sku_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_cancelled_by",
                table: "purchase_receipts",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_confirmed_by",
                table: "purchase_receipts",
                column: "confirmed_by");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_created_by",
                table: "purchase_receipts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_supplier_id",
                table: "purchase_receipts",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "uq_purchase_receipts_1",
                table: "purchase_receipts",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_receipt_cancellation",
                table: "purchase_receipts",
                column: "cancellation_key",
                unique: true,
                filter: "cancellation_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_receipt_confirmation",
                table: "purchase_receipts",
                column: "confirmation_key",
                unique: true,
                filter: "confirmation_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_restock_suggestions_receipt_id",
                table: "restock_suggestions",
                column: "receipt_id");

            migrationBuilder.CreateIndex(
                name: "IX_restock_suggestions_supplier_id",
                table: "restock_suggestions",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_restock_active",
                table: "restock_suggestions",
                column: "sku_id",
                unique: true,
                filter: "status IN ('Open','ReceiptDrafted')");

            migrationBuilder.CreateIndex(
                name: "IX_sale_lines_sku_id",
                table: "sale_lines",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "uq_sale_lines_1",
                table: "sale_lines",
                columns: new[] { "sale_id", "sku_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_cancelled_by",
                table: "sales",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_sales_confirmed_by",
                table: "sales",
                column: "confirmed_by");

            migrationBuilder.CreateIndex(
                name: "IX_sales_created_by",
                table: "sales",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_sales_report",
                table: "sales",
                column: "confirmed_at",
                filter: "status='Confirmed'");

            migrationBuilder.CreateIndex(
                name: "uq_sales_1",
                table: "sales",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_sales_cancellation",
                table: "sales",
                column: "cancellation_key",
                unique: true,
                filter: "cancellation_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_sales_confirmation",
                table: "sales",
                column: "confirmation_key",
                unique: true,
                filter: "confirmation_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_skus_low_stock",
                table: "skus",
                column: "id",
                filter: "is_active AND stock_qty <= low_stock_threshold");

            migrationBuilder.CreateIndex(
                name: "IX_skus_preferred_supplier_id",
                table: "skus",
                column: "preferred_supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_skus_product",
                table: "skus",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_skus_1",
                table: "skus",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_skus_2",
                table: "skus",
                columns: new[] { "product_id", "package_amount", "package_unit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustments_created_by",
                table: "stock_adjustments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustments_sku_id",
                table: "stock_adjustments",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustments_1",
                table: "stock_adjustments",
                column: "request_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_adjustment_count",
                table: "stock_adjustments",
                column: "stock_count_id",
                unique: true,
                filter: "stock_count_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_counted_by",
                table: "stock_counts",
                column: "counted_by");

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_reviewed_by",
                table: "stock_counts",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_sku_id",
                table: "stock_counts",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "ix_movements_sku_time",
                table: "stock_movements",
                columns: new[] { "sku_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_actor_id",
                table: "stock_movements",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_movements_1",
                table: "stock_movements",
                columns: new[] { "sku_id", "stock_version_after" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_movement_adjustment",
                table: "stock_movements",
                column: "adjustment_id",
                unique: true,
                filter: "adjustment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_movement_receipt",
                table: "stock_movements",
                columns: new[] { "receipt_id", "sku_id", "kind" },
                unique: true,
                filter: "receipt_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_movement_sale",
                table: "stock_movements",
                columns: new[] { "sale_id", "sku_id", "kind" },
                unique: true,
                filter: "sale_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_suppliers_1",
                table: "suppliers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_users_1",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backup_runs");

            migrationBuilder.DropTable(
                name: "daily_sku_snapshots");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "product_plant_tags");

            migrationBuilder.DropTable(
                name: "purchase_receipt_lines");

            migrationBuilder.DropTable(
                name: "restock_suggestions");

            migrationBuilder.DropTable(
                name: "sale_lines");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "daily_summaries");

            migrationBuilder.DropTable(
                name: "plant_tags");

            migrationBuilder.DropTable(
                name: "purchase_receipts");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "stock_adjustments");

            migrationBuilder.DropTable(
                name: "stock_counts");

            migrationBuilder.DropTable(
                name: "skus");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
