"""Execute lessons 75-78, verify TRX and documented demos, and detect an overflow mutation."""
from pathlib import Path
import json
import os
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SUITES = [('075-http-resilience', 8), ('076-background-work', 4), ('077-logging', 3), ('078-debugging', 9)]
SAMPLES = ['74-http-resilience', '75-background-work', '76-structured-logging', '77-debugging']
assert shutil.which('dotnet'), 'Install .NET SDK 10.'

def run(args, *, expected=0):
    env = os.environ.copy()
    env['DOTNET_NOLOGO'] = '1'
    result = subprocess.run(args, cwd=ROOT, env=env, capture_output=True, text=True,
                            encoding='utf-8', errors='replace', timeout=240)
    assert (result.returncode == 0) if expected == 0 else (result.returncode != 0), (args, result.stdout, result.stderr)
    return result.stdout

def trx(path, minimum, failures=0):
    doc = ET.parse(path)
    counts = doc.find('.//{*}Counters')
    assert counts is not None, 'No test counters.'
    total = int(counts.get('total', '0'))
    assert total >= minimum and int(counts.get('executed', '0')) == total, 'Missing or skipped tests.'
    assert int(counts.get('failed', '0')) == failures
    assert int(counts.get('passed', '0')) == total - failures
    rows = doc.findall('.//{*}UnitTestResult')
    assert len(rows) == total and all(row.get('outcome') in ['Passed', 'Failed'] for row in rows)
    return total

with tempfile.TemporaryDirectory(prefix='testing-next-') as directory:
    folder = Path(directory)
    for suite, minimum in SUITES:
        result = folder / suite
        run(['dotnet', 'test', str(ROOT/'tests'/suite/'SupportDesk.Tests.csproj'), '--nologo', '-m:1',
             '--logger', 'trx;LogFileName=results.trx', '--results-directory', str(result)])
        print(f'PASS: {suite}: {trx(result/"results.trx", minimum)} executed, zero failures/skips.')

    outputs = [run(['dotnet', 'run', '--no-build', '--project', f'samples/{sample}/SupportDesk.csproj']).strip()
               for sample in SAMPLES]
    assert outputs[0] == 'status=200; attempts=2', outputs[0]
    assert outputs[1] == 'completed=2; disposed=2', outputs[1]
    lines = outputs[2].splitlines()
    assert len(lines) == 1, outputs[2]
    event = json.loads(lines[0])
    assert event['Properties']['Outcome'] == 'delivered'
    assert event['Properties']['JobId'] == '11111111-1111-1111-1111-111111111111'
    assert 'private@example.invalid' not in outputs[2] and 'private body' not in outputs[2]
    assert re.fullmatch(r'broken=\[101,102\]\r?\ntrace=[0-9a-f]{32}; span=[0-9a-f]{16}; operation=tickets.page\r?\nfixed=\[\]', outputs[3]), outputs[3]
    print('PASS: all four documented sample outputs match their contracts.')

    # Change only a disposable copy. A real failed assertion, not merely build failure, must detect it.
    mutant = folder/'mutant'
    for relative in ['samples/77-debugging', 'tests/078-debugging']:
        shutil.copytree(ROOT/relative, mutant/relative, ignore=shutil.ignore_patterns('bin', 'obj', 'TestResults'))
    source = mutant/'samples/77-debugging/PageQuery.cs'
    text = source.read_text(encoding='utf-8')
    assert text.count('((long)page - 1) * size') == 1
    source.write_text(text.replace('((long)page - 1) * size', '(page - 1) * size'), encoding='utf-8')
    results = folder/'mutation'
    run(['dotnet', 'test', str(mutant/'tests/078-debugging/SupportDesk.Tests.csproj'), '--nologo', '-m:1',
         '--filter', 'FullyQualifiedName~Historical_repro', '--logger', 'trx;LogFileName=mutation.trx',
         '--results-directory', str(results)], expected=1)
    assert trx(results/'mutation.trx', 1, failures=1) == 1
    print('PASS: removing the wide multiplication fails the intended regression assertion in a temporary copy.')
