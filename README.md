# ms2-plans-and-recipes-catalog

Microservice for a **food, recipe and meal-plan catalogue** in a nutrition and meal-planning system.
DDD + Clean Architecture, CQRS with a transactional outbox **and an idempotent inbox**, .NET 8.

This service is the **reference template** for every new microservice on the Nur-Tricenter platform: a new
service should mirror its shape (layers, outbox/inbox, migrations, Consul registration, kernels).

## Purpose

Manage and expose a catalogue of foods with their nutritional information, recipes composed from those
foods, and meal plans that organise recipes into days and meal times. It is also the platform's **first
consumer**: it projects identity's `UserPublished`/`UserChanged` events into a local `PlanAuthors` read
model, exercising the receiving half of the messaging pattern.

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
| ORM | Entity Framework Core 9.0.3/9.0.4 |
| Database | PostgreSQL (Npgsql 9.0.4), one DB, three schemas |
| Messaging | RabbitMQ via `Joseco.Communication.External.RabbitMQ` 1.1.0 |
| Outbox | `Joseco.Outbox.EFCore` 1.1.0 |
| Discovery | Consul 1.7.14.7 |
| Telemetry | OpenTelemetry via `Nur.Tricenter.Observability` 1.0.9 |
| Security | `Nur.Tricenter.Security` 1.0.9 (JWT bearer + one policy per permission) |
| Access | `Nur.Tricenter.Access` 1.0.9 (permission names + wildcard expansion) |
| Integration | `Nur.Tricenter.Integration` 1.0.9 (shared message contracts) |
| API docs | Swagger / OpenAPI (Swashbuckle 7.2.0) |
| Patterns | Repository, Unit of Work, Factory, Result, Outbox, Inbox |

Package versions are declared once in `Directory.Packages.props`. Kernels are restored from the GitHub
Packages feed `Nur-tricenter` (see `NuGet.config`).

## Project structure

```
ms2-plans-and-recipes-catalog/
├── contracts/          # (historical) Catalog.Contracts: permissions, route table, integration messages
├── src/
│   ├── Catalog.Domain/         # Entities, value objects, domain events, repository interfaces, factories
│   ├── Catalog.Application/    # Commands, queries, DTOs, handlers, behaviours, domain-event handlers
│   ├── Catalog.Infrastructure/ # DbContexts, repositories, projections, outbox, inbox, observability, Consul
│   ├── Catalog.App/            # Controllers, Startup, health, Consul registration, Program (--migrate)
│   └── Catalog.WorkerService/  # Outbox drain, RabbitMQ publisher, inbox consumer, default queue registrar
├── Test/                       # Unit, infrastructure, worker, integration and contract tests
├── Directory.Packages.props    # Every package version, declared once
├── NuGet.config                # Feed that serves the platform kernels
├── catalog.compose.yml         # Platform variant (included by the orchestrator)
├── docker-compose.yml          # Standalone stack (own Postgres + network)
└── opencode.json               # OpenCode agent config (test-writer subagent)
```

### Dependency rule

```
Domain          →  (NuGet only)
Application     →  Domain
Infrastructure  →  Application
Catalog.App     →  Infrastructure
WorkerService   →  Infrastructure
```

The worker references Infrastructure only. `AddInfrastructure` already calls `AddApplication`, so a direct
reference from the worker would be a second edge the two projects can drift apart on.

### Platform kernels

`Nur.Tricenter.Access`, `Nur.Tricenter.Observability`, `Nur.Tricenter.Security` and
`Nur.Tricenter.Integration` arrive as **NuGet packages** from the organization's GitHub Packages feed,
never as project references to a sibling folder. Nothing here needs to know where
`nur-tricenter-shared-kernels` lives on disk, which is what lets this repository build on its own.

## CQRS: one database, three schemas

| Schema | Tables |
|---|---|
| `write` | `Food`, `Recipe`, `RecipeIngredient`, `MealPlan`, `PlanDay`, `MealTime`, `RecipeAssignment` |
| `read`  | the same seven, as projection models, plus `PlanAuthors`, `InboxMessages` |
| `outbox`| `outboxMessage` (fixed by `Joseco.Outbox.EFCore`) |

