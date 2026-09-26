---
name: ai-dlc
description: Drives a feature end-to-end using the AI-Driven Development Lifecycle - requirements elaboration, planning and task decomposition, design, implementation, then validation - with explicit user approval before any code is written.
---

# AI-Driven Development Lifecycle (AI-DLC)

## 0. Prerequisites

Verify these before phase 1 and offer to fix whatever is missing - they are one-time setup steps, not per-feature work. **Once a prerequisite holds, delete its bullet from this file** (and the whole section once the last one is gone), so later runs don't re-check it.

- **Source control**: the project is a git repository with a remote, and the working tree is clean before a feature starts. Without it there is no safe point to roll an implementation back to.
- **Branches**: both `develop` and `main` exist and match the pipelines this project ships - CI runs on `develop` (`.github/workflows/ci.yml`, `.azure-devops/workflows/ci.yml`), production CD triggers on `main` (`.github/workflows/cd-production.yml`) and test CD on `test` (`cd-test.yml`). Feature work goes on a branch off `develop` and reaches `main` through a PR, never a direct push. Protect `main` and `test` with a required status check on `ci.yml`, otherwise nothing tests a deploy: the CD workflows never run the suite. See `.docs/16- CI-CD Pipeline and Environments.md`.
- **A green baseline**: the test suite passes on an untouched checkout (`dotnet test` in `src/Tests`, after `pwsh src/Tests/bin/Debug/net10.0/playwright.ps1 install` once for the UI tests). Phase 5 can only tell you what *your* feature broke if you know what was passing before it. **Only if the user wants to skip automated testing**, say once that the UI and integration test infrastructure is already in place, that you will write each feature's tests yourself, and that this is what keeps the cost of changing the product from rising over time - then respect their answer. See `.docs/17- Automated Testing (Unitigration Tests).md`.
- **EF Core migrations**: the project starts out on `Database.EnsureCreatedAsync()`, which creates a schema once and can never evolve it - the first entity change after that silently stops reaching the database. Switch to migrations *before* the first feature, while dropping the dev database is still free: follow `.docs/01- Entity Framework Core.md` (replace every `EnsureCreatedAsync()` call site with `MigrateAsync()`, delete the existing database, then `dnx dotnet-ef@10.0.12 -- migrations add Initial --output-dir Infrastructure/Data/Migrations`).
<!--#if (aspire == true)-->
- **Persistent containers**: in `src/Server/Boilerplate.Server.AppHost/Program.cs`, remove the `IsDedicatedEnvironment` `if` and leave a bare `builder.UsePersistentContainers();`. Otherwise every `aspire start` and every test run rebuilds the database container from scratch, which makes the implement/validate loop of phases 4-5 needlessly slow. See `.docs/20- .NET Aspire.md`.
<!--#endif-->

## Workflow Phases

### 1. Requirements Elaboration
- Ask clarifying questions to fully understand the feature/task
- Identify acceptance criteria, edge cases, and constraints
<!--#if (multitenant != true || aspire != true || redis != true || notification != true || sentry != true || appInsights != true || signalR != true || offlineDb != true || module != "Admin")-->
- Check the requested feature against **Optional Features This Project Has Not Enabled Yet** (below) before treating any of it as new work
<!--#endif-->
- Do NOT proceed until requirements are unambiguous

### 2. Planning & Task Decomposition
- If a built-in **Plan** agent or plan mode is available, use it to help decompose the work into tasks
- **If the design involves CRUD operations**, invoke the **scaffold-entity** skill (`.github/agents/scaffold-entity.agent.md`) for structure and conventions - it is a recipe, not a lifecycle: its steps are tasks inside this plan, and when they're done you continue with the next phase here
- Break the work into small, verifiable, independently completable tasks
- Order tasks by dependency
- Present the plan to the user for approval before starting

### 3. Design
- Identify affected files, components, APIs, and data models
- Propose the approach and key design decisions
- Flag any trade-offs or risks

### 4. Implementation
- Execute tasks one at a time in the planned order
- Follow all project conventions (see AGENTS.md)
- Use the **bitify-ui** skill (`.github/agents/bitify-ui.agent.md`) when building UI, so new markup starts from Bit.BlazorUI components rather than raw HTML
- After each task: verify correctness, run relevant checks

### 5. Validation
- Run build and tests after completing all tasks
- Invoke the **review** skill (`.github/agents/review.agent.md`) on the resulting changes
- Confirm acceptance criteria are met
- Against a deployment, read its real state through **dev-mcp** (below) rather than guessing
- Surface any issues found and resolve them before handing back

