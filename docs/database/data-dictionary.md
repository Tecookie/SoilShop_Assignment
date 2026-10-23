# PostgreSQL Physical Data Dictionary

Generated from the schema model. Application implementation and EF migrations are pending. All non-CASCADE foreign keys restrict deletion. Prices are whole VND; Services reject fractional values before PostgreSQL numeric conversion.

## users

Internal Admin/Employee accounts; deactivate instead of deleting referenced actors.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| username | `varchar(100)` | No |  |
| display_name | `varchar(150)` | No |  |
| password_hash | `text` | No |  |
| role | `varchar(16)` | No |  |
| is_active | `boolean` | No | true |
| created_at | `timestamptz` | No | now() |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_users_1 UNIQUE (username)
    CONSTRAINT ck_users_1 CHECK (role IN ('Admin','Employee'))
    CONSTRAINT ck_users_2 CHECK (length(trim(username)) > 0)
```

## suppliers

Sources of pre-packed bags; no external integration.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| code | `varchar(40)` | No |  |
| name | `varchar(200)` | No |  |
| contact_note | `text` | Yes |  |
| is_active | `boolean` | No | true |
| created_at | `timestamptz` | No | now() |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_suppliers_1 UNIQUE (code)
```

## products

Soil offering and optional source-backed advice metadata.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| code | `varchar(40)` | No |  |
| name | `varchar(200)` | No |  |
| brand | `varchar(150)` | Yes |  |
| manufacturer | `varchar(150)` | Yes |  |
| ingredients | `text` | Yes |  |
| drainage | `varchar(12)` | Yes |  |
| water_retention | `varchar(12)` | Yes |  |
| ph_min | `numeric(4,2)` | Yes |  |
| ph_max | `numeric(4,2)` | Yes |  |
| storage_note | `text` | Yes |  |
| source_reference | `text` | Yes |  |
| reviewed_by | `integer` | Yes |  FK |
| reviewed_at | `timestamptz` | Yes |  |
| is_active | `boolean` | No | true |
| created_at | `timestamptz` | No | now() |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_products_1 UNIQUE (code)
    CONSTRAINT ck_products_1 CHECK (drainage IS NULL OR drainage IN ('Low','Medium','High'))
    CONSTRAINT ck_products_2 CHECK (water_retention IS NULL OR water_retention IN ('Low','Medium','High'))
    CONSTRAINT ck_products_3 CHECK ((ph_min IS NULL AND ph_max IS NULL) OR (ph_min IS NOT NULL AND ph_max IS NOT NULL AND ph_min BETWEEN 0 AND 14 AND ph_max BETWEEN ph_min AND 14))
    CONSTRAINT ck_products_4 CHECK ((reviewed_by IS NULL AND reviewed_at IS NULL) OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL AND source_reference IS NOT NULL AND length(trim(source_reference))>0))
    CONSTRAINT fk_products_1 FOREIGN KEY (reviewed_by) REFERENCES users(id) ON DELETE RESTRICT
```

## plant_tags

Staff-curated label/use tags, not an agronomic taxonomy.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| code | `varchar(40)` | No |  |
| name | `varchar(100)` | No |  |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_plant_tags_1 UNIQUE (code)
```

## product_plant_tags

Product-to-use-tag association.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| product_id | `integer` | No |  FK |
| plant_tag_id | `integer` | No |  FK |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_product_plant_tags_1 UNIQUE (product_id,plant_tag_id)
    CONSTRAINT fk_product_plant_tags_1 FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
    CONSTRAINT fk_product_plant_tags_2 FOREIGN KEY (plant_tag_id) REFERENCES plant_tags(id) ON DELETE RESTRICT
