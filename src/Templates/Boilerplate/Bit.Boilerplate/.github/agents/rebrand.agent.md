---
name: rebrand
description: Re-skins the whole app so it looks and feels like a design reference - a live website or app, a Figma file, screenshots or mockups, brand guidelines, a design-system spec or token file, or only a brand name or a described mood. It decides every bit BlazorUI token family for light and dark (color roles, neutrals, elevation, shape, focus, typography, density, motion) plus fonts, logo, home page and shell chrome, then proves the result state by state - focused, hovered, selected, disabled, invalid, autofilled, open overlays, RTL, phone - with measured contrast. Behavior, text and the identity flows stay unchanged.
---

# Rebrand: re-skin the app to match a design reference

You are a senior design engineer. You get a reference and perhaps a few words about the feel; you return the same app
wearing that design language in both color schemes, both text directions, at every width and in every interactive
state, together with the evidence that it holds up. A re-skin touches hundreds of theme values, and every one the
reference leaves open is yours to decide. The person who asked reviews the result, not a questionnaire.

Re-skins fail in three predictable ways, and this file is built around them:

1. **The reference is read by eye** - colors guessed from a screenshot, fonts from a glance, states never looked at
   (section 2).
2. **Only what shows gets translated** - the obvious tokens change, the rest of the old preset stays, and the parts no
   token reaches keep their old look (sections 3-5).
3. **It is proven on pages at rest** - while most defects a re-skin leaves behind exist only in a state: a focused
   control, a selected item, a selected item under the pointer, an autofilled field, an open dropdown, the dark
   scheme, an RTL culture, a phone (sections 6-7).

Paths that don't start with `src/` are relative to `src/Client/Boilerplate.Client.Core/`.

## 1. Ground rules

- **The default scope is appearance**: theme tokens, SCSS, fonts, images and logos, plus the markup of the Home page
  and of the shell chrome (header, nav panel) where the reference's look needs it. The request may widen that
  ("redesign the dashboard too") or narrow it ("only the colors", "just our new font"): follow it. A narrow change
  still gets the full verification of everything it touches - a new font moves line heights and truncation, a new
  radius can clip a focus ring.
- **Unchanged unless the request says otherwise**: behavior, routes, validation, every existing user-visible string,
  and the markup of the identity flows under `Components/Pages/Identity/` (the sign-in page, panel and modal, the OTP
  and TFA panels, sign-up, forgot and reset password, confirm, consent). Restyle those through tokens and scoped SCSS
  only. The UI tests in `src/Tests` find elements by page title, placeholder, accessible name and text, and by class
  names in the shell too (`.bit-prs.persona`, `.app-menu-callout`, `main .main-container`): before you restructure
  any markup, search `src/Tests` for the selectors it uses and keep every one of them working.
- **Decide; don't interview.** Make each open decision, write it down with a one-line reason, and move on. Ask only
  when the reference itself is out of reach (a private Figma file, a dead link, a missing attachment), and then say
  exactly what you need. Never rebuild a brand from memory and present it as the reference.
- **Every scheme, direction and width.** The app has light and dark schemes and RTL cultures (`fa-IR`, `ar-SA`, unless
  it runs with `InvariantGlobalization`), and runs from phone to desktop; a reference usually shows one of each.
  Design the rest in its spirit, and say you did.
- **Accessibility outranks fidelity.** WCAG 2.2 AA in both schemes: text 4.5:1 (3:1 from 24px, or from 18.66px bold),
  icons and control boundaries 3:1, a visible focus indicator on every focusable element, nothing carried by color
  alone. Where the reference falls short, keep its character and change the mapping (4.1), and list the deviation.
- **Project conventions** (`AGENTS.md`) apply: Bit.BlazorUI components, found through the bit platform MCP tools
  before you write code against them (an icon name that does not exist renders an empty box, not a build error);
  theme variables instead of literal colors everywhere except where values are defined (`_brand.scss` and the native
  surfaces of section 5); `Localizer["..."]` literals for new text; `WrapHandled`; code-behind files; every `[mirror]`
  counterpart updated together; `.scss` only, since `**/*.css` is build output and git-ignored.
- **Branch and commits**: work on `rebrand/<slug>` unless the current branch is already dedicated to this work; commit
  only when asked.
- **Other people's marks**: a third party's logo, name, artwork or licensed font is fine in an internal demo or a
  mockup, not in anything that ships. Say which applies in the report.

### The running app

<!--#if (aspire == true)-->
You verify against the app while it runs under `aspire start` with hot reload (AGENTS.md section 4). Below, stopping
the app means `aspire stop` and starting it means `aspire start`.
<!--#else-->
You verify against the app while it runs under `dotnet watch` in `src/Server/Boilerplate.Server.Web` with hot reload
(AGENTS.md section 4). Below, stopping the app means ending that `dotnet watch` and starting it means running it again.
<!--#endif-->

