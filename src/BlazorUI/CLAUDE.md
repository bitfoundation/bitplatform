# bit BlazorUI

Guidance for the bit BlazorUI component library and its demo app. Coding style comes from the
`.editorconfig` at the root of the `src` folder ([../CLAUDE.md](../CLAUDE.md)).

## Never edit outside this folder

**`src/BlazorUI` is the edit boundary, and it holds for every task.** The library, Extras, Legacy,
Icons, Assets, the source generators, the demo app and the tests are all inside it, so the work belongs
here. Every sibling under `src/` - `Templates` (the Boilerplate), `Butil`, `Bswup`, `Besql`, `Bmotion`,
`Brouter`, `Websites`, `CodeAnalyzers`, the rest - and the repo root are off limits, no matter what a
change in here does to them.

This bites hardest on a rename or a deletion in the public API: the Boilerplate consumes bit BlazorUI,
so retiring a type leaves it referencing something that no longer exists. That is still not a reason
to edit it. **Report what broke and where, and leave the fix to the maintainer** - the templates are
versioned and released on their own schedule, and a BlazorUI branch that carries template changes is a
branch that cannot be merged on its own.

So: grep the whole repo to *find out* what a change reaches, and say so. Only write inside
`src/BlazorUI`.

## Razor comments

**Never put a `@* ... *@` comment inside a tag, between its attributes.** It compiles, but the Razor
compiler mis-reads the attributes around it and the page throws at runtime. A comment about an
attribute goes in the comment block right above the element's opening tag:

```razor
@* Why the chip is named through aria-label. *@
<div role="listitem"
     aria-label="@TagName(tag)">
```

never

```razor
<div role="listitem"
     @* Why the chip is named through aria-label. *@
     aria-label="@TagName(tag)">
```

## Core components never render each other

A component in the core `Bit.BlazorUI` project draws everything it shows with its own markup and its own
SCSS; it never renders another public component of the library (no `<BitSpinnerLoading>` for a busy
state, no `<BitShimmer>` for a skeleton, no `<BitButton>` for an action). Every component is re-skinned on
its own - its `--bit-<Component>-*` variables, its `BitParams` cascade, its `Styles` / `Classes` - so one
rendered inside another would carry an app's restyle of the first into the second, and couple the two
components' markup, defaults and accessibility behaviour as well.

- **Draw the part locally**, under the component's own class prefix, reading the global tokens: an inline
  busy spinner is `@include spinner-ring($size, $color, $track)` from `Styles/functions.scss` - one ring
  of `$siz-spinner-stroke` turning on `$mot-duration-spinner` / `$mot-easing-spinner` over the shared
  `bit-spin` keyframes, with its forced-colors pair built in - never a hand-written copy of it (see
  `.bit-btn-spn`, `.bit-srb-spn`, `.bit-tfl-spn`). A part that goes GrayText when disabled in
  forced-colors mode includes `spinner-ring-forced-disabled` for its spinner in that block. A skeleton is
  a few bars of its own (`.bit-crd-skb`). Offer a `...Template` parameter for an app that wants a library
  component there instead - rendering one is the app's choice, never the component's default.
- **A part drawn locally is a part of the component's public surface**, since what it replaces was
  restylable through its own API: give it a `<Component>ClassStyles` member applied as
  `style="@Styles?.X" class="bit-xxx-yyy @Classes?.X"` (`Spinner`, `Skeleton` / `SkeletonBar`), the public
  `--bit-<Component>-*` variables an app would reach for (`spinner-size` / `-color` / `-track-color`,
  `skeleton-background` / `-color`) in the stylesheet's header, and both in the demo page's
  `componentSubClasses` and `componentCssVariables` tables, which `BitComponentCssVariablesContractTests`
  pins to the stylesheet.
- **Not covered**: a family's own internal shell (`BitLoading`, which every `Bit*Loading` renders its
  drawing into), plumbing that is not a visual component (`BitCascadingValueProvider`), and a service
  whose job is to render the component it serves (`BitModalService` opening a `BitModal`).