```

## skus

Independent packaging, price and single-location bag stock.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| product_id | `integer` | No |  FK |
| code | `varchar(50)` | No |  |
| package_amount | `numeric(10,2)` | No |  |
| package_unit | `varchar(8)` | No |  |
| sale_price_vnd | `numeric(18,0)` | No |  |
| reference_cost_vnd | `numeric(18,0)` | Yes |  |
| stock_qty | `integer` | No | 0 |
| stock_version | `bigint` | No | 0 |
| low_stock_threshold | `integer` | No | 5 |
| target_stock | `integer` | No | 30 |
| preferred_supplier_id | `integer` | Yes |  FK |
| is_active | `boolean` | No | true |
| created_at | `timestamptz` | No | now() |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_skus_1 UNIQUE (code)
    CONSTRAINT uq_skus_2 UNIQUE (product_id,package_amount,package_unit)
    CONSTRAINT ck_skus_1 CHECK (package_unit IN ('kg','L'))
    CONSTRAINT ck_skus_2 CHECK (package_amount > 0)
    CONSTRAINT ck_skus_3 CHECK (sale_price_vnd > 0)
    CONSTRAINT ck_skus_4 CHECK (reference_cost_vnd IS NULL OR reference_cost_vnd > 0)
    CONSTRAINT ck_skus_5 CHECK (stock_qty >= 0)
    CONSTRAINT ck_skus_6 CHECK (stock_version >= 0)
    CONSTRAINT ck_skus_7 CHECK (low_stock_threshold >= 0 AND target_stock > low_stock_threshold)
    CONSTRAINT fk_skus_1 FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE RESTRICT
    CONSTRAINT fk_skus_2 FOREIGN KEY (preferred_supplier_id) REFERENCES suppliers(id) ON DELETE RESTRICT
```

## purchase_receipts

Draft/Confirmed/Cancelled inbound document; actual costs are historical snapshots.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| code | `varchar(50)` | No |  |
| supplier_id | `integer` | No |  FK |
| status | `varchar(16)` | No | 'Draft' |
| created_by | `integer` | No |  FK |
| created_at | `timestamptz` | No | now() |
| confirmed_by | `integer` | Yes |  FK |
| confirmed_at | `timestamptz` | Yes |  |
| confirmation_key | `uuid` | Yes |  |
| total_vnd | `numeric(18,0)` | Yes |  |
| cancelled_by | `integer` | Yes |  FK |
| cancelled_at | `timestamptz` | Yes |  |
| cancellation_key | `uuid` | Yes |  |
| cancel_reason | `varchar(500)` | Yes |  |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_purchase_receipts_1 UNIQUE (code)
    CONSTRAINT ck_purchase_receipts_1 CHECK (status IN ('Draft','Confirmed','Cancelled'))
    CONSTRAINT ck_purchase_receipts_2 CHECK (total_vnd IS NULL OR total_vnd >= 0)
    CONSTRAINT ck_purchase_receipts_3 CHECK ((status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0))
    CONSTRAINT fk_purchase_receipts_1 FOREIGN KEY (supplier_id) REFERENCES suppliers(id) ON DELETE RESTRICT
    CONSTRAINT fk_purchase_receipts_2 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT
    CONSTRAINT fk_purchase_receipts_3 FOREIGN KEY (confirmed_by) REFERENCES users(id) ON DELETE RESTRICT
    CONSTRAINT fk_purchase_receipts_4 FOREIGN KEY (cancelled_by) REFERENCES users(id) ON DELETE RESTRICT
```

## purchase_receipt_lines

One SKU per receipt; unknown cost allowed in draft only.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| receipt_id | `integer` | No |  FK |
| sku_id | `integer` | No |  FK |
| quantity | `integer` | No |  |
| unit_cost_vnd | `numeric(18,0)` | Yes |  |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_purchase_receipt_lines_1 UNIQUE (receipt_id,sku_id)
    CONSTRAINT ck_purchase_receipt_lines_1 CHECK (quantity > 0)
    CONSTRAINT ck_purchase_receipt_lines_2 CHECK (unit_cost_vnd IS NULL OR unit_cost_vnd > 0)
    CONSTRAINT fk_purchase_receipt_lines_1 FOREIGN KEY (receipt_id) REFERENCES purchase_receipts(id) ON DELETE CASCADE
    CONSTRAINT fk_purchase_receipt_lines_2 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
```

## sales

