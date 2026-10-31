"""Single source for the PostgreSQL DESIGN baseline, dictionary and diagram fields."""
from pathlib import Path
import json

ROOT=Path(__file__).resolve().parents[2]
TABLES={}
def c(name,typ='integer',nullable=False,default=None,identity=False):
    return dict(name=name,type=typ,nullable=nullable,default=default,identity=identity)
def ident(big=False): return c('id','bigint' if big else 'integer',identity=True)
def now(name='created_at'): return c(name,'timestamptz',default='now()')
def version(name='version'): return c(name,'bigint',default='1')
def fk(column,parent,action='RESTRICT',key='id'): return (column,parent,key,action)
def t(name,desc,cols,fks=(),checks=(),unique=()):
    TABLES[name]=dict(description=desc,columns=cols,foreign_keys=list(fks),checks=list(checks),unique=list(unique))

t('users','Internal Admin/Employee accounts; deactivate instead of deleting referenced actors.',[
    ident(),c('username','varchar(100)'),c('display_name','varchar(150)'),c('password_hash','text'),
    c('role','varchar(16)'),c('is_active','boolean',default='true'),now(),version()],
    checks=["role IN ('Admin','Employee')","length(trim(username)) > 0"],unique=['username'])
t('suppliers','Sources of pre-packed bags; no external integration.',[
    ident(),c('code','varchar(40)'),c('name','varchar(200)'),c('contact_note','text',True),
    c('is_active','boolean',default='true'),now(),version()],unique=['code'])
t('products','Soil offering and optional source-backed advice metadata.',[
    ident(),c('code','varchar(40)'),c('name','varchar(200)'),c('brand','varchar(150)',True),
    c('manufacturer','varchar(150)',True),c('ingredients','text',True),
    c('aeration','varchar(12)',True),c('drainage','varchar(12)',True),c('water_retention','varchar(12)',True),
    c('ec_min','numeric(8,3)',True),c('ec_max','numeric(8,3)',True),c('ec_method','varchar(16)',True),
    c('use_context','varchar(12)',True),c('sample_available','boolean',True),
    *[c(n,'text',True) for n in ['structure_notes','nutrition_notes','organic_matter_notes','biology_notes','cleanliness_notes']],
    c('ph_min','numeric(4,2)',True),c('ph_max','numeric(4,2)',True),c('storage_note','text',True),
    c('source_reference','text',True),c('reviewed_by','integer',True),c('reviewed_at','timestamptz',True),
    c('is_active','boolean',default='true'),now(),version(),c('product_type','varchar(24)',default="'Unclassified'")],fks=[fk('reviewed_by','users')],
    checks=["drainage IS NULL OR drainage IN ('Low','Medium','High')",
            "water_retention IS NULL OR water_retention IN ('Low','Medium','High')",
            '(ph_min IS NULL AND ph_max IS NULL) OR (ph_min IS NOT NULL AND ph_max IS NOT NULL AND ph_min BETWEEN 0 AND 14 AND ph_max BETWEEN ph_min AND 14)',
            '(reviewed_by IS NULL AND reviewed_at IS NULL) OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL AND source_reference IS NOT NULL AND length(trim(source_reference))>0)',
            "product_type IN ('Unclassified','GeneralPurpose','FoliageMix','FloweringMix','SucculentMix','OrchidMix','SeedStartingMix')",
            "aeration IS NULL OR aeration IN ('Low','Medium','High')",
            "use_context IS NULL OR use_context IN ('Pot','InGround','Both')",
            "(ec_min IS NULL AND ec_max IS NULL AND ec_method IS NULL) OR (ec_min IS NOT NULL AND ec_max IS NOT NULL AND ec_min >= 0 AND ec_max >= ec_min AND ec_method IS NOT NULL AND ec_method IN ('SME','Extract1to2','PourThrough','Other'))"],unique=['code'])
t('plant_tags','Staff-curated label/use tags, not an agronomic taxonomy.',[
    ident(),c('code','varchar(40)'),c('name','varchar(100)')],unique=['code'])
