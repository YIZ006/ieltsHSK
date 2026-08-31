# Stage 1: Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching dependency restore
COPY backend/src/Backend.Domain/Backend.Domain.csproj backend/src/Backend.Domain/
COPY backend/src/Backend.Application/Backend.Application.csproj backend/src/Backend.Application/
COPY backend/src/Backend.Infrastructure/Backend.Infrastructure.csproj backend/src/Backend.Infrastructure/
COPY backend/src/Backend.Api/Backend.Api.csproj backend/src/Backend.Api/
COPY backend/Backend.sln backend/

RUN dotnet restore backend/Backend.sln

# Copy all source files and publish
COPY backend/ backend/
WORKDIR /src/backend/src/Backend.Api
RUN dotnet publish Backend.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Environment optimizations for container runtime
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_gcServer=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "Backend.Api.dll"]
