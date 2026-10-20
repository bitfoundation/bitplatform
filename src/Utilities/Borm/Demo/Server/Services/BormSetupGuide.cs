using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Borm.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor hosting model: the checklist, the guide's own sections on
/// installing the package and on hosting models, and - where the demo is that hosting model - the
/// demo's real hosting files.
/// <para>
/// Borm needs little wiring and has no JavaScript at all, but each of the few things it does need
/// fails in its own way. <c>AddBorm()</c> registers the field renderer registry every field resolves,
/// and a container without it throws the moment a form renders - in a Blazor Web App that is either
/// container, since every page is prerendered on the server before it runs in the browser. The
/// stylesheet is not injected for you, and a page without it renders a working but unstyled form. And a
/// statically rendered page never becomes interactive, so its form never answers a keystroke. What
/// differs per hosting model is where those lines go - which Program.cs, which host page.
/// </para>
/// </summary>
public static partial class BormSetupGuide
{
    public static readonly string[] HostingModels = ["web-app", "webassembly", "server", "hybrid"];

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

    /// <summary>The guide's own words, quoted under every hosting model: how to install, and what differs per model.</summary>
    private static readonly string[] _guideSections = ["Install", "Hosting models"];

    private sealed record Model(string Title, string Summary, string? Files, bool IncludeDemo);

    /// <summary>The wiring for one hosting model as Markdown, or null when the model is not one of <see cref="HostingModels"/>.</summary>
    public static string? Get(string? hostingModel) => Describe(hostingModel) is { } model ? Compose(model) : null;

    /// <summary>What sets one hosting model apart, in a paragraph - the line the search index ranks a setup guide by.</summary>
    public static string Summary(string hostingModel) => Describe(hostingModel) is { } model ? $"{model.Title}. {model.Summary.ReplaceLineEndings(" ")}" : string.Empty;

