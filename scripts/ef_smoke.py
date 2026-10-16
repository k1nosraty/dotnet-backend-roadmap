"""EF and Dapper lessons: real PostgreSQL, migrations, concurrency and API restart."""
from pathlib import Path
import concurrent.futures
import json
import os
import subprocess
import sys
import uuid
from http_checks import server, request

ROOT = Path(__file__).resolve().parents[1]
NAMES = ['40-ef-core','41-migrations','42-relationships','43-tracking','44-concurrency','45-data-access','46-dapper','47-persistent-api']
if sys.argv[1:] not in ([], ['--build-only']):
    raise SystemExit('Usage: python scripts/ef_smoke.py [--build-only]')
for name in NAMES:
    subprocess.run(['dotnet','build',str(ROOT/'samples'/name/'SupportDesk.csproj'),'--nologo'],check=True)
if sys.argv[1:]:
    print('PASS: EF builds only; no database exercised.')
    raise SystemExit(0)
if not os.environ.get('SUPPORTDESK_PG'):
    raise SystemExit('Set SUPPORTDESK_PG before running live checks.')
for name in NAMES[:-1]:
    run = subprocess.run(['dotnet',str(ROOT/'samples'/name/'bin/Debug/net10.0/SupportDesk.dll')],text=True,capture_output=True,timeout=120)
    assert run.returncode == 0 and 'PASS: lesson scenario' in run.stdout, (name,run.stdout,run.stderr)
    print(name, run.stdout)

# psql is needed only by this integration runner to own and clean its private schema.
# A fresh randomly generated identifier is safe to interpolate. Never accept it from HTTP.
schema = 'api48_' + uuid.uuid4().hex
pg = os.environ['SUPPORTDESK_PG']
# CI/local test server settings. Use PG* to avoid placing passwords on a process command line.
# SUPPORTDESK_TEST_PG_* override defaults when the local lab uses a different port/role.
penv = os.environ.copy()
for key, default in [('PGHOST','127.0.0.1'),('PGPORT','5432'),('PGDATABASE','supportdesk_lab'),('PGUSER','course'),('PGPASSWORD','course-ci-only')]:
    penv[key] = os.environ.get('SUPPORTDESK_TEST_' + key, default)
def sql(text):
    return subprocess.run(['psql','-X','-v','ON_ERROR_STOP=1','-tA','-c',text],env=penv,check=True,text=True,capture_output=True).stdout.strip()
api = ROOT/'samples/47-persistent-api'
dll = api/'bin/Debug/net10.0/SupportDesk.dll'
cs = pg.rstrip(';') + ';Search Path=' + schema
path = '/api/tickets'
def send(base, route, method='GET', payload=None):
    body = None if payload is None else json.dumps(payload,ensure_ascii=False).encode('utf-8')
    status, headers, raw = request(base, route, method, body, {'Content-Type':'application/json'})
    return status, headers, json.loads(raw) if raw else None
sql(f'CREATE SCHEMA {schema}')
try:
    env = os.environ.copy(); env['SUPPORTDESK_PG'] = cs
    for _ in range(2):
        migrate = subprocess.run(['dotnet',str(dll),'--migrate'],cwd=api,env=env,text=True,capture_output=True,timeout=90)
        assert migrate.returncode == 0 and 'PASS: application migrations applied' in migrate.stdout, (migrate.stdout,migrate.stderr)
    with server('47-persistent-api','Production',{'SUPPORTDESK_PG':cs}) as base:
        assert send(base,'/health/ready')[0] == 200
        for title in [None,'  ','ab']:
            assert send(base,path,'POST',{'title':title})[0] == 400
        status,headers,ticket = send(base,path,'POST',{'title':'  Persistent ticket  '})
        assert status == 201 and ticket['title'] == 'Persistent ticket' and ticket['id'] > 0
        assert headers.get('Location') and headers.get('X-Request-ID')
        ident = ticket['id']; route = f'{path}/{ident}'
        assert send(base,route+'/status','PUT',{'status':'Closed'})[0] == 409
        # Concurrent note writes may return conflicts; no lost update or sixth note is allowed.
        def note(i): return send(base,route+'/notes','POST',{'text':f'Concurrent note {i}'})[0]
        with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
            statuses = list(pool.map(note,range(12)))
        assert all(s in (200,409) for s in statuses), statuses
        current = send(base,route)[2]
        assert len(current['notes']) == statuses.count(200) <= 5, (statuses,current)
        for i in range(5-len(current['notes'])):
            assert send(base,route+'/notes','POST',{'text':f'Fill note {i}'})[0] == 200
        assert send(base,route+'/notes','POST',{'text':'Sixth note'})[0] == 409
        assert send(base,route+'/status','PUT',{'status':'InProgress'})[0] == 200
        assert send(base,route+'/status','PUT',{'status':'Closed'})[0] == 200
        assert send(base,route+'/status','PUT',{'status':'Closed'})[0] == 200
        assert send(base,route+'/title','PUT',{'title':'Forbidden edit'})[0] == 409
        assert send(base,route+'/notes','POST',{'text':'Forbidden note'})[0] == 409
        assert send(base,path+'?page=2147483647&pageSize=50')[2]['items'] == []
        assert send(base,path+'?q=Persistent')[2]['totalCount'] == 1
    # A different process/context must see durable state, not a static in-memory dictionary.
    with server('47-persistent-api','Production',{'SUPPORTDESK_PG':cs}) as base:
        saved = send(base,route)[2]
        assert saved['status'] == 'Closed' and len(saved['notes']) == 5, saved
        assert send(base,route,'DELETE')[0] == 204
        assert send(base,route)[0] == 404
    # No migration on ordinary startup; missing schema is a readable readiness failure.
    absent = 'absent48_' + uuid.uuid4().hex
    with server('47-persistent-api','Production',{'SUPPORTDESK_PG':pg.rstrip(';')+';Search Path='+absent}) as base:
        assert send(base,'/health')[0] == 200
        assert send(base,'/health/ready')[0] == 503
        status,headers,problem = send(base,path)
        assert status == 503 and 'requestId' in problem and headers.get('X-Request-ID'), problem
        assert 'Password' not in json.dumps(problem) and 'SELECT' not in json.dumps(problem), problem
    # Unreachable database: server starts but readiness and request fail without leaking credentials.
    broken = 'Host=127.0.0.1;Port=1;Database=missing;Username=course;Password=do-not-leak;Timeout=1;Command Timeout=1'
    with server('47-persistent-api','Production',{'SUPPORTDESK_PG':broken}) as base:
        assert send(base,'/health/ready')[0] == 503
        code,headers,problem = send(base,path)
        assert code == 503 and 'do-not-leak' not in json.dumps(problem), problem
    print('PASS: durable API restart, concurrency, domain rules, migration repeat, database failure.')
finally:
    sql(f'DROP SCHEMA {schema} CASCADE')