- **`Bit.BlazorUI.Extras` is the composition layer**: its components are built out of core ones (BitMessageBox
  renders a BitTextField), which is exactly what keeps the core ones from having to be.

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
- **A library-wide enum's table is written once.** `BitColor`, `BitSize`, `BitVariant` and the other
  types many pages list come from `Models/DemoSharedEnums` (`DemoSharedEnums.BitColor()` in the page's
  `componentSubEnums`), with one anchor id per type. A page passes `description:` for its own line above
  the table and `.Only(...)` for the members it supports (BitPagination's eight general colors; a name
  that is not a member throws, as does naming none), and keeps a table of its own only when the members
  mean something different there (BitLoading's pixel sizes) - still under the shared table's anchor
  id, and a type of its own never under a shared one (BitPersonaSize is `persona-size-enum`). A
  default that holds on one page only goes in that page's parameter description, not in the table. A
  shared table writes nothing but its anchor id: the members and values are read off the enum in
  declaration order, and the prose is the enum's own XML documentation, which
  `MSBuild/DemoSharedEnumDocs.targets` writes into a generated half of `DemoSharedEnums` before every
  compile (a WebAssembly page has none to read) - so a table's wording is changed in the enum's doc
  comments, and a new shared table names its enum in that file as well as adding its factory. A missing
  summary only empties a cell at runtime; `DemoSharedEnumsTests` is what fails on it, and pins every
  page's anchor ids - one per type, shared or not.
- **A parameter typed with a library-wide enum links the shared table and names what it honours.**
  `BitPlacement`, `BitPosition`, `BitShape`, `BitLineStyle` and `BitSelectionMode` are shared tables
  like the rest; a page adds the one it needs to its sub-enum list and points its row at the table's
  anchor id (`Href="#placement-enum"`). Which of the values a particular parameter honours is the row's
  own `Description` - it is what the site renders and what the MCP server hands an agent, and a
  component honouring four of nine values says nothing without it.
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
  plain element of its own (see `.icon-browser`, `.theming-doc`). The same holds inside a `DemoExample`: a
  class styled through an unanchored `::deep` and handed only to components (`Class`, `Classes`, a template)
  matches nothing, so the section's markup sits in a plain `<div>`. `DemoScopedCssAnchorTests` fails on one
  that does not, for every `.razor.scss` of the demo client at once.
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
what it is generic over, the demo pages the hand-written parameter tables and worked examples, the
iconography page's `IconCatalog` the glyphs with their categories and the everyday words that point at
MDL2's own (so `FindBitBlazorUIIcons` finds a house by teaching the page's search box, never by
growing a second table), the XML documentation everything else. So **adding a component to the nav is
all it takes for it to appear in the catalog, the search index and the completions.**

- **Seven tools, and the count is the design** - every description is paid for in every request. A
  listing is not a tool but what a retrieval tool answers with no argument
  (`GetBitBlazorUIComponent`, `GetBitBlazorUIType`, `GetBitBlazorUIThemingGuide`); a single-item
  lookup is not a tool when one taking a set already resolves each member.
- **Markdown, never JSON, and no output schemas.** JSON repeats a table's four field names per row,
  and `UseStructuredContent` sends the payload twice (`structuredContent` and text, for clients that
  cannot read a schema).
- **The server's `instructions`** (`BlazorUIMcpInstructions`) are all it says before being asked
  anything, so they carry only what a per-tool description cannot: which tool to call first, and the
  seven rules that decide whether markup that compiles also looks right. Nothing else on the server
  restates them - the prompts point at them - and their counts are interpolated from the catalogs,
  never typed.
- **Redundancy is designed out of the answers too.** A component's own types are documented in full;
  library-wide enums are named with their values and left to `GetBitBlazorUIType`. The three inherited
  parameter sets - `BitComponentBase` (nearly every component), `BitInputBase` (the inputs),
  `BitTextInputBase` (the ones typed into) - are three lookups, not three hundred repetitions: each
  answer NAMES the parameters it takes from each as that component closes it (BitTextField's is
  `BitInputBase<string>`) and points at the set for the prose. A multi-API component's tabs are the
  same sections in another API, so the examples tool answers with the first tab and says the others
  exist. A section written over several files is fenced once per file: the markup and its `@code`
  block keep the bare `razor` and `csharp` fences the client has always been sent, and every file
  beyond them is named above its fence - the name is what says where the code goes. Never left out is
  a NAME: every library type a signature mentions is named back with its members and the call
  returning it, since a type belonging to one component is kept out of the type listing.
