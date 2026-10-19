using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bortable.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, the guide's own sections on
/// installing the package, registering its services, linking its stylesheet and on hosting models,
/// and - where the demo is that render mode - the demo's real hosting files.
/// <para>
/// Bortable needs three things wired, and each fails in its own way. The service registration fails
/// loudly - the first list that renders throws, unable to inject its coordinator - but in a Blazor
/// Web App it is needed twice, once per container, and the server's copy is the one people leave
/// out, because the list "runs on WebAssembly". The other two fail without a word: without the
/// stylesheet a drag still works, but the dragged item is not dimmed and its original does not hide
/// while it is previewed in another list, so the item seems to be in two places at once; and a
/// statically rendered page shows its items and never answers a drag. What differs per render mode
/// is where the drag handlers run - on WebAssembly every dragover is handled in the browser, under
/// Blazor Server it is a round trip - and so which project the pages, and the registrations, belong in.
/// </para>
/// </summary>
public static partial class BortableSetupGuide
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

    /// <summary>The guide's sections every render mode quotes: what to install and register, and the stylesheet.</summary>
    private static readonly string[] _gettingStarted = ["Install", "Register the services", "Reference the stylesheet"];

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
                One project, and the lists run on the server: every dragstart, dragover and drop is handled in C#, so each one
                is a round trip over the SignalR circuit. A dragover that arrives while the previous one is still being
                handled is skipped rather than queued, so the list keeps up, but the preview trails the pointer by the
                network's latency. The drag coordinator is scoped to the circuit, so two users never share a drag. For heavy
                dragging over the internet, render just the pages with lists with `InteractiveWebAssembly`.
                """,
                [.. _gettingStarted, "Blazor Server", "Blazor Web App", "Prerendering", "Content security policy"],
                ServerFiles,
                IncludeDemo: false),

            "wasm" => new Mode(
                "Blazor Web App - InteractiveWebAssembly",
                """
                The lists run in the browser, so no dragover waits on the network - this is how the demo is built, and the
                files below are its own. The pages with lists must live in the .Client project so their assembly is
                downloaded to the browser; reference `Bit.Bortable` from it (the server gets it transitively). Register the
                services in BOTH `Program.cs` files: the server prerenders the same pages and has to resolve what each list
                injects - the demo does it once, in a shared `AddDemoServices` both projects call. Link the stylesheet from the
                server's `App.razor`, which is the host page.
                """,
                [.. _gettingStarted, "Blazor Web App", "Blazor WebAssembly", "Prerendering", "Content security policy"],
                null,
                IncludeDemo: true),

            "auto" => new Mode(
                "Blazor Web App - InteractiveAuto",
                """
                The first visit renders on the server while the WebAssembly runtime downloads, later ones in the browser, so
                the same list runs in either place - with a round trip per dragover on the first visit and none afterwards.
                Both containers therefore resolve the lists' services: register them in BOTH `Program.cs` files, and put the
                pages in the .Client project. The demo below uses InteractiveWebAssembly; for InteractiveAuto add
                `AddInteractiveServerComponents()` and `AddInteractiveServerRenderMode()` to the server's `Program.cs` and use
                `@rendermode="InteractiveAuto"` in `App.razor` - nothing else changes.
                """,
                [.. _gettingStarted, "Blazor Web App", "Blazor WebAssembly", "Blazor Server", "Prerendering", "Content security policy"],
                null,
                IncludeDemo: true),

            "standalone-wasm" => new Mode(
                "standalone Blazor WebAssembly",
                """
                One project and no prerendering: the lists run in the browser from the first render, and there is one
                container to register the services in. The host page is `wwwroot/index.html`, so that is where the
                stylesheet link goes.
                """,
                [.. _gettingStarted, "Blazor WebAssembly", "Content security policy"],
                StandaloneFiles,
                IncludeDemo: false),

            "hybrid" => new Mode(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                The lists run in-process and render into the embedded web view, so their drag events cross a local bridge
                rather than a network; drag and drop is the web view's own, so it works wherever the platform's web view
                supports HTML5 drag and drop. Register the services in `MauiProgram.cs` (or the WPF / Windows Forms host);
                the host page is the app's `wwwroot/index.html`, so that is where the stylesheet link goes.
                """,
                [.. _gettingStarted, "Blazor Hybrid"],
                HybridFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Bortable up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own. The server's `Program.cs` ")
                   .AppendLine("also hosts this MCP server - the `AddControllers`, `AddMcpServer`, `MapControllers` and `MapMcp` lines are not part ")
                   .AppendLine("of what Bortable needs.")
                   .AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BortableSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on installing and on this hosting model, so the caveats are the
        // library's rather than a paraphrase of them that can drift.
        var quoted = mode.GuideSections.Select(BortableSourceCatalog.GetGuideSection).Where(section => section is not null).ToArray();

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

        1. `dotnet add package Bit.Bortable` - one package, everything under the `Bit.Bortable` namespace. In a Blazor Web
           App with an interactive client, add it to the .Client project.
        2. Add `@using Bit.Bortable` to `_Imports.razor`.
        3. Register the services - REQUIRED: `builder.Services.AddBitBortableServices();`. It registers the scoped drag
           coordinator every list injects, and without it the first list that renders throws "Cannot provide a value for
           property 'Service' ... no registered service of type 'Bit.Bortable.BortableService'". In a Blazor Web App with
           WebAssembly or Auto, call it in BOTH `Program.cs` files - the server prerenders the same lists. Calling it
           twice is harmless.
        4. Link the stylesheet from the host page - it is REQUIRED, and nothing adds it for you:
           `<link rel="stylesheet" href="_content/Bit.Bortable/bit-bortable.css" />` (in a Blazor Web App's `App.razor`,
           `@Assets["_content/Bit.Bortable/bit-bortable.css"]`). Without it a drag still works, but the dragged item is not
           dimmed and its original does not hide while it is previewed in another list. Load your own overrides of its
           `--bit-bortable-*` properties after it. The script module needs no tag: each list imports it itself.
        5. Render the lists interactively. On a statically rendered page the items show and nothing can be dragged.
           Prerendering is fine: dragging starts once the page becomes interactive.
        6. Bind the items with `@bind-Items` to an `IList<TItem>` field (the parameter's type) holding a mutable list -
           a `List<T>`. The list is changed in place; an array is read-only and can only be cloned from. Items must be
           unique within a list (Blazor's `@key`): a list of strings must not hold the same string twice.
        7. Give a list that may be empty a `min-height` or some padding - an empty container with no height has no area
           to drop into.
        """;

    private const string ServerFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddBitBortableServices();

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        ### `Components/App.razor` (in `<head>`)

        ```razor
        <link rel="stylesheet" href="@Assets["_content/Bit.Bortable/bit-bortable.css"]" />
        ```

        ### `Components/_Imports.razor`

        ```razor
        @using Bit.Bortable
        ```

        ### `Components/Pages/Board.razor`

        ```razor
        @page "/board"
        @rendermode InteractiveServer

        <Bortable @bind-Items="_todo" TItem="string" Group="board">
            <ItemTemplate Context="item">@item</ItemTemplate>
        </Bortable>

        <Bortable @bind-Items="_done" TItem="string" Group="board">
            <ItemTemplate Context="item">@item</ItemTemplate>
        </Bortable>

        @code {
            private IList<string> _todo = new List<string> { "Write the spec", "Review the PR" };
            private IList<string> _done = new List<string> { "Set up CI" };
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

        builder.Services.AddBitBortableServices();

        await builder.Build().RunAsync();
        ```

        ### `wwwroot/index.html` (in `<head>`)

        ```html
        <link rel="stylesheet" href="_content/Bit.Bortable/bit-bortable.css" />
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Bortable
        ```

        ### `Pages/Todo.razor`

        ```razor
        @page "/todo"

        <Bortable @bind-Items="_items" TItem="string">
            <ItemTemplate Context="item">@item</ItemTemplate>
        </Bortable>

        @code {
            private IList<string> _items = new List<string> { "Item 1", "Item 2", "Item 3" };
        }
        ```
        """;

    private const string HybridFiles = """
        ## The files

        ### `MauiProgram.cs` (or the WPF / Windows Forms host)

        ```csharp
        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddBitBortableServices();
        ```

        ### `wwwroot/index.html` (in `<head>`)

        ```html
        <link rel="stylesheet" href="_content/Bit.Bortable/bit-bortable.css" />
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Bortable
        ```

        Put `<Bortable @bind-Items="_items" TItem="string"><ItemTemplate Context="item">@item</ItemTemplate></Bortable>` on
        any page the BlazorWebView renders.
        """;

    private const string Next = """
        The lists themselves are markup and a bound `IList<TItem>` - call `GetBortableGuideSection(heading: "Shared lists")`
        for lists that exchange items, `GetBortableApi(typeName: "Bortable")` for every parameter with its real default,
        `InspectBortableDrag()` to see what a drag does, or `GetBortableSourceFile()` for the index of the demo's working pages.
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
