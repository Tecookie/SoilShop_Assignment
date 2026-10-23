ï»¿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE daily_summaries (
        id integer GENERATED ALWAYS AS IDENTITY,
        business_date date NOT NULL,
        status varchar(16) NOT NULL DEFAULT ('Queued'),
        revenue_vnd numeric(18,0) NOT NULL DEFAULT (0),
        sold_bags bigint NOT NULL DEFAULT (0),
        sale_count integer NOT NULL DEFAULT (0),
        snapshot_at timestamptz,
        last_error text,
        created_at timestamptz NOT NULL DEFAULT (now()),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_daily_summaries" PRIMARY KEY (id),
        CONSTRAINT ck_daily_summaries_1 CHECK (status IN ('Queued','Processing','Completed','Failed')),
        CONSTRAINT ck_daily_summaries_2 CHECK (revenue_vnd >= 0 AND sold_bags >= 0 AND sale_count >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE outbox_messages (
        id uuid NOT NULL,
        kind varchar(24) NOT NULL,
        payload jsonb NOT NULL,
        created_at timestamptz NOT NULL DEFAULT (now()),
        published_at timestamptz,
        attempts integer NOT NULL DEFAULT (0),
        last_error text,
        CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id),
        CONSTRAINT ck_outbox_messages_1 CHECK (kind IN ('InventoryChanged','BackupRequested','SummaryRequested')),
        CONSTRAINT ck_outbox_messages_2 CHECK (attempts >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE plant_tags (
        id integer GENERATED ALWAYS AS IDENTITY,
        code varchar(40) NOT NULL,
        name varchar(100) NOT NULL,
        CONSTRAINT "PK_plant_tags" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE suppliers (
        id integer GENERATED ALWAYS AS IDENTITY,
        code varchar(40) NOT NULL,
        name varchar(200) NOT NULL,
        contact_note text,
        is_active boolean NOT NULL DEFAULT (true),
        created_at timestamptz NOT NULL DEFAULT (now()),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_suppliers" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE users (
        id integer GENERATED ALWAYS AS IDENTITY,
        username varchar(100) NOT NULL,
        display_name varchar(150) NOT NULL,
        password_hash text NOT NULL,
        role varchar(16) NOT NULL,
        is_active boolean NOT NULL DEFAULT (true),
        created_at timestamptz NOT NULL DEFAULT (now()),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_users" PRIMARY KEY (id),
        CONSTRAINT ck_users_1 CHECK (role IN ('Admin','Employee')),
        CONSTRAINT ck_users_2 CHECK (length(trim(username)) > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE backup_runs (
        id uuid NOT NULL,
        kind varchar(12) NOT NULL,
        scheduled_date date,
        requested_by integer,
        status varchar(16) NOT NULL DEFAULT ('Queued'),
        filename varchar(200),
        retained boolean NOT NULL DEFAULT (false),
        attempts integer NOT NULL DEFAULT (0),
        created_at timestamptz NOT NULL DEFAULT (now()),
        completed_at timestamptz,
        last_error text,
        CONSTRAINT "PK_backup_runs" PRIMARY KEY (id),
        CONSTRAINT ck_backup_runs_1 CHECK (kind IN ('Daily','Manual')),
        CONSTRAINT ck_backup_runs_2 CHECK (status IN ('Queued','Processing','Completed','Failed')),
        CONSTRAINT ck_backup_runs_3 CHECK ((kind='Daily' AND scheduled_date IS NOT NULL) OR (kind='Manual' AND scheduled_date IS NULL)),
        CONSTRAINT ck_backup_runs_4 CHECK (NOT retained OR (status='Completed' AND filename IS NOT NULL AND completed_at IS NOT NULL)),
        CONSTRAINT ck_backup_runs_5 CHECK (attempts >= 0),
        CONSTRAINT "FK_backup_runs_users_requested_by" FOREIGN KEY (requested_by) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE products (
        id integer GENERATED ALWAYS AS IDENTITY,
        code varchar(40) NOT NULL,
        name varchar(200) NOT NULL,
        brand varchar(150),
        manufacturer varchar(150),
        ingredients text,
        drainage varchar(12),
        water_retention varchar(12),
        ph_min numeric(4,2),
        ph_max numeric(4,2),
        storage_note text,
        source_reference text,
        reviewed_by integer,
        reviewed_at timestamptz,
        is_active boolean NOT NULL DEFAULT (true),
        created_at timestamptz NOT NULL DEFAULT (now()),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_products" PRIMARY KEY (id),
        CONSTRAINT ck_products_1 CHECK (drainage IS NULL OR drainage IN ('Low','Medium','High')),
        CONSTRAINT ck_products_2 CHECK (water_retention IS NULL OR water_retention IN ('Low','Medium','High')),
        CONSTRAINT ck_products_3 CHECK ((ph_min IS NULL AND ph_max IS NULL) OR (ph_min IS NOT NULL AND ph_max IS NOT NULL AND ph_min BETWEEN 0 AND 14 AND ph_max BETWEEN ph_min AND 14)),
        CONSTRAINT ck_products_4 CHECK ((reviewed_by IS NULL AND reviewed_at IS NULL) OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL AND source_reference IS NOT NULL AND length(trim(source_reference))>0)),
        CONSTRAINT "FK_products_users_reviewed_by" FOREIGN KEY (reviewed_by) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE purchase_receipts (
        id integer GENERATED ALWAYS AS IDENTITY,
        code varchar(50) NOT NULL,
        supplier_id integer NOT NULL,
        status varchar(16) NOT NULL DEFAULT ('Draft'),
        created_by integer NOT NULL,
        created_at timestamptz NOT NULL DEFAULT (now()),
        confirmed_by integer,
        confirmed_at timestamptz,
        confirmation_key uuid,
        total_vnd numeric(18,0),
        cancelled_by integer,
        cancelled_at timestamptz,
        cancellation_key uuid,
        cancel_reason varchar(500),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_purchase_receipts" PRIMARY KEY (id),
        CONSTRAINT ck_purchase_receipts_1 CHECK (status IN ('Draft','Confirmed','Cancelled')),
        CONSTRAINT ck_purchase_receipts_2 CHECK (total_vnd IS NULL OR total_vnd >= 0),
        CONSTRAINT ck_purchase_receipts_3 CHECK ((status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)),
        CONSTRAINT "FK_purchase_receipts_suppliers_supplier_id" FOREIGN KEY (supplier_id) REFERENCES suppliers (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_purchase_receipts_users_cancelled_by" FOREIGN KEY (cancelled_by) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_purchase_receipts_users_confirmed_by" FOREIGN KEY (confirmed_by) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_purchase_receipts_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE sales (
        id integer GENERATED ALWAYS AS IDENTITY,
        code varchar(50) NOT NULL,
        status varchar(16) NOT NULL DEFAULT ('Draft'),
        created_by integer NOT NULL,
        created_at timestamptz NOT NULL DEFAULT (now()),
        confirmed_by integer,
        confirmed_at timestamptz,
        confirmation_key uuid,
        total_vnd numeric(18,0),
        cancelled_by integer,
        cancelled_at timestamptz,
        cancellation_key uuid,
        cancel_reason varchar(500),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_sales" PRIMARY KEY (id),
        CONSTRAINT ck_sales_1 CHECK (status IN ('Draft','Confirmed','Cancelled')),
        CONSTRAINT ck_sales_2 CHECK (total_vnd IS NULL OR total_vnd >= 0),
        CONSTRAINT ck_sales_3 CHECK ((status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)),
        CONSTRAINT "FK_sales_users_cancelled_by" FOREIGN KEY (cancelled_by) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_sales_users_confirmed_by" FOREIGN KEY (confirmed_by) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_sales_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE product_plant_tags (
        id integer GENERATED ALWAYS AS IDENTITY,
        product_id integer NOT NULL,
        plant_tag_id integer NOT NULL,
        CONSTRAINT "PK_product_plant_tags" PRIMARY KEY (id),
        CONSTRAINT "FK_product_plant_tags_plant_tags_plant_tag_id" FOREIGN KEY (plant_tag_id) REFERENCES plant_tags (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_product_plant_tags_products_product_id" FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE skus (
        id integer GENERATED ALWAYS AS IDENTITY,
        product_id integer NOT NULL,
        code varchar(50) NOT NULL,
        package_amount numeric(10,2) NOT NULL,
        package_unit varchar(8) NOT NULL,
        sale_price_vnd numeric(18,0) NOT NULL,
        reference_cost_vnd numeric(18,0),
        stock_qty integer NOT NULL DEFAULT (0),
        stock_version bigint NOT NULL DEFAULT (0),
        low_stock_threshold integer NOT NULL DEFAULT (5),
        target_stock integer NOT NULL DEFAULT (30),
        preferred_supplier_id integer,
        is_active boolean NOT NULL DEFAULT (true),
        created_at timestamptz NOT NULL DEFAULT (now()),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_skus" PRIMARY KEY (id),
        CONSTRAINT ck_skus_1 CHECK (package_unit IN ('kg','L')),
        CONSTRAINT ck_skus_2 CHECK (package_amount > 0),
        CONSTRAINT ck_skus_3 CHECK (sale_price_vnd > 0),
        CONSTRAINT ck_skus_4 CHECK (reference_cost_vnd IS NULL OR reference_cost_vnd > 0),
        CONSTRAINT ck_skus_5 CHECK (stock_qty >= 0),
        CONSTRAINT ck_skus_6 CHECK (stock_version >= 0),
        CONSTRAINT ck_skus_7 CHECK (low_stock_threshold >= 0 AND target_stock > low_stock_threshold),
        CONSTRAINT "FK_skus_products_product_id" FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_skus_suppliers_preferred_supplier_id" FOREIGN KEY (preferred_supplier_id) REFERENCES suppliers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE daily_sku_snapshots (
        id bigint GENERATED ALWAYS AS IDENTITY,
        summary_id integer NOT NULL,
        sku_id integer NOT NULL,
        stock_qty integer NOT NULL,
        CONSTRAINT "PK_daily_sku_snapshots" PRIMARY KEY (id),
        CONSTRAINT ck_daily_sku_snapshots_1 CHECK (stock_qty >= 0),
        CONSTRAINT "FK_daily_sku_snapshots_daily_summaries_summary_id" FOREIGN KEY (summary_id) REFERENCES daily_summaries (id) ON DELETE CASCADE,
        CONSTRAINT "FK_daily_sku_snapshots_skus_sku_id" FOREIGN KEY (sku_id) REFERENCES skus (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE purchase_receipt_lines (
        id integer GENERATED ALWAYS AS IDENTITY,
        receipt_id integer NOT NULL,
        sku_id integer NOT NULL,
        quantity integer NOT NULL,
        unit_cost_vnd numeric(18,0),
        CONSTRAINT "PK_purchase_receipt_lines" PRIMARY KEY (id),
        CONSTRAINT ck_purchase_receipt_lines_1 CHECK (quantity > 0),
        CONSTRAINT ck_purchase_receipt_lines_2 CHECK (unit_cost_vnd IS NULL OR unit_cost_vnd > 0),
        CONSTRAINT "FK_purchase_receipt_lines_purchase_receipts_receipt_id" FOREIGN KEY (receipt_id) REFERENCES purchase_receipts (id) ON DELETE CASCADE,
        CONSTRAINT "FK_purchase_receipt_lines_skus_sku_id" FOREIGN KEY (sku_id) REFERENCES skus (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE restock_suggestions (
        id integer GENERATED ALWAYS AS IDENTITY,
        sku_id integer NOT NULL,
        supplier_id integer,
        observed_qty integer NOT NULL,
        suggested_qty integer NOT NULL,
        status varchar(20) NOT NULL DEFAULT ('Open'),
        receipt_id integer,
        created_at timestamptz NOT NULL DEFAULT (now()),
        updated_at timestamptz NOT NULL DEFAULT (now()),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_restock_suggestions" PRIMARY KEY (id),
        CONSTRAINT ck_restock_suggestions_1 CHECK (observed_qty >= 0 AND suggested_qty >= 0),
        CONSTRAINT ck_restock_suggestions_2 CHECK (status IN ('Open','ReceiptDrafted','Resolved')),
        CONSTRAINT ck_restock_suggestions_3 CHECK (status <> 'ReceiptDrafted' OR receipt_id IS NOT NULL),
        CONSTRAINT "FK_restock_suggestions_purchase_receipts_receipt_id" FOREIGN KEY (receipt_id) REFERENCES purchase_receipts (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_restock_suggestions_skus_sku_id" FOREIGN KEY (sku_id) REFERENCES skus (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_restock_suggestions_suppliers_supplier_id" FOREIGN KEY (supplier_id) REFERENCES suppliers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE sale_lines (
        id integer GENERATED ALWAYS AS IDENTITY,
        sale_id integer NOT NULL,
        sku_id integer NOT NULL,
        quantity integer NOT NULL,
        unit_price_vnd numeric(18,0),
        sku_label_snapshot varchar(300),
        CONSTRAINT "PK_sale_lines" PRIMARY KEY (id),
        CONSTRAINT ck_sale_lines_1 CHECK (quantity > 0),
        CONSTRAINT ck_sale_lines_2 CHECK (unit_price_vnd IS NULL OR unit_price_vnd > 0),
        CONSTRAINT "FK_sale_lines_sales_sale_id" FOREIGN KEY (sale_id) REFERENCES sales (id) ON DELETE CASCADE,
        CONSTRAINT "FK_sale_lines_skus_sku_id" FOREIGN KEY (sku_id) REFERENCES skus (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE stock_counts (
        id integer GENERATED ALWAYS AS IDENTITY,
        sku_id integer NOT NULL,
        expected_qty integer NOT NULL,
        expected_stock_version bigint NOT NULL,
        actual_qty integer NOT NULL,
        reason varchar(500) NOT NULL,
        status varchar(20) NOT NULL DEFAULT ('Pending'),
        counted_by integer NOT NULL,
        counted_at timestamptz NOT NULL DEFAULT (now()),
        reviewed_by integer,
        reviewed_at timestamptz,
        review_note varchar(500),
        version bigint NOT NULL DEFAULT (1),
        CONSTRAINT "PK_stock_counts" PRIMARY KEY (id),
        CONSTRAINT ck_stock_counts_1 CHECK (expected_qty >= 0 AND actual_qty >= 0 AND expected_stock_version >= 0),
        CONSTRAINT ck_stock_counts_2 CHECK (status IN ('Pending','Approved','RecountRequired','Rejected')),
        CONSTRAINT ck_stock_counts_3 CHECK (length(trim(reason))>0),
        CONSTRAINT ck_stock_counts_4 CHECK (status <> 'Approved' OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)),
        CONSTRAINT "FK_stock_counts_skus_sku_id" FOREIGN KEY (sku_id) REFERENCES skus (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_counts_users_counted_by" FOREIGN KEY (counted_by) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_counts_users_reviewed_by" FOREIGN KEY (reviewed_by) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE stock_adjustments (
        id integer GENERATED ALWAYS AS IDENTITY,
        request_key uuid NOT NULL,
        sku_id integer NOT NULL,
        stock_count_id integer,
        delta_qty integer NOT NULL,
        reason varchar(500) NOT NULL,
        created_by integer NOT NULL,
        created_at timestamptz NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_stock_adjustments" PRIMARY KEY (id),
        CONSTRAINT ck_stock_adjustments_1 CHECK (delta_qty <> 0),
        CONSTRAINT ck_stock_adjustments_2 CHECK (length(trim(reason))>0),
        CONSTRAINT "FK_stock_adjustments_skus_sku_id" FOREIGN KEY (sku_id) REFERENCES skus (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_adjustments_stock_counts_stock_count_id" FOREIGN KEY (stock_count_id) REFERENCES stock_counts (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_adjustments_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE TABLE stock_movements (
        id bigint GENERATED ALWAYS AS IDENTITY,
        sku_id integer NOT NULL,
        kind varchar(16) NOT NULL,
        delta_qty integer NOT NULL,
        balance_after integer NOT NULL,
        stock_version_after bigint NOT NULL,
        receipt_id integer,
        sale_id integer,
        adjustment_id integer,
        actor_id integer NOT NULL,
        created_at timestamptz NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_stock_movements" PRIMARY KEY (id),
        CONSTRAINT ck_stock_movements_1 CHECK (delta_qty <> 0 AND balance_after >= 0 AND stock_version_after > 0),
        CONSTRAINT ck_stock_movements_2 CHECK ((kind IN ('Purchase','PurchaseCancel') AND receipt_id IS NOT NULL AND sale_id IS NULL AND adjustment_id IS NULL) OR (kind IN ('Sale','SaleCancel') AND sale_id IS NOT NULL AND receipt_id IS NULL AND adjustment_id IS NULL) OR (kind='Adjustment' AND adjustment_id IS NOT NULL AND sale_id IS NULL AND receipt_id IS NULL)),
        CONSTRAINT ck_stock_movements_3 CHECK ((kind IN ('Purchase','SaleCancel') AND delta_qty>0) OR (kind IN ('Sale','PurchaseCancel') AND delta_qty<0) OR kind='Adjustment'),
        CONSTRAINT "FK_stock_movements_purchase_receipts_receipt_id" FOREIGN KEY (receipt_id) REFERENCES purchase_receipts (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_sales_sale_id" FOREIGN KEY (sale_id) REFERENCES sales (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_skus_sku_id" FOREIGN KEY (sku_id) REFERENCES skus (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_stock_adjustments_adjustment_id" FOREIGN KEY (adjustment_id) REFERENCES stock_adjustments (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_users_actor_id" FOREIGN KEY (actor_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_backup_runs_requested_by" ON backup_runs (requested_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_backup_daily ON backup_runs (scheduled_date) WHERE scheduled_date IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_daily_sku_snapshots_sku_id" ON daily_sku_snapshots (sku_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_daily_sku_snapshots_1 ON daily_sku_snapshots (summary_id, sku_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_daily_summaries_1 ON daily_summaries (business_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX ix_outbox_pending ON outbox_messages (created_at) WHERE published_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_plant_tags_1 ON plant_tags (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_product_plant_tags_plant_tag_id" ON product_plant_tags (plant_tag_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_product_plant_tags_1 ON product_plant_tags (product_id, plant_tag_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_products_reviewed_by" ON products (reviewed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_products_1 ON products (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_purchase_receipt_lines_sku_id" ON purchase_receipt_lines (sku_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_purchase_receipt_lines_1 ON purchase_receipt_lines (receipt_id, sku_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_purchase_receipts_cancelled_by" ON purchase_receipts (cancelled_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_purchase_receipts_confirmed_by" ON purchase_receipts (confirmed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_purchase_receipts_created_by" ON purchase_receipts (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_purchase_receipts_supplier_id" ON purchase_receipts (supplier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_purchase_receipts_1 ON purchase_receipts (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_receipt_cancellation ON purchase_receipts (cancellation_key) WHERE cancellation_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_receipt_confirmation ON purchase_receipts (confirmation_key) WHERE confirmation_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_restock_suggestions_receipt_id" ON restock_suggestions (receipt_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_restock_suggestions_supplier_id" ON restock_suggestions (supplier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_restock_active ON restock_suggestions (sku_id) WHERE status IN ('Open','ReceiptDrafted');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_sale_lines_sku_id" ON sale_lines (sku_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_sale_lines_1 ON sale_lines (sale_id, sku_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_sales_cancelled_by" ON sales (cancelled_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_sales_confirmed_by" ON sales (confirmed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_sales_created_by" ON sales (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX ix_sales_report ON sales (confirmed_at) WHERE status='Confirmed';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_sales_1 ON sales (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_sales_cancellation ON sales (cancellation_key) WHERE cancellation_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_sales_confirmation ON sales (confirmation_key) WHERE confirmation_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX ix_skus_low_stock ON skus (id) WHERE is_active AND stock_qty <= low_stock_threshold;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_skus_preferred_supplier_id" ON skus (preferred_supplier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX ix_skus_product ON skus (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_skus_1 ON skus (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_skus_2 ON skus (product_id, package_amount, package_unit);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_stock_adjustments_created_by" ON stock_adjustments (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_stock_adjustments_sku_id" ON stock_adjustments (sku_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_stock_adjustments_1 ON stock_adjustments (request_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_adjustment_count ON stock_adjustments (stock_count_id) WHERE stock_count_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_stock_counts_counted_by" ON stock_counts (counted_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_stock_counts_reviewed_by" ON stock_counts (reviewed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_stock_counts_sku_id" ON stock_counts (sku_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX ix_movements_sku_time ON stock_movements (sku_id, created_at, id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE INDEX "IX_stock_movements_actor_id" ON stock_movements (actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_stock_movements_1 ON stock_movements (sku_id, stock_version_after);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_movement_adjustment ON stock_movements (adjustment_id) WHERE adjustment_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_movement_receipt ON stock_movements (receipt_id, sku_id, kind) WHERE receipt_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX ux_movement_sale ON stock_movements (sale_id, sku_id, kind) WHERE sale_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_suppliers_1 ON suppliers (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    CREATE UNIQUE INDEX uq_users_1 ON users (username);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143607_InitialShop') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261023143607_InitialShop', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143700_CaseInsensitiveCodes') THEN
    CREATE UNIQUE INDEX ux_users_username_fold ON users (lower(username));
    CREATE UNIQUE INDEX ux_supplier_code_fold ON suppliers (upper(code));
    CREATE UNIQUE INDEX ux_product_code_fold ON products (upper(code));
    CREATE UNIQUE INDEX ux_sku_code_fold ON skus (upper(code));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023143700_CaseInsensitiveCodes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261023143700_CaseInsensitiveCodes', '10.0.13');
    END IF;
END $EF$;
COMMIT;
