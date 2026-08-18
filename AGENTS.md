# Repo Rules for Agents

1. Keep commits focused on source changes only.
2. Do not stage or commit temporary scratch files from the repository root, including:
   - `fix_json.js`
   - `fix_json.py`
   - `output.json`
   - `part*.html`
   - root-level loose `*.json` and `*.html` drafts
3. Keep generated test assets under the app-owned folders, such as `frontend/src/Frontend.App/wwwroot/sample-data/`, when they are meant to be part of the app.
4. Prefer updating existing tracked files over introducing parallel draft copies with similar names.
5. Before committing, run `git status --short` and verify no scratch files are included.
