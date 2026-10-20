# Integration Testing Rules — ms2-plans-and-recipes-catalog

> Governance document for the integration testing harness of the microservice
> **ms2-plans-and-recipes-catalog**. Defines how integration tests are written, organized,
> named and executed. All new integration-test code must comply with these rules.
>
> Adapted to the DDD + Clean Architecture context of this project (Domain: Alimento, Receta,
> PlanAlimentario; Application: CQRS/MediatR handlers; Infrastructure: **two** DbContexts —
> `ReadDbContext` + `WriteDbContext` — over PostgreSQL).
>
> Units tests (Domain + Application in isolation) follow
> [`unit-testing-rules.md`](unit-testing-rules.md). This document covers **only** the
> end-to-end HTTP wiring tests that live in `Test/IntegrationTest/`.

---

## 1. Stack

| Tool | Purpose | Reference version |
|------|---------|-------------------|
| **.NET** | Target framework | `net10.0` |
| **xUnit** | Test framework (`[Fact]`, `[Theory]`) | 2.9.3 |
| **xunit.runner.visualstudio** | Adapter for `dotnet test` | 3.0.2 |
| **Microsoft.NET.Test.Sdk** | Test SDK | 17.13.0 |
| **Microsoft.AspNetCore.Mvc.Testing** | `WebApplicationFactory<TEntryPoint>` | matching the shared framework (10.0.x) |
| **Microsoft.EntityFrameworkCore.InMemory** | In-memory provider backing the two DbContexts | matching the EF Core version used by `Infrastructure` (`10.0.0-preview.2`) |
| **coverlet.collector** | Coverage | 6.0.0 (optional) |
| **Moq** | Already present in the test project (used by unit tests) | 4.20.72 |

All files live in the **same test project** `Test/Test.csproj` (the unit tests stay in
`Test/UnitTest/`, integration tests go in `Test/IntegrationTest/`). To host the real WebApi you
will need to add to `Test.csproj`:

- `<ProjectReference>` to `..\WebApi\WebApi.csproj` (entry point, controllers) **and**
  `..\Infrastructure\Catalog.Infrastructure.csproj` (the `DbContext` types + read models).
- `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.InMemory`.

> The unit-testing-rules doc recommends keeping the test project free of infrastructure
> dependencies. Those rules target the unit tests; the integration suite **requires** these
> references by design. Filter by namespace when you want to run one kind only (see §15).

---

## 2. Scope

Integration tests cover the **WebApi layer end-to-end**: HTTP request → Controller → MediatR →
Handler → persistence, using an **in-memory fake infrastructure** instead of a real database.

What is proven:

- routing (route templates, route constraints `{id:guid}`),
- DI composition (every registration resolves inside the real host),
- JSON serialization (the `ApiResponse` envelope, camelCase, decimal precision),
- HTTP status-code mapping (200 / 400 / 404),
- the controller ↔ handler contract (command payloads, query DTOs).

What is **NOT** proven here (it belongs to unit tests in `Test/UnitTest/`):

- domain invariants and exceptions,
- handler branching, `IUnitOfWork.Commit()` semantics, mock interactions.

An integration test exists to prove the **wiring**, not to re-verify business rules already
covered at the unit level.

---

## 3. Architecture

```
Client (HttpClient from WebApplicationFactory)
        │  HTTP request (PostAsJsonAsync / GetAsync)
        ▼
   Controller (real, Catalog.App)            [Alimento | Receta | PlanAlimentario]
        │  injects only IMediator (real pipeline)
        ▼
   MediatR pipeline (real)
        ▼
   Handler (real)
        ├── Commands        → Catalog.Application (repositories + IUnitOfWork + factories)
        └── Queries         → Catalog.Infrastructure (repositories AND/OR ReadDbContext)
        ▼
   Repository / IUnitOfWork / ReadDbContext / WriteDbContext  ← FAKE (registered in the factory)
```

Only the **persistence boundary** is faked. Everything above it (controller, MediatR,
handlers, domain, factories `IRecetaFactory`, `IPlanAlimentarioFactory`, `IAlimentoFactory`,
domain event handlers) runs **for real**.

> Domain event handlers (`PublishIntegrationEventWhenAlimentoNutricionalActualizado`, etc.)
> only log — this app has **no** message bus. Nothing out-of-process needs to be stubbed
> besides the database.

---

## 4. This app is CQRS with two DbContexts (read the read/write split)

The app has **two public DbContexts** over the same PostgreSQL database:

