using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Blow.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, the guide's own sections on
/// installing the package and on hosting models, and - where the demo is that render mode - the
/// demo's real hosting files.
/// <para>
/// Blow needs little wiring - no services at all - and each of the few things it does need fails
/// without a word: the stylesheet is not injected for you, and a page without it renders every node
/// in a pile at the top-left; the canvas fills its container, so a container with no height renders
/// a canvas zero pixels tall; and a statically rendered page never becomes interactive, so its canvas
/// never measures a node or answers a drag. What does differ per render mode is where the canvas runs -
/// on WebAssembly every pointer move of a drag is handled in the browser, under Blazor Server it is
/// a round trip - and so which project the pages belong in.
/// </para>
/// </summary>
public static partial class BlowSetupGuide
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
                One project, and the canvas runs on the server: every pointer move of a drag, a pan, a connection or a
                resize is handled in C#, so each one is a round trip over the SignalR circuit and the editor feels exactly
                as far away as the server is. That is fine on a fast connection or for a flow people mostly look at; for
                heavy editing over the internet, render just the editor pages with `InteractiveWebAssembly`.
                """,
                ["Install", "Blazor Server", "Blazor Web App", "Prerendering", "Content security policy"],
                ServerFiles,
                IncludeDemo: false),

            "wasm" => new Mode(
                "Blazor Web App - InteractiveWebAssembly",
                """
                The canvas runs in the browser, so no gesture waits on the network - this is how the demo is built, and
                the files below are its own. The pages with a canvas must live in the .Client project so their assembly
                is downloaded to the browser; the server project prerenders the same page, where the canvas renders its
                first frame from your Nodes and Edges and comes alive once WebAssembly starts. Reference `Bit.Blow` from
                the .Client project (the server gets it transitively), and link the stylesheet from the server's
                `App.razor`, which is the host page. The demo's `AddDemoServices` registers nothing - Blow has no services.
                """,
                ["Install", "Blazor Web App", "Blazor WebAssembly", "Prerendering", "Content security policy"],
                null,
                IncludeDemo: true),

            "auto" => new Mode(
                "Blazor Web App - InteractiveAuto",
                """
                The first visit renders on the server while the WebAssembly runtime downloads, later ones in the browser,
                so the same canvas runs in either place - with a round trip per pointer move on the first visit and none
                afterwards. Nothing about the wiring changes between the two: there are no services to register in either
                container, but the pages must live in the .Client project. The demo below uses InteractiveWebAssembly; for
                InteractiveAuto add `AddInteractiveServerComponents()` and `AddInteractiveServerRenderMode()` to the
                server's `Program.cs` and use `@rendermode="InteractiveAuto"` in `App.razor` - nothing else changes.
                """,
                ["Install", "Blazor Web App", "Blazor WebAssembly", "Blazor Server", "Prerendering", "Content security policy"],
                null,
                IncludeDemo: true),

            "standalone-wasm" => new Mode(
                "standalone Blazor WebAssembly",
                """
                One project and no prerendering: the canvas runs in the browser from the first render. The host page is
                `wwwroot/index.html`, so that is where the stylesheet link goes.
                """,
                ["Install", "Blazor WebAssembly", "Content security policy"],
                StandaloneFiles,
                IncludeDemo: false),

            "hybrid" => new Mode(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                The canvas runs in-process and renders into the embedded WebView, so its pointer events cross a local
                bridge rather than a network. The host page is the app's `wwwroot/index.html`, so that is where the
                stylesheet link goes.
                """,
                ["Install", "Blazor Hybrid"],
                HybridFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Blow up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own.").AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BlowSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on installing and on this hosting model, so the caveats are the
        // library's rather than a paraphrase of them that can drift.
        var quoted = mode.GuideSections.Select(BlowSourceCatalog.GetGuideSection).Where(section => section is not null).ToArray();

        if (quoted.Length > 0)
        {
            builder.AppendLine("## From the guide").AppendLine();

            foreach (var section in quoted) builder.AppendLine(Demote(section!)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    /// <summary>The checklist every render mode's guide opens with - the same few steps, and the silent failures each one prevents.</summary>
    public const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Blow` - one package, everything under the `Bit.Blow` namespace. In a Blazor Web App
           with an interactive client, add it to the .Client project.
        2. Add `@using Bit.Blow` to `_Imports.razor`.
        3. There are NO services to register - do not look for an `AddBlow...` method, there is none.
        4. Link the stylesheet from the host page - it is REQUIRED, and nothing adds it for you:
           `<link rel="stylesheet" href="_content/Bit.Blow/bit-blow.css" />` (in a Blazor Web App's `App.razor`,
           `@Assets["_content/Bit.Blow/bit-blow.css"]`). Without it nodes are not positioned, handles do not sit on their
           sides and the add-ons are unstyled. Load your own overrides of its `--bit-blow-*` properties after it. The
           library's script is a module the canvas imports itself; there is no script tag to add.
        5. Give the canvas a height. `BlowCanvas` fills its container (`width: 100%; height: 100%`), so inside a
           container with no height it is zero pixels tall and nothing shows: `<div style="height: 600px"><BlowCanvas ... /></div>`,
           or `Style="height: 600px"` on the canvas itself.
        6. Render the canvas interactively. On a statically rendered page it is inert markup - it never measures a node,
           never anchors an edge to its handle, never pans, drags or connects. During prerendering it draws a first frame
           and comes alive once interactivity starts.
        7. Hand it `List<BlowNode>` and `List<BlowEdge>` fields and keep them: the canvas edits those lists in place as the
           user works and raises `NodesChanged`/`EdgesChanged` (and `OnSelectionChange`, `OnConnect`...). Bind those when
           your own markup reads the graph - an edit on the canvas re-renders only the canvas. A change you make outside
           a render of your component (a timer, a message from a server) needs `canvas.Refresh(remeasure: true)` or
           `InvokeAsync(StateHasChanged)`; a handle moved to another side needs `canvas.UpdateNodeInternals()`.
        8. Handle `OnError` while you build the graph: duplicate ids, edges to missing nodes and missing parents are
           reported there, once each, instead of being thrown.
        """;

    private const string ServerFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Nothing to register for Blow.

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        ### `Components/App.razor` (in `<head>`)

        ```razor
        <link rel="stylesheet" href="@Assets["_content/Bit.Blow/bit-blow.css"]" />
        ```

        ### `Components/_Imports.razor`

        ```razor
        @using Bit.Blow
        ```

        ### `Components/Pages/Flow.razor`

        ```razor
        @page "/flow"
        @rendermode InteractiveServer

        <div style="height: 600px">
            <BlowCanvas Nodes="_nodes" Edges="_edges" FitViewOnInit="true">
                <BlowBackground />
                <BlowControls />
            </BlowCanvas>
        </div>

        @code {
            private List<BlowNode> _nodes =
            [
                new() { Id = "1", Type = "input", Label = "Start", Position = new(0, 0) },
                new() { Id = "2", Type = "output", Label = "End", Position = new(0, 120) },
            ];

            private List<BlowEdge> _edges = [new() { Id = "e1-2", Source = "1", Target = "2" }];
        }
        ```
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

        // Nothing to register for Blow.

        await builder.Build().RunAsync();
        ```

        ### `wwwroot/index.html` (in `<head>`)

        ```html
        <link rel="stylesheet" href="_content/Bit.Blow/bit-blow.css" />
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Blow
        ```

        ### `Pages/Flow.razor`

        ```razor
        @page "/flow"

        <BlowCanvas Nodes="_nodes" Edges="_edges" Style="height: 600px" FitViewOnInit="true" />

        @code {
            private List<BlowNode> _nodes = [new() { Id = "1", Label = "Hello", Position = new(0, 0) }];
            private List<BlowEdge> _edges = [];
        }
        ```
        """;

    private const string HybridFiles = """
        ## The files

        ### `MauiProgram.cs` (or the WPF / Windows Forms host)

        ```csharp
        builder.Services.AddMauiBlazorWebView();

        // Nothing to register for Blow.
        ```

        ### `wwwroot/index.html` (in `<head>`)

        ```html
        <link rel="stylesheet" href="_content/Bit.Blow/bit-blow.css" />
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Blow
        ```

        Put `<BlowCanvas Nodes="_nodes" Edges="_edges" Style="height: 600px" />` on any page the BlazorWebView renders.
        """;

    private const string Next = """
        The flow itself is markup and plain objects - call `GetBlowGuideSection(heading: "Getting started")` for a first
        flow, custom nodes and edges, `GetBlowApi(typeName: "BlowCanvas")` for every parameter with its real default, or
        `GetBlowSourceFile()` for the index of the demo's working pages and examples.
        """;

    /// <summary>
    /// Pushes a quoted guide section down so its own heading is a third-level one, nested under "From
    /// the guide" instead of competing with the headings of this document - whatever level it started
    /// at, so a top-level section of the README quoted here would nest just the same as a sub-section.
    /// </summary>
    private static string Demote(string section)
    {
        var lines = section.Split('\n');
        var fenced = false;

        var top = lines.Select(line => line.TakeWhile(c => c == '#').Count()).FirstOrDefault(level => level > 0);
        var shift = top == 0 ? 0 : Math.Max(0, 3 - top);

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                fenced = fenced is false;
                continue;
            }

            if (fenced is false && lines[i].StartsWith('#')) lines[i] = $"{new string('#', shift)}{lines[i]}";
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
