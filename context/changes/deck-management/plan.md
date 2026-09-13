# Deck Management — Implementation Plan

## Overview

Add a deck management page at `/deck` where authenticated users can browse all their flashcards, edit a flashcard's question and answer inline, and delete a flashcard with browser confirmation. This fulfills FR-010 (browse), FR-011 (edit), and FR-012 (delete with confirmation) from the PRD.

## Current State Analysis

The flashcard data model is complete — `Flashcard` entity with Question, Answer, Source, UserId, CreatedAt, UpdatedAt exists and is persisted via EF Core + SQLite. The only API endpoint is `POST /api/flashcards` for creation (built in S-01). There is no way to list, update, or delete flashcards via the API, and no UI for browsing existing cards.

The frontend follows a Razor Page + vanilla JS pattern: each page has a `.cshtml` view, a minimal `.cshtml.cs` page model, and a `/js/<page>.js` file that handles all interactions via fetch calls to the API. CSS lives in `wwwroot/css/site.css` using a design-token system with CSS variables.

## Desired End State

An authenticated user clicks "Moja talia" in the header nav, sees all their flashcards as cards ordered by newest first, can click "Edytuj" to inline-edit question/answer, and can click "Usuń" to delete with browser `confirm()` dialog. The deck page shows an empty state message when no flashcards exist.

### Key Discoveries:

- `FlashcardEndpoints.cs:9-11` already creates a `/api/flashcards` group — new endpoints slot into the same class and group.
- `FlashcardResponse` record at `FlashcardEndpoints.cs:55` returns `Id, Question, Answer, Source, CreatedAt` — reusable for the list response. Need to add `UpdatedAt` for the edit flow.
- `ApplicationDbContext` auto-manages `UpdatedAt` via `SetTimestamps()` override — edits get timestamped automatically.
- The inline-edit pattern from `generate.js` (text ↔ textarea swap) is directly reusable for deck card editing.
- User's flashcards are filtered by `UserId` from `ClaimTypes.NameIdentifier` claim — same auth pattern as the create endpoint.

## What We're NOT Doing

- No pagination — MVP deck loads all cards in one fetch, ordered by newest first. Pagination is deferred until deck sizes warrant it.
- No search or filtering — PRD explicitly excluded this from MVP scope.
- No categories or tags — PRD Non-Goals.
- No editing of the Source field — it's provenance metadata, not user content.
- No soft delete or undo — PRD specifies hard delete with confirmation as sufficient.
- No SM-2 state considerations on edit — deferred to S-04 per roadmap risk note.

## Phase 1: Backend Endpoints

### Overview

Add three API endpoints to the existing `FlashcardEndpoints` class: GET (list all user's flashcards), PUT (update question/answer), and DELETE (hard delete). All endpoints are scoped to the authenticated user's flashcards.

### Changes Required:

#### 1. Extend flashcard API endpoints

**File**: `Endpoints/FlashcardEndpoints.cs`

**Intent**: Add GET `/api/flashcards` to list all flashcards for the authenticated user (ordered by CreatedAt descending), PUT `/api/flashcards/{id}` to update a flashcard's question and answer, and DELETE `/api/flashcards/{id}` to hard-delete a flashcard. All three endpoints must verify the flashcard belongs to the requesting user.

**Contract**:
- `GET /api/flashcards` — Returns `List<FlashcardResponse>` ordered by `CreatedAt` descending. No pagination parameters.
- `PUT /api/flashcards/{id}` — Accepts `UpdateFlashcardRequest(string Question, string Answer)`. Same validation as POST (non-empty, ≤5000 chars). Returns 404 if not found or wrong user. Returns 200 with updated `FlashcardResponse`.
- `DELETE /api/flashcards/{id}` — Returns 404 if not found or wrong user. Returns 204 on success.
- New DTO: `UpdateFlashcardRequest(string Question, string Answer)`.
- Extend `FlashcardResponse` to include `UpdatedAt` field (add `DateTime UpdatedAt` to the record).

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Application starts without errors: `dotnet run`

#### Manual Verification:

- GET `/api/flashcards` returns all user's cards ordered by newest first
- PUT `/api/flashcards/{id}` updates question/answer and returns updated card
- PUT with empty question returns 400
- PUT with wrong user's card ID returns 404
- DELETE `/api/flashcards/{id}` removes the card, returns 204
- DELETE with wrong user's card ID returns 404

---

## Phase 2: Deck Page + Frontend

### Overview

Create the `/deck` Razor Page with a card grid showing all flashcards, inline editing, delete with browser confirmation, empty state, and a "Moja talia" nav link in the header.

### Changes Required:

#### 1. Create the Deck Razor Page

**File**: `Pages/Deck.cshtml` (new)

**Intent**: The deck management page — renders a container for the card grid, an empty state message, and an error container. Cards are rendered client-side by `deck.js`.

**Contract**: Razor Page at route `/deck`. Contains: a `<div id="deck-container">` for the card grid, a `<div id="deck-empty">` for the empty state message (hidden by default), a `<div id="deck-error">` for errors. Script reference to `/js/deck.js` in the Scripts section.

**File**: `Pages/Deck.cshtml.cs` (new)

**Intent**: Minimal page model — authentication enforced by fallback policy.

**Contract**: `DeckModel : PageModel` with empty `OnGet()`.

#### 2. Create client-side deck logic

**File**: `wwwroot/js/deck.js` (new)

**Intent**: Fetch and render the user's flashcard deck as a card grid with inline edit and delete functionality.

**Contract**: Key behaviors:
- **On load**: Fetch `GET /api/flashcards`. Render each card showing: question text, answer text, source badge (AI/Manual), creation date, and action buttons (Edytuj, Usuń). If the list is empty, show the empty state message.
- **Edit flow**: Click "Edytuj" → question and answer text swap to textareas pre-filled with current values, action buttons change to "Zapisz" and "Anuluj". "Zapisz" → PUT `/api/flashcards/{id}` with new values, validate client-side first (non-empty, ≤5000 chars). On success, swap back to text display with updated content. On error, show inline error. "Anuluj" → revert to original text, no API call.
- **Delete flow**: Click "Usuń" → `confirm('Czy na pewno chcesz usunąć tę fiszkę?')`. If confirmed, DELETE `/api/flashcards/{id}`. On success, fade out and remove the card. If the deck becomes empty, show empty state.
- **Error handling**: Network errors show a top-level error message. Per-card errors (edit failures) show inline on the card.

#### 3. Add navigation link in header

**File**: `Pages/Shared/_Layout.cshtml`

**Intent**: Add a "Moja talia" nav link before the existing "Dodaj fiszkę" link, visible to authenticated users.

**Contract**: Inside the authenticated user block, add `<a href="/deck" class="nav-link">Moja talia</a>` before the existing "Dodaj fiszkę" link.

#### 4. Add deck page styles

**File**: `wwwroot/css/site.css`

**Intent**: Styles for the deck page card grid, flashcard cards, source badges, inline editing state, and empty state.

**Contract**: Styles for: `.deck-page` layout, `.deck-grid` card container, `.deck-card` individual card (consistent with `.proposal-card` sizing/spacing), `.source-badge` (small pill showing AI/Manual), `.deck-empty` empty state message, and inline edit state (textarea swap matching the `.edit-textarea` pattern from the generate page).

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Application starts without errors: `dotnet run`

#### Manual Verification:

- Authenticated user sees "Moja talia" link in header before "Dodaj fiszkę"
- Clicking "Moja talia" navigates to /deck
- Deck page shows all user's flashcards as cards
- Cards show question, answer, source (AI/Manual), and creation date
- Cards are ordered by newest first
- Clicking "Edytuj" swaps text to editable textareas
- Saving an edit updates the card and returns to display mode
- Cancelling an edit reverts without API call
- Edit validation prevents empty fields
- Clicking "Usuń" shows browser confirm dialog
- Confirming delete removes the card with fade-out
- Empty deck shows a friendly empty state message
- Unauthenticated user navigating to /deck is redirected to /LoginRequired

---

## Testing Strategy

### Unit Tests:

- No new unit tests — endpoints follow the same pattern as the existing POST. Testing is via manual API verification and integration testing.

### Integration Tests:

- GET `/api/flashcards` returns only the authenticated user's cards (not other users')
- PUT `/api/flashcards/{id}` rejects access to another user's card
- DELETE `/api/flashcards/{id}` rejects access to another user's card

