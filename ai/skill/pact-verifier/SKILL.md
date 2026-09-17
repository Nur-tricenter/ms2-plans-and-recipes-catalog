---
name: pact-verifier
description: Verifies that the Pact contract tests of ms2-plans-and-recipes-catalog comply with the PACT CONTRACT TESTING RULES defined in ai/project/pact-contract-testing-rules.md, and that the provider honours the generated pact. It analyzes consumer code (C-rules), pact file (G-rules), provider code (P-rules), regenerates the pact from the consumer tests, and runs the provider verification (dotnet test filtered to Catalog.Tests.ContractTest). Use it after pact-generator or when reviewing contract compliance. Language: EN.
---

# Pact Contract Test Verifier

Verifies that contract tests in the **ms2-plans-and-recipes-catalog** project comply with the
governance rules and, crucially, that the **provider honours the consumer contract**. It is the
companion to `pact-generator`. **This skill reads rules, analyzes code, REGENERATES the pact and
runs the provider verification. It never edits files (read-only verification).**

## Mandatory flow

### 1. Read the rules (always)

- Rules: `ai/project/pact-contract-testing-rules.md` (single source of truth — derive every check
  from its current content, never hardcode rules here).
- Supporting docs: `ai/project/README.md`.

If the rules file does not exist, stop and tell the user.

### 2. Determine the target

Expect one of:

- **A consumer test file / folder**: verify `Test/ContractTest/Consumer/`.
- **The whole contract harness**: verify `Test/ContractTest/` (consumer + pact file + provider).
- **A single interaction** identified by its `UponReceiving` description.

### 3. Static analysis — consumer code (C-rules, S-rules, N-rules)

For each consumer test file evaluate:

- Structure: location/namespace/paths per S1-S6; shared constants via `PactPaths` (S3); logs via
  `XUnitOutput` (S6).
- One interaction per `[Fact]`, unique stable description (C1, N3).
- `.WithRequest(...)`, `.WithQuery(...)` declared (C8), `.WithJsonBody(...)` camelCase.
- Response: `.WithHeader("Content-Type", "application/json; charset=utf-8")` (C7), matchers for
  unpinned values (C5), `Match.Number` (not `Match.Decimal`) for decimals (C11),
  `Match.MinType(example, 1)` for data-backed arrays and `Match.MaxType` for empty-capable arrays
  (never `MinType(example, 0)`) (C6, X1).
- Consumer assertions limited to what the consumer relies on (C4); fixed GUID examples (C9).
- Every `.Given(...)` state has a matching provider-side handler (C10, P7, G6).

Report each rule PASS/FAIL with `file:line` where the violation occurs.

### 4. Static analysis — pact file (G-rules)

Read `Test/ContractTest/pacts/catalog-web-client-catalog-api.json` and verify:

- File naming `{consumer}-{provider}.json` (G1), spec version 4.0 (G2), camelCase (G3).
- Body shape mirrors `ApiResponse<T>` and `PagedList<T>` (G5).
- The file is tracked by git (`git ls-files`) — regenerate if missing or stale (G4).

### 5. Static analysis — provider code (P-rules)

Verify provider files (`Provider/ContractPactStartup.cs`, `CatalogApiProviderFixture.cs`,
`CatalogApiProviderTests.cs`):

- Kestrel on a dynamic port, no `TestServer`/`WebApplicationFactory` (P1).
- `ContractPactStartup` calls `base.ConfigureServices` and swaps infra with
  `ReplaceInfrastructureWithInMemory` (P2, P3).
- `AddApplicationPart(typeof(Startup).Assembly)` present (P4, X3).
- Content root `WebApi/` via 4 `..` from `AppContext.BaseDirectory` (P5).
- Host owned by an `IClassFixture`; verification via `WithHttpEndpoint(fixture.ServerUri)` +
  `WithFileSource` + `WithProviderStateUrl(fixture.ServerUri + "provider-states")` (P6, P7, P8).
- `ProviderStateMiddleware` seeds the read model for every `.Given(...)` state name (P7).
- The only allowed production change is `Startup.ConfigureServices` being `virtual` (P2 section).

### 6. Dynamic verification — consumer first, then provider

Contract testing is strictly two phases (always this order):

```bash
# 1. Regenerate the pact (this is a generated artifact — do NOT edit it by hand)
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.ContractTest.Consumer"

# 2. Provider verification (replays the pact against the real API)
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.ContractTest.Provider"
```

Parse the outputs: consumer phase must report Passed == number of interactions; provider phase
must report `Pact verification successful`. Capture failures fully — for provider failures note the
exact mismatch (`expected … but was …`, status, header, body path). Do not truncate output.

### 7. Report generation

```
=== PACT CONTRACT TESTING VERIFICATION REPORT ===
Target: {path}
Date: {date}
Rules source: ai/project/pact-contract-testing-rules.md

--- STATIC ANALYSIS ---
Consumer  (S-rules):    [PASS]/[FAIL] per rule (file:line)
Consumer  (C-rules):    [PASS]/[FAIL] per rule (file:line)
Naming    (N-rules):    [PASS]/[FAIL] per rule
Pact file (G-rules):    [PASS]/[FAIL] per rule
Provider  (P-rules):    [PASS]/[FAIL] per rule
Anti-patterns (X):      [none found]/[found: Xn ...]

--- DYNAMIC RESULTS ---
Consumer: Total: {n} | Passed: {n} | Failed: {n} | pact file regenerated: {path}
Provider: interactions verified: {n} | Pact verification: {success/failed}
  Failures: {mismatch details}

--- SUMMARY ---
Verdict: {COMPLIANT | NON-COMPLIANT | PARTIAL}
```

### 8. Verdict logic

- **COMPLIANT**: all static rules PASS **and** consumer suite green **and** `Pact verification
  successful`.
- **NON-COMPLIANT**: any static FAIL or any consumer/provider test failure.
- **PARTIAL**: provider verification passes but static rules have non-blocking FAILs (fixable
  without changing the contract).

## Deliverables

- Full report; per FAIL: file path + line + rule id from the rules doc.
- For dynamic failures: quote the pact verifier mismatch so the consumer/provider diff is explicit.
- Suggest the fix (which side — consumer or provider — must change).
- Do **NOT** modify any file (read-only verification).

## Differences from pact-generator

| Aspect | Generator | Verifier |
|--------|-----------|----------|
| Creates/edits consumer test files | Yes | No |
| Reads rules | Yes | Yes |
| Regenerates pact (dotnet test consumer) | No (tells user) | Yes |
| Runs provider verification | No | Yes |
| Checks compliance | No | Yes |