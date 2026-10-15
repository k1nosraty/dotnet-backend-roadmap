"""Shared loopback HTTP test helpers; requires .NET SDK 10."""
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
if not shutil.which('dotnet'):
    raise SystemExit('Install .NET SDK 10 before running web checks.')

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

