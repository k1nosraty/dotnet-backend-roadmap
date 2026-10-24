"""Lessons 83-86: deterministic cache tests, contracts, bounded load, and optional real data services."""
from pathlib import Path
import json
import os
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from release_gate import serve, OPENER
from load_lab import measure, percentile, passes, validate_base

ROOT=Path(__file__).resolve().parents[1]
if sys.argv[1:] not in [[],['--with-docker'],['--docker-only']]:raise SystemExit('Usage: operations_smoke.py [--with-docker|--docker-only]')
assert shutil.which('dotnet'),'Install .NET SDK 10.'

def run(command, expected=0, env=None):
    result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=360)
    assert result.returncode==expected,(command,result.stdout,result.stderr)
    return result.stdout.strip()

def test(suite,folder,minimum,filter=None):
    result=folder/suite
    command=['dotnet','test',str(ROOT/'tests'/suite/'SupportDesk.Tests.csproj'),'--nologo','-m:1',
             '--logger','trx;LogFileName=result.trx','--results-directory',str(result)]
    if filter:command+=['--filter',filter]
    run(command)
    doc=ET.parse(result/'result.trx'); counts=doc.find('.//{*}Counters');assert counts is not None
    total=int(counts.get('total','0'));assert total>=minimum
    assert int(counts.get('executed','0'))==total and int(counts.get('passed','0'))==total and int(counts.get('failed','0'))==0
    rows=doc.findall('.//{*}UnitTestResult');assert len(rows)==total and all(row.get('outcome')=='Passed' for row in rows)
    print(f'PASS: {suite}: {total} executed, zero failed/skipped.',flush=True)

with tempfile.TemporaryDirectory(prefix='operations-') as directory:
    folder=Path(directory)
    if sys.argv[1:]!=['--docker-only']:
        test('083-cache',folder,7,'Category!=Docker')
        test('085-performance',folder,8)
        run(['dotnet','build',str(ROOT/'tests/084-jobs/SupportDesk.Tests.csproj'),'--nologo','-m:1'])
        run(['dotnet','build',str(ROOT/'samples/85-worker-delivery/SupportDesk.csproj'),'--nologo','-m:1'])
        env=os.environ.copy();env.pop('SUPPORTDESK_REDIS',None);env.pop('SUPPORTDESK_JOBS',None)
        output=run(['dotnet','run','--no-build','--project',str(ROOT/'samples/82-versioned-cache/SupportDesk.csproj')],env=env)
        assert output=='first=PRINTER; second=PRINTER; updated=NETWORK; builds=2',output
        run(['dotnet',str(ROOT/'samples/85-worker-delivery/bin/Debug/net10.0/SupportDesk.Lesson86.dll')],expected=2,env=env)
        # Test nearest-rank math and budgets independently of host speed.
        assert percentile([1,2,3,4,100],.95)==100 and percentile([1,2,3,4],.5)==2
        assert not passes({'latencyAllMs':{'p95':201},'errorRate':0},200,0)
        assert not passes({'latencyAllMs':{'p95':1},'errorRate':.01},200,0)
        for invalid in ['https://example.com','http://example.com','http://localhost/other','http://user@localhost']:
            try:validate_base(invalid)
            except ValueError:pass
            else:raise AssertionError('Non-loopback workload accepted.')
        published=folder/'performance'
        run(['dotnet','publish',str(ROOT/'samples/84-performance/SupportDesk.csproj'),'-c','Release','-o',str(published),'-p:UseAppHost=false','--nologo','-m:1'])
        with serve(published,'SupportDesk.Lesson85.dll') as base:
            for algorithm in ['loop','formula']:
                report=measure(base,algorithm,1_000_000,24,2)
                assert report['successful']==24 and report['failed']==0 and passes(report,5000,0),report
                print('OBSERVED: '+json.dumps(report),flush=True)
        print('PASS: bounded Release load returns correct values; CI uses a generous 5s p95 gate, not a production SLA.',flush=True)
        print('NOT RUN: real Redis/PostgreSQL and backup/restore unless a Docker option is selected.',flush=True)
    if sys.argv[1:] in [['--with-docker'],['--docker-only']]:
        assert shutil.which('docker') and run(['docker','info','--format','{{.OSType}}'])=='linux','Running Linux Docker is required; no skip fallback.'
        test('083-cache',folder,2,'Category=Docker')
        test('084-jobs',folder,6)
        print(run([sys.executable,str(ROOT/'scripts/operations_recovery.py')]),flush=True)
