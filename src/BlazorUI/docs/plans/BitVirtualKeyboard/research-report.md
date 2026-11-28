# Build BitVirtualKeyboard as a layout engine

> **Note (2026-11-25):** the row data in the section "Verified row data for the language catalog" below is superseded by `layout-data.md`, which corrects the Persian, UK, French, Brazilian and Turkish entries after a second decoding of the sources. The implementation plan (`PLAN.md`) also revises several recommendations made here (for example Backspace deletes one code point, not a grapheme cluster).

The recommendation is to build **BitVirtualKeyboard** as a data-driven layout engine with a catalog on top, not as a fixed set of keyboards. Its schema should follow Unicode CLDR LDML Keyboard 3.0:
- a "bag" of keys
- named layers, selected by modifier or by id
- rows that list key ids
- gestures: long-press, multitap and flick
- dead-key markers resolved by transforms

Geometry should use relative widths, gaps and a "stretch to fill" option, an idea shared by Keyman, Qt, GNOME Shell and HeliBoard. Per-purpose key swaps should use selectors in the style of FlorisBoard and HeliBoard ([UTS #35 Part 7](https://www.unicode.org/reports/tr35/tr35-keyboards.html); [Keyman touch-layout schema](https://github.com/keymanapp/keyman/blob/master/common/schemas/keyman-touch-layout/keyman-touch-layout.spec.json); [HeliBoard layouts.md](https://github.com/Helium314/HeliBoard/blob/main/layouts.md)).

On top of that engine, bit should ship two catalogs:
- **About 14 purpose pads**, selected automatically from the HTML `inputmode`/`type`/`enterkeyhint` signals: phone, PIN, numeric, decimal, calculator, POS, email, URL, search, hex and others.
- **About 25 national layouts**, including RTL Persian (ISIRI 9147), Arabic 101, Hebrew and Urdu. Their row data can be machine-extracted from Windows layout tables, and this report includes that data.

The opening in the market is real. Among the major Blazor suites, **only Blazorise ships a first-party on-screen keyboard**. Searches found none in Telerik, Syncfusion, DevExpress, Radzen, MudBlazor core or Smart.Blazor ([Blazorise docs](https://blazorise.com/docs/components/on-screen-keyboard); [Radzen forum](https://forum.radzen.com/t/how-to-open-an-on-screen-keyboard/20252)). None of the .NET components examined documents RTL keyboards, long-press alternates or dead keys. Those three features, plus a strong accessibility story, are where bit can lead.

The engineering risk sits less in the layouts than in browser text-entry correctness:
- inserting at the caret
- deleting whole grapheme clusters on Backspace
- firing real `input`/`change` events so `@bind` sees the change
- keeping focus in the input
- suppressing the OS keyboard with `inputmode="none"`

These belong in P0 next to the layout engine.

## Only Blazorise ships one, and no .NET keyboard documents RTL

There are few Blazor options, and each is narrow. **Blazorise** sets the API bar:
- a provider placed once, with inputs opting in
- six purpose layouts: Text, Numeric, Decimal, Email, Url, Telephone
- `LayoutProvider`/`LayoutSelector`/`ShouldShow` delegates
- a `KeyTemplate` RenderFragment
- an Enter-behaviour enum: Default, NewLine, Submit, Hide, KeyDown

Its keys come from nested `OnScreenKeyboardKey` lists, and its docs describe no language catalog ([Blazorise API](https://blazorise.com/docs/components/on-screen-keyboard/api)). **MudKeyboard**, a community package with about 10 GitHub stars, is pure C#. It offers:
- inline `Value` binding plus a docked global host
- an "emit mode"
- a value preview, where Cancel reverts the edit
- `string[][]` layouts with `{shift}`/`{bksp}` tokens
- good ARIA: `<button>` keys, `aria-pressed`, and `inert` when hidden

Its built-ins stop at Qwerty/Symbols/Numeric/Numpad/Price, and its docs mention no other languages and no RTL ([MudKeyboard](https://github.com/sardar97/mudkeyboard)). **BootstrapBlazor.OnScreenKeyboard** wraps the KioskBoard JavaScript library through `data-kioskboard-*` attributes ([NuGet](https://www.nuget.org/packages/BootstrapBlazor.OnScreenKeyboard)). KioskBoard itself ships 9 language JSONs, Persian and Arabic among them, and has not had a release since v2.3.0 in 2022 ([KioskBoard](https://github.com/furcan/KioskBoard)).

The desktop reference design is **Telerik RadVirtualKeyboard** (WPF and WinForms):
- relative `Width`/`Height` keys and `EmptySpace` spacers
- `KeyType` values Normal/Special/Modifier/Lock/Numpad
- an XML `LoadLayout`/`SaveLayout`
- a cancellable `KeySending` event plus `KeySent`
- letters that follow the OS culture

It is still a hardware-keyboard model with no long-press or flick concepts ([Telerik WPF keys layout](https://www.telerik.com/products/wpf/documentation/controls/radvirtualkeyboard/keys-layout); [Telerik WinForms PME](https://www.telerik.com/products/winforms/documentation/controls/virtual-keyboard/properties-methods-events)). **Avalonia's** paid VirtualKeyboard has the richest language-ID model: `en-US:kbd:standard`, `ja:ime:kana`, `ko:ime:hangul`, `zh:ime:rime` ([Avalonia control reference](https://docs.avaloniaui.net/controls/input/text-input/virtualkeyboard/virtualkeyboard-control)).

| Product | Stack / status | Layout definition | Languages / RTL | Long-press, dead keys | Notes for bit |
|---|---|---|---|---|---|
| Blazorise On-screen Keyboard | Blazor, first-party | `IReadOnlyList<IReadOnlyList<OnScreenKeyboardKey>>` + delegates | Not documented | Not documented | Copy its provider/opt-in model and Enter enum |
| MudKeyboard | Blazor, community, MIT | `string[][]` with `{tokens}` | QWERTY only (as documented) | One-shot Shift, double-tap Caps | Copy emit mode, value preview, ARIA |
| BootstrapBlazor.OnScreenKeyboard | Blazor add-on wrapping JS | KioskBoard JSON rows | 9 incl. fa, ar | No | Shows how thin JS wrappers are |
| BlazorVirtualKeyboard | Blazor prototype | Numpad only | No | No | Not competitive ([NuGet](https://www.nuget.org/packages/BlazorVirtualKeyboard)) |
| Telerik RadVirtualKeyboard | WPF/WinForms, commercial | XML `Rows/Row/Keys/Key`, key factory | OS culture sync | No | Model for cancellable events and `KeyType` |
| Avalonia VirtualKeyboard | Avalonia, paid | Not documented | Language IDs incl. IMEs | Popup keys (by resource names) | Model for layout-ID naming |
| Telerik/Syncfusion/DevExpress/Radzen Blazor, MudBlazor core, Smart.Blazor | — | None found | — | — | The gap bit fills |

A caveat: these absence findings come from searches, not a full audit of each catalog. Fluent UI Blazor, Ant Design Blazor and Ignite UI for Blazor were not checked. The market signal still holds up:
- Industrial web HMIs such as Ignition Perspective need workarounds to get the Windows OSK in kiosk mode ([Industrial Monitor Direct](https://industrialmonitordirect.com/blogs/knowledgebase/enabling-windows-osk-in-ignition-perspective-kiosk-mode)).
- Launching `TabTip.exe` from code fails on Windows 11 non-tablet desktops ([MS Q&A](https://learn.microsoft.com/en-us/answers/questions/923058/windows-11-touch-keyboard-run-from-code-cmd-ps)).
- A browser cannot launch either OS keyboard at all.

So for a Blazor kiosk, POS or HMI app, an in-page keyboard is the only dependable cross-platform option.

On the JavaScript side, **simple-keyboard** (2,464 stars, published to npm daily) and **MathLive** are the only large, actively maintained web keyboards. Mottie's jQuery Keyboard is the richest feature reference, but nobody has pushed to it since 2022 ([simple-keyboard](https://github.com/hodgef/simple-keyboard); [Mottie/Keyboard](https://github.com/Mottie/Keyboard)). simple-keyboard's option names (`layout`, `layoutName`, `display`, `buttonTheme`, `onKeyPress`, `onChange`) are the vocabulary developers will expect. It also shows what to avoid:
- Shift handling is left to user code.
- Keys are `<div>`s by default.
- No ARIA is built in.
- Its `rtl` option wraps the bound value in RLE/PDF control characters, which pollutes the data ([simple-keyboard source](https://github.com/hodgef/simple-keyboard/blob/master/src/lib/components/Keyboard.ts)).

## Ten schemas converge on keys, layers, rows, gestures and transforms

Every mature system describes rows of keys with **relative widths, not pixels**. Each one keeps what a key *shows* separate from what it *emits*, and stacks state on top as named layers or as selectors inside a key. The table maps each design concern to the best prior art. The model proposed below takes one idea from each row.

| Concern | Best prior art | What to borrow |
|---|---|---|
| Key bag + rows of ids | CLDR `keys/key` + `layer/row keys="…"` ([UTS #35](https://www.unicode.org/reports/tr35/tr35-keyboards.html#element-row)) | Rows reference keys by id. Undeclared ids are implied literals (CLDR implies a–z, A–Z, 0–9, `space`, `gap`) |
| Width units | CLDR `width` (default 1.0), GNOME `width` multiples, Qt weight with Key > Row > Layout inheritance ([Qt technical guide](https://github.com/qt/qtvirtualkeyboard/blob/dev/src/virtualkeyboard/doc/src/technical-guide.qdoc)) | `Width` in key units with layout and row defaults |
| Fill and gaps | CLDR `stretch`/`gap`, HeliBoard `width: -1`, Qt `FillerKey`, GNOME `leftOffset`, Keyman `sp: 10` spacer and `pad` | `Stretch`, `Gap` keys with width, a leading `Offset` |
| Tall/wide keys | Telerik `Height`, KLE `h`/`x2,y2`, QMK ISO Enter 1.25u×2u ([KLE format](https://github.com/ijprest/keyboard-layout-editor/wiki/Serialized-Data-Format)) | `Height` (row span) for numpad `+`/Enter. Leave non-rectangular shapes to P2 |
| Layers by modifier | CLDR `modifiers="shift"`, `"ctrl alt"`, `"altR"` ([UTS #35 layer](https://www.unicode.org/reports/tr35/tr35-keyboards.html#element-layer)) | `Modifiers` for hardware-derived layouts (base/Shift/AltGr/Shift+AltGr) |
| One-shot vs locking | GNOME Shell levels `mode: latched \| locked` ([us.json](https://gitlab.gnome.org/GNOME/gnome-shell/-/blob/main/data/osk-layouts/us.json)), Squeekboard `lock_view`/`unlock_view` | `Mode` per layer, and double-tap Shift to lock (MudKeyboard) |
| Return after output | Keyman `nextlayer` ([Keyman touch layout](https://github.com/keymanapp/keyman/blob/master/developer/docs/help/reference/file-types/keyman-touch-layout.md)) | `NextLayer` on keys, e.g. back to letters after a symbol |
| Small per-purpose diffs | FlorisBoard/HeliBoard `variation_selector` (comma becomes `@` on email, `/` on URL), `shift_state_selector`, `layout_direction_selector` ([charactersMod/default.json](https://github.com/florisboard/florisboard/blob/main/app/src/main/assets/ime/keyboard/org.florisboard.layouts/layouts/charactersMod/default.json)) | `Variations` by purpose, so the whole layout is not duplicated |
| Long-press | CLDR `longPressKeyIds` + `longPressDefaultKeyId`, Qt `alternativeKeys: "êeëèé"`, FlorisBoard per-language `popupMappings` with `main` + `relevant`, MathLive `variants` | Inline per-key list, plus a layout-level per-character table |
| Multitap, flick | CLDR `multiTapKeyIds`, `flick/flickSegment directions="n e s w ne nw se sw"`, Keyman `multitap[]`/`flick{}` | Same shapes. Flicks need tap alternatives (WCAG 2.5.1) |
| Secondary label | Qt `smallText`, MathLive `aside`, HeliBoard dialpad popup `ABC` | `Hint` ("ABC" on 2, "!?" on the period key) |
| Dead keys | CLDR markers `\m{acute}` + `transforms`, GreyWyvern `{ '"': { a: 'ä' } }` tables ([UTS #35 markers](https://www.unicode.org/reports/tr35/tr35-keyboards.html#markers); [ngx-material-keyboard deadkeys](https://github.com/ngx-material-keyboard/core/blob/master/src/core/src/configs/keyboard-deadkey.config.ts)) | Simple tables that compile to marker transforms |
| Backspace clusters | CLDR `transforms type="backspace"` deletes क्श in one press | `BackspaceTransforms` (P2), with grapheme deletion as default |
| Special keys | Keyman `K_BKSP`/`K_LOPT`/`K_ROPT`, labels `*ZWNJ*`, `*RTLEnter*`, `*RTLBkSp*`; KeyboardKit `KeyboardAction` enum | A closed action enum plus `Custom` |
| Shorthand | simple-keyboard `"{shift} z x c {bksp}"`, Squeekboard `views` rows + `buttons` table, vue-touch-keyboard `_meta` | String rows + a side table of rich `{token}` definitions |
| Locale fallback | Qt `<lang>_<COUNTRY>` → `<lang>_*` → `fallback` per layout type; Squeekboard `hint/lang[+variant][_wide]` | A registry keyed by (purpose, culture, width class) |
| Style vs layout | Qt: "layouts cannot specify any visual elements"; Squeekboard `outline` classes; Onboard `group` | `Type`/`Class` hooks only, with visuals in SCSS and ClassStyles |

Three conclusions come out of the comparison. First, **CLDR keyboard3 is the right semantic core**. It is the only vendor-neutral standard, it covers hardware and touch in one file, and the current spec is CLDR 49 / tr35-79, dated 2026-10-21 ([UTS #35](https://www.unicode.org/reports/tr35/tr35-keyboards.html)). Its own non-goals point to where a UI component has to add things. CLDR leaves out "frame keys" (Backspace, Enter, layer switching beyond `layerId`, hide), screen adaptation and accessibility, so the bit model needs an action enum and an ARIA layer on top. CLDR is also sparsely populated: the 3.0 repo holds only about a dozen sample keyboards ([CLDR keyboards/3.0](https://github.com/unicode-org/cldr/tree/main/keyboards/3.0)). bit therefore cannot simply import a world catalog from CLDR.

Second, **Keyman's touch JSON is the most practical JSON precedent**. Its shape is platform → layer → row → key with `id/text/sp/width/pad/nextlayer/sk/multitap/flick`, and it proves the CLDR concepts work on touch.

Third, **HeliBoard's selectors solve the duplication problem**. Without them, an email layout is a copy of the text layout with one key changed, multiplied by every language.

## A proposed C# object model and compact shorthand

The model below follows the bit file conventions. Types go in `BitVirtualKeyboardParams.cs` or alongside. Every class serializes cleanly with System.Text.Json, so layouts can also be loaded from JSON. Comments name the precedent for each member. The design choices labelled as inference are mine, not taken from a source.

```csharp
namespace Bit.BlazorUI;

public sealed class BitVirtualKeyboardLayout
{
    public required string Id { get; init; }      // BCP 47 + CLDR "-t-k0-" variant: "fa", "fa-t-k0-windows", "fr-t-k0-windows", "x-phone"
    public required string Name { get; init; }    // "فارسی (ISIRI 9147)" — CLDR info/@name
    public string? Indicator { get; init; }       // "FA" on the language key — CLDR info/@indicator
    public string? Culture { get; init; }         // decimal separator, case mapping (tr-TR ı/İ!)
    public BitDir Direction { get; init; }        // direction of produced TEXT; the key grid itself stays LTR
    public BitVirtualKeyboardForm Form { get; init; }       // Touch | Ansi | Iso | Abnt2 | Jis | Ks — CLDR forms
    public BitVirtualKeyboardDigitSet DigitSet { get; init; } // Latin | ArabicIndic | Persian | Devanagari | Thai
    public string? Extends { get; init; }         // inherit and patch another layout (Qt/HeliBoard fallback chains)
    public double KeySpacing { get; init; } = 0.1; // gap between keys in key units — Telerik KeySpacing

    public Dictionary<string, BitVirtualKeyboardKey> Keys { get; init; } = [];          // the key bag — CLDR keys/key
    public List<BitVirtualKeyboardLayer> Layers { get; init; } = [];                    // first = "base"
    public Dictionary<string, string> LongPress { get; init; } = [];                    // "e" -> "é è ê ë ē" — FlorisBoard popupMappings, Mottie altKeys
    public Dictionary<string, Dictionary<string, string>> DeadKeys { get; init; } = []; // "acute" -> { "e":"é", " ":"´" } — GreyWyvern tables
    public List<BitVirtualKeyboardTransform> Transforms { get; init; } = [];            // regex from/to — CLDR transforms (P2)
    public List<BitVirtualKeyboardTransform> BackspaceTransforms { get; init; } = [];   // CLDR type="backspace" (P2)
    public Dictionary<string, string> Displays { get; init; } = [];                    // output or key id -> keycap text — CLDR displays
}

public sealed class BitVirtualKeyboardLayer
{
    public required string Id { get; init; }                     // "base", "shift", "altgr", "symbols", "numbers"
    public BitVirtualKeyboardModifiers Modifiers { get; init; }  // None | Shift | AltGr | ShiftAltGr | Caps — CLDR layer/@modifiers
    public BitVirtualKeyboardLayerMode Mode { get; init; }       // Locked | Latched (one-shot) — GNOME Shell levels
    public double? MinWidth { get; init; }                       // pick by container width — CLDR minDeviceWidth, Squeekboard _wide
    public List<BitVirtualKeyboardRow> Rows { get; init; } = []; // accepts shorthand strings via implicit conversion
}

public sealed class BitVirtualKeyboardRow
{
    public List<BitVirtualKeyboardKey> Keys { get; init; } = [];
    public double Height { get; init; } = 1;                       // relative row height
    public BitVirtualKeyboardRowAlign Align { get; init; }         // Stretch | Center | Start | End (inference)
    public static implicit operator BitVirtualKeyboardRow(string shorthand) => BitVirtualKeyboardShorthand.ParseRow(shorthand);
}

public sealed class BitVirtualKeyboardKey
{
    public string? Id { get; init; }                // bag id; tokens in rows resolve against it
    public string? Output { get; init; }            // inserted text; multi-code-point OK ("لا", ".com", "क्ष") — CLDR output, Keyman U_xxxx_yyyy
    public string? Label { get; init; }             // keycap; defaults to Output; combining marks shown on ◌ (UTS #35 display rule)
    public string? Hint { get; init; }              // secondary label: "ABC" on 2 — Qt smallText, MathLive aside
    public string? Icon { get; init; }              // bit icon name for Backspace/Enter/Shift/globe
    public string? AriaLabel { get; init; }         // must contain the visible label (WCAG 2.5.3)
    public BitVirtualKeyboardKeyAction Action { get; init; }  // Character (default) | Backspace | Enter | Shift | …
    public string? Layer { get; init; }             // target of SwitchLayer/LockLayer — CLDR layerId, Squeekboard set_view
    public string? NextLayer { get; init; }         // layer to return to after output — Keyman nextlayer
    public string? DeadKey { get; init; }           // marker id; next key is resolved through Layout.DeadKeys — CLDR \m{…}
    public string? Command { get; init; }           // for Action=Custom, raised via OnCommand — Mottie keyaction, KeyboardKit custom(named:)

    public double? Width { get; init; }             // key units, default 1 — CLDR width, GNOME width, MathLive width
    public double Height { get; init; } = 1;        // rows spanned (numpad + and Enter) — Telerik Height, KLE h
    public bool Stretch { get; init; }              // absorb leftover row width — CLDR stretch, HeliBoard -1
    public double Offset { get; init; }             // empty space before the key — GNOME leftOffset, Keyman pad
    public bool Gap { get; init; }                  // invisible, inert spacer — CLDR gap, Keyman sp=10, Qt FillerKey
    public bool Blank { get; init; }                // visible but inert — Keyman sp=9

    public List<string>? LongPress { get; init; }   // overrides Layout.LongPress[Output] — CLDR longPressKeyIds
    public string? LongPressDefault { get; init; }  // pre-selected popup item — CLDR longPressDefaultKeyId, FlorisBoard "main"
    public List<string>? MultiTap { get; init; }    // T9-style cycling — CLDR multiTapKeyIds, Keyman multitap
    public Dictionary<BitVirtualKeyboardFlickDirection, string>? Flicks { get; init; } // n,ne,e,… — CLDR flicks (needs tap alternative)
    public bool? Repeat { get; init; }              // null = default per action (Backspace/Delete/Space/arrows repeat)
    public bool? Preview { get; init; }             // pressed-key bubble — Qt showPreview
    public Dictionary<BitVirtualKeyboardPurpose, BitVirtualKeyboardKey>? Variations { get; init; } // HeliBoard variation_selector
    public BitVirtualKeyboardKeyType? Type { get; init; } // Character | Function | Modifier | Primary | Numeric — styling hook
    public string? Class { get; init; }
    public string? Style { get; init; }
    public bool Scramble { get; init; } = true;     // participates in secure shuffling; frame keys default false
}

public sealed class BitVirtualKeyboardTransform { public required string From { get; init; } public string? To { get; init; } }

public enum BitVirtualKeyboardKeyAction
{
    Character, Text, Backspace, Delete, Enter, Space, Tab, ShiftTab,
    Shift, CapsLock, AltGr, SwitchLayer, LockLayer, DeadKey,
    NextLayout, LayoutMenu, Hide, Clear, Accept, Cancel, Next, Previous,
    ArrowLeft, ArrowRight, ArrowUp, ArrowDown, Home, End,
    SelectAll, Copy, Cut, Paste, Undo, Redo, Custom
}

public enum BitVirtualKeyboardPurpose      // superset of WHATWG inputmode + iOS/Android/Windows hints
{
    Text, Search, Email, Url, Tel, Numeric, Decimal, SignedDecimal,
    Pin, Calculator, Currency, Hex, Date, Time, Iban, Password, None
}
```

**Width and geometry rules** (inference based on the table above). A row's natural width is the sum of `Offset + Width` over its keys. The layout's unit width comes from its widest row. Narrower rows follow `Align`. The default, `Center`, gives the classic half-key indent on the QWERTY A-row, which GNOME encodes as `leftOffset: 0.5`. Avoid Keyman's habit of silently stretching the last key. Any `Stretch` keys share the leftover width.

Render with CSS grid. Use one column per quarter unit, since KLE geometry uses 0.25u steps, and let tall keys span rows with `grid-row: span N`. All sizes are relative. Absolute size comes from a key-size CSS variable set by the size preset. This keeps Qt's rule that the layout carries no visual data.

**Key behaviours by action.** `Shift` latches for one character. Double-tap locks it (MudKeyboard; GNOME `latched`). `CapsLock` and `LockLayer` lock until pressed again. `SwitchLayer` jumps to a layer, and `NextLayer` on a symbol key returns to letters afterwards. `DeadKey` sets a pending marker, which the next key resolves through `DeadKeys[marker][char]`. When no entry matches, emit the spacing accent followed by the character, the usual OS behaviour (inference; not sourced). Long-press and auto-repeat compete for the same gesture, so a key with a `LongPress` list does not repeat, the same rule HeliBoard uses ([HeliBoard layouts.md](https://github.com/Helium314/HeliBoard/blob/main/layouts.md)).

### Shorthand grammar

Most users should never write a `new BitVirtualKeyboardKey`. The shorthand follows simple-keyboard and Squeekboard: whitespace-separated tokens, one string per row, and `{…}` for named keys. The resolution rules are as follows:

1. A token in braces, `{id}`, resolves first to `Layout.Keys[id]` and then to a built-in key: `{bksp} {enter} {shift} {caps} {altgr} {space} {tab} {sym} {abc} {num} {lang} {hide} {clear} {accept} {cancel} {left} {right} {zwnj} {zwj} {lrm} {rlm} {nbsp} {gap}`.
2. A width override goes inside the braces: `{shift*1.5}`, `{gap*0.5}`, `{space*5}`. Literal `*` and `:` keys therefore need no escaping.
3. A layer jump goes inside the braces too: `{layer:symbols}`, or `{layer:symbols*1.5}` with a width.
4. Any other token is a character key that outputs itself. Multi-character tokens output the whole string (`.com`, `www.`, `لا`), as in simple-keyboard.
5. The escapes `\u{200C}` (CLDR syntax), `\{` and `\\` are supported.

Long-press lists stay out of the row strings, which keeps rows readable. They go in `Layout.LongPress` as space-separated lists, Mottie's `altKeys` format.

Example: the Persian ISIRI 9147 hardware form. The rows below are the exact ISIRI rows from the catalog section, plus frame keys.

```csharp
public static readonly BitVirtualKeyboardLayout PersianIsiri = new()
{
    Id = "fa", Name = "فارسی (ISIRI 9147)", Indicator = "فا", Culture = "fa-IR",
    Direction = BitDir.Rtl, Form = BitVirtualKeyboardForm.Iso, DigitSet = BitVirtualKeyboardDigitSet.Persian,
    Layers =
    [
        new() { Id = "base", Rows =
        [
            "{zwj} ۱ ۲ ۳ ۴ ۵ ۶ ۷ ۸ ۹ ۰ - = {bksp*2}",
            "{tab*1.5} ض ص ث ق ف غ ع ه خ ح ج چ",
            "{caps*1.75} ش س ی ب ل ا ت ن م ک گ {enter*2.25}",
            "{shift*2.25} ظ ط ز ر ذ د پ و . / {shift*2.75}",
            "{sym*1.5} {lang} {zwnj*1.5} {space*6} {altgr*1.5} {hide}",
        ]},
        new() { Id = "shift", Modifiers = BitVirtualKeyboardModifiers.Shift, Mode = BitVirtualKeyboardLayerMode.Latched, Rows =
        [
            "÷ ! ٬ ٫ ﷼ ٪ × ، * ) ( ـ + {bksp*2}",
            "{tab*1.5} \\u{652} \\u{64C} \\u{64D} \\u{64B} \\u{64F} \\u{650} \\u{64E} \\u{651} ] [ } {",
            "{caps*1.75} ؤ ئ ي إ أ آ ة » « : ؛ {enter*2.25}",
            "{shift*2.25} ك \\u{653} ژ \\u{670} {zwnj} \\u{654} ء > < ؟ {shift*2.75}",
            "{sym*1.5} {lang} {zwnj*1.5} {space*6} {altgr*1.5} {hide}",
        ]},
    ],
    Displays = { ["\u0652"] = "◌ْ", ["\u064E"] = "◌َ" /* … one entry per haraka */ },
};
```

The same engine produces a phone pad:

```csharp
Keys =
{
    ["2"] = new() { Output = "2", Hint = "ABC", MultiTap = ["a","b","c"] },  // MultiTap only when letter entry is enabled
    ["0"] = new() { Output = "0", Hint = "+", LongPress = ["+"], LongPressDefault = "+" },
},
Layers = [ new() { Id = "base", Rows = [ "1 2 3 {bksp}", "4 5 6 {space}", "7 8 9 {hide}", "* 0 # {enter}" ] } ]
```

The `Variations` selector handles the email/URL case without copying layouts. The text layout's comma key declares `Variations = { [Email] = new() { Output = "@" }, [Url] = new() { Output = "/" } }`, exactly as FlorisBoard and HeliBoard do. bit should also accept the Blazorise-style delegate, `Func<BitVirtualKeyboardContext, BitVirtualKeyboardLayout>`, for fully dynamic cases.

For interchange, the shipped catalog should be generated at build time from **kbdlayout.info**, which publishes every Windows layout as KLC, XML, JSON and CLDR downloads ([kbdlayout.info KBDFAR](https://kbdlayout.info/KBDFAR/)). Importers can follow in priority order: CLDR keyboard3 XML, then Keyman `.keyman-touch-layout`, then KLE JSON for geometry only. A generator emitting static C# data, rather than runtime JSON, keeps the catalog trim-safe (inference).

**Composition hook.** Korean, Japanese and Chinese need more than a key map. Hangul syllable composition is algorithmic and can be built in. Kana-to-kanji and pinyin-to-hanzi conversion need a dictionary and a candidate bar ([Wikipedia: Keyboard layout §Chinese](https://en.wikipedia.org/wiki/Keyboard_layout#Chinese)). So expose an `IBitVirtualKeyboardComposer` interface and a `CandidateBar` RenderFragment slot, comparable to simple-keyboard's `layoutCandidates` ([simple-keyboard interfaces.ts](https://github.com/hodgef/simple-keyboard/blob/master/src/lib/interfaces.ts)).

## Ship about 14 purpose pads and about 25 national layouts

Purpose layouts should be selected from standard signals, not configured by hand. WHATWG defines eight `inputmode` values: `none, text, tel, url, email, numeric, decimal, search`. It describes `numeric` as "useful for PIN entry", `decimal` as showing "the format separator for the locale", and `url` as offering "/" and "." plus strings like "www." or ".com". `enterkeyhint` covers `enter, done, go, next, previous, search, send` ([WHATWG §6.8.9–6.8.10](https://html.spec.whatwg.org/multipage/interaction.html#input-modalities:-the-inputmode-attribute)).

Native platforms add a few more types:
- iOS adds `numberPad` "for PIN entry", `phonePad`, `decimalPad`, `asciiCapableNumberPad` and `webSearch` ([Apple UIKeyboardType](https://developer.apple.com/documentation/uikit/uikeyboardtype)).
- Android adds date/time and number flags for signed and decimal ([InputType.java](https://github.com/aosp-mirror/platform_frameworks_base/blob/main/core/java/android/text/InputType.java)).
- Windows Url and email scopes put `.com` on the keyboard and extra punctuation on long-press of the period: `- + & " : /` for Url, `_ , ;` for email ([Microsoft Learn input scope](https://learn.microsoft.com/en-us/windows/uwp/design/input/use-input-scope-to-change-the-touch-keyboard)).

Only three conventions have formal standards behind them: the ITU-T E.161 phone keypad, the calculator/PC numpad, and the WHATWG modes ([Wikipedia: E.161](https://en.wikipedia.org/wiki/E.161)). The other pads below are bit's own design, and the table marks which is which.

| Id / purpose | Trigger | Rows (base layer) | Basis |
|---|---|---|---|
| `x-phone` (Tel) | `type=tel`, `inputmode=tel` | `1 2(ABC) 3(DEF)` / `4(GHI) 5(JKL) 6(MNO)` / `7(PQRS) 8(TUV) 9(WXYZ)` / `* 0(+) #`, right column `⌫ ␣ hide ↵` as in Qt | E.161 mapping ([Telephone keypad](https://en.wikipedia.org/wiki/Telephone_keypad)). Long-press 0 gives + in HeliBoard and FlorisBoard ([HeliBoard phone.json](https://github.com/Helium314/HeliBoard/blob/main/app/src/main/assets/layouts/phone/phone.json); [FlorisBoard telpad](https://github.com/florisboard/florisboard/blob/main/app/src/main/assets/ime/keyboard/org.florisboard.layouts/layouts/phone/telpad.json)). Qt's dialpad has `*` with alternatives `*+` ([dialpad.qml](https://github.com/qt/qtvirtualkeyboard/blob/dev/src/layouts/fallback/dialpad.qml)). Add a tactile "nub" on 5 (E.161) |
| `x-pin` (Pin, Numeric) | `inputmode=numeric`, PIN/OTP fields | `1 2 3` / `4 5 6` / `7 8 9` / `⌫ 0 ✓` | Phone order. iOS numberPad is described as being for PIN entry. Optional `Scramble` |
| `x-numpad` (Calculator) | explicit | `7 8 9 ×` / `4 5 6 −` / `1 2 3 +` / `0 . =`, optional top `C ± % ÷` | Calculator/PC numpad order ([Numeric keypad](https://en.wikipedia.org/wiki/Numeric_keypad)) |
| `x-numpad-pc` | explicit | `NumLock / * −` / `7 8 9 +(h2)` / `4 5 6` / `1 2 3 Enter(h2)` / `0(w2) .` | The PC 17-key numpad. Exercises `Height`/`Width` |
| `x-decimal` (Decimal) | `inputmode=decimal` | PIN pad with `⌫` replaced by the culture's separator (`.`, `,` or `٫` U+066B) and backspace moved to a fourth column | Separator from `CultureInfo.NumberFormat`. ISIRI puts ٫/٬ on Shift+3/Shift+2 ([ISIRI 9147](https://en.wikipedia.org/wiki/ISIRI_9147)) |
| `x-signed-decimal` | Android `NUMBER_FLAG_SIGNED` | Decimal + `−` (+ optional `e`) | Android number flags |
| `x-pos` (Currency) | explicit | `7 8 9 Qty×` / `4 5 6 Void` / `1 2 3 Clear` / `0 00 000 .` + wide `Total` | No standard (design). Keys emit commands as well as text. MudKeyboard's Pricepad enters pence first ([MudKeyboard](https://github.com/sardar97/mudkeyboard)) |
| `x-hex` (Hex) | explicit | `7 8 9 F` / `4 5 6 E` / `1 2 3 D` / `0 A B C` | Design |
| `x-date`, `x-time` | `type=date/time` (with text input) | Numeric + `/` or `-`; time adds `:` and an AM/PM toggle | Android datetime variation |
| `x-iban`, `x-card` | explicit | IBAN = uppercase A–Z + digits; card = digits only (grouping is a formatter concern) | Design |
| `x-abc` (kiosk / TV) | explicit | `A B C D E F G` / `H I J K L M N` / `O P Q R S T U` / `V W X Y Z ␣ ⌫` | Design. Requires D-pad navigation |
| `x-atm` | explicit | PIN pad + large Cancel / Clear / Enter | The red/yellow/green colour convention is unverified, so offer it as a theme, not a default |
| Email (variation) | `type=email`, `inputmode=email` | Text layout. Comma becomes `@`; bottom row `@ . _ - ␣ .com`. `.com` long-presses to `.net .org .edu` | FlorisBoard/HeliBoard selectors. JavaFX email layout has `@` and `.com` ([Oracle FXVK](https://docs.oracle.com/javase/8/javafx/user-interface-tutorial/embed.htm)) |
| URL (variation) | `type=url`, `inputmode=url` | Comma becomes `/`; adds `: . - _ www. .com https://`; narrow space key | JavaFX URL layout has no Space and offers "www." |
| Search (variation) | `type=search`, `enterkeyhint=search` | Text layout. Enter is labelled and announced "Search" | WHATWG |

National layouts need two forms each. CLDR models each keyboard as one hardware arrangement (`form`: `us`/`iso`/`abnt2`/`jis`/`ks`) with modifier layers, optionally plus touch layers chosen by `minDeviceWidth` ([UTS #35 forms](https://www.unicode.org/reports/tr35/tr35-keyboards.html#element-forms)). So ship a **Full** form with the number row and the AltGr layer, which kiosk and desktop users recognise from hardware. Ship a **Compact** touch form alongside it, derived from the Full form: it drops the number row and moves accents onto long-press (inference, following the CLDR and phone-OS pattern).

Separate ids matter where Microsoft's tables differ from the national standards:
- `fr` (AFNOR, per CLDR fr.xml) versus `fr-t-k0-windows` (legacy AZERTY)
- `fa` (ISIRI 9147) versus `fa-t-k0-windows`

Microsoft's KBDFAR differs from ISIRI on Shift+\` (ZWJ instead of ÷) and on Shift+X (ط instead of U+0653). It emits the four letters "ریال" on Shift+4 where ISIRI uses the single character ﷼, and it has no AltGr layer ([kbdlayout.info KBDFAR](https://kbdlayout.info/KBDFAR/)).

| Tier | Layout ids (proposed) | Dead keys (Windows data) | Notes |
|---|---|---|---|
| P0 | `en` (US), `en-GB`, `fa` (ISIRI), `ar` (Arabic 101), `he`, `de`, `fr-t-k0-windows`, `es`, `ru`, `tr` (Q) | DE ^ ´ \`; FR ^ ¨ ~ \`; ES \` ^ ´ ¨ ~; TR ^ ´ ¨ \` ~; RU none | Covers the RTL trio, Cyrillic, and Turkish casing (ı/İ needs tr-TR case mapping) |
| P1 | `fa-t-k0-windows`, `fr` (AFNOR), `fr-BE`, `de-CH`, `it`, `pt-BR` (ABNT2), `sv`/`fi`, `nb`, `da`, `el`, `ur`, `hi` (InScript), `th` (Kedmanee), `tr-t-k0-f`, `en-t-k0-dvorak`, `en-t-k0-colemak`, `en-t-k0-colemakdh` | CH-DE ´ ^ \` ~ ¨; BR ´ \` ~ ^ ¨; Nordic ´ \` ¨ ^ ~; EL ΄ ΅ ¨; IT none | ABNT2 needs the extra key by right Shift. InScript keys output multi-code-point conjuncts |
| P2 | `ko` (Dubeolsik + Hangul composer), `ja-Hira-t-k0-flicks` (12-key flick), `zh` (QWERTY + host-supplied candidates), `mt` (CLDR sample) | Not applicable | Need the composer hook. Japanese flick needs a tap fallback |

RTL handling follows from the Windows data, and it is subtle. **RTL hardware layouts keep the physical key positions and change only the characters.** So the key grid should not be mirrored: keep `direction: ltr` on the grid and set `dir="rtl"` on the target and preview (inference; no standard covers mirroring a touch grid). Paired punctuation is code-swapped instead. Arabic 101 and Persian put `)` on Shift+9 and `(` on Shift+0, and Hebrew also swaps `[`/`]`, because Unicode bidi mirroring flips the glyphs ([KBDA1](https://kbdlayout.info/KBDA1/); [KBDHEB](https://kbdlayout.info/KBDHEB/)).

Persian needs several things that Arabic layouts do not supply. The letters ی (U+06CC) and ک (U+06A9) must be used, not Arabic ي and ك. Other requirements:
- ZWNJ on Shift+Space and Shift+B
- Persian digits ۰–۹ (U+06F0–06F9)
- the Persian decimal separator ٫ and thousands separator ٬

Keycap rendering needs care as well. Isolate each keycap label (`<bdi>` or `unicode-bidi: isolate`). Draw combining harakat on a ◌ base, since UTS #35 calls a bare nonspacing mark an error ([UTS #35 display](https://www.unicode.org/reports/tr35/tr35-keyboards.html#element-display)). Give ZWNJ/ZWJ/LRM/RLM visible glyphs and spoken names, as Keyman's `*ZWNJ*`/`*RTLM*` labels do. Swap the Enter and Backspace icons for RTL (Keyman `*RTLEnter*`/`*RTLBkSp*`).

Never use simple-keyboard's approach of wrapping the value in RLE/PDF. The `Variations` mechanism can also carry a direction selector for keys whose icon or position flips, as HeliBoard's `layout_direction_selector` does.

## Prioritized features: correctness and accessibility first

The prior art shows the same failures again and again. Mottie's FAQ is the best record of them:
- no caret on `type=number`
- no perfect way to suppress the native keyboard
- lag from mixed `mousedown`/`touchstart` handlers
- modals stealing focus

([Mottie FAQ](https://github.com/Mottie/Keyboard/wiki/FAQ)). The browser facts behind these are fixed. `selectionStart` returns null on anything other than text, search, url, tel and password inputs ([MDN selectionStart](https://developer.mozilla.org/en-US/docs/Web/API/HTMLInputElement/selectionStart)). `readonly` suppression breaks the caret and blocks barcode scanners, which matters for POS ([Wappler thread](https://community.wappler.io/t/prevent-the-on-screen-keyboard-from-showing-on-a-mobile/55868)). `inputmode="none"` exists precisely "for when the page implements its own keyboard input control" ([MDN inputmode](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Global_attributes/inputmode)).

Accessibility is the clearest way to stand out:
- simple-keyboard renders `<div>`s with no ARIA by default ([simple-keyboard options](https://hodgef.com/simple-keyboard/documentation/options/)).
- No W3C pattern exists for a virtual keyboard.
- WCAG 2.2 applies directly. Path-based flicks need single-tap alternatives (2.5.1). Keypad emulation may fire on down-event because it is "considered essential" (2.5.2). Targets must be at least 24×24 px (2.5.8 AA), or 44×44 px for AAA. Long-press and multitap timeouts must be adjustable (2.2.1) ([WCAG 2.2](https://www.w3.org/TR/WCAG22/)).
- Kiosks, ATMs and payment terminals have been in scope of the European Accessibility Act since 28 June 2025 ([Accessible.org](https://accessible.org/eaa-self-service-terminals/)).

| Pri | Feature | Rationale / source |
|---|---|---|
| **P0** | Layout engine: key bag, layers, shorthand rows, `Width/Height/Stretch/Offset/Gap`, `Variations`, `LongPress` (inline + per-char table), actions enum, `NextLayer` | Shared by all mature schemas (section 2) |
| **P0** | Purpose pads: Text, Email, Url, Search, Tel, Pin, Numeric, Decimal, Calculator. Auto-selected from `type`/`inputmode`; Enter label from `enterkeyhint` (Enter, Done, Go, Next, Previous, Search, Send) | WHATWG; Blazorise layout enum |
| **P0** | P0 language tier incl. `fa`, `ar`, `he`. `Culture` drives the decimal separator and case mapping. `DigitSet` | Section 4 |
| **P0** | Two binding modes: inline `Value`/`ValueChanged` (component owns the text) and attach-to-target (`Target` ElementReference/selector or "focused opted-in input") | MudKeyboard inline + host; Avalonia `Target` vs `VirtualKeyboardScope` ([Avalonia](https://docs.avaloniaui.net/controls/input/text-input/virtualkeyboard)) |
| **P0** | JS text insertion: replace the selection at the caret (`setRangeText`), dispatch bubbling `input` (and `change` on commit) so `@bind` updates, append-only fallback for number/email, `MaxLength`, `AllowedChars`/`Pattern` filter | [MDN setRangeText](https://developer.mozilla.org/en-US/docs/Web/API/HTMLInputElement/setRangeText); simple-keyboard `maxLength`/`inputPattern` |
| **P0** | Grapheme-aware Backspace via `Intl.Segmenter` (surrogate pairs, ZWJ emoji, combining marks) | [MDN Intl.Segmenter](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl/Segmenter) |
| **P0** | Focus retention (`pointerdown` preventDefault, keys `tabindex=-1`), `inputmode="none"` set and restored on the target, Pointer Events with per-`pointerId` tracking, `touch-action: manipulation` | Blazorise `KeyTabIndex=-1`; simple-keyboard `preventMouseDownDefault`; Mottie FAQ |
| **P0** | Shift (latched; double-tap locks), Caps, symbols layer, AltGr layer for language layouts; `aria-pressed` with stable labels | GNOME levels; [WAI-ARIA APG button](https://www.w3.org/WAI/ARIA/apg/patterns/button/) |
| **P0** | Key repeat for Backspace, Delete, Space and arrows, with configurable `RepeatDelay`/`RepeatInterval` (defaults about 500 ms and about 50 ms) and an off switch | simple-keyboard hard-codes 500/100 ms with a "TODO configurable" comment; Mottie `repeatDelay` 500, `repeatRate` 20 cps ([Mottie usability](https://github.com/Mottie/Keyboard/wiki/Usability)) |
| **P0** | ARIA: `<button type="button">` keys with localized names, `role="group"` with a layout-named label, polite live region for layer/caps changes, `forced-colors`, `prefers-reduced-motion`, 24 px floor | WCAG 2.2; MudKeyboard |
| **P0** | Display modes Inline / Docked (bottom) / Popup (anchored); `ShowMode` Always / Focus / Manual; scroll target into view above the dock | Blazorise `Placement`, `AutoScroll`; Mottie position |
| **P0** | Theming: CSS variables + `BitVirtualKeyboardClassStyles` parts (`Root, Row, Key, CharacterKey, FunctionKey, ModifierKey, ActiveModifierKey, PrimaryKey, Gap, KeyLabel, KeyHint, Popup, PopupKey, PreviewBubble, Toolbar, CandidateBar, LiveRegion`); `KeyTemplate` RenderFragment | bit convention; Blazorise `KeyTemplate`; Telerik template selector |
| **P0** | Events: `OnKeyPress` (cancellable, can replace text, like Mottie `beforeInsert`), `OnKeyPressed`, `OnLayerChange`, `OnEnter`, `OnAccept`, `OnCancel`, `OnCommand`, `OnOpen/OnClose`; methods `Open/Close/Toggle`, `PressKey`, `SetLayer` | Telerik `KeySending`/`KeySent`; [Mottie methods](https://github.com/Mottie/Keyboard/wiki/Methods) |
| **P1** | Dead keys via `DeadKeys` tables; P1 language tier; Compact touch forms | Windows KLC dead-key inventories |
| **P1** | Long-press popup that is also keyboard- and tap-reachable; multitap; `LongPressDelay` adjustable | WCAG 2.5.1/2.2.1; CLDR gestures |
| **P1** | Size presets: Compact (≥24 px), Touch (44–48 px), Kiosk/Glove (about 15–25 mm per key) | WCAG; Apple 44 pt; Android 48 dp; NN/g 1 cm ([NN/g](https://www.nngroup.com/articles/touch-target-size/)); vendor glove guidance ([CDTech](https://www.cdtech-display.com/knowledges/how-to-optimize-ui-button-size-for-gloved-users-in-industrial-hmis/)) |
| **P1** | Secure mode: `Scramble` None/OnOpen/PerKeystroke using `RandomNumberGenerator`, frame keys fixed, no press highlight or preview, no live-region echo, no plain values in events. Documented as weak against screen capture | [Scrambling keypad paper](https://irma-international.org/article/scrambling-keypad-for-secure-pin-entry-to-defeat-shoulder-surfing-and-inference-attacks/282085/); [Mottie Scramble](https://github.com/Mottie/Keyboard/wiki/Extension-Scramble); [video-buffer attack](https://www.academia.edu/110600056/Analysis_on_Vulnerability_of_Password_Entry_Using_Virtual_Onscreen_Keyboard) |
| **P1** | Physical-keyboard highlight + CapsLock sync (`KeyboardEvent.code`, `getModifierState`), never blocking scanners | simple-keyboard `physicalKeyboardHighlight` |
| **P1** | Value preview with Cancel-reverts, Floating/draggable mode, Disabled/lockout state, inactivity auto-hide, key preview bubble (off for passwords) | MudKeyboard preview; NHTSA lockout ([US DOT](https://www.transportation.gov/briefing-room/us-dot-releases-guidelines-minimize-vehicle-distractions)); SiteKiosk small/full |
| **P1** | Sound (Web Audio after user activation) and haptics as progressive enhancement only | `navigator.vibrate` is unsupported in any Safari and was dropped from Firefox desktop 129 ([caniuse](https://caniuse.com/vibration)) |
| **P1** | Suggestion/candidate bar slot + `IBitVirtualKeyboardComposer`; built-in Hangul composer | simple-keyboard candidates |
| **P2** | Regex `Transforms`/`BackspaceTransforms`, CLDR keyboard3 and Keyman JSON importers, flick gestures, non-rectangular keys (ISO Enter) | UTS #35; KLE |
| **P2** | Switch scanning (row→column, auto/step, speed, dwell) and gamepad/D-pad navigation | [ISO/IEC 22121-3:2025](https://standards.iteh.ai/catalog/standards/iso/5f54a077-f52a-48d6-b87d-ca1dfb0e7d30/iso-iec-22121-3-2025); [Android Switch Access](https://support.google.com/accessibility/android/answer/6301490?hl=en) |
| **P2** | Split / one-handed modes, emoji panel, clipboard and undo keys, swipe gestures, contenteditable targets | Commercial kiosk keyboards ([Comfort OSK](https://www.comfortsoftware.com/on-screen-keyboard.html)); KeyboardKit actions |

Four implementation notes are specific to Blazor:
- **Insert text on the client.** In Blazor Server each event crosses SignalR, so text insertion should run in a small JS module. Only commands and final values should cross the circuit, and for passwords no per-key round-trips should happen at all. This is an architectural inference supported by MudKeyboard's design, which keeps interop to primitive strings ([MudKeyboard](https://github.com/sardar97/mudkeyboard)).
- **Plan the static SSR host.** On static SSR pages a docked host works when rendered with an interactive render mode outside `<Routes>`, the MudKeyboardHost recipe.
- **Integrate with existing bit inputs.** The library already contains `BitOtpInput`, `BitNumberField` and Extras' `BitPhoneInput`. An opt-in parameter or cascading host lets these pick the matching purpose pad automatically, which mirrors the Blazorise input opt-in.
- **Naming.** "Virtual keyboard" is the more common term in this survey: Telerik, Qt, Avalonia, MathLive and the W3C API use it, while only Blazorise and BootstrapBlazor say "on-screen". So **BitVirtualKeyboard** fits best. The docs should state that it is unrelated to the browser's `navigator.virtualKeyboard` API.

## Verified row data for the language catalog

Each row below was extracted from Windows `kbd*.dll` tables via kbdlayout.info KLC downloads, then cross-checked against CLDR and the national standards. Row E is the number row, starting at the \`/~ key. Row D runs Q…P, then `[ ]`, then `\` (scan code 2B). Row C runs A…;'. Row B starts with the ISO extra key (scan code 56) and then runs Z…/.

`x(dead)` marks a dead key, `◌x` a combining mark and `·` no output. `…` means the keys in between match the letters of the base layer. Every row comes from the cited kbdlayout.info page unless another source is named.

**US QWERTY** ([KBDUS](https://kbdlayout.info/KBDUS/))
```
base  E: ` 1 2 3 4 5 6 7 8 9 0 - =    D: q w e r t y u i o p [ ] \    C: a s d f g h j k l ; '    B: (\) z x c v b n m , . /
shift E: ~ ! @ # $ % ^ & * ( ) _ +    D: Q W E R T Y U I O P { } |    C: A S D F G H J K L : "    B: | Z X C V B N M < > ?
```
**UK** ([KBDUK](https://kbdlayout.info/KBDUK/)). Base differs from US at `D…#` and `B: \`. Shift row: `E: ¬ ! " £ $ % ^ & * ( ) _ +`, `D: Q…P { } ~`, `C: A…L : @`, `B: | Z…M < > ?`. AltGr: \`→¦, 4→€, e→é, u→ú, i→í, o→ó, a→á, with Shift+AltGr giving the capitals.

**French AZERTY, legacy Windows** ([KBDFR](https://kbdlayout.info/KBDFR/))
```
base  E: ² & é " ' ( - è _ ç à ) =    D: a z e r t y u i o p ^(dead) $ *    C: q s d f g h j k l m ù    B: < w x c v b n , ; : !
shift E: · 1 2 3 4 5 6 7 8 9 0 ° +    D: A Z E R T Y U I O P ¨(dead) £ µ    C: Q S D F G H J K L M %    B: > W X C V B N ? . / §
altgr E: · · ~(dead) # { [ | `(dead) \ ^ @ ] }    D: e→€, ]→¤
```
On AZERTY the digits need Shift, which matters for numeric-heavy forms.

**French AZERTY, AFNOR "Français normalisé"** ([CLDR fr.xml](https://github.com/unicode-org/cldr/blob/main/keyboards/3.0/fr.xml))
```
none  @ à é è ê ( ) ‘ ’ « » ' ^(dead) / a z e r t y u i o p - + / q s d f g h j k l m / * / < w x c v b n . , : ;
shift # 1 2 3 4 5 6 7 8 9 0 " ¨(dead) / A Z E R T Y U I O P – ± / Q S D F G H J K L M \ ½ / > W X C V B N ? ! … =
```
Its AltGr layer holds many dead keys (breve, acute, grave, macron, caron, dot-above) and "mode" dead keys (Greek, currency) resolved by about 36 CLDR transforms.

**Belgian AZERTY** ([KBDBE](https://kbdlayout.info/KBDBE/))
```
base  E: ² & é " ' ( § è ! ç à ) -    D: a z e r t y u i o p ^(dead) $ µ    C: q s d f g h j k l m ù    B: < w x c v b n , ; : =
shift E: ³ 1 2 3 4 5 6 7 8 9 0 ° _    D: … ¨(dead) * £    C: … M %    B: > W X C V B N ? . / +
altgr E: · | @ # { [ ^ · · { } · ·    D: e→€, [ ], `(dead)    C: ´(dead)    B: \ … ~(dead)
```
**German QWERTZ** ([KBDGR](https://kbdlayout.info/KBDGR/))
```
base  E: ^(dead) 1 2 3 4 5 6 7 8 9 0 ß ´(dead)    D: q w e r t z u i o p ü + #    C: a s d f g h j k l ö ä    B: < y x c v b n m , . -
shift E: ° ! " § $ % & / ( ) = ? `(dead)          D: Q W E R T Z U I O P Ü * '    C: A S D F G H J K L Ö Ä    B: > Y X C V B N M ; : _
altgr E: · · ² ³ · · · { [ ] } \ ·    D: q→@, e→€, +→~    B: <→|, m→µ    (Shift+AltGr+ß → ẞ)
```
**Swiss German** ([KBDSG](https://kbdlayout.info/KBDSG/))
```
base  E: § 1 2 3 4 5 6 7 8 9 0 ' ^(dead)    D: q w e r t z u i o p ü ¨(dead) $    C: a s d f g h j k l ö ä    B: < y x c v b n m , . -
shift E: ° + " * ç % & / ( ) = ? `(dead)    D: … P è ! £    C: … L é à    B: > Y X C V B N M ; : _
altgr E: · ¦ @ # ° § ¬ | ¢ · · ´(dead) ~(dead)    D: e→€ [ ] }    C: {    B: \
```
**Spanish** ([KBDSP](https://kbdlayout.info/KBDSP/))
```
base  E: º 1 2 3 4 5 6 7 8 9 0 ' ¡    D: q … p `(dead) + ç    C: a … l ñ ´(dead)    B: < z x c v b n m , . -
shift E: ª ! " · $ % & / ( ) = ? ¿    D: … ^(dead) * Ç       C: … Ñ ¨(dead)       B: > Z … ; : _
altgr E: \ | @ # ~(dead) € ¬    D: e→€ [ ] }    C: {
```
**Italian** ([KBDIT](https://kbdlayout.info/KBDIT/)). It has no dead keys.
```
base  E: \ 1 2 3 4 5 6 7 8 9 0 ' ì    D: q … p è + ù    C: a … l ò à    B: < z … , . -
shift E: | ! " £ $ % & / ( ) = ? ^    D: … é * §       C: … ç °       B: > … ; : _
altgr e→€, 5→€, è→[, +→], ò→@, à→#;  shift+altgr è→{, +→}
```
**Portuguese (Brazil) ABNT2** ([KBDBR](https://kbdlayout.info/KBDBR/); [CLDR pt-t-k0-abnt2](https://github.com/unicode-org/cldr/blob/main/keyboards/3.0/pt-t-k0-abnt2.xml)). The final `/` sits on the extra key by right Shift.
```
base  E: ' 1 2 3 4 5 6 7 8 9 0 - =    D: q … p ´(dead) [    C: a … l ç ~(dead) ]    B: \ z x c v b n m , . ; /
shift E: " ! @ # $ % ¨(dead) & * ( ) _ +    D: … `(dead) {    C: … Ç ^(dead) }    B: | Z … < > : ?
altgr E: ¹ ² ³ £ ¢ ¬ … §    D: q→/, w→?, e→°, [→ª    C: ]→º    B: c→₢
```
**Nordic** ([KBDSW](https://kbdlayout.info/KBDSW/), [KBDNO](https://kbdlayout.info/KBDNO/), [KBDDA](https://kbdlayout.info/KBDDA/))
```
sv/fi base  E: § 1 2 3 4 5 6 7 8 9 0 + ´(dead)    D: q … p å ¨(dead) '    C: a … l ö ä    B: < z … , . -
sv/fi shift E: ½ ! " # ¤ % & / ( ) = ? `(dead)    D: … Å ^(dead) *       C: … Ö Ä       B: > … ; : _
sv/fi altgr 2→@ 3→£ 4→$ 5→€ 7→{ 8→[ 9→] 0→} +→\ ¨→~(dead) <→| m→µ
nb          E: | 1…0 + \    D: … å ¨(dead) '    C: … ø æ
da          E: ½ 1…0 + ´(dead)    C: … æ ø
```
**Turkish Q** ([KBDTUQ](https://kbdlayout.info/KBDTUQ/)) and **Turkish F** ([KBDTUF](https://kbdlayout.info/KBDTUF/))
```
tr-Q base  E: " 1 2 3 4 5 6 7 8 9 0 * -    D: q w e r t y u ı o p ğ ü ,    C: a s d f g h j k l ş i    B: < z x c v b n m ö ç .
tr-Q shift E: é ! ' ^(dead) + % & / ( ) = ? _    D: Q W E R T Y U I O P Ğ Ü ;    C: A … L Ş İ    B: > Z … M Ö Ç :
tr-Q altgr q→@ e→€ t→₺ ı→i a→æ s→ß …
tr-F base  E: + 1 2 3 4 5 6 7 8 9 0 / -    D: f g ğ ı o d r n h p q w x    C: u i e a ü t k m l y ş    B: < j ö v c ç z s b . ,
tr-F shift E: * ! " ^(dead) $ % & ' ( ) = ? _    D: F G Ğ I O D R N H P Q W X    C: U İ E A Ü T K M L Y Ş    B: > J Ö V C Ç Z S B : ;
```
**Russian JCUKEN** ([KBDRU](https://kbdlayout.info/KBDRU/)). AltGr+8 gives ₽.
```
base  E: ё 1 2 3 4 5 6 7 8 9 0 - =    D: й ц у к е н г ш щ з х ъ \    C: ф ы в а п р о л д ж э    B: \ я ч с м и т ь б ю .
shift E: Ё ! " № ; % : ? * ( ) _ +    D: Й Ц У К Е Н Г Ш Щ З Х Ъ /    C: Ф Ы В А П Р О Л Д Ж Э    B: / Я Ч С М И Т Ь Б Ю ,
```
**Greek** ([KBDHE](https://kbdlayout.info/KBDHE/))
```
base  E: ` 1 … 0 - =    D: ; ς ε ρ τ υ θ ι ο π [ ] \    C: α σ δ φ γ η ξ κ λ ΄(dead) '    B: < ζ χ ψ ω β ν μ , . /
shift E: ~ ! @ # $ % ^ & * ( ) _ +    D: : ΅(dead) Ε Ρ Τ Υ Θ Ι Ο Π { } |    C: Α Σ Δ Φ Γ Η Ξ Κ Λ ¨(dead) "    B: > Ζ Χ Ψ Ω Β Ν Μ < > ?
altgr E: · · ² ³ £ § ¶ · ¤ ¦ ° ± ½    D: e→€ r→® y→¥ [→« ]→» \→¬    B: c→©
```
**Hebrew, Microsoft** ([KBDHEB](https://kbdlayout.info/KBDHEB/)). Shift gives Latin capitals, and brackets and parentheses are bidi-swapped. Ctrl+[ and Ctrl+] give LRM and RLM. AltGr: 4→₪, e→€, u→װ, h→ײ, j→ױ, -→ֿ.
```
base  E: ; 1 2 3 4 5 6 7 8 9 0 - =    D: / ' ק ר א ט ו ן ם פ ] [ \    C: ש ד ג כ ע י ח ל ך ף ,    B: \ ז ס ב ה נ מ צ ת ץ .
shift E: ~ ! @ # $ % ^ & * ) ( _ +    D: Q W E R T Y U I O P } { |    C: A … L : "    B: | Z X C V B N M > < ?
```
**Arabic 101** ([KBDA1](https://kbdlayout.info/KBDA1/)). The B-row key لا is two code points. Digits are European, and Ctrl+Shift+1..4 give ZWJ, ZWNJ, LRM and RLM.
```
base  E: ذ 1 2 3 4 5 6 7 8 9 0 - =    D: ض ص ث ق ف غ ع ه خ ح ج د \    C: ش س ي ب ل ا ت ن م ك ط    B: \ ئ ء ؤ ر لا ى ة و ز ظ
shift E: ◌ّ ! @ # $ % ^ & * ) ( _ +    D: ◌َ ◌ً ◌ُ ◌ٌ لإ إ ‘ ÷ × ؛ < > |    C: ◌ِ ◌ٍ ] [ لأ أ ـ ، / : "    B: | ~ ◌ْ } { لآ آ ’ , . ؟
```
**Persian ISIRI 9147** ([Wikipedia: ISIRI 9147](https://en.wikipedia.org/wiki/ISIRI_9147)). Shift+Space and Shift+B give ZWNJ, and AltGr+Space gives NBSP. AltGr+\ gives U+2010.
```
base  E: ZWJ ۱ ۲ ۳ ۴ ۵ ۶ ۷ ۸ ۹ ۰ - =    D: ض ص ث ق ف غ ع ه خ ح ج چ    C: ش س ی ب ل ا ت ن م ک گ    B: ظ ط ز ر ذ د پ و . /
shift E: ÷ ! ٬ ٫ ﷼ ٪ × ، * ) ( ـ +    D: ◌ْ ◌ٌ ◌ٍ ◌ً ◌ُ ◌ِ ◌َ ◌ّ ] [ } {    C: ؤ ئ ي إ أ آ ة » « : ؛    B: ك ◌ٓ ژ ◌ٰ ZWNJ ◌ٔ ء > < ؟
altgr E: ~ ` @ # $ % ^ & • LRM RLM _ −    D: ° – € – – LRO RLO PDF LRE RLE    C: – – ى – – ٱ – ﴾ ﴿ ; "    B: – – – ◌ٖ ZWJ ◌ٕ … , ' ?
```
**Persian, Microsoft** (`fa-t-k0-windows`, [KBDFAR](https://kbdlayout.info/KBDFAR/)) is ISIRI with these differences: Shift+\` gives ZWJ, Shift+X gives ط, Shift+4 gives "ریال", there is no AltGr layer, and Ctrl+Shift+1..4 give ZWJ, ZWNJ, LRM and RLM.

**Urdu** ([KBDURDU](https://kbdlayout.info/KBDURDU/)). The Ctrl+Shift layer holds the harakat and أ إ.
```
base  E: ` 1 … 0 - =    D: ط ص ھ د ٹ پ ت ب ج ح ] [ \    C: م و ر ن ل ہ ا ک ی ؛ '    B: \ ق ف ے س ش غ ع ، ۔ /
shift E: ~ ! @ # $ ٪ ^ ۖ ٭ ) ( _ +    D: ظ ض ذ ڈ ث ◌ّ ۃ ـ چ خ } { |    C: ژ ز ڑ ں ۂ ء آ گ ي : "    B: | ZWJ ZWNJ ۓ LRM ؤ ئ RLM > < ؟
```
**Hindi InScript** ([KBDINHIN](https://kbdlayout.info/KBDINHIN/)). AltGr on row E gives Devanagari digits, and Ctrl+Shift+4 gives ₹.
```
base  E: · 1 2 3 4 5 6 7 8 9 0 - ृ    D: ौ ै ा ी ू ब ह ग द ज ड ़ ॉ    C: ो े ् ि ु प र क त च ट    B: ॉ · ं म न व ल स , . य
shift E: · ऍ ॅ ्र र् ज्ञ त्र क्ष श्र ( ) ः ऋ    D: औ ऐ आ ई ऊ भ ङ घ ध झ ढ ञ ऑ    C: ओ ए अ इ उ फ ऱ ख थ छ ठ    B: ऑ · ँ ण · · ळ श ष । य़
```
**Thai Kedmanee** ([KBDTH0](https://kbdlayout.info/KBDTH0/))
```
base  E: _ ๅ / - ภ ถ ุ ึ ค ต จ ข ช    D: ๆ ไ ำ พ ะ ั ี ร น ย บ ล ฃ    C: ฟ ห ก ด เ ้ ่ า ส ว ง    B: ฃ ผ ป แ อ ิ ื ท ม ใ ฝ
shift E: % + ๑ ๒ ๓ ๔ ู ฿ ๕ ๖ ๗ ๘ ๙    D: ๐ " ฎ ฑ ธ ํ ๊ ณ ฯ ญ ฐ , ฅ    C: ฤ ฆ ฏ โ ฌ ็ ๋ ษ ศ ซ .    B: ฅ ( ) ฉ ฮ ฺ ์ ? ฒ ฬ ฦ
```
**Dvorak** ([KBDDV](https://kbdlayout.info/KBDDV/)), **Colemak** and **Colemak-DH** ([xkeyboard-config symbols/us](https://gitlab.freedesktop.org/xkeyboard-config/xkeyboard-config/-/blob/master/symbols/us)). Shift layers are the uppercase letters plus US symbols, and Dvorak's shift D row is `" < > P Y F G C R L ? + |`.
```
dvorak   E: ` 1 … 0 [ ]    D: ' , . p y f g c r l / = \    C: a o e u i d h t n s -    B: ; q j k x b m w v z
colemak  D: q w f p g j l u y ; [ ]    C: a r s t d h n e i o '    B: z x c v b k m , . /
colemakdh D: q w f p b j l u y ; [ ]   C: a r s t g m n e i o '    B: x c d v z k h , . /   (ISO: z also on the <> key)
```
**Korean Dubeolsik** ([Wikipedia: Dubeolsik](https://en.wikipedia.org/wiki/Keyboard_layout#Dubeolsik)). Shift gives ㅃ ㅉ ㄸ ㄲ ㅆ on Q–T and ㅒ ㅖ on O and P. Jamo compose algorithmically into syllables.
```
Q-row: ㅂ ㅈ ㄷ ㄱ ㅅ ㅛ ㅕ ㅑ ㅐ ㅔ    A-row: ㅁ ㄴ ㅇ ㄹ ㅎ ㅗ ㅓ ㅏ ㅣ    Z-row: ㅋ ㅌ ㅊ ㅍ ㅠ ㅜ ㅡ
```
**Japanese 12-key flick** ([CLDR ja-Hira-t-k0-flicks](https://github.com/unicode-org/cldr/blob/main/keyboards/3.0/ja-Hira-t-k0-flicks.xml)). Base rows: `あ か さ` / `た な は` / `ま や ら` / `sym わ 。 num` / `space`. The num layer is `1 2 3` / `4 5 6` / `7 8 9` / `, 0 .`. On each key, a tap gives the key's own kana; flicking west, east, south-west and south gives the i, u, e and o columns (あ: い う え お). Common phone UIs place these differently, so make the flick map data, not code.

Several gaps remain unverified:
- Hebrew SI-1452 punctuation differences from Microsoft's layout
- Arabic 102 and Maghreb AZERTY-Arabic variants
- JIS kana-on-key assignments
- the official ISIRI and KS X 5002 texts, which were not read directly

Treat these as follow-up checks before the P1 and P2 tiers ship.

## Conclusion

The survey changes what the hard part looks like. Drawing keys is easy. Most of the work in a virtual keyboard is **data plus text-entry plumbing**. The layout half is largely solved prior art: CLDR keyboard3 supplies the semantics, Keyman and GNOME the touch geometry, HeliBoard the selectors, and kbdlayout.info a machine-readable source for every Windows layout. bit can therefore offer a broader and more correct language catalog than any Blazor competitor, and do it largely by generation rather than by hand. The plumbing half has no off-the-shelf solution: caret-aware insertion, grapheme deletion, focus retention, `@bind` event semantics and OS keyboard suppression. It is where existing libraries keep failing, and it deserves the most engineering and test effort, especially in WebKit.

Two positions follow from this. First, **RTL should be a launch feature, not a later add-on**. Persian, Arabic and Hebrew appear in every multi-language JS library. Yet no .NET keyboard found documents them, and the rules involved trip up naive implementations:
- the grid is not mirrored
- paired punctuation is code-swapped
- Persian ی/ک are not Arabic ي/ك
- ZWNJ is a first-class key

bit already supports `BitDir`, which makes this a natural place to lead. Second, **accessibility and security trade off against each other and should be configured, not hard-coded**. Scrambled keypads defeat shoulder-surfing but break muscle memory and screen-reader spatial learning. Shipping scramble off by default, with documented limits, keeps BitVirtualKeyboard usable in the EAA-regulated kiosk and terminal settings that are its most likely market.
