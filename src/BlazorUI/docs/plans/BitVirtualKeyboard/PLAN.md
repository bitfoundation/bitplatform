# BitVirtualKeyboard implementation plan

This plan is written for an AI coding agent that will implement a new **BitVirtualKeyboard** component in the
`Bit.BlazorUI.Extras` package. It is self-contained: every decision the agent needs is made here, with the reason, and
every repository convention it must follow is pointed to by file and line. Where this plan and `src/BlazorUI/CLAUDE.md`
disagree, **CLAUDE.md wins**; record the disagreement in the hand-off.

The plan was fact-checked on 2026-11-25 against the repository, the web platform (with live runs in Chromium, Firefox
and WebKit), the keyboard layout sources and a compiled prototype of the C# model; section 16 records what was checked.

Companion files in this folder:

| File | What it holds | When to read it |
|---|---|---|
| `PLAN.md` (this file) | Decisions, API, model, behaviour, phases, tests, done criteria | Start to finish before writing code |
| `layout-data.md` | Row-by-row key data of every language layout (Windows layout tables and ISIRI 9147, re-verified) | Phase 4, as the single source of truth for key characters |
| `research-report.md` | The survey this plan is built on (prior art, sources, rationale). Its row-data section is superseded by `layout-data.md` | When a decision's reason is unclear |

---

## 0. Ground rules for the agent

1. **Read `src/BlazorUI/CLAUDE.md` in full before writing any code** (about 1,140 lines). The sections this component
   touches most: BOM (line 24), trimmed scripts and stylesheets (95), Razor comments (179), One name per concept (199),
   parameter order (228), unique root class (246), Extras is the composition layer (306), Escape claim (379), disabled
   activation and markup (427, 457), RTL without `:dir()` and Dir resolution (497, 518), culture-invariant numbers
   (558), bUnit `Key` (577), Demo pages (590), MCP server (721), theme tokens in SCSS (815), public and private CSS
   variables (951), "A state moves a variable, never the paint" (1108). Line numbers drift; search by heading.
2. **Also read** `src/CLAUDE.md` (JS interop payload rules), `src/BlazorUI/Tests/E2E/README.md` (E2E rules),
   `src/BlazorUI/docs/accessibility/README.md` (manual screen-reader verification, required here; see phase 9) and
   `src/BlazorUI/Tests/Bit.BlazorUI.Tests/Components/Extras/ResourceTimeline/Scripts/README.md` (Node script tests).
3. **Edit boundary:** write only inside `src/BlazorUI`. Never edit the repo root, other `src/` siblings or CI files.
4. **Every new `.cs`, `.razor`, `.scss`, `.ts`, `.md` file starts with a UTF-8 BOM** (`EF BB BF`). The Write tool does
   not add one. After creating a file, check it (`head -c 3 <file> | xxd` shows `efbb bf`) and prepend the BOM when it
   is missing. `SourceFilesBomTests` fails CI otherwise; it is the check that fails most often.
5. **Git:** never create branches, commits or pushes, and never stage files. The user commits.
6. **Do not guess a convention.** When this plan says "follow X", open X and copy its shape. Templates:
   - `Bit.BlazorUI.Extras/Components/ImageViewer/` (commit `0a94559a09`): non-input component with a TS module, a JS
     object handle, Strings, Params, ClassStyles, and E2E page and tests.
   - `Bit.BlazorUI.Extras/Components/CopyButton/` (commit `225adacd78`, the newest): every demo, catalog and contract
     registration point.
   - `Bit.BlazorUI.Extras/Components/MarkdownEditor/BitMarkdownEditor.razor.cs` (around lines 551-580 and 1640-1662):
     `[JSInvokable("X")] _X` callbacks and `DisposeAfterAsync` teardown.
   - `Bit.BlazorUI.Extras/Components/ResourceTimeline/` with `BitResourceTimelineEngine.ts` and its tests: a separate
     engine script tested in Node.
   - `Bit.BlazorUI/Components/Surfaces/Accordion/BitAccordion.razor.cs` (around 143-156 and 611-618) and
     `Collapse/BitCollapse.razor.cs` (around 684): a `[TwoWayBound]` value with its `DefaultX`.
7. **Work phase by phase** (section 10). Each phase ends with a build and its tests green before the next starts.
   Keep a running log of deviations from this plan and of anything left undone; it goes in the hand-off (section 13).
8. **Stop and ask the user** only for the open questions in section 15, or when a CLAUDE.md rule cannot be satisfied.
   Otherwise take the decision this plan gives.

---

## 1. Goal and scope

**Goal.** A data-driven on-screen keyboard for Blazor (Server, WebAssembly, Auto, Blazor Hybrid; under static SSR it
renders its first layer but is interactive only in an interactive render mode) that:

- ships a catalog of **purpose layouts** (Phone, PIN, Numeric, Decimal, Signed decimal, Calculator, POS, Hex, Date,
  Time, IBAN, ABC kiosk grid, ATM; Email, URL and Search as variations of the text layouts) and **language layouts**
  (P0 tier: English US and UK, Persian, Arabic, Hebrew, German, French legacy AZERTY, Spanish, Russian, Turkish Q), each
  in a **Full** (hardware-like) and a **Compact** (phone-like) form;
- lets users define **any custom layout** through a compact string shorthand or a full C# object model, also loadable
  from and savable to JSON: rows, relative widths and heights, gaps, layers (shift, caps, AltGr, symbols, anything),
  long-press alternates, multitap, dead keys, per-purpose key variations, custom command keys;
- types correctly into real inputs: at the caret, replacing the selection, deleting the way the browser does, firing
  the events `@bind` listens to, keeping focus in the input, and suppressing the OS keyboard;
- is accessible (WCAG 2.2 AA), RTL-correct, themeable through bit's tokens and CSS variables, and trim-safe.

**Market position (from the research).** Among the major Blazor suites only Blazorise ships an on-screen keyboard; none
of the .NET keyboards examined documents RTL, long-press alternates or dead keys. Those, plus accessibility and a broad,
correct language catalog, are the differentiators. Do not cut them from v1.

### 1.1 In scope for v1

- Layout model, shorthand parser, JSON converters, layout compiler and validator.
- The purpose layouts above; the P0 language tier in Full and Compact forms; shared symbol layers.
- Modes: **Inline** (rendered in place) and **Docked** (fixed to the bottom of the viewport, opens and closes).
- Targets: bound `Value` (no DOM input), one explicit input, or **follow focus** across opted-in inputs.
- Shift (one-shot, double-tap to lock), Caps Lock, AltGr, symbol layers, layer return (`NextLayer`).
- Long-press popup (pointer, tap-only and keyboard reachable), multitap, dead keys, key repeat.
- Enter behaviour from `enterkeyhint`; Tab / Shift+Tab / arrows / Home / End / Clear / Accept / Cancel keys.
- Text engine in TypeScript (caret insertion, browser-like Backspace, events, maxlength, allowed-characters filter,
  `type=email` and `type=number` handling), focus retention, `inputmode="none"` handling.
- Accessibility: buttons, names, `aria-pressed`, live region, roving focus in value mode, forced colors, reduced motion
  through the motion tokens, 24 px minimum target.
- Size presets, theming (CSS variables, ClassStyles, `KeyTemplate`), Strings (localizable names), Params (cascading
  defaults).
- Secure mode (no press echo, no text to .NET), scramble on open, physical-keyboard highlight, optional key sound and
  vibration.
- Demo page, bUnit tests, Node tests of the text engine, E2E tests, all registrations.

### 1.2 Out of scope for v1 (keep the model ready, do not implement)

- **Popup** mode anchored to the input and a **Floating/draggable** mode (no enum members yet; see 15).
- Flick gestures (only the direction enum is reserved), regex transforms, CLDR keyboard3 / Keyman importers.
- Composition engines (Korean Hangul, Japanese kana-kanji, Chinese pinyin) and the candidate bar.
- P1 / P2 language tiers (their data is in `layout-data.md` for a follow-up).
- Switch scanning, gamepad navigation, split and one-handed modes, emoji panel, clipboard keys, swipe typing,
  `contenteditable` targets, per-keystroke scrambling, exact Windows CapsLock semantics per key.
- Integration parameters on core inputs (`BitTextField` and friends): the core cannot reference Extras.

---

## 2. Decisions already made (do not re-open)

| # | Decision | Reason |
|---|---|---|
| D1 | Name **`BitVirtualKeyboard`**, folder `Bit.BlazorUI.Extras/Components/VirtualKeyboard/`, root class **`bit-vkb`** | "Virtual keyboard" is the common term (Telerik, Qt, Avalonia, MathLive, W3C). `bit-vkb` is unused in every package (re-grep before use; `BitRootClassContractTests`). |
| D2 | Inherits **`BitComponentBase`**, not `BitInputBase` | In target modes it edits someone else's input; in value mode it is a value source, not a form field. |
| D3 | **Hybrid architecture**: C# owns configuration, layout resolution, compilation and rendering of *all layers of the current layout*; TypeScript owns interaction state (active layer, modifiers, pending dead key, long-press, multitap, repeat), text insertion and focus | Zero-latency typing and key repeat on Blazor Server (no SignalR round trip per key); one code path for every render mode. |
| D4 | Layout semantics follow **CLDR LDML Keyboard 3.0** (key bag, layers, rows of key references, long-press, multitap, dead-key markers) with geometry from Keyman / Qt / GNOME (relative width, height, gap, offset, stretch) and per-purpose **variations** from FlorisBoard / HeliBoard | CLDR (version 49, 2026-10-21) is the only vendor-neutral standard; the others solved touch geometry and duplication. |
| D5 | Layout types are **`sealed record`s with `init` properties and no `required` members** (the compiler validates instead); built-in layouts are **created fresh on every access**; JSON goes through explicit converters and a source-generated context | `with { ... }` customisation; a `with` copy is shallow, so shared static instances would leak edits across Blazor Server circuits; System.Text.Json never uses implicit operators and reflection JSON is not trim/AOT-safe. |
| D6 | **The key grid is never mirrored** for RTL; each layer is `direction: ltr`. Text direction comes from the layout (`Direction`) and the target | RTL hardware layouts keep physical key positions and change only characters (Windows layout data). Brackets are already swapped in the data. |
| D7 | **Default form `Auto`**: Compact under a container width breakpoint (default 640 px), Full above | Phone-like on narrow screens, hardware-like on kiosks and desktops. |
| D8 | Frame-key icons are **inline SVG** inside the component, not icon-font glyphs | The core and Extras font subsets lack Back / ReturnKey / KeyboardClassic / Globe; adding glyphs means cutting the Extras woff2. Custom keys can still use `BitIconInfo`. |
| D9 | **Scramble and secure mode are opt-in** | Scrambling defeats shoulder-surfing but hurts accessibility and muscle memory, and is weak against screen capture. |
| D10 | Text insertion fires a cancelable `beforeinput`, then `input`; the engine fires `change` itself on commit, without duplicating a native one | Blazor's `@bind` listens to `change` (or `input`); browsers never fire `change` for a script-set value, but do fire it after physical typing in the same focus session. |
| D11 | Keys are `<button type="button">`. In target modes they have `tabindex="-1"` and the keyboard cancels `pointerdown` so focus stays in the input. In value mode the keyboard is a roving-tabindex grid | Focus retention is the most common failure of web keyboards (verified: cancelling `pointerdown` keeps focus for mouse in all three engines and for touch in Chromium and Firefox). Value mode has no input to keep focus in. |
| D12 | The built-in catalog is **static C# data** written from `layout-data.md` (Compact forms derived by a helper) | Trim-safe, no runtime parsing of data files, reviewable. |
| D13 | **Backspace deletes one code point**, except a whole emoji sequence | Matches Chromium and Firefox native behaviour (they delete a combining mark, a ZWNJ or a harakat alone); grapheme deletion would remove the Persian letter with a mistyped ZWNJ. |
| D14 | **Script-owned state is marked only with attributes Razor never renders** (`data-bit-vkb-*`, `hidden` on script-filled containers) | Blazor only patches attributes present in its render frames, so these survive re-renders of a retained element; classes and styles Razor renders would be overwritten. |

---

## 3. Repository facts the agent relies on

Verified on 2026-11-25 against develop at `6e83ea7d7e`. Re-check line numbers; they drift.

### 3.1 Component anatomy

- Base: `Bit.BlazorUI/Components/BitComponentBase.cs`: `RootElement` (53), `RootElementClass` (467), `ClassBuilder`
  (476), `StyleBuilder` (485), `RegisterCssStyles`/`RegisterCssClasses` (494/504), `bit-dis`/`bit-rtl` registered by the
  base (441-442), `DisabledAwareHtmlAttributes` (private protected, 757), `IsRtlAsync` (638), `IsRtlArrowAsync` (678,
  internal), `DisposeAsync(bool disposing)` (951), `IsDisposed` (19), `IsDisposing` (30). Base parameters: `AriaLabel`,
  `Class`, `Dir`, `Disabled`, `ForceAnimation`, `HtmlAttributes`, `Id`, `Style`, `TabIndex`, `Visibility`.
- Parameter attributes (internal in core, visible to Extras through `InternalsVisibleTo`): `[TwoWayBound]`,
  `[ResetClassBuilder]`, `[ResetStyleBuilder]`, `[CallOnSet(nameof(M))]`, `[CallOnSetAsync]`, `[Outranks(nameof(X))]`
  (a params-cascade hint). The source generator (`Bit.BlazorUI.SourceGenerators/Component/ComponentSourceGenerator.cs`)
  writes `SetParametersAsync`, and for each `[TwoWayBound]` parameter `X` it **generates** `XChanged`,
  `private bool XHasBeenSet` and `Task<bool> AssignX(T value)`. Never declare any of them; the class must be `partial`;
  never declare `SetParametersAsync`. `AssignX` returns `false` without assigning when `X` is bound one way (set, with no
  `XChanged` delegate).
- `DefaultX` pattern (copy it): `[Parameter] public bool? DefaultExpanded` beside the `[TwoWayBound]` value, and in
  `OnInitializedAsync`: `if (ExpandedHasBeenSet is false && DefaultExpanded.HasValue) await AssignExpanded(DefaultExpanded.Value);`
  (`BitAccordion.razor.cs` 611-618). For this component: `DefaultOpen` (`bool?`) and `DefaultValue` (`string?`, applied
  when `ValueHasBeenSet is false && DefaultValue is not null`).
