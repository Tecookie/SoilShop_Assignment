"""Static documentation consistency checks, not a SQL Server execution test."""
from pathlib import Path
import json
import re
import struct
import xml.etree.ElementTree as ET
from urllib.parse import unquote

ROOT=Path(__file__).resolve().parents[2]
docs=ROOT/'docs'
errors=[]
link_count=0
for p in [ROOT/'README.md', ROOT/'AGENTS.md', *docs.rglob('*.md'), *docs.rglob('*.html')]:
    s=p.read_text(encoding='utf-8')
    targets=re.findall(r'\]\(([^)]+)\)',s) if p.suffix=='.md' else re.findall(r'(?:href|src)="([^"]+)"',s)
    for dest in targets:
        dest=unquote(dest.strip('<>')).split('#')[0]
        if not dest or re.match(r'^[a-z]+:',dest): continue
        link_count+=1
        if not (p.parent/dest).exists(): errors.append(f'Missing link from {p.relative_to(ROOT)}: {dest}')

sql=(docs/'database/schema.sql').read_text(encoding='utf-8')
metadata=json.loads((docs/'diagrams/schema-metadata.json').read_text(encoding='utf-8'))
fk_count=0
for name,t in metadata.items():
    cols={c['name']:c for c in t['columns']}
    for m in re.finditer(r'FOREIGN KEY \(([^)]+)\) REFERENCES dbo\.(\w+)\(([^)]+)\)',t['definition']):
        fk_count+=1
        child,parent,keys=m.groups()
        parentcols={c['name']:c for c in metadata[parent]['columns']}
        cc=[x.strip() for x in child.split(',')]; pc=[x.strip() for x in keys.split(',')]
        if len(cc)!=len(pc): errors.append(f'FK arity: {name} -> {parent}')
        for c,k in zip(cc,pc):
            if c not in cols or k not in parentcols: errors.append(f'FK unknown column: {name}.{c} -> {parent}.{k}')
            elif cols[c]['type']!=parentcols[k]['type']: errors.append(f'FK type mismatch: {name}.{c}')
    if sum(c['pk'] for c in t['columns'])!=1: errors.append('Missing or repeated PK: '+name)
names=re.findall(r'CONSTRAINT (\w+)',sql)
if len(names)!=len(set(names)): errors.append('Duplicate named constraints')

manifest=json.loads((docs/'diagrams/manifest.json').read_text(encoding='utf-8'))
for d in manifest:
    slug=d['slug']; img=docs/'diagrams/images'
    tree=ET.parse(img/f'{slug}.svg')
    ids={e.attrib['id'] for e in tree.iter() if 'id' in e.attrib}
    for e in tree.iter():
        for value in e.attrib.values():
            for ref in re.findall(r'url\(#([^)]+)\)',value):
                if ref not in ids: errors.append('Missing SVG marker '+ref)
    raw=(img/f'{slug}.png').read_bytes()
    w,h=struct.unpack('>II',raw[16:24])
    if (w,h)!=(2560,1440): errors.append(f'Unexpected PNG dimensions {slug}: {w}x{h}')

dsl=(docs/'diagrams/workspace.dsl').read_text(encoding='utf-8')
declared=set(re.findall(r'(\w+)\s*=\s*(?:person|softwareSystem|container)\b',dsl))
for a,b in re.findall(r'(\w+)\s*->\s*(\w+)',dsl):
    if a not in declared or b not in declared: errors.append('Undeclared C4 element')
if len(re.findall(r'\bsystemContext\s+catalogue',dsl))!=1 or len(re.findall(r'\bcontainer\s+catalogue',dsl))!=1:
    errors.append('Expected one C4 L1 and one C4 L2 view')
if re.search(r'\b(?:component|deployment|systemLandscape)\s',dsl): errors.append('Unexpected extra C4 level')

report={'local_links_checked':link_count,'tables':len(metadata),'foreign_keys':fk_count,'diagram_exports':len(manifest),'errors':errors}
print(json.dumps(report,indent=2))
(docs/'diagrams/static-checks.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
raise SystemExit(bool(errors))
