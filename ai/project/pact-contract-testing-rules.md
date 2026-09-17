# Pact Contract Testing Rules

> Governance document for the **contract testing** harness (Consumer-Driven Contract Testing
> with **Pact**) of the microservice **ms2-plans-and-recipes-catalog**.
>
> It complements [`unit-testing-rules.md`](unit-testing-rules.md) and
> [`integration-testing-rules.md`](integration-testing-rules.md). Unit tests verify a class in
> isolation; integration tests verify a controller + real pipeline; **contract tests verify the
> agreed HTTP contract between a consumer and the Catalog API provider**.
>
> Adapted to the DDD + Clean Architecture context of this project (Domain: Alimento, Receta,
> PlanAlimentario; WebApi: `AlimentoController`, `RecetaController`, `PlanAlimentarioController`).

---

## 1. Objective and scope

Contract testing protects the **interface** between the Catalog API (provider) and its clients
(consumer: the web/mobile app or another microservice) without either side running the other in
the same test.

| Side | What it does | When it runs | Output |
|------|--------------|--------------|--------|
| **Consumer** | Declares the requests it will send and the responses it expects, against a Pact **mock server**. | Fast, no real provider, no database. | A **pact file** (`*.json`) — the contract. |
| **Provider** | Replays every interaction in the pact file against the **real running API** and fails if any response no longer matches. | Requires the app hosted on a real socket. | Verification pass/fail. |

> **Not** in scope: business-rule verification (unit tests), full controller behaviour with the
> whole pipeline (integration tests), performance, security.

---

## 2. Stack and harness (reference implementation)

| Tool | Purpose | Version |
|------|---------|---------|
| **PactNet** | .NET consumer/provider DSL + Rust core | 5.0.0 |
| **PactNet.Abstractions** | `Match`, `IOutput`, `PactConfig`, `IPactBuilderV4` (transitive) | 5.0.0 |
| **xUnit** | Test framework | 2.9.3 |

Target framework: **net10.0**. Everything lives in the **existing test project**
`Test/Test.csproj` (there is no separate contract project). The native FFI ships in the NuGet
package under `runtimes/win-x64/native/pact_ffi.dll` and is loaded automatically by PactNet.

Required `using` namespaces (PactNet is split across assemblies — do **not** guess):

| Type | Namespace |
|------|-----------|
| `Pact`, `PactConfig`, `IPactBuilderV4` | `PactNet` |
| `Match` | `PactNet.Matchers` |
| `IOutput` | `PactNet.Infrastructure.Outputters` |
| `PactVerifier`, `PactVerifierConfig` | `PactNet.Verifier` |

---

## 3. Structure rules (S)

- **S1 — Single project.** Contract tests live in `Test/Test.csproj`, folder `Test/ContractTest/`.
  Never create a new `.csproj`.
- **S2 — Layer folders.** `Test/ContractTest/Consumer/` holds the consumer test + the consumer
  HTTP client (`CatalogApiClient.cs`); `Test/ContractTest/Provider/` holds the provider test +
  host startup (`ContractPactStartup.cs`), fixture (`CatalogApiProviderFixture.cs`) and the
  provider-state middleware (`ProviderStateMiddleware.cs`).
- **S3 — Shared names.** Consumer/provider names and the pact paths are defined **once** in
  `Test/ContractTest/PactPaths.cs`. Never hardcode names inside tests.
- **S4 — Pact directory.** The pact file is written to `Test/ContractTest/pacts/` and **committed
  to the repository** (no Pact Broker in this project). Path = `AppContext.BaseDirectory` + 3
  `..` + `ContractTest/pacts`.
- **S5 — Namespaces.** `Catalog.Tests.ContractTest`, `Catalog.Tests.ContractTest.Consumer`,
  `Catalog.Tests.ContractTest.Provider`.
- **S6 — Output adapter.** Pact logs are routed to xUnit through a single `XUnitOutput : IOutput`
  (in `Test/ContractTest/`). Every `PactConfig`/`PactVerifierConfig` uses it.
- **S7 — No placeholders.** Never add `Class1.cs`/`UnitTest1.cs`.

---

## 4. Consumer rules (C)

- **C1 — One interaction per `[Fact]`.** Each test declares one `UponReceiving(...)` with a
  human-readable description and a unique, stable description string.
- **C2 — `Pact.V4`.** Build with `Pact.V4(consumer, provider, config).WithHttpInteractions()` and
  execute with `await _pactBuilder.VerifyAsync(async ctx => { ... })`.
- **C3 — Talk through a client.** The consumer must exercise a real `HttpClient` pointed at the
  Pact mock server (`ctx.MockServerUri`), not assert the mock by hand. The client and its DTOs
  live in `Consumer/CatalogApiClient.cs`.
