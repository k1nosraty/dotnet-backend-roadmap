"""Final four lessons: contracts, prototype boundary, build and optional real PostgreSQL suite."""
from pathlib import Path
import os
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from release_gate import serve, OPENER
ROOT=Path(__file__).resolve().parents[1]
if sys.argv[1:] not in [[],['--docker-only']]:raise SystemExit('Usage: capstone_smoke.py [--docker-only]')
assert shutil.which('dotnet'),'Install SDK 10.'

def run(command,expected=0,environment=None):
    result=subprocess.run(command,cwd=ROOT,env=environment,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=360)
    assert result.returncode==expected,(command,result.stdout,result.stderr)
    return result.stdout

def test(name,folder,minimum):
    results=folder/name
    run(['dotnet','test',str(ROOT/'tests'/name/'SupportDesk.Tests.csproj'),'--nologo','-m:1','--logger','trx;LogFileName=results.trx','--results-directory',str(results)])
    doc=ET.parse(results/'results.trx');counts=doc.find('.//{*}Counters');assert counts is not None
    total=int(counts.get('total','0'));assert total>=minimum and int(counts.get('executed','0'))==total
    assert int(counts.get('passed','0'))==total and int(counts.get('failed','0'))==0
    rows=doc.findall('.//{*}UnitTestResult');assert len(rows)==total and all(row.get('outcome')=='Passed' for row in rows)
    print(f'PASS: {name}: {total} executed; zero failures/skips.',flush=True)

with tempfile.TemporaryDirectory(prefix='capstone-') as directory:
    folder=Path(directory)
    if sys.argv[1:]==['--docker-only']:
        assert shutil.which('docker') and run(['docker','info','--format','{{.OSType}}']).strip()=='linux','Running Linux Docker required; no skip.'
        test('089-capstone',folder,9)
    else:
        test('087-contract',folder,8);test('088-prototype',folder,2)
        assert run(['dotnet','run','--project',str(ROOT/'samples/86-capstone-contract/SupportDesk.csproj')]).strip()=='state=Open; version=4; reopens=1; dueHours=4'
        run(['dotnet','build',str(ROOT/'tests/089-capstone/SupportDesk.Tests.csproj'),'--nologo','-m:1'])
        for n,name in [(88,'87-servicedesk-prototype'),(89,'88-servicedesk-hardening'),(90,'89-servicedesk-delivery')]:
            output=folder/str(n)
            run(['dotnet','publish',str(ROOT/'samples'/name/'SupportDesk.csproj'),'-c','Release','-o',str(output),'-p:UseAppHost=false','--nologo','-m:1'])
            assert not any('xunit' in p.name.lower() or p.name.endswith('.Tests.dll') for p in output.iterdir())
            if n==88:
                from urllib import request,error
                with serve(output,'SupportDesk.Lesson88.dll') as base:
                    try:OPENER.open(request.Request(base+'/requests/'+str(__import__('uuid').uuid4()),headers={'X-Test-Actor':'agent'}),timeout=5)
                    except error.HTTPError as ex:assert ex.code==401
                    else:raise AssertionError('Published prototype accepted a test identity.')
            else:
                # Invalid configuration must not start a normal public host or print the rejected connection.
                environment=os.environ.copy();environment.update({'Desk__Connection':'Password=DO_NOT_LOG','Desk__Keys':str(folder/'keys'),
                    'ASPNETCORE_ENVIRONMENT':'Production','DOTNET_ENVIRONMENT':'Production','Desk__LabHttp':'false'})
                result=subprocess.run(['dotnet',str(output/f'SupportDesk.Lesson{n}.dll')],cwd=folder,env=environment,
                    capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=30)
                assert result.returncode!=0,'Invalid configuration started a host.'
                assert 'DO_NOT_LOG' not in result.stdout+result.stderr,'Configuration leaked into startup diagnostics.'
        print('PASS: final snapshots publish without test assemblies; prototype denies fake identity; invalid secure-host configuration fails without leaking secrets.')
        print('NOT RUN: real Identity/PostgreSQL and TLS deployment; use --docker-only and capstone_delivery.py with Docker.')
