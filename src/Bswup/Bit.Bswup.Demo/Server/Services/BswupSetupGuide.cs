using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bswup.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one hosting model: the checklist, plus the real files of the matching
/// project under Samples/.
/// <para>
/// Bswup is set up across four files that have to agree with each other - the host document, the
/// service-worker file, the project file and the handler - and the differences between hosting
/// models land in exactly those files: a standalone WebAssembly app has no server-rendered host,
/// so its splash markup must live in index.html, while a Blazor Web App can render
/// &lt;BswupProgress&gt; into the first byte of HTML and has a second wwwroot whose assets the
/// client's manifest never lists. Getting one of those wrong produces an app that boots fine in
/// development and fails offline in production, so a known-good, compiling project is a better
/// answer than prose.
/// </para>
/// </summary>
public static partial class BswupSetupGuide
{
    public static readonly string[] HostingModels = ["standalone-wasm", "blazor-web-app"];

    // Composing a guide reads and splices in the whole matching sample project, and the answer is
    // the same every time - so each one is built once and handed out from there.
    private static readonly Lazy<string> _standaloneWasmGuide = new(ComposeStandaloneWasm);
    private static readonly Lazy<string> _blazorWebAppGuide = new(ComposeBlazorWebApp);

    public static string? Get(string? hostingModel)
    {
        // Every run of whitespace collapses to the single hyphen the names are written with, so a
        // tab or a double space between the words still resolves ("blazor  web app").
        var model = WhitespaceRegex().Replace((hostingModel ?? string.Empty).Trim().ToLowerInvariant(), "-");

        model = model switch
        {
            "standalone" or "wasm" or "webassembly" or "standalone-webassembly" or "blazor-wasm" => "standalone-wasm",
            "web-app" or "blazorwebapp" or "blazor-web-app-wasm" or "interactivewebassembly" or "hosted" or "hosted-wasm" => "blazor-web-app",
            _ => model
        };

        return model switch
        {
            "standalone-wasm" => _standaloneWasmGuide.Value,
            "blazor-web-app" => _blazorWebAppGuide.Value,
            _ => null
        };
    }

    private static string ComposeStandaloneWasm()
    {
        return Compose(
            "standalone Blazor WebAssembly (wwwroot/index.html is the host document)",
            """
            One project. `index.html` is a static file, so it is served before anything runs and is the right
            place for the splash markup - the `BswupProgress` component cannot help here, because on a first
            install Blazor only starts once the download finishes, which is far too late to paint a splash.
            Write the handler and the markup into `index.html` (the sample below is a complete, working one).
            """,
            "Sample/BasicSample/");
    }

    private static string ComposeBlazorWebApp()
    {
        return Compose(
            "Blazor Web App with an InteractiveWebAssembly client (Components/App.razor is the host document)",
            """
            Two projects. `App.razor` on the SERVER is the host document and is statically rendered, so
            `<BswupProgress />` lands in the first byte of HTML and no hand-written splash is needed. Two things
            are specific to this model:

            - the client's `service-worker-assets.js` only lists the CLIENT project's assets, so anything the
              host owns - the app shell (`/`), `_framework/blazor.web.js`, the fingerprinted
              `resource-collection.<hash>.js` - has to be added to `self.externalAssets` by hand, and
              `self.defaultUrl` has to point at the shell entry (`'/'`), since there is no `index.html`;
            - the host prerenders that shell. Cache the NON-prerendered copy by setting `self.noPrerenderQuery`
              and reading the same query back in `App.razor` to switch prerendering off for that one request -
              otherwise every offline deep link flashes the prerendered home page before the router corrects it.
            """,
            "Sample/FullSample/");
    }

