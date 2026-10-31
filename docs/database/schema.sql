-- Potting Soil Shop — PostgreSQL 16+ DESIGN baseline for a fresh database.
-- Generated from docs/tools/schema_model.py; review alongside EF migrations.
-- No roles, secrets, seeds or database creation. Run with psql ON_ERROR_STOP.
BEGIN;

CREATE TABLE users (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    username varchar(100) NOT NULL,
    display_name varchar(150) NOT NULL,
    password_hash text NOT NULL,
    role varchar(16) NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_users_1 UNIQUE (username),
    CONSTRAINT ck_users_1 CHECK (role IN ('Admin','Employee')),
    CONSTRAINT ck_users_2 CHECK (length(trim(username)) > 0)
);

CREATE TABLE suppliers (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    code varchar(40) NOT NULL,
    name varchar(200) NOT NULL,
    contact_note text,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_suppliers_1 UNIQUE (code)
);

CREATE TABLE products (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    code varchar(40) NOT NULL,
    name varchar(200) NOT NULL,
    brand varchar(150),
    manufacturer varchar(150),
    ingredients text,
    aeration varchar(12),
    drainage varchar(12),
    water_retention varchar(12),
    ec_min numeric(8,3),
    ec_max numeric(8,3),
    ec_method varchar(16),
    use_context varchar(12),
    sample_available boolean,
    structure_notes text,
    nutrition_notes text,
    organic_matter_notes text,
    biology_notes text,
    cleanliness_notes text,
    ph_min numeric(4,2),
    ph_max numeric(4,2),
    storage_note text,
    source_reference text,
    reviewed_by integer,
    reviewed_at timestamptz,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    product_type varchar(24) NOT NULL DEFAULT 'Unclassified',
    CONSTRAINT uq_products_1 UNIQUE (code),
    CONSTRAINT ck_products_1 CHECK (drainage IS NULL OR drainage IN ('Low','Medium','High')),
    CONSTRAINT ck_products_2 CHECK (water_retention IS NULL OR water_retention IN ('Low','Medium','High')),
    CONSTRAINT ck_products_3 CHECK ((ph_min IS NULL AND ph_max IS NULL) OR (ph_min IS NOT NULL AND ph_max IS NOT NULL AND ph_min BETWEEN 0 AND 14 AND ph_max BETWEEN ph_min AND 14)),
    CONSTRAINT ck_products_4 CHECK ((reviewed_by IS NULL AND reviewed_at IS NULL) OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL AND source_reference IS NOT NULL AND length(trim(source_reference))>0)),
    CONSTRAINT ck_products_5 CHECK (product_type IN ('Unclassified','GeneralPurpose','FoliageMix','FloweringMix','SucculentMix','OrchidMix','SeedStartingMix')),
    CONSTRAINT ck_products_6 CHECK (aeration IS NULL OR aeration IN ('Low','Medium','High')),
    CONSTRAINT ck_products_7 CHECK (use_context IS NULL OR use_context IN ('Pot','InGround','Both')),
    CONSTRAINT ck_products_8 CHECK ((ec_min IS NULL AND ec_max IS NULL AND ec_method IS NULL) OR (ec_min IS NOT NULL AND ec_max IS NOT NULL AND ec_min >= 0 AND ec_max >= ec_min AND ec_method IS NOT NULL AND ec_method IN ('SME','Extract1to2','PourThrough','Other'))),
    CONSTRAINT fk_products_1 FOREIGN KEY (reviewed_by) REFERENCES users(id) ON DELETE RESTRICT
);

CREATE TABLE plant_tags (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    code varchar(40) NOT NULL,
    name varchar(100) NOT NULL,
    CONSTRAINT uq_plant_tags_1 UNIQUE (code)
);

CREATE TABLE product_plant_tags (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    product_id integer NOT NULL,
    plant_tag_id integer NOT NULL,
    CONSTRAINT uq_product_plant_tags_1 UNIQUE (product_id,plant_tag_id),
    CONSTRAINT fk_product_plant_tags_1 FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE,
    CONSTRAINT fk_product_plant_tags_2 FOREIGN KEY (plant_tag_id) REFERENCES plant_tags(id) ON DELETE RESTRICT
);

