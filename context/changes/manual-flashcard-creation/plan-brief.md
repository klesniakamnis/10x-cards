# Manual Flashcard Creation — Plan Brief

> Full plan: `context/changes/manual-flashcard-creation/plan.md`

## What & Why

Add a page where users can manually create flashcards by typing a question and answer, without using the AI generation flow. FR-009 requires this as a "safety net" path — users should be able to add flashcards directly when they have specific Q/A in mind.

## Starting Point

The backend is already done: `POST /api/flashcards` (built in S-01) accepts any `FlashcardSource` including `Manual`. The Razor Pages infrastructure, layout, CSS system, and auth are all in place. This slice adds only frontend: one new page + one header nav link.

## Desired End State

An authenticated user clicks "Dodaj fiszkę" in the header, fills in a question and answer, clicks "Dodaj", sees a success banner, and the form clears for the next entry. The created flashcard has `Source = Manual` in the database.

## Key Decisions Made

| Decision              | Choice                              | Why (1 sentence)                                                  |
| --------------------- | ----------------------------------- | ----------------------------------------------------------------- |
| Page location         | Dedicated `/create` page            | Works without S-03 deck view; avoids cluttering the Generate page |
| Post-creation UX      | Success message + clear form        | Supports batch entry; no deck view to redirect to yet             |
| Navigation            | "Dodaj fiszkę" link in header       | Discoverable from any page; matches US-02 expectation             |
| Validation            | Client-side + server-side           | Instant feedback; matches Generate page pattern                   |

## Scope

**In scope:** Create page with Q/A form, client-side validation, API call to existing endpoint, success/error feedback, header nav link.

**Out of scope:** Deck list view (S-03), character counter, list of recently created cards, redirect after creation.

## Architecture / Approach

Thin UI layer: one Razor Page + one JS file calling the existing `POST /api/flashcards` endpoint. No backend changes. Follows the same fetch/validation/feedback pattern established in `generate.js`.

## Phases at a Glance

| Phase                         | What it delivers                                   | Key risk       |
| ----------------------------- | -------------------------------------------------- | -------------- |
| 1. Create Page + Navigation   | `/create` page, form, JS, header link, styles      | None — trivial |

**Prerequisites:** S-01 completed (provides the API endpoint and Razor Pages infrastructure).
**Estimated effort:** ~1 session, single phase.

## Open Risks & Assumptions

- When S-03 adds the deck view, the "Dodaj fiszkę" action will also need to be accessible from there — the `/create` page may become a redirect target or be inlined. This is acceptable; S-03 owns that decision.

## Success Criteria (Summary)

- User can create a manual flashcard from `/create` with `Source = Manual`
- Form validates, shows feedback, and clears for batch entry
- Page is accessible via header nav link from any page
