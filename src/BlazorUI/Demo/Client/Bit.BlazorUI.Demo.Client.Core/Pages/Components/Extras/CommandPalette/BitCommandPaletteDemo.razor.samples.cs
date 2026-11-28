namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CommandPalette;

public partial class BitCommandPaletteDemo
{
    private readonly string example1RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" OnCommandExecuted=""e => aLastRun = e.Command.Title"">
    <BitCommandPaletteGroup Heading=""Navigation"">
        <BitCommandPaletteOption Title=""Go to home"" Keywords=""start, dashboard"" />
        <BitCommandPaletteOption Title=""Go to settings"" Keywords=""preferences, options"" />
        <BitCommandPaletteOption Title=""Go to profile"" Subtitle=""Your account"" />
    </BitCommandPaletteGroup>
    <BitCommandPaletteGroup Heading=""Actions"">
        <BitCommandPaletteOption Title=""Create a document"" Badge=""New"" />
        <BitCommandPaletteOption Title=""Invite a teammate"" Keywords=""share, add people"" />
        <BitCommandPaletteOption Title=""Archive the project"" Disabled DisabledReason=""Only owners can archive"" />
    </BitCommandPaletteGroup>
</BitCommandPalette>

<BitText>Last run: <b>@aLastRun</b></BitText>

@code {
    private string? aLastRun;
}";

    private readonly string example2RazorCode = @"
@* File: App.razor (or wwwroot/index.html) - the Extras stylesheet and script, next to the core ones *@
<link href=""_content/Bit.BlazorUI.Extras/styles/bit.blazorui.extras.css"" rel=""stylesheet"" />
<script src=""_content/Bit.BlazorUI.Extras/scripts/bit.blazorui.extras.js""></script>

@* File: MainLayout.razor - one palette, rendered interactively on every page *@
@inherits LayoutComponentBase

<header>
    <BitCommandPaletteTrigger Label=""Search…"" />
</header>

<main>@Body</main>

<BitCommandPalette />

@* File: any page or component - registers its own commands, which leave with it *@
@inject IBitCommandPaletteService Palette
@inject ProjectsApi Api
@implements IDisposable

@code {
    private IDisposable? _commands;
    private bool _sidebarOpen;

    protected override void OnInitialized()
    {
        _commands = Palette.Register(
            BitCommandPaletteCmd.Navigate(""Home"", ""/"").Group(""Navigation"").Shortcut(""G H""),
            BitCommandPaletteCmd.Navigate(""Settings"", ""/settings"").Group(""Navigation"").Shortcut(""G S"").Keywords(""preferences"", ""options""),
            BitCommandPaletteCmd.Action(""Toggle sidebar"", () => _sidebarOpen = !_sidebarOpen).Group(""View"").Shortcut(""Mod+B""),
            BitCommandPaletteCmd.New(""Theme"").Group(""Preferences"").Children(
                BitCommandPaletteCmd.Action(""Light"", () => SetTheme(""light"")),
                BitCommandPaletteCmd.Action(""Dark"", () => SetTheme(""dark""))),
            BitCommandPaletteCmd.New(""Delete project"")
                .Group(""Danger zone"")
                .Destructive(""This cannot be undone"")
                .Handler(async ctx =>
                {
                    await Api.DeleteProjectAsync(ctx.CancellationToken);
                    return BitCommandPaletteCommandResult.Toast(""Project deleted"");
                }));
    }

    private void SetTheme(string theme) { /* ... */ }

    public void Dispose() => _commands?.Dispose();
}";
    private readonly string example2CsharpCode = @"
// File: Program.cs - in a Blazor Web App, in both the server and the client project (one shared method)
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    // Ctrl+K / Cmd+K is the default; this adds VS Code's Ctrl+Shift+P as well.
    options.AdditionalToggleShortcuts.Add(""Mod+Shift+P"");

    // Groups appear in this order on the empty query; any others follow alphabetically.
    options.GroupOrder.AddRange([""Navigation"", ""Actions"", ""Preferences"", ""Help""]);
});

// File: any component with IBitCommandPaletteService injected as Palette - opening it from code
await Palette.OpenAsync();
await Palette.OpenAsync("">"");                                // commands mode
await Palette.OpenAsync(pageCommandId: ""preferences.theme""); // straight to the Theme page
await Palette.ExecuteAsync(""navigation.settings"");           // run a command, nothing opened";

    private readonly string example3RazorCode = @"
@* File: PaletteHost.razor - per-page interactivity: a static layout renders it as <PaletteHost @rendermode=""InteractiveAuto"" /> *@
<BitCommandPalette />
<BitCommandPaletteCommands Commands=""_appCommands"" />

@code {
    // Registered from inside the interactive component, so they land in the same scope as the palette.
    private readonly List<BitCommandPaletteCommand> _appCommands = [ /* ... */ ];
}

@* File: YourApp.Client.csproj - lazy loading: mark the assembly in the project that runs in the browser *@
<ItemGroup>
  <BlazorWebAssemblyLazyLoad Include=""Bit.BlazorUI.Extras.wasm"" />
</ItemGroup>

@* File: App.razor (Routes.razor in a Blazor Web App) - the router waits for OnNavigateAsync before it renders the page *@
@using Microsoft.AspNetCore.Components.WebAssembly.Services
@inject LazyAssemblyLoader LazyAssemblyLoader

<Router AppAssembly=""typeof(App).Assembly"" OnNavigateAsync=""OnNavigateAsync"">
    ...
</Router>

@code {
    private async Task OnNavigateAsync(NavigationContext context)
    {
        // The pages with the palette: fetch Bit.BlazorUI.Extras before they render. A second call loads nothing again.
        // Routes match regardless of case, so /Admin/users is one of them too.
        if (context.Path.StartsWith(""admin"", StringComparison.OrdinalIgnoreCase))
            await LazyAssemblyLoader.LoadAssembliesAsync([""Bit.BlazorUI.Extras.wasm""]);
    }
}

@* File: AdminLayout.razor - only the lazily loaded pages use it, so it may use the palette's types *@
@inherits LayoutComponentBase
@inject IServiceProvider Services
@implements IDisposable

<BitCommandPaletteTrigger Label=""Search…"" />
@Body
<BitCommandPalette />

@code {
    private IDisposable? _commands;

    protected override void OnInitialized()
        => _commands = Services.GetBitCommandPaletteService().Register(
            BitCommandPaletteCmd.Navigate(""Users"", ""/admin/users"").Group(""Navigation""),
            BitCommandPaletteCmd.Navigate(""Billing"", ""/admin/billing"").Group(""Navigation""));

    public void Dispose() => _commands?.Dispose();
}";
    private readonly string example3CsharpCode = @"
// File: Program.cs of the browser project, when it lazy loads Bit.BlazorUI.Extras
builder.Services.AddBitBlazorUIServices(); // not AddBitBlazorUIExtrasServices

// The one ""services are not registered"" warning is expected here; this silences it without naming the palette's types.
builder.Logging.AddFilter(""Bit.BlazorUI.BitCommandPaletteServiceCollectionExtensions"", LogLevel.None);

// File: Program.cs of the server - a content security policy; the palette needs nothing beyond script-src 'self'
// ('wasm-unsafe-eval' is for Blazor WebAssembly itself).
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy = ""script-src 'self' 'wasm-unsafe-eval'"";
    await next();
});";

    private readonly string example4RazorCode = @"
@inject IBitCommandPaletteService paletteService
@implements IDisposable

<BitCommandPaletteTrigger />
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => aModalOpen = true"">Open by binding Open</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""AOpenWithQuery"">Open with ""hello"" typed</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ARunSayHello"">Run ""Say hello"" by its id</BitButton>

<BitToggle Label=""Toggle with Alt+K instead of Ctrl+K"" @bind-Value=""aAltToggle"" />

<BitCommandPalette @bind-Open=""aModalOpen""
                   Commands=""aModalCommands""
                   ToggleShortcut=""@aToggleShortcut""
                   OnOpened=""() => aOpenedCount++""
                   OnClosed=""() => aClosedCount++"" />

<BitText>
    Open: <b>@aModalOpen</b>, opened <b>@aOpenedCount</b> and closed <b>@aClosedCount</b> times, last run: <b>@aModalLastRun</b>
</BitText>";
    private readonly string example4CsharpCode = @"
private bool aModalOpen;
private bool aAltToggle;
private int aOpenedCount;
private int aClosedCount;
private string? aModalLastRun;
private BitCommandPaletteCommand[] aModalCommands = default!;
private readonly List<IDisposable> registrations = [];

// null leaves the toggle shortcut to the options: Mod+K.
private string? aToggleShortcut => aAltToggle ? ""Alt+K"" : null;

protected override void OnInitialized()
{
    aModalCommands =
    [
        BitCommandPaletteCmd.Action(""Say hello"", () => aModalLastRun = ""Say hello"")
            .Id(""a-say-hello"").Group(""Actions"").Keywords(""greet"", ""hi""),
        BitCommandPaletteCmd.Page(""Choose a greeting"",
            BitCommandPaletteCmd.Action(""Hola"", () => aModalLastRun = ""Hola""),
            BitCommandPaletteCmd.Action(""Bonjour"", () => aModalLastRun = ""Bonjour""),
            BitCommandPaletteCmd.Action(""Salam"", () => aModalLastRun = ""Salam""))
            .Id(""a-greetings"").Group(""Actions""),
        BitCommandPaletteCmd.Navigate(""Go to getting started"", ""/getting-started"").Group(""Navigation""),
    ];

    // Registered with the service rather than passed to the palette: the modal palette lists it all the same.
    // Register returns an IDisposable - dispose it to remove the command again.
    registrations.Add(paletteService.Register(
        BitCommandPaletteCmd.Action(""Wave goodbye"", () => aModalLastRun = ""Wave goodbye"")
            .Id(""a-wave-goodbye"").Group(""Registered through the service"")));
}

private Task AOpenWithQuery() => paletteService.OpenAsync(""hello"");

private Task ARunSayHello() => paletteService.ExecuteAsync(""a-say-hello"");

public void Dispose()
{
    foreach (var registration in registrations) registration.Dispose();
}";

    private readonly string example5RazorCode = @"
@* Each of these opens the page's modal palette (see the Modal section). *@

<BitCommandPaletteTrigger Label=""Search the docs…"" />
<BitCommandPaletteTrigger Label=""Find a greeting"" Query=""greet"" />
<BitCommandPaletteTrigger Label=""Choose a greeting"" PageCommandId=""a-greetings"" ShowShortcut=""false"" />

@* An icon only: Label="""" names it ""Open command palette"". *@
<BitCommandPaletteTrigger Label="""">
    <BitIcon IconName=""@BitIconName.Search"" />
</BitCommandPaletteTrigger>

@* A word, given as Label too: named ""Greetings, Open command palette"". *@
<BitCommandPaletteTrigger Label=""Greetings"" Query=""greet"">
    <b>Greetings</b>
</BitCommandPaletteTrigger>

@* No Label: named by its content, then a visually hidden "", Open command palette"". *@
<BitCommandPaletteTrigger Query=""hello"">
    Say <b>hello</b>
</BitCommandPaletteTrigger>

<BitCommandPaletteTrigger Label=""Mac keys"" MacKeys=""true"" />
<BitCommandPaletteTrigger Label=""Windows and Linux keys"" MacKeys=""false"" />";

    private readonly string example6RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""bCommands"" />

