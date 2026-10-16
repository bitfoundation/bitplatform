using System.Text;
using System.Reflection;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Bit.Belemetry.Demo.Server.Dtos;
using Bit.Belemetry.Demo.Server.Services;

namespace Bit.Belemetry.Demo.Server.Controllers;

/// <summary>
/// The Belemetry MCP server: the tools an AI agent calls to add telemetry to a Blazor app with Bit.Belemetry
/// without guessing - which project registers what, which option a setting really is, where consent applies,
/// and what an OTLP endpoint has to look like.
/// <para>
/// The surface is kept to seven tools on purpose. Every answer is derived - from the README, the contracts
/// document, the reflected assembly and its XML documentation, and the real files of the demo and the samples
/// - so nothing here is a second copy that could drift from the library.
/// </para>
/// <para>
/// Every tool is also a plain HTTP GET under <c>/api/mcp/...</c>, which makes it inspectable from a browser and
/// is what the demo's own MCP page calls to show the tools working live.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    /// <summary>
    /// The longest text a tool returns. Long enough for the largest README section or demo page, short enough
    /// that a couple of tool calls cannot crowd out a client's context window; longer files are read by line range.
    /// </summary>
    public const int MaxDocumentLength = 15_000;

    /// <summary>The Bit.Belemetry build the answers come from.</summary>
    public static readonly string BelemetryVersion =
        typeof(BelemetryOptions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BelemetryOptions).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    [HttpGet]
    [McpServerTool(Name = nameof(GetBelemetrySetupGuide))]
    [Description("Gets the complete wiring for adding Bit.Belemetry to one kind of Blazor host: which projects register AddBitBelemetryServices, where <BelemetryScript /> and <BelemetryInitializer /> go, where the .NET pipeline runs, plus the README's setup section and the real files of a working project. host: 'web-app', 'server', 'webassembly', 'auto', 'static-ssr', 'standalone-wasm' or 'hybrid' (MAUI/WPF/WinForms). Call this before writing any setup code.")]
    public string GetBelemetrySetupGuide(
        [Description("The host: web-app, server, webassembly, auto, static-ssr, standalone-wasm or hybrid.")] string host)
    {
        return Truncate(BelemetrySetupGuide.Get(host)
            ?? $"'{host}' is not a known host. Use one of: {string.Join(", ", BelemetrySetupGuide.HostIds)}.");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBelemetry))]
    [Description("Start here when you do not know where the answer lives. Searches the README, the demo pages, every public type of the reflected API and the reference tables at once, and returns the best matches, each with the exact follow-up tool call for its full text. Example queries: 'consent banner', 'send to Aspire dashboard', 'Honeycomb api key', 'web vitals', 'ILogger minimum level', 'offline'.")]
    public BelemetrySearchHitDto[] SearchBelemetry(
        [Description("What you are looking for, in plain words.")] string query,
        [Description("The most hits to return (1-50).")] int limit = 10)
    {
        return BelemetrySearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBelemetryApiDetails))]
    [Description("Gets one public Bit.Belemetry type, reflected from the assembly this server runs: its kind, summary, and every member with its type, its real default value (for options classes) and its XML documentation. Pass a type name such as 'BelemetryOptions', 'BelemetryBrowserOptions', 'IBelemetryConsent', 'BelemetryFeatures' or 'BelemetryOtlpExporterOptions'. Call it for every options class you are about to set - the defaults are the behaviour.")]
    public BelemetryApiDetailsResultDto GetBelemetryApiDetails(
        [Description("The type name, with or without the Bit.Belemetry namespace.")] string typeName)
    {
        if (BelemetryApiCatalog.Find(typeName) is { } details) return new(details, null);

        return new(null, $"Bit.Belemetry has no public type called '{typeName}'. Did you mean: {string.Join(", ", BelemetryApiCatalog.Suggest(typeName))}?");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GenerateBelemetryConfiguration))]
    [Description("Writes the Belemetry wiring for a host, a set of exporters and a consent model: a shared BelemetrySetup.Configure method, each project's AddBitBelemetryServices call, the App.razor and Routes.razor lines, a consent banner when needed, and notes on CORS, credentials and consent. Use it instead of writing the setup from memory.")]
    public BelemetryGeneratedConfigDto GenerateBelemetryConfiguration(
        [Description("The host: web-app, server, webassembly, auto, static-ssr, standalone-wasm or hybrid.")] string host,
        [Description("Comma-separated exporters: otlp, azure, console.")] string exporters = "otlp",
        [Description("The consent model: opt-in (nothing sent before the user decides; the GDPR default), opt-out, or none (no consent UI).")] string consent = "opt-in",
        [Description("Optional comma-separated BelemetryFeatures to enable at all (e.g. 'Errors,Performance,PageViews' or 'Diagnostics'); empty keeps All.")] string? features = null,
        [Description("Optional browser transport: Auto, Direct or Relay.")] string? transport = null)
    {
        return BelemetryConfigGenerator.Generate(host, exporters, consent, features, transport);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(ReviewBelemetryConfiguration))]
    [Description("Reviews Belemetry wiring - Program.cs, App.razor, Routes.razor, or all pasted together - for the mistakes that compile and then send nothing, send too much, or leak credentials: OTLP endpoints ending in /v1/traces, API keys in browser-visible exporter headers without the Relay transport, a consent default that grants analytics, a missing <BelemetryScript /> or <BelemetryInitializer />, the server pipeline being off, port 4317, blocking waits in WebAssembly, and more. Call it on what you wrote before finishing.")]
    public BelemetryReviewDto ReviewBelemetryConfiguration(
        [Description("The code to review.")] string code)
    {
        return BelemetryCodeReview.Review(code);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(LookupBelemetryConventions))]
    [Description("Looks up the reference tables: the feature bits and groups, the browser storage keys, the semantic conventions the browser and .NET emit (attributes, event names, span names, resource), the Azure Monitor mapping (envelope types, tags, retries), environment variables and browser options - plus the BelemetrySemanticConventions constants. Every word of the query must match the row. An empty query lists the tables. Examples: 'web_vital', 'session.id', 'PageView', 'ai.user', 'localStorage', 'Persistence'.")]
    public string LookupBelemetryConventions(
        [Description("Words to match, e.g. an attribute name, a feature, a record or envelope type; empty to list the tables.")] string? query = null)
    {
        return Truncate(BelemetryConventions.Lookup(query));
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBelemetrySourceFile))]
    [Description("Reads an embedded file: the README ('README.md', or one section as 'README.md#consent-and-gdpr'), a demo page or component ('Demo/Client/Pages/ConsentPage.razor'), the demo host ('Demo/Server/Program.cs'), or a hosting sample ('Samples/Server/Program.cs'). An empty path, or a folder, lists the files. Long files can be read by line range.")]
    public string GetBelemetrySourceFile(
        [Description("The path, e.g. 'Demo/Client/Shared/ConsentBanner.razor' or 'README.md#offline-persistence'; empty or a folder to list.")] string? path = null,
        [Description("The first line to return (1-based).")] int? startLine = null,
        [Description("The last line to return (inclusive).")] int? endLine = null)
    {
        var normalized = BelemetrySourceCatalog.Normalize(path ?? string.Empty);

        if (normalized.StartsWith("README", StringComparison.OrdinalIgnoreCase))
        {
            return Readme(normalized, startLine, endLine);
        }

        if (BelemetrySourceCatalog.GetSource(normalized) is { } content)
        {
            return Slice(normalized, content, startLine, endLine);
        }

        var folder = normalized.TrimEnd('/');
        var listed = BelemetrySourceCatalog.Paths
            .Where(p => folder.Length == 0 || p.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase) || p.Contains(folder, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var builder = new StringBuilder();

        if (folder.Length > 0 && listed.Length == 0)
        {
            builder.AppendLine($"No embedded file matches '{path}'. The files:").AppendLine();
            listed = BelemetrySourceCatalog.Paths;
        }
        else if (folder.Length > 0)
        {
            builder.AppendLine($"'{path}' is not a single file. Matching files:").AppendLine();
        }
        else
        {
            builder.AppendLine("Embedded files (pass one as path). Also: README.md, or README.md#<section-anchor>.").AppendLine();
        }

        foreach (var file in listed) builder.AppendLine($"- {file}");

        return builder.ToString();
    }

    /// <summary>
    /// Everything this server offers, reflected from its own attributes, for the MCP page of the demo. Not a
    /// tool: an MCP client already has all of this from tools/list, prompts/list and resources/list.
    /// </summary>
    [HttpGet]
    public McpCatalogDto GetMcpCatalog()
    {
        var tools = typeof(McpController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => (method, tool: method.GetCustomAttribute<McpServerToolAttribute>()))
            .Where(x => x.tool is not null)
            .Select(x => new McpToolDto(
                x.tool!.Name ?? x.method.Name,
                x.method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                [.. x.method.GetParameters().Select(p => new McpParameterDto(
                    p.Name!,
                    BelemetryApiCatalog.TypeName(p.ParameterType),
                    p.HasDefaultValue is false,
                    p.GetCustomAttribute<DescriptionAttribute>()?.Description))]))
            .ToArray();

        var prompts = typeof(McpPrompts).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => (method, prompt: method.GetCustomAttribute<McpServerPromptAttribute>()))
            .Where(x => x.prompt is not null)
            .Select(x => new McpPromptDto(x.prompt!.Name ?? x.method.Name, x.method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty))
            .ToArray();

        var resources = typeof(McpResources).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => (method, resource: method.GetCustomAttribute<McpServerResourceAttribute>()))
            .Where(x => x.resource is not null)
            .Select(x => new McpResourceDto(x.resource!.UriTemplate ?? string.Empty, x.resource.Name ?? x.method.Name, x.method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty))
            .ToArray();

        return new(tools, prompts, resources);
    }

    private static string Readme(string normalized, int? startLine, int? endLine)
    {
        var readme = BelemetrySourceCatalog.Readme;

        if (readme.Length == 0) return "The README is not embedded in this build. Use SearchBelemetry, GetBelemetryApiDetails and the demo pages instead.";

        var hash = normalized.IndexOf('#', StringComparison.Ordinal);

        if (hash < 0) return Slice("README.md", readme, startLine, endLine);

        var anchor = normalized[(hash + 1)..];
        var section = BelemetrySourceCatalog.GetSection(anchor);

        if (section is null)
        {
            return $"The README has no section '{anchor}'. Sections: {string.Join(", ", BelemetrySourceCatalog.Sections.Select(s => s.Anchor))}.";
        }

        // A ## section includes its ### subsections, as reading it in the README would.
        var builder = new StringBuilder().AppendLine($"{new string('#', section.Level)} {section.Heading}").AppendLine().AppendLine(section.Body);

        if (section.Level == 2)
        {
            foreach (var child in BelemetrySourceCatalog.Sections.Where(s => s.Level == 3 && s.Parent == section.Heading))
            {
                builder.AppendLine().AppendLine($"### {child.Heading}").AppendLine().AppendLine(child.Body);
            }
        }

        return Truncate(builder.ToString());
    }

    private static string Slice(string path, string content, int? startLine, int? endLine)
    {
        if (startLine is null && endLine is null) return Truncate(content, path);

        var lines = content.Split('\n');
        var first = Math.Clamp(startLine ?? 1, 1, Math.Max(1, lines.Length));
        var last = Math.Clamp(endLine ?? lines.Length, first, lines.Length);

        return Truncate(string.Join('\n', lines[(first - 1)..last]), path);
    }

    private static string Truncate(string text, string? path = null)
    {
        if (text.Length <= MaxDocumentLength) return text;

        var cut = text.LastIndexOf('\n', MaxDocumentLength);
        if (cut < MaxDocumentLength / 2) cut = MaxDocumentLength;

        var lines = text.AsSpan(0, cut).Count('\n') + 1;

        return text[..cut] + (path is null
            ? "\n\n[Truncated. Narrow the request.]"
            : $"\n\n[Truncated after line {lines}. Call GetBelemetrySourceFile(path: \"{path}\", startLine: {lines + 1}) for the rest.]");
    }
}
