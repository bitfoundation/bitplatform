# bit BlazorUI

Guidance for the bit BlazorUI component library and its demo app. Coding style comes from the
`.editorconfig` at the root of the `src` folder ([../CLAUDE.md](../CLAUDE.md)).

## Demo pages

A component's demo page is
`Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/<Category>/<Component>/<Component>Demo.razor`,
hosting one or more `DemoExample` sections. A multi-API component splits them across `BitPivotItem`
tabs (`_..ItemDemo`, `_..CustomDemo`, `_..OptionDemo`), each with its own `.razor`, `.razor.cs` and
`.razor.samples.cs`.

- **Order.** Component-specific sections first, starting with `Basic`; then the five near-identical
  look-and-feel ones, last and in this order: `Color`, `External Icons`, `Size`, `Style & Class`,
  `RTL`. A new section goes after the last component-specific one, before `Color`.
- **Numbering.** From 1, no gaps, in render order:
  `<DemoExample Title="Basic" RazorCode="@example1RazorCode" CsharpCode="@example1CsharpCode" Id="example1">`.
  `Id="exampleN"` and the `exampleNRazorCode` / `exampleNCsharpCode` fields share the `N`, and the
  fields are declared in that order. Reordering or inserting renumbers every later section, in the
  `.razor` **and** the `.razor.samples.cs`, plus any `Href="#exampleN"`.
- **A section only uses what has already been introduced.** A demo page is read from the top down, so
  a section may only use the parameters and the features its own section, or an earlier one, has
  introduced.
- **A multi-API component's tabs stay aligned**: same sections, same order, same titles, same data
  (same labels, same number of button groups per section) - only the API differs.
- **The samples match what is rendered.** `RazorCode` / `CsharpCode` are what a reader copies out, so
  they carry the markup that section actually renders, including any parameter added or renamed.
- **A feature that is not one file gets one tab per file.** `RazorCode` + `CsharpCode` is one file -
  the markup with its own `@code` block - and stays how nearly every section is written. A section
  whose feature also needs an isolated stylesheet, or a code-behind worth reading beside the markup,
  adds `CodeFiles="@exampleNCodeFiles"`, a `DemoCodeFile[]` field declared with the other `exampleN`
  fields:

  ```csharp
  private readonly DemoCodeFile[] example3CodeFiles =
  [
      new("BitFooDemo.razor.scss", example3ScssCode),
      new("BitFooDemo.razor.cs", example3CodeBehind),
  ];
  ```

  The pair is then the first tab, named by `RazorCodeName` (`.razor` unless it is set), and each file
  is one more. Name a file the way it would be named on disk: the name is both the tab and what says
  which language it is in. A tab named after something other than a file (`"Program.cs additions"`)
  passes its language as the third argument. One pane draws no tab strip, so an example that has only
  `CodeFiles`, or only the pair, looks exactly as it always has.

## The two page shells

Every page is one column of anchored sections with the "on this page" rail beside it; only what the
sections hold differs.

- **Prose pages** (Overview, Getting started, Theming, Iconography, Terms): `DocsArticle` +
  `DocSection` - eyebrow, title, badges, lead, sections separated by air and a hairline, then
  `DocFeedback`.
- **Component pages** (~110): `DemoPage`, the same shell by hand (`.doc-shell` / `.doc-main` /
  `.doc-head`, `DocSection`s for Notes, Introduction, Usage, API), for the two things `DocsArticle`
  does not offer - the reduced-motion class on the shell and the component-source links inside
  `DocFeedback`. Usage holds `DemoExample`s: title bar, folded source, live preview - **the source
  stays above the preview**, to be compared against what runs underneath it.

Shared by both:

- **`SideRail`** - the "on this page" rail, built purely from DOM headings marked
  `example-section-title`; nothing declares its contents in C#. `DocSection` marks its own (an `h2`,
  or an `h3` when nested in another as `Level="3"`), `DemoExample` its `h3`, `DemoPage` its cards'. It
  indents an example or subsection under its host section by the heading level the DOM read reports. A
  page too long for a flat list of chapters (Theming) groups its sections into chapters of `Level="3"`
  subsections instead of growing the flat run.
