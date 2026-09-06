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
- Invoke the **code-reviewer** skill (`.github/agents/code-reviewer.agent.md`) on the resulting changes
- Confirm acceptance criteria are met
- Surface any issues found and resolve them before handing back

## Rules
- Always complete phases 1-2 before writing any code
- Keep the user informed of phase transitions
- If blocked, explain why and propose alternatives - never silently skip work
- Limit each task to a single concern; avoid batching unrelated changes
