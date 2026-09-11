# Data Schema Setup — Plan Brief

> Full plan: `context/changes/data-schema-setup/plan.md`

## What & Why

Set up Entity Framework Core with SQLite and create the base schema (User, Flashcard) that every downstream slice in M-1 depends on. Without a persistence layer, no feature slice can store or retrieve data. The Source enum on Flashcard enables the PRD's core success metric (75% flashcards created via AI path) from day one.

## Starting Point

Bare ASP.NET Core 9.0 minimal API scaffold with two endpoints (`/health`, `/weatherforecast`). Zero data infrastructure — no EF Core, no database, no entities, no migrations.

## Desired End State

EF Core is wired into DI. Two entities (User, Flashcard) with auto-managed UTC timestamps exist. An initial migration creates both tables in a SQLite file at `./data/10xcards.db`. A `/db-health` endpoint confirms connectivity. The foundation is ready for F-02 (auth) and all feature slices.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| Database engine | SQLite | Zero setup, zero cost on Azure F1, sufficient for small-scale MVP. |
| Source tracking | Enum column now | PRD metric requires it; adding later just defers a guaranteed migration. |
| Timestamps | CreatedAt + UpdatedAt (auto UTC) | Essential for debugging and deck sorting; near-zero cost via SaveChanges override. |
| DB file location | `./data/10xcards.db` via config | Works locally and on Azure F1 without changes; .gitignored. |

## Scope

**In scope:**
- EF Core SQLite provider + Design packages
- User entity (Id, Email, timestamps)
- Flashcard entity (Id, Question, Answer, Source enum, UserId FK, timestamps)
- ApplicationDbContext with auto-timestamps
- Initial migration
- Connection string config
- `/db-health` temporary endpoint
- .gitignore for data/ directory

**Out of scope:**
- SM-2 fields (deferred to S-04)
- Auth tables/session management (F-02)
- Repository/service layer abstraction
- Seed data
- Production DB hosting

## Architecture / Approach

Standard EF Core setup: entity POCOs in `Data/`, a single `ApplicationDbContext` registered in DI, SQLite provider pointed at a local file. Auto-timestamps via `SaveChanges` override. Code-first migrations. Minimal API endpoints inject `ApplicationDbContext` directly — no repository abstraction until warranted.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. EF Core Setup | Packages, entities, DbContext, migration, DB health endpoint | None significant — greenfield EF Core setup on a well-understood stack |

**Prerequisites:** `dotnet-ef` CLI tool installed globally (`dotnet tool install --global dotnet-ef`)
**Estimated effort:** ~1 session, single phase

## Open Risks & Assumptions

- Azure F1 local storage is ephemeral — SQLite file may be lost on restart. Acceptable for MVP validation; not for production data.
- `dotnet-ef` CLI tool must be installed for migration commands.

## Success Criteria (Summary)

- `dotnet build` succeeds, `dotnet ef database update` creates the SQLite file with correct schema
- `/health` still returns "healthy" (no regression), `/db-health` confirms connectivity
- Schema matches design: Users (Id, Email, CreatedAt, UpdatedAt) and Flashcards (Id, Question, Answer, Source, UserId, CreatedAt, UpdatedAt)