t('product_plant_tags','Product-to-use-tag association.',[
    ident(),c('product_id'),c('plant_tag_id')],fks=[fk('product_id','products','CASCADE'),fk('plant_tag_id','plant_tags')],unique=['product_id,plant_tag_id'])
t('skus','Independent packaging, price and single-location bag stock.',[
    ident(),c('product_id'),c('code','varchar(50)'),c('package_amount','numeric(10,2)'),
    c('package_unit','varchar(8)'),c('sale_price_vnd','numeric(18,0)'),c('reference_cost_vnd','numeric(18,0)',True),
    c('stock_qty','integer',default='0'),c('stock_version','bigint',default='0'),
    c('low_stock_threshold','integer',default='5'),c('target_stock','integer',default='30'),
    c('preferred_supplier_id','integer',True),c('is_active','boolean',default='true'),now(),version()],
    fks=[fk('product_id','products'),fk('preferred_supplier_id','suppliers')],
    checks=["package_unit IN ('kg','L')",'package_amount > 0','sale_price_vnd > 0',
            'reference_cost_vnd IS NULL OR reference_cost_vnd > 0','stock_qty >= 0','stock_version >= 0',
            'low_stock_threshold >= 0 AND target_stock > low_stock_threshold'],unique=['code','product_id,package_amount,package_unit'])

def document_cols(receipt=False):
    return [ident(),c('code','varchar(50)'),*([c('supplier_id')] if receipt else []),
        c('status','varchar(16)',default="'Draft'"),c('created_by'),now(),
        c('confirmed_by','integer',True),c('confirmed_at','timestamptz',True),c('confirmation_key','uuid',True),
        c('total_vnd','numeric(18,0)',True),c('cancelled_by','integer',True),c('cancelled_at','timestamptz',True),
        c('cancellation_key','uuid',True),c('cancel_reason','varchar(500)',True),version()]
doc_checks=["status IN ('Draft','Confirmed','Cancelled')",'total_vnd IS NULL OR total_vnd >= 0',
    "(status='Draft' AND confirmed_at IS NULL AND confirmed_by IS NULL AND confirmation_key IS NULL AND total_vnd IS NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Confirmed' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NULL AND cancelled_by IS NULL AND cancellation_key IS NULL AND cancel_reason IS NULL) OR (status='Cancelled' AND confirmed_at IS NOT NULL AND confirmed_by IS NOT NULL AND confirmation_key IS NOT NULL AND total_vnd IS NOT NULL AND cancelled_at IS NOT NULL AND cancelled_by IS NOT NULL AND cancellation_key IS NOT NULL AND cancel_reason IS NOT NULL AND length(trim(cancel_reason))>0)"]
actor_fks=[fk('created_by','users'),fk('confirmed_by','users'),fk('cancelled_by','users')]
t('purchase_receipts','Draft/Confirmed/Cancelled inbound document; actual costs are historical snapshots.',document_cols(True),
    fks=[fk('supplier_id','suppliers'),*actor_fks],checks=doc_checks,unique=['code'])
t('purchase_receipt_lines','One SKU per receipt; unknown cost allowed in draft only.',[
    ident(),c('receipt_id'),c('sku_id'),c('quantity'),c('unit_cost_vnd','numeric(18,0)',True)],
    fks=[fk('receipt_id','purchase_receipts','CASCADE'),fk('sku_id','skus')],
    checks=['quantity > 0','unit_cost_vnd IS NULL OR unit_cost_vnd > 0'],unique=['receipt_id,sku_id'])
t('sales','Counter sale; no customer account/payment gateway; confirmed data is immutable.',document_cols(),
    fks=actor_fks,checks=doc_checks,unique=['code'])
t('sale_lines','Frozen SKU price at confirmation, quantity and display snapshot.',[
    ident(),c('sale_id'),c('sku_id'),c('quantity'),c('unit_price_vnd','numeric(18,0)',True),c('sku_label_snapshot','varchar(300)',True)],
    fks=[fk('sale_id','sales','CASCADE'),fk('sku_id','skus')],
    checks=['quantity > 0','unit_price_vnd IS NULL OR unit_price_vnd > 0'],unique=['sale_id,sku_id'])
