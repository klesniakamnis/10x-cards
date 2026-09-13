# Spaced Repetition Review Session — Implementation Plan

## Overview

Add spaced repetition review sessions using the SM-2 algorithm (scale 0–5) to 10xCards. An authenticated user clicks "Ucz się" to review flashcards whose next review date has arrived. Each card is presented as: see question → click to reveal answer → rate recall quality 0–5. The SM-2 algorithm updates the card's scheduling state (easiness factor, interval, repetitions) and sets the next review date. Each review is saved immediately — no session state, no data loss on interruption. This fulfills FR-013, FR-014, and FR-015 from the PRD.

## Current State Analysis

The `Flashcard` entity (`Data/Flashcard.cs`) has content fields (Question, Answer, Source) and timestamps (CreatedAt, UpdatedAt via IHasTimestamps) but no spaced repetition fields. The roadmap and data-schema-setup plan explicitly deferred SM-2 fields (interval, easiness_factor, repetitions, next_review_date) to this slice.

The API layer (`Endpoints/FlashcardEndpoints.cs`) has CRUD endpoints (POST, GET, PUT, DELETE) on a `/api/flashcards` group. All endpoints filter by authenticated user's ID from `ClaimTypes.NameIdentifier`. The frontend follows the Razor Page + vanilla JS + fetch pattern (`.cshtml` + `.cshtml.cs` + `/js/<page>.js`).

`ApplicationDbContext.SetTimestamps()` auto-sets CreatedAt/UpdatedAt for IHasTimestamps entities on SaveChanges. All timestamps use `DateTime.UtcNow`.

No SM-2 code, review endpoints, or study page exist in the codebase.

## Desired End State

An authenticated user navigates to `/study` (via "Ucz się" link in the header). The system fetches all flashcards whose `NextReviewDate <= DateTime.UtcNow`. If none are due, a friendly empty state with a link to create/generate cards is shown. Otherwise, cards are presented one at a time: the question is displayed, the user clicks "Pokaż odpowiedź" to reveal the answer, then rates recall quality on a 0–5 scale. Each rating is submitted immediately to the API, which applies the SM-2 formula and updates the card's scheduling state. After the last card, a summary screen shows how many cards were reviewed. New flashcards (AI-generated or manual) are immediately due for review upon creation.

### Key Discoveries:

- `FlashcardEndpoints.cs:9-11` — existing `/api/flashcards` group; new review endpoints slot into the same class.
- `ApplicationDbContext.cs:56-68` — `SetTimestamps()` runs on SaveChanges; can be extended to initialize `NextReviewDate` for new Flashcard entities.
- `Data/Flashcard.cs` — entity implements `IHasTimestamps`; SM-2 fields are a straightforward addition.
- `Pages/Shared/_Layout.cshtml:16-17` — nav links for authenticated users; "Ucz się" will be added as the first link.
- `deck-management/plan-brief.md:51` — deferred decision: editing a flashcard does NOT reset SM-2 state (decided in this planning session).
- No .NET SM-2 library exists with meaningful adoption; hand-rolling the standard formula (~20 lines) is the pragmatic choice.

## What We're NOT Doing

- No re-queuing failed cards within a session — each card appears exactly once per session; failed cards (grade < 3) reappear in the next session (interval reset to 1 day).
- No review history/log table — only the latest SM-2 state is stored on the Flashcard; no append-only log of individual reviews.
- No timezone-aware scheduling — "due today" is determined by UTC date boundary (`NextReviewDate <= DateTime.UtcNow`), no user timezone storage or conversion.
- No SM-2 state reset on flashcard edit — editing question/answer leaves scheduling state untouched.
- No custom SM-2 parameter tuning — standard SM-2 formula with canonical defaults (EF=2.5, min EF=1.3).
- No session resume/persistence — if the browser closes mid-session, already-reviewed cards are saved; remaining due cards show up in the next session.
- No pagination on due cards query — all due cards fetched in one request (acceptable at MVP scale).

## Critical Implementation Details

### SM-2 algorithm specification

The implementer must use the standard SM-2 formula exactly. After a review with grade `q` (0–5):

```
if q >= 3:
    if repetitions == 0: interval = 1
    elif repetitions == 1: interval = 6
    else: interval = round(interval * EF)
    repetitions += 1
else:
    repetitions = 0
    interval = 1

EF = EF + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02))
EF = max(EF, 1.3)

next_review_date = now + interval days
```

