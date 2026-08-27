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

## The two page shells

Every page is the same article: one column of anchored sections with the "on this page" rail
beside it. The two kinds of page differ only in what the sections hold.

- **The prose pages** (Overview, Getting started, Theming, Iconography, Terms) are `DocsArticle` +
  `DocSection`: eyebrow, title, badges, lead, then anchored sections separated by air and a hairline,
  then `DocFeedback`.
- **The component pages** (~110 of them) are `DemoPage`, which builds that same shell by hand -
  `.doc-shell` / `.doc-main` / `.doc-head` and `DocSection`s for Notes, Introduction, Usage, API -
  because it needs a few things `DocsArticle` does not offer (the reduced-motion class on the shell,
  the component-source links inside `DocFeedback`). What the Usage section holds is `DemoExample`:
  a title bar, the folded source, then the live preview. **The source stays above the preview**; it
  is what a reader opens the panel to compare against what is running underneath it.

Shared by both:

- **`SideRail`** - the "on this page" rail, built purely from headings marked
  `example-section-title` in the DOM. `DocSection` marks its own heading - an `h2`, or an `h3` when
  it is nested inside another one as `Level="3"` - `DemoExample` marks its `h3`, and `DemoPage`
  marks the headings of its cards; nothing declares its contents in C#. The rail indents an example,
  or a subsection, under the section that hosts it, keyed off the heading level the DOM read
  reports. A page long enough that a flat list of chapters stops helping (Theming) groups its
  sections into chapters of `Level="3"` subsections rather than growing the flat run.
- **`ComponentCatalog`** - the one list of components, DERIVED from `MainLayout.NavItems` rather
  than written out again. It powers the `/components` gallery, the home page's category grid, the
  header's search box, the category shown above a component's title, and the prev/next pager at the
  foot of every demo page. Adding a component to the nav is all it takes; the only thing the catalog
  adds of its own is a one-line summary per component and an icon per category.
- **`Styles/abstracts/_docs.scss`** - the docs layer's own tokens and mixins (rhythm, measure,
  focus ring, surfaces, scrollbars, eyebrow, display type). Everything in it is either derived from
  a `--bit-*` token, so all four presets and both schemes re-skin the chrome for free, or is a pure
  layout value the library has no opinion about. A scoped `.razor.scss` imports this one file.

Three rules that are easy to get wrong:

- **Scoped CSS needs an anchor.** `::deep` compiles to `[b-scope] .foo`, and the scope attribute
  only lands on plain HTML elements written in that `.razor` file. A page whose root markup is all
  components (`<PageOutlet>`, `<DocsArticle>`) has nowhere for it to land, so such a page wraps the
  part it styles in a plain element of its own (see `.icon-browser`, `.theming-doc`).
- **Prose rules stop at the section's own children.** `.doc-prose` sits on the prose pages only, but
  the same care applies anywhere a wrapper can contain a live preview: a descendant selector for
  `ul` would re-indent a BitNav's list, and one for `code` would re-skin the Text demo's output.
  Every rule in that block is bounded with `>`.
- **Reduced motion is honoured on the component pages and ignored everywhere else.** On a component
  page the animation is the subject, so those pages collapse the motion tokens
  (`.demo-reduced-motion`) and offer the ForceAnimation toggle to turn them back on; every other
  page restores the untouched `-full` values at `:root` and carries `bit-fam` on `.site-content`.
  Both halves live in `Styles/app.scss`, with the class gated by `MainLayout._isDemoPage`.

## The MCP server

`Demo/Bit.BlazorUI.Demo.Server` hosts the library's MCP server at `/mcp`, and mirrors every tool as
a plain HTTP GET under `/api/mcp/...` so each one is inspectable from a browser. The tools are in
`Controllers/McpController.cs`, with `McpPrompts` and `McpResources` beside them; everything they
answer from is in `Services/Mcp`. `Tests/Bit.BlazorUI.Tests.Mcp` drives the whole thing over HTTP
against the app as it is actually deployed.