- **`WriteDbContext`** — aggregates (`PlanAlimentario`, `Receta`, `Alimento`). Used by the
  repository implementations and `IUnitOfWork`.
- **`ReadDbContext`** — read models (`AlimentoReadModel`, `RecetaReadModel`,
  `PlanAlimentarioReadModel`). Used **directly** (no repository) by the list query handlers.

Query handlers are **not** homogeneous:

| Query handler | Data access | In integration tests sees... |
|---|---|---|
| `GetAlimentoByIdHandler`, `GetRecetaByIdHandler`, `GetInfoNutricionalRecetaHandler`, `GetPlanByIdHandler`, `GetComposicionPlanHandler` | inject **repositories** | the in-memory **repository fakes** → data created via POST **is** visible |
| `ListarAlimentosHandler`, `ListarRecetasHandler`, `ListarPlanesHandler`, `BuscarAlimentoPorCategoriaHandler` | inject **`ReadDbContext`** | the **InMemory `ReadDbContext`** → data created via POST is **NOT** visible |

> **Consequence (app-specific):** `POST` persists only inside the in-memory repository fake
> (a `ConcurrentDictionary`). List endpoints read `RecetaReadModel`/`AlimentoReadModel`/
> `PlanAlimentarioReadModel` — different CLR types that InMemory keeps in a **separate store**,
> even when the database name is the same. To test a list endpoint you **must seed the
> `ReadDbContext` InMemory database explicitly** via a DI scope (rule E6). To test a mutation or
> a single-entity GET you can rely on the command itself (rule I12).

---

## 5. Entry point and required production change (read this first)

**TEntryPoint:** `Catalog.App.Program` — a `public class Program` using the classic
`Host.CreateDefaultBuilder(...).UseStartup<Startup>()` bootstrap
(`WebApi/Program.cs`).

**Required production change — `WebApi/Startup.cs`:**

`Startup.Configure` (lines 46–53) runs **unconditionally**:

```csharp
using (var scope = app.ApplicationServices.CreateScope())
{
    var readContext = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
    readContext.Database.Migrate();                       // relational-only
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    dbInitializer.Initialize().GetAwaiter().GetResult();  // seeds 30 alimentos / 25 recetas / 2 planes
}
```

