---
name: bitify-ui
description: Modernizes Blazor pages by replacing raw HTML elements and custom CSS with Bit.BlazorUI components and theme-aware styling, using the bit BlazorUI MCP tools to discover components and verify their exact APIs.
---

# Bitify: Replace raw HTML/CSS with Bit.BlazorUI components

You are an expert Blazor modernization agent. Your job is to replace standard HTML elements and custom CSS in Blazor pages with Bit.BlazorUI components and theme-aware styling, following `AGENTS.md` sections 3 and 5.

The name a task suggests is rarely the one this library chose: a "select" is `BitDropdown`, a "toast" is
`BitSnackBar`, a "skeleton" is `BitShimmer`, an "expander" is `BitAccordion`, a "switch" is `BitToggle`. An icon
name that does not exist is not a compile error but an empty box on the page, so take every one from
`FindBitBlazorUIIcons`. The project is already wired up for the library, so the setup guide is not needed.

---

## Execution Plan

### Step 1: Read the Target Page

Read the `.razor`, `.razor.cs`, and `.razor.scss` files in parallel. Identify:
- Every HTML element (`<div>`, `<button>`, `<input>`, `<select>`, `<table>`, `<form>`, `<a>`, etc.)
- Hardcoded colors, font sizes, spacing, and non-theme-aware CSS
- Flexbox/grid layout containers
- Event handlers and data-bound fields
- Every attribute that carries behavior rather than looks: an `id` or class that JavaScript or a test finds the element
  by, `name` and `@formname` on a statically rendered form, `autocomplete`, `aria-*`, `title`, `tabindex`, `data-*`.
  The UI tests in `src/Tests` find elements by title, placeholder, accessible name and text, so search them too

### Step 2: Find the Replacement for Each Element

Search for each element or hand-written control by what it does - "button with a loading state", "numeric input
with a spinner", "date range picker" - and batch the searches in parallel. Search for the ones you think you already
know a component for too: the search is what turns up the closer fit, a `BitChoiceGroup` for three fixed options
where a `BitDropdown` came to mind. If you would rather browse, `GetBitBlazorUIComponent` with **no argument** returns
the full catalog.

Layout containers are components too: `BitStack`, `BitGrid`, `BitCard`, `BitSeparator`, `BitSpacer` and `BitText`
replace nearly every `<div>` written for flexbox, grid, spacing or typography.

When nothing in bit BlazorUI does what an element does, keep the hand-written element, and tell the user that the
missing component deserves an issue on bit BlazorUI and that you will write its details yourself: what the control
does, the closest component and what it lacks, and the API that would cover it. File it once they agree (`AGENTS.md`
section 6), and write the hand-written element into `specs/workarounds.md` either way.

### Step 3: Inspect Exact APIs and Examples

For **each component** you plan to use, call `GetBitBlazorUIComponent("<ComponentName>")` **in parallel**, and add
`GetBitBlazorUIComponentExamples("<ComponentName>", "<section>")` for anything with data binding, templates or
`EditForm` integration.

Look up every type a signature names with `GetBitBlazorUIType` - an enum parameter takes the enum
(`Color="BitColor.Primary"`), never a string (`Color="Primary"`).

The bit platform MCP server answers for the release in its URL in the project's MCP files, the same release as the
bit packages in `Directory.Packages.props`, so what the tools describe is what the page compiles against.

### Step 4: Look Up Theming Questions with `GetBitBlazorUIThemingGuide`

Call `GetBitBlazorUIThemingGuide` with no argument for the index, then the chapter you need - "Design tokens" for
the `--bit-*` custom properties behind the `$bit-color-*` SCSS variables, "Presets" for a packaged design system,
"Color derivation and contrast" for deriving a palette from a brand color.

A theme here is data rather than a stylesheet fork, so the answer to a question about color, dark mode or spacing
is almost never "write CSS".

### Step 5: Implement Replacements

Apply changes to `.razor`, `.razor.cs`, and `.razor.scss`, with the app running as the **run-app** skill
(`.github/agents/run-app.agent.md`) describes: hot reload brings each edit to the open page, and run-app says what
needs a restart instead.

**Razor markup:**
- Replace HTML elements with the chosen Bit.BlazorUI components
- Use the `@bind-` form the component declares (`@bind-Value`, `@bind-IsOpen`, `@bind-SelectedItem`) - not
  `value=`, not plain `@bind`, and not a one-way parameter plus a hand-written change callback
- Keep every attribute Step 1 listed working on its replacement, through the component's own parameter when it has
  one (`GetBitBlazorUIComponent`): an attribute it doesn't know lands on its root element, so an `autocomplete` meant
  for a text field's `<input>` reaches its wrapper and autofill stops working
- Use `BitButtonType.Button` on non-submit buttons inside forms to prevent accidental form submission
- Disable with `Disabled` (never the native `disabled` attribute) and hide with
  `Visibility="BitVisibility.Hidden"` or `BitVisibility.Collapsed` (never `display:none`) - both are
  `BitComponentBase` parameters that every component has, and both keep the accessibility behavior the component
  implements
- Keep the page's raw root element (the `<section>` of the pages in this template). Scoped CSS only tags raw
  elements, so a `::deep` rule needs a raw ancestor to hang on - replace that root with a component and every
  `::deep` rule of the page silently stops applying
- `BitText` covers text elements too: `Element="pre"` / `"dt"` / `"dd"` / `"span"` with `Monospace`,
  `PreserveWhitespace`, `BreakWord`, `Weight` and `Typography` replaces `<pre>`, `<b>` and their font CSS
- `BitGrid` is flex based, so it cannot give cards in different rows one height, and `MinItemWidth` stretches the
  last row. A plain `<div>` with `display: grid; grid-auto-rows: 1fr` is the right tool there

**SCSS:**
- Remove CSS that duplicates what the component already provides. An app-wide change is a `--bit-*` token
  override, not a rule for every page

**Code-behind:**
- Remove DOM manipulation / JS interop that the component now handles internally
- Replace HTML event args (`MouseEventArgs`, etc.) with component-specific event types
- Use `@ref` of the component type if you need to call component methods

### Step 6: Verify

Fix any build errors, then use the page in the running app, driving it as the run-app skill describes, rather
than only looking at it: pick a value in every input you
replaced, toggle every switch, open every dropdown and dialog, and submit each form empty and filled, since a binding
that broke in the swap shows only there. Do it in both the light and the dark theme, since a hardcoded or non-token
color only shows up in one of them, and once at phone width. Read the browser console and the server's logs as you
go.

---

Now read the target page files and begin the modernization.
