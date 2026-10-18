# Bit.Balette

An accessible, keyboard-first command palette for Blazor: the `Ctrl+K` / `⌘K` box that searches every page,
action and setting of an app and runs the one you pick. Matching, ranking, paging and history are written in C#;
JavaScript only opens the native `<dialog>`, moves focus and listens for keys - there is no JavaScript command-menu
library underneath.

## Quick start

```bash
dotnet add package Bit.Balette
```

```csharp
// Program.cs - in both projects of a Blazor Web App
builder.Services.AddBitBaletteServices();
```

```html
<!-- App.razor or index.html -->
<link rel="stylesheet" href="_content/Bit.Balette/bit-balette.css" />
```

```razor
@* _Imports.razor *@
@using Bit.Balette
```

```razor
@* MainLayout.razor - an interactively rendered layout *@
@inject IBaletteService Palette

<BaletteTrigger Label="Search…" />
@Body
<Balette />

@code {
    protected override void OnInitialized() => Palette.Register(
        BaletteCmd.Navigate("Home", "/").Group("Navigation").Shortcut("G H"),
        BaletteCmd.Navigate("Settings", "/settings").Group("Navigation").Shortcut("G S").Keywords("preferences"),
        BaletteCmd.New("Theme").Group("Preferences").Children(
            BaletteCmd.Action("Light", () => SetTheme("light")),
            BaletteCmd.Action("Dark", () => SetTheme("dark"))));

    private void SetTheme(string theme) { /* ... */ }
}
```

`Register` returns an `IDisposable` - dispose it to remove page-scoped commands, or use
`<BaletteCommands Commands="..." />`. Commands can also be written as markup with `<BaletteGroup>` and
`<BaletteItem>` inside `<Balette>`.

## Features

- An fzf-style fuzzy matcher with word-boundary, camelCase and consecutive-run bonuses, smart case and highlighting.
- Multi-term queries with `'exact`, `^prefix`, `suffix$` and `!negate` operators; weighted fields; priority and
  frecency ranking.
- Nested pages - static, lazily loaded, declarative or returned by a handler - with breadcrumbs.
- Prefix modes (`>`, `#`, `@`, `?`) and debounced, cancellable async sources.
- Prompts, multi-select, confirmation of destructive commands, toasts and a `Tab` action panel.
- Recents and pins persisted to `localStorage` or any store you plug in.
- Global shortcuts and sequences such as `G I`, matched in the browser, with platform-aware hints.
- A native modal `<dialog>` with the WAI-ARIA combobox pattern and live announcements.
- CSS-variable theming, light and dark, eight templates or a fully unstyled mode, localizable strings.
- Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 8, 9 and 10; trimming and AOT
  friendly.

## For AI agents

The demo site hosts an MCP server at `https://balette.bitplatform.dev/mcp` (also served through
`https://bitplatform.dev/mcp`) that answers from the shipped library: its API, its guide and the demo's working
pages.

## Documentation

The full guide - commands, declarative markup, handlers and results, nested pages, search and ranking, modes, async
sources, history, the keyboard, theming, templates, localization, accessibility and hosting models - is in the
[Balette README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Balette). See it running at
[balette.bitplatform.dev](https://balette.bitplatform.dev).