<BitButton Variant=""BitVariant.Outline"" OnClick=""BToggleHello"">@(bHello is null ? ""Register \""Say hello\"""" : ""Dispose the registration"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick='() => paletteService.OpenAsync(""hello"")'>Search ""hello"" in the modal palette</BitButton>

<BitText>Last run: <b>@bLastRun</b> · ""Say hello"" is <b>@(paletteService.Find(BHelloId) is null ? ""not registered"" : ""registered"")</b></BitText>";
    private readonly string example6CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private const string BHelloId = ""b-say-hello"";

private string bLastRun = ""Nothing yet"";
private IDisposable? bHello;
private BitCommandPaletteCommand[] bCommands = [];

protected override void OnInitialized()
{
    // Icon takes a RenderFragment as well as a CSS class: any markup, here an inline SVG drawn in the row's color.
    RenderFragment star = builder => builder.AddMarkupContent(0, ""<svg viewBox=\""0 0 16 16\"" width=\""14\"" height=\""14\"" fill=\""currentColor\""><path d=\""M8 1.5l1.9 4 4.4.5-3.3 3 .9 4.3L8 11.1l-3.9 2.2.9-4.3-3.3-3 4.4-.5z\"" /></svg>"");

    bCommands =
    [
        BitCommandPaletteCmd.New(""Export…"")
            .Group(""Builder"")
            .Subtitle(""A blank command, filled in step by step"")
            .Description(""New starts a command from its title; every other member has a builder method of the same name."")
            .Keywords(""download"", ""save as"")
            .Icon(""bit-icon bit-icon--Download"")
            .Handler(() => BRun(""Export…"")),
        BitCommandPaletteCmd.Action(""Save"", () => BRun(""Save""))
            .Group(""Builder"")
            .Icon(""bit-icon bit-icon--Save"")
            .Shortcut(""Mod+S"", global: false),
        BitCommandPaletteCmd.Action(""Reload"", BReloadAsync)
            .Group(""Builder"")
            .Icon(""bit-icon bit-icon--Refresh""),
        BitCommandPaletteCmd.Action(""Rename…"", ctx =>
            {
                BRun($""Rename… (typed \""{ctx.Query}\"")"");
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            })
            .Group(""Builder"")
            .Icon(""bit-icon bit-icon--Rename""),
        BitCommandPaletteCmd.Page(""Theme"",
                BitCommandPaletteCmd.Action(""Light"", () => BRun(""Theme › Light"")).Id(""b-builder-light""),
                BitCommandPaletteCmd.Action(""Dark"", () => BRun(""Theme › Dark"")).Id(""b-builder-dark""))
            .Group(""Builder"")
            .ChildrenPlaceholder(""Pick a theme…""),
        BitCommandPaletteCmd.Navigate(""Getting started"", ""/getting-started"")
            .Group(""Links"")
            .Keywords(""install"", ""setup""),
        BitCommandPaletteCmd.New(""bit platform on GitHub"")
            .Href(""https://github.com/bitfoundation/bitplatform"", newTab: true)
            .Group(""Links"")
            .Keywords(""source"", ""repository""),
        BitCommandPaletteCmd.New(""What's new"")
            .Group(""Links"")
            .Badge(""New"")
            .Icon(star)
            .Handler(() => BRun(""What's new""))
            .Kind(BitCommandPaletteCommandKind.Help),
    ];
}

private void BRun(string title)
{
    bLastRun = title;
    _ = InvokeAsync(StateHasChanged);
}

private async Task BReloadAsync()
{
    await Task.Delay(600);
    BRun(""Reload (after 600 ms)"");
}

private void BToggleHello()
{
    if (bHello is not null)
    {
        bHello.Dispose(); // gone again, from every palette
        bHello = null;
        return;
    }

    bHello = paletteService.Register(
        BitCommandPaletteCmd.Action(""Say hello"", () => BRun(""Say hello (registered from code)""))
            .Id(BHelloId)
            .Group(""Registered from code"")
            .Keywords(""wave"", ""hi""));
}

public void Dispose() => bHello?.Dispose();";

    private readonly string example7RazorCode = @"
<table class=""b-table"">
    <thead>
        <tr><th>Built as</th><th>Id</th></tr>
    </thead>
    <tbody>
        @foreach (var (built, id) in bIdentityExamples)
        {
            <tr><td><code>@built</code></td><td><code>@id</code></td></tr>
        }
    </tbody>
</table>

<BitButton Variant=""BitVariant.Outline"" OnClick=""BToggleGreetOverride"">@(bGreetOverride is null ? ""Register an override with the same id"" : ""Dispose the override"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick='() => paletteService.OpenAsync(""greet"")'>Search ""greet"" in the modal palette</BitButton>

<BitText>Find(""b-greet""): <b>@(paletteService.Find(BGreetId)?.Title)</b> · Last run: <b>@bGreetStatus</b></BitText>";
    private readonly string example7CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private const string BGreetId = ""b-greet"";

private string bGreetStatus = ""Nothing yet"";
private IDisposable? bGreet;
private IDisposable? bGreetOverride;

// Worked out by the library itself, so the table cannot drift from what it does.
private static readonly (string Built, string Id)[] bIdentityExamples =
[
    (""New(\""Settings\"").Group(\""Navigation\"")"", BitCommandPaletteCmd.New(""Settings"").Group(""Navigation"").Build().Id),
    (""New(\""Rename project…\"").Group(\""Actions\"")"", BitCommandPaletteCmd.New(""Rename project…"").Group(""Actions"").Build().Id),
    (""New(\""What's new\"").Group(\""Help\"")"", BitCommandPaletteCmd.New(""What's new"").Group(""Help"").Build().Id),
    (""New(\""Dark\"").Parent(\""preferences.theme\"")"", BitCommandPaletteCmd.New(""Dark"").Parent(""preferences.theme"").Build().Id),
    (""New(\""C#\"")"", BitCommandPaletteCmd.New(""C#"").Build().Id),
    (""New(\""C++\"")"", BitCommandPaletteCmd.New(""C++"").Build().Id),
    (""New(\""🚀\"")"", BitCommandPaletteCmd.New(""🚀"").Build().Id),
    (""New(\""Word wrap\"").Id(\""toggle:wordwrap\"")"", BitCommandPaletteCmd.New(""Word wrap"").Id(""toggle:wordwrap"").Build().Id),
    (""New(\""Word wrap\"").Group(\""Editor\"").Build().With(b => b.Title(\""Soft wrap\""))"", BitCommandPaletteCmd.New(""Word wrap"").Group(""Editor"").Build().With(b => b.Title(""Soft wrap"")).Id),
];

protected override void OnInitialized()
{
    bGreet = paletteService.Register(
        BitCommandPaletteCmd.Action(""Greet the team"", () => BGreet(""the first registration""))
            .Id(BGreetId)
            .Group(""Identity"")
            .Keywords(""welcome""));
}

private void BToggleGreetOverride()
{
    if (bGreetOverride is not null)
    {
        bGreetOverride.Dispose(); // the first registration's command is back
        bGreetOverride = null;
        return;
    }

    // The same id, registered later: it wins.
    bGreetOverride = paletteService.Register(
        BitCommandPaletteCmd.Action(""Greet this page's visitors"", () => BGreet(""the override""))
            .Id(BGreetId)
            .Group(""Identity"")
            .Keywords(""welcome""));
}

private void BGreet(string which)
{
    bGreetStatus = which;
    _ = InvokeAsync(StateHasChanged);
}

public void Dispose()
{
    bGreetOverride?.Dispose();
    bGreet?.Dispose();
}";

    private readonly string example8RazorCode = @"
<BitToggle @bind-Value=""bGroupWhenSearching"" Label=""Group the results while searching"" />

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   GroupWhenSearching=""bGroupWhenSearching""
                   Commands=""bOrderingCommands"" />";
    private readonly string example8CsharpCode = @"
// Program.cs: the app-wide order of the groups, and how a search is listed.
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    // First on the empty query, in this order; the other groups follow.
    options.GroupOrder.AddRange([""Edit"", ""View"", ""Help""]);

    // While searching: one flat list ranked by match, instead of a heading per group.
    options.GroupWhenSearching = false;
});

// The page:
private bool bGroupWhenSearching = true;

private static readonly BitCommandPaletteCommand[] bOrderingCommands =
[
    BitCommandPaletteCmd.New(""Search everywhere""),
    BitCommandPaletteCmd.New(""Zoom in"").Group(""View""),
    BitCommandPaletteCmd.New(""Zoom out"").Group(""View""),
    BitCommandPaletteCmd.New(""Reset zoom"").Group(""View"").Priority(BitCommandPaletteCommandPriority.Low),
    BitCommandPaletteCmd.New(""Cut"").Group(""Edit""),
    BitCommandPaletteCmd.New(""Copy"").Group(""Edit""),
    BitCommandPaletteCmd.New(""Paste"").Group(""Edit""),
    BitCommandPaletteCmd.New(""Undo"").Group(""Edit"").Priority(BitCommandPaletteCommandPriority.High),
    BitCommandPaletteCmd.New(""Send feedback"").Group(""Help"").Priority(BitCommandPaletteCommandPriority.Lowest).AlwaysShow(),
    BitCommandPaletteCmd.New(""Keyboard shortcuts"").Group(""Help""),
];";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""bWordWrap"" @bind-Value:after=""BBuildSettings"" Label=""Word wrap"" />
<BitToggle @bind-Value=""bMinimap"" @bind-Value:after=""BBuildSettings"" Label=""Show minimap"" />
@foreach (var theme in bEditorThemes)
{
    <BitButton Variant=""@(bEditorTheme == theme ? BitVariant.Fill : BitVariant.Outline)"" OnClick=""() => BSetEditorTheme(theme)"">@theme</BitButton>
}

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""bSettings"" />

<BitButton Variant=""BitVariant.Outline"" OnClick=""BToggleFocusMode"">@(bFocusMode ? ""Leave focus mode"" : ""Enter focus mode"") from the page</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick='() => paletteService.OpenAsync(""focus"")'>Search ""focus"" in the modal palette</BitButton>

<BitText>Registered as <code>b-focus-mode</code>: <b>@(paletteService.Find(BFocusModeId)?.Title)</b></BitText>";
    private readonly string example9CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private const string BFocusModeId = ""b-focus-mode"";
private static readonly string[] bEditorThemes = [""Light"", ""Dark"", ""High contrast""];

private bool bWordWrap = true;
private bool bMinimap;
private string bEditorTheme = ""Light"";
private bool bFocusMode;
private IDisposable? bFocusModeRegistration;
private BitCommandPaletteCommand[] bSettings = [];

protected override void OnInitialized()
{
    BBuildSettings();
    bFocusModeRegistration = paletteService.Register(BFocusModeCommand());
}

// A new collection every time, so the palette re-reads it and every switch and value is current.
private void BBuildSettings()
{
    bSettings =
    [
        BitCommandPaletteCmd.New(""Word wrap"")
            .Id(""b-setting-wordwrap"")
            .Group(""Editor"")
            .Keywords(""soft wrap"", ""lines"")
            .Checked(bWordWrap)
            .Handler(_ =>
            {
                bWordWrap = !bWordWrap;
                BSettingsChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            }),
        BitCommandPaletteCmd.New(""Show minimap"")
            .Id(""b-setting-minimap"")
            .Group(""Editor"")
            .Keywords(""outline"", ""overview"")
            .Checked(bMinimap)
            .Handler(_ =>
            {
                bMinimap = !bMinimap;
                BSettingsChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            }),
        BitCommandPaletteCmd.New(""Editor theme"")
            .Id(""b-setting-theme"")
            .Group(""Editor"")
            .Keywords(""appearance"", ""color scheme"")
            .Value(bEditorTheme)
            .ChildrenPlaceholder(""Pick a theme…"")
            .Children(bEditorThemes.Select(BEditorThemeOption)),
    ];
}

private BitCommandPaletteCommand BEditorThemeOption(string theme) =>
    BitCommandPaletteCmd.New(theme)
        .Id($""b-editor-theme-{theme.ToLowerInvariant().Replace(' ', '-')}"")
        .Checked(bEditorTheme == theme)
        .Handler(_ =>
        {
            bEditorTheme = theme;
            BSettingsChanged();
            return Task.FromResult(BitCommandPaletteCommandResult.Back);
        })
        .Build();

private void BSetEditorTheme(string theme)
{
    bEditorTheme = theme;
    BBuildSettings();
}

private void BSettingsChanged()
{
    BBuildSettings();
    _ = InvokeAsync(StateHasChanged);
}

// The title changes with the state; the id does not.
private BitCommandPaletteCommand BFocusModeCommand() =>
    BitCommandPaletteCmd.New(bFocusMode ? ""Leave focus mode"" : ""Enter focus mode"")
        .Id(BFocusModeId)
        .Group(""Editor"")
        .Keywords(""zen"", ""distraction free"")
        .Checked(bFocusMode)
        .Handler(_ =>
        {
            BToggleFocusMode();
            return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
        });

private void BToggleFocusMode()
{
    bFocusMode = !bFocusMode;
    paletteService.Update(BFocusModeCommand()); // replaces the command with the id b-focus-mode
    _ = InvokeAsync(StateHasChanged);
}

public void Dispose() => bFocusModeRegistration?.Dispose();";

    private readonly string example10RazorCode = @"
@foreach (var file in bFiles)
{
    <BitButton Variant=""@(bSelectedFiles.Contains(file) ? BitVariant.Fill : BitVariant.Outline)"" OnClick=""() => BToggleFile(file)"">@file</BitButton>
}

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""bAvailabilityCommands"" />

<BitButton Variant=""BitVariant.Outline"" OnClick='() => paletteService.OpenAsync(""merge"")'>Search ""merge"" in the modal palette</BitButton>

<BitText>Last run: <b>@bAvailabilityStatus</b></BitText>";
    private readonly string example10CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private static readonly string[] bFiles = [""README.md"", ""Program.cs"", ""App.razor"", ""site.css""];

private readonly HashSet<string> bSelectedFiles = [];
private string bAvailabilityStatus = ""Nothing yet"";
private IDisposable? bMergeRegistration;
private BitCommandPaletteCommand[] bAvailabilityCommands = [];

protected override void OnInitialized()
{
    // Registered with the service, so its global shortcut works while the modal palette is closed.
    bMergeRegistration = paletteService.Register(BMergeCommand());

    bAvailabilityCommands =
    [
        BMergeCommand(),
        BitCommandPaletteCmd.New(""Export as PDF"")
            .Group(""Files"")
            .Badge(""Pro"")
            .Icon(""bit-icon bit-icon--Download"")
            .Disabled(true, ""Requires the Pro plan"")
            .Handler(() => BAvailabilityRun(""Exported as PDF"")),
        BitCommandPaletteCmd.New(""Publish the draft"")
            .Group(""Files"")
            .Confirm(""Publish the draft to everyone?"")
            .Handler(() => BAvailabilityRun(""Published the draft (not really)"")),
        BitCommandPaletteCmd.New(""Delete the draft"")
            .Group(""Danger zone"")
            .Icon(""bit-icon bit-icon--Delete"")
            .Priority(BitCommandPaletteCommandPriority.Low)
            .Destructive(""This permanently deletes the draft"")
            .Handler(() => BAvailabilityRun(""Deleted the draft (not really)"")),
    ];
}

private BitCommandPaletteCommand BMergeCommand() =>
    BitCommandPaletteCmd.New(""Merge selected files"")
        .Id(""b-merge-files"")
        .Group(""Files"")
        .Shortcut(""Alt+M"")
        .Keywords(""combine"", ""join"")
        .Icon(""bit-icon bit-icon--Merge"")
        .VisibleWhen(() => bSelectedFiles.Count >= 2) // asked every time the list is built
        .Handler(() =>
        {
            var count = bSelectedFiles.Count;
            bSelectedFiles.Clear();
            paletteService.Refresh(); // the selection the predicate reads just changed
            BAvailabilityRun($""Merged {count} files (not really)"");
        });

private void BToggleFile(string file)
{
    if (bSelectedFiles.Remove(file) is false)
    {
        bSelectedFiles.Add(file);
    }

    // The modal palette is closed: tell it the predicate's answer may have changed, so Alt+M is claimed or released now.
    paletteService.Refresh();
}

private void BAvailabilityRun(string what)
{
    bAvailabilityStatus = what;
    _ = InvokeAsync(StateHasChanged);
}

public void Dispose() => bMergeRegistration?.Dispose();";

    private readonly string example11RazorCode = @"
<BitCommandPaletteCommands Commands=""bPageCommands"" />

<BitText>Counter: <b>@bCount</b></BitText>

<BitButton Variant=""BitVariant.Outline"" OnClick='() => paletteService.OpenAsync(""counter"")'>Search ""counter"" in the modal palette</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => BSetCount(bCount + 1)"">+1 from the page</BitButton>";
    private readonly string example11CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private const string BIncrementId = ""b-counter-increment"";
private const string BDecrementId = ""b-counter-decrement"";
private const string BResetId = ""b-counter-reset"";
private const string BZeroReason = ""The counter is already at zero"";

private int bCount;
private BitCommandPaletteCommand[] bPageCommands = [];

protected override void OnInitialized()
{
    bPageCommands =
    [
        BitCommandPaletteCmd.New(""Increment counter"")
            .Id(BIncrementId)
            .Group(""Counter"")
            .Shortcut(""Alt+Up"")
            .Keywords(""plus"", ""add"")
            .Badge(""0"")
            .Icon(""bit-icon bit-icon--Add"")
            .Description(""Registered by this page; gone when you leave it."")
            .Handler(_ => BStep(+1)),
        BitCommandPaletteCmd.New(""Decrement counter"")
            .Id(BDecrementId)
            .Group(""Counter"")
            .Shortcut(""Alt+Down"")
            .Keywords(""minus"", ""subtract"")
            .Badge(""0"")
            .Icon(""bit-icon bit-icon--Remove"")
            .Description(""Registered by this page; gone when you leave it."")
            .Handler(_ => BStep(-1)),
        BitCommandPaletteCmd.New(""Reset counter"")
            .Id(BResetId)
            .Group(""Counter"")
            .Keywords(""zero"", ""clear"")
            .Destructive(""Reset the counter to zero?"")
            .Disabled(true, BZeroReason)
            .Handler(() => BSetCount(0)),
    ];
}

private Task<BitCommandPaletteCommandResult> BStep(int delta)
{
    BSetCount(bCount + delta);
    return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
}

private void BSetCount(int count)
{
    bCount = count;

    // Commands are immutable, so a change is a copy - With keeps the id, the handler and the shortcut - and Update
    // swaps it in wherever it was registered.
    var badge = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    foreach (var id in new[] { BIncrementId, BDecrementId })
    {
        if (paletteService.Find(id) is { } command)
        {
            paletteService.Update(command.With(b => b.Badge(badge)));
        }
    }

    if (paletteService.Find(BResetId) is { } reset)
    {
        paletteService.Update(reset.With(b => b.Disabled(count == 0, count == 0 ? BZeroReason : null)));
    }

    _ = InvokeAsync(StateHasChanged);
}";

    private readonly string example12RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"">
    <BitCommandPaletteGroup Heading=""Quick create"" Priority=""20"" Tags=""@(new[] { ""create"" })"">
        <BitCommandPaletteOption Title=""New issue""
                                 Id=""b-new-issue""
                                 Subtitle=""In this project""
                                 Description=""Opens the new issue form.""
                                 Keywords=""create, add bug""
                                 Shortcut=""Alt+N""
                                 GlobalShortcut=""false""
                                 IconCss=""bit-icon bit-icon--Add""
                                 OnExecute='_ => bMarkupLastRun = ""New issue""' />
        <BitCommandPaletteOption Title=""New…"" ChildrenPlaceholder=""What do you want to create?"">
            <BitCommandPaletteOption Title=""Project"" OnExecute='_ => bMarkupLastRun = ""New project""' />
            <BitCommandPaletteOption Title=""Document"" Badge=""Beta"" OnExecute='_ => bMarkupLastRun = ""New document""' />
        </BitCommandPaletteOption>
        <BitCommandPaletteOption Title=""Set labels…"" MultiSelect ChildrenPlaceholder=""Check labels, Ctrl+Enter to apply…"" OnExecute=""BApplyLabels"">
            <BitCommandPaletteOption Title=""Bug"" Id=""b-label-bug"" />
            <BitCommandPaletteOption Title=""Feature"" Id=""b-label-feature"" />
            <BitCommandPaletteOption Title=""Docs"" Id=""b-label-docs"" />
        </BitCommandPaletteOption>
    </BitCommandPaletteGroup>
    <BitCommandPaletteGroup Heading=""Preferences"">
        <BitCommandPaletteOption Title=""Word wrap"" Checked=""bMarkupWrap"" KeepOpen OnExecute=""() => bMarkupWrap = !bMarkupWrap"" />
        <BitCommandPaletteOption Title=""Font size"" Value=""@($""{bMarkupFontSize}px"")"" KeepOpen OnExecute=""() => bMarkupFontSize = bMarkupFontSize >= 18 ? 12 : bMarkupFontSize + 2"" />
        <BitCommandPaletteOption Title=""Publish"" ConfirmMessage=""Publish to everyone?"" OnExecute='_ => bMarkupLastRun = ""Published (not really)""' />
        <BitCommandPaletteOption Title=""Clear the cache"" Destructive ConfirmMessage=""Clear the cache? Everything is downloaded again."" OnExecute='_ => bMarkupLastRun = ""Cleared the cache (not really)""' />
        <BitCommandPaletteOption Title=""Sync now"" Disabled DisabledReason=""You are offline"" />
    </BitCommandPaletteGroup>
    <BitCommandPaletteGroup Heading=""Links"">
        <BitCommandPaletteOption Title=""Getting started"" Href=""/getting-started"" />
        <BitCommandPaletteOption Title=""bit platform on GitHub"" Href=""https://github.com/bitfoundation/bitplatform"" NewTab Kind=""BitCommandPaletteCommandKind.Help"" />
    </BitCommandPaletteGroup>
    <BitCommandPaletteOption Title=""Couldn't find it?""
                             AlwaysShow
                             Priority=""BitCommandPaletteCommandPriority.Lowest""
                             OnExecute='ctx => bMarkupLastRun = $""Feedback: \""{ctx.Query}\""""'>
        <Icon>
            <svg viewBox=""0 0 16 16"" width=""14"" height=""14"" fill=""none"" stroke=""currentColor"" stroke-width=""1.5""><circle cx=""8"" cy=""8"" r=""6.5"" /><path d=""M6.2 6.2a1.9 1.9 0 1 1 2.6 1.8c-.5.2-.8.6-.8 1.1v.4M8 11.6v.1"" /></svg>
        </Icon>
    </BitCommandPaletteOption>
</BitCommandPalette>

<BitText>Last run: <b>@bMarkupLastRun</b> · Word wrap: <b>@bMarkupWrap</b> · Font size: <b>@bMarkupFontSize px</b></BitText>";
    private readonly string example12CsharpCode = @"
private string bMarkupLastRun = ""Nothing yet"";
private bool bMarkupWrap;
private int bMarkupFontSize = 14;

// Ctrl+Enter on the multi-select page: the checked options are in SelectedCommands.
private void BApplyLabels(BitCommandPaletteCommandContext ctx)
{
    bMarkupLastRun = ctx.SelectedCommands.Count == 0
        ? ""No labels""
        : $""Labels: {string.Join("", "", ctx.SelectedCommands.Select(c => c.Title))}"";
}";

    private readonly string example13RazorCode = @"
@if (bInlinePanelOpen)
{
    <div class=""b-inline-panel"" @onkeydown=""BInlinePanelKeyDown"">
        <div class=""b-inline-panel-header"">
            <b>Side panel</b>
            <BitButton Variant=""BitVariant.Text"" OnClick=""() => bInlinePanelOpen = false"">Close</BitButton>
        </div>
        <BitCommandPalette Inline
                           IncludeRegisteredCommands=""false""
                           ShowFooter=""false""
                           Placeholder=""Search this panel's commands…""
                           Style=""--bit-CommandPalette-max-height: 260px; --bit-layout-density-scale: 0.85;""
                           Commands=""bInlineCommands"" />
    </div>
}
else
{
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => bInlinePanelOpen = true"">Reopen the side panel</BitButton>
}

<BitText>Last run: <b>@bInlineStatus</b></BitText>";
    private readonly string example13CsharpCode = @"
private bool bInlinePanelOpen = true;
private string bInlineStatus = ""Nothing yet"";
private BitCommandPaletteCommand[] bInlineCommands = [];

protected override void OnInitialized()
{
    bInlineCommands =
    [
        BitCommandPaletteCmd.Action(""Show line numbers"", () => BInlineRun(""Show line numbers"")).Group(""View""),
        BitCommandPaletteCmd.Action(""Fold all regions"", () => BInlineRun(""Fold all regions"")).Group(""View""),
        BitCommandPaletteCmd.Action(""Format document"", () => BInlineRun(""Format document"")).Group(""Edit"").Shortcut(""Alt+Shift+F""),
        BitCommandPaletteCmd.Page(""Go to symbol"",
                BitCommandPaletteCmd.Action(""OnInitialized"", () => BInlineRun(""Went to OnInitialized"")).Id(""b-symbol-oninitialized""),
                BitCommandPaletteCmd.Action(""BuildRenderTree"", () => BInlineRun(""Went to BuildRenderTree"")).Id(""b-symbol-buildrendertree""),
                BitCommandPaletteCmd.Action(""Dispose"", () => BInlineRun(""Went to Dispose"")).Id(""b-symbol-dispose""))
            .Group(""Navigate"")
            .ChildrenPlaceholder(""Filter symbols…""),
    ];
}

private void BInlineRun(string what)
{
    bInlineStatus = what; // the inline palette resets itself to its root page afterwards
    _ = InvokeAsync(StateHasChanged);
}

// The palette claims Escape only when it has something to undo; the rest reaches the panel.
private void BInlinePanelKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
{
    if (e.Key == ""Escape"")
    {
        bInlinePanelOpen = false;
    }
}";

    private readonly string example14RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => paletteService.ExecuteAsync(BFocusModeId)"">ExecuteAsync(""b-focus-mode"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => paletteService.ExecuteAsync(BFailId)"">ExecuteAsync(""b-sync"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => paletteService.ExecuteAsync(BRefusedId)"">ExecuteAsync(""b-transfer"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""BToggleScratch"">@(paletteService.Find(BScratchId) is null ? ""Register \""Scratch note\"" again"" : ""Unregister(\""b-scratch-note\"")"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick='() => paletteService.OpenAsync(""service"")'>Open the modal palette</BitButton>

<BitText>
    The palette is <b>@(paletteService.IsOpen ? ""open"" : ""closed"")</b> ·
    IsMac: <b>@paletteService.IsMac</b>, so the toggle reads <BitCommandPaletteShortcutHint Shortcut=""bToggleShortcut"" MacKeys=""paletteService.IsMac"" /> ·
    the registry holds <b>@paletteService.Commands.Count</b> commands ·
    Find(""b-focus-mode""): <b>@(paletteService.Find(BFocusModeId)?.Title ?? ""null"")</b> ·
    Find(""b-scratch-note""): <b>@(paletteService.Find(BScratchId)?.Title ?? ""null"")</b>
</BitText>

<div class=""b-event-log"" role=""log"" aria-label=""Service events"">
    @if (bEvents.Count == 0)
    {
        <div>Open the palette or run a command.</div>
    }
    @foreach (var entry in bEvents)
    {
        <div>@entry</div>
    }
</div>";
    private readonly string example14CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

// b-focus-mode is the command the ""Toggles and settings"" example registers.
private const string BFocusModeId = ""b-focus-mode"";
private const string BFailId = ""b-sync"";
private const string BRefusedId = ""b-transfer"";
private const string BScratchId = ""b-scratch-note"";
private static readonly BitCommandPaletteKeyboardShortcut bToggleShortcut = ""Mod+K"";

private readonly List<string> bEvents = [];
private readonly List<IDisposable> registrations = [];

protected override void OnInitialized()
{
    registrations.Add(paletteService.Register(
        BitCommandPaletteCmd.New(""Sync with the server"")
            .Id(BFailId)
            .Group(""Service"")
            .Description(""Fails on purpose, to show CommandFailed."")
            .Handler(async _ =>
            {
                await Task.Delay(300);
                throw new InvalidOperationException(""The sync server is unreachable (on purpose)."");
            }),
        BitCommandPaletteCmd.Action(""Transfer ownership"", () => { })
            .Id(BRefusedId)
            .Group(""Service"")
            .Description(""Refused by its guard, to show CommandRefused."")
            .Guard(_ => false, ""Only the owner can transfer the project""),
        BScratchCommand()));

    // The service outlives the page: named handlers, removed in Dispose.
    paletteService.CommandsChanged += BOnCommandsChanged;
    paletteService.Opened += BOnOpened;
    paletteService.Closed += BOnClosed;
    paletteService.CommandExecuted += BOnCommandExecuted;
    paletteService.CommandFailed += BOnCommandFailed;
    paletteService.CommandRefused += BOnCommandRefused;
    paletteService.PlatformDetected += BOnPlatformDetected;
    paletteService.SequencePending += BOnSequencePending;
}

private BitCommandPaletteCommand BScratchCommand() =>
    BitCommandPaletteCmd.Action(""Scratch note"", () => BLog(""Scratch note ran""))
        .Id(BScratchId)
        .Group(""Service"");

private void BToggleScratch()
{
    if (paletteService.Find(BScratchId) is null)
    {
        registrations.Add(paletteService.Register(BScratchCommand()));
        return;
    }

    BLog($""Unregister(\""{BScratchId}\"") returned {paletteService.Unregister(BScratchId)}"");
}

private void BOnCommandsChanged() => BLog($""CommandsChanged  {paletteService.Commands.Count} commands registered"", collapse: true);

private void BOnOpened() => BLog(""Opened"");

private void BOnClosed() => BLog(""Closed"");

private void BOnCommandExecuted(object? sender, BitCommandPaletteCommandExecutedEventArgs e)
    => BLog($""CommandExecuted  {e.Command.Id} → {e.Result.GetType().Name.Replace(""Result"", """")} in {e.Duration.TotalMilliseconds:0} ms"");

private void BOnCommandFailed(object? sender, BitCommandPaletteCommandErrorEventArgs e)
{
    if (e.Command.Id == BFailId)
    {
        e.Message = ""The server did not answer. Nothing was changed.""; // what the palette shows
        e.Handled = true; // reported here: not logged, and not rethrown to ExecuteAsync
    }

    BLog($""CommandFailed  {e.Command.Id}: {e.Exception.Message}"");
}

private void BOnCommandRefused(object? sender, BitCommandPaletteCommandRefusedEventArgs e)
    => BLog($""CommandRefused  {e.Command.Id} ({e.Trigger}): {e.Message}"");

private void BOnPlatformDetected() => BLog($""PlatformDetected  IsMac = {paletteService.IsMac}"");

private void BOnSequencePending(BitCommandPaletteKeyboardShortcut? pending)
    => BLog(pending is null ? ""SequencePending  (over)"" : $""SequencePending  {pending.ToDisplayString(paletteService.IsMac)}…"");

private void BLog(string entry, bool collapse = false) => _ = InvokeAsync(() =>
{
    // The registry changes in bursts, so a run of CommandsChanged is one line.
    if (collapse && bEvents.Count > 0 && bEvents[0].Contains(""CommandsChanged"", StringComparison.Ordinal))
    {
        bEvents.RemoveAt(0);
    }

    bEvents.Insert(0, $""{DateTime.Now:HH:mm:ss}  {entry}"");
    if (bEvents.Count > 10)
    {
        bEvents.RemoveAt(bEvents.Count - 1);
    }

    StateHasChanged();
});

public void Dispose()
{
    paletteService.CommandsChanged -= BOnCommandsChanged;
    paletteService.Opened -= BOnOpened;
    paletteService.Closed -= BOnClosed;
    paletteService.CommandExecuted -= BOnCommandExecuted;
    paletteService.CommandFailed -= BOnCommandFailed;
    paletteService.CommandRefused -= BOnCommandRefused;
    paletteService.PlatformDetected -= BOnPlatformDetected;
    paletteService.SequencePending -= BOnSequencePending;

    foreach (var registration in registrations)
    {
        registration.Dispose();
    }
}";

    private readonly string example15RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""cResultCommands"" />

<BitText>@cResultStatus</BitText>";
    private readonly string example15CsharpCode = @"
private string cResultStatus = ""Nothing run yet."";
private bool cWordWrap;
private string cDensity = ""Comfortable"";
private int cTodoCount;
private readonly List<(string Id, string Text)> cTodos = [];
private BitCommandPaletteCommand[] cResultCommands = [];

protected override void OnInitialized()
{
    CBuildResultCommands();
}

private void CBuildResultCommands()
{
    cResultCommands =
    [
        // The to-dos added with ""Add a to-do…"": Enter completes one, and the palette stays open on the list.
        .. cTodos.Select(todo => BitCommandPaletteCmd.New(todo.Text)
            .Id(todo.Id)
            .Subtitle(""To-do · Enter completes it (KeepOpen)"")
            .ExcludeFromHistory()
            .Handler(_ =>
            {
                cTodos.Remove(todo);
                cResultStatus = $""Done: {todo.Text}"";
                CBuildResultCommands();
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            })
            .Build()),

        // No result: Close, the default.
        BitCommandPaletteCmd.Action(""Say hello"", () => { cResultStatus = ""Hello!""; StateHasChanged(); })
            .Subtitle(""Close - an inline palette goes back to its root""),

        // Stay, and rebuild the list: the commands are built again so the switch shows the new state.
        BitCommandPaletteCmd.New(""Word wrap"")
            .Subtitle(""KeepOpen - the switch flips, the palette stays"")
            .Checked(cWordWrap)
            .Handler(_ =>
            {
                cWordWrap = !cWordWrap;
                cResultStatus = $""Word wrap: {(cWordWrap ? ""on"" : ""off"")}"";
                CBuildResultCommands();
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            }),

        // Each option answers Back, to land on the list again.
        BitCommandPaletteCmd.Page(""Density"", CDensityOption(""Compact""), CDensityOption(""Comfortable""), CDensityOption(""Spacious""))
            .Subtitle(""Back - each option returns to this list"")
            .Value(cDensity),

        // A prompt, then Reset: back to the root, the query cleared, where the new to-do is listed.
        BitCommandPaletteCmd.New(""Add a to-do…"")
            .Subtitle(""Prompt, then Reset"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt(""New to-do"", submit =>
            {
                var text = submit.PromptValue!.Trim();
                if (text.Length == 0)
                {
                    return Task.FromResult(BitCommandPaletteCommandResult.Error(""A to-do needs some text.""));
                }

                cTodos.Add(($""c-todo-{++cTodoCount}"", text));
                cResultStatus = $""To-do added: {text}"";
                CBuildResultCommands();
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.Reset);
            }, placeholder: ""What needs doing?""))),

        // A message in the footer; keepOpen keeps the palette open under it.
        BitCommandPaletteCmd.New(""Check for updates"")
            .Subtitle(""Toast with keepOpen"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""You are on the latest version."", keepOpen: true))),

        BitCommandPaletteCmd.New(""Save all"")
            .Subtitle(""Toast - then the palette closes"")
            .Handler(_ =>
            {
                cResultStatus = ""All changes saved."";
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.Toast(""All changes saved.""));
            }),

        // An expected failure: announced, and the palette stays.
        BitCommandPaletteCmd.New(""Join the beta"")
            .Subtitle(""Error"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Error(""The beta is full. Try again next week.""))),

        // A page the handler decides when it runs; these rows are not registered anywhere.
        BitCommandPaletteCmd.New(""Export…"")
            .Subtitle(""Page"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Page(""Export format"",
            [
                BitCommandPaletteCmd.Action(""CSV"", () => CExported(""CSV"")).Subtitle(""Comma separated values"").Build(),
                BitCommandPaletteCmd.Action(""JSON"", () => CExported(""JSON"")).Subtitle(""Pretty printed"").Build(),
                BitCommandPaletteCmd.Action(""PDF"", () => CExported(""PDF"")).Subtitle(""Print friendly"").Badge(""Pro"").Disabled(true, ""Requires the Pro plan"").Build(),
            ], placeholder: ""Pick a format…""))),
    ];
}

private BitCommandPaletteCommand CDensityOption(string density) =>
    BitCommandPaletteCmd.New(density)
        .Id($""c-density-{density.ToLowerInvariant()}"")
        .Checked(cDensity == density)
        .Handler(_ =>
        {
            cDensity = density;
            cResultStatus = $""Density: {density}"";
            CBuildResultCommands();
            StateHasChanged();
            return Task.FromResult(BitCommandPaletteCommandResult.Back);
        });

private void CExported(string format)
{
    cResultStatus = $""Exported as {format}"";
    StateHasChanged();
}";

    private readonly string example16RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""cContextCommands"" />

@if (cContextLines.Count == 0)
{
    <BitText>Run ""Inspect the context"".</BitText>
}
else
{
    <ul>
        @foreach (var (member, value) in cContextLines)
        {
            <li><code>@member</code>: @value</li>
        }
    </ul>
}";
    private readonly string example16CsharpCode = @"
private readonly List<(string Member, string Value)> cContextLines = [];
private BitCommandPaletteCommand[] cContextCommands = [];

protected override void OnInitialized()
{
    cContextCommands =
    [
        BitCommandPaletteCmd.New(""Inspect the context"")
            .Keywords(""ctx"", ""handler"")
            .Handler(ctx =>
            {
                cContextLines.Clear();
                cContextLines.Add((""Command"", $""{ctx.Command.Title} ({ctx.Command.Id})""));
                cContextLines.Add((""Query"", ctx.Query.Length == 0 ? ""(empty)"" : $""\""{ctx.Query}\""""));
                cContextLines.Add((""ModePrefix"", ctx.ModePrefix ?? ""null - the default mode""));
                cContextLines.Add((""PromptValue"", ctx.PromptValue ?? ""null - not a prompt step""));
                cContextLines.Add((""SelectedCommands"", $""{ctx.SelectedCommands.Count} - not a multi-select page""));
                cContextLines.Add((""GetRequiredService<IBitCommandPaletteService>()"", ctx.GetRequiredService<IBitCommandPaletteService>() == ctx.Palette ? ""the same service as Palette"" : ""another service""));
                cContextLines.Add((""Navigation.Uri"", ctx.Navigation.Uri));
                cContextLines.Add((""Palette.Commands"", $""{ctx.Palette.Commands.Count} registered commands""));
                cContextLines.Add((""CancellationToken.CanBeCanceled"", ctx.CancellationToken.CanBeCanceled.ToString()));
                StateHasChanged();
                // Stay, with the query kept, so it can be run again with another one.
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            }),

        // Listed whatever was typed, last: the query it was reached with is the feedback.
        BitCommandPaletteCmd.New(""Feedback: couldn't find a command?"")
            .AlwaysShow()
            .Priority(BitCommandPaletteCommandPriority.Lowest)
            .Handler(ctx => Task.FromResult(BitCommandPaletteCommandResult.Toast(
                ctx.Query.Length > 0 ? $""Thanks! We logged “{ctx.Query}”."" : ""Thanks for the feedback!"", keepOpen: true))),
    ];
}";

    private readonly string example17RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""cPromptCommands"" />

<BitText>Project: <b>@cProjectName</b> · @cPromptStatus</BitText>";
    private readonly string example17CsharpCode = @"
// using System.Net.Mail;

private string cProjectName = ""Apollo"";
private string cPromptStatus = ""Nothing submitted yet."";
private static readonly string[] cMembers = [""ada@example.com"", ""grace@example.com""];
private BitCommandPaletteCommand[] cPromptCommands = [];

protected override void OnInitialized()
{
    cPromptCommands =
    [
        // The simplest prompt: a title and what to do with the text.
        BitCommandPaletteCmd.New(""Add a note…"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt(""New note"", submit =>
            {
                cPromptStatus = $""Note: {submit.PromptValue}"";
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.Toast(""Note added""));
            }))),

        // Pre-filled with the current value, with a placeholder for when it is cleared.
        BitCommandPaletteCmd.New(""Rename project…"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt(
                ""New project name"",
                submit =>
                {
                    var name = submit.PromptValue!.Trim();
                    if (name.Length == 0)
                    {
                        return Task.FromResult(BitCommandPaletteCommandResult.Error(""A project needs a name.""));
                    }

                    cProjectName = name;
                    cPromptStatus = $""Renamed to {name}"";
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.Toast($""Renamed to “{name}”""));
                },
                placeholder: ""Project name…"",
                initialValue: cProjectName))),

        // Validated as it is typed; checked by the ""server"" on submit.
        BitCommandPaletteCmd.New(""Invite by e-mail…"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt(
                ""E-mail address"",
                async submit =>
                {
                    var email = submit.PromptValue!.Trim();
                    await Task.Delay(400, submit.CancellationToken); // asking the server
                    if (cMembers.Contains(email, StringComparer.OrdinalIgnoreCase))
                    {
                        // Expected, so a result and not an exception: shown, and the prompt stays for a correction.
                        return BitCommandPaletteCommandResult.Error($""{email} is already a member."");
                    }

                    cPromptStatus = $""Invited {email}"";
                    StateHasChanged();
                    return BitCommandPaletteCommandResult.Toast($""Invitation sent to {email}"");
                },
                // Null for a value Enter may submit, the message to show otherwise.
                validate: value => MailAddress.TryCreate(value.Trim(), out var parsedAddress) ? null : ""Enter an e-mail address, like name@example.com."",
                placeholder: ""name@example.com""))),
    ];
}";

    private readonly string example18RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""cMultiCommands"" />

