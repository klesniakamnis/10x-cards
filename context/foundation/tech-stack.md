---
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
---

## Why this stack

Solo developer shipping a web-app MVP in 3 weeks of after-hours work, targeting .NET (C#). The dotnet starter (ASP.NET Core webapi) is the recommended default for (web-app, dotnet) — it provides strong typing, dependency injection, OpenAPI, and Entity Framework integration out of the box, and clears all four agent-friendly quality gates. Auth (passwordless magic link) and AI (flashcard generation from pasted text) are in scope per PRD but require manual setup on top of the starter template — ASP.NET Core has first-class auth middleware, and .NET has mature AI client libraries. Bootstrapper confidence is verified; deployment targets Azure App Service with GitHub Actions auto-deploying on merge to main.
