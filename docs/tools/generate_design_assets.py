"""Render editable diagram-design HTML from approved shop model; no application code."""
from pathlib import Path
import json
from diagram_primitives import OUT, ROOT, P, esc, text, rect, label, edge, node, save, DIAGRAMS
from schema_model import TABLES, generate

generate()

def box(x,y,w,h,title,kind,lines=(),focal=False,external=False,store=False):
    return node(x,y,w,h,title,kind,lines,focal,external,store)

# C1 includes real offline business actors, never fake external software integrations.
b=edge([(344,368),(576,368)],'OPERATIONS',(460,344))
b+=edge([(800,704),(800,544)],'ADMIN',(900,620))
b+=edge([(1200,208),(1088,208),(1088,120),(208,120),(208,280)],'SUPPLY INFO',(704,96),dashed=True)
b+=edge([(1200,600),(1120,600),(1120,864),(208,864),(208,464)],'COUNTER SALE',(736,840),dashed=True)
b+=box(64,280,280,184,'Employee','Person',[
    'Receive goods and record sales','Ask for explained product advice','Count stock; review replenishment','No price override or Admin approval'])
b+=box(576,264,448,280,'Potting Soil Shop','Software system',[
    'One internal shop / one stock location','Catalogue + source-backed product advice','Receipts / sales with frozen price snapshots',
    'Safe stock with traceable inventory changes','Replenishment and count approval','Scheduled reports + daily/manual backup'],True)
b+=box(576,704,448,120,'Admin / Owner','Person',[
    'Accounts, prices, metadata, cancellations','Count approval, adjustments and backups'])
b+=box(1200,144,320,160,'Supplier','External person',[
    'Delivers pre-packed bags','Provides product-label information','Offline channels; no system login'],external=True)
b+=box(1200,520,320,160,'Walk-in Customer','External person',[
    'Describes needs to staff','Purchases at the counter','No storefront or customer account'],external=True)
save('c4-l1-context','C1 — System Context','Employee and Admin use the shop system; Supplier and Customer interact with staff offline.',b,
     'Business context only. Databases, brokers and gRPC are internal containers and appear at Level 2.',
     'Solid = application access    Dashed = offline human interaction    No supplier/payment/AI integration',1600,960)

# C2 overview: nine nodes; protocol arrows and responsibility text enrich real boundaries.
b=rect(304,104,1440,920,stroke='rule',dash=True)+text(328,126,'POTTING SOIL SHOP — APPLICATION BOUNDARY',9,'soft',mono=True)
b+=edge([(248,336),(288,336),(288,392),(336,392)],'STAFF',(288,312))
b+=edge([(248,616),(272,616),(272,472),(336,472)],'ADMIN',(312,640))
b+=edge([(584,432),(720,432)],'REST / JWT',(652,408))
b+=edge([(848,344),(848,280)],'GRPC',(944,312))
b+=edge([(816,576),(816,768)],'EF / NPGSQL',(912,672))
b+=edge([(1040,392),(1136,392),(1136,208),(1248,208)],'XADD',(1152,184))
b+=edge([(1392,344),(1392,280)],'READ / ACK',(1488,312))
b+=edge([(1248,500),(1152,500),(1152,840),(1040,840)],'EF / PGDUMP',(1224,680))
b+=edge([(1408,576),(1408,768)],'ARCHIVE I/O',(1528,672))
b+=box(40,264,208,144,'Employee','Person',['Sales / receipts','Advice / counts'])
b+=box(40,544,208,144,'Admin','Person',['Approvals / pricing','Accounts / backups'])
b+=box(336,344,248,184,'React Web','Container',['React + Vite + TypeScript','Nginx /api proxy','Forms, advice and history','Polling reports and job results'])
b+=box(720,344,320,232,'REST API','Container',[
    'ASP.NET Core + EF / AutoMapper','JWT and role enforcement','Service-owned business transitions',
    'Atomic stock / ledger / outbox','Quote and advice orchestration','Hosted outbox relay = producer'],True)
b+=box(720,136,320,144,'Advisory & Calculation','Container',['.NET gRPC / Protobuf / HTTP2','CalculateTotals + RecommendProducts','Stateless rules; no DB access'])
b+=box(1248,136,304,144,'Redis Streams','Container',['Consumer groups + pending ACK','AOF / persistent volume','At-least-once event delivery'])
b+=box(1248,344,304,232,'Background Worker','Container',[
    '.NET Worker / consumer','Low-stock recheck + suggestions','Report generation and refresh',
    'Serial pg_dump + file retention','Schedules + retry / reconciliation','No sale stock deductions'])
