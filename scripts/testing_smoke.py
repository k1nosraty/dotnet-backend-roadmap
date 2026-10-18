"""Lessons 71-74: execute xUnit, real WAF requests, a detected mutation and normal-host denial.
Default mode runs 71-73 and BUILDS (but does not run) the Docker suite.
--with-docker also executes 74. --docker-only executes only the real PostgreSQL container suite.
"""
from pathlib import Path
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
from urllib import request, error
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
PROJECTS=['071-unit-tests','072-test-design','073-http-testing','074-database-testing']
if sys.argv[1:] not in ([],['--with-docker'],['--docker-only']):raise SystemExit('Usage: testing_smoke.py [--with-docker|--docker-only]')
if not shutil.which('dotnet'):raise SystemExit('Install .NET SDK 10.')

def invoke(args, *, cwd=None):
    p=subprocess.run(args,cwd=cwd,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=360)
    assert p.returncode==0,(args,p.stdout,p.stderr)
    return p.stdout

def counters(path, *, minimum, expect_failure=False):
    doc=ET.parse(path)
    row=doc.find('.//{*}Counters')
    assert row is not None,'Test runner did not produce counters.'
    total=int(row.get('total','0'));failed=int(row.get('failed','0'));passed=int(row.get('passed','0'))
    assert total>=minimum and int(row.get('executed','0'))==total,'No tests, discovery failure or skipped cases.'
    assert failed==(1 if expect_failure else 0) and passed+failed==total,'Unexpected failed or skipped test.'
    results=doc.findall('.//{*}UnitTestResult')
    assert len(results)==total and all(r.get('outcome') in ['Passed','Failed'] for r in results)
    return total

def test(project, folder, minimum):
    result=folder/project;result.mkdir()
    invoke(['dotnet','test',str(ROOT/'tests'/project/'SupportDesk.Tests.csproj'),'--nologo','-m:1',
        '--logger','trx;LogFileName=results.trx','--results-directory',str(result)])
    total=counters(result/'results.trx',minimum=minimum)
    print(f'PASS: {project}: {total} executed, zero failed/skipped (TRX verified).')

with tempfile.TemporaryDirectory(prefix='course-tests-') as directory:
    folder=Path(directory)
    if sys.argv[1:]!=['--docker-only']:
        for project,minimum in zip(PROJECTS[:3],[17,27,8]):test(project,folder,minimum)
        invoke(['dotnet','build',str(ROOT/'tests'/PROJECTS[3]/'SupportDesk.Tests.csproj'),'--nologo','-m:1'])
        # Prove a real assertion detects a weakened production boundary. Only a disposable copy changes.
        mutant=folder/'mutant'
        for relative in ['samples/70-unit-tests','tests/071-unit-tests']:
            shutil.copytree(ROOT/relative,mutant/relative,ignore=shutil.ignore_patterns('bin','obj','TestResults'))
        rules=mutant/'samples/70-unit-tests/Domain/TextRules.cs'
        text=rules.read_text(encoding='utf-8');assert text.count('length >= 3')==1
        rules.write_text(text.replace('length >= 3','length >= 2'),encoding='utf-8')
        output=folder/'mutation-results'
        p=subprocess.run(['dotnet','test',str(mutant/'tests/071-unit-tests/SupportDesk.Tests.csproj'),'--nologo','-m:1',
            '--filter','FullyQualifiedName~Rejects_too_short_title','--logger','trx;LogFileName=mutation.trx',
            '--results-directory',str(output)],capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
        assert p.returncode!=0,'Weakened guard was not detected.'
        counters(output/'mutation.trx',minimum=1,expect_failure=True)
        print('PASS: deliberate minimum-length regression fails the intended assertion in a temporary copy.')
        # WAF identity must never work against the published application's actual closed gate.
        published=folder/'published'
        invoke(['dotnet','publish',str(ROOT/'samples/72-http-testing/SupportDesk.csproj'),'-c','Release','-o',str(published),'--nologo','-m:1'])
        assert not any('xunit' in p.name.lower() or 'Testcontainers' in p.name or p.name.endswith('.Tests.dll') for p in published.iterdir())
        env=os.environ.copy();env.update({'ASPNETCORE_ENVIRONMENT':'Production','DOTNET_ENVIRONMENT':'Production'})
        opener=request.build_opener(request.ProxyHandler({}))
        def get(url,headers=None):
            try:
                with opener.open(request.Request(url,headers=headers or {}),timeout=10) as response:return response.status
            except error.HTTPError as ex:return ex.code
        with (folder/'host.log').open('w',encoding='utf-8') as log:
            host=subprocess.Popen(['dotnet',str(published/'SupportDesk.Lesson73.dll'),'--urls','http://127.0.0.1:0'],
                cwd=folder,env=env,stdout=log,stderr=subprocess.STDOUT)
            try:
                for _ in range(200):
                    logs=(folder/'host.log').read_text(encoding='utf-8',errors='replace')
                    assert host.poll() is None,'Published host exited.'
                    match=re.search(r'Now listening on: (http://127\.0\.0\.1:\d+)',logs)
                    if match and get(match.group(1)+'/health')==200:break
                    time.sleep(.1)
                else:raise AssertionError('Published host startup timeout.')
                for headers in [{},{'X-Test-Actor':'alice'},{'Authorization':'Bearer arbitrary-text','X-Test-Actor':'alice'}]:
                    assert get(match.group(1)+'/api/tickets/11111111-1111-1111-1111-111111111111',headers)==401
            finally:
                host.terminate()
                try:host.wait(timeout=15)
                except subprocess.TimeoutExpired:host.kill();host.wait(timeout=10)
        print('PASS: published host rejects test-only identities and contains no test runner assemblies.')
    if sys.argv[1:] in (['--with-docker'],['--docker-only']):
        assert shutil.which('docker'),'Docker CLI and a running Linux container engine are required.'
        assert invoke(['docker','info','--format','{{.OSType}}']).strip()=='linux','Switch Docker to Linux containers.'
        test(PROJECTS[3],folder,6)
        print('PASS: PostgreSQL 18.6 container tests actually executed; no external SUPPORTDESK_PG is used.')
    else:
        print('NOT RUN: lesson 74 requires Docker; only its build was verified. Use --with-docker.')
