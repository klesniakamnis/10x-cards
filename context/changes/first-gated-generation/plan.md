# First Gated Generation — Implementation Plan

## Overview

Build the north star slice for 10xCards: a user pastes text, the system generates flashcard proposals via OpenAI, and the user accepts/edits/rejects each proposal in an inline card interface. Accepted proposals become Flashcard entities in the database. This is the first slice that adds a frontend (Razor Pages + vanilla JS) and the first LLM integration (OpenAI via Microsoft.Extensions.AI). Proposals are transient — held in frontend state, not persisted in the database.

## Current State Analysis

The project is an ASP.NET Core 9.0 minimal API with:
- **Data layer**: EF Core 9.0.6 + SQLite with User, Flashcard (Question/Answer/Source/UserId), MagicLinkToken, AuthSession entities. `FlashcardSource.AiGenerated` enum value is ready. Flashcard fields: `Question` [MaxLength(5000)], `Answer` [MaxLength(5000)], `Source` (required, stored as string), `UserId` (FK, cascade delete). All entities implement `IHasTimestamps` for auto-managed UTC timestamps.
- **Auth**: Cookie-based magic link authentication with default-deny fallback policy. `OnRedirectToLogin` overridden to return 401 (API-friendly). `DbSessionStore` (singleton) for server-side session storage.
- **Frontend**: Absent — no Razor Pages, no static files, no wwwroot directory.
- **AI/LLM**: No packages, no integration. `10x-cards.csproj` has three packages: OpenApi, EF Core Design, EF Core SQLite.
- **Existing endpoints**: `/health`, `/db-health` (AllowAnonymous); `/weatherforecast` (auth); `/api/auth/*` (login, callback, logout, me). Pattern: `Endpoints/*.cs` extension method with `MapGroup()`.
- **Services pattern**: `IEmailSender` interface + `ConsoleEmailSender` implementation in same file (Services/), registered as singleton. This is the established convention for swappable service implementations.

## Desired End State

An authenticated user opens the Generate page in a browser, pastes up to 10,000 characters of text (Polish or English), clicks "Generate", sees a loading spinner, then receives a list of AI-generated Q/A proposal cards. For each card, the user can accept (creates a Flashcard in the database), edit inline then accept, or reject (removes from list). After all proposals are handled, a summary shows how many flashcards were added, with an option to generate more.

### Key Discoveries:

- `FlashcardSource.AiGenerated` exists — no schema change or migration needed for S-01
- `Microsoft.Extensions.AI.OpenAI` 10.10.0 is GA and provides `IChatClient` abstraction over the OpenAI SDK 2.13.0
- Auth's `OnRedirectToLogin` returns 401 for all requests — needs content negotiation to redirect browser requests to a login-required page while preserving 401 for API calls
- Creating an AI-generated flashcard is `db.Flashcards.Add(new Flashcard { Question, Answer, Source = FlashcardSource.AiGenerated, UserId })` — the existing entity handles everything
- The `ConsoleEmailSender`/`IEmailSender` pattern (interface + dev stub) maps directly to `IFlashcardGenerator` + `DevFlashcardGenerator`

## What We're NOT Doing

- No streaming/SSE for generation results — loading spinner, then full list
- No proposal persistence in database — proposals are transient frontend state, lost on page close
- No login page or registration UI — user authenticates via console-logged magic link URL
- No landing page — unauthenticated users see a minimal "login required" page
- No deck view or flashcard list — that's S-03 (deck management)
- No bulk accept/reject — PRD explicitly excludes it (Non-Goals)
- No undo for accept or reject
- No prompt iteration or A/B testing — ship a working prompt, iterate based on real usage
- No analytics collection for the 75% acceptance metric — post-MVP measurement
- No pre-validation of text quality before sending to LLM — accept any text, handle zero-results gracefully
- No automatic retry on LLM failures — show error, let user retry manually

## Implementation Approach

