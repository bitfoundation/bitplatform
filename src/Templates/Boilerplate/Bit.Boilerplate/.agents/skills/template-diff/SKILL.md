---
name: template-diff
description: Brings a change of the project template into this project - a newer template release, another .NET version, or one of the template's optional features switched on or off. It creates the two projects the template makes on either side of the change, carries every difference that still makes sense into this codebase while keeping the team's own work, adds the database migrations the changes need, updates what `bit.json` records, and proves the result builds, passes its tests and runs. Use when the user asks to upgrade or update the project to a newer project template, bit platform or .NET version, to bring in the template's latest changes, to add one of the template's optional features this project was created without - such as multi-tenancy, .NET Aspire, Redis, push notifications, Sentry, Application Insights, SignalR and with it the AI chatbot, the offline database or the Admin module - or to remove one it has, or says "run template-diff" or "run upgrade".
---

Read `.github/agents/template-diff.agent.md` (relative to the repository root) and follow it exactly.
