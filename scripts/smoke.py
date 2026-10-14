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

for name in ['03-ticket-menu', '04-ticket-methods', '05-ticket-collections']:
    project = ROOT / 'samples' / name / 'SupportDesk.csproj'
    subprocess.run(['dotnet', 'build', str(project), '--nologo'], check=True)

for name in ['03-ticket-menu', '04-ticket-methods']:
    output = run(name, '1\nab\nNetwork issue\n1\nPrinter issue\n2\n0\n')
    assert 'Invalid title. Try again.' in output, output
    assert 'Registered count: 2' in output, output
    assert output.count('Registered:') == 2, output
    assert 'Goodbye.' in output, output
    output = run(name, '9\n0\n')
    assert 'Unknown option.' in output and 'Goodbye.' in output, output
    output = run(name, '')
    assert 'Goodbye.' in output, output
    output = run(name, '1\n')
    assert 'Registered:' not in output, output

output = run('05-ticket-collections',
    '2\n1\nNetwork issue\n1\nPrinter issue\n4\n1\n1\nEmail issue\n'
    '3\n1\n3\n2\n4\n1\n5\n0\n')
for text in ['No tickets.', 'Added #3: Email issue', 'Found #2: Printer issue']:
    assert text in output, output
assert output.count('Ticket not found.') == 2, output
assert output.count('Deleted #1') == 2, output  # one success message, one history item
history = ['Added #1\n', 'Added #2\n', 'Deleted #1\n', 'Added #3\n']
positions = [output.rfind(text) for text in history]
assert all(p >= 0 for p in positions) and positions == sorted(positions), output
for value in ['abc', '0', '-2', '۲', '2147483648']:
    output = run('05-ticket-collections', f'3\n{value}\n0\n')
    assert 'Invalid ID.' in output, output
for stdin in ['', '1\n', '3\n', '4\n']:
    run('05-ticket-collections', stdin)  # timeout guards against EOF busy loops
output = run('05-ticket-collections', '1\nab\nValid title\n2\n0\n')
assert 'Added #1: Valid title' in output and '#2:' not in output, output
print('PASS: menu retry, refactoring, EOF, ID deletion/recreation, missing IDs and activity order.')
