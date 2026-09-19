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
6. **STRICT BACKEND RULE**: DO NOT write endpoints directly or inline inside `backend/src/Backend.Api/Program.cs`.
   - `Program.cs` is strictly reserved for service registration, middleware pipeline, and calling modular `app.MapXEndpoints()` extension methods.
   - All new endpoints must be placed inside the appropriate domain file under `backend/src/Backend.Api/Endpoints/<Domain>Endpoints.cs` (or create a new `Endpoints/<Feature>Endpoints.cs` if introducing a new bounded context).
   - Place all new DTOs in `Backend.Application/DTOs/`, never at the bottom of `Program.cs`.
   - See `docs/agent_backend_guidelines.md` for complete architecture patterns and templates.
7. **SECURITY RULE**: Every sensitive endpoint (Admin functions, AI grading invocation, and user data mutations) MUST be protected by `[Authorize]` (or `[Authorize(Roles = "admin")]`) and appropriate Rate Limiting.
