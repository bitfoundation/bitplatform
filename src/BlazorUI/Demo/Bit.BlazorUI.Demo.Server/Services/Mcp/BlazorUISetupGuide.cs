using System.Text;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// The wiring that has to be right before a single component renders, written out per hosting model.
/// <para>
/// Two of the four steps below fail silently rather than at build time: a missing
/// <c>AddBitBlazorUIServices()</c> surfaces as a service-resolution error deep inside a component,
/// and a missing stylesheet surfaces as components that render as unstyled markup. Both differ per
/// hosting model - which file holds the host page, and how many DI containers there are - so the
/// guide is per model rather than one page with four asides.
/// </para>
/// </summary>
public static class BlazorUISetupGuide
{
    /// <summary>The hosting models this guide is written for - the values the tool accepts.</summary>
    public static string[] HostingModels { get; } = ["web-app", "wasm", "server", "hybrid"];

    public static string? Get(string? hostingModel)
    {
        var model = (hostingModel ?? string.Empty).Trim().ToLowerInvariant();

        return model switch
        {
            "web-app" or "webapp" or "blazor-web-app" or "auto" => WebApp(),
            "wasm" or "webassembly" or "standalone-wasm" => Wasm(),
            "server" or "blazor-server" => Server(),
            "hybrid" or "maui" or "wpf" or "winforms" => Hybrid(),
            _ => null
        };
    }

    private static string WebApp()
    {
        var builder = Head("Blazor Web App (a server project plus a .Client project, with prerendering)");

        builder.AppendLine("Both projects reference the packages, and **both** register the services: the server project")
               .AppendLine("prerenders the same components the client then takes over, so a registration missing from")
               .AppendLine("either container fails on one render pass and not the other.").AppendLine();

        Packages(builder, "in both the server project and the .Client project");
        Namespace(builder, "the .Client project's `_Imports.razor` (and the server project's, if it renders components of its own)");

        builder.AppendLine("## 3. Register the services").AppendLine();
        builder.AppendLine("Put the registration in a method both projects compile - the usual shared `AddClientServices()` -")
               .AppendLine("and call it from each `Program.cs`, so the two containers cannot drift apart:").AppendLine();
        builder.AppendLine("```csharp")
               .AppendLine("// Shared, called from both Program.cs files")
               .AppendLine("services.AddBitBlazorUIServices();")
               .AppendLine("```").AppendLine();
        builder.AppendLine("Leave `trySingleton` at its default of `false` here. Services such as `BitModalService` hold")
               .AppendLine("per-circuit rendering state, and a singleton would share it across every visitor of the server")
               .AppendLine("half.").AppendLine();

        HostPage(builder, "Components/App.razor");
        Optional(builder);
        FirstPaint(builder);
        Verify(builder);

        return builder.ToString();
    }

    private static string Wasm()
    {
        var builder = Head("Standalone Blazor WebAssembly");

        Packages(builder, "in the project");
        Namespace(builder, "`_Imports.razor`");

        builder.AppendLine("## 3. Register the services").AppendLine();
        builder.AppendLine("```csharp")
               .AppendLine("// Program.cs")
               .AppendLine("builder.Services.AddBitBlazorUIServices(trySingleton: true);")
               .AppendLine("```").AppendLine();
        builder.AppendLine("`trySingleton: true` is safe here and nowhere on a server: a WebAssembly app is one user, so")
               .AppendLine("there is no second circuit for a shared instance to leak into.").AppendLine();

        HostPage(builder, "wwwroot/index.html");

        builder.AppendLine("`@Assets[\"\"]` and the `<Script>`/`<Link>` components only work in `App.razor`, which a")
               .AppendLine("standalone WebAssembly app does not have. Append the library version by hand instead:")
               .AppendLine($"`?v={BlazorUIAssemblies.Version}` - or whatever version the project references.").AppendLine();

        Optional(builder);
        Verify(builder);

        return builder.ToString();
    }

    private static string Server()
    {
        var builder = Head("Blazor Server (InteractiveServer, one project)");

        Packages(builder, "in the project");
        Namespace(builder, "`Components/_Imports.razor`");

        builder.AppendLine("## 3. Register the services").AppendLine();
        builder.AppendLine("```csharp")
               .AppendLine("// Program.cs")
               .AppendLine("builder.Services.AddBitBlazorUIServices();")
               .AppendLine("```").AppendLine();
        builder.AppendLine("Do **not** pass `trySingleton: true` on Blazor Server. `BitModalService` and its siblings hold")
               .AppendLine("the active modal container for the circuit they belong to, and a singleton would show one")
               .AppendLine("visitor's modal to another.").AppendLine();

        HostPage(builder, "Components/App.razor");
        Optional(builder);
        FirstPaint(builder);
        Verify(builder);

        return builder.ToString();
    }

    private static string Hybrid()
    {
        var builder = Head("Blazor Hybrid (MAUI, WPF or WinForms)");

        Packages(builder, "in the project");
        Namespace(builder, "`_Imports.razor`");

        builder.AppendLine("## 3. Register the services").AppendLine();
        builder.AppendLine("```csharp")
               .AppendLine("// MauiProgram.cs, after AddMauiBlazorWebView()")
               .AppendLine("builder.Services.AddBitBlazorUIServices(trySingleton: true);")
               .AppendLine("```").AppendLine();
        builder.AppendLine("`trySingleton: true` is safe here for the same reason it is on WebAssembly: one WebView, one")
               .AppendLine("user.").AppendLine();

        HostPage(builder, "wwwroot/index.html");

        builder.AppendLine("The assets are served from the app bundle rather than over HTTP, so there is no cache to bust")
               .AppendLine("and no version query string to add.").AppendLine();

        Optional(builder);
        Verify(builder);

        return builder.ToString();
    }

