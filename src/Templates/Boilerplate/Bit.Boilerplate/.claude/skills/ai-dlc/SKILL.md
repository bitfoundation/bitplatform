---
name: ai-dlc
description: Drives every task that is not a small, obvious edit through the AI-Driven Development Lifecycle, sized to the task - a known bug or a change that stays inside one feature is reproduced, fixed and validated, and anything bigger goes through requirements elaboration, planning and task decomposition, design, implementation, then validation, with explicit user approval before any code is written. Each task, a feature or a hotfix alike, gets a branch of its own off `main`, is tried on the Test environment from that branch, and reaches `main`, Production, through a pull request merged on the user's go-ahead once CI passes and every review comment is answered. Use it for every such task, whether or not the user asks for a structured cycle, and when the user says "run ai-dlc".
---

Read `.github/agents/ai-dlc.agent.md` (relative to the repository root) and follow it exactly.
