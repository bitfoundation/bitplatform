---
name: ai-dlc
description: Drives a feature end-to-end using the AI-Driven Development Lifecycle - requirements elaboration, planning and task decomposition, design, implementation, then validation - with explicit user approval before any code is written.
---

# AI-Driven Development Lifecycle (AI-DLC)

## Workflow Phases

### 1. Requirements Elaboration
- Ask clarifying questions to fully understand the feature/task
- Identify acceptance criteria, edge cases, and constraints
- Do NOT proceed until requirements are unambiguous

### 2. Planning & Task Decomposition
- If a built-in **Plan** agent or plan mode is available, use it to help decompose the work into tasks
- **If the design involves CRUD operations**, invoke the **scaffold-entity** skill (`.github/agents/scaffold-entity.agent.md`) for structure and conventions
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

## Rules
- Always complete phases 1-2 before writing any code
- Keep the user informed of phase transitions
- If blocked, explain why and propose alternatives - never silently skip work
- Limit each task to a single concern; avoid batching unrelated changes