Nothing on it is written down twice. The nav (`MainLayout.NavItems`, via `ComponentCatalog`) decides
which components exist and what they are also called; the loaded assemblies decide which package
each ships in and what it is generic over; the demo pages carry the hand-written parameter tables
and the worked examples; the XML documentation carries everything else. So **adding a component to
the nav is all it takes for it to appear in the catalog, the search index and the completions.**

- **Seven tools, and the count is the design.** A tool's description is paid for in every request of
  every session. So a listing is not a tool - it is what a retrieval tool answers when called with
  no argument (`GetBitBlazorUIComponent`, `GetBitBlazorUIType`, `GetBitBlazorUIThemingGuide` all do
  this); a single-item lookup is not a tool when one that takes a set already resolves each member.
- **Markdown, never JSON, and no output schemas.** A parameter table as JSON repeats its four field
  names on every row, and a tool declared with `UseStructuredContent` sends the payload twice - once
  as `structuredContent` and once as text, because the protocol asks a server to keep answering
  clients that cannot read a schema.
- **The server's `instructions` are the only thing it gets to say before it is asked anything**
  (`BlazorUIMcpInstructions`), so they carry only what a per-tool description cannot: which tool to
  call first, and the six rules that decide whether markup that compiles also looks right. Nothing
  else on the server restates them - the prompts point at them instead. The counts in them are
  interpolated from the catalogs, never typed.
- **Redundancy is designed out of the answers, not just the tools.** A component's own types are
  documented in full; the library-wide enums it takes are named with their values and left to
  `GetBitBlazorUIType`. The `BitComponentBase` parameters are one lookup rather than 110 repetitions.
  A multi-API component's tabs are the same sections in a different API, so the examples tool answers
  with the first tab and says the others exist.
- **A miss answers with the nearest names** (`BlazorUISuggest`, edit distance over the names with
  their shared `Bit` prefix removed) rather than with a refusal, and never as a failed tool call.

The demo pages' `.razor` files are embedded into the **server** assembly by its .csproj - the client
would otherwise ship four megabytes to every WebAssembly visitor. Only the markup that names and
orders the example sections is read from them; the samples and the tables are reflected off the
compiled page types, where reflection cannot misread them.

## Theme tokens in component SCSS

Component stylesheets never hard-code a design-system decision; they read it from the global tokens
declared in `Bit.BlazorUI/Styles/theme-variables.scss` (defaults in `Styles/Fluent/*.scss`, family
aliases in `Styles/family-tokens.scss`). This is what lets the packaged Material and Cupertino
presets re-skin the whole library from one `:root[bit-theme="..."]` block.

- **Type**: `font-size` comes from the ramp `$tg-fs-2xs..4xl` (never `spacing(n)`, which is for
  rhythm only); size classes map sm -> `$tg-fs-xs`, md -> `$tg-fs-sm`, lg -> `$tg-fs-md`.
  `font-weight` comes from `$tg-fw-light/regular/medium/semibold/bold` (never a literal number).
  Labels of interactive controls also set `letter-spacing: $tg-ctrl-letter-spacing` (buttons and
  tags add `text-transform: $tg-ctrl-text-transform`).
- **Shape**: the outer corner comes from the family alias - `$shp-radius-control` (inputs, pickers,
  badges, pagination, ...) with its three sub-families `$shp-radius-button` (buttons and dialog
  actions), `$shp-radius-chip` (tags and in-field chips) and `$shp-radius-selection` (the checkbox
  box), `$shp-radius-surface` (cards, accordions, messages), `$shp-radius-popup` (callouts, menus,
  tooltips, snackbars), `$shp-radius-dialog` (dialogs, modals); sub-elements use
  the scale `$shp-radius-none/xs/sm/md/lg/xl/2xl/full`. Heavier strokes (underline focus, selection
  indicators, thumb rings) use `$shp-border-width-thick`; inline spinners use `$siz-spinner-stroke`.
