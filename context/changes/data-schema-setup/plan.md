# Data Schema Setup — Implementation Plan

## Overview

Set up Entity Framework Core with SQLite as the persistence layer for 10xCards. Create the base schema with two entities (User, Flashcard) including a Source enum for the PRD's 75% AI-creation metric, auto-managed UTC timestamps, and an initial migration. This foundation slice unlocks every downstream slice in the milestone.

## Current State Analysis

The project is a bare ASP.NET Core 9.0 minimal API scaffold (`Program.cs`) with two endpoints (`/health`, `/weatherforecast`) and zero data infrastructure. No EF Core packages, no DbContext, no entity classes, no migrations directory, no database file.

- Entry point: `Program.cs:1-44` — minimal API with `WebApplication.CreateBuilder`
- Project file: `10x-cards.csproj` — targets `net9.0`, root namespace `_10x_cards`, only package is `Microsoft.AspNetCore.OpenApi`
- Config: `appsettings.json` — default logging config, no connection string

## Desired End State

EF Core is wired into the DI container. A `ApplicationDbContext` with `DbSet<User>` and `DbSet<Flashcard>` exists. An initial migration creates both tables. Running `dotnet ef database update` produces a working SQLite database at `./data/10xcards.db`. The `/health` endpoint still returns "healthy". A temporary `/db-health` endpoint confirms the database is reachable.

### Key Discoveries:

- Root namespace is `_10x_cards` (set in csproj) — all new files use this namespace
- Nullable reference types are enabled — entity properties must be explicitly nullable or required
- The project uses implicit usings — no need for `using System;` etc.
- Azure F1 local storage is ephemeral but acceptable for MVP validation (PRD target scale: small, single-digit users)

## What We're NOT Doing

- SM-2 spaced repetition fields (interval, easiness_factor, repetitions, next_review_date) — deferred to S-04 per roadmap
- Auth tables beyond bare User entity — F-02 will add session/token tables
- Seeding test data — no seed data in foundation
- Repository/service layer abstraction — slices inject DbContext directly; abstraction if/when warranted
- Production database hosting — SQLite file on local/Azure storage is the MVP target

## Implementation Approach

Single-phase foundation: add EF Core packages, create entities and DbContext in a `Data/` directory, wire DI in `Program.cs`, generate an initial migration, and add a temporary DB health check endpoint. Timestamps auto-set via a `SaveChanges` override on DbContext.

## Phase 1: EF Core Setup — Entities, DbContext, Migration

### Overview

Add all EF Core infrastructure in one phase: NuGet packages, entity classes, DbContext with auto-timestamps, connection string config, initial migration, and a DB health check endpoint.

### Changes Required:

#### 1. Add NuGet packages

**File**: `10x-cards.csproj`

**Intent**: Add EF Core SQLite provider and EF Core Design (for migrations tooling).

**Contract**: Two new `<PackageReference>` entries — `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design`, both version `9.0.*` (matching the target framework).

#### 2. Create FlashcardSource enum

**File**: `Data/FlashcardSource.cs` (new)

**Intent**: Define the source discriminator for the PRD's 75% AI-creation metric. Two values: `AiGenerated` and `Manual`.

**Contract**: `enum FlashcardSource { AiGenerated, Manual }` in namespace `_10x_cards.Data`.

#### 3. Create User entity

**File**: `Data/User.cs` (new)

**Intent**: Bare user entity for the flat access model. Stores email for magic-link auth (F-02). One user has many flashcards.

**Contract**: Properties — `Id` (Guid, PK), `Email` (string, required, unique index), `CreatedAt` (DateTime, UTC), `UpdatedAt` (DateTime, UTC). Navigation — `ICollection<Flashcard> Flashcards`.

#### 4. Create Flashcard entity

**File**: `Data/Flashcard.cs` (new)

**Intent**: Core domain entity — a question/answer pair owned by a user, with source tracking.

**Contract**: Properties — `Id` (Guid, PK), `Question` (string, required), `Answer` (string, required), `Source` (FlashcardSource enum, required), `UserId` (Guid, FK to User), `CreatedAt` (DateTime, UTC), `UpdatedAt` (DateTime, UTC). Navigation — `User User`.

#### 5. Create ApplicationDbContext

**File**: `Data/ApplicationDbContext.cs` (new)

**Intent**: EF Core DbContext with DbSets for both entities and auto-timestamp logic.

