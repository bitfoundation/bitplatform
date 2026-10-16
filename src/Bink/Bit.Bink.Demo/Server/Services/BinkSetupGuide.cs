using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bink.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, the guide's own section on that
/// hosting model, and - where the demo is that render mode - the demo's real hosting files.
/// <para>
/// Bink needs less wiring than most libraries - no services, and a stylesheet that injects itself -
/// which is exactly why an agent gets it wrong: it registers services that do not exist, adds a
/// stylesheet twice, or drops a canvas onto a statically rendered page where it never attaches. What
/// does differ per render mode is where the engine runs, so where the component has to live and
/// what Blazor Server's latency changes; the guide's hosting section, quoted verbatim, says so in
/// the library's own words.
/// </para>
/// </summary>
public static partial class BinkSetupGuide
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
                One project, and the engine runs on the server: pointer samples travel over the SignalR circuit once per
                animation frame and the canvas commands travel back. `WetInk` (on by default under interactive server
                rendering) draws a provisional trail in the browser while a batch is in flight, so the pen still feels
                attached. That is fine for a signature field or form annotation; sustained handwriting over a
                high-latency connection is what WebAssembly is for - render just the drawing pages with
                `InteractiveWebAssembly`.
                """,
                ["Blazor Server", "Blazor Web App"],
                ServerFiles,
                IncludeDemo: false),

            "wasm" => new Mode(
                "Blazor Web App - InteractiveWebAssembly",
                """
                The engine runs in the browser - the model the library is designed for, with a zero-copy render path
                (`BinkCanvas.UsesFastRenderPath`). The component must live in the .Client project so its assembly is
                downloaded to the browser; the server project prerenders the same page, where the canvas renders inert
                markup and attaches once WebAssembly starts. Reference `Bit.Bink` from the .Client project (the server
                gets it transitively). This is exactly how the demo is built, so the files below are its own; its
                `AddDemoServices` registers a download helper of the demo's, not anything Bink needs.
                """,
                ["Blazor Web App", "Blazor WebAssembly"],
                null,
                IncludeDemo: true),

            "auto" => new Mode(
                "Blazor Web App - InteractiveAuto",
                """
                The first visit renders on the server while the WebAssembly runtime downloads, later ones in the browser,
                so the same canvas runs in either place: the engine runs on the server (with wet ink) on the first visit
                and locally afterwards. Nothing about the wiring changes between the two - there are no services to
                register in either container - but the component must live in the .Client project. The demo below uses
                InteractiveWebAssembly; for InteractiveAuto add `AddInteractiveServerComponents()` and
                `AddInteractiveServerRenderMode()` to the server's `Program.cs` and use `@rendermode="InteractiveAuto"` in
                `App.razor` - nothing else changes.
                """,
                ["Blazor Web App", "Blazor WebAssembly", "Blazor Server"],
                null,
                IncludeDemo: true),

            "standalone-wasm" => new Mode(
                "standalone Blazor WebAssembly",
                """
                One project and no prerendering: the engine runs in the browser from the first render, over the
                zero-copy render path.
                """,
                ["Blazor WebAssembly"],
                StandaloneFiles,
                IncludeDemo: false),

            "hybrid" => new Mode(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                The engine runs in-process and the canvas in the embedded WebView, over the standard `IJSRuntime` path.
                The zero-copy render path is WebAssembly-only and is not needed here.
                """,
                ["Blazor Hybrid"],
                HybridFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Bink up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own.").AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BinkSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on the hosting model, so the caveats are the library's rather than a
        // paraphrase of them that can drift.
        builder.AppendLine("## From the guide").AppendLine();

        foreach (var heading in mode.GuideSections)
        {
            var section = BinkSourceCatalog.GetGuideSection(heading);
            if (section is null) continue;

            builder.AppendLine(Demote(section)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Bink` - one package, everything under the `Bit.Bink` namespace. In a Blazor Web App
           with an interactive client, add it to the .Client project.
        2. Add `@using Bit.Bink` to `_Imports.razor` (and `@using System.Numerics` where you build documents in code:
           points and sizes are `Vector2`).
        3. There are NO services to register - do not look for an `AddBink...` method, there is none.
        4. There is no stylesheet to add: `_content/Bit.Bink/bit-bink.css` is injected by the package's Blazor
           initializer. Linking it yourself also works (the injection then stands down); never link it twice under
           another path.
        5. Render the canvas interactively. On a statically rendered page `<BinkCanvas>` is inert markup; during
           prerendering it is inert until interactivity arrives, and then attaches by itself.
        6. Give the canvas a height (`Height="300px"` is the default) and an `AriaLabel`. Bind `DocumentChanged` when your
           own markup reads canvas state such as `CurrentDocument.IsEmpty` or `History.CanUndo` - drawing never
           re-renders anything on its own.
        7. Handle `Unavailable` to tell the user when the browser layer cannot start (a content security policy that
           blocks module scripts from your origin, static web assets not served).
        """;

    private const string ServerFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Nothing to register for Bink.

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        ### `Components/_Imports.razor`

        ```razor
        @using Bit.Bink
        ```

        ### `Components/Pages/Sign.razor`

        ```razor
        @page "/sign"
        @rendermode InteractiveServer

        <BinkCanvas @ref="_canvas" Height="180px" AriaLabel="Sign here" DocumentChanged="() => StateHasChanged()" />

        <button @onclick="() => _canvas!.Clear()" disabled="@IsEmpty">Clear</button>

        @code {
            private BinkCanvas? _canvas;

            private bool IsEmpty => _canvas?.CurrentDocument.IsEmpty ?? true;
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

        // Nothing to register for Bink.

        await builder.Build().RunAsync();
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Bink
        ```

        ### `Pages/Draw.razor`

        ```razor
        @page "/draw"

        <BinkCanvas Height="400px" Brush="_brush" />

        @code {
            private BinkBrush _brush = new BinkPenBrush();
        }
        ```

        There is no script tag or stylesheet link to add to `wwwroot/index.html`: the package's JavaScript is a module
        it imports itself, and its initializer injects the stylesheet.
        """;

    private const string HybridFiles = """
        ## The files

        ### `MauiProgram.cs` (or the WPF / Windows Forms host)

        ```csharp
        builder.Services.AddMauiBlazorWebView();

        // Nothing to register for Bink.
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Bink
        ```

        Put `<BinkCanvas Height="400px" />` on any page the BlazorWebView renders.
        """;

    private const string Next = """
        The drawing itself is markup and plain objects - call `GetBinkGuideSection(heading: "Getting started")` for the
        signature field, brushes, tools, saving and export, `GetBinkBrushes()` for the brush defaults, or
        `GetBinkSourceFile()` for the index of the demo's working pages.
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
