# Spaced Repetition Review Session — Plan Brief

> Full plan: `context/changes/srs-review-session/plan.md`

## What & Why

Add spaced repetition review sessions to 10xCards using the SM-2 algorithm (scale 0–5). Without this, users can create and manage flashcards but have no way to study them with scientifically-backed scheduling — the core value loop (create → review → retain) is incomplete. This is the last slice in the milestone's main validation path (Stream A: F-01 → F-02 → S-01 → S-04).

## Starting Point

The Flashcard entity has content fields (Question, Answer, Source) and timestamps but no scheduling state. CRUD endpoints and a deck management page exist. The SM-2 algorithm family and 0–5 scale were decided in shape-notes; the schema extension was explicitly deferred from F-01 to this slice.

## Desired End State

A user clicks "Ucz się" in the header, sees due flashcards one at a time (question → reveal answer → rate 0–5), and the system schedules each card's next review. New cards are immediately due upon creation. After reviewing all due cards, the session shows a summary. Reviews are saved instantly — no data loss if the browser closes.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| SM-2 implementation | Hand-rolled from spec (~20 lines) | No mature .NET SM-2 library exists; the standard formula is trivial and dependency-free. |
| Data model | SM-2 fields directly on Flashcard entity | 1:1 relationship (one state per card per user); avoids joins for the due-cards query. |
| Session UX | Question → reveal → rate 0–5 (6 buttons) | Classic Anki flow; preserves SM-2's full granularity for accurate scheduling. |
| Timezone | UTC boundary, no conversion | Zero complexity; acceptable at MVP scale where all users are likely in one timezone. |
| New card due date | Immediately due (NextReviewDate = CreatedAt) | Matches student workflow: create cards → start studying right away. |
| Failed cards in session | No re-queue; each card appears once | Predictable session length; failed cards reappear tomorrow (interval reset to 1). |
| Interrupted session | Save each review immediately; no resume state | Already-reviewed cards are saved; unreviewed ones remain due for the next session. |
| Edit resets SM-2? | No — editing Q/A leaves scheduling untouched | Avoids punishing typo fixes; matches Anki behavior. |

## Scope

**In scope:**
- SM-2 fields on Flashcard entity + migration with data backfill
- SM-2 calculation service (pure, stateless)
- API: GET due cards, POST review grade
- /study page with card-by-card session flow
- "Ucz się" nav link (first position)
- Empty state when no cards are due
- Session summary after last card

**Out of scope:**
- Review history/log table
- Re-queuing failed cards within a session
- Timezone-aware scheduling
- SM-2 state reset on edit
- Pagination on due cards
- Custom SM-2 parameter tuning

## Architecture / Approach

Additive feature following established patterns. Phase 1 extends the data model and adds a stateless `Sm2Service` plus two API endpoints (`GET /due`, `POST /{id}/review`). Phase 2 builds the `/study` page using the same Razor Page + vanilla JS + fetch pattern as existing pages. No new architectural patterns introduced.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Schema + SM-2 + API | DB migration, SM-2 service, due/review endpoints | Migration backfill must correctly set NextReviewDate = CreatedAt for existing rows |
| 2. Study Page + Frontend | /study page, session flow, nav link, styles | Grade button UX (6 options) may feel complex for first-time users |

**Prerequisites:** F-01 (schema), F-02 (auth), and flashcards in the database to review (S-01 or S-02 provides these)
**Estimated effort:** ~1–2 sessions across 2 phases

## Open Risks & Assumptions

- SM-2 formula constants are canonical — if a non-standard variant is needed later, the service is isolated and easy to swap.
- UTC-only scheduling may confuse users in extreme timezones (e.g., UTC+12), but at MVP scale this is acceptable.
- No review history means we cannot retroactively analyze learning patterns — acceptable for MVP, can be added later without schema change (append-only log table).

## Success Criteria (Summary)

- User can complete a full review session: see due cards, reveal answers, rate recall, and get a summary
- SM-2 algorithm correctly schedules next review dates (verified via API: grade 5 → longer interval, grade 0 → next day)
- New flashcards are immediately available for review upon creation
