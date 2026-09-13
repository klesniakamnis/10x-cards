# Auth Magic Link — Implementation Plan

## Overview

Implement passwordless magic link authentication for 10xCards. Cookie-based sessions backed by SQLite (survives Azure F1 cold starts), default-deny authorization with whitelisted public endpoints, and a pluggable email interface (console logging in dev). Users enter their email, receive a magic link, click it, and get a 30-day session cookie. First login auto-creates the User record.

## Current State Analysis

The project has EF Core + SQLite wired (`Data/ApplicationDbContext.cs`) with two entities: `User` (Guid Id, Email unique, timestamps) and `Flashcard`. No auth packages, no middleware, no session handling. The `User` entity already has the `Email` field magic link needs — no schema change to `User` required.

- Entry point: `Program.cs:1-65` — minimal API with `AddDbContext`, three endpoints (`/health`, `/db-health`, `/weatherforecast`)
- DbContext: `Data/ApplicationDbContext.cs` — auto-timestamps via `SaveChanges` override
- User entity: `Data/User.cs` — `Id`, `Email` (required, unique index), timestamps, `Flashcards` nav
- Config: `appsettings.json` — SQLite connection string at `./db/10xcards.db`
- Conventions: `AGENTS.md` — minimal APIs only, endpoint groups in separate files via extension methods, PascalCase files, records for DTOs

## Desired End State

A complete passwordless auth flow: POST email → console-logged magic link → GET callback → 30-day cookie session. All endpoints protected by default; only `/health`, `/db-health`, login, and callback are public. A `/api/auth/me` endpoint returns the current user. Sessions and tokens are DB-backed (two new tables). The email sender is behind an interface so a real provider can be swapped in later.

### Key Design Decisions:

- **Session mechanism:** HttpOnly cookie, 30-day TTL, DB-backed (AuthSession table)
- **Magic link tokens:** 15-minute expiry, single-use, stored in MagicLinkToken table
- **Email in dev:** Console logging via `IEmailSender` interface; real provider plugged in later
- **Session store:** Database (SQLite) — survives Azure F1 cold starts, no in-memory loss
- **Error UX:** Generic "link no longer valid" + "request new link" for expired/used/invalid tokens
- **Auth default:** Global authorization policy (default deny); public endpoints whitelisted with `AllowAnonymous`

## What We're NOT Doing

- Real email provider integration (SendGrid/Resend/Mailgun) — dev console logging only
- UI/frontend — API endpoints only; UI is a future slice
- OAuth/social login — magic link is the sole auth mechanism for MVP
- Rate limiting on login requests — acceptable risk at MVP scale
- CSRF protection — cookie is HttpOnly + SameSite=Lax; API-only (no form posts from browser)
- Refresh token rotation — single long-lived session, not access+refresh pattern

## Implementation Approach

Two phases. Phase 1 adds the schema (MagicLinkToken, AuthSession entities) and the email service abstraction. Phase 2 wires cookie auth middleware, builds the login/callback/logout/me endpoints, and applies the global authorization policy. Phase 1 is verifiable with build + migration. Phase 2 is verifiable end-to-end via console-logged magic link.

## Phase 1: Auth Schema + Services

### Overview

Add two new entities (MagicLinkToken, AuthSession) to the existing DbContext, generate a migration, and create the `IEmailSender` interface with a `ConsoleEmailSender` implementation that logs magic links to stdout.

### Changes Required:

#### 1. Create MagicLinkToken entity

**File**: `Data/MagicLinkToken.cs` (new)

**Intent**: Store single-use magic link tokens with expiry tracking. The token is a cryptographically random string sent in the magic link URL.

**Contract**: Properties — `Id` (Guid, PK), `Token` (string, required, unique index), `Email` (string, required), `ExpiresAt` (DateTime, UTC), `UsedAt` (DateTime?, nullable — set when consumed), `CreatedAt` (DateTime, UTC), `UpdatedAt` (DateTime, UTC).

#### 2. Create AuthSession entity

**File**: `Data/AuthSession.cs` (new)

**Intent**: DB-backed session record. The session ID is stored in the auth cookie; lookup on each request validates the session.

**Contract**: Properties — `Id` (Guid, PK), `UserId` (Guid, FK to User), `ExpiresAt` (DateTime, UTC), `CreatedAt` (DateTime, UTC), `UpdatedAt` (DateTime, UTC). Navigation — `User User`.

#### 3. Register new entities in ApplicationDbContext

**File**: `Data/ApplicationDbContext.cs` (modify)