This is the only section where a code snippet is warranted — the formula is the contract other phases depend on, and a wrong constant would silently corrupt scheduling.

---

## Phase 1: Schema Extension + SM-2 Algorithm + API

### Overview

Extend the Flashcard data model with SM-2 scheduling fields, create the SM-2 calculation service, add API endpoints for fetching due cards and submitting reviews, and ensure new flashcards are initialized with correct SM-2 defaults.

### Changes Required:

#### 1. Extend Flashcard entity with SM-2 fields

**File**: `Data/Flashcard.cs`

**Intent**: Add the four SM-2 state fields that the scheduling algorithm reads and writes after each review.

**Contract**: Four new properties on the `Flashcard` class: `EasinessFactor` (double, default 2.5), `Interval` (int, default 0, unit: days), `Repetitions` (int, default 0), `NextReviewDate` (DateTime). All non-nullable.

#### 2. Initialize NextReviewDate for new flashcards

**File**: `Data/ApplicationDbContext.cs`

**Intent**: Ensure that when a new Flashcard is added, its `NextReviewDate` is set to `CreatedAt` (i.e., immediately due). This parallels how `SetTimestamps()` already initializes `CreatedAt` for new entities.

**Contract**: In `SetTimestamps()`, for entities that are both `Added` state and of type `Flashcard`, also set `NextReviewDate = now`. This guarantees `NextReviewDate == CreatedAt` exactly, without requiring every caller (POST endpoint, generation endpoint) to set it manually.

#### 3. Configure SM-2 columns in EF Core model

**File**: `Data/ApplicationDbContext.cs`

**Intent**: Add EF Core configuration for the new Flashcard columns — precision for the double, default values for the migration.

**Contract**: In the `OnModelCreating` Flashcard configuration block, configure `EasinessFactor` with appropriate precision and default value (2.5), `Interval` default (0), `Repetitions` default (0).

#### 4. Create EF Core migration with data backfill

**File**: `Migrations/<timestamp>_AddSm2Fields.cs` (new, generated)

**Intent**: Add the four SM-2 columns to the Flashcards table and backfill existing rows with initial SM-2 values so they become immediately due for review.

**Contract**: After the schema change, run a SQL data migration: `UPDATE Flashcards SET NextReviewDate = CreatedAt WHERE NextReviewDate = '0001-01-01 00:00:00'` (or equivalent) to set existing flashcards as immediately due. EasinessFactor, Interval, and Repetitions get their column defaults (2.5, 0, 0).

#### 5. Create SM-2 service

**File**: `Services/Sm2Service.cs` (new)

**Intent**: Encapsulate the SM-2 algorithm as a pure, stateless calculation that takes current SM-2 state + grade and returns the new state. No database access — the caller reads and writes.

**Contract**: A static class with a single method: `static Sm2Result Calculate(double easinessFactor, int interval, int repetitions, int grade)`. Returns a record `Sm2Result(double EasinessFactor, int Interval, int Repetitions, DateTime NextReviewDate)` containing the updated scheduling state. Grade must be 0–5; throw `ArgumentOutOfRangeException` otherwise.

#### 6. Add review API endpoints

**File**: `Endpoints/FlashcardEndpoints.cs`

**Intent**: Add two endpoints to the existing flashcard API group: one to fetch flashcards due for review, and one to submit a review grade for a specific flashcard.

**Contract**:
- `GET /api/flashcards/due` — Returns `List<FlashcardResponse>` where `NextReviewDate <= DateTime.UtcNow` and `UserId == authenticated user`, ordered by `NextReviewDate` ascending (oldest due first). Returns empty list if none are due.
- `POST /api/flashcards/{id}/review` — Accepts `ReviewRequest(int Grade)`. Validates grade is 0–5, returns 400 if not. Finds the flashcard by ID and user, returns 404 if not found or wrong user. Applies `Sm2Service.Calculate()` with current state and grade, updates the flashcard's SM-2 fields, saves, returns 200 with the updated `FlashcardResponse`.
- New DTO: `ReviewRequest(int Grade)`.

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Migration applies cleanly: `dotnet ef database update`
- Application starts without errors: `dotnet run`

#### Manual Verification:

