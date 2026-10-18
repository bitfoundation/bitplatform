using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Balette.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, the guide's own section on that
/// hosting model, and - where the demo is that render mode - the demo's real hosting files.
/// <para>
/// Four things have to be in place before Ctrl+K opens anything, and each one fails quietly when it is
/// missing: the services in every DI container that renders the palette (a missing one surfaces while
/// prerendering, not at compile time), the stylesheet link (it is not injected - without it the palette
/// opens as unstyled markup at the top of the page), one <c>&lt;Balette /&gt;</c> in a layout that is
/// rendered interactively (a statically rendered one never attaches its keyboard handling), and the
/// namespace import. Handing over a known-good project instead of prose removes the guesswork about
/// which project each of them belongs in; the guide's hosting sections, quoted verbatim, say the rest in
/// the library's own words.
/// </para>
/// </summary>
public static partial class BaletteSetupGuide
{
    public static readonly string[] RenderModes = ["server", "wasm", "auto", "standalone-wasm", "hybrid"];

    /// <summary>The demo files that make up its wiring - the demo is a Blazor Web App using InteractiveWebAssembly.</summary>
    private static readonly string[] _hostingFiles =
    [
        "Demo/Client/Extensions/IServiceCollectionExtensions.cs",
        "Demo/Server/Program.cs",
        "Demo/Client/Program.cs",
        "Demo/Server/Components/App.razor",
        "Demo/Client/Routes.razor",
        "Demo/Client/Layout/MainLayout.razor",
        "Demo/Client/_Imports.razor",
    ];

    private sealed record Mode(string Title, string Summary, string[] GuideSections, string? Files, bool IncludeDemo);

    /// <summary>The wiring for one render mode as Markdown, or null when the mode is not one of <see cref="RenderModes"/>.</summary>
    public static string? Get(string? renderMode) => Describe(renderMode) is { } mode ? Compose(mode) : null;

    /// <summary>What sets one render mode apart, in a paragraph - the line the search index ranks a setup guide by.</summary>
    public static string Summary(string renderMode) => Describe(renderMode) is { } mode ? $"{mode.Title}. {mode.Summary.ReplaceLineEndings(" ")}" : string.Empty;

