# ====================================================================
# DOCKERFILE - BACKEND ASP.NET CORE (.NET 10)
# Multi-stage build tối ưu kích thước và bảo mật
# ====================================================================

# STAGE 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Sao chép các file .csproj trước để tận dụng Docker layer caching
COPY ["backend/src/Backend.Domain/Backend.Domain.csproj", "backend/src/Backend.Domain/"]
COPY ["backend/src/Backend.Application/Backend.Application.csproj", "backend/src/Backend.Application/"]
COPY ["backend/src/Backend.Infrastructure/Backend.Infrastructure.csproj", "backend/src/Backend.Infrastructure/"]
COPY ["backend/src/Backend.Api/Backend.Api.csproj", "backend/src/Backend.Api/"]

# Khôi phục các gói NuGet
RUN dotnet restore "backend/src/Backend.Api/Backend.Api.csproj"

# Sao chép toàn bộ mã nguồn backend và biên dịch Release
COPY backend/ ./backend/
WORKDIR "/src/backend/src/Backend.Api"
RUN dotnet publish "Backend.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 2: Runtime Image (Siêu nhẹ)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Thiết lập cổng lắng nghe chuẩn Cloud Container (8080)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Sao chép kết quả đã build từ Stage 1
COPY --from=build /app/publish .

# Khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "Backend.Api.dll"]