- GET `/api/flashcards/due` returns cards where NextReviewDate <= now
- GET `/api/flashcards/due` returns empty list when no cards are due
- POST `/api/flashcards/{id}/review` with grade 4 updates EF, interval, repetitions, next_review_date
- POST `/api/flashcards/{id}/review` with grade 1 resets interval to 1, repetitions to 0
- POST `/api/flashcards/{id}/review` with grade 6 returns 400
- POST `/api/flashcards/{id}/review` with wrong user's card returns 404
- Newly created flashcard (POST /api/flashcards) has NextReviewDate == CreatedAt
- Existing flashcards (after migration) have NextReviewDate == CreatedAt

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Study Page + Frontend

### Overview

Create the `/study` Razor Page with a card-by-card review session flow: fetch due cards, present question, reveal answer on click, rate recall 0–5, submit review, advance to next card, and show a summary when done. Add "Ucz się" nav link and study page styles.

### Changes Required:

#### 1. Create the Study Razor Page

**File**: `Pages/Study.cshtml` (new)

**Intent**: The study session page — renders containers for the card display, grade buttons, empty state, and session summary. Cards and interaction are driven client-side by `study.js`.

**Contract**: Razor Page at route `/study`. Contains: a `<div id="study-empty">` for the empty state (hidden by default), a `<div id="study-card">` for the current card (question area, answer area hidden by default, reveal button, grade buttons hidden by default), a `<div id="study-summary">` for the session-complete screen (hidden by default), a `<div id="study-error">` for errors. Progress indicator showing current card number out of total. Script reference to `/js/study.js` in the Scripts section.

**File**: `Pages/Study.cshtml.cs` (new)

**Intent**: Minimal page model — authentication enforced by fallback policy.

**Contract**: `StudyModel : PageModel` with empty `OnGet()`.

#### 2. Create client-side study session logic

**File**: `wwwroot/js/study.js` (new)

**Intent**: Fetch due cards and drive the review session flow entirely client-side: show question, reveal answer, collect grade, submit review, advance.

**Contract**: Key behaviors:
- **On load**: Fetch `GET /api/flashcards/due`. If empty, show `#study-empty` with message and links to `/create` and `/` (generate). If cards exist, store the array, show the first card.
- **Card display**: Show progress ("Fiszka 1 z N"), display question text. Answer area and grade buttons are hidden.
- **Reveal**: Click "Pokaż odpowiedź" button → show answer text and grade buttons (0–5), hide the reveal button.
- **Grade submission**: Click a grade button (0–5) → POST `/api/flashcards/{id}/review` with the grade. On success, advance to next card. On error, show inline error, re-enable grade buttons.
- **Session complete**: After the last card is reviewed, show `#study-summary` with the count of cards reviewed and a "Wróć do talii" link to `/deck`.
- **Grade button labels** (Polish): 0 = "Nie wiem", 1 = "Źle", 2 = "Prawie", 3 = "Trudno", 4 = "Dobrze", 5 = "Idealnie".

#### 3. Add navigation link in header

**File**: `Pages/Shared/_Layout.cshtml`

**Intent**: Add "Ucz się" as the first nav link for authenticated users, before "Moja talia".

**Contract**: Inside the authenticated user block, add `<a href="/study" class="nav-link">Ucz się</a>` as the first child before the existing "Moja talia" link. Final nav order: Ucz się → Moja talia → Dodaj fiszkę → email → Wyloguj.

#### 4. Add study page styles

**File**: `wwwroot/css/site.css`

**Intent**: Styles for the study page: card display area, reveal button, grade button strip, progress indicator, empty state, and session summary.

**Contract**: Styles for: `.study-page` layout (centered, max-width), `#study-card` card container (consistent with `.deck-card` sizing), `.study-question` and `.study-answer` text sections, `.study-reveal-btn` prominent reveal button, `.grade-buttons` horizontal button strip with 6 buttons (color-coded: red for 0–2, yellow for 3, green for 4–5), `.study-progress` progress text, `#study-empty` and `#study-summary` centered content blocks.

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Application starts without errors: `dotnet run`

#### Manual Verification:

- Authenticated user sees "Ucz się" link in header as first nav item
- Clicking "Ucz się" navigates to /study
- With due cards: study page shows first card's question with progress indicator
- Clicking "Pokaż odpowiedź" reveals answer and grade buttons
- Clicking a grade button submits the review and advances to next card
- After last card, summary screen shows count of reviewed cards
- Without due cards: study page shows empty state with links to create/generate
- After reviewing all due cards, starting another session shows empty state
- Unauthenticated user navigating to /study is redirected to /LoginRequired
- Grade 0–2 cards reappear as due the next day (verify via API after reviewing)

---

## Testing Strategy

### Unit Tests:

