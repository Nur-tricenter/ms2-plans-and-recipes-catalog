<img width="1553" height="754" alt="image" src="https://github.com/user-attachments/assets/99e61c6a-8fba-446b-a38e-9c9b98230f30" />

# ms2-plans-and-recipes-catalog

Microservice for a **food, recipe and meal-plan catalogue** in a nutrition and meal-planning
system. DDD + Clean Architecture, CQRS with a transactional outbox, .NET 8.

## Purpose

Manage and expose a catalogue of foods with their nutritional information, recipes composed from
those foods, and meal plans that organise recipes into days and meal times.

## Features

### Foods
- CRUD with nutritional information (calories, protein, carbs, fat), unit of measure and category.
- Search by category.
- Publishes an integration event when nutritional data changes.

### Recipes
- CRUD with name and instructions.
- Add and remove ingredients (food + portion).
- Computes total nutritional information from the ingredients.

### Meal plans
- CRUD with duration (15 or 30 days) and meals per day.
- Plan days are generated when the plan is created.
- Meal times (breakfast, lunch, dinner, …) per day.
- Recipes with rations assigned to each meal time.
- Full composition: plan → days → meal times → recipes → ingredients.
- Publishes an integration event when a plan is created.

## Stack

| Component | Technology |
|---|---|
| Runtime | .NET 8 (`net8.0`) |
| Architecture | DDD + Clean Architecture |
| CQRS | MediatR 14 |
| ORM | Entity Framework Core 9.0.3 |
| Database | PostgreSQL via Npgsql 9.0.4 |
| Messaging | RabbitMQ, via `Joseco.Communication` |
| Outbox | `Joseco.Outbox.EFCore` |
| Discovery | Consul |
| Telemetry | OpenTelemetry via `Nur.Tricenter.Observability` |
| API docs | Swagger / OpenAPI |
| Patterns | Repository, Unit of Work, Factory, Result |

