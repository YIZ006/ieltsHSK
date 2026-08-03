# ieltsHSK Monorepo

This repository contains two independent .NET 9 applications in one Git repository:

- `frontend/` — Blazor WebAssembly app
- `backend/` — ASP.NET Core Web API (Clean Architecture style)

No shared solution is used and there are no project references between frontend and backend.

## Solutions

- `/home/runner/work/ieltsHSK/ieltsHSK/frontend/Frontend.sln`
- `/home/runner/work/ieltsHSK/ieltsHSK/backend/Backend.sln`

## Build

```bash
dotnet restore /home/runner/work/ieltsHSK/ieltsHSK/frontend/Frontend.sln
dotnet restore /home/runner/work/ieltsHSK/ieltsHSK/backend/Backend.sln
dotnet build /home/runner/work/ieltsHSK/ieltsHSK/frontend/Frontend.sln -c Release
dotnet build /home/runner/work/ieltsHSK/ieltsHSK/backend/Backend.sln -c Release
```

Build outputs are generated under each project `bin/Release/net9.0/` directory.

## Run

Backend:

```bash
dotnet run --project /home/runner/work/ieltsHSK/ieltsHSK/backend/src/Backend.Api/Backend.Api.csproj --launch-profile https
```

Frontend:

```bash
dotnet run --project /home/runner/work/ieltsHSK/ieltsHSK/frontend/src/Frontend.App/Frontend.App.csproj --launch-profile https
```

Default local URLs:

- Backend: `https://localhost:7101` (`http://localhost:5101`)
- Frontend: `https://localhost:7102` (`http://localhost:5102`)