"""JWT resource-server and real OIDC-handler tests; IdP transport is controlled, not a public service."""
from pathlib import Path
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
assert shutil.which('dotnet'),'Install .NET SDK 10.'
def run(args):
    result=subprocess.run(args,cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=360)
    assert result.returncode==0,(args,result.stdout,result.stderr)
    return result.stdout
with tempfile.TemporaryDirectory(prefix='identity-labs-') as directory:
    folder=Path(directory)
    for name,minimum in [('JwtApi',16),('OidcClient',12)]:
        results=folder/(name+'-results')
        run(['dotnet','test',f'supplements/identity/{name}.Tests/{name}.Tests.csproj','--nologo','-m:1',
            '--logger','trx;LogFileName=results.trx','--results-directory',str(results)])
        doc=ET.parse(results/'results.trx');counts=doc.find('.//{*}Counters');assert counts is not None
        total=int(counts.get('total','0'))
        assert total>=minimum and int(counts.get('passed','0'))==total and int(counts.get('executed','0'))==total
        assert all(row.get('outcome')=='Passed' for row in doc.findall('.//{*}UnitTestResult'))
        print(f'PASS: {name}: {total} executed, zero failures/skips.',flush=True)
        output=folder/(name+'-publish')
        run(['dotnet','publish',f'supplements/identity/{name}/{name}.csproj','-c','Release','-o',str(output),'-p:UseAppHost=false','--nologo','-m:1'])
        assert not any('xunit' in p.name.lower() or p.name.endswith('.Tests.dll') for p in output.iterdir())
    print('PASS: both identity hosts publish without test issuers or test assemblies.')
    print('NOT RUN: external IdP, browser, real TLS or multi-instance key storage. OIDC uses a controlled test-only IdP backchannel.')
