using System.Text;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// The complete wiring for adding Bit.Belemetry to one kind of Blazor host, assembled from the README's
/// setup section and the real files of the sample that runs that host - not written out here a second time.
/// <para>
/// The host decides three things a reader cannot see in any signature: which DI containers register the
/// services, where the .NET pipeline runs (in the browser, on the server, in the app), and where the browser
/// script comes from (<c>&lt;BelemetryScript /&gt;</c>, a script tag, or nothing at all in Hybrid). Every
/// answer states all three before it shows any file.
/// </para>
/// </summary>
public static class BelemetrySetupGuide
{
    /// <summary>One host the guide knows.</summary>
    /// <param name="Id">What to pass as <c>host</c>.</param>
    /// <param name="Title">The host's name.</param>
    /// <param name="Section">The README section the prose comes from.</param>
    /// <param name="Facts">The three facts above, for this host.</param>
    /// <param name="Files">The embedded files that show it, most important first. Missing ones are skipped.</param>
    public sealed record Host(string Id, string Title, string Section, string[] Facts, string[] Files);

    /// <summary>Every host, in the order the docs present them.</summary>
    public static readonly Host[] Hosts =
    [
        new("web-app", "Blazor Web App (any render mode)", "Blazor Web App",
            [
                "Register `AddBitBelemetryServices` in BOTH projects (server and client) with the same configuration - the server renders the browser script's configuration from its options, the client runs the .NET pipeline in WebAssembly.",
                "Put `<BelemetryScript />` in the `<head>` of App.razor and `<BelemetryInitializer />` once in Routes.razor (or the main layout).",
                "The .NET pipeline runs where the components run: in the browser for WebAssembly/Auto; on the server only with `options.Server.CollectDotnetTelemetry = true`.",
            ],
            ["Samples/Wasm/Bit.Belemetry.Samples.Wasm/Program.cs", "Samples/Wasm/Bit.Belemetry.Samples.Wasm.Client/Program.cs", "Samples/Wasm/Bit.Belemetry.Samples.Wasm/Components/App.razor", "Samples/Core/Routes.razor", "Demo/Client/Routes.razor"]),

        new("server", "Blazor Web App, InteractiveServer", "Blazor Web App",
            [
                "Register `AddBitBelemetryServices` in the server's Program.cs (there is no client project).",
                "`<BelemetryScript />` in App.razor's `<head>` exports the browser's records itself (Direct transport) - the endpoint must allow the site's origin (CORS) - and `<BelemetryInitializer />` in Routes.razor connects consent and `IBelemetry` to the circuit.",
                "Server-side .NET telemetry (ILogger, Activity, Meter) is OFF by default; turn it on with `options.Server.CollectDotnetTelemetry = true`. It follows `Server.Features`, not a user's consent. `IBelemetry` calls of a circuit go through that user's browser, so their consent applies.",
            ],
            ["Samples/Server/Program.cs", "Samples/Server/Components/App.razor", "Samples/Core/Routes.razor", "Samples/Core/Components/ConsentBanner.razor", "Samples/Server/appsettings.json"]),

        new("webassembly", "Blazor Web App, InteractiveWebAssembly", "Blazor Web App",
            [
                "Register `AddBitBelemetryServices` in BOTH the server and the client project, with the same service name and exporters.",
                "`<BelemetryScript />` in the server's App.razor `<head>`, `<BelemetryInitializer />` in Routes.razor.",
                "The .NET pipeline runs in the browser, under the user's consent, and is flushed with `keepalive` on unload. The server only serves the script unless `Server.CollectDotnetTelemetry` is on.",
            ],
            ["Samples/Wasm/Bit.Belemetry.Samples.Wasm/Program.cs", "Samples/Wasm/Bit.Belemetry.Samples.Wasm.Client/Program.cs", "Samples/Wasm/Bit.Belemetry.Samples.Wasm/Components/App.razor", "Samples/Core/Routes.razor", "Samples/Core/Components/ConsentBanner.razor"]),

        new("auto", "Blazor Web App, InteractiveAuto", "Blazor Web App",
            [
                "Register `AddBitBelemetryServices` in BOTH projects: a page starts on the server circuit and moves to WebAssembly on a later visit, and both containers need the services.",
                "`<BelemetryScript />` in App.razor's `<head>`, `<BelemetryInitializer />` in Routes.razor.",
                "The browser script is the same either way; the .NET pipeline runs on the server only with `Server.CollectDotnetTelemetry`, and in the browser once WebAssembly takes over.",
            ],
            ["Samples/Auto/Bit.Belemetry.Samples.Auto/Program.cs", "Samples/Auto/Bit.Belemetry.Samples.Auto.Client/Program.cs", "Samples/Auto/Bit.Belemetry.Samples.Auto/Components/App.razor", "Samples/Core/Routes.razor"]),

        new("static-ssr", "Blazor Web App, static SSR pages", "Blazor Web App",
            [
                "Register `AddBitBelemetryServices` in the server's Program.cs.",
                "`<BelemetryScript />` in App.razor's `<head>` is all a static page needs: the script starts from the configuration it writes into the page, and records page views (including enhanced navigation), clicks, web vitals, errors and fetches with no interactivity at all.",
                "`IBelemetryConsent` and `IBelemetry` need an interactive component; on purely static pages drive consent from JavaScript (`BitBelemetry.setConsent(...)`). Server .NET telemetry needs `Server.CollectDotnetTelemetry`.",
            ],
            ["Samples/Server/Program.cs", "Samples/Server/Components/App.razor"]),

        new("standalone-wasm", "Standalone Blazor WebAssembly", "Standalone Blazor WebAssembly",
            [
                "Register `AddBitBelemetryServices` in the one Program.cs.",
                "`<BelemetryInitializer />` in App.razor starts the browser script; optionally add `<script type=\"module\" src=\"_content/Bit.Belemetry/bit-belemetry.js\"></script>` to index.html to capture what happens before Blazor starts.",
                "Everything runs in the browser under the user's consent; the exporter endpoints must allow the site's origin (CORS).",
            ],
            []),

        new("hybrid", "Blazor Hybrid (MAUI, WPF, WinForms)", "Blazor Hybrid",
            [
                "Register `AddBitBelemetryServices` in MauiProgram.cs (or the WPF/WinForms host's service collection).",
                "No `<BelemetryScript />` (there is no server-rendered head): `<BelemetryInitializer />` in Routes.razor/Main.razor starts the script, which RELAYS its records to .NET over JS interop - no CORS, no credentials in the page.",
                "The .NET pipeline exports everything and persists unsent requests to files while offline; flush with `IBelemetry.FlushAsync()` when the app is suspended.",
            ],
            ["Samples/Maui/MauiProgram.cs", "Samples/Maui/App.xaml.cs", "Samples/Maui/wwwroot/index.html", "Samples/Core/Routes.razor"]),
    ];

