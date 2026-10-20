# syntax=docker/dockerfile:1

# Multi-stage: restore with the SDK, ship only the published output on the ASP.NET runtime.
#
# BUILD CONTEXT IS THIS REPOSITORY, not the parent folder:
#
#   docker build -t <image> .
#
# The platform kernels used to be a sibling repository copied into this image. They are NuGet
# packages now, published to GitHub Packages, so nothing outside this folder is needed: the
# context is exactly the repository and every project reference resolves through restore.
#
# Two things this file has to get right, and both fail in ways that read like a different
# problem than they are:
#
#  1. The SDK and runtime tags must match this service's TargetFramework (net8.0). An sdk:10.0
#     tag building a net8.0 project restores fine and then dies at run time with a missing
#     framework.
#
#  2. NuGet.config carries the organization feed, and restore needs credentials for a private
#     feed. They arrive as build secrets — never bake a token into an image layer.

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
COPY src/Catalog.App/Catalog.App.csproj src/Catalog.App/

# The secret is mounted by id only; src comes from the docker build command line. Declaring src
# in BOTH places fails with "both source and id can't be set".
#
# The credentials are exported rather than inlined: NuGet expands %NUGET_USERNAME% and
# %NUGET_AUTH_TOKEN% from the environment when it reads NuGet.config.
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_token \
    export NUGET_USERNAME="$(cat /run/secrets/nuget_username)" && \
    export NUGET_AUTH_TOKEN="$(cat /run/secrets/nuget_token)" && \
    dotnet restore src/Catalog.App/Catalog.App.csproj

# --- Sources and publish ------------------------------------------------------
COPY . .

RUN dotnet publish src/Catalog.App/Catalog.App.csproj \
        -c Release \
        -o /app/publish \
        --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl backs the container healthcheck. The aspnet image does not ship it, and a healthcheck
# whose command is missing fails with "executable file not found" rather than reporting
# unhealthy, which reads as a mystery until you inspect the container.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Catalog.App.dll"]