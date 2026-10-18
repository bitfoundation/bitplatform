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
- **Queries with operators**: several terms that must all match, plus `'exact`, `^prefix`, `suffix$` and `!negate`.
- **Ranking you can reason about**: weighted fields (title › keywords › subtitle › group › description › tags),
  then priority and frecency - how often and how recently a command was used.
- **Nested pages**: static children, lazily loaded children, nested markup, or a page a handler returns, with
  breadcrumbs and `Backspace` to go back.
- **Modes** by prefix (`>` commands, `#` issues, `@` people, `?` help) that scope the registry and the sources.
- **Async sources**: debounced, cancellable searches against your own APIs, either re-ranked with everything else
  or kept in the order the server returned.
- **Multi-step flows**: free-text prompts, multi-select with `Ctrl+Enter`, confirmation for destructive commands,
  toasts, and a Raycast-style action panel on `Tab`.
- **Recents and pins** persisted to `localStorage`, or to any store you plug in.
- **Global shortcuts**, including sequences such as `G I`, matched in the browser so typing never costs a round
  trip; platform-aware hints (⌘ on macOS, Ctrl elsewhere).
- **Accessibility as architecture**: a native modal `<dialog>`, the WAI-ARIA combobox and listbox pattern, a live
  region announcing result counts, focus restore, reduced motion.
- **Theming through CSS variables**, light and dark, eight templates, or a fully unstyled mode.
- Works on **Blazor WebAssembly, Blazor Server, Blazor Web App and Blazor Hybrid**, on .NET 8, 9 and 10, and is
  trimming and AOT friendly.

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

- .NET 8, .NET 9 or .NET 10
- Blazor WebAssembly, Blazor Server, Blazor Web App (any interactive render mode) or Blazor Hybrid
- A browser with the `<dialog>` element - every evergreen browser since 2022

## The demo

