---
project: "10xCards"
version: 1
status: active
created: 2026-09-14
test_base_profile: none
---

# Test Plan: 10xCards

> Derived from `context/foundation/prd.md` (v1), `context/foundation/roadmap.md`, hot-spot scan (2026-09-14), and user interview.
> Edit §3 status cells as rollout progresses. For structural changes, run `/10x-test-plan --refresh`.

## §1 Strategy

### Principles

1. **Cost × signal.** Every test the rollout adds must answer one question: *what is the cheapest test that gives a real signal for this risk?* Do not promote to e2e because it "feels safer"; do not layer a vision model on top of a deterministic diff that already catches the regression.

2. **User concerns are evidence.** Risks the team has lived through carry the same weight as PRD lines or hot-spot data.

3. **Risks are scenarios, not code locations.** The risk map (§2) cites evidence — PRD lines, interview answers, hot-spot directories with churn counts. It never asserts a file as "where the failure lives." File:line anchors are `/10x-research` output, produced per rollout phase against the current code. If a §2 row cites `src/foo/bar.ts:42`, it has crossed the line.

### Approach

The project has **no existing test infrastructure** — no test runner, no test project, no test files. The rollout bootstraps the test runner in Phase 1 (cheapest layer for the highest-confidence risks), adds integration tests in Phase 2 (risks requiring HTTP pipeline and DB), and wires CI quality gates in Phase 3 (locks the floor).

## §2 Risk Map

### Top Risks

| # | Risk (failure scenario) | Impact | Likelihood | Source(s) |
|---|---|---|---|---|
| 1 | SM-2 scheduling produces wrong intervals / easiness factor — student reviews on wrong schedule, SR benefit lost silently | High | Medium | Interview Q3 (low-confidence area); PRD FR-013–FR-015; `Services/` 5 changes/30d |
| 2 | Silent data corruption in flashcard / review persistence — student's deck or review history lost or corrupted without visible error | High | Medium | Interview Q1 (top worry), Q2 (burned by EF migrations); `Data/` hottest dir 16 changes/30d; 6 slices extending same tables |
| 3 | AI generation → proposal contract drift — LLM output format changes break parsing, producing garbage or zero proposals | High | Medium | PRD US-01, FR-004–FR-008 (north star flow); external LLM dependency; no contract test exists |
| 4 | Auth session / magic-link regression — user locked out or session lost mid-study, losing review progress | High | Low | PRD FR-001–FR-003, NFR (all views behind auth wall); single point of failure for every product feature |
| 5 | Ownership bypass (IDOR) on flashcard endpoints — authenticated User A reads/modifies User B's flashcards by guessing IDs | High | Medium | PRD Access Control (per-user data isolation); `Endpoints/` 5 changes/30d; every CRUD endpoint must enforce ownership |
| 6 | Text input limit bypass → unbounded LLM cost — attacker bypasses client-side character limit, sends massive payload to generation endpoint | Medium | Medium | PRD FR-004 (text limit); server-side validation is the security boundary, not the frontend |

### Impact × Likelihood Rubric

- **High impact:** Data loss, privacy violation, core product loop broken, user locked out.
- **Medium impact:** Cost overrun, degraded UX, metric distortion.
- **Low impact:** Cosmetic, low-traffic path, internal tooling.
- **High likelihood:** Active churn area (10+ changes/30d), no guard exists, external dependency.
- **Medium likelihood:** Moderate churn (3–9 changes/30d), partial guard, known failure pattern.
- **Low likelihood:** Stable area (0–2 changes/30d), established pattern, low attack surface.

### Risk Response Guidance