t('stock_counts','One SKU physical count with captured stock version and Admin decision.',[
    ident(),c('sku_id'),c('expected_qty'),c('expected_stock_version','bigint'),c('actual_qty'),
    c('reason','varchar(500)'),c('status','varchar(20)',default="'Pending'"),c('counted_by'),now('counted_at'),
    c('reviewed_by','integer',True),c('reviewed_at','timestamptz',True),c('review_note','varchar(500)',True),version()],
    fks=[fk('sku_id','skus'),fk('counted_by','users'),fk('reviewed_by','users')],
    checks=['expected_qty >= 0 AND actual_qty >= 0 AND expected_stock_version >= 0',
            "status IN ('Pending','Approved','RecountRequired','Rejected')","length(trim(reason))>0",
            "status <> 'Approved' OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)"])
t('stock_adjustments','Immutable signed correction, optionally originating from an approved stock count.',[
    ident(),c('request_key','uuid'),c('sku_id'),c('stock_count_id','integer',True),c('delta_qty'),
    c('reason','varchar(500)'),c('created_by'),now()],
    fks=[fk('sku_id','skus'),fk('stock_count_id','stock_counts'),fk('created_by','users')],
    checks=['delta_qty <> 0',"length(trim(reason))>0"],unique=['request_key'])
t('stock_movements','Immutable stock ledger, one row per source/SKU/action.',[
    ident(True),c('sku_id'),c('kind','varchar(16)'),c('delta_qty'),c('balance_after'),c('stock_version_after','bigint'),
    c('receipt_id','integer',True),c('sale_id','integer',True),c('adjustment_id','integer',True),c('actor_id'),now()],
    fks=[fk('sku_id','skus'),fk('receipt_id','purchase_receipts'),fk('sale_id','sales'),fk('adjustment_id','stock_adjustments'),fk('actor_id','users')],
    checks=['delta_qty <> 0 AND balance_after >= 0 AND stock_version_after > 0',
        "(kind IN ('Purchase','PurchaseCancel') AND receipt_id IS NOT NULL AND sale_id IS NULL AND adjustment_id IS NULL) OR (kind IN ('Sale','SaleCancel') AND sale_id IS NOT NULL AND receipt_id IS NULL AND adjustment_id IS NULL) OR (kind='Adjustment' AND adjustment_id IS NOT NULL AND sale_id IS NULL AND receipt_id IS NULL)",
        "(kind IN ('Purchase','SaleCancel') AND delta_qty>0) OR (kind IN ('Sale','PurchaseCancel') AND delta_qty<0) OR kind='Adjustment'"],
    unique=['sku_id,stock_version_after'])
t('restock_suggestions','Single active low-stock alert/suggestion per SKU and optional receipt-draft link.',[
    ident(),c('sku_id'),c('supplier_id','integer',True),c('observed_qty'),c('suggested_qty'),
    c('status','varchar(20)',default="'Open'"),c('receipt_id','integer',True),now(),now('updated_at'),version()],
    fks=[fk('sku_id','skus'),fk('supplier_id','suppliers'),fk('receipt_id','purchase_receipts')],
    checks=['observed_qty >= 0 AND suggested_qty >= 0',"status IN ('Open','ReceiptDrafted','Resolved')",
            "status <> 'ReceiptDrafted' OR receipt_id IS NOT NULL"])
t('outbox_messages','Durable post-commit event intent; API relay publishes to Redis.',[
    c('id','uuid'),c('kind','varchar(24)'),c('payload','jsonb'),now(),c('published_at','timestamptz',True),
    c('attempts','integer',default='0'),c('last_error','text',True)],
    checks=["kind IN ('InventoryChanged','BackupRequested','SummaryRequested')",'attempts >= 0'])