<BitText>@cMultiStatus</BitText>";
    private readonly string example18CsharpCode = @"
private string cMultiStatus = ""Nothing applied yet."";
private BitCommandPaletteCommand[] cMultiCommands = [];

private static readonly string[] cWebhookEvents = [""push"", ""pull_request"", ""issues"", ""release"", ""deployment""];

private static readonly (string Name, string Role)[] cPeople =
[
    (""Ada Lovelace"", ""Engineering""),
    (""Grace Hopper"", ""Engineering""),
    (""Alan Turing"", ""Research""),
    (""Katherine Johnson"", ""Data""),
    (""Margaret Hamilton"", ""Engineering""),
    (""Linus Torvalds"", ""Platform""),
    (""Barbara Liskov"", ""Architecture""),
    (""Dennis Ritchie"", ""Platform""),
    (""Hedy Lamarr"", ""Research""),
    (""Tim Berners-Lee"", ""Web""),
];

protected override void OnInitialized()
{
    cMultiCommands =
    [
        BitCommandPaletteCmd.Page(""Set labels…"",
                BitCommandPaletteCmd.New(""bug"").Id(""c-label-bug""),
                BitCommandPaletteCmd.New(""enhancement"").Id(""c-label-enhancement""),
                BitCommandPaletteCmd.New(""documentation"").Id(""c-label-documentation""),
                BitCommandPaletteCmd.New(""performance"").Id(""c-label-performance""))
            .MultiSelect()
            .ChildrenPlaceholder(""Check labels, Ctrl+Enter to apply…"")
            // Runs on Ctrl+Enter, with the checked rows.
            .Handler(ctx =>
            {
                var labels = ctx.SelectedCommands.Select(c => c.Title).ToList();
                cMultiStatus = labels.Count == 0 ? ""Labels cleared."" : $""Labels: {string.Join("", "", labels)}"";
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.Toast(labels.Count == 1 ? ""1 label applied"" : $""{labels.Count} labels applied""));
            }),

        // Searched as the user types; what is checked stays checked across searches.
        BitCommandPaletteCmd.New(""Invite people…"")
            .MultiSelect()
            .ChildrenPlaceholder(""Search people, Enter checks, Ctrl+Enter invites…"")
            .Source(""People"", async (query, cancellationToken) =>
            {
                await Task.Delay(250, cancellationToken); // the directory API
                return cPeople
                    .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Role.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(p => BitCommandPaletteCmd.New(p.Name).Id($""c-invite-{p.Name.Replace(' ', '-').ToLowerInvariant()}"").Subtitle(p.Role).Build())
                    .ToList();
            })
            .Handler(ctx =>
            {
                cMultiStatus = $""Invited: {string.Join("", "", ctx.SelectedCommands.Select(c => c.Title))}"";
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.Toast(ctx.SelectedCommands.Count == 1 ? ""1 person invited"" : $""{ctx.SelectedCommands.Count} people invited""));
            }),

        // A prompt, then a multi-select page its submit returns.
        BitCommandPaletteCmd.New(""Create webhook…"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt(
                ""Payload URL"",
                address => Task.FromResult(BitCommandPaletteCommandResult.Page(
                    ""Events to send"",
                    [.. cWebhookEvents.Select(e => BitCommandPaletteCmd.New(e).Id($""c-event-{e}"").Build())],
                    placeholder: ""Check events, Ctrl+Enter to create…"",
                    multiSelect: true,
                    onSubmit: picked =>
                    {
                        var events = picked.SelectedCommands.Select(c => c.Title).ToList();
                        if (events.Count == 0)
                        {
                            return Task.FromResult(BitCommandPaletteCommandResult.Error(""Pick at least one event.""));
                        }

                        cMultiStatus = $""Webhook to {address.PromptValue!.Trim()} for {string.Join("", "", events)}"";
                        StateHasChanged();
                        return Task.FromResult(BitCommandPaletteCommandResult.Toast(events.Count == 1 ? ""Webhook created for 1 event"" : $""Webhook created for {events.Count} events""));
                    })),
                validate: value => Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && uri.Host.Length > 0
                    ? null
                    : ""Enter an https:// address."",
                placeholder: ""https://example.com/hooks"",
                initialValue: ""https://""))),
    ];
}";

    private readonly string example19RazorCode = @"
@implements IDisposable

<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(""generate report""))"">Find it in the palette</BitButton>

<BitText>@cReportStatus</BitText>";
    private readonly string example19CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private readonly List<IDisposable> registrations = [];
private string cReportStatus = ""Not run yet."";

protected override void OnInitialized()
{
    registrations.Add(paletteService.Register(
        BitCommandPaletteCmd.New(""Generate report…"")
            .Id(""c-report"")
            .Group(""Long-running"")
            .Keywords(""slow"", ""busy"")
            .Handler(async ctx =>
            {
                cReportStatus = ""Generating…"";
                StateHasChanged();
                try
                {
                    // Esc closes the palette and cancels this token.
                    await Task.Delay(3000, ctx.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cReportStatus = ""Cancelled: the palette was closed while the report was being generated."";
                    StateHasChanged();
                    throw; // stops the handler; a cancellation is not reported as a failure
                }

                cReportStatus = ""Report generated."";
                StateHasChanged();
                return BitCommandPaletteCommandResult.Toast(""Report ready"", keepOpen: true);
            })));
}

public void Dispose()
{
    foreach (var registration in registrations)
    {
        registration.Dispose();
    }
}";

    private readonly string example20RazorCode = @"
@implements IDisposable

<BitCheckbox Label=""Handled = true (this page reports its own failures)"" @bind-Value=""cHandleFailures"" />

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""cErrorCommands"" />

<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => CRunFromCodeAsync(""c-sync""))"">Run ""Sync with server"" from code</BitButton>

<BitText>@cExecuteStatus</BitText>
<div class=""c-log"" aria-label=""What the CommandFailed subscriber saw"">
    @if (cFailures.Count == 0)
    {
        <div>The CommandFailed subscriber has seen nothing yet.</div>
    }
    @foreach (var failure in cFailures)
    {
        <div>@failure</div>
    }
</div>";
    private readonly string example20CsharpCode = @"
// using System.ComponentModel.DataAnnotations;

