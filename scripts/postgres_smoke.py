"""Build lessons 035-040, then exercise them against a real PostgreSQL 18 lab database."""
from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
NAMES = ['34-relational','35-sql-crud','36-joins','37-index-plans','38-transactions','39-ticket-schema']
if sys.argv[1:] not in ([], ['--build-only']):
    raise SystemExit('Usage: python scripts/postgres_smoke.py [--build-only]')
for name in NAMES:
    subprocess.run(['dotnet','build',str(ROOT/'samples'/name/'SupportDesk.csproj'),'--nologo'],check=True)
if sys.argv[1:]:
    print('PASS: PostgreSQL sample builds (database not exercised in --build-only mode).')
    raise SystemExit(0)
if not os.environ.get('SUPPORTDESK_PG'):
    raise SystemExit('Set SUPPORTDESK_PG to a dedicated PostgreSQL lab database before live checks.')
for name in NAMES:
    command = ['dotnet',str(ROOT/'samples'/name/'bin/Debug/net10.0/SupportDesk.dll')]
    env = os.environ.copy()
    env.pop('SUPPORTDESK_PG',None)
    missing = subprocess.run(command,env=env,text=True,capture_output=True,timeout=15)
    assert missing.returncode == 2 and 'Set SUPPORTDESK_PG' in missing.stderr, (name,missing.stdout,missing.stderr)
    run = subprocess.run(command,text=True,capture_output=True,timeout=120)
    assert run.returncode == 0, (name,run.stdout,run.stderr)
    assert 'PASS: lesson scenario' in run.stdout and 'Lab cleanup failed' not in run.stderr, (name,run.stdout,run.stderr)
    if name == '35-sql-crud':
        assert 'PASS: parameterized CRUD' in run.stdout
    if name == '37-index-plans':
        assert 'Execution Time' in run.stdout and 'Buffers' in run.stdout, run.stdout
    if name == '38-transactions':
        assert 'SQLSTATE 40001 and rollback' in run.stdout
    print(f'PASS: PostgreSQL {name}; successful scenario and cleanup.',flush=True)
print('PASS: real PostgreSQL constraints, parameterized CRUD, Unicode, joins, EXPLAIN, isolation, rollback and schema.')
