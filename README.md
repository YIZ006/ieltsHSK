# ieltsHSK Monorepo

This repository contains two independent .NET 9 applications in one Git repo:

- `frontend/` - Blazor WebAssembly app
- `backend/` - ASP.NET Core Web API using a Clean Architecture layout

The two apps are deployed and built separately. There are no project references between them.

## What The Project Does

This project is an IELTS learning platform with:

- IELTS mock tests for Listening, Reading, Writing, and Speaking
- admin pages for creating and managing mock tests
- public exam content stored as JSON
- answer keys stored as separate JSON files
- test submissions stored in the backend database
- Cloudflare R2 integration for hosting exam and answer files

## Repository Layout

- `frontend/`
  - Blazor UI
  - IELTS exam pages and admin pages
  - static sample data under `frontend/src/Frontend.App/wwwroot/sample-data/`
  - local browser-side scoring and answer-key loading
- `backend/`
  - API endpoints
  - domain entities
  - application DTOs and abstractions
  - persistence and EF Core migrations
  - R2 upload service and auth service
- `AGENTS.md`
  - repo rules for agents and cleanup policy

## Key Flow

Mock test flow:

1. Admin uploads or pastes the exam JSON URL.
2. Admin uploads or pastes the answer-key JSON URL.
3. The frontend opens the test using those public links.
4. The user answers questions in the browser.
5. On submit, the frontend loads the answer key and grades the attempt.
6. The result is saved to the backend via `/api/test-submissions`.

For Listening and Reading, answer keys are separate files from the exam JSON.

## Solutions

- `frontend/Frontend.sln`
- `backend/Backend.sln`

## Build

Build each app separately:

```bash
dotnet restore frontend/Frontend.sln
dotnet restore backend/Backend.sln
dotnet build frontend/Frontend.sln -c Release
dotnet build backend/Backend.sln -c Release
```

Build outputs are written to each project `bin/Release/net9.0/` directory.

## Run

Backend:

```bash
dotnet run --project backend/src/Backend.Api/Backend.Api.csproj --launch-profile https
```

Frontend:

```bash
dotnet run --project frontend/src/Frontend.App/Frontend.App.csproj --launch-profile https
```

Default local URLs:

- Backend: `https://localhost:7101` and `http://localhost:5101`
- Frontend: `https://localhost:7102` and `http://localhost:5102`

## Sample Data

The frontend ships with local sample JSON files under:

- `frontend/src/Frontend.App/wwwroot/sample-data/`

These are useful for local testing without R2. The app also supports public Cloudflare R2 URLs for real mock-test content.

## Notes For Contributors

- Keep exam JSON and answer-key JSON as separate files.
- Keep root-level scratch files out of commits.
- Prefer editing tracked source files over creating parallel draft copies in the repo root.
- If you add new mock-test fixtures, put them under `frontend/src/Frontend.App/wwwroot/sample-data/` unless they are meant only as temporary local drafts.

## Useful Documents

- `speaking-audio-api-proxy.md` - notes for the speaking audio upload/proxy flow
- `walkthrough.md` - project walkthrough notes

