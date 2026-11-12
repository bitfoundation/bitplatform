using System.Net;
using System.Text.RegularExpressions;
using Bit.Brouter.Tests.Hosting.Infrastructure;
using Modes = Bit.Brouter.Tests.Harness.Web.HarnessRenderModes;

namespace Bit.Brouter.Tests.Hosting;

/// <summary>
/// What each render mode's server response has to contain. bUnit renders in an interactive renderer
/// with no HTTP response at all, and even the in-process HtmlRenderer tests leave RendererInfo
/// unpopulated - which makes Brouter treat them as interactive and skip its static-rendering-only
/// branches (the 404 propagation among them). Only a real host exercises those.
/// </summary>
[TestClass]
public class HarnessHostingTests
{
    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task A_matched_deep_link_is_rendered_into_the_first_response(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/items/42", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"page-item\" data-item-id=\"42\"", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    [DataRow(Modes.ServerNoPrerender)]
    [DataRow(Modes.WebAssemblyNoPrerender)]
    [DataRow(Modes.AutoNoPrerender)]
    public async Task A_mode_without_prerendering_sends_only_the_interactive_component_marker(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/items/42", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.IsFalse(html.Contains("id=\"page-item\"", StringComparison.Ordinal), "A non-prerendered mode rendered the route on the server.");
        Assert.Contains("<!--Blazor:", html);
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task An_unmatched_url_is_answered_with_the_not_found_status_and_the_not_found_content(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/nope/deeper", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        // The status alone is not enough: the user still has to see the fallback, and an interactive
        // mode still needs its script to boot on this page.
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.Contains("id=\"not-found\" data-path=\"/nope/deeper\"", html, $"The {(int)response.StatusCode} response did not carry Brouter's NotFound content ({html.Length} characters).");
        Assert.Contains("_framework/blazor.web.js", html);
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task NavigationManager_NotFound_from_a_page_is_answered_with_404_and_the_not_found_content(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/missing/0", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("id=\"not-found\" data-path=\"/missing/0\"", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task A_guard_redirect_during_static_rendering_redirects_the_request(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateNonRedirectingClient().GetAsync("/guarded", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        var actualValue = response.Headers.Location?.AbsolutePath;
        Assert.AreEqual("/denied", actualValue);
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task A_RedirectTo_route_redirects_the_request(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateNonRedirectingClient().GetAsync("/old-about", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        var actualValue = response.Headers.Location?.AbsolutePath;
        Assert.AreEqual("/about", actualValue);
    }

    [TestMethod]
    [DataRow(Modes.ServerNoPrerender)]
    [DataRow(Modes.WebAssemblyNoPrerender)]
    public async Task A_guarded_url_is_left_to_the_client_when_nothing_is_prerendered(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateNonRedirectingClient().GetAsync("/guarded", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task The_loader_runs_once_in_the_request_that_renders_its_page(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/data", TestContext.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        var dataScope = RenderedHtml.TextOf(html, "data-scope");
        Assert.IsFalse(string.IsNullOrEmpty(dataScope), "The response carried no loader data.");
        Assert.AreEqual(RenderedHtml.TextOf(html, "render-scope"), dataScope);
        Assert.AreEqual("1", RenderedHtml.TextOf(html, "data-run"));
    }

    [TestMethod]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task The_loader_result_is_persisted_for_the_WebAssembly_runtime_to_restore(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/data", TestContext.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        var state = RenderedHtml.WebAssemblyPersistedState(html);

        Assert.IsTrue(state.TryGetValue("Bit.Brouter|/data||0", out var persisted),
            $"PersistLoaderState wrote nothing for /data. Persisted keys: {string.Join(", ", state.Keys)}");
        Assert.Contains("Bit.Brouter.Tests.Harness.HarnessData", persisted);
        Assert.Contains(RenderedHtml.TextOf(html, "data-scope")!, persisted);
    }

    [TestMethod]
    public async Task Static_rendering_persists_no_loader_state()
    {
        using var response = await HarnessHostFactory.Shared(Modes.Ssr).CreateClient().GetAsync("/data", TestContext.CancellationToken);

        var state = RenderedHtml.WebAssemblyPersistedState(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        Assert.DoesNotContain(k => k.StartsWith("Bit.Brouter|", StringComparison.Ordinal), state.Keys,
            "Loader state was persisted for a page no interactive runtime will ever pick up.");
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task A_failing_loader_renders_the_route_error_content(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/broken", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"route-error\">harness loader failure<", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Auto)]
    public async Task The_script_module_is_served_as_javascript_with_every_export_Brouter_imports(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/_content/Bit.Brouter/bit-brouter.js", TestContext.CancellationToken);

        // Brouter swallows a failed import, so a missing module never surfaces as an error: every
        // JS-backed feature just stops working. Only the response shows it.
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var actualValue = response.Content.Headers.ContentType?.MediaType;
        Assert.AreEqual("text/javascript", actualValue,
            "The module URL was answered by something other than the static asset (the app's catch-all page?).");

        var exports = ExportedNames(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        foreach (var export in new[] { "wireConditionalPreventDefault", "wirePreload", "beginViewTransition", "completeViewTransition", "setConfirmExternalNavigation", "saveScrollPosition", "applyNavigationEffects" })
        {
            Assert.Contains(export, exports, $"bit-brouter.js does not export {export}.");
        }
    }

    // The names a module's export declarations make public: `export function name(` as tsc emits them
    // (Debug), and `export { local as name, name }` as esbuild's minified output lists them (Release).
    // A name merely appearing somewhere in the script - in a comment, or as part of a longer
    // identifier - does not count.
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
                // "local as exported" or just "name": the public name is the last token.
                var tokens = specifier.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 0) names.Add(tokens[^1]);
            }
        }

        return names;
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
