---
name: integration-test-generator
description: Generates integration tests (xUnit + WebApplicationFactory + in-memory fakes) for the ms2-plans-and-recipes-catalog microservice, reading and applying the rules defined in ai/project/integration-testing-rules.md. Use it when asked to generate, create or write integration, end-to-end or HTTP tests for the WebApi controllers (AlimentoController, RecetaController, PlanAlimentarioController), or for a controller endpoint. It ONLY creates tests, it NEVER runs them. Language: EN.
---

# Integration Test Generator

Generates integration tests for the **ms2-plans-and-recipes-catalog** project, strictly
following the governance rules of the harness. **This skill ONLY creates test files. It does
NOT build, run or validate them.** It is the companion to `integration-test-verifier`.

## Mandatory flow

### 1. Read the rules (always)

Before writing any test, **read the current rules**:

- Rules: `ai/project/integration-testing-rules.md`

This path is relative to the repository root. Always apply the **latest** version of that
file. If it does not exist, stop and tell the user before continuing.

### 2. Verify the REQUIRED setup FIRST (blockers)

Integration tests exercise the real WebApi host, so two prerequisites must hold before any
file is generated:

1. **`Test/Test.csproj` must reference the host projects and packages.**
   - `<ProjectReference>` to `..\WebApi\WebApi.csproj` and
     `..\Infrastructure\Catalog.Infrastructure.csproj`.
   - `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.InMemory`.
   - If any is missing, **add it to `Test/Test.csproj`** and report the modification in the
     final summary (this is a required, non-optional setup step — no integration test can
     compile without it).
2. **`WebApi/Startup.cs` must guard the migrate/seed block** with
   `readContext.Database.IsRelational()` (rules §5, rule E8). `Database.Migrate()` throws
   against the EF Core InMemory provider, so **without this guard no integration test can
   start the host**. Verify the guard exists; if it does **NOT**, **STOP** and report the
   blocker — a production change is required and you must **never modify production source
   silently**.

### 3. Understand the source code under test

- Identify the target controller in `WebApi/Controllers/` (`AlimentoController`,
  `RecetaController`, `PlanAlimentarioController`).
- Read the controller: route prefix (`api/v1/alimentos`, `api/v1/recetas`, `api/v1/planes`),
  every action (HTTP verb + route), the command/query DTO it sends via MediatR, and what it
  returns on success/failure. Use the endpoint reference table in the rules doc (§6) — do not
  invent status codes.
- Classify the handlers invoked by that controller (rules §4 — CQRS with two DbContexts):
  - command handlers persist through **repositories** → state is visible via the fake repos;
  - single-entity query handlers (`GetById`, info-nutricional, composición) also inject
    **repositories** → they see what a previous POST stored;
  - **list** query handlers (`ListarAlimentos`, `ListarRecetas`, `ListarPlanes`,
    `BuscarAlimentoPorCategoria`) inject **`ReadDbContext`** directly → they read read models
    and will NOT see data created by a POST. Those tests must seed the InMemory
    `ReadDbContext` explicitly (rule E6).

### 4. Determine the target

Expect/ask for one of these inputs:

- **A single controller**: generate `{Controller}Test.cs` plus its `Setup/` files.
- **An entire folder** (`WebApi/Controllers/`): one test class per controller + shared
  `Setup/` helpers.
- **A single endpoint**: a focused test class covering that endpoint's happy/failure paths.

### 5. Location and naming (according to the rules)

All files live in the **same test project** `Test/Test.csproj`, under `Test/IntegrationTest/`:

```
Test/IntegrationTest/
├── Controllers/
│   ├── AlimentoControllerTest.cs
│   ├── RecetaControllerTest.cs
│   └── PlanAlimentarioControllerTest.cs
└── Setup/
    ├── RecetaControllerWebApplicationFactory.cs     (one factory per controller)
    ├── InMemoryRecetaRepository.cs                  (one fake per repository interface)
    ├── InMemoryAlimentoRepository.cs
    ├── InMemoryPlanAlimentarioRepository.cs
    ├── InMemoryUnitOfWork.cs
    └── {Feature}Response.cs                         (mirrors the ApiResponse JSON shape)
```

- Namespace: `Catalog.Tests.IntegrationTest.Controllers` for test classes,
  `Catalog.Tests.IntegrationTest.Setup` for fakes/factories/DTOs.
