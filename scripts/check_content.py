"""Check the curriculum count, written lessons and local Markdown links."""
from pathlib import Path
import json
import re
from urllib.parse import unquote

root = Path(__file__).resolve().parents[1]
lessons = json.loads((root / 'course.json').read_text(encoding='utf-8'))
assert len(lessons) == 90
assert [item['id'] for item in lessons] == list(range(1, 91))
for item in lessons:
    exists = (root / item['path']).is_file()
    assert exists == (item['status'] == 'written'), item
for path in root.rglob('*.md'):
    text = path.read_text(encoding='utf-8')
    assert text.count('```') % 2 == 0, f'Unclosed fence: {path}'
    for target in re.findall(r'\]\(([^)]+)\)', text):
        if '://' in target or target.startswith('#'):
            continue
        destination = path.parent / unquote(target.split('#')[0])
        assert destination.exists(), f'Broken link: {path}: {target}'
written = sum(item['status'] == 'written' for item in lessons)
print(f'PASS: 90 curriculum entries, {written} written lessons, local links and code fences.')

# A runnable project for every written lesson after environment setup, with no orphan snapshot.
catalog = json.loads((root / 'samples/catalog.json').read_text(encoding='utf-8'))
expected_ids = [item['id'] for item in lessons if item['status'] == 'written' and item['id'] != 1]
assert [row['lesson'] for row in catalog] == expected_ids, 'Sample catalog does not cover written lessons.'
projects = {path.relative_to(root).as_posix() for path in (root / 'samples').glob('*/SupportDesk.csproj')}
assert {row['project'] for row in catalog} == projects, 'Orphan or missing sample project.'
for row in catalog:
    lesson_text = (root / lessons[row['lesson'] - 1]['path']).read_text(encoding='utf-8')
    assert row['project'] in lesson_text, f"Lesson/sample command mismatch: {row}"
for path in root.rglob('*.md'):
    text = path.read_text(encoding='utf-8')
    for project in re.findall(r'samples/[a-zA-Z0-9-]+/[a-zA-Z0-9.-]+\.csproj', text):
        assert (root / project).is_file(), f'Broken project command: {path}: {project}'
print(f'PASS: {len(catalog)} runnable sample projects match written lesson commands.')