CREATE TABLE skus (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    product_id integer NOT NULL,
    code varchar(50) NOT NULL,
    package_amount numeric(10,2) NOT NULL,
    package_unit varchar(8) NOT NULL,
    sale_price_vnd numeric(18,0) NOT NULL,
    reference_cost_vnd numeric(18,0),
    stock_qty integer NOT NULL DEFAULT 0,
    stock_version bigint NOT NULL DEFAULT 0,
    low_stock_threshold integer NOT NULL DEFAULT 5,
    target_stock integer NOT NULL DEFAULT 30,
    preferred_supplier_id integer,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_skus_1 UNIQUE (code),
    CONSTRAINT uq_skus_2 UNIQUE (product_id,package_amount,package_unit),
    CONSTRAINT ck_skus_1 CHECK (package_unit IN ('kg','L')),
    CONSTRAINT ck_skus_2 CHECK (package_amount > 0),
    CONSTRAINT ck_skus_3 CHECK (sale_price_vnd > 0),
    CONSTRAINT ck_skus_4 CHECK (reference_cost_vnd IS NULL OR reference_cost_vnd > 0),
    CONSTRAINT ck_skus_5 CHECK (stock_qty >= 0),
    CONSTRAINT ck_skus_6 CHECK (stock_version >= 0),
    CONSTRAINT ck_skus_7 CHECK (low_stock_threshold >= 0 AND target_stock > low_stock_threshold),
    CONSTRAINT fk_skus_1 FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE RESTRICT,
    CONSTRAINT fk_skus_2 FOREIGN KEY (preferred_supplier_id) REFERENCES suppliers(id) ON DELETE RESTRICT
);

CREATE TABLE purchase_receipts (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    code varchar(50) NOT NULL,
    supplier_id integer NOT NULL,
    status varchar(16) NOT NULL DEFAULT 'Draft',
    created_by integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    confirmed_by integer,
    confirmed_at timestamptz,
    confirmation_key uuid,
    total_vnd numeric(18,0),
    cancelled_by integer,
    cancelled_at timestamptz,
    cancellation_key uuid,
    cancel_reason varchar(500),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_purchase_receipts_1 UNIQUE (code),
    CONSTRAINT ck_purchase_receipts_1 CHECK (status IN ('Draft','Confirmed','Cancelled')),
    CONSTRAINT ck_purchase_receipts_2 CHECK (total_vnd IS NULL OR total_vnd >= 0),
    CONSTRAINT ck_purchase_receipts_3 CHECK ((status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)),
    CONSTRAINT fk_purchase_receipts_1 FOREIGN KEY (supplier_id) REFERENCES suppliers(id) ON DELETE RESTRICT,
    CONSTRAINT fk_purchase_receipts_2 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_purchase_receipts_3 FOREIGN KEY (confirmed_by) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_purchase_receipts_4 FOREIGN KEY (cancelled_by) REFERENCES users(id) ON DELETE RESTRICT
);

CREATE TABLE purchase_receipt_lines (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    receipt_id integer NOT NULL,
    sku_id integer NOT NULL,
    quantity integer NOT NULL,
    unit_cost_vnd numeric(18,0),
    CONSTRAINT uq_purchase_receipt_lines_1 UNIQUE (receipt_id,sku_id),
    CONSTRAINT ck_purchase_receipt_lines_1 CHECK (quantity > 0),
    CONSTRAINT ck_purchase_receipt_lines_2 CHECK (unit_cost_vnd IS NULL OR unit_cost_vnd > 0),
    CONSTRAINT fk_purchase_receipt_lines_1 FOREIGN KEY (receipt_id) REFERENCES purchase_receipts(id) ON DELETE CASCADE,
    CONSTRAINT fk_purchase_receipt_lines_2 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
);

