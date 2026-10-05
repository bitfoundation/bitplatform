---
name: rebrand
description: Re-skins the whole app so it looks and feels like a design reference - a live website or app, a Figma file, screenshots or mockups, brand guidelines, a design-system spec or token file, or only a brand name or a described mood. It decides every bit BlazorUI token family for light and dark (color roles, neutrals, elevation, shape, focus, typography, density, motion) plus fonts, logo, the home page, the navigation and the not-found page, then proves the result state by state - focused, hovered, selected, disabled, invalid, autofilled, open overlays, RTL, phone - with measured contrast. Existing behavior, text and the identity flows stay unchanged.
---

# Rebrand: re-skin the app to match a design reference

You are a senior design engineer. You get a reference and perhaps a few words about the feel; you return the same app,
working as before, wearing that design language in both color schemes, both text directions, at every width and in
every interactive state, together with the evidence that it holds up. Every value the reference leaves open is yours
to decide. The person who asked reviews the result, not a questionnaire.

Re-skins fail in three predictable ways, and this file is built around them:

1. **The reference is read by eye** - colors guessed from a screenshot, fonts from a glance, states never looked at
   (section 2).
2. **Only what shows gets translated** - the obvious tokens change, the rest of the old preset stays, and the parts no
   token reaches keep their old look (sections 3-5).
3. **It is proven on pages at rest** - while most defects a re-skin leaves behind exist only in a state: a focused
   control, a selected item under the pointer, an autofilled field, an open dropdown, the dark scheme, an RTL culture,
   a phone (sections 6-7).

Paths that don't start with `src/` are relative to `src/Client/Boilerplate.Client.Core/`.

## 1. Ground rules

- **Scope**: by default, everything the user sees, while everything that works keeps working - theme tokens, SCSS,
  fonts, images and logos, plus the markup of the Home page, the header and its user area, the nav bar, the nav panel
  and the not-found page wherever the reference's look needs it: a hero on Home, the reference's navigation features,
  one new menu item that shows them (section 5). Follow a request that widens or narrows that, and still verify
  everything a change touches: a new font moves line heights, a new radius can clip a focus ring.
- **Unchanged unless the request says otherwise**: what existing pages, menu items and actions do, the routes,
  validation, every existing user-visible string, and the markup of the identity flows under
  `Components/Pages/Identity/`, which you restyle through tokens and scoped SCSS only. New text goes through
  `Localizer["..."]` literals. The UI tests in `src/Tests` find elements by title, placeholder, accessible name, text
  and some class names: search them before you restructure any markup, and keep every selector they use working.
- **Decide; don't interview.** Write each open decision down with a one-line reason and move on. Ask only when the
  reference itself is out of reach (a private Figma file, a dead link), and never rebuild a brand from memory and
  present it as the reference.
- **Every scheme, direction and width.** Light and dark, the RTL cultures the app ships, phone to desktop. A reference
  usually shows one of each: design the rest in its spirit, and say you did.
- **Accessibility outranks fidelity.** WCAG 2.2 AA in both schemes: text 4.5:1 (3:1 for large text), icons and
  control boundaries 3:1, a visible focus indicator on every focusable element, nothing carried by color alone. Where
  the reference falls short, keep its character, change the mapping (4.1), and list the deviation.
- **Public first, hacks on the record.** Reach each look through what bit BlazorUI documents as public - design
  tokens, per-component variables, parameters, APIs - and look it up with its MCP tools (AGENTS.md section 3) instead
  of guessing. When nothing public gets there, a hack is allowed, but undocumented markup and class names change
  without notice: keep every style hack in one partial, `Styles/_brand-hacks.scss`, so it goes in one deletion once
  bit offers a public way, keep markup hacks minimal, tell the user each one deserves an upstream issue and offer to
  file it (AGENTS.md section 6), and list it in the report (section 9).
- **Project conventions** (`AGENTS.md`) apply, `[mirror]` comments included. Values live in `.scss`; `**/*.css` is
  build output.
- **Branch and commits**: work on `rebrand/<slug>` unless the current branch is already dedicated to this work; commit
  only when asked.
