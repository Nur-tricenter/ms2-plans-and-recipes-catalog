---
name: pact-generator
description: Generates standalone Pact consumer tests (xUnit + PactNet) for the ms2-plans-and-recipes-catalog microservice, reading and applying the rules defined in ai/project/pact-contract-testing-rules.md. Use it when asked to generate, create or write consumer contract tests for a controller endpoint, a CatalogApiClient method, or new interactions. It ONLY creates consumer-side code, it NEVER runs it. Language: EN.
---

# Pact Contract Test Generator (consumer side)

Generates **consumer-side contract tests** for the **ms2-plans-and-recipes-catalog** project,
strictly following the governance rules. **This skill ONLY creates/edits consumer test code. It
does NOT build, run or validate anything.**

## Mandatory flow

### 1. Read the rules (always)

- Rules: `ai/project/pact-contract-testing-rules.md`
- Supporting docs: `ai/project/README.md`, `ai/project/unit-testing-rules.md` (naming format only)
- Test project structure: `ai/project/README.md`

Always apply the **latest** version. If the rules file does not exist, stop and tell the user.

### 2. Understand the endpoints under contract

- Read the target controller(s) in `WebApi/Controllers/` (`AlimentoController`,
  `RecetaController`, `PlanAlimentarioController`) and the DTOs they bind.
- Identify the HTTP method, route, query parameters, request body shape and the response
  envelope (`ApiResponse<T>` in `WebApi/Utils/`, listable `data` mirrors `PagedList<T>` in
  `Application/Utils/`).
- Read the existing interactions in `Test/ContractTest/` so the new ones stay consistent and
  reuse `PactPaths`.

### 3. Determine the target

Expect one of:

- **A controller/endpoint**: if there is no consumer test for it yet, create one interaction (or a
  small set) covering the happy-path request/response the consumer relies on.
- **A `CatalogApiClient` method**: ensure the new client method has a matching interaction.
- **"Add a provider state to X"**: adjust only the consumer interaction — never the pact file.

### 4. Files to create/edit

- `Test/ContractTest/PactPaths.cs` — shared consumer/provider names (edit only to add a new
  consumer/provider pair to this project; do not duplicate constants elsewhere).
- `Test/ContractTest/Consumer/CatalogApiClient.cs` — add the `HttpClient` method + DTO records
  for the new interaction (camelCase wire format; property-name case-insensitive deserialization).
- `Test/ContractTest/Consumer/CatalogApiConsumerTests.cs` — add one `[Fact]` per interaction.

### 5. Interaction authoring (per rule C1-C11)

For each new interaction:

1. `_pactBuilder.UponReceiving("<unique verb phrase>")` (N3).
2. Declare the request: `.WithRequest(HttpMethod.X, "/api/v1/...")`, `.WithQuery(...)` for
   pagination (C8), `.WithJsonBody(new { ... })` (camelCase). Literal values for the typical call
   the consumer makes (C5).
3. Declare the response: `.WillRespond().WithStatus(HttpStatusCode.OK)` +
   `.WithHeader("Content-Type", "application/json; charset=utf-8")` (C7) +
   `.WithJsonBody(new { ... })`.
4. In the response body use matchers for anything the consumer does not pin down:
   - `Match.Type(...)` for strings/bools/GUIDs that must just be present and same-type,
   - `Match.Integer(...)` for integral fields and `Match.Number(...)` for decimal fields
     (**not** `Match.Decimal`, C11),
   - `Match.MinType(example, 1)` for arrays guaranteed non-empty by a `.Given(...)` state (C6),
   - `Match.MaxType(example, max)` for arrays that may legitimately be empty (C6) —
     **never** `Match.MinType(example, 0)` (throws, X1).
5. Wrap the block in `await _pactBuilder.VerifyAsync(async ctx => { ... })`, create the
   `CatalogApiClient` with `ctx.MockServerUri`, call the matching client method and assert only
   what the consumer depends on (C4). Fixed example GUIDs, never `Guid.NewGuid()` (C9).

## Rules you MUST follow

- Only happy-path / contract-shape tests. No error-path logic for infra failures.
- One `[Fact]` per interaction description; unique, stable descriptions (N3).
- Method names in English `{Action}_{Result}` (N2), AAA comments (`//Arrange`, `//Act / Assert`).
- **Provider states for data-backed interactions** (C10): list endpoints declare `.Given("<state>")`
  (e.g. `"existen alimentos registrados"`). Every `.Given(...)` MUST be handled provider-side by
  `ProviderStateMiddleware` (P7) using the exact same state name — add the handler when the state
  is new, or flag it to the caller so the provider side can be updated.
- Path computation: `PactDir = AppContext.BaseDirectory` + `..`/`..`/`..` + `ContractTest/pacts`
  (S4). Base dir is `Test/bin/{cfg}/{tfm}`, so exactly three `..` land on `Test/`.
- Put Pact logs through `XUnitOutput(ITestOutputHelper output)` in every `PactConfig` (S6).

## Deliverables

- The new/edited consumer files, the added interactions listed with their descriptions.
- Do NOT create `Test/ContractTest/pacts/*.json` manually — tell the caller to run:

```bash
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.ContractTest.Consumer"
```

- This skill never runs `dotnet test` and never touches the provider side or production code
  (beyond noting the allowed `Startup.ConfigureServices` `virtual` flag).