| Risk # | What would prove protection | Must challenge | Context needed | Likely cheapest layer | Anti-pattern to avoid |
|---|---|---|---|---|---|
| 1 | Given a flashcard with known review history (specific grades on specific dates), SM-2 computes expected next interval, EF, and repetition count matching the published SM-2 spec | "Implementation matches the formula" — expected values must come from the SM-2 spec, not from running the code and asserting current output | SM-2 entry point, state shape (interval, EF, repetitions, next_review_date), edge cases (first review, grade < 3 reset, EF floor at 1.3) | Unit test (pure function, no I/O) | **Oracle problem** — copying expected values from the implementation instead of computing independently from the SM-2 algorithm spec |
| 2 | After a CRUD + review sequence, the database state matches expectations — no orphaned records, no lost review history, correct FK integrity | "SaveChangesAsync returned without error" — the test must read back persisted data and verify state | DbContext config, entity relationships (User → Flashcard → review state), cascade behavior, nullable FK handling | Integration test (needs real or realistic DB) | Trusting EF in-memory provider to behave identically to real SQL; testing only creation without read-back verification |
| 3 | Given known LLM response shapes (valid, malformed, truncated, empty), the proposal parser produces expected structured output or fails gracefully without creating corrupt proposals | "Parser works because we tested it with one good response" — must cover edge cases: empty array, missing fields, extra fields, truncated JSON | IFlashcardGenerator interface, response DTO shape, parsing/mapping logic | Unit test with stubbed LLM responses (contract test) | Coupling test to a specific model version's actual output; testing only the happy path |
| 4 | Unauthenticated request to protected endpoint returns 401/redirect without leaking resource existence; authenticated request accesses correct user's data | "Auth works because the middleware is registered" — test must verify endpoint-level rejection, not just middleware existence | Auth middleware registration, session/token validation, endpoint authorization | Integration test (WebApplicationFactory) | Testing only the happy path (valid session) without testing unauthorized access |
| 5 | User A cannot read, update, or delete User B's flashcards via any endpoint, even by guessing/enumerating IDs | "Ownership checked because the query filters by userId" — test must attempt cross-user access and verify rejection | Endpoint ownership filters, userId extraction from session, which endpoints accept flashcard IDs | Integration test (two authenticated users + HTTP) | Testing only with the owning user; never testing with a non-owning authenticated user |
| 6 | Generation request exceeding server-side limit is rejected before reaching the LLM client | "Limit enforced because the frontend validates it" — test must send raw HTTP request bypassing frontend | Generation endpoint, validation logic, character limit constant | Integration test (HTTP with oversized payload) | Testing only via the UI; treating client-side validation as a security boundary |

## §3 Phased Rollout

| # | Phase name | Goal | Risks covered | Test types | Status | Change folder |
|---|---|---|---|---|---|---|
| 1 | Critical-path unit coverage | Bootstrap xUnit project; prove SM-2 math against the spec; prove AI proposal parser handles valid and malformed responses | #1, #3 | Unit tests | planned | testing-critical-path-coverage |
| 2 | Integration safety net | Prove data persistence integrity, auth enforcement, ownership isolation, and input validation across the HTTP pipeline | #2, #4, #5, #6 | Integration tests (WebApplicationFactory) | not started | — |
| 3 | Quality gates wiring | Wire GitHub Actions CI to run tests on PR; fail build on test failure; lock the test floor | All (regression prevention) | CI configuration, pre-merge gate | not started | — |

### Order rationale

