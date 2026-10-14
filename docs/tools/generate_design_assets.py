"""Regenerate diagram HTML and the SQL-derived data dictionary; no application code."""
from pathlib import Path
import html
import re
import json
import math

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs' / 'diagrams'
OUT.mkdir(parents=True, exist_ok=True)
P = {'paper':'#FFFFFF','paper-2':'#F1F5F9','ink':'#142B49','muted':'#48617C',
     'soft':'#48617C','rule':'#D5DFEA','rule-solid':'#48617C','accent':'#007C91',
     'accent-tint':'#E8F6FA','link':'#175E9B'}

def esc(s):
    return html.escape(str(s), quote=True)

def text(x,y,s,size=12,color='ink',weight=400,anchor='start',mono=False):
    family = 'Consolas, monospace' if mono else 'Segoe UI, Arial, sans-serif'
    return f'<text x="{x}" y="{y}" font-size="{size}" fill="{P[color]}" font-weight="{weight}" text-anchor="{anchor}" font-family="{family}">{esc(s)}</text>'

def rect(x,y,w,h,fill='paper',stroke='rule-solid',radius=6,dash=False):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{P[fill]}" stroke="{P[stroke]}" stroke-width="1.2"'+(' stroke-dasharray="6 4"' if dash else '')+'/>'

def label(x,y,s):
    # Labels sit on opaque masks, at least 8px clear of connector strokes.
    w = max(40,len(s)*6.3+16)
    return f'<rect x="{x-w/2}" y="{y-12}" width="{w}" height="20" fill="{P["paper"]}"/>'+text(x,y+2,s,9,'muted',anchor='middle',mono=True)

def rounded(points,r=8):
    d=f'M {points[0][0]},{points[0][1]}'
    for i in range(1,len(points)-1):
        a,b,c=points[i-1],points[i],points[i+1]
        if (a[0]==b[0]==c[0]) or (a[1]==b[1]==c[1]):
            d+=f' L {b[0]},{b[1]}'
            continue
        before=(b[0]+math.copysign(r,a[0]-b[0]) if a[0]!=b[0] else b[0],
                b[1]+math.copysign(r,a[1]-b[1]) if a[1]!=b[1] else b[1])
        after=(b[0]+math.copysign(r,c[0]-b[0]) if c[0]!=b[0] else b[0],
               b[1]+math.copysign(r,c[1]-b[1]) if c[1]!=b[1] else b[1])
        d+=f' L {before[0]},{before[1]} Q {b[0]},{b[1]} {after[0]},{after[1]}'
    return d+f' L {points[-1][0]},{points[-1][1]}'

def edge(points,lab=None,at=None,arrow=True,dashed=False,accent=False):
    color=P['accent' if accent else 'muted']
    a=f'<path d="{rounded(points)}" fill="none" stroke="{color}" stroke-width="1.5"'
    if arrow: a+=' marker-end="url(#arrow)"'
    if dashed: a+=' stroke-dasharray="6 4"'
    return a+'/>'+ (label(*at,lab) if lab else '')

def node(x,y,w,h,name,tag,lines=(),focal=False,external=False,store=False):
    body=rect(x,y,w,h,'accent-tint' if focal else 'paper','accent' if focal else 'rule-solid',8,dash=external)
    if store:
        body+=f'<path d="M{x},{y+16} Q{x+w/2},{y+32} {x+w},{y+16}" stroke="{P["muted"]}" fill="none"/>'
    body+=text(x+16,y+36 if store else y+24,tag.upper(),9,'soft',mono=True)
    body+=text(x+16,y+56 if store else y+48,name,16,weight=600)
    for i,s in enumerate(lines): body+=text(x+16,y+(76 if store else 68)+i*16,s,11,'muted')
    return body

