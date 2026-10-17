#!/usr/bin/env python3
"""Exporta os fontes da cópia de trabalho, sem builds ou pacotes anteriores."""
from datetime import datetime, timezone
from pathlib import Path
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parent.parent
EXCLUDED_DIRS = {
    ".git", "zips", "bin", "obj", "artifacts", "node_modules", ".vs", ".vscode",
    "__pycache__", ".venv", "venv", "TestResults", ".cache",
}


def project_files():
    output = subprocess.check_output(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cwd=ROOT
    )
    for name in sorted(set(output.decode("utf-8").split("\0")) - {""}):
        path = Path(name)
        if any(part in EXCLUDED_DIRS for part in path.parts):
            continue
        if (path.name == ".env" or path.name.startswith(".env.")
                or path.suffix.lower() in {".zip", ".pfx", ".p12", ".pem", ".key", ".user", ".suo", ".log"}):
            continue
        full_path = ROOT / path
        if full_path.is_symlink():
            raise RuntimeError(f"Revise o link simbólico antes de exportar: {name}")
        if full_path.is_file():  # Arquivos removidos do working tree ficam de fora.
            yield path


def main():
    files = list(project_files())
    if not files:
        raise RuntimeError("Nenhum arquivo de projeto encontrado.")
    destination = ROOT / "zips"
    destination.mkdir(exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%d")
    revision = 1
    while True:
        archive = destination / f"f8translate-source-{stamp}-r{revision}.zip"
        try:
            # Modo exclusivo impede sobrescrita, inclusive em execuções concorrentes.
            with zipfile.ZipFile(archive, "x", zipfile.ZIP_DEFLATED) as bundle:
                for path in files:
                    bundle.write(ROOT / path, path.as_posix())
            break
        except FileExistsError:
            revision += 1
    with zipfile.ZipFile(archive) as bundle:
        if bundle.testzip() is not None:
            raise RuntimeError("Falha na verificação de integridade do ZIP.")
    print(f"{archive.relative_to(ROOT)} ({len(files)} arquivos)")


if __name__ == "__main__":
    main()