CREATE TABLE sales (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    code varchar(50) NOT NULL,
    status varchar(16) NOT NULL DEFAULT 'Draft',
    created_by integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    confirmed_by integer,
    confirmed_at timestamptz,
    confirmation_key uuid,
    total_vnd numeric(18,0),
    cancelled_by integer,
    cancelled_at timestamptz,
    cancellation_key uuid,
    cancel_reason varchar(500),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_sales_1 UNIQUE (code),
    CONSTRAINT ck_sales_1 CHECK (status IN ('Draft','Confirmed','Cancelled')),
    CONSTRAINT ck_sales_2 CHECK (total_vnd IS NULL OR total_vnd >= 0),
    CONSTRAINT ck_sales_3 CHECK ((status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)),
    CONSTRAINT fk_sales_1 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_sales_2 FOREIGN KEY (confirmed_by) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_sales_3 FOREIGN KEY (cancelled_by) REFERENCES users(id) ON DELETE RESTRICT
);

CREATE TABLE sale_lines (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    sale_id integer NOT NULL,
    sku_id integer NOT NULL,
    quantity integer NOT NULL,
    unit_price_vnd numeric(18,0),
    sku_label_snapshot varchar(300),
    CONSTRAINT uq_sale_lines_1 UNIQUE (sale_id,sku_id),
    CONSTRAINT ck_sale_lines_1 CHECK (quantity > 0),
    CONSTRAINT ck_sale_lines_2 CHECK (unit_price_vnd IS NULL OR unit_price_vnd > 0),
    CONSTRAINT fk_sale_lines_1 FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE CASCADE,
    CONSTRAINT fk_sale_lines_2 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
);

CREATE TABLE stock_counts (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    sku_id integer NOT NULL,
    expected_qty integer NOT NULL,
    expected_stock_version bigint NOT NULL,
    actual_qty integer NOT NULL,
    reason varchar(500) NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Pending',
    counted_by integer NOT NULL,
    counted_at timestamptz NOT NULL DEFAULT now(),
    reviewed_by integer,
    reviewed_at timestamptz,
    review_note varchar(500),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_stock_counts_1 CHECK (expected_qty >= 0 AND actual_qty >= 0 AND expected_stock_version >= 0),
    CONSTRAINT ck_stock_counts_2 CHECK (status IN ('Pending','Approved','RecountRequired','Rejected')),
    CONSTRAINT ck_stock_counts_3 CHECK (length(trim(reason))>0),
    CONSTRAINT ck_stock_counts_4 CHECK (status <> 'Approved' OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)),
    CONSTRAINT fk_stock_counts_1 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT,
    CONSTRAINT fk_stock_counts_2 FOREIGN KEY (counted_by) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_stock_counts_3 FOREIGN KEY (reviewed_by) REFERENCES users(id) ON DELETE RESTRICT
);

CREATE TABLE stock_adjustments (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    request_key uuid NOT NULL,
    sku_id integer NOT NULL,
    stock_count_id integer,
    delta_qty integer NOT NULL,
    reason varchar(500) NOT NULL,
    created_by integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_stock_adjustments_1 UNIQUE (request_key),
    CONSTRAINT ck_stock_adjustments_1 CHECK (delta_qty <> 0),
    CONSTRAINT ck_stock_adjustments_2 CHECK (length(trim(reason))>0),
    CONSTRAINT fk_stock_adjustments_1 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT,
    CONSTRAINT fk_stock_adjustments_2 FOREIGN KEY (stock_count_id) REFERENCES stock_counts(id) ON DELETE RESTRICT,
    CONSTRAINT fk_stock_adjustments_3 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT
);