t('daily_summaries','One recomputable Vietnam business-date report, explicitly timestamped.',[
    ident(),c('business_date','date'),c('status','varchar(16)',default="'Queued'"),c('revenue_vnd','numeric(18,0)',default='0'),
    c('sold_bags','bigint',default='0'),c('sale_count','integer',default='0'),c('snapshot_at','timestamptz',True),
    c('last_error','text',True),now(),version()],
    checks=["status IN ('Queued','Processing','Completed','Failed')",'revenue_vnd >= 0 AND sold_bags >= 0 AND sale_count >= 0'],unique=['business_date'])
t('daily_sku_snapshots','Stock levels at report generation, not reconstructed day-end inventory.',[
    ident(True),c('summary_id'),c('sku_id'),c('stock_qty')],
    fks=[fk('summary_id','daily_summaries','CASCADE'),fk('sku_id','skus')],checks=['stock_qty >= 0'],unique=['summary_id,sku_id'])
t('backup_runs','Daily/manual pg_dump jobs; seven successful retained files across both kinds.',[
    c('id','uuid'),c('kind','varchar(12)'),c('scheduled_date','date',True),c('requested_by','integer',True),
    c('status','varchar(16)',default="'Queued'"),c('filename','varchar(200)',True),c('retained','boolean',default='false'),
    c('attempts','integer',default='0'),now(),c('completed_at','timestamptz',True),c('last_error','text',True)],
    fks=[fk('requested_by','users')],checks=["kind IN ('Daily','Manual')","status IN ('Queued','Processing','Completed','Failed')",
        "(kind='Daily' AND scheduled_date IS NOT NULL) OR (kind='Manual' AND scheduled_date IS NULL)",
        "NOT retained OR (status='Completed' AND filename IS NOT NULL AND completed_at IS NOT NULL)",'attempts >= 0'])

INDEXES=[
    'CREATE UNIQUE INDEX ux_users_username_fold ON users (lower(username));',
    'CREATE UNIQUE INDEX ux_supplier_code_fold ON suppliers (upper(code));',
    'CREATE UNIQUE INDEX ux_product_code_fold ON products (upper(code));',
    'CREATE UNIQUE INDEX ux_sku_code_fold ON skus (upper(code));',
    'CREATE INDEX ix_skus_product ON skus (product_id);',
    'CREATE INDEX ix_skus_low_stock ON skus (id) WHERE is_active AND stock_qty <= low_stock_threshold;',
    'CREATE UNIQUE INDEX ux_sales_confirmation ON sales (confirmation_key) WHERE confirmation_key IS NOT NULL;',
    'CREATE UNIQUE INDEX ux_sales_cancellation ON sales (cancellation_key) WHERE cancellation_key IS NOT NULL;',
    'CREATE UNIQUE INDEX ux_receipt_confirmation ON purchase_receipts (confirmation_key) WHERE confirmation_key IS NOT NULL;',
    'CREATE UNIQUE INDEX ux_receipt_cancellation ON purchase_receipts (cancellation_key) WHERE cancellation_key IS NOT NULL;',
    'CREATE UNIQUE INDEX ux_adjustment_count ON stock_adjustments (stock_count_id) WHERE stock_count_id IS NOT NULL;',
    'CREATE UNIQUE INDEX ux_movement_sale ON stock_movements (sale_id,sku_id,kind) WHERE sale_id IS NOT NULL;',
    'CREATE UNIQUE INDEX ux_movement_receipt ON stock_movements (receipt_id,sku_id,kind) WHERE receipt_id IS NOT NULL;',
    'CREATE UNIQUE INDEX ux_movement_adjustment ON stock_movements (adjustment_id) WHERE adjustment_id IS NOT NULL;',
    "CREATE UNIQUE INDEX ux_restock_active ON restock_suggestions (sku_id) WHERE status IN ('Open','ReceiptDrafted');",
    'CREATE INDEX ix_movements_sku_time ON stock_movements (sku_id,created_at DESC,id DESC);',
    'CREATE INDEX ix_outbox_pending ON outbox_messages (created_at) WHERE published_at IS NULL;',
    "CREATE INDEX ix_sales_report ON sales (confirmed_at) WHERE status='Confirmed';",
    'CREATE UNIQUE INDEX ux_backup_daily ON backup_runs (scheduled_date) WHERE scheduled_date IS NOT NULL;'
]

