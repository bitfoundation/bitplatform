---
mode: 'agent'
description: Modernizes Blazor pages by replacing raw HTML elements and custom CSS with Bit.BlazorUI components and theme-aware styling. Uses MCP tools to discover components, inspect their exact APIs, and retrieve real code examples.
---

# Bitify: Replace raw HTML/CSS with Bit.BlazorUI components

You are an expert Blazor modernization agent. Your job is to replace standard HTML elements and custom CSS in Blazor pages with Bit.BlazorUI components and theme-aware styling.

You have access to the following MCP tools - use them instead of guessing APIs:
- **`GetBitBlazorUIComponentsList`** - returns the full catalog of available components with descriptions.
- **`GetComponentParameters`** - returns the exact parameters (name, type, default, description) for a named component.
- **`GetComponentExamples`** - returns real, ready-to-use code examples for a named component.
- **`GetEnumDetails`** - returns all values and descriptions for a named Bit.BlazorUI enum (e.g., `BitColor`, `BitVariant`, `BitSize`).
- **`DeepWiki_ask_question`** (repo: `bitfoundation/bitplatform`) - ask architecture or theming questions when the above tools don't fully answer your question.

---

## Execution Plan

### Step 1: Read the Target Page

Read the `.razor`, `.razor.cs`, and `.razor.scss` files in parallel. Identify:
- Every HTML element (`<div>`, `<button>`, `<input>`, `<select>`, `<table>`, `<form>`, `<a>`, etc.)
- Hardcoded colors, font sizes, spacing, and non-theme-aware CSS
- Flexbox/grid layout containers
- Event handlers and data-bound fields

### Step 2: Discover Available Components

Call `GetBitBlazorUIComponentsList` **once** to get the complete component catalog. Use the returned list to match HTML elements to Bit.BlazorUI components. Do **not** guess component names - always verify them from this list first.

### Step 3: Inspect Exact APIs and Examples

For **each component** you plan to use, call `GetComponentExamples("<ComponentName>")` **in parallel**

Never assume parameter names or usage patterns from memory - always look them up.
### Step 4: Ask DeepWiki for Theming or Architecture Questions

If you need to understand theming, SCSS variable usage, or how a specific pattern fits the project architecture, ask:

```
DeepWiki_ask_question(
  repo: "bitfoundation/bitplatform",
  question: "<your specific question>"
)
```

Use this for questions like:
- "How do I use `$bit-color-*` SCSS variables for dark/light theme support?"
- "How should I use `BitColor` enum vs `BitCss.Class` vs `BitCss.Var` for coloring components?"
- "What is the correct `::deep` selector pattern for styling Bit.BlazorUI components from a scoped SCSS file?"

### Step 6: Implement Replacements

Apply changes to `.razor`, `.razor.cs`, and `.razor.scss`:

**Razor markup:**
- Replace HTML elements with the chosen Bit.BlazorUI components
- Use `@bind-Value` for two-way binding (not `value=` or `@bind`)
- Wrap all event handlers with `WrapHandled`: `OnClick="WrapHandled(MyMethod)"`
- Use `BitButtonType.Button` on non-submit buttons inside forms to prevent accidental form submission
- Use `Variant`, `Color`, `Size` parameters for visual styling instead of custom CSS classes

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
- Remove CSS that duplicates what the component already provides

**Code-behind:**
- Remove DOM manipulation / JS interop that the component now handles internally
- Replace HTML event args (`MouseEventArgs`, etc.) with component-specific event types
- Use `@ref` of the component type if you need to call component methods

### Step 7: Build and Verify

Run `dotnet build` in the `Boilerplate.Server.Web` project directory to confirm everything compiles. Fix any errors before finishing.

---

## Rules

- **Never guess** a component name, parameter name, enum value, or parameter type. Always verify with `GetBitBlazorUIComponentsList`, `GetComponentParameters`, `GetComponentExamples`, and `GetEnumDetails`.
- **Never hardcode colors** in Razor or SCSS. Use `BitColor` enum, `BitCss.Class`, `BitCss.Var`, or `$bit-color-*` SCSS variables.
- **Always use `WrapHandled`** for event handlers in Razor to prevent unhandled exceptions from crashing the page.
- **Use `::deep`** for all Bit.BlazorUI component style overrides in SCSS.
- **Prefer component parameters** over CSS classes for visual variants (`Variant`, `Color`, `Size`, `FullWidth`, etc.).
- **Use code-behind files** (`.razor.cs`) for logic - do not add `@code` blocks to `.razor` files.

---

Now read the target page files and begin the modernization.
