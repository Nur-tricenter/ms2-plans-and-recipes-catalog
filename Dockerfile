# syntax=docker/dockerfile:1

# Multi-stage: restore with the SDK, ship only the published output on the ASP.NET runtime.
#
# BUILD CONTEXT IS THE PARENT FOLDER, not this repository:
#
#   cd <parent>
#   docker build -f ms2-plans-and-recipes-catalog/Dockerfile -t <image> .
#
# The reason is the shared kernels. Catalog.Application and Catalog.Infrastructure reference
# ../nur-tricenter-shared-kernels/* by relative path, and Docker cannot COPY from outside the
# build context — so the context has to be the folder that contains BOTH repositories. Every
# path below is therefore prefixed with ms2-plans-and-recipes-catalog/.
#
# Two more things this file has to get right:
#
#  1. The SDK and runtime tags must match this service's TargetFramework (net8.0). An sdk:10.0
#     tag building a net8.0 project restores fine and then fails at run time with a missing
#     framework, which reads like a publish problem rather than a tag problem.
#
#  2. Every project reachable through a ProjectReference must be copied BEFORE restore, or NuGet
#     cannot resolve it. That includes contracts/ and the shared kernels.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# --- Central package versions ---------------------------------------------------
# Both repos manage versions centrally, and NuGet reads these before restore, so they must be
# in place before any project manifest is restored.
COPY nur-tricenter-shared-kernels/Directory.Packages.props nur-tricenter-shared-kernels/
COPY ms2-plans-and-recipes-catalog/Directory.Packages.props ./

# --- Shared kernels (sibling repository, referenced by path) --------------------
COPY nur-tricenter-shared-kernels/nur-tricenter-access nur-tricenter-shared-kernels/nur-tricenter-access
COPY nur-tricenter-shared-kernels/nur-tricenter-integration nur-tricenter-shared-kernels/nur-tricenter-integration
COPY nur-tricenter-shared-kernels/nur-tricenter-observability nur-tricenter-shared-kernels/nur-tricenter-observability

# --- Project manifests ---------------------------------------------------------
COPY ms2-plans-and-recipes-catalog/contracts/Catalog.Contracts.csproj ms2-plans-and-recipes-catalog/contracts/
COPY ms2-plans-and-recipes-catalog/src/Catalog.Domain/Catalog.Domain.csproj ms2-plans-and-recipes-catalog/src/Catalog.Domain/
COPY ms2-plans-and-recipes-catalog/src/Catalog.Application/Catalog.Application.csproj ms2-plans-and-recipes-catalog/src/Catalog.Application/
COPY ms2-plans-and-recipes-catalog/src/Catalog.Infrastructure/Catalog.Infrastructure.csproj ms2-plans-and-recipes-catalog/src/Catalog.Infrastructure/
COPY ms2-plans-and-recipes-catalog/src/Catalog.App/Catalog.App.csproj ms2-plans-and-recipes-catalog/src/Catalog.App/

RUN dotnet restore ms2-plans-and-recipes-catalog/src/Catalog.App/Catalog.App.csproj

# --- Sources -------------------------------------------------------------------
COPY ms2-plans-and-recipes-catalog/ ms2-plans-and-recipes-catalog/
COPY nur-tricenter-shared-kernels/nur-tricenter-access/ nur-tricenter-shared-kernels/nur-tricenter-access/
COPY nur-tricenter-shared-kernels/nur-tricenter-integration/ nur-tricenter-shared-kernels/nur-tricenter-integration/
COPY nur-tricenter-shared-kernels/nur-tricenter-observability/ nur-tricenter-shared-kernels/nur-tricenter-observability/

RUN dotnet publish ms2-plans-and-recipes-catalog/src/Catalog.App/Catalog.App.csproj \
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