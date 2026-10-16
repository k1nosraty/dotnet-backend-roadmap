"""Maintainer bootstrap for the reference migrations, never application startup.

Already committed migrations are immutable: this command refuses to overwrite them.
The bootstrap CI printed the generated files for review; normal CI never runs this file.
"""
from pathlib import Path
import json
import subprocess
ROOT=Path(__file__).resolve().parents[1]
def ef(name, migration):
    subprocess.run(['dotnet','ef','migrations','add',migration,'--project',str(ROOT/'samples'/name/'SupportDesk.csproj'),'--output-dir','Migrations'],cwd=ROOT,check=True)
subprocess.run(['dotnet','tool','restore'],cwd=ROOT,check=True)
for name in ['41-migrations','47-persistent-api']:
    folder=ROOT/'samples'/name
    if (folder/'Migrations').exists():
        raise SystemExit('Refusing to overwrite committed migrations: '+name)
    subprocess.run(['dotnet','restore',str(folder/'SupportDesk.csproj')],cwd=ROOT,check=True)
    if name=='41-migrations':
        model=folder/'Persistence/DeskContext.cs'
        original=model.read_text()
        changed='\n'.join('  b.Ignore(x => x.Priority);' if 'EVOLVE_PRIORITY' in line else line for line in original.splitlines())
        try:
            model.write_text(changed)
            ef(name,'InitialCreate')
        finally:
            model.write_text(original)
        ef(name,'AddPriority')
    else:
        ef(name,'InitialCreate')
    for path in sorted((folder/'Migrations').glob('*.cs')):
        print('COURSE_MIGRATION '+json.dumps({'path':path.relative_to(ROOT).as_posix(),'content':path.read_text()}),flush=True)
