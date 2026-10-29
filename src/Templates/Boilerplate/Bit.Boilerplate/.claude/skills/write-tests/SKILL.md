---
name: write-tests
description: Writes and runs this project's automated tests. It decides which kind of test a change needs - an integration test through the app's own api clients, a UI test in a real browser, a bUnit test that renders a component in memory, or a plain unit test - builds it on the test server and seeded data the project ships, places and names it the way the other tests are, runs the suite and reads a failure. It takes a baseline of what already fails before a change, so a failure afterwards can be told apart. Use when the user asks to write, add, fix or run tests, to cover a change or a bug with a test, or to find out why a test fails, when another skill tests a change, or says "run write-tests".
---

Read `.github/agents/write-tests.agent.md` (relative to the repository root) and follow it exactly.