- **The type has the last word on what exists, the demo page on how it is described.** The tables are
  the better prose and what the site renders, but a parameter added without updating the page is
  invisible in them, and one this server does not name is one an agent will not use. So each answer is
  the table plus every `[Parameter]` on the compiled type it does not name, its default read off a
  constructed instance; likewise the public members, less what is public only to be called from
  elsewhere (`[JSInvokable]` callbacks, generated `Assign*` setters). The one part of an API with no
  type behind it is the public `--bit-<Component>-*` custom properties: the demo page's
  `componentCssVariables` table is their whole source, and the answer carries it with the one thing
  the names do not say - that they inherit, so `:root`, an ancestor and an instance's `Style` are all
  places to set one. The other part reflection alone misses is an **extension member**: one package's
  contribution to another's type is compiled into the container that declares it, so a type's answer
  carries what the packages add to it (`BlazorUIExtensionMembers`, read off the `<Extension>$` marker
  rather than off the generated names), and a container nobody writes - `BitThemePresetsExtensions` -
  is kept out of the listing and answered as a pointer to the type its members are read off.
- **What a table cannot say is derived rather than left out**: which parameters are two-way bindable
  (an `X` with an `XChanged` beside it, printed as `@bind-X`), what constrains a generic component's
  type arguments, whether a type named beside a component is a class it takes or a component that
  goes inside its markup, and the `<Component>Params` a `BitParams` ancestor sets its defaults with -
  read off the component's own `[CascadingParameter]`, so it is in neither table, and named with the
  call that lists its members rather than tabulated, since its members are the parameter table again
  as nullables.
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
  snackbars); `$shp-radius-dialog` (dialogs, modals); `$shp-radius-sheet` (the inner corners of panels,
  square unless a preset rounds them). Two parts design systems shape apart have a primitive of their own:
  the tab-strip selection indicator `$shp-radius-tab-indicator` / `-base` and the linear progress track
  and bar `$shp-radius-progress` (square under Fluent, rounded under the Extras presets). Sub-elements use the scale
  `$shp-radius-none/xs/sm/md/lg/xl/2xl/full`. Heavier strokes (underline focus, selection indicators,
  thumb rings) use `$shp-border-width-thick`; inline spinners `$siz-spinner-stroke`.
- **Size**: control heights per size class `$siz-ctrl-sm/md/lg` (also 32px icon-button squares),
  control padding `$siz-ctrl-pad-x-sm/md/lg` / `$siz-ctrl-pad-y-sm/md/lg`, minimum control width
  `$siz-ctrl-min-width`, glyphs inside controls `$siz-icon-sm/md/lg`, checkbox box / radio ring
  `$siz-sel-sm/md/lg`, popup list row heights `$siz-item-sm/md/lg`, pivot headers `$siz-tab` with
  selection-indicator stroke `$siz-tab-indicator`, separator thickness `$siz-divider`, linear progress
  tracks `$siz-track-sm/md/lg`, switch track and knob `$siz-switch-w/h/thumb-sm/md/lg`, slider handle
  `$siz-slider-thumb-sm/md/lg`, badge height and dot `$siz-badge-sm/md/lg` / `$siz-badge-dot-sm/md/lg`,
  tag (chip) height inside its rule `$siz-chip-sm/md/lg`, scrolling popup lists `$siz-popup-max-height`.
- **Spacing & layout**: dialogs and message boxes inset their content with `$spa-dialog`, cards with
  `$spa-card-sm/md/lg`; dialog action footers lay out via `$layout-dialog-actions-direction` /
  `$layout-dialog-actions-justify` / `$layout-dialog-actions-align` (never a literal `row` /
  `flex-end` / `center` in a dialog footer - Cupertino stacks its actions full width).
- **Elevation**: `$box-shadow-card/popup/dialog/sheet/tooltip/snackbar/appbar-top/appbar-bottom` per
  surface family (plus `$box-shadow-card-hover`, the lift of a card under the pointer), never
  `$box-shadow-callout` directly.