- **`ComponentCatalog`** - the one list of components, DERIVED from `MainLayout.NavItems`, so adding a
  component to the nav is all it takes. It powers the `/components` gallery, the home page's category
  grid, the header's search box, the category above a component's title, and the prev/next pager on
  every demo page. Of its own it adds a one-line summary and glyph per component, an icon and blurb
  per category, and the category's NuGet package (Extras and Theming -> `Bit.BlazorUI.Extras`, Legacy
  -> `Bit.BlazorUI.Legacy`, the rest core); a component with no glyph falls back to its category's, so
  the map may lag the nav without leaving a card blank. `ComponentCatalog.Search` ranks matches (name,
  then alias, then the words that only describe it); the gallery uses it when a term is typed, the
  header's box always.
- **`Styles/abstracts/_docs.scss`** - the docs layer's own tokens and mixins (rhythm, measure, focus
  ring, surfaces, scrollbars, eyebrow, display type), each either derived from a `--bit-*` token, so
  all four presets and both schemes re-skin the chrome for free, or a pure layout value the library
  has no opinion about. A scoped `.razor.scss` imports this one file.

Three rules that are easy to get wrong:

- **Scoped CSS needs an anchor.** `::deep` compiles to `[b-scope] .foo`, and the scope attribute lands
  only on plain HTML elements written in that `.razor`. A page whose root markup is all components
  (`<PageOutlet>`, `<DocsArticle>`) has nowhere for it to land, so it wraps the part it styles in a
  plain element of its own (see `.icon-browser`, `.theming-doc`).
- **Prose rules stop at the section's own children.** `.doc-prose` sits on the prose pages only, but
  the same care applies wherever a wrapper can contain a live preview: a descendant selector for `ul`
  would re-indent a BitNav's list, one for `code` re-skin the Text demo's output. Every rule in that
  block is bounded with `>`.
- **Reduced motion is honoured on the component pages and ignored everywhere else.** There the
  animation is the subject, so those pages collapse the motion tokens (`.demo-reduced-motion`) and
  offer the ForceAnimation toggle to turn them back on; every other page restores the untouched
  `-full` values at `:root` and carries `bit-fam` on `.site-content`. Both halves live in
  `Styles/app.scss`, the class gated by `MainLayout._isDemoPage`.

## The MCP server

`Demo/Bit.BlazorUI.Demo.Server` hosts the library's MCP server at `/mcp`, mirroring every tool as a
plain HTTP GET under `/api/mcp/...` so each is inspectable from a browser. The tools are in
`Controllers/McpController.cs`, with `McpPrompts` and `McpResources` beside them; everything they
answer from is in `Services/Mcp`. `Tests/Bit.BlazorUI.Tests.Mcp` drives it over HTTP against the app
as actually deployed.

Nothing is written down twice: the nav (`MainLayout.NavItems`, via `ComponentCatalog`) decides which
components exist and what they are also called, the loaded assemblies which package each ships in and
what it is generic over, the demo pages the hand-written parameter tables and worked examples, the XML
documentation everything else. So **adding a component to the nav is all it takes for it to appear in
the catalog, the search index and the completions.**

- **Seven tools, and the count is the design** - every description is paid for in every request. A
  listing is not a tool but what a retrieval tool answers with no argument
  (`GetBitBlazorUIComponent`, `GetBitBlazorUIType`, `GetBitBlazorUIThemingGuide`); a single-item
  lookup is not a tool when one taking a set already resolves each member.
- **Markdown, never JSON, and no output schemas.** JSON repeats a table's four field names per row,
  and `UseStructuredContent` sends the payload twice (`structuredContent` and text, for clients that
  cannot read a schema).
- **The server's `instructions`** (`BlazorUIMcpInstructions`) are all it says before being asked
  anything, so they carry only what a per-tool description cannot: which tool to call first, and the
  six rules that decide whether markup that compiles also looks right. Nothing else on the server
  restates them - the prompts point at them - and their counts are interpolated from the catalogs,
  never typed.