    private static string Compose(string title, string summary, string samplePrefix)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Bswup up - {title}").AppendLine();
        builder.AppendLine(summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();
        builder.AppendLine("## The files, from a working sample").AppendLine();
        builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own.").AppendLine();

        // The host document, the worker files and the project files are the ones setup lives in;
        // the sample's pages and layouts are the app, and would only pad the answer.
        var files = BswupSourceCatalog.SourceFiles
            .Where(file => file.Path.StartsWith(samplePrefix, StringComparison.OrdinalIgnoreCase) && IsSetupFile(file.Path))
            .OrderBy(file => SetupOrder(file.Path))
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var content = BswupSourceCatalog.GetSourceFile(file.Path);
            if (content is null) continue;

            content = content.TrimEnd();
            var fence = Fence(content);

            builder.AppendLine($"### `{file.Path}`").AppendLine()
                   .AppendLine($"{fence}{Language(file.Path)}")
                   .AppendLine(content)
                   .AppendLine(fence)
                   .AppendLine();
        }

        builder.AppendLine("""
            Next: `GetBswupServiceWorkerSettings` for everything the service-worker file can configure,
            `InspectBswupServiceWorker` to check the file you end up with, and `GetBswupProgressUI` for the
            built-in splash component's parameters.
            """);

        return builder.ToString();
    }

    private static bool IsSetupFile(string path)
    {
        var name = Path.GetFileName(path);

        return name.StartsWith("service-worker", StringComparison.OrdinalIgnoreCase)
            || name.Equals("index.html", StringComparison.OrdinalIgnoreCase)
            || name.Equals("App.razor", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Program.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Host document first, then the worker files, then the project wiring - the order they are written in.</summary>
    private static int SetupOrder(string path)
    {
        var name = Path.GetFileName(path);

        // index.html before App.razor: in a standalone app both exist and index.html is the host
        // document, while App.razor is only the app's root component.
        if (name.Equals("index.html", StringComparison.OrdinalIgnoreCase)) return 0;
        if (name.Equals("App.razor", StringComparison.OrdinalIgnoreCase)) return 1;
        if (name.StartsWith("service-worker", StringComparison.OrdinalIgnoreCase)) return 2;

        return path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ? 3 : 4;
    }

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Bswup` in the project that owns `wwwroot` (the WebAssembly project).
        2. Add `autostart="false"` to the Blazor entry script in the host document. Bswup starts Blazor itself -
           on a first install, only once the assets are cached.
        3. Reference `_content/Bit.Bswup/bit-bswup.js` AFTER that script, with the attributes you need
           (`GetBswupScriptOptions` lists them). No attribute is required.
        4. Create `wwwroot/service-worker.js` and `wwwroot/service-worker.published.js`, and register the
           published one in the .csproj:
           `<ServiceWorker Include="wwwroot\service-worker.js" PublishedContent="wwwroot\service-worker.published.js" />`.
           The published file is what deployed builds actually ship - a setting added to only one of the two is
           the single most common Bswup bug, and it only shows up in production.
        5. Set `<ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>` and
           `<StaticWebAssetFingerprintingEnabled>false</StaticWebAssetFingerprintingEnabled>` in that .csproj:
           fingerprinted file names would not match the URLs recorded in the manifest, leaving the worker unable
           to precache them.
        6. Give the page a handler - either the built-in one (`<BswupProgress />` plus
           `_content/Bit.Bswup/bit-bswup.progress.js` and its stylesheet) or your own function named by the
           `handler` attribute. Without one, a first install still completes, but updates stay staged silently.
        7. Serve `service-worker.js` and `_content/Bit.Bswup/bit-bswup.sw.js` with `Cache-Control: no-cache`, and
           the fingerprinted assets with a long max-age. A cached service-worker script is how clients get stuck
           on an old version.
        """;

    /// <summary>
    /// The fence for a file's code block. It has to outrun the longest run of backticks in the file
    /// itself - a file that shows Markdown would otherwise end the block in the middle of its own
    /// content, and the rest of it would render as prose.
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
        ".ts" => "typescript",
        ".html" => "html",
        ".json" => "json",
        _ => string.Empty
    };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