`DbContext.Database.Migrate()` **throws** when the provider is EF Core InMemory ("Relational
-specific methods can only be used when the context is using a relational database provider").
Therefore the block MUST be guarded in production code:

```csharp
using (var scope = app.ApplicationServices.CreateScope())
{
    var readContext = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
    if (readContext.Database.IsRelational())
    {
        readContext.Database.Migrate();
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize().GetAwaiter().GetResult();
    }
}
```

- Development and production behavior is unchanged (`IsRelational()` is `true` with Npgsql).
- In tests with InMemory it also prevents the seeder from polluting every test database.
- **This touches production source.** Do it explicitly and call it out in code review/PR.
  Do **NOT** try to dodge it with `builder.UseEnvironment(...)`: the block is **not**
  environment-gated, so overriding the environment changes nothing — and it would silently
  disable ASP.NET Core's Development-only `UseDeveloperExceptionPage()` behavior.

---

## 6. Endpoint reference

### AlimentoController — `api/v1/alimentos`

| Method | Route | Request | Success | Failure |
|---|---|---|---|---|
| `POST` | `api/v1/alimentos` | `CrearAlimentoCommand` | 200 `ApiResponse<Guid>` | 400 `ApiResponse<Guid>` |
| `GET` | `api/v1/alimentos/{id:guid}` | route `Guid id` | 200 `ApiResponse<AlimentoDto>` | 404 `ApiResponse` |
| `GET` | `api/v1/alimentos?page&pageSize` | query | 200 `ApiResponse<PagedList<AlimentoDto>>` | 400 |
| `GET` | `api/v1/alimentos/categoria/{categoria}?page&pageSize` | route + query | 200 `ApiResponse<PagedList<AlimentoDto>>` | 400 |
| `PUT` | `api/v1/alimentos` | `ActualizarAlimentoCommand` | 200 `ApiResponse` | **404** |

### RecetaController — `api/v1/recetas`

| Method | Route | Request | Success | Failure |
|---|---|---|---|---|
| `POST` | `api/v1/recetas` | `CrearRecetaCommand` | 200 `ApiResponse<Guid>` | 400 |
| `POST` | `api/v1/recetas/ingredientes` | `AgregarIngredienteCommand` (`[MinLength(1)]`) | 200 `ApiResponse` | 400 |
| `DELETE` | `api/v1/recetas/{recetaId:guid}/ingredientes/{alimentoId:guid}` | route ×2 | 200 `ApiResponse` | 400 |
| `GET` | `api/v1/recetas/{id:guid}` | route | 200 `ApiResponse<RecetaDto>` | 404 |
| `GET` | `api/v1/recetas?page&pageSize` | query | 200 `ApiResponse<PagedList<RecetaDto>>` | 400 |
| `GET` | `api/v1/recetas/{recetaId:guid}/info-nutricional` | route | 200 `ApiResponse<InfoNutricionalDto>` | 404 |

### PlanAlimentarioController — `api/v1/planes`

| Method | Route | Request | Success | Failure |
|---|---|---|---|---|
| `POST` | `api/v1/planes` | `CrearPlanCommand` | 200 `ApiResponse<Guid>` | 400 |
| `POST` | `api/v1/planes/tiempos-comida` | `AgregarTiempoComidaCommand` | 200 `ApiResponse` | 400 |
| `POST` | `api/v1/planes/asignar-recetas` | `AsignarRecetaATiempoCommand` (`[MinLength(1)]`) | 200 `ApiResponse` | 400 |
| `DELETE` | `api/v1/planes/{planId:guid}/tiempos-comida/{tiempoComidaId:guid}/recetas/{recetaId:guid}` | route ×3 | 200 `ApiResponse` | 400 |
| `GET` | `api/v1/planes/{planId:guid}` | route | 200 `ApiResponse<ComposicionPlanDto>` | 404 |
| `GET` | `api/v1/planes` | — | 200 `ApiResponse<IEnumerable<PlanAlimentarioDto>>` | 400 |

**Status-code contract:**
- Commands that fail a business rule or input validation → **400** `BadRequest(ApiResponse.Fail(...))`.
- Reads that do not find the aggregate → **404** `NotFound(ApiResponse.Fail(...))`.
  Exception: `PUT api/v1/alimentos` fails with **404**, not 400.
- `[ApiController]` model validation (malformed JSON, `[MinLength(1)]` lists, bad route GUID)
  produces ASP.NET's automatic **400 `ProblemDetails`** before the handler runs.
- There is **no** global exception middleware: handlers catch domain exceptions and return
  `Result.Fail(...)`; an unhandled exception surfaces as a plain HTTP **500**.

---

## 7. Rules (test classes)

| # | Rule |
|---|------|
| I1 | One test class per controller. File: `Test/IntegrationTest/Controllers/{Controller}Test.cs`, implementing `IClassFixture<{Controller}WebApplicationFactory>` |
| I2 | Inject the factory via constructor and store it in a `private readonly` field. Do **NOT** create a new factory per test |
| I3 | Get the `HttpClient` with `_factory.CreateClient()` at the start of **every** test — never share a client across tests |
| I4 | Use the AAA pattern with explicit `//Arrange`, `//Act`, `//Assert` comments |
| I5 | Test method naming: `{Endpoint}_{Scenario}` in English — e.g. `Create_WithValidRequest`, `GetById_WhenNotFound_Returns404`, `AgregarIngrediente_WithUnknownAlimento_Returns400`. Keep Spanish action names when they mirror a real endpoint (`AgregarIngrediente_...*`) |
| I6 | Never call a real database, external API, or filesystem. Replace **every** outbound dependency (`IRecetaRepository`, `IPlanAlimentarioRepository`, `IAlimentoRepository`, `IUnitOfWork`) and the `DbContextOptions` of **both** DbContexts with in-memory fakes registered in `ConfigureWebHost` |
| I7 | Register each fake by **removing** the existing `ServiceDescriptor` first (`SingleOrDefault` + `Remove`), then adding the fake with `AddSingleton`. Never leave both registrations in the container |
| I8 | Use anonymous objects (`new { Nombre = "Ensalada", Instrucciones = "Mezclar" }`) for request bodies unless a shared request command already exists that is safe to reuse |
| I9 | Deserialize the response with `ReadFromJsonAsync<T>()` into a dedicated `{Feature}Response` class in `Test/IntegrationTest/Setup/` that mirrors the real **`ApiResponse`** JSON shape (`success`, `message`, `errors`, `data`) — do NOT parse `JsonDocument` manually |
| I10 | Assert the HTTP layer **first**: `response.EnsureSuccessStatusCode()` for the happy path, or `Assert.Equal(HttpStatusCode.X, response.StatusCode)` for failure paths |
| I11 | Assert the response envelope (`Success`, `Data`, `Message`) **before** asserting persisted state |
| I12 | After asserting the HTTP response, open a DI scope with `_factory.Services.CreateScope()` and resolve the **fake repository** to assert persisted state directly. Always `using var scope = ...` |
| I13 | Every test uses fresh `Guid.NewGuid()` for entity/route IDs — never hardcode IDs, since the fakes are singletons shared across tests in the same class |
| I14 | Do **NOT** assert on exception messages or stack traces for failure paths; assert only the resulting `HttpStatusCode` (and the `ProblemDetails`/`ApiResponse` envelope when it matters). Do **NOT** assert the exact text of a domain error that is already unit-tested |

---

## 8. In-memory fakes (`Test/IntegrationTest/Setup/`)

### Rules

| # | Rule |
|---|------|
| F1 | One fake class per repository interface: `InMemory{Repository}` (`InMemoryRecetaRepository`, `InMemoryAlimentoRepository`, `InMemoryPlanAlimentarioRepository`), implementing the real interface from `Catalog.Domain` |
| F2 | Back the fake with a `ConcurrentDictionary<Guid, TEntity>` — the factory registers it as `Singleton`, so it must be thread-safe across parallel test execution |
| F3 | `FindByIdAsync` returns `null` when the id is missing; `CreateAsync` adds the aggregate; `UpdateAsync`/`RemoveAsync` operate on the dictionary. **Do NOT invent unique-key behavior**: the EF configs in this app define no unique index on `Nombre` — duplicate-name behavior is a **domain** rule (e.g. `Receta.AgregarIngrediente`), already covered by unit tests |
| F4 | `IUnitOfWork` fakes are no-ops (`Task.CompletedTask`) — integration tests do not verify commit semantics; that belongs in Application unit tests |
| F5 | Never add business logic to a fake beyond what is needed to reproduce infrastructure behavior (not-found, add, update, remove). Business rules belong in the Domain layer |

### Example — fake repository

```csharp
public class InMemoryRecetaRepository : IRecetaRepository
{
    private readonly ConcurrentDictionary<Guid, Receta> _items = new();

    public Task<Receta?> FindByIdAsync(Guid id)
        => Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    public Task CreateAsync(Receta receta)
    {
        _items[receta.Id] = receta;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Receta receta)
    {
        _items[receta.Id] = receta;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Receta receta)
    {
        _items.TryRemove(receta.Id, out _);
        return Task.CompletedTask;
    }
}
```

---

## 9. EF Core InMemory for the two DbContexts

Four query handlers bypass repositories and inject `ReadDbContext` directly
(`ListarAlimentosHandler`, `ListarRecetasHandler`, `ListarPlanesHandler`,
`BuscarAlimentoPorCategoriaHandler`). There is no repository interface to swap in that case —
swap the DbContext's **provider** instead. Never let such a handler hit a real Npgsql
connection in a test.

### Rules

| # | Rule |
|---|------|
| E1 | Detect this case when a handler's constructor takes a `DbContext`-derived type (e.g. `ReadDbContext dbContext`) instead of a repository interface |
| E2 | In this app `ReadDbContext`, `WriteDbContext` and the read models are **`public`** — no `InternalsVisibleTo` is required. But `Test.csproj` **must** reference `Catalog.Infrastructure.csproj` (and `WebApi.csproj` for the entry point/controllers) and add `Microsoft.EntityFrameworkCore.InMemory` |
| E3 | Remove `DbContextOptions<T>` registrations with `services.RemoveAll<DbContextOptions<ReadDbContext>>()` **and** `services.RemoveAll<DbContextOptions<WriteDbContext>>()`. Removing only `T` itself (as for repositories, I7) is **not** enough: `AddDbContext` also registers `DbContextOptions<T>` |
| E4 | Re-register by building the options directly and adding the built instance: `services.AddSingleton(new DbContextOptionsBuilder<T>().UseInMemoryDatabase(_dbName).Options)`. Do **NOT** call `services.AddDbContext<T>(...)` again: `AddDbContext` is **additive** — a second call layers the new provider on top of the original and EF Core throws `InvalidOperationException: ... multiple database providers ... have been registered` the first time the context is used |
| E5 | Use ONE fixed database name per factory instance: a `private readonly string _dbName = $"IntTests_{Guid.NewGuid()}";` field set at factory construction, shared by **both** contexts. State persists across requests within the same test class — mirrors the Singleton behavior of repository fakes (I13) |
| E6 | **List endpoints read read models, not repository state** (§4). To test `GET .../alimentos`, `GET .../recetas`, `GET .../planes` or `.../alimentos/categoria/{categoria}`, seed the **`ReadDbContext` InMemory store directly** from a DI scope (`scope.ServiceProvider.GetRequiredService<ReadDbContext>()`, add read models, `SaveChangesAsync`) before making the request. For mutations and single-entity GETs, the command itself provides the state (I12). Seed and assert through the same context type — never mix domain aggregates and read models |
| E7 | Never add business logic to seeded data beyond what the test scenario needs (F5 applies here too). Seed only the read models the endpoint reads |
| E8 | `Startup.Configure` calls `ReadDbContext.Database.Migrate()` + `IDbInitializer.Initialize()` unconditionally. Guard that block with `readContext.Database.IsRelational()` in production code before any integration test using InMemory can run — see §5. Do NOT override the hosting environment to dodge it |

### Example — seeding the InMemory ReadDbContext for a list test

```csharp
//Arrange
var client = _factory.CreateClient();
using (var scope = _factory.Services.CreateScope())
{
    var readDb = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
    readDb.Alimento.Add(new AlimentoReadModel
    {
        Id = alimentoGuid,
        Nombre = "MANZANA",
        Categoria = "FRUTAS",
        UnidadMedida = (int)UnidadMedida.Gramo,
        Cantidad = 100,
        Calorias = 52,
        CreatedAt = DateTime.UtcNow,
        IsDeleted = false
    });
    await readDb.SaveChangesAsync();
}

//Act
var response = await client.GetAsync($"/api/v1/alimentos?page=1&pageSize=10");
```

---

## 10. WebApplicationFactory (`Test/IntegrationTest/Setup/`)

### Rules

| # | Rule |
|---|------|
| W1 | Name the factory `{Controller}WebApplicationFactory`, extending `WebApplicationFactory<Program>` where `Program` is `Catalog.App.Program` |
| W2 | Override `ConfigureWebHost`; inside `ConfigureServices` remove every real infrastructure registration **before** adding the fake (repositories, `IUnitOfWork`, both `DbContextOptions<T>`) |
| W3 | Register fakes as `Singleton` so state persists across requests within the same test (needed to assert persisted state after the HTTP call) |
| W4 | Do **NOT** override routing, middleware or controllers — only the DI container for outbound dependencies. If `UseHttpsRedirection()` (present in `Startup.Configure`) ever produces unexpected redirects in tests, override only the HTTPS-port setting (`builder.UseSetting("https_port", "0")`) — never touch the middleware pipeline |
| W5 | Extract a shared static helper (e.g. `ServiceCollectionExtensions.ReplaceWithInMemoryFake<TService, TFake>`) to keep the remove-then-add dance (I7) in one place |

### Example — full factory (repositories + both DbContexts)

```csharp
public class RecetaControllerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.ReplaceWithInMemoryFake<IRecetaRepository, InMemoryRecetaRepository>();
            services.ReplaceWithInMemoryFake<IAlimentoRepository, InMemoryAlimentoRepository>();

            var uowDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
            if (uowDescriptor != null)
            {
                services.Remove(uowDescriptor);
            }
            services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();

            services.RemoveAll<DbContextOptions<ReadDbContext>>();
            services.AddSingleton(new DbContextOptionsBuilder<ReadDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options);

            services.RemoveAll<DbContextOptions<WriteDbContext>>();
            services.AddSingleton(new DbContextOptionsBuilder<WriteDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options);
        });
    }
}
```

---

## 11. Example — full controller test

Again, this app is CQRS: `Create` persists into the fake `IRecetaRepository`, and
`GetById` reads from the same fake → the created receta is visible immediately.

```csharp
public class RecetaControllerTest : IClassFixture<RecetaControllerWebApplicationFactory>
{
    private readonly RecetaControllerWebApplicationFactory _factory;

    public RecetaControllerTest(RecetaControllerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidRequest_PersistsReceta()
    {
        //Arrange
        var client = _factory.CreateClient();

        //Act
        var response = await client.PostAsJsonAsync("/api/v1/recetas",
            new { Nombre = "Ensalada de Frutas", Instrucciones = "Picar y mezclar." });

        //Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<RecetaCreateResponse>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotEqual(Guid.Empty, body.Data);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRecetaRepository>();
        var receta = await repository.FindByIdAsync(body.Data);
        Assert.NotNull(receta);
        Assert.Equal("Ensalada de Frutas", receta.Nombre.Value);
    }

    [Fact]
    public async Task GetById_WhenRecetaDoesNotExist_Returns404()
    {
        //Arrange
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        //Act
        var response = await client.GetAsync($"/api/v1/recetas/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

> `Create` generates a fresh `Guid` inside the domain factory, so the id is obtained from the
> response (`body.Data`), never guessed (I13).

---

## 12. Response DTOs (`Test/IntegrationTest/Setup/`)

`{Feature}Response` mirrors the `ApiResponse`/`ApiResponse<T>` envelope
(`WebApi/Utils/ApiResponse.cs`): `Success`, `Message`, `Errors`, `Data`. ASP.NET Core
serializes properties with camelCase, and `ReadFromJsonAsync` (System.Net.Http.Json) uses the
web defaults (case-insensitive), so PascalCase properties bind correctly.

```csharp
public class RecetaCreateResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public Guid Data { get; set; }
}
```

For paged endpoints mirror the `PagedList<T>` shape
(`Application/Utils/PagedList.cs`) inside a `Data` property, e.g. `AlimentoPageResponse.Data =
PagedList<AlimentoItemResponse>`.

---

## 13. Naming conventions

| Element | Convention | Example |
|---|---|---|
| Test class | `{Controller}Test` | `RecetaControllerTest` |
| Test method | `{Endpoint}_{Scenario}` | `Create_WithValidRequest` |
| Factory | `{Controller}WebApplicationFactory` | `RecetaControllerWebApplicationFactory` |
| Fake repository | `InMemory{Repository}` | `InMemoryRecetaRepository` |
| Response DTO | `{Feature}Response` | `RecetaCreateResponse` |
| Namespace | `Catalog.Tests.IntegrationTest.{Area}` | `Catalog.Tests.IntegrationTest.Controllers`, `Catalog.Tests.IntegrationTest.Setup` |

---

## 14. Coverage target and execution

Match the unit-testing conventions (§13 of `unit-testing-rules.md`): every controller must
have at least one happy-path and one failure-path test per endpoint that returns
anything other than 200 on failure.

```bash
# whole suite (unit + integration)
dotnet test Test/Test.csproj