b+=box(720,768,320,184,'PostgreSQL','Database',[
    'Catalogue / slips / stock versions','Ledger, counts and replenishment','Outbox / summaries / backup runs'],True,store=True)
b+=box(1248,768,304,184,'Backup Archive Volume','File store',[
    'Worker writes .dump archives','7 successful files total','Daily + Admin manual trigger'],store=True)
save('c4-l2-containers','C2 — Complete Container View','Two internal roles access Web/API; API calls stateless gRPC, commits PostgreSQL transactions and publishes durable outbox events to Redis for Worker follow-up.',b,
     'Six executing services plus one file store. Direct responses are implicit; focused C2 views explain synchronous and async paths.',
     'Arrows = connection initiator or file writer    Dashed box = system boundary    No extra component-level boxes',1800,1120)

b=edge([(304,424),(400,424)],'STAFF INTENT',(352,400))
b+=edge([(640,424),(800,424)],'REST / JWT',(720,400))
b+=edge([(960,344),(960,280)],'GRPC / HTTP2',(1072,312))
b+=edge([(928,560),(928,704)],'TRANSACTION',(1040,632))
b+=box(64,344,240,160,'Employee / Admin','People',['Select need or SKU quantities','Review quote / confirmation','Handle 409 conflict feedback'])
b+=box(400,344,240,160,'React Web','Container',['Advice reasons and source','Sale / receipt draft forms','Stock-count approval UI'])
b+=box(800,344,320,216,'REST API','Container',[
    '1. Load trusted prices / metadata','2. Call RPC outside stock locks','3. Lock document + ordered SKUs','4. Recheck price / guarded stock','5. Commit slip + ledger + outbox'],True)
b+=box(800,136,320,144,'Advisory & Calculation','Container',['Calculate whole-VND totals','Rank positive metadata matches','Return reasons + rule version'])
b+=box(800,704,320,144,'PostgreSQL','Database',['Conditional stock update + versions','All lines succeed or all roll back'],store=True)
b+=text(1200,384,'LAST BAG',14,'accent',600)+text(1200,412,'Two confirmations, one bag:',12)+text(1200,436,'one commit, one 409 conflict.',12)
b+=text(64,720,'NO RESERVATION IN DRAFT',14,'accent',600)+text(64,748,'A repeated action key returns the recorded result.',12)+text(64,772,'Advice availability is rechecked when the sale commits.',12)
save('c4-l2-synchronous','C2 — Advice and Synchronous Stock','A filtered Level 2 perspective shows direct Web/API/gRPC/PostgreSQL requests and where atomic stock correctness is enforced.',b,
     'Same containers as overview; workflow notes are responsibilities, not C4 components. No Redis dependency for sale acceptance.',
     'API → gRPC returns totals/reasons    API → PostgreSQL commits authoritative business state',1600,960)

b=edge([(344,424),(480,424)],'XADD',(412,400))
b+=edge([(928,424),(760,424)],'READ / ACK',(844,400))
b+=edge([(344,500),(400,500),(400,752),(928,752)],'DURABLE INTENT',(648,728))
b+=edge([(1024,544),(1024,680)],'EF / PGDUMP',(1136,616))
b+=edge([(1088,344),(1088,272)],'FILE WRITE',(1200,308))
b+=box(64,344,280,200,'REST API','Container',[
    'Business TX writes outbox row','Hosted relay publishes XADD','Mark published only after send','Manual summary / backup request'],True)
b+=box(480,344,280,200,'Redis Streams','Container',[
    'InventoryChanged / job requests','Consumer group and pending list','Reclaim after crash; ACK last','No business stock authority'])
b+=box(928,344,304,200,'Background Worker','Container',[
    'Read current stock and thresholds','Upsert one active suggestion','Daily/manual reports and backups','Idempotent handling + reconciliation'],True)