def generate():
    out=ROOT/'docs/database'; out.mkdir(parents=True,exist_ok=True)
    sql=['-- Potting Soil Shop — PostgreSQL 16+ DESIGN baseline for a fresh database.',
         '-- Generated from docs/tools/schema_model.py; review alongside EF migrations.',
         '-- No roles, secrets, seeds or database creation. Run with psql ON_ERROR_STOP.', 'BEGIN;','']
    dictionary=['# PostgreSQL Physical Data Dictionary','',
        'Generated from the schema model. Application implementation and EF migrations are pending. All non-CASCADE foreign keys restrict deletion. Prices are whole VND; Services reject fractional values before PostgreSQL numeric conversion.','']
    for name,tbl in TABLES.items():
        statements=[]
        dictionary += [f'## {name}', '',tbl['description'],'','| Column | PostgreSQL type | Nullable | Default / key |','|---|---|---|---|']
        for col in tbl['columns']:
            s=f"    {col['name']} {col['type']}"
            if col['identity']: s+=' GENERATED ALWAYS AS IDENTITY'
            if not col['nullable']: s+=' NOT NULL'
            if col['default'] is not None: s+=' DEFAULT '+col['default']
            if col['name']=='id': s+=' PRIMARY KEY'
            statements.append(s)
            flags='PK' if col['name']=='id' else str(col['default'] or '')
            if any(f[0]==col['name'] for f in tbl['foreign_keys']): flags+=' FK'
            dictionary.append(f"| {col['name']} | `{col['type']}` | {'Yes' if col['nullable'] else 'No'} | {flags} |")
        for i,u in enumerate(tbl['unique']): statements.append(f'    CONSTRAINT uq_{name}_{i+1} UNIQUE ({u})')
        for i,chk in enumerate(tbl['checks']): statements.append(f'    CONSTRAINT ck_{name}_{i+1} CHECK ({chk})')
        for i,(cc,parent,key,action) in enumerate(tbl['foreign_keys']):
            statements.append(f'    CONSTRAINT fk_{name}_{i+1} FOREIGN KEY ({cc}) REFERENCES {parent}({key}) ON DELETE {action}')
        ddl=f'CREATE TABLE {name} (\n'+',\n'.join(statements)+'\n);'
        sql += [ddl,'']
        dictionary+=['','Constraints / foreign keys:','','```sql','\n'.join(statements[len(tbl['columns']):]) or '-- Inline primary key only.','```','']
    sql+=INDEXES+['','COMMIT;','',
        '-- Services own document state transitions, nonempty lines, totals, stock-version increments,',
        '-- ledger balance correctness, reasoned count approval, and outbox insertion in one transaction.',
        '-- Append-only business records and published metadata sources require API/role enforcement.']
    (out/'schema.sql').write_text('\n'.join(sql)+'\n',encoding='utf-8')
    dictionary+=['## Additional indexes','','```sql',*INDEXES,'```','',
        '## Service invariants','',
        '- A confirmed document must have at least one line and known positive prices.',
        '- Confirmation/cancellation locks the document; stock updates, ledger, totals and outbox commit atomically.',
        '- StockCount approval compares expected_stock_version; stock_adjustments.stock_count_id is unique.',
        '- Recorded source and curator are required when publishing metadata for advisory use.',
        '- References in JSON outbox payloads are operation IDs, not polymorphic SQL foreign keys.',
        '- Only one active RestockSuggestion per SKU; receipt-draft conversion is idempotent.',
        '- Master-data delete requires no historical references; prefer is_active=false.']
    (out/'data-dictionary.md').write_text('\n'.join(dictionary)+'\n',encoding='utf-8')
    (ROOT/'docs/diagrams/schema-metadata.json').write_text(json.dumps(TABLES,indent=2)+'\n',encoding='utf-8')
    print(f'Generated PostgreSQL schema and dictionary: {len(TABLES)} tables.')

if __name__=='__main__': generate()