Three phases, each independently verifiable. Phase 1 adds the frontend framework and app shell (Razor Pages + static files). Phase 2 adds the AI service behind an `IFlashcardGenerator` interface with a dev stub and the generation API endpoint. Phase 3 wires the full client-side flow and the flashcard creation endpoint, delivering the end-to-end north star experience.

**Cut line**: If S-01 takes longer than expected, inline edit (FR-007) is the first feature to cut. Accept + reject alone validate the "human gate" hypothesis. Edit can be added in S-03 (deck management) where it's also needed.

## Critical Implementation Details

### Auth content negotiation

The current `OnRedirectToLogin` handler (`Program.cs:30-34`) returns HTTP 401 for all unauthenticated requests. With Razor Pages serving browser requests, this must be updated to content-negotiate: if the request's `Accept` header contains `application/json`, return 401 (preserving existing API behavior); otherwise redirect to `/LoginRequired`. This is a behavioral change to existing code — both paths must be verified.

## Phase 1: Razor Pages Infrastructure + App Shell

### Overview

Add Razor Pages to the ASP.NET Core pipeline, create a minimal layout shell, and build the Generate page skeleton. After this phase, the application renders HTML and provides the UI container for the generation flow.

### Changes Required:

#### 1. Add Razor Pages services, static files, and content negotiation

**File**: `Program.cs`

**Intent**: Wire Razor Pages into the DI container and endpoint routing. Add static file serving for CSS/JS. Update the OnRedirectToLogin handler to content-negotiate between API clients (401) and browser requests (redirect).

**Contract**: `builder.Services.AddRazorPages()` added to services. `app.UseStaticFiles()` inserted before `app.UseAuthentication()` in the middleware pipeline. `app.MapRazorPages()` added after existing endpoint mappings. The `OnRedirectToLogin` callback checks `context.Request.Headers.Accept` for `application/json` — if present, sets 401; otherwise redirects to `/LoginRequired`.

#### 2. Create Razor Pages boilerplate

**File**: `Pages/_ViewImports.cshtml` (new)

**Intent**: Standard Razor Pages imports shared across all pages.

**Contract**: `@using _10x_cards` and `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`.

**File**: `Pages/_ViewStart.cshtml` (new)

**Intent**: Set the default layout for all pages.

**Contract**: Sets layout to `_Layout`.

#### 3. Create layout

**File**: `Pages/Shared/_Layout.cshtml` (new)

**Intent**: Minimal app shell — header with app name, authenticated user info with logout, main content area, CSS link.

**Contract**: HTML5 document with charset and viewport meta tags, link to `/css/site.css`. Header contains "10xCards" title. When the user is authenticated: displays their email and a logout form (POST to `/api/auth/logout`). `@RenderBody()` in the main content area. Optional `@RenderSection("Scripts", required: false)` before closing body tag.

#### 4. Create Generate page

**File**: `Pages/Generate.cshtml` (new)

**File**: `Pages/Generate.cshtml.cs` (new)

**Intent**: The main page of the application — textarea for pasting source text, character counter, generate button, and empty containers for proposals and summary (populated by JS in Phase 3).

**Contract**: Razor Page at route `/` (default page, via `@page "/"`). Page model requires authentication (fallback policy applies, no `[AllowAnonymous]`). Contains: a `<textarea>` with `id="source-text"` and `maxlength="10000"`, a character counter `<span>` showing `0 / 10 000`, a generate `<button>` with `id="generate-btn"`, an empty `<div id="proposals-container">`, an empty `<div id="summary-container">`, and an empty `<div id="error-container">`.

#### 5. Create LoginRequired page

**File**: `Pages/LoginRequired.cshtml` (new)

**File**: `Pages/LoginRequired.cshtml.cs` (new)

**Intent**: Friendly page shown to unauthenticated users instead of a raw 401 response.

**Contract**: Razor Page at route `/LoginRequired` with `[AllowAnonymous]` on the page model. Displays a message explaining the user needs to log in via a magic link. In development, includes a note about using the console-logged URL.

#### 6. Create base stylesheet

**File**: `wwwroot/css/site.css` (new)

