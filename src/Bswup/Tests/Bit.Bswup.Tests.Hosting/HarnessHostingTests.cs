using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bit.Bswup.Tests.Hosting.Infrastructure;
using Modes = Bit.Bswup.Tests.Harness.Web.HarnessRenderModes;

namespace Bit.Bswup.Tests.Hosting;

/// <summary>
/// What the server side of a Bswup app has to serve for the worker and the page script to work - checked over
/// HTTP, on every target framework Bit.Bswup ships for. A worker degrades silently: a precached file the host does
/// not serve (or serves with other bytes than its hash) is only a skipped asset under the default lax tolerance,
/// and an app that works online until the day it goes offline.
/// </summary>
[TestClass]
public class HarnessHostingTests
{
    private static readonly string[] _bswupFiles =
        ["bit-bswup.js", "bit-bswup.progress.js", "bit-bswup.sw.js", "bit-bswup.sw-cleanup.js", "bit-bswup.progress.css"];

    [TestMethod]
    [DataRow("/")]
    [DataRow("/standalone/")]
    public async Task Every_Bswup_script_and_stylesheet_is_served_with_its_content_type(string appPath)
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();

        foreach (var file in _bswupFiles)
        {
            using var response = await client.GetAsync($"{appPath}_content/Bit.Bswup/{file}", TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{file} was not served.");
            var expectedValue = file.EndsWith(".css", StringComparison.Ordinal) ? "text/css" : "text/javascript";
            var actualValue = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual(expectedValue, actualValue,
                $"{file} was answered by something other than the static asset.");

            var content = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            if (file.EndsWith(".js", StringComparison.Ordinal))
            {
                Assert.MatchesRegex(new Regex(@"version[""']\]\s*=\s*[""']\d+\.\d+\.\d+"), content, $"{file} declares no version.");
            }
        }
    }

