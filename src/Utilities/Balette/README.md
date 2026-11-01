# Bit.Balette

An accessible, keyboard-first command palette for Blazor - the `Ctrl+K` / `⌘K` box that searches every page,
action and setting of an app and runs the one you pick.

Matching, ranking, paging, history and every other decision are written in C#. JavaScript does only what the
browser alone can do: open the native `<dialog>`, move focus into and out of it, scroll the active row into view
and listen for keys. There is no JavaScript command-menu library underneath.

```razor
<Balette />
<BaletteTrigger Label="Search or run a command…" />

@code {
    [Inject] private IBaletteService Palette { get; set; } = default!;

    protected override void OnInitialized()
    {
        Palette.Register(
            BaletteCmd.Navigate("Home", "/").Group("Navigation").Shortcut("G H"),
            BaletteCmd.Navigate("Settings", "/settings").Group("Navigation").Shortcut("G S").Keywords("preferences"),
            BaletteCmd.Action("Toggle sidebar", () => _sidebarOpen = !_sidebarOpen).Shortcut("Mod+B"));
    }

    private bool _sidebarOpen;
}
```

## What it is for

The one place in an application where everything can be reached by typing: navigation, actions, settings,
records fetched from an API, multi-step flows like "rename", "assign to" or "set labels", and the keyboard
shortcuts that go with them. It is the pattern of VS Code's Quick Pick, Linear, GitHub, Raycast and cmdk, built
natively for Blazor.

## Highlights

- **An fzf-style fuzzy matcher**: dynamic programming with affine gaps, bonuses for word starts, camelCase humps,
  consecutive runs and the first character, smart case, and the optimal positions to highlight.
- **Queries with operators**: several terms that must all match, plus `'exact`, `^prefix`, `suffix$`, `!negate` and
  `"quoted phrases"`.
- **Text as people type it**: `cafe` finds "Café", the Arabic and Persian forms of a letter (`ي`/`ی`, `ك`/`ک`) and
  Persian digits (`۱۲`/`12`) are the same character, and a query that finds nothing is retried allowing a typo
  (`settigns` finds "Settings").
- **Ranking you can reason about**: weighted fields (title › keywords › subtitle › group › description › tags),
  then priority and frecency - how often and how recently a command was used.
- **Nested pages**: static children, lazily loaded children, nested markup, or a page a handler returns, with
  breadcrumbs and `Backspace` to go back - and nested commands found from the root, "Theme › Dark", as kbar does.
- **Modes** by prefix (`>` commands, `#` issues, `@` people, `?` help) that scope the registry and the sources.
- **Async sources**: debounced, cancellable searches against your own APIs, either re-ranked with everything else
  or kept in the order the server returned.
- **Multi-step flows**: free-text prompts, multi-select with `Ctrl+Enter`, confirmation for destructive commands,
  toasts, and a Raycast-style action panel on `Tab`.
- **Recents and pins** persisted to `localStorage`, or to any store you plug in.
- **Global shortcuts**, including sequences such as `G I`, matched in the browser so typing never costs a round
  trip; platform-aware hints (⌘ on macOS, Ctrl elsewhere); they work on Russian, Persian, Arabic or Greek keyboard
  layouts too, and conflicts are reported.
- **Accessibility as architecture**: a native modal `<dialog>`, the WAI-ARIA combobox and grouped listbox pattern,
  live regions for result counts, pages, toasts and errors, focus restore, forced colours, reduced motion, right-to-left
  layouts - audited with axe-core in the browser suite.
- **Failures handled**: a handler or a page loader that throws is logged and reported in the palette, never ends a
  Blazor Server circuit, and a page that failed to load offers a retry.
- **Theming through CSS variables**, light and dark, eight templates, or a fully unstyled mode.
- Works on **Blazor WebAssembly, Blazor Server, Blazor Web App and Blazor Hybrid**, on .NET 10 and 11, is
  trimming and AOT friendly, and can be lazy loaded on WebAssembly.

## Quick start

```bash
dotnet add package Bit.Balette
```

Register the services in `Program.cs`:

```csharp
builder.Services.AddBitBaletteServices();
```

Reference the stylesheet in the host page (`App.razor` or `index.html`):

```html
<link rel="stylesheet" href="_content/Bit.Balette/bit-balette.css" />
```

Add the namespace to `_Imports.razor`:

```razor
@using Bit.Balette
```

Place the palette once, in an interactively rendered layout, and register commands from anywhere through
`IBaletteService`:

```razor
<Balette />
```