    /// <summary>The ids <see cref="Get"/> accepts.</summary>
    public static string[] HostIds => [.. Hosts.Select(h => h.Id)];

    /// <summary>Finds a host by id, also accepting the common spellings (<c>wasm</c>, <c>maui</c>, <c>ssr</c>...).</summary>
    public static Host? Find(string? host)
    {
        var id = (host ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '-').Replace('_', '-');

        id = id switch
        {
            "blazor-web-app" or "webapp" or "web" => "web-app",
            "interactiveserver" or "blazor-server" => "server",
            "wasm" or "interactivewebassembly" or "web-assembly" => "webassembly",
            "interactiveauto" => "auto",
            "ssr" or "static" or "static-server" => "static-ssr",
            "standalone" or "standalone-webassembly" or "wasm-standalone" => "standalone-wasm",
            "maui" or "wpf" or "winforms" or "blazor-hybrid" => "hybrid",
            _ => id,
        };

        return Hosts.FirstOrDefault(h => h.Id == id);
    }

    /// <summary>The guide for <paramref name="host"/> as markdown, or null when the host is unknown.</summary>
    public static string? Get(string? host)
    {
        if (Find(host) is not { } found) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# Setting up bit Belemetry: {found.Title}").AppendLine();
        builder.AppendLine("## What this host decides").AppendLine();
        foreach (var fact in found.Facts) builder.AppendLine($"- {fact}");
        builder.AppendLine();

        builder.AppendLine("## Install").AppendLine();
        builder.AppendLine("```bash").AppendLine("dotnet add package Bit.Belemetry").AppendLine("```").AppendLine();
        builder.AppendLine("Add `@using Bit.Belemetry` to `_Imports.razor` in every project whose components use it.").AppendLine();

        var section = BelemetrySourceCatalog.Sections.FirstOrDefault(s => s.Level == 3 && s.Heading.StartsWith(found.Section, StringComparison.OrdinalIgnoreCase));

        if (section is not null)
        {
            builder.AppendLine($"## From the guide: {section.Heading}").AppendLine();
            builder.AppendLine(section.Body).AppendLine();
        }

        var files = found.Files.Select(path => (path, content: BelemetrySourceCatalog.GetSource(path))).Where(f => f.content is not null).ToArray();

        if (files.Length > 0)
        {
            builder.AppendLine("## A working project").AppendLine();
            builder.AppendLine("These are the real files of a project that runs this host (the demo site or a sample) - copy the Belemetry parts.").AppendLine();

            foreach (var (path, content) in files)
            {
                builder.AppendLine($"### {path}").AppendLine();
                builder.AppendLine($"```{Fence(path)}").AppendLine(content!.TrimEnd()).AppendLine("```").AppendLine();
            }
        }

        builder.AppendLine("## Next").AppendLine();
        builder.AppendLine("- Consent: nothing is sent until the user grants a feature (`Consent.Default` is `None`). Add a banner that calls `IBelemetryConsent.SetAsync(...)` - `GetBelemetrySourceFile` with `Demo/Client/Shared/ConsentBanner.razor` shows one.");
        builder.AppendLine("- This documentation site is itself a Blazor Web App wired this way: `Demo/Server/Program.cs`, `Demo/Client/Program.cs` and `Demo/Client/Services/DemoTelemetry.cs` (via `GetBelemetrySourceFile`).");
        builder.AppendLine("- `GenerateBelemetryConfiguration` writes the configuration for your exporters and consent model; `ReviewBelemetryConfiguration` checks what you wrote.");

        return builder.ToString();
    }

    private static string Fence(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".cs" => "csharp",
        ".razor" => "razor",
        ".html" => "html",
        ".json" => "json",
        ".csproj" => "xml",
        _ => string.Empty,
    };
}