Package versions are declared once in `Directory.Packages.props`. There is no pipeline that
publishes the image; see [Docker Hub](#docker-hub).

## Project structure

```
ms2-plans-and-recipes-catalog/
├── contracts/          # Catalog.Contracts: permissions, route table, integration messages
├── src/
│   ├── Catalog.Domain/         # Entities, value objects, domain events, repository interfaces
│   ├── Catalog.Application/    # Commands, queries, DTOs, handlers, behaviours
│   ├── Catalog.Infrastructure/ # DbContexts, repositories, projections, outbox, observability
│   ├── Catalog.App/            # Controllers, Startup, health, Consul registration
│   └── Catalog.WorkerService/  # Outbox processor, RabbitMQ publisher
├── Test/                       # Unit, infrastructure, worker, integration and Pact contract tests
├── Directory.Packages.props    # Every package version, declared once
└── NuGet.config                # Feed that serves the platform kernels
```

### Dependency rule

```
Domain          →  (NuGet only)
Application     →  Domain, Catalog.Contracts
Infrastructure  →  Application
Catalog.App     →  Infrastructure
WorkerService   →  Infrastructure
```

The worker references Infrastructure only. `AddInfrastructure` already calls `AddApplication`,
so a direct reference from the worker would be a second edge the two projects can drift apart on.

### Platform kernels

`Nur.Tricenter.Access` and `Nur.Tricenter.Observability` arrive as **NuGet packages** from the
organization's GitHub Packages feed, not as project references to a sibling folder. Nothing here
needs to know where `nur-tricenter-shared-kernels` lives on disk, which is what lets this
repository build on its own.

## CQRS

Nine commands and nine queries, with separate marker types. **Every query is served from
`StoredDbContext`**; no query handler injects a repository.

| Handler | Reads |
|---|---|
| `ListFoodsHandler` | `StoredDbContext.Foods` |
| `SearchFoodsByCategoryHandler` | `StoredDbContext.Foods` |
| `GetFoodByIdHandler` | `StoredDbContext.Foods` |
| `ListRecipesHandler` | `StoredDbContext.Recipes` |
| `GetRecipeByIdHandler` | `StoredDbContext.Recipes` + `.Foods` |
| `GetRecipeNutritionalInfoHandler` | `StoredDbContext.Recipes` + `.Foods` |
| `ListMealPlansHandler` | `StoredDbContext.MealPlans` |
| `GetMealPlanByIdHandler` | `StoredDbContext.MealPlans` + `.Recipes` |
| `GetMealPlanCompositionHandler` | `StoredDbContext.MealPlans` + `.Recipes` + `.Foods` |

### Persistence: one database, three schemas

| Schema | Tables |
|---|---|
| `write` | `Food`, `Recipe`, `RecipeIngredient`, `MealPlan`, `PlanDay`, `MealTime`, `RecipeAssignment` |
| `read` | the same seven, as projection models |
| `outbox` | `outboxMessage` |

Schema names live in one place, `Persistence/DatabaseSchemas.cs`, and every `ToTable` call
references them. Each context also sets `HasDefaultSchema`, so a mapping that forgets the explicit
`ToTable` still cannot collide.

The `outbox` schema is not this project's choice: `Joseco.Outbox.EFCore` declares
`OutboxMessageConfig<T>` as `internal` and calls `ToTable("outboxMessage", "outbox")` with no
extension point, so the schema is fixed by the library.

`PersistenceSchemaSeparationTests` fails if the two contexts ever map the same table name into the
same schema. That test is load-bearing because EF Core InMemory **cannot** catch this class of
defect — it gives each context its own store, so an entirely green suite says nothing about schema
collisions.

### The outbox chain

```
Command → DomainDbContext.SaveChanges()
        → domain event → outbox.outboxMessage   (same transaction)
        → WorkerService: OutboxProcessor<DomainEvent>
        → Project* handlers → StoredDbContext.SaveChanges()
        → PublishIntegrationEvent* → RabbitMQ
```

The chain has three links and no framework enforces them: MediatR discovers handlers by convention,
so a missing link is neither a compile error nor a DI error. It has happened — `FoodUpdated` had a
projection but no outbox handler, and the event was raised, drained, and dropped.

`OutboxChainCoverageTests` asserts that every domain event has both a `SaveInOutbox` handler and a
projection. The test is verified to fail when a handler is removed.

## API endpoints

### Foods (`api/v1/foods`)

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/foods` | Create a food |
| GET | `/api/v1/foods` | List foods, paged |
| GET | `/api/v1/foods/{id:guid}` | Get one food |
| GET | `/api/v1/foods/category/{category}` | Search by category |
| PUT | `/api/v1/foods` | Update a food — **404** on failure, not 400 |

### Recipes (`api/v1/recipes`)

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/recipes` | Create a recipe |
| GET | `/api/v1/recipes` | List recipes, paged |
| GET | `/api/v1/recipes/{id:guid}` | Get one recipe with its ingredients |
| POST | `/api/v1/recipes/ingredients` | Add an ingredient |
| DELETE | `/api/v1/recipes/{recipeId:guid}/ingredients/{foodId:guid}` | Remove an ingredient |
| GET | `/api/v1/recipes/{recipeId:guid}/nutritional-info` | Computed nutritional totals |

### Meal plans (`api/v1/meal-plans`)

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/meal-plans` | Create a plan |
| GET | `/api/v1/meal-plans` | List plans |
| GET | `/api/v1/meal-plans/{planId:guid}` | Full composition |
| POST | `/api/v1/meal-plans/meal-times` | Add a meal time to a day |
| POST | `/api/v1/meal-plans/assign-recipes` | Assign a recipe with a ration |
| DELETE | `/api/v1/meal-plans/{planId}/meal-times/{mealTimeId}/recipes/{recipeId}` | Unassign |

### Status codes

- A command that fails a business rule or validation → **400**
- A read that finds nothing → **404** (including `PUT /api/v1/foods`, which is 404 and not 400)
- `[ApiController]` model validation → **400 `ProblemDetails`** before the handler runs
- There is **no** global exception middleware: handlers catch and return `Result.Fail`; an
  unhandled exception surfaces as a plain **500**

## Migrations

The schema is applied by an **explicit step**, never on boot:

```bash
dotnet run --project src/Catalog.App/Catalog.App.csproj -- --migrate
# or against the published binary:
dotnet Catalog.App.dll --migrate
```

It applies the write-side migrations (`DomainDbContext`) then the read-side ones
(`StoredDbContext`). It exits `0` on success and `1` on failure, so an orchestrator can gate on it.
It is idempotent: a second run applies nothing.

For local development, `Migrations:ApplyOnStartup: true` in `appsettings.Development.json` removes
the ceremony from `dotnet run`. **Do not set it in a deployment.**

## Running locally

### Option 1 — the orchestrator (recommended)

The orchestrator repository brings up the whole platform — infrastructure plus this service and
the gateway — with per-service compose profiles:

```bash
cd <folder containing nur-tricenter-orchestrator>
./start-stack.sh          # or start-stack.ps1 on Windows
```

That is the intended workflow: bind-mounted local binaries, no image build required.

### Option 2 — Docker Compose in this repository

Brings up PostgreSQL, applies the schema in a one-shot step, and starts the API:

```bash
# 1. Local configuration, once (creates a git-ignored appsettings.Development.json)
cp src/Catalog.App/appsettings.Development.template.json src/Catalog.App/appsettings.Development.json

# 2. Start the stack
docker compose up -d
```

`webapi-migrate` runs `dotnet Catalog.App.dll --migrate`; `webapi` waits for it to succeed
(`service_completed_successfully`) before starting. The API never migrates or seeds on its own.

> The migration step is safe on an empty database. If the `catalog-db-data` volume already holds an
> old schema, recreate it with `docker compose down -v` first: both histories start from zero and do
> not migrate a pre-existing schema.

### Option 3 — local run

```bash
cp src/Catalog.WorkerService/appsettings.Development.template.json src/Catalog.WorkerService/appsettings.Development.json
dotnet run --project src/Catalog.App/Catalog.App.csproj
```

The worker needs a RabbitMQ broker. In this repository's compose there is none, so outbox rows
accumulate without being published; the orchestrator provides one.

## Docker Hub

The compose consumes `robertperezlino/ms2-plans-and-recipes-catalog:latest`. **No pipeline
publishes it** — build and push by hand. `:latest` is whatever was pushed last and does not
guarantee it matches your working tree.

```bash
# 1. Credentials for the private feed, once. No byte-order mark: a BOM in NuGet.config
#    makes NuGet ignore the whole file, including the credentials block.
dotnet nuget add source https://nuget.pkg.github.com/Nur-tricenter/index.json \
  --name Nur-tricenter --username YOUR_USER --password YOUR_TOKEN
setx NUGET_USERNAME YOUR_USER
setx NUGET_AUTH_TOKEN YOUR_TOKEN

# 2. Build. The context is THIS repository: the kernels are packages, not a sibling folder.
docker build --secret id=nuget_username,src=%TEMP%/nu_user.txt \
             --secret id=nuget_token,src=%TEMP%/nu_tok.txt \
             -t robertperezlino/ms2-plans-and-recipes-catalog:latest .

# 3. Push
docker push robertperezlino/ms2-plans-and-recipes-catalog:latest
```

Both secret files must be **UTF-8 without a BOM**; PowerShell's `Set-Content -Encoding UTF8` adds
one, and a BOM in the username makes the Basic auth fail with a `401` that blames the token.

The image declares no `ASPNETCORE_ENVIRONMENT`; the compose sets `Production`. For your own
deployment, export the configuration variables — above all
`ConnectionStrings__CatalogDbConnectionString` — and leave `Migrations__ApplyOnStartup` `false`:
the schema belongs in the explicit `--migrate` step, not in a web process boot.

## Configuration

| Key | Where | Meaning |
|---|---|---|
| `ConnectionStrings:CatalogDbConnectionString` | env var or local json | **Required.** One database serves both sides. |
| `Migrations:ApplyOnStartup` | `appsettings.json`, default `false` | Runs the migration on boot. Local development only. |
| `Seed:Enabled` | `appsettings.json`, default `false` | Sample catalogue data (30 foods, 25 recipes, 2 plans). |
| `Swagger:Enabled` | `appsettings.json`, default `false` | Serves the OpenAPI document. |
| `HttpsRedirection:Enabled` | `appsettings.json`, default `true` | Off when TLS terminates upstream. |
| `RabbitMqSettings:*` | `RabbitMqSettings` | Host, credentials, virtual host, retry policy. |
| `ServiceRegistration:ServiceDiscoveryAddress` | env var | Consul URL. **Enables registration and the database health check when set.** |
| `Jwt` | — | Not used. Authentication is out of scope by decision. |

**No credentials belong in version-controlled files.** The connection string is absent from every
tracked `appsettings.json` and arrives from an environment variable or from a local, git-ignored
`appsettings.Development.json`. `TrackedConfigurationTests` fails if that changes.

## Tests

454 tests, all green.

```bash
dotnet test Test/Test.csproj                                     # everything
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.Domain"
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.Application"
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.IntegrationTest"
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.ContractTest"
dotnet test Test/Test.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage
```

Three RabbitMQ tests skip themselves when no broker is reachable, which is the correct behaviour:
they are the end-to-end proof that the outbox is published, and they need a broker.

> The agent-facing documents under `ai/` predate the current codebase. See `ai/README.md` for the
> full list of what changed and which statements no longer hold.

## Resources

| Resource | URL |
|---|---|
| Swagger | http://localhost:8082/swagger |
| Health | http://localhost:8082/health |
| API | http://localhost:8082/api/v1/… |
| PostgreSQL | localhost:5433 |

Through the gateway, the public entry point, routes are prefixed by service:

```
POST http://localhost:15005/api/catalog/v1/foods
GET  http://localhost:15005/api/catalog/v1/foods
```