**Intent**: Minimal CSS for the layout shell, form elements, and card components (card styles prepared for Phase 3 usage).

**Contract**: Styles for: page layout (max-width container, header with flexbox), typography (system font stack), form elements (textarea full-width, styled buttons), card component (`.proposal-card` with bordered sections for Q and A, action button row), loading spinner (`.loading-spinner`), summary message, error message, fade-out transition for card removal. Responsive for desktop viewport (Chrome/Firefox as per NFR).

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Application starts without errors: `dotnet run` (no startup exceptions)

#### Manual Verification:

- Authenticated user navigating to `/` sees the Generate page with layout (header with app name, email, logout; textarea with counter; generate button)
- Unauthenticated user navigating to `/` is redirected to `/LoginRequired` page
- API requests (Accept: application/json) to protected endpoints still receive 401 (not redirect)
- Character counter displays and updates as text is typed in the textarea
- Logout link/button in header works (signs out, redirects appropriately)

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: AI Service + Generation Endpoint

### Overview

Add OpenAI integration behind an `IFlashcardGenerator` interface, with a development stub for fast iteration. Build the POST `/api/generation` endpoint that accepts source text and returns Q/A proposals as JSON.

### Changes Required:

#### 1. Add NuGet packages

**File**: `10x-cards.csproj`

**Intent**: Add Microsoft.Extensions.AI integration packages for OpenAI chat completions.

**Contract**: Two new package references: `Microsoft.Extensions.AI` (10.10.0) and `Microsoft.Extensions.AI.OpenAI` (10.10.0). Use `--source https://api.nuget.org/v3/index.json` flag when running `dotnet add package` (known local NuGet source issue in this environment).

#### 2. Create flashcard generation service interface and DTO

**File**: `Services/IFlashcardGenerator.cs` (new)

**Intent**: Define the contract for flashcard generation. Any implementation (OpenAI, dev stub, future providers) satisfies this interface. Include the proposal DTO in the same file, following the `IEmailSender`/`ConsoleEmailSender` pattern of co-locating interface and related types.

**Contract**: Interface `IFlashcardGenerator` with single method `Task<List<FlashcardProposal>> GenerateFlashcardsAsync(string sourceText, CancellationToken cancellationToken = default)`. Record `FlashcardProposal(string Question, string Answer)` as the return item type. Namespace: `_10x_cards.Services`.

#### 3. Create OpenAI implementation

**File**: `Services/OpenAiFlashcardGenerator.cs` (new)

**Intent**: Production implementation that calls OpenAI's chat completions API to extract educational Q/A pairs from source text. Uses Microsoft.Extensions.AI's `IChatClient` abstraction for provider independence.

**Contract**: Constructor takes `IChatClient` (injected via DI). `GenerateFlashcardsAsync` sends a chat completion request with a system prompt instructing the model to: (1) extract key concepts from the provided text, (2) generate question/answer pairs suitable for spaced repetition study, (3) output as a JSON array of `{"question": "...", "answer": "..."}` objects, (4) generate in the same language as the input text, (5) focus on understanding not trivia. Parses the response content as JSON into `List<FlashcardProposal>`. Handles JSON parsing failures (malformed or wrapped in markdown code fences) by stripping markdown fences before parsing and throwing a descriptive exception on ultimate parse failure.

#### 4. Create development stub

**File**: `Services/DevFlashcardGenerator.cs` (new)

**Intent**: Development stub that returns hardcoded proposals after a simulated delay. Enables fast UI iteration without API costs or network dependency.

**Contract**: Implements `IFlashcardGenerator`. Returns 3–5 hardcoded Q/A proposals (realistic educational content) after a 2-second `Task.Delay` to simulate LLM latency. Logs a message indicating stub usage via `ILogger<DevFlashcardGenerator>`.

#### 5. Configure DI and API key

**File**: `Program.cs`

**Intent**: Register `IFlashcardGenerator` with environment-based implementation selection. In Development, use the stub; in production, use OpenAI with an API key from configuration.