- **Other people's marks**: a third party's logo, name, artwork or licensed font is fine in an internal demo or a
  mockup, not in anything that ships. Say which applies in the report.

### The running app

<!--#if (aspire == true)-->
Verify against the app while it runs under `aspire start` with hot reload (AGENTS.md section 4).
<!--#else-->
Verify against the app while it runs under `dotnet watch` in `src/Server/Boilerplate.Server.Web` with hot reload
(AGENTS.md section 4).
<!--#endif-->

- Style and markup edits reach the open page on their own. Batch what needs a rebuild, such as native app icons, and
  stop the app before `dotnet test`, because the running app holds the build outputs.
- The client build treats Sass warnings as errors.
- When a style edit does not show up, check that the generated stylesheet contains it before you debug the rule. C#
  that already ran, such as startup code and field initializers, changes only after a restart.
- The Content Security Policy is off in Development and strict everywhere else
  (`Components/Layout/ContentSecurityPolicy.razor`), so an image or font loaded from another origin works locally and
  breaks in production. Self-host such assets, or use only what that policy allows.

## 2. Read the reference into a spec

The reference can arrive as anything - a URL, a Figma link, screenshots, a PDF, a DESIGN.md, a token export, a
product name, a sentence about a mood, or a mix of them - and you read it with whatever tools you have. The output of
this step is always the same: a written spec precise enough that another engineer could build the theme from it
without ever seeing the reference. Keep it, with every capture you take, in `.playwright-mcp/rebrand/<slug>/` at the
repository root (git-ignored scratch; `<slug>` names the reference).

The spec covers, for light and for dark:

