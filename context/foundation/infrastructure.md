---
project: 10x-cards
researched_at: 2026-09-11
recommended_platform: azure-app-service
runner_up: render
context_type: mvp
tech_stack:
  language: "C#"
  framework: "ASP.NET Core 9.0"
  runtime: ".NET 9.0"
---

## Recommendation

**Deploy on Azure App Service (F1 free tier to start, upgrade to B1 $13/mo when needed).**

Azure App Service is the only researched platform with first-class .NET 9.0 support — no Docker needed, `az webapp up` deploys directly from `dotnet publish`. It scores 5/5 on agent-friendly criteria and offers an F1 free tier at $0/mo for initial development and testing. The native .NET integration eliminates the Dockerfile overhead that all three alternatives (Render, Railway, Fly.io) require — a meaningful DX advantage for a solo developer on a 3-week timeline. The user prioritized minimizing cost; F1 is genuinely free, and the upgrade path to B1 ($13/mo) is a single CLI command when real users arrive.

## Platform Comparison

Three platforms were dropped by hard filter: Cloudflare Workers, Vercel, and Netlify do not support the .NET 9.0 server-side runtime. Fly.io was dropped in soft scoring: no free tier for new signups, Managed Postgres minimum $38/mo, and experimental (not GA) MCP server.

| Platform | CLI-first | Managed | Agent docs | Stable deploy | MCP | Cost (always-on) |
|---|---|---|---|---|---|---|
| Azure App Service | Pass | Pass | Pass (llms.txt) | Pass | Pass (GA) | F1 $0 (limited) / B1 $13/mo |
| Render | Pass | Pass | Pass (llms.txt) | Pass | Pass (GA, 20+ tools) | Free (cold starts) / $7/mo |
| Railway | Pass | Pass | Pass (llms.txt) | Pass | Pass (GA early) | $5/mo |
| Fly.io | Pass | Pass | Pass (llms.txt) | Pass | Partial (experimental) | ~$5-10/mo |

### Shortlisted Platforms

#### 1. Azure App Service (Recommended)

First-class .NET 9.0 support — no Docker needed. `az webapp up` deploys directly from `dotnet publish`. F1 free tier: 60 CPU-min/day, no Always On, no custom domain. B1: ~$13/mo (always-on, custom domains). GA Azure MCP Server 1.0; built-in MCP for App Service in Preview. Deployment slots (rollback) require Standard tier ($70+/mo). Co-located Azure SQL, Blob Storage, Service Bus — all GA. CLI: `az webapp up`, `az webapp log tail`, `az webapp deployment slot swap`.

#### 2. Render

Free tier with 750 instance-hours/month, spins down after 15 min idle. Starter at $7/mo for always-on. GA MCP server at `mcp.render.com` with 20+ tools (deploy, logs, metrics, Postgres queries). Dedicated `render rollback` command. Co-located Postgres (free 30 days, then $7/mo), Redis-compatible KV, cron jobs, background workers. Docker required for .NET — must bind to `0.0.0.0:10000`. CLI: `render deploy`, `render logs`, `render rollback`.

#### 3. Railway

$5/mo Hobby plan (includes $5 credit). GA MCP server at `mcp.railway.com` (OAuth, Claude Code supported). Co-located Postgres, MySQL, Redis, MongoDB as first-class templates. Docker required for .NET (Railpack does not support .NET). 60-second WebSocket idle timeout requires heartbeats. CLI: `railway up`, `railway logs`, `railway redeploy`.

## Anti-Bias Cross-Check: Azure App Service

### Devil's Advocate — Weaknesses

1. **F1 free tier is deceptively limited.** 60 CPU-minutes/day and no Always On means the app unloads after ~20 min idle. Every first request after idle triggers a cold start (5-15s for .NET JIT). For a flashcard app students open a few times a day, they'll hit cold starts every session.
2. **Rollback requires Standard tier ($70+/mo).** Deployment slots — Azure's rollback mechanism — are gated behind the Standard S1 plan. On Free/Basic you can only redeploy the previous artifact manually.
3. **Azure's operational surface is enormous.** Resource groups, App Service Plans, subscription hierarchy, RBAC, Service Connectors — a solo developer must navigate substantial Azure-specific complexity that Railway/Render abstract away entirely.
4. **Vendor lock-in path is steep.** Once you use Azure SQL + Service Connector + Managed Identity, migrating away later involves rewriting the data access and auth layers. Railway/Render/Fly use standard Postgres connection strings with no platform-specific auth layer.
5. **.NET 9 is Standard Term Support — EOL November 2026.** Migration to .NET 10 LTS is needed within ~2 months of shipping the MVP.

### Pre-Mortem — How This Could Fail

The solo developer deployed 10xCards to Azure App Service F1, attracted to the $0 price tag. The first two weeks went smoothly — `az webapp up` deployed the API effortlessly. Then real users arrived. Every student hitting the app after class got a 10-15 second cold start because the free tier unloaded the process after 20 minutes of idle. The developer upgraded to B1 ($13/mo) to enable Always On, but the AI flashcard generation endpoint consumed more CPU than expected — the 60 CPU-minute daily cap on F1 had masked this by simply refusing requests. On B1 the requests succeeded but the single vCPU saturated during concurrent generation calls. When a buggy deployment shipped at 11pm, the developer discovered rollback required Standard tier deployment slots — unavailable on Basic. They redeployed the previous commit manually, losing 40 minutes. By month three, Azure SQL ($5/mo minimum for serverless) plus B1 ($13/mo) plus Blob storage totaled $25/mo — more than Railway or Render would have cost for the same workload. The developer began researching migration options, only to discover the Service Connector auth pattern didn't translate to standard connection strings.

