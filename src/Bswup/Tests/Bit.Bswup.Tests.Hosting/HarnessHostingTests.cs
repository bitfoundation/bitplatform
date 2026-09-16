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
            using var response = await client.GetAsync($"{appPath}_content/Bit.Bswup/{file}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{file} was not served.");
            Assert.AreEqual(file.EndsWith(".css", StringComparison.Ordinal) ? "text/css" : "text/javascript", response.Content.Headers.ContentType?.MediaType,
                $"{file} was answered by something other than the static asset.");

            var content = await response.Content.ReadAsStringAsync();
            if (file.EndsWith(".js", StringComparison.Ordinal))
            {
                StringAssert.Matches(content, new Regex(@"version[""']\]\s*=\s*[""']\d+\.\d+\.\d+"), $"{file} declares no version.");
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
        Assert.IsTrue(manifest.Precached().Any(asset => asset.Url.StartsWith("_framework/", StringComparison.Ordinal)), "The manifest lists no framework files.");
        Assert.IsTrue(manifest.Precached().Any(asset => asset.Url == "_content/Bit.Bswup/bit-bswup.js"), "The manifest does not list the Bswup page script.");

        // The harness writes each session's script-tag options into the standalone app's index.html, so that one file
        // is served with other bytes than the build hashed - by design of the harness, not a deployment problem.
        var problems = appPath == "/standalone/"
            ? await manifest.VerifyServedAsync(client, appPath, @"^index\.html$")
            : await manifest.VerifyServedAsync(client, appPath);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.WebAssemblyNoPrerender)]
    [DataRow(Modes.Auto)]
    [DataRow(Modes.Server)]
    public async Task The_host_document_leaves_starting_Blazor_to_Bswup(string mode)
    {
        using var client = HarnessHostFactory.Shared(mode).CreateSessionClient();
        var html = await client.GetStringAsync("/");

        var blazor = Regex.Match(html, @"<script src=""_framework/blazor\.web\.js""[^>]*>");
        Assert.IsTrue(blazor.Success, "The document does not load blazor.web.js.");
        StringAssert.Contains(blazor.Value, "autostart=\"false\"");

        var bswup = html.IndexOf("_content/Bit.Bswup/bit-bswup.js", StringComparison.Ordinal);
        Assert.IsTrue(bswup > blazor.Index, "bit-bswup.js has to come after the Blazor script it starts.");
        Assert.IsTrue(html.IndexOf("_content/Bit.Bswup/bit-bswup.progress.js", StringComparison.Ordinal) > bswup);
    }

    [TestMethod]
    public async Task BswupProgress_renders_its_parameters_as_configuration_attributes_and_no_script()
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();

        var defaults = BswupElement(await client.GetStringAsync("/"));
        StringAssert.Contains(defaults, "data-bit-bswup-config=\"true\"");
        StringAssert.Contains(defaults, "data-bit-bswup-auto-reload=\"true\"");
        StringAssert.Contains(defaults, "data-bit-bswup-show-assets=\"false\"");
        StringAssert.Contains(defaults, "data-bit-bswup-show-on-update=\"true\"");
        StringAssert.Contains(defaults, "data-bit-bswup-app-container=\"#app\"");
        StringAssert.Contains(defaults, "data-bit-bswup-handler=\"harnessBswupHandler\"");

        await HarnessHostFactory.SetOptionsAsync(client, new { progress = new { autoReload = false, showAssets = true, hideApp = true, autoHide = true, showOnUpdate = false } });
        var html = await client.GetStringAsync("/");
        var configured = BswupElement(html);

        StringAssert.Contains(configured, "data-bit-bswup-auto-reload=\"false\"");
        StringAssert.Contains(configured, "data-bit-bswup-show-assets=\"true\"");
        StringAssert.Contains(configured, "data-bit-bswup-hide-app=\"true\"");
        StringAssert.Contains(configured, "data-bit-bswup-auto-hide=\"true\"");
        StringAssert.Contains(configured, "data-bit-bswup-show-on-update=\"false\"");
        StringAssert.Matches(html, new Regex(@"id=""bit-bswup-assets""[^>]*display:\s*block"));

        // The component works under a strict CSP and when rendered interactively because it emits no script.
        Assert.IsFalse(ScriptInsideSplash(html), "BswupProgress emitted an inline script.");
    }

    [TestMethod]
    public async Task The_update_button_is_rendered_outside_the_splash_overlay()
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();
        var html = await client.GetStringAsync("/");

        var splashEnd = html.IndexOf("id=\"bit-bswup-error-retry\"", StringComparison.Ordinal);
        var button = Regex.Match(html, @"<button id=""bit-bswup-reload""[^>]*>");
        Assert.IsTrue(button.Success, "The update button is not rendered.");
        StringAssert.Contains(button.Value, "type=\"button\"");
        StringAssert.Contains(button.Value, "display: none");

        // Everything between the retry button (the splash's last element) and the update button closes the splash.
        var between = html[splashEnd..button.Index];
        Assert.AreEqual(3, Regex.Matches(between, "</div>").Count, "The update button is nested inside #bit-bswup.");
        StringAssert.Contains(html, "id=\"bit-bswup-reload-status\" role=\"status\"");
    }

    [TestMethod]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    [DataRow(Modes.Server)]
    public async Task The_app_shell_the_worker_downloads_is_not_prerendered(string mode)
    {
        using var client = HarnessHostFactory.Shared(mode).CreateSessionClient();

        // The status line is rendered by the harness root itself, so it is in every prerendered document.
        StringAssert.Contains(await client.GetStringAsync("/"), "id=\"status\"", "The document is not prerendered to begin with.");

        var shell = await client.GetStringAsync("/?no-prerender=true");
        Assert.IsFalse(shell.Contains("id=\"status\"", StringComparison.Ordinal), "The noPrerenderQuery document carries one request's prerendered output.");
        StringAssert.Contains(shell, "<!--Blazor:");
    }

    [TestMethod]
    public async Task The_standalone_app_answers_its_routes_with_its_document_configured_for_its_sub_path()
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();

        using var response = await client.GetAsync("/standalone/counter");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        StringAssert.Contains(html, "<base href=\"/standalone/\" />");
        StringAssert.Matches(html, new Regex(@"<script src=""_framework/blazor\.webassembly\.js"" autostart=""false"">"));
        StringAssert.Matches(html, new Regex(@"<script src=""_content/Bit\.Bswup/bit-bswup\.js"" scope=""/standalone/"">"));
    }

    [TestMethod]
    public async Task The_worker_script_carries_the_session_settings_and_changes_with_every_release()
    {
        using var client = HarnessHostFactory.Shared(Modes.WebAssembly).CreateSessionClient();

        await HarnessHostFactory.SetOptionsAsync(client, new { workerSettings = new { errorTolerance = "strict", prohibitedUrls = new[] { new Dictionary<string, string> { ["$regex"] = @"\/admin\/", ["flags"] = "i" } } } });
        var first = await client.GetStringAsync("/service-worker.js");

        StringAssert.Contains(first, "self.errorTolerance = \"strict\";");
        StringAssert.Contains(first, "self.prohibitedUrls = [new RegExp(\"\\\\/admin\\\\/\", \"i\")];");
        StringAssert.EndsWith(first.TrimEnd(), "self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');");

        using (var publish = await client.PutAsJsonAsync("/_harness/version", new { version = 2 })) publish.EnsureSuccessStatusCode();
        Assert.AreNotEqual(first, await client.GetStringAsync("/service-worker.js"), "A new release left the worker script byte-identical, so no browser would find the update.");
        StringAssert.EndsWith((await PrecacheManifest.LoadAsync(client, "/")).Version, "-h2");

        await HarnessHostFactory.SetOptionsAsync(client, new { worker = "cleanup" });
        StringAssert.EndsWith((await client.GetStringAsync("/service-worker.js")).TrimEnd(), "self.importScripts('_content/Bit.Bswup/bit-bswup.sw-cleanup.js');");
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
}
