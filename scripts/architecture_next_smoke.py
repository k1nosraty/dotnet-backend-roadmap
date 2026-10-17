"""Check lessons 53-56: aggregate, adapter contracts, mapping and real feature HTTP.

Requires .NET SDK 10. PostgreSQL suites remain separate; these new adapters are educational.
"""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time
from urllib import request, error
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
NAMES = ['52-domain-aggregate','53-ports-adapters','54-explicit-mapping','55-vertical-slices']
SAMPLES = ROOT / 'samples'


def invoke(args, cwd=None):
    r = subprocess.run(args, cwd=cwd, capture_output=True, text=True,
                       encoding='utf-8', errors='replace', timeout=180)
    assert r.returncode == 0, (args, r.stdout, r.stderr)
    return r.stdout


# The same handlers, result mapping and Domain remain shared between the compared layouts.
a, b = SAMPLES / NAMES[2], SAMPLES / NAMES[3]
for source in (a / 'src').rglob('*.cs'):
    if {'obj', 'bin'} & set(source.parts):
        continue
    matches = [p for p in (b / 'src').rglob(source.name) if not {'obj', 'bin'} & set(p.parts)]
    assert len(matches) == 1 and source.read_bytes() == matches[0].read_bytes(), source
for name in NAMES:
    base = SAMPLES / name
    for part, allowed in [('Domain', set()), ('Application', {'../Domain/SupportDesk.Domain.csproj'}),
                          ('Infrastructure', {'../Domain/SupportDesk.Domain.csproj','../Application/SupportDesk.Application.csproj'})]:
        doc = ET.parse(base / f'src/{part}/SupportDesk.{part}.csproj')
        assert {x.attrib['Include'] for x in doc.findall('.//ProjectReference')} == allowed, (name, part)
        assert not doc.findall('.//PackageReference'), (name, part)
print('PASS: project boundaries and unchanged handlers in the feature layout.')
if not shutil.which('dotnet'):
    print('Install .NET SDK 10 to build and run the scenarios.', file=sys.stderr)
    raise SystemExit(2)
for name in NAMES:
    base = SAMPLES / name
    invoke(['dotnet','build',str(base/'SupportDesk.csproj'),'--nologo'])
    dll = base / 'bin/Debug/net10.0/SupportDesk.dll'
    output = invoke(['dotnet',str(dll),'--check'])
    assert 'PASS: architecture behavior' in output and 'PASS: aggregate invariants' in output, output
    if name != NAMES[0]:
        assert 'PASS: port contracts' in output, output
    if name in NAMES[2:]:
        assert 'PASS: explicit mapping' in output, output
    if name != NAMES[-1]:
        assert 'Success: 1 Closed notes=1' in invoke(['dotnet',str(dll)])
    print(f'PASS: {name} build and behavioral checks.')

with tempfile.TemporaryDirectory(prefix='architecture-next-') as directory:
    folder = Path(directory)
    for name in NAMES[1:3]:
        dll = SAMPLES/name/'bin/Debug/net10.0/SupportDesk.dll'
        path = folder/(name+'.json')
        first = invoke(['dotnet',str(dll),'--file',str(path)], cwd=folder)
        before = path.read_bytes()
        second = invoke(['dotnet',str(dll),'--file',str(path)], cwd=folder)
        assert 'Success:' in first and 'Conflict:' in second, (first,second)
        assert 'Stored: Closed notes=1 version=2' in second and path.read_bytes() == before, second
    print('PASS: JSON adapter survives independent process restart without reseeding.')

    published = folder/'published'
    invoke(['dotnet','publish',str(SAMPLES/NAMES[-1]/'SupportDesk.csproj'),'-c','Release','-o',str(published),'--nologo'])
    assert 'PASS: explicit mapping' in invoke(['dotnet',str(published/'SupportDesk.dll'),'--check'],cwd=folder)

    def send(base, path, body=None, method='POST'):
        data = None if body is None else json.dumps(body).encode('utf-8')
        req = request.Request(base+path, data=data, headers={'Content-Type':'application/json'}, method=method)
        try:
            with request.urlopen(req,timeout=15) as response:
                return response.status, json.loads(response.read())
        except error.HTTPError as ex:
            raw = ex.read()
            return ex.code, json.loads(raw) if raw else None

    with socket.socket() as sock:
        sock.bind(('127.0.0.1',0)); port=sock.getsockname()[1]
    base=f'http://127.0.0.1:{port}'
    env=os.environ.copy();env['ASPNETCORE_ENVIRONMENT']='Production'
    with (folder/'server.log').open('w',encoding='utf-8') as log:
        process=subprocess.Popen(['dotnet',str(published/'SupportDesk.dll'),'--urls',base],
                                 cwd=folder,env=env,stdout=log,stderr=subprocess.STDOUT)
        try:
            # Probe a missing resource; never mutate the fixture as a readiness check.
            for _ in range(200):
                if process.poll() is not None:
                    raise AssertionError((folder/'server.log').read_text())
                try:
                    if send(base,'/api/tickets/999/close')[0]==404: break
                except (OSError,error.URLError): time.sleep(0.1)
            else: raise AssertionError('HTTP server did not become ready.')
            assert send(base,'/api/tickets/1/close')[0] == 409  # InProgress without resolution
            assert send(base,'/api/tickets/1/resolve',{'resolution':'ab'})[0] == 400
            assert send(base,'/api/tickets/1/title',{'title':None},'PUT')[0] == 400
            status, body = send(base,'/api/tickets/1/title',{'title':'  New title  ','status':'Closed','id':99},'PUT')
            assert status==200 and body['title']=='New title' and body['status']=='InProgress', body
            assert set(body)=={'id','title','status','noteCount'}, body
            def resolve(i):
                return send(base,'/api/tickets/1/resolve',{'resolution':f'Fixed cable {i}'})[0]
            with ThreadPoolExecutor(max_workers=8) as pool:
                statuses=list(pool.map(resolve,range(8)))
            assert statuses.count(200)==1 and statuses.count(409)==7, statuses
            assert send(base,'/api/tickets/1/title',{'title':'Forbidden rename'},'PUT')[0]==409
            status, body=send(base,'/api/tickets/1/close')
            assert status==200 and body['status']=='Closed' and body['noteCount']==1, body
            assert send(base,'/api/tickets/no-number/close')[0]==404
        finally:
            process.terminate()
            try: process.wait(timeout=15)
            except subprocess.TimeoutExpired: process.kill();process.wait(timeout=15)
    print('PASS: published feature API, mapping, domain conflicts and concurrent HTTP resolve.')
