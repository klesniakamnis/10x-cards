# Deck Management — Plan Brief

> Full plan: `context/changes/deck-management/plan.md`

## What & Why

Add a deck management page where users can browse, edit, and delete their flashcards. FR-010/011/012 require this as the core CRUD interface — without it, users can create flashcards but never see, fix, or remove them.

## Starting Point

The `Flashcard` entity and database schema are complete (from F-01). `POST /api/flashcards` exists (from S-01) but there are no GET, PUT, or DELETE endpoints. No UI exists for viewing saved flashcards. The Razor Pages + vanilla JS + fetch pattern is established across Generate and Create pages.

## Desired End State

An authenticated user clicks "Moja talia" in the header, sees all their flashcards as cards (newest first), can inline-edit any card's question/answer, and can delete any card with a browser confirmation dialog. Empty decks show a friendly message.

## Key Decisions Made

| Decision              | Choice                              | Why (1 sentence)                                                  |
| --------------------- | ----------------------------------- | ----------------------------------------------------------------- |
| List layout           | Card grid                           | Visual consistency with existing proposal cards from Generate page |
| Edit UX               | Inline editing on the card          | Reuses proven pattern from generate.js; no page navigation needed |
| Delete confirmation   | Browser confirm() dialog            | Zero UI work; meets NFR requirement; universally understood       |
| Pagination            | None at MVP, newest first           | MVP users won't have 100+ cards; avoids premature complexity      |
| Route                 | /deck                               | Clear, noun-based route matching domain language                  |
| Editable fields       | Question and Answer only            | Source is provenance metadata, not user content                   |
| Nav placement         | "Moja talia" before "Dodaj fiszkę"  | Browse-first ordering: check what you have, then add              |

## Scope

**In scope:** Deck list page, card grid display, inline edit (Q/A), hard delete with confirm(), empty state, source badge (AI/Manual), header nav link.

**Out of scope:** Pagination, search/filter, categories/tags, Source editing, soft delete/undo, SM-2 state on edit (S-04's concern).

## Architecture / Approach

Thin CRUD layer: 3 new API endpoints (GET list, PUT update, DELETE) added to the existing `FlashcardEndpoints` class, plus one Razor Page with a JS file calling those endpoints. All endpoints verify flashcard ownership via the authenticated user's ID. No schema changes or migrations needed.

## Phases at a Glance

| Phase                         | What it delivers                                   | Key risk       |
| ----------------------------- | -------------------------------------------------- | -------------- |
| 1. Backend Endpoints          | GET, PUT, DELETE API endpoints for flashcards       | None — standard CRUD |
| 2. Deck Page + Frontend       | /deck page, card grid, inline edit, delete, nav     | None — follows existing patterns |

**Prerequisites:** F-01 (data schema) and F-02 (auth) completed. S-01 provides the endpoint pattern and FlashcardResponse DTO.
**Estimated effort:** ~1-2 sessions, two phases.

## Open Risks & Assumptions

- When S-04 adds spaced repetition, editing a flashcard may need to reset SM-2 state — that decision is explicitly deferred to S-04's planning.
- No pagination means performance degrades if a user accumulates hundreds of cards — acceptable for MVP; revisit if usage data shows large decks.

## Success Criteria (Summary)

- User can browse all their flashcards at /deck, ordered by newest first
- User can inline-edit any flashcard's question and answer
- User can delete a flashcard after confirming via browser dialog
- Empty deck shows a friendly message; page is accessible via header nav