### Unknown Unknowns

- **Windows runtime patch delays are real.** As of Oct 2025, .NET 8/9 patches on Windows App Service instances are delayed due to a platform issue. Mitigation: explicitly select a Linux App Service plan.
- **F1 tier has no SLA and no custom domain.** The app is only reachable at `yourapp.azurewebsites.net`. For a student-facing product, this may be acceptable for MVP.
- **`az webapp up` creates resources you may not expect.** It auto-creates an App Service Plan and Resource Group if they don't exist. Without a naming convention, orphaned resources accumulate and may incur costs.
- **Azure for Students ($100 credit) exists but requires .edu verification.** If applicable, this could cover 6-8 months of B1 hosting — not discoverable from the App Service pricing page.
- **Built-in MCP for App Service is Preview.** The GA Azure MCP Server covers general Azure ops, but the App Service-specific MCP (turning your API into an MCP server) was announced at Build 2026 and may change.

## Operational Story

- **Preview deploys**: Azure supports deployment slots for staging previews, but slots require Standard tier ($70+/mo). On F1/B1, use a separate App Service instance for staging, or deploy to a `-staging` named app. GitHub Actions can deploy on PR merge.
- **Secrets**: Application settings (env vars) stored in Azure Portal or via `az webapp config appsettings set`. Scoped per slot. For sensitive values, reference Azure Key Vault via `@Microsoft.KeyVault(SecretUri=...)`. Rotation via Key Vault policies.
- **Rollback**: On Standard+, swap deployment slots: `az webapp deployment slot swap --slot staging --target-slot production`. On F1/B1, redeploy the previous commit manually: `az webapp deploy --src-path <previous-zip>`. No one-command rollback on free/basic tiers.
- **Approval**: Deploys via `az webapp up` or `az webapp deploy` can be triggered by an agent unattended. Slot swaps, resource deletion, and scaling require explicit action. No built-in approval gate — use GitHub branch protection for the deploy workflow.
- **Logs**: `az webapp log tail --name <app> --resource-group <rg>` streams real-time stdout/stderr. Application Insights (free tier available) provides structured telemetry. Azure MCP Server 1.0 exposes resource-level operations.

## Risk Register

| Risk | Source | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| F1 cold starts (5-15s) after 20 min idle | Devil's advocate | High | Medium | Accept for dev; upgrade to B1 with Always On when users arrive |
| 60 CPU-min/day cap on F1 | Devil's advocate | High | High | Monitor CPU usage; upgrade to B1 before AI generation load hits the cap |
| No rollback on Free/Basic tiers | Devil's advocate | Medium | High | Keep git history clean; practice manual redeployment from a known-good commit |
| Azure operational complexity | Devil's advocate | Medium | Medium | Use `az webapp up` for simple deploys; avoid Service Connector/Managed Identity until needed |
| Vendor lock-in via Azure-specific auth patterns | Devil's advocate | Low | High | Use standard connection strings (not Service Connector) for MVP; defer Managed Identity |
| Windows runtime patch delays | Unknown unknowns | Medium | Medium | Deploy to Linux App Service plan explicitly |
| Orphaned Azure resources from `az webapp up` | Unknown unknowns | Medium | Low | Name resources with `10x-cards-` prefix; review resource group monthly |
| .NET 9 STS EOL Nov 2026 | Research finding | Certain | Medium | Plan migration to .NET 10 LTS before Nov 2026; monitor release timeline |

## Getting Started

1. **Install the Azure CLI**: download from [learn.microsoft.com/cli/azure/install-azure-cli](https://learn.microsoft.com/cli/azure/install-azure-cli) or `winget install Microsoft.AzureCLI`. Log in with `az login`.

2. **Publish the app**:
   ```
   dotnet publish -c Release -o ./publish
   ```

3. **Deploy to Azure App Service F1 (Linux)**:
   ```
   az webapp up --name 10x-cards --resource-group 10x-cards-rg --runtime "DOTNETCORE:9.0" --sku F1 --os-type Linux --location westeurope
   ```
   This creates the App Service Plan, Resource Group, and app in one command. Use `westeurope` (Netherlands) for lowest latency to Polish users.

4. **Set environment variables**:
   ```
   az webapp config appsettings set --name 10x-cards --resource-group 10x-cards-rg --settings ASPNETCORE_ENVIRONMENT=Production
   ```

5. **Verify**: `az webapp log tail --name 10x-cards --resource-group 10x-cards-rg` to stream logs. Open `https://10x-cards.azurewebsites.net/weatherforecast` to confirm the API responds.

## Out of Scope

The following were not evaluated in this research:
- CI/CD pipeline setup (GitHub Actions workflow)
- Production-scale architecture (multi-region, HA, DR)
- Database selection and schema design
- Email provider selection for magic link auth
- AI/LLM provider selection for flashcard generation
- Azure Key Vault or Managed Identity configuration
