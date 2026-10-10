FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY src/Shared/Shared.csproj src/Shared/
COPY src/Domain/Catalog.Domain.csproj src/Domain/
COPY src/Application/Catalog.Application.csproj src/Application/
COPY src/Infrastructure/Catalog.Infrastructure.csproj src/Infrastructure/
COPY src/WebApi/WebApi.csproj src/WebApi/
RUN dotnet restore src/WebApi/WebApi.csproj

COPY . .
RUN dotnet publish src/WebApi/WebApi.csproj -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app


RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .


ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]