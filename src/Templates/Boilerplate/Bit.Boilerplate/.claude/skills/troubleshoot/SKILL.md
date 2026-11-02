---
name: troubleshoot
description: Finds and fixes a problem reported in the Test or Production environment. It works from the code that environment runs - `develop` for Test, `main` for Production - and reads that environment's own state through its dev-mcp server, its errors through Sentry or Application Insights, and its data, background jobs and logs. It reproduces the problem before changing anything, and fixes it through ai-dlc with a test that fails without the fix. Use when the user reports a problem seen in the Test or Production environment - an error, wrong data, a failing background job, an email or notification that never arrived, something broken in the deployed app - or asks to look into errors in Sentry or Application Insights, or says "run troubleshoot".
---

Read `.github/agents/troubleshoot.agent.md` (relative to the repository root) and follow it exactly.