CREATE TABLE stock_movements (
    id bigint GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    sku_id integer NOT NULL,
    kind varchar(16) NOT NULL,
    delta_qty integer NOT NULL,
    balance_after integer NOT NULL,
    stock_version_after bigint NOT NULL,
    receipt_id integer,
    sale_id integer,
    adjustment_id integer,
    actor_id integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_stock_movements_1 UNIQUE (sku_id,stock_version_after),
    CONSTRAINT ck_stock_movements_1 CHECK (delta_qty <> 0 AND balance_after >= 0 AND stock_version_after > 0),
    CONSTRAINT ck_stock_movements_2 CHECK ((kind IN ('Purchase','PurchaseCancel') AND receipt_id IS NOT NULL AND sale_id IS NULL AND adjustment_id IS NULL) OR (kind IN ('Sale','SaleCancel') AND sale_id IS NOT NULL AND receipt_id IS NULL AND adjustment_id IS NULL) OR (kind='Adjustment' AND adjustment_id IS NOT NULL AND sale_id IS NULL AND receipt_id IS NULL)),
    CONSTRAINT ck_stock_movements_3 CHECK ((kind IN ('Purchase','SaleCancel') AND delta_qty>0) OR (kind IN ('Sale','PurchaseCancel') AND delta_qty<0) OR kind='Adjustment'),
    CONSTRAINT fk_stock_movements_1 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT,
    CONSTRAINT fk_stock_movements_2 FOREIGN KEY (receipt_id) REFERENCES purchase_receipts(id) ON DELETE RESTRICT,
    CONSTRAINT fk_stock_movements_3 FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE RESTRICT,
    CONSTRAINT fk_stock_movements_4 FOREIGN KEY (adjustment_id) REFERENCES stock_adjustments(id) ON DELETE RESTRICT,
    CONSTRAINT fk_stock_movements_5 FOREIGN KEY (actor_id) REFERENCES users(id) ON DELETE RESTRICT
);

CREATE TABLE restock_suggestions (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    sku_id integer NOT NULL,
    supplier_id integer,
    observed_qty integer NOT NULL,
    suggested_qty integer NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Open',
    receipt_id integer,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_restock_suggestions_1 CHECK (observed_qty >= 0 AND suggested_qty >= 0),
    CONSTRAINT ck_restock_suggestions_2 CHECK (status IN ('Open','ReceiptDrafted','Resolved')),
    CONSTRAINT ck_restock_suggestions_3 CHECK (status <> 'ReceiptDrafted' OR receipt_id IS NOT NULL),
    CONSTRAINT fk_restock_suggestions_1 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT,
    CONSTRAINT fk_restock_suggestions_2 FOREIGN KEY (supplier_id) REFERENCES suppliers(id) ON DELETE RESTRICT,
    CONSTRAINT fk_restock_suggestions_3 FOREIGN KEY (receipt_id) REFERENCES purchase_receipts(id) ON DELETE RESTRICT
);

CREATE TABLE outbox_messages (
    id uuid NOT NULL PRIMARY KEY,
    kind varchar(24) NOT NULL,
    payload jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    published_at timestamptz,
    attempts integer NOT NULL DEFAULT 0,
    last_error text,
    CONSTRAINT ck_outbox_messages_1 CHECK (kind IN ('InventoryChanged','BackupRequested','SummaryRequested')),
    CONSTRAINT ck_outbox_messages_2 CHECK (attempts >= 0)
);

CREATE TABLE daily_summaries (
    id integer GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    business_date date NOT NULL,
    status varchar(16) NOT NULL DEFAULT 'Queued',
    revenue_vnd numeric(18,0) NOT NULL DEFAULT 0,
    sold_bags bigint NOT NULL DEFAULT 0,
    sale_count integer NOT NULL DEFAULT 0,
    snapshot_at timestamptz,
    last_error text,
    created_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_daily_summaries_1 UNIQUE (business_date),
    CONSTRAINT ck_daily_summaries_1 CHECK (status IN ('Queued','Processing','Completed','Failed')),
    CONSTRAINT ck_daily_summaries_2 CHECK (revenue_vnd >= 0 AND sold_bags >= 0 AND sale_count >= 0)
);

CREATE TABLE daily_sku_snapshots (
    id bigint GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    summary_id integer NOT NULL,
    sku_id integer NOT NULL,
    stock_qty integer NOT NULL,
    CONSTRAINT uq_daily_sku_snapshots_1 UNIQUE (summary_id,sku_id),
    CONSTRAINT ck_daily_sku_snapshots_1 CHECK (stock_qty >= 0),
    CONSTRAINT fk_daily_sku_snapshots_1 FOREIGN KEY (summary_id) REFERENCES daily_summaries(id) ON DELETE CASCADE,
    CONSTRAINT fk_daily_sku_snapshots_2 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
);

