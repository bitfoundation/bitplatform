---
name: bitify-ui
description: Modernizes Blazor pages by replacing raw HTML elements and custom CSS with Bit.BlazorUI components and theme-aware styling, using the bit BlazorUI MCP tools to discover components and verify their exact APIs. Use when the user asks to bitify a page, replace HTML/div/button/input markup with Bit components, remove hardcoded colors, make a page theme-aware or dark-mode ready, or says "run bitify".
---

# Bitify: Replace raw HTML/CSS with Bit.BlazorUI components

You are an expert Blazor modernization agent. Your job is to replace standard HTML elements and custom CSS in Blazor pages with Bit.BlazorUI components and theme-aware styling.

You have access to the following MCP tools - use them instead of guessing APIs. They answer from the assemblies
and documentation pages that ship today, not from memory:

- **`SearchBitBlazorUI(query, limit?)`** - searches components, parameters, examples, types, enum values and the
  theming reference at once, and returns the exact follow-up call for each match. Ask it by **capability**, not by
  name: "searchable multi-select with chips", "toast notification", "virtualized table with sorting". This is your
  first call, even when a component name comes to mind - the name a task suggests is rarely the name this library
  chose (a "select" is `BitDropdown`, a "toast" is `BitSnackBar`, a "skeleton" is `BitShimmer`, an "expander" is
  `BitAccordion`, a "switch" is `BitToggle`).
- **`GetBitBlazorUIComponent(name?)`** - the full reference of one component: its package, every parameter with
  type/default/description, its public members, its `Classes`/`Styles` bag and item classes, the types its
  parameters take, its inherited parameters (`BitComponentBase`, `BitInputBase`, `BitTextInputBase`), which
  parameters are two-way bindable, and the titles of its worked examples. Accepts `BitDropdown`, `DatePicker`,
  `Toast`, or a demo route. **Omit `name`** to get the whole component catalog grouped by category.
- **`GetBitBlazorUIComponentExamples(name, example?)`** - the real Razor/C# the documentation site runs, one
  section per feature. This is the only place the non-obvious parts show up: which parameters go together, what
  templates receive, how the component binds inside an `EditForm`. Narrow it with `example` (section titles come
  back from `GetBitBlazorUIComponent`) instead of pulling every section.
- **`GetBitBlazorUIType(typeName?)`** - the full reference of any public type read out of the shipped assembly:
  enums (`BitColor`, `BitVariant`, `BitSize`, `BitVisibility`), classes (`BitDropdownItem<TValue>`), services
  (`BitModalService`) and static catalogs. Dotted names reach nested catalogs, e.g. `BitCss.Var.Color.Primary`.
  **Omit `typeName`** to list the library-wide types.
- **`GetBitBlazorUIThemingGuide(section?)`** - one chapter of the theming reference: design tokens and how to
  override one, the packaged presets (Fluent, Fluent 2, Material, Cupertino), deriving a palette from one brand
  color, contrast, density, RTL, the C#/JS APIs, and avoiding a wrong-theme flash on the first server-rendered
  frame. **Omit `section`** to get the index.
- **`FindBitBlazorUIIcons(query, limit?)`** - finds `BitIconName` glyphs by what they depict (over two thousand
  Fabric/MDL2 icons, matched word by word, so "add friend" finds `AddFriend`). A glyph name that does not exist is
  **not** a compile error - it is an empty box on the page, so never type one from memory.
- **`GetBitBlazorUISetupGuide(hostingModel)`** - only if a project is missing `AddBitBlazorUIServices()` or the
  `bit.blazorui.css` / `bit.blazorui.js` tags. This template is already wired up, so you normally will not need it.
- **`ask_question`** - for the **third-party** libraries its description names (Mapperly, Aspire, Hangfire,
  FusionCache and the others). It explicitly excludes the bit platform libraries - never point it at Bit.BlazorUI.

When any of these tools cannot resolve an argument it answers with the nearest candidates and the call that lists
them - read that answer instead of retrying blind.

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

### Step 4: Look Up Theming Questions with `GetBitBlazorUIThemingGuide`

Theming, tokens and component styling are answered by the bit BlazorUI tools, **not** by `ask_question`. Call
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
- Disable with `IsEnabled="false"` (never the native `disabled` attribute) and hide with
  `Visibility="BitVisibility.Hidden"` or `BitVisibility.Collapsed` (never `display:none`) - both are
  `BitComponentBase` parameters that every component has, and both keep the accessibility behavior the component
  implements
- Per-part overrides belong in the component's `Classes` / `Styles` bag, not in a wrapper selector

**SCSS:**
- Replace hardcoded colors with SCSS variables from `_bit-css-variables.scss`:
  - `$bit-color-primary`, `$bit-color-secondary`
  - `$bit-color-foreground-primary`, `$bit-color-foreground-secondary`
  - `$bit-color-background-primary`, `$bit-color-background-secondary`
  - `$bit-color-border-primary`, `$bit-color-border-secondary`
- Use `::deep` to style child elements inside Bit.BlazorUI components:
  ```scss
  .my-component {
      ::deep .bit-btn-pri { ... }
  }
  ```
- Remove CSS that duplicates what the component already provides. An app-wide change is a `--bit-*` token
  override, not a rule that selects a `.bit-<component>` class - the latter breaks on the next release

**Code-behind:**
- Remove DOM manipulation / JS interop that the component now handles internally
- Replace HTML event args (`MouseEventArgs`, etc.) with component-specific event types
- Use `@ref` of the component type if you need to call component methods

### Step 6: Build and Verify

Run `dotnet build` in the `Boilerplate.Server.Web` project directory to confirm everything compiles. Fix any errors before finishing.

---

## Rules

- **Never guess** a component name, parameter name, enum value, parameter type, or icon name. Always verify with
  `SearchBitBlazorUI`, `GetBitBlazorUIComponent`, `GetBitBlazorUIType` and `FindBitBlazorUIIcons`.
- **Never hardcode colors** in Razor or SCSS. Use `BitColor` enum, `BitCss.Class`, `BitCss.Var`, or `$bit-color-*` SCSS variables.
- **Always use `WrapHandled`** for event handlers in Razor to prevent unhandled exceptions from crashing the page.
- **Use `::deep`** for all Bit.BlazorUI component style overrides in SCSS.
- **Prefer component parameters** over CSS classes for visual variants (`Variant`, `Color`, `Size`, `FullWidth`, etc.).
- **Use code-behind files** (`.razor.cs`) for logic - do not add `@code` blocks to `.razor` files.

---

Now read the target page files and begin the modernization.
