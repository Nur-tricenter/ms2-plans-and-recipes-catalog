FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY src/Catalog.Domain/Catalog.Domain.csproj src/Catalog.Domain/
COPY src/Catalog.Application/Catalog.Application.csproj src/Catalog.Application/
COPY src/Catalog.Infrastructure/Catalog.Infrastructure.csproj src/Catalog.Infrastructure/
COPY src/Catalog.WebApi/Catalog.WebApi.csproj src/Catalog.WebApi/
RUN dotnet restore src/Catalog.WebApi/Catalog.WebApi.csproj

COPY . .
RUN dotnet publish src/Catalog.WebApi/Catalog.WebApi.csproj -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app


RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .


ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Catalog.WebApi.dll"]
