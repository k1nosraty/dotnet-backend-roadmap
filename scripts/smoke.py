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

# Foundation lessons 007-012: resources, persistence and cancellation.
import json
import tempfile

new_samples = ['06-ticket-model', '07-ticket-nullability', '08-ticket-exceptions',
               '09-ticket-linq', '10-ticket-json', '11-ticket-async']
for name in new_samples:
    subprocess.run(['dotnet', 'build', str(ROOT / 'samples' / name / 'SupportDesk.csproj'),
                    '--nologo'], check=True)

output = run('06-ticket-model')
assert 'First: #1 Router issue Closed' in output, output
assert 'Alias is first: True' in output and 'Second: #2 Printer issue Open' in output, output
output = run('07-ticket-nullability')
for text in ['Found: Network issue', 'Missing: Ticket not found', 'Selection: None']:
    assert text in output, output
output = run('09-ticket-linq')
for text in ['Deferred count: 3', 'Snapshot count: 2', 'Open: 3', 'Closed: 1']:
    assert text in output, output
assert output.index('#2:') < output.index('#3:') < output.index('#4:'), output

def run_args(name, arguments, stdin='', expected_code=0):
    result = subprocess.run(
        ['dotnet', 'run', '--no-build', '--project',
         str(ROOT / 'samples' / name / 'SupportDesk.csproj'), '--', *map(str, arguments)],
        input=stdin, text=True, capture_output=True, timeout=30)
    assert result.returncode == expected_code, (name, arguments, result.stdout, result.stderr)
    return result.stdout + result.stderr

with tempfile.TemporaryDirectory(prefix='supportdesk-') as directory:
    folder = Path(directory)
    log = folder / 'activity.log'
    output = run_args('08-ticket-exceptions', [log])
    assert 'Invalid title rejected.' in output and 'status: Open' in output, output
    run_args('08-ticket-exceptions', [log])
    assert log.read_text().splitlines() == ['Viewed ticket #1', 'Viewed ticket #1']
    run_args('08-ticket-exceptions', [folder / 'missing-directory' / 'log'], expected_code=1)

    path = folder / 'sync.json'
    assert 'Count: 0' in run_args('10-ticket-json', [path, 'list'])
    assert not path.exists(), 'Reading a missing file must not create it.'
    run_args('10-ticket-json', [path, 'add', 'مشکل شبکه'])
    assert 'مشکل شبکه' in run_args('10-ticket-json', [path, 'list'])
    run_args('10-ticket-json', [path, 'add', 'Printer issue'])
    assert 'Count: 2' in run_args('10-ticket-json', [path, 'list'])
    original = path.read_bytes()
    run_args('10-ticket-json', [path, 'add', 'ab'], expected_code=1)
    assert path.read_bytes() == original

    corrupt_cases = [
        '{broken', 'null',
        json.dumps({'Version': 2, 'Tickets': []}),
        json.dumps({'Version': 1, 'Tickets': None}),
        json.dumps({'Version': 1, 'Tickets': [None]}),
        json.dumps({'Version': 1, 'Tickets': [{'Id': 1, 'Title': None, 'Status': 0}]}),
        json.dumps({'Version': 1, 'Tickets': [{'Id': 0, 'Title': 'Valid title', 'Status': 0}]}),
        json.dumps({'Version': 1, 'Tickets': [{'Id': 1, 'Title': 'ab', 'Status': 0}]}),
        json.dumps({'Version': 1, 'Tickets': [{'Id': 1, 'Title': 'Valid title', 'Status': 99}]}),
        json.dumps({'Version': 1, 'Tickets': [
            {'Id': 1, 'Title': 'Valid title', 'Status': 0},
            {'Id': 1, 'Title': 'Other title', 'Status': 2}]}),
    ]
    for name in ['10-ticket-json', '11-ticket-async']:
        for text in corrupt_cases:
            bad = folder / 'bad.json'
            bad.write_text(text, encoding='utf-8')
            before = bad.read_bytes()
            arguments = [bad, 'add', 'Valid title'] if name == '10-ticket-json' else [bad]
            run_args(name, arguments, '1\nValid title\n0\n', expected_code=1)
            assert bad.read_bytes() == before, (name, text)

    async_path = folder / 'async.json'
    output = run_args('11-ticket-async', [async_path],
        '1\nNetwork issue\n4\n1\n3\n1\n4\n1\n5\n0\n')
    for text in ['Invalid transition.', 'Saved status: InProgress',
                 'Saved status: Closed', 'Open: 0', 'InProgress: 0', 'Closed: 1']:
        assert text in output, output
    output = run_args('11-ticket-async', [async_path], '2\n1\nPrinter issue\n0\n')
    assert '#1: Network issue (Closed)' in output and 'Saved #2' in output, output
    original = async_path.read_bytes()
    output = run_args('11-ticket-async', [async_path], '4\n2\n3\n99\n1\nab\n0\n')
    assert 'Invalid transition.' in output and 'Ticket not found.' in output, output
    assert async_path.read_bytes() == original, 'Rejected actions must not save.'
    output = run_args('11-ticket-async', [async_path, '--cancel-demo'])
    assert 'Cancelled.' in output and async_path.read_bytes() == original, output
    cancel_path = folder / 'cancel-new.json'
    assert 'Cancelled.' in run_args('11-ticket-async', [cancel_path, '--cancel-demo'])
    assert not cancel_path.exists()
    for stdin in ['', '1\n', '3\n', '4\n']:
        run_args('11-ticket-async', [async_path], stdin)
    assert async_path.read_bytes() == original
    assert not list(folder.glob('*.tmp')), 'Temporary files should be cleaned up.'

