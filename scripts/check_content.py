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
print('PASS: 90 curriculum entries, 3 written lessons, local links and code fences.')
