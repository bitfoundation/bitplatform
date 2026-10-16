using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one hosting model: the checklist, plus the real files of a working project
/// where this repository has one.
/// <para>
/// Setting Blex up is where a host's differences actually bite - which DI container registers the
/// stores, which lifetime they get, where <c>&lt;BlexProvider&gt;</c> goes, whether browser storage is
/// reachable - and getting one of them wrong shows up as state that silently resets, or as a missing
/// service during prerendering, rather than as a compiler error. Handing over a known-good project
/// instead of prose removes the guesswork.
/// </para>
/// </summary>
public static partial class BlexSetupGuide
{
    public static readonly string[] HostingModels = ["server", "wasm", "auto", "standalone-wasm", "maui-hybrid", "maui"];

    public static string? Get(string? hostingModel)
    {
        var mode = (hostingModel ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

        mode = mode switch
        {
            "interactiveserver" or "blazor-server" => "server",
            "webassembly" or "interactivewebassembly" or "blazor-wasm" => "wasm",
            "interactiveauto" => "auto",
            "standalone" or "standalone-webassembly" => "standalone-wasm",
            "hybrid" or "blazor-hybrid" or "maui-blazor" => "maui-hybrid",
            "maui-native" or "xaml" or "native" => "maui",
            _ => mode
        };

        return mode switch
        {
            "server" => Server(),
            "wasm" => FromFiles(
                "Blazor Web App - InteractiveWebAssembly (prerendered)",
                """
                Two projects, two DI containers: the server prerenders the very same components the browser then
                hydrates, so every Blex registration must happen in BOTH `Program.cs` files - through one shared
                extension method the two projects call (`AddDemoServices` below). `<BlexProvider>` wraps the router in
                the client's `Routes.razor`; it hands the prerendered state to the interactive render and then
                rehydrates persisted stores from browser storage once the WebAssembly runtime is up. This is exactly
                how the documentation site itself is wired.
                """,
                ["Demo/Client/Bit.Blex.Demo.Client.csproj", "Demo/Client/Program.cs", "Demo/Client/Extensions/IServiceCollectionExtensions.cs",
                 "Demo/Client/Routes.razor", "Demo/Client/Stores/CounterStore.cs", "Demo/Server/Components/App.razor"]),
            "auto" => Auto(),
            "standalone-wasm" => FromFiles(
                "standalone Blazor WebAssembly",
                """
                One project, one DI container, no prerendering. Register Blex in `Program.cs` and wrap the router in
                `<BlexProvider>` in `App.razor`. This is the minimal sample in the repository, complete.
                """,
                ["Sample/Bit.Blex.Samples.Wasm.csproj", "Sample/Program.cs", "Sample/App.razor", "Sample/_Imports.razor",
                 "Sample/Stores/CounterStore.cs", "Sample/Pages/Counter.razor"]),
            "maui-hybrid" => FromGuide(
                ".NET MAUI Blazor Hybrid",
                """
                A Blazor Hybrid app is a Blazor app inside a `BlazorWebView`: register Blex in `MauiProgram.cs` exactly
                as a Blazor app does (scoped lifetimes - the `BlazorWebView` creates the scope) and wrap the root
                component's router in `<BlexProvider>`. Persist to the WebView's `localStorage`, or add
                `Bit.Blex.Maui` and persist to OS-native `Preferences` instead.
                """,
                "Blazor Hybrid",
                HybridCode),
            "maui" => FromGuide(
                ".NET MAUI (native XAML)",
                """
                A native MAUI app has no DI scopes, so register through the `MauiAppBuilder` extensions of
                `Bit.Blex.Maui` - `UseBitBlex()`, `AddBitBlexStore<T>()`, `AddBitBlexPreferencesPersistence()` - which
                register everything as singletons and do at `MauiApp.Build()` what `<BlexProvider>` does in Blazor.
                Stores implement `INotifyPropertyChanged`, so XAML binds to generated state directly.
                """,
                "Native (XAML)"),
            _ => null
        };
    }

    private static string FromFiles(string title, string summary, string[] paths)
    {
        var builder = Start(title, summary);

        builder.AppendLine("## The files, from a working project").AppendLine();
        builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own. Inside this repository the ")
               .AppendLine("projects reference the generator as an analyzer project reference - from NuGet it is the `Bit.Blex.Generators` package.")
               .AppendLine();

        foreach (var path in paths)
        {
            var content = BlexSourceCatalog.GetSourceFile(path);
            if (content is null) continue;

            AppendFile(builder, path, content);
        }

        builder.AppendLine("""
            For a store that exercises every attribute, call `GetBlexSourceFile(path: "Demo/Client/Stores/CartStore.cs")`;
            for the attributes themselves, `GetBlexGuideSection(heading: "The whole store")`.
            """);

        return builder.ToString();
    }

    private static string FromGuide(string title, string summary, string heading, string? code = null)
    {
        var builder = Start(title, summary);

        if (code is not null) builder.AppendLine(code).AppendLine();

        var section = BlexSourceCatalog.GetGuideSection(heading);
        if (section is not null) builder.AppendLine(section).AppendLine();

        return builder.ToString();
    }

    private const string HybridCode = """
        ## The files

        ### `MauiProgram.cs`

        ```csharp
        using Bit.Blex;
        using Bit.Blex.Blazor;

        public static class MauiProgram
        {
            public static MauiApp CreateMauiApp()
            {
                var builder = MauiApp.CreateBuilder();
                builder.UseMauiApp<App>();
                builder.Services.AddMauiBlazorWebView();

                // Scoped, exactly as in a Blazor app: the BlazorWebView creates the scope.
                builder.Services.AddBitBlexServices(options => options.DevToolsName = "My App");
                builder.Services.AddBitBlexStore<CounterStore>();

                // Either the WebView's localStorage...
                builder.Services.AddBitBlexLocalStoragePersistence();
                // ...or, with Bit.Blex.Maui, OS-native Preferences:
                // builder.Services.AddBitBlexPreferencesPersistence();

                return builder.Build();
            }
        }
        ```

        ### `Components/Routes.razor`

        ```razor
        @* No browser extension exists inside a WebView, so the DevTools bridge is skipped outright. *@
        <BlexProvider EnableDevTools="false">
            <Router AppAssembly="typeof(MauiProgram).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
                </Found>
            </Router>
        </BlexProvider>
        ```
        """;

    private static string Server()
    {
        var builder = Start(
            "Blazor Web App - InteractiveServer",
            """
            One project, one DI container. Every circuit is a DI scope, so the default scoped lifetimes give each user
            their own manager and stores. During prerendering browser storage is unreachable; `<BlexProvider>` retries
            the rehydration on the first interactive render, so nothing extra is needed.
            """);

        builder.AppendLine("""
            ## The files

            ### `Program.cs`

            ```csharp
            using Bit.Blex;
            using Bit.Blex.Blazor;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddBitBlexServices(options => options.DevToolsName = "My App");
            builder.Services.AddBitBlexStore<CounterStore>();
            builder.Services.AddBitBlexLocalStoragePersistence();   // optional
            builder.Services.AddBitBlexHistory();                    // optional

            var app = builder.Build();

            app.UseAntiforgery();
            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
            ```

            ### `Components/Routes.razor`

            ```razor
            <BlexProvider EnableDevTools="false">
                <Router AppAssembly="typeof(Program).Assembly">
                    <Found Context="routeData">
                        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
                    </Found>
                </Router>
            </BlexProvider>
            ```

            ### `Components/App.razor` (body)

            ```razor
            <Routes @rendermode="InteractiveServer" />
            ```

            `<BlexProvider>` must be inside the interactive boundary - a statically rendered provider never starts
            persistence or the DevTools bridge. Turn the bridge off (`EnableDevTools="false"`) unless the Redux
            DevTools time-travel is wanted: on Blazor Server it streams the whole state tree over the circuit.
            """);

        return builder.ToString();
    }

    private static string Auto()
    {
        var builder = Start(
            "Blazor Web App - InteractiveAuto",
            """
            Same as InteractiveWebAssembly, plus the server-interactive path: the first visit runs on the server while
            the WebAssembly runtime downloads. Both containers render your components, so both register Blex - through
            one shared extension method in the client project, called from both `Program.cs` files.
            """);

        builder.AppendLine("""
            ## The difference from the `wasm` setup

            Take the files `GetBlexSetupGuide(hostingModel: "wasm")` returns and change two things:

            1. In the server's `Program.cs`, register both render modes:

               ```csharp
               builder.Services.AddRazorComponents()
                   .AddInteractiveServerComponents()
                   .AddInteractiveWebAssemblyComponents();

               app.MapRazorComponents<App>()
                   .AddInteractiveServerRenderMode()
                   .AddInteractiveWebAssemblyRenderMode()
                   .AddAdditionalAssemblies(typeof(MyApp.Client._Imports).Assembly);
               ```

            2. In `App.razor`, render `<Routes @rendermode="InteractiveAuto" />`.

            A store's state lives in whichever runtime is rendering. When a later visit switches from the server to
            WebAssembly the store starts again from its persisted copy - mark the stores that must survive the switch
            `[BlexStore(Persist = true)]` and register `AddBitBlexLocalStoragePersistence()`.
            """);

        return builder.ToString();
    }

    private static StringBuilder Start(string title, string summary)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Blex up - {title}").AppendLine();
        builder.AppendLine(summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        return builder;
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

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Blex` and `dotnet add package Bit.Blex.Generators` - the generator is what turns a
           `[BlexStore]` class into a store; without it a store compiles to nothing and every generated member is missing.
           Add `Bit.Blex.Blazor` for Blazor, `Bit.Blex.Maui` for MAUI.
        2. Declare each store as a `partial class` marked `[BlexStore]` - state in `[BlexState]` fields, actions in
           `[BlexAction] OnXxx` methods. Check it with `InspectBlexStore` before building on it.
        3. Call `AddBitBlexServices(...)` and `AddBitBlexStore<T>()` for every store in EVERY DI container that renders
           your components - a Blazor Web App with an interactive client has two of them.
        4. Wrap the router in `<BlexProvider>` once, inside the interactive render boundary (Blazor), or call
           `UseBitBlex()` (native MAUI). It registers the stores with the manager, rehydrates persisted state, starts
           undo/redo recording and connects the Redux DevTools bridge.
        5. In components, inherit `BlexComponentBase` and `Subscribe(store)` (or subscribe to a selector) in
           `OnInitialized`, so the component re-renders when the state it reads changes.
        """;

    /// <summary>The fence for a file's code block - longer than any run of backticks inside the file itself.</summary>
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
        ".html" => "html",
        _ => string.Empty
    };

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