**Contract**: In Development environment: register `DevFlashcardGenerator` as `IFlashcardGenerator` (scoped lifetime). In non-Development: read `OpenAI:ApiKey` from configuration, instantiate an OpenAI `IChatClient` for model `gpt-4o-mini`, register `OpenAiFlashcardGenerator` as `IFlashcardGenerator` (scoped). API key stored via `dotnet user-secrets` for local development, environment variable for Azure deployment.

#### 6. Create generation endpoint

**File**: `Endpoints/GenerationEndpoints.cs` (new)

**Intent**: API endpoint for generating flashcard proposals from source text. Follows the `AuthEndpoints` extension method pattern.

**Contract**: Static class with `MapGenerationEndpoints(this WebApplication app)` extension method returning `WebApplication`. Route group: `/api/generation`. Single endpoint: POST `/` accepting `GenerationRequest(string SourceText)`. Validation: `SourceText` not null/whitespace and `SourceText.Length` ≤ 10,000 — on failure returns 400 with `{ error: "..." }`. On success: calls `IFlashcardGenerator.GenerateFlashcardsAsync`, returns 200 with `GenerationResponse(List<FlashcardProposal> Proposals)`. On empty proposals list (LLM returned nothing): returns 200 with empty proposals array (frontend handles the empty state). On `IFlashcardGenerator` exception: logs error via `ILogger`, returns 500 with `{ error: "Generation failed. Please try again." }`. DTOs (`GenerationRequest`, `GenerationResponse`) defined as records in the same file.

#### 7. Register generation endpoints

**File**: `Program.cs`

**Intent**: Wire the generation endpoints into the app's endpoint routing.

**Contract**: `app.MapGenerationEndpoints()` added after `app.MapAuthEndpoints()`.

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Application starts without errors in Development mode (dev stub active)

#### Manual Verification:

- POST `/api/generation` with valid text (authenticated via cookie, curl) returns JSON proposals from dev stub
- POST `/api/generation` with empty/whitespace text returns 400
- POST `/api/generation` with text exceeding 10,000 characters returns 400
- POST `/api/generation` without authentication returns 401
- When `OpenAI:ApiKey` is configured and environment is non-Development, real OpenAI integration returns proposals from pasted text

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 3: Flashcard Creation + Frontend Integration

### Overview

Build the POST `/api/flashcards` endpoint for accepting proposals as flashcards, then wire the complete client-side generation flow: AJAX call → loading state → proposal cards → accept/reject/edit → summary message. After this phase, the north star flow works end-to-end in the browser.

### Changes Required:

#### 1. Create flashcard creation endpoint

**File**: `Endpoints/FlashcardEndpoints.cs` (new)

**Intent**: API endpoint for creating a flashcard from an accepted proposal. Designed to be reusable by S-02 (manual creation) — accepts any `FlashcardSource`, not just `AiGenerated`.

**Contract**: Static class with `MapFlashcardEndpoints(this WebApplication app)` extension method returning `WebApplication`. Route group: `/api/flashcards`. Single endpoint: POST `/` accepting `CreateFlashcardRequest(string Question, string Answer, string Source)`. Validation: `Question` and `Answer` not null/whitespace and ≤ 5,000 characters each; `Source` must parse to a valid `FlashcardSource` enum value. Gets current user ID from `ClaimTypes.NameIdentifier`. Creates a `Flashcard` entity with the provided values and the current user's ID, saves to DB. Returns 201 with `FlashcardResponse(Guid Id, string Question, string Answer, string Source, DateTime CreatedAt)`. DTOs defined as records in the same file.

#### 2. Register flashcard endpoints

**File**: `Program.cs`

**Intent**: Wire the flashcard endpoints into the app's endpoint routing.

**Contract**: `app.MapFlashcardEndpoints()` added after `app.MapGenerationEndpoints()`.

#### 3. Create client-side generation flow

**File**: `wwwroot/js/generate.js` (new)