The demo site, [balette.bitplatform.dev](https://balette.bitplatform.dev), has one global palette and a page per
area: registering commands (fluent and declarative, page-scoped, toggles that stay in sync), nested pages and
multi-step flows, search and ranking with a live view of the matcher's scores, modes and async sources, the
keyboard, theming and templates, and a large-registry page that times a search over thousands of commands in your
browser.

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
API, this guide and the demo's working pages - instead of guessing at an API it half remembers. It is also served,
next to the other bit libraries, through the aggregated `https://bitplatform.dev/mcp` endpoint.

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
- [Options](#options)
- [Performance](#performance)
- [Troubleshooting](#troubleshooting)

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
Server, one per application on WebAssembly.

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

`BaletteGroup` applies its heading, and its priority and tags to the items that do not set their own.
`BaletteItem` has a parameter for every member of the model; `Keywords` is a comma or space separated string, and
nested items turn their parent into a page. `OnExecute` receives the `BaletteCommandContext`, and the palette closes
afterwards unless `KeepOpen` is set; an item with `Href` and no `OnExecute` navigates. An item only re-registers
when one of its parameters actually changed, so re-rendering the layout is cheap.

Markup commands belong to that palette instance, the service's registry to every palette in the scope, and the
palette's `Commands` parameter adds a third, per-instance list.

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
| `BaletteCommandResult.Toast(message, keepOpen)` | Show a message in the footer; closes shortly afterwards unless `keepOpen`. |

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
        return Task.FromResult(BaletteCommandResult.Toast($"{ctx.SelectedCommands.Count} labels applied"));
    });
```

On a multi-select page `Enter`, a click or `Space` (on an empty query) toggles a row, and `Ctrl+Enter` (⌘↩) runs the
page's handler with the checked commands. A page returned with `BaletteCommandResult.Page(..., multiSelect: true,
onSubmit: ...)` works the same way.

### Errors

An exception thrown by a handler raises `IBaletteService.CommandFailed`. Set `Handled = true` on its arguments to
keep the exception from propagating; the palette then shows its message in the footer. Cancellation is not an
error: a handler that throws `OperationCanceledException` simply stops.

## Nested pages

Selecting a command that has children opens a page listing them, with breadcrumbs and a back button. `Backspace` or
`←` on an empty query and `Esc` go back; the query typed before the page opened comes back with it.

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

The page opens immediately with a loading state and fills in when the provider returns.

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
are kept and highlighted with `<mark>`.

### Query operators

A query is split on spaces and every term must match some field of the command:

| Term | Meaning |
| --- | --- |
| `set the` | Fuzzy, both terms. |
| `'wrap` | Exact substring. |
| `^go` | Prefix of a field. |
| `ings$` | Suffix of a field. |
| `!delete` | Excludes commands containing "delete" in the title, subtitle, group or keywords. |

### Field weights

Each term scores against every field and keeps the best weighted score: title 1.0, keywords 0.9, subtitle 0.7,
group 0.5, description 0.4, tags 0.3. Change them through `options.Weights`:

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
the positions to highlight - and set `options.Matcher`. `BaletteSubstringMatcher` is a ready-made case-insensitive
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
icon while any source is pending. A source that throws is treated as having found nothing.

With `ApplyMatcher = true` the results are scored by the palette's matcher and interleaved with the registry by
rank. With `false` - the default - the source's own order is kept and shown as a block under its name, which is
right when the server already ranked by relevance.

Sources are queried in the default mode when added to `options.Sources`, registered at runtime with
`Palette.RegisterSource`, or resolved from DI with `services.AddBitBaletteSource<TSource>()`. A mode queries its own
`Sources` instead. Two ready-made sources cover the common cases: `BaletteDelegateSource` wraps a delegate, and
`BaletteStaticSource` serves a large in-memory list through the matcher, without registering thousands of
commands.

## Recents, pins and the action panel

On the empty query of the root page, pinned commands are listed first and the most recently used ones (up to
`options.MaxRecents`, 5) next. Every run is recorded unless the command has `ExcludeFromHistory`, and the counts
feed [frecency](#priority-and-frecency).

`Tab` (or `→` on an empty query) opens the action panel of the active command: its own `Actions`, then "Pin to top"
or "Unpin", and "Remove from recents" when it has been used. `Ctrl+Enter` runs the first secondary action directly,
`Mod+Shift+P` pins or unpins and `Shift+Delete` forgets.

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
| `Ctrl+Enter` (⌘↩) | Apply a multi-select page, or run the first secondary action. |
| `Tab`, `→` on an empty query | The action panel. |
| `Backspace`, `←` on an empty query | Back one page. |
| `Esc` | Disarm a confirmation, then go back, then close. |
| `Mod+Shift+P` | Pin or unpin the active command. |
| `Shift+Delete` | Remove the active command from the recents. |

These keys are claimed in the browser before Blazor sees them, so `Tab` never leaves the dialog and `Backspace`
never deletes text it should not, even over a slow Blazor Server connection.

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
`Enter` confirms that command and no other.

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
| `Update(command)` | Replace the command with the same id, or add it. |
| `Unregister(id)`, `Find(id)` | Remove or look up one command, nested ones included. |
| `RegisterMode(mode)`, `RegisterSource(source)` | Add a mode or a default-mode source at runtime; dispose to remove. |
| `OpenAsync(query, pageCommandId)`, `CloseAsync()`, `ToggleAsync()` | Drive the palette. |
| `ExecuteAsync(id)` | Run a command as if it had been selected - registered, declared in markup or passed to `Commands` - opening the palette when it needs it. |
| `IsOpen`, `IsMac`, `Commands`, `Modes`, `Sources` | State. |
| `CommandsChanged`, `Opened`, `Closed`, `CommandExecuted`, `CommandFailed`, `PlatformDetected` | Events. |

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
| `--bit-balette-fg`, `--bit-balette-fg-muted`, `--bit-balette-fg-subtle` | `#111318`, `#6b7280`, `#9ca3af` | Text. |
| `--bit-balette-border` | `rgba(17, 19, 24, 0.08)` | Dividers. |
| `--bit-balette-accent`, `--bit-balette-accent-fg`, `--bit-balette-accent-soft` | `#4f46e5`, ... | Highlights, badges, the mode chip. |
| `--bit-balette-active-bg` | `rgba(17, 19, 24, 0.06)` | The active row. |
| `--bit-balette-danger`, `--bit-balette-danger-soft` | `#dc2626`, ... | Destructive commands. |
| `--bit-balette-mark-bg` | `rgba(79, 70, 229, 0.18)` | Highlighted characters. |
| `--bit-balette-kbd-bg`, `--bit-balette-kbd-fg`, `--bit-balette-kbd-border` | `#f3f4f6`, ... | Key caps. |
| `--bit-balette-backdrop`, `--bit-balette-shadow`, `--bit-balette-scrollbar` | ... | The rest. |

Override them globally, on `dialog[data-balette="dialog"]`, or inline:

```razor
<Balette Style="--bit-balette-width: 760px; --bit-balette-accent: #0f766e" />
```

### Light and dark

The palette follows `prefers-color-scheme`, `<html data-theme="light">` or `<html data-theme="dark">` forces one,
and the component's `Theme` parameter (`auto`, `light`, `dark`; `options.Theme` by default) forces it for the dialog
alone. Animations are dropped under `prefers-reduced-motion`, and below 640px the dialog fills the screen and hides
the hints and the detail pane.

### Unstyled

`Unstyled="true"` drops the `bit-balette-dialog` class every rule is scoped to, leaving bare markup with stable
`data-balette` attributes - `dialog`, `root`, `main`, `header`, `breadcrumbs`, `crumb`, `mode`, `input-row`,
`input`, `spinner`, `selected-count`, `close`, `list`, `group-heading`, `item`, `label`, `mark`, `badge`, `shortcut`,
`empty`, `loading`, `prompt`, `footer`, `toast`, `detail`, and `trigger` on the trigger button - and state attributes
on the rows: `data-active`, `data-checked`, `data-disabled`, `data-destructive`, `data-confirming`, `data-kind` and `data-value`
(the command's id). Those attributes are also the stable hooks for tests.

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

Every label and announcement is a property of `BaletteStrings`: set `options.Strings`, or the palette's `Strings`
parameter, to an instance with your texts, or to a subclass that reads them from an `IStringLocalizer`.

```csharp
options.Strings = new BaletteStrings
{
    Placeholder = "Befehl eingeben oder suchen…",
    NoResults = "Keine Ergebnisse für „{0}“",
    ResultsAnnouncement = "{0} Ergebnisse verfügbar",
};
```

## Accessibility

- The palette is a native modal `<dialog>`: it sits in the top layer, traps focus, makes the page behind it inert,
  locks scrolling, and gives focus back to whatever had it when it closes.
- Focus stays on the input, which is a `combobox` controlling a `listbox`; `aria-activedescendant` points at the
  active `option`, which carries `aria-selected`, and multi-select pages add `aria-multiselectable` and
  `aria-checked` - the WAI-ARIA combobox pattern, so screen readers follow the selection without focus moving.
- A polite live region announces the result count after every search, the empty state and loading.
- Disabled commands are `aria-disabled` and say why; group headings are presentational, so they are not read as
  options.
- Shortcut hints have an accessible name in words ("Ctrl+K", "G then S"), and the trigger declares
  `aria-keyshortcuts`.
- Pointer hover only moves the selection once the pointer actually moves, so a list scrolling under a resting mouse
  does not steal the keyboard's selection (`DisablePointerSelection` turns hover off completely).

## Hosting models

### Blazor WebAssembly

Register the services in `Program.cs`, link the stylesheet in `index.html`, and place `<Balette />` in
`MainLayout.razor`. Searching runs on the browser's one thread, so on every keystroke whose search took longer than
a frame the palette lets the keystroke paint first and folds keys typed meanwhile into one search - typing stays
responsive with thousands of commands.

### Blazor Server

The same setup. Shortcuts and navigation keys are handled in the browser, so only the query itself and the
selected command cross the connection. Handlers run on the server, in the circuit's scope.

### Blazor Web App

Register the services in **both** projects - every page is prerendered on the server before it runs in the browser,
so anything a component injects must resolve in both containers - and link the stylesheet in `App.razor`. The
palette needs an interactive render mode:

- With global interactivity (`<Routes @rendermode="InteractiveWebAssembly" />` or `InteractiveServer` or
  `InteractiveAuto`), put it in `MainLayout.razor`.
- With per-page interactivity the layout is static: wrap the palette in a small component rendered with an
  interactive mode, and register commands from components that run in the same mode, because the registry is
  scoped to one circuit or one WebAssembly runtime.

While prerendering, the palette renders a closed dialog; it attaches its keyboard handling once it is interactive.

### Blazor Hybrid

Works unchanged in a MAUI, WPF or Windows Forms `BlazorWebView`; history goes to the web view's `localStorage`.

### Content security policy

The browser layer is a JavaScript module loaded from `_content/Bit.Balette/bit-balette.js` with `import()`; there
are no inline scripts and no `eval`, so `script-src 'self'` is enough.

## Options

| Option | Default | Notes |
| --- | --- | --- |
| `ToggleShortcut` | `Mod+K` | Plus `AdditionalToggleShortcuts`. |
| `SuppressShortcutsInEditableElements` | `true` | Plain keys do not fire from inputs. |
| `SequenceTimeoutMs` | `1000` | How long a sequence waits for its next chord. |
| `DebounceMs` | `150` | Async sources. |
| `Loop`, `VimBindings` | `true`, `false` | Arrow navigation. |
| `EnableHistory`, `MaxRecents`, `MaxHistoryEntries` | `true`, `5`, `200` | |
| `HistoryStorageKey` | `bit-balette.history` | The `localStorage` key. |
| `GroupWhenSearching` | `true` | A flat ranked list when `false`. |
| `MaxResults` | `150` | Rows kept per query, groups excluded. |
| `GroupOrder` | empty | Explicit group order. |
| `PriorityWeight`, `FrecencyWeight` | `0.25`, `6` | Ranking nudges while searching. |
| `ShowModeHints` | `true` | The "Modes" group on the empty query. |
| `CloseOnBackdropClick`, `CloseOnNavigation`, `ResetOnClose` | `true` | |
| `HighlightMatches`, `ShowFooter`, `ShowShortcuts` | `true` | |
| `Virtualize`, `RowHeight` | `false`, `44` | Recommended above about 500 visible rows; while on, rows are `RowHeight` pixels tall whatever `--bit-balette-row-height` says. |
| `Matcher`, `Weights` | `BaletteFuzzyMatcher.Default` | Pluggable. |
| `Strings` | English | See [localization](#localization). |
| `Commands`, `Modes`, `Sources` | empty | Registered at startup. |
| `Theme` | `auto` | `light` or `dark` to force one. |

Most display options can be overridden per palette with the parameter of the same name.

## Performance

A palette runs its search on every keystroke, so the hot path is written for it:

- The query is parsed into terms once per keystroke, not once per command.
- The matcher allocates nothing per field: it reuses per-thread buffers, rejects non-subsequences before the
  dynamic programming starts, and avoids LINQ.
- Ranking only sorts what it keeps: with `MaxResults` it holds the best rows in a heap and sorts those.
- Results from async sources reuse the registry's matches for the same query instead of matching again.
- On WebAssembly a search that took longer than a frame lets the keystroke paint first and folds fast typing into
  one search; navigation keys still act on the latest query.

The benchmarks in `Tests/Bit.Balette.Benchmarks` measure the matcher, the ranker and a whole keystroke over
registries of different sizes. With very large lists, also turn on `Virtualize`, or serve the data through a
`BaletteStaticSource` instead of registering every row.

## Troubleshooting

**`Ctrl+K` does nothing.** The palette is not interactive: it is in a statically rendered layout, or the services
are registered in only one project of a Blazor Web App. Also check that no other script calls `preventDefault` on
the key first.

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