- **Identity**: three personality words, and the three to five signature moves that make the reference recognizable
  within a second ("pill buttons in ink", "one saturated color, only ever on large surfaces", "hairlines instead of
  shadows").
- **Vocabulary**: what the reference calls its own sections and features, so that what you add speaks its language.
- **Color by role**, each value with its source: brand accents, text tiers, canvas and surface tiers, borders, links,
  the selected or current indication, focus, status colors, the modal scrim.
- **Typography**: families and their licensed substitutes; the size, weight, line-height and tracking ramp; the case
  of buttons, labels and headings.
- **Shape and elevation**: the radius of each component family, stroke widths, and how layers separate - shadow, tint
  or hairline - in each scheme.
- **Density and motion**: control heights, paddings, the spacing rhythm; durations and easing, or the intended feel.
- **States, as the reference draws them**: hover, pressed, focus, selected or current, disabled, invalid,
  placeholder. These are your targets in section 7, so capture them, not just pages at rest.
- **Imagery and composition**: the logo in a version for light and for dark surfaces, illustration and icon style, and
  the composition of the header and its user area, the navigation and its features (featured items, badges,
  counters), the landing area, and the not-found page.

Prefer measured and stated values to impressions, and know what each kind of evidence can tell you:

- **Stated** sources - official brand guidelines, a design-system spec, Figma variables, token files, DESIGN.md -
  carry intent: brand colors, type, logo rules. They rarely cover control heights, focus rings or dark mode. Official
  guidelines outrank third-party write-ups.
- **A live site or app** carries what actually shipped: read computed styles rather than pixels, and capture its
  states, its not-found page and its dark mode if it has one. Its marketing pages are not its product UI.
- **Images** carry composition and feel. Their colors drift with compression and scaling: sample flat areas, never
  anti-aliased edges, and treat the values as approximate.
- **A name or a mood** alone: find the official sources yourself; for a mood, choose concrete references and name them.
- Where sources disagree, stated intent wins for brand values and measured UI wins for UI details. Mark every value the
  reference does not give as *derived*, with the reason.

Two things are easy to get wrong:

- **Fonts.** A proprietary family gets a licensed substitute: the one the brand's own guidelines name, otherwise the
  closest open-license face. Self-host it whenever the app must render offline (MAUI, Windows, the PWA), and give a
  Latin-only family per-script fallbacks for the app's non-Latin cultures, or those cultures drop to a system font.
  Decide the digits each culture expects as well: native digits where its readers expect them, Latin digits for codes,
  versions and identifiers.
- **Logos.** An SVG shown through `<img>` needs its `xmlns` attribute and cannot inherit `currentColor`; markup copied
  out of a web page usually has neither. Keep a version for each surface the logo sits on.

## 3. The base preset, then where the theme goes

bit BlazorUI paints everything from `--bit-*` design tokens - color roles and neutrals, shape, elevation, typography,
size, spacing, motion and layout - plus documented per-component variables, so a theme is data, and a preset is one
complete set of values for them. Read the theming guide (`GetBitBlazorUIThemingGuide`) before you decide anything,
at least its chapters on design tokens, presets, and color derivation and contrast.

### 3.1 Settle the base preset first

<!--#if (theme == "Fluent2")-->
This app is built on bit BlazorUI's Fluent 2 preset (`fluent2-light` and `fluent2-dark`). bit also ships Material and
Cupertino.
<!--#elif (theme == "Fluent")-->
This app is built on bit BlazorUI's Fluent preset (`fluent-light` and `fluent-dark`). bit also ships Fluent 2,
Material and Cupertino.
<!--#elif (theme == "Material")-->
This app is built on bit BlazorUI's Material preset (`material-light` and `material-dark`). bit also ships Fluent 2
and Cupertino.
<!--#elif (theme == "Cupertino")-->
This app is built on bit BlazorUI's Cupertino preset (`cupertino-light` and `cupertino-dark`). bit also ships Fluent 2
and Material.
<!--#endif-->

Compare them with the spec: the preset whose shape, elevation, density, type and motion already sit closest to the
reference leaves the least to re-value. If another preset is closer, switch the app to it first - the presets chapter
shows how - and change everything that pins the current one together: `AppThemePresets`
(`src/Client/Boilerplate.Client.Core/Infrastructure/Services/AppThemePresets.cs`), the preset stylesheet and theme
names of the host pages, and every `[mirror]` counterpart they name. Record the choice with its reason, and close it
before anything below: every later decision re-values this preset.

### 3.2 Where the values go

Re-value the base preset's light and dark themes in place, in one new file, `Styles/_brand.scss`, imported from
`Styles/app.scss` after the abstracts. Use the preset's own selectors - `:root[bit-theme="..."]` for each of its two
theme names, each paired with its descendant twin `:root [bit-theme="..."]` - as the presets chapter prescribes for
authoring a preset. `app.css` loads after the preset, so equal specificity wins, and theme persistence, the
server-rendered first paint and the light/dark toggle keep working with no C# change.

- Put what both schemes share in one place and include it in both.
- Style hacks, if any, go to `Styles/_brand-hacks.scss`, imported right after `_brand.scss`, never into it.
- If `_brand.scss` already exists, revise it rather than stacking a second layer on top.
- If `app.scss` holds a commented-out background override with a `[mirror]` note, replace it with the import and move
  the note to `_brand.scss`, which now owns the page background.
- Keep the reasons for your values in the spec and the report, not in comments.
- `Styles/abstracts/_bit-css-variables.scss` only maps tokens to SCSS variables, so it follows the brand on its own:
  don't edit it to re-skin.

**Derive the palette; don't hand-pick ramps.** bit BlazorUI derives a whole color role from one main color the way
its own palettes are built; the theming guide shows how. Run it in a throwaway program outside the repository, on the
bit BlazorUI version the project uses, generate every role, then pin the brand's exact values on top. Derivation does
not decide everything: check the hue the neutrals pick up, the focus color of every role, and status colors that
should keep their conventional hues.

## 4. Decide every token family

Work from the decisions that move the most pixels to those that move the fewest: role mapping and neutrals,
typography, shape and elevation, states, signature moves, then the wiring of section 5.

### 4.1 Role mapping comes first

bit paints the primary role both as a **fill** (filled buttons, checked controls, sliders, progress, badges) and as a
**foreground** on the page (links, text and outline buttons, icons, the selected indicator of navigation). So before
the brand color gets the role, measure it against the primary and secondary backgrounds in each scheme:

- **4.5:1 or better**: it can be the primary as it is.
- **Below that**, choose one and record why:
  - (a) the primary is the brand's ink (its near-black or deep navy), and the brand color moves to surfaces and
    signature fills - the app bar, the hero, the main call to action;
  - (b) the primary is a deeper tone of the brand color that clears 4.5:1, if it still reads as the brand;
  - (c) per scheme: the bright brand color as the primary in dark, where it usually passes, with (a) or (b) in light.
- The on-color of the role must clear 4.5:1 on every fill of the role, the darker steps included. Bright fills need a
  dark on-color.
- When the primary ends up close to the body text color, links need more than color to stand out: underline them.

As a rule of thumb, bright yellows, greens, cyans, oranges and pinks fail on white; deep blues, indigos, violets and
reds pass.

### 4.2 Every other family

Each family ends with a decision - changed, derived, or kept with a reason. "Kept" counts only if you looked.

- **Accents and status**: secondary, tertiary and info from the brand's secondary colors, or derived; success,
  warning and error keep their conventional hues, harmonized toward the brand a few degrees at most. Measure any tone
  you use as text: bright fills rarely pass.
- **Neutrals**: canvas and surface tiers, text tiers, borders, the overlay scrim. Every text tier clears 4.5:1 on
  every surface tier; control boundaries clear 3:1. A dark canvas sits off pure black unless the brand's is black.
- **Elevation**: per surface family. Dark schemes need stronger shadows or a hairline instead; a flat reference
  separates with borders.
- **Shape**: the radius of each component family and the stroke widths. An ancestor's `overflow: hidden` clips focus
  rings.
- **Focus**: ring width, offset and color, for every role. The ring clears 3:1 on every surface a control can sit on,
  brand-colored ones included.
- **Typography**: families, the type ramp, weights, line heights, tracking and case, the `BitText` variants included.
- **Size and density**: control heights and paddings, icon and selection sizes, minimum widths, the density scale.
- **Motion**: durations and easing, set the way the theming guide describes so that reduced motion still collapses
  them.
- **Signature moves**: documented component knobs, and app SCSS for the app's own surfaces.

### 4.3 Every state is a decision

A component in a selected, checked, current or active state paints a fill, a label and often an icon. For every such
component the app shows, decide all three, and their combinations with hover and focus: navigation items (the new
one with its badge or featured treatment included), tabs, checkboxes and toggles, choice groups, pagination, tags,
selected rows, the selected date.

### 4.4 What bit does not paint

Some pixels come from the browser or a third party, not from bit, and no token reaches them. Decide each, per scheme:

- **Autofill**: browsers repaint autofilled fields with their own background and text colors; restyle them to the
  field's resting colors.
- **Text selection**: give it the reference's treatment, with legible text.
- **Native widgets** - scrollbars, native pickers, `<select>` lists - follow `color-scheme`. A surface painted in the
  opposite scheme (a dark hero in light mode) needs its own.
- **Embedded widgets** - a captcha, a map, a video or payment frame - paint themselves, often in light only: match
  each to the scheme around it through its own settings, or frame it so it does not clash.
- **Forced colors**: make sure your own surfaces (gradients, images behind text, brand fills) don't hide content
  under `forced-colors: active`.

## 5. Carry the brand past the tokens

| What | Where | Note |
| --- | --- | --- |
| Native chrome and first paint | `AppThemePresets` and its `[mirror]` counterparts | They must equal the new page background of each scheme, or the window caption, the status bar and the WebView meet the page at a visible seam |
| Accent picker | `<BitAccentColorSwitcher>` in `Components/Layout/Header/AppMenu.razor` | A picked accent overrides the brand. Remove it, or limit it to brand accents, and report that as a behavior change |
| Logo and identity art | the nav panel logo and the identity side image in `Components/Layout/AppShell.razor`; the WebAuthn server icon in `src/Server/Boilerplate.Server.Api/Program.Services.cs` | It must read on both schemes' surfaces. Replace assets in place rather than adding copies |
| Home | `Components/Pages/Home/` | May become the reference's hero or landing composition, built with Bit components; its existing links keep leading where they lead |
| Header, nav bar and nav panel | `Components/Layout/` | Take the reference's navigation features - featured items, badges, counters - and the look of its user area, while every existing item and action does what it did |
| A new menu item | the nav bar and the nav panel | Add one item to both, named in the reference's vocabulary rather than "404" or "Not found", pointing at a route the app does not have, so that it opens the re-skinned not-found page. When the reference has navigation features such as a badge or a featured treatment, this item carries them in both menus |
| Not-found page | `Components/Pages/NotFoundPage.razor` and the illustration it shows | Make it look like the reference's own not-found page, or one in its spirit when the reference shows none |
| Before the stylesheet loads | the loaders and the update progress bar of the host pages, and `Components/Common/LoadingComponent.razor` | `[mirror]`; their literal colors and fonts are the first thing users see |
| App icons and splash | the favicons, PWA icons, the MAUI app icon and splash | Only from an official asset; the native ones need a rebuild |
| Emails | the identity email templates of `src/Server/Boilerplate.Server.Api` | Email clients can't read CSS variables: literal brand colors and a web-safe font stack. In scope for a real brand change, not for a look-alike demo |

<!--#if (module == "Admin")-->
**Charts**: give the Dashboard's charts a series palette drawn from the brand through the chart's per-component
variables, with every series told apart from its neighbors and legible on both schemes' surfaces, and axes, grid lines
and legend in the brand's neutrals.

<!--#endif-->
Then hunt for what is left of the old look rather than trusting this table: search the code for literal colors and
font names outside the theme layer, and look on screen for the preset's accent color and typeface. Replace every hit,
or list it as a follow-up.

## 6. See the app as it is

Drive the running web app with your browser automation (AGENTS.md section 4), set up so it cannot mislead you:

- **Before the first change**, capture the key cells of the matrix in 7.1 and a snapshot of every `--bit-*` token, in
  both schemes.
- **Disable the HTTP cache** for the session: a stale stylesheet, font or image can survive into the next round.
- **Load each scheme natively**: set the persisted theme preference before navigating rather than toggling after load,
  which leaves stateful UI such as the theme toggle showing the old state.
- **Clear any stored accent**: it overlays its own palette.
- **Reach states the way a user does.** A click leaves the pointer where it clicked, so what you see next is
  *selected + hover* until you move it away. A state you expected and don't see is a finding, not a pass.
- **Sign in** with the seeded test account (`src/Tests/Features/Identity/TestData.cs`) for the authenticated pages.

## 7. Verify states, not pages

### 7.1 The state matrix

Cover every row in both schemes at desktop (about 1440px) and phone (about 390px) widths, plus one pass at a tablet
width, one in an RTL culture, and one in the culture with the longest strings, where labels beside badges and inside
buttons truncate first:

| Axis | Cover |
| --- | --- |
| Surfaces | every anonymous page and every authenticated one (`src/Shared/PageUrls.cs`), with their forms and dialogs, and the not-found page the new menu item opens |
| Focus | every Tab stop on every page (7.3) |
| Pointer | hover and pressed on each button variant and color in use, links, nav items, rows, tabs, chips |
| Selection | the current nav item in both menus (the new one too, once it is clicked), the selected tab, checked checkboxes, radios and toggles, selected rows, the selected date, the current page - each also hovered and focused |
| Input | empty (placeholder), filled, invalid (submit an empty form), required, disabled, read-only, autofilled |
| Feedback | busy buttons, loaders, shimmers, progress, empty states, messages and snackbars of every severity |
| Overlays | dropdowns, date pickers, callouts and menus, dialogs and modals, panels, tooltips |
| First visit | empty storage: the consent banner and any other first-run UI, which a reused session never shows again |
| Preferences | `prefers-reduced-motion: reduce` and `forced-colors: active`, once each |

Real input - Tab, pointer, typing, submitting - is the most faithful way into a state. For a state that is awkward to
reach, force it for a capture, or render a temporary specimen of a component family in all its states and revert it
before you finish. Automation cannot produce browser autofill: check that its styling is in place, and list "look at
an autofilled sign-in" as a hand check.

Short on time? These cells find the most: the dark scheme with a selected nav item, the pointer off it and on it; the
new menu item in both menus and the not-found page it opens; the Tab walk through sign-in; filled, invalid and
placeholder fields; an open dropdown and a dialog; home and sign-in at phone width and in RTL.

### 7.2 Measure contrast instead of judging it

Screenshots are for judgment; contrast is for measurement. In each state, measure in the page every visible text,
input value, placeholder, icon, badge and SVG shape against the backdrop actually behind it, with translucent layers
composited, and list everything below WCAG AA, worst first. Disabled UI is exempt, and anything over a gradient or an
image is judged by eye. Re-run it after every fix, in the states the fix touches: an empty list in every state is the
bar.

### 7.3 Focus

Tab through every page from the top, in both schemes and in RTL. Capture each stop cropped to the focused element
plus a margin, and the same crop unfocused: identical crops mean there is no visible indicator. At every stop the
indicator clears 3:1 against what surrounds it, is not clipped or hidden under a sticky header, and matches the
reference's focus treatment. The order follows the reading order, mirrored in RTL.

### 7.4 The token ledger

Snapshot the computed value of every `--bit-*` token in each scheme before your first change and after the last, and
diff them. Every token whose value did not change must belong to a family you decided to keep.

### 7.5 Review against the reference

Compare each round's captures with the reference's, and fix the biggest gaps first:

1. Recognizable as the reference within a second.
2. Color: the brand color where the reference puts it, and a role mapping that holds in both schemes.
3. Typography: family, weights, scale, tracking, case.
4. Shape and elevation, per component family.
5. States drawn the way the reference draws them.
6. Composition: the landing area, the navigation and its features, and the not-found page arranged the way the
   reference arranges them.
7. A dark scheme as good as the light one, judged on its own rather than as an inversion.
8. Nothing left of the old look (section 5).
9. Nothing broken: overflow, clipped or truncated text, overlaps, misalignment at any width, wrong mirroring in RTL.

If your environment has subagents, a reviewer that did not write the theme sees what its author misses: give it the
reference captures, the spec and the latest captures, and bound its time.

### 7.6 Rounds and tests

Work in rounds - change, verify the matrix, fix - two at least, and until section 8 holds. After the last change, stop
the app, run `dotnet test` in `src/Tests` under a timeout, start the app again, and change nothing after the tests
pass.

## 8. Done means

- Every token family has a recorded decision for light and dark, and the ledger shows nothing kept by accident.
- The contrast measurement comes back empty in every state of the matrix, or each remaining entry is a justified
  deviation.
- Every Tab stop shows a visible, unclipped focus indicator, in both schemes.
- Rest, hover, selected, selected + hover and selected + focus are told apart for every selectable component in use.
- Autofill, text selection and native widgets are styled for both schemes.
- The new menu item sits in both menus, named in the reference's vocabulary and carrying its navigation features, and
  opens the re-skinned not-found page.
- Nothing of the old look remains, or every leftover is a listed follow-up.
- The native chrome color equals the page background in both schemes.
- The RTL culture, the culture with the longest strings, and the phone and tablet widths hold up.
<!--#if (module == "Admin")-->
- The Dashboard's charts carry the brand's palette in both schemes.
<!--#endif-->
- Every hack is in `_brand-hacks.scss` or listed in the report, with its upstream issue offered.
- Existing pages, items and actions work as before, the identity markup, the routes and the existing text are
  unchanged, the build is clean, and `dotnet test` passes.

## 9. Report

Write `.playwright-mcp/rebrand/<slug>/report.md`, in the language the request was written in, and show it:

- The decisions a designer would want to revisit first: the role mapping, the base preset, font substitutes, the dark
  palette, the new menu item's name, and everything marked *derived*.
- Before, after and reference captures of the key surfaces and states, in both schemes.
- Deviations from the reference, and why (accessibility, licensing, platform limits).
- What you could not verify (autofill by hand, the native icons), and the follow-ups: assets that need a rebuild,
  emails, a persisted accent.
- **Hacks and issues to file**: each hack - what it does, where it lives, the public API that would replace it - with
  a ready-to-file issue for each, offered to the user.
- `git diff --stat`.