    private static StringBuilder Head(string model)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Adding bit BlazorUI {BlazorUIAssemblies.Version} to a {model} app").AppendLine();

        return builder;
    }

    private static void Packages(StringBuilder builder, string where)
    {
        builder.AppendLine("## 1. Install").AppendLine();
        builder.AppendLine($"Reference `Bit.BlazorUI` {where}:").AppendLine();
        builder.AppendLine("```").AppendLine("dotnet add package Bit.BlazorUI").AppendLine("```").AppendLine();
    }

    private static void Namespace(StringBuilder builder, string where)
    {
        builder.AppendLine("## 2. Import the namespace").AppendLine();
        builder.AppendLine($"Add one line to {where}:").AppendLine();
        builder.AppendLine("```razor").AppendLine("@using Bit.BlazorUI").AppendLine("```").AppendLine();
    }

    private static void HostPage(StringBuilder builder, string hostPage)
    {
        builder.AppendLine("## 4. The stylesheet and the script").AppendLine();
        builder.AppendLine($"In `{hostPage}` - the stylesheet in `<head>`, the script at the end of `<body>`:").AppendLine();
        builder.AppendLine("```html")
               .AppendLine(BlazorUIAssemblies.Core.StylesheetTag)
               .AppendLine(BlazorUIAssemblies.Core.ScriptTag)
               .AppendLine("```").AppendLine();
        builder.AppendLine("Without the stylesheet every component renders as unstyled markup; without the script the ones")
               .AppendLine("that measure or position themselves - callouts, dropdowns, tooltips, the modal - do nothing.")
               .AppendLine("Neither failure produces a build error or a console message.").AppendLine();
    }

    /// <summary>The four optional packages, each with the whole of what adding it takes.</summary>
    private static void Optional(StringBuilder builder)
    {
        builder.AppendLine("## Optional packages").AppendLine();
        builder.AppendLine("Each is independent; add only the ones the app uses. `GetBitBlazorUIComponent` says which package")
               .AppendLine("a component ships in.").AppendLine();

        foreach (var package in BlazorUIAssemblies.Packages.Where(p => p.Required is false))
        {
            builder.AppendLine($"### {package.PackageId}").AppendLine();
            builder.AppendLine(package.Summary).AppendLine();
            builder.AppendLine("```").AppendLine($"dotnet add package {package.PackageId}").AppendLine("```").AppendLine();

            if (package.Registration is not null)
            {
                builder.AppendLine("```csharp").AppendLine($"services.{package.Registration}; // also registers the core services").AppendLine("```").AppendLine();
            }

            builder.AppendLine("```html").AppendLine(package.StylesheetTag);
            if (package.Script is not null) builder.AppendLine(package.ScriptTag);
            builder.AppendLine("```").AppendLine();
        }

        builder.AppendLine("`Bit.BlazorUI.Legacy` also needs `@using Bit.BlazorUI.Legacy` in `_Imports.razor`: the previous")
               .AppendLine("components live in their own namespace so both generations can be referenced at once.").AppendLine();
    }

    /// <summary>The theme flash, which only exists where a server paints the first frame.</summary>
    private static void FirstPaint(StringBuilder builder)
    {
        builder.AppendLine("## Theme on the first frame").AppendLine();
        builder.AppendLine("A server-rendered first frame is painted before any JavaScript runs, so an app with a dark theme")
               .AppendLine("flashes light unless the theme is resolved during the render. `BitThemeSsr` does that from the")
               .AppendLine("preference cookie the client mirrors the choice into:").AppendLine();
        builder.AppendLine("```razor")
               .AppendLine("@{ var themeAttributes = BitThemeSsr.BuildRootThemeAttributeMap(HttpContext.Request.Cookies[BitThemeCookie.PreferenceCookieName]); }")
               .AppendLine("<html lang=\"en\" @attributes=\"themeAttributes\" bit-theme-persist bit-theme-persist-cookie>")
               .AppendLine("<head>")
               .AppendLine("    @((MarkupString)BitThemeSsr.InlineHeadScript)")
               .AppendLine("```").AppendLine();
        builder.AppendLine("`GetBitBlazorUIThemingGuide(section: \"Server-side rendering\")` has the whole of it.").AppendLine();
    }

    private static void Verify(StringBuilder builder)
    {
        builder.AppendLine("## Verify").AppendLine();
        builder.AppendLine("Put this on a page and load it. It fails visibly for each of the three mistakes above: an")
               .AppendLine("unregistered service throws, a missing stylesheet renders a plain HTML button, and a missing")
               .AppendLine("script leaves the callout closed when the button is clicked.").AppendLine();
        builder.AppendLine("```razor")
               .AppendLine("<BitButton Variant=\"BitVariant.Fill\" Color=\"BitColor.Primary\" OnClick=\"@(() => _open = true)\">Check</BitButton>")
               .AppendLine("<BitModal @bind-IsOpen=\"_open\"><div style=\"padding:1rem\">Everything is wired up.</div></BitModal>")
               .AppendLine()
               .AppendLine("@code { private bool _open; }")
               .AppendLine("```").AppendLine();
    }
}
