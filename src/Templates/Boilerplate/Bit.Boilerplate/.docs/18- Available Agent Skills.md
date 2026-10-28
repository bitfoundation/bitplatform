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
- Ensures light/dark theme compatibility

**Example usage**: `/bitify-ui UserProfile.razor`

---

### 4. Review (`review`)

**Canonical file**: `.github/agents/review.agent.md`

**What it does**: Reviews code changes against this project's conventions and reports findings. It never modifies code.

**When to use it**: Before opening a PR, or any time you want a convention check on a diff.

**Key capabilities**:
- Checks Bit.BlazorUI usage, `WrapHandled`, enhanced lifecycle methods, code-behind and scoped SCSS
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
- Phase 1 - Requirements elaboration, with clarifying questions until requirements are unambiguous
- Phase 2 - Planning and task decomposition, presented for approval
- Phase 3 - Design of affected files, components, APIs and data models
- Phase 4 - Implementation, one task at a time
- On the full track, writes the agreed requirements into the feature specs in `specs/` and the approved plan into `specs/changes/` - design, a task checklist and plan changes, kept current - so the work can be resumed in another session or tool and reviewed with the code, and picks the work up from there when the branch already has a plan
- Phase 5 - Validation via build, tests and the `review` skill
- Checks every request against the optional template features this project did not enable, so they are ported from `bitfoundation/bitplatform` instead of reinvented
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
- Prefers bit BlazorUI's public API; when a look needs a hack, keeps it in one partial and reports it with a ready-to-file upstream issue
- Verifies a state matrix - focus, hover, selected, disabled, invalid, overlays, RTL, phone and tablet, both schemes - with measured contrast, a keyboard focus walk and a token ledger, then runs the tests

Everything lives in the one skill file; it brings no scripts or tools of its own.

**Example usage**: `/rebrand Make the app look like our new brand. Figma: <link to the design file>. Feel: calm, precise, editorial.`

---

### 7. Upgrade (`upgrade`)

**Canonical file**: `.github/agents/upgrade.agent.md`

**What it does**: Brings a newer bit Boilerplate version into the project. `bit diff` creates the project with the version it came from and with the newer one into one git repository in bit's own folder in the user's home, so the second commit is exactly what the template changed for a project with this name and these options. The skill carries each of those changes into the codebase, keeping what the team changed, adds the database migrations the changes need, and updates the version `bit.json` and the MCP server configurations record.

**When to use it**: When a newer bit Boilerplate is out and the project should get its fixes, features and dependency updates.

**Key capabilities**:
- Reads the project's version and options from `bit.json`, and checks them against the code before diffing
- Works on a branch of its own, so the upgrade is reviewed and merged as one change and never mixes with other work
- Treats the old version as the common ancestor: takes files the team never changed, merges files both changed, and leaves out what no longer has anything to attach to
- Knows which differences come from generating a project (ports, GUIDs, dates, the seeded accounts' password, the app certificate) rather than from the upgrade
- Adds a migration for every database context whose model changed, instead of editing existing ones
- Asks only about decisions that change how the app behaves
- Proves the result: build, formatting, tests, and the app running

**Example usage**: `/upgrade` or `/upgrade to the newest prerelease`

---

### AI Wiki

Ask your own question [here](https://bitplatform.dev/ask)

---
