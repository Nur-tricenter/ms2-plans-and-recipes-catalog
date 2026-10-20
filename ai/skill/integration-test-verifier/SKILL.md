---
name: integration-test-verifier
description: Verifies that generated integration tests comply with the INTEGRATION TESTING RULES defined in ai/project/integration-testing-rules.md. For a specific controller test class, a folder (Test/IntegrationTest), or the whole project, it reads the rules, analyzes compliance (I, F, W, E rule sets + anti-patterns), runs dotnet test (filtered to Catalog.Tests.Integration), and reports pass/fail per rule. Use it after integration-test-generator or when reviewing integration test compliance. Language: EN.
---

# Integration Test Verifier

Verifies that integration tests in the **ms2-plans-and-recipes-catalog** project comply with
the governance rules. **This skill reads rules, analyzes test files, runs the integration test
suite, and reports compliance.** It is the companion to `integration-test-generator`.

## Mandatory flow

### 1. Read the rules (always)

Before any verification, **read the current rules**:

- Rules: `ai/project/integration-testing-rules.md`

This is the **single source of truth**. **Do NOT hardcode or duplicate any rule here** —
always derive every check from the latest content of that file. If it does not exist, stop
and tell the user.

### 2. Determine the target

Expect/ask for one of these inputs:

- **A single test class**: verify that specific `{Controller}Test.cs` (plus its referenced
  `Setup/` files).
- **A folder of tests**: verify all files under `Test/IntegrationTest/` (recursive).
- **The entire integration suite**: verify everything under `Test/IntegrationTest/`.

### 3. Pre-check — required setup (blockers)

Before static analysis, verify the two prerequisites that let integration tests compile and
host the real WebApi:

1. `Test/Test.csproj` references `..\WebApi\WebApi.csproj`, `..\Infrastructure\Catalog.Infrastructure.csproj`,
   `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.InMemory`
   (rules §1). Missing → report **BLOCKED** (cannot compile).
2. `WebApi/Startup.cs` guards the migrate/seed block with `readContext.Database.IsRelational()`
   (rules §5, rule E8). Missing → report **BLOCKED** (the host fails to start against the
   InMemory provider and every test errors). Do **NOT** modify production code.

### 4. Static analysis — against the rules

For each integration test file in the target, read the rules document and evaluate **every
applicable rule**. Group checks by the sections of the rules doc:

- **File & structure** — `Test/IntegrationTest/Controllers/` + `Test/IntegrationTest/Setup/` layout
  (§4), namespaces `Catalog.Tests.IntegrationTest.*`, class naming, no placeholders.
- **Method naming** — `{Endpoint}_{Scenario}` (§13, rule I5), AAA comments `//Arrange`,
  `//Act`, `//Assert` (I4).
- **Controller test rules** (I1–I14) — one class per controller via
  `IClassFixture<{Controller}WebApplicationFactory>` (I1, I2); fresh client per test (I3);
  anonymous request bodies (I8); `ReadFromJsonAsync<{Feature}Response>()` (I9); HTTP layer
  asserted first (I10); envelope asserted before state (I11); persisted-state assertion via DI
  scope resolving the fake repository (I12); fresh `Guid.NewGuid()` ids (I13); no exception
  text assertions (I14).
- **Fakes** (F1–F5) — `InMemory{Repository}` per interface, `ConcurrentDictionary` backing,
  Singleton registration, no invented unique-key behavior, no business logic.
- **WebApplicationFactory** (W1–W5) — extends `WebApplicationFactory<Program>`
  (`Catalog.App.Program`), remove-then-add fakes, Singleton, no pipeline overrides.
- **EF Core InMemory** (E1–E8) — both `DbContextOptions<T>` swapped for InMemory; fixed db
  name per factory; no second `AddDbContext<T>`; **list endpoints seed the InMemory
  `ReadDbContext` read models directly** (E6); no mixing of domain aggregates and read models.
- **Status-code contract** (§6) — 400 for command failures, 404 for missing reads
  (PUT alimentos → 404), `[ApiController]` 400 `ProblemDetails`, unhandled → 500.
- **Anti-patterns** section (§15) — all 12 items.

For each rule, report **PASS or FAIL** with the `file:line` where the violation occurs. Do not
copy the rule text; reference the source rule id and section instead.

### 5. Dynamic verification — run the integration suite

After static analysis, **build the test project if needed**, then run and capture results:

```bash
dotnet build Test/Test.csproj
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.Integration" --verbosity normal
```

Parse the output to determine: total tests discovered, passed, failed (list each with name and
error), and skipped. Do not pipe/truncate the output; capture it fully and search for failures.
If the pre-check reported **BLOCKED**, do not run full verification — report the blocker.

### 6. Coverage analysis (optional, if requested)

```bash
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Catalog.Tests.Integration" --collect:"XPlat Code Coverage" --results-directory ./coverage
```

Target: every controller must have at least one happy-path and one failure-path test per
endpoint that returns anything other than 200 on failure (rules §14).

### 7. Report generation

Produce a structured report:

```
=== INTEGRATION TEST VERIFICATION REPORT ===
Target: {file or folder path}
Date: {current date}
Rules source: ai/project/integration-testing-rules.md

--- PRE-CHECK ---
  [OK|BLOCKED] Test.csproj references (WebApi, Infrastructure, Mvc.Testing, EFCore.InMemory)
  [OK|BLOCKED] Startup.cs Database.IsRelational() guard (§5)

--- STATIC ANALYSIS ---
File: {filename}
  [PASS] {rule_id} - {short description}
  [FAIL] {rule_id} - {short description} (line {n})
  ...
  Total: {pass} PASS / {fail} FAIL

--- DYNAMIC RESULTS ---
  Total: {n} | Passed: {n} | Failed: {n} | Skipped: {n}
  Failed tests:
    - {FullyQualifiedName}
      Error: {message}

--- SUMMARY ---
  Verdict: {BLOCKED | COMPLIANT | NON-COMPLIANT | PARTIAL}
```

### 8. Verdict logic

- **BLOCKED**: any pre-check prerequisite fails (project references or `Startup.cs` guard
  missing) — production/test-project change required before verification can even run.
- **COMPLIANT**: all static rules PASS and all tests PASS.
- **NON-COMPLIANT**: any static rule FAIL or any test FAIL.
- **PARTIAL**: all tests PASS but some static rules FAIL (fixable without breaking tests).

## Deliverables

- Return the full verification report.
- For each FAIL: **file path, line number**, and what rule id from the rules doc it violates.
- Suggest the fix when possible.
- Do **NOT** modify any files — this skill is read-only verification only.

## Differences from integration-test-generator

| Aspect | Generator | Verifier |
|--------|-----------|----------|
| Creates files | Yes (+ adds required `Test.csproj` references/packages) | No |
| Reads rules | Yes | Yes |
| Runs tests | No | Yes (filtered `Catalog.Tests.Integration`) |
| Checks pre-requisites (`Test.csproj`, `Startup.cs` guard) | Yes (reports/blocked) | Yes (report as BLOCKED) |
| Checks compliance | No | Yes |
| Modifies code | Yes (test files + test project setup only) | No |