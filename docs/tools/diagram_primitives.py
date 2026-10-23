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
def save(slug,title,desc,body,caption,legend='Solid: connection / relationship    Dashed: offline or system boundary',w=1280,h=720):
    svg=(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" role="img" aria-labelledby="{slug}-title {slug}-desc">'
         f'<title id="{slug}-title">{esc(title)}</title><desc id="{slug}-desc">{esc(desc)}</desc>'
         f'<defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M 0 1 L 9 5 L 0 9 Z" fill="{P["muted"]}"/></marker></defs>'
         f'<rect width="{w}" height="{h}" fill="{P["paper"]}"/>'
         +text(40,40,title,28,weight=600)+text(40,64,'POTTING SOIL SHOP / PRN232 / DESIGN',9,'soft',mono=True)
         +body+f'<line x1="40" y1="{h-64}" x2="{w-40}" y2="{h-64}" stroke="{P["rule"]}"/>'
         +text(40,h-40,legend,11,'muted')+text(40,h-16,caption,11,'muted')+'</svg>')
    document=(f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{esc(title)}</title>'
              f'<style>body{{margin:0;background:#fff;color:#142B49;font-family:Segoe UI,Arial,sans-serif}}main{{max-width:{w}px;margin:24px auto;padding:16px;min-width:0}}.diagram-container{{width:100%;overflow-x:auto}}svg{{display:block;width:{w}px;min-width:{w}px}}p{{line-height:1.6}}a{{color:#175E9B}}@media print{{.diagram-container{{overflow-x:visible}}svg{{width:100%;min-width:0}}}}</style></head>'
              f'<body><main><div class="diagram-container">{svg}</div><p>{esc(desc)}</p><p><a href="index.html">All diagrams</a> · <a href="images/{slug}.svg">SVG</a> · <a href="images/{slug}.png">PNG</a></p></main></body></html>')
    (OUT/f'{slug}.html').write_text(document,encoding='utf-8')
    DIAGRAMS.append((slug,title,desc,w,h))