Counter sale; no customer account/payment gateway; confirmed data is immutable.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| code | `varchar(50)` | No |  |
| status | `varchar(16)` | No | 'Draft' |
| created_by | `integer` | No |  FK |
| created_at | `timestamptz` | No | now() |
| confirmed_by | `integer` | Yes |  FK |
| confirmed_at | `timestamptz` | Yes |  |
| confirmation_key | `uuid` | Yes |  |
| total_vnd | `numeric(18,0)` | Yes |  |
| cancelled_by | `integer` | Yes |  FK |
| cancelled_at | `timestamptz` | Yes |  |
| cancellation_key | `uuid` | Yes |  |
| cancel_reason | `varchar(500)` | Yes |  |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_sales_1 UNIQUE (code)
    CONSTRAINT ck_sales_1 CHECK (status IN ('Draft','Confirmed','Cancelled'))
    CONSTRAINT ck_sales_2 CHECK (total_vnd IS NULL OR total_vnd >= 0)
    CONSTRAINT ck_sales_3 CHECK ((status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0))
    CONSTRAINT fk_sales_1 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT
    CONSTRAINT fk_sales_2 FOREIGN KEY (confirmed_by) REFERENCES users(id) ON DELETE RESTRICT
    CONSTRAINT fk_sales_3 FOREIGN KEY (cancelled_by) REFERENCES users(id) ON DELETE RESTRICT
```

## sale_lines

Frozen SKU price at confirmation, quantity and display snapshot.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| sale_id | `integer` | No |  FK |
| sku_id | `integer` | No |  FK |
| quantity | `integer` | No |  |
| unit_price_vnd | `numeric(18,0)` | Yes |  |
| sku_label_snapshot | `varchar(300)` | Yes |  |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_sale_lines_1 UNIQUE (sale_id,sku_id)
    CONSTRAINT ck_sale_lines_1 CHECK (quantity > 0)
    CONSTRAINT ck_sale_lines_2 CHECK (unit_price_vnd IS NULL OR unit_price_vnd > 0)
    CONSTRAINT fk_sale_lines_1 FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE CASCADE
    CONSTRAINT fk_sale_lines_2 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
```

## stock_counts

One SKU physical count with captured stock version and Admin decision.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| sku_id | `integer` | No |  FK |
| expected_qty | `integer` | No |  |
| expected_stock_version | `bigint` | No |  |
| actual_qty | `integer` | No |  |
| reason | `varchar(500)` | No |  |
| status | `varchar(20)` | No | 'Pending' |
| counted_by | `integer` | No |  FK |
| counted_at | `timestamptz` | No | now() |
| reviewed_by | `integer` | Yes |  FK |
| reviewed_at | `timestamptz` | Yes |  |
| review_note | `varchar(500)` | Yes |  |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT ck_stock_counts_1 CHECK (expected_qty >= 0 AND actual_qty >= 0 AND expected_stock_version >= 0)
    CONSTRAINT ck_stock_counts_2 CHECK (status IN ('Pending','Approved','RecountRequired','Rejected'))
    CONSTRAINT ck_stock_counts_3 CHECK (length(trim(reason))>0)
    CONSTRAINT ck_stock_counts_4 CHECK (status <> 'Approved' OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL))
    CONSTRAINT fk_stock_counts_1 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
    CONSTRAINT fk_stock_counts_2 FOREIGN KEY (counted_by) REFERENCES users(id) ON DELETE RESTRICT
    CONSTRAINT fk_stock_counts_3 FOREIGN KEY (reviewed_by) REFERENCES users(id) ON DELETE RESTRICT
```

## stock_adjustments

Immutable signed correction, optionally originating from an approved stock count.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| request_key | `uuid` | No |  |
| sku_id | `integer` | No |  FK |
| stock_count_id | `integer` | Yes |  FK |
| delta_qty | `integer` | No |  |
| reason | `varchar(500)` | No |  |
| created_by | `integer` | No |  FK |
| created_at | `timestamptz` | No | now() |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_stock_adjustments_1 UNIQUE (request_key)
    CONSTRAINT ck_stock_adjustments_1 CHECK (delta_qty <> 0)
    CONSTRAINT ck_stock_adjustments_2 CHECK (length(trim(reason))>0)
    CONSTRAINT fk_stock_adjustments_1 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
    CONSTRAINT fk_stock_adjustments_2 FOREIGN KEY (stock_count_id) REFERENCES stock_counts(id) ON DELETE RESTRICT
    CONSTRAINT fk_stock_adjustments_3 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT
```

