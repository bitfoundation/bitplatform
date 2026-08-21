using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor hosting model: the checklist, plus the real files of the
/// matching project in this repository.
/// <para>
/// Butil's setup is three steps, and every one of them lands somewhere different per hosting
/// model: which file is the host page that has to carry the script tag, how many DI containers
/// have to register the services, and whether there is a prerender pass at all. Getting one wrong
/// does not fail the build - it produces an app where every browser call silently returns a
/// default, which is the hardest failure to diagnose from the outside. Handing over a known-good,
/// compiling project instead of prose removes the guesswork.
/// </para>
/// </summary>
public static partial class ButilSetupGuide
{
    public static readonly string[] HostingModels = ["wasm", "web-app", "server", "hybrid"];

    public static string? Get(string? hostingModel)
    {
        var mode = (hostingModel ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

        mode = mode switch
        {
            "webassembly" or "blazor-wasm" or "standalone-wasm" or "standalone" => "wasm",
            "blazor-web-app" or "webapp" or "auto" or "interactiveauto" or "interactivewebassembly" => "web-app",
            "blazor-server" or "interactiveserver" => "server",
            "maui" or "blazor-hybrid" or "webview" or "wpf" or "winforms" => "hybrid",
            _ => mode
        };

        return mode switch
        {
            "wasm" => Compose(
                "standalone Blazor WebAssembly",
                """
                One project, one DI container, and `wwwroot/index.html` is the host page that carries both script
                tags. Nothing prerenders, so a browser call is safe from the first render onwards - though
                `OnAfterRenderAsync` is still the habit to keep if the code might ever move into a prerendered app.
                """,
                ["Sample/Bit.Butil.Samples.Web/", "Sample/Bit.Butil.Samples.Core/Extentions/"]),

            "web-app" => Compose(
                "Blazor Web App (InteractiveWebAssembly / InteractiveAuto, with prerendering)",
                """
                Two projects, two DI containers, and this is where Butil setups actually go wrong: the server
                prerenders the very same components the browser then hydrates, so `AddBitButilServices()` must be
                called in BOTH `Program.cs` files - typically through one shared extension method the two projects
                call. Missing it in the host fails the prerender pass with an unresolvable service; missing it in the
                client fails after hydration.

                The host page is `Components/App.razor`, and the Butil script goes in it before `blazor.web.js`.
                Because the page is prerendered, every browser call has to happen in `OnAfterRenderAsync` or in an
                event handler - during the prerender pass there is no JS runtime and reads return safe defaults.

                The host's own `Program.cs` needs three lines of it:

                ```csharp
                builder.Services.AddRazorComponents()
                    .AddInteractiveWebAssemblyComponents();

                // The prerender pass instantiates the client's components in THIS container, so it has to
                // register the very same services the WebAssembly container does.
                builder.Services.AddDemoServices();
                ```

                The files below are this documentation site itself, which is exactly that shape. Its full host
                `Program.cs` is `GetButilSourceFile(path: "Demo/Server/Program.cs")` - it carries endpoints of its
                own that have nothing to do with Butil, which is why only the part that matters is quoted above.
                """,
                ["Demo/Server/Components/", "Demo/Client/Program.cs", "Demo/Client/Extensions/"]),

            "server" => Server(),

            "hybrid" => Compose(
                "Blazor Hybrid (MAUI, WPF, WinForms)",
                """
                One project, one DI container, and the host page is the `wwwroot/index.html` inside the app - the
                BlazorWebView loads it, so the script tag goes there exactly as it would in a standalone WebAssembly
                app. The WebView is a real browser engine, so what works depends on which engine the platform ships
                (WebView2 on Windows, WKWebView on Apple, Chromium on Android) rather than on .NET.
                """,
                ["Sample/Bit.Butil.Samples.Maui/", "Sample/Bit.Butil.Samples.Core/Extentions/"]),

            _ => null
        };
    }

    private static string Compose(string title, string summary, string[] pathPrefixes)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Butil up - {title}").AppendLine();
        builder.AppendLine(summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();
        builder.AppendLine("## The files, from a working project").AppendLine();
        builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own.").AppendLine();

        // The .csproj goes last: it is the least interesting of the files and the longest of them.
        var files = ButilSourceCatalog.SourceFiles
            .Where(f => pathPrefixes.Any(prefix => f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Where(f => IsSetupFile(f.Path))
            .OrderBy(f => f.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var content = ButilSourceCatalog.GetSourceFile(file.Path);
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
            Then inject what you need. `GetButilApiDetails` with no type name lists every service, `PlanButilFeature`
            says what one of them - or the whole set a feature needs - has to have arranged before it will work, and
            `GetButilSourceFile(path: "Demo/Client/Pages/StoragePage.razor")` is a complete, working page using one.
            """);

        return builder.ToString();
    }

    /// <summary>
    /// The files that show how an app is wired: the host page, the entry point, the registration
    /// and the project file. A sample's own pages and layout are what it demonstrates, not how it
    /// is set up, and they would bury the four files that matter.
    /// </summary>
    private static bool IsSetupFile(string path)
    {
        var name = System.IO.Path.GetFileName(path);

        return name.Equals("Program.cs", StringComparison.OrdinalIgnoreCase)
            || name.Equals("MauiProgram.cs", StringComparison.OrdinalIgnoreCase)
            || name.Equals("index.html", StringComparison.OrdinalIgnoreCase)
            || name.Equals("App.razor", StringComparison.OrdinalIgnoreCase)
            || name.Equals("IServiceCollectionExtensions.cs", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static string Server()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Setting Butil up - Blazor Server (InteractiveServer)").AppendLine();
        builder.AppendLine("""
            One project, one DI container. Every browser call is a round trip over the circuit, so the wrappers behave
            exactly as they do under WebAssembly but each one costs a network hop - batch what you can, and do not put
            a Butil read inside a tight render loop. `BitButil.UseFastInvoke()` is a no-op here: there is no in-process
            JavaScript to call synchronously.

            The host page is `Components/App.razor` in a Blazor Web App, or `Pages/_Host.cshtml` in a classic
            (pre-.NET 8) Blazor Server app. If the app prerenders - the default - the same rule as everywhere else
            applies: touch the browser from `OnAfterRenderAsync`, not from `OnInitializedAsync`.
            """).AppendLine();
        builder.AppendLine(Checklist).AppendLine();
        builder.AppendLine("""
            ## The files

            ### `Program.cs`

            ```csharp
            using Bit.Butil;
            // Where App.razor lives, so MapRazorComponents<App>() below resolves.
            using MyApp.Components;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // One container, so one registration - unlike a Web App with an interactive client.
            builder.Services.AddBitButilServices();

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
            ```

            ### `Components/App.razor`

            ```razor
            <!DOCTYPE html>
            <html lang="en">

            <head>
                <base href="/" />
                <HeadOutlet @rendermode="InteractiveServer" />
            </head>

            <body>
                <Routes @rendermode="InteractiveServer" />

                @* Before the Blazor script: the app boots as soon as that second script runs, so
                   window.BitButil has to exist by then. *@
                <script src="_content/Bit.Butil/bit-butil.js"></script>
                <script src="_framework/blazor.web.js"></script>
            </body>

            </html>
            ```

            ### `_Imports.razor`

            Nothing is required - Butil services are injected by their full name (`@inject Bit.Butil.Clipboard clipboard`)
            so no using directive is needed. Adding `@using Bit.Butil` lets you write `@inject Clipboard clipboard`
            instead, and brings the enums and option types into scope.
            """);

        return builder.ToString();
    }

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Butil`.
        2. Add `<script src="_content/Bit.Butil/bit-butil.js"></script>` to the host page, BEFORE the Blazor script.
           The app boots as soon as the Blazor script runs, so `window.BitButil` has to exist by then. It is a static
           web asset of the package - there is nothing to copy into your own wwwroot. Two optional csproj switches
           decide how much of that script a published app ships: by default a trimmed publish (standalone
           WebAssembly) rebuilds `bit-butil.js` from only the modules the trimmed `Bit.Butil.dll` still calls
           (`<BitButilTrimScripts>false</BitButilTrimScripts>` opts out); `<BitButilLazyScripts>true</BitButilLazyScripts>`
           drops the script tag altogether and has each API `import()` its own module on first use, in every
           hosting model - set it in every project that uses Butil.
        3. Call `AddBitButilServices()` in EVERY DI container that renders your components. A Blazor Web App with an
           interactive WebAssembly client has two of them (the host prerenders, the browser hydrates), and a missing
           registration in either one surfaces at runtime rather than at compile time.
        4. Inject a wrapper by its own name - `@inject Bit.Butil.Clipboard clipboard` - and call it.
        5. Call the browser from `OnAfterRenderAsync` or from an event handler, never from `OnInitializedAsync`.
           Under prerendering there is no JS runtime: reads return safe defaults and void calls are no-ops, so the
           code does not crash - it silently does nothing, which is worse.
        6. Dispose what you open. Every subscription returns a `ButilSubscription`, and handles
           (`MediaStreamHandle`, `MediaRecordingHandle`, the File System and WakeLock handles) hold real hardware
           until they are disposed.
        """;

    /// <summary>
    /// The fence for a file's code block. It has to outrun the longest run of backticks in the file
    /// itself - a page that shows Markdown would otherwise end the block in the middle of its own
    /// content, and the rest of the file would render as prose.
    /// </summary>
    private static string Fence(string content)
    {
        var longest = BacktickRunRegex().Matches(content).Select(match => match.Length).DefaultIfEmpty(0).Max();

        return new string('`', Math.Max(3, longest + 1));
    }

    private static string Language(string path) => System.IO.Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".razor" => "razor",
        ".cs" => "csharp",
        ".csproj" => "xml",
        ".html" => "html",
        ".css" => "css",
        ".js" => "javascript",
        _ => string.Empty
    };

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