- Test class: `{Controller}Test` (`RecetaControllerTest`), implementing
  `IClassFixture<{Controller}WebApplicationFactory>`.
- Test method: English `{Endpoint}_{Scenario}` — `Create_WithValidRequest`,
  `GetById_WhenNotFound_Returns404`, `AgregarIngrediente_WithUnknownAlimento_Returns400`.
- Factory: `{Controller}WebApplicationFactory : WebApplicationFactory<Program>` where
  `Program` is `Catalog.App.Program`.
- Fake repository: `InMemory{Repository}` implementing the interface from `Catalog.Domain`.
- Response DTO: `{Feature}Response`, with `Success`, `Message`, `Errors` (+ `Data` on the
  generic one) to mirror `ApiResponse`/`ApiResponse<T>`.

## Writing rules you MUST follow

### General
- **AAA** pattern with explicit `//Arrange`, `//Act`, `//Assert` comments (I4).
- Fresh `HttpClient` per test via `_factory.CreateClient()` (I3); factory injected once via
  constructor and stored in a `private readonly` field (I2).
- Fresh `Guid.NewGuid()` for every entity/route id — never hardcode ids (I13).
- Request bodies as anonymous objects `new { Nombre = "...", Instrucciones = "..." }` (I8).
- Deserialize responses with `ReadFromJsonAsync<{Feature}Response>()` — never parse
  `JsonDocument` manually (I9).
- Assert the **HTTP layer first** (`EnsureSuccessStatusCode()` / `StatusCode`) (I10), then the
  envelope (`Success`, `Data`) (I11), then persisted state (I12).
- No dependence on order, time, exact GUID, culture, network, disk or a real database.

### Status-code contract (from §6 — do not invent)
- Commands failing a business rule → **400**. Reads not finding the aggregate → **404**
  (exception: `PUT api/v1/alimentos` fails with **404**).
- `[ApiController]` model validation (malformed JSON, `[MinLength(1)]` lists, bad route Guid)
  → automatic **400 `ProblemDetails`**, before the handler runs.
- Unhandled exception → plain HTTP **500**. Do not assert exception messages/stack traces
  (I14).

### Rules per area (summarized from the rules doc — verify the details there)
- **Controller tests** (I1–I14): one class per controller, one `{Endpoint}_{Scenario}`
  method per scenario, happy path + every failure path that changes the status code.
- **Fakes** (F1–F5): `InMemory{Repository}` backed by `ConcurrentDictionary<Guid, TEntity>`,
  registered **Singleton**; `FindByIdAsync` → `null` when missing; no invented unique-key
  behavior (this app has no EF unique index on `Nombre` — duplicate behavior is a Domain rule,
  already unit-tested); `InMemoryUnitOfWork` is a no-op.
- **WebApplicationFactory** (W1–W5): remove the existing `ServiceDescriptor`
  (`SingleOrDefault` + `Remove`) BEFORE adding the fake; never leave both registrations; do
  NOT override routing/middleware/controllers; register fakes as Singleton.
- **EF Core InMemory for both DbContexts** (E1–E8): `RemoveAll<DbContextOptions<T>>()` then
  `AddSingleton(new DbContextOptionsBuilder<T>().UseInMemoryDatabase(_dbName).Options)` for
  `ReadDbContext` AND `WriteDbContext`; one fixed db name per factory
  (`$"IntTests_{Guid.NewGuid()}"`); do NOT call `AddDbContext<T>` a second time (additive →
  "multiple database providers" exception). **List endpoints must seed the InMemory
  `ReadDbContext` read models directly via a DI scope before the request** (E6) — a POST never
  propagates into the read model store.

## Deliverables

- Write the controller test class(es) and all required `Setup/` files in the correct mirror
  location per the rules doc (§4, §13).
- Do NOT create placeholders (`Class1.cs`, `UnitTest1.cs`).
- Do NOT run `dotnet test` or any build/execution command. Creation and setup only.
- Report: list of created files, the `Test/Test.csproj` setup modifications applied (if any),
  and whether the `Startup.cs` guard blocker was verified present or is blocking.

## Before finishing

- Verify against the checklist and anti-patterns in `ai/project/integration-testing-rules.md`
  (§13, §15).
- Do not execute tests; if the user wants to run them, tell them to run:

```bash
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.Integration"
```