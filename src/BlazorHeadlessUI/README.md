# Bit.BlazorHeadlessUI

**Accessible, unstyled UI primitives for Blazor — with accessibility testing built in.**

Bit.BlazorHeadlessUI gives you the hard parts of interactive UI — keyboard interaction, focus management, ARIA semantics, positioning, dismissal, announcements — as plain Razor components that render semantic HTML and nothing else. You bring the styles: plain CSS, Tailwind, Bootstrap, Fluent tokens or your own design system.

It ships with **Bit.BlazorHeadlessUI.Testing**, a bUnit-based accessibility toolkit (WAI-ARIA rule engine, accessible-name and focus-order inspection, keyboard helpers, live-region assertions) used to test every component of the library — and usable for testing *your* components too.

Inspired by [Radix UI](https://www.radix-ui.com/), [React Aria](https://react-spectrum.adobe.com/react-aria/), [Headless UI](https://headlessui.com/), [Ark UI](https://ark-ui.com/) and [cmdk](https://cmdk.paco.me/), implemented natively for Blazor (Server, WebAssembly, Auto and static SSR prerendering).

---

## Contents

- [Why Bit.BlazorHeadlessUI](#why-bitblazorheadlessui)
- [Installation](#installation)
- [Components](#components)
- [Concepts](#concepts)
- [Styling](#styling)
- [Testing accessibility](#testing-accessibility)
- [Demo](#demo)
- [Contributing](#contributing)

## Why Bit.BlazorHeadlessUI

- **Zero visual opinion.** No CSS ships with the library. Components render semantic elements and expose their state through `data-*` and ARIA attributes.
- **Accessibility is the feature.** Every widget follows the [WAI-ARIA Authoring Practices](https://www.w3.org/WAI/ARIA/apg/): roles, states, properties, keyboard interaction, focus management, typeahead and screen-reader announcements.
- **Composable.** Compound components (`BitBhMenu` → `BitBhMenuButton` + `BitBhMenuItems` + `BitBhMenuItem`) share state through cascading contexts, so you control the markup and nesting.
- **Controlled or uncontrolled.** Every stateful component works with `@bind-…`, with `Default…` initial values, or with change callbacks (`On…Change`).
- **Forms built in.** `EditForm` validation, `BitBhField` label / description / error wiring, hidden inputs for plain form posts.
- **Polymorphic.** Render any element with `As="…"`; `class`, `style` and any attribute flow through.
- **Robust overlays.** Layered dismissal (Escape, outside click, focus out) that respects nesting, focus scopes with restore, `inert` / `aria-hidden` for modals, scroll locking, portals that keep the component tree intact, and exit animations.
- **Positioning engine included.** Anchor panels with flip, shift, size and arrow support — no Popper / Floating UI dependency.
- **Large data.** Option, item and tree collections use stable ids (never indexes) and support virtualization.
- **Tested.** Every component is covered by keyboard and ARIA tests, plus automated accessibility audits.

## Installation

```bash
dotnet add package Bit.BlazorHeadlessUI
```

Register the services (recommended — components fall back to sensible defaults without it):

```csharp
// Program.cs (server and, for WebAssembly / Auto, the client project too)
builder.Services.AddBitBlazorHeadlessUIServices();
```

Import the namespace:

```razor
@* _Imports.razor *@
@using Bit.BlazorHeadlessUI
```

That's it. The small JavaScript runtime (`bit-blazorheadlessui.js`) is loaded automatically through a Razor class library initializer; there is no script tag to add. Components must run in an **interactive** render mode to respond to input; during prerendering they render their initial, accessible markup.

Targets .NET 8, 9 and 10.

## Components

| Area | Components |
| --- | --- |
| **Overlays** | `BitBhDialog`, `BitBhAlertDialog`, `BitBhPopover`, `BitBhTooltip` (+ `BitBhTooltipProvider`), `BitBhHoverCard`, `BitBhPortal` |
| **Menus** | `BitBhMenu` (submenus, checkbox / radio items, groups, labels), `BitBhMenubar`, `BitBhContextMenu` |
| **Selection** | `BitBhListbox` (single / multi, typeahead, virtualized), `BitBhCombobox` (autocomplete, multi, virtualized), `BitBhCommand` / `BitBhCommandDialog` (command palette with fuzzy ranking and nested pages) |
| **Hierarchies & data** | `BitBhTree` (single / multi / tri-state checkboxes, lazy loading), `BitBhGrid` (data grid and treegrid with cell navigation and row selection), `BitBhSortable` (pointer and keyboard reordering with announcements) |
| **Disclosure** | `BitBhTabGroup`, `BitBhAccordion`, `BitBhDisclosure`, `BitBhCollapsible` |
| **Form controls** | `BitBhField` (`BitBhLabel`, `BitBhDescription`, `BitBhErrorMessage`, `BitBhInput`, `BitBhSelect`, `BitBhTextarea`), `BitBhFieldset`, `BitBhCheckbox` (+ `BitBhCheckboxGroup`, select-all), `BitBhSwitch`, `BitBhRadioGroup`, `BitBhSlider` (range, vertical), `BitBhToggle`, `BitBhToggleGroup` |
| **Layout & feedback** | `BitBhToolbar`, `BitBhSeparator`, `BitBhProgress`, `BitBhMeter`, `BitBhToaster` / `BitBhToastViewport`, `BitBhAvatar`, `BitBhAspectRatio`, `BitBhButton`, `BitBhCloseButton` |
| **Accessibility utilities** | `BitBhFocusScope` / `BitBhFocusTrap`, `BitBhRovingFocusGroup`, `BitBhVisuallyHidden`, `BitBhLiveRegion`, `IBitBhAnnouncer`, `BitBhHotkey`, `BitBhDirectionProvider`, `BitBhDataInteractive`, `BitBhTransition` |

### A taste

```razor
<BitBhMenu>
    <BitBhMenuButton class="btn">Options</BitBhMenuButton>
    <BitBhMenuItems class="menu" Anchor="@("bottom start")">
        <BitBhMenuItem OnClick="Edit">Edit</BitBhMenuItem>
        <BitBhMenuItem OnClick="Duplicate" Disabled="true">Duplicate</BitBhMenuItem>
        <BitBhMenuSeparator />
        <BitBhMenuSub>
            <BitBhMenuSubTrigger>Share</BitBhMenuSubTrigger>
            <BitBhMenuSubContent class="menu">
                <BitBhMenuItem OnClick="CopyLink">Copy link</BitBhMenuItem>
            </BitBhMenuSubContent>
        </BitBhMenuSub>
    </BitBhMenuItems>
</BitBhMenu>
```

```razor
<EditForm Model="model" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <BitBhField>
        <BitBhLabel>Country</BitBhLabel>
        <BitBhCombobox TValue="string" @bind-Value="model.Country" OnQueryChange="q => query = q">
            <BitBhComboboxInput TValue="string" class="input" />
            <BitBhComboboxOptions TValue="string" class="options" Anchor="@("bottom start")">
                @foreach (var c in Countries.Where(c => BitBhFilter.Contains(c, query)))
                {
                    <BitBhComboboxOption TValue="string" Value="@c">@c</BitBhComboboxOption>
                }
                <BitBhComboboxEmpty TValue="string">No matches</BitBhComboboxEmpty>
            </BitBhComboboxOptions>
        </BitBhCombobox>
        <BitBhErrorMessage class="error" />
    </BitBhField>
</EditForm>
```

```razor
<BitBhCommandDialog @bind-Open="paletteOpen" Shortcut="mod+k" Label="Command palette">
    <BitBhCommandInput placeholder="Type a command…" />
    <BitBhCommandList>
        <BitBhCommandEmpty>No results.</BitBhCommandEmpty>
        <BitBhCommandGroup Heading="Navigation">
            <BitBhCommandItem OnSelect="@(() => Nav.NavigateTo("/settings"))" Keywords="@(["preferences"])">Settings</BitBhCommandItem>
        </BitBhCommandGroup>
    </BitBhCommandList>
</BitBhCommandDialog>
```

## Concepts

### Compound components and state

Parent components own state and cascade it to their parts. Every stateful part supports three modes:

```razor
<BitBhTabGroup @bind-Value="tab">…</BitBhTabGroup>                  @* controlled *@
<BitBhTabGroup DefaultValue="profile">…</BitBhTabGroup>             @* uncontrolled *@
<BitBhTabGroup OnValueChange="v => Log(v)">…</BitBhTabGroup>        @* uncontrolled + notified *@
```

### Rendering state in content: `Template`

`ChildContent` is always a plain `RenderFragment`, so components nest freely inside `EditForm`, panels and each other. When markup depends on a component's state, use its `Template` instead:

```razor
<BitBhDisclosureButton>
    <Template Context="d">@(d.IsOpen ? "Hide" : "Show") details</Template>
</BitBhDisclosureButton>
```

Most of the time CSS is enough: style `[data-state="open"]`, `[aria-expanded="true"]`, `[data-active]` … directly.

### Common parameters

Every component derives from `BitBhComponentBase`:

| Parameter | Purpose |
| --- | --- |
| `As` | Render a different element (`As="a"`, `As="li"`, `As="table"` …) |
| `Id` | Explicit id; generated otherwise. ARIA references (`aria-controls`, `aria-labelledby` …) are wired for you |
| `Ref` | `Action<ElementReference>` to get the element |
| any attribute | `class`, `style`, `data-*`, `aria-*` … are forwarded; `class` / `style` merge with internal values, and so does `aria-describedby` (your description and the one a part wires in are both kept) |

Event handlers you put on a part run too: `<BitBhTab @onclick="Track">` calls `Track` and then switches the tab.

### Positioning

Floating parts (`BitBhMenuItems`, `BitBhListboxOptions`, `BitBhComboboxOptions`, `BitBhPopoverPanel`, `BitBhTooltipContent`, `BitBhHoverCardContent`, submenus …) take an `Anchor`:

```razor
<BitBhPopoverPanel Anchor="@(new BitBhAnchorOptions { To = "bottom end", Gap = 6, MatchReferenceWidth = true })" />
<BitBhMenuItems Anchor="@("right start")" />   @* a placement string converts implicitly *@
```

Placements: `top | right | bottom | left`, optionally with `start` / `end`. Options: `Gap`, `Offset`, `Padding`, `Flip`, `Shift`, `MatchReferenceWidth`, `HideWhenDetached`, `ArrowPadding`. Panels receive `data-anchor="bottom start"` (the resolved side) and the CSS variables `--bit-bh-anchor-width`, `--bit-bh-anchor-height`, `--bit-bh-available-width`, `--bit-bh-available-height`, `--bit-bh-transform-origin`. Arrows (`BitBhPopoverArrow`, `BitBhTooltipArrow`, `BitBhMenuArrow`) are positioned automatically.

### Overlays, dismissal and focus

- Escape and outside interaction dismiss the **topmost** layer only; `OnDismissRequest` lets you cancel (`r.Cancel = true`). A cancelled request leaves the layer dismissable by the next press.
- A surface closes with the surface it was opened from (a submenu with its menu, a popover with the dialog it is in); its `OnDismissRequest` sees `BitBhDismissReason.ParentClosed`, which cannot be cancelled.
- Modals (`BitBhDialog`, `BitBhAlertDialog`, `Modal` popovers) trap focus, make the rest of the page `inert`, lock scrolling and restore focus on close.
- `BitBhPortal` moves the DOM node but keeps the component tree, so cascading values and events keep working.
- Exit animations: closing parts stay mounted until their CSS animations / transitions finish (`data-state="closed"`).

### Announcements

`IBitBhAnnouncer` speaks through a shared polite / assertive live region. Components use it for result counts (command palette), toasts and drag-and-drop steps; you can inject it too:

```razor
@inject IBitBhAnnouncer Announcer
<button @onclick='() => Announcer.AnnounceAsync("Saved")'>Save</button>
```

### Right-to-left

Wrap content in `<BitBhDirectionProvider Dir="BitBhDirection.Rtl">` (or set `Dir` per component) and horizontal arrow keys mirror.

## Styling

Every component exposes state as attributes. The most common hooks:

```css
[data-state="open"], [data-state="closed"]            /* disclosure, menus, popovers, dialogs */
[data-state="checked"], [data-state="indeterminate"]  /* checkbox, switch, radio */
[data-state="active"]                                 /* selected tab */
[data-state="on"]                                     /* pressed toggle */
[data-active], [data-highlighted]                     /* keyboard / pointer highlight in menus, listboxes */
[data-selected], [data-disabled], [data-invalid]
[data-orientation="vertical"], [data-anchor^="top"]
[aria-expanded="true"], [aria-selected="true"], [aria-pressed="true"]
```

**Plain CSS**

```css
.menu-item[data-highlighted] { background: #eff6ff; }
.dialog[data-state="closed"] { animation: fade-out 150ms ease-in; }
```

**Tailwind v4** — arbitrary data / aria variants work out of the box:

```razor
<BitBhMenuItem class="rounded px-2 py-1 data-highlighted:bg-blue-50 data-disabled:opacity-50">Edit</BitBhMenuItem>
<BitBhTab class="border-b-2 border-transparent data-[state=active]:border-blue-600">Profile</BitBhTab>
```

**Bootstrap** — reuse its classes and map states:

```razor
<BitBhMenuItems class="dropdown-menu show">
    <BitBhMenuItem class="dropdown-item">Edit</BitBhMenuItem>
</BitBhMenuItems>
```
```css
.dropdown-item[data-highlighted] { background-color: var(--bs-dropdown-link-hover-bg); }
```

**Design tokens (Fluent or your own)** — headless components consume whatever custom properties your theme defines:

```css
.menu { background: var(--colorNeutralBackground1); border-radius: var(--borderRadiusMedium); }
```

> If you give a hideable part an explicit `display` (e.g. `display: flex`), keep `[hidden] { display: none }` working: `.panel[hidden] { display: none; }`, or in Tailwind `@layer base { [hidden] { display: none !important; } }`.

## Testing accessibility

```bash
dotnet add package Bit.BlazorHeadlessUI.Testing
```

```csharp
[TestClass]
public class SaveMenuTests : BitBhTestContext
{
    [TestMethod]
    public void Menu_is_accessible_and_keyboard_operable()
    {
        var cut = Render<SaveMenu>();               // .razor test files can use Render(@<SaveMenu />)
        cut.Find("button").Press("ArrowDown");

        var menu = cut.Find("[role=menu]");
        Assert.AreEqual("Save as…", cut.ActiveDescendant(menu)!.AccessibleName());
        cut.ShouldBeAccessible();                   // WAI-ARIA rule engine
        Assert.ContainsSingle(cut.TabSequence());   // one tab stop
    }
}
```

`BitBhTestContext` emulates the browser behaviour the components rely on (document order and text of items, so typeahead and filtering work like in a browser) and records announcements (`await Announcer.WaitForAsync("3 results")`). The audit (`ShouldBeAccessible`, `Audit`) checks roles, required states, allowed attributes, ID references, required owned elements / context, accessible names, focusable content inside `aria-hidden`, nested interactive controls, positive `tabindex`, presentational conflicts, `aria-activedescendant` targets and form labels, and can render Markdown / JSON reports for CI. bUnit is framework-neutral, so the toolkit works with MSTest, xUnit and NUnit alike. See [the Testing README](Bit.BlazorHeadlessUI.Testing/README.md).

The audit complements — it does not replace — browser-based checks (e.g. axe-core with Playwright) and testing with real assistive technology.

## Demo

https://blazorheadlessui.bitplatform.dev

```bash
dotnet run --project src/BlazorHeadlessUI/Demo/Bit.BlazorHeadlessUI.Demo.Server
```

Every component is demonstrated twice - hand-written CSS at `/<component>` and Tailwind at `/tw/<component>` - with the Razor and CSS for each demo one click away. Debug builds run the demo as Blazor Server; Release builds (and `-p:IncludeWasm=true`) run it as Blazor WebAssembly. The demo also hosts an MCP server at `/mcp` that hands an AI agent the components, their parameters, styling hooks and demo examples.

```
Bit.BlazorHeadlessUI/                     the library (Components, Core, Scripts/*.ts compiled to wwwroot)
Bit.BlazorHeadlessUI.Testing/             accessibility test toolkit (bUnit)
Demo/Bit.BlazorHeadlessUI.Demo.Server/    demo host and MCP server
Demo/Client/Bit.BlazorHeadlessUI.Demo.Client.Core/   demo pages, layout and styles (CSS + Tailwind)
Demo/Client/Bit.BlazorHeadlessUI.Demo.Client.Web/    demo WebAssembly client
Tests/Bit.BlazorHeadlessUI.Tests/         component, keyboard, ARIA and engine tests (MSTest + bUnit)
Tests/Bit.BlazorHeadlessUI.Tests.Mcp/     MCP server tests
Tests/Bit.BlazorHeadlessUI.Tests.E2E/     real-browser tests: axe-core + keyboard (Playwright)
```

## Contributing

From the `src` folder (its `global.json` selects the test runner):

```bash
dotnet build BlazorHeadlessUI/Bit.BlazorHeadlessUI.slnx
cd BlazorHeadlessUI/Tests/Bit.BlazorHeadlessUI.Tests && dotnet test     # component, keyboard and ARIA tests (bUnit)
```

Debug builds target .NET 10 only; pass `-p:BuildAllTfms=true` to build and test .NET 8 and 9 as well, as CI does. The browser suite (axe-core on every demo page + keyboard flows) is described in [Tests/Bit.BlazorHeadlessUI.Tests.E2E/README.md](Tests/Bit.BlazorHeadlessUI.Tests.E2E/README.md) and runs in `bit.ci.BlazorHeadlessUI.e2e.yml`.

New components should follow the APG pattern for their role, cover keyboard interaction and ARIA in tests (`ShouldBeAccessible()` on every state), and come with CSS and Tailwind demos.

## License

[MIT](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE)