[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private readonly List<IDisposable> registrations = [];
private bool cHandleFailures = true;
private string cExecuteStatus = """";
private readonly List<string> cFailures = [];
private BitCommandPaletteCommand[] cErrorCommands = [];

protected override void OnInitialized()
{
    cErrorCommands =
    [
        BitCommandPaletteCmd.New(""Sync with server"")
            .Id(""c-sync"")
            .Group(""Errors"")
            .Handler(async ctx =>
            {
                await Task.Delay(300, ctx.CancellationToken);
                // Never shown: it names a server and a user.
                throw new InvalidOperationException(""Connection to db-prod-7.internal:5432 refused for user 'svc_sync'."");
            }),

        BitCommandPaletteCmd.New(""Publish release"")
            .Id(""c-publish"")
            .Group(""Errors"")
            .Handler(async ctx =>
            {
                await Task.Delay(300, ctx.CancellationToken);
                throw new ValidationException(""The changelog is empty: write one before publishing."");
            }),
    ];

    // Registered too, so ExecuteAsync can run them from code.
    registrations.Add(paletteService.Register(cErrorCommands));

    paletteService.CommandFailed += COnCommandFailed;
}

private void COnCommandFailed(object? sender, BitCommandPaletteCommandErrorEventArgs e)
{
    if (e.Command.Id is not (""c-sync"" or ""c-publish"" or ""c-deployments""))
    {
        return; // any other command's failure is left to the palette, which logs it
    }

    if (e.Exception is ValidationException)
    {
        e.Message = e.Exception.Message; // written for users
    }

    e.Handled = cHandleFailures; // reported by this page: not logged, not rethrown

    var shown = e.Command.Id == ""c-deployments"" ? ""the page's error state, with Try again"" : $""“{e.Message}”"";
    cFailures.Insert(0, $""{DateTime.Now:HH:mm:ss}  {e.Command.Title}: {e.Exception.GetType().Name}, shown as {shown}"");
    if (cFailures.Count > 6)
    {
        cFailures.RemoveAt(cFailures.Count - 1);
    }

    _ = InvokeAsync(StateHasChanged);
}

private async Task CRunFromCodeAsync(string commandId)
{
    try
    {
        await paletteService.ExecuteAsync(commandId);
        cExecuteStatus = $""ExecuteAsync(\""{commandId}\"") returned normally: the subscriber handled the failure."";
    }
    catch (Exception ex)
    {
        // Unhandled by the subscriber, the failure reaches the code that ran the command.
        cExecuteStatus = $""ExecuteAsync(\""{commandId}\"") threw {ex.GetType().Name}."";
    }
}

public void Dispose()
{
    paletteService.CommandFailed -= COnCommandFailed;
    foreach (var registration in registrations)
    {
        registration.Dispose();
    }
}";

    private readonly string example21RazorCode = @"
@implements IDisposable

<BitCheckbox Label=""Pro plan"" @bind-Value=""cIsPro"" />
<BitCheckbox Label=""Admin"" @bind-Value=""cIsAdmin"" />

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""cGuardCommands"" />

<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.ExecuteAsync(""c-guard-export""))"">Run ""Export as PDF"" from code</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(pageCommandId: ""c-guard-admin""))"">Open the Admin page</BitButton>

<BitText>@cGuardStatus</BitText>
<div class=""c-log"" aria-label=""What the CommandRefused subscriber saw"">
    @if (cRefusals.Count == 0)
    {
        <div>Nothing refused yet.</div>
    }
    @foreach (var refusal in cRefusals)
    {
        <div>@refusal</div>
    }
</div>";
    private readonly string example21CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private readonly List<IDisposable> registrations = [];
private bool cIsPro;
private bool cIsAdmin;
private string cGuardStatus = ""Nothing run yet."";
private readonly List<string> cRefusals = [];
private BitCommandPaletteCommand[] cGuardCommands = [];

protected override void OnInitialized()
{
    cGuardCommands =
    [
        BitCommandPaletteCmd.New(""Export as PDF"")
            .Id(""c-guard-export"")
            .Group(""Guards"")
            .Shortcut(""G X"")
            // A check that needs nothing async, and what to say when it refuses.
            .Guard(_ => cIsPro, ""Exports need the Pro plan."")
            .Handler(_ =>
            {
                cGuardStatus = ""Exported as PDF."";
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.Toast(""Exported as PDF""));
            }),

        BitCommandPaletteCmd.New(""Admin"")
            .Id(""c-guard-admin"")
            .Group(""Guards"")
            // An async check; on a page, it guards every command under it too.
            .Guard(async (ctx, cancellationToken) =>
            {
                await Task.Delay(400, cancellationToken); // the server deciding
                return cIsAdmin ? BitCommandPaletteGuardResult.Allow : BitCommandPaletteGuardResult.Deny(""Admins only."");
            })
            .Children(
                BitCommandPaletteCmd.Action(""Purge the cache"", () => { cGuardStatus = ""Cache purged.""; StateHasChanged(); }).Id(""c-guard-admin-purge""),
                BitCommandPaletteCmd.Action(""Read the audit log"", () => { cGuardStatus = ""Audit log opened.""; StateHasChanged(); }).Id(""c-guard-admin-audit"")),
    ];

    // Registered too, so the shortcut and ExecuteAsync reach them.
    registrations.Add(paletteService.Register(cGuardCommands));

    paletteService.CommandRefused += COnCommandRefused;
}

private void COnCommandRefused(object? sender, BitCommandPaletteCommandRefusedEventArgs e)
{
    cRefusals.Insert(0, $""{DateTime.Now:HH:mm:ss}  {e.Command.Title}, from {e.Trigger}: {e.Message ?? ""(the default message)""}"");
    if (cRefusals.Count > 6)
    {
        cRefusals.RemoveAt(cRefusals.Count - 1);
    }

    _ = InvokeAsync(StateHasChanged);
}

public void Dispose()
{
    paletteService.CommandRefused -= COnCommandRefused;
    foreach (var registration in registrations)
    {
        registration.Dispose();
    }
}

// ---- A guard for many commands, registered at startup (not used by this demo page) ----

// Program.cs
builder.Services.AddBitCommandPaletteCommandGuard<PermissionGuard>();

// A command opts in with .Data(new RequiredPermission(""reports.export"")).
public sealed record RequiredPermission(string Name);

public sealed class PermissionGuard(AuthenticationStateProvider auth) : IBitCommandPaletteCommandGuard
{
    public async ValueTask<BitCommandPaletteGuardResult> CheckAsync(BitCommandPaletteGuardContext context, CancellationToken cancellationToken)
    {
        if (context.Command.Data is not RequiredPermission { Name: var permission })
        {
            return BitCommandPaletteGuardResult.Allow;
        }

        var user = (await auth.GetAuthenticationStateAsync()).User;
        return user.HasClaim(""permission"", permission)
            ? BitCommandPaletteGuardResult.Allow
            : BitCommandPaletteGuardResult.Deny($""You need the {permission} permission."");
    }
}";

    private readonly string example22RazorCode = @"
@implements IDisposable

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""cNestedCommands"">
    <BitCommandPaletteOption Title=""Theme"" Value=""@cTheme"" ChildrenPlaceholder=""Pick a theme…"">
        <BitCommandPaletteOption Title=""Light"" Checked=""@(cTheme == ""Light"")"" OnExecute=""@(() => CSetTheme(""Light""))"" />
        <BitCommandPaletteOption Title=""Dark"" Checked=""@(cTheme == ""Dark"")"" OnExecute=""@(() => CSetTheme(""Dark""))"" />
        <BitCommandPaletteOption Title=""High contrast"" Checked=""@(cTheme == ""High contrast"")"" OnExecute=""@(() => CSetTheme(""High contrast""))"" />
    </BitCommandPaletteOption>
</BitCommandPalette>

<BitText>@cNestedStatus</BitText>

<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(pageCommandId: ""c-editor-theme""))"">Open on Editor theme</BitButton>
<BitCommandPaletteTrigger Label=""Change editor theme"" PageCommandId=""c-editor-theme"" ShowShortcut=""false"" />
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(""solarized""))"">Search ""solarized"" from the root</BitButton>

<BitText>Editor theme: <b>@cEditorTheme</b></BitText>";
    private readonly string example22CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private readonly List<IDisposable> registrations = [];
private string cNestedStatus = ""Nothing picked yet."";
private string cTheme = ""Light"";
private string cLanguage = ""English"";
private string cEditorTheme = ""Light"";
private int cDeploymentLoads;
private BitCommandPaletteCommand[] cNestedCommands = [];

private static readonly string[] cLanguages = [""English"", ""Deutsch"", ""Français"", ""Español""];
private static readonly string[] cBranches = [""main"", ""develop"", ""release/10.4"", ""feature/command-palette""];

private static readonly (string Name, string Role)[] cPeople =
[
    (""Ada Lovelace"", ""Engineering""),
    (""Grace Hopper"", ""Engineering""),
    (""Alan Turing"", ""Research""),
    (""Katherine Johnson"", ""Data""),
    (""Margaret Hamilton"", ""Engineering""),
    (""Linus Torvalds"", ""Platform""),
    (""Barbara Liskov"", ""Architecture""),
    (""Dennis Ritchie"", ""Platform""),
    (""Hedy Lamarr"", ""Research""),
    (""Tim Berners-Lee"", ""Web""),
];

private static readonly (string Version, string Environment, string When)[] cDeployments =
[
    (""v4.2.1"", ""Production"", ""2 hours ago""),
    (""v4.2.1"", ""Staging"", ""5 hours ago""),
    (""v4.2.0"", ""Production"", ""yesterday""),
    (""v4.1.9"", ""Production"", ""last week""),
];

protected override void OnInitialized()
{
    CBuildNestedCommands();

    // Registered with the service: reachable by id from OpenAsync(pageCommandId:) and a trigger's PageCommandId.
    registrations.Add(paletteService.Register(
        BitCommandPaletteCmd.New(""Preferences"")
            .Id(""c-prefs"")
            .Group(""Nested pages"")
            .Children(
                BitCommandPaletteCmd.New(""Editor theme"")
                    .Id(""c-editor-theme"")
                    .ChildrenPlaceholder(""Pick an editor theme…"")
                    .Children(CEditorThemeOption(""Light""), CEditorThemeOption(""Dark""), CEditorThemeOption(""Solarized"")),
                BitCommandPaletteCmd.Action(""Reset the editor"", () => { cEditorTheme = ""Light""; StateHasChanged(); })
                    .Id(""c-prefs-reset""))));
}

private void CBuildNestedCommands()
{
    cNestedCommands =
    [
        // Static children; each option goes Back to the list.
        BitCommandPaletteCmd.New(""Language"")
            .Value(cLanguage)
            .ChildrenPlaceholder(""Pick a language…"")
            .Children(cLanguages.Select(language => BitCommandPaletteCmd.New(language)
                .Id($""c-language-{language.ToLowerInvariant()}"")
                .Checked(cLanguage == language)
                .Handler(_ =>
                {
                    cLanguage = language;
                    cNestedStatus = $""Language: {language}"";
                    CBuildNestedCommands();
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.Back);
                })
                .Build())),

        // Loaded each time the page opens.
        BitCommandPaletteCmd.New(""Assign to…"")
            .Children(async (ctx, cancellationToken) =>
            {
                await Task.Delay(400, cancellationToken); // the API call
                return cPeople.Select(p => BitCommandPaletteCmd.New(p.Name)
                        .Subtitle(p.Role)
                        .Handler(() => { cNestedStatus = $""Assigned to {p.Name}""; StateHasChanged(); })
                        .Build())
                    .ToList();
            }, placeholder: ""Pick an assignee…""),

        // A loader that fails every other time: an error state with Try again.
        BitCommandPaletteCmd.New(""Recent deployments…"")
            .Id(""c-deployments"")
            .Children(async (ctx, cancellationToken) =>
            {
                await Task.Delay(500, cancellationToken);
                if (++cDeploymentLoads % 2 == 1)
                {
                    throw new HttpRequestException(""503 Service Unavailable from deploy-api.internal"");
                }

                return cDeployments.Select(d => BitCommandPaletteCmd.New($""{d.Version} to {d.Environment}"")
                        .Subtitle(d.When)
                        .ExcludeFromHistory()
                        .Handler(() => { cNestedStatus = $""Opened {d.Version} on {d.Environment}""; StateHasChanged(); })
                        .Build())
                    .ToList();
            }, placeholder: ""Pick a deployment…""),

        // A page the handler decides once it found out what there is.
        BitCommandPaletteCmd.New(""Switch branch…"")
            .Handler(async ctx =>
            {
                await Task.Delay(300, ctx.CancellationToken); // fetching the branches
                return BitCommandPaletteCommandResult.Page(""Switch branch"",
                    [.. cBranches.Select(branch => BitCommandPaletteCmd.Action(branch, () => { cNestedStatus = $""Switched to {branch}""; StateHasChanged(); }).Build())],
                    placeholder: ""Pick a branch…"");
            }),
    ];
}

private void CSetTheme(string theme)
{
    cTheme = theme;
    cNestedStatus = $""Theme: {theme}"";
}

private BitCommandPaletteCommand CEditorThemeOption(string theme) =>
    BitCommandPaletteCmd.New(theme)
        .Id($""c-editor-theme-{theme.ToLowerInvariant()}"")
        .Handler(() => { cEditorTheme = theme; StateHasChanged(); });

public void Dispose()
{
    foreach (var registration in registrations)
    {
        registration.Dispose();
    }
}";

    private readonly string example23RazorCode = @"
@implements IDisposable

<BitCheckbox Label=""EnableHistory"" @bind-Value=""cEnableHistory"" />

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""cEnableHistory"" Commands=""cHistoryCommands"" />

<BitText>@cHistoryStatus</BitText>
<BitText>Pinned: <b>@CHistoryText(cHistory.Pinned(), ""nothing - press Tab on a row and pick Pin to top"")</b></BitText>
<BitText>Recent: <b>@CHistoryText(cHistory.Recents(int.MaxValue), ""nothing used yet"")</b></BitText>

<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => cHistory.ClearAsync())"">Clear recent commands</BitButton>";
    private readonly string example23CsharpCode = @"
// The history behind every palette's recents, pins and frecency.
[Inject] private BitCommandPaletteHistory cHistory { get; set; } = default!;

private bool cEnableHistory = true;
private string cHistoryStatus = ""Nothing run yet."";
private BitCommandPaletteCommand[] cHistoryCommands = [];

protected override void OnInitialized()
{
    cHistoryCommands =
    [
        CPersonCommand(""c-history-ada"", ""Ada Lovelace"", ""Engineering""),
        CPersonCommand(""c-history-grace"", ""Grace Hopper"", ""Engineering""),
        CPersonCommand(""c-history-alan"", ""Alan Turing"", ""Research""),
        BitCommandPaletteCmd.New(""Start a timer"")
            .Id(""c-history-timer"")
            .Handler(_ => CHistoryRun(""Timer started"")),
        BitCommandPaletteCmd.Navigate(""Go to the Button demo"", ""/components/button"")
            .Id(""c-history-button"")
            .Subtitle(""A link: Ctrl+Enter opens it in a new tab""),
        BitCommandPaletteCmd.New(""Scratch note"")
            .Id(""c-history-scratch"")
            .Subtitle(""ExcludeFromHistory: never recent, no action panel"")
            .ExcludeFromHistory()
            .Handler(_ => CHistoryRun(""Scratch note opened"")),
    ];

    // Raised when the palette loads the history, and on every record, pin and clear.
    cHistory.Changed += COnHistoryChanged;
}

// Listed first in the row's action panel (Tab), before Pin to top; Ctrl+Enter runs the first one.
private BitCommandPaletteCommand CPersonCommand(string id, string name, string role) =>
    BitCommandPaletteCmd.New(name)
        .Id(id)
        .Subtitle(role)
        .Handler(_ => CHistoryRun($""Opened {name}'s profile""))
        .Actions(
            BitCommandPaletteCmd.New(""Send an e-mail"").Id($""{id}-email"").ExcludeFromHistory().Handler(_ => CHistoryRun($""E-mail to {name} drafted"")),
            BitCommandPaletteCmd.New(""Mention in an issue"").Id($""{id}-mention"").ExcludeFromHistory().Handler(_ => CHistoryRun($""{name} mentioned"")));

private Task<BitCommandPaletteCommandResult> CHistoryRun(string message)
{
    cHistoryStatus = message;
    StateHasChanged();
    return Task.FromResult(BitCommandPaletteCommandResult.Toast(message, keepOpen: true));
}

private string CHistoryText(IReadOnlyList<string> ids, string none)
{
    var titles = ids.Select(id => cHistoryCommands.FirstOrDefault(c => c.Id == id))
                    .OfType<BitCommandPaletteCommand>()
                    .Select(c => $""{c.Title} ({cHistory.Get(c.Id)?.Count ?? 0}×)"")
                    .ToList();
    return titles.Count == 0 ? none : string.Join("", "", titles);
}

private void COnHistoryChanged() => _ = InvokeAsync(StateHasChanged);

public void Dispose()
{
    cHistory.Changed -= COnHistoryChanged;
}

// ---- Keeping the history elsewhere, registered at startup (not used by this demo page) ----

// Program.cs
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    options.MaxRecents = 8;
    options.HistoryStorageKey = ""my-app.palette-history"";
});
builder.Services.AddBitCommandPaletteHistoryStore<ServerHistoryStore>(); // per user, on your server
// or: builder.Services.AddBitCommandPaletteInMemoryHistoryStore();      // for the lifetime of the scope only

public sealed class ServerHistoryStore(HttpClient http) : IBitCommandPaletteHistoryStore
{
    public async ValueTask<IDictionary<string, BitCommandPaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
        => await http.GetFromJsonAsync<Dictionary<string, BitCommandPaletteCommandUsage>>(""api/palette-history"", cancellationToken)
           ?? new Dictionary<string, BitCommandPaletteCommandUsage>();

    public async ValueTask SaveAsync(IReadOnlyDictionary<string, BitCommandPaletteCommandUsage> usage, CancellationToken cancellationToken = default)
        => (await http.PutAsJsonAsync(""api/palette-history"", usage, cancellationToken)).EnsureSuccessStatusCode();
}";

    private readonly string example24RazorCode = @"
<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Commands=""dLabCommands""
                   Placeholder=""Try gs, fm, rc, mcp or Mcp…""
                   OnQueryChanged=""DLabQueryChanged"" />

<table>
    <thead>
        <tr><th>#</th><th>Command</th><th>Score</th><th>Rank</th></tr>
    </thead>
    <tbody>
        @if (dLabMatches.Count == 0)
        {
            <tr><td colspan=""4"">No command matches ""@dLabQuery"" as typed.</td></tr>
        }
        @foreach (var (match, index) in dLabMatches.Select((m, i) => (m, i)))
        {
            <tr>
                <td>@(index + 1)</td>
                <td><BitCommandPaletteHighlightedText Text=""@match.Command.Title"" Positions=""@match.TitlePositions"" /></td>
                <td>@DFormat(match.Score)</td>
                <td>@DFormat(match.Rank)</td>
            </tr>
        }
    </tbody>
</table>";
    private readonly string example24CsharpCode = @"
// The ranker the palette uses, over the default options: score, then priority and frecency.
private static readonly BitCommandPaletteRanker dRanker = new(new BitCommandPaletteOptions());

private static readonly BitCommandPaletteCommand[] dLabCommands =
[
    BitCommandPaletteCmd.New(""Go to settings"").Id(""d-lab-settings""),
    BitCommandPaletteCmd.New(""Git: stash changes"").Id(""d-lab-stash""),
    BitCommandPaletteCmd.New(""Toggle sidebar"").Id(""d-lab-sidebar""),
    BitCommandPaletteCmd.New(""FuzzyMatcher.cs"").Id(""d-lab-fuzzy""),
    BitCommandPaletteCmd.New(""Format document"").Id(""d-lab-format""),
    BitCommandPaletteCmd.New(""Open recent file"").Id(""d-lab-recent""),
    BitCommandPaletteCmd.New(""Reload window"").Id(""d-lab-reload""),
    BitCommandPaletteCmd.New(""Rename symbol"").Id(""d-lab-rename""),
    BitCommandPaletteCmd.New(""MCP server"").Id(""d-lab-mcp""),
];

private string dLabQuery = string.Empty;
private List<BitCommandPaletteMatch> dLabMatches = [];

protected override void OnInitialized()
{
    DLabQueryChanged(string.Empty);
}

private void DLabQueryChanged(string query)
{
    dLabQuery = query;

    // What the palette does on every keystroke: parse the query once, match every command, rank the matches.
    var parsed = BitCommandPaletteQuery.Parse(query);
    var matches = new List<BitCommandPaletteMatch>();
    foreach (var command in dLabCommands)
    {
        if (BitCommandPaletteFuzzyMatcher.Default.Match(command, parsed) is { } match)
        {
            matches.Add(match);
        }
    }

    // No frecency here, as the palette has EnableHistory=""false"".
    dLabMatches = dRanker.Rank(matches, hasQuery: parsed.Terms.Count > 0, frecencyOf: _ => 0);
}

private static string DFormat(double value) => value.ToString(""0.##"", CultureInfo.InvariantCulture);";

    private readonly string example25RazorCode = @"
<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   Commands=""dFoldCommands""
                   Placeholder=""Try cafe, lodz, علی, کتاب or ۱۲…"" />

<BitToggle Label=""Match literally (Normalize = false)"" @bind-Value=""dFoldLiteral"" />

<table>
    <thead>
        <tr><th>Typed</th><th>Finds</th><th>Why</th></tr>
    </thead>
    <tbody>
        @foreach (var example in dFoldExamples)
        {
            var match = DFoldMatch(example.Typed);
            <tr>
                <td dir=""auto""><code>@example.Typed</code></td>
                <td dir=""auto"">
                    @if (match is null)
                    {
                        <i>nothing</i>
                    }
                    else
                    {
                        <BitCommandPaletteHighlightedText Text=""@match.Command.Title"" Positions=""@match.TitlePositions"" />
                    }
                </td>
                <td>@example.Why</td>
            </tr>
        }
    </tbody>
</table>";
    private readonly string example25CsharpCode = @"
// Stored exactly as written: accents, Arabic-script letters, ASCII digits.
private static readonly BitCommandPaletteCommand[] dFoldCommands =
[
    BitCommandPaletteCmd.New(""Café"").Id(""d-fold-cafe""),
    BitCommandPaletteCmd.New(""Łódź"").Id(""d-fold-lodz""),
    BitCommandPaletteCmd.New(""Türkçe"").Id(""d-fold-turkce""),
    BitCommandPaletteCmd.New(""علي"").Id(""d-fold-ali""),
    BitCommandPaletteCmd.New(""فارسی"").Id(""d-fold-farsi""),
    BitCommandPaletteCmd.New(""كتاب"").Id(""d-fold-ketab""),
    BitCommandPaletteCmd.New(""إسم"").Id(""d-fold-esm""),
    BitCommandPaletteCmd.New(""العربية"").Id(""d-fold-arabic""),
    BitCommandPaletteCmd.New(""Order 12"").Id(""d-fold-order""),
    BitCommandPaletteCmd.New(""Step 3"").Id(""d-fold-step""),
];

private static readonly (string Typed, string Why)[] dFoldExamples =
[
    (""cafe"", ""Latin letters lose their diacritics.""),
    (""café"", ""A term that types a diacritic means that letter, and is matched as written.""),
    (""lodz"", ""Letters with a stroke fold too: Ł is L, as in fzf.""),
    (""turkce"", ""ü and ç are u and c.""),
    (""علی"", ""Typed with the Persian yeh (ی), stored with the Arabic one (ي).""),
    (""فارسي"", ""The other way round: typed with the Arabic yeh, stored with the Persian one.""),
    (""کتاب"", ""Typed with the Persian kaf (ک), stored with the Arabic one (ك).""),
    (""اسم"", ""Alef with hamza or madda is plain alef.""),
    (""العربيه"", ""Typed with heh (ه), stored with teh marbuta (ة).""),
    (""۱۲"", ""Persian digits are ASCII digits.""),
    (""٣"", ""Arabic-Indic digits too.""),
    (""ｏｒｄｅｒ"", ""Fullwidth letters, which some input methods type, are ASCII.""),
];

// fzf's --literal: every character as it is.
private static readonly IBitCommandPaletteMatcher dLiteralMatcher = new BitCommandPaletteFuzzyMatcher { Normalize = false };

private bool dFoldLiteral;

private BitCommandPaletteMatch? DFoldMatch(string typed)
{
    var matcher = dFoldLiteral ? dLiteralMatcher : BitCommandPaletteFuzzyMatcher.Default;
    var query = BitCommandPaletteQuery.Parse(typed);

    return dFoldCommands.Select(command => matcher.Match(command, query))
                        .OfType<BitCommandPaletteMatch>()
                        .MaxBy(match => match.Score);
}";

    private readonly string example26RazorCode = @"
<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   Commands=""dOperatorCommands""
                   Placeholder=""Try ^set, project$ or show !minimap…""
                   OnQueryChanged=""DOperatorQueryChanged"" />

<div>
    <span>BitCommandPaletteQuery.Parse:</span>
    @if (dOperatorQuery.IsTooLong)
    {
        <span>longer than @BitCommandPaletteQuery.MaxLength characters - no command matches.</span>
    }
    else if (dOperatorQuery.Terms.Count == 0)
    {
        <span>no terms - every command matches.</span>
    }
    @for (var i = 0; i < dOperatorQuery.Terms.Count; i++)
    {
        var term = dOperatorQuery.Terms[i];
        if (i > 0)
        {
            <span>@(dOperatorQuery.Terms[i - 1].Group == term.Group ? ""or"" : ""and"")</span>
        }
        <span>@term.Operator <code>@term.Text</code></span>
    }
</div>

<table>
    <thead>
        <tr><th>Query</th><th>Means</th></tr>
    </thead>
    <tbody>
        @foreach (var example in dOperatorExamples)
        {
            <tr>
                <td><code>@example.Query</code></td>
                <td>@example.Means</td>
            </tr>
        }
    </tbody>
</table>";
    private readonly string example26CsharpCode = @"
private static readonly BitCommandPaletteCommand[] dOperatorCommands =
[
    BitCommandPaletteCmd.New(""Toggle word wrap"").Id(""d-op-wrap"").Group(""View""),
    BitCommandPaletteCmd.New(""Show minimap"").Id(""d-op-minimap"").Group(""View""),
    BitCommandPaletteCmd.New(""Show breadcrumbs"").Id(""d-op-breadcrumbs"").Group(""View""),
    BitCommandPaletteCmd.New(""Wrap selection in a tag"").Id(""d-op-wrap-tag"").Group(""Edit""),
    BitCommandPaletteCmd.New(""Export project"").Id(""d-op-export"").Group(""File""),
    BitCommandPaletteCmd.New(""Open project"").Id(""d-op-open"").Group(""File""),
    BitCommandPaletteCmd.New(""Close project"").Id(""d-op-close"").Group(""File""),
    BitCommandPaletteCmd.New(""Settings"").Id(""d-op-settings"").Group(""Preferences""),
    BitCommandPaletteCmd.New(""Set the language"").Id(""d-op-language"").Group(""Preferences""),
    BitCommandPaletteCmd.New(""Change the theme"").Id(""d-op-theme"").Group(""Preferences""),
];

private static readonly (string Query, string Means)[] dOperatorExamples =
[
    (""wrap word"", ""Two fuzzy terms, both required, in any order.""),
    (""'export"", ""Exact: the text itself, no gaps.""),
    (""^set"", ""Prefix: a field starts with it.""),
    (""project$"", ""Suffix: a field ends with it.""),
    (""show !minimap"", ""Negation: drops every command that contains the term.""),
    (""\""word wrap\"""", ""A quoted phrase: matched exactly, the space included.""),
    (""!\""open project\"" project"", ""A negated phrase: the projects, except Open project.""),
    (""^\""set the\"""", ""An anchored phrase: a field starts with it.""),
    (""^change | ^set"", ""Alternatives: starts with change, or with set.""),
];

private BitCommandPaletteQuery dOperatorQuery = BitCommandPaletteQuery.Empty;

private void DOperatorQueryChanged(string query) => dOperatorQuery = BitCommandPaletteQuery.Parse(query);";

    private readonly string example27RazorCode = @"
<BitToggle Label=""TypoTolerance"" @bind-Value=""dTypoTolerance"" />

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   Commands=""dTypoCommands""
                   TypoTolerance=""dTypoTolerance""
                   Placeholder=""Try settigns, langauge, privacyy or notefications…"" />";
    private readonly string example27CsharpCode = @"
private bool dTypoTolerance = true;

private static readonly BitCommandPaletteCommand[] dTypoCommands =
[
    BitCommandPaletteCmd.New(""Settings"").Id(""d-typo-settings""),
    BitCommandPaletteCmd.New(""Language"").Id(""d-typo-language""),
    BitCommandPaletteCmd.New(""Privacy"").Id(""d-typo-privacy""),
    BitCommandPaletteCmd.New(""Notifications"").Id(""d-typo-notifications""),
    BitCommandPaletteCmd.New(""Appearance"").Id(""d-typo-appearance""),
    BitCommandPaletteCmd.New(""Keyboard shortcuts"").Id(""d-typo-shortcuts""),
];";

    private readonly string example28RazorCode = @"
<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   GroupWhenSearching=""false""
                   Commands=""dWeightCommands""
                   Placeholder=""Try color, col or clr…""
                   OnQueryChanged=""DWeightQueryChanged"" />

<table>
    <thead>
        <tr><th>Command</th><th>Field that won</th><th>Raw score</th><th>Weight</th><th>Weighted</th></tr>
    </thead>
    <tbody>
        @foreach (var row in DWeightRows())
        {
            <tr>
                <td>@row.Command</td>
                <td>@row.Field</td>
                <td>@DFormat(row.Raw)</td>
                <td>@DFormat(row.Weight)</td>
                <td><b>@DFormat(row.Raw * row.Weight)</b></td>
            </tr>
        }
    </tbody>
</table>";
    private readonly string example28CsharpCode = @"
// The default weights; options.Weights holds the app's.
private static readonly BitCommandPaletteMatchWeights dWeights = new();

// ""color"" in a different field of each one.
private static readonly BitCommandPaletteCommand[] dWeightCommands =
[
    BitCommandPaletteCmd.New(""Color picker"").Id(""d-weight-title""),
    BitCommandPaletteCmd.New(""Brand kit"").Id(""d-weight-keyword"").Keywords(""logo"", ""color""),
    BitCommandPaletteCmd.New(""Appearance"").Id(""d-weight-subtitle"").Subtitle(""Fonts, color and spacing""),
    BitCommandPaletteCmd.New(""Contrast checker"").Id(""d-weight-group"").Group(""Color""),
    BitCommandPaletteCmd.New(""Design tokens"").Id(""d-weight-description"").Description(""Radius, shadow and color values""),
    BitCommandPaletteCmd.New(""Export assets"").Id(""d-weight-tag"").Tags(""color""),
];

private string dWeightQuery = string.Empty;

private sealed record DWeightRow(string Command, string Field, double Raw, double Weight);

private void DWeightQueryChanged(string query) => dWeightQuery = query;

// What one term scores in each field: the field with the best weighted score counts.
private List<DWeightRow> DWeightRows()
{
    var term = dWeightQuery.Trim();
    var rows = new List<DWeightRow>();
    if (term.Length == 0) return rows;

    foreach (var command in dWeightCommands)
    {
        DWeightRow? best = null;
        foreach (var (field, text, weight) in DWeightFields(command))
        {
            var match = BitCommandPaletteFuzzyMatcher.Default.Match(term, text ?? string.Empty);
            if (match.Matched && (best is null || match.Score * weight > best.Raw * best.Weight))
            {
                best = new(command.Title, field, match.Score, weight);
            }
        }

        if (best is not null)
        {
            rows.Add(best);
        }
    }

    return [.. rows.OrderByDescending(row => row.Raw * row.Weight)];
}

private static IEnumerable<(string Field, string? Text, double Weight)> DWeightFields(BitCommandPaletteCommand command)
{
    yield return (""Title"", command.Title, dWeights.Title);
    foreach (var keyword in command.Keywords) yield return ($""Keyword \""{keyword}\"""", keyword, dWeights.Keyword);
    yield return (""Subtitle"", command.Subtitle, dWeights.Subtitle);
    yield return (""Group"", command.Group, dWeights.Group);
    yield return (""Description"", command.Description, dWeights.Description);
    foreach (var tag in command.Tags) yield return ($""Tag \""{tag}\"""", tag, dWeights.Tag);
}

private static string DFormat(double value) => value.ToString(""0.##"", CultureInfo.InvariantCulture);";

    private readonly string example29RazorCode = @"
<BitTextField Label=""Query"" @bind-Value=""dRankQuery"" Immediate />
<BitSlider Label=""PriorityWeight"" Min=""0"" Max=""2"" Step=""0.05"" @bind-Value=""dPriorityWeight"" />
<BitSlider Label=""FrecencyWeight"" Min=""0"" Max=""20"" Step=""1"" @bind-Value=""dFrecencyWeight"" />
<BitSlider Label=""Uses of Weekly report"" Min=""0"" Max=""20"" Step=""1"" @bind-Value=""dRankWeeklyUses"" />

<BitText>rank = score + priority × @DFormat(dPriorityWeight) + ln(1 + frecency) × @DFormat(dFrecencyWeight)</BitText>

<table>
    <thead>
        <tr><th>#</th><th>Command</th><th>Priority</th><th>Frecency</th><th>Score</th><th>Rank</th></tr>
    </thead>
    <tbody>
        @foreach (var (match, index) in DRankRows().Select((m, i) => (m, i)))
        {
            <tr>
                <td>@(index + 1)</td>
                <td><BitCommandPaletteHighlightedText Text=""@match.Command.Title"" Positions=""@match.TitlePositions"" /></td>
                <td>@match.Command.Priority</td>
                <td>@DFormat(match.Frecency)</td>
                <td>@DFormat(match.Score)</td>
                <td><b>@DFormat(match.Rank)</b></td>
            </tr>
        }
    </tbody>
</table>

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Commands=""dRankCommands""
                   Placeholder=""Type report…"" />";
    private readonly string example29CsharpCode = @"
private static readonly BitCommandPaletteCommand[] dRankCommands =
[
    BitCommandPaletteCmd.New(""Reports overview"").Id(""d-rank-overview"").Priority(BitCommandPaletteCommandPriority.High),
    BitCommandPaletteCmd.New(""Sales report"").Id(""d-rank-sales""),
    BitCommandPaletteCmd.New(""Weekly report"").Id(""d-rank-weekly""),
    BitCommandPaletteCmd.New(""Report a bug"").Id(""d-rank-bug"").Priority(BitCommandPaletteCommandPriority.Low),
    BitCommandPaletteCmd.New(""Archived reports"").Id(""d-rank-archived"").Priority(BitCommandPaletteCommandPriority.Lowest),
];

private string? dRankQuery = ""report"";
private double dPriorityWeight = 0.25;  // the default of options.PriorityWeight
private double dFrecencyWeight = 6;     // the default of options.FrecencyWeight
private double dRankWeeklyUses = 0;

private List<BitCommandPaletteMatch> DRankRows()
{
    // A ranker over options of its own; the palette's ranker uses the app's options.
    var ranker = new BitCommandPaletteRanker(new BitCommandPaletteOptions
    {
        PriorityWeight = dPriorityWeight,
        FrecencyWeight = dFrecencyWeight,
    });

    var query = BitCommandPaletteQuery.Parse(dRankQuery);
    var matches = dRankCommands.Select(command => BitCommandPaletteFuzzyMatcher.Default.Match(command, query))
                               .OfType<BitCommandPaletteMatch>();
    var now = DateTimeOffset.UtcNow;

    return ranker.Rank(matches, hasQuery: query.Terms.Count > 0, frecencyOf: id => DRankFrecency(id, now));
}

// Made-up usage: what the palette's history would have recorded.
private double DRankFrecency(string commandId, DateTimeOffset now) => commandId switch
{
    ""d-rank-weekly"" when dRankWeeklyUses > 0
        => BitCommandPaletteHistory.ComputeFrecency(new((int)dRankWeeklyUses, now.AddHours(-2), false), now),
    ""d-rank-bug""
        => BitCommandPaletteHistory.ComputeFrecency(new(3, now.AddDays(-5), false), now),
    _ => 0,
};

private static string DFormat(double value) => value.ToString(""0.##"", CultureInfo.InvariantCulture);";

    private readonly string example30RazorCode = @"
<BitChoiceGroup Label=""Matcher"" Horizontal TItem=""BitChoiceGroupOption<string>"" TValue=""string"" @bind-Value=""dMatcherName"">
    <BitChoiceGroupOption Text=""Fuzzy, smart case"" Value=""@(""fuzzy"")"" />
    <BitChoiceGroupOption Text=""Fuzzy, ignore case"" Value=""@(""ignore-case"")"" />
    <BitChoiceGroupOption Text=""Fuzzy, literal"" Value=""@(""literal"")"" />
    <BitChoiceGroupOption Text=""Substring"" Value=""@(""substring"")"" />
    <BitChoiceGroupOption Text=""DPrefixMatcher"" Value=""@(""prefix"")"" />
</BitChoiceGroup>

<BitTextField Label=""Query"" @bind-Value=""dMatcherQuery"" Immediate />

<table>
    <thead>
        <tr><th>#</th><th>Command</th><th>Score</th></tr>
    </thead>
    <tbody>
        @foreach (var (match, index) in DMatcherRows().Select((m, i) => (m, i)))
        {
            <tr>
                <td>@(index + 1)</td>
                <td><BitCommandPaletteHighlightedText Text=""@match.Command.Title"" Positions=""@match.TitlePositions"" /></td>
                <td>@DFormat(match.Score)</td>
            </tr>
        }
    </tbody>
</table>";
    private readonly string example30CsharpCode = @"
// For the whole app, in Program.cs:
// builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options => options.Matcher = BitCommandPaletteSubstringMatcher.Default);
// builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options => options.Matcher = new DPrefixMatcher());

// A matcher of your own: a field matches when it starts with the term.
private sealed class DPrefixMatcher : IBitCommandPaletteMatcher
{
    public BitCommandPaletteFieldMatch Match(string query, string text)
        => text.StartsWith(query, StringComparison.OrdinalIgnoreCase)
            ? new BitCommandPaletteFieldMatch(true, 100 - text.Length, [.. Enumerable.Range(0, query.Length)])
            : BitCommandPaletteFieldMatch.None;
}

private static readonly IBitCommandPaletteMatcher dIgnoreCaseMatcher = new BitCommandPaletteFuzzyMatcher { IgnoreCase = true };
private static readonly IBitCommandPaletteMatcher dLiteralMatcher = new BitCommandPaletteFuzzyMatcher { Normalize = false };
private static readonly IBitCommandPaletteMatcher dPrefixMatcher = new DPrefixMatcher();
private static readonly BitCommandPaletteRanker dRanker = new(new BitCommandPaletteOptions());

private static readonly BitCommandPaletteCommand[] dLabCommands =
[
    BitCommandPaletteCmd.New(""Go to settings"").Id(""d-lab-settings""),
    BitCommandPaletteCmd.New(""Git: stash changes"").Id(""d-lab-stash""),
    BitCommandPaletteCmd.New(""Toggle sidebar"").Id(""d-lab-sidebar""),
    BitCommandPaletteCmd.New(""FuzzyMatcher.cs"").Id(""d-lab-fuzzy""),
    BitCommandPaletteCmd.New(""Format document"").Id(""d-lab-format""),
    BitCommandPaletteCmd.New(""Open recent file"").Id(""d-lab-recent""),
    BitCommandPaletteCmd.New(""Reload window"").Id(""d-lab-reload""),
    BitCommandPaletteCmd.New(""Rename symbol"").Id(""d-lab-rename""),
    BitCommandPaletteCmd.New(""MCP server"").Id(""d-lab-mcp""),
];

private string? dMatcherName = ""fuzzy"";
private string? dMatcherQuery = ""re"";

private IBitCommandPaletteMatcher DSelectedMatcher() => dMatcherName switch
{
    ""ignore-case"" => dIgnoreCaseMatcher,
    ""literal"" => dLiteralMatcher,
    ""substring"" => BitCommandPaletteSubstringMatcher.Default,
    ""prefix"" => dPrefixMatcher,
    _ => BitCommandPaletteFuzzyMatcher.Default,
};

private List<BitCommandPaletteMatch> DMatcherRows()
{
    var matcher = DSelectedMatcher();
    var query = BitCommandPaletteQuery.Parse(dMatcherQuery);
    var matches = new List<BitCommandPaletteMatch>();
    foreach (var command in dLabCommands)
    {
        if (matcher.Match(command, query) is { } match)
        {
            matches.Add(match);
        }
    }

    return dRanker.Rank(matches, hasQuery: query.Terms.Count > 0, frecencyOf: _ => 0);
}

private static string DFormat(double value) => value.ToString(""0.##"", CultureInfo.InvariantCulture);";

    private readonly string example31RazorCode = @"
<BitCommandPaletteTrigger Query="">"" Label=""Run a command"" ShowShortcut=""false"" />
<BitCommandPaletteTrigger Query=""?"" Label=""Help"" ShowShortcut=""false"" />

<BitCommandPalette Inline Placeholder=""Type >, ? or >>, or pick a mode…"" />

<table>
    <thead>
        <tr><th>Prefix</th><th>Mode</th><th>Lists</th><th>Hint</th></tr>
    </thead>
    <tbody>
        @foreach (var mode in paletteService.Modes)
        {
            <tr>
                <td><code>@mode.Prefix</code></td>
                <td><b>@mode.Name</b> @mode.Description</td>
                <td>@DModeScope(mode)</td>
                <td>@(mode.ShowHint ? ""Yes"" : ""No"")</td>
            </tr>
        }
    </tbody>
</table>";
    private readonly string example31CsharpCode = @"
// In Program.cs, for the whole app:
// builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
// {
//     options.Modes.Add(new BitCommandPaletteCommandMode { Prefix = "">"", Name = ""Commands"", Tags = [""command""] });
//     options.ShowModeHints = false; // no ""Modes"" group on the empty query
// });

[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private readonly List<IDisposable> registrations = [];

private static readonly BitCommandPaletteCommand[] dModeCommands =
[
    BitCommandPaletteCmd.New(""Sort the lines"").Id(""d-mode-sort"").Tags(""command"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Sorted the lines""))),
    BitCommandPaletteCmd.New(""Toggle word wrap"").Id(""d-mode-wrap"").Tags(""command"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Toggled word wrap""))),
    BitCommandPaletteCmd.New(""Format the document"").Id(""d-mode-format"").Tags(""command"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Formatted the document""))),
    BitCommandPaletteCmd.New(""Keyboard shortcuts"").Id(""d-mode-shortcuts"").Group(""Help"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Ctrl+K opens the palette""))),
    BitCommandPaletteCmd.New(""Search syntax"").Id(""d-mode-syntax"").Group(""Help"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""'exact ^prefix suffix$ !not \""a phrase\""""))),
    BitCommandPaletteCmd.New(""Release notes"").Id(""d-mode-notes"").Group(""Help"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Nothing new today""))),
];