The full walkthrough is in [getting started](#getting-started).

## Requirements

- .NET 10 or .NET 11
- Blazor WebAssembly, Blazor Server, Blazor Web App (any interactive render mode) or Blazor Hybrid
- A browser with the `<dialog>` element - every evergreen browser since 2022

## The demo

The demo site, [balette.bitplatform.dev](https://balette.bitplatform.dev), has one global palette and a page per
area: registering commands (fluent and declarative, page-scoped, shown only when they apply, toggles that stay in
sync), nested pages and multi-step flows (prompts, multi-select, long-running and failing commands, recents and pins),
search and ranking with a live view of the matcher's scores, modes and async sources (paged, failing, timed out), the
keyboard, theming with every template and an unstyled palette running, localization and right-to-left text, an
options reference with the site's live values, and a large-registry page that times a search over thousands of
commands in your browser.

[`Samples`](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Balette/Samples/) is the render-mode
harness rather than the docs: one shared [Core](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Balette/Samples/Core/)
project of pages (page-scoped commands and global shortcuts, nested pages and multi-step flows, modes and async sources
registered at runtime, and an inline palette), hosted unchanged under
[Server](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Balette/Samples/Server/),
[Wasm](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Balette/Samples/Wasm/) and
[Auto](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Balette/Samples/Auto/) so the same palette
runs, prerendered first, in each render mode, and under [Maui](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Balette/Samples/Maui/),
a Blazor Hybrid app for Android, iOS, macOS and Windows. Each host is also a minimal example of the wiring that
platform needs.

## How it fits together

```text
Keystroke (browser)
  -> oninput                       the query, once per keystroke
  -> BaletteQuery.Parse            terms and operators, parsed once
  -> IBaletteMatcher               BaletteFuzzyMatcher scores every visible command's fields
  -> BaletteRanker                 score + priority + frecency, top MaxResults kept
  -> rows                          groups, recents, pinned and source blocks flattened into one list
  -> <dialog>                      combobox + listbox, aria-activedescendant on the active row
```

Everything ships in one package, `Bit.Balette`, under one namespace, `Bit.Balette`:

| Part | Contents |
| --- | --- |
| The model | `BaletteCommand` and its fluent builder `BaletteCmd`, results, contexts, modes, keyboard shortcuts. Immutable and free of UI. |
| Search | `BaletteFuzzyMatcher`, `BaletteSubstringMatcher`, field weights, `BaletteRanker`, async sources. Pure C#; it runs in a unit test. |
| The service | `IBaletteService`: the registry, the open state and the events, one per circuit or browser tab. |
| History | Recents, pins and frecency over a pluggable `IBaletteHistoryStore`. |
| The components | `Balette`, `BaletteTrigger`, `BaletteGroup`, `BaletteItem`, `BaletteCommands`, `BaletteShortcutHint`, `BaletteHighlightedText`, and the browser layer. |

Groups and items are flattened into one list of rows, so keyboard navigation, `aria-activedescendant`, scrolling
and virtualization all work on a single model.

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://balette.bitplatform.dev/mcp`, so an AI agent writing command-palette code reads the library itself - its
API, this guide and the demo's working pages - instead of guessing at an API it half remembers.

```json
{
  "mcpServers": {
    "balette": { "type": "http", "url": "https://balette.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBalette`, which searches this guide, every public type and member, the setup guides, the
stylesheet's variables and classes and the demo's pages at once, and points each hit at the exact follow-up call.
`GetBaletteSetupGuide` gives the wiring for one render mode, `GetBaletteGuideSection` one section of this guide,
`GetBaletteApi` one public type with its members, real defaults and documentation, `GetBaletteThemeVariables` the
stylesheet's custom properties with their light and dark values, and `GetBaletteSourceFile` one of the demo's
working pages, or the library's component markup and stylesheet; each returns its index when called without a key. `InspectBaletteSearch` runs the library's own
matcher and ranker over a query and a list of commands and reports each command's score, highlight positions and
final order - the quickest way to see why a command ranks where it does. Every tool is read-only.

## Documentation

- [Getting started](#getting-started)
- [Commands](#commands)
- [Declarative markup](#declarative-markup)
- [Inline palettes](#inline-palettes)
- [Handlers and results](#handlers-and-results)
- [Nested pages](#nested-pages)
- [Search and ranking](#search-and-ranking)
- [Modes](#modes)
- [Async sources](#async-sources)
- [Recents, pins and the action panel](#recents-pins-and-the-action-panel)
- [Keyboard](#keyboard)
- [The service](#the-service)
- [Theming](#theming)
- [Templates](#templates)
- [Localization](#localization)
- [Accessibility](#accessibility)
- [Hosting models](#hosting-models)
- [Security](#security)
- [Options](#options)
- [Performance](#performance)
- [Troubleshooting](#troubleshooting)
- [Migration notes](#migration-notes)

## Getting started

### Install

```bash
dotnet add package Bit.Balette
```

In a Blazor Web App with an interactive client, the package belongs in the `.Client` project, and the services are
registered in both projects (see [hosting models](#hosting-models)).

### Register the services

```csharp
builder.Services.AddBitBaletteServices(options =>
{
    // Ctrl+K / ⌘K is the default; this adds VS Code's Ctrl+Shift+P as well.
    options.AdditionalToggleShortcuts.Add("Mod+Shift+P");

    // Groups appear in this order on the empty query; any others follow alphabetically.
    options.GroupOrder.AddRange(["Navigation", "Actions", "Preferences", "Help"]);
});
```

The options are a singleton. The registry, the open state and the history are scoped: one per circuit on Blazor
Server, one per application on WebAssembly. A WebAssembly app that downloads Balette only when it is needed skips this
step - see [lazy loading](#lazy-loading-on-webassembly).

### Reference the stylesheet

```html
<link rel="stylesheet" href="_content/Bit.Balette/bit-balette.css" />
```

It is a plain stylesheet with no dependencies. Leave it out and set `Unstyled="true"` to style everything yourself
(see [theming](#theming)).

### Place the palette

Put one `Balette` in a layout that is rendered interactively, and a `BaletteTrigger` wherever a visible entry point
helps:

```razor
@inherits LayoutComponentBase

<header>
    <BaletteTrigger Label="Search…" />
</header>

<main>@Body</main>

<Balette />
```

The palette renders a closed `<dialog>` until it is opened, so it costs nothing while it is not in use. It needs an
interactive render mode: a statically rendered palette never attaches its keyboard handling.

### Register commands

Inject `IBaletteService` wherever commands belong and register them:

```razor
@inject IBaletteService Palette
@implements IDisposable

@code {
    private IDisposable? _commands;

    protected override void OnInitialized()
    {
        _commands = Palette.Register(
            BaletteCmd.Navigate("Home", "/").Group("Navigation").Shortcut("G H"),
            BaletteCmd.Navigate("Settings", "/settings").Group("Navigation").Shortcut("G S").Keywords("preferences", "options"),
            BaletteCmd.Action("Toggle sidebar", () => _sidebarOpen = !_sidebarOpen).Group("View").Shortcut("Mod+B"),
            BaletteCmd.New("Theme").Group("Preferences").Children(
                BaletteCmd.Action("Light", () => SetTheme("light")),
                BaletteCmd.Action("Dark", () => SetTheme("dark"))),
            BaletteCmd.New("Delete project")
                .Group("Danger zone")
                .Destructive("This cannot be undone")
                .Handler(async ctx =>
                {
                    await Api.DeleteProjectAsync(ctx.CancellationToken);
                    return BaletteCommandResult.Toast("Project deleted");
                }));
    }

    // Commands registered by a page leave with the page.
    public void Dispose() => _commands?.Dispose();
}
```

`Register` returns an `IDisposable`: dispose it to remove the commands again. Commands that belong to the whole
app are registered once, from the layout or at startup through `options.Commands`.

### Open it

`Ctrl+K` (⌘K on macOS) toggles the palette. Code opens it through the service, optionally with a query - a mode
prefix included - or directly on a command's page:

```csharp
await Palette.OpenAsync();
await Palette.OpenAsync(">");                              // commands mode
await Palette.OpenAsync(pageCommandId: "preferences.theme"); // straight to the Theme page
await Palette.ExecuteAsync("navigation.settings");          // run a command without opening anything
```

### Where to go next

[Commands](#commands) covers everything a command can be. [Handlers and results](#handlers-and-results) and
[nested pages](#nested-pages) cover multi-step flows, [modes](#modes) and [async sources](#async-sources) cover
searching beyond the registry, and [theming](#theming) covers the look.

## Commands

### The model

A `BaletteCommand` is an immutable description of one entry: an action, a navigation target, a toggle, a setting or
a container of nested commands.

| Member | Meaning |
| --- | --- |
| `Id` | Stable identity. Derived from the title, group and parent when not set (see [identity](#identity)). |
| `Title` | The label. Searched with the highest weight and highlighted. Required. |
| `Subtitle` | A secondary label next to the title, such as a path or a category. Searched and highlighted. |
| `Description` | Longer text for the detail pane. Searched with a low weight. |
| `Keywords` | Aliases, synonyms and abbreviations. A keyword hit is shown under the title. |
| `Group` | The heading the command is listed under. |
| `Icon` / `IconCss` | A `RenderFragment`, or a CSS class such as `bi bi-house` for an icon font. |
| `Shortcut` / `GlobalShortcut` | A `BaletteKeyboardShortcut`; global by default, so it fires while the palette is closed. |
| `Priority` | A ranking nudge; `BaletteCommandPriority` has named levels from `Lowest` (-100) to `Highest` (100). |
| `Parent` | The id of the command whose page lists this one. |
| `Children` / `ChildrenProvider` | Nested commands, static or loaded when the page opens. |
| `ChildrenPlaceholder` / `MultiSelectChildren` | The search box placeholder on that page, and whether its rows are checkboxes. |
| `Actions` | Secondary actions shown in the action panel on `Tab`. |
| `Handler` | What runs on selection: `Func<BaletteCommandContext, Task<BaletteCommandResult>>`. |
| `Href` / `ForceLoad` / `OpenInNewTab` | A navigation target, used when there is no handler. |
| `Kind` | `Action`, `Navigation`, `Toggle`, `Setting`, `Search`, `Page`, `Help` or `Recent`: picks the default icon and is exposed as `data-kind`. |
| `Disabled` / `DisabledReason` | Shown but not runnable; the reason replaces the subtitle. |
| `IsVisible` | A predicate evaluated every time the list is built. |
| `Destructive` / `ConfirmMessage` | Styled as dangerous; with a message, `Enter` has to be pressed twice. |
| `Badge`, `Value`, `Checked` | A pill ("New", "Beta", "3"), a current value ("Dark"), or a toggle's state. |
| `AlwaysShow` | Listed whatever the query, like cmdk's `forceMount` - for a "couldn't find it?" row. |
| `ExcludeFromHistory` | Never recorded in recents or frecency. |
| `Tags` | Free-form labels; [modes](#modes) can scope to them. |
| `Data` | Anything your templates or handlers need. |

Commands are immutable on purpose: they are shared between the registry, the rows and the history, and a change is
a new command with the same id. `With` derives one:

```csharp
var renamed = command.With(b => b.Title("Rename…").Badge("New"));
```

### The fluent builder

`BaletteCmd` is the entry point; every method returns a `BaletteCommandBuilder`, which converts to a command
implicitly, so `Build()` is only needed where the target type is not a `BaletteCommand`:

```csharp
BaletteCmd.New("Export…")                       // a blank command
BaletteCmd.Navigate("Settings", "/settings")    // Kind = Navigation, Href set
BaletteCmd.Action("Save", () => Save())         // runs and closes
BaletteCmd.Action("Reload", ReloadAsync)        // Func<Task>
BaletteCmd.Action("Rename…", ctx => ...)        // full control over the result
BaletteCmd.Page("Theme", light, dark)           // Kind = Page, with children
```

`Handler` has overloads for `Action`, `Action<BaletteCommandContext>`, `Func<BaletteCommandContext, Task>` - all
of which close the palette afterwards - and `Func<BaletteCommandContext, Task<BaletteCommandResult>>`, which says
what happens next.

### Identity

`Id` defaults to a slug of the title, prefixed with the parent's id and the group: `Settings` in the group
`Navigation` is `navigation.settings`, and `Dark` under the page `preferences.theme` is `preferences.theme.dark`.
Set it explicitly whenever the title can change - a toggle whose label says "On" or "Off", a translated title - so
recents, pins and the active row survive the change:

```csharp
BaletteCmd.New(wordWrap ? "Disable word wrap" : "Enable word wrap").Id("toggle:wordwrap")
```

Copies made with `With` keep their id even when the title changes. Registering a command with an id that is
already registered replaces it: later registrations win, which lets a page override a global command.

Different titles can derive the same id - `C#` and `C++` are both `c` - and then one replaces the other. When two
commands registered together share an id, a warning is logged (through `ILogger`) naming both titles; give them
explicit ids. A title made of symbols only, such as `🚀`, derives its id from its code points (`u1f680`).

### Groups and ordering

On the empty query, ungrouped commands come first, then the groups in `options.GroupOrder`, then the rest - the
group holding the highest-priority or most-used command first, alphabetically on a tie. Inside a group, commands are ordered by priority, then by frecency, then in the order they were
registered or declared - list `Auto`, `Light` and `Dark` and that is the order they appear in. While searching,
results are ordered by rank, equal ranks by title, groups by their best match, and `GroupWhenSearching = false` flattens the results into one ranked list
instead.

### Toggles and settings

A toggle shows a switch, a setting shows its current value, and both usually keep the palette open so the change
can be seen:

```csharp
BaletteCmd.New("Word wrap").Id("toggle:wordwrap").Checked(state.WordWrap)
    .Handler(_ => { state.ToggleWordWrap(); return Task.FromResult(BaletteCommandResult.KeepOpen); });

BaletteCmd.New("Theme").Value("Dark").Children(/* the options */);
```

Because commands are immutable, the new state is a new command: re-register it, or call
`Palette.Update(command)`, which replaces the command with the same id wherever it was registered, its static
children included.
`BaletteCommands` (see [declarative markup](#declarative-markup)) makes this a matter of re-rendering.

### Disabled, hidden and destructive commands

```csharp
BaletteCmd.New("Export as PDF").Disabled(true, "Requires the Pro plan")  // listed, not runnable
BaletteCmd.New("Merge").VisibleWhen(() => selection.Count > 1)          // listed only when true
BaletteCmd.New("Delete project").Destructive("This permanently deletes the project")
```

`VisibleWhen` is evaluated whenever the list is built. When state it reads changes while the palette is closed - a
selection, the signed-in user - call `Palette.Refresh()`, so the global shortcut of a command that just became
available answers, and the one of a command that went away leaves its keys to the page.

A disabled command answers `Enter` with its reason in the footer. A destructive command is drawn in the danger
colour; with a confirmation message the first `Enter` arms it and shows the message, the second runs it, and
`Esc` or moving away disarms it.

### Page-scoped commands

Commands that only make sense on one page are registered by that page and disposed with it:

```razor
<BaletteCommands Commands="_pageCommands" />
```

`BaletteCommands` registers its `Commands` while it is rendered and swaps the registration when the parameter gets a
new collection instance - assign a new list after a state change, and the palette shows the new commands.

## Declarative markup

Commands can also be written next to the markup they belong to, as children of the palette:

```razor
<Balette>
    <BaletteGroup Heading="Quick create" Priority="20">
        <BaletteItem Title="New issue" Shortcut="C" Keywords="create add" OnExecute="CreateIssue" />
        <BaletteItem Title="New…" ChildrenPlaceholder="What do you want to create?">
            <BaletteItem Title="Project" OnExecute="NewProject" />
            <BaletteItem Title="Document" OnExecute="NewDocument" Badge="Beta" />
        </BaletteItem>
    </BaletteGroup>
</Balette>
```

`BaletteGroup` applies its heading to its items, its priority to the items that do not set their own, and adds its tags
to theirs. `BaletteItem` has a parameter for most members of the model; `Keywords` is a comma or space separated string,
and nested items turn their parent into a page. `Data`, `VisibleWhen`, `ExcludeFromHistory`, `Actions`, `ForceLoad`,
lazily loaded children, page sources and handlers that answer with a prompt, a page or a toast need the builder. `OnExecute` receives the `BaletteCommandContext`, and the palette closes
afterwards unless `KeepOpen` is set; an item with `Href` and no `OnExecute` navigates. An item only re-registers
when one of its parameters actually changed, so re-rendering the layout is cheap.

Markup commands belong to that palette instance, the service's registry to every palette in the scope, and the
palette's `Commands` parameter adds a third, per-instance list.

## Inline palettes

A palette can also live in a page rather than over it - a settings search, a docs search page, a list of commands in
a side panel - with the same search, keyboard and templates:

```razor
<Balette Inline="true"
         IncludeRegisteredCommands="false"
         Commands="_settings"
         Density="compact"
         Placeholder="Search settings…" />
```

An inline palette is a `<section>` named like the dialog (`data-balette="inline"`), always open, as wide as its
container; `--bit-balette-max-height` bounds its list. `Esc` goes back a page or clears the query instead of closing,
and with nothing to undo it is left to the page, so a drawer around the palette still closes on it. `Tab` moves the
focus on, as everywhere else on a page: there is no focus trap. A command that would close the palette resets it to its
root page, and result counts are announced only while it has the focus. It stays out of the way of the modal palette a
layout usually has too: it neither opens on the toggle shortcut nor answers `OpenAsync`, `CloseAsync` or
`ExecuteAsync`, and claims no global shortcuts, so nothing runs twice; a key the modal palette claimed - `Ctrl+K` typed
in the inline palette's box - is left to it. `IncludeRegisteredCommands="false"` leaves out the registry's commands,
sources and modes, so the palette lists only what is declared in its markup or passed to `Commands`.

## Handlers and results

### Results

A handler returns a `BaletteCommandResult` that says what happens next:

| Result | Effect |
| --- | --- |
| `BaletteCommandResult.Close` | Close the palette - the default of every `Action` overload. |
| `BaletteCommandResult.KeepOpen` | Stay open on the same page and rebuild the list, for toggles. |
| `BaletteCommandResult.Back` | Go back one page. |
| `BaletteCommandResult.Reset` | Clear the query and return to the root page, staying open. |
| `BaletteCommandResult.Page(title, commands, placeholder, multiSelect, onSubmit)` | Push a page listing `commands`. |
| `BaletteCommandResult.Prompt(title, onSubmit, placeholder, initialValue)` | Ask for free text, then run `onSubmit` with it. |
| `BaletteCommandResult.Toast(message, keepOpen)` | Show a message in the footer; closes shortly afterwards unless `keepOpen`, or unless the palette is used again meanwhile. |
| `BaletteCommandResult.Error(message)` | Show an error, announced assertively, and stay where the palette is - on a prompt, so the value can be corrected. |

### The context

`BaletteCommandContext` carries the `Command` being run, the `Query` without its mode prefix, the `ModePrefix`,
the `PromptValue` of a prompt step, the `SelectedCommands` of a multi-select page, `Services`, `Navigation`, the
`Palette` service itself and a `CancellationToken` that is cancelled when the palette is closed while the handler
is still running; what such a handler returns afterwards is ignored, so a late page or prompt never reopens it.
`GetRequiredService<T>()` resolves a service from the scope.

### Prompts

```csharp
BaletteCmd.New("Rename project…").Shortcut("F2").Handler(_ => Task.FromResult(BaletteCommandResult.Prompt(
    "New project name",
    ctx =>
    {
        state.Rename(ctx.PromptValue!.Trim());
        return Task.FromResult(BaletteCommandResult.Toast($"Renamed to “{ctx.PromptValue}”"));
    },
    placeholder: "Project name…",
    initialValue: state.ProjectName)));
```

The search box becomes the text field: `Enter` submits, `Esc` goes back.

A prompt can validate what is typed. The validator returns null for a value that may be submitted and the message to
show otherwise; `Enter` on an invalid value shows and announces the message, marks the box `aria-invalid`, and from
then on the message follows the text as it is corrected. A check that needs a server belongs in `onSubmit`, which
answers with `BaletteCommandResult.Error` to keep the prompt open:

```csharp
BaletteCommandResult.Prompt(
    "New project name",
    async ctx =>
    {
        var result = await Api.RenameAsync(ctx.PromptValue!, ctx.CancellationToken);
        return result.Ok ? BaletteCommandResult.Close : BaletteCommandResult.Error(result.Message);
    },
    validate: value => string.IsNullOrWhiteSpace(value) ? "A project needs a name." : null,
    initialValue: state.ProjectName);
```

### Multi-select

```csharp
BaletteCmd.New("Set labels…")
    .MultiSelect()
    .ChildrenPlaceholder("Toggle labels, Ctrl+Enter to apply…")
    .Children(
        BaletteCmd.New("bug").Id("label:bug"),
        BaletteCmd.New("enhancement").Id("label:enhancement"),
        BaletteCmd.New("documentation").Id("label:documentation"))
    .Handler(ctx =>
    {
        state.SetLabels(ctx.SelectedCommands.Select(c => c.Title));
        return Task.FromResult(BaletteCommandResult.Toast(ctx.SelectedCommands.Count == 1 ? "1 label applied" : $"{ctx.SelectedCommands.Count} labels applied"));
    });
```

On a multi-select page `Enter`, a click or `Space` (on an empty query) toggles a row, and `Ctrl+Enter` (⌘↩) runs the
page's handler with the checked commands. A page returned with `BaletteCommandResult.Page(..., multiSelect: true,
onSubmit: ...)` works the same way.

### Long-running commands

A handler that is still running after 150 ms shows the palette busy: the spinner replaces the search icon, the list is
`aria-busy`, the running row carries `data-running` and "Working…" (`BaletteStrings.Running`) is announced. A quick
handler never flickers. A second `Enter` is ignored while it runs, and `Esc` closes the palette and cancels the
handler's `CancellationToken`; what it returns afterwards is dropped.

### Errors

An exception thrown by a handler - or by the loader of a page's children - raises `IBaletteService.CommandFailed`, is
logged through `ILogger<Balette>` and is reported in the palette: a handler's failure as a message in the footer,
announced assertively, a page's as an error state with a **Try again** button (`Enter` retries too).

The message is a localized, generic `BaletteStrings.CommandFailed` ("Couldn’t run “Export”."), so an exception's own
text - which can name a server, a table or a user - never reaches the screen by accident. A subscriber picks another:

```csharp
Palette.CommandFailed += (_, e) =>
{
    if (e.Exception is ValidationException)
    {
        e.Message = e.Exception.Message; // written for users
        e.Handled = true;                // reported: not logged
    }
};
```

`Message = ""` shows nothing. A failure in the palette's own UI - a row chosen by keyboard or mouse, a global shortcut -
is never rethrown, so it can neither end a Blazor Server circuit nor vanish into the browser's console;
`Palette.ExecuteAsync(id)` rethrows an unhandled one to the code that called it. Cancellation is not an error: a
handler that throws `OperationCanceledException` simply stops - unless it wraps a `TimeoutException`, which is how
`HttpClient` reports a timeout, and is reported like any other failure.

When the reader leaves the page a handler was started on while it runs - `Esc` out of a prompt whose submit is still
working - its answer about pages (`Back`, a page, a prompt, `Reset`) is dropped rather than applied to the page they are
on now; closing, toasts and errors still apply.

## Nested pages

Selecting a command that has children opens a page listing them, with breadcrumbs and a back button. `Backspace` or
`←` on an empty query and `Esc` go back; the query typed before the page opened comes back with it. Each ancestor in
the breadcrumbs is a button back to its page.

### Finding nested commands from the root

Nested commands are found from the root page too (`options.SearchNestedCommands`, on by default), as in kbar: typing
`dark` lists **Theme › Dark** under Theme's group, and `theme dark` matches the ancestors' titles as well
(`Weights.Path`, 0.5). A root command and a nested one that match equally well are listed root first. Choosing a nested
page opens it with its ancestors' pages behind it, so `Esc` goes up one level at a time. Recently used and pinned
nested commands are listed on the root page with their path.

Only commands registered with the service or declared in markup are found - a lazily loaded page's children do not
exist until it opens - and only when every ancestor is visible and enabled and none is a multi-select page, whose rows
are choices rather than commands. Modes never include them. Turn it off with `SearchNestedCommands="false"` on the
palette or in the options.

### Static children

```csharp
BaletteCmd.New("Theme").Children(
    BaletteCmd.Action("Light", () => SetTheme("light")),
    BaletteCmd.Action("Dark", () => SetTheme("dark")))
```

Registered children are flattened into the registry with their `Parent` set, so `Find`, `ExecuteAsync`, shortcuts
and `OpenAsync(pageCommandId: ...)` reach them like any other command.

### Lazily loaded children

```csharp
BaletteCmd.New("Assign to…").Children(async (ctx, cancellationToken) =>
{
    var people = await Api.GetPeopleAsync(cancellationToken);
    return people.Select(p => BaletteCmd.Action(p.Name, () => Assign(p)).Subtitle(p.Role).Build()).ToList();
}, placeholder: "Pick an assignee…")
```

The page opens immediately with a loading state and fills in when the provider returns. The `cancellationToken` is
cancelled when the page is left, the palette is closed and reset, or the page is loaded again, and an abandoned load
changes nothing. A provider that throws gets an error state with a retry.

### Declarative children

Nested `BaletteItem`s, shown in [declarative markup](#declarative-markup).

### Pages a handler returns

```csharp
BaletteCmd.New("Export…").Handler(_ => Task.FromResult(BaletteCommandResult.Page("Export format",
[
    BaletteCmd.Action("CSV", ExportCsv).Subtitle("Comma separated values").Build(),
    BaletteCmd.Action("JSON", ExportJson).Subtitle("Pretty printed").Build(),
])))
```

Useful when the list depends on what the handler found out. These commands are not registered anywhere: they live on
the page only.

### Opening directly on a page

```csharp
await Palette.OpenAsync(pageCommandId: "preferences.theme");
```

A nested page opens with its ancestors' pages behind it, so the breadcrumbs show the whole path.

```razor
<BaletteTrigger Label="Change theme" PageCommandId="preferences.theme" />
```

## Search and ranking

### The fuzzy matcher

`BaletteFuzzyMatcher` is modelled on fzf's algorithm. A query matches when its characters appear in order in the
text; among all such alignments it finds the best one by dynamic programming:

| Score | Value | Why |
| --- | --- | --- |
| `ScoreMatch` | 16 | Every matched character. |
| `ScoreGapStart` / `ScoreGapExtension` | -3 / -1 | Skipped characters cost, the first one of a gap most. |
| `BonusBoundary` | 8 | A match at the start of a word: `gs` finds **G**o to **S**ettings. |
| `BonusCamel` | 7 | A camelCase hump or a letter-to-digit step: `fm` finds **F**uzzy**M**atcher. |
| `BonusNonWord` | 8 | Matching punctuation the query actually typed. |
| `BonusConsecutive` | 4 | Characters that follow each other, carrying the bonus that started the run. |
| `BonusFirstCharMultiplier` | 2 | The first query character's bonus counts double. |

Smart case: a query in lower case matches case-insensitively, one with an upper-case letter matches case-sensitively
(`IgnoreCase = true` turns that off). Ties go to the shorter text and the earlier match. The alignment's positions
are kept and highlighted with `<mark>`; a highlight never splits a character from its combining accents or an emoji
from its other half.

The dynamic programming fills (term length + 1) x (field length + 1) cells. Past `BaletteFuzzyMatcher.MaxCells`
(1,048,576) - a term against a very long description - the matcher aligns greedily instead, as fzf does: the first
stretch of the field that holds the term, scored the same way, in linear time. The term is still found and highlighted;
the alignment is just not always the best one. A query is at most `BaletteQuery.MaxLength` (256) characters: the
palette's input stops there, and a longer query passed in by code matches nothing (`BaletteQuery.IsTooLong`).

### Text as people type it

`BaletteFuzzyMatcher.Normalize` (on by default) matches what people type against what is stored:

| Typed | Finds | Why |
| --- | --- | --- |
| `cafe`, `lodz`, `sao paulo` | Café, Łódź, São Paulo | Latin letters lose their diacritics - unless the term types one itself, as in fzf: `café` means that letter. |
| `علی`, `کتاب`, `اسم` | علي, كتاب, إسم | The Arabic and Persian forms of yeh and kaf, alef with hamza or madda, teh marbuta and heh are one letter: which one is stored depends on the keyboard it was typed on. |
| `۱۲`, `٣` | Order 12, Step 3 | Arabic-Indic and Persian digits are ASCII digits; fullwidth ASCII is ASCII. |

Every mapping is one character to one, so highlighting stays exact. The operators below follow the same rules.
`new BaletteFuzzyMatcher { Normalize = false }` matches literally, like fzf's `--literal`.

### Query operators

A query is split on spaces and every term must match some field of the command:

| Term | Meaning |
| --- | --- |
| `set the` | Fuzzy, both terms. |
| `'wrap` | Exact substring. |
| `^go` | Prefix of a field. |
| `ings$` | Suffix of a field. |
| `!delete` | Excludes commands containing "delete" in the title, subtitle, group or keywords. |
| `^go \| ^open` | Alternatives: a `\|` standing on its own between two terms means either will do, as in fzf. `!docker \| git` holds for anything about git, or not about docker. |
| `"go to"` | A phrase, spaces included, matched exactly. `!"..."`, `^"..."` and `"..."$` exclude or anchor it; curly quotes work, and a quote left open takes the rest of the query while it is typed. |

### Typos

A query that matches nothing is tried once more allowing a typo per term - a missing, extra, wrong or swapped letter in
a term of 4 to 7 characters, two from 8 on - so `settigns` finds "Settings". Only a query that found nothing is
retried, so this never adds noise to one that works; such results are announced and marked "No exact matches.
Showing close ones." (`BaletteStrings.ApproximateResults`), and `BaletteMatch.IsApproximate` says so to templates. Turn
it off with `options.TypoTolerance = false` or `TypoTolerance="false"`.

### Field weights

Each term scores against every field and keeps the best weighted score: title 1.0, keywords 0.9, subtitle 0.7,
group 0.5, a nested command's ancestors 0.5, description 0.4, tags 0.3. Change them through `options.Weights`:

```csharp
options.Weights.Description = 0.6;
```

### Priority and frecency

With a query, the final rank is the match score plus `Priority × PriorityWeight` (0.25) plus
`ln(1 + frecency) × FrecencyWeight` (6): the match dominates, and what you use often wins close calls. Without a
query, commands are ordered by priority first, then frecency.

Frecency is the number of uses, capped at 20, times a weight for how long ago the last one was - 100 within 4
hours, 70 within a day, 50 within three days, 30 within a week, 10 within a month, 5 after that - divided by 10.

### A custom matcher

Implement `IBaletteMatcher` - one method, `Match(query, text)`, returning a `BaletteFieldMatch` with a score and
the positions to highlight - and set `options.Matcher`. `BaletteQuery.Parse(text)` is the palette's own parser, public
for tools that explain a search: its `Terms` say each term's text, operator and alternative group. `BaletteSubstringMatcher` is a ready-made case-insensitive
substring matcher for data where fuzziness hurts more than it helps. Multi-term queries, operators and field weights
work with any matcher. Matching can also be called directly, for example in a test:

```csharp
BaletteMatch? match = BaletteFuzzyMatcher.Default.Match(command, "gs");
```

## Modes

A mode is activated by typing its prefix and scopes the palette to part of the registry, to its own sources, or
both - VS Code's `>`, GitHub's `#` and `@`:

```csharp
options.Modes.Add(new BaletteCommandMode
{
    Prefix = ">",
    Name = "Commands",
    Description = "Run any command",
    Placeholder = "Run a command…",
    Tags = ["command"],             // only commands tagged "command"
});

options.Modes.Add(new BaletteCommandMode
{
    Prefix = "@",
    Name = "People",
    Description = "Find a teammate",
    HideRegistryCommands = true,    // only the sources below
    Sources = [new PeopleSource()],
});

options.Modes.Add(new BaletteCommandMode { Prefix = "?", Name = "Help", Groups = ["Help"] });
```

The active mode is shown as a chip next to the breadcrumbs. On the empty query of the default mode, every mode with
`ShowHint` is listed under "Modes" (turn the whole group off with `options.ShowModeHints = false`); selecting one
types its prefix. Modes can also be added and removed at runtime with `Palette.RegisterMode(mode)`. The longest
matching prefix wins, so `>>` and `>` can coexist.

## Async sources

An `IBaletteSource` searches something the registry does not hold - an API, a database, a search index:

```csharp
public sealed class PeopleSource(PeopleApi api) : IBaletteSource
{
    public string Name => "People";        // the group heading of its results
    public int MinQueryLength => 1;        // 0 also asks on the empty query
    public int MaxResults => 20;
    public bool ApplyMatcher => true;      // re-rank with everything else

    public async Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken)
    {
        var people = await api.SearchAsync(query, cancellationToken);
        return people.Select(p => BaletteCmd.New(p.Name).Subtitle(p.Role).Handler(_ => Open(p)).Build()).ToList();
    }
}
```

Sources are asked after `options.DebounceMs` (150 ms) without typing, and every newer keystroke cancels the
`cancellationToken` of the previous search, so a slow API never shows stale results. A spinner replaces the search
icon while any source is pending.

### Paging

A source that has more than one page of results answers through `SearchPageAsync(query, cursor, context, token)`: the
first page with a null `cursor`, each next one with the `NextCursor` of the page before. A page with a cursor gets a row
after the source's results - "Show 37 more…" when the page says `TotalCount`, "Show more…" when it does not - which
loads the next page, appends it without repeating what is listed, and selects its first row. A source that implements
only `SearchAsync` answers a single page, as before.

```csharp
new BaletteDelegateSource("Issues", async (query, cursor, context, ct) =>
{
    var page = await Api.SearchIssuesAsync(query, after: cursor, take: 20, ct);
    return new BaletteSourcePage([.. page.Items.Select(ToCommand)], page.NextCursor, page.Total);
});
```

A source that throws, or that has not answered after `options.SourceTimeoutMs` (10 s) - whether or not it honours its
token - is logged and leaves a row in
place of its results - "Couldn’t search People", in the danger colour - that asks it again when chosen. The other
sources' results stay: one failing API does not take the whole list with it.

With `ApplyMatcher = true` the results are scored by the palette's matcher and interleaved with the registry by
rank. With `false` - the default - the source's own order is kept and shown as a block under its name, which is
right when the server already ranked by relevance.

Sources are queried in the default mode when added to `options.Sources`, registered at runtime with
`Palette.RegisterSource`, or resolved from DI with `services.AddBitBaletteSource<TSource>()` - those belong to the
scope and stay registered whichever palette comes and goes. A mode queries its own
`Sources` instead.

### Searching from a page

A command's page can search a server as the user types instead of listing what it has - an "Assign to…" over
thousands of people, which a lazily loaded page would have to download whole:

```csharp
BaletteCmd.New("Assign to…").Source("People", async (query, cancellationToken) =>
{
    var people = await Api.SearchPeopleAsync(query, take: 20, cancellationToken);
    return people.Select(p => BaletteCmd.Action(p.Name, () => Assign(p)).Subtitle(p.Role).Build()).ToList();
})
```

The source is asked when the page opens (its `minQueryLength` is 0 by default here) and for every query typed on it,
with the same debounce, cancellation, timeout and retry as the root's sources; `BaletteSourceContext.ParentCommandId`
says which page asked. `.Source(IBaletteSource)` takes any source. On a multi-select page the checked rows are kept
across searches, so `Ctrl+Enter` applies everything checked, whichever query listed it. Two ready-made sources cover the common cases: `BaletteDelegateSource` wraps a delegate, and
`BaletteStaticSource` serves a large in-memory list through the matcher, without registering thousands of
commands.

## Recents, pins and the action panel

On the empty query of the root page, pinned commands are listed first and the most recently used ones (up to
`options.MaxRecents`, 5) next. Every run is recorded unless the command has `ExcludeFromHistory`, and the counts
feed [frecency](#priority-and-frecency).

`Tab` (or `→` on an empty query, or a right click) opens the action panel of the active command: its own `Actions`,
then "Pin to top" or "Unpin", and "Remove from recents" when it has been used. `Ctrl+Enter` runs the first secondary
action directly - on a link without actions it opens the link in a new tab, as GitHub's palette does - `Mod+Shift+P`
pins or unpins and `Shift+Delete` forgets.

Pinned commands are listed in the order they were pinned. Every action panel also offers "Clear recent commands",
which forgets every use and keeps the pins - a privacy control within reach of the list it clears.

History is kept in `localStorage` under `options.HistoryStorageKey` (`bit-balette.history`), capped at
`options.MaxHistoryEntries` (200). To keep it elsewhere - per user on the server, or nowhere at all:

```csharp
builder.Services.AddBitBaletteHistoryStore<MyServerHistoryStore>(); // implements IBaletteHistoryStore
builder.Services.AddBitBaletteInMemoryHistoryStore();              // for the lifetime of the scope only
```

Turn history off entirely with `options.EnableHistory = false`, or on one palette with `EnableHistory="false"`.

## Keyboard

### Inside the palette

| Keys | Action |
| --- | --- |
| `↑` `↓`, `Ctrl+N` `Ctrl+P` | Move; wraps around unless `Loop` is off. |
| `Ctrl+J` `Ctrl+K` | Move, when `VimBindings` is on. |
| `Home` `End`, `PgUp` `PgDn` | Jump to the first or last row, or by ten. |
| `Enter` | Run, open the page, or toggle on a multi-select page. |
| `Ctrl+Enter` (⌘↩) | Apply a multi-select page, run the first secondary action, or open a link in a new tab. |
| `Tab`, `→` on an empty query | The action panel. |
| `Backspace`, `←` on an empty query | Back one page. |
| `Esc` | Disarm a confirmation, then go back, then close. |
| `Mod+Shift+P` | Pin or unpin the active command. |
| `Shift+Delete` | Remove the active command from the recents. |

These keys are claimed in the browser before Blazor sees them, so `Tab` never leaves the dialog and `Backspace`
never deletes text it should not, even over a slow Blazor Server connection. In right-to-left text `←` and `→` trade
places: `→` points back to the start of the line, so it goes back a page.

### Global shortcuts

A command's `Shortcut` fires while the palette is closed, from anywhere on the page:

```csharp
BaletteCmd.Navigate("Settings", "/settings").Shortcut("G S")    // a sequence: G, then S
BaletteCmd.Action("Toggle sidebar", Toggle).Shortcut("Mod+B")   // a chord
BaletteCmd.New("Search").Shortcut("/", global: true)
BaletteCmd.New("Preview").Shortcut("Space", global: false)      // only shown as a hint
```

The registered shortcuts are synced to the browser as strings and matched there, so only a completed shortcut
crosses the interop boundary - typing on the page never causes a round trip. A sequence waits
`options.SequenceTimeoutMs` (1000) for its next chord. Plain keys are ignored while an input, a textarea or a
content-editable element has focus (`SuppressShortcutsInEditableElements`); chords with `Ctrl`, `Alt` or `⌘` still
fire. Whatever needs the palette opens it: a command with a page or a confirmation, and a handler that answers with
a page, a prompt or a toast that stays open. A confirmation started this way gets a page of its own, so the second
`Enter` confirms that command and no other. Only a command that can run claims its keys: the shortcut of a disabled
command, or of one its `VisibleWhen` predicate hides, is left to the page.

### Keyboard layouts, held keys and input methods

A shortcut is matched on the character the key types and, when that is not an ASCII character, on the key it sits on
as well (`KeyboardEvent.code`, as VS Code does): `Ctrl+K` works when a Russian layout types `л`, and `G S` when a
Persian one types `ل` `س`. A Latin layout such as Dvorak or AZERTY keeps its own letters. A held key's repeats are
claimed - the browser does not act on them - but run nothing, so a held `Ctrl+K` does not flicker the palette open and
shut. A character typed with AltGr, or with Option or Alt alone (`Ω` for Option+Z on a Mac), is typing when it lands in a
text field, not the shortcut its physical key would make. Keys that belong to an input method's composition (`isComposing`, or `keyCode` 229) are never shortcuts, so
confirming a Japanese or Chinese candidate with `Enter` does not run the active row.

### Sequences in progress

After the first chords of a sequence - `G` of `G S` - a small status near the bottom of the page shows them and says the
next key is awaited, as VS Code does in its status bar, and screen readers announce it; it goes when the sequence
completes, breaks off or times out. `options.ShowSequenceHint = false` turns it off; `IBaletteService.SequencePending`
reports it to code either way, with the chords pressed so far and then null.

### Conflicts

When several commands claim one global shortcut, the one registered last wins - the rule ids follow too - so a page's
own `Mod+S` takes over from the layout's while the page is shown. The palette logs a warning, once each, for a shortcut
several commands claim, for one the toggle shortcut takes, and for a sequence that can never complete because its first
chords are a shortcut of their own (`G` registered next to `G I`), and for one browsers keep for themselves and never
hand to a page (`Mod+W`, `Mod+T`, `Mod+N`, `Ctrl+Tab`...).

### Shortcut syntax

Chords are separated by spaces and keys inside a chord by `+`: `Mod+K`, `Ctrl+Shift+P`, `G I`, `Alt+Z`, `F2`,
`Shift+?`. `Mod` is ⌘ on macOS and `Ctrl` elsewhere. Modifiers have aliases (`Control`, `Cmd`, `Command`, `Win`,
`Option`, `Opt`, `⌃`, `⇧`, `⌥`, `⌘`), and so do keys (`Esc`, `Return`, `Space`, `Del`, `Up`, `PgDn`, `Plus`, `Comma`,
`Slash`...). `BaletteKeyboardShortcut.Parse` throws a `FormatException` for a chord without a key or with two;
`TryParse` does not, and a string converts implicitly wherever a shortcut is expected.

### Platform-aware hints

The palette detects macOS and iOS once it starts, and every hint follows: `⌘ ⇧ K` there, `Ctrl Shift K`
elsewhere. `IBaletteService.IsMac` exposes the result to your own UI and `PlatformDetected` says when it is known,
`BaletteShortcutHint` renders any shortcut the same way, and `BaletteTrigger` updates itself.

### The toggle

`options.ToggleShortcut` (`Mod+K`) and `options.AdditionalToggleShortcuts` open and close the palette, and the
palette's `ToggleShortcut` parameter overrides the first. Inside the palette its own chords win over a toggle bound
to the same keys: with Vim bindings on, `Ctrl+K` moves up rather than closing.

## The service

`IBaletteService` is the palette's programmatic surface, scoped like the rest:

| Member | Purpose |
| --- | --- |
| `Register(...)` | Add commands; dispose the result to remove them. |
| `Refresh()` | Re-evaluate `VisibleWhen` predicates for the global shortcuts and an open list after application state changed. |
| `Update(command)` | Replace the command with the same id, or add it. |
| `Unregister(id)`, `Find(id)` | Remove or look up one command, nested ones included. |
| `RegisterMode(mode)`, `RegisterSource(source)` | Add a mode or a default-mode source at runtime; dispose to remove. |
| `OpenAsync(query, pageCommandId)`, `CloseAsync()`, `ToggleAsync()` | Drive the palette. |
| `ExecuteAsync(id)` | Run a command as if it had been selected - registered, declared in markup or passed to `Commands` - opening the palette when it needs it. |
| `IsOpen`, `IsMac`, `Commands`, `Modes`, `Sources` | State. |
| `CommandsChanged`, `Opened`, `Closed`, `CommandExecuted`, `CommandFailed`, `PlatformDetected` | Events. |

Where the services are not registered - an app that [lazy loads](#lazy-loading-on-webassembly) Balette on
WebAssembly - `IServiceProvider.GetBaletteService()` returns the one the palette uses; where they are, it returns the
registered one, so code that has to work both ways can always call it.

`CommandExecuted` reports the command, the result and how long the handler took. The `Balette` component exposes the
same `OpenAsync`, `CloseAsync` and `ToggleAsync`, plus `SetQueryAsync`, `Query` and `ActiveCommand`, and an `Open`
parameter for two-way binding:

```razor
<Balette @bind-Open="_paletteOpen" OnCommandExecuted="Log" OnQueryChanged="Track" />
```

## Theming

### The variables

Every visual is a custom property on `:root`, so the trigger and shortcut hints outside the dialog share them with
it:

| Variable | Default (light) | Purpose |
| --- | --- | --- |
| `--bit-balette-width` | `640px` | The dialog's width (920px with a detail pane). |
| `--bit-balette-max-height` | `min(70vh, 560px)` | The list's height limit. |
| `--bit-balette-radius` | `14px` | Corner radius. |
| `--bit-balette-row-height` | `44px` | Minimum row height (`options.RowHeight` sets it while `Virtualize` is on). |
| `--bit-balette-gap` | `8px` | List padding. |
| `--bit-balette-font`, `--bit-balette-font-size`, `--bit-balette-mono` | system stacks, `14px` | Typography. |
| `--bit-balette-z` | `1000` | Stacking order. |
| `--bit-balette-bg`, `--bit-balette-bg-elevated` | `#ffffff`, `#f6f7f9` | Surfaces. |
| `--bit-balette-fg`, `--bit-balette-fg-muted`, `--bit-balette-fg-subtle` | `#111318`, `#5f6673`, `#6b7280` | Text. |
| `--bit-balette-border` | `rgba(17, 19, 24, 0.08)` | Dividers. |
| `--bit-balette-accent`, `--bit-balette-accent-fg`, `--bit-balette-accent-soft` | `#4f46e5`, ... | Highlights, badges, the mode chip. |
| `--bit-balette-active-bg` | `rgba(17, 19, 24, 0.06)` | The active row. |
| `--bit-balette-danger`, `--bit-balette-danger-soft` | `#c81e1e`, ... | Destructive commands. |
| `--bit-balette-mark-bg` | `rgba(79, 70, 229, 0.18)` | Highlighted characters. |
| `--bit-balette-kbd-bg`, `--bit-balette-kbd-fg`, `--bit-balette-kbd-border` | `#f3f4f6`, ... | Key caps. |
| `--bit-balette-backdrop`, `--bit-balette-shadow`, `--bit-balette-scrollbar` | ... | The overlay, the dialog's shadow, the list's scrollbar. |
| `--bit-balette-top` | `12vh` | How far down the dialog opens. |
| `--bit-balette-input-height`, `--bit-balette-input-font-size` | `56px`, `16px` | The search box. |
| `--bit-balette-item-radius`, `--bit-balette-item-padding`, `--bit-balette-item-gap` | `8px`, `6px 10px`, `12px` | Rows. |
| `--bit-balette-heading-font-size`, `--bit-balette-footer-height`, `--bit-balette-detail-width` | `11px`, `40px`, `40%` | Group headings, the footer, the detail pane. |
| `--bit-balette-backdrop-blur`, `--bit-balette-duration` | `2px`, `140ms` | The overlay's blur and the pop-in. |
| `--bit-balette-heading-fg`, `--bit-balette-focus-ring`, `--bit-balette-toggle-knob` | unset | Optional: group headings (else `--bit-balette-fg-muted`), focus outlines (else `--bit-balette-accent`), the switch's knob (else white). |

Every text colour pair of the default light and dark themes meets WCAG AA (4.5:1), on the active row and the footer
too.

Override them globally, on `dialog[data-balette="dialog"]`, or inline:

```razor
<Balette Style="--bit-balette-width: 760px; --bit-balette-accent: #0f766e" />
```

### Light and dark

The palette follows `prefers-color-scheme`, `<html data-theme="light">` or `<html data-theme="dark">` forces one,
and the component's `Theme` parameter (`auto`, `light`, `dark`; `options.Theme` by default) forces it for the dialog
alone. Animations are dropped under `prefers-reduced-motion`, and below 640px the dialog fills the screen and hides
the hints and the detail pane.

### Forced colours, more contrast and right-to-left text

When the reader asks for more contrast (`prefers-contrast: more`), borders are drawn stronger, the active row is
outlined and highlighted characters are underlined as well as tinted.

In forced-colours mode (Windows High Contrast) backgrounds and shadows disappear, so the active row is outlined in the
system `Highlight` colour, the dialog gets a border, and checkboxes, switches and highlights are drawn in system colours.

Every rule uses logical properties, so `dir="rtl"` lays the palette out from the right: the footer's last hint, the
detail pane's border and the switch's knob all follow, arrows and chevrons are mirrored, and user text - titles,
subtitles, badges, values, breadcrumbs, headings - isolates its own direction (`dir="auto"`), so an English title in a
Persian UI keeps its punctuation where it belongs. Shortcut key caps always read left to right, as on the keys.

### Density

`Density="compact"` (or `options.Density`) tightens rows, the search box and the footer for a dense application or an
inline palette; `comfortable` is the default. The value is exposed as `data-density`, so a stylesheet can define more.

### Unstyled

`Unstyled="true"` drops the `bit-balette-dialog` class every rule is scoped to, leaving bare markup with stable
`data-balette` attributes - `dialog` (`inline` for an inline palette), `root`, `main`, `header`, `breadcrumbs`, `crumb`, `mode`, `input-row`,
`input`, `spinner`, `selected-count`, `close`, `notice`, `list`, `group`, `group-heading`, `item`, `path`, `label`,
`mark`, `badge`, `shortcut`, `empty`, `loading`, `error`, `retry`, `prompt`, `prompt-error`, `footer`, `toast`,
`status`, `alert`, `detail`, and `trigger` on the trigger button - and state attributes on the rows: `data-active`,
`data-checked`, `data-disabled`, `data-destructive`, `data-confirming`, `data-running`, `data-error`, `data-kind` and
`data-value` (the command's id). The dialog carries `data-open`, `data-busy`, `data-density`, `data-theme`,
`data-multiselect` and `data-ready` - set once its browser layer listens for keys, which is what a test waits for
before pressing `Ctrl+K` on a page that was prerendered. Those attributes are also the stable hooks for tests.

## Templates

| Parameter | Receives | Replaces |
| --- | --- | --- |
| `ItemTemplate` | `BaletteItemContext` (row, active, checked, pinned, confirming, isMac) | A row's content. |
| `GroupHeaderTemplate` | the heading | A group heading. |
| `EmptyTemplate` | the query | "No results". |
| `LoadingTemplate` | - | The loading state. |
| `HeaderTemplate`, `FooterTemplate` | `BaletteStateContext` | Content above the input; the footer. |
| `DetailTemplate` | the active `BaletteCommand` | Adds a detail pane, Raycast style. |
| `IconTemplate` | the `BaletteCommand` | Every row's icon. |

```razor
<Balette>
    <DetailTemplate>
        <h3>@context.Title</h3>
        <p>@context.Description</p>
        @if (context.Shortcut is not null)
        {
            <BaletteShortcutHint Shortcut="context.Shortcut" />
        }
    </DetailTemplate>
</Balette>
```

The row element itself - its role, ARIA state, data attributes and click handling - stays the palette's, so a
template cannot break keyboard navigation. `BaletteHighlightedText` renders a text with the matched positions marked,
for templates that want the highlighting back.

## Localization

Every label and announcement is a property of `BaletteStrings` - the ones only screen readers hear included, such as
`Breadcrumb`, `On`/`Off`, `ShortcutThen` ("G then S") and `PageAnnouncement`: set `options.Strings`, or the palette's
`Strings` parameter, to an instance with your texts, or to a subclass that reads them from an `IStringLocalizer`.
For languages whose plural forms one template per count cannot express, override `ResultsAnnouncementFor(int count)`.

```csharp
options.Strings = new BaletteStrings
{
    Placeholder = "Befehl eingeben oder suchen…",
    NoResults = "Keine Ergebnisse für „{0}“",
    ResultsAnnouncement = "{0} Ergebnisse verfügbar",
};
```

When titles are translated, add the untranslated title as a keyword - `.Keywords("Settings")` on "Einstellungen" -
so people who learned the English name still find it, as VS Code does with its English aliases.

## Accessibility

- The palette is a native modal `<dialog>`: it sits in the top layer, traps focus, makes the page behind it inert,
  locks scrolling, and gives focus back to whatever had it when it closes.
- Focus stays on the input, which is a `combobox` controlling a `listbox`; `aria-activedescendant` points at the
  active `option`, which carries `aria-selected`, and multi-select pages add `aria-multiselectable` and
  `aria-checked` - the WAI-ARIA combobox pattern, so screen readers follow the selection without focus moving.
- Each group is a `role="group"` named by its heading (the WAI-ARIA grouped listbox), so screen readers say which
  group an option is in; the listbox owns only options and groups. The list is a listbox only while it lists
  something - the empty, loading and error states are statuses and alerts instead - and the combobox's
  `aria-expanded` follows. On a prompt the input is a plain text field named by the prompt's question. In a virtualized list, where rows cannot be
  wrapped, each option is described by its heading and carries `aria-posinset` and `aria-setsize`.
- A single-select list marks its active option `aria-selected`; a multi-select page marks its checked options
  `aria-checked` and nothing else - one state per list, as the pattern asks.
- Polite live regions, present before anything is announced in them, say the result count after every search, the
  page the palette moved to ("Theme. 3 results available"), loading, and every toast whether or not the footer is
  shown; errors are announced assertively. The list is `aria-busy` while it loads.
- A toggle's state and a pinned command are spoken ("On", "Off", "Pinned"), not only drawn; a shortcut hint is spoken
  in words while its key caps are hidden from screen readers.
- Disabled commands are `aria-disabled` and say why; group headings are presentational, so they are not read as
  options. Clicking a row leaves the focus in the input.
- Shortcut hints have an accessible name in words ("Ctrl+K", "G then S"), and the trigger declares
  `aria-keyshortcuts`.
- Pointer hover only moves the selection once the pointer actually moves, so a list scrolling under a resting mouse
  does not steal the keyboard's selection (`DisablePointerSelection` turns hover off completely).
- The close button's accessible name holds its visible "Esc" (WCAG 2.5.3), and the buttons are at least 24 px.

The browser suite runs axe-core (WCAG 2.2 A and AA rules) over the root list, search results, a nested page, a
multi-select page and a prompt, in light and dark, with the demo's theme and with the library's defaults. An automated
audit finds only part of the problems; it is not a claim of conformance, and screen reader testing by people who use
them remains the measure.

## Hosting models

### Blazor WebAssembly

Register the services in `Program.cs`, link the stylesheet in `index.html`, and place `<Balette />` in
`MainLayout.razor`. Searching runs on the browser's one thread, so on every keystroke whose search took longer than
a frame the palette lets the keystroke paint first and folds keys typed meanwhile into one search - typing stays
responsive with thousands of commands. An app that is not compiled ahead of time can download the library only when
a page with the palette opens - see [lazy loading](#lazy-loading-on-webassembly).

### Lazy loading on WebAssembly

The components also work without `AddBitBaletteServices`, so a WebAssembly app can fetch Balette only when someone
opens a part of the app that has a palette. When the services are not registered, `Balette`, `BaletteTrigger` and
`BaletteCommands` create every service they need themselves, once per browser connection, and
`IServiceProvider.GetBaletteService()` hands the app's own code the same `IBaletteService`. Mark the assembly as lazy
loaded in the project that runs in the browser, and load it before the first page that uses it renders:

```xml
<ItemGroup>
  <BlazorWebAssemblyLazyLoad Include="Bit.Balette.wasm" />
</ItemGroup>
```

```razor
@* App.razor: the router waits for OnNavigateAsync before it renders the page. *@
@using Microsoft.AspNetCore.Components.WebAssembly.Services
@inject LazyAssemblyLoader LazyAssemblyLoader

<Router AppAssembly="typeof(App).Assembly" OnNavigateAsync="OnNavigateAsync">
    ...
</Router>

@code {
    private async Task OnNavigateAsync(NavigationContext context)
    {
        // The pages with the palette: fetch Balette before they render. A second call loads nothing again. Routes
        // match regardless of case, so /Admin/users is one of them too.
        if (context.Path.StartsWith("admin", StringComparison.OrdinalIgnoreCase))
            await LazyAssemblyLoader.LoadAssembliesAsync(["Bit.Balette.wasm"]);
    }
}
```

Those pages, or a layout only they use, hold the palette and register commands through `GetBaletteService()` instead
of injecting `IBaletteService`:

```razor
@* AdminLayout.razor *@
@inherits LayoutComponentBase
@inject IServiceProvider Services
@implements IDisposable

<BaletteTrigger Label="Search…" />
@Body
<Balette />

@code {
    private IDisposable? _commands;

    protected override void OnInitialized()
        => _commands = Services.GetBaletteService().Register(
            BaletteCmd.Navigate("Users", "/admin/users").Group("Navigation"),
            BaletteCmd.Navigate("Billing", "/admin/billing").Group("Navigation"));

    public void Dispose() => _commands?.Dispose();
}
```

The stylesheet is a plain `<link>`, served whether the assembly has loaded or not, and the palette imports its
JavaScript module the first time it renders.

- Leave `AddBitBaletteServices` out of that project's `Program.cs`: calling it references Balette's types before the
  assembly is there. For the same reason only what renders after the assembly has loaded may use Balette's types -
  the pages it is loaded for and a layout only they use, not `App.razor`, a layout every page shares or `Program.cs`.
- No type in an assembly the router reads when the app starts - the app's own assembly first of all - may derive from
  or implement one of Balette's: no `IBaletteSource` class, no `IBaletteHistoryStore`, no component that inherits
  `Balette`. The router reads the types of those assemblies at startup, and on WebAssembly a type whose base type or
  interface lives in an assembly that is not loaded yet fails to load - and keeps failing after Bit.Balette has loaded,
  breaking the app's use of it for the rest of the session. Put such a class in a library that is lazy loaded together
  with Bit.Balette.
- Without the registration the palette runs on the default `BaletteOptions`. Its parameters override most of them
  per instance (`Strings`, `ToggleShortcut`, `Theme`, `Density`, `Loop`, `EnableHistory`, ...) - a `BaletteTrigger`
  shows and announces the toggle shortcut and the strings of the palette it opens - and modes, sources and commands are
  registered through `GetBaletteService()` instead of `options.Modes`, `options.Sources` and `options.Commands`. The
  rest - such as `GroupOrder`, `AdditionalToggleShortcuts`, the field weights, the matcher and `AllowedUrlSchemes` -
  keep their defaults; an app that needs them registers the services rather than lazy loading.
- The history store cannot be replaced either: recents, pins and frecency go to the browser's `localStorage` under the
  default `HistoryStorageKey`, `bit-balette.history`, which every app on the same origin shares. An app that must not
  write to the browser sets `EnableHistory="false"` on its palettes.
- The palette logs one warning that the services are not registered - expected here; it is what tells an app that
  should have registered them, such as the client project of a Blazor Web App that forgot to, why its options are
  ignored. `builder.Logging.AddFilter("Bit.Balette.BitBalette", LogLevel.None)` in `Program.cs` silences it without
  referencing Balette's types.
- Do not lazy load in an AOT build. AOT compiles every assembly into `dotnet.native.wasm`, so there is nothing left to
  download later, and lazy loading an AOT-compiled assembly can fail at startup.

### Blazor Server

The same setup. Shortcuts and navigation keys are handled in the browser, so only the query itself and the
selected command cross the connection. Handlers run on the server, in the circuit's scope. Keys typed faster than
the round trip are all kept: the input is bound, so a render answering an earlier key never writes its value over the
keys typed since.

### Blazor Web App

Register the services in **both** projects - every page is prerendered on the server before it runs in the browser,
so anything a component injects must resolve in both containers - and link the stylesheet in `App.razor`. The
palette needs an interactive render mode:

- With global interactivity (`<Routes @rendermode="InteractiveWebAssembly" />` or `InteractiveServer` or
  `InteractiveAuto`), put it in `MainLayout.razor`.
- With per-page interactivity the layout is static: wrap the palette in a small component rendered with an
  interactive mode, and register commands from components that run in the same mode, because the registry is
  scoped to one circuit or one WebAssembly runtime.

While prerendering, the palette renders a closed dialog; it attaches its keyboard handling once it is interactive, and
marks the dialog `data-ready="true"` when it has - what an end-to-end test waits for before pressing `Ctrl+K`.

The browser suite runs against Blazor Server as well as WebAssembly (`BALETTE_E2E_RENDER_MODE=Server`, see the E2E
README): every key the palette handles then crosses the circuit.

### Blazor Hybrid

Works unchanged in a MAUI, WPF or Windows Forms `BlazorWebView`; history goes to the web view's `localStorage`.

### Content security policy

The browser layer is a JavaScript module loaded from `_content/Bit.Balette/bit-balette.js` with `import()`; there
are no inline scripts and no `eval`, so `script-src 'self'` is enough.

## Security

- **Text is text.** Titles, subtitles, descriptions, badges, queries and whatever an async source returns are rendered
  as text by Razor, never as HTML. Icons are `RenderFragment`s you build, or a CSS class name.
- **Links are checked.** A command's `Href` is followed only when it is relative or its scheme is in
  `options.AllowedUrlSchemes` (`http`, `https`, `mailto`, `tel`); a `javascript:`, `data:` or `vbscript:` link - read the
  way a browser reads it, tabs and control characters included - is refused and logged. Commands often carry API data,
  so this matters most for sources.
- **Errors are generic.** A failed command shows `BaletteStrings.CommandFailed`, not the exception's text (see
  [errors](#errors)).
- **History is ids only.** Recents and pins store command ids, counts and times - never queries. A source whose
  command ids embed personal data (an e-mail address) should set `ExcludeFromHistory`, or use opaque ids.
- **Visibility is not authorization.** `VisibleWhen` and `Disabled` shape the UI; the server must still check every
  action a handler asks it to perform.
- Stored history is validated when it is read: entries a hand-edited or corrupt `localStorage` holds are dropped.
- **Search is bounded.** A query is at most `BaletteQuery.MaxLength` (256) characters, and no field comparison fills
  more than `BaletteFuzzyMatcher.MaxCells` cells - a longer one is aligned greedily, and gets no typo-tolerant second
  chance - so a pasted wall of text or a huge field cannot make a keystroke allocate gigabytes (on Blazor Server, on
  the server). Search work still grows with the length of the fields you give it: keep descriptions to what a palette
  row needs, and search large documents through an async source backed by a real index.

## Options

| Option | Default | Notes |
| --- | --- | --- |
| `ToggleShortcut` | `Mod+K` | Plus `AdditionalToggleShortcuts`. |
| `SuppressShortcutsInEditableElements` | `true` | Plain keys do not fire from inputs. |
| `SequenceTimeoutMs` | `1000` | How long a sequence waits for its next chord; must be positive. |
| `DebounceMs` | `150` | Async sources. Must not be negative. |
| `SourceTimeoutMs` | `10000` | How long a source may take before it counts as failed; 0 waits for ever. |
| `Loop`, `VimBindings` | `true`, `false` | Arrow navigation. |
| `EnableHistory`, `MaxRecents`, `MaxHistoryEntries` | `true`, `5`, `200` | |
| `HistoryStorageKey` | `bit-balette.history` | The `localStorage` key. |
| `GroupWhenSearching` | `true` | A flat ranked list when `false`. |
| `MaxResults` | `150` | Rows kept per query, groups excluded. Must be positive; pinned and recent commands are listed beyond it. |
| `GroupOrder` | empty | Explicit group order. |
| `PriorityWeight`, `FrecencyWeight` | `0.25`, `6` | Ranking nudges while searching. |
| `ShowModeHints` | `true` | The "Modes" group on the empty query. |
| `SearchNestedCommands` | `true` | Find nested commands from the root page. |
| `TypoTolerance` | `true` | Retry a query that found nothing allowing a typo per term. |
| `AllowedUrlSchemes` | `http`, `https`, `mailto`, `tel` | Schemes an `Href` may use; relative links always may. |
| `CloseOnBackdropClick`, `CloseOnNavigation`, `ResetOnClose` | `true` | |
| `HighlightMatches`, `ShowFooter`, `ShowShortcuts` | `true` | |
| `Virtualize`, `RowHeight` | `false`, `44` | Recommended above about 500 visible rows; while on, rows are `RowHeight` pixels tall whatever `--bit-balette-row-height` says. |
| `Matcher`, `Weights` | `BaletteFuzzyMatcher.Default` | Pluggable. |
| `Strings` | English | See [localization](#localization). |
| `Commands`, `Modes`, `Sources` | empty | Registered at startup. |
| `Theme` | `auto` | `light` or `dark` to force one. |
| `Density` | `comfortable` | `compact` for tighter rows. |

Most display options can be overridden per palette with the parameter of the same name.

## Performance

A palette runs its search on every keystroke, so the hot path is written for it:

- The registry is read once, not on every keystroke: the palette keeps a snapshot and takes a new one only when the
  registry, the commands declared in its markup or its `Commands` parameter changed. Pass `Commands` a new collection
  to change it (or call `Refresh()` after changing one in place). Global shortcuts are re-evaluated when the snapshot
  changes, when the palette opens or closes, after a command ran and on `Refresh()` - not while typing.
- The query is parsed into terms once per keystroke, not once per command.
- The matcher allocates nothing per field: it reuses per-thread buffers, rejects non-subsequences before the
  dynamic programming starts, and avoids LINQ. A field too long for those buffers borrows from `ArrayPool<T>.Shared`
  and gives back, so long descriptions leave no large-object garbage behind; one past `BaletteFuzzyMatcher.MaxCells`
  is aligned greedily in linear time.
- Ranking only sorts what it keeps: with `MaxResults` it holds the best rows in a heap and sorts those.
- Results from async sources reuse the registry's matches for the same query instead of matching again.
- On WebAssembly a search that took longer than a frame lets the keystroke paint first and folds fast typing into
  one search; navigation keys still act on the latest query.

The benchmarks in `Tests/Bit.Balette.Benchmarks` measure the matcher, the ranker and a whole keystroke over
registries of different sizes. With very large lists, also turn on `Virtualize`, or serve the data through a
`BaletteStaticSource` instead of registering every row.

## Troubleshooting

**`Ctrl+K` does nothing.** The palette is not interactive: it is in a statically rendered layout. Also check that no
other script calls `preventDefault` on the key first.

**Options from `Program.cs` stop working once the page turns interactive** - a toggle shortcut, the group order,
commands added through `options.Commands`. The services are registered in only one project of a Blazor Web App, and
the palette in the other runs on the default options; it logs a warning saying the services are not registered. Call
`AddBitBaletteServices` in both, through one shared method.

**The palette opens unstyled.** The stylesheet link is missing: add
`<link rel="stylesheet" href="_content/Bit.Balette/bit-balette.css" />` to the host page.

**A toggle shows its old state.** Commands are immutable: register a new command with the same id, call
`Update`, or give `BaletteCommands` a new list.

**Recents forget a command whose title changed.** Its id was derived from the title; set an explicit `Id`.

**A page's commands are still listed after leaving it.** Dispose the handle `Register` returned, or use
`<BaletteCommands>`.

**A global shortcut does not fire in a text box.** That is deliberate for plain keys; use a chord with a modifier,
or set `SuppressShortcutsInEditableElements = false`.

**Two palettes open at once.** Place one `Balette` per scope; every instance listens to the same service.

**A command does nothing, or the wrong one runs.** Look at the app's log: the palette warns about duplicate ids, about
shortcuts several commands claim or the toggle takes, about links it refused, and logs every failed command and page.

**A command's shortcut works only after opening the palette.** Its `VisibleWhen` changed while the palette was closed:
call `Palette.Refresh()` after the state it reads changes.

**Commands passed to the `Commands` parameter do not update.** The palette re-reads the parameter when it gets a new
collection; assign a new list, or call `Palette.Refresh()` after changing the same one.

## Migration notes

Changes in behaviour since the first release:

- A handler exception raised from the palette's own UI is no longer rethrown, and the footer shows
  `BaletteStrings.CommandFailed` instead of the exception's message. To show the message, set
  `e.Message = e.Exception.Message` in `CommandFailed`. `ExecuteAsync` still rethrows unhandled failures.
- When two commands claim one global shortcut, the one registered last runs (it used to be the first).
- Nested commands are found from the root page (`SearchNestedCommands`), and a query that finds nothing is retried
  with typo tolerance (`TypoTolerance`); set either to `false` for the old behaviour.
- Matching ignores Latin diacritics and Arabic-script letter variants (`BaletteFuzzyMatcher.Normalize`).
- On a multi-select page options carry `aria-checked` only; `aria-selected` is no longer set on the active one.
- Toasts are announced by live regions inside the dialog; the footer's toast element no longer has a `role`. A
  `FooterTemplate` should render `Toast` without a live role of its own, or it is announced twice.
- `BaletteRowKind.Separator` is obsolete: the palette never rendered it, and a listbox may not contain separators.
- The palette re-reads its `Commands` parameter only when it is given a new collection (it used to re-enumerate it on
  every keystroke).
- The default theme's `--bit-balette-fg-muted`, `--bit-balette-fg-subtle` and `--bit-balette-danger` are darker in light
  mode, and `--bit-balette-fg-subtle` lighter in dark mode, to meet WCAG AA contrast.
