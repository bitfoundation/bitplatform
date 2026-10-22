---
name: bitify-ui
description: Modernizes Blazor pages by replacing raw HTML elements and custom CSS with Bit.BlazorUI components and theme-aware styling, using the bit BlazorUI MCP tools to discover components and verify their exact APIs.
---

# Bitify: Replace raw HTML/CSS with Bit.BlazorUI components

You are an expert Blazor modernization agent. Your job is to replace standard HTML elements and custom CSS in Blazor pages with Bit.BlazorUI components and theme-aware styling.

Use the bit BlazorUI MCP tools instead of guessing APIs; their own descriptions say what each one returns.
`SearchBitBlazorUI` comes first, asked by **capability** rather than by name, because the name a task suggests is
rarely the name this library chose: a "select" is `BitDropdown`, a "toast" is `BitSnackBar`, a "skeleton" is
`BitShimmer`, an "expander" is `BitAccordion`, a "switch" is `BitToggle`. An icon name that does not exist is not a
compile error but an empty box on the page, so take every one from `FindBitBlazorUIIcons`. The project is already
wired up for the library, so the setup guide is not needed.

---

## Execution Plan

### Step 1: Read the Target Page

Read the `.razor`, `.razor.cs`, and `.razor.scss` files in parallel. Identify:
- Every HTML element (`<div>`, `<button>`, `<input>`, `<select>`, `<table>`, `<form>`, `<a>`, etc.)
- Hardcoded colors, font sizes, spacing, and non-theme-aware CSS
- Flexbox/grid layout containers
- Event handlers and data-bound fields

### Step 2: Find the Replacement for Each Element

For each element or hand-written control, call `SearchBitBlazorUI` with **what it does**, not what it is called -
"button with a loading state", "numeric input with a spinner", "date range picker". Batch the searches in parallel.
If you would rather browse, call `GetBitBlazorUIComponent` with **no argument** for the full catalog. Do **not**
guess a component name.

Layout containers are components too: `BitStack`, `BitGrid`, `BitCard`, `BitSeparator`, `BitSpacer` and `BitText`
replace nearly every `<div>` written for flexbox, grid, spacing or typography.

### Step 3: Inspect Exact APIs and Examples

For **each component** you plan to use, call `GetBitBlazorUIComponent("<ComponentName>")` **in parallel**, and add
`GetBitBlazorUIComponentExamples("<ComponentName>", "<section>")` for anything with data binding, templates or
`EditForm` integration. Never assume parameter names, defaults or types from memory.

Look up every type a signature names with `GetBitBlazorUIType` - an enum parameter takes the enum
(`Color="BitColor.Primary"`), never a string (`Color="Primary"`).

The bit platform MCP server answers for the release in its URL in the project's MCP files, the same release as the
bit packages in `Directory.Packages.props`, so what the tools describe is what the page compiles against.

### Step 4: Look Up Theming Questions with `GetBitBlazorUIThemingGuide`

Theming, tokens and component styling are answered by the bit BlazorUI tools. Call
`GetBitBlazorUIThemingGuide` with no argument for the index, then the chapter you need - "Design tokens" for the
`--bit-*` custom properties behind the `$bit-color-*` SCSS variables, "Presets" for a packaged design system,
"Color derivation and contrast" for deriving a palette from a brand color.

A theme here is data rather than a stylesheet fork, so the answer to a question about color, dark mode or spacing
is almost never "write CSS".

### Step 5: Implement Replacements

Apply changes to `.razor`, `.razor.cs`, and `.razor.scss`:

**Razor markup:**
- Replace HTML elements with the chosen Bit.BlazorUI components
- Use the `@bind-` form the component declares (`@bind-Value`, `@bind-IsOpen`, `@bind-SelectedItem`) - not
  `value=`, not plain `@bind`, and not a one-way parameter plus a hand-written change callback
- Wrap all event handlers with `WrapHandled`: `OnClick="WrapHandled(MyMethod)"`
- Use `BitButtonType.Button` on non-submit buttons inside forms to prevent accidental form submission
- Use `Variant`, `Color`, `Size` parameters for visual styling instead of custom CSS classes
- Disable with `Disabled` (never the native `disabled` attribute) and hide with
  `Visibility="BitVisibility.Hidden"` or `BitVisibility.Collapsed` (never `display:none`) - both are
  `BitComponentBase` parameters that every component has, and both keep the accessibility behavior the component
  implements
- Per-part overrides belong in the component's `Classes` / `Styles` bag, not in a wrapper selector
- Keep the page's raw root element (the `<section>` of the pages in this template). Scoped CSS only tags raw
  elements, so a `::deep` rule needs a raw ancestor to hang on - replace that root with a component and every
  `::deep` rule of the page silently stops applying
- `BitText` covers text elements too: `Element="pre"` / `"dt"` / `"dd"` / `"span"` with `Monospace`,
  `PreserveWhitespace`, `BreakWord`, `Weight` and `Typography` replaces `<pre>`, `<b>` and their font CSS
- `BitGrid` is flex based, so it cannot give cards in different rows one height, and `MinItemWidth` stretches the
  last row. A plain `<div>` with `display: grid; grid-auto-rows: 1fr` is the right tool there

**SCSS:**
- Replace hardcoded colors with SCSS variables from `_bit-css-variables.scss`:
  - `$bit-color-primary`, `$bit-color-secondary`
  - `$bit-color-foreground-primary`, `$bit-color-foreground-secondary`
  - `$bit-color-background-primary`, `$bit-color-background-secondary`
  - `$bit-color-border-primary`, `$bit-color-border-secondary`
- Style a component through its API first: a parameter, then its `Classes` / `Styles` part bag, then its public
  `--bit-<Component>-*` CSS variables, then a theme token. A `::deep` rule only targets a class you passed in through
  `Class` or `Classes`, never the library's own `.bit-*` class names or its private `--bit-<abbr>-*` variables
- Remove CSS that duplicates what the component already provides. An app-wide change is a `--bit-*` token
  override, not a rule that selects a `.bit-<component>` class - the latter breaks on the next release

**Code-behind:**
- Remove DOM manipulation / JS interop that the component now handles internally
- Replace HTML event args (`MouseEventArgs`, etc.) with component-specific event types
- Use `@ref` of the component type if you need to call component methods

### Step 6: Build and Verify

If the app is already running under `dotnet watch` or `aspire start`, let hot reload pick the changes up instead of
rebuilding; otherwise start it that way rather than with `dotnet run`, so later edits do not need a rebuild either.
Fix any build errors before finishing, then open the page in both the light and the dark theme: a hardcoded or
non-token color only shows up in one of them.

---

## Rules

- **Never guess** a component name, parameter name, enum value, parameter type, or icon name. Always verify with
  `SearchBitBlazorUI`, `GetBitBlazorUIComponent`, `GetBitBlazorUIType` and `FindBitBlazorUIIcons`.
- **Never hardcode colors** in Razor or SCSS. Use `BitColor` enum, `BitCss.Class`, `BitCss.Var`, or `$bit-color-*` SCSS variables.
- **Always use `WrapHandled`** for event handlers in Razor to prevent unhandled exceptions from crashing the page.
- **Style through the component API** (parameters, `Classes` / `Styles`, public `--bit-<Component>-*` variables) before any `::deep` rule, and never select the library's own `.bit-*` classes.
- **Prefer component parameters** over CSS classes for visual variants (`Variant`, `Color`, `Size`, `FullWidth`, etc.).
- **Use code-behind files** (`.razor.cs`) for logic - do not add `@code` blocks to `.razor` files.

---

Now read the target page files and begin the modernization.
