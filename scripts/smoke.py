"""Run sample behavior checks from any working directory; requires .NET SDK 10."""
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
if not shutil.which('dotnet'):
    print('Cannot run: install .NET SDK 10 and reopen the terminal.', file=sys.stderr)
    raise SystemExit(2)

for name in ['01-ticket-summary', '02-ticket-intake']:
    project = ROOT / 'samples' / name / 'SupportDesk.csproj'
    subprocess.run(['dotnet', 'build', str(project), '--nologo'], check=True)

def run(name, stdin=''):
    project = ROOT / 'samples' / name / 'SupportDesk.csproj'
    result = subprocess.run(
        ['dotnet', 'run', '--no-build', '--project', str(project)],
        input=stdin, text=True, capture_output=True, check=True, timeout=30,
    )
    return result.stdout

summary = run('01-ticket-summary')
assert 'Total: 10' in summary and 'Completion: 20.0%' in summary, summary

cases = [
    ('Network issue\n2.5\n', 'Estimate: 2.5 hours'),
    ('  Network issue  \n2.5\n', 'Ticket: Network issue'),
    ('abc\n1\n', 'Estimate: 1 hours'),
    ('x' * 120 + '\n1000\n', 'Estimate: 1000 hours'),
    ('ab\n2\n', 'Invalid title.'),
    ('   \n2\n', 'Invalid title.'),
    ('x' * 121 + '\n2\n', 'Invalid title.'),
    ('', 'Invalid title.'),
    ('Network issue\n', 'Invalid estimated hours.'),
]
for value in ['abc', '0', '-2', '1000.01', '2,5', '۲٫۵', ' 2.5 ']:
    cases.append((f'Network issue\n{value}\n', 'Invalid estimated hours.'))
for stdin, expected in cases:
    actual = run('02-ticket-intake', stdin)
    assert expected in actual, (stdin, expected, actual)
print(f'PASS: summary and {len(cases)} intake scenarios.')