b+=box(928,680,304,144,'PostgreSQL','Database',['Outbox / suggestion and job state','Consistent report snapshot'],store=True)
b+=box(928,128,304,144,'Backup Archive Volume','File store',['pg_dump custom format','Retain seven successful archives'],store=True)
b+=text(64,176,'ASYNCHRONOUS FOLLOW-UP',14,'accent',600)+text(64,204,'Redis delay does not undo or delay a completed sale.',12)+text(64,228,'Duplicate delivery must not duplicate suggestions or jobs.',12)
save('c4-l2-asynchronous','C2 — Async Replenishment and Backups','A filtered Level 2 perspective shows API outbox publishing, Redis consumption, persistent job state and Worker-owned backup archives.',b,
     'Daily schedules also run inside Worker. Worker writes dump files; PostgreSQL is queried over its normal connection.',
     'XADD = API producer    READ / ACK = Worker consumer    DB intent and consumer results survive process restarts',1600,960)

# Conceptual trading view: 8 nodes / 9 relationships, with line entities explicit.
def ent(x,y,name,lines=(),focal=False): return box(x,y,240,112,name,'Entity',lines,focal)
b=edge([(304,200),(400,200)],'1 / 0..N',(352,178),arrow=False)
b+=edge([(640,200),(736,200)],'1 / 0..N',(688,178),arrow=False)
b+=edge([(976,200),(1072,200)],'N / 1',(1024,178),arrow=False)
b+=edge([(520,256),(520,432)],'1 / 0..N',(584,344),arrow=False)
b+=edge([(640,488),(736,488)],'N / 1',(688,466),arrow=False)
b+=edge([(1192,256),(1192,432)],'N / 1',(1256,344),arrow=False)
b+=ent(64,144,'Product',['Source-backed soil metadata'])
b+=ent(400,144,'SKU',['Package, price and bag stock'],True)
b+=ent(736,144,'Receipt Line',['Actual cost and bag quantity'])
b+=ent(1072,144,'Purchase Receipt',['Draft / Confirmed / Cancelled'])
b+=ent(400,432,'Sale Line',['Frozen price and quantity'])
b+=ent(736,432,'Sales Slip',['Staff records counter sale'])
b+=ent(1072,432,'Supplier',['Offline goods / label source'])
save('erd-trading','Conceptual ERD — Catalogue and Trading','Products have SKUs; purchase and sale lines reference SKUs; receipts belong to Suppliers.',b,
     'Read each label from first named/source endpoint to second. Drafts may have zero lines; confirmed slips require at least one.',
     'Product → SKU    SKU → Receipt/Sale lines    Lines → document    Supplier → receipts',1440,720)

# Conceptual inventory view; identity links to actor/master data are tabulated in the document.
b=edge([(304,200),(400,200)],'1 / 0..N',(352,178),arrow=False)
b+=edge([(640,200),(736,200)],'1 / 0..1',(688,178),arrow=False)
b+=edge([(976,200),(1072,200)],'1 / 1',(1024,178),arrow=False)
b+=edge([(184,256),(184,432)],'1 / 0..N',(248,344),arrow=False)
b+=edge([(304,488),(400,488)],'1 / 0..1',(352,466),arrow=False)
b+=edge([(184,144),(184,112),(1192,112),(1192,144)],'1 / 0..N',(688,90),arrow=False)
b+=ent(64,144,'SKU',['Current stock + stock version'],True)
b+=ent(400,144,'Stock Count',['Expected vs actual; review'])
b+=ent(736,144,'Stock Adjustment',['Approved discrepancy / damage'])
b+=ent(1072,144,'Stock Movement',['Immutable signed stock audit'])
b+=ent(64,432,'Restock Suggestion',['One active item per SKU'])
b+=ent(400,432,'Receipt Draft',['Conversion does not add stock'])
b+=ent(880,432,'Plant / Use Tag',['Source-backed product labels'])
b+=ent(1200,432,'Product',['Optional metadata'])
b+=edge([(1120,488),(1200,488)],'N / M',(1160,466),arrow=False)
save('erd-operations','Conceptual ERD — Advice and Stock Control','A SKU has physical counts and replenishment suggestions; accepted discrepancies become adjustments and ledger entries; products carry use tags.',b,
     'Count may yield zero or one adjustment. Each adjustment has one ledger entry; other movements originate from receipts/sales.',
     'Actor, Supplier and SKU references are detailed in the relationship table and physical model.',1520,720)