- Bindable naming: `X` / `XChanged` / `DefaultX`; no `Is` prefix on own state (CLAUDE.md "One name per concept").
- Params: `public partial class BitVirtualKeyboardParams : BitComponentBaseParams, IBitComponentParams`;
  `public const string ParamName = $"{nameof(BitParams)}.{nameof(BitVirtualKeyboard)}";`; a `[DynamicDependency]`
  public constructor; `public string Name => ParamName;`; `UpdateParameters(BitVirtualKeyboard c)` calling
  `UpdateBaseParameters(c)` then `c.TakeFromCascade(nameof(P), P.Value, static x => x.P, static (x, v) => x.P = v)` per
  member. Cascading parameter `[CascadingParameter(Name = BitVirtualKeyboardParams.ParamName)]`; call
  `CascadingParameters?.UpdateParameters(this)` first thing in `OnParametersSet` (and early in `OnInitializedAsync`
  only if a cascaded value is read there).
- Strings: `BitVirtualKeyboardStrings` with settable English defaults; `[Parameter] BitVirtualKeyboardStrings? Strings`;
  `private static readonly BitVirtualKeyboardStrings _defaultStrings = new();`; `ActiveStrings => Strings ?? _defaultStrings`
  (copy `BitImageViewerStrings`).
- JS callbacks: `[JSInvokable("OnX")] public async Task _OnX(...)` (the underscore avoids clashing with the `OnX`
  EventCallback parameters), each with `[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(T))]` for its
  argument and result records, and each starting with `if (IsDisposing) return;` (or returning a default result).
- Disposal: `await _dotnetObj.DisposeAfterAsync(async () => { try { await _handle.InvokeVoidAsync("dispose"); await _handle.DisposeAsync(); } catch (JSException) { } })`
  (`Bit.BlazorUI/Extensions/DotNetObjectReferenceExtensions.cs`; it already swallows `JSDisconnectedException`,
  `OperationCanceledException` and `ObjectDisposedException`). With it, every `[JSInvokable]` must read `IsDisposing`
  (`BitJsInvokableDisposalContractTests` scans the IL; `IsDisposed` alone fails it).
- Reused core enums: `BitInputMode` (None, Text, Decimal, Numeric, Tel, Search, Email, Url), `BitEnterKeyHint` (Enter,
  Done, Go, Next, Previous, Search, Send), `BitDir` (Ltr, Rtl, Auto), `BitSize`, `BitIconInfo` (+ `BitIconInfo.From`).
  `BitCssClasses.Size(Size, "bit-vkb")` yields `bit-vkb-sm/md/lg` or nothing when unset.
- Model types of large Extras components live in a `Models/` subfolder, one type per file, enums included
  (`ResourceTimeline/Models/*`).

### 3.2 Scripts

- One `.ts` per script next to the component; `<TypeScriptFiles Include="**\*.ts" />` picks it up (no csproj edit).
  `tsconfig.json`: `strict`, `target ES2019`, `lib ["DOM", "ESNext"]`, `outFile`.
- Shared files: `Bit.BlazorUI.Extras/Scripts/Extras.ts`, `general.ts` (`DotNetObject`), `BitBlazorUI.d.ts` (declares the
  core members Extras scripts use; `isComposingKey`, `isRtl`, `invokeDotNet` and `notifyDotNet` are already there;
  **add `escapeClaimer(e: Event): Element | null`**, defined in `Bit.BlazorUI/Scripts/Utils.Escape.ts` 55).
- Two scripts:
  - `BitVirtualKeyboard.ts`: `namespace BitBlazorUI { export class BitVirtualKeyboard { public static setup(...) } }`,
    the DOM side. Runs nothing at load (listeners are added by `setup`), so it needs no `bit-trim` pragma and is kept by
    its literal interop identifier.
  - `BitVirtualKeyboardEngine.ts`: pure functions as statics of `BitBlazorUI.BitVirtualKeyboardEngine` (no DOM access),
    carrying `// bit-trim: on-demand` exactly as `BitResourceTimelineEngine.ts` (line 2) does. Its top level declares
    only `BitBlazorUI` namespace members. Mirror how `BitResourceTimeline.ts` uses its engine (calls and load order).
- C# calls `"BitBlazorUI...."` identifiers only through the loader helpers (`InvokeScriptAsync<T>`,
  `InvokeScriptVoidAsync` (internal), `InvokeVoid`, `Invoke<T>`, `EnsureScriptAsync`) in
  `Bit.BlazorUI/Extensions/JsInterop/IJSRuntimeExtensions.cs`, always with **string-literal identifiers**, from an
  `internal static class BitVirtualKeyboardJsRuntimeExtensions` (`BitVirtualKeyboardSetup`, ...). Calls on the returned
  `IJSObjectReference` handle (`_handle.InvokeVoidAsync("update", ...)`) go directly, as in BitImageViewer.
- Payloads: named `internal sealed record`s (camelCase on the wire) with
  `[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(T))]` at the call site for **every** payload type
  including nested and element types. Never anonymous types (src/CLAUDE.md).
- In TS: read IME composition only through `BitBlazorUI.Utils.isComposingKey(e)`, never `.isComposing` or
  `keyCode === 229` (`BitEscapeClaimContractTests.TheCompositionOfAKeyShouldBeReadInOnePlace`). Call .NET with
  `Utils.notifyDotNet` for fire-and-forget and `Utils.invokeDotNet` when the result is awaited.
- Build through `dotnet build` (the csproj `BuildJavaScript` target runs `Bit.BlazorUI.Build/build-assets.mjs`). Never
  run `build-assets.mjs` by hand: it leaves stale outputs that break later Release builds.

### 3.3 Styles

- `BitVirtualKeyboard.scss` starts with `@import '../../../Bit.BlazorUI/Styles/functions.scss';` (its only preamble),
  documents its public variables in a header comment (copy `BitImageViewer.scss` 1-13), and is imported in
  `Bit.BlazorUI.Extras/Styles/extra-components.scss` right after the `Virtualize` import.
- Public variables `--bit-VirtualKeyboard-*` are read with a fallback and never declared, and every one must be listed
  in the demo's `componentCssVariables` (`BitComponentCssVariablesContractTests`). Private variables `--bit-vkb-*` reset
  through a `$vkb-private-properties` list with `@each`; read order private, then public, then default; a state moves a
  variable, never the paint (CLAUDE.md 951-1117). Theme tokens only (`$clr-*`, `$shp-radius-*`, `$tg-fs-*`,
  `$siz-ctrl-*`, `$mot-easing`, `mot-scaled-duration()`, `spacing()`), never literals.
- **Motion:** no `prefers-reduced-motion` query of its own; durations go through `mot-scaled-duration()`
  (`BitReducedMotionDurationContractTests`).
- **RTL:** `:is(.bit-rtl, :dir(rtl))` is only for the root element itself; parts use
  `:is(:dir(rtl), .bit-vkb.bit-rtl > … > *)` with child combinators, or a rule inside `@supports not selector(:dir(rtl))`
  (CLAUDE.md 497-517, `BitComponentDirFallbackTests`). The keyboard needs almost none: the grid is LTR (D6) and icon
  flipping follows the layout's direction through a C#-rendered class (5.6).
- Part classes are `bit-vkb-<3 letters>`.

### 3.4 Registration points (commits `225adacd78` and `0a94559a09`)

1. `Bit.BlazorUI.Extras/Components/VirtualKeyboard/**` (component files).
2. `Bit.BlazorUI.Extras/Styles/extra-components.scss` (+1 `@import`, after `Virtualize`).
3. `Bit.BlazorUI.Extras/Scripts/BitBlazorUI.d.ts` (+ `escapeClaimer`).
4. `Demo/Client/Bit.BlazorUI.Demo.Client.Core/Shared/MainLayout.razor.NavItems.cs`: one item in the `Extras` group after
   `Virtualize`: `new() { Text = "VirtualKeyboard", Url = "/components/virtualkeyboard", Description = "OnScreenKeyboard, TouchKeyboard, SoftKeyboard", Data = "Keyboard, On-screen keyboard, Touch keyboard, Soft keyboard, Kiosk, POS, Numpad, PIN pad, Dial pad", AdditionalUrls = ["/components/virtual-keyboard"] },`.
   The nav item drives the catalog, search, pager, sitemap and MCP answers.
5. `Demo/Client/Bit.BlazorUI.Demo.Client.Core/Models/ComponentCatalog.cs`: `["VirtualKeyboard"] = BitIconName.KeyboardClassic,`
   in the `// Extras` glyph block and a one-line summary in the `// Extras` summary block.
6. Demo page folder `Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/Extras/VirtualKeyboard/`
   (`BitVirtualKeyboardDemo.razor`, `.razor.cs`, `.razor.samples.cs`, `.razor.scss`).
7. `Tests/Bit.BlazorUI.Tests.Mcp/ComponentCatalogTests.cs`: `[DataRow("BitVirtualKeyboard", "BitVirtualKeyboardParams")]`.
8. `Tests/Bit.BlazorUI.Tests/Utils/BitDisabledActivationContractTests.cs`: a `Components` entry (a hand-written list;
   no `RequiredParameters` entry is needed since the component renders with no parameters).
9. Unit tests `Tests/Bit.BlazorUI.Tests/Components/Extras/VirtualKeyboard/` (with `Scripts/` for the Node tests).
10. E2E: `Tests/E2E/Bit.BlazorUI.Tests.E2E.TestHost.Client/Components/Pages/Extras/BitVirtualKeyboardE2E.razor`
    (`@page "/e2e/virtual-keyboard"`; discovered by reflection, and `HostE2ETests` visits it, so it must load without
    console errors) and `Tests/E2E/Bit.BlazorUI.Tests.E2E/Extras/BitVirtualKeyboardE2ETests.cs`.
11. Then run the whole unit suite: the contract tests (`BitRootClassContractTests`, `BitParameterNamingContractTests`,
    `Params/BitIconCascadeTests`, `Theme/*`, `DemoPageContractTests`, ...) name anything else that is missing.

Not needed: csproj edits, `_Imports.razor`, `CompatibilitySuppressions.xml`, `BreakingChanges.*` (new API only),
README lists, `compilerconfig.json`, `BitStrings.cs`, icon-font subset changes (D8).

---

## 4. Public API specification

All public types live in `namespace Bit.BlazorUI;`. Component files in the component folder; model types, enums,
args and contexts in `VirtualKeyboard/Models/`, one type per file. XML doc comments on every public member (the MCP
server and the demo read them).