protected override void OnInitialized()
{
    registrations.Add(paletteService.Register(dModeCommands));

    registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
    {
        Prefix = "">"",
        Name = ""Commands"",
        Description = ""Run an editor command"",
        Placeholder = ""Run a command…"",
        Tags = [""command""],          // only the commands tagged ""command""
    }));

    registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
    {
        Prefix = ""?"",
        Name = ""Help"",
        Description = ""Browse the help topics"",
        Groups = [""Help""],           // only the Help group
    }));

    registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
    {
        Prefix = "">>"",               // the longest prefix wins over "">""
        Name = ""Everything"",
        Description = ""Every registered command"",
        ShowHint = false,            // not listed under Modes
    }));
}

private static string DModeScope(BitCommandPaletteCommandMode mode)
    => mode.HideRegistryCommands ? $""only its sources: {string.Join("", "", mode.Sources.Select(s => s.Name))}""
     : mode.Tags is { Count: > 0 } tags ? $""commands tagged {string.Join("", "", tags)}""
     : mode.Groups is { Count: > 0 } groups ? $""the {string.Join("", "", groups)} group""
     : ""every command"";

public void Dispose()
{
    foreach (var registration in registrations) registration.Dispose();
}";

    private readonly string example32RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(""@""))"">Open in People mode</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(""@ada lovelace""))"">Search @@ada lovelace</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(""status""))"">Search ""status""</BitButton>

<BitChoiceGroup Label=""The Service status source"" Horizontal TItem=""BitChoiceGroupOption<string>"" TValue=""string"" @bind-Value=""dStatusBehaviour"">
    <BitChoiceGroupOption Text=""Answers"" Value=""@(""answers"")"" />
    <BitChoiceGroupOption Text=""Throws"" Value=""@(""throws"")"" />
    <BitChoiceGroupOption Text=""Never answers"" Value=""@(""hangs"")"" />
</BitChoiceGroup>

<BitButton Variant=""BitVariant.Text"" OnClick=""DClearSourceCalls"">Clear the calls</BitButton>

<table>
    <thead>
        <tr><th>Started</th><th>Source</th><th>Query</th><th>Outcome</th><th>Results</th><th>Took</th></tr>
    </thead>
    <tbody>
        @{ var calls = DSourceCallsSnapshot(); }
        @if (calls.Length == 0)
        {
            <tr><td colspan=""6"">No source has been asked yet.</td></tr>
        }
        @foreach (var call in calls)
        {
            <tr>
                <td>@call.StartedAt.ToString(""HH:mm:ss.fff"", CultureInfo.InvariantCulture)</td>
                <td>@call.Source</td>
                <td><code>@(call.Query.Length == 0 ? ""(empty)"" : call.Query)</code></td>
                <td>@call.Outcome</td>
                <td>@(call.Outcome == ""Completed"" ? call.Results.ToString(CultureInfo.InvariantCulture) : ""-"")</td>
                <td>@(call.Outcome == ""Running"" ? ""-"" : DMs(call.Took))</td>
            </tr>
        }
    </tbody>
</table>";
    private readonly string example32CsharpCode = @"
// In Program.cs, for the whole app:
// builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
// {
//     options.Sources.Add(new DocsSource());   // asked in the default mode
//     options.DebounceMs = 150;                // quiet time after the last keystroke before a source is asked
//     options.SourceTimeoutMs = 10_000;        // then it counts as failed; 0 waits forever
// });
// builder.Services.AddBitCommandPaletteSource<OrdersSource>(); // a default-mode source resolved from DI, scoped

[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private readonly List<IDisposable> registrations = [];

private sealed record DPerson(string Name, string Role, string Email);

private static readonly DPerson[] dPeople =
[
    new(""Ada Lovelace"", ""Engineering"", ""ada@example.com""),
    new(""Grace Hopper"", ""Engineering"", ""grace@example.com""),
    new(""Alan Turing"", ""Research"", ""alan@example.com""),
    new(""Katherine Johnson"", ""Data"", ""katherine@example.com""),
    new(""Margaret Hamilton"", ""Engineering"", ""margaret@example.com""),
    new(""Linus Torvalds"", ""Platform"", ""linus@example.com""),
    new(""Barbara Liskov"", ""Architecture"", ""barbara@example.com""),
    new(""Dennis Ritchie"", ""Platform"", ""dennis@example.com""),
    new(""Hedy Lamarr"", ""Research"", ""hedy@example.com""),
    new(""Tim Berners-Lee"", ""Web"", ""tim@example.com""),
];

private static readonly BitCommandPaletteCommand[] dTeams =
[
    BitCommandPaletteCmd.New(""Platform team"").Id(""d-team-platform"").Subtitle(""Builds and runs the platform""),
    BitCommandPaletteCmd.New(""Research team"").Id(""d-team-research"").Subtitle(""Looks ahead""),
    BitCommandPaletteCmd.New(""Data team"").Id(""d-team-data"").Subtitle(""Owns the warehouse""),
    BitCommandPaletteCmd.New(""Web team"").Id(""d-team-web"").Subtitle(""Ships the sites""),
];

private static readonly (string Name, string State)[] dServiceStates =
[
    (""API"", ""Operational""),
    (""Web app"", ""Operational""),
    (""Database"", ""Degraded performance""),
    (""Search index"", ""Operational""),
    (""E-mail delivery"", ""Operational""),
];

private string? dStatusBehaviour = ""answers"";

protected override void OnInitialized()
{
    registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
    {
        Prefix = ""@"",
        Name = ""People"",
        Description = ""Find a teammate or a team"",
        Placeholder = ""Search people…"",
        HideRegistryCommands = true,   // only the sources' rows
        Sources = [new DPeopleSource(this), new BitCommandPaletteStaticSource(""Teams"", dTeams)],
    }));

    // Asked in the default mode, from 3 characters on.
    registrations.Add(paletteService.RegisterSource(new BitCommandPaletteDelegateSource(""Service status"", DSearchStatusAsync, minQueryLength: 3)));
}

// Re-ranked: returns candidates, the palette scores and highlights them with everything else.
private sealed class DPeopleSource(BitCommandPaletteDemo page) : IBitCommandPaletteSource
{
    public string Name => ""People"";        // the heading of its results
    public int MinQueryLength => 1;        // 0 also asks on the empty query
    public int MaxResults => 20;
    public bool ApplyMatcher => true;      // re-rank with the palette's matcher

    public Task<IReadOnlyList<BitCommandPaletteCommand>> SearchAsync(string query, BitCommandPaletteSourceContext context, CancellationToken cancellationToken)
        => page.DTrackAsync(Name, query, async ct =>
        {
            await Task.Delay(250, ct);     // stands in for the API call; the token cancels it when the query moves on

            return (IReadOnlyList<BitCommandPaletteCommand>)[.. dPeople
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Role.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(p => BitCommandPaletteCmd.New(p.Name)
                    .Id($""d-person-{p.Email}"")             // an id of its own: names are not unique
                    .Subtitle(p.Role)
                    .Keywords(p.Email)
                    .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($""Opened the profile of {p.Name}"")))
                    .Build())];
        }, cancellationToken);
}

// Ordered (ApplyMatcher is false): trouble first, kept as one block under the source's name.
private Task<IReadOnlyList<BitCommandPaletteCommand>> DSearchStatusAsync(string query, CancellationToken cancellationToken)
    => DTrackAsync(""Service status"", query, async ct =>
    {
        switch (dStatusBehaviour)
        {
            case ""throws"":
                await Task.Delay(200, ct);
                throw new InvalidOperationException(""The status service answered 502 Bad Gateway."");
            case ""hangs"":
                await Task.Delay(Timeout.Infinite, ct);   // cancelled when SourceTimeoutMs passes
                break;
        }

        await Task.Delay(200, ct);
        var everything = query.Contains(""status"", StringComparison.OrdinalIgnoreCase);
        return (IReadOnlyList<BitCommandPaletteCommand>)[.. dServiceStates
            .Where(s => everything || s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.State == ""Operational"")
            .Select(s => BitCommandPaletteCmd.New(s.Name)
                .Id($""d-status-{s.Name}"")
                .Subtitle(s.State)
                .ExcludeFromHistory()
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($""{s.Name}: {s.State}"", keepOpen: true)))
                .Build())];
    }, cancellationToken);

// The call log: every call that reached a source, and how it ended.
private sealed class DSourceCall(string source, string query)
{
    public string Source { get; } = source;
    public string Query { get; } = query;
    public DateTime StartedAt { get; } = DateTime.Now;
    public string Outcome { get; set; } = ""Running"";
    public int Results { get; set; }
    public TimeSpan Took { get; set; }
}

private readonly List<DSourceCall> dSourceCalls = [];

private async Task<IReadOnlyList<BitCommandPaletteCommand>> DTrackAsync(string source, string query,
    Func<CancellationToken, Task<IReadOnlyList<BitCommandPaletteCommand>>> search, CancellationToken cancellationToken)
{
    var call = new DSourceCall(source, query);
    lock (dSourceCalls)
    {
        dSourceCalls.Insert(0, call);
        if (dSourceCalls.Count > 12) dSourceCalls.RemoveAt(dSourceCalls.Count - 1);
    }

    _ = InvokeAsync(StateHasChanged);
    var started = System.Diagnostics.Stopwatch.GetTimestamp();
    try
    {
        var results = await search(cancellationToken);
        call.Results = results.Count;
        call.Outcome = ""Completed"";
        return results;
    }
    catch (OperationCanceledException)
    {
        call.Outcome = ""Cancelled"";
        throw;
    }
    catch
    {
        call.Outcome = ""Failed"";
        throw;
    }
    finally
    {
        call.Took = System.Diagnostics.Stopwatch.GetElapsedTime(started);
        _ = InvokeAsync(StateHasChanged);
    }
}

private DSourceCall[] DSourceCallsSnapshot()
{
    lock (dSourceCalls) return [.. dSourceCalls];
}

private void DClearSourceCalls()
{
    lock (dSourceCalls) dSourceCalls.Clear();
}

private static string DMs(TimeSpan value) => $""{value.TotalMilliseconds.ToString(""0.##"", CultureInfo.InvariantCulture)} ms"";

public void Dispose()
{
    foreach (var registration in registrations) registration.Dispose();
}";

    private readonly string example33RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(""$""))"">Open in Packages mode</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => paletteService.OpenAsync(""$acme""))"">Search $acme</BitButton>";
    private readonly string example33CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private readonly List<IDisposable> registrations = [];

private const int DPackagePageSize = 10;

// Sixty packages: four vendors, fifteen areas.
private static readonly string[] dPackages =
[
    .. from vendor in new[] { ""Acme"", ""Contoso"", ""Fabrikam"", ""Northwind"" }
       from area in new[] { ""Core"", ""Http"", ""Json"", ""Logging"", ""Caching"", ""Auth"", ""Data"", ""Testing"", ""Blazor"", ""Grpc"", ""Metrics"", ""Search"", ""Storage"", ""Mail"", ""Queue"" }
       select $""{vendor}.{area}""
];

protected override void OnInitialized()
{
    registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
    {
        Prefix = ""$"",
        Name = ""Packages"",
        Description = $""Browse {dPackages.Length} packages, {DPackagePageSize} at a time"",
        Placeholder = ""Search packages…"",
        HideRegistryCommands = true,
        Sources =
        [
            new BitCommandPaletteDelegateSource(""Packages"",
                (query, cursor, context, ct) => DSearchPackagesAsync(query, cursor, ct),
                minQueryLength: 0,                 // opens on a list
                maxResults: DPackagePageSize),
        ],
    }));
}

// One page of the packages matching the query.
private async Task<BitCommandPaletteSourcePage> DSearchPackagesAsync(string query, string? cursor, CancellationToken cancellationToken)
{
    // The cursor is the offset of the page here; a real API would hand out an opaque token.
    var offset = int.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var from) ? from : 0;
    var hits = dPackages.Where(p => p.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    await Task.Delay(200, cancellationToken); // stands in for the API call

    IReadOnlyList<BitCommandPaletteCommand> commands = [.. hits.Skip(offset).Take(DPackagePageSize).Select(name => BitCommandPaletteCmd.New(name)
        .Id($""d-package-{name}"")
        .Subtitle($""{2 + name.Length % 5}.{name.Length % 10}.0"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($""Opened {name}"")))
        .Build())];

    var next = offset + DPackagePageSize < hits.Count ? (offset + DPackagePageSize).ToString(CultureInfo.InvariantCulture) : null;
    return new BitCommandPaletteSourcePage(commands, next, hits.Count); // the total feeds ""Show N more…""
}

public void Dispose()
{
    foreach (var registration in registrations) registration.Dispose();
}";

    private readonly string example34RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""dPageSourceCommands"" />

<BitText>@dAssignStatus</BitText>";
    private readonly string example34CsharpCode = @"
private string dAssignStatus = ""Nobody assigned yet."";
private BitCommandPaletteCommand[] dPageSourceCommands = default!;

protected override void OnInitialized()
{
    dPageSourceCommands =
    [
        BitCommandPaletteCmd.New(""Assign to…"")
            .Id(""d-page-assign"")
            .ChildrenPlaceholder(""Search people…"")
            .Source(""People"", DSearchPeopleForPageAsync),

        BitCommandPaletteCmd.New(""Request reviews…"")
            .Id(""d-page-reviewers"")
            .ChildrenPlaceholder(""Search people, check several, Ctrl+Enter to apply…"")
            .MultiSelect()
            .Source(""People"", DSearchPeopleForPageAsync)
            .Handler(ctx => DSetAssignStatus(ctx.SelectedCommands.Count == 0
                ? ""No reviews requested.""
                : $""Reviews requested from {string.Join("", "", ctx.SelectedCommands.Select(c => c.Title))}."")),

        BitCommandPaletteCmd.New(""Browse packages…"")
            .Id(""d-page-packages"")
            .Source(""Packages"", (query, cursor, ct) => DSearchPackagesAsync(query, cursor, ct), maxResults: DPackagePageSize),
    ];
}

private async Task<IReadOnlyList<BitCommandPaletteCommand>> DSearchPeopleForPageAsync(string query, CancellationToken cancellationToken)
{
    await Task.Delay(250, cancellationToken); // stands in for the API call

    return [.. dPeople
        .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        .Select(p => BitCommandPaletteCmd.Action(p.Name, () => DSetAssignStatus($""Assigned to {p.Name}.""))
            .Id($""d-assign-{p.Email}"")
            .Subtitle(p.Role)
            .Build())];
}

private void DSetAssignStatus(string status)
{
    dAssignStatus = status;
    _ = InvokeAsync(StateHasChanged);
}

// dPeople as in ""Async sources"", DSearchPackagesAsync and DPackagePageSize as in ""Paging"".";

    private readonly string example35RazorCode = @"
<BitChoiceGroup Label=""Commands""
                Horizontal
                TItem=""BitChoiceGroupOption<int>""
                TValue=""int""
                Disabled=""dPerfRegistration is not null""
                @bind-Value=""dPerfCount""
                @bind-Value:after=""DBuildPerfCommands"">
    <BitChoiceGroupOption Text=""500"" Value=""500"" />
    <BitChoiceGroupOption Text=""2,000"" Value=""2000"" />
    <BitChoiceGroupOption Text=""10,000"" Value=""10000"" />
</BitChoiceGroup>
<BitToggle Label=""Virtualize"" @bind-Value=""dPerfVirtualize"" />

<BitButton Disabled=""dPerfBusy"" OnClick=""DMeasureAsync"">@(dPerfBusy ? ""Measuring…"" : ""Measure searches"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""DTogglePerfRegistration"">@(dPerfRegistration is null ? ""Register them with the service"" : ""Unregister them"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" Disabled=""dPerfRegistration is null"" OnClick=""@(() => paletteService.OpenAsync(""cmd 1234""))"">Open the modal with ""cmd 1234""</BitButton>

@if (dPerfRegisterTime is { } took)
{
    <BitText>The last registration took @DMs(took).</BitText>
}

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Commands=""dPerfCommands""
                   Virtualize=""dPerfVirtualize""
                   Placeholder=""Try c12, cmd 1234 or ^command 7…"" />

@if (dPerfResults.Count > 0)
{
    <table>
        <thead>
            <tr><th>Query</th><th>Matched</th><th>Match</th><th>Rank</th><th>Total</th><th>Static source</th></tr>
        </thead>
        <tbody>
            @foreach (var result in dPerfResults)
            {
                <tr>
                    <td><code>@result.Query</code></td>
                    <td>@result.Matched.ToString(""N0"", CultureInfo.InvariantCulture)</td>
                    <td>@DMs(result.Match)</td>
                    <td>@DMs(result.Rank)</td>
                    <td><b>@DMs(result.Match + result.Rank)</b></td>
                    <td>@DMs(result.Source)</td>
                </tr>
            }
        </tbody>
    </table>
}";
    private readonly string example35CsharpCode = @"
// In Program.cs, for long lists:
// builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
// {
//     options.MaxResults = 1000;   // rows kept per query
//     options.Virtualize = true;   // render only the visible ones
//     options.RowHeight = 44;      // every row's height while virtualized, in pixels
// });

[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;
[Inject] private IServiceProvider dServices { get; set; } = default!;

private readonly List<IDisposable> registrations = [];

private static readonly BitCommandPaletteRanker dRanker = new(new BitCommandPaletteOptions());
private static readonly string[] dPerfQueries = [""c12"", ""cmd 1234"", ""command"", ""synthetic 19"", ""^command 7"", ""zzz""];

private sealed record DPerfMeasurement(string Query, int Matched, TimeSpan Match, TimeSpan Rank, TimeSpan Source);

private readonly List<DPerfMeasurement> dPerfResults = [];
private List<BitCommandPaletteCommand> dPerfCommands = [];
private int dPerfCount = 2000;
private bool dPerfVirtualize;
private bool dPerfBusy;
private IDisposable? dPerfRegistration;
private TimeSpan? dPerfRegisterTime;

protected override void OnInitialized()
{
    DBuildPerfCommands();
}

// A new list each time: the palette takes a new snapshot when Commands gets a new collection.
private void DBuildPerfCommands()
{
    dPerfCommands = [.. Enumerable.Range(1, dPerfCount).Select(i => BitCommandPaletteCmd.New($""Command {i}"")
        .Id($""d-perf-{i}"")
        .Group(""Bulk"")
        .Subtitle($""Synthetic entry #{i}"")
        .Priority(BitCommandPaletteCommandPriority.Lowest)
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($""Ran command {i}"", keepOpen: true)))
        .Build())];
    dPerfResults.Clear();
}

private void DTogglePerfRegistration()
{
    var started = System.Diagnostics.Stopwatch.GetTimestamp();
    if (dPerfRegistration is null)
    {
        dPerfRegistration = paletteService.Register(dPerfCommands);
        registrations.Add(dPerfRegistration);
    }
    else
    {
        dPerfRegistration.Dispose();
        registrations.Remove(dPerfRegistration);
        dPerfRegistration = null;
    }

    dPerfRegisterTime = System.Diagnostics.Stopwatch.GetElapsedTime(started);
}

private async Task DMeasureAsync()
{
    dPerfBusy = true;
    dPerfResults.Clear();

    var commands = dPerfCommands;
    var source = new BitCommandPaletteStaticSource(""Catalog"", commands);
    var context = new BitCommandPaletteSourceContext(null, null, dServices);
    foreach (var text in dPerfQueries)
    {
        // WebAssembly runs on the page's only thread: yielding lets the table fill in as it goes.
        await Task.Delay(1);

        var query = BitCommandPaletteQuery.Parse(text);
        List<TimeSpan> matchTimes = [], rankTimes = [], sourceTimes = [];
        var matched = 0;
        for (var run = 0; run < 5; run++)
        {
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            var matches = new List<BitCommandPaletteMatch>();
            foreach (var command in commands)
            {
                if (BitCommandPaletteFuzzyMatcher.Default.Match(command, query) is { } match)
                {
                    matches.Add(match);
                }
            }
            matchTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started));

            started = System.Diagnostics.Stopwatch.GetTimestamp();
            dRanker.Rank(matches, hasQuery: true, frecencyOf: _ => 0);
            rankTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started));
            matched = matches.Count;

            started = System.Diagnostics.Stopwatch.GetTimestamp();
            await source.SearchAsync(text, context, CancellationToken.None);
            sourceTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started));
        }

        dPerfResults.Add(new(text, matched, DMedian(matchTimes), DMedian(rankTimes), DMedian(sourceTimes)));
        StateHasChanged();
    }

    dPerfBusy = false;
}

private static TimeSpan DMedian(List<TimeSpan> values)
{
    values.Sort();
    return values[values.Count / 2];
}