    private static Model? Describe(string? hostingModel)
    {
        var name = (hostingModel ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

        name = name switch
        {
            "webapp" or "blazor-web-app" or "blazor-webapp" or "interactivewebassembly" or "interactiveauto" or "auto" => "web-app",
            "wasm" or "standalone" or "standalone-wasm" or "standalone-webassembly" or "blazor-webassembly" or "blazor-wasm" => "webassembly",
            "blazor-server" or "interactiveserver" => "server",
            "maui" or "blazor-hybrid" or "wpf" or "winforms" or "windows-forms" => "hybrid",
            _ => name
        };

        return name switch
        {
            "web-app" => new Model(
                "Blazor Web App with a WebAssembly or Auto client",
                """
                Two containers: the server prerenders every page, then the page runs again in the browser - so
                `AddBorm()` (and any custom renderer or message provider) goes into BOTH Program.cs files, and the
                demo keeps the two in step with one shared `AddDemoServices` extension. Reference `Bit.Borm` from the
                .Client project (the server gets it transitively), put the pages with a form in the .Client project so
                their assembly reaches the browser, and link the stylesheet from the server's `App.razor`, which is the
                host page. This is how the demo is built, and the files below are its own; its server `Program.cs` also
                maps this MCP server (`AddControllers`, `AddMcpServer`, `MapControllers`, `MapMcp`) - leave those lines
                out of your app.
                """,
                null,
                IncludeDemo: true),

            "webassembly" => new Model(
                "standalone Blazor WebAssembly",
                """
                One project, one container and no prerendering: `AddBorm()` goes into its Program.cs, and the host page
                is `wwwroot/index.html`, so that is where the stylesheet link goes. Every rule, condition and computed
                value runs in the browser with no round trip.
                """,
                StandaloneFiles,
                IncludeDemo: false),

            "server" => new Model(
                "Blazor Server, or a Blazor Web App rendering InteractiveServer",
                """
                One container - the server's - so `AddBorm()` is registered once, and the stylesheet is linked from
                `App.razor` (or `_Host.cshtml` in an older Blazor Server app). The form runs on the server: every field
                that updates on input sends its keystrokes over the SignalR circuit, so keep the default
                update-on-change, or debounce (`UpdateOnInput(debounceMilliseconds: 300)`), on a busy form.
                """,
                ServerFiles,
                IncludeDemo: false),

            "hybrid" => new Model(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                The form runs in-process and renders into the embedded WebView. `AddBorm()` goes into the host's service
                registration next to `AddMauiBlazorWebView()` (or `AddWpfBlazorWebView()`/`AddWindowsFormsBlazorWebView()`),
                and the host page is the app's `wwwroot/index.html`, so that is where the stylesheet link goes.
                """,
                HybridFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Model model)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Borm up - {model.Title}").AppendLine();
        builder.AppendLine(model.Summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        if (model.Files is not null) builder.AppendLine(model.Files).AppendLine();

        if (model.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is once the projects and namespaces are renamed to your own - except what is ")
                   .AppendLine("this documentation site's own: in `App.razor`, `app.css`, `demo.css`, `theme.js` and `site.js` are the site's ")
                   .AppendLine("chrome and theme, and the `DocsLayout` that `Routes.razor` names stands for your own layout. The one line Borm ")
                   .AppendLine("needs in the host page is the `bit-borm.css` link.")
                   .AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BormSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on installing and on the hosting models, so the caveats are the
        // library's rather than a paraphrase of them that can drift.
        var quoted = _guideSections.Select(BormSourceCatalog.GetGuideSection).Where(section => section is not null).ToArray();

        if (quoted.Length > 0)
        {
            builder.AppendLine("## From the guide").AppendLine();

            foreach (var section in quoted) builder.AppendLine(Demote(section!)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    /// <summary>The checklist every hosting model's guide opens with - the same few steps, and the failure each one prevents.</summary>
    public const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.Borm` - one package, every type in the `Bit.Borm` namespace and prefixed `Borm`. In a
           Blazor Web App with an interactive client, add it to the .Client project. FluentValidation support is a separate,
           optional package: `dotnet add package Bit.Borm.FluentValidation`, for `state.UseFluentValidation(...)`.
        2. Add `@using Bit.Borm` to `_Imports.razor`.
        3. Register the rendering services with `builder.Services.AddBorm();` in EVERY container that renders a form. In a
           Blazor Web App that is both the server's and the client's Program.cs: the page is prerendered on the server and
           then runs in the browser, and both containers must resolve the field renderer registry. A container without it
           fails as soon as a field renders, with "There is no registered service of type
           'Bit.Borm.IBormFieldRendererRegistry'". Custom renderers are registered in the same call -
           `AddBorm(registry => registry.RegisterCustom<StarRatingInput>("rating"))` - and so belong in both containers too;
           calling `AddBorm` twice configures the registry that is already there.
        4. Link the stylesheet from the host page - nothing adds it for you: `<link rel="stylesheet"
           href="_content/Bit.Borm/bit-borm.css" />` (in a Blazor Web App's `App.razor`,
           `@Assets["_content/Bit.Borm/bit-borm.css"]`). Without it the form still works, as plain HTML - no theme, no
           grid, no error colours. Load your own overrides of its `--bit-borm-*` properties after it.
        5. There is NO script to add: Borm has no JavaScript and no browser-only API - every rule, condition and computed
           value runs in C#.
        6. Render the form interactively. In static server rendering it is markup that cannot respond to input; give the
           page (or the app) an interactive render mode. The form prerenders as plain HTML and becomes interactive with the page.
        7. Render with `<BormView Definition="..." Model="..." OnValidSubmit="..." />` - a schema from
           `BormSchemaGenerator.Generate<T>()`, `BormBuilder.For<T>()` or `BormJsonSchemaImporter.TryImport(...)` - or pass
           `Data` (a dictionary) instead of `Model` when there is no compiled model. A `BormState` the page builds itself
           is the page's to dispose.
        8. Two forms on one page share DOM ids when their fields share names: give each its own `IdPrefix`.
        9. Optional: `builder.Services.AddBormMessages<MyMessageProvider>()` replaces the built-in English text (an
           `IBormMessageProvider`, keys on `BormMessageKeys`) - in every container, like `AddBorm()`.
        """;

    private const string ServerFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddBorm();

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        ### `Components/App.razor` (in `<head>`)

        ```razor
        <link rel="stylesheet" href="@Assets["_content/Bit.Borm/bit-borm.css"]" />
        ```

        ### `Components/_Imports.razor`

        ```razor
        @using Bit.Borm
        ```

        ### `Components/Pages/Contact.razor`

        ```razor
        @page "/contact"
        @rendermode InteractiveServer
        @using System.ComponentModel.DataAnnotations

        <BormView Definition="_definition" Model="_model" OnValidSubmit="Save" />

        @code {
            private readonly Contact _model = new();
            private readonly BormDefinition _definition = BormSchemaGenerator.Generate<Contact>();

            private Task Save(BormState state) => Task.CompletedTask; // _model is populated and valid here

            public class Contact
            {
                [Required, MaxLength(80)] public string Name { get; set; } = "";
                [Required, EmailAddress] public string Email { get; set; } = "";
            }
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

        builder.Services.AddBorm();

        await builder.Build().RunAsync();
        ```

        ### `wwwroot/index.html` (in `<head>`)

        ```html
        <link rel="stylesheet" href="_content/Bit.Borm/bit-borm.css" />
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Borm
        ```

        ### `Pages/Feedback.razor`

        ```razor
        @page "/feedback"

        <BormView Definition="_definition" Data="_data" OnValidSubmit="Send" />

        @code {
            private readonly Dictionary<string, object?> _data = new();

            private readonly BormDefinition _definition = BormBuilder.Create()
                .Title("Feedback")
                .Text("subject", f => f.Required())
                .Field("body", BormFieldType.TextArea, f => f.AsTextArea(rows: 6))
                .Build();

            private Task Send(BormState state) => Task.CompletedTask; // _data holds the answers here
        }
        ```
        """;

    private const string HybridFiles = """
        ## The files

        ### `MauiProgram.cs` (or the WPF / Windows Forms host)

        ```csharp
        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddBorm();
        ```

        ### `wwwroot/index.html` (in `<head>`)

        ```html
        <link rel="stylesheet" href="_content/Bit.Borm/bit-borm.css" />
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Borm
        ```

        Put `<BormView Definition="_definition" Model="_model" OnValidSubmit="Save" />` on any page the BlazorWebView renders.
        """;

    private const string Next = """
        The form itself is a schema - call `GetBormGuideSection(heading: "Getting started")` for a first form,
        `GetBormApi(typeName: "BormView")` for every parameter with its real default, `GetBormApi(typeName: "BormFieldBuilder")`
        for what a field can be told, or `GetBormSourceFile()` for the index of the demo's working forms.
        """;

    /// <summary>
    /// Pushes a quoted guide section down so its own heading is a third-level one, nested under "From
    /// the guide" instead of competing with the headings of this document - whatever level it started
    /// at, so a top-level section of the README quoted here nests just the same as a sub-section.
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
        _ => string.Empty
    };

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
