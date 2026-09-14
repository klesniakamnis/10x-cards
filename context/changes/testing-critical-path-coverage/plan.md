# Critical-path Unit Coverage — Implementation Plan

## Overview

Bootstrap an xUnit test project and write unit tests for the two highest-confidence risks in the test plan: SM-2 scheduling correctness (Risk #1) and AI proposal parser contract drift (Risk #3). This is rollout Phase 1 of `context/foundation/test-plan.md`.

## Current State Analysis

The project has no test infrastructure — no test runner, no test project, no solution file. The two components under test are well-isolated:

- **Sm2Service.Calculate** (`Services/Sm2Service.cs:5`) — a static method implementing the SM-2 algorithm. Pure except for `DateTime.UtcNow` at line 30. Verified against published SM-2 spec; implementation is correct.
- **OpenAiFlashcardGenerator.GenerateFlashcardsAsync** (`Services/OpenAiFlashcardGenerator.cs:23`) — takes `IChatClient` via constructor injection. Parses LLM JSON response, strips markdown fences, filters empty proposals. `ChatResponse` is directly constructable (`new ChatResponse(new ChatMessage(ChatRole.Assistant, text))`).

## Desired End State

Running `dotnet test` from the repo root executes all unit tests and they pass. The SM-2 tests prove the algorithm matches the published spec using independently computed oracle values. The AI parser tests prove the parsing chain handles valid, malformed, and edge-case LLM responses gracefully. A `FakeChatClient` test double enables future AI-related tests without mock library dependencies.

### Key Discoveries:

- `Services/Sm2Service.cs:30` — `DateTime.UtcNow` prevents deterministic NextReviewDate assertions; approximate assertions (5s tolerance) are the pragmatic solution.
- `Microsoft.Extensions.AI.ChatResponse` is a class with a `ChatResponse(ChatMessage)` constructor; `ChatMessage(ChatRole, string)` provides the text. No mock library needed.
- `Services/OpenAiFlashcardGenerator.cs:60` — `StripMarkdownFences` regex handles ` ```json ` and bare ` ``` ` fences.
- `Services/OpenAiFlashcardGenerator.cs:63-66` — JSON deserialization uses `PropertyNameCaseInsensitive = true`.
- `Services/OpenAiFlashcardGenerator.cs:68-72` — Internal `JsonFlashcard` record has nullable `string?` fields; the Where filter at line 42 excludes null/empty values.
- `Endpoints/GenerationEndpoints.cs:16-17` — Server-side 10,000-char text limit already exists (corrects test plan Risk #6 for Phase 2).

## What We're NOT Doing

- No integration tests (Phase 2 of the test plan).
- No CI/CD pipeline changes (Phase 3 of the test plan).
- No refactoring of production code (no TimeProvider injection for DateTime.UtcNow).
- No mock library dependency (Moq, NSubstitute) — hand-written FakeChatClient only.
- No testing of the generation endpoint itself (that's HTTP pipeline — Phase 2).
- No testing of DevFlashcardGenerator (it's a hardcoded stub, not a risk surface).

## Implementation Approach

Three phases: (1) scaffold test infrastructure, (2) SM-2 unit tests with spec-derived oracle values, (3) AI parser contract tests with stubbed LLM responses. Each phase builds on the previous and is independently verifiable.

## Phase 1: Test infrastructure

### Overview

Create the solution file, xUnit test project, project references, and the FakeChatClient test double.

### Changes Required:

#### 1. Solution file

**File**: `10x-cards.sln` (new, at repo root)

**Intent**: Create a solution file that includes both the main project and the test project, enabling `dotnet test` from the repo root.

**Contract**: `dotnet new sln` + `dotnet sln add 10x-cards.csproj` + `dotnet sln add tests/10x-cards.Tests/10x-cards.Tests.csproj`

#### 2. Test project

**File**: `tests/10x-cards.Tests/10x-cards.Tests.csproj` (new)

**Intent**: Create an xUnit test project targeting net9.0 with a reference to the main project.

**Contract**: `dotnet new xunit -o tests/10x-cards.Tests` + `dotnet add tests/10x-cards.Tests reference 10x-cards.csproj`. The project must target `net9.0` and reference `Microsoft.Extensions.AI` (for ChatResponse/ChatMessage types used in FakeChatClient).

#### 3. FakeChatClient test double

**File**: `tests/10x-cards.Tests/Fakes/FakeChatClient.cs` (new)

**Intent**: A minimal hand-written implementation of `IChatClient` that returns a configurable text response, enabling AI parser tests without a mock library.

**Contract**: Implements `IChatClient`. Exposes a `ResponseText` property (default `"[]"`). `GetResponseAsync` returns `new ChatResponse(new ChatMessage(ChatRole.Assistant, ResponseText))`. `GetStreamingResponseAsync` throws `NotImplementedException`. `GetService` returns null. `Dispose` is a no-op.

### Success Criteria:

#### Automated Verification:

- `dotnet build` succeeds for the solution (both projects)
- `dotnet test` runs from repo root with zero tests (no failures)

#### Manual Verification:

- Solution file includes both projects when inspected

---

## Phase 2: SM-2 unit tests

### Overview

Write unit tests for `Sm2Service.Calculate` covering 11 scenarios: 9 spec-derived oracle cases + 2 boundary validation cases. Oracle values are computed independently from the SM-2 algorithm specification, not from running the code.

### Changes Required:

#### 1. SM-2 test class

**File**: `tests/10x-cards.Tests/Services/Sm2ServiceTests.cs` (new)

**Intent**: Test every branch of the SM-2 algorithm against independently computed expected values. Each test names the scenario it covers and asserts Interval, EasinessFactor, and Repetitions exactly, with NextReviewDate asserted within a 5-second tolerance.

**Contract**: Static `Sm2Service.Calculate(ef, interval, repetitions, grade)` returns `Sm2Result`. The 11 test cases:

| # | Input (EF, interval, reps, grade) | Expected (EF, interval, reps) | Scenario |
|---|---|---|---|
| 1 | (2.5, 0, 0, 4) | (2.5, 1, 1) | First review, correct |
| 2 | (2.5, 1, 1, 4) | (2.5, 6, 2) | Second review, correct |
| 3 | (2.5, 6, 2, 4) | (2.5, 15, 3) | Third review — round(6 * 2.5) = 15 |
| 4 | (2.5, 15, 3, 4) | (2.5, 38, 4) | Fourth review — round(15 * 2.5) = 38 |
| 5 | (2.5, 0, 0, 5) | (2.6, 1, 1) | Perfect recall — EF +0.1 |
| 6 | (2.5, 0, 0, 3) | (2.36, 1, 1) | Hard correct — EF -0.14 |
| 7 | (2.5, 15, 3, 2) | (2.18, 1, 0) | Fail — reset interval and reps |
| 8 | (2.5, 6, 2, 0) | (1.7, 1, 0) | Complete blackout — EF -0.8 |
| 9 | (1.3, 1, 0, 0) | (1.3, 1, 0) | EF floor — clamped to 1.3 |
| 10 | (2.5, 0, 0, -1) | ArgumentOutOfRangeException | Grade below range |
| 11 | (2.5, 0, 0, 6) | ArgumentOutOfRangeException | Grade above range |

EF assertions should use a tolerance of 0.001 for floating-point comparison. NextReviewDate assertions should verify `DateTime.UtcNow.AddDays(expectedInterval)` within a 5-second tolerance.

### Success Criteria:

#### Automated Verification:

- `dotnet test --filter "FullyQualifiedName~Sm2ServiceTests"` — all 11 tests pass
- `dotnet build` — solution builds without warnings

#### Manual Verification:

- Oracle values in test match independent hand-computation from SM-2 spec (cross-check at least 3 rows)

---

## Phase 3: AI proposal parser unit tests

### Overview

Write unit tests for `OpenAiFlashcardGenerator.GenerateFlashcardsAsync` covering 14 scenarios using `FakeChatClient` to control LLM responses. Tests verify the JSON parsing chain: null safety, markdown fence stripping, deserialization, field filtering, and error handling.

### Changes Required:

#### 1. AI parser test class

**File**: `tests/10x-cards.Tests/Services/OpenAiFlashcardGeneratorTests.cs` (new)

**Intent**: Test every branch of the parsing chain by varying the `FakeChatClient.ResponseText` and verifying the returned `List<FlashcardProposal>` or expected exception.

**Contract**: Instantiate `OpenAiFlashcardGenerator(fakeChatClient)` and call `GenerateFlashcardsAsync("any source text")`. The 14 test cases:

| # | FakeChatClient.ResponseText | Expected | Scenario |
|---|---|---|---|
| 1 | `[{"question":"Q1","answer":"A1"}]` | 1 proposal (Q1, A1) | Valid single item |
| 2 | `[{"question":"Q1","answer":"A1"},{"question":"Q2","answer":"A2"}]` | 2 proposals | Valid multiple items |
| 3 | `` ```json\n[{"question":"Q","answer":"A"}]\n``` `` | 1 proposal | Markdown-fenced JSON |
| 4 | `` ```\n[{"question":"Q","answer":"A"}]\n``` `` | 1 proposal | Bare markdown fence |
| 5 | `[]` | Empty list | Empty array |
| 6 | `null` (response.Text returns null) | Empty list | Null response text |
| 7 | `[{"question":"","answer":"A1"}]` | Empty list | Empty question filtered |
| 8 | `[{"question":"Q1","answer":""}]` | Empty list | Empty answer filtered |
| 9 | `[{"question":null,"answer":"A1"}]` | Empty list | Null question filtered |
| 10 | `[{"question":"Q","answer":"A","extra":"x"}]` | 1 proposal | Extra fields ignored |
| 11 | `[{"Question":"Q","Answer":"A"}]` | 1 proposal | Case-insensitive fields |
| 12 | `not json at all` | Throws InvalidOperationException | Malformed response |
| 13 | `[{"question":"Q","answer"` | Throws InvalidOperationException | Truncated JSON |
| 14 | `[{"question":"Q1","answer":"A1"},{"question":"","answer":""}]` | 1 proposal | Mixed valid/invalid |

For test #6 (null response text), `FakeChatClient` needs a mode where `ResponseText` being null produces a `ChatResponse` whose `.Text` returns null. This may require constructing `ChatResponse` with an empty `ChatMessage` or a message with null content — verify the exact behavior during implementation.

### Success Criteria:

#### Automated Verification:

- `dotnet test --filter "FullyQualifiedName~OpenAiFlashcardGeneratorTests"` — all 14 tests pass
- `dotnet test` — full suite (25 tests) passes from repo root
- `dotnet build` — solution builds without warnings

#### Manual Verification:

- FakeChatClient is reusable for future integration tests (verify it lives in a shared `Fakes/` directory)

---

## Testing Strategy

### Unit Tests:

- **SM-2**: 11 scenarios covering every branch (correct/incorrect grade, progression through repetitions, EF floor, boundary validation). Oracle values from the published SM-2 spec.
- **AI parser**: 14 scenarios covering the full parsing chain (valid JSON, markdown fences, empty/null/malformed responses, field filtering, case insensitivity).

### What this does NOT test (deferred to Phase 2):

- HTTP endpoint behavior (auth, ownership, validation)
- Database persistence and read-back
- End-to-end generation flow

## References

- Research: `context/changes/testing-critical-path-coverage/research.md`
- Test plan: `context/foundation/test-plan.md` (§3 Phase 1, §2 Risks #1 and #3)
- SM-2 algorithm spec: `context/changes/srs-review-session/plan.md:44-59`
- SM-2 implementation: `Services/Sm2Service.cs`
- AI parser implementation: `Services/OpenAiFlashcardGenerator.cs`
- Generator interface: `Services/IFlashcardGenerator.cs`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Test infrastructure

#### Automated

- [x] 1.1 `dotnet build` succeeds for the solution (both projects) — bf0341d
- [x] 1.2 `dotnet test` runs from repo root with zero tests — bf0341d

#### Manual

- [x] 1.3 Solution file includes both projects — bf0341d

### Phase 2: SM-2 unit tests

#### Automated

- [x] 2.1 `dotnet test --filter "FullyQualifiedName~Sm2ServiceTests"` — all 11 tests pass — 0d16b7b
- [x] 2.2 `dotnet build` — solution builds without warnings — 0d16b7b

#### Manual

- [x] 2.3 Oracle values match independent hand-computation from SM-2 spec — 0d16b7b

### Phase 3: AI proposal parser unit tests

#### Automated

- [x] 3.1 `dotnet test --filter "FullyQualifiedName~OpenAiFlashcardGeneratorTests"` — all 14 tests pass
- [x] 3.2 `dotnet test` — full suite (25 tests) passes from repo root
- [x] 3.3 `dotnet build` — solution builds without warnings

#### Manual

- [x] 3.4 FakeChatClient is reusable in shared Fakes/ directory