DIAGRAMS=[]
def save(slug,title,desc,body,caption,legend='Solid: connection / relationship    Dashed: offline or system boundary'):
    w,h=1280,720
    svg=(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" role="img" aria-labelledby="{slug}-title {slug}-desc">'
         f'<title id="{slug}-title">{esc(title)}</title><desc id="{slug}-desc">{esc(desc)}</desc>'
         f'<defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M 0 1 L 9 5 L 0 9 Z" fill="{P["muted"]}"/></marker></defs>'
         f'<rect width="1280" height="720" fill="{P["paper"]}"/>'
         +text(40,40,title,28,weight=600)+text(40,64,'BROKER PROPERTY CATALOGUE / PRN232 / DESIGN',9,'soft',mono=True)
         +body+f'<line x1="40" y1="656" x2="1240" y2="656" stroke="{P["rule"]}"/>'
         +text(40,680,legend,11,'muted')+text(40,704,caption,11,'muted')+'</svg>')
    document=(f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{esc(title)}</title>'
              '<style>body{margin:0;background:#fff;color:#142B49;font-family:Segoe UI,Arial,sans-serif}main{max-width:1320px;margin:24px auto;padding:16px;min-width:0}.diagram-container{width:100%;overflow-x:auto}svg{display:block;width:1280px;min-width:1280px}p{line-height:1.6}a{color:#175E9B}@media print{.diagram-container{overflow-x:visible}svg{width:100%;min-width:0}}</style></head>'
              f'<body><main><div class="diagram-container">{svg}</div><p>{esc(desc)}</p><p><a href="index.html">All diagrams</a> · <a href="images/{slug}.svg">SVG</a> · <a href="images/{slug}.png">PNG</a></p></main></body></html>')
    (OUT/f'{slug}.html').write_text(document,encoding='utf-8')
    DIAGRAMS.append((slug,title,desc))

# C4 L1: offline developer information is represented as a person relationship.
b=edge([(352,248),(440,248),(440,280),(536,280)],'MAINTAINS',(440,230))
b+=edge([(352,472),(456,472),(456,364),(536,364)],'ADMINISTERS',(400,400))
b+=edge([(1008,272),(920,272),(920,200),(352,200)],'OFFLINE DATA',(752,182),dashed=True)
b+=node(112,160,240,128,'Employee','Person',['Inventory, prices and status'])
b+=node(112,400,240,128,'Admin','Person',['Accounts and backup requests'])
b+=node(536,216,320,204,'Broker Property Catalogue','Software system',['Internal inventory for one broker','Bulk creation and missing details','Status history, summaries, backups'],focal=True)
b+=node(1008,224,224,144,'Property Developer','External person',['Allocations and details','No login or API integration'],external=True)
save('c4-l1-context','C4 Level 1 — System Context','Employees and Admin use the internal catalogue; developers supply information offline to employees.',b,'Developers are catalogue stakeholders, not connected software systems.')

# C4 L2: one combined staff node keeps the container view focused on deployments.
b=rect(224,88,832,544,stroke='rule',dash=True)+text(240,108,'BROKER PROPERTY CATALOGUE — CONTAINERS',9,'soft',mono=True)
b+=edge([(200,348),(240,348)],'WEB',(220,326))
b+=edge([(408,348),(464,348)],'REST/JSON',(436,326))
b+=edge([(560,296),(560,232)],'GRPC/HTTP2',(628,264))
b+=edge([(528,416),(528,508)],'EF / TDS',(576,464))
b+=edge([(890,296),(890,232)],'STREAMS',(956,264))
b+=edge([(800,380),(724,380),(724,552),(660,552)],'EF / TDS',(776,466))
b+=edge([(660,584),(800,584)],'BACKUP',(740,612))
b+=edge([(920,416),(920,508)],'PRUNE FILES',(984,464))
b+=node(40,296,160,120,'Staff','People',['Admin + Employee'])
b+=node(240,296,168,120,'React Web','Container',['React / Nginx','Forms and polling'])
b+=node(464,296,196,120,'REST API','Container',['ASP.NET Core','JWT / CRUD / outbox'],focal=True)
b+=node(464,120,196,112,'Numbering Service','Container',['.NET gRPC','Stateless previews'])
b+=node(800,296,180,120,'Worker','Container',['.NET BackgroundService','Relay + job handlers'])
b+=node(800,120,180,112,'Redis Streams','Container',['XADD / XREADGROUP','Async work delivery'])
b+=node(464,508,196,120,'SQL Server','Database',['EF Core + job state'],focal=True,store=True)
b+=node(800,508,180,120,'Backup Files','File store',['Named volume / .bak'],store=True)
save('c4-l2-containers','C4 Level 2 — Containers','React calls the REST API, the API calls gRPC and SQL, and the Worker relays and consumes Redis work while SQL writes backup files.',b,'Worker produces + consumes streams. API records requests in SQL outbox; no direct API–Worker call.','Arrows: initiator of connection / file write    Boundary: application scope    Staff = Admin + Employee')

