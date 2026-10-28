using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bdnd.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, the guide's own sections on
/// installing the package, registering its service and on hosting models, and - where the demo is
/// that render mode - the demo's real hosting files.
/// <para>
/// Bdnd needs little wiring, and each part of it fails in its own way. The service registration
/// belongs in every container - a component of the app's own that injects <c>BdndInterop</c> fails
/// without it, even though the draggables and drop targets then create the interop themselves - and
/// in a Blazor Web App that is both, because the pages are prerendered on the server before they run
/// in the browser. Every component must sit inside a <c>BdndProvider</c>, or it throws as it
/// initializes. And a statically rendered page renders its draggables as inert markup that never
/// wires up a drag event. What does differ per render mode is where the drag callbacks run - in the
/// browser on WebAssembly, a round trip away under Blazor Server - and so which project the pages
/// belong in.
/// </para>
/// </summary>
public static partial class BdndSetupGuide
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
                One project, and the drag callbacks run on the server: the browser raises dragstart, dragenter and drop
                at once, and each managed handler - OnBegin, OnHover, OnDrop, OnEnd - runs a SignalR round trip later.
                Drops still land where they should; what changes is that `ItemFactory` returning null can no longer stop
                the native drag (it goes on as an inert drag no target accepts), a custom drag layer starts from an
                estimated position that is corrected a moment later, and `OnHover` sends a message over the circuit for
                every dragover - bind it only where you need it.
                """,
                ["Install", "Register the service", "Blazor Server", "Prerendering"],
                ServerFiles,
                IncludeDemo: false),

            "wasm" => new Mode(
                "Blazor Web App - InteractiveWebAssembly",
                """
                The drag callbacks run in the browser, so none of them waits on the network - this is how the demo is
                built, and the files below are its own. The pages that drag must live in the .Client project so their
                assembly is downloaded to the browser; the server project prerenders the same pages, where the
                components render their idle state and come alive once WebAssembly starts. Reference `Bit.Bdnd` from the
                .Client project (the server gets it transitively) and call `AddBitBdndServices()` in BOTH Program.cs
                files - the demo does it once, in the `AddDemoServices` extension both of them call.
                """,
                ["Install", "Register the service", "Blazor Web App", "Blazor WebAssembly", "Prerendering"],
                null,
                IncludeDemo: true),

            "auto" => new Mode(
                "Blazor Web App - InteractiveAuto",
                """
                The first visit renders on the server while the WebAssembly runtime downloads, later ones in the browser,
                so the same components run in either place - with a round trip per drag callback on the first visit and
                none afterwards. The wiring is the same in both: `AddBitBdndServices()` in BOTH Program.cs files, the
                server's and the client's, and the pages in the .Client project. The demo below uses InteractiveWebAssembly; for
                InteractiveAuto add `AddInteractiveServerComponents()` and `AddInteractiveServerRenderMode()` to the
                server's `Program.cs` and use `@rendermode="InteractiveAuto"` in `App.razor` - nothing else changes.
                """,
                ["Install", "Register the service", "Blazor Web App", "Blazor WebAssembly", "Blazor Server", "Prerendering"],
                null,
                IncludeDemo: true),

            "standalone-wasm" => new Mode(
                "standalone Blazor WebAssembly",
                """
                One project and no prerendering: the components run in the browser from the first render, and every drag
                callback is handled without a network round trip. Register the service in the one `Program.cs` there is.
                This is the setup for an app that loads Bit.Bdnd at startup; one that lazy loads it registers nothing - see
                `GetBdndGuideSection(heading: "Lazy loading on WebAssembly")`.
                """,
                ["Install", "Register the service", "Blazor WebAssembly"],
                StandaloneFiles,
                IncludeDemo: false),

            "hybrid" => new Mode(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                The components run in-process and render into the embedded WebView, whose JS runtime is not in-process -
                so they behave as on Blazor Server, without the network latency. Drag and drop needs a web view that
                raises HTML5 drag events: desktop web views do, touch input on mobile web views generally does not.
                """,
                ["Install", "Register the service", "Blazor Hybrid"],
                HybridFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Bdnd up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own. The server's `Program.cs` ")
                   .AppendLine("also maps this MCP server - leave those lines out of your app.")
                   .AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BdndSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on installing and on this hosting model, so the caveats are the
        // library's rather than a paraphrase of them that can drift.
        var quoted = mode.GuideSections.Select(BdndSourceCatalog.GetGuideSection).Where(section => section is not null).ToArray();

        if (quoted.Length > 0)
        {
            builder.AppendLine("## From the guide").AppendLine();

            foreach (var section in quoted) builder.AppendLine(Demote(section!)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    /// <summary>The checklist every render mode's guide opens with - the same few steps, and the failure each one prevents.</summary>
    public const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Bdnd` - one package, everything under the `Bit.Bdnd` namespace. In a Blazor Web App
           with an interactive client, add it to the .Client project.
        2. Register the service: `builder.Services.AddBitBdndServices();` in `Program.cs`, in EVERY project that renders
           the components. In a Blazor Web App that is both the server's and the client's `Program.cs`: the pages are
           prerendered on the server, so what a component injects has to resolve in both containers. The draggables and
           drop targets create the interop themselves where it is missing, but a component of your own that injects
           `BdndInterop` - a sortable card measuring itself - throws `There is no registered service of type
           'Bit.Bdnd.BdndInterop'`. Calling it twice is harmless. (Only a WebAssembly app that lazy loads Bit.Bdnd leaves
           the registration out - see the guide's lazy loading section.)
        3. Add `@using Bit.Bdnd` to `_Imports.razor`.
        4. There is NO stylesheet to link and no script tag to add: the components import their script module,
           `_content/Bit.Bdnd/bit-bdnd.js`, themselves the first time they need it. Style the drag states yourself,
           from the collected state (`IsDragging`, `IsOver`, `CanDrop`).
        5. Wrap every region that drags and drops in a `<BdndProvider>`. A `BdndDraggable`, `BdndDropTarget` or
           `BdndDragLayer` outside one throws `InvalidOperationException: ... must be placed inside a BdndProvider`, and
           a draggable only reaches the targets of its own provider.
        6. Render the page interactively (`@rendermode InteractiveWebAssembly`, `InteractiveServer` or `InteractiveAuto`,
           or an interactive parent). On a statically rendered page the markup appears and nothing can be dragged.
           While prerendering, the components render their idle state and start working once the page is interactive.
        7. Match the types: a target only reacts to a draggable whose `Type` is in its `Accept`/`AcceptTypes` - compared
           as strings, case included, never by the item's .NET type. Keep the strings in one constants class.
        8. Drags need a pointer that raises HTML5 drag events - a mouse, a pen or a trackpad. Like React DnD's HTML5
           backend, Bdnd has no touch backend.
        """;

    private const string ServerFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        using Bit.Bdnd;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddBitBdndServices();

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        ### `Components/_Imports.razor`

        ```razor
        @using Bit.Bdnd
        ```

        ### `Components/Pages/Board.razor`

        ```razor
        @page "/board"
        @rendermode InteractiveServer

        <BdndProvider>
            <BdndDraggable TItem="string" Type="card" Item="@("Write the docs")" Context="drag">
                <div class="card" style="opacity: @(drag.IsDragging ? 0.4 : 1)">Write the docs</div>
            </BdndDraggable>

            <BdndDropTarget TItem="string" Accept="card" OnDrop="@(e => _done = e.Item)" Context="drop">
                <div class="column @(drop.IsOver && drop.CanDrop ? "highlight" : "")">Done: @_done</div>
            </BdndDropTarget>
        </BdndProvider>

        @code {
            private string? _done;
        }
        ```
        """;

    private const string StandaloneFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        using Bit.Bdnd;
        using Microsoft.AspNetCore.Components.Web;
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddBitBdndServices();

        await builder.Build().RunAsync();
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Bdnd
        ```

        ### `Pages/Board.razor`

        ```razor
        @page "/board"

        <BdndProvider>
            <BdndDraggable TItem="string" Type="card" Item="@("Write the docs")">
                <div class="card">Write the docs</div>
            </BdndDraggable>

            <BdndDropTarget TItem="string" Accept="card" OnDrop="@(e => _done = e.Item)" Context="drop">
                <div class="column @(drop.IsOver ? "highlight" : "")">Done: @_done</div>
            </BdndDropTarget>
        </BdndProvider>

        @code {
            private string? _done;
        }
        ```

        Nothing goes into `wwwroot/index.html`: there is no stylesheet, and the script module is imported on demand.
        """;

    private const string HybridFiles = """
        ## The files

        ### `MauiProgram.cs` (or the WPF / Windows Forms host)

        ```csharp
        using Bit.Bdnd;

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddBitBdndServices();
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Bdnd
        ```

        Put a `<BdndProvider>` with its draggables and drop targets on any page the BlazorWebView renders; nothing goes
        into `wwwroot/index.html`.
        """;

    private const string Next = """
        The drag itself is markup - call `GetBdndGuideSection(heading: "Getting started")` for a first drag and drop,
        `GetBdndApi(typeName: "BdndDraggable")` or `GetBdndApi(typeName: "BdndDropTarget")` for every parameter with its
        real default, `GetBdndSourceFile()` for the index of the demo's working pages, or `InspectBdndDrop()` to see how a
        drag over nested targets plays out.
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
        content = content.TrimStart('\uFEFF').TrimEnd();
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
        ".ts" => "typescript",
        ".js" => "javascript",
        _ => string.Empty
    };

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