**Contract**: Inherits `DbContext`. Exposes `DbSet<User> Users` and `DbSet<Flashcard> Flashcards`. Overrides `SaveChangesAsync` and `SaveChanges` to auto-set `CreatedAt` (on Add) and `UpdatedAt` (on Add or Modify) to `DateTime.UtcNow`. `OnModelCreating` configures: unique index on `User.Email`, `Flashcard.Source` stored as string (enum-to-string conversion), cascade delete on User→Flashcards relationship.

#### 6. Configure connection string

**File**: `appsettings.json`

**Intent**: Add a connection string pointing to the SQLite database file.

**Contract**: Add `"ConnectionStrings": { "DefaultConnection": "Data Source=./data/10xcards.db" }`.

#### 7. Wire DbContext into DI and add DB health check

**File**: `Program.cs`

**Intent**: Register `ApplicationDbContext` with SQLite provider in the service container. Add a `/db-health` endpoint that verifies database connectivity. Ensure the `data/` directory exists at startup.

**Contract**: `builder.Services.AddDbContext<ApplicationDbContext>(...)` using the connection string from config. New `MapGet("/db-health", ...)` endpoint that calls `dbContext.Database.CanConnectAsync()` and returns ok/error. Directory.CreateDirectory for `./data/` before `app.Run()`.

#### 8. Add data/ directory to .gitignore

**File**: `.gitignore`

**Intent**: Exclude the SQLite database file from version control.

**Contract**: Append `data/` to the existing `.gitignore`.

#### 9. Create initial migration

**Intent**: Generate and apply the EF Core migration that creates the Users and Flashcards tables.

**Contract**: Run `dotnet ef migrations add InitialCreate` to generate migration files in `Migrations/`. Run `dotnet ef database update` to apply.

### Success Criteria:

#### Automated Verification:

- Project builds without errors: `dotnet build`
- EF Core migration generates cleanly: `dotnet ef migrations list` shows `InitialCreate`
- Database update applies without errors: `dotnet ef database update`
- SQLite file exists at `./data/10xcards.db` after migration
- Existing tests (if any) still pass: `dotnet test`

#### Manual Verification:

- `/health` endpoint returns "healthy" (no regression)
- `/db-health` endpoint returns success confirming DB connectivity
- SQLite DB contains `Users` and `Flashcards` tables with correct columns (verify via `sqlite3` or DB browser)

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

## Testing Strategy

### Unit Tests:

- No unit tests for this foundation phase — the entities are POCOs and the DbContext is standard EF Core. Testing value comes from integration verification.

### Integration Tests:

- The `/db-health` endpoint serves as a runtime integration test.
- `dotnet ef migrations list` confirms migration tooling works end-to-end.

### Manual Testing Steps:

1. Run `dotnet build` — confirms all packages resolve and code compiles
2. Run `dotnet ef database update` — confirms migration applies
3. Run `dotnet run` and hit `/health` — confirms no regression
4. Run `dotnet run` and hit `/db-health` — confirms DB connectivity
5. Open `./data/10xcards.db` with a SQLite viewer — confirm Users and Flashcards tables with expected columns

## Performance Considerations

SQLite is single-writer; concurrent writes serialize. This is acceptable for the PRD's target scale (small, single-digit users). If concurrency becomes an issue post-MVP, the switch to PostgreSQL requires only a provider swap and connection string change — the schema and DbContext remain the same.

## Migration Notes

This is the first migration on a greenfield project — no existing data, no backwards compatibility concerns. The `InitialCreate` migration creates both tables from scratch.

## References

- Roadmap item: `context/foundation/roadmap.md` — F-01: data-schema-setup
- PRD success criteria: `context/foundation/prd.md:38-39` — 75% AI-creation metric requires Source field
- Tech stack: `context/foundation/tech-stack.md` — .NET 9.0, EF Core implied
- Current entry point: `Program.cs:1-44`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: EF Core Setup — Entities, DbContext, Migration

#### Automated

- [x] 1.1 Project builds without errors
- [x] 1.2 EF Core migration generates cleanly
- [x] 1.3 Database update applies without errors
- [x] 1.4 SQLite file exists at ./data/10xcards.db
- [x] 1.5 Existing tests still pass

#### Manual

- [x] 1.6 /health endpoint returns "healthy"
- [x] 1.7 /db-health endpoint returns success
- [x] 1.8 SQLite DB contains correct tables and columns
