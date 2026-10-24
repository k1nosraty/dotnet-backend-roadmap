"""Only UUID-owned disposable PostgreSQL: dump/restore, pending-job recovery, bad candidate and rollback."""
from pathlib import Path
import json
import os
import shutil
import subprocess
import tempfile
import time
from urllib import error
from uuid import uuid4
from release_gate import serve, OPENER, digest

ROOT=Path(__file__).resolve().parents[1]
assert shutil.which('docker') and shutil.which('dotnet'),'Linux Docker and SDK 10 are required.'
name='course-recovery-'+uuid4().hex[:12]
env=os.environ.copy();env['POSTGRES_PASSWORD']=uuid4().hex

def run(args,expected=0,environment=None):
    result=subprocess.run(args,cwd=ROOT,env=environment,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=360)
    assert (result.returncode==0 if expected==0 else result.returncode!=0),(args,result.stdout,result.stderr)
    return result.stdout.strip()

def sql(database,text):
    return run(['docker','exec',name,'psql','-U','course','-d',database,'-At','-v','ON_ERROR_STOP=1','-c',text])

def status(base,path):
    try:
        with OPENER.open(base+path,timeout=5) as response:return response.status,response.read()
    except error.HTTPError as ex:return ex.code,ex.read()

def wait_ready(base,expected=200):
    deadline=time.monotonic()+20
    while time.monotonic()<deadline:
        if status(base,'/health/ready')[0]==expected:return
        time.sleep(.1)
    raise AssertionError('Readiness did not reach expected state.')

def wait_done(database,expected):
    deadline=time.monotonic()+20
    while time.monotonic()<deadline:
        if sql(database,"SELECT count(*) FROM jobs WHERE state='done'")==str(expected):return
        time.sleep(.1)
    raise AssertionError('Persisted jobs were not recovered.')

try:
    assert run(['docker','info','--format','{{.OSType}}'])=='linux'
    run(['docker','run','-d','--name',name,'-e','POSTGRES_USER=course','-e','POSTGRES_DB=jobs','-e','POSTGRES_PASSWORD',
         '-p','127.0.0.1::5432','postgres:18.6'],environment=env)
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        result=subprocess.run(['docker','exec',name,'pg_isready','-h','127.0.0.1','-U','course','-d','jobs'],capture_output=True)
        if result.returncode==0:break
        time.sleep(.2)
    else:raise AssertionError('PostgreSQL startup timeout.')
    port=json.loads(run(['docker','inspect',name]))[0]['NetworkSettings']['Ports']['5432/tcp'][0]['HostPort']
    connection=f'Host=127.0.0.1;Port={port};Database=jobs;Username=course;Password={env["POSTGRES_PASSWORD"]};Timeout=3;Command Timeout=3'
    lab_env=os.environ.copy();lab_env['SUPPORTDESK_JOBS']=connection
    run(['dotnet','build',str(ROOT/'samples/83-durable-jobs/SupportDesk.csproj'),'--nologo','-m:1'])
    cli=['dotnet',str(ROOT/'samples/83-durable-jobs/bin/Debug/net10.0/SupportDesk.Lesson84.dll')]
    for mode in ['init','seed','once']:run(cli+[mode],environment=lab_env)
    sql('jobs',"INSERT INTO jobs(id,state) VALUES('22222222-2222-2222-2222-222222222222','pending')")
    before=sql('jobs',"SELECT id,state,attempts FROM jobs ORDER BY id")
    effects_before=sql('jobs',"SELECT job_id,value FROM effects ORDER BY job_id")
    started=time.monotonic()
    run(['docker','exec',name,'pg_dump','-U','course','-d','jobs','-Fc','-f','/tmp/jobs.dump'])
    dump_seconds=time.monotonic()-started
    run(['docker','exec',name,'createdb','-U','course','restored'])
    started=time.monotonic()
    run(['docker','exec',name,'pg_restore','-U','course','-d','restored','--exit-on-error','--no-owner','/tmp/jobs.dump'])
    restore_seconds=time.monotonic()-started
    assert sql('restored',"SELECT id,state,attempts FROM jobs ORDER BY id")==before
    assert sql('restored',"SELECT job_id,value FROM effects ORDER BY job_id")==effects_before
    run(['docker','exec',name,'sh','-c','printf invalid > /tmp/bad.dump'])
    run(['docker','exec',name,'pg_restore','--list','/tmp/bad.dump'],expected=1)
    print(f'PASS: pg_dump/pg_restore roundtrip keeps job IDs, states, attempts and effects; corrupt archive rejected. dump={dump_seconds:.3f}s restore={restore_seconds:.3f}s',flush=True)
    with tempfile.TemporaryDirectory(prefix='worker-releases-') as directory:
        folder=Path(directory);artifacts={}
        for version in ['1.0.0','1.1.0']:
            output=folder/version
            run(['dotnet','publish',str(ROOT/'samples/85-worker-delivery/SupportDesk.csproj'),'-c','Release','-o',str(output),
                 '-p:UseAppHost=false','-p:CourseVersion='+version,'--nologo','-m:1'])
            artifacts[version]={p.relative_to(output).as_posix():digest(p) for p in output.rglob('*') if p.is_file()}
        restored=connection.replace('Database=jobs;','Database=restored;')
        settings={'SUPPORTDESK_JOBS':restored,'Maintenance':'false'}
        with serve(folder/'1.0.0','SupportDesk.Lesson86.dll',settings) as base:
            wait_ready(base);wait_done('restored',2)
            assert json.loads(status(base,'/version')[1])['version']=='1.0.0'
        sql('restored',"INSERT INTO jobs(id,state) VALUES('33333333-3333-3333-3333-333333333333','pending')")
        with serve(folder/'1.1.0','SupportDesk.Lesson86.dll',{**settings,'Maintenance':'true'}) as base:
            wait_ready(base,503);assert status(base,'/health/live')[0]==200
            assert json.loads(status(base,'/version')[1])['version']=='1.1.0'
            assert sql('restored',"SELECT count(*) FROM jobs WHERE state='pending'")=='1'
        assert {p.relative_to(folder/'1.0.0').as_posix():digest(p) for p in (folder/'1.0.0').rglob('*') if p.is_file()}==artifacts['1.0.0']
        with serve(folder/'1.0.0','SupportDesk.Lesson86.dll',settings) as base:
            wait_ready(base);wait_done('restored',3)
            assert json.loads(status(base,'/version')[1])['version']=='1.0.0'
        assert sql('restored',"SELECT count(*) FROM effects")=='3'
        assert sql('jobs',"SELECT id,state,attempts FROM jobs ORDER BY id")==before,'Source backup database was modified.'
        assert sql('jobs',"SELECT job_id,value FROM effects ORDER BY job_id")==effects_before
    print('PASS: restored pending jobs survive host restart; not-ready candidate stays idle; unchanged previous artifact resumes with one local DB effect per job.',flush=True)
finally:
    subprocess.run(['docker','rm','-f',name],capture_output=True,timeout=30)
