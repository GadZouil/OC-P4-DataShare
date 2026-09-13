FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY DataShare.Api/DataShare.Api.csproj DataShare.Api/
RUN dotnet restore DataShare.Api/DataShare.Api.csproj

COPY DataShare.Api/ DataShare.Api/
RUN dotnet publish DataShare.Api/DataShare.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# curl est utilise par le healthcheck Docker Compose (GET /api/health)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "DataShare.Api.dll"]