- **Redundancy is designed out of the answers too.** A component's own types are documented in full;
  library-wide enums are named with their values and left to `GetBitBlazorUIType`. The three inherited
  parameter sets - `BitComponentBase` (nearly every component), `BitInputBase` (the inputs),
  `BitTextInputBase` (the ones typed into) - are three lookups, not three hundred repetitions: each
  answer NAMES the parameters it takes from each as that component closes it (BitTextField's is
  `BitInputBase<string>`) and points at the set for the prose. A multi-API component's tabs are the
  same sections in another API, so the examples tool answers with the first tab and says the others
  exist. A section written over several files is fenced once per file, each fence named with the file
  it came from - the name is what says where the code goes. Never left out is a NAME: every library
  type a signature mentions is named back with its members and the call returning it, since a type
  belonging to one component is kept out of the type listing.
- **The type has the last word on what exists, the demo page on how it is described.** The tables are
  the better prose and what the site renders, but a parameter added without updating the page is
  invisible in them, and one this server does not name is one an agent will not use. So each answer is
  the table plus every `[Parameter]` on the compiled type it does not name, its default read off a
  constructed instance; likewise the public members, less what is public only to be called from
  elsewhere (`[JSInvokable]` callbacks, generated `Assign*` setters).
- **What a table cannot say is derived rather than left out**: which parameters are two-way bindable
  (an `X` with an `XChanged` beside it, printed as `@bind-X`), what constrains a generic component's
  type arguments, and whether a type named beside a component is a class it takes or a component that
  goes inside its markup.
- **A miss answers with the nearest names** (`BlazorUISuggest`, edit distance over the names less
  their shared `Bit` prefix) rather than a refusal, and never as a failed tool call.

The demo pages' `.razor` files are embedded into the **server** assembly by its .csproj - the client
would otherwise ship four megabytes to every WebAssembly visitor. Only the markup naming and ordering
the example sections is read from them; the samples and tables are reflected off the compiled page
types, where reflection cannot misread them.

## Theme tokens in component SCSS

Component stylesheets never hard-code a design-system decision; they read the global tokens declared
in `Bit.BlazorUI/Styles/theme-variables.scss` (defaults in `Styles/Fluent/*.scss`, family aliases in
`Styles/family-tokens.scss`). That is what lets the packaged Material and Cupertino presets re-skin
the whole library from one `:root[bit-theme="..."]` block.

- **Type**: `font-size` from the ramp `$tg-fs-2xs..4xl` (never `spacing(n)`, which is rhythm only);
  size classes map sm -> `$tg-fs-xs`, md -> `$tg-fs-sm`, lg -> `$tg-fs-md`. `font-weight` from
  `$tg-fw-light/regular/medium/semibold/bold`, never a literal number. Labels of interactive controls
  also set `letter-spacing: $tg-ctrl-letter-spacing`; buttons and tags add
  `text-transform: $tg-ctrl-text-transform`.
- **Shape**: the outer corner from the family alias - `$shp-radius-control` (inputs, pickers, badges,
  pagination, ...) with its sub-families `$shp-radius-button` (buttons, dialog actions),
  `$shp-radius-chip` (tags, in-field chips) and `$shp-radius-selection` (the checkbox box);
  `$shp-radius-surface` (cards, accordions, messages); `$shp-radius-popup` (callouts, menus, tooltips,
  snackbars); `$shp-radius-dialog` (dialogs, modals). Sub-elements use the scale
  `$shp-radius-none/xs/sm/md/lg/xl/2xl/full`. Heavier strokes (underline focus, selection indicators,
  thumb rings) use `$shp-border-width-thick`; inline spinners `$siz-spinner-stroke`.