# Conceptual model: entity relationships, no operational tables or SQL types.
def entity(x,y,name,lines,focal=False):
    return node(x,y,240,108,name,'Entity',lines,focal=focal)
b=edge([(304,176),(400,176)],'1 / 0..N',(352,154),arrow=False)
b+=edge([(640,176),(832,176)],'1 / 0..N',(736,154),arrow=False)
b+=edge([(512,232),(512,348)],'1 / 0..N',(576,292),arrow=False)
b+=edge([(832,208),(744,208),(744,388),(640,388)],'0..1 / 0..N',(816,300),arrow=False)
b+=edge([(400,396),(304,396)],'1 / 0..N',(352,374),arrow=False)
b+=edge([(640,428),(832,428)],'1 / 0..N',(736,408),arrow=False)
b+=edge([(952,540),(952,456)],'1 / 0..N',(1020,500),arrow=False)
b+=entity(64,124,'Developer',['Supplies inventory'])
b+=entity(400,124,'Project',['Owned by one Developer'])
b+=entity(832,124,'Building',['In one Project; floor count'])
b+=entity(400,348,'Property Unit',['Apartment / Townhouse / Villa','Optional details and price'],True)
b+=entity(64,348,'Additional Information',['Editable name–value pairs'])
b+=entity(832,348,'Status Change',['From / to / reason / time'])
b+=entity(832,540,'Staff Account',['Actor who changes status'])
save('erd-conceptual','Conceptual ERD — Business Catalogue','Developers own Projects; Units belong to Projects, Apartments also belong to Buildings, and Units have additional information and status changes.',b,'Building is required only for Apartments; its Project must equal the Unit’s Project.','Read 1 / 0..N as parent / children. Building edge: each Unit has 0..1 Building; each Building has 0..N Units.')

# Derive column inventory directly from baseline SQL.
sql=(ROOT/'docs/database/schema.sql').read_text(encoding='utf-8')
TABLES={}
for match in re.finditer(r'CREATE TABLE dbo\.(\w+) \(\n(.*?)\n\);',sql,re.S):
    name,body=match.groups()
    cols=[]
    for line in body.splitlines():
        m=re.match(r'    (\w+) ((?:nvarchar|varchar|decimal|datetime2)\([^)]*\)|\w+) (.*)',line)
        if m and m[1]!='CONSTRAINT':
            col,typ,rest=m.groups()
            cols.append({'name':col,'type':typ,'nullable':not bool(re.search(r'\bNOT NULL\b',rest)),'pk':'PRIMARY KEY' in rest,'definition':line.strip().rstrip(',')})
    TABLES[name]={'columns':cols,'definition':body}
assert len(TABLES)==12, f'Expected 12 tables, got {len(TABLES)}'
dictionary=['# Physical Database Data Dictionary','','SQL Server baseline: `BrokerCatalogue`. Generated from [schema.sql](schema.sql). All foreign keys use NO ACTION unless explicitly marked CASCADE. Operational EventId/OperationId references are enforced by handlers, not polymorphic SQL foreign keys.','','Unknown descriptive values use NULL. UTC timestamps use datetime2(3); rowversion is a concurrency token, not a date. The Services reject fractional VND before SQL conversion.','']
for name,t in TABLES.items():
    dictionary+=['## '+name,'','| Column | SQL type | Nullable | Definition / inline constraint |','|---|---|---|---|']
    for c in t['columns']:
        dictionary.append(f'| {c["name"]} | `{c["type"]}` | {"Yes" if c["nullable"] else "No"} | `{c["definition"]}` |')
    dictionary+=['','Table-level constraints:','','```sql',t['definition'],'```','']
