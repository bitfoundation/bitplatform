# Stage 18: Available Agent Skills

This project ships several specialized **Agent Skills** designed to help you with specific development tasks. Each skill is carefully crafted to follow the project's conventions and best practices, and each one works across every major AI coding tool - GitHub Copilot (VS Code, Visual Studio, github.com, CLI), Claude Code, Cursor, Gemini CLI, Gemini Code Assist, Google Antigravity, OpenAI Codex, JetBrains Junie and Windsurf - without duplicating their content per tool.

## How skills work

An [Agent Skill](https://agentskills.io) is a folder containing a `SKILL.md` file with YAML frontmatter (`name` and `description`) followed by markdown instructions. Agents load skills through **progressive disclosure**:

1. Only the `description` of each skill is preloaded at session start (roughly 100 tokens each), so the agent knows *that* the skill exists and *when* it applies.
2. The body is read only when the agent decides the skill is relevant, or when you invoke it explicitly with `/<skill-name>`.

That means adding skills costs almost nothing in context until one is actually needed - the opposite of putting the same instructions in `AGENTS.md`, which loads in full on every single prompt.

## Where the files live

The full instructions live once, in `.github/agents/`, and every other location is a thin pointer at them:

| Path | Purpose |
| --- | --- |
| `.github/agents/<name>.agent.md` | **The canonical file with the actual body. Edit this one.** Listed directly in Visual Studio's agent picker and `@` menu (and as an `@<name>` agent in VS Code), which show agents but not skills. |
| `.agents/skills/<name>/SKILL.md` | Discovery stub for the tool-neutral Agent Skills location, read natively by GitHub Copilot (VS Code, Visual Studio, github.com coding agent, Copilot CLI, JetBrains agent mode), Cursor, OpenAI Codex, JetBrains Junie, Windsurf, Google Antigravity and Gemini CLI. It mirrors the canonical frontmatter and delegates to the canonical file - keep only the frontmatter in sync. |
| `.claude/skills/<name>/SKILL.md` | Bridge stub for Claude Code, which only discovers skills under `.claude/skills/`. Same delegation rule. |
| `.gemini/commands/<name>.toml` | Command shim that guarantees `/<skill-name>` in Gemini CLI and Gemini Code Assist agent mode, where Agent Skills discovery may not be enabled yet. It injects the canonical file via `@{...}`. |

The instructions themselves follow the same single-source pattern: every tool that reads `AGENTS.md` natively (Copilot, Cursor, Codex, Junie, Windsurf, Antigravity) gets it directly, while `CLAUDE.md` and `GEMINI.md` are two-line entry points that inline it with an `@AGENTS.md` import for Claude Code and Gemini. Section 7 of `AGENTS.md` indexes the skills for anything that reads none of the skill locations.

To add a new skill: create `.github/agents/<your-skill>.agent.md` with the full instructions, add the matching `.agents` and `.claude` stubs and the `.gemini` shim, and add a row to the `AGENTS.md` section 7 index. The `description` is the most important field - write it as *"...Use when the user asks to X, Y, or says Z"*, because that sentence is the only thing the agent sees when deciding whether to load the skill.

---

## Available Skills

### 1. Scaffold Entity (`scaffold-entity`)

**Canonical file**: `.github/agents/scaffold-entity.agent.md`

**What it does**: Generates a complete CRUD (Create, Read, Update, Delete) implementation for a new entity in your project, including all necessary layers from database to UI.

**When to use it**: When a new data entity's fields, rules and screens are settled, usually as a task of an `ai-dlc` plan. A new feature whose shape is still open starts with `ai-dlc`, which calls this skill once the entity is pinned down.

**Key capabilities**:
- Starts from the CRUD entity the project ships, or from the Admin module's when it ships none, and asks whether rows are edited in a modal or on a page of their own
- Creates Entity Type Configuration for EF Core
- Generates DTO (Data Transfer Object) with validation attributes, or separate request and response DTOs when what the client reads and writes differ a lot
- Creates Mapper using Mapperly for high-performance object mapping, ignoring on the way in every property the client must not write
- Generates API Controller with OData support
- Creates IAppController Interface for strongly-typed HTTP client
- Adds `.resx` keys only for validation attributes; other new text goes through the `Localizer["..."]` literal indexer
- Creates Data Grid Page for listing records
- Creates Add/Edit Modal or Page for creating and updating records
- Adds a permission to `AppFeatures` when the entity needs its own, and tests for the rules it adds
- Integrates with navigation (PageUrls.cs, NavBar, MainLayout items)
- Updates AppJsonContext for JSON serialization
- Generates EF Core migration

**Example usage**: `/scaffold-entity Product with Name, Description, Price and CategoryId properties`

---

### 2. Localize Strings (`localize-strings`)

**Canonical file**: `.github/agents/localize-strings.agent.md`

**What it does**: Identifies hardcoded strings in your code and moves them to resource files (.resx) for proper localization support.

**When to use it**: When you explicitly want hardcoded user-facing text in your Blazor components, pages, or controllers moved into `AppStrings.resx` - for example as a dedicated translation pass before a release - and when the changes are about to be pushed. (Day-to-day, new text stays in the `Localizer["..."]` literal indexer per `AGENTS.md` section 5, because `.resx` edits break hot reload.)

**Key capabilities**:
- Identifies hardcoded user-facing strings in selected code
- Adds new entries to `AppStrings.resx` with appropriate resource keys
- Generates strongly-typed resource classes
- Updates code to use `IStringLocalizer<AppStrings>` pattern
- Uses `nameof(AppStrings.ResourceKey)` for type-safe resource access
- Preserves string formatting with placeholders (e.g., `{0}`, `{1}`)
- Follows naming conventions with descriptive resource keys
- Fills in the new keys for every other language in `Bit.ResxTranslator.json` with `bit translate`

**What it won't move**:
- CSS class names or IDs
- Configuration keys
- API endpoints or URLs
- Technical constants (file extensions, mime types)
- Log messages

**Example usage**: `/localize-strings Dashboard.razor`

---

### 3. Bitify UI (`bitify-ui`)

**Canonical file**: `.github/agents/bitify-ui.agent.md`

**What it does**: Modernizes your Blazor pages by replacing standard HTML elements and custom CSS with Bit.BlazorUI components and theme-aware styling.

**When to use it**: When you have pages using generic HTML elements (like `<button>`, `<input>`, `<div>`) and want to upgrade them to use the Bit.BlazorUI component library for consistency, better UX, and theme support.

**Key capabilities**:
- Analyzes current HTML markup and identifies replaceable elements
- Uses the bit BlazorUI MCP tools (`SearchBitBlazorUI`, `GetBitBlazorUIComponent`, `GetBitBlazorUIComponentExamples`, `GetBitBlazorUIType`, `GetBitBlazorUIThemingGuide`, `FindBitBlazorUIIcons`) to verify component names, parameters, enum values and icon names instead of guessing
- Replaces HTML elements with proper Bit.BlazorUI components:
  - `<button>` → `BitButton`, `BitActionButton`
  - `<input type="text">` → `BitTextField`
  - `<select>` → `BitDropdown`
- Converts custom CSS to theme-aware styling using `$bit-color-*` variables
- Uses `::deep` selectors for proper component styling
- Updates event handlers to use `WrapHandled` pattern
- Keeps every attribute that carries behavior - ids and classes that scripts or tests use, `autocomplete`, `aria-*` - working on its replacement
- Ensures light/dark theme compatibility
- Uses the page in the running app afterwards - every replaced input, dropdown, dialog and form, in both themes and at phone width - since a binding broken in the swap shows only there
- When bit BlazorUI has no component for an element, keeps the hand-written one, offers to write and file the issue for the missing component, and records it in `specs/workarounds.md`

**Example usage**: `/bitify-ui UserProfile.razor`

---

### 4. Review (`review`)

**Canonical file**: `.github/agents/review.agent.md`

**What it does**: Reviews code changes against this project's conventions and reports findings. It never modifies code.

**When to use it**: Before opening a PR, or any time you want a convention check on a diff.

**Key capabilities**:
- Reviews the branch's whole diff against `develop` from several angles - the checklist, what removed lines used to guarantee, and the callers of what changed - and checks each finding again before it reports it, most severe first
- Checks Bit.BlazorUI usage, `WrapHandled`, enhanced lifecycle methods, code-behind and scoped SCSS
- Checks the common C# and Blazor pitfalls: `async void`, cancellation tokens that go nowhere, local times, culture-sensitive parsing, undisposed subscriptions, missing `@key`, N+1 queries
- Flags code that rewrites what the project already ships, and fixes made at the wrong level
- Checks theming (`BitColor`, `BitCss.Class`, `BitCss.Var`, `$bit-color-*`, `::deep`)
- Checks API controllers (`AppControllerBase`, `IAppController`, `[EnableQuery]`, Mapperly `Project()`, `long Version`)
- Checks DTOs (`[DtoResourceType]`, `nameof(AppStrings.X)` validation, `AppJsonContext` registration)
- Checks nullability, `async/await`, structured logging, secrets and localization
- Checks that `[mirror]` counterparts got the same change and that new features come with tests
- Flags security gaps the change leaves unaddressed: authorization, ownership checks, server-side validation, sanitizing, and sensitive data in logs
- Ignores anything `.editorconfig` already handles

It carries `context: fork` in its frontmatter, so tools that support forked skill contexts (Claude Code, VS Code Copilot) run it in a dedicated subagent context instead of filling your main conversation; every other tool simply ignores that field and runs it inline.

**Example usage**: `/review` on the current branch's changes

---

### 5. AI-DLC (`ai-dlc`)

**Canonical file**: `.github/agents/ai-dlc.agent.md`

**What it does**: Drives a task end-to-end through the AI-Driven Development Lifecycle, with approval gates before any code is written.

**When to use it**: For every task that is not a small, obvious edit: building or changing a feature, fixing a reported bug even when its cause turns out obvious, work spanning several files. AGENTS.md makes it the agent's default for such tasks, so it runs whether or not you ask for it; `/ai-dlc` starts it explicitly.

**Key capabilities**:
- Sizes the work first: a known bug or a change inside one feature takes a short track - reproduce, fix, a test that fails without the fix, validation - with no plan to approve, and moves to the full track when it turns out bigger
- Picks the branch first: unrelated work never lands on the branch you are on, and before a new branch leaves `develop` it names the unmerged branches this work needs and proposes settling them
- Phase 1 - Requirements elaboration, with clarifying questions until requirements are unambiguous
- Phase 2 - Planning and task decomposition, presented for approval
- Phase 3 - Design of affected files, components, APIs and data models, and the change management a change needs when it meets data or apps already in use: a required column on a table with rows gets a default or a backfill, and an API the mobile app in users' hands still calls keeps its old contract or makes older apps update
- Phase 4 - Implementation, one task at a time
- On the full track, writes the agreed requirements into the feature specs in `specs/` and the approved plan into `specs/changes/` - design, a task checklist and plan changes, kept current - so the work can be resumed in another session or tool and reviewed with the code, and picks the work up from there when the branch already has a plan
- Phase 5 - Validation via build, the tests `write-tests` describes, the `review` skill, and a walk through the change's acceptance criteria in the running app
- Phase 7 - Delivery: settles what the change leaves open (literals into `.resx`, specs and plan in step with the diff), opens a PR into `develop` whose description comes from every commit on the branch, with a test plan that maps each acceptance criterion to its test and links the spec and the plan, and merges it once CI passes unless you ask to review it first; `develop` deploys Test and `main` Production, and a release into `main` waits for your go-ahead
- Researches rules from outside the codebase (taxes, regulations, a provider's API) from primary sources, and hands back every claim with its evidence, listing what it didn't verify
- Checks every request against the optional template features this project did not enable, and brings one in with `template-diff` instead of reinventing it
- Proposes the bit libraries this project doesn't use yet, which `AGENTS.md` lists with their MCP servers, when a request falls in their area
- Delegates to `scaffold-entity` where it applies
- Checks, alongside phase 1, whether the project is on the newest bit platform release, makes the case for upgrading from the release notes, and remembers a no instead of asking again every session

**Example usage**: `/ai-dlc add a customer feedback feature`

---

### 6. Rebrand (`rebrand`)

**Canonical file**: `.github/agents/rebrand.agent.md`

**What it does**: Re-skins the whole app to look and feel like a design reference - color roles, neutrals, elevation, shape, focus, typography, density and motion for light and dark, plus fonts, the logo, the home page, the navigation and the not-found page - without changing what already works, the existing text or the identity flows, and proves the result state by state.

**When to use it**: When a designer or product owner wants the app to look like a given product, brand, design system or mockup - whether the reference is a live website, a Figma file, screenshots, brand guidelines, a token file or only a described mood.

**Key capabilities**:
- Turns any reference into a written spec - color by role, typography, shape, elevation, density, motion, states and imagery, each value with its source
- Settles the base preset first: the one the project was created with, or another bit BlazorUI preset that sits closer to the reference
- Puts every value into one `Styles/_brand.scss` that re-values that preset's light and dark themes in place, with the palette derived the way bit BlazorUI builds its own
- Decides every interactive state (selected, hovered, focused) and what the browser paints: autofill, text selection and native widgets
- Carries the brand into the native chrome color, the accent picker, logo, identity art, loaders, app icons and emails
- Gives Home the reference's hero, and the nav bar and nav panel its navigation features, through one new menu item named in the reference's own vocabulary that opens the re-skinned not-found page
- Prefers bit BlazorUI's public API; when a look needs a hack, keeps it in one partial, records it in `specs/workarounds.md` and reports it with a ready-to-file upstream issue
- Uses whatever the reference has - its logo, fonts and artwork - and lists in the report what each one's license needs before the app ships
- Verifies a state matrix - focus, hover, selected, disabled, invalid, overlays, RTL, phone and tablet, both schemes - with measured contrast, a keyboard focus walk and a token ledger, then runs the tests

Everything lives in the one skill file; it brings no scripts or tools of its own.

**Example usage**: `/rebrand Make the app look like our new brand. Figma: <link to the design file>. Feel: calm, precise, editorial.`

---

### 7. Template Diff (`template-diff`)

**Canonical file**: `.github/agents/template-diff.agent.md`

**What it does**: Brings a change of the project template into the project: a newer template version, another .NET version, or one of the template's optional features switched on or off. Each one is the difference between two projects the template creates - `bit diff` creates them for a newer version or another .NET version, and for a feature the skill creates them with `bit new`, the feature off in one and on in the other - and the skill carries every difference that still applies into the codebase, keeping what the team changed, adds the database migrations the changes need, and updates what `bit.json` and the MCP server configurations record.

**When to use it**: When a newer template version is out and the project should get its fixes, features and dependency updates, when the project moves to another .NET version, or when it needs a feature it was created without, or should drop one it has.

**Key capabilities**:
- Reads the project's version and options from `bit.json`, and checks them against the code before diffing
- Works on a branch of its own, so the change is reviewed and merged as one and never mixes with other work
- Treats the project the change starts from as the common ancestor: takes files the team never changed, merges files both changed, and leaves out what no longer has anything to attach to
- Knows which differences come from generating a project (ports, GUIDs, dates, the seeded accounts' password, the app certificate) rather than from the change
- Adds a migration for every database context whose model changed, instead of editing existing ones
- Records which tests already failed before the change, so a failure afterwards is told apart from one it brought in
- Asks only about decisions that change how the app behaves
- Proves the result: build, formatting, tests, and the app running

**Example usage**: `/template-diff` for the newest version, `/template-diff move to .NET 11` or `/template-diff add Sentry`

---

### 8. Port (`port`)

**Canonical file**: `.github/agents/port.agent.md`

**What it does**: Brings an existing application's functionality into this project - its features, business rules, calculations, reports, permissions and workflows, with their UI and UX - rebuilt on this project's architecture instead of copied, and proves feature parity in the running app. `rebrand` decides how the result looks; `port` decides what it does.

**When to use it**: When an existing product - an old .NET app, a site or app written in another language, a native mobile app, or one whose source code is gone - should be rebuilt on this project.

**Key capabilities**:
- Takes any source: code in any language, the running app, screenshots, videos, manuals or a database. When the source isn't C#, or there is no source code, it starts from a reverse-engineering pass - an installed reverse-engineering skill's, or its own by the same rules
- Turns the source into one spec per feature and a parity table in the plan: each behavior, its source reference, where it lives now, its test and its status
- Recommends how the source maps onto the template, with a reason for each: the template's identity when the source's is a subset of it, multi-tenancy brought in first when the source is multi-tenant and the project isn't, further apps as client and host projects of their own, the optional template features the source needs, and whether the source's data moves over
- Replaces the source's heavy client packages with bit platform equivalents or light libraries, and moves as much of their work as it can to the server
- Rewrites JavaScript in C# with Bit.Butil wherever C# can do the same, and keeps it only where it works the DOM heavily, such as canvas and SVG drawing
- Rewrites what a native SDK does in cross-platform C# or JavaScript when that is simple, so an app that ran only on Android can come out on every platform, and keeps the SDKs only native code can replace, which the MAUI app can use
- Copies none of the source's code but the JavaScript it keeps, and gives the project new secrets instead of the source's unless one can't be reissued
- Runs the work through `ai-dlc`, hands the look of each app to `rebrand`, and walks every row of the parity table in the running app, side by side with the source where it still runs

**Example usage**: `/port our old Xamarin point of sale in ../Tradeer - the till and the back office`

---

### 9. Deploy (`deploy`)

**Canonical file**: `.github/agents/deploy.agent.md`

**What it does**: Puts the project on your own Linux VM as two environments that deploy themselves - Test from `develop` and Production from `main` - and walks you through every step, doing the ones it has access to.

**When to use it**: When the project should go live for the first time, or a new environment, server or domain should be set up.

**Key capabilities**:
- Sets up Docker and a self-hosted GitHub Actions runner on the VM, one runner per environment, serving only the deploy jobs of a private repository - over SSH it sets up itself when you aren't technical
- Finds the external services this project depends on - database, Redis, file storage (Cloudflare R2 recommended for S3), email, AI and speech models, Sentry, Application Insights, push notifications and the rest - and helps you get each one, per environment
- Creates the GitHub environments and stores their variables and secrets with `gh`, from a git-ignored file it deletes afterwards, so no secret passes through the chat
- Works with the template's CD, which builds on GitHub and runs on the VM: GitHub-hosted runners build the images into GitHub Container Registry, and the VM's runner only pulls them, migrates the database and brings them up on Docker Compose, failing unless every container is healthy and the public address answers
- Settles how the web app is served from what the business needs: prerendering only when search engines must find its pages, and then one cache - Cloudflare's edge or the server's output cache - never both
- Serves the app on your own domain through a Cloudflare Tunnel on Cloudflare's free plan, so the VM keeps every inbound port closed, and names a production-ready alternative if you don't want Cloudflare
- Keeps SSH reachable without an open port, through the tunnel behind Cloudflare Access with a second factor and a browser terminal, and closes port 22 only once that route works, with the provider's console as the way back in
- Offers to add .NET Aspire first when the project was created without it, since the template's CD is built on it
- Checks the first Test deployment at its public address and asks its dev-mcp for the health of every service, settling each one that reports it isn't configured, then hands back what runs where, where the secrets live, what is backed up and how to roll back

**Example usage**: `/deploy to my Ubuntu VM, with app.contoso.com for Production and test.contoso.com for Test`

---

### 10. Run App (`run-app`)

**Canonical file**: `.github/agents/run-app.agent.md`

**What it does**: Starts the app and uses it the way a user would, to see a change working, then stops it cleanly. `ai-dlc`, `bitify-ui`, `scaffold-entity`, `rebrand`, `port` and `template-diff` use it wherever they check the running app.

**When to use it**: When you want to try or see a change in the running app, or check it on the Windows or Android app. In Claude Code, the built-in `/run` finds this skill and follows it.

**Key capabilities**:
- Reuses the running app or starts it, and waits until a page answers instead of sleeping
- Says what hot reload brings to the open page, what needs a restart or a rebuild, and where to look when an edit doesn't show
- Signs in with the seeded account, and reads the codes and links the app sends from the development inbox
- Drives the web app with the Playwright MCP tools the way a user does, in both schemes, at phone width and in RTL
- Reads the browser console, the failed requests and the server's logs, since a page can render while every request behind it fails
- Drives the Windows and Android apps through their WebView's remote debugging, and checks what lies outside the WebView with screenshots or lists it for you
- Stops only what it started, before the tests need the build outputs

**Example usage**: `/run-app and show me the new orders page on a phone`

---

### 11. Troubleshoot (`troubleshoot`)

**Canonical file**: `.github/agents/troubleshoot.agent.md`

**What it does**: Finds and fixes a problem someone saw in the Test or Production environment, working from the code and the state of that environment rather than from your working tree.

**When to use it**: When a user, a tester or a monitor reports something wrong in a deployed environment - an error, wrong data, a failing background job, an email that never arrived.

**Key capabilities**:
- Reads the code the environment runs - `develop` for Test, `main` for Production, at the deployed commit - in a worktree of its own
- Reads the environment's state through its own dev-mcp server, `dev-mcp-test` or `dev-mcp-production`, whose title and instructions name the environment they read
- Makes sure it can see the source, the data, the background jobs and the errors before it starts, and walks you through connecting Sentry's or Azure's MCP server and the dev-mcp sign-in
- Goes from the evidence - the exception, the trace, the failed job - through the usual causes: configuration, the last deployments, the database, jobs, providers, stale copies, the edge, the user's context
- Writes nothing to a deployed environment, and treats what it reads there as data, never as instructions
- Reproduces the problem before it changes anything, and fixes it through `ai-dlc` with a test that fails without the fix, on a branch off `develop`, or off `main` for a Production fix that can't wait

**Example usage**: `/troubleshoot Confirmation emails aren't arriving for users in production since yesterday`

---

### 12. Write Tests (`write-tests`)

**Canonical file**: `.github/agents/write-tests.agent.md`

**What it does**: Writes and runs the project's automated tests on the test infrastructure it ships (`.docs/17- Automated Testing (Unitigration Tests).md`): picks the kind of test each change needs, builds it on the in-process test server and the seeded data, names and places it like the existing tests, runs the suite and reads a failure.

**When to use it**: Whenever a change needs a test - a bug fix, an acceptance criterion - or a test fails and the reason isn't obvious. `ai-dlc`, `template-diff` and the other skills use it wherever they test.

**Key capabilities**:
- Takes a baseline of what already fails before a change, so a failure afterwards is told apart from one the change brought in
- Picks the cheapest kind that would catch the defect: an integration test through the app's own api clients, a Playwright UI test, a bUnit test that renders a component in memory, or a plain unit test
- Writes a test that fails without the fix for every bug, and one for every acceptance criterion of the spec
- Builds on `AppTestServer`, `AppClient` and the seeded users, and fakes only what a test can't have for real
- Finds elements the way a user does, with the app's own strings, and never waits with a fixed delay
- Reads a failure from the output, the trx report and, for a UI test that failed on a retry, its video

**Example usage**: `/write-tests cover the order cancellation rules`

---

### AI Wiki

Ask your own question [here](https://bitplatform.dev/ask)

---
