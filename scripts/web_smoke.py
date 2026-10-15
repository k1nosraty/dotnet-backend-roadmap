"""Build the web examples and test real loopback HTTP; requires .NET SDK 10."""
from pathlib import Path
from contextlib import contextmanager
from concurrent.futures import ThreadPoolExecutor
import json
import os
import re
import shutil
import subprocess
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
SAMPLES = ['22-http-basics', '23-web-host', '24-routing-binding']
if not shutil.which('dotnet'):
    raise SystemExit('Install .NET SDK 10 before running web checks.')
for name in SAMPLES:
    subprocess.run(['dotnet', 'build', str(ROOT / 'samples' / name / 'SupportDesk.csproj'), '--nologo'], check=True)

@contextmanager
def server(name, environment='Production', overrides=None, arguments=None):
    project = ROOT / 'samples' / name
    env = os.environ.copy()
    env['DOTNET_ENVIRONMENT'] = environment
    env['ASPNETCORE_ENVIRONMENT'] = environment
    env.pop('Course__Greeting', None)
    env.update(overrides or {})
    with tempfile.TemporaryDirectory(prefix='supportdesk-web-') as directory:
        log_path = Path(directory) / 'server.log'
        with log_path.open('w', encoding='utf-8') as log:
            process = subprocess.Popen(
                ['dotnet', str(project / 'bin' / 'Debug' / 'net10.0' / 'SupportDesk.dll'),
                 '--urls', 'http://127.0.0.1:0', *(arguments or [])],
                cwd=project, env=env, stdout=log, stderr=subprocess.STDOUT)
            try:
                deadline = time.monotonic() + 25
                base = None
                while time.monotonic() < deadline:
                    logs = log_path.read_text(encoding='utf-8', errors='replace')
                    match = re.search(r'Now listening on: (http://127\.0\.0\.1:\d+)', logs)
                    if match:
                        base = match.group(1)
                        break
                    if process.poll() is not None:
                        raise AssertionError(f'{name} exited during startup:\n{logs}')
                    time.sleep(0.1)
                assert base, f'{name} startup timeout: {log_path.read_text()}'
                yield base
            finally:
                if process.poll() is None:
                    process.terminate()
                    try:
                        process.wait(timeout=8)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.wait(timeout=5)

opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))

def request(base, path, method='GET', body=None, headers=None):
    req = urllib.request.Request(base + path, data=body, method=method, headers=headers or {})
    try:
        with opener.open(req, timeout=5) as response:
            return response.status, response.headers, response.read()
    except urllib.error.HTTPError as error:
        with error:
            return error.code, error.headers, error.read()

def get_json(base, path, expected=200):
    status, headers, data = request(base, path)
    assert status == expected, (path, status, data)
    assert 'application/json' in headers.get('Content-Type', ''), (path, headers)
    return json.loads(data)

def create(base, title, expected=201):
    status, headers, data = request(base, '/tickets', 'POST',
        json.dumps({'title': title}, ensure_ascii=False).encode('utf-8'), {'Content-Type': 'application/json'})
    assert status == expected, (status, data)
    return headers, json.loads(data) if data else None

with server('22-http-basics') as base:
    assert get_json(base, '/tickets') == []
    headers, first = create(base, '  مشکل شبکه  ')
    assert first['title'] == 'مشکل شبکه' and first['id'] == 1, first
    assert headers.get('Location') == '/tickets/1', headers
    assert get_json(base, headers['Location']) == first
    _, second = create(base, 'مشکل شبکه')
    assert second['id'] != first['id'], (first, second)  # This POST has no idempotency key.
    create(base, 'ab', expected=400)
    assert len(get_json(base, '/tickets')) == 2
    assert request(base, '/tickets/99')[0] == 404
    assert request(base, '/tickets/0')[0] == 400
    assert request(base, '/tickets/1', 'DELETE')[0] == 204
    status, _, data = request(base, '/tickets/1', 'DELETE')
    assert status == 404 and request(base, '/tickets/1')[0] == 404, (status, data)
    assert len(get_json(base, '/tickets')) == 1
    status, _, data = request(base, '/tickets/2', 'DELETE')
    assert status == 204 and data == b'', (status, data)
    with ThreadPoolExecutor(max_workers=6) as pool:
        created = list(pool.map(lambda i: create(base, f'Concurrent ticket {i}')[1], range(12)))
    assert len({row['id'] for row in created}) == 12
    assert len(get_json(base, '/tickets')) == 12
# A fresh process has a fresh in-memory store.
with server('22-http-basics') as base:
    assert get_json(base, '/tickets') == []

with server('23-web-host') as base:
    status, headers, data = request(base, '/health')
    assert status == 200 and headers.get('X-Course') == 'supportdesk' and json.loads(data)['status'] == 'ok'
    first = get_json(base, '/info')
    assert first['environment'] == 'Production' and first == get_json(base, '/info')
    assert get_json(base, '/greeting')['message'] == 'Welcome to SupportDesk'
with server('23-web-host', environment='Development', overrides={'Course__Greeting': 'From environment'}) as base:
    assert get_json(base, '/info')['environment'] == 'Development'
    assert get_json(base, '/greeting')['message'] == 'From environment'
with server('23-web-host', overrides={'Course__Greeting': 'From environment'},
            arguments=['--Course:Greeting', 'From command line']) as base:
    assert get_json(base, '/greeting')['message'] == 'From command line'

with server('24-routing-binding') as base:
    create(base, 'Network issue')
    create(base, 'Printer issue')
    create(base, 'Network outage')
    assert len(get_json(base, '/tickets')) == 3
    results = get_json(base, '/tickets?q=NETWORK&limit=1')
    assert len(results) == 1 and results[0]['title'] == 'Network issue', results
    assert get_json(base, '/tickets?q=absent') == []
    assert request(base, '/tickets/abc')[0] == 404
    assert request(base, '/lookup/abc')[0] == 400
    assert request(base, '/tickets/-1')[0] == 400
    for query in ['?limit=abc', '?limit=0', '?limit=51', '?q=' + 'x' * 121]:
        assert request(base, '/tickets' + query)[0] == 400, query
    assert get_json(base, '/client')['client'] == 'unknown'
    status, _, data = request(base, '/client', headers={'X-Client': 'desktop'})
    assert status == 200 and json.loads(data)['client'] == 'desktop'
    assert request(base, '/client', headers={'X-Client': 'x' * 51})[0] == 400
    before = get_json(base, '/tickets')
    for raw in [b'{broken', b'null', b'{}', b'{"title":null}', b'{"title":1}', b'{"title":"ab"}']:
        status, _, data = request(base, '/tickets', 'POST', raw, {'Content-Type': 'application/json'})
        assert status == 400, (raw, status, data)
    assert request(base, '/tickets', 'POST')[0] == 400
    assert request(base, '/tickets', 'POST', b'title=Network', {'Content-Type': 'text/plain'})[0] == 415
    status, headers, _ = request(base, '/tickets', 'PUT')
    assert status == 405 and 'GET' in headers.get('Allow', '') and 'POST' in headers.get('Allow', ''), (status, headers)
    assert get_json(base, '/tickets') == before, 'Rejected input changed state.'
    headers, created = create(base, '  Valid new ticket  ')
    assert created['id'] == 4 and get_json(base, headers['Location'])['title'] == 'Valid new ticket'

print('PASS: live HTTP status/header/body, create/delete semantics, concurrent IDs, restart, host/config and binding failures.')
