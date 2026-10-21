"""Verify and run the exact lesson-81 artifact without rebuilding it. No external deployment."""
from contextlib import contextmanager
from pathlib import Path
import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
import time
from urllib import request

DLL = 'SupportDesk.Lesson81.dll'
OPENER = request.build_opener(request.ProxyHandler({}))

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def verify(folder, expected_commit):
    folder = Path(folder)
    manifest = json.loads((folder/'manifest.json').read_text(encoding='utf-8'))
    assert re.fullmatch(r'[0-9a-f]{40}|local', expected_commit), 'Invalid expected commit.'
    assert manifest['commit'] == expected_commit and manifest['schema'] == 1, 'Wrong build provenance.'
    assert not any(path.is_symlink() for path in folder.rglob('*')), 'Symlinks are not accepted.'
    actual = {path.relative_to(folder).as_posix() for path in folder.rglob('*') if path.is_file()}
    files = manifest['files']
    assert set(files) == actual - {'manifest.json'} and DLL in files, 'Missing or extra artifact files.'
    for name, checksum in files.items():
        assert (folder/name).resolve().is_relative_to(folder.resolve()), 'Path outside artifact.'
        assert digest(folder/name) == checksum, f'Artifact checksum mismatch: {name}'
    return manifest

@contextmanager
def serve(folder, dll):
    folder = Path(folder).resolve()
    env = os.environ.copy()
    env.update({'DOTNET_ENVIRONMENT': 'Production', 'ASPNETCORE_ENVIRONMENT': 'Production'})
    # CLI URL takes precedence over machine settings; logs contain only synthetic lab traffic.
    with tempfile.TemporaryDirectory(prefix='published-lab-') as directory:
        log_path = Path(directory)/'host.log'
        with log_path.open('w', encoding='utf-8') as log:
            host = subprocess.Popen(['dotnet', str(folder/dll), '--urls', 'http://127.0.0.1:0'],
                                    cwd=folder, env=env, stdout=log, stderr=subprocess.STDOUT)
            try:
                deadline = time.monotonic() + 30
                while time.monotonic() < deadline:
                    logs = log_path.read_text(encoding='utf-8', errors='replace')
                    assert host.poll() is None, f'Published host exited: {logs}'
                    match = re.search(r'Now listening on: (http://127\.0\.0\.1:\d+)', logs)
                    if match:
                        yield match.group(1)
                        break
                    time.sleep(.1)
                else: raise AssertionError('Published host startup timeout.')
            finally:
                host.terminate()
                try: host.wait(timeout=10)
                except subprocess.TimeoutExpired: host.kill(); host.wait(timeout=5)

def probe(folder, expected_commit):
    verify(folder, expected_commit)
    with serve(folder, DLL) as base:
        with OPENER.open(base+'/version', timeout=10) as response:
            info = json.load(response)
        assert info == {'service': 'release-lab', 'version': '1.0.0', 'commit': expected_commit}, info
        with OPENER.open(base+'/health/live', timeout=10) as response:
            assert response.status == 200 and response.read() == b'Healthy'

if __name__ == '__main__':
    if len(sys.argv) != 3: raise SystemExit('Usage: release_gate.py ARTIFACT_DIRECTORY EXPECTED_COMMIT')
    probe(Path(sys.argv[1]), sys.argv[2])
    print('PASS: artifact file set, SHA-256, expected commit and actual published HTTP response; no rebuild or external deploy.')