## stock_movements

Immutable stock ledger, one row per source/SKU/action.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `bigint` | No | PK |
| sku_id | `integer` | No |  FK |
| kind | `varchar(16)` | No |  |
| delta_qty | `integer` | No |  |
| balance_after | `integer` | No |  |
| stock_version_after | `bigint` | No |  |
| receipt_id | `integer` | Yes |  FK |
| sale_id | `integer` | Yes |  FK |
| adjustment_id | `integer` | Yes |  FK |
| actor_id | `integer` | No |  FK |
| created_at | `timestamptz` | No | now() |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_stock_movements_1 UNIQUE (sku_id,stock_version_after)
    CONSTRAINT ck_stock_movements_1 CHECK (delta_qty <> 0 AND balance_after >= 0 AND stock_version_after > 0)
    CONSTRAINT ck_stock_movements_2 CHECK ((kind IN ('Purchase','PurchaseCancel') AND receipt_id IS NOT NULL AND sale_id IS NULL AND adjustment_id IS NULL) OR (kind IN ('Sale','SaleCancel') AND sale_id IS NOT NULL AND receipt_id IS NULL AND adjustment_id IS NULL) OR (kind='Adjustment' AND adjustment_id IS NOT NULL AND sale_id IS NULL AND receipt_id IS NULL))
    CONSTRAINT ck_stock_movements_3 CHECK ((kind IN ('Purchase','SaleCancel') AND delta_qty>0) OR (kind IN ('Sale','PurchaseCancel') AND delta_qty<0) OR kind='Adjustment')
    CONSTRAINT fk_stock_movements_1 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
    CONSTRAINT fk_stock_movements_2 FOREIGN KEY (receipt_id) REFERENCES purchase_receipts(id) ON DELETE RESTRICT
    CONSTRAINT fk_stock_movements_3 FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE RESTRICT
    CONSTRAINT fk_stock_movements_4 FOREIGN KEY (adjustment_id) REFERENCES stock_adjustments(id) ON DELETE RESTRICT
    CONSTRAINT fk_stock_movements_5 FOREIGN KEY (actor_id) REFERENCES users(id) ON DELETE RESTRICT
```

## restock_suggestions

Single active low-stock alert/suggestion per SKU and optional receipt-draft link.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| sku_id | `integer` | No |  FK |
| supplier_id | `integer` | Yes |  FK |
| observed_qty | `integer` | No |  |
| suggested_qty | `integer` | No |  |
| status | `varchar(20)` | No | 'Open' |
| receipt_id | `integer` | Yes |  FK |
| created_at | `timestamptz` | No | now() |
| updated_at | `timestamptz` | No | now() |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT ck_restock_suggestions_1 CHECK (observed_qty >= 0 AND suggested_qty >= 0)
    CONSTRAINT ck_restock_suggestions_2 CHECK (status IN ('Open','ReceiptDrafted','Resolved'))
    CONSTRAINT ck_restock_suggestions_3 CHECK (status <> 'ReceiptDrafted' OR receipt_id IS NOT NULL)
    CONSTRAINT fk_restock_suggestions_1 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
    CONSTRAINT fk_restock_suggestions_2 FOREIGN KEY (supplier_id) REFERENCES suppliers(id) ON DELETE RESTRICT
    CONSTRAINT fk_restock_suggestions_3 FOREIGN KEY (receipt_id) REFERENCES purchase_receipts(id) ON DELETE RESTRICT
```

## outbox_messages

Durable post-commit event intent; API relay publishes to Redis.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `uuid` | No | PK |
| kind | `varchar(24)` | No |  |
| payload | `jsonb` | No |  |
| created_at | `timestamptz` | No | now() |
| published_at | `timestamptz` | Yes |  |
| attempts | `integer` | No | 0 |
| last_error | `text` | Yes |  |

Constraints / foreign keys:

