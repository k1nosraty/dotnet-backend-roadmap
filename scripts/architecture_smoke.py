"""Behavior, direct references, negative compilation and publish for lessons 49-52.

Requires SDK 10, but no PostgreSQL or external NuGet packages. Run from any cwd.
"""
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SAMPLES = ROOT / 'samples'
NAMES = ['48-responsibilities', '49-folders', '50-projects', '51-application-services']


def invoke(args, *, cwd=None, success=True):
    result = subprocess.run(args, cwd=cwd, capture_output=True, text=True,
                            encoding='utf-8', errors='replace', timeout=180)
    if success and result.returncode != 0:
        raise AssertionError(f'{args}\n{result.stdout}\n{result.stderr}')
    return result


def check_references(base):
    """Check this sample's deliberate direct project graph, not arbitrary C# coupling."""
    allowed = {
        'SupportDesk.csproj': {
            'src/Domain/SupportDesk.Domain.csproj',
            'src/Application/SupportDesk.Application.csproj',
            'src/Infrastructure/SupportDesk.Infrastructure.csproj',
        },
        'src/Domain/SupportDesk.Domain.csproj': set(),
        'src/Application/SupportDesk.Application.csproj': {'../Domain/SupportDesk.Domain.csproj'},
        'src/Infrastructure/SupportDesk.Infrastructure.csproj': {
            '../Domain/SupportDesk.Domain.csproj', '../Application/SupportDesk.Application.csproj',
        },
    }
    found = {p.relative_to(base).as_posix() for p in base.rglob('*.csproj')}
    assert found == set(allowed), (base, found)
    for rel, expected in allowed.items():
        doc = ET.parse(base / rel)
        actual = {item.attrib['Include'] for item in doc.findall('.//ProjectReference')}
        assert actual == expected, (base, rel, actual)
        assert not doc.findall('.//PackageReference'), (base, rel, 'unexpected package')


# Lesson 50 changes file location, not executable source or namespaces.
a, b = SAMPLES / NAMES[0], SAMPLES / NAMES[1]
for source in a.rglob('*.cs'):
    if {'bin', 'obj'} & set(source.parts):
        continue
    matches = [p for p in b.rglob(source.name) if not {'bin', 'obj'} & set(p.parts)]
    assert len(matches) == 1 and source.read_bytes() == matches[0].read_bytes(), source
for name in NAMES[2:]:
    check_references(SAMPLES / name)
print('PASS: relocation and declared project dependency graphs.')

if not shutil.which('dotnet'):
    print('Cannot run behavior checks: install .NET SDK 10 and reopen the terminal.', file=sys.stderr)
    raise SystemExit(2)

for name in NAMES:
    base = SAMPLES / name
    project = base / 'SupportDesk.csproj'
    invoke(['dotnet', 'build', str(project), '--nologo'])
    dll = base / 'bin' / 'Debug' / 'net10.0' / 'SupportDesk.dll'
    checked = invoke(['dotnet', str(dll), '--check'])
    assert 'PASS: architecture behavior' in checked.stdout, checked.stdout
    demo = invoke(['dotnet', str(dll)])
    assert 'Success: 1 Closed notes=1' in demo.stdout, demo.stdout
    print(f'PASS: {name} build, behavior, concurrent stale writers and demo.')

with tempfile.TemporaryDirectory(prefix='architecture-check-') as directory:
    tmp = Path(directory)
    # All negative experiments occur in disposable copies. They never mutate course sources.
    original = SAMPLES / '50-projects'
    for kind in ['domain-reference', 'internal-adapter']:
        target = tmp / kind
        shutil.copytree(original, target, ignore=shutil.ignore_patterns('bin', 'obj'))
        if kind == 'domain-reference':
            (target / 'src/Domain/Illegal.cs').write_text(
                'namespace SupportDesk.Domain; public static class Illegal { '
                'public static System.Type Target => typeof(SupportDesk.Application.CloseTicket); }',
                encoding='utf-8')
            project = target / 'src/Domain/SupportDesk.Domain.csproj'
            expected = ('CS0234', 'CS0246')
        else:
            with (target / 'Program.cs').open('a', encoding='utf-8') as file:
                file.write('\nConsole.WriteLine(typeof(SupportDesk.Infrastructure.InMemoryTicketStore));\n')
            project = target / 'SupportDesk.csproj'
            expected = ('CS0122',)
        result = invoke(['dotnet', 'build', str(project), '--nologo'], success=False)
        output = result.stdout + result.stderr
        assert result.returncode != 0 and any(code in output for code in expected), output
        print(f'PASS: compiler rejects {kind}.')

    published = tmp / 'published'
    invoke(['dotnet', 'publish', str(SAMPLES / NAMES[-1] / 'SupportDesk.csproj'),
            '-c', 'Release', '-o', str(published), '--nologo'])
    elsewhere = tmp / 'elsewhere'
    elsewhere.mkdir()
    result = invoke(['dotnet', str(published / 'SupportDesk.dll'), '--check'], cwd=elsewhere)
    assert 'PASS: architecture behavior' in result.stdout, result.stdout
    for layer in ['Domain', 'Application', 'Infrastructure']:
        assert (published / f'SupportDesk.{layer}.dll').is_file(), layer
    print('PASS: complete multi-project publish runs outside the source directory.')