def table(x,y,name,fields):
    cols={c['name']:c for c in TABLES[name]['columns']}
    omitted=len(cols)-len(fields)
    h=48+24*len(fields)+(32 if omitted else 8)
    result=rect(x,y,336,h)+rect(x,y,336,48,'paper-2',radius=0)
    result+=text(x+12,y+18,'TABLE',8,'soft',mono=True)+text(x+12,y+38,name,14,weight=600)
    for i,f in enumerate(fields):
        c=cols[f]; yy=y+48+24*i
        if i%2==0: result+=f'<rect x="{x+1}" y="{yy}" width="334" height="24" fill="{P["paper-2"]}"/>'
        result+=text(x+12,yy+16,f,12,weight=600)+text(x+324,yy+16,c['type']+(' ?' if c['nullable'] else ''),9,'muted',anchor='end',mono=True)
        if f=='id': result+=text(x+208,yy+16,'PK',8,'soft',mono=True)
        elif any(link[0]==f for link in TABLES[name]['foreign_keys']): result+=text(x+208,yy+16,'FK',8,'soft',mono=True)
    if omitted: result+=text(x+12,y+48+24*len(fields)+20,f'+ {omitted} more columns — see dictionary',9,'muted',mono=True)
    return result

def physical(slug,title,description,tables,edges,caption,w=1440,h=720):
    b=''.join(edge(points,action,at) for points,at,action in edges)
    b+=''.join(table(*t) for t in tables)
    save(slug,title,description,b,caption,'FK: child column → parent key    RESTRICT preserves referenced history    ? = nullable',w,h)

physical('db-catalogue','Physical DB — Products and SKU Stock','SKUs reference Products and an optional preferred Supplier.',[
    (40,144,'products',['id','code','name','product_type','source_reference','reviewed_by','drainage']),
    (552,144,'skus',['id','product_id','preferred_supplier_id','code','stock_qty','stock_version','sale_price_vnd']),
    (1064,144,'suppliers',['id','code','name','is_active'])],[
    ([(552,228),(464,228),(464,204),(376,204)],(464,180),'RESTRICT'),
    ([(888,252),(976,252),(976,204),(1064,204)],(976,180),'RESTRICT')],
    'SKU stock/version are Service-controlled. Products.reviewed_by also references users; see full dictionary.')

physical('db-purchases','Physical DB — Purchase Receipts','Lines reference Receipt and SKU; historical receipt headers reference Supplier and staff.',[
    (40,144,'purchase_receipts',['id','supplier_id','status','confirmed_at','total_vnd','confirmation_key']),
    (552,144,'purchase_receipt_lines',['id','receipt_id','sku_id','quantity','unit_cost_vnd']),
    (1064,144,'skus',['id','code','stock_qty','stock_version'])],[
    ([(552,228),(464,228),(464,204),(376,204)],(464,180),'CASCADE'),
    ([(888,252),(976,252),(976,204),(1064,204)],(976,180),'RESTRICT')],
    'CASCADE is used only for eligible draft deletion. Confirmed header/lines are immutable through Services.')

physical('db-sales','Physical DB — Sale Snapshots','Sale lines reference a Sales document and SKU; confirmed prices remain frozen.',[
    (40,144,'sales',['id','status','created_by','confirmation_key','total_vnd','cancellation_key','cancel_reason']),
    (552,144,'sale_lines',['id','sale_id','sku_id','quantity','unit_price_vnd','sku_label_snapshot']),
    (1064,144,'skus',['id','code','sale_price_vnd','stock_qty'])],[
    ([(552,228),(464,228),(464,204),(376,204)],(464,180),'CASCADE'),
    ([(888,252),(976,252),(976,204),(1064,204)],(976,180),'RESTRICT')],
    'Action-key unique indexes + locked document state prevent repeated deductions and cancellations.')

physical('db-inventory','Physical DB — Stocktaking and Audit','Counts and adjustments reference SKU; an approved nonzero count creates at most one adjustment.',[
    (40,144,'skus',['id','stock_qty','stock_version']),
    (552,144,'stock_counts',['id','sku_id','expected_qty','expected_stock_version','actual_qty','status','reviewed_by']),
    (1064,144,'stock_adjustments',['id','sku_id','stock_count_id','delta_qty','request_key','created_by']),
    (1064,448,'stock_movements',['id','adjustment_id','sku_id','kind','delta_qty','balance_after','stock_version_after'])],[
    ([(552,228),(464,228),(464,204),(376,204)],(464,180),'RESTRICT'),
    ([(1064,252),(976,252),(976,204),(888,204)],(976,180),'RESTRICT'),
    ([(1400,532),(1432,532),(1432,204),(1400,204)],(1368,404),'RESTRICT')],
    'Adjustment SKU/actor and ledger SKU/document/actor links are in SQL. Unique stock_count_id prevents duplicate approval.',1480,800)