- **C4 — Assert the consumer's own expectations.** Inside `VerifyAsync`, assert only what the
  consumer relies on (`Success`, non-empty `Id`, `Page`/`PageSize`, ...). Do **not** re-assert the
  whole payload — the matchers already own the contract.
- **C5 — `WithJsonBody` + `Match.*`.** Request/response bodies are anonymous objects (camelCase).
  Fixed scalar values are declared literally; **values the consumer must not pin down are wrapped
  in matchers** (`Match.Type(...)`, `Match.Integer(...)`, `Match.Number(...)`, `Match.Regex(...)`).
- **C6 — Arrays backed by a provider state use `Match.MinType`; possibly-empty arrays use `Match.MaxType`.**
  A list whose data is guaranteed by a `.Given(...)` state is declared with `Match.MinType(example, 1)`
  so the provider must return at least one element. A list that can legitimately be empty uses
  `Match.MaxType(example, max)`. Never `Match.MinType(example, 0)` (throws `ArgumentException`, X1).
- **C7 — Explicit Content-Type.** Every response declares
  `.WithHeader("Content-Type", "application/json; charset=utf-8")`, matching ASP.NET Core.
- **C8 — Query parameters are declared.** Paginated GETs must declare `.WithQuery("page", "1")` and
  `.WithQuery("pageSize", "10")`.
- **C9 — Deterministic examples.** Use fixed `Guid`/values as examples; never `Guid.NewGuid()` or
  `DateTime.Now` in a matcher example (the pact file must be stable in git).
- **C10 — Provider states feed the data-backed interactions.** List endpoints declare
  pagination via `.Given("<state>")` (e.g. `.Given("existen alimentos registrados")`) — a provider
  that returns an empty list would make the contract meaningless. Every `.Given(...)` **must** be
  handled provider-side (see P7); state names are business language, unique and stable (N3).
- **C11 — Numbers use `Match.Number`, not `Match.Decimal`.** The API serializes `decimal` fields
  as plain integers when the value has no fractional part (`100`, not `100.0`). `Match.Decimal`
  requires an explicit decimal in the response and fails provider verification for integer output;
  `Match.Number(...)` matches any JSON number (C7 in the first draft of this file was Content-Type).

---

## 5. Pact file rules (G)

- **G1 — One file per consumer/provider pair**: `{consumer}-{provider}.json`
  (`catalog-web-client-catalog-api.json`).
- **G2 — Spec version 4.0** with `metadata.pactRust` present.
- **G3 — camelCase** JSON (`PactConfig.DefaultJsonSettings` with `JsonNamingPolicy.CamelCase`),
  matching the API's serialization.
- **G4 — Committed and reviewed.** The pact file is a versioned artifact: commit it and review its
  diff when interactions change. Never hand-edit it — regenerate from the consumer tests.
- **G5 — Shape mirrors `ApiResponse<T>`**: `{ success, message, data }`; a paged `data` mirrors
  `PagedList<T>`: `{ items, page, pageSize, totalCount, hasNextPage, hasPreviousPage }`.
- **G6 — `providerStates` recorded.** Every consumer `.Given("<state>")` is written into the pact;
  the provider-side state handler must use the **exact same state name** (N3), otherwise pact
  reports a missing state during verification.

---

## 6. Provider rules (P)

- **P1 — Real socket required.** Pact's Rust core calls the provider **out-of-process**, so
  `TestServer`/`WebApplicationFactory` (in-memory transport) **cannot** be used. Host Kestrel with
  `Host.CreateDefaultBuilder().ConfigureWebHostDefaults(...)` on a dynamic port
  (`http://127.0.0.1:0`).
- **P2 — Reuse the real pipeline.** The host uses `ContractPactStartup : Startup` and overrides
  `ConfigureServices` to run `base.ConfigureServices(...)` first, so routing, controllers and
  middleware are production-identical.
- **P3 — Swap only infrastructure.** After `base`, call
  `services.ReplaceInfrastructureWithInMemory(dbName)` (from `Test/IntegrationTest/Setup`) to
  replace PostgreSQL with the in-memory fakes. Never mock business behaviour on the provider side.
- **P4 — Register the WebApi application part.** Because the host is built with
  `Host.CreateDefaultBuilder`, the default application part points at the test assembly. Add
  `services.AddControllers().AddApplicationPart(typeof(Startup).Assembly)` or every endpoint
  returns 404.
- **P5 — Content root = `WebApi/`** so `appsettings.json` is loaded. Compute it from
  `AppContext.BaseDirectory` (4 `..` → repository root, then `WebApi`).
