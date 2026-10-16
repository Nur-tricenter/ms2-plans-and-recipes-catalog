<img width="1553" height="754" alt="image" src="https://github.com/user-attachments/assets/99e61c6a-8fba-446b-a38e-9c9b98230f30" />

# ms2-plans-and-recipes-catalog

Microservicio de **Catálogo de Alimentos, Recetas y Planes Alimentarios** para un sistema de nutrición y meal-planning.

## Propósito

Gestionar y exponer un catálogo completo de alimentos con su información nutricional, recetas compuestas por dichos alimentos, y planes alimentarios que organizan recetas en días y tiempos de comida.

## Funcionalidades

### Alimentos
- CRUD de alimentos con información nutricional (calorías, proteínas, carbohidratos, grasas), unidad de medida y categoría.
- Búsqueda de alimentos por categoría.
- Publicación de evento de integración al actualizar datos nutricionales.

### Recetas
- CRUD de recetas con nombre e instrucciones.
- Agregar y remover ingredientes (alimento + porción).
- Cálculo de información nutricional total de una receta basado en sus ingredientes.

### Planes Alimentarios
- CRUD de planes con duración (15 o 30 días) y número de comidas por día.
- Generación automática de los días del plan al crearlo.
- Gestión de tiempos de comida (desayuno, almuerzo, cena, etc.) por día.
- Asignación de recetas con raciones a cada tiempo de comida.
- Obtención de la composición completa del plan (plan > días > tiempos de comida > recetas > ingredientes).
- Publicación de evento de integración al crear un plan.

## Stack Tecnológico

| Componente | Tecnología |
|------------|------------|
| Framework | .NET (ASP.NET Core Web API) |
| Arquitectura | DDD + Clean Architecture |
| Patrón CQRS | MediatR |
| ORM | Entity Framework Core |
| Base de datos | PostgreSQL (Docker) |
| Documentación API | Swagger / OpenAPI |
| Patrones | Repository, Unit of Work, Factory, Result |

## Estructura del Proyecto

```
ms2-plans-and-recipes-catalog/
├── Domain/          # Entidades, Value Objects, Eventos, Repositorios (interfaces)
├── Application/     # Casos de uso (Commands/Queries), DTOs, Handlers
├── Infrastructure/  # Persistencia (EF Core DbContexts), Repositorios (implementación)
├── WebApi/          # Controladores REST, Middleware, Startup
├── Shared/          # Tipos compartidos (Result, PagedList, etc.)
└── Test/            # Pruebas unitarias e integración
```

## API Endpoints

### Alimentos (`/api/v1/alimentos`)
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/alimentos` | Crear alimento |
| GET | `/alimentos/{id}` | Obtener alimento por ID |
| GET | `/alimentos` | Listar alimentos (paginado) |
| GET | `/alimentos/categoria/{categoria}` | Buscar por categoría (paginado) |
| PUT | `/alimentos` | Actualizar alimento |

### Recetas (`/api/v1/recetas`)
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/recetas` | Crear receta |
| POST | `/recetas/ingredientes` | Agregar ingredientes |
| DELETE | `/recetas/{recetaId}/ingredientes/{alimentoId}` | Remover ingrediente |
| GET | `/recetas/{id}` | Obtener receta por ID |
| GET | `/recetas` | Listar recetas (paginado) |
| GET | `/recetas/{recetaId}/info-nutricional` | Obtener info nutricional |

### Planes Alimentarios (`/api/v1/planes`)
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/planes` | Crear plan |
| POST | `/planes/tiempos-comida` | Agregar tiempo de comida |
| POST | `/planes/asignar-recetas` | Asignar recetas a tiempo de comida |
| DELETE | `/planes/{planId}/tiempos-comida/{tiempoComidaId}/recetas/{recetaId}` | Remover receta de tiempo de comida |
| GET | `/planes/{planId}` | Obtener composición del plan |
| GET | `/planes` | Listar planes |

## Requisitos

| Herramienta | Versión |
|-------------|---------|
| Docker Desktop | Última estable (recomendado) |
| .NET SDK | 10.x (solo si quieres correrlo directamente) |

## Cómo ejecutar el proyecto

### Opción 1 — Docker Compose (recomendada)

Levanta la API y la base de datos PostgreSQL en un solo comando desde la raíz del proyecto:

```bash
docker compose up -d
```

Al primer arranque se crea la base de datos, se aplican las migraciones automáticamente y se cargan los datos de ejemplo.

| Recurso | URL |
|---------|-----|
| Documentación interactiva (Swagger) | http://localhost:8080/swagger |
| API | http://localhost:8080/api/v1/... |
| PostgreSQL | localhost:5433 |

Para detener el stack:

```bash
docker compose down
```

### Opción 2 — Ejecución directa con el SDK de .NET

Requiere un servidor PostgreSQL disponible (por ejemplo, el del `docker compose`) y la cadena de conexión configurada en `WebApi/appsettings.json`:

```bash
dotnet restore
dotnet run --project src/Catalog.WebApi/Catalog.WebApi.csproj
```

Al arrancar, la aplicación aplica las migraciones pendientes automáticamente. Swagger queda disponible en `http://localhost:5000/swagger` (o el puerto definido en `launchSettings.json`).

## Pruebas

El proyecto de tests vive en `Test/` (xUnit + Moq para unit tests, WebApplicationFactory para integration tests y PactNet para contract testing):

```
Test/
├── UnitTest/          # Unit tests: Domain (entidades, value objects, factories) y Application (handlers CQRS)
├── IntegrationTest/   # Integration tests: flujos HTTP completos por controlador (WebApplicationFactory + EF InMemory)
└── ContractTest/      # Contract testing con Pact: consumer + provider (pact committeado en pacts/)
```

Las reglas y skills usados para generar/verificar los tests están en [`ai/`](ai/): `ai/project/*.md` (reglas de unit, integration y pact) y `ai/skill/*` (skills generadoras y verificadoras).

### Ejecutar la suite completa

```bash
dotnet test
```

### Ejecutar con reporte de code coverage

```bash
dotnet test Test/Test.csproj --collect:"XPlat Code Coverage" --results-directory Test/coverage
```

El reporte `coverage.cobertura.xml` se genera dentro de `Test/coverage/` (carpeta excluida de git).

### Ver el reporte de coverage en HTML

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool   # una sola vez
reportgenerator "-reports:Test\coverage\**\coverage.cobertura.xml" "-targetdir:Test\coverage\report" "-reporttypes:Html"
```

Luego abrir `Test/coverage/report/index.html` en el navegador.

Cobertura objetivo (según `ai/project/unit-testing-rules.md`): Domain ≥ 90%, Application ≥ 80%, Value Objects ≥ 95%.