private static string DMs(TimeSpan value) => $""{value.TotalMilliseconds.ToString(""0.##"", CultureInfo.InvariantCulture)} ms"";

public void Dispose()
{
    foreach (var registration in registrations) registration.Dispose();
}";

    private readonly string example36RazorCode = @"
<BitToggle Label=""Loop"" @bind-Value=""eLoop"" />
<BitToggle Label=""VimBindings"" @bind-Value=""eVimBindings"" />
<BitToggle Label=""DisablePointerSelection"" @bind-Value=""eDisablePointerSelection"" />
<BitToggle Label=""Right to left"" @bind-Value=""eRtl"" />

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   Commands=""eKeyCommands""
                   Loop=""eLoop""
                   VimBindings=""eVimBindings""
                   DisablePointerSelection=""eDisablePointerSelection""
                   Dir=""@(eRtl ? BitDir.Rtl : BitDir.Ltr)"" />

<BitText>@eKeyStatus</BitText>";
    private readonly string example36CsharpCode = @"
private bool eLoop = true;
private bool eVimBindings;
private bool eDisablePointerSelection;
private bool eRtl;
private string eKeyStatus = ""Nothing run yet."";
private BitCommandPaletteCommand[] eKeyCommands = default!;

protected override void OnInitialized()
{
    eKeyCommands =
    [
        BitCommandPaletteCmd.Action(""Open the inbox"", () => EKeyRan(""Opened the inbox"")).Id(""e-key-inbox"").Group(""Mail"")
            .Actions(
                BitCommandPaletteCmd.Action(""Mark all as read"", () => EKeyRan(""Marked all as read"")).Id(""e-key-inbox-read""),
                BitCommandPaletteCmd.Action(""Archive everything"", () => EKeyRan(""Archived everything"")).Id(""e-key-inbox-archive"")),
        BitCommandPaletteCmd.Action(""Compose a message"", () => EKeyRan(""Composed a message"")).Id(""e-key-compose"").Group(""Mail""),
        BitCommandPaletteCmd.Action(""Reply to all"", () => EKeyRan(""Replied to all"")).Id(""e-key-reply-all"").Group(""Mail""),
        BitCommandPaletteCmd.Action(""Forward the thread"", () => EKeyRan(""Forwarded the thread"")).Id(""e-key-forward"").Group(""Mail""),
        BitCommandPaletteCmd.Action(""Snooze the thread"", () => EKeyRan(""Snoozed the thread"")).Id(""e-key-snooze"").Group(""Mail""),
        BitCommandPaletteCmd.Action(""Mute the thread"", () => EKeyRan(""Muted the thread"")).Id(""e-key-mute"").Group(""Mail""),
        BitCommandPaletteCmd.Page(""Folders"",
            BitCommandPaletteCmd.Action(""Drafts"", () => EKeyRan(""Opened Drafts"")).Id(""e-key-drafts""),
            BitCommandPaletteCmd.Action(""Sent"", () => EKeyRan(""Opened Sent"")).Id(""e-key-sent""),
            BitCommandPaletteCmd.Action(""Spam"", () => EKeyRan(""Opened Spam"")).Id(""e-key-spam""))
            .Id(""e-key-folders"").Group(""Organize""),
        BitCommandPaletteCmd.Page(""Label the thread"",
            BitCommandPaletteCmd.New(""Work"").Id(""e-key-label-work""),
            BitCommandPaletteCmd.New(""Family"").Id(""e-key-label-family""),
            BitCommandPaletteCmd.New(""Travel"").Id(""e-key-label-travel""))
            .Id(""e-key-labels"").Group(""Organize"")
            .MultiSelect()
            .Handler(ctx => EKeyRan(ctx.SelectedCommands.Count == 0
                ? ""Removed every label""
                : $""Labelled: {string.Join("", "", ctx.SelectedCommands.Select(c => c.Title))}"")),
        BitCommandPaletteCmd.Action(""Print the thread"", () => EKeyRan(""Printed the thread (not really)"")).Id(""e-key-print"").Group(""Organize""),
        BitCommandPaletteCmd.Action(""Block the sender"", () => EKeyRan(""Blocked the sender"")).Id(""e-key-block"").Group(""Organize""),
        BitCommandPaletteCmd.Action(""Report spam"", () => EKeyRan(""Reported spam"")).Id(""e-key-report"").Group(""Organize""),
        BitCommandPaletteCmd.Action(""Empty the trash"", () => EKeyRan(""Emptied the trash (not really)"")).Id(""e-key-trash"").Group(""Organize"")
            .Destructive(""Empty the trash? Press Enter again.""),
    ];
}

private void EKeyRan(string what)
{
    eKeyStatus = what;
    // A handler run from a key press re-renders the palette, not this page.
    _ = InvokeAsync(StateHasChanged);
}";

    private readonly string example37RazorCode = @"
<table>
    @foreach (var command in paletteService.Commands.Where(c => c.Shortcut is not null && c.GlobalShortcut))
    {
        <tr>
            <td><BitCommandPaletteShortcutHint Shortcut=""command.Shortcut"" MacKeys=""paletteService.IsMac"" /></td>
            <td>@command.Title</td>
            <td>@(command.Shortcut!.IsSequence ? ""sequence"" : ""chord"")</td>
        </tr>
    }
</table>

<BitText>Last run by a shortcut: <b>@(eShortcutLast ?? ""nothing yet"")</b></BitText>

<BitTextField Label=""Typing is not a shortcut"" Placeholder=""Type g then n here…"" />";
    private readonly string example37CsharpCode = @"
// Program.cs - on by default: a plain key pressed in a text field is typing. Off, G then N fires there too.
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options => options.SuppressShortcutsInEditableElements = true);

// The page
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private readonly List<IDisposable> registrations = [];
private string? eShortcutLast;

protected override void OnInitialized()
{
    registrations.Add(paletteService.Register(
        BitCommandPaletteCmd.Action(""New note"", () => EShortcutRan(""New note""))
            .Id(""e-note-new"").Group(""Notes"").Shortcut(""Alt+Shift+N""),          // a chord
        BitCommandPaletteCmd.Action(""Go to notes"", () => EShortcutRan(""Go to notes""))
            .Id(""e-note-go"").Group(""Notes"").Shortcut(""G N""),                   // a sequence: G, then N
        BitCommandPaletteCmd.Page(""Change the note color"",
                BitCommandPaletteCmd.Action(""Yellow"", () => EShortcutRan(""Color: yellow"")).Id(""e-note-yellow""),
                BitCommandPaletteCmd.Action(""Blue"", () => EShortcutRan(""Color: blue"")).Id(""e-note-blue""),
                BitCommandPaletteCmd.Action(""Green"", () => EShortcutRan(""Color: green"")).Id(""e-note-green""))
            .Id(""e-note-color"").Group(""Notes"").Shortcut(""G C""),                // opens the palette on its page
        BitCommandPaletteCmd.Action(""Discard the draft"", () => EShortcutRan(""Discard the draft""))
            .Id(""e-note-discard"").Group(""Notes"").Shortcut(""Alt+Shift+D"")
            .Destructive(""Discard the draft?""),                                // opens the palette to confirm
        BitCommandPaletteCmd.Action(""Preview the note"", () => EShortcutRan(""Preview the note""))
            .Id(""e-note-preview"").Group(""Notes"").Shortcut(""Space"", global: false))); // a hint on its row only

    // The table lists the registry: render again when it changes, and once the platform (⌘ or Ctrl) is known.
    paletteService.CommandsChanged += ERefresh;
    paletteService.PlatformDetected += ERefresh;
}

private void EShortcutRan(string title)
{
    eShortcutLast = $""{title} at {DateTime.Now:HH:mm:ss}"";
    // The modal palette ran the handler: re-render this page.
    _ = InvokeAsync(StateHasChanged);
}

private void ERefresh() => _ = InvokeAsync(StateHasChanged);

public void Dispose()
{
    // The service outlives the page: unregister the commands and unsubscribe, or the page stays alive and rendering.
    foreach (var registration in registrations) registration.Dispose();
    paletteService.CommandsChanged -= ERefresh;
    paletteService.PlatformDetected -= ERefresh;
}";

    private readonly string example38RazorCode = @"
<BitText>Last run by a shortcut: <b>@(eShortcutLast ?? ""nothing yet"")</b></BitText>";
    private readonly string example38CsharpCode = @"
// Written once, in Latin letters: G N also fires where a Persian layout types ل then د,
// and Alt+Shift+N where a Russian one types т.
paletteService.Register(
    BitCommandPaletteCmd.Action(""New note"", () => EShortcutRan(""New note"")).Shortcut(""Alt+Shift+N""),
    BitCommandPaletteCmd.Action(""Go to notes"", () => EShortcutRan(""Go to notes"")).Shortcut(""G N""));";

    private readonly string example39RazorCode = @"
<BitText>
    Sequence:
    <b>@(eSequencePending is null ? ""none in progress"" : $""{eSequencePending.ToDisplayString(paletteService.IsMac)}, waiting for the next key"")</b>
</BitText>";
    private readonly string example39CsharpCode = @"
// Program.cs
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    options.SequenceTimeoutMs = 1500;   // how long G waits for the next key; must be positive
    options.ShowSequenceHint = false;   // no built-in status: the app shows it, as below
});

// The page
private BitCommandPaletteKeyboardShortcut? eSequencePending;

protected override void OnInitialized()
{
    paletteService.SequencePending += EOnSequencePending;
}

private void EOnSequencePending(BitCommandPaletteKeyboardShortcut? pending)
{
    // ""G"" after the first key of G N, null once it completed, broke off or timed out.
    eSequencePending = pending;
    _ = InvokeAsync(StateHasChanged);
}

public void Dispose() => paletteService.SequencePending -= EOnSequencePending;";

    private readonly string example40RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""EToggleConflict"">@EConflictButtonText</BitButton>

<BitText>
    <kbd>Alt</kbd> <kbd>Q</kbd> runs <b>@(eConflictOverride is null ? ""Save draft"" : ""Save and publish"")</b>,
    last pressed: <b>@(eConflictLast ?? ""not yet"")</b>
</BitText>";
    private readonly string example40CsharpCode = @"
private IDisposable? eConflictOverride;
private string? eConflictLast;

