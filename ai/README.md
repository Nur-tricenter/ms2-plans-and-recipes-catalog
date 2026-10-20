# ai/ — documentation for automated agents

> **State: needs a full rewrite.** The eleven documents in this folder were written for an
> earlier version of the codebase and describe types, routes, package versions and folder layouts
> that no longer exist. An audit on 2026-10-20 found roughly 65 statements that are wrong in a
> way that leads a reader to write code that does not compile.
>
> This file exists so nobody trusts them by accident. Replace it as each document is rewritten.

## What is here

| Path | Size | State |
|------|------|-------|
| `project/README.md` | 5.6 KB | Stale structure, stale names |
| `project/unit-testing-rules.md` | 16.4 KB | Stale names, wrong TFM, wrong method names |
| `project/integration-testing-rules.md` | 27.2 KB | Stale names, wrong DbContexts, wrong routes, a prerequisite that does not exist |
| `project/pact-contract-testing-rules.md` | 13.3 KB | Stale names, wrong TFM |
| `agent/test-writer.md` | 10.1 KB | Stale classifiers, references a non-existent blocker |
| `skill/unit-test-generator/SKILL.md` | 4.4 KB | Stale names, wrong mirror paths |
| `skill/unit-test-verifier/SKILL.md` | 4.4 KB | Structurally sound; inherits the stale rules |
| `skill/integration-test-generator/SKILL.md` | 8.7 KB | Stale names, wrong DbContexts, a prerequisite that does not exist |
| `skill/integration-test-verifier/SKILL.md` | 7.1 KB | Stale names |
| `skill/pact-generator/SKILL.md` | 5.2 KB | Stale controller names |
| `skill/pact-verifier/SKILL.md` | 6.3 KB | Stale names |

## The drift, in one table

Every row below is a case where following the document produces code that does not compile, or a
reader who concludes the implementation is wrong when it is correct.

| The documents say | The code actually has |
|---|---|
| `net10.0` | `net8.0` |
| `Alimento`, `Receta`, `PlanAlimentario` | `Food`, `Recipe`, `MealPlan` |
| `ReadDbContext`, `WriteDbContext` | `StoredDbContext`, `DomainDbContext` |
| `AlimentoReadModel`, `RecetaReadModel` | `FoodModel`, `RecipeModel`, `MealPlanModel` |
| `IRecetaRepository`, `IAlimentoRepository` | `IRecipeRepository`, `IFoodRepository` |
| `api/v1/alimentos`, `/recetas`, `/planes` | `api/v1/foods`, `/recipes`, `/meal-plans` |
| `IUnitOfWork.Commit()` | `IUnitOfWork.CommitAsync(CancellationToken)` |
| `Catalog.WebApi` / `WebApi/` | `Catalog.App` / `src/Catalog.App/` |
| `Test/Application/{Aggregate}/` | `Test/UnitTest/Application/{Aggregate}/` |
| Query handlers read repositories | All nine read `StoredDbContext` |
| `Startup.Configure` needs an `IsRelational()` guard | It delegates to `IDatabaseStartup`, gated by `Migrations:ApplyOnStartup` |
| `EF Core 10.0.0-preview.2` | `EF Core 9.0.3`, `Npgsql 9.0.4` |
| The service has no message bus | `Catalog.WorkerService` publishes to RabbitMQ |

## What the documents omit entirely

Code that exists, is tested, and has no guidance written about it:

| Area | Where |
|------|-------|
| Outbox chain coverage | `Test/UnitTest/Application/DomainEventHandler/OutboxChainCoverageTests.cs` |
| Twelve projection handlers | `Infrastructure/Persistence/StoredModel/Projection/` |
| `SaveInOutbox*` and `PublishIntegrationEvent*` handlers | `Application/UseCase/DomainEventHandler/` |
| Pipeline behaviours | `Test/UnitTest/Application/Behaviors/` |
| Domain event serialization | `Test/UnitTest/Domain/Events/` |
| Infrastructure unit tests | `Test/UnitTest/Infrastructure/` |
| Worker service tests | `Test/UnitTest/WorkerService/` |
| RabbitMQ integration tests | `Test/IntegrationTest/RabbitMq/` |
| Health and pipeline tests | `Test/IntegrationTest/{Health,Configuration}/` |
| The `contracts/` project | `Catalog.Contracts` — permissions, routes, integration messages |
| Platform kernel consumption | `Nur.Tricenter.Access`, `Nur.Tricenter.Observability` as NuGet packages |

The documents also forbid testing infrastructure (§15 item 6 of the unit rules) while the suite
contains more than fifty such tests, deliberately.

## What is true today

For reference, the facts the rewritten documents must state:

- **Runtime:** `net8.0`, EF Core `9.0.3`, Npgsql `9.0.4`
- **Architecture:** DDD + Clean, five projects plus `contracts/`
- **CQRS:** nine commands, nine queries, every query served from `StoredDbContext`
- **Persistence:** one database, three schemas (`write`, `read`, `outbox`)
- **Projection:** transactional outbox, `Catalog.WorkerService` projects asynchronously
- **Tests:** 454 total — unit, infrastructure, worker, integration, Pact contract
- **Messaging:** five integration contracts, RabbitMQ, producer only
- **Discovery:** self-registration in Consul, `/health`
- **No authentication,** by decision

## Before rewriting

Two things are worth fixing structurally rather than document by document:

1. **The six `SKILL.md` files duplicate ~70% of each other.** Each one restates the rules from
   `ai/project/*.md` inline, so a rule change has to be mirrored in three places and drifts.
   The skills should reference the rules document rather than copy it.
2. **The four `project/*.md` files repeat the same material** — naming, AAA, isolation,
   anti-patterns. One shared document carrying the common rules would let each track document
   carry only what is specific to it.

Fix the duplication first, then rewrite. Otherwise the next drift is only a matter of time.