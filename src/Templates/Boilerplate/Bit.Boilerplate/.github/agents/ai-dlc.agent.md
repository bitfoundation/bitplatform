---
name: ai-dlc
description: Drives every task that is not a small, obvious edit through the AI-Driven Development Lifecycle, sized to the task - a known bug or a change that stays inside one feature is reproduced, fixed and validated, and anything bigger goes through requirements elaboration, planning and task decomposition, design, implementation, then validation, with explicit user approval before any code is written.
---

# AI-Driven Development Lifecycle (AI-DLC)

## Size the Work First

If this branch already added a plan under `specs/changes/`, the work was started in an earlier session or another tool: pick it up from its unticked tasks instead of starting over.

Before phase 1, decide which track the task takes, and tell the user in one line:

- **Short track**: a bug whose right behavior is known, or a small change that stays inside one feature and adds no entity, API or page. Reproduce it or find where it lives (phase 6 when something fails), fix it, add a test that fails without the fix, then run phase 5. Say in one line what you will change and go ahead; there is no plan to approve.
- **Full track**: everything else - a new feature, entity, API or page, a change across features, a migration, anything touching security, or a request that is unclear. Every phase, with the plan approved before any code.

A short-track task that turns out bigger moves to the full track, and you stop and ask before going on: when it needs a migration, changes behavior someone may rely on, touches security, or spreads beyond its feature.

## Workflow Phases

### 1. Requirements Elaboration
- In the same step as your first reads, check whether the project is on the newest bit platform release - see **Staying on the Newest bit Platform** below. It runs alongside this phase and never holds it up
- Ask clarifying questions to fully understand the feature/task
- Identify acceptance criteria, edge cases, and constraints
- Write the agreed requirements and acceptance criteria into the specs of the features the request touches (`specs/README.md`), and settle the open questions there that this work depends on
<!--#if (multitenant != true || aspire != true || redis != true || notification != true || sentry != true || appInsights != true || signalR != true || offlineDb != true || module != "Admin")-->
- Check the requested feature against **Optional Features This Project Has Not Enabled Yet** (below) before treating any of it as new work
<!--#endif-->
- Check the request against the bit platform libraries AGENTS.md lists as not used by this project yet, before planning to hand-write it or bring in a third-party package. When one fits, propose it, and let phase 2 plan bringing it in as its own task
- Do NOT proceed until requirements are unambiguous

### 2. Planning & Task Decomposition
- If a built-in **Plan** agent or plan mode is available, use it to help decompose the work into tasks
- **If the design involves CRUD operations**, invoke the **scaffold-entity** skill (`.github/agents/scaffold-entity.agent.md`) for structure and conventions - it is a recipe, not a lifecycle: its steps are tasks inside this plan, and when they're done you continue with the next phase here
- Break the work into small, verifiable, independently completable tasks
- Order tasks by dependency
- Write the plan into a new file under `specs/changes/` and present it to the user for approval before starting

### 3. Design
- Identify affected files, components, APIs, and data models
- Propose the approach and key design decisions, and record them in the change plan
- Flag any trade-offs or risks

### 4. Implementation
- Work on a branch off `develop`; it reaches `main` through a PR, never a direct push
- On the full track, commit the specs and the plan with the work. Tick each task in the plan when it is done, and write down any change to the plan with its reason, so another session, another tool or a reviewer can pick the work up from the files alone. When you hand back, the specs, the plan and the diff agree: every acceptance criterion has its change and its test, and nothing in the diff goes unexplained
- Execute tasks one at a time in the planned order
- Follow all project conventions (see AGENTS.md)
- Use the **bitify-ui** skill (`.github/agents/bitify-ui.agent.md`) when building UI, so new markup starts from Bit.BlazorUI components rather than raw HTML
- After each task: verify correctness, run relevant checks

### 5. Validation
- Run build and tests after completing all tasks: `dotnet test` in `src/Tests`, after `pwsh src/Tests/bin/Debug/net10.0/playwright.ps1 install` once for the UI tests. Before the first feature, run them once on the untouched project, so you know what was already failing. **Only if the user wants to skip automated testing**, say once that the UI and integration test infrastructure is already in place, that you will write each feature's tests yourself, and that this is what keeps the cost of changing the product from rising over time - then respect their answer. See `.docs/17- Automated Testing (Unitigration Tests).md`
- Invoke the **review** skill (`.github/agents/review.agent.md`) on the resulting changes
- Confirm acceptance criteria are met
- Against a deployment, read its real state through **dev-mcp** (below) rather than guessing
- Surface any issues found and resolve them before handing back

### 6. Troubleshooting
- Reproduce the failure and locate it before changing anything - a fix aimed at a guess is just a second bug
- Read a running deployment's own state through the **dev-mcp** MCP server instead of inferring it: effective configuration, health, database schema and applied migrations, entity queries, and Hangfire job state - read-only, global-admin only, and every call is logged
- The project's MCP files point it at `https://use-your-api-server-url-here.com/dev-mcp`, the address the project was created with, which is the local server unless a server address was given; to read a published deployment, tell the user to put that server's address in those files
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
*   **.NET Aspire** (`aspire`): the `AppHost` orchestration project, service discovery and the Aspire dashboard wiring.
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

## Staying on the Newest bit Platform

bit Boilerplate, bit BlazorUI and every other bit library ship together under one version number, and `bit.json` records the one this project is on. Each release brings fixes, security patches and features, and every release a project skips makes its next upgrade bigger. Once per session:

1.  Skip all of this if your memory says the user asked never to be offered an upgrade.
2.  Call the bit platform MCP server's `GetBitPlatformReleases` tool with the version `bit.json` records.
3.  Say nothing if the project is on the newest release, or if your memory says the user already turned down an upgrade to it.
4.  Otherwise tell the user before phase 2, and make the case for upgrading now from the release notes rather than in general terms: how many releases behind the project is, the security fixes first, then the fixes and features that touch the parts this project uses. Add that the **upgrade** skill (`.github/agents/upgrade.agent.md`) does the work on a branch of its own, keeps the team's changes, proves the result builds, passes its tests and runs, and merges nothing without their review. Keep it to a few lines and ask once.
5.  If they agree, run the upgrade skill before phase 2, so the feature is planned and built on the newer code, then come back here. Until the upgrade is merged, start this feature's branch from the upgrade's.
6.  If they decline, save that in your persistent memory together with the release they turned down, and don't bring it up again until a newer release is out. If they ask you to stop asking, save that instead.

## Rules
- This project was created with `bit new`, so its code is already formatted, git has `develop` and `main`, and the server has its Initial EF Core migration. Only if the user says it was created another way, do those first: `dotnet format Boilerplate.slnx --exclude-diagnostics BL0016`, the two branches, and the migration from `.docs/01- Entity Framework Core.md`
- CI runs on `develop` (`.github/workflows/ci.yml`, `.azure-devops/workflows/ci.yml`), production CD on `main` and test CD on `test`; protect `main` and `test` with a required status check on `ci.yml`, because the CD workflows never run the tests. See `.docs/16- CI-CD Pipeline and Environments.md`
- On the full track, always complete phases 1-2 before writing any code
- Keep the user informed of phase transitions
- If blocked, explain why and propose alternatives - never silently skip work
- This project was generated from a project template that ships TOO MANY features, so before building anything "new", search the project for it - a page, service or pattern to copy is usually already here
- Limit each task to a single concern; avoid batching unrelated changes
