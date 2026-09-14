---
date: 2026-09-14T12:00:00+02:00
researcher: claude
git_commit: 788988b964f51aeeaebbe813677e85e3c1adef7e
branch: docs/f01-data-schema-plan
repository: klesniakamnis/10x-cards
topic: "Ground rollout Phase 1: SM-2 scheduling correctness (Risk #1) and AI proposal parser contract (Risk #3)"
tags: [research, testing, sm2, ai-generation, unit-tests, xunit]
status: complete
last_updated: 2026-09-14
last_updated_by: claude
---

# Research: Critical-path unit coverage — SM-2 and AI proposal parser

**Date**: 2026-09-14
**Researcher**: claude
**Git Commit**: 788988b
**Branch**: docs/f01-data-schema-plan
**Repository**: klesniakamnis/10x-cards

## Research Question

Ground the real failure paths for test-plan Risk #1 (SM-2 scheduling correctness) and Risk #3 (AI generation proposal contract drift). For each: locate the code anchor, quote relevant lines, verify or correct the test plan's response guidance, identify the cheapest useful test layer, and flag any testability issues.

## Summary

Both risk areas are well-suited for unit testing with minimal infrastructure. The SM-2 implementation is a single static method (`Sm2Service.Calculate`) that follows the published spec correctly, with one testability issue: `DateTime.UtcNow` embedded in the method prevents deterministic NextReviewDate assertions. The AI proposal parser (`OpenAiFlashcardGenerator`) takes an `IChatClient` via constructor injection, making it stubbable — the parsing logic handles valid JSON, markdown-fenced JSON, and throws on malformed input. No existing tests exist for either. The test plan's response guidance is confirmed with minor refinements.

## Detailed Findings

### Risk #1 — SM-2 Scheduling Correctness

#### Code anchor

**Entry point:** [Sm2Service.cs:5](Services/Sm2Service.cs:5) — `Sm2Service.Calculate(double easinessFactor, int interval, int repetitions, int grade)`

**Return type:** [Sm2Service.cs:36](Services/Sm2Service.cs:36) — `Sm2Result(double EasinessFactor, int Interval, int Repetitions, DateTime NextReviewDate)`

**Caller:** [FlashcardEndpoints.cs:129](Endpoints/FlashcardEndpoints.cs:129) — `Sm2Service.Calculate(flashcard.EasinessFactor, flashcard.Interval, flashcard.Repetitions, request.Grade)` in the `POST /api/flashcards/{id}/review` endpoint.

**Entity state shape:** [Flashcard.cs:17-20](Data/Flashcard.cs:17-20) — SM-2 fields on the entity:
```csharp
public double EasinessFactor { get; set; } = 2.5;  // default matches SM-2 spec
public int Interval { get; set; }                   // default 0
public int Repetitions { get; set; }                // default 0
public DateTime NextReviewDate { get; set; }        // default DateTime.MinValue
```

#### Spec verification

The implementation matches the published SM-2 algorithm exactly:

1. **Grade >= 3 (correct):** interval progression 1 → 6 → round(interval × EF), repetitions++ — [Sm2Service.cs:12-18](Services/Sm2Service.cs:12-18) ✓
2. **Grade < 3 (incorrect):** reset repetitions=0, interval=1 — [Sm2Service.cs:22-23](Services/Sm2Service.cs:22-23) ✓
3. **EF update formula:** `EF + (0.1 - (5-q)*(0.08 + (5-q)*0.02))` — [Sm2Service.cs:26](Services/Sm2Service.cs:26) ✓
4. **EF floor at 1.3:** — [Sm2Service.cs:27-28](Services/Sm2Service.cs:27-28) ✓
5. **EF updated AFTER interval calculation:** The method receives `easinessFactor` as param, uses it for interval calc at line 16, then updates it at line 26 — correct ordering ✓
6. **Grade validation:** `ArgumentOutOfRangeException` for grade < 0 or > 5 — [Sm2Service.cs:7-8](Services/Sm2Service.cs:7-8) ✓

