# First Gated Generation — Plan Brief

> Full plan: `context/changes/first-gated-generation/plan.md`

## What & Why

Build the north star slice for 10xCards: a user pastes text, the system generates flashcard proposals via OpenAI, and the user accepts/edits/rejects each one — with accepted proposals saved as flashcards. This is the minimal end-to-end flow that validates the product hypothesis: AI-generated flashcards with human quality gate are good enough that users accept ≥75% of them.

## Starting Point

The project is a headless ASP.NET Core 9.0 API with EF Core + SQLite, cookie-based magic link auth, and zero frontend. The Flashcard entity exists with a `FlashcardSource.AiGenerated` enum value. No LLM integration, no UI, no static files — everything frontend and AI is greenfield.

## Desired End State

An authenticated user opens the app in a browser, sees a textarea, pastes text, clicks "Generate", waits through a loading spinner, then reviews a list of AI-generated Q/A proposal cards. Each card can be accepted (creates a Flashcard in the DB), edited inline then accepted, or rejected (removed from UI). After all proposals are handled, a summary shows how many flashcards were added to the deck, with an option to generate more.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
|---|---|---|---|
| Frontend technology | Razor Pages + vanilla JS | Zero new tooling for a solo .NET dev; fastest to ship with no Node/npm build step. | Plan |
| LLM provider | OpenAI API via Microsoft.Extensions.AI | Best .NET integration (Microsoft-backed), cheap (GPT-4o-mini ~$0.15/1M tokens), API data not used for training by default. | Plan |
| Proposal persistence | Transient (frontend state) | Simplest — no new DB tables, no proposal lifecycle; regeneration is cheap. | Plan |
| UI scope | Minimal app shell + generation page | Enough to test the full flow; no login page, no landing page, no deck view. | Plan |
| Generation UX | Loading spinner, then full list | Simplest implementation; no SSE/streaming complexity. | Plan |
| Text input limit | 10,000 characters | PRD starting point; fits GPT-4o-mini context with room for prompt + output; ~$0.002/call. | Plan |
| LLM error handling | Show error message, let user retry | Simple UX; text stays in form for retry; details logged server-side. | Plan |
| Card interaction | Inline cards with action buttons | Matches PRD's "inline edit, no separate screen" requirement. | Plan |
| Zero results handling | Message + keep text in form | No pre-validation; clear feedback without blame. | Plan |
| LLM testing | IFlashcardGenerator interface + dev stub | Follows existing IEmailSender/ConsoleEmailSender pattern; zero API cost during development. | Plan |
| Session end | Summary message + generate more option | Clear closure; natural transition to next text fragment. | Plan |
| Cut line | Edit is first to drop | Accept + reject alone validate the "human gate" hypothesis; edit can land in S-03. | Plan |

## Scope

**In scope:**
- Razor Pages infrastructure + minimal app shell (layout, header, logout)
- OpenAI integration with IFlashcardGenerator abstraction + dev stub
- POST /api/generation endpoint (text → proposals)
- POST /api/flashcards endpoint (accept proposal → create Flashcard)
- Client-side generation flow (AJAX, loading, cards, accept/reject/edit, summary)
- Content negotiation for auth (401 for API, redirect for browser)
- LoginRequired page for unauthenticated browser requests

**Out of scope:**
- Login/registration UI, landing page
- Proposal persistence in database
- Deck view / flashcard list (S-03)
- Streaming/SSE generation
- Bulk accept/reject
- Analytics / acceptance rate measurement
- Automatic LLM retry

## Architecture / Approach

Razor Pages serve the HTML shell; vanilla JavaScript handles the interactive generation flow via AJAX calls to two new API endpoints. The LLM integration sits behind an `IFlashcardGenerator` interface — production uses OpenAI via `IChatClient` (Microsoft.Extensions.AI), development uses a hardcoded stub. Proposals flow: API → JSON response → JS renders cards → user decisions → Accept POSTs to create Flashcard entity. No new DB tables or migrations needed.

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Razor Pages + App Shell | HTML-rendering web app with layout, Generate page skeleton, LoginRequired page | Content negotiation change may break existing API auth behavior |
| 2. AI Service + Generation Endpoint | IFlashcardGenerator + OpenAI impl + dev stub + POST /api/generation | Microsoft.Extensions.AI package API may have changed since research |
| 3. Flashcard Creation + Frontend Integration | POST /api/flashcards + full JS flow (generate → cards → accept/reject/edit → summary) | Vanilla JS complexity for inline edit interaction |

**Prerequisites:** F-01 review fixes (uncommitted: IHasTimestamps, MaxLength, absolute paths) should be committed + migrated first.
**Estimated effort:** ~2–3 sessions across 3 phases.

## Open Risks & Assumptions

- Microsoft.Extensions.AI.OpenAI 10.10.0 API may have breaking changes vs training data — verify package API at implementation time
- GPT-4o-mini prompt for Q/A extraction needs empirical tuning — ship a working prompt, iterate based on real usage
- OpenAI API key management on Azure F1 (environment variables) — not tested yet
- Inline edit with vanilla JS is the most complex UI interaction — this is the cut line if S-01 takes too long

## Success Criteria (Summary)

- A user can paste text, generate AI proposals, accept/reject them, and see accepted flashcards in the database with `Source = AiGenerated`
- The dev stub enables full-flow testing without an OpenAI API key
- The generation endpoint validates text length and handles LLM errors gracefully