print('PASS: all 11 samples; reference behavior, nullable, resource disposal, LINQ, '
      'JSON round-trip, corrupt data preservation, status persistence, EOF and pre-cancellation.')

# Lesson 013: validate the public model from a separate consumer assembly.
encapsulation_project = ROOT / 'samples' / '12-ticket-encapsulation' / 'SupportDesk.csproj'
subprocess.run(['dotnet', 'build', str(encapsulation_project), '--nologo'], check=True)
output = run('12-ticket-encapsulation')
for text in ['Rejected rename; title: Network issue', 'Rejected note; count: 0',
             'Rejected close without note; status: InProgress', 'Live view count: 1',
             'Copy count: 2; actual count: 1', 'Wrapper rejected external mutation.',
             'Final: Closed; notes: 1; title: Network issue']:
    assert text in output, output

import xml.etree.ElementTree as ET

def consumer_project(directory):
    directory.mkdir()
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
    settings = ET.SubElement(project, 'PropertyGroup')
    for key, value in {'OutputType': 'Exe', 'TargetFramework': 'net10.0',
                       'ImplicitUsings': 'enable', 'Nullable': 'enable',
                       'TreatWarningsAsErrors': 'true'}.items():
        ET.SubElement(settings, key).text = value
    references = ET.SubElement(project, 'ItemGroup')
    ET.SubElement(references, 'ProjectReference', Include=str(encapsulation_project))
    ET.ElementTree(project).write(directory / 'Consumer.csproj', encoding='unicode')
    return directory / 'Consumer.csproj'

with tempfile.TemporaryDirectory(prefix='encapsulation-') as directory:
    folder = Path(directory)
    project = consumer_project(folder / 'checks')
    (project.parent / 'Program.cs').write_text('''
using SupportDesk.Domain;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static void Reject<T>(Action action) where T : Exception
{
    try { action(); }
    catch (Exception ex) when (ex is T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}

Reject<ArgumentOutOfRangeException>(() => { _ = new Ticket(0, "Valid title"); });
Reject<ArgumentException>(() => { _ = new Ticket(1, "ab"); });
Reject<ArgumentNullException>(() => { _ = new Ticket(1, null!); });
_ = new Ticket(1, new string('x', 120));
Reject<ArgumentException>(() => { _ = new Ticket(1, new string('x', 121)); });

Ticket ticket = new(1, "  Network issue  ");
Check(ticket.Title == "Network issue", "Constructor normalization");
Reject<ArgumentException>(() => ticket.Rename("ab"));
Check(ticket.Title == "Network issue", "Rejected rename changed title");
Reject<InvalidOperationException>(() => ticket.Close());
Check(ticket.Status == TicketStatus.Open, "Rejected close changed status");
ticket.Start();
Reject<InvalidOperationException>(() => ticket.Start());
Reject<InvalidOperationException>(() => ticket.Close());
Check(ticket.Status == TicketStatus.InProgress, "Rejected transition changed status");

IReadOnlyList<string> view = ticket.Notes;
Reject<ArgumentException>(() => ticket.AddNote("ab"));
Reject<ArgumentException>(() => ticket.AddNote(new string('x', 501)));
Reject<ArgumentNullException>(() => ticket.AddNote(null!));
Check(view.Count == 0, "Rejected notes were added");
ticket.AddNote("  abc  ");
Check(view.Count == 1 && view[0] == "abc", "Live view or normalization broken");
Reject<InvalidOperationException>(() => ticket.AddNote(" abc "));
Check(view.Count == 1, "Duplicate changed count");
ticket.AddNote(new string('x', 500));
ticket.AddNote("Note");
ticket.AddNote("note");
ticket.AddNote("Fifth note");
Check(view.Count == 5, "Valid notes or case-sensitive rule broken");
string[] before = view.ToArray();
Reject<InvalidOperationException>(() => ticket.AddNote("Sixth note"));
Check(view.SequenceEqual(before), "Capacity rejection changed notes");
Reject<NotSupportedException>(() => ((ICollection<string>)view).Add("Bypass"));
Reject<NotSupportedException>(() => ((IList<string>)view)[0] = "Bypass");
Reject<NotSupportedException>(() => ((ICollection<string>)view).Clear());
Check(view.SequenceEqual(before), "Wrapper permitted mutation");
List<string> copy = view.ToList();
copy.Clear();
Check(view.Count == 5, "Copy clear changed original");
ticket.Close();
Reject<InvalidOperationException>(() => ticket.Rename("New title"));
Reject<InvalidOperationException>(() => ticket.AddNote("Late note"));
Check(ticket.Status == TicketStatus.Closed && ticket.Title == "Network issue" &&
      view.SequenceEqual(before), "Rejected edit changed closed ticket");
Console.WriteLine("PASS: model boundaries and rejected-operation state preservation.");
''', encoding='utf-8')
    result = subprocess.run(['dotnet', 'run', '--project', str(project)],
                            text=True, capture_output=True, timeout=60)
    assert result.returncode == 0, (result.stdout, result.stderr)
    assert 'PASS: model boundaries' in result.stdout, result.stdout

    negative = consumer_project(folder / 'negative')
    (negative.parent / 'Program.cs').write_text(
        'using SupportDesk.Domain;\nvar ticket = new Ticket(1, "Valid title");\n'
        'ticket.Title = "Bypass";\n', encoding='utf-8')
    result = subprocess.run(['dotnet', 'build', str(negative), '--nologo'],
                            text=True, capture_output=True, timeout=60)
    assert result.returncode != 0 and 'CS0272' in result.stdout + result.stderr, (
        result.stdout, result.stderr)

print('PASS: lesson 013 boundary cases, read-only wrapper and compile-time setter protection.')