CREATE TABLE backup_runs (
    id uuid NOT NULL PRIMARY KEY,
    kind varchar(12) NOT NULL,
    scheduled_date date,
    requested_by integer,
    status varchar(16) NOT NULL DEFAULT 'Queued',
    filename varchar(200),
    retained boolean NOT NULL DEFAULT false,
    attempts integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz,
    last_error text,
    CONSTRAINT ck_backup_runs_1 CHECK (kind IN ('Daily','Manual')),
    CONSTRAINT ck_backup_runs_2 CHECK (status IN ('Queued','Processing','Completed','Failed')),
    CONSTRAINT ck_backup_runs_3 CHECK ((kind='Daily' AND scheduled_date IS NOT NULL) OR (kind='Manual' AND scheduled_date IS NULL)),
    CONSTRAINT ck_backup_runs_4 CHECK (NOT retained OR (status='Completed' AND filename IS NOT NULL AND completed_at IS NOT NULL)),
    CONSTRAINT ck_backup_runs_5 CHECK (attempts >= 0),
    CONSTRAINT fk_backup_runs_1 FOREIGN KEY (requested_by) REFERENCES users(id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX ux_users_username_fold ON users (lower(username));
CREATE UNIQUE INDEX ux_supplier_code_fold ON suppliers (upper(code));
CREATE UNIQUE INDEX ux_product_code_fold ON products (upper(code));
CREATE UNIQUE INDEX ux_sku_code_fold ON skus (upper(code));
CREATE INDEX ix_skus_product ON skus (product_id);
CREATE INDEX ix_skus_low_stock ON skus (id) WHERE is_active AND stock_qty <= low_stock_threshold;
CREATE UNIQUE INDEX ux_sales_confirmation ON sales (confirmation_key) WHERE confirmation_key IS NOT NULL;
CREATE UNIQUE INDEX ux_sales_cancellation ON sales (cancellation_key) WHERE cancellation_key IS NOT NULL;
CREATE UNIQUE INDEX ux_receipt_confirmation ON purchase_receipts (confirmation_key) WHERE confirmation_key IS NOT NULL;
CREATE UNIQUE INDEX ux_receipt_cancellation ON purchase_receipts (cancellation_key) WHERE cancellation_key IS NOT NULL;
CREATE UNIQUE INDEX ux_adjustment_count ON stock_adjustments (stock_count_id) WHERE stock_count_id IS NOT NULL;
CREATE UNIQUE INDEX ux_movement_sale ON stock_movements (sale_id,sku_id,kind) WHERE sale_id IS NOT NULL;
CREATE UNIQUE INDEX ux_movement_receipt ON stock_movements (receipt_id,sku_id,kind) WHERE receipt_id IS NOT NULL;
CREATE UNIQUE INDEX ux_movement_adjustment ON stock_movements (adjustment_id) WHERE adjustment_id IS NOT NULL;
CREATE UNIQUE INDEX ux_restock_active ON restock_suggestions (sku_id) WHERE status IN ('Open','ReceiptDrafted');
CREATE INDEX ix_movements_sku_time ON stock_movements (sku_id,created_at DESC,id DESC);
CREATE INDEX ix_outbox_pending ON outbox_messages (created_at) WHERE published_at IS NULL;
CREATE INDEX ix_sales_report ON sales (confirmed_at) WHERE status='Confirmed';
CREATE UNIQUE INDEX ux_backup_daily ON backup_runs (scheduled_date) WHERE scheduled_date IS NOT NULL;

COMMIT;

-- Services own document state transitions, nonempty lines, totals, stock-version increments,
-- ledger balance correctness, reasoned count approval, and outbox insertion in one transaction.
-- Append-only business records and published metadata sources require API/role enforcement.
