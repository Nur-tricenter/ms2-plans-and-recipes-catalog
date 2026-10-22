# syntax=docker/dockerfile:1

# Multi-stage: restore with the SDK, ship only the published output on the ASP.NET runtime.
#
#   docker build -f src/Catalog.WorkerService/Dockerfile -t ms2-plans-and-recipes-catalog-worker .
#
# BUILD CONTEXT IS THIS REPOSITORY, not the parent folder:
#
#   The platform kernels used to be a sibling repository copied into this image. They are NuGet
#   packages now, published to GitHub Packages, so nothing outside this folder is needed: the
#   context is exactly the repository and every project reference resolves through restore.
#
# Same two constraints as the API image, and for the same reasons:
#
#  1. The SDK and runtime tags must match this service's TargetFramework (net8.0). An sdk:10.0 tag
#     building a net8.0 project restores fine and then dies at run time on a missing framework.
#
#  2. NuGet.config carries the organization feed, and restore needs credentials for a private feed.
#     They arrive as build secrets — never bake a token into an image layer. The variable is
#     NUGET_AUTH_TOKEN, which is what this repository's NuGet.config expands.
#
# No curl in the runtime image: this host serves no HTTP, so its healthcheck reads the process
# table instead of probing a port that does not exist.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# --- Restore layer ------------------------------------------------------------
# Only the manifests first, so editing source does not invalidate the NuGet cache.
# Directory.Packages.props comes first: central package management reads the versions from it,
# so NuGet needs it before any manifest is restored.
COPY Directory.Packages.props NuGet.config ./
COPY contracts/Catalog.Contracts.csproj contracts/
COPY src/Catalog.Domain/Catalog.Domain.csproj src/Catalog.Domain/
COPY src/Catalog.Application/Catalog.Application.csproj src/Catalog.Application/
COPY src/Catalog.Infrastructure/Catalog.Infrastructure.csproj src/Catalog.Infrastructure/
COPY src/Catalog.WorkerService/Catalog.WorkerService.csproj src/Catalog.WorkerService/

RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_token \
    export NUGET_USERNAME="$(cat /run/secrets/nuget_username)" && \
    export NUGET_AUTH_TOKEN="$(cat /run/secrets/nuget_token)" && \
    dotnet restore src/Catalog.WorkerService/Catalog.WorkerService.csproj

# --- Sources and publish ------------------------------------------------------
COPY . .

RUN dotnet publish src/Catalog.WorkerService/Catalog.WorkerService.csproj \
        -c Release \
        -o /app/publish \
        --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Catalog.WorkerService.dll"]