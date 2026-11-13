---
name: troubleshoot
description: Finds and fixes a problem reported in the Test or Production environment, slowness included, such as a website that is slow to load. It works from the code that environment runs - the branch last deployed to Test, `main` for Production - and reads that environment's own state through its dev-mcp server: its errors, logs, traces, data and background jobs, and Sentry or Application Insights when the project reports to one. It reproduces the problem before changing anything, and fixes it through ai-dlc with a test that fails without the fix. Use when the user reports a problem seen in the Test or Production environment - an error, wrong data, a failing background job, an email or notification that never arrived, something broken or slow in the deployed app, a website that is slow to load - or asks to look into errors in Sentry or Application Insights, or into the app's performance, or says "run troubleshoot".
---

Read `.github/agents/troubleshoot.agent.md` (relative to the repository root) and follow it exactly.
