# Repository Guidelines

10xCards is an ASP.NET Core 9.0 webapi that generates flashcard proposals from pasted text via AI and schedules spaced repetition reviews. C# with nullable reference types enabled; root namespace `_10x_cards`.

## Hard Rules

- Never commit secrets, API keys, or connection strings. Use user-secrets (`dotnet user-secrets set`) for local dev; environment variables for deployment.
- `context/` is managed by 10xWorkflow skills — do not edit files there directly.
- Nullable reference types are enabled project-wide (`@10x-cards.csproj`). Do not suppress nullable warnings with `!` without a justifying comment.

## Build and Development

- `dotnet restore`
- `dotnet build`
- `dotnet run` — start the dev server at http://localhost:5285.
- `dotnet test` — run tests (test project not yet scaffolded).

## Project Structure

- `Program.cs` — entry point; endpoint registration and middleware pipeline.
- `Properties/launchSettings.json` — dev server profiles (ports 5285 / 7023).
- `appsettings.json` / `appsettings.Development.json` — configuration per environment.
- `10x-cards.csproj` — project manifest targeting net9.0 with OpenApi.
- `context/foundation/` — product requirements (`prd.md`), discovery notes (`shape-notes.md`), tech-stack decision (`tech-stack.md`).

## Coding Conventions

- Target framework: net9.0. Implicit usings enabled — do not add explicit `using` statements for common System and ASP.NET namespaces.
- Do not add `[ApiController]` classes. Use minimal APIs (`app.MapGet`, `app.MapPost`) exclusively.
- Place new endpoint groups in separate files using extension methods on `WebApplication` (e.g. `app.MapFlashcardEndpoints()`).
- Name files in PascalCase. Use records for DTOs and response models.

## Testing

Not yet configured. When added, use xUnit with `dotnet test`. Place test projects in a sibling `tests/` directory following the `ProjectName.Tests` naming convention.

## Commit and PR Conventions

No git repository initialized yet. When set up: use Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`). CI target is GitHub Actions with auto-deploy on merge to main, deploying to Azure App Service.

## Architecture Notes

See `@context/foundation/prd.md` for the full product requirements. Key features to implement:

- Passwordless auth via magic link email.
- AI-powered flashcard generation from pasted text (Polish and English).
- Spaced repetition scheduling (SM-2 family, scale 0-5).
- Single flat user role model; all product views behind auth wall.
