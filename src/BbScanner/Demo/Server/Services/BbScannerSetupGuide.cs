using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BbScanner.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, the guide's own section on that
/// hosting model, and - where the demo is that render mode - the demo's real hosting files.
/// <para>
/// BbScanner needs less wiring than most libraries - no services, and a stylesheet that injects itself -
/// which is exactly why an agent gets it wrong: it registers services that do not exist, links the
/// stylesheet a second time, or puts the scanner on a page that is not rendered on WebAssembly, where
/// it reports NotSupported instead of opening the camera. What does differ per render mode is whether
/// the camera can be reached at all, and so where the component has to live; the guide's hosting
/// section, quoted verbatim, says so in the library's own words.
/// </para>
/// </summary>
public static partial class BbScannerSetupGuide
{
    public static readonly string[] RenderModes = ["wasm", "standalone-wasm", "auto", "server", "hybrid"];

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
            "wasm" => new Mode(
                "Blazor Web App - InteractiveWebAssembly",
                """
                The model the scanner is built for: the component, the frame loop and the decoder all run in the browser,
                and every camera frame crosses from JavaScript into managed memory exactly once, through a `[JSImport]`
                memory view. The component must live in the .Client project so its assembly is downloaded to the browser;
                the server prerenders the same page, where the scanner renders its markup and starts the camera once
                WebAssembly takes over. Reference `Bit.BbScanner` from the .Client project (the server gets it
                transitively). This is exactly how the demo is built, so the files below are its own.
                """,
                ["Blazor Web App", "Blazor WebAssembly"],
                null,
                IncludeDemo: true),

            "standalone-wasm" => new Mode(
                "standalone Blazor WebAssembly",
                """
                One project and no prerendering: the scanner runs in the browser from the first render and opens the
                camera as soon as it is on screen (unless `AutoStart` is false).
                """,
                ["Blazor WebAssembly"],
                StandaloneFiles,
                IncludeDemo: false),

            "auto" => new Mode(
                "Blazor Web App - InteractiveAuto",
                """
                The first visit renders on the server while the WebAssembly runtime downloads, and a scanner rendered on
                the server cannot reach the camera: it reports `BbScannerErrorKind.NotSupported`. Render the pages that
                hold a scanner with `@rendermode InteractiveWebAssembly` - they can sit in an otherwise InteractiveAuto
                app - and put them in the .Client project. The rest of the wiring is the WebAssembly setup below; the
                demo uses InteractiveWebAssembly throughout.
                """,
                ["Blazor Web App", "Blazor Server"],
                null,
                IncludeDemo: true),

            "server" => new Mode(
                "Blazor Web App - InteractiveServer",
                """
                Camera scanning is not available: the component reads frames through `[JSImport]` memory views, which
                only exist on WebAssembly, so under interactive server rendering it reports
                `BbScannerErrorKind.NotSupported` rather than pretending to scan. Two ways forward: render just the
                scanning page with `InteractiveWebAssembly` (a .Client project is all it takes), or decode on the server -
                `BbBarcodeDecoder` has no dependency on Blazor or JavaScript, so an uploaded image, once your own code has
                turned it into pixels, decodes in-process.
                """,
                ["Blazor Server", "Using the decoder without Blazor"],
                ServerFiles,
                IncludeDemo: false),

            "hybrid" => new Mode(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                The component's camera path is WebAssembly-only, so in a BlazorWebView it reports `NotSupported`. The
                decoder is plain .NET and runs in-process: feed `BbBarcodeDecoder` the frames of the platform's own camera
                API (or a picked photo) as pixels.
                """,
                ["Blazor Hybrid", "Using the decoder without Blazor"],
                HybridFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting BbScanner up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own.").AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BbScannerSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on the hosting model, so the caveats are the library's rather than a
        // paraphrase of them that can drift.
        builder.AppendLine("## From the guide").AppendLine();

        foreach (var heading in mode.GuideSections)
        {
            var section = BbScannerSourceCatalog.GetGuideSection(heading);
            if (section is null) continue;

            builder.AppendLine(Demote(section)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.BbScanner` - one package, everything under the `Bit.BbScanner` namespace. In a Blazor
           Web App with an interactive client, add it to the .Client project.
        2. Add `@using Bit.BbScanner` to `_Imports.razor`.
        3. There are NO services to register - do not look for an `AddBbScanner...` method, there is none.
        4. There is no stylesheet to add: `_content/Bit.BbScanner/bit-bbscanner.css` is injected by the package's Blazor
           initializer. Linking it yourself also works (the injection then stands down); never link it twice under
           another path.
        5. Render the scanner on WebAssembly. On a statically rendered page `<BbScanner>` is inert markup; during
           prerendering it is inert until WebAssembly takes over, and then starts by itself. Under Server or Hybrid
           rendering it reports `NotSupported`.
        6. Serve the app over HTTPS or from `localhost`: browsers only expose the camera to a secure context, and the
           scanner reports `NotSupported` with that reason when the page is not one.
        7. Narrow `Formats` to the symbologies the app needs, handle `OnScan`, and handle `OnError` - a denied
           permission arrives there as `BbScannerErrorKind.PermissionDenied`, a missing camera as `NoCamera`.
        """;

    private const string ServerFiles = """
        ## The files

        ### `Components/Pages/Scan.razor` in the .Client project - the scanning page on WebAssembly

        ```razor
        @page "/scan"
        @rendermode InteractiveWebAssembly

        <BbScanner Formats="BbBarcodeFormat.QrCode | BbBarcodeFormat.Ean13"
                   Region="BbScanRegion.CenteredSquare(0.7)"
                   OnScan="HandleScan" />

        @code {
            private void HandleScan(BbBarcodeResult result) => Console.WriteLine($"{result.Format}: {result.Text}");
        }
        ```

        ### Or decode on the server

        ```csharp
        // rgba: the image as packed RGBA pixels, produced by whichever image library the app already uses.
        var options = BbScannerOptions.ForStillImages();
        options.Formats = BbBarcodeFormat.QrCode;

        using var decoder = new BbBarcodeDecoder(options);
        var result = decoder.Decode(rgba, width, height, BbPixelFormat.Rgba32);
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

        // Nothing to register for BbScanner.

        await builder.Build().RunAsync();
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.BbScanner
        ```

        ### `Pages/Scan.razor`

        ```razor
        @page "/scan"

        <BbScanner Formats="BbBarcodeFormat.QrCode" OnScan="r => _last = r" OnError="e => _error = e" />

        <p>@(_last?.Text ?? _error?.Message ?? "Point the camera at a QR code.")</p>

        @code {
            private BbBarcodeResult? _last;
            private BbScannerException? _error;
        }
        ```

        There is no script tag or stylesheet link to add to `wwwroot/index.html`: the package's JavaScript is a module
        it imports itself, and its initializer injects the stylesheet.
        """;

    private const string HybridFiles = """
        ## The files

        ### Decoding a frame the app captured itself

        ```csharp
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.QrCode });

        // One decoder per scanning session: it owns the scratch state a frame needs and is not thread safe.
        var result = decoder.Decode(pixels, width, height, BbPixelFormat.Bgra32);
        ```
        """;

    private const string Next = """
        The scanning itself is markup and plain objects - call `GetBbScannerGuideSection(heading: "Using the component")`
        for the parameters, events and methods, `GetBbScannerFormats()` for the symbologies and the options that steer
        them, `InspectBbScannerSymbol()` to see a real `BbBarcodeResult`, or `GetBbScannerSourceFile()` for the index of
        the demo's working pages.
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
