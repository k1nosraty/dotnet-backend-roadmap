"""Check lessons 57-60: read/write ports, validators, dependency rules and TeamDesk delivery."""
from pathlib import Path
from contextlib import contextmanager
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

ROOT=Path(__file__).resolve().parents[1]
NAMES=['56-command-query','57-fluent-validation','58-architecture-tests','59-teamdesk']
SAMPLES=ROOT/'samples'


def invoke(args,cwd=None):
    result=subprocess.run(args,cwd=cwd,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
    assert result.returncode==0,(args,result.stdout,result.stderr)
    return result.stdout


def architecture(base):
    policy={
        'Domain':set(),
        'Application':{'../Domain/SupportDesk.Domain.csproj'},
        'Infrastructure':{'../Domain/SupportDesk.Domain.csproj','../Application/SupportDesk.Application.csproj'},
    }
    for layer,allowed in policy.items():
        doc=ET.parse(base/f'src/{layer}/SupportDesk.{layer}.csproj')
        refs={x.attrib['Include'] for x in doc.findall('.//ProjectReference')}
        assert refs==allowed,(layer,refs)
        packages={x.attrib['Include']:x.attrib['Version'] for x in doc.findall('.//PackageReference')}
        expected={'FluentValidation':'12.2.0'} if layer=='Application' and base.name!='56-command-query' else {}
        assert packages==expected,(layer,packages)
    adrs=base/'docs/adr'
    if base.name in NAMES[2:]:
        assert len(list(adrs.glob('*.md')))>=3,'Missing ADRs'
        for p in adrs.glob('*.md'):
            text=p.read_text(encoding='utf-8')
            for section in ['وضعیت','زمینه','تصمیم','جایگزین','پیامد']:
                assert section in text,(p,section)


for name in NAMES: architecture(SAMPLES/name)
with tempfile.TemporaryDirectory(prefix='architecture-policy-') as directory:
    base=Path(directory)/'58-architecture-tests'
    shutil.copytree(SAMPLES/NAMES[2],base,ignore=shutil.ignore_patterns('obj','bin'))
    project=base/'src/Domain/SupportDesk.Domain.csproj'
    text=project.read_text(encoding='utf-8')
    project.write_text(text.replace('</Project>','<ItemGroup><ProjectReference Include="../Infrastructure/SupportDesk.Infrastructure.csproj" /></ItemGroup></Project>'),encoding='utf-8')
    rejected=False
    try: architecture(base)
    except AssertionError: rejected=True
    assert rejected,'Policy accepted forbidden Domain reference'
print('PASS: declared project/package policy, ADR completeness and deliberate violation rejection.')
if not shutil.which('dotnet'):
    print('Install .NET SDK 10 to run build/HTTP checks.',file=sys.stderr);raise SystemExit(2)
for name in NAMES:
    base=SAMPLES/name
    invoke(['dotnet','build',str(base/'SupportDesk.csproj'),'--nologo'])
    output=invoke(['dotnet',str(base/'bin/Debug/net10.0/SupportDesk.dll'),'--check'])
    assert 'PASS: command/query separation' in output,output
    if name!=NAMES[0]: assert 'PASS: FluentValidation boundaries' in output,output
    if name in NAMES[2:]: assert 'PASS: compiled assembly dependency boundaries' in output,output
    if name==NAMES[-1]: assert 'PASS: TeamDesk lifecycle' in output,output
    print(f'PASS: {name} build and behavior.')


def send(base,path,body=None,method='GET'):
    data=None if body is None else json.dumps(body).encode('utf-8')
    req=request.Request(base+path,data=data,headers={'Content-Type':'application/json'},method=method)
    try:
        with request.urlopen(req,timeout=15) as response:
            raw=response.read();return response.status,json.loads(raw) if raw else None,dict(response.headers)
    except error.HTTPError as ex:
        raw=ex.read();return ex.code,json.loads(raw) if raw else None,dict(ex.headers)


@contextmanager
def server(dll,folder,data_path=None):
    with socket.socket() as sock:
        sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
    base=f'http://127.0.0.1:{port}'
    env=os.environ.copy();env['ASPNETCORE_ENVIRONMENT']='Production';env.pop('TEAMDESK_DATA',None)
    if data_path is not None:env['TEAMDESK_DATA']=str(data_path)
    with (folder/'server.log').open('w',encoding='utf-8') as log:
        process=subprocess.Popen(['dotnet',str(dll),'--urls',base],cwd=folder,env=env,stdout=log,stderr=subprocess.STDOUT)
        try:
            for _ in range(200):
                if process.poll() is not None:raise AssertionError((folder/'server.log').read_text(encoding='utf-8'))
                try:
                    if send(base,'/api/tickets/999')[0]==404:break
                except (OSError,error.URLError):time.sleep(0.1)
            else:raise AssertionError('Server readiness timeout.')
            try:
                yield base
            except Exception:
                log.flush()
                print((folder/'server.log').read_text(encoding='utf-8'), file=sys.stderr)
                raise
        finally:
            process.terminate()
            try:process.wait(timeout=15)
            except subprocess.TimeoutExpired:process.kill();process.wait(timeout=15)


with tempfile.TemporaryDirectory(prefix='teamdesk-delivery-') as directory:
    folder=Path(directory)
    # HTTP checks for every snapshot, not just the final delivery.
    for name in NAMES:
        dll=SAMPLES/name/'bin/Debug/net10.0/SupportDesk.dll'
        with server(dll,folder) as base:
            initial=send(base,'/api/tickets')[1]
            for _ in range(3):
                assert send(base,'/api/tickets')[1]==initial
            assert initial['total']==1 and initial['items'][0]['id']==1,initial
            assert send(base,'/api/tickets?page=2147483647&pageSize=50')[1]['items']==[]
            assert send(base,'/api/tickets?page=0')[0]==400
            assert send(base,'/api/tickets?status=Unknown')[0]==400
            assert send(base,'/api/tickets/0')[0]==400
            status,body,_=send(base,'/api/tickets/0/resolve',{'resolution':'ab'},'POST')
            assert status==400,body
            if name!=NAMES[0]:assert set(body['errors'])=={'Id','Resolution'},body
            assert send(base,'/api/tickets/1')[1]==initial['items'][0]
        print(f'PASS: {name} real read-only HTTP and validation.')

    # Validate coverage with an UNFILTERED inventory. A clean --vulnerable report
    # legitimately omits frameworks that have no vulnerable packages.
    package_command = ['dotnet', 'list', str(SAMPLES/NAMES[-1]/'SupportDesk.csproj'), 'package']
    inventory = json.loads(invoke(package_command + ['--include-transitive', '--format', 'json', '--output-version', '1']))
    report = json.loads(invoke(package_command + ['--vulnerable', '--include-transitive', '--format', 'json', '--output-version', '1']))
    for result in [inventory, report]:
        assert result.get('version') == 1 and result.get('projects'), 'Incomplete NuGet report.'
        assert not result.get('errors'), result
        for problem in result.get('problems', []):
            assert str(problem.get('severity', '')).lower() != 'error', problem
    inventory_paths = {str(Path(p['path']).resolve()) for p in inventory['projects']}
    assert {str(Path(p['path']).resolve()) for p in report['projects']} == inventory_paths
    packages = []
    for project in inventory['projects']:
        assert project.get('frameworks'), 'Unfiltered dependency inventory has no frameworks.'
        for framework in project['frameworks']:
            for group in ['topLevelPackages', 'transitivePackages']:
                packages.extend(framework.get(group, []))
    assert any(p.get('id') == 'FluentValidation' and p.get('resolvedVersion') == '12.2.0' for p in packages), packages
    for project in report['projects']:
        for framework in project.get('frameworks', []):
            for group in ['topLevelPackages', 'transitivePackages']:
                assert not any(p.get('vulnerabilities') for p in framework.get(group, [])), framework
    print('PASS: NuGet vulnerability report has no reported vulnerable dependencies.')

    output=folder/'published'
    invoke(['dotnet','publish',str(SAMPLES/NAMES[-1]/'SupportDesk.csproj'),'-c','Release','-o',str(output),'--nologo'])
    dll=output/'SupportDesk.dll'
    assert 'PASS: TeamDesk lifecycle' in invoke(['dotnet',str(dll),'--check'],cwd=folder)
    path=folder/'teamdesk.json'
    with server(dll,folder,path) as base:
        assert send(base,'/api/tickets/1/title',{'title':'Renamed seed'},'PUT')[0]==200
        assert send(base,'/api/tickets/1/notes',{'text':'ab'},'POST')[0]==400
        assert send(base,'/api/tickets/1/notes',{'text':'First note'},'POST')[0]==200
        assert send(base,'/api/tickets/1/notes',{'text':'  First note  '},'POST')[0]==409
        for i in range(4):
            assert send(base,'/api/tickets/1/notes',{'text':f'Extra note {i}'},'POST')[0]==200
        assert send(base,'/api/tickets/1/notes',{'text':'Sixth note'},'POST')[0]==409
        assert send(base,'/api/tickets/1/status',{'status':'Unknown'},'PUT')[0]==400
        assert send(base,'/api/tickets/1/close',method='POST')[0]==200
        assert send(base,'/api/tickets/1/title',{'title':'Blocked rename'},'PUT')[0]==409
        assert send(base,'/api/tickets/1/notes',{'text':'Blocked note'},'POST')[0]==409
        assert send(base,'/api/tickets/1')[1]['noteCount']==5
        assert send(base,'/api/tickets',{'title':'ab'},'POST')[0]==400
        def create(i):return send(base,'/api/tickets',{'title':f'Network {i}'},'POST')
        with ThreadPoolExecutor(max_workers=8) as pool:created=list(pool.map(create,range(8)))
        assert all(x[0]==201 for x in created),created
        ids=[x[1]['id'] for x in created]
        assert len(set(ids))==8,ids
        for status,body,headers in created:
            location=next(v for k,v in headers.items() if k.lower()=='location')
            assert send(base,location)[1]==body
        target=min(ids)  # Preserve this resource while deleting the highest identity.
        assert send(base,f'/api/tickets/{target}/resolve',{'resolution':'Fixed cable'},'POST')[0]==409
        assert send(base,f'/api/tickets/{target}/status',{'status':'InProgress'},'PUT')[0]==200
        def resolve(i):return send(base,f'/api/tickets/{target}/resolve',{'resolution':f'Fixed cable {i}'},'POST')[0]
        with ThreadPoolExecutor(max_workers=8) as pool:statuses=list(pool.map(resolve,range(8)))
        assert statuses.count(200)==1 and statuses.count(409)==7,statuses
        saved=send(base,f'/api/tickets/{target}')[1]
        assert saved['status']=='Closed' and saved['noteCount']==1,saved
        removed=max(ids)
        assert send(base,f'/api/tickets/{removed}',method='DELETE')[0]==204
        assert send(base,f'/api/tickets/{removed}')[0]==404
        before_restart=send(base,'/api/tickets')[1]
        assert before_restart['total']==8  # initial seed + 8 creates - 1 delete
        prior_bytes=path.read_bytes()
        for _ in range(3):assert send(base,'/api/tickets')[1]==before_restart
        assert path.read_bytes()==prior_bytes,'Reads rewrote persistent state'
    with server(dll,folder,path) as base:
        assert send(base,'/api/tickets')[1]==before_restart
        assert send(base,f'/api/tickets/{target}')[1]==saved
        later=send(base,'/api/tickets',{'title':'After restart'},'POST')
        assert later[0]==201 and later[1]['id']>max(ids),later
        path.write_text('{broken',encoding='utf-8')
        status,body,_=send(base,'/api/tickets')
        assert status==500 and 'broken' not in json.dumps(body),body
        assert path.read_text(encoding='utf-8')=='{broken'
    assert not list(folder.glob('*.tmp')),'Leaked temporary file'
    print('PASS: published TeamDesk CRUD, concurrent creates/resolves, restart, stable IDs and safe corrupt-storage failure.')
