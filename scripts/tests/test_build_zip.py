"""Verifica conteúdo, exclusões e revisões sem dependências externas."""
import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest
import zipfile

SPEC = importlib.util.spec_from_file_location("build_zip", Path(__file__).parents[1] / "build_zip.py")
build_zip = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(build_zip)


class ExportTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.previous_root = build_zip.ROOT
        build_zip.ROOT = self.root
        subprocess.run(["git", "init", "-q", str(self.root)], check=True)
        self.write(".gitignore", "zips/\nartifacts/\n")
        self.write("AGENTS.md", "instruções")
        self.write("src/App.cs", "// código")
        self.write(".github/workflows/build.yml", "name: exemplo")
        self.write("removed.txt", "antigo")
        subprocess.run(["git", "add", "removed.txt"], cwd=self.root, check=True)
        (self.root / "removed.txt").unlink()
        for name in [".env", ".env.local", "secret.pfx", "obj/cache", "bin/app.exe", "artifacts/app.exe", "zips/old.zip"]:
            self.write(name, "não exportar")

    def write(self, name, text):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def tearDown(self):
        build_zip.ROOT = self.previous_root
        self.temp.cleanup()

    def test_complete_source_export_excludes_builds_secrets_and_deleted_files(self):
        build_zip.main()
        archive = next(self.root.glob("zips/f8translate-source-*.zip"))
        with zipfile.ZipFile(archive) as bundle:
            self.assertIsNone(bundle.testzip())
            self.assertEqual(set(bundle.namelist()), {
                ".gitignore", "AGENTS.md", "src/App.cs", ".github/workflows/build.yml"
            })

    def test_new_revision_does_not_overwrite_previous(self):
        build_zip.main()
        first = next(self.root.glob("zips/f8translate-source-*.zip"))
        original = first.read_bytes()
        self.write("src/App.cs", "// alteração")
        build_zip.main()
        self.assertEqual(len(list(self.root.glob("zips/f8translate-source-*.zip"))), 2)
        self.assertEqual(first.read_bytes(), original)


if __name__ == "__main__":
    unittest.main()