#### Testability issue: `DateTime.UtcNow`

[Sm2Service.cs:30](Services/Sm2Service.cs:30): `var nextReviewDate = DateTime.UtcNow.AddDays(interval);`

The method is pure EXCEPT for this clock dependency. Three test options:

1. **Pragmatic (recommended):** Assert `interval`, `EasinessFactor`, and `Repetitions` exactly. Assert `NextReviewDate` approximately: `Assert.InRange(result.NextReviewDate, DateTime.UtcNow.AddDays(expectedInterval).AddSeconds(-2), DateTime.UtcNow.AddDays(expectedInterval).AddSeconds(2))`. This is safe because the date is derived from the deterministic `interval`, and a 2-second window eliminates flakiness.
2. **Refactor to `TimeProvider`:** Accept an `abstract TimeProvider` parameter (the .NET 8+ clock abstraction). More testable but requires changing the method signature and all callers — overhead for Phase 1.
3. **Refactor to return interval only:** Remove `NextReviewDate` from `Sm2Result` and let the caller compute it. Most pure, but changes the API contract used by `FlashcardEndpoints.cs:134`.

Recommendation: option 1 for Phase 1 (pragmatic, no refactoring needed). Consider option 2 in a future phase if clock precision becomes important.

#### Independent oracle values (computed from SM-2 spec, NOT from the code)

Starting state: EF=2.5, interval=0, repetitions=0

**EF delta by grade (formula: Δ = 0.1 - (5-q)*(0.08 + (5-q)*0.02)):**
- q=0: Δ = 0.1 - 5*(0.08 + 5*0.02) = 0.1 - 5*0.18 = -0.80 → EF = 1.70
- q=1: Δ = 0.1 - 4*(0.08 + 4*0.02) = 0.1 - 4*0.16 = -0.54 → EF = 1.96
- q=2: Δ = 0.1 - 3*(0.08 + 3*0.02) = 0.1 - 3*0.14 = -0.32 → EF = 2.18
- q=3: Δ = 0.1 - 2*(0.08 + 2*0.02) = 0.1 - 2*0.12 = -0.14 → EF = 2.36
- q=4: Δ = 0.1 - 1*(0.08 + 1*0.02) = 0.1 - 0.10 = 0.00 → EF = 2.50
- q=5: Δ = 0.1 - 0*(0.08 + 0*0.02) = 0.10 → EF = 2.60

**EF floor scenario:** Starting EF=1.3, q=0 → EF = 1.3 + (-0.80) = 0.50 → clamped to 1.3

**Key test scenarios (9 cases):**

| # | Input (EF, interval, reps, grade) | Expected (EF, interval, reps) | Scenario |
|---|---|---|---|
| 1 | (2.5, 0, 0, 4) | (2.5, 1, 1) | First review, correct — interval=1, EF unchanged |
| 2 | (2.5, 1, 1, 4) | (2.5, 6, 2) | Second review, correct — interval=6 |
| 3 | (2.5, 6, 2, 4) | (2.5, 15, 3) | Third review, correct — interval=round(6×2.5)=15 |
| 4 | (2.5, 15, 3, 4) | (2.5, 38, 4) | Fourth review — interval=round(15×2.5)=38 |
| 5 | (2.5, 0, 0, 5) | (2.6, 1, 1) | Perfect recall — EF increases by 0.1 |
| 6 | (2.5, 0, 0, 3) | (2.36, 1, 1) | Hard correct — EF decreases by 0.14 |
| 7 | (2.5, 15, 3, 2) | (2.18, 1, 0) | Fail — interval=1, reps=0 (reset) |
| 8 | (2.5, 6, 2, 0) | (1.7, 1, 0) | Complete blackout — EF drops hard |
| 9 | (1.3, 1, 0, 0) | (1.3, 1, 0) | EF floor — already at 1.3, q=0 would push to 0.5, clamped to 1.3 |