- SM-2 service: verify grade >= 3 increments repetitions and calculates correct intervals (1, 6, 6*EF, ...)
- SM-2 service: verify grade < 3 resets repetitions to 0 and interval to 1
- SM-2 service: verify EF never drops below 1.3
- SM-2 service: verify grade outside 0–5 throws ArgumentOutOfRangeException

### Integration Tests:

- GET `/api/flashcards/due` returns only the authenticated user's due cards (not other users')
- POST `/api/flashcards/{id}/review` rejects access to another user's card
- POST `/api/flashcards/{id}/review` correctly updates SM-2 fields in database

### Manual Testing Steps:

1. Log in via magic link
2. Create a few flashcards (both AI-generated and manual)
3. Navigate to /study — verify all new cards appear (they're immediately due)
4. Review the first card: see question → reveal answer → rate with grade 5
5. Verify card advances, progress indicator updates
6. Review remaining cards with mixed grades (0, 3, 5)
7. Verify summary screen shows correct count
8. Start another session — verify no cards are due (all just reviewed)
9. Navigate to /deck — verify cards still exist (review doesn't delete them)
10. Check via API that grade-0 cards have NextReviewDate = tomorrow, grade-5 cards have NextReviewDate further out

## Performance Considerations

No pagination on due cards query — all due cards loaded in one fetch. Acceptable for MVP scale (< 100 cards per user). The query `WHERE NextReviewDate <= @now AND UserId = @userId` benefits from an index on `(UserId, NextReviewDate)` if needed, but at MVP scale the existing UserId FK index suffices.

## Migration Notes

The migration adds 4 non-nullable columns with defaults to the Flashcards table. Existing rows get EasinessFactor=2.5, Interval=0, Repetitions=0 from column defaults. A data migration SQL statement sets NextReviewDate=CreatedAt for all existing rows, making them immediately due for review. This is safe and idempotent.

## References

- PRD requirements: US-03, FR-013 (start session), FR-014 (rate recall), FR-015 (save result + schedule next)
- SM-2 algorithm: original Piotr Wozniak specification (1990), scale 0–5
- Shape-notes decision: `context/foundation/shape-notes.md:40` — SM-2 family confirmed
- Existing endpoints: `Endpoints/FlashcardEndpoints.cs` — CRUD on `/api/flashcards`
- Existing entity: `Data/Flashcard.cs`, `Data/ApplicationDbContext.cs`
- Deck management deferral: `context/changes/deck-management/plan-brief.md:51` — edit does not reset SM-2
- Layout: `Pages/Shared/_Layout.cshtml`
- CSS system: `wwwroot/css/site.css`
- Frontend patterns: `wwwroot/js/deck.js` (fetch + render), `wwwroot/js/create.js` (fetch + validation)

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Schema Extension + SM-2 Algorithm + API

#### Automated

- [x] 1.1 Project builds without errors — e34d32a
- [x] 1.2 Migration applies cleanly — e34d32a
- [x] 1.3 Application starts without errors — e34d32a

#### Manual

- [ ] 1.4 GET /api/flashcards/due returns due cards
- [ ] 1.5 GET /api/flashcards/due returns empty when none due
- [ ] 1.6 POST /api/flashcards/{id}/review with grade 4 updates SM-2 fields correctly
- [ ] 1.7 POST /api/flashcards/{id}/review with grade 1 resets interval and repetitions
- [ ] 1.8 POST /api/flashcards/{id}/review with grade 6 returns 400
- [ ] 1.9 POST /api/flashcards/{id}/review with wrong user returns 404
- [ ] 1.10 New flashcard has NextReviewDate == CreatedAt
- [ ] 1.11 Existing flashcards after migration have NextReviewDate == CreatedAt

### Phase 2: Study Page + Frontend

#### Automated

- [x] 2.1 Project builds without errors
- [x] 2.2 Application starts without errors

#### Manual

- [ ] 2.3 "Ucz się" link visible in header as first nav item
- [ ] 2.4 Study page shows first due card's question with progress indicator
- [ ] 2.5 "Pokaż odpowiedź" reveals answer and grade buttons
- [ ] 2.6 Grade submission advances to next card
- [ ] 2.7 Summary screen shows after last card with correct count
- [ ] 2.8 Empty state shown when no cards are due
- [ ] 2.9 After reviewing all cards, new session shows empty state
- [ ] 2.10 Unauthenticated user redirected to /LoginRequired
- [ ] 2.11 Grade 0–2 cards reappear as due next day