- **Motion**: `$mot-easing` for state transitions, `$mot-easing-decelerate` / `-accelerate` for popup
  entry / exit; never a literal `ease` or `cubic-bezier` outside a looping loader keyframe.
- **Opacity**: a disabled element that keeps its own colors dims with `$opa-dis`; text-bearing
  controls use the `$clr-*-dis` color tokens instead.
- **Density**: a size or inset measured in spacing units is never computed in a theme scope - one
  computed on `:root` is inherited as a length, so a density set lower down would never reach it. A
  preset declares its unitless `--bit-<token>-steps` and resets `--bit-<token>: initial`; the
  `theme-variables.scss` alias reads `var(--bit-<token>, calc(unit * density * var(--bit-<token>-steps)))`
  where a component uses it, and a value set for the token itself wins. A C# inline style that needs one
  reads it through a private property the component's stylesheet resolves from the alias (BitDialog's
  `--bit-dlg-dmw`), never as a bare `var(--bit-<token>)`. `BitThemeDensityAwareTokensTests` pins it
  against the compiled bundles.

The packaged Fluent 2, Material and Cupertino presets ship with **Bit.BlazorUI.Extras**
(`Bit.BlazorUI.Extras/Styles/Fluent2`, `.../Styles/Material`, `.../Styles/Cupertino`) as override-only
bundles (`_content/Bit.BlazorUI.Extras/styles/bit.blazorui.fluent2.css` / `...material.css` /
`...cupertino.css`, linked after the core stylesheet). They reach the core theming APIs through the
**preset extension point**, so an app uses one set of types for every preset: their names are
**extension members** on core's `BitThemePresets` / `BitThemeName` (`BitThemePresetsExtensions` /
`BitThemeNameExtensions` in Extras - container types nobody names, C# 14 `extension(T)` blocks), and
their first-paint surfaces are registered with `BitThemePresetRegistry` from
`BitExtraThemeRegistration`'s `[ModuleInitializer]`, which `BitThemeSurfaces` reads as a live view.
Reading `BitThemePresets.MaterialDark` is itself what loads the assembly and so what registers the
presets - as is calling `AddBitBlazorUIExtrasServices`, a method of that assembly, which is why it
makes no registration call of its own; an app that links a bundle but never touches Extras in C# can
call `BitExtraThemeRegistration.Register()`. An app declares its own preset with
`BitThemePresetRegistry.Register(new BitThemePreset { ... })` (`Remove` takes one back out of the
first-paint table - it does not drive `BitThemeSwitcher`, whose items are its own parameters).
**`Register` replaces and is the app's verb; a package uses `TryRegister`, which only fills gaps** -
it skips a name that is already registered or that the app has `Remove`d (removal is remembered) -
because a module initializer runs whenever the assembly happens to load, usually after `Program.cs`,
and must not undo an app's re-skin or removal. `BitThemeSwitcher` is the ready-made chrome for
picking between them.

The extension members are the form to reach for, but they cannot be everything, so **the plain
`BitExtraThemePresets` (`const`s) / `BitExtraThemeName` / `BitExtraThemeSurfaces` stay public and say
exactly the same things** - keep them in step, `BitThemePresetRegistryTests` pins that. Two reasons:
an extension member is a property, so it cannot be a `case` label, an attribute argument or a default
parameter value; and `extension(T)` needs C# 14 on the READING side too, so a `net8.0` / `net9.0`
consumer - both still targeted - is handed `CS9202: Feature 'extensions' is not available in
C# 12.0`. The Boilerplate template's MAUI/Windows native code and host page name the plain types for
that reason. Inside these projects either form compiles, because `Bit.Build.props` sets
`LangVersion=preview` (and the test csproj pins `14.0`). Their `colors.*.scss` palettes are GENERATED by the seed-derivation pipeline - regenerate with
the recipe each file's header documents, never hand-edit - while `tokens.*.scss` holds the
hand-written shape/size/typography/motion values (Fluent 2 also splits its per-scheme ambient/key
elevation into `shadows.fluent2-*.scss`). The theme tests read every packaged preset straight out of
the source tree beside the core stylesheets: the tests' `SourceFiles` (`ReadThemeStylesheet`,
`EnumerateThemeStylesheets`) addresses the Extras preset folders as siblings of `Fluent`.