Plus: grade=-1 and grade=6 → `ArgumentOutOfRangeException`

### Risk #3 — AI Generation Proposal Contract Drift

#### Code anchor

**Interface:** [IFlashcardGenerator.cs:5](Services/IFlashcardGenerator.cs:5) — `Task<List<FlashcardProposal>> GenerateFlashcardsAsync(string sourceText, CancellationToken cancellationToken = default)`

**DTO:** [IFlashcardGenerator.cs:8](Services/IFlashcardGenerator.cs:8) — `FlashcardProposal(string Question, string Answer)`

**Production implementation:** [OpenAiFlashcardGenerator.cs:7-73](Services/OpenAiFlashcardGenerator.cs:7-73) — takes `IChatClient` via primary constructor

**Dev stub:** [DevFlashcardGenerator.cs:3-23](Services/DevFlashcardGenerator.cs:3-23) — returns hardcoded proposals with 2s delay (not relevant for contract testing)

**Caller:** [GenerationEndpoints.cs:11-29](Endpoints/GenerationEndpoints.cs:11-29) — `POST /api/generation/` endpoint, injects `IFlashcardGenerator`

#### Parsing flow analysis

The `GenerateFlashcardsAsync` method at [OpenAiFlashcardGenerator.cs:23-51](Services/OpenAiFlashcardGenerator.cs:23-51) follows this chain:

1. **LLM call:** `chatClient.GetResponseAsync(messages)` → `ChatResponse` — [line 25-30](Services/OpenAiFlashcardGenerator.cs:25-30)
2. **Null safety:** `response.Text ?? ""` — [line 32](Services/OpenAiFlashcardGenerator.cs:32)
3. **Markdown fence stripping:** `StripMarkdownFences(content)` — [line 33](Services/OpenAiFlashcardGenerator.cs:33), regex at [line 60](Services/OpenAiFlashcardGenerator.cs:60): `^```(?:json)?\s*\n?(.*?)\n?\s*```$`
4. **JSON deserialization:** `JsonSerializer.Deserialize<List<JsonFlashcard>>(content, JsonOptions)` with `PropertyNameCaseInsensitive = true` — [line 37](Services/OpenAiFlashcardGenerator.cs:37)
5. **Null list guard:** `if (items is null) return []` — [line 38-39](Services/OpenAiFlashcardGenerator.cs:38-39)
6. **Field filtering:** `.Where(i => !string.IsNullOrWhiteSpace(i.Question) && !string.IsNullOrWhiteSpace(i.Answer))` — [line 42](Services/OpenAiFlashcardGenerator.cs:42)
7. **Mapping:** `.Select(i => new FlashcardProposal(i.Question!, i.Answer!))` — [line 43](Services/OpenAiFlashcardGenerator.cs:43)
8. **Error path:** `JsonException` → `InvalidOperationException` with truncated raw content (max 200 chars) — [lines 47-50](Services/OpenAiFlashcardGenerator.cs:47-50)

#### Stubbability

`IChatClient` from `Microsoft.Extensions.AI` (v9.5.0, [10x-cards.csproj:17](10x-cards.csproj:17)) is an interface. The test needs to stub `GetResponseAsync` to return a `ChatResponse` with controlled `.Text`. The `Microsoft.Extensions.AI` package includes abstractions that should be stubbable via a mock framework (Moq, NSubstitute) or a simple hand-written stub.

The `ChatResponse` type needs a constructor or factory that accepts text content. The test should construct a `ChatResponse` whose `.Text` property returns the desired JSON string. If the type is sealed/difficult to construct, a thin adapter or the `Microsoft.Extensions.AI.Testing` package (if available) may be needed — this is a research item for the plan phase.

#### Test scenarios (12 cases)

