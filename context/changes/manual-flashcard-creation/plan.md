# Manual Flashcard Creation — Implementation Plan

## Overview

Add a dedicated page at `/create` where an authenticated user can manually create a flashcard by entering a question and an answer. The page calls the existing `POST /api/flashcards` endpoint (built in S-01) with `Source: "Manual"`. After creation, the form clears and shows a success message so the user can add another card. A navigation link in the header makes the page accessible from anywhere.

## Current State Analysis

The backend for creating flashcards is already complete:
- `POST /api/flashcards` endpoint in `Endpoints/FlashcardEndpoints.cs` accepts `CreateFlashcardRequest(Question, Answer, Source)` with server-side validation (non-empty, ≤5000 chars, valid `FlashcardSource` enum).
- `FlashcardSource.Manual` exists in `Data/FlashcardSource.cs`.
- Razor Pages infrastructure, layout shell, CSS design system, and auth are all in place from S-01 Phase 1.
- The Generate page (`Pages/Generate.cshtml` + `wwwroot/js/generate.js`) establishes the frontend patterns: fetch-based API calls, inline error/success messages, client-side validation.

## Desired End State

An authenticated user clicks "Dodaj fiszkę" in the header nav, lands on `/create`, fills in question and answer textareas, clicks "Dodaj", and sees a success message confirming the flashcard was saved. The form clears for the next entry. Validation prevents submission of empty or over-length fields both client-side (instant feedback) and server-side (safety net).

### Key Discoveries:

- `POST /api/flashcards` is already reusable — no backend changes needed for S-02.
- The layout header (`Pages/Shared/_Layout.cshtml:13-19`) already conditionally renders user info when authenticated — the nav link slots in naturally here.
- CSS variables and button styles from `wwwroot/css/site.css` cover everything the form needs; only minimal additions for the create page layout.

## What We're NOT Doing

- No deck list view — that's S-03.
- No list of recently created cards on the create page — keep the form minimal.
- No character counter on the create form — the 5000-char limit is generous enough that a counter is unnecessary (unlike the 10k-char generate textarea).
- No redirect after creation — stay on the form for batch entry.

## Phase 1: Create Page + Navigation

### Overview

Add the `/create` Razor Page with a Q/A form, client-side JavaScript for validation and API interaction, styles for the form, and a nav link in the layout header.

### Changes Required:

#### 1. Create the Razor Page

**File**: `Pages/Create.cshtml` (new)

**Intent**: The manual flashcard creation page — two textareas (question and answer), a submit button, and containers for success/error messages.

**Contract**: Razor Page at route `/create`. Contains: a `<textarea>` with `id="question"` and `maxlength="5000"`, a `<textarea>` with `id="answer"` and `maxlength="5000"`, a submit `<button>` with `id="create-btn"`, a `<div id="create-error">` for inline errors, and a `<div id="create-success">` for the success banner. Script reference to `/js/create.js` in the Scripts section.

**File**: `Pages/Create.cshtml.cs` (new)

**Intent**: Minimal page model — authentication enforced by fallback policy (no `[AllowAnonymous]`), no server-side logic needed.

**Contract**: `CreateModel : PageModel` with empty `OnGet()`.

#### 2. Create client-side logic

**File**: `wwwroot/js/create.js` (new)

**Intent**: Handle form submission — validate fields client-side, POST to `/api/flashcards` with `Source: "Manual"`, show success/error feedback, clear the form on success.

**Contract**: Key behaviors:
- **Submit button click**: Read both textareas. Validate: both non-empty after trim, both ≤5000 chars. Show inline error if validation fails. POST to `/api/flashcards` with `{ question, answer, source: "Manual" }`. During request: disable button and textareas.
- **On 201 response**: Show success message ("Fiszka dodana!"), clear both textareas, re-enable form, focus the question textarea for the next entry.
- **On error response (4xx/5xx)**: Show the error message from the response, re-enable form, keep field values for correction.

#### 3. Add navigation link in header

**File**: `Pages/Shared/_Layout.cshtml`

**Intent**: Add a "Dodaj fiszkę" link visible to authenticated users so the create page is discoverable from any page.

**Contract**: Inside the authenticated user block, add an `<a href="/create">` link styled consistently with the header. Positioned before the user email/logout cluster.

#### 4. Add create page styles

**File**: `wwwroot/css/site.css`

**Intent**: Minimal styles for the create page form layout.

**Contract**: Styles for: `.create-page` layout (form with labeled textareas, consistent with `.generate-page` spacing), `.create-success` (reuses `.summary-message` pattern), nav link styling in the header (`.nav-link` or inline styles consistent with existing header elements).

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Application starts without errors: `dotnet run`

#### Manual Verification:

- Authenticated user sees "Dodaj fiszkę" link in header on all pages
- Clicking the link navigates to `/create`
- Submitting valid Q/A creates a flashcard in the database with `Source = Manual`
- Success message appears after creation
- Form clears after successful creation, question textarea has focus
- Empty fields show validation error without calling the API
- Fields exceeding 5000 chars show validation error
- Unauthenticated user navigating to `/create` is redirected to `/LoginRequired`

---

## Testing Strategy

### Unit Tests:

- No new unit tests — the endpoint is already tested via S-01. The new code is a thin UI layer calling the existing API.

### Integration Tests:

- POST `/api/flashcards` with `Source: "Manual"` creates a record with the correct source (verified with curl + DB inspection — this should already work from S-01).

### Manual Testing Steps:

1. Log in via magic link
2. Click "Dodaj fiszkę" in the header
3. Enter a question and answer, click "Dodaj"
4. Verify the success message appears and form clears
5. Verify the flashcard exists in the database with `Source = Manual`
6. Try submitting with empty question — validation error, no API call
7. Try submitting with empty answer — validation error, no API call
8. Add 3 flashcards in a row — each creates a record, form resets each time

## Performance Considerations

None — single POST to an existing endpoint creating one DB row. No performance concerns at MVP scale.

## Migration Notes

No migration needed. The `Flashcard` entity and `FlashcardSource.Manual` enum value already exist.

## References

- PRD requirement: FR-009 (manual flashcard creation)
- User story: US-02 (student creates flashcard manually)
- Existing endpoint: `Endpoints/FlashcardEndpoints.cs` — `POST /api/flashcards`
- Existing entity: `Data/Flashcard.cs`, `Data/FlashcardSource.cs`
- Layout: `Pages/Shared/_Layout.cshtml`
- CSS system: `wwwroot/css/site.css`
- Frontend pattern: `wwwroot/js/generate.js` — fetch + validation + feedback pattern

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Create Page + Navigation

#### Automated

- [x] 1.1 Project builds without errors
- [x] 1.2 Application starts without errors

#### Manual

- [x] 1.3 Authenticated user sees "Dodaj fiszkę" link in header
- [x] 1.4 Clicking link navigates to /create
- [x] 1.5 Valid Q/A creates flashcard with Source = Manual
- [x] 1.6 Success message appears and form clears after creation
- [x] 1.7 Empty fields show validation error
- [x] 1.8 Unauthenticated user redirected to /LoginRequired