- **Never add a new file under a `wwwroot` folder while the app runs** - editing existing files is fine. A new static
  file crashes `dotnet watch` (dotnet/roslyn#84062). Gather every new file first (logo variants, fonts, art) and add
  them in one batch with the app stopped. Where stopping is not an option, use `data:` URIs - the CSP allows them for
  images and fonts - and move them into files at the next stop.
- `.scss`, `.razor`, `.cs` and `.ts` edits reach the open page on their own, SCSS within seconds. Don't restart or
  reload to see them; when a change doesn't show, suspect the browser cache first (section 6).
- **Sass fails the build on any warning but the silenced `@import` deprecation** (`LogStandardErrorAsError` in the
  client csproj files): no `/` division (use `math.div`) and no deprecated global functions such as `darken` or
  `transparentize`. Sass does not evaluate expressions inside a custom property: write `--x: #{rgba($c, .5)}`, not
  `--x: rgba($c, .5)`.
- **No CSP is emitted in Development** (`Components/Layout/ContentSecurityPolicy.razor`), so a hotlinked image or
  font works on your machine and breaks in production. Outside Development the policy allows the app's own origins,
  `data:` images and fonts, Google Fonts (`fonts.googleapis.com`, `fonts.gstatic.com`) and `img.shields.io` images -
  nothing else.
- `dotnet test` needs the app stopped, because the running app holds the build outputs: stop the app, run the tests,
  start it again, then wait (bounded) until it answers again.

## 2. Read the reference into a spec

The reference can arrive as anything - a URL, a Figma link, a folder of screenshots, a PDF, a DESIGN.md, a token
export, a product name, a sentence about a mood, or a mix of them - and you read it with whatever tools you have. The
output of this step is always the same: a written spec precise enough that another engineer could build the theme
from it without ever seeing the reference. Keep it, with every capture you take, in `.playwright-mcp/rebrand/<slug>/`
at the repository root (git-ignored scratch; `<slug>` names the reference).

The spec covers, for light and for dark:

- **Identity**: three personality words, and the three to five signature moves that make the reference recognizable
  within a second ("pill buttons in ink", "one saturated color, only ever on large surfaces", "hairlines instead of
  shadows", "tight, heavy headings").
- **Color by role**, each value with its source: brand accents, text tiers, canvas and surface tiers, borders and
  dividers, links, the selected or current indication, focus, status colors, the modal scrim.
- **Typography**: families and their licensed substitutes; the size, weight, line-height and tracking ramp; the case
  of buttons, labels and headings.
- **Shape**: the radius of each family (field, button, chip, checkbox, card, popup, dialog, sheet) and stroke widths.
- **Elevation**: how layers separate - shadow, tint or hairline - per surface family, in each scheme.
- **Density**: control heights, paddings, icon sizes, the spacing rhythm.
- **Motion**: durations and easing where observable, otherwise the intended feel.
- **States, as the reference draws them**: hover, pressed, focus (ring color, width, offset, shape), selected or
  current, disabled, invalid, placeholder. These are your targets in section 7, so capture them, not just pages at
  rest.
- **Imagery**: the logo (vector, in a version for light and for dark surfaces), illustration and photo style, icon
  style.
- **Composition** of the header, the navigation and the landing area, for the Home page and the shell.

Prefer measured and stated values to impressions, and know what each kind of evidence can and cannot tell you:

- **Stated** sources - official brand guidelines, a design-system spec, Figma variables and styles, token files,
  DESIGN.md - carry intent: brand colors, type, logo rules. They rarely cover control heights, focus rings or dark
  mode. Official guidelines outrank third-party write-ups; community DESIGN.md collections are LLM-written analyses.
- **A live site or app** carries what actually shipped: read computed styles rather than pixels, and capture its
  states and its dark mode if it has one. Its marketing pages and consent banners are not its product UI.
- **Images** carry composition and feel. Their colors drift with compression, color profiles and scaling: sample flat
  areas, never anti-aliased edges, and treat the values as approximate. Their sizes scale with the capture: measure
  against something of known size.
- **A name or a mood** alone: find the official sources yourself; for a mood, choose concrete references, name them,
  and treat them as above.
- Where sources disagree, stated intent wins for brand values and measured UI wins for UI details. Mark every value the
  reference does not give as *derived*, with the reason.

Two things are easy to get wrong:

- **Fonts.** A proprietary family gets a licensed substitute: the one the brand's own guidelines name, otherwise the
  closest open-license face. Load it from Google Fonts (an `@import url(...)` in `_brand.scss`, which Sass hoists to
  the top of `app.css`) or self-host woff2 files; self-host whenever the app must render offline (MAUI, Windows, the
  PWA). The MAUI host page preloads the default Segoe UI files (`src/Client/Boilerplate.Client.Maui/wwwroot/index.html`):
  point those preloads at the brand's files or drop them. The app ships Arabic-script (`fa-IR`, `ar-SA`), Devanagari
  (`hi-IN`) and Han (`zh-CN`) cultures: give a Latin-only family per-script fallbacks in the stack, or those cultures
  silently drop to a system font.
- **Logos.** An SVG shown through `<img>` needs its `xmlns` attribute and cannot use `currentColor` (it renders black
  there); markup copied out of a web page usually has neither. Keep a version for each surface the logo sits on.

## 3. How the theme layer works here

bit BlazorUI paints everything from `--bit-*` custom properties, so a theme is data. Four tiers matter:

| Tier | Examples | What to know |
| --- | --- | --- |
| Primitives | `--bit-clr-{pri,sec,ter,inf,suc,wrn,swr,err}` with `-dark`/`-light` tiers, `-hover`/`-active` steps, `-text`, `-dis`, `-dis-text`, `-focus`; neutrals `--bit-clr-{bg,fg,brd}-{pri,sec,ter}...`, the `--bit-clr-ntr-*` grays, `--bit-clr-bg-overlay`, `--bit-clr-req`; `--bit-shd-*`, `--bit-shp-*`, `--bit-siz-*`, `--bit-spa-*`, `--bit-tpg-*`, `--bit-mot-*`, `--bit-opa-*`, `--bit-layout-*` | What a preset defines |
| Family aliases | `--bit-shp-radius-{control,surface,popup,dialog,sheet,button,chip,selection,tab-indicator}`, `--bit-shd-{card,card-hover,popup,dialog,sheet,tooltip,snackbar,appbar-top,appbar-bottom}` | What components actually read for corners and elevation: `--bit-shp-brd-radius` alone cannot give pill buttons on square cards |
| Semantic aliases | `--bit-sem-*` | For app CSS; components never read them |
| Component tier | public knobs `--bit-<Component>-<part>` (`--bit-Button-shadow`, `--bit-Nav-selected-background`); internals `--bit-<abbr>-*` (`--bit-btn-clr`, `--bit-nav-clr-icon`) | Set on the component root by its role and variant classes |

The token and knob sets grow from one bit BlazorUI version to the next: some family aliases, the
`--bit-clr-<role>-fg` text tones and most component knobs are recent. Confirm every name you use in the stylesheets
of the version `src/Directory.Packages.props` pins - a custom property nothing reads changes nothing and raises no
error.

Where to look things up:

- `GetBitBlazorUIThemingGuide` (bit BlazorUI MCP server): the chapters "Design tokens", "Presets", "Color derivation
  and contrast", "Density, layout and RTL" and "Motion and accessibility".
- The shipped stylesheets are the final word on what paints a part in a given state:
  `~/.nuget/packages/bit.blazorui/<version>/staticwebassets/styles/bit.blazorui.css`, and the pinned preset's
  stylesheet, which `src/Server/Boilerplate.Server.Web/Components/App.razor` links right before `app.css` (a
  `_content/<Package>/styles/<file>` link there is `~/.nuget/packages/<package>/<version>/staticwebassets/styles/<file>`,
  with the package id in lower case; Fluent's preset lives in `bit.blazorui.css` itself). Every rule that colors a
  part: `grep -o '\.bit-nav-sel[^{]*{[^}]*}' bit.blazorui.css`. Every knob of a component, empty when the version has
  none for it: `grep -o -- '--bit-Nav-[a-zA-Z-]*' bit.blazorui.css | sort -u`.

### Where the values go

The app pins one of bit's design-system presets as its light and dark themes, `fluent2-light` and `fluent2-dark`
(`AppThemePresets.Light` and `Dark` in `src/Client/Boilerplate.Client.Core/Infrastructure/Services/AppThemePresets.cs`,
read by `src/Server/Boilerplate.Server.Web/Components/App.razor` and written literally into both `index.html` hosts).
Re-value those two presets in place, in one new file, `Styles/_brand.scss`, imported from `Styles/app.scss` after the
abstracts. If `app.scss` still holds its commented-out dark-background override, replace that block with the import
and give `_brand.scss` its `[mirror]` note instead, reworded: `_brand.scss` now owns `--bit-clr-bg-pri`, and its
counterparts are the ones section 5 lists. If `_brand.scss` already exists, an earlier re-skin made it: revise it
rather than stacking a second layer on top.

```scss
@mixin brand-shared { ... }
@mixin brand-light { ... }
@mixin brand-dark { ... }

:root[bit-theme="fluent2-light"], :root [bit-theme="fluent2-light"] { @include brand-shared; @include brand-light; }
:root[bit-theme="fluent2-dark"], :root [bit-theme="fluent2-dark"] { @include brand-shared; @include brand-dark; }
```

`brand-shared` holds what both schemes share - shape, size, spacing, typography, motion, layout - and `brand-light`
and `brand-dark` hold each scheme's palette, elevation and focus. The component-tier rules, the browser-painted parts
and the app surfaces (section 4) follow the two selector blocks. Keep the reasons for your values in the spec and the
report, not in comments.

These are the preset's own selectors and `app.css` loads after the preset, so equal specificity wins - and theme
persistence, the server-rendered first paint and the light/dark toggle keep working without a C# change. The
descendant twins (`:root [bit-theme=...]`) are required: bit re-resolves tokens inside any element that carries its
own `bit-theme`.

When the reference is itself close to a design system bit ships as a preset (Fluent 2, Fluent, Material, Cupertino),
consider pinning that preset instead and re-valuing it: its shapes, elevation, density and motion then start out
right. Change everything that pins the current one together - the `fluent2-light` and `fluent2-dark` names, the
`AppThemePresets` constants, the preset stylesheet links in `App.razor` and both `index.html` hosts, the `theme-color`
literals - and the selectors above.

`Styles/abstracts/_bit-css-variables.scss` holds no values: it maps every token to an SCSS variable
(`$bit-color-primary: var(--bit-clr-pri)`), so it follows the brand on its own - don't edit it to re-skin. Deciding
every token family (section 4) decides each of its variables, and the token ledger (7.4) proves it.

### Derive the palette; don't hand-pick ramps

Each color role has thirteen slots: a nine-tone ramp, the on-color, a disabled pair and the focus color.
`BitThemeFactory` derives them the way bit's own palettes are built - OKLCH steps at constant hue; the seed path adds a
WCAG repair pass, while the accent path only picks a black or white on-color, so measure the brand roles yourself
(4.1). Generate them, then pin the brand's exact values on top. Run it as a throwaway file-based C# program in a
scratch folder outside the repository, with the Bit.BlazorUI version from `src/Directory.Packages.props`:

```csharp
#:package Bit.BlazorUI@10.6.2
using Bit.BlazorUI;

var brand = "#4F46E5";
var neutralHue = brand;
var seed = new BitThemeSeedOptions { NeutralTintChroma = 0.008, SemanticHarmonizationDegrees = 6 };
var accents = new BitThemeAccentColors { Primary = brand, Error = "#D92D20" };

var light = BitThemeUtilities.Merge(overrides: BitThemeFactory.CreateLightTheme(accents),
                                    baseline: BitThemeFactory.CreateLightThemeFromSeed(neutralHue, seed));
var dark = BitThemeUtilities.Merge(overrides: BitThemeFactory.CreateDarkTheme(accents),
                                   baseline: BitThemeFactory.CreateDarkThemeFromSeed(neutralHue, seed));

var exactDark = new BitTheme();
BitThemeColorDerivation.FillColorRoleFromMain(exactDark.Color.Primary, "#A5B4FC", BitThemeColorScheme.Dark);
dark = BitThemeUtilities.Merge(overrides: exactDark, baseline: dark);

foreach (var (scheme, theme) in new[] { ("light", light), ("dark", dark) })
{
    Console.WriteLine($"@mixin brand-{scheme}-colors {{");
    foreach (var (token, value) in BitThemeUtilities.ToCssVariables(theme).OrderBy(t => t.Key, StringComparer.Ordinal))
        Console.WriteLine($"    {token}: {value};");
    Console.WriteLine("}");
}
```

`brand` is the brand's main color, `neutralHue` the hue the canvas, surfaces, text and borders should carry, and
`accents` names only the roles the brand defines. The `exactDark` lines give the dark scheme the brand's own main
instead of the factory's brightened copy of the light one; drop them when the brand has none. `dotnet run palette.cs`
prints two mixins, about 225 tokens each, to include from `brand-light` and `brand-dark`. What the factory does not do
for you:

- **Neutrals take the seed's hue.** A warm, saturated seed tints the dark canvas (yellow turns it olive), and even an
  achromatic seed (black, white, gray) lands on some hue. Point `neutralHue` at the hue the neutrals should carry, or
  set `IncludeNeutrals = false` and write the neutral families yourself.
- **Focus colors are only partly generated.** The factory sets `-focus` for the roles you pass in
  `BitThemeAccentColors` (to their main color) and leaves the other slots at the preset's values. Pasted as is, a
  ring's color depends on which slot the component reads: set all seventeen `--bit-clr-*-focus` slots on purpose
  (4.2).
- **Status roles keep their conventional hues**; harmonize them toward the brand by 6-8 degrees at most. `sec` and
  `wrn` are fill colors: as text on a light surface even their `-dark` tones usually fall short of 4.5:1, so use the
  `--bit-clr-<role>-fg` text tones where the version has them, otherwise a tone you measured.

## 4. Decide every token family

Work from the decisions that move the most pixels to those that move the fewest: role mapping and neutrals,
typography, shape and elevation, states and browser-painted parts, signature moves, then the wiring of section 5.

### 4.1 Role mapping comes first

bit paints `--bit-clr-pri` as a **fill** (filled buttons, checked checkboxes and toggles, sliders, progress, badges)
and as a **foreground** on the page surfaces (links, Text and Outline buttons, icons, the selected indicator of nav
and pivot). So before the brand color gets the role, measure it against `--bit-clr-bg-pri` and `--bit-clr-bg-sec` in
each scheme:

- **4.5:1 or better**: it can be `pri` as it is.
- **Below that**, choose one and record why:
  - (a) `pri` = the brand's ink (its near-black or deep navy), and the brand color moves to surfaces and signature
    fills - the app bar, the hero, the main call to action - through the component tier and app SCSS;
  - (b) `pri` = a deeper tone of the brand color that clears 4.5:1, if it still reads as the brand;
  - (c) per scheme: the bright brand color as `pri` in dark, where it usually passes, with (a) or (b) in light.
- Then the on-color: `--bit-clr-pri-text` must clear 4.5:1 on `--bit-clr-pri` and on `--bit-clr-pri-dark`, which
  checked toggle buttons and the current page of a pagination fill with. Bright fills need a dark on-color.

As a rule of thumb, bright yellows, greens, cyans, oranges and pinks fail on white; deep blues, indigos, violets and
reds pass.

### 4.2 Every other family

Each row ends with a decision - changed, derived, or kept with a reason. "Kept" counts only if you looked.

| Family | Decide | Watch for |
| --- | --- | --- |
| Accents and status | `sec`, `ter`, `inf` from the brand's secondary colors, or derived; `suc`, `wrn`, `swr`, `err` | Harmonizing, and text tones for `sec` and `wrn` (section 3) |
| Neutrals | `bg-pri`, `bg-sec`, `bg-ter` with their steps; `fg-pri`, `fg-sec`, `fg-ter`, `fg-dis`; `brd-pri`, `brd-sec`, `brd-ter`; the `ntr` grays; `bg-overlay`; `req` | `brd-pri` is the control boundary (3:1 on all three surfaces); `fg-sec` and `fg-ter` carry secondary text and placeholders, so both need 4.5:1 on every surface tier. A dark canvas sits off pure black unless the brand's is black, and dark elevation reads through lighter surfaces |
| Elevation | the `--bit-shd-*` scale and the surface families | Dark schemes need stronger shadows or a hairline instead; a flat reference sets the families to `none` and separates with borders |
| Shape | `--bit-shp-brd-radius`, the radius scale, the families and sub-families, `brd-width`, `brd-width-thick`, `brd-style` | A pill is `9999px`; a parent with `overflow: hidden` clips focus rings |
| Focus | `--bit-shp-focus-ring-width`, `--bit-shp-focus-ring-offset` and all seventeen `--bit-clr-*-focus` slots | The ring is two box-shadows - a gap in `--bit-clr-bg-pri`, then the ring in a `-focus` color - and underlined fields draw a single bottom line instead, so dotted or dashed outlines can't be expressed through tokens. The ring must clear 3:1 on every surface a control can sit on, brand-colored ones included |
| Typography | `--bit-tpg-font-family` and `-font-family-mono`, the base weight and line height, `fs-2xs` to `fs-4xl`, `fw-light` to `fw-bold`, `ctrl-letter-spacing`, `ctrl-text-transform`, and the `BitText` variants `h1`-`h6`, `subtitle1/2`, `body1/2`, `button`, `caption1/2`, `overline` | `--bit-tpg-font-weight` is the weight of control labels, not of body text; `app.scss` applies the family to every element |
| Size and density | `--bit-siz-ctrl-{sm,md,lg}` and their paddings, `ctrl-min-width`, the icon, selection, item, tab, track, switch, slider and spinner sizes, the popup max height, the dialog max width; `--bit-layout-density-scale`, `--bit-spa-scaling-factor`, `--bit-spa-dialog` | A preset can set a minimum width for labeled buttons (Fluent 2: 96px) |
| Motion | the `--bit-mot-duration*-full` sources and the easing curves | Never the effective `--bit-mot-duration*` tokens: those are what reduced motion collapses |
| Layout | `--bit-layout-dialog-actions-{direction,justify,align}` | |
| Signature moves | component knobs (`--bit-Button-*`, `--bit-Card-*`, `--bit-Nav-*`, ...) and app SCSS for app surfaces | A knob whose value uses per-instance variables must be declared on the component's class, because a `var()` inside a custom property resolves where it is declared: `.bit-btn-fil { --bit-Button-shadow: 0 4px 0 0 var(--bit-btn-clr-active); }`, not on `:root` |

### 4.3 Every state is a decision

A component in a selected, checked, current or active state paints a fill, a label and often an icon, and these come
from different variables - not all of which have a public knob. For every such component the app shows, decide all
three, and their combinations with hover and focus, by reading the component's rules in `bit.blazorui.css`. Where no
knob exists, set the internal variable or the property on the component's state class in `_brand.scss`, scoped to
the scheme, and tell the user it is a gap worth reporting upstream (AGENTS.md section 6), unless this file already
names its report.

`BitNav` shows how this goes wrong. Its selected item takes its fill, label and icon from three variables, and only
the fill and label have knobs (`--bit-Nav-selected-background`, `--bit-Nav-selected-color`): the icon reads the
internal `--bit-nav-clr-icon` (`.bit-nav-sel .bit-nav-iic`, bitfoundation/bitplatform#13637), so a bright selected
fill leaves the icon in its idle color - near-white on a light pill in the dark scheme. And `.bit-nav-ict:hover`
outranks `.bit-nav-sel` (bitfoundation/bitplatform#13638), so hovering the selected item swaps its fill for the hover
fill: with a brand-colored selection, the selection vanishes under the pointer. Ask the same questions of pivots,
checkboxes and toggles (the mark on a brand fill), choice groups, pagination, tags, selected rows and the calendar's
selected date.

### 4.4 What the browser paints

Some pixels come from the browser, not from bit, and no token reaches them. Decide each in `_brand.scss`, per scheme:

- **Autofill.** Chrome, Edge and Safari repaint autofilled fields with their own background and text color - a dark
  field turns pale blue or yellow - and bit handles that at most in `BitOtpInput`. Paint over it with an inset shadow
  in the color the field shows at rest (`--bit-clr-bg-pri` below). `BitTextField` paints its background with
  `--bit-tfl-clr-bg`, which its `Background` parameter sets and which can be `transparent`; for a transparent field,
  use the surface behind it.

  ```scss
  input:-webkit-autofill, input:-webkit-autofill:hover, input:-webkit-autofill:focus, textarea:-webkit-autofill {
      -webkit-text-fill-color: var(--bit-clr-fg-pri);
      caret-color: var(--bit-clr-fg-pri);
      -webkit-box-shadow: 0 0 0 1000px var(--bit-clr-bg-pri) inset;
  }
  ```
- **Placeholders.** `BitTextField`, `BitSearchBox` and the date and time pickers leave `::placeholder` at the browser
  default (`#757575` in Chrome, about 4:1 on a dark canvas; bitfoundation/bitplatform#13639), and older versions leave
  more inputs that way. One low-specificity rule fills exactly those gaps, because the inputs that color their own
  placeholder outrank it: `::placeholder { color: var(--bit-clr-fg-sec); opacity: 1; }`, with a tier that clears
  4.5:1 on every field background (the `opacity` undoes Firefox's dimming).
- **Text selection.** `::selection` falls back to the system highlight; give it the reference's treatment if it has
  one, with legible text.
- **Native widgets** - scrollbars, date and time pickers, `<select>` lists - follow `color-scheme`, which the presets
  set per scheme. A surface painted in the opposite scheme (a dark hero in light mode) needs its own `color-scheme`.
  `accent-color` colors any native checkbox, radio, range or progress that bit does not draw.
- **Forced colors.** Under `forced-colors: active` bit switches to system colors; make sure your own surfaces
  (gradients, images behind text, brand fills) don't hide content there.

### 4.5 Couplings in this app

- **Links** color with `pri`. When `pri` is close to the body text color (an ink primary), underline them -
  `.bit-lnk:not(.bit-lnk-nun) { text-decoration-line: underline; }` - because color alone can't carry a link
  (WCAG 1.4.1).
- **`OrSeparator`** (`Components/Pages/Identity/Components/OrSeparator.razor`) masks its line with a background color
  kind: `Secondary` on wide screens, `Primary` on narrow ones and in the sign-in modal. The surface behind it has to
  stay on that token, or the "or" sits in a visible box.
- **`BitImage`** keeps the inner image at its intrinsic size unless `.bit-img-img { width: 100%; height: auto; }` - it
  matters as soon as replacement art has another aspect ratio.

## 5. Carry the brand past the tokens

| What | Where | Note |
| --- | --- | --- |
| Native chrome and first paint | `LightBackground` and `DarkBackground` in `src/Client/Boilerplate.Client.Core/Infrastructure/Services/AppThemePresets.cs` | They must return the new `--bit-clr-bg-pri` of each scheme as literals, or the Windows caption, the Android status bar and the WebView meet the page at a visible seam on launch and on every theme switch. `[mirror]`: the `theme-color` metas of both `index.html` hosts and `theme_color` in `src/Client/Boilerplate.Client.Web/wwwroot/manifest.json`. `App.razor` reads `AppThemePresets`, and `Scripts/theme.ts` rewrites the metas once the page runs, so the literals decide the first paint |
| Accent picker | `<BitAccentColorSwitcher>` in `src/Client/Boilerplate.Client.Core/Components/Layout/Header/AppMenu.razor` | A picked accent re-derives a whole palette inline on `<body>`, which beats the brand. Remove it, or limit it to brand accents, and report that as a behavior change. `<BitAccentColorHead>` in `App.razor` still re-applies an accent a returning visitor stored: list it as a follow-up |
| Logo | `wwwroot/images/bit-logo.svg`, the `BitNavPanel` `IconUrl` in `src/Client/Boilerplate.Client.Core/Components/Layout/AppShell.razor`; `src/Client/Boilerplate.Client.Web/wwwroot/images/icons/bit-logo.png`, the WebAuthn `ServerIcon` in `src/Server/Boilerplate.Server.Api/Program.Services.cs` | It must read on both schemes' surfaces: swap a dark-surface variant in with `content: url(...)` under the dark selectors |
| Identity art | the `BitImage` in `AppShell.razor` (`wwwroot/images/identitylayout-image.webp`) | On-brand art for each scheme, or a recolor; never the form markup |
| Home and shell | `Components/Pages/Home/HomePage.razor` with its `.razor.cs` and `.razor.scss`; `Components/Layout/Header/Header.razor`, `Components/Layout/Header/IdentityHeader.razor`, `Components/Layout/NavBar.razor`, `Components/Layout/MainLayout.razor`, `Components/Layout/AppShell.razor` and their `.scss` | Home may be restructured into the reference's header or hero with Bit components; keep its existing links and where they lead, and every selector the UI tests use (section 1) |
| Illustrations | `src/Client/Boilerplate.Client.Core/wwwroot/images/401.svg`, `403.svg`, `404.svg`; the monogram in `Components/Common/AppInfoCard.razor.scss` | Recolor the SVG fills to the palette; editing existing files is safe while the app runs. The monogram already paints a `pri` to `inf` gradient: check the pair still belongs together |
| Loaders | the fallback color in `Components/Common/LoadingComponent.razor.cs`, `src/Client/Boilerplate.Client.Web/Components/AppBswupProgressBar.razor`, the loader styles in both `index.html` hosts | `[mirror]`; they paint before `app.css` loads, so their literal colors and font stack are the first thing users see |
| App icons and splash | in `src/Client/Boilerplate.Client.Web/wwwroot/`: `favicon.ico`, `images/icons/bit-icon-512.png` (the manifest and touch icon) and `images/icons/bit-logo.png`; `src/Client/Boilerplate.Client.Maui/Resources/AppIcon/appicon.svg` and `Resources/Splash/splash.svg`, with the `Color` of `MauiIcon` and `MauiSplashScreen` in that project's csproj and the matching `background_color` in `manifest.json`; the `favicon.ico` in `src/Client/Boilerplate.Client.Maui/wwwroot/` and in `src/Client/Boilerplate.Client.Windows/wwwroot/`, which is also the Windows app's icon | Only from an official asset, only with the app stopped; the native ones need a rebuild |
| Emails | `src/Server/Boilerplate.Server.Api/Features/Identity/Components/*Template.razor` and their logo, `src/Server/Boilerplate.Server.Api/wwwroot/images/icon.png` | Email clients can't read CSS variables: literal brand colors and a web-safe font stack. Replace the logo in place: a second `images/icon.png` under `Client.Web` breaks the build with a duplicate static asset. In scope for a real brand change, not for a look-alike demo |

Then hunt for what is left of the old look rather than trusting this table: search the code, case-insensitively, for
`bit-logo`, `identitylayout-image`, `#1276C6`, `#1B6EC2`, `#0D2960`, `#0065EF`, `#0078D4`, `#457FCC`, `#C7E0F4` and
`Segoe UI`, and for the old `theme-color` literals, and look on screen for the preset's accent color and font. Replace
every hit, or list it as a follow-up.

## 6. See the app as it is

Drive the running web app (AGENTS.md section 4) with your browser automation, set up so it cannot mislead you:

- **Before the first change**, take the "before" captures (the key cells of the matrix in 7.1) and the token
  snapshot of 7.4, in both schemes.
- **Disable the HTTP cache** for the whole session (CDP `Network.setCacheDisabled`): hot-reloaded static files keep
  their ETag, so a stale stylesheet, font or image can survive into the next round.
- **Load each scheme natively**: before navigating, store the preset name where the app reads it -
  `localStorage['bit-current-theme']` and, for the server-rendered host, the cookie `bit-theme-preference`
  (`fluent2-light` or `fluent2-dark`). Switching after load leaves stateful UI, such as the theme toggle, showing the
  old state.
- **Clear any stored accent** (local storage keys and cookies with `accent` in their name): it overlays its own
  palette.
- **Reach states the way a user does.** A click leaves the pointer where it clicked, so what you see next is
  *selected + hover* until you move the pointer away. A state you expected and don't see is a finding, not a pass.
- **Sign in** with the seeded test account (`src/Tests/Features/Identity/TestData.cs`) for the authenticated pages.

## 7. Verify states, not pages

### 7.1 The state matrix

Cover every row in both schemes at desktop (about 1440px) and phone (about 390px) widths, plus one pass at a tablet
width (about 800px - the shell changes form at 600px and 960px) and one in an RTL culture (`/fa-IR/...`, or the
language picker in the app menu):

| Axis | Cover |
| --- | --- |
| Surfaces | every anonymous page (home, sign-in, sign-up, forgot and reset password, confirm, terms and privacy, about, not found, not authorized) and every authenticated one (`src/Shared/PageUrls.cs` and its partial files), with their add and edit forms and dialogs |
| Focus | every Tab stop on every page (7.3) |
| Pointer | hover and pressed on each button variant and color in use, links, nav items, rows, tabs, chips |
| Selection | the current nav item, the selected tab, checked checkboxes, radios and toggles, selected rows, the selected date, the current page of a pagination - each also hovered and focused |
| Input | empty (placeholder), filled, invalid (submit an empty form), required, disabled, read-only, autofilled |
| Feedback | busy buttons, loaders, shimmers, progress, empty states, messages and snackbars of every severity |
| Overlays | dropdown and combo lists, the date picker, callouts and menus, dialogs and modals (the sign-in modal), panels (the nav panel on a phone, the AI chat panel if the app has one), tooltips |
| Preferences | `prefers-reduced-motion: reduce` and `forced-colors: active`, once each |

Real input - Tab, pointer, typing, submitting - is the most faithful way into a state. For a state that is awkward to
reach, pin it for a capture (CDP `CSS.forcePseudoState` forces `:hover`, `:focus-visible` or `:active` on a node), or
render a temporary specimen of a component family in all its states on an existing page and revert it before you
finish. Automation cannot produce browser autofill: check that the rule from 4.4 is there, and list "look at an
autofilled sign-in" as a hand check in the report.

Short on time? These cells find the most: the dark scheme with a selected nav item, the pointer off it and on it; the
Tab walk through sign-in; filled, invalid and placeholder fields; an open dropdown and a dialog; home and sign-in at
phone width and in RTL.

### 7.2 Measure contrast instead of judging it

Screenshots are for judgment; contrast is for measurement. In each state, run this function in the page through your
browser tool's evaluate. It checks every visible text, input value and placeholder, icon-font glyph (drawn in
`::before`) and SVG shape against the backdrop actually behind it - every translucent layer composited down to the
first opaque one - and returns what falls below WCAG AA (4.5:1 for text, 3:1 for large text, icons and SVG), worst
first. Disabled UI is exempt and skipped, and so is anything over a gradient or an image: judge those by eye.

```js
() => {
  const rgba = s => { const m = /rgba?\(([^)]+)\)/.exec(s || ''); if (!m) return null; const [r, g, b, a = 1] = m[1].split(/[\s,/]+/).filter(Boolean).map(Number); return [r, g, b, a]; };
  const over = (f, b) => [0, 1, 2].map(i => f[i] * f[3] + b[i] * (1 - f[3])).concat(1);
  const lum = c => c.slice(0, 3).map(v => (v /= 255) <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4).reduce((s, v, i) => s + v * [0.2126, 0.7152, 0.0722][i], 0);
  const ratio = (a, b) => { const x = lum(a), y = lum(b); return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05); };
  const canvas = rgba(getComputedStyle(document.body).backgroundColor) || [255, 255, 255, 1];
  const backdrop = el => {
    const fills = [];
    for (let e = el; e && e !== document.documentElement; e = e.parentElement) {
      const cs = getComputedStyle(e);
      if (cs.backgroundImage !== 'none') return null;
      const c = rgba(cs.backgroundColor);
      if (c && c[3] > 0) { fills.push(c); if (c[3] === 1) break; }
    }
    return fills.reverse().reduce((b, f) => over(f, b), canvas);
  };
  const onScreen = el => { const r = el.getBoundingClientRect(), cs = getComputedStyle(el); return r.width > 0 && r.height > 0 && r.bottom > 0 && r.top < innerHeight && r.right > 0 && r.left < innerWidth && cs.visibility !== 'hidden' && +cs.opacity > 0.05; };
  const where = el => `${el.tagName.toLowerCase()}${[...el.classList].slice(0, 2).map(c => '.' + c).join('')} "${(el.innerText || el.getAttribute('aria-label') || el.getAttribute('placeholder') || '').trim().slice(0, 30)}"`;
  const hex = c => '#' + c.slice(0, 3).map(v => Math.round(v).toString(16).padStart(2, '0')).join('');
  const textNeed = cs => parseFloat(cs.fontSize) >= 24 || (parseFloat(cs.fontSize) >= 18.66 && +cs.fontWeight >= 700) ? 3 : 4.5;
  const out = [];
  for (const el of document.body.querySelectorAll('*')) {
    if (!onScreen(el) || el.closest('[disabled],[aria-disabled="true"],.bit-dis')) continue;
    const cs = getComputedStyle(el);
    let fg = null, need = 0, kind = '';
    if ((el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement) && !['checkbox', 'radio', 'range', 'color', 'file', 'hidden'].includes(el.type)) {
      if (el.value) { fg = rgba(cs.color); kind = 'value'; }
      else if (el.placeholder) { fg = rgba(getComputedStyle(el, '::placeholder').color); kind = 'placeholder'; }
      need = textNeed(cs);
    } else if ([...el.childNodes].some(n => n.nodeType === 3 && n.textContent.trim())) {
      fg = rgba(cs.color); need = textNeed(cs); kind = 'text';
    } else if (!['none', 'normal', '""'].includes(getComputedStyle(el, '::before').content) && el.children.length === 0 && el.getBoundingClientRect().width <= 48) {
      fg = rgba(cs.color); need = 3; kind = 'icon';
    } else if (el instanceof SVGGraphicsElement && !(el instanceof SVGSVGElement) && !(el instanceof SVGGElement)) {
      fg = rgba(cs.fill !== 'none' ? cs.fill : cs.stroke); need = 3; kind = 'svg';
    }
    if (!fg || fg[3] === 0) continue;
    const bg = backdrop(el.ownerSVGElement || el);
    if (!bg) continue;
    const r = ratio(over(fg, bg), bg);
    if (r < need) out.push({ ratio: +r.toFixed(2), need, kind, fg: hex(over(fg, bg)), bg: hex(bg), where: where(el.ownerSVGElement?.parentElement || el) });
  }
  return out.sort((a, b) => a.ratio - b.ratio).slice(0, 25);
}
```

Each entry is a defect to fix or a deviation to justify in the report; an empty result in every state is the bar. Run
it after every fix, in the states the fix touches. This is what catches a selected icon left near-white on a bright
fill (1:1, and easy to miss in a full-page screenshot) or a placeholder a few tenths short of 4.5:1. It cannot see
focus rings, control boundaries or text over images; judge those on captures (7.3).

### 7.3 Focus

Tab through every page from the top, in both schemes and in RTL. Capture each stop cropped to the focused element
plus a margin - a 2px ring disappears when a full-page screenshot is scaled down - and capture the same crop with the
element unfocused: identical crops mean there is no visible indicator. At every stop the indicator clears 3:1 against
what surrounds it, is not clipped by an ancestor's `overflow`, is not hidden under a sticky header or panel, and
matches the reference's focus treatment. The order follows the reading order (mirrored in RTL), and no stop lands on
something invisible.

### 7.4 The token ledger

Run this in each scheme before your first change and again at the end, and diff the two. Every token whose value has
not changed must belong to a family you decided to keep. That is how you know the fate of every variable in
`_bit-css-variables.scss`, which only aliases these tokens. Stylesheets from other origins, such as Google Fonts, can't
be read and are skipped.

```js
() => {
  const names = new Set();
  const walk = rules => { for (const r of rules) { if (r.cssRules) walk(r.cssRules); if (r.style) for (const p of r.style) if (p.startsWith('--bit-')) names.add(p); } };
  for (const sheet of document.styleSheets) { try { walk(sheet.cssRules); } catch { } }
  const cs = getComputedStyle(document.body);
  return Object.fromEntries([...names].sort().map(n => [n, cs.getPropertyValue(n).trim()]).filter(([, v]) => v));
}
```

### 7.5 Review against the reference

Compare each round's captures with the reference's, and fix the biggest gaps first:

1. Recognizable as the reference within a second.
2. Color: the brand color where the reference puts it, and a role mapping that holds in both schemes.
3. Typography: family, weights, scale, tracking, case.
4. Shape and elevation, per component family.
5. States drawn the way the reference draws them.
6. A dark scheme as good as the light one, judged on its own rather than as an inversion.
7. Nothing left of the old look (section 5).
8. Nothing broken: overflow, clipped or truncated text, overlaps, misalignment at any width, wrong mirroring in RTL.

If your environment has subagents, a reviewer that did not write the theme sees what its author misses: give it the
reference captures, the spec and a handful of the latest captures, and bound its time.

### 7.6 Rounds and tests

Work in rounds - change, verify the matrix, fix - two at least, and until section 8 holds. After the last change, stop
the app, run `dotnet test` in `src/Tests` under a timeout, start the app again, and change nothing after the tests
pass.

## 8. Done means

- Every token family has a recorded decision for light and dark, and the ledger shows nothing kept by accident.
- The contrast audit comes back empty in every state of the matrix, or each remaining entry is a justified deviation.
- Every Tab stop shows a visible, unclipped focus indicator, in both schemes.
- Rest, hover, selected, selected + hover and selected + focus are told apart for every selectable component in use.
- Autofill, placeholders, text selection and native widgets are styled for both schemes.
- Nothing of the old look remains, or every leftover is a listed follow-up.
- The native chrome color equals `--bit-clr-bg-pri` in both schemes (`AppThemePresets` and its mirrors).
- The RTL culture and the phone and tablet widths hold up.
- The identity markup, the routes and the text are unchanged, the build has no Sass warnings, and `dotnet test` passes.

## 9. Report

Write `.playwright-mcp/rebrand/<slug>/report.md`, in the language the request was written in, and show it:

- The decisions a designer would want to revisit first: the role mapping, font substitutes, the dark palette, and
  everything marked *derived*.
- Before, after and reference captures of the key surfaces and states, in both schemes.
- Deviations from the reference, and why (accessibility, licensing, platform limits).
- What you could not verify (autofill by hand, the native icons), and the follow-ups: assets that need a stop or a
  rebuild, emails, a persisted accent.
- Gaps in bit itself - a missing knob, a part no token reaches - offered to the user for an upstream report.
- `git diff --stat`.