- **Size**: control heights per size class `$siz-ctrl-sm/md/lg` (also 32px icon-button squares),
  control padding `$siz-ctrl-pad-x-sm/md/lg` / `$siz-ctrl-pad-y-sm/md/lg`, minimum control width
  `$siz-ctrl-min-width`, glyphs inside controls `$siz-icon-sm/md/lg`, checkbox box / radio ring
  `$siz-sel-sm/md/lg`, popup list row heights `$siz-item-sm/md/lg`, pivot headers `$siz-tab` with
  selection-indicator stroke `$siz-tab-indicator`, separator thickness `$siz-divider`, linear progress
  tracks `$siz-track-sm/md/lg`, switch track and knob `$siz-switch-w/h/thumb-sm/md/lg`, slider handle
  `$siz-slider-thumb-sm/md/lg`, scrolling popup lists `$siz-popup-max-height`.
- **Spacing & layout**: dialogs and message boxes inset their content with `$spa-dialog`; their action
  footers lay out via `$layout-dialog-actions-direction` / `$layout-dialog-actions-justify` /
  `$layout-dialog-actions-align` (never a literal `row` / `flex-end` / `center` in a dialog footer -
  Cupertino stacks its actions full width).
- **Elevation**: `$box-shadow-card/popup/dialog/sheet/tooltip/snackbar/appbar-top/appbar-bottom` per
  surface family, never `$box-shadow-callout` directly.
- **Motion**: `$mot-easing` for state transitions, `$mot-easing-decelerate` / `-accelerate` for popup
  entry / exit; never a literal `ease` or `cubic-bezier` outside a looping loader keyframe.
- **Opacity**: a disabled element that keeps its own colors dims with `$opa-dis`; text-bearing
  controls use the `$clr-*-dis` color tokens instead.

The packaged Fluent 2, Material and Cupertino presets ship with **Bit.BlazorUI.Extras**
(`Bit.BlazorUI.Extras/Styles/Fluent2`, `.../Styles/Material`, `.../Styles/Cupertino`) as override-only
bundles (`_content/Bit.BlazorUI.Extras/styles/bit.blazorui.fluent2.css` / `...material.css` /
`...cupertino.css`, linked after the core stylesheet). Their C# names live on `BitExtraThemePresets` /
`BitExtraThemeName` - core's `BitThemePresets` / `BitThemeName` carry only the Fluent family the core
stylesheet itself implements - and `BitThemeSwitcher` is the ready-made chrome for picking between
them. Their `colors.*.scss` palettes are GENERATED by the seed-derivation pipeline - regenerate with
the recipe each file's header documents, never hand-edit - while `tokens.*.scss` holds the
hand-written shape/size/typography/motion values (Fluent 2 also splits its per-scheme ambient/key
elevation into `shadows.fluent2-*.scss`). The theme tests read every packaged preset out of one
`theme-styles` tree, which the test csproj links both projects' stylesheets into.

A preset declares **nothing but `--bit-*` tokens** and never selects a component class. Needing to
restyle `.bit-<cmp>-*` from a preset is the signal that a design-system decision is missing from the
global token tier: add the token, let the component read it, and keep the preset a pure
`:root[bit-theme="..."]` block.

Adding a preset means touching all of: its `Styles/<Name>/` folder and bundle entry point,
`Bit.BlazorUI.Extras/compilerconfig.json` and the csproj `BuildCss` target, `BitExtraThemePresets` /
`BitExtraThemeName`, `BitThemeSwitcher.DefaultDesignSystems`, the test csproj's `theme-styles` link
and the palette/derivation test `DataRow`s, the demo host pages
(`Demo/Bit.BlazorUI.Demo.Server/Components/App.razor`, the MAUI `index.html`), `ScssCompilerService`,
and the ThemingPage docs.

Adding a global token means touching all of: `theme-variables.scss` (the `$` alias), a
`Styles/Fluent/*.scss` default (or `family-tokens.scss` for an alias), the `BitTheme` model class,
`BitCss.var.cs`, `BitThemeMapper` (`MapToCssVariables`, `Merge`, `Normalize*`),
`BitThemeSerialization.EnsureNestedObjects` for a new branch, and the ThemingPage docs; the theme
contract tests in `Tests/Bit.BlazorUI.Tests/Utils/Theme` fail on any drift between them.