    private static Mode? Describe(string? renderMode)
    {
        var mode = (renderMode ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

        mode = mode switch
        {
            "interactiveserver" or "blazor-server" => "server",
            "webassembly" or "interactivewebassembly" or "blazor-wasm" => "wasm",
            "interactiveauto" => "auto",
            "standalone" or "standalone-webassembly" or "standalone-wasm-app" => "standalone-wasm",
            "maui" or "blazor-hybrid" or "wpf" or "winforms" => "hybrid",
            _ => mode
        };

        return mode switch
        {
            "server" => new Mode(
                "Blazor Web App - InteractiveServer",
                """
                One project, one DI container per circuit: `AddBitBaletteServices` is called once, in `Program.cs`, and every
                user gets their own registry, open state and history. Shortcuts, sequences and the navigation keys are
                matched in the browser, so typing on the page costs nothing; what crosses the connection is the query on
                every keystroke and the command that was picked. The palette therefore searches synchronously per keystroke
                rather than deferring to the next frame - the network already separates typing from rendering - and the
                handlers run on the server, in the circuit's scope. Matching itself is fast there; what a user feels is
                the round trip each keystroke waits for before the list updates - so on connections with real latency,
                render the layout with `InteractiveWebAssembly` instead.
                """,
                ["Blazor Server", "Blazor Web App", "Content security policy"],
                ServerFiles,
                IncludeDemo: false),

            "wasm" => new Mode(
                "Blazor Web App - InteractiveWebAssembly",
                """
                Two projects, two DI containers: the server prerenders the very same layout the browser then runs, so
                `AddBitBaletteServices` must be called in BOTH `Program.cs` files - the demo does it through one shared
                extension method both projects call (`AddDemoServices` below), which also keeps the options identical.
                The package and the components belong in the .Client project; the stylesheet is linked by the server's
                `App.razor`. Matching and ranking run in the browser, next to the list they fill. This is exactly how the
                demo is built, so the files below are its own - `AddDemoServices` also registers the demo's own state and
                fake directory, and the MCP lines of the server's `Program.cs` (`AddControllers`, `AddMcpServer`,
                `MapControllers`, `MapMcp`) serve this site's MCP server; neither is anything the palette needs.
                """,
                ["Blazor Web App", "Blazor WebAssembly", "Content security policy"],
                null,
                IncludeDemo: true),

            "auto" => new Mode(
                "Blazor Web App - InteractiveAuto",
                """
                Two projects, two DI containers, and the palette may run in either: the first visit renders on the server
                while the WebAssembly runtime downloads, later ones in the browser. So BOTH containers register the
                services - with the SAME options, since the palette must behave the same wherever it runs - and the
                registry is per circuit on the server, per runtime in the browser. The demo below uses
                InteractiveWebAssembly; for InteractiveAuto add `AddInteractiveServerComponents()` and
                `AddInteractiveServerRenderMode()` to the server's `Program.cs` and use `@rendermode="InteractiveAuto"` on
                `<Routes>` and `<HeadOutlet>` in `App.razor` - nothing else about the wiring changes. Its MCP lines serve this
                site's MCP server, not the palette.
                """,
                ["Blazor Web App", "Blazor WebAssembly", "Blazor Server", "Content security policy"],
                null,
                IncludeDemo: true),

            "standalone-wasm" => new Mode(
                "standalone Blazor WebAssembly",
                """
                One project, one DI container for the lifetime of the app, and no prerendering - the palette is interactive
                from the first render and searches in the browser.
                """,
                ["Blazor WebAssembly", "Content security policy"],
                StandaloneFiles,
                IncludeDemo: false),

            "hybrid" => new Mode(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                One container, in-process: the palette runs in the BlazorWebView like any other component, and the history
                goes to the web view's `localStorage`. The stylesheet is linked from the web view's `wwwroot/index.html`.
                """,
                ["Blazor Hybrid", "Content security policy"],
                HybridFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Balette up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is once the projects and namespaces are renamed to your own - except what is the ")
                   .AppendLine("demo's own: `<NavMenu />` and the `DemoState`/`DemoCommands` the layout uses stand for your navigation and your commands.")
                   .AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BaletteSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on the hosting model, so the caveats are the library's rather than a
        // paraphrase of them that can drift.
        builder.AppendLine("## From the guide").AppendLine();

        foreach (var heading in mode.GuideSections)
        {
            var section = BaletteSourceCatalog.GetGuideSection(heading);
            if (section is null) continue;

            builder.AppendLine(Demote(section)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Balette` - one package, everything under the `Bit.Balette` namespace. In a Blazor Web App
           with an interactive client, add it to the .Client project (the server project gets it through its reference).
        2. Call `builder.Services.AddBitBaletteServices(...)` in EVERY DI container that renders the palette. A Blazor Web App
           with an interactive client has two of them, and a missing registration surfaces as a failure to resolve
           `IBaletteService` while the page is prerendered. Share one extension method between the two `Program.cs` files so
           both get the same options.
        3. Link the stylesheet from the host page - `App.razor` in a Web App, `wwwroot/index.html` otherwise - BEFORE your own
           stylesheet, so your overrides of its `--bit-balette-*` variables win:
           `<link rel="stylesheet" href="_content/Bit.Balette/bit-balette.css" />`. It is NOT injected for you: without it the
           palette opens as unstyled markup.
        4. Add `@using Bit.Balette` to the `_Imports.razor` of every project with palette markup or commands.
        5. Render exactly one `<Balette />`, in a layout that is rendered interactively (global interactivity, as below). In a
           statically rendered layout it never attaches its keyboard handling, so Ctrl+K does nothing. With per-page
           interactivity, wrap it in a small component rendered with an interactive mode, and register commands from
           components in that same mode - the registry is scoped to one circuit or one WebAssembly runtime.
        6. Register commands with `IBaletteService.Register(...)` - and dispose the handle it returns when the page goes - or
           with `<BaletteCommands Commands="_commands" />`, which does both for you; app-wide ones once, from the layout or
           through `options.Commands`. Commands are immutable: to change one, hand over a new list or call `Update`.
        7. Holding the component in a field (`@ref`) inside a namespace under `Bit` other than `Bit.Balette` itself: write the
           type as `Bit.Balette.Balette` - there, `Balette` alone names the namespace, and the field does not compile.
        """;

    private const string ServerFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddBitBaletteServices(options =>
        {
            options.GroupOrder.AddRange(["Navigation", "Actions"]);
        });

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        ### `Components/App.razor` (the `<head>` and the `<body>`)

        ```razor
        <link rel="stylesheet" href="@Assets["_content/Bit.Balette/bit-balette.css"]" />
        <link rel="stylesheet" href="@Assets["app.css"]" />
        <HeadOutlet @rendermode="InteractiveServer" />
        ...
        <Routes @rendermode="InteractiveServer" />
        ```

        ### `Components/Layout/MainLayout.razor`

        ```razor
        @inherits LayoutComponentBase

        <header><BaletteTrigger Label="Search or run a command…" /></header>

        <main>@Body</main>

        @* App-wide commands, registered while the layout is rendered. *@
        <BaletteCommands Commands="_commands" />

        @* One palette for the whole app. *@
        <Balette />

        @code {
            private readonly BaletteCommand[] _commands =
            [
                BaletteCmd.Navigate("Home", "/").Group("Navigation").Shortcut("G H"),
                BaletteCmd.Navigate("Settings", "/settings").Group("Navigation").Shortcut("G S").Keywords("preferences"),
            ];
        }
        ```

        ### `Components/_Imports.razor`

        ```razor
        @using Bit.Balette
        ```

        Blazor Server reconnects with a new circuit, and so a new scope: the commands are registered again by the
        components that render, and the history is in the browser's `localStorage`, so recents and pins survive it.
        """;

    private const string StandaloneFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        using Microsoft.AspNetCore.Components.Web;
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddBitBaletteServices();

        await builder.Build().RunAsync();
        ```

        ### `wwwroot/index.html` (the `<head>`)

        ```html
        <link rel="stylesheet" href="_content/Bit.Balette/bit-balette.css" />
        <link rel="stylesheet" href="css/app.css" />
        ```

        ### `Layout/MainLayout.razor`

        ```razor
        @inherits LayoutComponentBase

        <BaletteTrigger />

        <main>@Body</main>

        <Balette />
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Balette
        ```

        There is no script tag to add: the library's JavaScript is a module it imports itself, from
        `_content/Bit.Balette/bit-balette.js`.
        """;

    private const string HybridFiles = """
        ## The files

        ### `MauiProgram.cs` (or the WPF / Windows Forms host)

        ```csharp
        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddBitBaletteServices();
        ```

        ### `wwwroot/index.html` (the `<head>`)

        ```html
        <link rel="stylesheet" href="_content/Bit.Balette/bit-balette.css" />
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Balette
        ```

        Put `<Balette />` in the `MainLayout.razor` the BlazorWebView renders.
        """;

    private const string Next = """
        The commands themselves are plain objects - call `GetBaletteGuideSection(heading: "Commands")` for everything a
        command can be, `GetBaletteSourceFile(path: "Demo/Client/Services/DemoCommands.cs")` for the demo's whole registry,
        or `GetBaletteSourceFile()` for the index of the demo's working pages.
        """;

    /// <summary>
    /// Pushes a quoted guide section two heading levels down, so it nests under "From the guide"
    /// instead of competing with the headings of this document.
    /// </summary>
    private static string Demote(string section)
    {
        var lines = section.Split('\n');
        var fenced = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                fenced = fenced is false;
                continue;
            }

            if (fenced is false && lines[i].StartsWith('#')) lines[i] = $"#{lines[i]}";
        }

        return string.Join('\n', lines);
    }

    private static void AppendFile(StringBuilder builder, string path, string content)
    {
        content = content.TrimEnd();
        var fence = Fence(content);

        builder.AppendLine($"### `{path}`").AppendLine()
               .AppendLine($"{fence}{Language(path)}")
               .AppendLine(content)
               .AppendLine(fence)
               .AppendLine();
    }

    /// <summary>
    /// The fence for a file's code block. It has to outrun the longest run of backticks in the file
    /// itself - a file that shows Markdown would otherwise end the block in the middle of its own
    /// content, and the rest of the file would render as prose.
    /// </summary>
    private static string Fence(string content)
    {
        var longest = BacktickRunRegex().Matches(content).Select(match => match.Length).DefaultIfEmpty(0).Max();

        return new string('`', Math.Max(3, longest + 1));
    }

    private static string Language(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".razor" => "razor",
        ".cs" => "csharp",
        ".csproj" => "xml",
        ".css" => "css",
        ".js" => "javascript",
        _ => string.Empty
    };

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