dictionary+=['## Indexes','','```sql',sql[sql.index('CREATE INDEX IX_Projects'):sql.index('-- Service invariants')].replace('\nGO\n','\n'),'```','','## Service-level invariants','',sql[sql.index('-- Service invariants'):].replace('-- ','- ')]
(ROOT/'docs/database/data-dictionary.md').write_text('\n'.join(dictionary),encoding='utf-8')
(OUT/'schema-metadata.json').write_text(json.dumps(TABLES,indent=2),encoding='utf-8')

def table(x,y,name,fields,focal=False):
    cols={c['name']:c for c in TABLES[name]['columns']}
    omitted=len(cols)-len(fields)
    height=48+24*len(fields)+(32 if omitted else 8)
    result=rect(x,y,320,height)
    result+=rect(x,y,320,48,'accent-tint' if focal else 'paper-2','accent' if focal else 'rule-solid',0)
    result+=text(x+12,y+18,'TABLE',8,'soft',mono=True)+text(x+12,y+38,'dbo.'+name,14,weight=600)
    for i,f in enumerate(fields):
        c=cols[f]; yy=y+48+24*i
        if i%2==0: result+=f'<rect x="{x+1}" y="{yy}" width="318" height="24" fill="{P["paper-2"]}"/>'
        result+=text(x+12,yy+16,f,12,weight=600)+text(x+308,yy+16,c['type']+(' ?' if c['nullable'] else ''),9,'muted',anchor='end',mono=True)
        if c['pk']: result+=text(x+164,yy+16,'PK',8,'soft',mono=True)
    if omitted: result+=text(x+12,y+48+24*len(fields)+20,f'+ {omitted} more columns — see dictionary',9,'muted',mono=True)
    return result

def db_edge(points,at,action='NO ACTION'):
    return edge(points,action,at,accent=action=='CASCADE')

def physical(slug,title,desc,edges,tables,caption):
    b=''.join(db_edge(*e) for e in edges)
    b+=''.join(table(*t) for t in tables)
    save(slug,title,desc,b,caption,'FK arrows: child column → parent key    NO ACTION = restrict deletion    ? = nullable; all columns in dictionary')

physical('db-catalogue','Physical DB — Master Catalogue','Projects reference Developers and Buildings reference Projects; deletion is restricted.',[
    ([(480,204),(432,204),(432,180),(384,180)],(432,162)),
    ([(896,204),(848,204),(848,180),(800,180)],(848,162))], [
    (64,120,'Developers',['Id','Code','Name','ContactName','Email','Phone']),
    (480,120,'Projects',['Id','DeveloperId','Code','Name','Address']),
    (896,120,'Buildings',['Id','ProjectId','Code','Name','FloorCount','Classification'])],
    'Unique codes: Developer and Project globally; Building within Project. Building also exposes unique (ProjectId, Id).')

physical('db-unit-location','Physical DB — Unit Location','Units reference Projects and Buildings with a same-Project composite constraint.',[
    ([(480,204),(432,204),(432,180),(384,180)],(432,162)),
    ([(800,228),(848,228),(848,180),(896,180)],(848,162))], [
    (64,120,'Projects',['Id','Code','Name']),
    (480,120,'Units',['Id','ProjectId','BuildingId','Code','UnitType','FloorNumber','PriceVnd']),
    (896,120,'Buildings',['Id','ProjectId','Code','FloorCount'])],
    'Building link is composite: Units(ProjectId, BuildingId) → Buildings(ProjectId, Id). Only Apartments have a Building.')

