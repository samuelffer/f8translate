#!/usr/bin/env python3
"""Mantém só o conjunto recém-publicado, restrito à mesma branch/workflow."""
import json
import os
import urllib.request

CURRENT_NAMES = {"F8Translate-compact-win-x64", "F8Translate-fontes"}

# Nomes antigos permanecem elegíveis para limpeza, não para publicação.
NAMES = {
    "F8Translate-win-x64",  # nome do pacote legado
    "F8Translate-compact-win-x64",
    "F8Translate-portable-win-x64",
    "F8Translate-fontes",
}


def belongs_to_previous_delivery(artifact, current, candidate):
    return (
        artifact["name"] in NAMES
        and artifact.get("workflow_run", {}).get("id") != current["id"]
        and candidate.get("head_branch") == current.get("head_branch")
        and candidate.get("workflow_id") == current.get("workflow_id")
        and candidate.get("repository", {}).get("id") == current.get("repository", {}).get("id")
        and candidate.get("event") in {"push", "workflow_dispatch"}
    )


def has_current_delivery(artifacts, run_id):
    published = {a["name"] for a in artifacts if a.get("workflow_run", {}).get("id") == run_id}
    return CURRENT_NAMES <= published


def main():
    # O token é fornecido apenas ao passo de limpeza, nunca persistido/exportado.
    repo = os.environ["GITHUB_REPOSITORY"]
    token = os.environ["GH_TOKEN"]
    run_id = int(os.environ["GITHUB_RUN_ID"])
    base = f"https://api.github.com/repos/{repo}"

    def request(path, method="GET"):
        req = urllib.request.Request(base + path, method=method, headers={
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
        })
        with urllib.request.urlopen(req, timeout=30) as response:
            body = response.read()
            return json.loads(body) if body else None

    current = request(f"/actions/runs/{run_id}")
    artifacts = []
    page = 1
    while True:
        batch = request(f"/actions/artifacts?per_page=100&page={page}")["artifacts"]
        artifacts.extend(batch)
        if len(batch) < 100:
            break
        page += 1

    # Não apagar enquanto faltar qualquer um dos dois pacotes atuais.
    if not has_current_delivery(artifacts, run_id):
        raise RuntimeError("Pacotes atuais incompletos; limpeza cancelada por segurança.")

    runs = {}
    deleted = 0
    for artifact in artifacts:
        owner = artifact.get("workflow_run", {})
        old_id = owner.get("id")
        if artifact["name"] not in NAMES or not old_id or old_id == run_id:
            continue
        if owner.get("head_branch") != current["head_branch"]:
            continue
        if old_id not in runs:
            runs[old_id] = request(f"/actions/runs/{old_id}")
        if belongs_to_previous_delivery(artifact, current, runs[old_id]):
            request(f'/actions/artifacts/{artifact["id"]}', method="DELETE")
            deleted += 1
    print(f"Limpeza concluída: {deleted} artefato(s) antigo(s) da mesma branch/workflow removido(s).")


if __name__ == "__main__":
    main()
