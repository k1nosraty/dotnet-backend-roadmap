"""Verify all nine controller snapshots, API contracts and a published final API."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from contextlib import contextmanager
import json
import os
import subprocess
import tempfile
import time
from http_checks import ROOT, server, request, get_json

SAMPLES = ['25-controllers', '26-dto-mapping', '27-validation', '28-error-pipeline',
           '29-options', '30-request-scopes', '31-rest-pagination', '32-openapi', '33-supportdesk-api']
for name in SAMPLES:
    subprocess.run(['dotnet', 'build', str(ROOT / 'samples' / name / 'SupportDesk.csproj'), '--nologo'], check=True)

def send(base, path='/api/tickets', method='POST', value=None, expected=201):
    body = json.dumps(value, ensure_ascii=False).encode('utf-8')
    status, headers, data = request(base, path, method, body, {'Content-Type': 'application/json'})
    assert status == expected, (path, method, value, status, data)
    return headers, json.loads(data) if data else None

def make(base, title='Network issue', **extra):
    return send(base, value={'title': title, **extra})

def rows(base):
    data = get_json(base, '/api/tickets')
    return data['items'] if isinstance(data, dict) else data

def problem(base, path, expected):
    status, headers, data = request(base, path)
    assert status == expected and 'application/problem+json' in headers.get('Content-Type', ''), (status, headers, data)
    value = json.loads(data)
    assert value['status'] == expected and value.get('title'), value
    return value

for i, name in enumerate(SAMPLES):
    with server(name) as base:
        assert rows(base) == []
        headers, first = make(base, '  مشکل شبکه  ', id=999, status='Closed', internalSecret='ignored')
        assert first['id'] == 1 and first['title'] == 'مشکل شبکه', first
        location = headers['Location']
        assert location.endswith('/api/tickets/1') and get_json(base, location.replace(base, '')) == first
        assert set(first) == ({'id', 'title', 'status', 'notes'} if i == 8 else {'id', 'title'}), first
        if i == 8:
            assert first['status'] == 'Open' and first['notes'] == []
        send(base, value={'title': 'ab'}, expected=400)
        send(base, value={'title': ' ' * 10}, expected=400)
        assert len(rows(base)) == 1
        assert request(base, '/api/tickets/abc')[0] == 404
        problem(base, '/api/tickets/999', 404)
        problem(base, '/api/tickets/0', 400)
        if i >= 1:
            _, renamed = send(base, '/api/tickets/1/title', 'PUT', {'title': '  Changed title  ', 'id': 77}, 200)
            assert renamed['id'] == 1 and renamed['title'] == 'Changed title'
            _, repeated = send(base, '/api/tickets/1/title', 'PUT', {'title': 'Changed title'}, 200)
            assert repeated == renamed
            send(base, '/api/tickets/999/title', 'PUT', {'title': 'Missing title'}, 404)
        if i >= 2:
            for raw in [b'{bad', b'null', b'{}', b'{"title":null}', b'{"title":42}', b'{"title":"ab"}', b'{"title":"' + b'x'*121 + b'"}']:
                status, hdr, data = request(base, '/api/tickets', 'POST', raw, {'Content-Type': 'application/json'})
                assert status == 400 and 'application/problem+json' in hdr.get('Content-Type', ''), (raw, status, data)
                assert json.loads(data)['errors'], data
            status, hdr, data = request(base, '/api/tickets', 'POST', headers={'Content-Type': 'application/json'})
            assert status == 400, (status, hdr, data)
            assert request(base, '/api/tickets', 'POST', b'abc', {'Content-Type': 'text/plain'})[0] == 415
            assert len(rows(base)) == 1
        if i >= 3:
            problem(base, '/no-route', 404)
            assert request(base, '/health')[1].get('X-Request-ID')
            assert request(base, '/diagnostics/failure')[0] == 404
        if i >= 5:
            assert request(base, '/diagnostics/scope')[0] == 404
        assert request(base, '/api/tickets/1', 'DELETE')[0] == 204
        assert request(base, '/api/tickets/1', 'DELETE')[0] == 404
        assert rows(base) == []
    print(f'PASS: {name} contract, validation and mutation boundaries.')

# Known failures are handled even in Development; private details stay in server logs.
with server('28-error-pipeline', environment='Development') as base:
    status, headers, data = request(base, '/diagnostics/failure')
    value = json.loads(data)
    assert status == 500 and 'application/problem+json' in headers.get('Content-Type', '')
    assert value['requestId'] == headers['X-Request-ID'], (value, dict(headers))
    assert b'course-internal-sentinel' not in data and b'stackTrace' not in data
    assert get_json(base, '/health')['status'] == 'ok'

for name in ['29-options', '30-request-scopes']:
    with server(name, overrides={'Tickets__DefaultPageSize': '3', 'Tickets__MaxPageSize': '8'}) as base:
        assert get_json(base, '/api/tickets/settings') == {'defaultPageSize': 3, 'maxPageSize': 8}
# Invalid options stop startup, including in Production.
for overrides in [{'Tickets__DefaultPageSize': '0'}, {'Tickets__MaxPageSize': '101'},
                  {'Tickets__DefaultPageSize': '60', 'Tickets__MaxPageSize': '50'}]:
    project = ROOT / 'samples/29-options'
    env = os.environ.copy()
    env.update(overrides, DOTNET_ENVIRONMENT='Production', ASPNETCORE_ENVIRONMENT='Production')
    result = subprocess.run(['dotnet', str(project/'bin/Debug/net10.0/SupportDesk.dll'), '--urls', 'http://127.0.0.1:0'],
        cwd=project, env=env, text=True, capture_output=True, timeout=20)
    assert result.returncode != 0 and 'OptionsValidationException' in result.stderr + result.stdout

with server('30-request-scopes', environment='Development') as base:
    first = get_json(base, '/diagnostics/scope')
    second = get_json(base, '/diagnostics/scope')
    assert first['direct'] == first['viaService'] and second['direct'] == second['viaService']
    assert first['direct'] != second['direct']
    make(base)
    assert len(rows(base)) == 1  # Store survives the request scope.

for name in SAMPLES[6:]:
    with server(name, overrides={'Tickets__DefaultPageSize': '2', 'Tickets__MaxPageSize': '5'}) as base:
        for title in ['Network alpha', 'Printer beta', 'Network gamma', 'Network delta']:
            make(base, title)
        default = get_json(base, '/api/tickets')
        assert default['pageSize'] == 2 and len(default['items']) == 2 and default['totalCount'] == 4
        p1 = get_json(base, '/api/tickets?q=network&page=1&pageSize=2')
        p2 = get_json(base, '/api/tickets?q=network&page=2&pageSize=2')
        assert p1['totalCount'] == p2['totalCount'] == 3
        assert [t['id'] for t in p1['items']] == [1,3] and [t['id'] for t in p2['items']] == [4]
        assert get_json(base, '/api/tickets?page=2147483647&pageSize=5')['items'] == []
        assert get_json(base, '/api/tickets?q=absent')['totalCount'] == 0
        for suffix in ['?page=0','?pageSize=0','?pageSize=6','?page=abc','?q='+'x'*121]:
            problem(base, '/api/tickets'+suffix, 400)
        assert request(base, '/openapi/v1.json')[0] == 404  # Development only.

for name in SAMPLES[7:]:
    with server(name, environment='Development') as base:
        document = get_json(base, '/openapi/v1.json')
        assert document['openapi'].startswith('3.')
        paths = document['paths']
        assert 'get' in paths['/api/tickets'] and 'post' in paths['/api/tickets']
        assert '201' in paths['/api/tickets']['post']['responses']
        assert '404' in paths['/api/tickets/{id}']['get']['responses']
        assert all('/diagnostics/' not in path for path in paths)
        assert 'schemas' in document['components']
        if name == SAMPLES[-1]:
            assert '/api/tickets/{id}/status' in paths and '/api/tickets/{id}/notes' in paths
            assert '409' in paths['/api/tickets/{id}/notes']['post']['responses']
            assert '409' in paths['/api/tickets/{id}/title']['put']['responses']

with server(SAMPLES[-1]) as base:
    make(base)
    send(base, '/api/tickets/1/status', 'PUT', {'status': 'Closed'}, 409)
    send(base, '/api/tickets/1/status', 'PUT', {'status': 'unknown'}, 400)
    assert get_json(base, '/api/tickets/1')['status'] == 'Open'
    send(base, '/api/tickets/1/status', 'PUT', {'status': 'InProgress'}, 200)
    send(base, '/api/tickets/1/status', 'PUT', {'status': 'Closed'}, 409)
    for note in ['   ', 'ab', 'x' * 501]:
        send(base, '/api/tickets/1/notes', 'POST', {'text': note}, 400)
    send(base, '/api/tickets/999/notes', 'POST', {'text': 'Missing note'}, 404)
    send(base, '/api/tickets/1/notes', 'POST', {'text': '  Fixed network  '}, 200)
    before = get_json(base, '/api/tickets/1')
    send(base, '/api/tickets/1/notes', 'POST', {'text': ' Fixed network '}, 409)
    assert get_json(base, '/api/tickets/1') == before
    # Capacity is checked with mutation under the same lock, including concurrent callers.
    def add_parallel_note(i):
        status, _, data = request(base, '/api/tickets/1/notes', 'POST',
            json.dumps({'text': f'Follow-up {i}'}).encode(), {'Content-Type': 'application/json'})
        assert status in (200, 409), (status, data)
        return status
    with ThreadPoolExecutor(max_workers=6) as pool:
        statuses = list(pool.map(add_parallel_note, range(10)))
    assert statuses.count(200) == 4 and statuses.count(409) == 6, statuses
    full = get_json(base, '/api/tickets/1')
    assert len(full['notes']) == 5 and full['notes'][0] == 'Fixed network'
    send(base, '/api/tickets/1/notes', 'POST', {'text': 'Over capacity'}, 409)
    assert get_json(base, '/api/tickets/1') == full
    _, closed = send(base, '/api/tickets/1/status', 'PUT', {'status': 'Closed'}, 200)
    assert closed['status'] == 'Closed' and closed['notes'] == full['notes']
    _, repeated = send(base, '/api/tickets/1/status', 'PUT', {'status': 'Closed'}, 200)
    assert repeated == closed
    send(base, '/api/tickets/1/status', 'PUT', {'status': 'Open'}, 409)
    send(base, '/api/tickets/1/title', 'PUT', {'title': 'Forbidden rename'}, 409)
    send(base, '/api/tickets/1/notes', 'POST', {'text': 'Forbidden late note'}, 409)
    assert get_json(base, '/api/tickets/1') == closed
    # Parallel creates cannot reuse IDs.
    with ThreadPoolExecutor(max_workers=6) as pool:
        made = list(pool.map(lambda i: make(base, f'Concurrent {i}')[1], range(12)))
    assert len({t['id'] for t in made}) == 12
with server(SAMPLES[-1]) as base:
    assert rows(base) == []

# Publish and run from a separate directory, with copied appsettings and no source cwd.
with tempfile.TemporaryDirectory(prefix='supportdesk-api-publish-') as directory:
    output = Path(directory) / 'output'
    subprocess.run(['dotnet', 'publish', str(ROOT/'samples'/SAMPLES[-1]/'SupportDesk.csproj'),
        '-c','Release','-o',str(output),'--nologo'], check=True)
    env = os.environ.copy()
    env.update(DOTNET_ENVIRONMENT='Production', ASPNETCORE_ENVIRONMENT='Production')
    # Published layout differs from Debug; launch this DLL from its own output directory.
    import re
    with (Path(directory)/'publish.log').open('w') as log:
        process = subprocess.Popen(['dotnet',str(output/'SupportDesk.dll'),'--urls','http://127.0.0.1:0'],
            cwd=output,env=env,stdout=log,stderr=subprocess.STDOUT)
        try:
            deadline=time.monotonic()+25
            base=None
            while time.monotonic()<deadline:
                logs=(Path(directory)/'publish.log').read_text()
                match=re.search(r'Now listening on: (http://127\.0\.0\.1:\d+)',logs)
                if match: base=match.group(1);break
                assert process.poll() is None,logs
                time.sleep(0.1)
            assert base,logs
            assert get_json(base,'/health')['status']=='ok'
            assert get_json(base,'/api/tickets')['pageSize']==20
            make(base,'Published API ticket')
            assert get_json(base,'/api/tickets/1')['title']=='Published API ticket'
        finally:
            process.terminate()
            try:process.wait(timeout=8)
            except subprocess.TimeoutExpired:process.kill();process.wait(timeout=5)
print('PASS: section three API, scopes, startup validation, pagination, OpenAPI, domain conflicts, concurrency and published DLL.')
