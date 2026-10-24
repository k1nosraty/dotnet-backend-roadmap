"""Real final ServiceDesk deployment in UUID-owned local Docker resources; no public deployment."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import json
import os
import re
import shutil
import ssl
import subprocess
import tempfile
import time
from urllib import request,error
from uuid import uuid4
from load_lab import percentile
ROOT=Path(__file__).resolve().parents[1]
assert shutil.which('docker'),'Running Linux Docker is required.'
project='course-capstone-'+uuid4().hex[:12]
environment=os.environ.copy();environment.update({'CAPSTONE_DB_PASSWORD':'Aa9!'+uuid4().hex,'CAPSTONE_LAB_PASSWORD':'Aa9!'+uuid4().hex,
    'CAPSTONE_SCHEMA':'capstone_'+uuid4().hex,'CAPSTONE_TLS_PORT':'0','CAPSTONE_DATABASE':'desk','CAPSTONE_COMMIT':os.environ.get('GITHUB_SHA','local')})
assert re.fullmatch(r'[0-9a-f]{40}|local',environment['CAPSTONE_COMMIT'])
compose=['docker','compose','-p',project,'-f',str(ROOT/'samples/89-servicedesk-delivery/compose.yaml')]

def run(args,expected=0):
    result=subprocess.run(args,cwd=ROOT,env=environment,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=600)
    assert result.returncode==expected,(args,result.stdout,result.stderr)
    return result.stdout.strip()

def send(base,context,path,method='GET',body=None,token=None,headers=None):
    request_headers={**(headers or {})}
    if token:request_headers['Authorization']='Bearer '+token
    if body is not None:request_headers['Content-Type']='application/json'
    data=None if body is None else json.dumps(body).encode()
    opener=request.build_opener(request.ProxyHandler({}),request.HTTPSHandler(context=context))
    req=request.Request(base+path,data=data,method=method,headers=request_headers)
    try:
        with opener.open(req,timeout=10) as response:status,raw=response.status,response.read()
    except error.HTTPError as ex:status,raw=ex.code,ex.read()
    try:payload=json.loads(raw)
    except json.JSONDecodeError:payload=raw.decode('utf-8',errors='replace')
    return status,payload

def ready(base,context):
    deadline=time.monotonic()+60
    while time.monotonic()<deadline:
        try:
            if send(base,context,'/health/ready')[0]==200:return
        except (error.URLError,TimeoutError,ConnectionError):pass
        time.sleep(.2)
    raise AssertionError('Final app did not become ready over verified TLS.')

try:
    assert run(['docker','info','--format','{{.OSType}}'])=='linux'
    run(compose+['config','--quiet']);run(compose+['build','app']);run(compose+['up','-d','db'])
    db=run(compose+['ps','-q','db']);deadline=time.monotonic()+60
    while time.monotonic()<deadline:
        result=subprocess.run(['docker','exec',db,'pg_isready','-h','127.0.0.1','-U','course','-d','desk'],capture_output=True)
        if result.returncode==0:break
        time.sleep(.2)
    else:raise AssertionError('PostgreSQL not ready.')
    run(compose+['run','--rm','--no-deps','-e','ASPNETCORE_ENVIRONMENT=Development','-e','Desk__LabHttp=true',
                 '-e','CAPSTONE_LAB_PASSWORD','app','--init-lab'])
    run(compose+['up','-d','app','proxy'])
    proxy=run(compose+['ps','-q','proxy']);app=run(compose+['ps','-q','app'])
    def inspected(name):return json.loads(run(['docker','inspect',name]))[0]
    assert all(value is None for value in inspected(app)['NetworkSettings']['Ports'].values()),'Backend port is exposed.'
    assert all(value is None for value in inspected(db)['NetworkSettings']['Ports'].values()),'Database port is exposed.'
    assert run(['docker','exec',app,'id','-u']) not in ['0','']
    before_hash=run(['docker','exec',app,'sha256sum','/app/SupportDesk.Lesson90.dll'])
    port=inspected(proxy)['NetworkSettings']['Ports']['443/tcp'][0]['HostPort'];base='https://localhost:'+port
    with tempfile.TemporaryDirectory(prefix='capstone-ca-') as directory:
        root=Path(directory)/'root.crt';deadline=time.monotonic()+60
        while time.monotonic()<deadline:
            result=subprocess.run(['docker','cp',proxy+':/data/caddy/pki/authorities/local/root.crt',str(root)],capture_output=True)
            if result.returncode==0:break
            time.sleep(.2)
        else:raise AssertionError('Local CA not created.')
        context=ssl.create_default_context(cafile=str(root));ready(base,context)
        assert send(base,context,'/version')[1]=={'service':'servicedesk','version':'1.0.0','commit':environment['CAPSTONE_COMMIT']}
        assert send(base,context,'/requests',headers={'X-Test-Actor':'agent','X-Tenant':'alpha'})[0]==401
        def login(email):
            status,payload=send(base,context,'/auth/login','POST',{'email':email,'password':environment['CAPSTONE_LAB_PASSWORD']})
            assert status==200,'Real Identity login failed.'
            return payload['accessToken']
        owner=login('requester@alpha.test');agent=login('agent@alpha.test')
        key=str(uuid4());body={'title':'Capstone delivery proof','priority':'urgent'}
        status,row=send(base,context,'/requests','POST',body,owner,{'Idempotency-Key':key});assert status==201
        status,replay=send(base,context,'/requests','POST',body,owner,{'Idempotency-Key':key});assert status==200 and replay['id']==row['id']
        route='/requests/'+row['id']
        assert send(base,context,route+'/claim','POST',{'version':1},owner)[0]==403
        assert send(base,context,route+'/claim','POST',{'version':1},agent)[0]==200
        assert send(base,context,route+'/close','POST',{'version':2,'resolution':'Verified deployment'},agent)[0]==200
        assert len(send(base,context,route+'/audit',token=owner)[1])==3
        def read(_):
            started=time.perf_counter();status,rows=send(base,context,'/requests?page=1&size=10',token=owner)
            assert status==200 and len(rows)==1
            return (time.perf_counter()-started)*1000
        with ThreadPoolExecutor(max_workers=4) as pool:latencies=list(pool.map(read,range(24)))
        assert percentile(latencies,.95)<=5000,'Synthetic CI read budget exceeded.'
        print(f'PASS: real TLS+Identity login, scoped workflow and idempotency; 24 authenticated PostgreSQL reads, zero errors, p95={percentile(latencies,.95):.2f}ms (CI budget only).',flush=True)
        schema=environment['CAPSTONE_SCHEMA']
        run(['docker','exec',db,'pg_dump','-U','course','-d','desk','-Fc','-n',schema,'-f','/tmp/capstone.dump'])
        run(['docker','exec',db,'createdb','-U','course','restored'])
        run(['docker','exec',db,'pg_restore','-U','course','-d','restored','--exit-on-error','--no-owner','/tmp/capstone.dump'])
        environment['CAPSTONE_DATABASE']='restored'
        run(compose+['up','-d','--no-deps','--force-recreate','app']);ready(base,context)
        restored_app=run(compose+['ps','-q','app'])
        assert run(['docker','exec',restored_app,'sha256sum','/app/SupportDesk.Lesson90.dll'])==before_hash
        status,restored=send(base,context,route,token=owner);assert status==200 and restored['state']=='Closed' and restored['version']==3
        assert len(send(base,context,route+'/audit',token=owner)[1])==3
        print('PASS: same final image restarts against restored Identity+business schema; previous bearer token works with persisted keys, request version/state and audit survive.',flush=True)
except Exception:
    logs=subprocess.run(compose+['logs','--no-color','--tail','60'],cwd=ROOT,env=environment,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=30)
    print(logs.stdout[-12000:],flush=True)
    raise
finally:
    subprocess.run(compose+['down','--volumes','--remove-orphans','--rmi','local'],cwd=ROOT,env=environment,capture_output=True,timeout=90)
