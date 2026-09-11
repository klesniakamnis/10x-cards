# Plan: First Deployment to Azure App Service

## Context

The 10xCards project is a freshly scaffolded ASP.NET Core 9.0 webapi (weatherforecast sample). No git, no .gitignore, no CI/CD config. The infrastructure decision (`context/foundation/infrastructure.md`) recommends Azure App Service F1 (free tier, Linux, westeurope region). This plan gets the scaffolded app deployed and responding on a public URL as a smoke test — before any feature work begins.

## Prerequisites to verify

- **Azure CLI**: the explore agent reported `az` not found. Must be installed or confirmed available before deployment steps.
- **Azure subscription**: user must have an active Azure subscription (free tier is fine).

## Steps

### 1. Add .gitignore

Create a standard .NET `.gitignore` at the project root. Must exclude:
- `bin/`, `obj/`, `.vs/`
- `*.csproj.user`
- `appsettings.Development.json` (keep in git for now — no secrets yet; revisit when secrets are added)
- Standard OS/IDE files (Thumbs.db, .DS_Store, *.swp, .idea/)

Source: `dotnet new gitignore` generates the canonical template.

### 2. Initialize git + initial commit

- `git init`
- `git add` all project files (csproj, Program.cs, appsettings, launchSettings, AGENTS.md, context/, 10x-cards.http)
- Initial commit with Conventional Commits: `feat: scaffold 10xCards ASP.NET Core 9.0 webapi`

### 3. Add a health check endpoint

Add a minimal `/health` endpoint to `Program.cs` before `app.Run()`:
```csharp
app.MapGet("/health", () => Results.Ok("healthy"));
```
This gives Azure App Service a lightweight health check target and confirms the app is responsive without hitting the heavier weatherforecast logic.

Commit: `feat: add /health endpoint for Azure App Service health checks`

### 4. Verify Azure CLI availability

Run `az --version`. If not installed:
- Windows: `winget install Microsoft.AzureCLI`
- Then `az login` to authenticate

### 5. Publish and deploy

```powershell
dotnet publish -c Release -o ./publish
az webapp up --name 10x-cards --resource-group 10x-cards-rg --runtime "DOTNETCORE:9.0" --sku F1 --os-type Linux --location westeurope
```

`az webapp up` creates the Resource Group, App Service Plan, and App in one command. Uses westeurope (Netherlands) for lowest latency to Polish users.

### 6. Verify deployment

- Open `https://10x-cards.azurewebsites.net/health` — should return "healthy"
- Open `https://10x-cards.azurewebsites.net/weatherforecast` — should return JSON forecast data
- Run `az webapp log tail --name 10x-cards --resource-group 10x-cards-rg` to stream logs and confirm no errors

### 7. Note F1 tier limitations

After verifying the deployment works, note in conversation:
- F1 has no Always On — app unloads after ~20 min idle (5-15s cold starts)
- 60 CPU-min/day cap — sufficient for smoke testing, not for real users
- No custom domain — accessible only at `*.azurewebsites.net`
- No deployment slots — rollback is manual redeployment

## Files modified

| File | Action |
|---|---|
| `.gitignore` | Create (via `dotnet new gitignore`) |
| `Program.cs` | Add `/health` endpoint (1 line) |

## Files NOT modified

- `10x-cards.csproj` — no new packages needed
- `appsettings.json` — no config changes for F1 deployment
- `context/` — managed by skills, not edited directly

## Verification

1. `dotnet build` — compiles without errors
2. `dotnet run` + hit `http://localhost:5285/health` locally — returns "healthy"
3. After deploy: `https://10x-cards.azurewebsites.net/health` responds 200
4. After deploy: `https://10x-cards.azurewebsites.net/weatherforecast` returns JSON
5. `az webapp log tail` shows clean startup with no errors
