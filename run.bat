@echo off
start cmd /k "cd backend\src\Backend.Api && dotnet watch run"
start cmd /k "cd frontend\src\Frontend.App && dotnet watch run"