# integration only
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.Integration"

# a single controller
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.IntegrationTest.Controllers.RecetaControllerTest"

# unit tests only (unchanged)
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.Domain|FullyQualifiedName~Catalog.Tests.Application"

# with coverage
dotnet test Test/Test.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage
```

---

## 15. Anti-patterns (DO NOT)

1. **DO NOT** point tests at a real database, container, or external service. Everything
   outbound is an in-memory fake.
2. **DO NOT** create a new `WebApplicationFactory` per test method — reuse the one injected via
   `IClassFixture`.
3. **DO NOT** hardcode entity IDs — fakes are singletons shared across every test in the class;
   collisions produce false failures. Take ids from the response or from `Guid.NewGuid()`.
4. **DO NOT** re-test business rules already covered by Domain/Application unit tests.
   Integration tests verify wiring, not logic (no duplicate-name assertions, no exception texts).
5. **DO NOT** parse response bodies manually with `JsonDocument`/`JsonSerializer` inline —
   define a `{Feature}Response` DTO in `Setup`.
6. **DO NOT** skip the HTTP-layer assertion (`EnsureSuccessStatusCode` / `StatusCode`) before
   asserting the body or persisted state.
7. **DO NOT** add business logic to an in-memory fake beyond reproducing infrastructure
   behavior (add, update, remove, not-found).
8. **DO NOT** assert on exception messages or stack traces for failure paths — assert
   `HttpStatusCode` (and the envelope) only.
9. **DO NOT** let a handler that injects `ReadDbContext`/`WriteDbContext` hit a real database.
   Swap its provider for EF Core InMemory in the factory (E-rules) and seed the read models
   directly (E6).
10. **DO NOT** assume a write command makes list endpoints return data — list endpoints read
    read models from `ReadDbContext`, not repository fakes (§4 / E6).
11. **DO NOT** call `services.AddDbContext<T>(...)` a second time to swap the provider — it is
    additive and throws "multiple database providers" (E4).
12. **DO NOT** try to run integration tests before guarding `Startup.Configure`'s
    `Migrate()`/seed block with `Database.IsRelational()` (§5).