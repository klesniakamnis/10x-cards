<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Data Schema Setup (F-01)

- **Plan**: context/changes/data-schema-setup/plan.md
- **Scope**: Phase 1 of 1
- **Date**: 2026-09-13
- **Verdict**: NEEDS ATTENTION
- **Findings**: 0 critical, 3 warnings, 3 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | PASS |
| Safety & Quality | WARNING |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — SetTimestamps uses magic strings, fragile to new entities

- **Severity**: WARNING
- **Impact**: MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: Data/ApplicationDbContext.cs:60-69
- **Detail**: `SetTimestamps()` iterates ALL tracked entities and calls `entry.Property("UpdatedAt")` / `entry.Property("CreatedAt")` via string lookup. If any entity without these properties is ever tracked, `SaveChanges` throws `InvalidOperationException` at runtime. Currently all entities have the properties, but this is fragile as the schema grows.
- **Fix A (Recommended)**: Define an `IHasTimestamps` interface with `CreatedAt` and `UpdatedAt`, implement it on entities, filter with `ChangeTracker.Entries<IHasTimestamps>()`.
  - Strength: Type-safe, self-documenting, new entities opt in explicitly.
  - Tradeoff: Adds an interface + implements on 4 entities (User, Flashcard, MagicLinkToken, AuthSession).
  - Confidence: HIGH — standard EF Core pattern.
  - Blind spot: None significant.
- **Fix B**: Add a try/catch around the property lookup to silently skip entities without timestamps.
  - Strength: Zero-touch on existing entities.
  - Tradeoff: Silently swallows missing timestamps — entities that *should* have them could be skipped without notice.
  - Confidence: LOW — masks bugs.
  - Blind spot: Hard to diagnose when timestamps stop being set.
- **Decision**: FIXED via Fix A

### F2 — /db-health leaks exception details

- **Severity**: WARNING
- **Impact**: LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Program.cs (catch block in /db-health handler)
- **Detail**: The catch block returns `ex.Message` directly in the JSON response. SQLite/EF Core exception messages can expose file system paths, connection string fragments, or internal details. Currently no auth on this endpoint (expected for F-01), but the leak persists even after F-02 added AllowAnonymous to it.
- **Fix**: Replace `error = ex.Message` with a generic `database = "error"` and log the exception server-side with `ILogger`.
- **Decision**: FIXED

### F3 — FlashcardSource enum default silently assigns AiGenerated

- **Severity**: WARNING
- **Impact**: MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Data Safety
- **Location**: Data/Flashcard.cs:8
- **Detail**: `Source` is a value-type enum with no `required` modifier. Default value `0` maps to `FlashcardSource.AiGenerated`. If a future endpoint creates a Flashcard without setting `Source`, the card silently counts as AI-generated, inflating the PRD's 75% AI-creation metric.
- **Fix**: Add `required` modifier to the `Source` property, matching the pattern already used on `Question` and `Answer`.
  - Strength: Compile-time enforcement; consistent with other required properties on the same entity.
  - Tradeoff: Minimal — purely additive.
  - Confidence: HIGH — follows existing pattern in the same file.
  - Blind spot: None significant.
- **Decision**: FIXED

### F4 — Database path drift: plan says ./data/, implementation uses ./db/

- **Severity**: OBSERVATION
- **Impact**: LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: appsettings.json, Program.cs, .gitignore
- **Detail**: Plan specifies `./data/10xcards.db`, `Directory.CreateDirectory("./data/")`, and `.gitignore` entry `data/`. Implementation consistently uses `./db/` across all three files. The deviation is internally consistent but the plan contracts were never updated to reflect it.
- **Fix**: No code change needed — implementation is self-consistent. Update plan contracts (sections 6, 7, 8) to say `db/` if desired, or note the deviation.
- **Decision**: FIXED — plan text updated to reflect actual ./db/ path

### F5 — No MaxLength on string properties

- **Severity**: OBSERVATION
- **Impact**: LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Data Safety
- **Location**: Data/User.cs:6, Data/Flashcard.cs:6-7
- **Detail**: `Email`, `Question`, `Answer` have no MaxLength. SQLite accepts unbounded TEXT, but a future migration to PostgreSQL/SQL Server would map these to `nvarchar(max)`. Without constraints, write endpoints could store arbitrarily large payloads.
- **Fix**: Add `[MaxLength]` attributes or Fluent API constraints: Email 320 (RFC 5321), Question/Answer ~5000. Low urgency since no write endpoints exist yet.
- **Decision**: FIXED

### F6 — Relative database path fragile across deployment environments

- **Severity**: OBSERVATION
- **Impact**: LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Reliability
- **Location**: Program.cs, appsettings.json
- **Detail**: Both `Directory.CreateDirectory("./db/")` and `Data Source=./db/10xcards.db` use relative paths. These resolve against the working directory at runtime, which varies between `dotnet run`, `dotnet <app>.dll`, and Azure App Service. Plan acknowledges SQLite on Azure F1 is ephemeral for MVP.
- **Fix**: Acceptable for MVP. Before production deployment, use `Path.Combine(builder.Environment.ContentRootPath, "db")` for directory creation and an environment-specific connection string with absolute path.
- **Decision**: FIXED — switched to ContentRootPath-based absolute paths
