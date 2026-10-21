"""Lessons 79-82 without Docker: execute tests, publish four hosts and check a release artifact."""
from pathlib import Path
import argparse
import json
import os
import re
import shutil
import subprocess
import tempfile
from urllib import request
import xml.etree.ElementTree as ET
from release_gate import digest, probe, serve, verify, OPENER

ROOT = Path(__file__).resolve().parents[1]
SAMPLES = ['78-container-lab', '79-proxy-tls', '80-release-artifact', '81-observability']
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--artifact-dir', type=Path, help='Write a new lesson-81 artifact directory (must not already exist).')
args = parser.parse_args()
commit = os.environ.get('GITHUB_SHA', 'local')
assert re.fullmatch(r'[0-9a-f]{40}|local', commit), 'Invalid build commit.'
assert shutil.which('dotnet'), 'Install .NET SDK 10.'

def run(command):
    result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=360)
    assert result.returncode == 0, (command, result.stdout, result.stderr)

with tempfile.TemporaryDirectory(prefix='production-labs-') as directory:
    folder = Path(directory)
    for suite, minimum in [('080-proxy-tls', 5), ('082-observability', 4)]:
        results = folder/suite
        run(['dotnet', 'test', str(ROOT/'tests'/suite/'SupportDesk.Tests.csproj'), '--nologo', '-m:1',
             '--logger', 'trx;LogFileName=results.trx', '--results-directory', str(results)])
        doc = ET.parse(results/'results.trx')
        counts = doc.find('.//{*}Counters'); assert counts is not None
        total = int(counts.get('total', '0'))
        assert total >= minimum and int(counts.get('executed', '0')) == total
        assert int(counts.get('passed', '0')) == total and int(counts.get('failed', '0')) == 0
        rows = doc.findall('.//{*}UnitTestResult')
        assert len(rows) == total and all(row.get('outcome') == 'Passed' for row in rows)
        print(f'PASS: {suite}: {total} executed with zero failures/skips.', flush=True)

    for number, sample in enumerate(SAMPLES, 79):
        published = folder/str(number)
        command = ['dotnet', 'publish', str(ROOT/'samples'/sample/'SupportDesk.csproj'), '-c', 'Release', '-o', str(published),
                   '--nologo', '-m:1', '-p:UseAppHost=false']
        if number == 81: command.append('-p:CourseCommit='+commit)
        run(command)
        assert not any('xunit' in path.name.lower() or path.name.endswith('.Tests.dll') for path in published.iterdir())
        if number == 80:
            # Trusted-peer middleware is tested by WAF and by real Caddy in the Docker suite.
            continue
        if number == 81:
            files = {path.relative_to(published).as_posix(): digest(path) for path in sorted(published.rglob('*')) if path.is_file()}
            (published/'manifest.json').write_text(json.dumps({'schema': 1, 'commit': commit, 'files': files}, indent=2), encoding='utf-8')
            probe(published, commit)
            damaged = folder/'damaged'; shutil.copytree(published, damaged)
            with (damaged/'SupportDesk.Lesson81.dll').open('ab') as file: file.write(b'changed')
            try: verify(damaged, commit)
            except AssertionError as ex: assert 'checksum mismatch' in str(ex)
            else: raise AssertionError('Tampered DLL was accepted.')
            try: verify(published, '0'*40 if commit != '0'*40 else '1'*40)
            except AssertionError as ex: assert 'provenance' in str(ex)
            else: raise AssertionError('Wrong commit was accepted.')
            if args.artifact_dir:
                assert not args.artifact_dir.exists(), 'Choose a new artifact directory; nothing is overwritten.'
                shutil.copytree(published, args.artifact_dir)
            continue
        with serve(published, f'SupportDesk.Lesson{number}.dll') as base:
            with OPENER.open(base+'/health/live', timeout=10) as response:
                assert response.status == 200 and response.read() == b'Healthy'
            if number == 79:
                with OPENER.open(base+'/', timeout=10) as response: assert json.load(response)['service'] == 'container-lab'
            if number == 82:
                with OPENER.open(base+'/health/ready', timeout=10) as response: assert response.status == 200
                with OPENER.open(request.Request(base+'/work/accepted', method='POST'), timeout=10) as response: assert response.status == 200
    print('PASS: four Release publications; actual published HTTP probes; manifest rejects a changed DLL and wrong commit.')
    print('NOT RUN: Docker build and TLS transport; use production_docker_smoke.py with a running Linux Docker engine.')