| # | Stubbed `response.Text` | Expected result | Scenario |
|---|---|---|---|
| 1 | `[{"question":"Q1","answer":"A1"}]` | 1 proposal: Q1/A1 | Valid single item |
| 2 | `[{"question":"Q1","answer":"A1"},{"question":"Q2","answer":"A2"}]` | 2 proposals | Valid multiple items |
| 3 | `` ```json\n[{"question":"Q","answer":"A"}]\n``` `` | 1 proposal | Markdown-fenced JSON |
| 4 | `` ```\n[{"question":"Q","answer":"A"}]\n``` `` | 1 proposal | Markdown fence without language tag |
| 5 | `[]` | Empty list | Empty array |
| 6 | `null` (response.Text is null) | Empty list | Null response text |
| 7 | `[{"question":"","answer":"A1"}]` | Empty list | Empty question filtered out |
| 8 | `[{"question":"Q1","answer":""}]` | Empty list | Empty answer filtered out |
| 9 | `[{"question":null,"answer":"A1"}]` | Empty list | Null question filtered out |
| 10 | `[{"question":"Q","answer":"A","extra":"field"}]` | 1 proposal | Extra fields ignored |
| 11 | `[{"Question":"Q","Answer":"A"}]` | 1 proposal | Case-insensitive field names |
| 12 | `not json at all` | Throws `InvalidOperationException` | Malformed response |
| 13 | `[{"question":"Q","answer` | Throws `InvalidOperationException` | Truncated JSON |
| 14 | `[{"question":"Q1","answer":"A1"},{"question":"","answer":""}]` | 1 proposal (second filtered) | Mixed valid/invalid items |

### Cross-cutting: Test project bootstrapping

No test project exists. Phase 1 must create:

- `tests/10x-cards.Tests/10x-cards.Tests.csproj` targeting `net9.0`
- Package references: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`
- For AI contract tests: `Microsoft.Extensions.AI` (to stub `IChatClient`)
- Project reference to the main `10x-cards` project
- Convention from AGENTS.md: xUnit, `tests/` directory

The solution file (`*.sln`) does not appear to exist — the project runs standalone via `dotnet run`. The test project can be added with `dotnet new xunit -o tests/10x-cards.Tests` and linked via `dotnet add tests/10x-cards.Tests reference 10x-cards.csproj`.

## Code References

- `Services/Sm2Service.cs:5` — SM-2 algorithm entry point (static method)
- `Services/Sm2Service.cs:30` — `DateTime.UtcNow` testability issue
- `Services/Sm2Service.cs:36` — Sm2Result record definition
- `Services/IFlashcardGenerator.cs:5` — Generator interface
- `Services/IFlashcardGenerator.cs:8` — FlashcardProposal DTO
- `Services/OpenAiFlashcardGenerator.cs:7` — Production generator (takes IChatClient)
- `Services/OpenAiFlashcardGenerator.cs:32-50` — Parsing chain (null safety → fence strip → deserialize → filter)
- `Services/OpenAiFlashcardGenerator.cs:47-50` — Error path (JsonException → InvalidOperationException)
- `Services/OpenAiFlashcardGenerator.cs:60` — Markdown fence regex
- `Services/OpenAiFlashcardGenerator.cs:63-66` — JSON options (case-insensitive)
- `Services/OpenAiFlashcardGenerator.cs:68-72` — Internal JsonFlashcard record (nullable fields)
- `Endpoints/FlashcardEndpoints.cs:118-139` — Review endpoint (SM-2 caller)
- `Endpoints/GenerationEndpoints.cs:11-29` — Generation endpoint (generator caller)
- `Endpoints/GenerationEndpoints.cs:16-17` — Server-side text limit validation (10,000 chars) ← corrects Risk #6 from test plan
- `Data/Flashcard.cs:17-20` — SM-2 state fields on entity
- `10x-cards.csproj:17` — Microsoft.Extensions.AI v9.5.0
- `Program.cs:24-31` — DI registration (DevFlashcardGenerator in dev, OpenAiFlashcardGenerator in prod)

## Architecture Insights

1. **SM-2 is a pure static method** — no DI, no state, no side effects except `DateTime.UtcNow`. Ideal for unit testing. The method is called from exactly one place (the review endpoint).

2. **Generator uses constructor-injected `IChatClient`** — the `Microsoft.Extensions.AI` abstraction provides a clean seam for stubbing. The internal `JsonFlashcard` record is `private sealed`, so tests cannot access it directly — they test via the public `GenerateFlashcardsAsync` method, which is correct (testing behavior, not internals).

3. **Markdown fence stripping is a separate concern** — the `StripMarkdownFences` method at [OpenAiFlashcardGenerator.cs:53-58](Services/OpenAiFlashcardGenerator.cs:53-58) handles LLM responses wrapped in ` ```json ... ``` `. This is a common LLM behavior pattern. The regex is `private static`, so it's tested indirectly through the main method — acceptable for unit scope.

4. **Error reporting includes raw content** — the `InvalidOperationException` at [line 49](Services/OpenAiFlashcardGenerator.cs:49) truncates raw LLM response to 200 chars. Tests should verify this doesn't leak sensitive user text beyond what's needed for debugging.

## Historical Context

- `context/changes/srs-review-session/plan.md:42-59` — SM-2 algorithm spec was documented during the SRS review session planning. The plan explicitly states "use the standard SM-2 formula exactly" and includes the pseudocode. The implementation matches this spec.
- `context/changes/srs-review-session/plan.md:28` — "No .NET SM-2 library exists with meaningful adoption; hand-rolling the standard formula (~20 lines) is the pragmatic choice." This confirms the hand-rolled approach was deliberate.

## Test Plan Corrections

### Risk #6 — Text input limit: server-side validation EXISTS

The test plan's Risk #6 framed the risk as "attacker bypasses client-side character limit." Research found that **server-side validation already exists** at [GenerationEndpoints.cs:16-17](Endpoints/GenerationEndpoints.cs:16-17):

```csharp
if (request.SourceText.Length > 10_000)
    return Results.Json(new { error = "Source text must not exceed 10,000 characters." }, statusCode: 400);
