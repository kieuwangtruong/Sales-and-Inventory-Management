# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY UserAuthService.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# ── Runtime stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Chờ SQL Server sẵn sàng (dùng wait-for-it hoặc retry trong code)
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 5001
ENTRYPOINT ["dotnet", "UserAuthService.dll"]
