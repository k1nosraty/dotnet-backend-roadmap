"""Lessons 65-68: real Identity grants, resource ownership, CORS/CSRF and request budgets.
--build-only builds all snapshots and checks anonymous/fallback authorization over HTTP.
Live mode needs a dedicated PostgreSQL database, SUPPORTDESK_PG and psql; see section README.
"""
from pathlib import Path
from contextlib import contextmanager
import http.cookiejar
import json
import os
import re
import secrets
import shutil
import subprocess
import sys
import tempfile
import time
import uuid
from concurrent.futures import ThreadPoolExecutor
from urllib import request, error, parse

ROOT=Path(__file__).resolve().parents[1]
NAMES=['64-policies','65-resource-ownership','66-web-security','67-rate-limits']
if sys.argv[1:] not in ([],['--build-only']):raise SystemExit('Usage: security_next_smoke.py [--build-only]')
if not shutil.which('dotnet'):raise SystemExit('Install .NET SDK 10.')
def run(args, *, env=None, cwd=None):
    p=subprocess.run(args,env=env,cwd=cwd,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
    assert p.returncode==0,(p.stdout,p.stderr)
    return p.stdout

def client():
    return request.build_opener(request.ProxyHandler({}),request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

def send(opener,base,path,method='GET',body=None,headers=None):
    data=None if body is None else json.dumps(body).encode('utf-8')
    req=request.Request(base+path,data=data,method=method,headers={'Content-Type':'application/json',**(headers or {})})
    try:
        with opener.open(req,timeout=15) as response:
            raw=response.read();return response.status,json.loads(raw) if raw else None,response.headers
    except error.HTTPError as ex:
        raw=ex.read()
        try:parsed=json.loads(raw) if raw else None
        except json.JSONDecodeError:parsed=None
        return ex.code,parsed,ex.headers

@contextmanager
def server(dll,folder,overrides=None,ready_status=200):
    env=os.environ.copy();env['ASPNETCORE_ENVIRONMENT']='Development';env['DOTNET_ENVIRONMENT']='Development'
    env.update(overrides or {})
    with (folder/'server.log').open('w',encoding='utf-8') as log:
        process=subprocess.Popen(['dotnet',str(dll),'--urls','http://127.0.0.1:0'],cwd=folder,env=env,stdout=log,stderr=subprocess.STDOUT)
        try:
            for _ in range(250):
                logs=(folder/'server.log').read_text(encoding='utf-8',errors='replace')
                if process.poll() is not None:raise AssertionError('Authentication lab exited during startup. Inspect its private test log.')
                match=re.search(r'Now listening on: (http://127\.0\.0\.1:\d+)',logs)
                if match:
                    base=match.group(1)
                    if send(client(),base,'/health')[0]==ready_status:break
                time.sleep(0.1)
            else:raise AssertionError('Server startup timeout.')
            yield base
        finally:
            if process.poll() is None:
                process.terminate()
                try:process.wait(timeout=15)
                except subprocess.TimeoutExpired:process.kill();process.wait(timeout=10)


for name in NAMES:
    run(['dotnet','build',str(ROOT/'samples'/name/'SupportDesk.csproj'),'--nologo','-m:1'])
    with tempfile.TemporaryDirectory(prefix='access-anonymous-') as directory:
        folder=Path(directory)
        env={'SUPPORTDESK_PG':'Host=127.0.0.1;Database=unused;Username=unused',
             'TEAMDESK_KEYS':str(folder/'keys'),'TEAMDESK_INSECURE_LAB':'1'}
        with server(ROOT/'samples'/name/'bin/Debug/net10.0/SupportDesk.dll',folder,env) as base:
            for path in ['/api/private-default','/api/staff/report']:
                for headers in [{},{'X-Role':'SupportAgent','X-User':'staff','X-Permission':'tickets.manage'},
                                {'Authorization':'Bearer arbitrary-input'}]:
                    assert send(client(),base,path,headers=headers)[0]==401,'Untrusted input bypassed authorization.'
            if name!=NAMES[0]:assert send(client(),base,'/api/tickets')[0]==401
    print(f'PASS: {name} build and anonymous/default-deny HTTP.')
if sys.argv[1:]:
    print('PASS: build-only; real account, ownership and abuse-control checks were not exercised.')
    raise SystemExit(0)
if not os.environ.get('SUPPORTDESK_PG'):raise SystemExit('Set SUPPORTDESK_PG to a dedicated PostgreSQL lab database.')
penv=os.environ.copy()
for key,default in [('PGHOST','127.0.0.1'),('PGPORT','5432'),('PGDATABASE','supportdesk_lab'),('PGUSER','course'),('PGPASSWORD','course-ci-only')]:
    penv[key]=os.environ.get('SUPPORTDESK_TEST_'+key,default)
def sql(text):return run(['psql','-X','-v','ON_ERROR_STOP=1','-tA','-c',text],env=penv)
def auth(token):return {'Authorization':'Bearer '+token}

for lesson,name in enumerate(NAMES,65):
    schema=f'auth{lesson}_'+uuid.uuid4().hex
    with tempfile.TemporaryDirectory(prefix=f'access{lesson}-') as directory:
        folder=Path(directory);keys=folder/'keys';keys.mkdir()
        password='Lab-'+secrets.token_urlsafe(24)+'!7aA'
        env=os.environ.copy();env.update({'SUPPORTDESK_PG':os.environ['SUPPORTDESK_PG'].rstrip(';')+';Search Path='+schema,
            'TEAMDESK_KEYS':str(keys),'TEAMDESK_LAB_PASSWORD':password,'TEAMDESK_INSECURE_LAB':'1',
            'ASPNETCORE_ENVIRONMENT':'Development','DOTNET_ENVIRONMENT':'Development'})
        output=folder/'published'
        run(['dotnet','publish',str(ROOT/'samples'/name/'SupportDesk.csproj'),'-c','Release','-o',str(output),'--nologo','-m:1'])
        dll=output/'SupportDesk.dll'
        sql(f'CREATE SCHEMA {schema}')
        try:
            assert 'PASS: fresh access lab initialized' in run(['dotnet',str(dll),'--init-lab'],env=env,cwd=folder)
            hostenv={k:v for k,v in env.items() if k!='TEAMDESK_LAB_PASSWORD'}
            with server(dll,folder,hostenv) as base:
                assert send(client(),base,'/health/ready')[0]==200
                def login(who,extra=None):
                    result=send(client(),base,'/auth/login','POST',{'email':who+'@example.test','password':password,**(extra or {})})
                    assert result[0]==200,'Disposable lab login failed.'
                    return result[1]['accessToken']
                owner,peer,other,staff,roleonly,claimonly=[login(x) for x in ['owner','peer','other','staff','roleonly','claimonly']]
                assert send(client(),base,'/api/private-default',headers=auth(owner))[0]==200
                assert send(client(),base,'/api/staff/report',headers=auth(owner))[0]==403
                for token in [peer,other,roleonly,claimonly]:
                    assert send(client(),base,'/api/staff/report',headers=auth(token))[0]==403,'Role/permission AND policy bypassed.'
                assert send(client(),base,'/api/staff/report',headers=auth(staff))[0]==200
                forged={**auth(owner),'X-Role':'SupportAgent','X-Permission':'tickets.manage'}
                assert send(client(),base,'/api/staff/report',headers=forged)[0]==403
                # Prove the current database grant wins over an already-issued principal.
                sql(f"INSERT INTO {schema}.\"AspNetUserClaims\" (\"UserId\",\"ClaimType\",\"ClaimValue\") SELECT \"Id\",'permission','tickets.manage' FROM {schema}.\"AspNetUsers\" WHERE \"Email\"='roleonly@example.test'")
                assert send(client(),base,'/api/staff/report',headers=auth(roleonly))[0]==200
                sql(f"DELETE FROM {schema}.\"AspNetUserClaims\" WHERE \"ClaimType\"='permission' AND \"UserId\"=(SELECT \"Id\" FROM {schema}.\"AspNetUsers\" WHERE \"Email\"='staff@example.test')")
                assert send(client(),base,'/api/staff/report',headers=auth(staff))[0]==403,'Revoked grant survived in an old token.'
                if lesson>=66:
                    made=send(client(),base,'/api/tickets','POST',{'title':'Owner alpha ticket'},auth(owner))
                    assert made[0]==201 and made[1]['title']=='Owner alpha ticket'
                    ticket=made[1]['id'];path='/api/tickets/'+ticket
                    assert send(client(),base,path,headers=auth(owner))[0]==200
                    for token in [peer,other,staff]:
                        assert send(client(),base,path,headers=auth(token))[0]==404
                        assert send(client(),base,path+'/title','POST',{'title':'Stolen update'},auth(token))[0]==404
                        assert send(client(),base,'/api/tickets',headers=auth(token))[1]==[]
                    assert send(client(),base,'/api/tickets/'+str(uuid.uuid4()),headers=auth(peer))[:2]==send(client(),base,path,headers=auth(peer))[:2]
                    assert send(client(),base,path,headers={**auth(peer),'X-Owner-Id':ticket,'X-Tenant':'alpha'})[0]==404
                    assert send(client(),base,path+'/title','POST',{'title':'Renamed by owner'},auth(owner))[0]==204
                    assert send(client(),base,path,headers=auth(owner))[1]['title']=='Renamed by owner'
                    assert send(client(),base,'/api/tickets','POST',{'title':'Forged','ownerId':'peer','tenantId':'beta'},auth(owner))[0]==400
                    assert send(client(),base,path+'/title','POST',{'title':'Forged','tenantId':'beta'},auth(owner))[0]==400
                    assert len(send(client(),base,'/api/tickets',headers=auth(owner))[1])==1
                    sql(f"UPDATE {schema}.\"AspNetUserClaims\" SET \"ClaimValue\"='beta' WHERE \"ClaimType\"='tenant' AND \"UserId\"=(SELECT \"Id\" FROM {schema}.\"AspNetUsers\" WHERE \"Email\"='owner@example.test')")
                    assert send(client(),base,path,headers=auth(owner))[0]==404,'Tenant membership was taken from stale token claims.'
                    assert send(client(),base,'/api/tickets',headers=auth(owner))[1]==[]
                    sql(f"UPDATE {schema}.\"AspNetUserClaims\" SET \"ClaimValue\"='alpha' WHERE \"ClaimType\"='tenant' AND \"UserId\"=(SELECT \"Id\" FROM {schema}.\"AspNetUsers\" WHERE \"Email\"='owner@example.test')")
                    assert send(client(),base,path,headers=auth(owner))[0]==200
                if lesson>=67:
                    allowed='https://portal.example.test';evil='https://evil.example.test'
                    status,_,headers=send(client(),base,'/api/tickets','OPTIONS',headers={'Origin':allowed,'Access-Control-Request-Method':'POST','Access-Control-Request-Headers':'authorization,content-type'})
                    assert status==204 and headers.get('Access-Control-Allow-Origin')==allowed
                    assert 'POST' in headers.get('Access-Control-Allow-Methods','')
                    assert not headers.get('Access-Control-Allow-Credentials'),'Cross-origin cookies were enabled.'
                    for origin in [evil,'null','https://portal.example.test.evil.test']:
                        headers=send(client(),base,'/api/tickets','OPTIONS',headers={'Origin':origin,'Access-Control-Request-Method':'POST'})[2]
                        assert not headers.get('Access-Control-Allow-Origin')
                    status,_,headers=send(client(),base,path,headers={**auth(owner),'Origin':evil})
                    assert status==200 and not headers.get('Access-Control-Allow-Origin'),'CORS was confused with server authorization.'
                    assert send(client(),base,path,headers={'Origin':allowed})[0]==401
                    for term in ["' OR 1=1 --",'%', '_']:
                        assert send(client(),base,'/api/tickets/search?'+parse.urlencode({'term':term}),headers=auth(owner))[1]==[],'Search input changed query meaning.'
                    assert len(send(client(),base,'/api/tickets/search?term=Renamed',headers=auth(owner))[1])==1
                    markup='<script>alert(1)</script>'
                    assert send(client(),base,path+'/title','POST',{'title':markup},auth(owner))[0]==204
                    status,body,headers=send(client(),base,path,headers=auth(owner))
                    assert status==200 and body['title']==markup and headers.get_content_type()=='application/json'
                    assert headers.get('X-Content-Type-Options')=='nosniff'
                    cookie=client()
                    assert send(cookie,base,'/auth/login?mode=cookie','POST',{'email':'owner@example.test','password':password})[0]==400
                    csrf=send(cookie,base,'/auth/csrf')[1]['requestToken']
                    assert send(cookie,base,'/auth/login?mode=cookie','POST',{'email':'owner@example.test','password':password},{'X-CSRF-TOKEN':csrf})[0]==200
                    assert send(cookie,base,'/api/tickets','POST',{'title':'Cookie attempt'})[0]==401
                    assert send(cookie,base,'/api/cookie-action','POST')[0]==400
                    csrf=send(cookie,base,'/auth/csrf')[1]['requestToken']
                    assert send(cookie,base,'/api/cookie-action','POST',headers={'X-CSRF-TOKEN':csrf})[0]==204
                if lesson==68:
                    # New host below resets only the in-process limiter; accounts and keys persist.
                    saved_owner,saved_peer=owner,peer
            if lesson==68:
                with server(dll,folder,hostenv) as base:
                    # Parallel requests exercise real atomic permit acquisition, not a mocked counter.
                    def get_ticket(_):return send(client(),base,path,headers=auth(saved_owner))[0]
                    with ThreadPoolExecutor(max_workers=8) as pool:statuses=list(pool.map(get_ticket,range(40)))
                    assert statuses.count(200)==32 and statuses.count(429)==8,'Per-user request budget was not enforced.'
                    status,_,headers=send(client(),base,path,headers={**auth(saved_owner),'X-Forwarded-For':'203.0.113.7'})
                    assert status==429 and int(headers['Retry-After'])>0
                    assert send(client(),base,'/api/tickets',headers=auth(saved_peer))[0]==200,'One user consumed another user budget.'
                    assert send(client(),base,'/health')[0]==200
                    # Login and refresh share an IP budget. Varying email/forwarded headers cannot reset it.
                    for i in range(12):
                        result=send(client(),base,'/auth/login','POST',{'email':f'nonexistent{i}@example.test','password':'Wrong!9Aa'},
                            {'X-Forwarded-For':f'203.0.113.{i}'})
                        assert result[0]==401
                    assert send(client(),base,'/auth/refresh','POST',{'refreshToken':'invalid'})[0]==429
                    assert send(client(),base,'/auth/login','POST',{'email':'owner@example.test','password':password})[0]==429
            print(f'PASS: lesson {lesson} real PostgreSQL grants, authorization and published HTTP boundaries.')
        finally:
            sql(f'DROP SCHEMA {schema} CASCADE')
print('PASS: current grants, ownership/tenant isolation, safe input, CORS/CSRF and partitioned rate limits.')