```

The risk should be reframed from "validation doesn't exist" to "regression of existing server-side validation." The Phase 2 integration test should verify this limit is enforced, not introduce it.

### Risk #1 — Response guidance refinement

The test plan's "Context needed" listed "edge cases (first review, grade < 3 reset, EF floor at 1.3)." Research confirms these are the correct edge cases and adds:
- `DateTime.UtcNow` testability issue — tests should use approximate assertions for `NextReviewDate`
- Grade boundary (q=3 vs q=2) is the pass/fail threshold — important edge case
- Starting EF at 1.3 (floor already hit) is a distinct scenario from "EF drops to floor"

### Risk #3 — Response guidance refinement

The test plan listed "IFlashcardGenerator interface, response DTO shape, parsing/mapping logic" as context needed. Research confirms and adds:
- The `IChatClient` stub mechanism needs verification during `/10x-plan` — check if `ChatResponse` is constructable in test context
- The `StripMarkdownFences` regex pattern covers `json` and no-language fences but may miss other LLM formatting quirks (e.g., leading whitespace before the fence)
- The `JsonFlashcard` internal record uses nullable `string?` for both fields — the null filtering is correct

## Open Questions

1. **`IChatClient` stubbability:** Can `ChatResponse` be constructed directly in tests, or does it require a test double library? The `Microsoft.Extensions.AI` package v9.5.0 may include test helpers — verify during `/10x-plan`.
2. **`DateTime.UtcNow` strategy:** Should Phase 1 use approximate assertions (simpler) or refactor to `TimeProvider` (more testable)? Recommendation: approximate assertions for Phase 1; consider `TimeProvider` if future phases need deterministic time.
3. **Solution file:** No `.sln` exists. Should `dotnet new sln` be run to create one, or continue with standalone project + test project? A solution file simplifies `dotnet test` from root.