**Intent**: Add DbSets and configure relationships/indexes for the two new entities.

**Contract**: Add `DbSet<MagicLinkToken> MagicLinkTokens` and `DbSet<AuthSession> AuthSessions`. In `OnModelCreating`: unique index on `MagicLinkToken.Token`, FK from `AuthSession.UserId` to `User` with cascade delete.

#### 4. Create IEmailSender interface and ConsoleEmailSender

**File**: `Services/IEmailSender.cs` (new)

**Intent**: Pluggable email abstraction. Dev uses console logging; production swaps in a real provider.

**Contract**: Interface with `Task SendMagicLinkAsync(string email, string magicLinkUrl)`. `ConsoleEmailSender` implements it by logging the email and URL to `ILogger<ConsoleEmailSender>`.

#### 5. Generate migration

**Intent**: Create EF Core migration for the two new tables.

**Contract**: Run `dotnet ef migrations add AddAuthTables` to generate migration files. Run `dotnet ef database update` to apply.

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Migration generates cleanly: `dotnet ef migrations list` shows `AddAuthTables`
- Database update applies without errors: `dotnet ef database update`
- Existing tests (if any) still pass: `dotnet test`

#### Manual Verification:

- SQLite DB contains `MagicLinkTokens` and `AuthSessions` tables with correct columns
- `/health` and `/db-health` endpoints still work (no regression)

## Phase 2: Auth Middleware + Endpoints

### Overview

Wire ASP.NET Core cookie authentication with a custom DB-backed ticket store, add a global authorization policy (default deny), create the login/callback/logout/me endpoints in a separate `AuthEndpoints.cs` file, and whitelist public endpoints with `AllowAnonymous`.

### Changes Required:

#### 1. Create DbSessionStore (custom ITicketStore)

**File**: `Auth/DbSessionStore.cs` (new)

**Intent**: Bridge ASP.NET Core's cookie auth to our AuthSession table. Cookie holds the session ID; the ticket (claims) are looked up from the DB on each request.

**Contract**: Implements `Microsoft.AspNetCore.Authentication.Cookies.ITicketStore`. Methods:
- `StoreAsync`: create AuthSession row, serialize AuthenticationTicket, return session ID as key.
- `RenewAsync`: update the session row.
- `RetrieveAsync`: look up by session ID, return deserialized ticket if not expired.
- `RemoveAsync`: delete the session row.

Store the serialized `AuthenticationTicket` as a byte[] column on AuthSession (add `TicketData` property of type `byte[]` to the entity).

#### 2. Update AuthSession entity with TicketData

**File**: `Data/AuthSession.cs` (modify)

**Intent**: Add serialized ticket storage for the DB-backed session store.

**Contract**: Add `byte[] TicketData` (required) property to AuthSession.

#### 3. Create AuthEndpoints

**File**: `Endpoints/AuthEndpoints.cs` (new)

**Intent**: All auth-related endpoints in one file, registered via extension method per AGENTS.md conventions.

**Contract**: Extension method `MapAuthEndpoints(this WebApplication app)` registering:

- `POST /api/auth/login` — accepts `{ email }`. Generates a cryptographically random token (32 bytes, base64url), stores MagicLinkToken row, calls `IEmailSender.SendMagicLinkAsync` with the callback URL. Returns 200 OK always (no email enumeration). AllowAnonymous.
- `GET /api/auth/callback?token={token}` — validates token (exists, not expired, not used). If valid: marks token as used, finds or creates User by email, creates AuthSession, signs in with cookie, redirects to `/`. If invalid: returns generic error. AllowAnonymous.
- `POST /api/auth/logout` — signs out, deletes AuthSession. Returns 200 OK.
- `GET /api/auth/me` — returns current user info `{ id, email }`. Returns 401 if not authenticated (handled by global policy).

#### 4. Wire auth middleware in Program.cs

**File**: `Program.cs` (modify)

**Intent**: Register cookie authentication, the DB session store, the email sender, the global authorization policy, and map auth endpoints.

**Contract**:
- Register `IEmailSender` as `ConsoleEmailSender` (singleton).
- Register `ITicketStore` as `DbSessionStore` (scoped).
- Add `builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Lax; options.ExpireTimeSpan = TimeSpan.FromDays(30); options.SlidingExpiration = true; options.SessionStore = ...; options.LoginPath = ...; })`.
- Add `builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())`.
- Add `app.UseAuthentication()` and `app.UseAuthorization()` before endpoint mapping.
- Add `.AllowAnonymous()` to `/health`, `/db-health`, and the login/callback endpoints.
- Call `app.MapAuthEndpoints()`.

