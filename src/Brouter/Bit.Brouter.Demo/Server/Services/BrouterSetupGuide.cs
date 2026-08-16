using System.Text;

namespace Bit.Brouter.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, plus the real files of the
/// matching project under Samples/.
/// <para>
/// Setting a router up is where a render mode's differences actually bite - which DI container has
/// to register the services, which assembly holds the catch-all host page, which assemblies the
/// host has to be told about - and getting one of them wrong produces a blank page rather than an
/// error. Handing over a known-good, compiling project instead of prose removes the guesswork.
/// </para>
/// </summary>
public static class BrouterSetupGuide
{
    public static readonly string[] RenderModes = ["server", "wasm", "auto", "standalone-wasm"];

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
            "server" => Compose(
                "Blazor Web App - InteractiveServer",
                """
                One project. The host registers the router services once, and a catch-all host page hands every
                URL to Brouter. `AddBitBrouterServices` goes in the single (server) container.
                """,
                "Sample/Server/"),

            "wasm" => Compose(
                "Blazor Web App - InteractiveWebAssembly",
                """
                Two projects, two DI containers: the server prerenders the very same components the browser then
                hydrates, so `AddBitBrouterServices` must be called in BOTH `Program.cs` files - typically through
                one shared extension method the two projects call (see `AddCoreServices` below). The catch-all host
                page lives in the client project, so the host has to be told about that assembly with
                `AddAdditionalAssemblies`.
                """,
                "Sample/Wasm/"),

            "auto" => Compose(
                "Blazor Web App - InteractiveAuto",
                """
                Same as InteractiveWebAssembly, plus the server-interactive path: the first visit runs on the server
                while the WebAssembly runtime downloads, so both render modes are registered and both containers
                register the router services.
                """,
                "Sample/Auto/"),

            "standalone-wasm" => StandaloneWasm(),

            _ => null
        };
    }

    private static string Compose(string title, string summary, string samplePrefix)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Brouter up - {title}").AppendLine();
        builder.AppendLine(summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();
        builder.AppendLine("## The files, from a working sample").AppendLine();
        builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own.").AppendLine();

        // The shared registration method lives in the project all the sample hosts reference, so it
        // sits outside the per-render-mode folder while being the piece most easily got wrong.
        var files = BrouterSourceCatalog.SourceFiles
            .Where(f => f.Path.StartsWith(samplePrefix, StringComparison.OrdinalIgnoreCase) ||
                        f.Path.StartsWith("Sample/Core/Extensions/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var content = BrouterSourceCatalog.GetSourceFile(file.Path);
            if (content is null) continue;

            builder.AppendLine($"### `{file.Path}`").AppendLine()
                   .AppendLine($"```{Language(file.Path)}")
                   .AppendLine(content.TrimEnd())
                   .AppendLine("```")
                   .AppendLine();
        }

        builder.AppendLine("""
            The routes themselves (`AppRouter.razor` and the pages it renders) live in a shared project both of the
            above reference - call `GetBrouterSourceFile(path: "Demo/Client/AppRouter.razor")` for a route table that
            exercises nearly every feature, or `GetBrouterGuideSection(heading: "Quick start")` for the smallest one.
            """);

        return builder.ToString();
    }

    private static string StandaloneWasm()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Setting Brouter up - standalone Blazor WebAssembly").AppendLine();
        builder.AppendLine("""
            A standalone WebAssembly app has no server-rendered host page, so there is no catch-all `@page` to
            declare: `<Brouter>` is rendered straight from the root component and owns the URL space from the start.
            One project, one DI container.
            """).AppendLine();
        builder.AppendLine(Checklist).AppendLine();
        builder.AppendLine("""
            ## The files

            ### `Program.cs`

            ```csharp
            using Microsoft.AspNetCore.Components.Web;
            using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddBitBrouterServices(o =>
            {
                o.ScrollBehavior = BrouterScrollMode.ToTop;
                o.FocusOnNavigateSelector = "h1";
            });

            await builder.Build().RunAsync();
            ```

            ### `App.razor`

            ```razor
            @* No host page and no built-in Router: Brouter is the root of the URL space. *@
            <Brouter NotFoundUrl="404" AppAssembly="@GetType().Assembly">
                <Routes>
                    <Broute Path="/" Component="@typeof(HomePage)" />
                    <Broute Path="/users/{id:int}" Component="@typeof(UserPage)" />

                    <Broute Path="404">
                        <Content><h1>404</h1><p>Nothing here.</p></Content>
                    </Broute>
                </Routes>
            </Brouter>
            ```

            ### `_Imports.razor`

            ```razor
            @using Bit.Brouter
            ```

            `wwwroot/index.html` needs nothing beyond the usual `<div id="app">` and `blazor.webassembly.js`:
            Brouter ships its JavaScript as a static web asset, so there is no script tag to add.
            """);

        return builder.ToString();
    }

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Brouter` (optionally `Bit.Brouter.Generators` with `PrivateAssets="all"` for the
           typed `BrouterRoutes` URL builders).
        2. Call `AddBitBrouterServices(...)` in EVERY DI container that renders your components - a Blazor Web App
           with an interactive client has two of them, and a missing registration surfaces as a failure to resolve
           `IBrouter` during prerendering.
        3. Add `@using Bit.Brouter` to `_Imports.razor` of every project that declares routes.
        4. Give Brouter the whole URL space: one catch-all host page (`@page "/"` + `@page "/{*path}"`) that renders
           your `<Brouter>` component - it is what makes deep links work, because every URL then matches a Razor
           component endpoint and Brouter resolves the real route (including its own 404) from there.
        5. Declare the routes, and/or point `AppAssembly`/`AdditionalAssemblies` at the assemblies whose `@page`
           components should be discovered.
        6. Do not also render Blazor's built-in `<Router>`; Brouter replaces it.
        """;

    private static string Language(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".razor" => "razor",
        ".cs" => "csharp",
        ".csproj" => "xml",
        ".css" => "css",
        ".js" => "javascript",
        _ => string.Empty
    };
}
