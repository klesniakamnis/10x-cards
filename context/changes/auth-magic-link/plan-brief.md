# Plan Brief: auth-magic-link

**Change:** Passwordless auth (magic link) + authorization middleware
**Phases:** 2
**Complexity:** MEDIUM

## Phase 1: Auth Schema + Services
New entities (MagicLinkToken, AuthSession), migration, IEmailSender interface with ConsoleEmailSender.

## Phase 2: Auth Middleware + Endpoints
Cookie auth with DB-backed ticket store, global default-deny policy, login/callback/logout/me endpoints.

## Key Decisions
- Cookie-based sessions, 30-day TTL, HttpOnly + SameSite=Lax
- 15-minute single-use magic link tokens
- DB-backed sessions (survives Azure F1 cold starts)
- Console email logging in dev, pluggable IEmailSender interface
- Default deny auth policy, AllowAnonymous whitelist
- Generic error UX for invalid/expired tokens