A preset declares **nothing but `--bit-*` tokens** and never selects a component class. Needing to
restyle `.bit-<cmp>-*` from a preset is the signal that a design-system decision is missing from the
global token tier: add the token, let the component read it, and keep the preset a pure
`:root[bit-theme="..."]` block.

A component's own `--bit-<Component>-*` properties (the public surface its demo page documents as
`componentCssVariables`) are read off its root **with a fallback and never declared**, so they
inherit. A component whose popup is rendered outside that root - a callout, a menu, a panel - renders
it as a SIBLING of the root, and the callout JS reparents it to the body while it is open. So
`Callouts.moveCalloutToBody` never moves the popup on its own: it moves it into a chain of
`display: contents` copies of every ancestor it is leaving (same tag for plain structural elements,
the consumer's classes plus `bit-fam` and `bit-css-*`, inline style, CSS-isolation scopes, `bit-*` and
theme-scoping `data-*theme` / `-scheme` / `-mode` attributes, `dir`, `lang`; kept in step by a
`MutationObserver` while it is open), plus one link for the root, named by the `rootId` argument of
`BitCalloutToggleCallout`, that carries only what the consumer meant for the popup too: the custom
properties of its inline style, `bit-fam`, `bit-theme`, the theme-scoping `data-*` attributes,
CSS-isolation scopes, `dir`, `lang`. That link is built even when the popup is rendered straight into
the body. A variable set on `:root`, on an ancestor's `Style` or class, or on the `Style` /
`Styles.Root` of one instance - and a `BitThemeProvider` or `[bit-theme]` scope around it - therefore
reaches the popup the way it reaches the root, and still follows the theme while it is open. A style
of a passing state of the root (`Styles.Focused`, `Styles.Toggled`) does not: while it is applied the
root renders the style it has without it as `data-bit-popup-style` (`GetPopupStyle`), and a link copies
that instead, so the popup never flips as the focus moves between the field and the popup. What
identifies an element or makes it act is never copied (id, role, aria-*, other `data-*`, the state
`:hover` / `:focus-within` match), so a variable set through such a selector does not reach the popup.
Nor are the library's component classes, which its scripts find elements by with `closest()` (so a
popup that needs its own color and size classes, like the TimePicker's, declares them itself), nor any
class of the root: the root is not an ancestor of the popup, and a link carrying its `Class` would let
the consumer's rules for the field's subtree match the popup (use `Classes.Callout`). Another callout's
relocated popup, when an inner one is opened from it, is copied for its classes alone - never its inline
style, which holds that callout's own placement and the sizing its parameters write (a DropMenu's `Width`
as `--bit-DropMenu-callout-width`), and which an inner popup of the same kind would read as its own; a
component that writes private sizing variables onto its popup still resets them in that popup's own rule
(`.bit-clo-cal`), since a closed inner popup sits under the outer one in the page too. The flip side of
copying an ancestor's classes is deliberate, as the same classes are what carry a class-declared
variable, an inherited text style or a `::deep` rule into the popup: a page's descendant selectors
(`.card div`) match the popup again, as they would in place, so `general.scss` pins only the relocated
parts' `position: fixed`; and an app's `closest('.its-class')` from inside the popup finds the inert copy
rather than nothing. A responsive panel stays in its chain until its exit transition has run. Such a
component resolves its variables in one mixin that both the root and the popup include, and copies
nothing in C#: a popup's open call passes `rootId: _Id`, except one opened from inside another popup,
which inherits through that one's chain, and the TimePicker's, which is rendered INSIDE its root and so
inherits it as an ancestor. The exceptions are parts that sit beside the root but are never relocated -
the calendar of a `Standalone` date picker or date range picker, the dial of a `Standalone` circular time
picker - which get no chain, so they still have the public declarations of `Style` / `Styles.Root`
copied onto them (`BitPublicCssVariables`, `GetStandaloneStyles` / `GetCalloutStyle`).

**A parameter written on the component wins over the public variable that restyles what it sets** -
the variables restyle the default, never a choice, so `:root { --bit-Badge-background: gray }` leaves
`<BitBadge Color="BitColor.Error">` red. Three things make that hold:

