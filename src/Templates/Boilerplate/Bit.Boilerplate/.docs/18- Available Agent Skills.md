# Stage 18: Available Agent Skills

This project ships several specialized **Agent Skills** designed to help you with specific development tasks. Each skill is carefully crafted to follow the project's conventions and best practices, and each one works across every major AI coding tool - GitHub Copilot (VS Code, Visual Studio, github.com, CLI), Claude Code, Cursor, Gemini CLI, Gemini Code Assist, Google Antigravity, OpenAI Codex, JetBrains Junie and Windsurf - without duplicating their content per tool.

## How skills work

An [Agent Skill](https://agentskills.io) is a folder containing a `SKILL.md` file with YAML frontmatter (`name` and `description`) followed by markdown instructions. Agents load skills through **progressive disclosure**:

1. Only the `description` of each skill is preloaded at session start (roughly 100 tokens each), so the agent knows *that* the skill exists and *when* it applies.
2. The body is read only when the agent decides the skill is relevant, or when you invoke it explicitly with `/<skill-name>`.

That means adding skills costs almost nothing in context until one is actually needed - the opposite of putting the same instructions in `AGENTS.md`, which loads in full on every single prompt.

## Where the files live

`.agents/skills/` is the tool-neutral location the Agent Skills standard converged on, and almost every tool reads it natively:

| Path | Purpose |
| --- | --- |
| `.agents/skills/<name>/SKILL.md` | **The canonical file. Edit this one.** Read natively by GitHub Copilot (VS Code, Visual Studio, github.com coding agent, Copilot CLI, JetBrains agent mode), Cursor, OpenAI Codex, JetBrains Junie, Windsurf, Google Antigravity and Gemini CLI. |
| `.claude/skills/<name>/SKILL.md` | Bridge stub for Claude Code, which only discovers skills under `.claude/skills/`. It mirrors the canonical frontmatter and delegates to the canonical file - keep only the frontmatter in sync. |
| `.gemini/commands/<name>.toml` | Command shim that guarantees `/<skill-name>` in Gemini CLI and Gemini Code Assist agent mode, where Agent Skills discovery may not be enabled yet. It injects the canonical file via `@{...}`. |

The instructions themselves follow the same single-source pattern: every tool that reads `AGENTS.md` natively (Copilot, Cursor, Codex, Junie, Windsurf, Antigravity) gets it directly, while `CLAUDE.md` and `GEMINI.md` are two-line entry points that inline it with an `@AGENTS.md` import for Claude Code and Gemini. Section 7 of `AGENTS.md` indexes the skills for anything that reads none of the skill locations.

To add a new skill: create `.agents/skills/<your-skill>/SKILL.md`, add a matching stub and shim, and add a row to the `AGENTS.md` section 7 index. The `description` is the most important field - write it as *"...Use when the user asks to X, Y, or says Z"*, because that sentence is the only thing the agent sees when deciding whether to load the skill.

---

## Available Skills

### 1. Scaffold Entity (`scaffold-entity`)

**Canonical file**: `.agents/skills/scaffold-entity/SKILL.md`

**What it does**: Generates a complete CRUD (Create, Read, Update, Delete) implementation for a new entity in your project, including all necessary layers from database to UI.

**When to use it**: When you need to add a new data entity to your application with full CRUD functionality.

**Key capabilities**:
- Creates Entity Type Configuration for EF Core
- Generates DTO (Data Transfer Object) with validation attributes
- Creates Mapper using Mapperly for high-performance object mapping
- Generates API Controller with OData support
- Creates IAppController Interface for strongly-typed HTTP client
- Adds Resource Strings to AppStrings.resx for localization
- Creates Data Grid Page for listing records
- Creates Add/Edit Page for creating and updating records
- Integrates with navigation (PageUrls.cs, NavBar, MainLayout items)
- Updates AppJsonContext for JSON serialization
- Generates EF Core migration

**Example usage**: `/scaffold-entity Product with Name, Description, Price and CategoryId properties`

---

### 2. Localize Strings (`localize-strings`)

**Canonical file**: `.agents/skills/localize-strings/SKILL.md`

**What it does**: Identifies hardcoded strings in your code and moves them to resource files (.resx) for proper localization support.

**When to use it**: When you explicitly want hardcoded user-facing text in your Blazor components, pages, or controllers moved into `AppStrings.resx` - for example as a dedicated translation pass before a release. (Day-to-day, new text stays in the `Localizer["..."]` literal indexer per `AGENTS.md` section 5, because `.resx` edits break hot reload.)

**Key capabilities**:
- Identifies hardcoded user-facing strings in selected code
- Adds new entries to `AppStrings.resx` with appropriate resource keys
- Generates strongly-typed resource classes
- Updates code to use `IStringLocalizer<AppStrings>` pattern
- Uses `nameof(AppStrings.ResourceKey)` for type-safe resource access
- Preserves string formatting with placeholders (e.g., `{0}`, `{1}`)
- Follows naming conventions with descriptive resource keys

**What it won't move**:
- CSS class names or IDs
- Configuration keys
- API endpoints or URLs
- Technical constants (file extensions, mime types)
- Log messages

**Example usage**: `/localize-strings Dashboard.razor`

---

### 3. Bitify UI (`bitify-ui`)

**Canonical file**: `.agents/skills/bitify-ui/SKILL.md`

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

### 4. Code Reviewer (`code-reviewer`)

**Canonical file**: `.agents/skills/code-reviewer/SKILL.md`

**What it does**: Reviews code changes against this project's conventions and reports findings. It never modifies code.

**When to use it**: Before opening a PR, or any time you want a convention check on a diff.

**Key capabilities**:
- Checks Bit.BlazorUI usage, `WrapHandled`, enhanced lifecycle methods, code-behind and scoped SCSS
- Checks theming (`BitColor`, `BitCss.Class`, `BitCss.Var`, `$bit-color-*`, `::deep`)
- Checks API controllers (`AppControllerBase`, `IAppController`, `[EnableQuery]`, Mapperly `Project()`, `long Version`)
- Checks DTOs (`[DtoResourceType]`, `nameof(AppStrings.X)` validation, `AppJsonContext` registration)
- Checks nullability, `async/await`, structured logging, secrets and localization
- Ignores anything `.editorconfig` already handles

It carries `context: fork` in its frontmatter, so tools that support forked skill contexts (Claude Code, VS Code Copilot) run it in a dedicated subagent context instead of filling your main conversation; every other tool simply ignores that field and runs it inline.

**Example usage**: `/code-reviewer` on the current branch's changes

---

### 5. AI-DLC (`ai-dlc`)

**Canonical file**: `.agents/skills/ai-dlc/SKILL.md`

**What it does**: Drives a feature end-to-end through the AI-Driven Development Lifecycle, with approval gates before any code is written.

**When to use it**: For non-trivial features where you want requirements pinned down and a plan approved before implementation starts.

**Key capabilities**:
- Phase 1 - Requirements elaboration, with clarifying questions until requirements are unambiguous
- Phase 2 - Planning and task decomposition, presented for approval
- Phase 3 - Design of affected files, components, APIs and data models
- Phase 4 - Implementation, one task at a time
- Phase 5 - Validation via build, tests and the `code-reviewer` skill
- Delegates to `scaffold-entity` and `bitify-ui` where they apply

**Example usage**: `/ai-dlc add a customer feedback feature`

---

### AI Wiki: Answered Questions

Ask your own question [here](https://bitplatform.dev/ask)

---