Schema names live in one place, `Persistence/DatabaseSchemas.cs`, and every `ToTable` call references
them. Each context also sets `HasDefaultSchema`, so a mapping that forgets the explicit `ToTable` still
cannot collide. `PersistenceSchemaSeparationTests` fails if the two contexts ever map the same table name
into the same schema (EF Core InMemory cannot catch this class of defect).

## The outbox chain

```
Command → DomainDbContext.SaveChanges()
        → domain event → outbox.outboxMessage   (same transaction)
        → WorkerService: OutboxProcessor<DomainEvent>
        → Project* handlers → StoredDbContext.SaveChanges()
        → PublishIntegrationEvent* → RabbitMQ
```

The chain has three links and no framework enforces them: MediatR discovers handlers by convention, so a
missing link is neither a compile error nor a DI error. `OutboxChainCoverageTests` asserts that every
domain event has both a `SaveInOutbox` handler and a projection. It has happened — `FoodUpdated` had a
projection but no outbox handler, and the event was raised, drained, and dropped.

## The inbox (consumer side)

The catalog is also a consumer: it projects identity's `UserPublished` into the `PlanAuthors` read model.

```
RabbitMQ delivery
  → TracingConsumer<UserPublished, …>   re-attaches the publisher's span
       → IdempotentConsumer<UserPublished, …>   claims the message in the inbox
            → UserPublishedConsumer   projects it (upsert)
```

- **`InboxMessage`** (`read.InboxMessages`) is the atomic claim: the primary key is the publisher's
  message id, so a redelivery collides on insert and the handler is skipped.
- **Claim commits before the work**: a crash between claiming and completing loses that message's effect
  (the row exists, so the redelivery is skipped). For a projection this is recoverable by replaying the
  exchange; for a non-idempotent side effect this decorator is the wrong tool.
- **Tracing is outside the inbox** on purpose: a redelivery the inbox skips must still appear in the
  trace (it is a real delivery, and a redelivery storm is what an operator looks for).
- **`UserPublishedConsumer` upserts**: redelivery produces no duplicate row and no error. Rows are kept
  (never deleted) so a late duplicate is still rejected.

## Events

### Published (exchanges, fanout, non-durable — must match `Joseco.Communication.External.RabbitMQ 1.1.0`)

| Exchange | Trigger |
|---|---|
| `food-published` | Food created |
| `food-nutritional-info-updated` | Food nutritional data changed |
| `recipe-published` | Recipe created |
| `recipe-updated` | Recipe updated / ingredient added or removed |
| `meal-plan-published` | Meal plan created |

### Consumed

| Exchange | Queue | Consumer chain | Effect |
|---|---|---|---|
| `user-published` | `ms2.user-published` | Tracing → Idempotent → `UserPublishedConsumer` | Upsert `PlanAuthors` (read model) |

The queue name must match `orchestrator/topology/rabbitmq-definitions.json` (`{consumer}.{exchange}`).
A typo silently creates a second queue that receives the same messages.

### Default queue (never consumed)

`catalog.default` is declared and bound to every catalog exchange by `DefaultQueueRegistrar`. It is a
**passive sink** — the complete record of everything published. It is durable and never consumed; do not
"improve" it into `AddRabbitMqConsumer` (competing consumers would destroy the record).

## API endpoints

### Foods (`/api/v1/foods`)

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/foods` | Create a food |
| GET | `/api/v1/foods` | List foods, paged |
| GET | `/api/v1/foods/{id:guid}` | Get one food |
| GET | `/api/v1/foods/category/{category}` | Search by category |
| PUT | `/api/v1/foods` | Update a food — **404** on failure, not 400 |

### Recipes (`/api/v1/recipes`)

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/recipes` | Create a recipe |
| GET | `/api/v1/recipes` | List recipes, paged |
| GET | `/api/v1/recipes/{id:guid}` | Get one recipe with its ingredients |
| POST | `/api/v1/recipes/ingredients` | Add an ingredient |
| DELETE | `/api/v1/recipes/{recipeId:guid}/ingredients/{foodId:guid}` | Remove an ingredient |
| GET | `/api/v1/recipes/{recipeId:guid}/nutritional-info` | Computed nutritional totals |

