"""Lessons 61-64. --build-only checks builds + the fail-closed HTTP boundary.
Live mode requires PostgreSQL, SUPPORTDESK_PG and psql (PG* defaults match CI).
All accounts, schemas and keys are disposable. No password or token is printed.
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
from urllib import request, error

ROOT=Path(__file__).resolve().parents[1]
NAMES=['60-threat-model','61-identity','62-password-recovery','63-cookie-bearer']
if sys.argv[1:] not in ([],['--build-only']):raise SystemExit('Usage: security_smoke.py [--build-only]')
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
    run(['dotnet','build',str(ROOT/'samples'/name/'SupportDesk.csproj'),'--nologo'])
print('PASS: security sample builds.')
with tempfile.TemporaryDirectory(prefix='security61-') as directory:
    with server(ROOT/'samples/60-threat-model/bin/Debug/net10.0/SupportDesk.dll',Path(directory)) as base:
        for headers in [{},{'X-User':'admin','X-Role':'Administrator'},{'Authorization':'Bearer arbitrary-text'}]:
            assert send(client(),base,'/api/tickets',headers=headers)[0]==401,'Untrusted identity input bypassed the closed gate.'
        assert send(client(),base,'/health')[0]==200
print('PASS: fail-closed boundary ignores forged identity inputs.')
if sys.argv[1:]:
    print('PASS: build-only; account persistence and authentication were not exercised.')
    raise SystemExit(0)
if not os.environ.get('SUPPORTDESK_PG'):raise SystemExit('Set SUPPORTDESK_PG to a dedicated PostgreSQL lab database.')
for name in NAMES[1:3]:
    out=run(['dotnet',str(ROOT/'samples'/name/'bin/Debug/net10.0/SupportDesk.dll')])
    assert 'PASS: lesson scenario' in out
    assert 'PASS: Identity account creation' in out
    if name==NAMES[2]:assert 'PASS: salted password hashes' in out
    print(f'PASS: {name} real PostgreSQL account checks.')

penv=os.environ.copy()
for key,default in [('PGHOST','127.0.0.1'),('PGPORT','5432'),('PGDATABASE','supportdesk_lab'),('PGUSER','course'),('PGPASSWORD','course-ci-only')]:
    penv[key]=os.environ.get('SUPPORTDESK_TEST_'+key,default)
def sql(text):
    return run(['psql','-X','-v','ON_ERROR_STOP=1','-tA','-c',text],env=penv)

schema='auth64_'+uuid.uuid4().hex
with tempfile.TemporaryDirectory(prefix='security64-') as directory:
    folder=Path(directory);keys=folder/'keys';keys.mkdir()
    connection=os.environ['SUPPORTDESK_PG'].rstrip(';')+';Search Path='+schema
    password='Lab-'+secrets.token_urlsafe(24)+'!7aA'
    email='auth64@example.test'
    env=os.environ.copy();env.update({'SUPPORTDESK_PG':connection,'TEAMDESK_KEYS':str(keys),
        'TEAMDESK_LAB_EMAIL':email,'TEAMDESK_LAB_PASSWORD':password,'TEAMDESK_INSECURE_LAB':'1',
        'ASPNETCORE_ENVIRONMENT':'Development','DOTNET_ENVIRONMENT':'Development'})
    output=folder/'published'
    run(['dotnet','publish',str(ROOT/'samples/63-cookie-bearer/SupportDesk.csproj'),'-c','Release','-o',str(output),'--nologo'])
    dll=output/'SupportDesk.dll'
    sql(f'CREATE SCHEMA {schema}')
    try:
        assert 'PASS: fresh authentication lab initialized' in run(['dotnet',str(dll),'--init-lab'],env=env,cwd=folder)
        # Provisioning credentials are not needed by an HTTP host.
        hostenv={k:v for k,v in env.items() if k not in ['TEAMDESK_LAB_PASSWORD','TEAMDESK_LAB_EMAIL']}
        bearer_client=client();cookie_client=client()
        def bearer(token):return {'Authorization':'Bearer '+token}
        with server(dll,folder,hostenv) as base:
            assert send(client(),base,'/health/ready')[0]==200
            assert send(client(),base,'/api/me')[0]==401
            assert send(client(),base,'/api/me',headers={'X-User':'admin','X-Role':'Administrator'})[0]==401
            wrong=send(client(),base,'/auth/login','POST',{'email':email,'password':'Wrong-password!9Aa'})
            missing=send(client(),base,'/auth/login','POST',{'email':'missing@example.test','password':'Wrong-password!9Aa'})
            assert wrong[0]==missing[0]==401 and wrong[1]==missing[1],'Login reveals account existence through response shape.'
            status,tokens,_=send(bearer_client,base,'/auth/login','POST',{'email':email,'password':password})
            assert status==200 and tokens.get('accessToken') and tokens.get('refreshToken'),'Token issuance failed.'
            access,refresh=tokens['accessToken'],tokens['refreshToken']
            assert send(bearer_client,base,'/api/me',headers=bearer(access))[0]==200
            assert send(bearer_client,base,'/api/me',headers=bearer('invalid-'+access))[0]==401
            assert send(bearer_client,base,'/api/bearer-action','POST',headers=bearer(access))[0]==204
            assert send(bearer_client,base,'/api/cookie-action','POST',headers=bearer(access))[0]==401
            assert send(client(),base,'/auth/refresh','POST',{'refreshToken':'invalid'})[0]==401
            assert send(client(),base,'/auth/refresh','POST',{'refreshToken':refresh})[0]==200
            # This built-in protected-token flow does not implement refresh-token reuse detection.
            assert send(client(),base,'/auth/refresh','POST',{'refreshToken':refresh})[0]==200
            assert send(cookie_client,base,'/auth/login?mode=cookie','POST',{'email':email,'password':password})[0]==400
            csrf=send(cookie_client,base,'/auth/csrf')[1]['requestToken']
            status,_,headers=send(cookie_client,base,'/auth/login?mode=cookie','POST',{'email':email,'password':password},{'X-CSRF-TOKEN':csrf})
            assert status==200
            cookies=' '.join(headers.get_all('Set-Cookie',[]))
            assert 'TeamDesk.Auth=' in cookies and 'httponly' in cookies.lower() and 'samesite=lax' in cookies.lower()
            assert send(cookie_client,base,'/api/me')[0]==200
            assert send(cookie_client,base,'/api/bearer-action','POST')[0]==401
            assert send(cookie_client,base,'/api/cookie-action','POST')[0]==400
            assert send(cookie_client,base,'/api/cookie-action','POST',headers={'X-CSRF-TOKEN':csrf})[0]==400,'Anonymous CSRF token accepted after login.'
            csrf=send(cookie_client,base,'/auth/csrf')[1]['requestToken']
            assert send(cookie_client,base,'/api/cookie-action','POST',headers={'X-CSRF-TOKEN':csrf})[0]==204
            assert send(cookie_client,base,'/auth/logout','POST')[0]==400
            assert send(cookie_client,base,'/auth/logout','POST',headers={'X-CSRF-TOKEN':csrf})[0]==204
            assert send(cookie_client,base,'/api/me')[0]==401
            assert send(bearer_client,base,'/api/me',headers=bearer(access))[0]==200,'Cookie logout unexpectedly revoked bearer access.'
            csrf=send(cookie_client,base,'/auth/csrf')[1]['requestToken']
            assert send(cookie_client,base,'/auth/login?mode=cookie','POST',{'email':email,'password':password},{'X-CSRF-TOKEN':csrf})[0]==200
        with server(dll,folder,hostenv) as base:
            assert send(bearer_client,base,'/api/me',headers=bearer(access))[0]==200,'Persisted key ring did not survive restart.'
            assert send(cookie_client,base,'/api/me')[0]==200,'Cookie did not survive host restart.'
        production={**hostenv,'ASPNETCORE_ENVIRONMENT':'Production','DOTNET_ENVIRONMENT':'Production','TEAMDESK_INSECURE_LAB':'0'}
        with server(dll,folder,production,ready_status=400) as base:
            assert send(client(),base,'/auth/csrf')[0]==400,'Plain HTTP unexpectedly allowed outside local lab.'
        print('PASS: real Identity cookie/bearer, refresh, CSRF, logout boundaries, persistent accounts/keys and transport policy.')
    finally:
        sql(f'DROP SCHEMA {schema} CASCADE')
