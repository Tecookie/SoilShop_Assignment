"""Test failed archive writes and recovery against the isolated completion stack only."""
from pathlib import Path
import json, os, subprocess, time, uuid
from playwright.sync_api import sync_playwright

ROOT=Path(__file__).resolve().parents[1]
if os.environ.get('SHOP_CHECK_PROJECT')!='soilshop-completion-check':
    raise SystemExit('Use only the isolated soilshop-completion-check project.')
settings=dict(line.split('=',1) for line in (ROOT/'.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
base='http://127.0.0.1:'+os.environ.get('SHOP_CHECK_PORT','18086')
def docker(*args):
    result=subprocess.run(['docker',*args],capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
    assert result.returncode==0,result.stderr[-1000:]
    return result.stdout.strip()
def wait_for(check):
    deadline=time.monotonic()+120
    while time.monotonic()<deadline:
        result=check()
        if result:return result
        time.sleep(1)
    raise AssertionError('Timed out waiting for backup result')
names=docker('ps','-q','--filter','label=com.docker.compose.project=soilshop-completion-check','--filter','label=com.docker.compose.service=worker').splitlines()
assert len(names)==1
worker=names[0]
with sync_playwright() as p:
    request=p.request.new_context(base_url=base)
    login=request.post('/api/auth/login',data={'username':settings['BOOTSTRAP_USERNAME'],'password':settings['BOOTSTRAP_PASSWORD']});assert login.ok
    headers={'Authorization':'Bearer '+login.json()['token']}
    def list_runs():
        response=request.get('/api/backups?pageSize=100',headers=headers);assert response.ok;return response.json()['items']
    def submit():
        response=request.post('/api/backups',headers=headers,data={'requestKey':str(uuid.uuid4())});assert response.ok,(response.status,response.text()[:300]);return response.json()['id']
    wait_for(lambda:not any(x['status'] in ('Queued','Processing') for x in list_runs()))
    before={x['filename'] for x in list_runs() if x['retained']};assert len(before)==7
    docker('exec','--user','root',worker,'chmod','555','/backups')
    try:
        failed=submit()
        wait_for(lambda:next((x for x in list_runs() if x['id']==failed and x['status']=='Failed'),None))
        assert {x['filename'] for x in list_runs() if x['retained']}==before
    finally:
        docker('exec','--user','root',worker,'chmod','755','/backups')
    success=submit();wait_for(lambda:next((x for x in list_runs() if x['id']==success and x['status']=='Completed' and x['retained']),None))
    assert len([x for x in list_runs() if x['retained']])==7
    result={'passed':2,'checks':['Failed write preserves seven retained archives','A subsequent backup succeeds and retention remains seven'],'scope':'isolated soilshop-completion-check only'}
    (ROOT/'test-results'/'backup-failure-checks.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result))