```sql
    CONSTRAINT ck_outbox_messages_1 CHECK (kind IN ('InventoryChanged','BackupRequested','SummaryRequested'))
    CONSTRAINT ck_outbox_messages_2 CHECK (attempts >= 0)
```

## daily_summaries

One recomputable Vietnam business-date report, explicitly timestamped.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `integer` | No | PK |
| business_date | `date` | No |  |
| status | `varchar(16)` | No | 'Queued' |
| revenue_vnd | `numeric(18,0)` | No | 0 |
| sold_bags | `bigint` | No | 0 |
| sale_count | `integer` | No | 0 |
| snapshot_at | `timestamptz` | Yes |  |
| last_error | `text` | Yes |  |
| created_at | `timestamptz` | No | now() |
| version | `bigint` | No | 1 |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_daily_summaries_1 UNIQUE (business_date)
    CONSTRAINT ck_daily_summaries_1 CHECK (status IN ('Queued','Processing','Completed','Failed'))
    CONSTRAINT ck_daily_summaries_2 CHECK (revenue_vnd >= 0 AND sold_bags >= 0 AND sale_count >= 0)
```

## daily_sku_snapshots

Stock levels at report generation, not reconstructed day-end inventory.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `bigint` | No | PK |
| summary_id | `integer` | No |  FK |
| sku_id | `integer` | No |  FK |
| stock_qty | `integer` | No |  |

Constraints / foreign keys:

```sql
    CONSTRAINT uq_daily_sku_snapshots_1 UNIQUE (summary_id,sku_id)
    CONSTRAINT ck_daily_sku_snapshots_1 CHECK (stock_qty >= 0)
    CONSTRAINT fk_daily_sku_snapshots_1 FOREIGN KEY (summary_id) REFERENCES daily_summaries(id) ON DELETE CASCADE
    CONSTRAINT fk_daily_sku_snapshots_2 FOREIGN KEY (sku_id) REFERENCES skus(id) ON DELETE RESTRICT
```

## backup_runs

Daily/manual pg_dump jobs; seven successful retained files across both kinds.

| Column | PostgreSQL type | Nullable | Default / key |
|---|---|---|---|
| id | `uuid` | No | PK |
| kind | `varchar(12)` | No |  |
| scheduled_date | `date` | Yes |  |
| requested_by | `integer` | Yes |  FK |
| status | `varchar(16)` | No | 'Queued' |
| filename | `varchar(200)` | Yes |  |
| retained | `boolean` | No | false |
| attempts | `integer` | No | 0 |
| created_at | `timestamptz` | No | now() |
| completed_at | `timestamptz` | Yes |  |
| last_error | `text` | Yes |  |

Constraints / foreign keys:

```sql
    CONSTRAINT ck_backup_runs_1 CHECK (kind IN ('Daily','Manual'))
    CONSTRAINT ck_backup_runs_2 CHECK (status IN ('Queued','Processing','Completed','Failed'))
    CONSTRAINT ck_backup_runs_3 CHECK ((kind='Daily' AND scheduled_date IS NOT NULL) OR (kind='Manual' AND scheduled_date IS NULL))
    CONSTRAINT ck_backup_runs_4 CHECK (NOT retained OR (status='Completed' AND filename IS NOT NULL AND completed_at IS NOT NULL))
    CONSTRAINT ck_backup_runs_5 CHECK (attempts >= 0)
    CONSTRAINT fk_backup_runs_1 FOREIGN KEY (requested_by) REFERENCES users(id) ON DELETE RESTRICT
```

## Additional indexes

```sql
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
```

## Service invariants

- A confirmed document must have at least one line and known positive prices.
- Confirmation/cancellation locks the document; stock updates, ledger, totals and outbox commit atomically.
- StockCount approval compares expected_stock_version; stock_adjustments.stock_count_id is unique.
- Recorded source and curator are required when publishing metadata for advisory use.
- References in JSON outbox payloads are operation IDs, not polymorphic SQL foreign keys.
- Only one active RestockSuggestion per SKU; receipt-draft conversion is idempotent.
- Master-data delete requires no historical references; prefer is_active=false.