physical('db-unit-audit','Physical DB — Details and Status History','UnitAttributes cascade on eligible Unit deletion; immutable history restricts Unit and actor deletion.',[
    ([(384,204),(432,204),(432,172),(480,172)],(432,150),'CASCADE'),
    ([(896,204),(848,204),(848,188),(800,188)],(848,164)),
    ([(1216,228),(1240,228),(1240,500),(1216,500)],(1184,392))], [
    (64,120,'UnitAttributes',['Id','UnitId','Name','Value'],True),
    (480,120,'Units',['Id','Code','ManagementStatus','PriceVnd','Version']),
    (896,120,'UnitStatusHistory',['Id','UnitId','ChangedByUserId','OldStatus','NewStatus','Reason','ChangedAtUtc']),
    (896,440,'Users',['Id','Username','Role','IsActive'])],
    'Service permits Unit deletion only before any status history exists. Each status update and history row commit together.')

physical('db-batches','Physical DB — Bulk Creation','Accepted UnitBatches reference staff; created Units retain their originating Batch.',[
    ([(480,228),(432,228),(432,180),(384,180)],(432,156)),
    ([(896,204),(848,204),(848,180),(800,180)],(848,156))], [
    (64,120,'Users',['Id','Username','Role']),
    (480,120,'UnitBatches',['Id','RequestId','RequestedByUserId','ProjectId','BuildingId','Status','RequestedCount']),
    (896,120,'Units',['Id','OriginBatchId','Code','ProjectId','UnitType'])],
    'Batch also references Project and optional Building, as defined in SQL. PayloadJson holds immutable accepted unit data.')

physical('db-summaries','Physical DB — Project Summaries','Each ProjectSummary belongs to one Project and one SummaryRun.',[
    ([(480,204),(432,204),(432,180),(384,180)],(432,156)),
    ([(800,228),(848,228),(848,180),(896,180)],(848,156))], [
    (64,120,'Projects',['Id','Code','Name']),
    (480,120,'ProjectSummaries',['Id','ProjectId','SummaryRunId','TotalCount','ManagedCount','SoldCount','ManagedWithoutPriceCount']),
    (896,120,'SummaryRuns',['Id','EventId','ScheduledDate','RequestedByUserId','Status','AsOfUtc'])],
    'Unique (SummaryRunId, ProjectId); one scheduled run per local date. SummaryRuns also has an optional Users FK.')

physical('db-operations','Physical DB — Outbox and Backups','Outbox carries durable intent; BackupRuns store execution and retention state and optionally reference requesting staff.',[
    ([(800,228),(848,228),(848,180),(896,180)],(848,156))], [
    (64,120,'OutboxMessages',['Id','Kind','OperationId','OccurredAtUtc','PublishedAtUtc','PublishAttempts']),
    (480,120,'BackupRuns',['Id','EventId','RequestedByUserId','TriggerKind','Status','FileName','Retained']),
    (896,120,'Users',['Id','Username','Role'])],
    'EventId corresponds to Outbox Id by handler rule, not a SQL FK. Separate Daily vs Change/Manual retention pools.')

links=''.join(f'<article><h2><a href="{s}.html">{esc(t)}</a></h2><p>{esc(d)}</p><a href="{s}.html"><img src="images/{s}.png" alt="{esc(t)}" loading="lazy"></a><p><a href="images/{s}.svg">SVG</a> · <a href="images/{s}.png">PNG</a></p></article>' for s,t,d in DIAGRAMS)
(OUT/'index.html').write_text('<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Broker Catalogue — Diagrams</title><style>body{font-family:Segoe UI,Arial,sans-serif;color:#142b49;background:#fff;margin:32px}main{max-width:1280px;margin:auto}h1{font-size:32px}a{color:#175e9b}article{border-top:1px solid #d5dfea;padding:24px 0}img{max-width:100%;border:1px solid #d5dfea}p{line-height:1.6}</style></head><body><main><h1>Broker Catalogue — Design Diagrams</h1><p>C4 Level 1 and Level 2 only. Business ERD and physical subsystems. White / navy / cyan; offline system typography.</p>'+links+'</main></body></html>',encoding='utf-8')
(OUT/'manifest.json').write_text(json.dumps([{'slug':s,'title':t,'description':d} for s,t,d in DIAGRAMS],indent=2),encoding='utf-8')
print(f'Generated {len(DIAGRAMS)} HTML diagrams and dictionary for {len(TABLES)} tables.')
