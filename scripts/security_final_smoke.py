"""Lessons 69-70: isolated configuration, minimal logs, transactional audit and security review.
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
NAMES=['68-secrets-audit','69-security-review']
if sys.argv[1:] not in ([],['--build-only']):raise SystemExit('Usage: security_final_smoke.py [--build-only]')
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



def checked_exit(args, env):
    p=subprocess.run(args,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=30)
    assert p.returncode!=0,'Unsafe startup configuration was accepted.'
    return p.stdout+p.stderr

def scan(text, forbidden):
    assert all(value not in text for value in forbidden if value),'Sensitive canary appeared in application diagnostics or audit.'

for lesson,name in enumerate(NAMES,69):
    run(['dotnet','build',str(ROOT/'samples'/name/'SupportDesk.csproj'),'--nologo','-m:1'])
    dll=ROOT/'samples'/name/'bin/Debug/net10.0/SupportDesk.dll'
    with tempfile.TemporaryDirectory(prefix='audit-boundary-') as directory:
        folder=Path(directory);schema=f'auth{lesson}_'+uuid.uuid4().hex
        env=os.environ.copy();env.update({'SUPPORTDESK_PG':'Host=127.0.0.1;Database=unused;Username=unused;Search Path='+schema,
            'TEAMDESK_KEYS':str(folder/'keys'),'TEAMDESK_INSECURE_LAB':'1',
            'ASPNETCORE_ENVIRONMENT':'Development','DOTNET_ENVIRONMENT':'Development'})
        canary='ConfigurationCanary_'+secrets.token_hex(16)
        for value in ['',f'Host=localhost;Password={canary};UnknownField={canary}',
                      env['SUPPORTDESK_PG']+';Include Error Detail=true',env['SUPPORTDESK_PG']+';Log Parameters=true',
                      'Host=localhost;Database=unused;Username=unused;Search Path=public']:
            bad={**env,'SUPPORTDESK_PG':value}
            scan(checked_exit(['dotnet',str(dll)],bad),[canary])
        checked_exit(['dotnet',str(dll)],{**env,'TEAMDESK_KEYS':'relative-keys'})
        checked_exit(['dotnet',str(dll)],{**env,'ASPNETCORE_ENVIRONMENT':'Production','DOTNET_ENVIRONMENT':'Production'})
        if lesson==70:
            output=run(['dotnet',str(dll),'--review'],env=env,cwd=folder)
            assert 'PASS: actual route metadata and deliberate public-route regression rejection' in output
            inventory=json.loads(next(line for line in output.splitlines() if line.startswith('[{')))
            assert len(inventory)>=15
        with server(dll,folder,env) as base:
            for path in ['/api/staff/audit','/api/tickets','/api/private-default']:
                assert send(client(),base,path)[0]==401
                assert send(client(),base,path,headers={'X-Role':'SupportAgent','X-Permission':'audit.read'})[0]==401
        print(f'PASS: lesson {lesson} build, secret-safe rejected configuration, anonymous boundary and CLI review.')
if sys.argv[1:]:
    print('PASS: build-only; transactional audit and real account behavior were not exercised.')
    raise SystemExit(0)
if not os.environ.get('SUPPORTDESK_PG'):raise SystemExit('Set SUPPORTDESK_PG to a dedicated PostgreSQL lab database.')
penv=os.environ.copy()
for key,default in [('PGHOST','127.0.0.1'),('PGPORT','5432'),('PGDATABASE','supportdesk_lab'),('PGUSER','course'),('PGPASSWORD','course-ci-only')]:
    penv[key]=os.environ.get('SUPPORTDESK_TEST_'+key,default)
def sql(text):return run(['psql','-X','-v','ON_ERROR_STOP=1','-tA','-c',text],env=penv)
def auth(token):return {'Authorization':'Bearer '+token}

for lesson,name in enumerate(NAMES,69):
    schema=f'auth{lesson}_'+uuid.uuid4().hex
    with tempfile.TemporaryDirectory(prefix=f'audit{lesson}-') as directory:
        folder=Path(directory);keys=folder/'keys';keys.mkdir()
        password='Lab-'+secrets.token_urlsafe(24)+'!7aA'
        marker='PrivateTitle_'+secrets.token_hex(12)
        query_marker='PrivateQuery_'+secrets.token_hex(12)
        bad_token='UntrustedBearer_'+secrets.token_hex(12)
        bad_email='private-'+secrets.token_hex(8)+'@example.test'
        env=os.environ.copy();env.update({'SUPPORTDESK_PG':os.environ['SUPPORTDESK_PG'].rstrip(';')+';Search Path='+schema,
            'TEAMDESK_KEYS':str(keys),'TEAMDESK_LAB_PASSWORD':password,'TEAMDESK_INSECURE_LAB':'1',
            'ASPNETCORE_ENVIRONMENT':'Development','DOTNET_ENVIRONMENT':'Development'})
        output=folder/'published'
        run(['dotnet','publish',str(ROOT/'samples'/name/'SupportDesk.csproj'),'-c','Release','-o',str(output),'--nologo','-m:1'])
        dll=output/'SupportDesk.dll'
        sql(f'CREATE SCHEMA {schema}')
        try:
            provision=run(['dotnet',str(dll),'--init-lab'],env=env,cwd=folder)
            assert 'PASS: fresh access lab initialized' in provision
            scan(provision,[password])
            hostenv={k:v for k,v in env.items() if k!='TEAMDESK_LAB_PASSWORD'}
            tokens=[]
            with server(dll,folder,hostenv) as base:
                assert send(client(),base,'/health/ready')[0]==200
                def login(who):
                    status,body,_=send(client(),base,'/auth/login','POST',{'email':who+'@example.test','password':password})
                    assert status==200,'Disposable account login failed.'
                    tokens.extend([body['accessToken'],body['refreshToken']])
                    return body['accessToken']
                owner,peer,other,staff,roleonly=[login(x) for x in ['owner','peer','other','staff','roleonly']]
                for token in [owner,peer,other,roleonly]:
                    assert send(client(),base,'/api/staff/audit',headers=auth(token))[0]==403
                assert send(client(),base,'/api/staff/audit',headers=auth(staff))[1]==[]
                assert send(client(),base,'/auth/login','POST',{'email':bad_email,'password':password})[0]==401
                assert send(client(),base,'/api/me',headers=auth(bad_token))[0]==401
                assert send(client(),base,'/auth/login','POST',{'email':bad_email,'password':'x'*20000})[0]==413
                made=send(client(),base,'/api/tickets','POST',{'title':marker},auth(owner))
                assert made[0]==201
                ticket=made[1]['id'];path='/api/tickets/'+ticket
                audit=send(client(),base,'/api/staff/audit',headers=auth(staff))
                assert audit[0]==200 and len(audit[1])==1
                first=audit[1][0]
                assert first['kind']=='TicketCreated' and first['targetId']==ticket
                assert set(first)=={'id','occurredAt','actorId','kind','targetId'}
                assert send(client(),base,path,headers=auth(peer))[0]==404
                assert send(client(),base,path+'/title','POST',{'title':'Unauthorized mutation'},auth(other))[0]==404
                assert send(client(),base,path,headers=auth(owner))[1]['title']==marker
                assert send(client(),base,'/api/tickets','POST',{'title':marker,'ownerId':'forged'},auth(owner))[0]==400
                assert send(client(),base,'/api/tickets/search?term='+query_marker,headers=auth(owner))[1]==[]
                assert send(client(),base,path+'/title','POST',{'title':marker+' Renamed'},auth(owner))[0]==204
                # Separate tenant's mutation must not enter alpha's audit response.
                assert send(client(),base,'/api/tickets','POST',{'title':'Other tenant'},auth(other))[0]==201
                rows=send(client(),base,'/api/staff/audit',headers=auth(staff))[1]
                assert len(rows)==2 and {x['kind'] for x in rows}=={'TicketCreated','TicketRenamed'}
                assert all(x['targetId']==ticket for x in rows)
                # An audit write failure must roll back a prior ExecuteUpdate as well as failed creates.
                sql(f'ALTER TABLE {schema}."AuditEntries" RENAME TO "AuditUnavailable"')
                try:
                    failure=send(client(),base,path+'/title','POST',{'title':'Must roll back'},auth(owner))
                    assert failure[0]==500 and failure[1]['title']=='Server operation failed.'
                    assert send(client(),base,'/api/tickets','POST',{'title':'Must not insert'},auth(owner))[0]==500
                finally:
                    sql(f'ALTER TABLE {schema}."AuditUnavailable" RENAME TO "AuditEntries"')
                assert send(client(),base,path,headers=auth(owner))[1]['title']==marker+' Renamed'
                assert len(send(client(),base,'/api/tickets',headers=auth(owner))[1])==1
                assert len(send(client(),base,'/api/staff/audit',headers=auth(staff))[1])==2
                # Revoke only audit.read: the same staff token retains report permission, loses audit.
                sql(f"DELETE FROM {schema}.\"AspNetUserClaims\" WHERE \"ClaimType\"='permission' AND \"ClaimValue\"='audit.read' AND \"UserId\"=(SELECT \"Id\" FROM {schema}.\"AspNetUsers\" WHERE \"Email\"='staff@example.test')")
                assert send(client(),base,'/api/staff/audit',headers=auth(staff))[0]==403
                assert send(client(),base,'/api/staff/report',headers=auth(staff))[0]==200
                # Sensitive values must not survive in the selected app log or durable audit rows.
                forbidden=[password,marker,query_marker,bad_email,bad_token,*tokens,'owner@example.test','staff@example.test']
                scan(json.dumps(rows),forbidden)
                audit_dump=sql(f'SELECT row_to_json(a) FROM {schema}."AuditEntries" a')
                scan(audit_dump,forbidden)
            logs=(folder/'server.log').read_text(encoding='utf-8')
            scan(logs,forbidden)
            events=[json.loads(line) for line in logs.splitlines() if line.startswith('{')]
            security=[e for e in events if e.get('Category')=='SupportDesk.Audit.SecurityEvents']
            codes=[e.get('State',{}).get('Code') for e in security]
            for code in ['LoginSucceeded','LoginRejected','AccessDenied','TicketCreated','TicketRenamed','ServerFailure']:
                assert code in codes,'Expected fixed security event was absent.'
            assert codes.count('TicketCreated')==2 and codes.count('TicketRenamed')==1,'Failed writes emitted a success event.'
            with server(dll,folder,hostenv) as base:
                assert send(client(),base,path,headers=auth(owner))[0]==200
                assert send(client(),base,path,headers=auth(other))[0]==404
                assert sql(f'SELECT count(*) FROM {schema}."AuditEntries"').strip()=='3'
            print(f'PASS: lesson {lesson} real transactional audit, rollback, tenant/policy isolation, safe diagnostics and restart.')
        finally:
            sql(f'DROP SCHEMA {schema} CASCADE')
print('PASS: final security labs; no claim of production certification or exhaustive vulnerability coverage.')