**Intent**: Wire the complete client-side flow for the Generate page — form interaction, API calls, proposal card rendering, accept/reject/edit interactions, and summary display. Vanilla JavaScript with no framework dependencies.

**Contract**: Key behaviors:
- **Generate button click**: Read textarea value, validate length (must be 1–10,000 chars — show inline error if empty or too long). POST to `/api/generation` with JSON body `{ sourceText }`. During the request: disable button and textarea, show loading spinner, hide previous error/summary.
- **Render proposals**: On 200 response with non-empty proposals array, render each proposal as a `.proposal-card` element containing the question text, answer text, and three action buttons (Accept, Edit, Reject). On 200 with empty array, show "No flashcards could be generated from this text. Try pasting a different fragment with more factual content." On error response (4xx/5xx), show the error message and re-enable the form.
- **Accept**: POST to `/api/flashcards` with `{ question, answer, source: "AiGenerated" }`. On 201: remove the card from the DOM (CSS fade-out), increment an accepted counter. On error: show inline error on the card.
- **Reject**: Remove the card from the DOM (CSS fade-out). No API call.
- **Edit**: Replace the Q/A text displays with `<textarea>` elements pre-filled with the current values. Replace action buttons with Save and Cancel. Save: write edited values back into the card's data and return to view mode. Cancel: discard edits and return to view mode. After save, the card is in view mode with updated text — the user must still click Accept to create the flashcard.
- **Summary**: When all cards have been accepted or rejected (proposals container is empty), show: "You added N flashcards to your deck. Paste more text to generate more." Clear and re-enable the textarea and button for another cycle.
- **Character counter**: On textarea `input` event, update the counter span with current length.

#### 4. Add script reference to Generate page

**File**: `Pages/Generate.cshtml`

**Intent**: Load the generation JavaScript and ensure it runs after the DOM is ready.

**Contract**: `<script src="/js/generate.js"></script>` added at the bottom of the page content or in the Scripts section.

#### 5. Complete card and interaction styles

**File**: `wwwroot/css/site.css`

**Intent**: Add styles for proposal cards in all states (view, editing, accepting, rejecting), loading spinner animation, summary message, and error display.

**Contract**: Additional styles for: `.proposal-card` layout (bordered card with Q/A sections separated, action buttons row at bottom), `.proposal-card .editing` state (textarea fields replacing static text), button variants (`.btn-accept` green/positive, `.btn-reject` red/negative, `.btn-edit` neutral), `.loading-spinner` (CSS animation, centered), `.summary-message` (success styling), `.error-message` (error styling), `.fade-out` (opacity transition for smooth card removal). All within the existing `site.css` file.

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Application starts without errors: `dotnet run`

#### Manual Verification:

- Full end-to-end flow: paste text → click generate → see loading spinner → see proposal cards → accept some, reject some → see summary with correct count
- Accepted proposal creates a Flashcard in the database with `Source = AiGenerated` and the correct user ID
- Rejected proposal removes the card without any database record
- Edit allows modifying Q/A text before accepting; the modified text is what gets saved to the database
- Character counter shows correct count, prevents input beyond 10,000 characters
- Empty textarea shows validation error without calling the API
- Loading spinner is visible during generation, disappears when proposals render
- On generation error, error message is displayed and textarea retains its content for retry
- After summary, user can paste new text and generate again without page reload
- All interactions work as a single-page experience within the Razor Page

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Testing Strategy

### Unit Tests:

- No unit tests in MVP phases. The `IFlashcardGenerator` interface enables unit testing of consumers, and the `DevFlashcardGenerator` stub serves as a functional test harness during development. Unit tests can be added as a follow-up.

### Integration Tests:

- POST `/api/generation` returns valid proposals via dev stub (verified with curl)
- POST `/api/flashcards` creates a Flashcard record with correct Source and UserId (verified with curl + database inspection)

### Manual Testing Steps:

