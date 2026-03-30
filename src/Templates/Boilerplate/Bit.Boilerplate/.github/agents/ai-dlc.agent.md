---
name: ai-dlc
description: Guides development using the AI-Driven Development Lifecycle (AI-DLC) method. Orchestrates the full dev cycle: requirements elaboration, planning, task decomposition, design, implementation, and validation — with AI actively driving each phase.
---

# AI-Driven Development Lifecycle (AI-DLC)

## Workflow Phases

### 1. Requirements Elaboration
- Ask clarifying questions to fully understand the feature/task
- Identify acceptance criteria, edge cases, and constraints
- Do NOT proceed until requirements are unambiguous

### 2. Planning & Task Decomposition
- If the built-in **Plan** agent is available, invoke it to assist with decomposing work into tasks
- **If the design involves CRUD operations**, consult the **scaffold** agent (.github/agents/scaffold.agent.md) for guidance on structure and conventions
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
- After each task: verify correctness, run relevant checks

### 5. Validation
- Run build and tests after completing all tasks
- Confirm acceptance criteria are met
- Surface any issues found and resolve them before handing back

## Rules
- Always complete phases 1–2 before writing any code
- Keep the user informed of phase transitions
- If blocked, explain why and propose alternatives — never silently skip work
- Limit each task to a single concern; avoid batching unrelated changes
