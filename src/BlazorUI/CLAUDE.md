# bit BlazorUI

Guidance for working on the bit BlazorUI component library and its demo app.

Coding style comes from the `.editorconfig` at the root of the `src` folder, as described in
[../CLAUDE.md](../CLAUDE.md).

## Demo pages

Each component has a demo page under
`Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/<Category>/<Component>/`, built from a
`<Component>Demo.razor` page hosting one or more `DemoExample` sections. Multi-API components split
those sections across `BitPivotItem` tabs (`_..ItemDemo`, `_..CustomDemo`, `_..OptionDemo`), each with
its own `.razor`, `.razor.cs` and `.razor.samples.cs` files.

### Order of the demo sections

The generic, look-and-feel sections always come **last**, in exactly this order:

1. `Color`
2. `External Icons`
3. `Size`
4. `Style & Class`
5. `RTL`

Everything specific to the component comes before them, starting with `Basic`. The reason is that
these five sections are near-identical on every page and carry no information about what the
component actually does, so they must not sit between the sections that do.

When adding a new section, insert it after the last component-specific one and before `Color`, then
renumber the sections that follow.

### Numbering of the demo sections

The sections are numbered sequentially from 1, with no gaps, in the order they are rendered:

```razor
<DemoExample Title="Basic" RazorCode="@example1RazorCode" CsharpCode="@example1CsharpCode" Id="example1">
```

`Id="exampleN"` and the `exampleNRazorCode` / `exampleNCsharpCode` fields in the `.razor.samples.cs`
file must all carry the same `N`, and the fields must be declared in that same order. Reordering or
inserting a section means renumbering every section after it, in the `.razor` **and** the
`.razor.samples.cs`, and updating any `Href="#exampleN"` cross-references.

### Consistency across the tabs of a multi-API component

The tabs of a multi-API component are three views of the same feature set, so they must stay aligned:
the same sections, in the same order, with the same titles, demonstrating the same data (same labels,
same number of button groups per section). A reader comparing two tabs should see only the API
difference, never a difference in what is being demonstrated.

### The code samples must match what is rendered

`RazorCode` / `CsharpCode` are what a reader copies out of the page, so they have to reflect the
markup actually rendered in that section, including any parameter that was added or renamed.

## Theme tokens in component SCSS

Component stylesheets never hard-code a design-system decision; they read it from the global tokens
declared in `Bit.BlazorUI/Styles/theme-variables.scss` (defaults in `Styles/Fluent/*.scss`, family
aliases in `Styles/family-tokens.scss`). This is what lets a Material or Cupertino preset re-skin the
whole library from one `:root[bit-theme="..."]` block.

- **Type**: `font-size` comes from the ramp `$tg-fs-2xs..4xl` (never `spacing(n)`, which is for
  rhythm only); size classes map sm -> `$tg-fs-xs`, md -> `$tg-fs-sm`, lg -> `$tg-fs-md`.
  `font-weight` comes from `$tg-fw-light/regular/medium/semibold/bold` (never a literal number).
  Labels of interactive controls also set `letter-spacing: $tg-ctrl-letter-spacing` (buttons and
  tags add `text-transform: $tg-ctrl-text-transform`).
- **Shape**: the outer corner comes from the family alias - `$shp-radius-control` (buttons, inputs,
  tags, checkboxes, ...), `$shp-radius-surface` (cards, accordions, messages), `$shp-radius-popup`
  (callouts, menus, tooltips, snackbars), `$shp-radius-dialog` (dialogs, modals); sub-elements use
  the scale `$shp-radius-none/xs/sm/md/lg/xl/2xl/full`. Heavier strokes (underline focus, selection
  indicators, thumb rings) use `$shp-border-width-thick`; inline spinners use `$siz-spinner-stroke`.
- **Size**: control heights per size class are `$siz-ctrl-sm/md/lg` (also for 32px icon-button
  squares), glyphs inside controls `$siz-icon-sm/md/lg`, checkbox box / radio ring `$siz-sel-sm/md/lg`,
  scrolling popup lists `$siz-popup-max-height`.
- **Elevation**: `$box-shadow-card/popup/dialog/sheet/tooltip/snackbar/appbar-top/appbar-bottom` per
  surface family, never `$box-shadow-callout` directly.
- **Motion**: `$mot-easing` for state transitions, `$mot-easing-decelerate` / `-accelerate` for
  popup entry / exit; never a literal `ease` or `cubic-bezier` outside a looping loader keyframe.
- **Opacity**: a disabled element that keeps its own colors dims with `$opa-dis`; text-bearing
  controls use the `$clr-*-dis` color tokens instead.

Adding a global token means touching all of: `theme-variables.scss` (the `$` alias), a
`Styles/Fluent/*.scss` default (or `family-tokens.scss` for an alias), the `BitTheme` model class,
`BitCss.var.cs`, `BitThemeMapper` (`MapToCssVariables`, `Merge`, `Normalize*`),
`BitThemeSerialization.EnsureNestedObjects` for a new branch, and the ThemingPage docs; the theme
contract tests in `Tests/Bit.BlazorUI.Tests/Utils/Theme` fail on any drift between them.
