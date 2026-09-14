# Critical-path Unit Coverage — Plan Brief

> Full plan: `context/changes/testing-critical-path-coverage/plan.md`
> Research: `context/changes/testing-critical-path-coverage/research.md`
> Test plan: `context/foundation/test-plan.md` (§3 Phase 1)

## What & Why

Bootstrap the project's first test infrastructure and prove the two highest-confidence risks from the test plan: SM-2 scheduling correctness and AI proposal parser contract integrity. The project currently has zero tests, zero test config, and no solution file. This is rollout Phase 1 of the test plan — the cheapest layer (unit tests) for the most critical risks.

## Starting Point

No test infrastructure exists. SM-2 is implemented as a single static method (`Sm2Service.Calculate`) verified against the published spec. The AI parser (`OpenAiFlashcardGenerator`) takes an injectable `IChatClient` — research confirmed `ChatResponse` is directly constructable, so no mock library is needed.

## Desired End State

`dotnet test` runs from repo root and 25 tests pass: 11 for SM-2 (oracle values from the spec, not the code) and 14 for the AI parser (stubbed LLM responses covering valid, malformed, and edge cases). A reusable `FakeChatClient` test double is available for future tests.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
|---|---|---|---|
| IChatClient stubbing | Hand-written FakeChatClient | Zero dependencies, explicit, readable — ChatResponse is directly constructable so no mock library needed. | Research + Plan |
| DateTime.UtcNow strategy | Approximate assertions (5s tolerance) | No production code changes; Interval/EF/Repetitions tested exactly, NextReviewDate derived from deterministic Interval. | Research + Plan |
| Solution file | Yes, create .sln | Enables `dotnet test` from root; standard .NET convention; needed for CI in test plan Phase 3. | Plan |
| Oracle values | Independently computed from SM-2 spec | Avoids the oracle problem (copying expected values from the code under test). | Research |

## Scope

**In scope:**
- Solution file (.sln) with main + test projects
- xUnit test project (`tests/10x-cards.Tests/`)
- FakeChatClient test double
- 11 SM-2 unit tests (9 spec scenarios + 2 boundary validation)
- 14 AI parser unit tests (valid, malformed, empty, edge cases)

**Out of scope:**
- Integration tests (test plan Phase 2)
- CI/CD pipeline (test plan Phase 3)
- Production code refactoring (no TimeProvider injection)
- Mock library dependencies
- DevFlashcardGenerator testing (hardcoded stub, not a risk surface)

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Test infrastructure | .sln, xUnit project, FakeChatClient | Project reference or package version mismatch |
| 2. SM-2 unit tests | 11 tests proving algorithm correctness | Oracle problem — must use spec-derived values |
| 3. AI parser unit tests | 14 tests proving parsing chain resilience | Null ChatResponse.Text behavior needs runtime verification |

**Prerequisites:** None — greenfield test infrastructure.
**Estimated effort:** ~1 session across 3 phases.

## Open Risks & Assumptions

- `ChatResponse` constructed with null content may not produce `Text == null` — test #6 needs runtime verification during implementation.
- Approximate DateTime assertions assume test execution within 5 seconds — safe for CI but worth noting.

## Success Criteria (Summary)

- `dotnet test` passes 25 tests from the repo root
- SM-2 oracle values are independently derived from the published spec
- AI parser handles all 14 response shapes without regression