- **Size**: control heights per size class are `$siz-ctrl-sm/md/lg` (also for 32px icon-button
  squares), control padding `$siz-ctrl-pad-x-sm/md/lg` / `$siz-ctrl-pad-y-sm/md/lg`, minimum control
  width `$siz-ctrl-min-width`, glyphs inside controls `$siz-icon-sm/md/lg`, checkbox box / radio ring
  `$siz-sel-sm/md/lg`, popup list row heights `$siz-item-sm/md/lg`, pivot headers `$siz-tab` with
  selection-indicator stroke `$siz-tab-indicator`, separator thickness `$siz-divider`, linear
  progress tracks `$siz-track-sm/md/lg`, the switch track and knob `$siz-switch-w/h/thumb-sm/md/lg`,
  the slider handle `$siz-slider-thumb-sm/md/lg`, scrolling popup lists `$siz-popup-max-height`.
- **Spacing & layout**: dialogs and message boxes inset their content with `$spa-dialog`, and their
  action footers lay out via `$layout-dialog-actions-direction` / `$layout-dialog-actions-justify` /
  `$layout-dialog-actions-align` (never a literal `row` / `flex-end` / `center` in a dialog footer -
  Cupertino stacks its actions full width).
- **Elevation**: `$box-shadow-card/popup/dialog/sheet/tooltip/snackbar/appbar-top/appbar-bottom` per
  surface family, never `$box-shadow-callout` directly.
- **Motion**: `$mot-easing` for state transitions, `$mot-easing-decelerate` / `-accelerate` for
  popup entry / exit; never a literal `ease` or `cubic-bezier` outside a looping loader keyframe.
- **Opacity**: a disabled element that keeps its own colors dims with `$opa-dis`; text-bearing
  controls use the `$clr-*-dis` color tokens instead.

The packaged Fluent 2, Material and Cupertino presets ship with **Bit.BlazorUI.Extras**, in
`Bit.BlazorUI.Extras/Styles/Fluent2`, `.../Styles/Material` and `.../Styles/Cupertino`, as
override-only bundles (`_content/Bit.BlazorUI.Extras/styles/bit.blazorui.fluent2.css` /
`...material.css` / `...cupertino.css`, linked after the core stylesheet). Their C# names live next
to them on `BitExtraThemePresets` / `BitExtraThemeName` - core's `BitThemePresets` / `BitThemeName`
carry only the Fluent family the core stylesheet itself implements - and `BitThemeSwitcher` is the
ready-made chrome for picking between them. Their `colors.*.scss` palettes are GENERATED by the
library's seed-derivation pipeline - regenerate them with the recipe each file's header documents,
never hand-edit - while `tokens.*.scss` holds the hand-written shape/size/typography/motion values
(Fluent 2 additionally splits its per-scheme ambient/key elevation into `shadows.fluent2-*.scss`).
The theme tests read every packaged preset out of one `theme-styles` tree, which the test csproj
links both projects' stylesheets into.

Adding a preset means touching all of: its `Styles/<Name>/` folder and bundle entry point,
`Bit.BlazorUI.Extras/compilerconfig.json` and the csproj `BuildCss` target, `BitExtraThemePresets` /
`BitExtraThemeName`, `BitThemeSwitcher.DefaultDesignSystems`, the test csproj's `theme-styles` link
and the palette/derivation test `DataRow`s, the demo host pages
(`Demo/Bit.BlazorUI.Demo.Server/Components/App.razor`, the MAUI `index.html`) and
`ScssCompilerService`, and the ThemingPage docs.

A preset declares **nothing but `--bit-*` tokens**: it never selects a component class. Needing to
restyle `.bit-<cmp>-*` from a preset is the signal that a design-system decision is missing from the
global token tier - add the token (see below), let the component read it, and keep the preset a pure
`:root[bit-theme="..."]` block.

Adding a global token means touching all of: `theme-variables.scss` (the `$` alias), a
`Styles/Fluent/*.scss` default (or `family-tokens.scss` for an alias), the `BitTheme` model class,
`BitCss.var.cs`, `BitThemeMapper` (`MapToCssVariables`, `Merge`, `Normalize*`),
`BitThemeSerialization.EnsureNestedObjects` for a new branch, and the ThemingPage docs; the theme
contract tests in `Tests/Bit.BlazorUI.Tests/Utils/Theme` fail on any drift between them.