### Meal plans (`/api/v1/meal-plans`)

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/meal-plans` | Create a plan |
| GET | `/api/v1/meal-plans` | List plans |
| GET | `/api/v1/meal-plans/{planId:guid}` | Full composition |
| POST | `/api/v1/meal-plans/meal-times` | Add a meal time to a day |
| POST | `/api/v1/meal-plans/assign-recipes` | Assign a recipe with a ration |
| DELETE | `/api/v1/meal-plans/{planId}/meal-times/{mealTimeId}/recipes/{recipeId}` | Unassign |

### Status codes

- A command that fails a business rule or validation → **400**.
- A read that finds nothing → **404** (including `PUT /api/v1/foods`).
- `[ApiController]` model validation → **400 `ProblemDetails`** before the handler runs.
- There is **no** global exception middleware: handlers catch and return `Result.Fail`; an unhandled
  exception surfaces as a plain **500**.
- Authentication required on write endpoints via `Nur.Tricenter.Security` (`CatalogPermission` policies);
  the author of a meal plan is read from the token's `sub` claim.

## Migrations

The schema is applied by an **explicit step**, never on boot:

```bash
dotnet run --project src/Catalog.App/Catalog.App.csproj -- --migrate
# or against the published binary:
dotnet Catalog.App.dll --migrate
```

It applies the write-side migrations (`DomainDbContext`) then the read-side ones (`StoredDbContext`).
Exits `0` on success and `1` on failure, so an orchestrator can gate on it. Idempotent: a second run
applies nothing.

For local development, `Migrations:ApplyOnStartup: true` in `appsettings.Development.json` removes the
ceremony. **Do not set it in a deployment.**

## Running locally

### Option 1 — the orchestrator (recommended)

```bash
cd <folder containing nur-tricenter-orchestrator>
./start-stack.sh          # or start-stack.ps1 on Windows
```

Brings up Postgres, RabbitMQ, Consul, the catalog (api + worker + migrate), identity and the gateway, with
per-service compose profiles.

### Option 2 — Docker Compose in this repository

```bash
cp src/Catalog.App/appsettings.Development.template.json src/Catalog.App/appsettings.Development.json
docker compose up -d
```

`webapi-migrate` runs `dotnet Catalog.App.dll --migrate`; `webapi` waits for it to succeed
(`service_completed_successfully`) before starting.

### Option 3 — local run

```bash
cp src/Catalog.WorkerService/appsettings.Development.template.json src/Catalog.WorkerService/appsettings.Development.json
dotnet run --project src/Catalog.App/Catalog.App.csproj
```

The worker needs a RabbitMQ broker. In this repository's compose there is none, so outbox rows accumulate
without being published; the orchestrator provides one.

## Configuration

| Key | Where | Meaning |
|---|---|---|
| `ConnectionStrings:CatalogDbConnectionString` | env var or local json | **Required.** One database serves both sides. |
| `Migrations:ApplyOnStartup` | `appsettings.json`, default `false` | Runs the migration on boot. Local development only. |
| `Seed:Enabled` | `appsettings.json`, default `false` | Sample catalogue data (30 foods, 25 recipes, 2 plans). |
| `Swagger:Enabled` | `appsettings.json`, default `false` | Serves the OpenAPI document. |
| `HttpsRedirection:Enabled` | `appsettings.json`, default `true` | Off when TLS terminates upstream. |
| `RabbitMqSettings:*` | `RabbitMqSettings` | Host, credentials, virtual host, retry policy. |
| `ServiceRegistration:ServiceDiscoveryAddress` | env var | Consul URL. Enables registration + DB health check when set. |
| `Jwt` | `JwtOptions` (shared kernel) | Signing key, issuer, audience, lifetime — must match identity's. |
| `Seed:Enabled` | `appsettings.json` | Sample data. |

No credentials belong in version-controlled files. The connection string is absent from every tracked
`appsettings.json` and arrives from an environment variable or a local, git-ignored
`appsettings.Development.json`. `TrackedConfigurationTests` fails if that changes.

## Consul & service registration

- Self-registers when `ServiceRegistration:ServiceDiscoveryAddress` is set.
- Host and port are **pinned** (`ServiceRegistration:Host` / `Port`), not sniffed: during hosted-service
  startup Kestrel's bound address is usually still unresolved.
- `ServiceName = catalog-service` (so the gateway routes `/api/catalog/**`).
- The Postgres health check and registration share the same predicate (`IsServiceDiscoveryEnabled`).
- `/health` is always mapped and `AllowAnonymous` (Consul probes carry no credentials).

## Observability

- `Nur.Tricenter.Observability` wires traces, metrics and the meter collection list. A meter not on the
  kernel's list is counted in-process and silently never exported.
- `OutboxTracingService` wraps `IOutboxService<T>` so every staged outbox row inherits the ambient trace.
- `TracingConsumer` re-attaches the publisher's span on inbound messages.
- Serilog is configured directly in the worker (host builder type difference) with the same enrichers as
  the kernel's `UseLogging`.

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

Three RabbitMQ tests skip themselves when no broker is reachable (correct behaviour: they are the
end-to-end proof that the outbox is published).

## Docker Hub

The compose consumes `robertperezlino/ms2-plans-and-recipes-catalog:latest`. **No pipeline publishes it**
— build and push by hand. `:latest` is whatever was pushed last.

```bash
dotnet nuget add source https://nuget.pkg.github.com/Nur-tricenter/index.json \
  --name Nur-tricenter --username YOUR_USER --password YOUR_TOKEN
setx NUGET_USERNAME YOUR_USER
setx NUGET_AUTH_TOKEN YOUR_TOKEN

docker build --secret id=nuget_username,src=%TEMP%/nu_user.txt \
             --secret id=nuget_token,src=%TEMP%/nu_tok.txt \
             -t robertperezlino/ms2-plans-and-recipes-catalog:latest .
docker push robertperezlino/ms2-plans-and-recipes-catalog:latest
```

Both secret files must be **UTF-8 without a BOM**.

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

## How a new microservice should mirror this service

1. **Layers**: `Domain` → `Application` (commands/queries/DTOs) → `Infrastructure` (DbContexts,
   repositories, outbox, inbox, observability, Consul) → `App` (controllers, Startup, health, Consul
   registration) + `WorkerService` (outbox drain, inbox consumer, default queue registrar).
2. **Dependency rule**: Application never references EF or a broker; Infrastructure implements the
   abstractions; App/Worker reference Infrastructure only.
3. **Central package management**: one `Directory.Packages.props`; kernels pinned to a single version
   (1.0.9).
4. **One database, three schemas** (`write`, `read`, `outbox`). Schema names in `DatabaseSchemas.cs`.
5. **Outbox chain**: command → `DomainDbContext.SaveChanges()` → domain event → `outbox.outboxMessage`
   (same transaction) → worker `OutboxProcessor` → projection in `StoredDbContext` → publish integration
   event to RabbitMQ. `OutboxChainCoverageTests` guards the chain.
6. **Inbox (consumer side)**: `TracingConsumer` → `IdempotentConsumer` → projection consumer, with an
   `InboxMessage` table keyed by the publisher's message id. Use it whenever a handler is not naturally
   idempotent.
7. **Migrations are explicit**: `dotnet run --project src/<Service>.App/<Service>.App.csproj -- --migrate`.
   Never on boot in production.
8. **Consul registration**: self-registers when `ServiceRegistration:ServiceDiscoveryAddress` is set; host
   and port are pinned, not sniffed; `ServiceName = <service>-service`.
9. **Auth**: validates JWTs with `Nur.Tricenter.Security` using the same key/issuer/audience as identity;
   the author of a resource is read from the token's `sub` claim.
10. **Gateway route**: prefix `/api/v1/...` so the gateway's `PathPrefix: /api` forwards correctly.
11. **Events**: publish via the shared contracts in `Nur.Tricenter.Integration` (add new contracts there,
    never duplicate); consume from other services' exchanges with the inbox decorator chain.
12. **Tests**: unit, application, infrastructure (InMemory vs real Postgres), worker, integration, contract.
    RabbitMQ tests skip when no broker is reachable.
13. **Observability**: wire through `Nur.Tricenter.Observability` so meters are exported; stamp the outbox
    row with the ambient trace and re-attach it on consume.
14. **No credentials in version control**: connection strings arrive from env vars or a git-ignored
    `appsettings.Development.json`.

> The agent-facing documents under `ai/` predate the current codebase. See `ai/README.md` for the full
> list of what changed and which statements no longer hold.