1. Authenticate via magic link (POST `/api/auth/login`, open console-logged URL)
2. Navigate to `/` — see Generate page with layout
3. Paste a multi-paragraph text excerpt (Polish or English), click Generate
4. Wait for proposals — loading spinner visible during generation
5. Accept 2 proposals — verify flashcards appear in the database
6. Reject 1 proposal — verify it does NOT appear in the database
7. Edit 1 proposal's answer text, then accept — verify the modified text is saved
8. After all proposals are handled, see summary: "You added N flashcards to your deck"
9. Paste new text, generate again — flow repeats without page reload
10. Try with empty textarea — validation prevents submission
11. Try with text exceeding 10,000 characters — validation prevents submission or truncates

## Performance Considerations

- GPT-4o-mini processes ~10,000 characters in 5–15 seconds depending on OpenAI load. The loading spinner provides feedback during this wait. No hard timeout in MVP (per NFR).
- Each generation costs approximately $0.002 (10k input tokens at $0.15/1M). At single-digit users, cost is negligible.
- Proposal cards are rendered and managed entirely client-side — accept/reject/edit interactions require no server round-trip except the final accept POST.
- SQLite single-writer limitation is not a concern at MVP scale.

## Migration Notes

No new EF Core migration is needed for S-01. The existing Flashcard schema (Question, Answer, Source, UserId, CreatedAt, UpdatedAt) supports everything this slice requires.

**Prerequisite**: The uncommitted F-01 review fixes (IHasTimestamps interface, MaxLength attributes, absolute DB paths, generic error response, required enum modifier) should be committed and a migration created for the MaxLength/annotation changes before starting S-01 implementation. These changes are currently in the working tree.

## References

- Roadmap item: `context/foundation/roadmap.md` — S-01: first-gated-generation (north star)
- PRD requirements: `context/foundation/prd.md` — US-01, FR-004–FR-008, NFR (progress indicator >2s)
- Shape notes: `context/foundation/shape-notes.md` — vision, business logic, forward notes
- Existing endpoint pattern: `Endpoints/AuthEndpoints.cs` — extension method with MapGroup
- Existing service pattern: `Services/IEmailSender.cs` — interface + dev stub in same file
- Data entity: `Data/Flashcard.cs` — target entity for accepted proposals
- Enum: `Data/FlashcardSource.cs` — `AiGenerated` value used for accepted proposals

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Razor Pages Infrastructure + App Shell

#### Automated

- [x] 1.1 Project builds without errors — 69f2f75
- [x] 1.2 Application starts without errors — 69f2f75

#### Manual

- [x] 1.3 Authenticated user sees Generate page with layout — 69f2f75
- [x] 1.4 Unauthenticated user is redirected to LoginRequired — 69f2f75
- [x] 1.5 API requests still receive 401 (not redirect) — 69f2f75
- [x] 1.6 Character counter displays and updates — 69f2f75
- [x] 1.7 Logout link works — 69f2f75

### Phase 2: AI Service + Generation Endpoint

#### Automated

- [x] 2.1 Project builds without errors
- [x] 2.2 Application starts in Development mode (dev stub active)

#### Manual

- [x] 2.3 POST /api/generation with valid text returns proposals (dev stub)
- [x] 2.4 POST /api/generation with empty text returns 400
- [x] 2.5 POST /api/generation with >10,000 chars returns 400
- [x] 2.6 POST /api/generation without auth returns 401
- [x] 2.7 Real OpenAI integration returns proposals (when API key configured)

### Phase 3: Flashcard Creation + Frontend Integration

#### Automated

- [ ] 3.1 Project builds without errors
- [ ] 3.2 Application starts without errors

#### Manual

- [ ] 3.3 Full end-to-end flow works (paste → generate → accept/reject → summary)
- [ ] 3.4 Accept creates Flashcard in database
- [ ] 3.5 Reject removes card without DB record
- [ ] 3.6 Edit allows modifying Q/A before accepting, saves modified text
- [ ] 3.7 Character counter and input validation work
- [ ] 3.8 Loading spinner displays during generation
- [ ] 3.9 Error message displays on generation failure
- [ ] 3.10 Summary shows correct count and allows generating more