#### 5. Update migration for TicketData column

**Intent**: Generate a new migration for the `TicketData` column added to AuthSession.

**Contract**: Run `dotnet ef migrations add AddSessionTicketData`. Run `dotnet ef database update`.

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- Migration applies without errors: `dotnet ef database update`
- Existing tests (if any) still pass: `dotnet test`

#### Manual Verification:

- `GET /health` returns 200 (AllowAnonymous works)
- `GET /db-health` returns 200 (AllowAnonymous works)
- `GET /weatherforecast` returns 401 (default deny works)
- `GET /api/auth/me` returns 401 when not authenticated
- `POST /api/auth/login` with `{ "email": "test@example.com" }` returns 200 and logs magic link URL to console
- Clicking the console-logged URL (GET callback) signs in and redirects
- `GET /api/auth/me` returns `{ id, email }` after sign-in (cookie is set)
- `POST /api/auth/logout` clears session; subsequent `/api/auth/me` returns 401
- Using an already-used or expired token returns the generic error

## Testing Strategy

### Unit Tests:

- No unit tests for this foundation phase — the auth flow is best verified by integration/manual testing. Unit tests add value at the service layer when business logic grows.

### Integration Tests:

- The four auth endpoints serve as integration tests for the full flow.
- `/api/auth/me` returning 401 vs 200 confirms the global auth policy and cookie session are wired correctly.

### Manual Testing Steps:

1. Run `dotnet build` — confirms compilation
2. Run `dotnet ef database update` — confirms migrations apply
3. Run `dotnet run` and hit `/health` — confirms no regression, AllowAnonymous works
4. Hit `/weatherforecast` — confirms 401 (default deny)
5. POST `/api/auth/login` with email — confirms magic link logged to console
6. GET the logged callback URL — confirms sign-in, cookie set
7. GET `/api/auth/me` — confirms authenticated user returned
8. POST `/api/auth/logout` — confirms sign-out
9. GET `/api/auth/me` again — confirms 401 after logout
10. Repeat login with expired/used token — confirms generic error

## Performance Considerations

DB-backed sessions mean a database read on every authenticated request. At MVP scale (single-digit users), this is negligible. If it becomes an issue, an in-memory cache with short TTL can sit in front of the DB lookup without changing the interface.

## Migration Notes

Two migrations total: `AddAuthTables` (Phase 1) creates MagicLinkToken and AuthSession tables. `AddSessionTicketData` (Phase 2) adds the `TicketData` column to AuthSession. Both are additive — no existing table modifications, no data migration needed.

## References

- Roadmap item: `context/foundation/roadmap.md` — F-02: auth-magic-link
- PRD auth requirements: `context/foundation/prd.md` — FR-001, FR-002, FR-003, Access Control
- Shape notes: `context/foundation/shape-notes.md` — passwordless magic link, flat user model
- Tech stack: `context/foundation/tech-stack.md` — `has_auth: true`
- Existing DbContext: `Data/ApplicationDbContext.cs`
- Existing User entity: `Data/User.cs`
- AGENTS.md conventions: `AGENTS.md` — minimal APIs, endpoint groups in separate files

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Auth Schema + Services

#### Automated

- [x] 1.1 Project builds without errors — d493931
- [x] 1.2 Migration generates cleanly — d493931
- [x] 1.3 Database update applies without errors — d493931
- [x] 1.4 Existing tests still pass — d493931

#### Manual

- [x] 1.5 SQLite DB contains MagicLinkTokens and AuthSessions tables — d493931
- [x] 1.6 /health and /db-health still work — d493931

### Phase 2: Auth Middleware + Endpoints

#### Automated

- [x] 2.1 Project builds without errors — 0650781
- [x] 2.2 Migration applies without errors — 0650781
- [x] 2.3 Existing tests still pass — 0650781

#### Manual

- [x] 2.4 GET /health returns 200 — 0650781
- [x] 2.5 GET /weatherforecast returns 401 — 0650781
- [x] 2.6 POST /api/auth/login logs magic link to console — 0650781
- [x] 2.7 GET callback URL signs in and redirects — 0650781
- [x] 2.8 GET /api/auth/me returns user info after sign-in — 0650781
- [x] 2.9 POST /api/auth/logout clears session — 0650781
- [x] 2.10 Expired/used token returns generic error — 0650781
