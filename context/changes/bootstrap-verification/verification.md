---
bootstrapped_at: 2026-09-11T14:41:54Z
starter_id: dotnet
starter_name: ".NET (ASP.NET Core webapi)"
project_name: 10x-cards
language_family: dotnet
package_manager: dotnet
cwd_strategy: subdir-then-move
bootstrapper_confidence: verified
phase_3_status: ok
audit_command: "dotnet list package --vulnerable --include-transitive"
---

## Hand-off

```yaml
starter_id: dotnet
package_manager: dotnet
project_name: 10x-cards
hints:
  language_family: dotnet
  team_size: solo
  deployment_target: azure-app-service
  ci_provider: github-actions
  ci_default_flow: auto-deploy-on-merge
  bootstrapper_confidence: verified
  path_taken: standard
  quality_override: false
  self_check_answers: null
  has_auth: true
  has_payments: false
  has_realtime: false
  has_ai: true
  has_background_jobs: false
```

Solo developer shipping a web-app MVP in 3 weeks of after-hours work, targeting .NET (C#). The dotnet starter (ASP.NET Core webapi) is the recommended default for (web-app, dotnet) — it provides strong typing, dependency injection, OpenAPI, and Entity Framework integration out of the box, and clears all four agent-friendly quality gates. Auth (passwordless magic link) and AI (flashcard generation from pasted text) are in scope per PRD but require manual setup on top of the starter template — ASP.NET Core has first-class auth middleware, and .NET has mature AI client libraries. Bootstrapper confidence is verified; deployment targets Azure App Service with GitHub Actions auto-deploying on merge to main.

## Pre-scaffold verification

| Signal        | Value                                                       | Severity | Notes                                          |
| ------------- | ----------------------------------------------------------- | -------- | ---------------------------------------------- |
| npm package   | not run                                                     | —        | non-JS starter; no npm package to check        |
| GitHub repo   | not run                                                     | —        | docs_url (learn.microsoft.com) is not a GitHub URL |

No recency signal available for this starter. Proceeded without warning.

## Scaffold log

**Resolved invocation**: `dotnet new webapi -n "10x-cards" -o ".bootstrap-scaffold" --no-restore`
**Strategy**: subdir-then-move
**Exit code**: 0
**Files moved**: 6 (10x-cards.csproj, 10x-cards.http, appsettings.Development.json, appsettings.json, Program.cs, Properties/launchSettings.json)
**Conflicts (.scaffold siblings)**: none
**.gitignore handling**: absent in scaffold
**.bootstrap-scaffold cleanup**: deleted

## Post-scaffold audit

**Tool**: `dotnet list package --vulnerable --include-transitive`
**Summary**: 0 CRITICAL, 0 HIGH, 0 MODERATE, 0 LOW
**Direct vs transitive**: 0/0/0/0 direct of total 0/0/0/0

Clean dependency tree — no known vulnerabilities at scaffold time.

## Hints recorded but not acted on

| Hint                       | Value                              |
| -------------------------- | ---------------------------------- |
| bootstrapper_confidence    | verified                           |
| quality_override           | false                              |
| path_taken                 | standard                           |
| self_check_answers         | null                               |
| team_size                  | solo                               |
| deployment_target          | azure-app-service                  |
| ci_provider                | github-actions                     |
| ci_default_flow            | auto-deploy-on-merge               |
| has_auth                   | true                               |
| has_payments               | false                              |
| has_realtime               | false                              |
| has_ai                     | true                               |
| has_background_jobs        | false                              |

## Next steps

Next: a future skill will set up agent context (CLAUDE.md, AGENTS.md). For now, your project is scaffolded and verified — happy hacking.

Useful manual steps in the meantime:
- `git init` (if you have not already) to start your own repo history.
- Review any `.scaffold` siblings the conflict policy created and decide which version of each file to keep.
- Address audit findings per your project's risk tolerance — the full breakdown is in this log.
