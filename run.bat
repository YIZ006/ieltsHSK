@echo off
start "Backend API" cmd /k "dotnet watch run --project backend\src\Backend.Api\Backend.Api.csproj"
start "Frontend App" cmd /k "dotnet watch run --project frontend\src\Frontend.App\Frontend.App.csproj"