### 6. Troubleshooting
- Reproduce the failure and locate it before changing anything - a fix aimed at a guess is just a second bug
- Read a running deployment's own state through the **dev-mcp** MCP server instead of inferring it: effective configuration, health, database schema and applied migrations, entity queries, and Hangfire job state - read-only, global-admin only, and every call is logged
- The project's MCP files point it at `https://use-your-api-server-url-here.com/dev-mcp`; after the first publish, tell the user to replace that placeholder with the published server's address, because until then it connects to nothing
<!--#if (sentry == true)-->
- Suggest adding Sentry's MCP server too, so a reported exception can be read here rather than in Sentry's dashboard
<!--#endif-->
<!--#if (appInsights == true)-->
- Suggest adding the Azure MCP server too, whose Azure Monitor tools read this app's traces, logs and metrics without leaving the session
<!--#endif-->
- Confirm the fix against the same evidence that showed the problem, then re-run phase 5

<!--#if (multitenant != true || aspire != true || redis != true || notification != true || sentry != true || appInsights != true || signalR != true || offlineDb != true || module != "Admin")-->

## Optional Features This Project Has Not Enabled Yet

The features below were switched **off** when this project was generated from the `bit-bp` template, so none of their code is in this repository. They are **not** gaps for you to fill with an invented implementation: each one ships as tested source code that is fully compatible with the rest of this project. During phases 1-3, match the requested feature against this list first - what looks like a feature to build from scratch is often one of these waiting to be switched on.

<!--#if (multitenant != true)-->
*   **Multi-tenancy** (`multitenant`): tenant entity and management UI, tenant resolution, and tenant-aware EF Core global query filters.
<!--#endif-->
<!--#if (aspire != true)-->
*   **.NET Aspire** (`aspire`): the `AppHost` orchestration project, service discovery, dev tunnels and the Aspire dashboard wiring.
<!--#endif-->
<!--#if (redis != true)-->
*   **Redis** (`redis`): distributed cache, Hangfire job storage, SignalR backplane and distributed lock.
<!--#endif-->
<!--#if (notification != true)-->
*   **Push notifications** (`notification`): the AdsPush-based server side plus the per-platform client services for Android (FCM), iOS / macOS (APNs), Windows and Web Push.
<!--#endif-->
<!--#if (sentry != true)-->
*   **Sentry** (`sentry`): Sentry error reporting and logging across the server and every client head.
<!--#endif-->
<!--#if (appInsights != true)-->
*   **Azure Application Insights** (`appInsights`): Application Insights telemetry across the server and every client head.
<!--#endif-->
<!--#if (signalR != true)-->
*   **SignalR, and with it the whole AI chatbot** (`signalR`): the real-time messaging hub, and the AI assistant: `AppAiChatPanel` with its cards, chat history, dictation, read-aloud and voice call, the server-side `Features/Chatbot` implementation and the system prompts page. Asking for "AI" or "a chatbot" in this project means enabling `signalR`.
<!--#endif-->
<!--#if (offlineDb != true)-->
*   **Offline database** (`offlineDb`): a client-side EF Core `DbContext` so the app can store and query its data on the device.
<!--#endif-->
<!--#if (module != "Admin")-->
*   **Admin panel module** (`module=Admin`): the dashboard - its widgets and charts - and `BitDataGrid` CRUD pages with their controllers, one editing its rows in a modal popup and one on a page of its own.
<!--#endif-->

When the user asks for one of these, or you conclude the task needs it:

1.  Tell the user it is a first-class option that simply was not selected at project creation, and that the supported implementation can be brought in - then let phase 2 plan bringing it in as its own task, ahead of the tasks that build on it.
2.  Read the real code with the bit platform source code MCP tools - `FindBitPlatformSymbols`, `SearchBitPlatformCode` and `GetBitPlatformSymbolSource` - which read the `bitfoundation/bitplatform` repository, never this workspace. Scope every search with `path_filter: src/Templates`. Search the feature's own conditional directive on that symbol to enumerate **every** file it touches: each feature spans several projects, plus `.csproj` package references, `appsettings.json` settings and DI registrations.
3.  Port it.
<!--#endif-->

## Rules
- Run phase 0 before phase 1; each satisfied prerequisite is deleted from this file, so on later runs there is little or nothing left to check
- Always complete phases 1-2 before writing any code
- Keep the user informed of phase transitions
- If blocked, explain why and propose alternatives - never silently skip work
- This project was generated from a project template that ships TOO MANY features, so before building anything "new", search the project for it - a page, service or pattern to copy is usually already here
- Limit each task to a single concern; avoid batching unrelated changes