### Manual Testing Steps:

1. Log in via magic link
2. Navigate to /deck — verify empty state if no cards exist
3. Create a few flashcards (both AI-generated and manual)
4. Navigate to /deck — verify all cards appear, newest first
5. Click "Edytuj" on a card — verify textareas appear with current content
6. Edit question and answer, click "Zapisz" — verify card updates
7. Click "Edytuj" then "Anuluj" — verify no changes
8. Try to save with empty question — verify validation error
9. Click "Usuń" — verify confirm dialog appears
10. Confirm deletion — verify card removed with animation
11. Delete all cards — verify empty state reappears

## Performance Considerations

No pagination at MVP — all cards loaded in one fetch. Acceptable for MVP scale (< 100 cards per user). If deck sizes grow past this, S-04 or a follow-up slice should add cursor-based pagination.

## Migration Notes

No migration needed. The Flashcard entity and all required fields already exist.

## References

- PRD requirements: FR-010 (browse), FR-011 (edit), FR-012 (delete)
- NFR: Delete requires explicit user confirmation
- Existing endpoint: `Endpoints/FlashcardEndpoints.cs` — `POST /api/flashcards`
- Existing entity: `Data/Flashcard.cs`, `Data/FlashcardSource.cs`
- Layout: `Pages/Shared/_Layout.cshtml`
- CSS system: `wwwroot/css/site.css`
- Frontend patterns: `wwwroot/js/generate.js` (inline edit), `wwwroot/js/create.js` (fetch + validation)

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Backend Endpoints

#### Automated

- [x] 1.1 Project builds without errors — d5ee3be
- [x] 1.2 Application starts without errors — d5ee3be

#### Manual

- [x] 1.3 GET /api/flashcards returns user's cards ordered by newest first — d5ee3be
- [x] 1.4 PUT /api/flashcards/{id} updates question/answer — d5ee3be
- [x] 1.5 PUT with empty question returns 400 — d5ee3be
- [x] 1.6 PUT with wrong user's card returns 404 — d5ee3be
- [x] 1.7 DELETE /api/flashcards/{id} removes card, returns 204 — d5ee3be
- [x] 1.8 DELETE with wrong user's card returns 404 — d5ee3be

### Phase 2: Deck Page + Frontend

#### Automated

- [x] 2.1 Project builds without errors
- [x] 2.2 Application starts without errors

#### Manual

- [x] 2.3 "Moja talia" link visible in header before "Dodaj fiszkę"
- [x] 2.4 Deck page shows all user's flashcards as cards
- [x] 2.5 Cards ordered by newest first with question, answer, source, date
- [x] 2.6 Inline edit works: textarea swap, save updates card, cancel reverts
- [x] 2.7 Edit validation prevents empty fields
- [x] 2.8 Delete shows confirm dialog, confirmed delete removes card with fade-out
- [x] 2.9 Empty deck shows friendly empty state message
- [x] 2.10 Unauthenticated user redirected to /LoginRequired