- **An unset parameter publishes nothing.** The `_ =>` arm of a `Color` / `Size` / `Shape` switch is
  `string.Empty`, and a value parameter (`Height`, `Gap`) writes no inline property while it is null: a
  class that always carried the default would be indistinguishable from one that was asked for. A value
  from a `BitParams` ancestor counts as set.
- **The root rule resets what those classes publish**, so an instance nested in another one's content
  never inherits the outer one's choice; the classes come later in the file at the same weight and still
  win on the root that carries them. The reset is never written by hand: one list per component,
  `$bdg-private-properties: clr, clr-txt, ...;`, declared above the rule, drives it with
  `@each $name in $bdg-private-properties { --bit-bdg-#{$name}: initial; }`, so a new private variable is
  one name added to the list. A component writing the same variables on a sibling of its root (a callout)
  keeps a second list for it (`$srb-cal-private-properties`).
- **Every read ranks private, then public, then default**:
  `var(--bit-bdg-clr, var(--bit-Badge-background, #{$clr-pri}))`, never
  `var(--bit-Badge-background, var(--bit-bdg-clr))` - the last fallback being the token the unset
  parameter stands for (the primary role, the medium size). A state the component is in (disabled, a
  dot) is read before all three, as before.

What a parameter does not paint stays the variable's: the transparent background of an Outline badge is
the Variant's recipe rather than the Color's, so `--bit-Badge-background` still fills it under an
explicit `Color`. The demo page's `componentCssVariables` row says which parameter wins over each
variable ("The Color parameter wins over it."), and the stylesheet test pins the order - no
`var(--bit-<Component>-..., var(--bit-<prefix>-` left where the private one is a parameter's, plus the
resets on the root. `BitBadge` and `BitShimmer` are the reference implementations. Every private property a
role or size class publishes, or the component writes into an inline style, has to be declared again by the
component's own rules - in its list, or by the root rule at the default the unset parameter stands for;
`BitComponentPrivatePropertyResetTests` fails on one that is not, on a list no `@each` resets, and on a listed
name nothing uses any more, for every stylesheet at once. A variant rule giving one slot the value `initial`
(`.bit-drm-otl { --bit-drm-rst-bg: initial; }`) is a value, not a reset, and stays written out.

The lists reset with `initial` rather than registering the variables as `@property { inherits: false; }`
(which only `BitIcon` does): a variable that does not inherit cannot be read by the parts inside the root
that sets it, and nearly every component publishes on its root what its children read (a card's title size,
its image height).

A component that renders a core one and wants a default other than the core one's (BitMessageBox's neutral
buttons) never passes it as the parameter, which would make it a choice that outranks an app's variables. It
leaves the parameter unset and adds the core component's host-default class instead (`bit-btn-dft-<role>`),
read after the public variables as the last fallback.

Adding a preset means touching all of: its `Styles/<Name>/` folder and bundle entry point,
`Bit.BlazorUI.Extras/compilerconfig.json` and the csproj `BuildCss` target, `BitExtraThemePresets`,
`BitExtraThemeName` and the two extension containers (`BitThemePresetsExtensions` /
`BitThemeNameExtensions`),
`BitExtraThemeRegistration` (the surfaces), `BitThemeSwitcher.DefaultDesignSystems`, the tests' `SourceFiles.ExtrasPresetFolders`
and the palette/derivation test `DataRow`s, the demo host pages
(`Demo/Bit.BlazorUI.Demo.Server/Components/App.razor`, the MAUI `index.html`), `ScssCompilerService`,
and the ThemingPage docs.

Adding a global token means touching all of: `theme-variables.scss` (the `$` alias), a
`Styles/Fluent/*.scss` default (or `family-tokens.scss` for an alias), the `BitTheme` model class,
`BitCss.var.cs`, `BitThemeMapper` (`MapToCssVariables`, `Merge`, `Normalize*`),
`BitThemeSerialization.EnsureNestedObjects` for a new branch, and the ThemingPage docs; the theme
contract tests in `Tests/Bit.BlazorUI.Tests/Utils/Theme` fail on any drift between them.
