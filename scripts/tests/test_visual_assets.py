"""Verifica referências locais, imagens PNG e licenças sem dependências de imagem."""
from pathlib import Path
import re
import struct
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "src/F8Translate"


class VisualAssetTests(unittest.TestCase):
    def test_xaml_references_only_existing_local_images(self):
        document = ET.parse(APP / "MainView.xaml")
        for node in document.iter():
            source = node.get("Source", "")
            if not source or source.startswith("{Binding"):
                continue
            with self.subTest(source=source):
                self.assertTrue(source.startswith("Assets/"))
                self.assertTrue((APP / source).is_file())

    def test_small_pngs_and_all_language_flags_are_present(self):
        for file in (APP / "Assets").rglob("*.png"):
            with self.subTest(file=file):
                data = file.read_bytes()
                self.assertEqual(data[:8], b"\x89PNG\r\n\x1a\n")
                width, height = struct.unpack(">II", data[16:24])
                self.assertLessEqual(max(width, height), 160)
        for country in ["gb", "pt", "es", "fr", "de", "it", "jp"]:
            self.assertTrue((APP / f"Assets/Flags/{country}.png").is_file())

    def test_licenses_are_distributed_and_runtime_is_not_bundled(self):
        project = (APP / "F8Translate.csproj").read_text(encoding="utf-8")
        self.assertIn("<SelfContained>false</SelfContained>", project)
        self.assertIn('CopyToPublishDirectory="PreserveNewest"', project)
        for package in ["lucide", "circle-flags"]:
            self.assertTrue((ROOT / f"assets/vendor/{package}/LICENSE").is_file())


if __name__ == "__main__":
    unittest.main()