- **Phase 1 first:** Cheapest layer (unit tests, no infrastructure) for highest-confidence risks (#1, #3). Establishes the test runner and project structure that Phase 2 builds on.
- **Phase 2 second:** Requires the test project from Phase 1. Covers risks that need real HTTP pipeline and DB interaction — more setup cost but higher blast-radius risks (#2, #5).
- **Phase 3 last:** Locks the floor after Phases 1–2 deliver the suite. Without a CI gate, tests exist but regressions can still ship silently.

## §4 Stack

- **Language / framework:** C# / ASP.NET Core 9.0 / .NET 9.0
- **Test runner:** None yet. Convention from AGENTS.md: xUnit, `tests/` directory, `ProjectName.Tests` naming.
- **Test base profile:** `none` — no test config, no test project, no test files.
- **Deployment:** Azure App Service F1 (free tier), manual deploy via `az webapp up`. No CI/CD pipeline.
- **Database:** Entity Framework Core with migrations (provider TBD per environment — likely SQLite dev / Azure SQL prod based on infrastructure.md).
- **AI provider:** OpenAI (per `Services/OpenAiFlashcardGenerator.cs` existence in hot-spot scan; `Services/DevFlashcardGenerator.cs` as local dev stub).
- **Auth:** Custom magic-link implementation (per `Auth/` directory, `Data/MagicLinkToken.cs`, `Data/AuthSession.cs`).

**Stack grounding tools (current session):**
- Docs: not available in current session — no Context7 or framework docs MCP detected; checked: 2026-09-14
- Search: WebSearch available via ToolSearch but not used for this plan; checked: 2026-09-14
- Runtime/browser: Claude Browser available — possible for visual verification of UI; checked: 2026-09-14
- Provider/platform: none detected in current session; checked: 2026-09-14

## §5 Discovery Sources

| Source | Path | Type | Gist |
|---|---|---|---|
| PRD | `context/foundation/prd.md` | PRD | AI flashcard generation from pasted text; magic-link auth; SM-2 reviews; 15 must-have FRs |
| Roadmap | `context/foundation/roadmap.md` | Roadmap | 6 slices (F-01, F-02, S-01–S-04), all in-progress; north star = S-01 |
| Tech stack | `context/foundation/tech-stack.md` | Tech-stack | C# / ASP.NET Core 9.0 / Azure App Service / GitHub Actions |
| Infrastructure | `context/foundation/infrastructure.md` | Infra | Azure App Service F1; no CI/CD; .NET 9 STS EOL Nov 2026 |
| Shape notes | `context/foundation/shape-notes.md` | Brief | SM-2 (0–5 scale); passwordless magic link; PL+EN; Chrome+Firefox desktop |
| AGENTS.md | `AGENTS.md` | Rules | Minimal APIs only; PascalCase files; xUnit when added; nullable refs enabled |
| Hot-spot scan | git log (2026-09-14) | Churn | 12 commits/30d; `Data/` (16), `Pages/` (13), `Migrations/` (12), `Services/` (5), `Endpoints/` (5) |
| User interview | Phase 2 (2026-09-14) | Interview | Top worry: data loss; burned by: EF migrations; low confidence: SM-2 math; skip: migration SQL tests |

## §6 Cookbook

Test patterns and conventions shipped by the rollout. Each phase fills in its section when it completes.

### Phase 1 — Critical-path unit coverage

- TBD — see §3 Phase 1 for SM-2 scheduling correctness pattern (verify against published spec, not implementation output).
- TBD — see §3 Phase 1 for AI proposal parser contract pattern (stubbed responses: valid, malformed, truncated, empty).

### Phase 2 — Integration safety net

- TBD — see §3 Phase 2 for data persistence read-back verification pattern.
- TBD — see §3 Phase 2 for auth enforcement pattern (reject unauthenticated, no resource-existence leakage).
- TBD — see §3 Phase 2 for ownership isolation / IDOR pattern (cross-user access attempt).
- TBD — see §3 Phase 2 for input validation pattern (server-side limit, bypass frontend).

### Phase 3 — Quality gates wiring

- TBD — see §3 Phase 3 for CI gate configuration and test-floor enforcement.

## §7 Negative Space

What this plan explicitly does NOT test, and why.

| Area | Why excluded | Revisit when |
|---|---|---|
| Generated EF migration SQL | User decision (Q5): EF Core generates it; the generator is the test. Schema *state after migration* is covered by §2 Risk #2. | Migration causes data loss in a non-dev environment |
| CSS / visual polish | Low blast radius; no design system; styling is functional, not brand-critical | Design system adopted or visual regression reported |
| Landing / login page static content | Simple content, low churn (`Auth/` 1 change/30d), few users at MVP scale | Public pages become conversion-critical |
| Mobile responsiveness | PRD Non-Goal: mobile not guaranteed; Chrome+Firefox desktop only | Mobile support added to scope |
| PDF/DOCX/EPUB import | PRD Non-Goal: text-paste only in MVP | Import feature added |
| Multi-user collaboration / sharing | PRD Non-Goal: single-user data model | Sharing feature added |
| LLM prompt quality / flashcard pedagogical quality | Product metric (75% acceptance), not a test-automatable property; measured in production | Acceptance rate drops below threshold consistently |
| Performance / load testing | Target scale: small (single-digit users); no latency SLA beyond "progress indicator > 2s" | User base grows or latency SLA added |
