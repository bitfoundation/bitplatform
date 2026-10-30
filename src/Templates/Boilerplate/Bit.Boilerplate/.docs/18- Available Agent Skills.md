# Stage 18: Available Agent Skills

Agent Skills are the playbooks the AI coding agent follows for a kind of task, instead of improvising it. This project
ships the ones below, and they work the same in GitHub Copilot, Claude Code, Cursor, Gemini, Codex, Junie, Windsurf and
Antigravity.

Run one with `/<skill-name>`, or just describe the task: the agent knows each skill's one-line description from the
start of a session and loads the skill when a task matches it.

## The skills

| Skill | Use it when | Example |
| --- | --- | --- |
| `ai-dlc` | Any task beyond a small, obvious edit: a feature, a bug fix, work across several files. It takes the task from requirements through plan, design, code and tests to the pull request, with your approval before any code. The agent picks it on its own for such tasks. | `/ai-dlc add a customer feedback feature` |
| `scaffold-entity` | A new CRUD entity whose fields and screens are settled: entity, migration, DTO, mapper, controller, pages, permission and tests. | `/scaffold-entity Product with Name, Price and CategoryId` |
| `write-tests` | A change needs a test, or a test fails and the reason isn't obvious. | `/write-tests cover the order cancellation rules` |
| `run-app` | You want to see a change in the running app, on the web, Windows or Android. | `/run-app and show me the orders page on a phone` |
| `review` | Before a pull request: it checks the branch's changes against the project's conventions and reports, without changing code. | `/review` |
| `bitify-ui` | A page uses raw HTML and CSS that should become Bit.BlazorUI components and theme-aware styles. | `/bitify-ui UserProfile.razor` |
| `localize-strings` | Text written as `Localizer["..."]` literals should move into `AppStrings.resx` and get translated, usually before a push. | `/localize-strings Dashboard.razor` |
| `rebrand` | The app should look like a brand, a site, a Figma file or a mood: colors, fonts, logo, home page and navigation, in light and dark. | `/rebrand to match our new brand guide` |
| `port` | An existing app, in any language or with no source left, should be rebuilt on this project with the same features. | `/port our old Xamarin point of sale` |
| `template-diff` | A newer template version is out, the project moves to another .NET version, or it needs a template feature it was created without. | `/template-diff add Sentry` |
| `deploy` | The project goes live for the first time, or a new environment, server or domain is needed. | `/deploy to my Ubuntu VM` |
| `troubleshoot` | Something is wrong in Test or Production: an error, wrong data, a failed background job. | `/troubleshoot confirmation emails stopped arriving` |

## Where they live

Each skill is one file, `.github/agents/<name>.agent.md`; edit that one. The stubs in `.agents/skills/`,
`.claude/skills/` and `.gemini/commands/` only point each tool at it, and section 7 of `AGENTS.md` lists the skills for
tools that read none of those folders.

To add a skill, write its `.github/agents/<name>.agent.md`, add the three stubs and a row in section 7 of `AGENTS.md`.
The agent decides whether to load a skill from its `description` alone, so say there when to use it.

---

### AI Wiki

Ask your own question [here](https://bitplatform.dev/ask)

---