- **P6 — Fixture owns the host.** The host is started/stopped by an `IClassFixture<>` fixture that
  exposes `ServerUri` and disposes cleanly. The provider test is a single `[Fact]`.
- **P7 — Provider states are handled by a middleware.** Before each `.Given(...)` interaction,
  Pact POSTs `/provider-states` with `{ consumer, state, params }`. The test host exposes a
  `ProviderStateMiddleware` (in `Test/ContractTest/Provider/`) that seeds the in-memory
  `ReadDbContext` for the contract's states (`"existen alimentos registrados"`,
  `"existen recetas registradas"`) with the same fixed ids the consumer examples use. The provider
  test enables it with `.WithProviderStateUrl(new Uri(fixture.ServerUri, "provider-states"))`.
- **P8 — File-source verification.** Verify with
  `new PactVerifier(PactPaths.Provider, config).WithHttpEndpoint(fixture.ServerUri).WithFileSource(new FileInfo(PactPaths.PactFile)).WithProviderStateUrl(...).Verify()`,
  and fail with a clear message if the pact file is missing (generate it with the consumer test).

### Production changes allowed by this harness
`WebApi/Startup.cs` exposes `ConfigureServices` and `Configure` as `public virtual` (P2, P7) so the
contract host can reuse the production pipeline and inject the provider-state middleware. These are
the **only** production changes contract testing requires; do not modify production code further to
make contract tests pass.

---

## 7. Naming rules (N)

- **N1 — Names.** `PactPaths.Consumer = "catalog-web-client"`, `PactPaths.Provider = "catalog-api"`
  (lowercase, hyphen-separated).
- **N2 — Test methods** use the unit-testing English format
  `{Action}_{Scenario}` — e.g. `CreateAlimento_ReturnsCreatedId`, `GetAlimentos_ReturnsPagedList`.
- **N3 — Interaction descriptions** are lowercase verb phrases, unique and stable — e.g.
  `"a request to create an alimento"`, `"a request to list recetas"`.

---

## 8. Execution order (mandatory)

Contract testing is **two phases**; always consumer first, then provider:

```bash
# 1. Consumer: generates/updates Test/ContractTest/pacts/*.json
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.ContractTest.Consumer"

# 2. Provider: replays the pact against the real API
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.ContractTest.Provider"

# Whole suite
dotnet test Test/Test.csproj
```

---

## 9. Pre-PR checklist

- [ ] Every new consumer interaction has a unique `UponReceiving` description (C1, N3).
- [ ] The pact file was **regenerated** by running the consumer tests (never hand-edited) (G4).
- [ ] The provider verification passes against the **regenerated** pact (P8).
- [ ] Values the consumer does not care about are wrapped in `Match.*` matchers (C5).
- [ ] Data-backed arrays use `Match.MinType(example, 1)` with a handled `.Given(...)`; possibly-empty arrays use `Match.MaxType`, never `Match.MinType(example, 0)` (C6).
- [ ] Numeric fields use `Match.Number`, not `Match.Decimal` (C11).
- [ ] Response `Content-Type` is declared (C7).
- [ ] `PactPaths` is the only place names/paths are defined (S3, N1).
- [ ] Every `.Given(...)` state name matches a provider-state handler (C10, P7, G6).
- [ ] No hardcoded port, no `TestServer`, no real database (P1, P3).
- [ ] Pact logs go through `XUnitOutput` (S6).
- [ ] `Test/ContractTest/pacts/*.json` is committed (S4).

---

## 10. Anti-patterns (X) — must NOT appear

- **X1** — Using `Match.MinType(example, 0)` (throws `ArgumentException`: min must be > 0).
- **X2** — Hosting the provider with `WebApplicationFactory`/`TestServer` (Pact FFI is
  out-of-process → connection failures).
- **X3** — Forgetting `AddApplicationPart(typeof(Startup).Assembly)` → all endpoints 404 (P4).
- **X4** — Hardcoding the provider URL/port instead of the fixture's dynamic `ServerUri`.
- **X5** — Asserting the full response body in the consumer test instead of matchers (C4).
- **X6** — Pinning volatile values (new GUID each run) in the pact, producing noisy diffs (C9).
- **X7** — Committing a pact file that the provider has not verified.
- **X8** — Declaring a `.Given(...)` state with no provider-side handler → verification fails (C10, P7).
- **X9** — Modifying business/production code beyond `Startup.ConfigureServices`/`Configure` being
  `virtual` to make a contract test pass.
- **X10** — Using `Match.Decimal` on numeric fields the provider serializes as integers
  (`100` instead of `100.0`) → "Expected ... (Integer) to be a decimal number" (C11).