private string EConflictButtonText => eConflictOverride is null
    ? ""Register \""Save and publish\"" on Alt+Q""
    : ""Dispose \""Save and publish\"""";

protected override void OnInitialized()
{
    // The page's own command on Alt+Q.
    registrations.Add(paletteService.Register(
        BitCommandPaletteCmd.Action(""Save draft"", () => EConflictRan(""Save draft""))
            .Id(""e-conflict-save"").Group(""Conflicts"").Shortcut(""Alt+Q"")));
}

private void EToggleConflict()
{
    if (eConflictOverride is not null)
    {
        // Disposed, and Alt+Q is Save draft's again.
        eConflictOverride.Dispose();
        eConflictOverride = null;
        return;
    }

    // A second command on the same keys: registered later, so it wins them, and the palette logs a warning.
    eConflictOverride = paletteService.Register(
        BitCommandPaletteCmd.Action(""Save and publish"", () => EConflictRan(""Save and publish""))
            .Id(""e-conflict-publish"").Group(""Conflicts"").Shortcut(""Alt+Q""));
}

private void EConflictRan(string title)
{
    eConflictLast = $""{title} at {DateTime.Now:HH:mm:ss}"";
    _ = InvokeAsync(StateHasChanged);
}

public void Dispose() => eConflictOverride?.Dispose();";

    private readonly string example41RazorCode = @"
<BitTextField Label=""Shortcut"" @bind-Value=""eShortcutText"" Immediate />

@foreach (var sample in eShortcutSamples)
{
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => eShortcutText = sample"">@sample</BitButton>
}

@if (BitCommandPaletteKeyboardShortcut.TryParse(eShortcutText, out var eParsed))
{
    <table>
        <tr><th>On macOS</th><td><BitCommandPaletteShortcutHint Shortcut=""eParsed"" MacKeys=""true"" /></td></tr>
        <tr><th>Elsewhere</th><td><BitCommandPaletteShortcutHint Shortcut=""eParsed"" MacKeys=""false"" /></td></tr>
        <tr><th>In words</th><td>@eParsed.ToDisplayString(isMac: false)</td></tr>
        <tr><th>Normalized</th><td><code>@eParsed.Normalized</code></td></tr>
        <tr><th>Kind</th><td>@(eParsed.IsSequence ? $""a sequence of {eParsed.Chords.Count} chords"" : ""a chord"")</td></tr>
    </table>
}
else
{
    <BitText>Not a shortcut: <code>TryParse</code> returns false, and <code>Parse</code> would throw a <code>FormatException</code>.</BitText>
}";
    private readonly string example41CsharpCode = @"
private string? eShortcutText = ""Mod+Shift+K"";
private static readonly string[] eShortcutSamples = [""Mod+K"", ""Ctrl+Shift+P"", ""G I"", ""Cmd+Option+Up"", ""Shift+?"", ""Alt+Comma"", ""Ctrl+Shift"", ""A+B""];

// Everywhere a shortcut is expected, a string converts implicitly:
BitCommandPaletteCmd.New(""Copy share link"").Shortcut(""Mod+Shift+C"");     // a chord: ⌘⇧C or Ctrl+Shift+C
BitCommandPaletteCmd.Navigate(""Settings"", ""/settings"").Shortcut(""G S"");   // a sequence: G, then S
BitCommandPaletteKeyboardShortcut shortcut = ""Ctrl+Shift+P"";
var chord = new BitCommandPaletteKeyChord(""p"", Ctrl: true, Shift: true);  // the same chord, built
bool ok = BitCommandPaletteKeyboardShortcut.TryParse(""Ctrl+Shift"", out _); // false: no key";

    private readonly string example42RazorCode = @"
<BitText>This browser is <b>@(paletteService.IsMac ? ""a Mac"" : ""not a Mac"")</b>.</BitText>

<BitCommandPaletteShortcutHint Shortcut=""eHintChord"" MacKeys=""paletteService.IsMac"" />
<BitCommandPaletteShortcutHint Shortcut=""eHintSequence"" MacKeys=""paletteService.IsMac"" />

<BitCommandPaletteShortcutHint Shortcut=""eHintChord"" MacKeys=""true"" />
<BitCommandPaletteShortcutHint Shortcut=""eHintSequence"" MacKeys=""true"" />

<BitCommandPaletteShortcutHint Shortcut=""eHintChord"" MacKeys=""false"" />
<BitCommandPaletteShortcutHint Shortcut=""eHintSequence"" MacKeys=""false"" />

<BitCommandPaletteTrigger />
<BitCommandPaletteTrigger MacKeys=""true"" />
<BitCommandPaletteTrigger MacKeys=""false"" />";
    private readonly string example42CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private static readonly BitCommandPaletteKeyboardShortcut eHintChord = ""Mod+Shift+K"";
private static readonly BitCommandPaletteKeyboardShortcut eHintSequence = ""G S"";

// IsMac is known once the palette has started in the browser.
protected override void OnInitialized() => paletteService.PlatformDetected += ERefresh;

private void ERefresh() => _ = InvokeAsync(StateHasChanged);

public void Dispose() => paletteService.PlatformDetected -= ERefresh;";

    private readonly string example43RazorCode = @"
@inject BitCommandPaletteOptions eOptions

@foreach (var toggle in EToggleShortcuts)
{
    <BitCommandPaletteShortcutHint Shortcut=""toggle"" MacKeys=""paletteService.IsMac"" />
}

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => paletteService.ToggleAsync()"">paletteService.ToggleAsync()</BitButton>

@* One palette with its own main toggle: *@
<BitCommandPalette ToggleShortcut=""Mod+P"" />";
    private readonly string example43CsharpCode = @"
// Program.cs
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    options.ToggleShortcut = ""Mod+K"";                      // the default; null for none
    options.AdditionalToggleShortcuts.Add(""Mod+Shift+P"");  // more keys for the same
});

// The page
private IEnumerable<BitCommandPaletteKeyboardShortcut> EToggleShortcuts
    => eOptions.ToggleShortcut is null
        ? eOptions.AdditionalToggleShortcuts
        : [eOptions.ToggleShortcut, .. eOptions.AdditionalToggleShortcuts];";

    private readonly string example44RazorCode = @"
@foreach (var query in eFinderQueries)
{
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => eFinder.SetQueryAsync(query)"">SetQueryAsync(""@query"")</BitButton>
}

<BitCommandPalette @ref=""eFinder""
                   Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Placeholder=""Search the options…""
                   Commands=""eOptionCommands""
                   OnQueryChanged=""query => eFinderQuery = query"" />

<BitText>
    OnQueryChanged: <b>@(eFinderQuery.Length == 0 ? ""(empty)"" : eFinderQuery)</b>,
    picked: <b>@(eOptionPicked ?? ""nothing yet"")</b>
</BitText>";
    private readonly string example44CsharpCode = @"
// Program.cs
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    options.ToggleShortcut = ""Mod+K"";
    options.SequenceTimeoutMs = 1500;     // must be positive: 0 throws ArgumentOutOfRangeException
    options.MaxResults = 100;             // must be positive
    options.DebounceMs = 200;             // must not be negative
    options.TypoTolerance = false;
    options.ResetOnClose = false;         // reopen where it was left
    options.GroupOrder.AddRange([""Navigation"", ""Actions""]);
    options.Commands.Add(BitCommandPaletteCmd.Navigate(""Home"", ""/"").Group(""Navigation""));
});

// The page
[Inject] private BitCommandPaletteOptions eOptions { get; set; } = default!;

private BitCommandPalette eFinder = default!;
private string eFinderQuery = """";
private string? eOptionPicked;
private BitCommandPaletteCommand[] eOptionCommands = default!;
private static readonly string[] eFinderQueries = [""history"", ""shortcut"", ""source"", ""^show"", """"];

private sealed record EOptionInfo(string Name, string Group, Func<BitCommandPaletteOptions, string> Read, string Effect);

private static readonly EOptionInfo[] eOptionInfos =
[
    new(""ToggleShortcut"", ""Keyboard"", o => o.ToggleShortcut?.ToString() ?? ""none"", ""Opens and closes the palette; null for none.""),
    new(""Loop"", ""Keyboard"", o => EBool(o.Loop), ""Arrowing past the last row comes back to the first.""),
    new(""MaxRecents"", ""History"", o => ENumber(o.MaxRecents), ""Recent commands listed on the empty query.""),
    // ... one per option
];

protected override void OnInitialized()
{
    eOptionCommands =
    [
        .. eOptionInfos.Select(o => (BitCommandPaletteCommand)BitCommandPaletteCmd.New(o.Name)
            .Id($""e-option-{o.Name}"")
            .Group(o.Group)
            .Subtitle(o.Effect)
            .Value(o.Read(eOptions))       // the value this app runs with, at the end of the row
            .Handler(() => EOptionChosen(o))),
    ];
}

private void EOptionChosen(EOptionInfo option)
{
    eOptionPicked = $""{option.Name} = {option.Read(eOptions)}"";
    _ = InvokeAsync(StateHasChanged);
}

private static string EBool(bool value) => value ? ""true"" : ""false"";
private static string ENumber(double value) => value.ToString(""0.##"", System.Globalization.CultureInfo.InvariantCulture);";

    private readonly string example45RazorCode = @"
<BitParams Parameters=""eCascadedParams"">
    <BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""eCascadeCommands"" />

    <BitCommandPalette Inline
                       IncludeRegisteredCommands=""false""
                       Commands=""eCascadeCommands""
                       Placeholder=""Its own placeholder wins…""
                       ShowFooter=""true""
                       HighlightMatches=""true"" />
</BitParams>";
    private readonly string example45CsharpCode = @"
private readonly BitCommandPaletteParams[] eCascadedParams =
[
    new()
    {
        Placeholder = ""Placeholder from BitParams…"",
        ShowFooter = false,
        HighlightMatches = false,
        EnableHistory = false,
        VimBindings = true,
    }
];

private static readonly BitCommandPaletteCommand[] eCascadeCommands =
[
    BitCommandPaletteCmd.New(""Banana"").Id(""e-cascade-banana"").Group(""Fruits""),
    BitCommandPaletteCmd.New(""Mango"").Id(""e-cascade-mango"").Group(""Fruits""),
    BitCommandPaletteCmd.New(""Orange"").Id(""e-cascade-orange"").Group(""Fruits""),
    BitCommandPaletteCmd.New(""Pineapple"").Id(""e-cascade-pineapple"").Group(""Fruits""),
];";

    private readonly string example46RazorCode = @"
<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   Commands=""eSecurityCommands""
                   OnCommandExecuted=""e => eSecurityLast = e.Command.Title"" />

<BitText>Last run: <b>@(eSecurityLast ?? ""nothing yet"")</b></BitText>";
    private readonly string example46CsharpCode = @"
// Program.cs
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    options.AllowedUrlSchemes.Add(""vscode"");      // let commands open vscode:// links
    options.AllowedUrlSchemes.Remove(""tel"");
    options.RecordCommandIdsInTraces = false;     // ids embed personal data: keep them out of traces and logs
});

// The page
private const string eUnsafeHref = ""javascript:alert('This never runs')"";
private string? eSecurityLast;

private static readonly BitCommandPaletteCommand[] eSecurityCommands =
[
    // Not in AllowedUrlSchemes: refused, logged and reported, never followed.
    BitCommandPaletteCmd.New(""Follow a javascript: link"").Id(""e-security-unsafe-link"").Group(""Links"")
        .Subtitle(eUnsafeHref)
        .Href(eUnsafeHref),
    // Rendered as text, never as HTML.
    BitCommandPaletteCmd.New(""<img src=x onerror=alert(1)> stays text"").Id(""e-security-html-title"").Group(""Text"")
        .Handler(() => { }),
    // An opaque id rather than the e-mail address, and kept out of the history.
    BitCommandPaletteCmd.New(""Sam Rivera"").Id(""e-security-person-1042"").Group(""People"")
        .Subtitle(""sam@example.com"")
        .ExcludeFromHistory()
        .Handler(() => { }),
];";

    private readonly string example47RazorCode = @"
<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   Commands=""eHistoryCommands""
                   Placeholder=""Run a few, then clear the query…"" />

<BitButton Variant=""BitVariant.Outline"" OnClick=""EReadStoredHistory"">Read localStorage</BitButton>
<BitText>Key: <code>@eOptions.HistoryStorageKey</code>, MaxRecents: <b>@eOptions.MaxRecents</b>, MaxHistoryEntries: <b>@eOptions.MaxHistoryEntries</b></BitText>

<pre>@(eStoredHistory ?? ""Not read yet."")</pre>";
    private readonly string example47CsharpCode = @"
// Program.cs
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    options.HistoryStorageKey = ""my-app.palette-history"";  // the default store's localStorage key
    options.MaxRecents = 8;                                // recents on the empty query
    options.MaxHistoryEntries = 500;                       // pinned commands are never pruned
});

// Either: nothing in the browser, for the lifetime of the circuit (Server) or the tab (WebAssembly).
builder.Services.AddBitCommandPaletteInMemoryHistoryStore();

// Or: per user, on the server.
builder.Services.AddBitCommandPaletteHistoryStore<UserHistoryStore>();

// IUserSettings is your own service: a database, a user profile API...
public sealed class UserHistoryStore(IUserSettings settings) : IBitCommandPaletteHistoryStore
{
    public async ValueTask<IDictionary<string, BitCommandPaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
        => await settings.LoadPaletteHistoryAsync(cancellationToken)
           ?? new Dictionary<string, BitCommandPaletteCommandUsage>(StringComparer.Ordinal);

    public async ValueTask SaveAsync(IReadOnlyDictionary<string, BitCommandPaletteCommandUsage> usage, CancellationToken cancellationToken = default)
        => await settings.SavePaletteHistoryAsync(usage, cancellationToken);
}

// The page
[Inject] private BitCommandPaletteOptions eOptions { get; set; } = default!;
[Inject] private IJSRuntime eJs { get; set; } = default!;

private string? eStoredHistory;

private static readonly BitCommandPaletteCommand[] eHistoryCommands =
[
    BitCommandPaletteCmd.New(""Open the calendar"").Id(""e-history-calendar"").Group(""Apps"").Handler(() => { }),
    BitCommandPaletteCmd.New(""Open the contacts"").Id(""e-history-contacts"").Group(""Apps"").Handler(() => { }),
    BitCommandPaletteCmd.New(""Open the notes"").Id(""e-history-notes"").Group(""Apps"").Handler(() => { }),
    BitCommandPaletteCmd.New(""Open the tasks"").Id(""e-history-tasks"").Group(""Apps"").Handler(() => { }),
    BitCommandPaletteCmd.New(""Open the weather"").Id(""e-history-weather"").Group(""Apps"").Handler(() => { }),
    BitCommandPaletteCmd.New(""Open the photos"").Id(""e-history-photos"").Group(""Apps"").Handler(() => { }),
];

private async Task EReadStoredHistory()
{
    var json = await eJs.InvokeAsync<string?>(""localStorage.getItem"", eOptions.HistoryStorageKey);
    eStoredHistory = string.IsNullOrEmpty(json) ? ""(nothing stored yet)"" : json;
}";

    private readonly string example48RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" DisablePointerSelection Commands=""fSkinCommands"">
    <ItemTemplate Context=""item"">
        <span class=""f-row"">
            <span class=""f-text"">
                <span class=""f-title"">
                    @if (item.IsConfirming)
                    {
                        <b>Confirm: </b>
                    }
                    <BitCommandPaletteHighlightedText Text=""@item.Row.Command.Title"" Positions=""@item.Row.Match?.TitlePositions"" />
                </span>
                @if (item.Row.Command.Disabled && item.Row.Command.DisabledReason is not null)
                {
                    <span class=""f-sub"">@item.Row.Command.DisabledReason</span>
                }
            </span>
            @if (item.Row.Command.Checked is { } on)
            {
                @* Drawn for the eye, said in words for screen readers. *@
                <span class=""f-switch"" aria-hidden=""true"">@(on ? ""●"" : ""○"")</span>
                <span class=""f-vhd"">@(on ? ""On"" : ""Off"")</span>
            }
            @if (item.Row.Command.Shortcut is not null)
            {
                <BitCommandPaletteShortcutHint Shortcut=""item.Row.Command.Shortcut"" MacKeys=""item.IsMac"" />
            }
        </span>
    </ItemTemplate>
</BitCommandPalette>

<style>
    .f-switch { flex: none; color: var(--bit-clr-pri); }
    .f-vhd { position: absolute; width: 1px; height: 1px; overflow: hidden; white-space: nowrap; clip-path: inset(50%); }
</style>";
    private readonly string example48CsharpCode = @"
// fSkinCommands and FBuildSkinCommands: as in the CSS variables sample. The .f-row, .f-text, .f-title and .f-sub
// classes: as in the Item template sample.";

    private readonly string example49RazorCode = @"
@* Nothing to set up: the dialog, the combobox, the grouped listbox and the live regions come with the palette. *@
<BitCommandPaletteTrigger />
<BitCommandPalette />";

    private readonly string example50RazorCode = @"
";
    private readonly string example50CsharpCode = @"
// File: Program.cs - OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter(BitCommandPaletteDiagnostics.MeterName))
    .WithTracing(t => t.AddSource(BitCommandPaletteDiagnostics.ActivitySourceName));

// File: Program.cs - or Bit.Belemetry
builder.Services.AddBitBelemetryServices(options =>
{
    options.Metrics.Meters.Add(BitCommandPaletteDiagnostics.MeterName);
    options.Traces.Sources.Add(BitCommandPaletteDiagnostics.ActivitySourceName);
});

// File: Program.cs - keep command ids out of traces and logs when they embed personal data
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options => options.RecordCommandIdsInTraces = false);";

    private readonly string example51RazorCode = @"
";
    private readonly string example51CsharpCode = @"
// File: ServiceCollectionExtensions.cs in a project both the server and the client reference -
// so the two Program.cs files register the same services with the same options.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientServices(this IServiceCollection services)
    {
        services.AddBitBlazorUIExtrasServices(commandPalette: options => options.GroupOrder.AddRange([""Navigation"", ""Actions""]));
        return services;
    }
}

// File: Program.cs of the server, and Program.cs of the client
builder.Services.AddClientServices();

// File: any component - after the state a VisibleWhen predicate reads has changed
Palette.Refresh();";

    private readonly string example52RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fFiles"" Placeholder=""Search files…"">
    <ItemTemplate Context=""item"">
        <span class=""f-row"">
            <span class=""f-ext"" aria-hidden=""true"">@FExtension(item.Row.Command)</span>
            <span class=""f-text"">
                <span class=""f-title"">
                    <BitCommandPaletteHighlightedText Text=""@item.Row.Command.Title"" Positions=""@item.Row.Match?.TitlePositions"" />
                </span>
                @if (item.Row.Command.Subtitle is not null)
                {
                    <span class=""f-sub"">
                        <BitCommandPaletteHighlightedText Text=""@item.Row.Command.Subtitle"" Positions=""@item.Row.Match?.SubtitlePositions"" />
                    </span>
                }
            </span>
            @if (item.Row.Command.Shortcut is not null)
            {
                <BitCommandPaletteShortcutHint Shortcut=""item.Row.Command.Shortcut"" MacKeys=""item.IsMac"" />
            }
            @if (item.IsActive)
            {
                <kbd class=""f-go"" aria-hidden=""true"">@(item.IsMac ? ""↩"" : ""Enter"")</kbd>
            }
        </span>
    </ItemTemplate>
</BitCommandPalette>

<style>
    .f-row { display: flex; gap: .75rem; width: 100%; min-width: 0; align-items: center; }
    .f-ext { flex: none; min-width: 2.75rem; padding: .125rem .375rem; border-radius: .25rem; text-align: center;
             font-size: .6875rem; font-weight: 600; text-transform: uppercase;
             color: var(--bit-clr-fg-sec); background: var(--bit-clr-bg-sec); }
    .f-text { flex: 1 1 auto; min-width: 0; display: flex; flex-direction: column; }
    .f-title, .f-sub { overflow: hidden; white-space: nowrap; text-overflow: ellipsis; }
    .f-title { font-weight: 600; }
    .f-sub { font-size: .75rem; color: var(--bit-clr-fg-sec); }
    .f-go { flex: none; font: inherit; font-size: .6875rem; padding: 0 .375rem;
            border: 1px solid var(--bit-clr-brd-pri); border-radius: .25rem; }
</style>";
    private readonly string example52CsharpCode = @"
private sealed record FFileInfo(string Name, string Folder, string Size, string Modified);

private static readonly FFileInfo[] fRecentFiles =
[
    new(""Program.cs"", ""src/Server"", ""4 KB"", ""Today, 09:12""),
    new(""App.razor"", ""src/Client"", ""2 KB"", ""Today, 08:40""),
    new(""README.md"", ""/"", ""11 KB"", ""Yesterday""),
    new(""appsettings.json"", ""src/Server"", ""1 KB"", ""Monday""),
    new(""site.css"", ""src/Client/wwwroot"", ""7 KB"", ""Last week""),
];

private static readonly FFileInfo[] fArchivedFiles =
[
    new(""Startup.cs"", ""archive/2023"", ""6 KB"", ""March 2023""),
    new(""web.config"", ""archive/2023"", ""2 KB"", ""March 2023""),
    new(""bundleconfig.json"", ""archive/2022"", ""1 KB"", ""June 2022""),
];

private List<BitCommandPaletteCommand> fFiles = [];

protected override void OnInitialized()
{
    fFiles =
    [
        .. fRecentFiles.Select((file, i) => FFile(file, ""Recent files"", i < 2 ? $""Alt+{i + 1}"" : null)),
        BitCommandPaletteCmd.New(""The archive…"").Id(""f-archive"").Group(""Folders"").Subtitle(""Loads for a second"")
            .Children(async (_, ct) =>
            {
                // Long enough to see the loading state (a real app would call its API here).
                await Task.Delay(1000, ct);
                return fArchivedFiles.Select(file => FFile(file, null, null)).ToList();
            }, placeholder: ""Search the archive…""),
        BitCommandPaletteCmd.New(""An empty folder"").Id(""f-empty-folder"").Group(""Folders"").Subtitle(""Nothing in it"")
            .Children(async (_, ct) =>
            {
                await Task.Delay(300, ct);
                return [];
            }),
    ];
}

private static BitCommandPaletteCommand FFile(FFileInfo file, string? group, string? shortcut)
{
    var builder = BitCommandPaletteCmd.New(file.Name)
        .Id($""f-file:{file.Folder}/{file.Name}"")
        .Group(group)
        .Subtitle(file.Folder)
        .Data(file)
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($""Opened {file.Name}"", keepOpen: true)));

    // A shortcut shown on the row only: global: false keeps it from being claimed on the whole page.
    return shortcut is null ? builder : builder.Shortcut(shortcut, global: false);
}

private static string FExtension(BitCommandPaletteCommand command)
{
    if (command.Children is not null || command.ChildrenProvider is not null) return ""dir"";

    var dot = command.Title.LastIndexOf('.');
    return dot < 0 ? ""txt"" : command.Title[(dot + 1)..];
}";

    private readonly string example53RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fFiles"" Placeholder=""Search files…"">
    <IconTemplate Context=""command"">
        <span class=""f-icon"" data-ext=""@FExtension(command)"">@(FExtension(command)[..1].ToUpperInvariant())</span>
    </IconTemplate>
    <GroupHeaderTemplate Context=""heading"">
        <span class=""f-group""><span aria-hidden=""true"">⌁ </span>@heading</span>
    </GroupHeaderTemplate>
</BitCommandPalette>

<style>
    .f-icon { width: 1.25rem; height: 1.25rem; display: inline-flex; align-items: center; justify-content: center;
              border-radius: .25rem; font-size: .6875rem; font-weight: 700; color: #fff; background: #64748b; }
    .f-icon[data-ext=""cs""] { background: #7c3aed; }
    .f-icon[data-ext=""razor""] { background: #a21caf; }
    .f-icon[data-ext=""md""] { background: #0f766e; }
    .f-icon[data-ext=""json""] { background: #b45309; }
    .f-icon[data-ext=""css""] { background: #1d4ed8; }
    .f-icon[data-ext=""dir""] { background: #475569; }
    .f-group { color: var(--bit-clr-pri); }
</style>";
    private readonly string example53CsharpCode = @"
// fFiles, FFileInfo, FFile and FExtension: as in the Item template sample.";

    private readonly string example54RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fFiles"" Placeholder=""Search files…"">
    <HeaderTemplate Context=""state"">
        <div class=""f-header"">
            @(state.Breadcrumbs.Count > 0 ? string.Join("" › "", state.Breadcrumbs) : ""All files"")
            ·
            @(state.IsLoading ? ""loading…"" : state.ResultCount == 1 ? ""1 file"" : $""{state.ResultCount} files"")
            @if (state.Query.Length > 0)
            {
                <span> matching “@state.Query”</span>
            }
        </div>
    </HeaderTemplate>
    <FooterTemplate Context=""state"">
        <div class=""f-footer"">
            <span class=""f-footer-text"">
                @if (state.Toast is not null)
                {
                    @state.Toast
                }
                else if (state.ActiveCommand?.Data is FFileInfo file)
                {
                    @($""{file.Folder}/{file.Name} · {file.Size}"")
                }
            </span>
            <span aria-hidden=""true""><kbd class=""f-go"">@(state.IsMac ? ""↩"" : ""Enter"")</kbd> to open</span>
        </div>
    </FooterTemplate>
</BitCommandPalette>

<style>
    .f-header { padding: .5rem 1rem 0; font-size: .75rem; color: var(--bit-clr-fg-sec); }
    .f-footer { display: flex; gap: 1rem; align-items: center; justify-content: space-between; min-height: 2.5rem;
                padding: 0 1rem; font-size: .75rem; color: var(--bit-clr-fg-sec);
                background: var(--bit-clr-bg-sec); border-top: 1px solid var(--bit-clr-brd-sec); }
    .f-footer-text { min-width: 0; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; }
</style>";
    private readonly string example54CsharpCode = @"
// fFiles, FFileInfo and FFile: as in the Item template sample. The files carry their FFileInfo as Data.";

    private readonly string example55RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fFiles"" Placeholder=""Search files…"">
    <DetailTemplate Context=""command"">
        <div class=""f-detail"">
            <b>@command.Title</b>
            @if (command.Data is FFileInfo file)
            {
                <dl>
                    <dt>Folder</dt>
                    <dd>@file.Folder</dd>
                    <dt>Size</dt>
                    <dd>@file.Size</dd>
                    <dt>Modified</dt>
                    <dd>@file.Modified</dd>
                </dl>
            }
            else
            {
                <p>@command.Subtitle</p>
            }
            @if (command.Shortcut is not null)
            {
                <BitCommandPaletteShortcutHint Shortcut=""command.Shortcut"" />
            }
        </div>
    </DetailTemplate>
</BitCommandPalette>

<style>
    .f-detail { display: flex; gap: .75rem; flex-direction: column; overflow-wrap: anywhere; }
    .f-detail dl { margin: 0; display: grid; gap: .25rem .75rem; grid-template-columns: auto 1fr; }
    .f-detail dt { color: var(--bit-clr-fg-sec); }
    .f-detail dd { margin: 0; color: var(--bit-clr-fg-pri); }
    .f-detail p { margin: 0; }
</style>";
    private readonly string example55CsharpCode = @"
// fFiles, FFileInfo and FFile: as in the Item template sample. The files carry their FFileInfo as Data.";

    private readonly string example56RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fFiles"" Placeholder=""Search files…"">
    <LoadingTemplate>
        <p class=""f-state"" role=""status"">Opening the folder…</p>
    </LoadingTemplate>
    <NoResultsTemplate Context=""query"">
        <p class=""f-state"">No file matches “@query”. Try <code>json</code>.</p>
    </NoResultsTemplate>
    <EmptyTemplate>
        <p class=""f-state"">This folder is empty.</p>
    </EmptyTemplate>
</BitCommandPalette>

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" NoResultsText=""There is nothing here to find."">
    <EmptyTemplate>
        <p class=""f-state"">No commands yet. They appear here once they are registered.</p>
    </EmptyTemplate>
</BitCommandPalette>

<style>
    .f-state { margin: 0; padding: 2rem 1rem; text-align: center; color: var(--bit-clr-fg-sec); }
</style>";
    private readonly string example56CsharpCode = @"
// fFiles, FFileInfo and FFile: as in the Item template sample. ""The archive…"" and ""An empty folder"" are lazily loaded pages:
BitCommandPaletteCmd.New(""The archive…"").Id(""f-archive"").Group(""Folders"").Subtitle(""Loads for a second"")
    .Children(async (_, ct) =>
    {
        await Task.Delay(1000, ct); // a real app would call its API here
        return fArchivedFiles.Select(file => FFile(file, null, null)).ToList();
    }, placeholder: ""Search the archive…""),
BitCommandPaletteCmd.New(""An empty folder"").Id(""f-empty-folder"").Group(""Folders"").Subtitle(""Nothing in it"")
    .Children(async (_, ct) =>
    {
        await Task.Delay(300, ct);
        return [];
    }),";

    private readonly string example57RazorCode = @"
<div class=""f-buttons"">
    @foreach (var language in fLanguages)
    {
        <BitButton Size=""BitSize.Small""
                   Variant=""@(fLanguage == language ? BitVariant.Fill : BitVariant.Outline)""
                   OnClick=""() => FSetLanguage(language)""
                   lang=""@language.Code"">@language.Name</BitButton>
    }
</div>

<div lang=""@fLanguage.Code"">
    <BitCommandPalette Inline
                       IncludeRegisteredCommands=""false""
                       EnableHistory=""false""
                       Strings=""fLanguage.Strings""
                       Commands=""fL10nCommands"" />
</div>";
    private readonly string example57CsharpCode = @"
// What the sample commands say, in one language.
private sealed record FTexts(
    string General, string DangerZone,
    string OpenSettings, string SettingsOpened,
    string DarkMode,
    string Export, string ChooseFormat, string AsPdf, string ExportedPdf, string AsCsv, string NeedsPro,
    string DeleteFiles, string ConfirmDelete, string Deleted);

private sealed record FLanguage(string Code, string Name, BitCommandPaletteStrings Strings, FTexts Texts);

private static readonly FLanguage[] fLanguages =
[
    new(""en"", ""English"", new BitCommandPaletteStrings(), new(
        ""General"", ""Danger zone"",
        ""Open settings"", ""Settings opened"",
        ""Dark mode"",
        ""Export…"", ""Choose a format…"", ""As PDF"", ""Exported as PDF"", ""As CSV"", ""Requires the Pro plan"",
        ""Delete all files"", ""Delete every file for good?"", ""Deleted (not really)"")),
    new(""de"", ""Deutsch"", new BitCommandPaletteStrings
    {
        Placeholder = ""Befehl eingeben oder suchen…"",
        PromptPlaceholder = ""Wert eingeben und Eingabetaste drücken…"",
        NoResults = ""Keine Ergebnisse für „{0}“"",
        NoResultsHint = ""Versuchen Sie eine andere Suche, oder prüfen Sie die Schreibweise."",
        Empty = ""Noch nichts anzuzeigen."",
        Loading = ""Wird gesucht…"",
        Recent = ""Zuletzt verwendet"",
        Pinned = ""Angeheftet"",
        Suggestions = ""Vorschläge"",
        Modes = ""Modi"",
        Actions = ""Aktionen"",
        Results = ""Ergebnisse"",
        Other = ""Sonstiges"",
        Navigate = ""zum Navigieren"",
        Select = ""zum Auswählen"",
        Close = ""zum Schließen"",
        ClearQuery = ""zum Leeren"",
        Back = ""zurück"",
        OpenActions = ""für Aktionen"",
        Toggle = ""zum Umschalten"",
        Apply = ""zum Anwenden"",
        Pin = ""Oben anheften"",
        Unpin = ""Lösen"",
        RemoveFromRecents = ""Aus „Zuletzt verwendet“ entfernen"",
        ClearRecents = ""Zuletzt verwendete Befehle löschen"",
        ConfirmPrefix = ""Bestätigen: "",
        ConfirmHint = ""Zum Bestätigen erneut die Eingabetaste drücken, Esc zum Abbrechen"",
        SelectedCount = ""{0} ausgewählt"",
        ResultsAnnouncement = ""{0} Ergebnisse verfügbar"",
        OneResultAnnouncement = ""1 Ergebnis verfügbar"",
        NoResultsAnnouncement = ""Keine Ergebnisse"",
        Disabled = ""Nicht verfügbar"",
        DialogLabel = ""Befehlspalette"",
        InputLabel = ""Befehle durchsuchen"",
        CloseButton = ""Schließen"",
        BackButton = ""Zurück"",
        Breadcrumb = ""Navigationspfad"",
        OpenPalette = ""Befehlspalette öffnen"",
        TriggerNameSeparator = "", "",
        Search = ""Suchen"",
        Home = ""Start"",
        MoreResults = ""{0} weitere anzeigen…"",
        LoadMore = ""Mehr anzeigen…"",
        SequencePending = ""wartet auf die nächste Taste…"",
        CommandFailed = ""„{0}“ konnte nicht ausgeführt werden."",
        CommandRefused = ""„{0}“ darf nicht ausgeführt werden."",
        CommandUnavailable = ""„{0}“ ist nicht mehr verfügbar."",
        LoadFailed = ""Diese Liste konnte nicht geladen werden."",
        Retry = ""Erneut versuchen"",
        ApproximateResults = ""Keine genauen Treffer. Ähnliche werden angezeigt."",
        Running = ""Wird ausgeführt…"",
        SourceFailed = ""{0} konnte nicht durchsucht werden"",
        On = ""Ein"",
        Off = ""Aus"",
        ShortcutThen = ""dann"",
    }, new(
        ""Allgemein"", ""Gefahrenbereich"",
        ""Einstellungen öffnen"", ""Einstellungen geöffnet"",
        ""Dunkelmodus"",
        ""Exportieren…"", ""Format wählen…"", ""Als PDF"", ""Als PDF exportiert"", ""Als CSV"", ""Erfordert den Pro-Tarif"",
        ""Alle Dateien löschen"", ""Wirklich alle Dateien endgültig löschen?"", ""Gelöscht (nicht wirklich)"")),
    new(""es"", ""Español"", new BitCommandPaletteStrings
    {
        Placeholder = ""Escribe un comando o busca…"",
        PromptPlaceholder = ""Escribe un valor y pulsa Intro…"",
        NoResults = ""No hay resultados para «{0}»"",
        NoResultsHint = ""Prueba con otra búsqueda o revisa la ortografía."",
        Empty = ""Todavía no hay nada que mostrar."",
        Loading = ""Buscando…"",
        Recent = ""Recientes"",
        Pinned = ""Fijados"",
        Suggestions = ""Sugerencias"",
        Modes = ""Modos"",
        Actions = ""Acciones"",
        Results = ""Resultados"",
        Other = ""Otros"",
        Navigate = ""para moverte"",
        Select = ""para elegir"",
        Close = ""para cerrar"",
        ClearQuery = ""para borrar"",
        Back = ""para volver"",
        OpenActions = ""para ver acciones"",
        Toggle = ""para marcar"",
        Apply = ""para aplicar"",
        Pin = ""Fijar arriba"",
        Unpin = ""Dejar de fijar"",
        RemoveFromRecents = ""Quitar de recientes"",
        ClearRecents = ""Borrar los comandos recientes"",
        ConfirmPrefix = ""Confirmar: "",
        ConfirmHint = ""Pulsa Intro de nuevo para confirmar, o Esc para cancelar"",
        SelectedCount = ""{0} seleccionados"",
        ResultsAnnouncement = ""{0} resultados disponibles"",
        OneResultAnnouncement = ""1 resultado disponible"",
        NoResultsAnnouncement = ""Sin resultados"",
        Disabled = ""No disponible"",
        DialogLabel = ""Paleta de comandos"",
        InputLabel = ""Buscar comandos"",
        CloseButton = ""Cerrar"",
        BackButton = ""Volver"",
        Breadcrumb = ""Ruta de navegación"",
        OpenPalette = ""Abrir la paleta de comandos"",
        TriggerNameSeparator = "", "",
        Search = ""Buscar"",
        Home = ""Inicio"",
        MoreResults = ""Mostrar {0} más…"",
        LoadMore = ""Mostrar más…"",
        SequencePending = ""esperando la siguiente tecla…"",
        CommandFailed = ""No se pudo ejecutar «{0}»."",
        CommandRefused = ""No puedes ejecutar «{0}»."",
        CommandUnavailable = ""«{0}» ya no está disponible."",
        LoadFailed = ""No se pudo cargar esta lista."",
        Retry = ""Reintentar"",
        ApproximateResults = ""No hay coincidencias exactas. Se muestran las más parecidas."",
        Running = ""Trabajando…"",
        SourceFailed = ""No se pudo buscar en {0}"",
        On = ""Activado"",
        Off = ""Desactivado"",
        ShortcutThen = ""luego"",
    }, new(
        ""General"", ""Zona de peligro"",
        ""Abrir la configuración"", ""Configuración abierta"",
        ""Modo oscuro"",
        ""Exportar…"", ""Elige un formato…"", ""Como PDF"", ""Exportado como PDF"", ""Como CSV"", ""Requiere el plan Pro"",
        ""Eliminar todos los archivos"", ""¿Eliminar todos los archivos para siempre?"", ""Eliminados (no de verdad)"")),
];

private FLanguage fLanguage = fLanguages[0];
private List<BitCommandPaletteCommand> fL10nCommands = [];
private bool fL10nDark;

protected override void OnInitialized() => fL10nCommands = FBuildL10nCommands();

private void FSetLanguage(FLanguage language)
{
    fLanguage = language;
    // A new list: the palette re-reads Commands when it is handed a new collection.
    fL10nCommands = FBuildL10nCommands();
}

private List<BitCommandPaletteCommand> FBuildL10nCommands()
{
    var t = fLanguage.Texts;
    var english = fLanguages[0].Texts;

    return
    [
        // The same id in every language; the English title stays findable as a keyword.
        BitCommandPaletteCmd.New(t.OpenSettings).Id(""f-l10n:settings"").Group(t.General).Keywords(english.OpenSettings)
            .Shortcut(""G S"", global: false)
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(t.SettingsOpened, keepOpen: true))),
        BitCommandPaletteCmd.New(t.DarkMode).Id(""f-l10n:dark"").Group(t.General).Keywords(english.DarkMode)
            .Checked(fL10nDark)
            .Handler(_ =>
            {
                fL10nDark = !fL10nDark;
                fL10nCommands = FBuildL10nCommands();
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            }),
        BitCommandPaletteCmd.New(t.Export).Id(""f-l10n:export"").Group(t.General).Keywords(english.Export)
            .ChildrenPlaceholder(t.ChooseFormat)
            .Children(
                BitCommandPaletteCmd.New(t.AsPdf).Id(""f-l10n:export.pdf"").Keywords(english.AsPdf)
                    .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(t.ExportedPdf, keepOpen: true))),
                BitCommandPaletteCmd.New(t.AsCsv).Id(""f-l10n:export.csv"").Keywords(english.AsCsv).Disabled(true, t.NeedsPro)),
        BitCommandPaletteCmd.New(t.DeleteFiles).Id(""f-l10n:delete"").Group(t.DangerZone).Keywords(english.DeleteFiles)
            .Destructive(t.ConfirmDelete)
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(t.Deleted, keepOpen: true))),
    ];
}



// ---------- Every palette: Program.cs ----------

builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options =>
{
    options.Strings = new BitCommandPaletteStrings
    {
        Placeholder = ""Befehl eingeben oder suchen…"",
        NoResults = ""Keine Ergebnisse für „{0}“"",
        ResultsAnnouncement = ""{0} Ergebnisse verfügbar"",
        On = ""Ein"",
        Off = ""Aus"",
        ShortcutThen = ""dann"",
    };
});



// ---------- Resource files: read the strings from an IStringLocalizer ----------

public sealed class LocalizedCommandPaletteStrings(IStringLocalizer<LocalizedCommandPaletteStrings> localizer) : BitCommandPaletteStrings
{
    public override string Placeholder => localizer[nameof(Placeholder)];
    public override string NoResults => localizer[nameof(NoResults)];
    public override string ResultsAnnouncement => localizer[nameof(ResultsAnnouncement)];
    // ...
}

// @inject IStringLocalizer<LocalizedCommandPaletteStrings> Localizer
// <BitCommandPalette Strings=""new LocalizedCommandPaletteStrings(Localizer)"" />



// ---------- Plural forms: Polish has three ----------

public sealed class PolishCommandPaletteStrings : BitCommandPaletteStrings
{
    public override string ResultsAnnouncementFor(int count) => count switch
    {
        0 => ""Brak wyników"",
        1 => ""1 wynik"",
        _ when count % 10 is >= 2 and <= 4 && count % 100 is < 12 or > 14 => $""{count} wyniki"",
        _ => $""{count} wyników"",
    };
}



// ---------- NoResults and Empty fall back to BlazorUI's own strings ----------

public sealed class GermanBitStrings : BitStrings
{
    public override string NoResultsFound => ""Keine Ergebnisse"";
    public override string NoItemsFound => ""Keine Einträge"";
    // ...
}

builder.Services.AddBitBlazorUIServices(options => options.Strings = new GermanBitStrings());";

    private readonly string example58RazorCode = @"
<BitCommandPaletteTrigger Label=""Search mail"" Style=""@fTriggerStyle"" />

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fSkinCommands"" Style=""@fVariablesStyle"">
    <DetailTemplate Context=""command"">
        <b>@command.Title</b>
        <p>@(command.Subtitle ?? command.Group)</p>
    </DetailTemplate>
</BitCommandPalette>";
    private readonly string example58CsharpCode = @"
private const string fVariablesStyle =
    ""--bit-CommandPalette-width: 760px;"" +                       // the modal dialog only
    ""--bit-CommandPalette-max-height: 320px;"" +
    ""--bit-CommandPalette-top: 8vh;"" +                           // the modal dialog only
    ""--bit-CommandPalette-radius: 1.25rem;"" +
    ""--bit-CommandPalette-background: color-mix(in srgb, #ea580c 5%, var(--bit-clr-bg-pri));"" +
    ""--bit-CommandPalette-color: color-mix(in srgb, #9a3412 35%, var(--bit-clr-fg-pri));"" +
    ""--bit-CommandPalette-border-color: color-mix(in srgb, #ea580c 35%, var(--bit-clr-brd-sec));"" +
    ""--bit-CommandPalette-shadow: 0 1.5rem 3rem rgba(154, 52, 18, .3);"" + // the modal dialog only
    ""--bit-CommandPalette-backdrop: rgba(67, 20, 7, .45);"" +     // the modal dialog only
    ""--bit-CommandPalette-row-height: 3rem;"" +
    ""--bit-CommandPalette-item-radius: 999px;"" +
    ""--bit-CommandPalette-active-background: color-mix(in srgb, #ea580c 16%, transparent);"" +
    ""--bit-CommandPalette-accent: #ea580c;"" +
    ""--bit-CommandPalette-mark-background: color-mix(in srgb, #ea580c 30%, transparent);"" +
    ""--bit-CommandPalette-detail-width: 35%;"" +
    ""--bit-CommandPalette-focus-color: #ea580c;"";

private const string fTriggerStyle =
    ""--bit-CommandPaletteTrigger-height: 2.5rem;"" +
    ""--bit-CommandPaletteTrigger-background: color-mix(in srgb, #ea580c 8%, var(--bit-clr-bg-pri));"" +
    ""--bit-CommandPaletteTrigger-color: color-mix(in srgb, #9a3412 50%, var(--bit-clr-fg-pri));"" +
    ""--bit-CommandPaletteTrigger-border-color: #ea580c;"" +
    ""--bit-CommandPaletteTrigger-radius: 999px;"" +
    ""--bit-CommandPaletteTrigger-focus-color: #ea580c;"";

private bool fNotifications = true;
private List<BitCommandPaletteCommand> fSkinCommands = [];

protected override void OnInitialized() => fSkinCommands = FBuildSkinCommands();

private List<BitCommandPaletteCommand> FBuildSkinCommands() =>
[
    BitCommandPaletteCmd.New(""Open the inbox"").Id(""f-skin:inbox"").Group(""Mail"").Subtitle(""3 unread"").Badge(""12"")
        .Shortcut(""G I"", global: false)
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Inbox opened"", keepOpen: true))),
    BitCommandPaletteCmd.New(""Archive all mail"").Id(""f-skin:archive"").Group(""Mail"").Destructive(""Archive every message?"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Archived (not really)"", keepOpen: true))),
    BitCommandPaletteCmd.New(""Export the mailbox"").Id(""f-skin:export"").Group(""Mail"").Disabled(true, ""Requires the Pro plan""),
    BitCommandPaletteCmd.New(""Notifications"").Id(""f-skin:notifications"").Group(""Settings"").Checked(fNotifications)
        .Handler(_ =>
        {
            fNotifications = !fNotifications;
            fSkinCommands = FBuildSkinCommands();
            StateHasChanged();
            return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
        }),
    BitCommandPaletteCmd.Page(""Theme"",
            BitCommandPaletteCmd.New(""Light"").Id(""f-skin:theme.light"")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Light (not really)"", keepOpen: true))),
            BitCommandPaletteCmd.New(""Dark"").Id(""f-skin:theme.dark"")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""Dark (not really)"", keepOpen: true))))
        .Id(""f-skin:theme"").Group(""Settings""),
];



/* ---------- Or for every palette and trigger of the app, in its stylesheet (app.css) ----------
:root {
    --bit-CommandPalette-accent: #ea580c;
    --bit-CommandPalette-width: 760px;
    --bit-CommandPaletteTrigger-radius: 999px;
}
*/";

    private readonly string example59RazorCode = @"
@foreach (var skin in fSkins)
{
    <BitButton Size=""BitSize.Small""
               Variant=""@(fSkin == skin ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""() => fSkin = skin"">@skin.Name</BitButton>
}

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fSkinCommands"" Style=""@fSkin.Style"" />

<BitText>Style: <code>@(fSkin.Style.Length == 0 ? ""(none)"" : fSkin.Style)</code></BitText>";
    private readonly string example59CsharpCode = @"
private sealed record FSkin(string Name, string Style);

private static readonly FSkin[] fSkins =
[
    new(""Default"", """"),
    new(""Indigo"",
        ""--bit-CommandPalette-accent: #4f46e5;"" +
        ""--bit-CommandPalette-mark-background: rgba(79, 70, 229, .2);"" +
        ""--bit-CommandPalette-active-background: rgba(79, 70, 229, .1);"" +
        ""--bit-CommandPalette-radius: 14px;"" +
        ""--bit-CommandPalette-item-radius: 8px;""),
    new(""Teal"",
        ""--bit-CommandPalette-accent: #0d9488;"" +
        ""--bit-CommandPalette-background: color-mix(in srgb, #0d9488 4%, var(--bit-clr-bg-pri));"" +
        ""--bit-CommandPalette-border-color: color-mix(in srgb, #0d9488 30%, var(--bit-clr-brd-sec));"" +
        ""--bit-CommandPalette-active-background: color-mix(in srgb, #0d9488 14%, transparent);"" +
        ""--bit-CommandPalette-mark-background: color-mix(in srgb, #0d9488 28%, transparent);""),
    new(""Rose"",
        ""--bit-CommandPalette-accent: #db2777;"" +
        ""--bit-CommandPalette-background: color-mix(in srgb, #db2777 4%, var(--bit-clr-bg-pri));"" +
        ""--bit-CommandPalette-border-color: color-mix(in srgb, #db2777 30%, var(--bit-clr-brd-sec));"" +
        ""--bit-CommandPalette-active-background: color-mix(in srgb, #db2777 12%, transparent);"" +
        ""--bit-CommandPalette-mark-background: color-mix(in srgb, #db2777 25%, transparent);""),
    new(""Flat"",
        ""--bit-CommandPalette-radius: 0;"" +
        ""--bit-CommandPalette-item-radius: 0;"" +
        ""--bit-CommandPalette-shadow: none;"" +
        ""--bit-CommandPalette-border-color: var(--bit-clr-brd-pri);"" +
        ""--bit-CommandPalette-active-background: color-mix(in srgb, var(--bit-clr-fg-pri) 10%, transparent);""),
    new(""Pill"",
        ""--bit-CommandPalette-radius: 1.75rem;"" +
        ""--bit-CommandPalette-item-radius: 999px;"" +
        ""--bit-CommandPalette-row-height: 3rem;""),
    new(""Dense list"",
        ""--bit-CommandPalette-row-height: 2rem;"" +
        ""--bit-CommandPalette-max-height: 240px;"" +
        ""--bit-CommandPalette-item-radius: 4px;"" +
        ""--bit-CommandPalette-radius: 6px;""),
    new(""Terminal"",
        ""font-family: ui-monospace, SFMono-Regular, Consolas, monospace;"" +
        ""--bit-CommandPalette-accent: #16a34a;"" +
        ""--bit-CommandPalette-radius: 4px;"" +
        ""--bit-CommandPalette-item-radius: 2px;"" +
        ""--bit-CommandPalette-active-background: color-mix(in srgb, #16a34a 16%, transparent);"" +
        ""--bit-CommandPalette-mark-background: color-mix(in srgb, #16a34a 30%, transparent);""),
];

private FSkin fSkin = fSkins[1];

// fSkinCommands and FBuildSkinCommands: as in the CSS variables sample.";

    private readonly string example60RazorCode = @"
@foreach (var (scheme, label) in fSchemes)
{
    <BitButton Size=""BitSize.Small""
               Variant=""@(fScheme == scheme ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""() => fScheme = scheme"">@label</BitButton>
}

@* The attribute on the palette itself (or on any element around it): *@
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fSkinCommands"" bit-theme=""@fScheme"" />

@* A theme of its own: *@
<BitThemeProvider Theme=""fDarkTheme"">
    <BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fSkinCommands"" />
</BitThemeProvider>";
    private readonly string example60CsharpCode = @"
private static readonly (string? Scheme, string Label)[] fSchemes =
[
    (null, ""Follows the page""),
    (""light"", ""Light""),
    (""dark"", ""Dark""),
];

private string? fScheme = ""dark"";

private readonly BitTheme fDarkTheme = BitThemeFactory.CreateDarkThemeFromSeed(BitAccentColorPresets.Teal);

// fSkinCommands and FBuildSkinCommands: as in the CSS variables sample.";

    private readonly string example61RazorCode = @"
@foreach (var (name, scale) in fDensities)
{
    <BitButton Size=""BitSize.Small""
               Variant=""@(fDensity == scale ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""() => fDensity = scale"">@name (@scale)</BitButton>
}

<BitCommandPaletteTrigger Style=""@($""--bit-layout-density-scale: {fDensity}"")"" />

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Commands=""fSkinCommands""
                   Style=""@($""--bit-layout-density-scale: {fDensity}"")"" />";
    private readonly string example61CsharpCode = @"
// Strings, not numbers: a number would be written in the current culture (""0,8"" in German), which CSS does not read.
private static readonly (string Name, string Scale)[] fDensities =
[
    (""Compact"", ""0.8""),
    (""Default"", ""1""),
    (""Spacious"", ""1.2""),
];

private string fDensity = ""0.8"";

// fSkinCommands and FBuildSkinCommands: as in the CSS variables sample.";

    private readonly string example62RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" EnableHistory=""false"" Commands=""fSkinCommands"" />";
    private readonly string example62CsharpCode = @"
// fSkinCommands and FBuildSkinCommands: as in the CSS variables sample.";

    private readonly string example63RazorCode = @"
<BitCommandPalette Inline
                   Class=""f-pantry""
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Placeholder=""Search the pantry…""
                   Commands=""fPantry"" />

<style>
    .f-pantry {
        font-family: Georgia, 'Times New Roman', serif;
        --bit-CommandPalette-radius: 0;
        --bit-CommandPalette-background: transparent;
        --bit-CommandPalette-border-color: currentColor;
    }
    .f-pantry [data-bit-cpl=""input""] { font-style: italic; }
    .f-pantry [data-bit-cpl=""group-heading""] { font-size: 1rem; letter-spacing: 0; text-transform: none;
                                               border-bottom: 1px dashed currentColor; }
    .f-pantry [data-bit-cpl=""item""] { border-radius: 0; }
    .f-pantry [data-bit-cpl=""item""][data-active=""true""] { color: Canvas; background: CanvasText; }
    .f-pantry [data-bit-cpl=""item""][data-disabled=""true""] { text-decoration: line-through; }
    .f-pantry [data-bit-cpl=""mark""] { color: inherit; background: none; text-decoration: underline; }
    .f-pantry [data-bit-cpl=""footer""] { display: none; }
</style>";
    private readonly string example63CsharpCode = @"
private static readonly BitCommandPaletteCommand[] fPantry =
[
    BitCommandPaletteCmd.New(""Apples"").Id(""f-pantry:apples"").Group(""Fruit"").Subtitle(""6 left"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
    BitCommandPaletteCmd.New(""Bananas"").Id(""f-pantry:bananas"").Group(""Fruit"").Subtitle(""Ripe"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
    BitCommandPaletteCmd.New(""Cherries"").Id(""f-pantry:cherries"").Group(""Fruit"").Disabled(true, ""Out of season""),
    BitCommandPaletteCmd.New(""Carrots"").Id(""f-pantry:carrots"").Group(""Vegetables"").Subtitle(""1 kg"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
    BitCommandPaletteCmd.New(""Leeks"").Id(""f-pantry:leeks"").Group(""Vegetables"").Subtitle(""3"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
    BitCommandPaletteCmd.New(""Throw out the leftovers"").Id(""f-pantry:leftovers"").Group(""Vegetables"")
        .Destructive(""Throw them all out?"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
];";

    private readonly string example64RazorCode = @"
<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Commands=""fColorCommands""
                   Class=""f-custom-class""
                   Style=""--bit-CommandPalette-accent: #7c3aed; --bit-CommandPalette-active-background: #7c3aed22;"" />

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Commands=""fColorCommands""
                   Styles=""@(new() { Root = ""border-color: #7c3aed;"",
                                     Header = ""background: #7c3aed14;"",
                                     Input = ""font-weight: 600;"",
                                     List = ""padding: 0.75rem;"",
                                     GroupHeading = ""color: #7c3aed; text-transform: none; letter-spacing: 0;"",
                                     Item = ""margin-block: 2px;"",
                                     ActiveItem = ""background: #7c3aed; color: white;"",
                                     Empty = ""color: #7c3aed;"",
                                     NoResults = ""color: #b91c1c;"",
                                     Footer = ""background: #7c3aed14;"",
                                     Detail = ""background: #7c3aed0d;"" })"">
    <DetailTemplate Context=""command"">
        <b>@command.Title</b>
        <p>@command.Description</p>
    </DetailTemplate>
</BitCommandPalette>

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Commands=""fColorCommands""
                   Classes=""@(new() { Root = ""f-root"",
                                      Header = ""f-header-part"",
                                      Input = ""f-input"",
                                      List = ""f-list"",
                                      GroupHeading = ""f-heading"",
                                      Item = ""f-item"",
                                      ActiveItem = ""f-active-item"",
                                      Empty = ""f-empty"",
                                      NoResults = ""f-no-results"",
                                      Footer = ""f-footer-part"",
                                      Detail = ""f-detail-part"" })"">
    <DetailTemplate Context=""command"">
        <b>@command.Title</b>
        <p>@command.Description</p>
    </DetailTemplate>
</BitCommandPalette>

<BitCommandPaletteTrigger Styles=""@(new() { Root = ""border-color: #7c3aed; border-radius: 999px;"",
                                            Icon = ""color: #7c3aed;"",
                                            Label = ""font-weight: 600;"",
                                            Shortcut = ""opacity: .7;"" })"" />

<BitCommandPaletteTrigger Classes=""@(new() { Root = ""f-trigger"",
                                             Icon = ""f-trigger-icon"",
                                             Label = ""f-trigger-label"",
                                             Shortcut = ""f-trigger-shortcut"" })"" />

<style>
    .f-custom-class { max-width: 28rem; border-width: 2px; }

    .f-root { --bit-CommandPalette-accent: #0d9488; border-color: #0d9488; }
    .f-header-part { border-bottom: 2px solid #0d9488; }
    .f-input { color: #0f766e; }
    .f-list { background: #0d94880a; }
    .f-heading { color: #0f766e; }
    .f-item { border-inline-start: 3px solid transparent; }
    .f-active-item { border-inline-start-color: #0d9488; font-weight: 600; }
    .f-empty, .f-no-results { font-style: italic; }
    .f-footer-part { color: #0f766e; }
    .f-detail-part { border-inline-start: 2px solid #0d9488; }

    .f-trigger { border-style: dashed; }
    .f-trigger-icon { color: #0d9488; }
    .f-trigger-label { font-style: italic; }
    .f-trigger-shortcut { opacity: .6; }
</style>";
    private readonly string example64CsharpCode = @"
private static readonly BitCommandPaletteCommand[] fColorCommands =
[
    BitCommandPaletteCmd.New(""Violet"").Id(""f-color:violet"").Group(""Colors"").Badge(""Accent"")
        .Description(""#7c3aed, the accent of these samples.""),
    BitCommandPaletteCmd.New(""Indigo"").Id(""f-color:indigo"").Group(""Colors"")
        .Description(""#4f46e5, a deep blue-violet.""),
    BitCommandPaletteCmd.New(""Teal"").Id(""f-color:teal"").Group(""Colors"")
        .Description(""#0d9488, a cool blue-green.""),
    BitCommandPaletteCmd.New(""Nothing inside"").Id(""f-color:empty"").Group(""Pages"").Subtitle(""Opens an empty page"")
        .Children((_, _) => Task.FromResult<IReadOnlyList<BitCommandPaletteCommand>>([])),
];";

    private readonly string example65RazorCode = @"
<BitCommandPalette Dir=""BitDir.Rtl""
                   lang=""fa""
                   Inline
                   IncludeRegisteredCommands=""false""
                   EnableHistory=""false""
                   Strings=""fPersianStrings""
                   Commands=""fRtlCommands"" />";
    private readonly string example65CsharpCode = @"
// Persian strings that announce the result count in Persian digits.
private sealed class FPersianStrings : BitCommandPaletteStrings
{
    public override string ResultsAnnouncementFor(int count) => count switch
    {
        0 => NoResultsAnnouncement,
        1 => OneResultAnnouncement,
        _ => Format(ResultsAnnouncement, FPersianDigits(count)),
    };
}

private static string FPersianDigits(int value)
    => string.Concat(value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                          .Select(c => char.IsAsciiDigit(c) ? (char)('۰' + (c - '0')) : c));

private static readonly FPersianStrings fPersianStrings = new()
{
    Placeholder = ""فرمانی بنویسید یا جست‌وجو کنید…"",
    PromptPlaceholder = ""مقداری بنویسید و Enter را بزنید…"",
    NoResults = ""نتیجه‌ای برای «{0}» نیست"",
    NoResultsHint = ""عبارت دیگری را امتحان کنید، یا املای آن را بررسی کنید."",
    Empty = ""هنوز چیزی برای نمایش نیست."",
    Loading = ""در حال جست‌وجو…"",
    Recent = ""اخیر"",
    Pinned = ""سنجاق‌شده"",
    Suggestions = ""پیشنهادها"",
    Modes = ""حالت‌ها"",
    Actions = ""کارها"",
    Results = ""نتایج"",
    Other = ""سایر"",
    Navigate = ""برای حرکت"",
    Select = ""برای انتخاب"",
    Close = ""برای بستن"",
    ClearQuery = ""برای پاک کردن"",
    Back = ""برای بازگشت"",
    OpenActions = ""برای کارها"",
    Toggle = ""برای تغییر وضعیت"",
    Apply = ""برای اعمال"",
    Pin = ""سنجاق کردن به بالا"",
    Unpin = ""برداشتن سنجاق"",
    RemoveFromRecents = ""حذف از اخیر"",
    ClearRecents = ""پاک کردن فرمان‌های اخیر"",
    ConfirmPrefix = ""تأیید: "",
    ConfirmHint = ""برای تأیید دوباره Enter و برای لغو Esc را بزنید"",
    SelectedCount = ""{0} مورد انتخاب شد"",
    ResultsAnnouncement = ""{0} نتیجه موجود است"",
    OneResultAnnouncement = ""یک نتیجه موجود است"",
    NoResultsAnnouncement = ""نتیجه‌ای نیست"",
    Disabled = ""در دسترس نیست"",
    DialogLabel = ""پالت فرمان"",
    InputLabel = ""جست‌وجوی فرمان‌ها"",
    CloseButton = ""بستن"",
    BackButton = ""بازگشت"",
    Breadcrumb = ""مسیر"",
    OpenPalette = ""باز کردن پالت فرمان"",
    TriggerNameSeparator = ""، "",
    Search = ""جست‌وجو"",
    Home = ""خانه"",
    MoreResults = ""نمایش {0} مورد دیگر…"",
    LoadMore = ""نمایش بیشتر…"",
    SequencePending = ""در انتظار کلید بعدی…"",
    CommandFailed = ""اجرای «{0}» ممکن نشد."",
    CommandRefused = ""اجازهٔ اجرای «{0}» را ندارید."",
    CommandUnavailable = ""«{0}» دیگر در دسترس نیست."",
    LoadFailed = ""این فهرست بارگذاری نشد."",
    Retry = ""تلاش دوباره"",
    ApproximateResults = ""تطابق دقیقی پیدا نشد؛ نزدیک‌ترین‌ها نمایش داده می‌شوند."",
    Running = ""در حال انجام…"",
    SourceFailed = ""جست‌وجو در {0} ممکن نشد"",
    On = ""روشن"",
    Off = ""خاموش"",
    ShortcutThen = ""سپس"",
};

private List<BitCommandPaletteCommand> fRtlCommands = [];
private bool fRtlDark;

protected override void OnInitialized() => fRtlCommands = FBuildRtlCommands();

private List<BitCommandPaletteCommand> FBuildRtlCommands() =>
[
    BitCommandPaletteCmd.New(""باز کردن تنظیمات"").Id(""f-rtl:settings"").Group(""عمومی"").Keywords(""Open settings"")
        .Shortcut(""G S"", global: false)
        .Actions(BitCommandPaletteCmd.New(""کپی کردن پیوند"").Id(""f-rtl:settings.copy"")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""پیوند کپی شد (نه واقعاً)"", keepOpen: true))))
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""تنظیمات باز شد"", keepOpen: true))),
    BitCommandPaletteCmd.New(""حالت تاریک"").Id(""f-rtl:dark"").Group(""عمومی"").Keywords(""Dark mode"")
        .Checked(fRtlDark)
        .Handler(_ =>
        {
            fRtlDark = !fRtlDark;
            fRtlCommands = FBuildRtlCommands();
            StateHasChanged();
            return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
        }),
    BitCommandPaletteCmd.New(""Open in VS Code…"").Id(""f-rtl:vscode"").Group(""عمومی"").Subtitle(""ویرایشگر پیش‌فرض"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""در VS Code باز شد (نه واقعاً)"", keepOpen: true))),
    BitCommandPaletteCmd.New(""خروجی گرفتن…"").Id(""f-rtl:export"").Group(""عمومی"").Keywords(""Export"").Subtitle(""۲ قالب"")
        .ChildrenPlaceholder(""قالبی را انتخاب کنید…"")
        .Children(
            BitCommandPaletteCmd.New(""به صورت PDF"").Id(""f-rtl:export.pdf"")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""خروجی PDF گرفته شد"", keepOpen: true))),
            BitCommandPaletteCmd.New(""به صورت CSV"").Id(""f-rtl:export.csv"").Disabled(true, ""به طرح Pro نیاز دارد"")),
    BitCommandPaletteCmd.New(""حذف همه فایل‌ها"").Id(""f-rtl:delete"").Group(""منطقه خطر"").Keywords(""Delete all files"")
        .Destructive(""همه فایل‌ها برای همیشه حذف شوند؟"")
        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(""حذف شد (نه واقعاً)"", keepOpen: true))),
];";
}
