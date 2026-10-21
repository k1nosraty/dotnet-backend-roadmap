"""Real Linux Docker builds, non-root execution and CA-verified TLS through Caddy. No skip fallback."""
from pathlib import Path
import json
import os
import shutil
import ssl
import subprocess
import tempfile
import time
from urllib import request, error
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
assert shutil.which('docker'), 'Docker CLI and a running Linux container engine are required.'

def run(arguments, *, env=None):
    result = subprocess.run(arguments, cwd=ROOT, env=env, capture_output=True, text=True,
                            encoding='utf-8', errors='replace', timeout=600)
    assert result.returncode == 0, (arguments, result.stdout, result.stderr)
    return result.stdout.strip()

def wait_get(opener, url, headers=None):
    deadline = time.monotonic() + 60
    while time.monotonic() < deadline:
        try:
            with opener.open(request.Request(url, headers=headers or {}), timeout=5) as response:
                return response.status, response.read()
        except (error.URLError, TimeoutError, ConnectionError): time.sleep(.2)
    raise AssertionError('Container HTTP/TLS startup timeout.')

assert run(['docker', 'info', '--format', '{{.OSType}}']) == 'linux', 'Switch Docker to Linux containers.'
project = 'course-production-' + uuid4().hex[:12]
image = project + ':lesson79'
container = project + '-app'
compose_file = ROOT/'samples/79-proxy-tls/compose.yaml'
env = os.environ.copy(); env['COURSE_TLS_PORT'] = '0'
compose = ['docker', 'compose', '-p', project, '-f', str(compose_file)]
try:
    run(['docker', 'build', '-t', image, str(ROOT/'samples/78-container-lab')])
    user = run(['docker', 'image', 'inspect', image, '--format', '{{.Config.User}}'])
    assert user and user not in ['0', 'root'], 'App image must select the non-root user.'
    run(['docker', 'run', '-d', '--name', container, '--read-only', '--tmpfs', '/tmp:rw,noexec,nosuid,size=32m',
         '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges', '-p', '127.0.0.1::8080', image])
    binding = json.loads(run(['docker', 'inspect', container]))[0]['NetworkSettings']['Ports']['8080/tcp'][0]
    opener = request.build_opener(request.ProxyHandler({}))
    status, body = wait_get(opener, 'http://127.0.0.1:'+binding['HostPort']+'/')
    assert status == 200 and json.loads(body)['service'] == 'container-lab'
    assert run(['docker', 'exec', container, 'id', '-u']) not in ['0', ''], 'Runtime user is root.'
    assert run(['docker', 'exec', container, 'dotnet', '--list-sdks']) == '', 'SDK leaked into final image.'
    run(['docker', 'stop', '--time', '10', container])
    assert json.loads(run(['docker', 'inspect', container]))[0]['State']['ExitCode'] == 0, 'SIGTERM did not stop cleanly.'
    print('PASS: lesson79 image serves HTTP as non-root with read-only rootfs, no SDK, and clean SIGTERM.', flush=True)

    run(compose+['config', '--quiet'], env=env)
    run(compose+['up', '-d', '--build'], env=env)
    proxy = run(compose+['ps', '-q', 'proxy'], env=env)
    app = run(compose+['ps', '-q', 'app'], env=env)
    app_info = json.loads(run(['docker', 'inspect', app]))[0]
    assert all(value is None for value in app_info['NetworkSettings']['Ports'].values()), 'Backend has a published host port.'
    binding = json.loads(run(['docker', 'inspect', proxy]))[0]['NetworkSettings']['Ports']['443/tcp'][0]
    base = 'https://localhost:'+binding['HostPort']
    with tempfile.TemporaryDirectory(prefix='course-tls-') as directory:
        root = Path(directory)/'root.crt'
        deadline = time.monotonic() + 60
        while time.monotonic() < deadline:
            result = subprocess.run(['docker', 'cp', proxy+':/data/caddy/pki/authorities/local/root.crt', str(root)], capture_output=True)
            if result.returncode == 0: break
            time.sleep(.2)
        else: raise AssertionError('Local CA was not created.')
        context = ssl.create_default_context(cafile=str(root))
        trusted = request.build_opener(request.ProxyHandler({}), request.HTTPSHandler(context=context))
        status, body = wait_get(trusted, base+'/info', {'X-Forwarded-Proto': 'http', 'X-Forwarded-Host': 'attacker.invalid'})
        assert status == 200 and json.loads(body) == {'scheme': 'https', 'secure': True}
        # Default trust must reject this private CA; never use insecure/no-verification mode.
        untrusted = request.build_opener(request.ProxyHandler({}), request.HTTPSHandler(context=ssl.create_default_context()))
        try:
            untrusted.open(base+'/info', timeout=5)
        except error.URLError as ex:
            assert isinstance(ex.reason, ssl.SSLCertVerificationError), str(ex)
        else: raise AssertionError('An untrusted local CA was unexpectedly accepted.')
        try:
            trusted.open(request.Request(base+'/info', headers={'Host': 'attacker.invalid'}), timeout=5)
        except error.HTTPError as ex: assert ex.code in [400, 404, 421]
        else: raise AssertionError('Invalid Host was accepted through the proxy.')
    print('PASS: real Caddy TLS validates hostname+private CA, rejects untrusted CA, overwrites spoofed scheme and isolates backend.')
except Exception:
    logs = subprocess.run(compose+['logs', '--no-color', '--tail', '80'], cwd=ROOT, env=env,
                          capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=30)
    print(logs.stdout[-12000:], flush=True)
    raise
finally:
    # Only UUID-owned lab resources; no prune, global certificate trust, or user container deletion.
    subprocess.run(compose+['down', '--volumes', '--remove-orphans'], cwd=ROOT, env=env, capture_output=True, timeout=60)
    subprocess.run(['docker', 'rm', '-f', container], capture_output=True, timeout=30)
    subprocess.run(['docker', 'image', 'rm', image], capture_output=True, timeout=30)