physical('db-replenishment','Physical DB — Replenishment','Suggestion references SKU, optional Supplier and a linked receipt draft.',[
    (40,144,'skus',['id','stock_qty','low_stock_threshold','target_stock','preferred_supplier_id']),
    (552,144,'restock_suggestions',['id','sku_id','receipt_id','supplier_id','observed_qty','suggested_qty','status']),
    (1064,144,'purchase_receipts',['id','supplier_id','status'])],[
    ([(552,228),(464,228),(464,204),(376,204)],(464,180),'RESTRICT'),
    ([(888,252),(976,252),(976,204),(1064,204)],(976,180),'RESTRICT')],
    'A partial unique index allows only one Open/ReceiptDrafted suggestion per SKU; conversion adds no stock.')

physical('db-tags','Physical DB — Metadata Tags and Review','Product/tag associations support structured advisory metadata; source review belongs to staff.',[
    (40,144,'products',['id','source_reference','reviewed_by','reviewed_at','ph_min','ph_max']),
    (552,144,'product_plant_tags',['id','product_id','plant_tag_id']),
    (1064,144,'plant_tags',['id','code','name']),
    (40,448,'users',['id','username','role','is_active'])],[
    ([(552,228),(464,228),(464,204),(376,204)],(464,180),'CASCADE'),
    ([(888,252),(976,252),(976,204),(1064,204)],(976,180),'RESTRICT'),
    ([(40,252),(16,252),(16,508),(40,508)],(96,404),'RESTRICT')],
    'Metadata can be unknown. Users review supplied labels; a review is not independent lab certification.')

physical('db-background','Physical DB — Reports, Outbox and Backup','Timestamped summaries reference per-SKU snapshots; outbox/job state supports async handling.',[
    (40,144,'outbox_messages',['id','kind','payload','published_at','attempts']),
    (552,144,'daily_summaries',['id','business_date','status','revenue_vnd','sold_bags','snapshot_at']),
    (1064,144,'daily_sku_snapshots',['id','summary_id','sku_id','stock_qty']),
    (40,448,'backup_runs',['id','kind','scheduled_date','requested_by','status','filename','retained'])],[
    ([(1064,228),(976,228),(976,204),(888,204)],(976,180),'CASCADE')],
    'Snapshot SKU and BackupRun requester links are in SQL. Outbox JSON operation references are not SQL foreign keys.',1440,800)

gallery=''.join(f'<article><h2><a href="{s}.html">{esc(t)}</a></h2><p>{esc(d)}</p><a href="{s}.html"><img src="images/{s}.png" alt="{esc(t)}" loading="lazy"></a><p><a href="images/{s}.svg">SVG</a> · <a href="images/{s}.png">PNG</a> · {w}×{h} vector canvas</p></article>' for s,t,d,w,h in DIAGRAMS)
(OUT/'index.html').write_text('<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Potting Soil Shop — Design Gallery</title><style>body{font-family:Segoe UI,Arial,sans-serif;color:#142b49;margin:32px}main{max-width:1600px;margin:auto}a{color:#175e9b}article{border-top:1px solid #d5dfea;padding:24px 0}img{max-width:100%;border:1px solid #d5dfea}p{line-height:1.6}</style></head><body><main><h1>Potting Soil Shop — Design Gallery</h1><p>C1 context, C2 overview plus synchronous/async perspectives; conceptual ERDs and physical subsystems. No C3/C4 diagrams.</p>'+gallery+'</main></body></html>',encoding='utf-8')
(OUT/'manifest.json').write_text(json.dumps([dict(slug=s,title=t,description=d,width=w,height=h) for s,t,d,w,h in DIAGRAMS],indent=2)+'\n',encoding='utf-8')
print(f'Generated {len(DIAGRAMS)} self-contained HTML diagram sources.')
