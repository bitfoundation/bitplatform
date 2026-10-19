using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Belemetry.Tests.Hosting.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Modes = Bit.Belemetry.Tests.Harness.Web.HarnessRenderModes;

namespace Bit.Belemetry.Tests.Hosting;

/// <summary>
/// What every render mode's first response has to carry for the browser script to start before Blazor does: the
/// statically rendered <c>&lt;BelemetryScript /&gt;</c> with a configuration the script can read, and the module it
/// points at. bUnit renders components without any HTTP response, so only a real host shows this.
/// </summary>
[TestClass]
public class BrowserConfigHostingTests
{
    // The top-level keys of the "Browser config" contract in CLAUDE.md that are always written ("resource" can be
    // empty, the nullable ones are omitted when null).
    private static readonly string[] _contractKeys =
    [
        "service", "resource", "sdkVersion", "transport", "exporters", "features", "consent", "pageViews", "interactions",
        "performance", "dependencies", "errors", "logs", "privacy", "session", "sampling", "batch", "persistence", "debug",
    ];

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.ServerNoPrerender)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.WebAssemblyNoPrerender)]
    [DataRow(Modes.Auto)]
    [DataRow(Modes.AutoNoPrerender)]
    public async Task Every_render_mode_prerenders_the_script_with_a_parsable_config(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        using var config = RenderedHtml.BrowserConfig(html);
        Assert.IsNotNull(config, $"The {mode} response carried no #bit-belemetry-config element.");

        var root = config.RootElement;
        foreach (var key in _contractKeys)
        {
            Assert.IsTrue(root.TryGetProperty(key, out _), $"The config has no \"{key}\". Config: {root}");
        }

        // Rendered in the head, config first: the module reads the element as soon as it runs.
        var head = html[..html.IndexOf("</head>", StringComparison.Ordinal)];
        var configAt = head.IndexOf("id=\"bit-belemetry-config\"", StringComparison.Ordinal);
        var moduleAt = head.IndexOf("_content/Bit.Belemetry/bit-belemetry.js", StringComparison.Ordinal);
        Assert.IsTrue(configAt >= 0 && moduleAt > configAt, "The config and the module script are not both in the head, config first.");
        Assert.AreEqual("_content/Bit.Belemetry/bit-belemetry.js", RenderedHtml.ModuleSource(html));
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task The_config_reflects_the_options(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/");
        using var config = RenderedHtml.BrowserConfig(await response.Content.ReadAsStringAsync())!;
        var root = config.RootElement;

        var service = root.GetProperty("service");
        Assert.AreEqual("belemetry-harness", service.GetProperty("name").GetString());
        Assert.AreEqual("1.0.0", service.GetProperty("version").GetString());
        Assert.AreEqual("Test", service.GetProperty("environment").GetString());
        // Each browser tab is its own instance: the server's instance id must not be handed to every page.
        Assert.IsFalse(service.TryGetProperty("instanceId", out _), "The server's service.instance.id leaked into the browser config.");

        // A web host renders the config for a browser that exports itself.
        Assert.AreEqual("direct", root.GetProperty("transport").GetString());
        Assert.AreEqual((int)BelemetryFeatures.All, root.GetProperty("features").GetInt32());
        Assert.AreEqual(0, root.GetProperty("consent").GetProperty("default").GetInt32());
        Assert.AreEqual(500, root.GetProperty("batch").GetProperty("flushIntervalMs").GetInt32());
        Assert.IsFalse(root.GetProperty("persistence").GetProperty("enabled").GetBoolean());

        var exporters = root.GetProperty("exporters").EnumerateArray().ToList();
        var otlp = exporters.Single(e => e.GetProperty("type").GetString() == "otlp");
        Assert.AreEqual("http://localhost/test-collector", otlp.GetProperty("endpoint").GetString());
        Assert.AreEqual("http/json", otlp.GetProperty("protocol").GetString());

        var azure = exporters.Single(e => e.GetProperty("type").GetString() == "azureMonitor");
        StringAssert.Contains(azure.GetProperty("connectionString").GetString(), HarnessServiceCollectionExtensions.InstrumentationKey);

        // The exporter URLs are added to the fetch/XHR ignore list, so the script never traces its own exports.
        var ignored = root.GetProperty("dependencies").GetProperty("ignoreUrls").EnumerateArray().Select(u => u.GetString()!).ToList();
        Assert.IsTrue(ignored.Any(pattern => Regex.IsMatch("http://localhost/test-collector/v1/traces", pattern)),
            $"No ignoreUrls entry matches the OTLP traces URL: {string.Join(", ", ignored)}");
        Assert.IsTrue(ignored.Any(pattern => Regex.IsMatch("http://localhost/test-collector/v2.1/track", pattern)),
            $"No ignoreUrls entry matches the Azure ingestion URL: {string.Join(", ", ignored)}");
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Auto)]
    public async Task The_script_module_is_served_as_javascript_with_the_documented_api(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/_content/Bit.Belemetry/bit-belemetry.js");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/javascript", response.Content.Headers.ContentType?.MediaType,
            "The module URL was answered by something other than the static asset.");

        var script = await response.Content.ReadAsStringAsync();
        var exports = ExportedNames(script);
        foreach (var export in new[] { "start", "setConsent", "getConsent", "isConsentDecided", "setUser", "trackEvent", "trackPageView", "trackException", "flush", "getContext", "attachDotNet", "sendRequests" })
        {
            Assert.IsTrue(exports.Contains(export), $"bit-belemetry.js does not export {export}. Exports: {string.Join(", ", exports)}");
        }

        // The auto-start that makes <BelemetryScript /> work without Blazor.
        StringAssert.Contains(script, "bit-belemetry-config");
    }

    [TestMethod]
    public async Task The_harness_page_of_every_mode_shares_one_route_table()
    {
        // The harness pages come from the Razor class library through AddAdditionalAssemblies; a page that only the
        // interactive router knew would answer 404 to a deep link.
        foreach (var path in new[] { "/", "/second", "/third" })
        {
            using var response = await HarnessHostFactory.Shared(Modes.Ssr).CreateClient().GetAsync(path);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, path);
            StringAssert.Contains(await response.Content.ReadAsStringAsync(), $"id=\"page-{(path == "/" ? "home" : path[1..])}\"");
        }
    }

    // The names a module's export declarations make public: `export function name(` and `export { local as name }`.
    private static HashSet<string> ExportedNames(string script)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match declaration in Regex.Matches(script, @"\bexport\s+(?:async\s+)?(?:function\s*\*\s*|(?:function|class|const|let|var)\s+)([A-Za-z_$][\w$]*)"))
        {
            names.Add(declaration.Groups[1].Value);
        }

        foreach (Match list in Regex.Matches(script, @"\bexport\s*\{([^}]*)\}"))
        {
            foreach (var specifier in list.Groups[1].Value.Split(','))
            {
                var tokens = specifier.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 0) names.Add(tokens[^1]);
            }
        }

        return names;
    }
}
