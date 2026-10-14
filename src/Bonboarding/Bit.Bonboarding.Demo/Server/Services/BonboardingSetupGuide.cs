using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bonboarding.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, plus the real files of this demo
/// where the demo is that render mode.
/// <para>
/// Four things have to be in place before the first tour shows, and each one fails quietly when it
/// is missing: the services in every DI container that renders a tour (a missing one surfaces during
/// prerendering, not at compile time), the stylesheet (a tour without it renders unstyled at the
/// bottom of the page), one <c>BonboardingRoot</c> host (without it a started tour renders nothing at
/// all) and the namespace import. Handing over a known-good project instead of prose removes the
/// guesswork about which project each of them belongs in.
/// </para>
/// </summary>
public static partial class BonboardingSetupGuide
{
    public static readonly string[] RenderModes = ["server", "wasm", "auto", "standalone-wasm"];

    /// <summary>The demo files that make up its wiring - the demo is a Blazor Web App using InteractiveAuto.</summary>
    private static readonly string[] _hostingFiles =
    [
        "Demo/Client/Extensions/IServiceCollectionExtensions.cs",
        "Demo/Server/Program.cs",
        "Demo/Client/Program.cs",
        "Demo/Server/Components/App.razor",
        "Demo/Client/Layout/MainLayout.razor",
        "Demo/Client/_Imports.razor",
    ];

    public static string? Get(string? renderMode)
    {
        var mode = (renderMode ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

        mode = mode switch
        {
            "interactiveserver" or "blazor-server" => "server",
            "webassembly" or "interactivewebassembly" or "blazor-wasm" => "wasm",
            "interactiveauto" => "auto",
            "standalone" or "standalone-webassembly" or "standalone-wasm-app" => "standalone-wasm",
            _ => mode
        };

        return mode switch
        {
            "server" => Server(),

            "wasm" => FromDemo(
                "Blazor Web App - InteractiveWebAssembly",
                """
                Two projects, two DI containers: the server prerenders the very same components the browser then
                hydrates, so `AddBitBonboardingServices` must be called in BOTH `Program.cs` files - the demo does it
                through one shared extension method the two projects call (`AddDemoServices` below). The demo itself
                uses InteractiveAuto; for InteractiveWebAssembly drop `AddInteractiveServerComponents` /
                `AddInteractiveServerRenderMode` from the server's `Program.cs` and use `@rendermode="InteractiveWebAssembly"`
                in `App.razor` - nothing else about the wiring changes.
                """),

            "auto" => FromDemo(
                "Blazor Web App - InteractiveAuto",
                """
                Two projects, two DI containers, and a component may run in either: the first visit renders on the
                server while the WebAssembly runtime downloads, later ones in the browser. So both containers register
                the services - with the SAME options, since a tour must look and persist the same wherever it runs.
                This is exactly how the demo is built, so the files below are its own.
                """),

            "standalone-wasm" => StandaloneWasm(),

            _ => null
        };
    }

    private static string FromDemo(string title, string summary)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Bonboarding up - {title}").AppendLine();
        builder.AppendLine(summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();
        builder.AppendLine("## The files, from this demo").AppendLine();
        builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own.").AppendLine();

        foreach (var path in _hostingFiles)
        {
            var content = BonboardingSourceCatalog.GetSourceFile(path);
            if (content is null) continue;

            AppendFile(builder, path, content);
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    private static string Server()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Setting Bonboarding up - Blazor Web App - InteractiveServer").AppendLine();
        builder.AppendLine("""
            One project, one DI container per circuit: `AddBitBonboardingServices` is called once, in the server's
            `Program.cs`. Every Bonboarding service is scoped, so each circuit - each user - gets its own tours and its
            own progress.
            """).AppendLine();
        builder.AppendLine(Checklist).AppendLine();
        builder.AppendLine("""
            ## The files

            ### `Program.cs`

            ```csharp
            using Bit.Bonboarding;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddBitBonboardingServices(options =>
            {
                options.Labels.Skip = "Not now";
            });

            var app = builder.Build();

            app.UseAntiforgery();
            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
            ```

            ### `Components/App.razor` (the `<head>`)

            ```razor
            <link rel="stylesheet" href="@Assets["_content/Bit.Bonboarding/bit-bonboarding.css"]" />
            <link rel="stylesheet" href="@Assets["app.css"]" />
            ```

            ### `Components/Layout/MainLayout.razor`

            ```razor
            @inherits LayoutComponentBase

            <main>@Body</main>

            @* One host renders every tour the application starts, wherever it is started from. *@
            <BonboardingRoot />
            ```

            ### `Components/_Imports.razor`

            ```razor
            @using Bit.Bonboarding
            ```

            Blazor Server reconnects with a new circuit, and so a new scope: the library treats the interop failures of
            a dropped circuit as expected, and progress is in the browser's storage, so a reconnected user resumes
            where they were.
            """).AppendLine();
        builder.AppendLine(Next);

        return builder.ToString();
    }

    private static string StandaloneWasm()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Setting Bonboarding up - standalone Blazor WebAssembly").AppendLine();
        builder.AppendLine("""
            One project, one DI container for the lifetime of the app, and no prerendering - so tours can start as
            early as the first render.
            """).AppendLine();
        builder.AppendLine(Checklist).AppendLine();
        builder.AppendLine("""
            ## The files

            ### `Program.cs`

            ```csharp
            using Bit.Bonboarding;
            using Microsoft.AspNetCore.Components.Web;
            using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddBitBonboardingServices();

            await builder.Build().RunAsync();
            ```

            ### `wwwroot/index.html` (the `<head>`)

            ```html
            <link rel="stylesheet" href="_content/Bit.Bonboarding/bit-bonboarding.css" />
            <link rel="stylesheet" href="css/app.css" />
            ```

            ### `Layout/MainLayout.razor`

            ```razor
            @inherits LayoutComponentBase

            <main>@Body</main>

            <BonboardingRoot />
            ```

            ### `_Imports.razor`

            ```razor
            @using Bit.Bonboarding
            ```

            There is no script tag to add: the library's JavaScript module is a static web asset it imports itself.
            """).AppendLine();
        builder.AppendLine(Next);

        return builder.ToString();
    }

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Bonboarding`.
        2. Call `AddBitBonboardingServices(...)` in EVERY DI container that renders a tour - a Blazor Web App with an
           interactive client has two of them, and a missing registration surfaces as a failure to resolve
           `IBonboardingService` during prerendering. Give both the same options.
        3. Reference `_content/Bit.Bonboarding/bit-bonboarding.css` from the host page, before your own stylesheet so
           your overrides of its `--bit-bonboarding-*` variables win.
        4. Render exactly one `<BonboardingRoot />`, normally in the main layout. It hosts every tour the app starts;
           without it a started tour is running but nothing is drawn.
        5. Add `@using Bit.Bonboarding` to `_Imports.razor` of every project that declares tours.
        6. Start tours from `OnAfterRenderAsync(firstRender: true)`, a user action, or `<BonboardingTour AutoStart=...>` -
           never from `OnInitializedAsync`, which also runs during prerendering, in a scope that is thrown away.
        """;

    private const string Next = """
        The tours themselves are ordinary markup or data - call `GetBonboardingGuideSection(heading: "Getting started")`
        for the smallest complete one, or `GetBonboardingSourceFile(path: "Demo/Client/Pages/Home.razor")` for the
        demo's welcome tour.
        """;

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