### 4.1 Parameters of `BitVirtualKeyboard` (declare in this alphabetical order, case ignored)

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `AllowedCharacters` | `string?` | `null` | A regular-expression **character-class body** (for example `0-9.,`). Each typed grapheme must match `new RegExp('^[' + body + ']+$', 'u')`, checked in JS. |
| `AutoShift` | `bool` | `true` | For layouts without a shift layer, derive one (5.4 step 5). |
| `Classes` | `BitVirtualKeyboardClassStyles?` | `null` | Per-part CSS classes. `[ResetClassBuilder]`. |
| `CompactBreakpoint` | `double` | `640` | Container width in CSS px under which `Form = Auto` renders the Compact form. |
| `Culture` | `CultureInfo?` | `null` (current culture) | Separators, default language. |
| `DefaultOpen` | `bool?` | `null` | Uncontrolled initial `Open`. |
| `DefaultValue` | `string?` | `null` | Uncontrolled initial `Value` (value mode). |
| `Digits` | `BitVirtualKeyboardDigits` | `Latin` | Digit glyphs the purpose pads type, and the decimal separator they use (5.3). |
| `EnterBehavior` | `BitVirtualKeyboardEnterBehavior` | `Auto` | What Enter does (6.7). |
| `FollowFocus` | `bool` | `false` | Attach to whichever opted-in input gets focus (6.9). |
| `Form` | `BitVirtualKeyboardForm` | `Auto` | `Auto`, `Full`, `Compact`. |
| `Haptics` | `bool` | `false` | Vibrate on key press where `navigator.vibrate` works (6.13). |
| `KeyTemplate` | `RenderFragment<BitVirtualKeyboardKeyContext>?` | `null` | Renders the content of every key (the button stays the component's). |
| `Language` | `string?` | `null` | Id of the language layout (`"en"`, `"fa"`, ...). Null: the culture's two-letter name if in the catalog, else `"en"`. |
| `Languages` | `IReadOnlyList<string>?` | `null` | Layout ids the `lang` key cycles through; the key is dropped when fewer than two. |
| `Layout` | `BitVirtualKeyboardLayout?` | `null` | A complete custom layout. Wins over `Language`. |
| `LayoutSelector` | `Func<BitVirtualKeyboardLayoutContext, BitVirtualKeyboardLayout?>?` | `null` | Picks a layout per target and purpose; a null result falls back to the built-in resolution. |
| `LongPressDelay` | `int` | `500` | Milliseconds a key is held before its alternates open. |
| `MaxLength` | `int?` | `null` | Value-mode limit in UTF-16 code units; in target modes the target's own `maxlength` applies. |
| `Mode` | `BitVirtualKeyboardMode` | `Inline` | `Inline` or `Docked`. |
| `MultiTapTimeout` | `int` | `1000` | Milliseconds within which a repeated tap cycles a multitap key. |
| `OnCommand` | `EventCallback<BitVirtualKeyboardCommandArgs>` | | A `Custom` key was pressed. |
| `OnEnter` | `EventCallback` | | Enter was pressed (after the Enter behaviour ran). |
| `OnInput` | `EventCallback<string?>` | | The text changed (value mode: the new value; target modes: the target's new value). Never raised in secure mode (6.12). The script only reports target changes when this has a delegate. |
| `OnKeyPress` | `EventCallback<BitVirtualKeyboardKeyPressArgs>` | | Before a key acts; handlers may set `Cancel` or replace `Text`. Only when this has a delegate does the script wait for .NET before acting. |
| `OnLayerChange` | `EventCallback<string>` | | The active layer changed (id). |
| `OnLayoutChange` | `EventCallback<BitVirtualKeyboardLayout>` | | The resolved layout changed (language key, purpose, form). |
| `Open` | `bool` | `false` | Docked-mode visibility. `[Parameter, TwoWayBound]` (generates `OpenChanged`). Ignored in Inline mode. |
| `Purpose` | `BitVirtualKeyboardPurpose?` | `null` | Forces a purpose. Null: detected from the target (6.10), else `Text`. |
| `RepeatDelay` | `int` | `500` | Milliseconds before a held repeatable key starts repeating; `0` disables repeat. |
| `RepeatInterval` | `int` | `50` | Milliseconds between repeats. |
| `ReserveSpace` | `bool` | `true` | Docked mode: the root keeps the panel's height in the page flow so content can scroll above the keyboard. |
| `Scramble` | `bool` | `false` | Shuffle the positions of the scramblable keys (5.4 step 7) each time the keyboard opens or attaches. |
| `Secure` | `bool` | `false` | Secure mode (6.12). Also on automatically for `type=password` targets. |
| `ShowKeyPreview` | `bool` | `true` | Show the enlarged-key bubble on press for character keys (never in secure mode). |
| `Size` | `BitSize?` | `null` (Medium) | Key height preset (6.11). |
| `Sound` | `bool` | `false` | Short click through Web Audio (6.13). |
| `Strings` | `BitVirtualKeyboardStrings?` | `null` | Localizable names and announcements. |
| `Styles` | `BitVirtualKeyboardClassStyles?` | `null` | Per-part inline styles. `[ResetStyleBuilder]`. |
| `Target` | `ElementReference?` | `null` | One explicit input or textarea to type into. |
| `TargetSelector` | `string?` | `null` | CSS selector of the inputs `FollowFocus` attaches to. Null: elements carrying the `data-bit-vkb` attribute. |
| `Value` | `string?` | `null` | Value-mode text. `[Parameter, TwoWayBound]` (generates `ValueChanged`). |

**Target precedence:** `Target` (if set) > `FollowFocus` (if true) > value mode. In Inline mode with `FollowFocus` and no
input attached, keys are inert (6.9); it never falls back to value mode.

### 4.2 Public members

| Member | Meaning |
|---|---|
| `ValueTask PressKeyAsync(string keyId)` | Acts as if the key were pressed (`keyId` as in 4.3). |
| `ValueTask SetLayerAsync(string layerId)` | Switches the active layer. |
| `ValueTask AttachAsync(ElementReference input)` / `ValueTask DetachAsync()` | Attach to or detach from an input at run time. |
| `string? ActiveLayer { get; }` | The active layer id as last reported by the script. |
| `BitVirtualKeyboardLayout? ActiveLayout { get; }` | The resolved layout. |

Opening and closing in Docked mode is the `Open` parameter (bind it, or let `FollowFocus` drive it).

### 4.3 Event argument and context types

```csharp
public sealed class BitVirtualKeyboardKeyPressArgs
{
    public string KeyId { get; init; } = "";       // Key.Id ?? built-in name ?? Output
    public BitVirtualKeyboardKeyAction Action { get; init; }
    public string? Text { get; set; }              // what will be typed; a handler may replace it. Null in secure mode.
    public string Layer { get; init; } = "";
    public bool Cancel { get; set; }               // true: the key does nothing
}

public sealed class BitVirtualKeyboardCommandArgs
{
    public string Command { get; init; } = "";
    public string KeyId { get; init; } = "";
}

public sealed class BitVirtualKeyboardLayoutContext
{
    public BitVirtualKeyboardPurpose Purpose { get; init; }
    public string? InputType { get; init; }        // the target's type attribute
    public string? InputMode { get; init; }        // the target's original inputmode (never the "none" the keyboard set)
    public string? EnterKeyHint { get; init; }
    public string? TargetId { get; init; }
    public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;
    public BitVirtualKeyboardForm Form { get; init; }   // Full or Compact (Auto already resolved)
}

public sealed class BitVirtualKeyboardKeyContext
{
    public BitVirtualKeyboardKey Key { get; init; } = default!;   // the compiled key
    public string Label { get; init; } = "";
    public string? Hint { get; init; }
}
```

### 4.4 Enums

```csharp
public enum BitVirtualKeyboardMode { Inline, Docked }
public enum BitVirtualKeyboardForm { Auto, Full, Compact }
public enum BitVirtualKeyboardDigits { Latin, ArabicIndic, Persian }
public enum BitVirtualKeyboardEnterBehavior { Auto, NewLine, Submit, Next, Close, None }
public enum BitVirtualKeyboardRowAlign { Center, Start, End }

public enum BitVirtualKeyboardPurpose
{
    Text, Search, Email, Url, Tel, Numeric, Decimal, SignedDecimal,
    Pin, Calculator, Pos, Hex, Date, Time, Iban, Alphabetic, Atm
}

public enum BitVirtualKeyboardKeyAction
{
    Character,     // types Output
    Backspace, Delete, Enter, Space, Tab, ShiftTab,
    Shift, CapsLock, AltGr,
    SwitchLayer,   // to Key.Layer, stays there
    DeadKey,       // sets the pending marker Key.DeadKey
    NextLayout,    // cycles Languages
    Hide, Clear, Accept, Cancel,
    ArrowLeft, ArrowRight, ArrowUp, ArrowDown, Home, End,
    Custom         // raises OnCommand(Key.Command)
}

[Flags] public enum BitVirtualKeyboardModifiers { None = 0, Shift = 1, AltGr = 2, Caps = 4 }
public enum BitVirtualKeyboardFlickDirection { N, NE, E, SE, S, SW, W, NW }   // reserved for flicks; no behaviour in v1
```

Every public enum of the model carries `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` so JSON layouts read
`"Action": "Backspace"`.

### 4.5 ClassStyles parts (`BitVirtualKeyboardClassStyles`)

`Root`, `Panel` (docked panel), `Layers` (the wrapper of the layers), `Layer`, `Key`, `CharacterKey`, `FunctionKey`,
`ModifierKey`, `ActiveKey`, `PrimaryKey` (Enter), `Gap`, `Blank`, `KeyLabel`, `KeyHint`, `KeyIcon`, `Popup`,
`PopupKey`, `Preview`, `LiveRegion`; each a `string?` with an XML doc ("Custom CSS classes/styles for the ... of the
BitVirtualKeyboard."). Part classes: `bit-vkb-pnl`, `bit-vkb-lys`, `bit-vkb-lyr` (+ `bit-vkb-lyb` on the first layer),
`bit-vkb-key`, `bit-vkb-chr`, `bit-vkb-fnc`, `bit-vkb-mod`, `bit-vkb-pri`, `bit-vkb-gap`, `bit-vkb-blk`, `bit-vkb-lbl`,
`bit-vkb-hnt`, `bit-vkb-ico`, `bit-vkb-pop`, `bit-vkb-ppk`, `bit-vkb-prv`, `bit-vkb-liv`, `bit-vkb-rsv` (the
docked reserve spacer, no part), `bit-vkb-rtl-ico` (5.6), and the key modifier
`bit-vkb-nub` (tactile dot, used through `Key.Class`).

- Razor renders `Root`, `Panel`, `Layers`, `Layer`, `Key`, `CharacterKey`, `FunctionKey`, `ModifierKey`, `PrimaryKey`,
  `Gap`, `Blank`, `KeyLabel`, `KeyHint`, `KeyIcon`, `LiveRegion`, and the **always-rendered, empty** `Popup` and
  `Preview` containers (with their classes and styles).
- The script applies `ActiveKey` and `PopupKey` (it receives their classes and styles in the options) and re-applies
  them after every `update`. Active state itself is the attribute `data-bit-vkb-act` (D14).

### 4.6 Strings (`BitVirtualKeyboardStrings`)

Settable properties with English defaults: `RoleDescription` ("virtual keyboard"), `Backspace`, `Delete`, `Enter`,
`Done`, `Go`, `Next`, `Previous`, `Search`, `Send`, `Space`, `Tab`, `ShiftTab` ("Shift Tab"), `Shift`, `CapsLock`
("Caps lock"), `AltGr`, `Symbols` ("Symbols"), `MoreSymbols` ("More symbols"), `Letters` ("Letters"), `NextLanguage`
("Next language"), `Hide` ("Hide keyboard"), `Clear`, `Accept`, `Cancel`, `ArrowLeft` ("Left"), `ArrowRight`
("Right"), `ArrowUp` ("Up"), `ArrowDown` ("Down"), `Home`, `End`, `Qty` ("Qty"), `Void` ("Void"), `Total` ("Total"),
`Zwnj` ("zero-width non-joiner"), `Zwj` ("zero-width joiner"), `Lrm` ("left-to-right mark"), `Rlm` ("right-to-left
mark"), `Nbsp` ("no-break space"), `ShiftOn` ("Shift on"), `ShiftLocked` ("Shift locked"), `ShiftOff` ("Shift off"),
`CapsLockOn` ("Caps lock on"), `CapsLockOff` ("Caps lock off"), `LayerAnnouncement` ("{0} keyboard"), `Alternates`
("Alternates for {0}").

**Accessible names and WCAG 2.5.3:** a key's name must contain its visible text. Text labels use the label itself
unless the layout gives an `AriaLabel` that contains it. Built-ins with a *textual* label (`ZWNJ`, `ZWJ`, `LRM`, `RLM`,
`NBSP`, `Qty`, `Void`, `Total`, `abc` "ABC", `sym` "?123", a layer key's label) get "`<label>, <Strings name>`" when the
two differ (for example "ZWNJ, zero-width non-joiner", "ABC, Letters", "?123, Symbols"). Icon keys (Backspace, Enter,
Shift, Caps, Tab, Hide, Language, arrows) use the Strings name.

### 4.7 Params (`BitVirtualKeyboardParams`)

Cascaded defaults through `<BitParams>`: `AllowedCharacters`, `AutoShift`, `Classes`, `CompactBreakpoint`, `Culture`,
`Digits`, `EnterBehavior`, `Form`, `Haptics`, `Language`, `Languages`, `LongPressDelay`, `Mode`, `MultiTapTimeout`,
`RepeatDelay`, `RepeatInterval`, `ReserveSpace`, `Secure`, `ShowKeyPreview`, `Size`, `Sound`, `Strings`, `Styles`, plus
the base set. Left out on purpose (say so in the remarks, as `BitPhoneInputParams` does): `Value`, `DefaultValue`,
`Open`, `DefaultOpen`, `Target`, `TargetSelector`, `FollowFocus`, `Layout`, `LayoutSelector`, `KeyTemplate`, `Purpose`,
`Scramble`, `MaxLength`, every event callback.

---

## 5. Layout model

### 5.1 Types (`VirtualKeyboard/Models/`)

```csharp
namespace Bit.BlazorUI;

public sealed record BitVirtualKeyboardLayout
{
    public string Id { get; init; } = "";                       // BCP 47 with CLDR -t-k0- variants: "fa", "fr-t-k0-windows", "x-phone"
    public string Name { get; init; } = "";                     // display name: "فارسی (ISIRI 9147)"
    public string? Indicator { get; init; }                     // short text on the language key: "FA"
    public string? Culture { get; init; }                       // culture name for case mapping and separators: "fa-IR"
    public BitDir Direction { get; init; } = BitDir.Ltr;        // direction of the produced text; the grid stays LTR (D6)
    public BitVirtualKeyboardForm Form { get; init; } = BitVirtualKeyboardForm.Full;  // Full or Compact; never Auto
    public string? CompactOf { get; init; }                     // Compact layouts: the id of their Full layout
    public Dictionary<string, BitVirtualKeyboardKey> Keys { get; init; } = [];        // the key bag (CLDR keys/key)
    public List<BitVirtualKeyboardLayer> Layers { get; init; } = [];                  // first = base layer
    public Dictionary<string, string> LongPress { get; init; } = [];                  // output -> "é è ê ë ē" (space-separated)
    public Dictionary<string, Dictionary<string, string>> DeadKeys { get; init; } = []; // marker -> { "e": "é", " ": "´" }
    public Dictionary<string, string> Displays { get; init; } = [];                   // output or key id -> keycap label

    public static BitVirtualKeyboardLayout FromJson(string json) { ... }   // source-generated context (see JSON below)
    public string ToJson() { ... }
}

public sealed record BitVirtualKeyboardLayer
{
    public string Id { get; init; } = "";                        // "base", "shift", "caps", "altgr", "shift-altgr", "symbols", ...
    public string? Name { get; init; }                           // for the live region and the layer key's default label
    public BitVirtualKeyboardModifiers Modifiers { get; init; }  // which modifier state selects this layer
    public List<BitVirtualKeyboardRow> Rows { get; init; } = [];
}

[JsonConverter(typeof(BitVirtualKeyboardRowJsonConverter))]
public sealed record BitVirtualKeyboardRow
{
    public List<BitVirtualKeyboardKey> Keys { get; init; } = [];
    public BitVirtualKeyboardRowAlign Align { get; init; } = BitVirtualKeyboardRowAlign.Center;
    public static implicit operator BitVirtualKeyboardRow(string shorthand) => BitVirtualKeyboardShorthand.ParseRow(shorthand);
}

public sealed record BitVirtualKeyboardKey
{
    public string? Id { get; init; }                 // bag id or built-in name ("bksp", "layer:symbols")
    public string? Output { get; init; }             // text typed; may be several code points ("لا", ".com", "क्ष")
    public string? Label { get; init; }              // keycap; see 5.4 step 9
    public string? Hint { get; init; }               // small secondary label ("ABC" on 2)
    public BitIconInfo? Icon { get; init; }          // custom-key icon; built-in frame keys draw inline SVG (D8)
    public string? IconName { get; init; }           // resolved with BitIconInfo.From(Icon, IconName)
    public string? AriaLabel { get; init; }          // must contain the visible label
    public BitVirtualKeyboardKeyAction? Action { get; init; }  // null: inherit (bag, built-in), else Character
    public string? Layer { get; init; }              // target of SwitchLayer
    public string? NextLayer { get; init; }          // layer to return to after this key types
    public string? DeadKey { get; init; }            // marker id for Action = DeadKey
    public string? Command { get; init; }            // id for Action = Custom
    public double? Width { get; init; }              // null: inherit, else 1. Key units; multiples of 0.25
    public int? Height { get; init; }                // null: inherit, else 1. Rows spanned
    public double? Offset { get; init; }             // null: inherit, else 0. Empty key units before the key
    public bool? Stretch { get; init; }              // takes the row's leftover width
    public bool? Gap { get; init; }                  // invisible, inert spacer of Width
    public bool? Blank { get; init; }                // visible but inert (ATM blank keys, empty AltGr positions)
    public List<string>? LongPress { get; init; }    // overrides Layout.LongPress[Output]
    public string? LongPressDefault { get; init; }   // pre-selected item in the popup
    public List<string>? MultiTap { get; init; }     // cycle on repeated taps
    public bool? Repeat { get; init; }               // null: Backspace, Delete, Space and arrows repeat; others do not
    public bool? Preview { get; init; }              // null: character keys show the bubble
    public List<BitVirtualKeyboardPurpose>? Purposes { get; init; }  // present only for these purposes
    public Dictionary<BitVirtualKeyboardPurpose, BitVirtualKeyboardKey>? Variations { get; init; } // per-purpose overrides
    public bool? Scramble { get; init; }             // null: single-digit Character keys scramble, others stay put
    public string? Class { get; init; }
    public string? Style { get; init; }
}
```

Notes:
- Every property that takes part in the merge (5.4 step 2) is nullable, so "not given" differs from "given as the
  default". The effective defaults (`Character`, width 1, height 1, offset 0, false) apply only after the merge.
- No `required` members: the compiler validates `Id`, `Name` and every reference with a clear message (5.4 step 1).
- Collections are initialised to `[]`, so object initialisers (`Keys = { ["2"] = new() { ... } }`) and collection
  expressions (`Rows = ["q w e r t y u i o p", "a s d f g h j k l"]`, through the implicit conversion) both compile
  (verified on SDK 11.0.100).
- **`with` is shallow.** `BitVirtualKeyboardLayouts.English with { Id = "my" }` shares the `Keys`, `Layers` and other
  collections of that instance. Built-ins are created fresh on every access (section 8), so this is safe for them, but
  documentation and the demo must show copying a collection before changing it:
  `BitVirtualKeyboardLayouts.English with { Id = "my", Displays = new(BitVirtualKeyboardLayouts.English.Displays) { ["q"] = "Q!" } }`.
  A `with` expression accepts only `member = expression`, never a nested collection initialiser.
- **C# escapes:** in a C# regular string use C# escapes (`"\u0652"`); C# has no `\u{...}` escape (CS1009). The
  shorthand's `\u{...}` escape works in verbatim (`@"..."`) and raw (`"""..."""`) strings; in a regular C# string every
  shorthand backslash is doubled (`"\\u{200C}"`, `"\\\\"` for the shorthand `\\`).
- **JSON:** `BitVirtualKeyboardRowJsonConverter` reads a row from a string (through `BitVirtualKeyboardShorthand.ParseRow`)
  or from an object, and writes an object. `FromJson`/`ToJson` go through a source-generated
  `BitVirtualKeyboardJsonContext` (trim-safe; follow `Bit.BlazorUI.Extras/Utils/BitExtrasJsonContext.cs`), writing
  with `DefaultIgnoreCondition = WhenWritingNull`. JSON allows only `\uXXXX` escapes, so the shorthand escape is
  written `"\\u{200C}"` in JSON (or use the JSON escape, see below). The context sets
  `PropertyNameCaseInsensitive = true`, and the row converter's object branch compares names case-insensitively
  (otherwise `{"id": ...}` would silently read as an empty `Id`). `Direction` carries
  `[JsonConverter(typeof(JsonStringEnumConverter<BitDir>))]` on the property, since `BitDir` is a core enum without
  one. `BitIconInfo` also writes its read-only `IsEmpty`; JSON tests should expect it. JSON escape example: `"\u200C"`).

### 5.2 Shorthand grammar (`BitVirtualKeyboardShorthand.ParseRow`)

One string per row; tokens separated by one or more ASCII spaces. Each token, in this order:

1. **Width suffix.** A token longer than one character that does not start with `{` and matches
   `^(.+)\*([0-9]+(?:\.[0-9]+)?)$` is split into a head and a width (ASCII digits only). The head continues through
   rules 2-4. Examples: `0*2` is a "0" key two units wide; `**2` is a "*" key two units wide; `\\*1.5` is a `\` key 1.5
   wide; `\{*2` is a `{` key two units wide. A literal that itself ends in `*<digits>` cannot be written in shorthand;
   give it through the bag (`Keys = { ["x23"] = new() { Output = "2*3" } }`, token `{x23}`) or the object model.
2. **Escape.** `\\` → `\`; `\s` → a space character key; `\u{HEX}` (1-6 ASCII hex digits) → that code point; `\`
   followed by any other text → that text unchanged (`\{shift}` types `{shift}`); a lone `\` → `\`.
3. **Named key.** A token of three or more characters that starts with `{` **or** ends with `}` must match
   `^\{([a-z][a-z0-9-]*)(?::([^*{}\s]+))?(?:\*([0-9]+(?:\.[0-9]+)?))?\}$` (group 1 the name, group 2 an argument, group
   3 a width); anything else of that shape (`{Shift}`, `{2}`, `{shift}*2`, `{*2`, or `{layer:sym bols}`, which splits
   into `{layer:sym` and `bols}`) is a validation error, never a literal. Lone `{`, `}` and `{}` stay literals. The parser records an unresolved
   reference (`Id` = `name` or `name:arg`, plus `Layer`/`DeadKey`/`Command` from the argument and the inline width);
   the compiler resolves it against `Layout.Keys` (by `name:arg`, then `name`) and then the built-ins (5.3). An unknown
   name is a validation error.
4. **Literal.** Anything else (including a lone `{`, `}`, `{}`, `*`, `:` and a backtick) is a character key. A literal
   whose text is a key of `Layout.Keys` resolves to that bag entry, with `Output` defaulting to the text (this is how
   the phone pad's `2` gets its `ABC` hint). Multi-character literals type the whole string (`.com`, `www.`, `لا`).

Combining marks (`\u{064E}` and friends) are typed as written; their keycaps are drawn on a dotted circle (5.6).

### 5.3 Built-in named keys

All built-ins default to width 1 except `space` (width 2, `Stretch`) and `tld` (1.5). Layout rows give frame-key widths explicitly
(`{bksp*2}`), so purpose pads stay rectangular.

| Name | Action / meaning | Notes |
|---|---|---|
| `bksp` | Backspace | repeats |
| `del` | Delete | repeats |
| `enter` | Enter | label and name from `enterkeyhint` (6.7); `PrimaryKey` part |
| `space` | Space (types U+0020) | width 2, `Stretch = true`; repeats |
| `tab` / `shifttab` | Tab / ShiftTab | |
| `shift` | Shift | one-shot; double-tap locks (6.2) |
| `caps` | CapsLock | |
| `altgr` | AltGr | dropped when the layout has no AltGr layer |
| `layer:<id>` | SwitchLayer to `<id>` | label from `Displays["layer:<id>"]`, else the layer's `Name` |
| `abc` | SwitchLayer to the base layer | label from `Displays["abc"]` or "ABC" |
| `sym` | SwitchLayer to `symbols` | label from `Displays["sym"]` or "?123" |
| `dead:<marker>` | DeadKey with marker | label from `Displays["dead:<marker>"]`, else `DeadKeys[marker][" "]` |
| `cmd:<id>` | Custom with Command `<id>` | label from `Displays["cmd:<id>"]`, else the id |
| `lang` | NextLayout | label is the next layout's `Indicator`; dropped when fewer than two `Languages` |
| `hide` | Hide | dropped in Inline mode |
| `clear` | Clear (empties the value) | |
| `accept` / `cancel` | Accept / Cancel | `cancel` also dispatches a synthetic Escape (6.8) |
| `left` `right` `up` `down` `home` `end` | caret keys | arrows repeat |
| `zwnj` `zwj` `lrm` `rlm` `nbsp` | Character U+200C, U+200D, U+200E, U+200F, U+00A0 | visible text label and composed name (4.6) |
| `gap` | Gap | invisible spacer |
| `blank` | Blank | visible inert key |
| `decimal` | Character: the decimal separator | `Digits = Latin`: the culture's separator if it is `.` or `,`, else `.`; `Persian`/`ArabicIndic`: U+066B. (ICU gives fa-IR and ar-* U+066B even for Latin digits, hence the rule.) |
| `datesep` | Character: the culture's `DateSeparator` with U+200E, U+200F and U+061C removed | ar-SA's is U+200F + "/" |
| `comma` | Character "," | `Variations`: Email "@", Url "/". `fa` and `ar` layouts override `Output` with "،" (U+060C) in their bag |
| `tld` | Character ".com" | `Purposes = [Email, Url]`, width 1.5; long-press ".net .org .edu .io" |
| `cur` | Character "$" | layouts override it in their bag (he ₪, ru ₽, tr ₺, de/fr/es €, en-GB £, fa ﷼) |
| `qty` `void` | Custom commands "qty", "void" | labels from Strings |
| `total` | Enter with the label Strings.Total | |

A bag entry with the same id overrides the built-in; non-null token values (inline width) override both.

### 5.4 Compilation (`BitVirtualKeyboardLayoutCompiler`, internal)

Input: a layout, the effective purpose, culture, mode, `Languages` count, `AutoShift`, `Digits`, a scramble seed or
none. Output: an internal immutable `CompiledLayout`: per layer, the compiled keys with final properties and grid
placement, the column and row counts, plus the JS payload.

**Cache** by content: a stable hash of the layout's canonical JSON plus purpose, mode, languages count, culture name,
digits and `AutoShift`. Scrambled output is never cached. Strings-derived names are applied at render, not cached.
The payload counts as changed only when its serialized form differs from the last one sent (a new `Layout` instance with
the same content, a new `Languages` list, or a `LayoutSelector` returning a fresh instance must not cause an update).
A same-reference fast path skips hashing when `Layout` is the instance seen last.

**Cultures:** separators and digits come from the component's `Culture` ?? the current culture; case mapping comes
from `Layout.Culture` ?? `Culture` ?? the current culture.

Steps:
1. **Validate** (throw `ArgumentException` naming the layout id, layer id, row index and token): non-empty `Id` and
   `Name`; at least one layer; unique layer ids; non-empty rows; every named reference resolves; `SwitchLayer` and
   `NextLayer` targets exist; widths are positive multiples of 0.25; heights >= 1; `DeadKey` markers exist in
   `DeadKeys`; `Custom` keys have a `Command`; no key ends past the column count (step 8).
2. **Resolve** each key: built-in defaults < bag entry < row token (non-null wins); then apply the effective defaults.
3. **Purpose**: drop keys whose `Purposes` excludes the purpose; apply `Variations[purpose]` (non-null wins).
4. **Mode / environment**: `hide` in Inline mode, `lang` with fewer than two languages, `altgr` without an AltGr layer,
   and `tld` outside Email/Url are removed in language layouts (the stretch key absorbs the space). In purpose pads,
   every key removed by step 3 or this step is replaced by a `gap` of its width (their grids stay rectangular).
5. **Derived layers**: if no layer has `Modifiers.Shift`, `AutoShift` is on and the layout has a `shift` key, derive
   `shift` from `base`; if no layer has `Modifiers.Caps` and the layout has a `caps` key, derive `caps` from `base`
   (pads, Arabic and Hebrew get no useless layers). Both upper-case only `Character` outputs that are a single letter
   grapheme whose upper case differs; everything else is copied unchanged. Upper-casing is culture-aware but the
   Turkish and Azerbaijani pairs are hard-coded (i → İ U+0130, ı → I) because Blazor WebAssembly's default ICU shard
   for an en-* app has no tr data and invariant globalization drops Turkish casing.
6. **Digits**: in purpose pads only, map outputs `0`-`9` to the chosen digit set (Persian U+06F0-06F9, Arabic-Indic
   U+0660-0669).
7. **Scramble** (seeded by the component with `RandomNumberGenerator` on open/attach): permute the **grid positions**
   of the scramblable keys (`Scramble ?? (Action == Character && Output is a single digit)`) within each layer. Each key
   keeps its index, output and label, so the DOM and the script's key table always agree. Keys exchange slots, and
   each slot keeps its geometry: a key takes the width and height of the slot it lands in (`x-decimal`'s `0*2`).
8. **Geometry** (5.5).
9. **Labels**: label = `Label` ?? `Displays[Id]` ?? `Displays[Output]` ?? the built-in label ?? `Output`. Names per 4.6.
   Fold `MultiTap` items into the long-press list when the key has none (an untimed alternative, 6.4).

### 5.5 Geometry (CSS grid, quarter-unit columns)

- Work in quarter units: `q(x) = round(x * 4)`.
- **Columns** = the maximum natural row width over **all layers** of the layout, so key sizes do not change when the
  layer changes.
- Natural width of row `r` = sum of `q(Offset) + q(Width)` over its own keys **plus the cells of row `r` already held
  by taller keys of earlier rows**.
- Stretch: leftover `= columns - natural`, split among the row's `Stretch` keys in whole quarter units, the remainder
  to the last stretch key.
- Alignment: a row with leftover and no stretch key aligns by `Align` (Center: start offset `floor(leftover / 2)`;
  Start: 0; End: leftover). A row that shares cells with a taller key always aligns Start.
- Placement: keep a set of occupied (row, column) cells. Before placing a key, advance the cursor until every cell
  `[cursor, cursor + q(Width))` in every row `r .. r + h - 1` is free; mark them. A key that would end past `columns` or
  past the last row is a validation error.
- Output per key: `grid-row: {r+1} / span {h}; grid-column: {c+1} / span {q(Width)}` as an inline style built with
  `FormattableString.Invariant`. Each layer: `grid-template-columns: repeat({columns}, minmax(0, 1fr)); grid-template-rows: repeat({rows}, var(--bit-vkb-kh))`
  plus `direction: ltr` in the stylesheet.
- Gap keys render `<span class="bit-vkb-gap" aria-hidden="true" style="grid-…">` with the Gap part and the key's
  `Class`/`Style`. Blank keys render `<span class="bit-vkb-key bit-vkb-blk" aria-hidden="true">` (never a button: an
  unnamed button fails axe `button-name`).

### 5.6 Keycap rendering rules

- Wrap every text label in `<bdi>`.
- A label that is a lone combining mark (Unicode category Mn) is drawn on U+25CC (dotted circle): `◌َ`.
- `zwnj`, `zwj`, `lrm`, `rlm`, `nbsp` show `ZWNJ`, `ZWJ`, `LRM`, `RLM`, `NBSP` in a smaller font.
- Frame-key icons (Backspace, Enter, Shift, Caps, Tab, Hide, Language, arrows) are inline SVG with
  `aria-hidden="true"`. When the layout's `Direction` is RTL, C# adds the class `bit-vkb-rtl-ico` to the layers
  wrapper and the stylesheet flips the Backspace, Enter and Tab icons horizontally.
- `Hint` renders in a corner span (`bit-vkb-hnt`).

---

## 6. Behaviour specification

### 6.1 Ownership of state and the DOM contract (D3, D14)

- C#: parameters, resolved and compiled layout, rendering. C# never changes which layer is visible after the first
  render.
- JS: active layer, shift/caps/AltGr state, pending dead key, multitap cycle, long-press popup, preview, repeat timers,
  target element, value-mode buffer and caret.
- Razor renders the layers inside `<div class="bit-vkb-lys">`, each as `<div class="bit-vkb-lyr" data-bit-vkb-layer="{id}">`,
  the first one also with `bit-vkb-lyb`. The script marks the active layer with `data-bit-vkb-on` and the root with
  `data-bit-vkb-ready`. CSS: layers hidden, except `[data-bit-vkb-on]`; before the script runs (prerender, static SSR)
  `.bit-vkb:not([data-bit-vkb-ready]) > … > .bit-vkb-lyb` (child combinators only, so nested keyboards are not reached).
- Other script-owned attributes: `data-bit-vkb-act` (latched/locked modifier, pending dead key, physical key held),
  `data-bit-vkb-down` (pressed paint, instead of `:active`), `data-bit-vkb-sec` (secure mode on the root),
  `data-bit-vkb-detached` (Inline + FollowFocus with no input), `hidden` on the popup and preview containers,
  `data-bit-vkb-open` and `inert` on the docked panel, and the `height` of the reserve spacer (a Razor-rendered,
  style-less `<div class="bit-vkb-rsv" aria-hidden="true">`, used by `ReserveSpace`). Razor never renders any of them. `aria-pressed` is rendered by Razor as `"false"` for modifier keys and never changed by Razor
  afterwards; the script sets it.
- Keys carry `data-bit-vkb-k="{index}"` (invariant integer, index into the payload's key table). The root carries
  `data-bit-vkb-v="{payloadVersion}"` (invariant). The script ignores presses while that version differs from the version
  of its options (Blazor patches the DOM before `OnAfterRenderAsync` sends the new table).
- After every render in which the payload changed, C# calls `handle.update(payload)`; the script re-applies the active
  layer (reset to base when the layout id changed, kept if the layer still exists) and its ActiveKey/PopupKey classes.

### 6.2 Layers and modifiers

- Shift layer = the layer with `Modifiers == Shift`; caps = `Caps`; AltGr = `AltGr`; Shift+AltGr = `Shift | AltGr`.
- **Shift**: tap = latched (the next typed character uses the shift layer, then back). A second tap within 400 ms =
  locked (stays until tapped again). A tap while locked = off. `aria-pressed="true"` while latched or locked.
- **CapsLock**: toggles the caps layer (letters upper-case, digits and symbols unchanged). Shift while caps is on uses
  the shift layer.
- **AltGr**: latched like Shift (no double-tap lock); with Shift active selects the Shift+AltGr layer if present.
- **SwitchLayer**: switch and stay. Keys of that layer with `NextLayer` return after typing; `abc` returns to base.
- Announce layer, Shift and Caps changes in the polite live region (not in secure mode).

### 6.3 Long-press

- Alternates = `Key.LongPress` ?? `Layout.LongPress[output]` (space-separated); a shift-layer key looks up its own
  output.
- Pointer: hold for `LongPressDelay` → the popup opens above the key (below near the top edge). Sliding to an item and
  releasing types it; **releasing without moving leaves the popup open, and tapping an item types it** (a non-dragging
  alternative, WCAG 2.5.7). Releasing outside or tapping elsewhere closes it. A plain tap still types the key itself.
- Keyboard: in value mode `ArrowUp` on a focused key opens the popup; in every mode a `contextmenu` event on a key opens
  it (WCAG 2.1.1). Popup items are buttons; Escape closes the popup first (6.9).
- A key with alternates does not auto-repeat.

### 6.4 Multitap

- Repeated taps on the same `MultiTap` key within `MultiTapTimeout` replace the last typed character with the next item
  (cyclic). Any other key or the timeout commits. The items are also in the key's long-press popup (5.4 step 9), which
  is the untimed path.

### 6.5 Dead keys

- A `DeadKey` key sets the pending marker (`data-bit-vkb-act`) and types nothing.
- The next character key looks up `DeadKeys[marker][output]`: found → type it; not found → type `DeadKeys[marker][" "]`
  (the spacing accent) if present, then the character. Space types the spacing accent. A second dead key types the
  first one's spacing accent, then becomes pending. Backspace cancels the pending marker.

### 6.6 Repeat

- Repeatable keys act once on press, then after `RepeatDelay` every `RepeatInterval` until release or pointer cancel.
  `RepeatDelay = 0` disables repeat. Timers live in JS. Acting on the down event is conformant: WCAG 2.5.2 treats
  functions that emulate a keyboard or keypad key press as essential.

### 6.7 Enter

- Label and accessible name from the target's `enterkeyhint` (`enter`, `done`, `go`, `next`, `previous`, `search`,
  `send`) through Strings; purpose `Search` defaults to "Search"; `total` uses "Total".
- `Auto`: textarea → newline; `enterkeyhint=next` → focus the next focusable form control, `previous` → the previous
  one; an input inside a form → `form.requestSubmit()` (Baseline since 2022; where it is missing, click the form's first
  submit button, never `form.submit()`, which skips validation and the `submit` event); otherwise nothing.
  `NewLine`, `Submit`, `Next`, `Close` (close the docked keyboard) and `None` force one behaviour.
- In target modes, dispatch a synthetic `keydown`/`keyup` for Enter to the target first (6.8); if the keydown is
  `defaultPrevented`, skip the behaviour. Then raise `OnEnter`.
- In value mode `Auto` and `Next` behave as `None` (raise `OnEnter` only); `NewLine` inserts `\n`; no synthetic key
  events are sent.

### 6.8 Text engine

Adapters: an **input/textarea adapter** (target modes) and a **buffer adapter** (value mode: a string plus a caret held
in JS). Pure logic lives in `BitVirtualKeyboardEngine.ts`.

- **Selection support:** `selectionStart`, `setRangeText` and `setSelectionRange` work only on input types `text`,
  `search`, `url`, `tel`, `password` and on `textarea`; on others `selectionStart` is null and the others throw
  `InvalidStateError` (verified in all three engines).
- **Insert:** dispatch a cancelable `beforeinput` (`new InputEvent("beforeinput", { inputType: "insertText", data, bubbles: true, cancelable: true })`);
  if cancelled, stop. Check `maxlength` (UTF-16 code units, as browsers count) and `AllowedCharacters`. Then
  `setRangeText(text, start, end, "end")` (replaces any selection, caret after; it fires no `input` but does queue a
  `select` event, so tests must not count events naively), and dispatch a bubbling `input` `InputEvent`
  (`inputType: "insertText"`, `data`). Browsers do not enforce `maxlength` on script edits, so the engine must.
- **Backspace (D13):** a selection → delete it. Otherwise take the previous grapheme cluster (`Intl.Segmenter`,
  `granularity: "grapheme"`); if it is an emoji sequence (contains `\p{Extended_Pictographic}` or
  `\p{Regional_Indicator}`, or ends with U+20E3 keycap) delete the whole cluster, else delete the last **code point**
  (never half a surrogate pair). `inputType: "deleteContentBackward"`. **Delete** mirrors it forward.
- **Caret keys:** move by grapheme; `ArrowLeft`/`ArrowRight` follow the target's computed direction (in an RTL target
  ArrowLeft moves forward in logical order). Home/End go to the start/end of the line (textarea) or value.
- **Clear:** select all and delete (`inputType: "deleteContent"`), then `input`.
- **`type=email`:** no selection API: append at the end; Backspace removes the last code point (or emoji sequence);
  caret keys do nothing. The browser trims leading and trailing spaces of the value.
- **`type=number`:** the browser sanitises any value that is not a valid floating-point number to `""` (so `1.` or `-`
  would wipe the field). The engine keeps a shadow buffer, accepts only ASCII digits, `.`, `-`, `e`, writes `value` only
  when the buffer is a valid number, and makes the `decimal` key type `.` and the digits Latin for it, whatever
  `Digits` says (5.4 step 6 is skipped for a `number` target). Document that
  `type="text" inputmode="decimal"` is the better target.
- **Synthetic key events:** for Enter, Tab, Shift+Tab, Escape (`cancel`), arrows, Backspace and Delete dispatch
  `keydown` then `keyup` (`KeyboardEvent` with `key`, `code`, `bubbles: true`, `cancelable: true`) before acting; skip the
  action if the keydown was cancelled. Untrusted events never trigger the browser's default action (verified), so the
  engine performs it. Never send them in value mode.
- **`change` (D10):** keep `committedValue` = the value at attach. Listen to the target's own trusted `change` and move
  `committedValue` to its value. On Enter, Accept, detach and close, and on `focusout` (deferred with `setTimeout(0)` so
  a native `change` runs first), dispatch a bubbling `change` only if `value !== committedValue`, then update it.
- **Cancel:** restore the value at attach, dispatch `input` and `change`, then close (Docked) or detach.
- **Value mode protocol:** the script sends `OnValue(value, seq)` (seq increments per change; coalesce within one
  animation frame). C# keeps `_lastJsValue`/`_lastJsSeq`, assigns with `AssignValue`, and includes a `value` in the
  payload only when `Value != _lastJsValue` (a real external change), stamped with the last seq it saw. The script
  ignores a payload value whose seq is older than its latest change and moves the caret to the end only for a real
  external change.
- **One-way binds:** when `AssignOpen`/`AssignValue` returns `false` (bound one way), push the C# value back
  (`handle.setOpen(Open)` / `update`) so a one-way `Open`/`Value` stays authoritative.

### 6.9 Attaching, focus, Escape and the OS keyboard

- **Focus retention (target modes):** a `pointerdown` listener on the root calls `preventDefault()`; keys have
  `tabindex="-1"`. Pointer Events only (no mouse/touch mixing), presses tracked per `pointerId` (multi-touch),
  `touch-action: manipulation` on keys. Verify iOS Safari by hand (11.4); Playwright WebKit on Windows cannot tap.
- **OS keyboard suppression:** on attach set `inputmode="none"` on the target, remembering the original attribute;
  restore it on detach. Never use `readonly` (hides the caret, blocks barcode scanners).
- **Qualifying targets:** an `input` of type `text`, `search`, `url`, `tel`, `email`, `password` or `number`, or a
  `textarea`; not disabled, not read-only. `date`, `time`, `datetime-local`, `month`, `week`, `color` and `range`
  inputs never qualify (their values sanitise partial text away).
- **FollowFocus:** one `focusin`/`focusout` listener pair on the document while enabled. A qualifying target matching
  `TargetSelector` (default `[data-bit-vkb]`) attaches on focus, reports its info to .NET (6.10) and, in Docked mode,
  opens. When focus moves to a non-qualifying element outside the keyboard: commit `change` (6.8), detach, and in
  Docked mode close. The `data-bit-vkb` value may name a purpose in kebab case (`pin`, `signed-decimal`, `date`),
  case-insensitive; an unknown value is ignored.
- **Inline + FollowFocus with nothing attached:** keys are inert; the script sets `data-bit-vkb-detached` on the root.
- **Docked mode:** the panel is `position: fixed; inset-inline: 0; bottom: 0` with
  `padding-bottom: env(safe-area-inset-bottom)` (on iOS this inset is non-zero only with `viewport-fit=cover`). Closed:
  translated off-screen and `inert` (both set by the script, 6.1). With `ReserveSpace`, the script gives the
  `bit-vkb-rsv` spacer (in normal flow) the panel's height while open.
  After opening, scroll the target above the panel (`scroll-margin-bottom` = panel height + 16 px,
  `scrollIntoView({ block: "nearest" })`, restore). `position: fixed` breaks inside a transformed ancestor: document
  that a docked keyboard belongs in the layout, outside dialogs and transformed panels.
- **Escape:** in target modes focus stays in the target, outside the keyboard, so a claim on the keyboard root never
  matches (`Utils.claimEscape` walks up from `e.target`). While a docked keyboard is open (or a long-press popup is
  shown) with an input attached, the script sets `data-bit-esc="claim"` on the **target** (remembering and restoring any
  value it had, like `inputmode`) and, on a keydown of the target, acts when `Utils.escapeClaimer(e) === target` and
  `!Utils.isComposingKey(e)`: close the popup first, else close the keyboard. If the target already carries a
  `data-bit-esc` of its own (a `BitSearchBox` input's `"text"`), leave it: the keyboard then acts only on an Escape
  that no claim took (`Utils.escapeClaimer(e) === null`), so one press never both clears the text and closes the
  keyboard. In value mode the root renders `data-bit-esc="claim"` whenever the keyboard acts on Escape (Docked and
  `Open`, a long-press popup shown, or focus inside the roving grid), so a surrounding dialog does not close too (read
  CLAUDE.md 379-425; any `*Claim` member may only yield `"claim"` or `"text"`, `BitEscapeClaimContractTests`).
- **Value-mode focus:** the active layer is a roving-tabindex grid (one key `tabindex="0"`); arrow keys move by visual
  position (the grid is LTR, so Left is always left), Enter and Space activate, Escape blurs.

### 6.10 Purpose detection and layout resolution

1. Purpose = `Purpose` ?? the target's `data-bit-vkb` value ?? from the target's original `inputmode` (`tel` → Tel,
   `numeric` → Numeric, `decimal` → Decimal, `email` → Email, `url` → Url, `search` → Search, `text` → Text) ?? from
   `type` (`tel`, `email`, `url`, `search`; `number` → Decimal; `password` → Text plus secure mode) ?? Text.
2. `Layout` set → use it (the purpose still applies its `Purposes`/`Variations`).
3. `LayoutSelector` returns non-null → use it.
4. Purposes `Text`, `Email`, `Url`, `Search` use the language layout (`Language` ?? culture two-letter name if in the
   catalog ?? `"en"`) in the resolved form; the others use the pad whose Purpose column in 8.1 names them
   (`Tel` → `x-phone`, `Alphabetic` → `x-abc`, ...).
5. Form `Auto` → Compact when the root's width < `CompactBreakpoint` (a ResizeObserver reports crossings only, through
   `_OnCompact`).

### 6.11 Accessibility and sizing

- Root: `role="group"`, `aria-roledescription` = Strings.RoleDescription, `aria-label` = `AriaLabel` ?? layout name.
- Each layer: `role="group"` with its name. Keys: native buttons named per 4.6; modifiers use `aria-pressed` with a
  label that never changes (APG). Disabled component: every key `disabled` + `aria-disabled="true"` per CLAUDE.md's
  disabled markup rules, root `bit-dis`, `DisabledAwareHtmlAttributes` splatted.
- Polite live region (`bit-vkb-liv`, visually hidden with the existing mixin) for layer, Shift and Caps announcements;
  never typed characters.
- **Key height:** `var(--bit-vkb-kh, var(--bit-VirtualKeyboard-key-height, max(24px, spacing(5.5))))`. The size
  classes publish the private `--bit-vkb-kh` (an explicit `Size` wins over the public variable, as CLAUDE.md requires):
  `bit-vkb-sm` `max(24px, spacing(3))`, `bit-vkb-md` `max(44px, spacing(5.5))`, `bit-vkb-lg` `spacing(8)`; an unset
  `Size` publishes nothing; `kh` is in `$vkb-private-properties`. `spacing(n)` is 0.5rem × the layout density
  (`functions.scss` 28-29), so the `max()` floors keep the WCAG 2.5.8 (24 px) and 2.5.5 (44 px) sizes at low
  densities; note these literal floors as WCAG constants in the scss header. Do not use `$siz-ctrl-*` (26/32/40 px
  under Fluent).
- `@media (forced-colors: active)`: keys get system-color borders; active state uses `Highlight`.
- Motion only through `mot-scaled-duration()` and `$mot-easing` (3.3).
- Manual screen-reader check per `docs/accessibility/README.md` (roles, keyboard model and announcements are new, and
  long-press makes it touch-aware), recorded in `docs/accessibility/records/BitVirtualKeyboard.md` from the template.

### 6.12 Secure mode and physical keyboard

- Secure (parameter, or a password target): no pressed paint (no `data-bit-vkb-down`), no preview bubble, no sound, no
  live-region text; `OnKeyPress.Text` is null; `OnInput` is not raised and the script sends no target text to .NET. In
  value mode `Value`/`ValueChanged` still update (the app owns that value).
- Physical highlight (not in secure mode): while attached, `keydown`/`keyup` listeners mark the key whose output equals
  `event.key` (or whose built-in action matches `Backspace`, `Enter`, `Shift`, ...) with `data-bit-vkb-act` while held;
  `getModifierState("CapsLock")` syncs the Caps key. Never `preventDefault` physical keys; skip composing keys
  (`Utils.isComposingKey`).

### 6.13 Sound and haptics

- Sound: a short oscillator click through a lazily created `AudioContext`, resumed on the first `pointerup`, `click` or
  `keydown` (for touch and pen, `pointerdown` is not a user-activation event).
- Haptics: `navigator.vibrate?.(10)` after sticky user activation. It never shipped in Safari, was removed from desktop
  Firefox in version 129, and is a silent no-op in Firefox for Android since 79, so feature detection cannot promise it.

---

## 7. Interop

```ts
namespace BitBlazorUI {
    export class BitVirtualKeyboard {
        public static setup(root: HTMLElement, dotnet: DotNetObject, options: BitVirtualKeyboardOptions): BitVirtualKeyboardHandle { ... }
    }
}
// handle: update(options), attach(element | null), setLayer(id), press(keyId), setOpen(open), dispose()
```

- C# payload `internal sealed record BitVirtualKeyboardJsOptions` (camelCase): `version`, `layoutId`, `layers` (ids;
  shift, caps, altgr, shift-altgr ids), `keys` (array of `BitVirtualKeyboardJsKey`: `a` action, `o` output, `l` layer,
  `nl` next layer, `dk` dead marker, `c` command, `lp` alternates, `lpd` default alternate, `mt` multitap, `r` repeat,
  `p` preview, `n` accessible name, `id` key id), `deadKeys`, timing values, `enterBehavior`, `mode`, `open`,
  `followFocus`, `targetSelector`, `allowedCharacters`, `maxLength`, `secure`, `showKeyPreview`, `sound`, `haptics`,
  `hasKeyPressHandler`, `hasInputHandler`, `compactBreakpoint`, `value` + `valueSeq` (6.8), ActiveKey/PopupKey classes
  and styles, and the strings the script needs (popup label, announcements, Enter labels).
- `[JSInvokable]` callbacks (3.1 naming): `_OnKeyPress` → `BitVirtualKeyboardJsKeyPressResult` (only called when
  `hasKeyPressHandler`), `_OnValue(string?, long)`, `_OnTargetValue(string?)` (raises `OnInput`; only when
  `hasInputHandler` and not secure), `_OnLayer(string)`, `_OnTarget(BitVirtualKeyboardJsTargetInfo?)`, `_OnEnter()`,
  `_OnCommand(string, string)`, `_OnOpen(bool)`, `_OnCompact(bool)`, `_OnNextLayout()`.
- Lifecycle: `OnAfterRenderAsync(firstRender)` → `setup`, store the handle; if the payload version moved while `setup`
  was awaited, call `update`; if the component was disposed meanwhile, dispose the handle at once. Later renders →
  `update` only when the payload changed (5.4). On `_OnOpen`/`_OnTarget` with `Scramble`, re-seed, re-render and update.
- Disposal per 3.1: the script's `dispose()` removes every listener, restores `inputmode` and `data-bit-esc` on the
  target, and detaches.

---

## 8. Built-in catalog

Public static class `BitVirtualKeyboardLayouts` (partial over `BitVirtualKeyboardLayouts.cs`, `.Purpose.cs`,
`.Latin.cs`, `.Rtl.cs`, `.Cyrillic.cs`, `.Symbols.cs`, `.Compact.cs`) with:

- one `public static BitVirtualKeyboardLayout X => CreateX();` per layout (for example `English`, `EnglishCompact`,
  `Persian`, `PersianCompact`, `Phone`, `Pin`, ...). **Every access builds a fresh instance**; no static mutable state
  (D5). The compiler's content cache makes repeated access cheap;
- `public static BitVirtualKeyboardLayout? Get(string id)` and `public static IReadOnlyList<string> Ids { get; }`
  (Compact ids are `<id>-compact`);
- `public static BitVirtualKeyboardLayout ForPurpose(BitVirtualKeyboardPurpose purpose)`.

### 8.1 Purpose pads (shorthand rows of the base layer)

Phone order (1-2-3 on top) follows ITU-T E.161; calculator order (7-8-9 on top) follows calculators and PC numpads.
Rows are this plan's design unless the last column says otherwise. Every row of every pad must fill exactly the column
count (a Phase 4 test).

| Id | Purpose | Rows | Extras / basis |
|---|---|---|---|
| `x-phone` | Tel | `1 2 3 {bksp}` / `4 5 6 {space*1}` / `7 8 9 {hide}` / `* 0 # {enter}` | Hints 2 ABC, 3 DEF, 4 GHI, 5 JKL, 6 MNO, 7 PQRS, 8 TUV, 9 WXYZ (E.161); `0` hint and long-press `+`; `*` long-press `,` (pause); `#` long-press `;` (wait); key 5 gets `Class = "bit-vkb-nub"` (E.161 tactile mark). `hide` becomes a gap in Inline mode. |
| `x-pin` | Pin | `1 2 3` / `4 5 6` / `7 8 9` / `{bksp} 0 {enter}` | Digits `Preview = false`; digits scramblable. |
| `x-numeric` | Numeric | same rows as `x-pin` | Preview on. |
| `x-decimal` | Decimal | `1 2 3 {bksp}` / `4 5 6 {enter}` / `7 8 9` / `0*2 {decimal}` | Bag: `enter` with `Height = 3`. |
| `x-signed-decimal` | SignedDecimal | `1 2 3 {bksp}` / `4 5 6 {enter}` / `7 8 9` / `- 0 {decimal}` | Same tall Enter. |
| `x-calculator` | Calculator | `{clear} ( ) /` / `7 8 9 *` / `4 5 6 -` / `1 2 3 +` / `0*2 {decimal} =` | `Displays`: `*` → `×`, `/` → `÷`, `-` → `−` (outputs stay ASCII). |
| `x-pos` | Pos | `7 8 9 {qty}` / `4 5 6 {void}` / `1 2 3 {clear}` / `0 00 000 {decimal}` / `{total*4}` | No standard exists; design. `qty`/`void` raise `OnCommand`. |
| `x-hex` | Hex | `7 8 9 F` / `4 5 6 E` / `1 2 3 D` / `0 A B C` / `{bksp*2} {enter*2}` | |
| `x-date` | Date | `1 2 3` / `4 5 6` / `7 8 9` / `{datesep} 0 {bksp}` | For text inputs with `data-bit-vkb="date"`. |
| `x-time` | Time | `1 2 3` / `4 5 6` / `7 8 9` / `: 0 {bksp}` | For text inputs with `data-bit-vkb="time"`. |
| `x-iban` | Iban | `1 2 3 4 5 6 7 8 9 0` / `Q W E R T Y U I O P` / `A S D F G H J K L {gap}` / `Z X C V B N M {bksp*3}` / `{space*8} {enter*2}` | Upper-case outputs, no shift key. |
| `x-abc` | Alphabetic | `a b c d e f g` / `h i j k l m n` / `o p q r s t u` / `{shift} v w x y z {bksp}` / `{space*5} {enter*2}` | Kiosk/TV grid; auto shift. |
| `x-atm` | Atm | `1 2 3 {cancel}` / `4 5 6 {clear}` / `7 8 9 {enter}` / `{blank} 0 {blank} {blank}` | The red/yellow/green colour convention is unverified: a demo theme, not a default. |

Email, URL and Search are not pads: they are the language layout with the `comma` variation, the `tld` key and the
Enter label (6.7).

### 8.2 Language layouts, Full form

P0 ids: `en` (US QWERTY), `en-GB`, `fa` (ISIRI 9147), `ar` (Arabic 101), `he`, `de`, `fr-t-k0-windows` (legacy AZERTY;
the id follows CLDR's registered `k0-windows` transform because CLDR's `fr` is the newer AFNOR layout), `es`, `ru`,
`tr` (Q). Build each from `layout-data.md` (rows are in scan-code order; read its header). Every row of every Full
base layer must be exactly 15 units wide (a Phase 4 test).

**ANSI** form (`en`, `he`, `ar`, `fa`, `ru`):

```
E: <row E, 13 keys> {bksp*2}
D: {tab*1.5} <row D first 12 keys> <row D last key (scan 2B)>*1.5      e.g. \\*1.5 for a backslash
C: {caps*1.75} <row C, 11 keys> {enter*2.25}
B: {shift*2.25} <row B without its first key (scan 56), 10 keys> {shift*2.75}
A: {lang*1.5} {space} {altgr*1.5} {hide*1.5}
```

**ISO** form (`en-GB`, `de`, `fr-t-k0-windows`, `es`, `tr`), with a bag key `iso-enter` (`Action = Enter`,
`Height = 2`) written `{iso-enter*1.25}` at the end of row D in every layer, so `{enter}` elsewhere stays one row high:

```
E: <row E, 13 keys> {bksp*2}
D: {tab*1.75} <row D first 12 keys> {iso-enter*1.25}
C: {caps*1.75} <row C, 11 keys> <row D last key (scan 2B)>             (the tall Enter holds the last 1.25 units)
B: {shift*1.25} <row B, 11 keys incl. scan 56> {shift*2.75}
A: {lang*1.5} {space} {altgr*1.5} {hide*1.5}
```

Full layouts have **no symbol layers**: every symbol is on their own base, shift and AltGr layers, as on hardware.

- Layers: `base`, `shift` (`Modifiers.Shift`), and where `layout-data.md` lists AltGr characters, `altgr` and (if any)
  `shift-altgr`. In AltGr layers, **character positions** without an AltGr character are `{blank}`; frame keys stay.
- Persian has no scan-56 key in ISIRI; `layout-data.md` takes its `\ |` from KBDFAR (unused by the ANSI form anyway).
- Dead keys marked `x(dead)` become `{dead:<marker>}` keys. Markers: `grave` (U+0300, spacing U+0060), `acute` (U+0301,
  U+00B4), `circumflex` (U+0302, U+005E), `tilde` (U+0303, U+007E), `diaeresis` (U+0308, U+00A8); they cover every
  dead key of the P0 layouts. Build each table from Unicode composition: for each base letter a-z and A-Z whose
  `(letter + combining mark).Normalize(NormalizationForm.FormC)` is a single code point, add the entry; add `" "` → the
  spacing accent. For `tr` also add `ı` → `î í ï ì` and `İ` → `Î Í Ï Ì` (KBDTUQ tables). The composition rule yields a
  few entries Windows lacks (`^` + `c` → `ĉ`); that is accepted.

### 8.3 Language layouts, Compact form

Derived by a documented helper `CreateCompact(full, options)` (in `.Compact.cs`), not hand-typed. It copies the Full
layout's `Displays`, `LongPress`, `DeadKeys` and bag, except `iso-enter`:

1. **Letter rows** = the Full base rows D (scan 10-1B, never the scan-2B key), C and B, keeping only keys whose output
   is a letter (Unicode category L*, or a multi-code-point string starting with one), in row order.
2. Row 3 gets `{shift*1.5}` before and `{bksp*1.5}` after; Arabic-script and Hebrew layouts (no case) get only
   `{bksp*1.5}`. Per-language additions are explicit options:
   - `ar`: `ذ` (on row E in Arabic 101) is inserted at the start of row 3, which then ends with `{bksp}` (width 1) so
     the row stays 12 units, as wide as row D.
3. Letters only on the Full shift layer become long-press alternates (8.4).
4. Bottom row: `{sym*1.5} {lang} {comma} {space} . {tld} {enter*1.5}` (`tld` only for Email/Url; `fa`/`ar` `comma`
   types `،`; `fa` adds `{zwnj}` right after `{space}`, since Persian needs it constantly).
5. Symbol layers `symbols` and `symbols2` from 8.5 (Latin set for Latin, Cyrillic and Hebrew; Arabic-script set for
   `fa` and `ar`).

### 8.4 Long-press tables (design; a native-speaker review is open question 4)

- `en`/`en-GB`: `a` → `à á â ä æ ã å ā`; `c` → `ç`; `e` → `è é ê ë ē`; `i` → `ì í î ï`; `n` → `ñ`;
  `o` → `ò ó ô ö õ ø œ`; `u` → `ù ú û ü`; `y` → `ý ÿ`; `s` → `ß`; `.` → `, ? ! ' " : ;`.
- `de`: `a` → `ä à á`; `o` → `ö ó ò`; `u` → `ü ú ù`; `s` → `ß`; `e` → `é è ê`.
- `fr-t-k0-windows`: `e` → `é è ê ë`; `a` → `à â æ`; `u` → `ù û ü`; `i` → `î ï`; `o` → `ô œ`; `c` → `ç`; `y` → `ÿ`.
- `es`: `a` → `á`; `e` → `é`; `i` → `í`; `o` → `ó`; `u` → `ú ü`; `n` → `ñ`; on the symbols layer `?` → `¿` and
  `!` → `¡`.
- `tr`: `a` → `â`; `i` → `î`; `u` → `û` (kâr, hâlâ); the other Turkish letters are already keys.
- `ru`: `е` → `ё`.
- `fa`: `ا` → `آ أ إ ء`; `ی` → `ئ ى`; `ه` → `ۀ ة`; `و` → `ؤ`; `ز` → `ژ`; `.` → `، ؛ ؟ ! « »`. Do **not** offer the
  Arabic ي (U+064A) or ك (U+0643): they look like ی/ک but are different code points and break Persian search. Note
  for the docs: ISIRI types the ezafe as ه + U+0654, which is not canonically equivalent to ۀ (U+06C0).
- `ar`: `ا` → `آ أ إ ء`; `ي` → `ئ ى`; `ه` → `ة`; `و` → `ؤ`; `.` → `، ؛ ؟ !`.
- `he`: `.` → `׳ ״` (geresh, gershayim; the Compact rows drop the `'` key).

### 8.5 Symbol layers (design)

Shorthand rows as written (in C# source double every backslash, see 5.1). Every row is 10 units.

Latin set:
```
symbols   1 2 3 4 5 6 7 8 9 0
          @ # {cur} _ & - + ( ) /
          {layer:symbols2*1.5} * " ' : ; ! ? {bksp*1.5}
          {abc*1.5} {lang} , {space} . {enter*1.5}
symbols2  ~ ` | • √ π ÷ × ¶ ∆
          £ ¢ € ¥ ^ ° = { } \\
          {sym*1.5} % © ® ™ ✓ [ ] {bksp*1.5}
          {abc*1.5} {lang} , {space} . {enter*1.5}
```

Arabic-script set (`fa`: Persian digits ۰-۹, `cur` = ﷼; `ar`: Arabic-Indic digits ٠-٩ by default, following the
`Digits` parameter, `cur` = `$`):
```
symbols   <ten digits>
          ! @ # {cur} ٪ ^ & * ( )
          {layer:symbols2*1.5} - _ + « » : ؛ {bksp*1.5}
          {abc*1.5} {lang} ، {space} . {enter*1.5}
symbols2  ~ ` | • ÷ × ٫ ٬ [ ]
          { } < > / \\ = " ' ؟
          {sym*1.5} … ـ $ € £ ¥ ¢ {bksp*1.5}
          {abc*1.5} {lang} ، {space} . {enter*1.5}
```

Brackets are typed as their code points; bidi mirroring displays them correctly in RTL text. Do not swap them here
(these layers are not hardware-derived).

---

## 9. Files to create or modify

Create (all with BOM):

```
Bit.BlazorUI.Extras/Components/VirtualKeyboard/
  BitVirtualKeyboard.razor
  BitVirtualKeyboard.razor.cs
  BitVirtualKeyboard.scss
  BitVirtualKeyboard.ts
  BitVirtualKeyboardEngine.ts                     (// bit-trim: on-demand; pure functions)
  BitVirtualKeyboardClassStyles.cs
  BitVirtualKeyboardParams.cs
  BitVirtualKeyboardStrings.cs
  BitVirtualKeyboardJsRuntimeExtensions.cs        (internal: extension class + Js payload records)
  BitVirtualKeyboardShorthand.cs                  (public static: ParseRow, ParseRows)
  BitVirtualKeyboardLayoutCompiler.cs             (internal)
  BitVirtualKeyboardJsonContext.cs                (internal, source-generated)
  BitVirtualKeyboardRowJsonConverter.cs
  BitVirtualKeyboardLayouts.cs (+ .Purpose.cs, .Latin.cs, .Rtl.cs, .Cyrillic.cs, .Symbols.cs, .Compact.cs)
  Models/  BitVirtualKeyboardLayout.cs, ...Layer.cs, ...Row.cs, ...Key.cs, ...KeyPressArgs.cs, ...CommandArgs.cs,
           ...LayoutContext.cs, ...KeyContext.cs, and one file per enum (Mode, Form, Digits, EnterBehavior, RowAlign,
           Purpose, KeyAction, Modifiers, FlickDirection)
Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/Extras/VirtualKeyboard/
  BitVirtualKeyboardDemo.razor, .razor.cs, .razor.samples.cs, .razor.scss
Tests/Bit.BlazorUI.Tests/Components/Extras/VirtualKeyboard/
  BitVirtualKeyboardShorthandTests.cs, ...LayoutCompilerTests.cs, ...GeometryTests.cs, ...JsonTests.cs,
  ...LayoutsCatalogTests.cs, ...LayoutDataTests.cs, ...Tests.cs (rendering), ...ParamsTests.cs, ...InteropTests.cs,
  ...StylesheetTests.cs (if a default needs pinning), ...ScriptTests.cs
  Scripts/README.md, Scripts/helpers/engine-fixture.mjs, Scripts/*.test.mjs
Tests/E2E/Bit.BlazorUI.Tests.E2E.TestHost.Client/Components/Pages/Extras/BitVirtualKeyboardE2E.razor
Tests/E2E/Bit.BlazorUI.Tests.E2E/Extras/BitVirtualKeyboardE2ETests.cs
docs/accessibility/records/BitVirtualKeyboard.md   (from the template, after the manual check)
```

Modify: `Bit.BlazorUI.Extras/Styles/extra-components.scss`, `Bit.BlazorUI.Extras/Scripts/BitBlazorUI.d.ts`,
`Demo/.../Shared/MainLayout.razor.NavItems.cs`, `Demo/.../Models/ComponentCatalog.cs`,
`Tests/Bit.BlazorUI.Tests.Mcp/ComponentCatalogTests.cs`, `Tests/Bit.BlazorUI.Tests/Utils/BitDisabledActivationContractTests.cs`,
and any contract-test list the unit suite reports.

**Node script tests** (copy the ResourceTimeline setup exactly): `BitVirtualKeyboardScriptTests.cs` runs
`node --test --test-reporter=spec` over every `Scripts/*.test.mjs` with that folder as working directory, `NO_COLOR=1`,
and an environment variable `BIT_VKB_MODULES` set to `SourceFiles.GetPath("Bit.BlazorUI.Extras", "wwwroot", "scripts", "modules")`;
it asserts the built `BitVirtualKeyboardEngine.js` module exists (the Extras build must run first), is
`Assert.Inconclusive` when `node` cannot start, and times out after 3 minutes. `helpers/engine-fixture.mjs` sets
`globalThis.window = globalThis`, creates `globalThis.BitBlazorUI`, stubs every `BitBlazorUI.Utils.*` the engine calls,
imports the module and re-exports `BitBlazorUI.BitVirtualKeyboardEngine`. Node 22.7+ (or 20.19+), no package.json.

---

## 10. Phases

"Build" means `dotnet build src/BlazorUI/Bit.BlazorUI.Extras/Bit.BlazorUI.Extras.csproj` from the repo root. The SDK is
pinned by `src/global.json` (11.0.100); if it is missing, run `dotnet` from a directory outside `src` with absolute
paths. The packages target `net11.0;net10.0` and Debug builds only net11.0: use no .NET 11-only API, and add
`-p:BuildAllTfms=true` to the build at the phase 4 and phase 9 gates.

### Phase 0 - Reconnaissance (no code)

1. Read the files in section 0 and the templates in 0.6 end to end; `git show --stat 225adacd78 0a94559a09`.
2. Grep for `bit-vkb` and `VirtualKeyboard` across `src/BlazorUI` (expect only these plan docs and an unrelated E2E
   test name, `EnterKeyHint_LabelsTheReturnKeyOfAVirtualKeyboard`).
3. Confirm `spacing()` in `functions.scss` and `escapeClaimer` in `Utils.Escape.ts`.
4. Start the deviation log.

### Phase 1 - Model, shorthand, JSON, compiler, geometry, first layouts (pure C#)

1. Enums, records, `BitVirtualKeyboardShorthand`, JSON converters and context, `BitVirtualKeyboardLayoutCompiler`
   (validation, resolution, purpose, environment, derived layers, digits, scramble, geometry, labels).
2. The `en` Full and Compact layouts and the Latin symbol layers, so the component renders with no parameters from
   phase 2.
3. Unit tests (no bUnit needed): every grammar rule of 5.2 (named keys with argument and width, invalid brace tokens
   as errors, width suffix on literals and escapes, lone braces, escapes, multi-character tokens, Arabic and combining
   marks, bag lookup of literals); every validation error; merge precedence (a `{bksp}` token keeps the bag's width and
   action); derived shift and caps for `en-US` and `tr-TR` (`i` → `İ`, `.com` unchanged); decimal separator per
   `Digits` for `fa-IR`, `ar-SA`, `de-DE`; geometry (centring, stretch remainders, tall Enter occupancy in `x-decimal`
   and the ISO template with no overlap, columns across layers); purpose variations, `Purposes` removal and pad gaps;
   scramble permutes positions only; JSON round trip of `en` and a hand-written JSON layout with string rows and
   string enums.

Gate: build green; new tests green.

### Phase 2 - Rendering, styles and the first registrations

1. `BitVirtualKeyboard.razor(.cs)`: parameters (alphabetical), ClassStyles, Strings, Params, root markup
   (`@ref="RootElement"`, `@attributes="DisabledAwareHtmlAttributes"`, `id`, `class`, `style`, `dir`), the layers
   wrapper and layers, keys, gaps, blanks, labels, hints, inline-SVG icons, live region, the empty popup and preview
   containers, the docked panel wrapper, `data-bit-vkb-v`, the value-mode `data-bit-esc`.
2. `BitVirtualKeyboard.scss`: layer grid (`direction: ltr`), key paint from tokens and private variables, size presets
   (6.11), `[data-bit-vkb-act]`, `[data-bit-vkb-down]`, `PrimaryKey`, gap, blank, popup and preview, docked panel and
   its transition (motion tokens), forced colors, `bit-vkb-rtl-ico`, `bit-vkb-nub`, the private property reset list,
   the public variable header. Import it in `extra-components.scss`.
3. Registrations that contract tests need as soon as the component and its stylesheet exist: the nav item, the catalog
   glyph and summary, and a **demo page skeleton** (both `@page` routes, `PageOutlet`, `DemoPage` with
   `SecondaryNames` equal to the nav `Description` split on commas, a Basic example, `componentParameters`,
   `componentSubClasses`, `componentSubEnums`, `componentCssVariables` listing every public variable the stylesheet
   reads), plus the `BitDisabledActivationContractTests.Components` entry and the MCP `[DataRow]`.
4. bUnit tests: root class and parts; one `<button type="button">` per non-gap, non-blank key, with names; gaps and
   blanks as `aria-hidden` spans; `aria-pressed="false"` on modifiers; `role="group"` and roledescription; disabled
   markup; grid styles culture-invariant (render under `fa-IR` and `de-DE`); `<bdi>` labels and dotted-circle combining
   marks; `hide` dropped in Inline mode (language layouts) and a gap in pads; `lang` dropped with one language; Params
   cascade (copy `BitCopyButtonParamsTests`).

Gate: build green; component tests green; run the **whole** unit suite and fix what the contract tests name.

### Phase 3 - Scripts and interop (text engine, focus, layers)

1. `BitVirtualKeyboardEngine.ts` and `BitVirtualKeyboard.ts` per sections 6 and 7; `BitBlazorUI.d.ts`; the JS runtime
   extensions and payload records with `[DynamicDependency]`; `OnAfterRenderAsync` setup/update; disposal.
2. Node tests over the engine: Backspace removes one code point from `"e\u0301"`, `"\u0628\u064E"` (ب + fatha) and `"\u0645\u06CC\u200C"` (می + ZWNJ), and the
   whole cluster from `"a👍🏽"`, `"\u{1F468}\u200D\u{1F469}\u200D\u{1F467}"` (ZWJ family), `"🇮🇷"` and a keycap; insert with `maxlength` (UTF-16 units) and
   `AllowedCharacters`; selection replacement; number-buffer validity; dead-key resolution and fallback; multitap cycle;
   shift state machine (latched → typed → off; double tap → locked; caps); value-mode seq protocol.
3. bUnit interop tests (JSInterop Loose; `SetupModule`/`Setup` as the ImageViewer tests do): setup once with the expected
   payload; `update` only when the payload changed (not for an equal new `Layout` instance); callbacks assign `Value`
   and `Open`, and push back when bound one way; `OnKeyPress` round trip honours `Cancel`/`Text`; secure mode sends no
   text; `[JSInvokable]`s return after disposal.

Gate: build green (the scripts compile under `strict`); Node and bUnit tests green; `BitAssetTrimmingContractTests`,
`BitLazyScriptsContractTests`, `BitEscapeClaimContractTests`, `BitJsInvokableDisposalContractTests` green.

### Phase 4 - Catalog

1. Purpose pads (8.1).
2. P0 Full layouts from `layout-data.md` (8.2), dead-key tables.
3. Compact helper (8.3), long-press tables (8.4), symbol layers (8.5).
4. Catalog tests: every id compiles in both forms and for every purpose; ids unique; `ForPurpose` mapping; every row of
   every pad fills the column count; every row of every Full base layer is 15 units; every row of every symbol layer is
   10 units; each access returns a new instance; data spot checks against `layout-data.md` for each P0 layout (exact
   base and shift rows D and C; Persian row C `ش س ی ب ل ا ت ن م ک گ` with U+06CC and U+06A9 and never U+064A or
   U+0643; Arabic row C with U+064A and U+0643; German `{dead:circumflex}` first on row E; Hebrew shift row E with `)`
   before `(`; Persian Compact has `{zwnj}`; Arabic Compact has `ذ`).

Gate: build green with `-p:BuildAllTfms=true`; catalog tests green.

### Phase 5 - Modes, targets, purpose detection, Enter

1. Inline/Docked, `Open` and `DefaultOpen`, `ReserveSpace`, scroll-into-view, Escape (6.9).
2. `Target`, `FollowFocus`, `TargetSelector`, `data-bit-vkb` purposes, `inputmode` save/restore, qualifying types,
   purpose resolution (6.10), Auto form, `Languages` + `lang` + `OnLayoutChange`.
3. Enter behaviours (6.7), Accept/Cancel/Clear, `change` tracking (6.8).
4. bUnit tests for the resolution logic (purpose from target info, layout precedence, form switching).

Gate: build and tests green.

### Phase 6 - Long-press, multitap, dead keys, repeat, preview, physical highlight

Implement 6.3-6.6 and 6.12 in the scripts, filling the Razor-rendered popup and preview containers. Node tests for the
timing logic with fake timers where practical.

Gate: build, Node and bUnit tests green.

### Phase 7 - Secure, scramble, size, sound, haptics, theming polish

Implement 6.12-6.13, scramble on open/attach, size presets, all public CSS variables (each in the scss header and the
demo `componentCssVariables`).

Gate: build and tests green; `BitComponentCssVariablesContractTests`, `BitComponentPrivatePropertyResetTests`,
`BitComponentStatePaintTests`, `BitReducedMotionDurationContractTests`, `BitComponentDirFallbackTests` green.

### Phase 8 - Complete the demo page

Follow CLAUDE.md "Demo pages" (section order, numbering, samples matching the render, sorted parameter table, shared
enums such as `Size` linked with `Href = "#size-enum"` and `DemoSharedEnums.BitSize()`, `componentSubClasses` for
ClassStyles, Strings, the layout records and args, `componentSubEnums` for every own enum, `componentPublicMembers`,
`componentCssVariables`). Sections, in order:

1 Basic (inline, value mode, `@bind-Value`), 2 Attach to an input (`Target`), 3 Follow focus (Docked, a form of mixed
inputs with `data-bit-vkb`), 4 Purposes (a selector over every purpose pad), 5 Languages (`Language`, `Languages`, the
`lang` key; include fa, ar, he), 6 Form (Full / Compact / Auto), 7 Custom layout with shorthand, 8 Custom layout with
the object model (bag keys, long-press, multitap, dead keys, variations, custom commands, tall keys), 9 Customizing a
built-in with `with { ... }` (copying the collection it changes), 10 Layout from JSON, 11 Events (`OnKeyPress`
cancel/replace, `OnInput`, `OnEnter`, `OnCommand`, `OnLayerChange`), 12 Enter behavior, 13 Secure and scramble (PIN
pad), 14 Key template, 15 Disabled, then the library-wide sections in CLAUDE.md order that apply (Size, Style & Class,
RTL; no Color, since the component has no `Color` parameter).

Run the demo (`dotnet run --project src/BlazorUI/Demo/Bit.BlazorUI.Demo.Server`) and use every section by hand in
Chromium; then once with `-p:IncludeWasm=true`.

Gate: `DemoPageContractTests`, `DemoSharedEnumsTests`, `DemoScopedCssAnchorTests`, `BitParameterOrderContractTests`
green; the MCP suite green.

### Phase 9 - E2E and final verification

1. The E2E page `/e2e/virtual-keyboard` (one `E2ESection` per feature with a stable `Id`, echo spans, driver buttons,
   `<E2EStatus />` at the end) and the test class (`[TestClass]`, `[E2ERetry]`, `: E2ETestBase`, `OpenAsync`,
   `Expect(...)`, `AssertNoAxeViolationsAsync("#section")` per section). Tests in 11.3.
2. Run in Chromium, Firefox and WebKit, server and wasm modes, scoped with
   `--filter "FullyQualifiedName~Extras.BitVirtualKeyboard"`. Firefox and WebKit have pre-existing failures in other
   components; re-run a failing class alone before treating it as real, and check a suspect failure on `develop`.
3. The whole unit suite, the MCP suite, a Release build of the Extras with `-p:RunTrimAnalyzers=true` (it builds both
   target frameworks), and the BOM check over every new file.
4. Manual screen-reader pass per `docs/accessibility/README.md` (Tier 1: NVDA with Firefox and Chrome, JAWS, macOS
   VoiceOver; iOS VoiceOver and Tier 2 TalkBack and Narrator since the component is touch-aware), recorded in
   `docs/accessibility/records/BitVirtualKeyboard.md`, marking *Not run* where a reader is unavailable.

Gate: section 12.

---

## 11. Testing

### 11.1 Commands

```bash
# build
dotnet build src/BlazorUI/Bit.BlazorUI.Extras/Bit.BlazorUI.Extras.csproj
dotnet build src/BlazorUI/Tests/Bit.BlazorUI.Tests/Bit.BlazorUI.Tests.csproj

# unit tests. CI (ci.blazorui.yml) builds with -f <tfm> -p:BuildAllTfms=true and runs `dotnet test --no-build -f <tfm>`
# in the test folder for net11.0 and net10.0. If `dotnet test` reports zero tests locally, run the assembly directly:
dotnet exec src/BlazorUI/Tests/Bit.BlazorUI.Tests/bin/Debug/net11.0/Bit.BlazorUI.Tests.dll --filter "FullyQualifiedName~VirtualKeyboard"
dotnet exec src/BlazorUI/Tests/Bit.BlazorUI.Tests/bin/Debug/net11.0/Bit.BlazorUI.Tests.dll   # whole suite (contract tests)

# MCP suite
dotnet run --project src/BlazorUI/Tests/Bit.BlazorUI.Tests.Mcp

# E2E (build first; install browsers once with the generated playwright.ps1; check the port is free:
# Get-NetTCPConnection -LocalPort 5290)
dotnet run --project src/BlazorUI/Tests/E2E/Bit.BlazorUI.Tests.E2E.TestHost --no-build --framework net11.0 --urls http://localhost:5290
E2E_HOST_URL=http://localhost:5290 BROWSER=chromium dotnet exec src/BlazorUI/Tests/E2E/Bit.BlazorUI.Tests.E2E/bin/Debug/net11.0/Bit.BlazorUI.Tests.E2E.dll --filter "FullyQualifiedName~Extras.BitVirtualKeyboard"
# repeat with BROWSER=firefox and BROWSER=webkit, and with E2E_RENDER_MODE=wasm

# both target frameworks and trim analyzers
dotnet build src/BlazorUI/Bit.BlazorUI.Extras/Bit.BlazorUI.Extras.csproj -c Release -p:RunTrimAnalyzers=true

# BOM check for new files (Git Bash)
for f in $(git ls-files --others --exclude-standard src/BlazorUI); do head -c 3 "$f" | xxd -p | grep -q efbbbf || echo "NO BOM: $f"; done
```

`src/BlazorUI/Tests/E2E/README.md` wins over these lines. Never kill every `dotnet.exe` (other runs use it).

### 11.2 Unit and Node coverage (minimum)

Shorthand grammar; JSON; validation errors; merge precedence; purpose variations; derived layers (en, tr); digits and
separators; scramble; geometry; catalog integrity and data spot checks; rendering markup and accessibility attributes;
culture invariance; disabled; Params; Strings fallback and composed names; interop setup/update/callbacks/disposal and
one-way binds; text-engine pure functions.

### 11.3 E2E coverage (minimum, every engine, both render modes)

1. Typing into an input at the caret and replacing a selection; the bound value updates with `@bind` (on change) and
   `@bind:event="oninput"`; the input keeps focus after each key press.
2. Backspace removes one code point after `e` + combining acute and after a Persian ZWNJ, and a whole emoji with skin
   tone, a ZWJ family and a flag.
3. `maxlength` and `AllowedCharacters` honoured; `type=email` appends and deletes; `type=number` accepts `1`, `.`, `5`
   in sequence and ends with `1.5` (never wiped).
4. `inputmode="none"` set on attach and the original restored on detach.
5. Shift one-shot, double-tap lock, Caps Lock (digits unchanged); symbols layer and return; an AltGr character on `de`.
6. Long-press popup by holding, by holding and releasing then tapping an item, and by `contextmenu`; dead key `^` + `e`
   = `ê` and `^` + `x` = `^x`.
7. Key repeat while holding Backspace (count grows; stops on release).
8. Enter: newline in a textarea, submit in a form, `next` moves focus; a keydown cancelled by the page prevents it.
9. `change` fires once on blur after virtual typing, and once (not twice) after mixed physical and virtual typing.
10. Follow focus with Docked mode: opens on focus of an opted-in input, closes on blur, Escape closes it while focus is
    in the input, the focused input is scrolled above the panel.
11. Purpose detection: `type=tel` shows the phone pad, `inputmode=decimal` the decimal pad, `type=email` shows `@` and
    `.com`; a `type=date` input does not attach.
12. RTL: the Persian layout renders with the grid in LTR order (ض leftmost on the first letter row), labels isolated,
    typed text correct, `{zwnj}` types U+200C.
13. Value-mode roving focus: Tab into the keyboard, arrows move, Enter types.
14. Secure mode on a password input: no preview bubble and no pressed attribute; `OnInput` not raised.
15. Axe scan with no violations for every section; the page loads without console errors.

### 11.4 Manual checks

The demo by hand (Chromium, then once each in Firefox and a WebKit-based browser if available); a touch device or touch
emulation for long-press and multi-touch; **iOS Safari for focus retention on tap** (not testable in Playwright on
Windows); the screen-reader pass (phase 9).

---

## 12. Definition of done

- Every phase gate passed; the whole unit suite, the MCP suite and the scoped E2E runs green, except failures shown to
  be pre-existing on `develop` (list them with evidence).
- A Release build of the Extras with trim analyzers passes (both target frameworks).
- Every new file has a BOM; no file outside `src/BlazorUI` changed (`git status --short`).
- The demo page is complete per CLAUDE.md; nav, catalog, MCP DataRow and contract lists updated.
- The accessibility record exists.
- No commits, branches or pushes made.
- The hand-off is written (section 13).

## 13. Hand-off report (the final message)

1. What was built (files created and modified, grouped).
2. Test results with counts per suite and engine, and any pre-existing failures with how they were attributed.
3. Deviations from this plan, each with its reason.
4. Anything not done, and why.
5. Open questions still waiting for the maintainer (section 15).

---

## 14. Risks and pitfalls

| Risk | Mitigation |
|---|---|
| Blazor re-render wipes script state | D14 attributes only; popup/preview containers rendered unconditionally and empty; payload version on the root. |
| Wrong character typed between a re-render and `update` | `data-bit-vkb-v` check; scramble permutes positions, not outputs. |
| Focus leaves the input on key press | `pointerdown` `preventDefault`, `tabindex="-1"` keys; E2E 1 in every engine; iOS by hand. |
| `@bind` not updating, or `OnChange` twice | `input` per change; `change` on commit only when the value differs from the native baseline (6.8); E2E 1 and 9. |
| `type=number`/`email` lacking selection APIs; number sanitisation | Feature detection per type; shadow buffer for numbers; E2E 3. |
| Escape never reaching the keyboard in target modes | Claim on the target (6.9); E2E 10. |
| Trimming drops the scripts or payload members | Literal identifiers, `[DynamicDependency]` on every payload type, `on-demand` engine, trimming contract tests, Release build with trim analyzers. |
| Shared mutable built-ins on Blazor Server | Fresh instance per access (D5). |
| Recompiling on every parent render | Content-hash cache; payload compared by serialized form. |
| Turkish casing missing in WASM | Hard-coded tr/az pairs (5.4 step 5). |
| BOM missing on new files | Check after every file creation; the DoD loop. |
| Contract tests failing on naming, order or demo tables | Alphabetical parameters, no `Is` prefix, demo skeleton in phase 2, whole suite at every gate. |
| Layout data typos | Copy from `layout-data.md`; width and spot-check tests; native-speaker review (15). |
| SignalR latency on Server | Typing and repeat in JS; `OnKeyPress`/`OnInput` round trips only when handlers exist. |

## 15. Open questions for the maintainer (ask only if blocking; otherwise proceed with the default in brackets)

1. Popup (anchored) and Floating modes: v1 or follow-up? [follow-up]
2. The P0 language list and the `fr-t-k0-windows` id for legacy AZERTY (CLDR `fr` is AFNOR). [as in 8.2]
3. The POS, ATM, IBAN and symbol-layer designs (no standard exists). [as in 8.1 and 8.5]
4. A native-speaker review of the long-press tables (8.4) and the Arabic-script symbol layers (8.5). [ship as written,
   flag in the hand-off]
5. Core inputs gaining an opt-in to the keyboard later (needs a core-side hook). [out of scope]

---

## 16. Verification record (2026-11-25)

This plan was checked by four independent reviews before hand-off; the corrections are already folded in above.

- **Repository:** every file, type, member, attribute, contract test, registration point, command and CLAUDE.md
  reference was opened and confirmed against develop `6e83ea7d7e` (for example the generator's `XHasBeenSet`/`AssignX`,
  the JSInvokable naming, `DisposeAfterAsync` and the IL check, the Escape claim walk, the Node test runner, the demo
  and MCP contracts).
- **Web platform:** selection APIs, `setRangeText`, `change` semantics, untrusted events, Blazor's event delegation
  (`EventDelegator.ts`, no `isTrusted` check), `maxlength` units, number/date sanitisation, native Backspace behaviour,
  focus retention and feature support were confirmed against WHATWG/W3C/MDN/Unicode sources and by live runs in
  Chromium 156, Firefox 157 and WebKit 27.2.
- **Standards:** WCAG 2.2 criteria (2.1.1, 2.5.2, 2.5.3, 2.5.5, 2.5.7, 2.5.8), APG toggle buttons, ITU-T E.161
  letters and the tactile mark on 5, CLDR 49 and the `k0-windows` transform, Unicode code points and dead-key spacing
  characters, Android timing defaults.
- **Layout data:** every P0 layout was re-decoded from the Windows KLC tables (dll 10.0.29667.1000) and ISIRI 9147
  (Wikipedia raw table) and compared character by character; `layout-data.md` was corrected (Persian 2B/ISO keys and
  AltGr row, UK, French, ABNT2 and Turkish AltGr data).
- **C# design:** the model compiled on SDK 11.0.100 (collection expressions through the implicit conversion, object
  initialisers, `with`); the grammar regexes and the geometry algorithm were run on the purpose pads and the Full
  templates (the overlap and width issues found there are fixed in 5.2, 5.3, 5.5, 8.1 and 8.2); System.Text.Json
  behaviour (no implicit operators, numeric enums by default, enum dictionary keys) was measured.
- **Second round on the revised plan:** the new grammar was run over every shorthand row in the plan, and the new
  geometry over every pad (Inline and Docked), the `en` ANSI and `de` ISO Full layers and the Compact and symbol
  layers (every pad row fills its columns, every Full row is 15 units, every symbol row 10, no overlaps); the model
  compiled with zero errors and zero AOT/trim warnings, with a source-generated JSON round trip of string rows, string
  enums and enum dictionary keys. Its findings (missing Full symbols layer, ISO Enter leaking into other layers,
  grammar edge cases, label-in-name for `abc`/`sym`, number digits, key-height floors, docked state ownership,
  scramble slot geometry, JSON details, culture precedence, Escape edge cases) are fixed above.
- **Not verifiable here:** iOS Safari touch focus retention, screen-reader behaviour, and a native-speaker review of the
  Arabic-script and Hebrew long-press/symbol designs. They are in 11.4, phase 9 and section 15.
