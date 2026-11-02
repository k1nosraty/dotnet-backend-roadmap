"""Regression checks for invisible characters in copyable lesson commands."""
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


class ContentChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix='course-content-')
        cls.addClassCleanup(cls.temporary.cleanup)
        cls.root = Path(cls.temporary.name) / 'course'
        shutil.copytree(Path(__file__).resolve().parents[1], cls.root,
                        ignore=shutil.ignore_patterns('.git', 'bin', 'obj', '__pycache__', 'artifacts', 'exercises'))

    def check(self):
        return subprocess.run([sys.executable, str(self.root / 'scripts/check_content.py')],
                              capture_output=True, text=True, encoding='utf-8', timeout=30)

    def test_valid_course_passes(self):
        result = self.check()
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_invisible_command_characters_fail_with_location(self):
        path = self.root / 'lessons/08-production/085-performance/README.md'
        original = path.read_text(encoding='utf-8')
        try:
            for character in ['\x07', '\x08', '\x1b', '\x7f']:
                with self.subTest(code=ord(character)):
                    path.write_text(original + '\n' + character, encoding='utf-8')
                    result = self.check()
                    self.assertNotEqual(0, result.returncode)
                    self.assertIn('Invisible control character:', result.stderr)
                    self.assertIn('085-performance', result.stderr)
        finally:
            path.write_text(original, encoding='utf-8')


if __name__ == '__main__':
    unittest.main()