    [TestMethod]
    [DataRow(Modes.WebAssembly, "/")]
    [DataRow(Modes.Server, "/")]
    [DataRow(Modes.WebAssembly, "/standalone/")]
    public async Task Every_asset_the_worker_precaches_is_served_with_the_bytes_its_hash_describes(string mode, string appPath)
    {
        using var client = HarnessHostFactory.Shared(mode).CreateSessionClient();

        var manifest = await PrecacheManifest.LoadAsync(client, appPath);
        Assert.Contains(asset => asset.Url.StartsWith("_framework/", StringComparison.Ordinal), manifest.Precached(), "The manifest lists no framework files.");
        Assert.Contains(asset => asset.Url == "_content/Bit.Bswup/bit-bswup.js", manifest.Precached(), "The manifest does not list the Bswup page script.");

        // The harness writes each session's script-tag options into the standalone app's index.html, so that one file
        // is served with other bytes than the build hashed - by design of the harness, not a deployment problem.
        var problems = appPath == "/standalone/"
            ? await manifest.VerifyServedAsync(client, appPath, @"^index\.html$")
            : await manifest.VerifyServedAsync(client, appPath);

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.WebAssemblyNoPrerender)]
    [DataRow(Modes.Auto)]
    [DataRow(Modes.Server)]
    public async Task The_host_document_leaves_starting_Blazor_to_Bswup(string mode)
    {
        using var client = HarnessHostFactory.Shared(mode).CreateSessionClient();
        var html = await client.GetStringAsync("/", TestContext.CancellationToken);

        var blazor = Regex.Match(html, @"<script src=""_framework/blazor\.web\.js""[^>]*>");
        Assert.IsTrue(blazor.Success, "The document does not load blazor.web.js.");
        Assert.Contains("autostart=\"false\"", blazor.Value);

        var bswup = html.IndexOf("_content/Bit.Bswup/bit-bswup.js", StringComparison.Ordinal);
        Assert.IsGreaterThan(blazor.Index, bswup, "bit-bswup.js has to come after the Blazor script it starts.");
        Assert.IsGreaterThan(bswup, html.IndexOf("_content/Bit.Bswup/bit-bswup.progress.js", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BswupProgress_renders_its_parameters_as_configuration_attributes_and_no_script()
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();

        var defaults = BswupElement(await client.GetStringAsync("/", TestContext.CancellationToken));
        Assert.Contains("data-bit-bswup-config=\"true\"", defaults);
        Assert.Contains("data-bit-bswup-auto-reload=\"true\"", defaults);
        Assert.Contains("data-bit-bswup-show-assets=\"false\"", defaults);
        Assert.Contains("data-bit-bswup-show-on-update=\"true\"", defaults);
        Assert.Contains("data-bit-bswup-app-container=\"#app\"", defaults);
        Assert.Contains("data-bit-bswup-handler=\"harnessBswupHandler\"", defaults);

        await HarnessHostFactory.SetOptionsAsync(client, new { progress = new { autoReload = false, showAssets = true, hideApp = true, autoHide = true, showOnUpdate = false } });
        var html = await client.GetStringAsync("/", TestContext.CancellationToken);
        var configured = BswupElement(html);

        Assert.Contains("data-bit-bswup-auto-reload=\"false\"", configured);
        Assert.Contains("data-bit-bswup-show-assets=\"true\"", configured);
        Assert.Contains("data-bit-bswup-hide-app=\"true\"", configured);
        Assert.Contains("data-bit-bswup-auto-hide=\"true\"", configured);
        Assert.Contains("data-bit-bswup-show-on-update=\"false\"", configured);
        Assert.MatchesRegex(new Regex(@"id=""bit-bswup-assets""[^>]*display:\s*block"), html);

        // The component works under a strict CSP and when rendered interactively because it emits no script.
        Assert.IsFalse(ScriptInsideSplash(html), "BswupProgress emitted an inline script.");
    }

    [TestMethod]
    public async Task The_update_button_is_rendered_outside_the_splash_overlay()
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();
        var html = await client.GetStringAsync("/", TestContext.CancellationToken);

        var splashEnd = html.IndexOf("id=\"bit-bswup-error-retry\"", StringComparison.Ordinal);
        var button = Regex.Match(html, @"<button id=""bit-bswup-reload""[^>]*>");
        Assert.IsTrue(button.Success, "The update button is not rendered.");
        Assert.Contains("type=\"button\"", button.Value);
        Assert.Contains("display: none", button.Value);

        // Everything between the retry button (the splash's last element) and the update button closes the splash.
        var between = html[splashEnd..button.Index];
        Assert.HasCount(3, Regex.Matches(between, "</div>"), "The update button is nested inside #bit-bswup.");
        Assert.Contains("id=\"bit-bswup-reload-status\" role=\"status\"", html);
    }

    [TestMethod]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    [DataRow(Modes.Server)]
    public async Task The_app_shell_the_worker_downloads_is_not_prerendered(string mode)
    {
        using var client = HarnessHostFactory.Shared(mode).CreateSessionClient();

        // The status line is rendered by the harness root itself, so it is in every prerendered document.
        Assert.Contains("id=\"status\"", await client.GetStringAsync("/", TestContext.CancellationToken), "The document is not prerendered to begin with.");

        var shell = await client.GetStringAsync("/?no-prerender=true", TestContext.CancellationToken);
        Assert.IsFalse(shell.Contains("id=\"status\"", StringComparison.Ordinal), "The noPrerenderQuery document carries one request's prerendered output.");
        Assert.Contains("<!--Blazor:", shell);
    }

    [TestMethod]
    public async Task The_standalone_app_answers_its_routes_with_its_document_configured_for_its_sub_path()
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();

        using var response = await client.GetAsync("/standalone/counter", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.Contains("<base href=\"/standalone/\" />", html);
        Assert.MatchesRegex(new Regex(@"<script src=""_framework/blazor\.webassembly\.js"" autostart=""false"">"), html);
        Assert.MatchesRegex(new Regex(@"<script src=""_content/Bit\.Bswup/bit-bswup\.js"" scope=""/standalone/"">"), html);
    }

    [TestMethod]
    public async Task The_worker_script_carries_the_session_settings_and_changes_with_every_release()
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();

        await HarnessHostFactory.SetOptionsAsync(client, new { workerSettings = new { errorTolerance = "strict", prohibitedUrls = new[] { new Dictionary<string, string> { ["$regex"] = @"\/admin\/", ["flags"] = "i" } } } });
        var first = await client.GetStringAsync("/service-worker.js", TestContext.CancellationToken);

        Assert.Contains("self.errorTolerance = \"strict\";", first);
        Assert.Contains("self.prohibitedUrls = [new RegExp(\"\\\\/admin\\\\/\", \"i\")];", first);
        Assert.EndsWith("self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');", first.TrimEnd());

        using (var publish = await client.PutAsJsonAsync("/_harness/version", new { version = 2 }, cancellationToken: TestContext.CancellationToken)) publish.EnsureSuccessStatusCode();
        Assert.AreNotEqual(first, await client.GetStringAsync("/service-worker.js", TestContext.CancellationToken), "A new release left the worker script byte-identical, so no browser would find the update.");
        Assert.EndsWith("-h2", (await PrecacheManifest.LoadAsync(client, "/")).Version);

        await HarnessHostFactory.SetOptionsAsync(client, new { worker = "cleanup" });
        Assert.EndsWith("self.importScripts('_content/Bit.Bswup/bit-bswup.sw-cleanup.js');", (await client.GetStringAsync("/service-worker.js", TestContext.CancellationToken)).TrimEnd());
    }

    private static string BswupElement(string html)
    {
        var match = Regex.Match(html, @"<div id=""bit-bswup""[^>]*>");
        Assert.IsTrue(match.Success, "BswupProgress is not rendered.");
        return match.Value;
    }

    private static bool ScriptInsideSplash(string html)
    {
        var start = html.IndexOf("<div id=\"bit-bswup\"", StringComparison.Ordinal);
        var end = html.IndexOf("<button id=\"bit-bswup-reload\"", StringComparison.Ordinal);
        return start >= 0 && end > start && html[start..end].Contains("<script", StringComparison.Ordinal);
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
