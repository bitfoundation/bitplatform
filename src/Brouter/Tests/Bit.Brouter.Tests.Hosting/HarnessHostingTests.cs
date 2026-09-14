using System.Net;
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
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/items/42");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "id=\"page-item\" data-item-id=\"42\"");
    }

    [TestMethod]
    [DataRow(Modes.ServerNoPrerender)]
    [DataRow(Modes.WebAssemblyNoPrerender)]
    [DataRow(Modes.AutoNoPrerender)]
    public async Task A_mode_without_prerendering_sends_only_the_interactive_component_marker(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/items/42");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(html.Contains("id=\"page-item\"", StringComparison.Ordinal), "A non-prerendered mode rendered the route on the server.");
        StringAssert.Contains(html, "<!--Blazor:");
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task An_unmatched_url_is_answered_with_the_not_found_status_and_the_not_found_content(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/nope/deeper");

#if NET10_0_OR_GREATER
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
#else
        // Before .NET 10 there is no NavigationManager.NotFound, so no way for a router to set the status.
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
#endif

        // The status alone is not enough: the user still has to see the fallback, and an interactive
        // mode still needs its script to boot on this page.
        var html = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(html, "id=\"not-found\" data-path=\"/nope/deeper\"", $"The {(int)response.StatusCode} response did not carry Brouter's NotFound content ({html.Length} characters).");
        StringAssert.Contains(html, "_framework/blazor.web.js");
    }

#if NET10_0_OR_GREATER
    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task NavigationManager_NotFound_from_a_page_is_answered_with_404_and_the_not_found_content(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/missing/0");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "id=\"not-found\" data-path=\"/missing/0\"");
    }
#endif

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task A_guard_redirect_during_static_rendering_redirects_the_request(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateNonRedirectingClient().GetAsync("/guarded");

        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.AreEqual("/denied", response.Headers.Location?.AbsolutePath);
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task A_RedirectTo_route_redirects_the_request(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateNonRedirectingClient().GetAsync("/old-about");

        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.AreEqual("/about", response.Headers.Location?.AbsolutePath);
    }

    [TestMethod]
    [DataRow(Modes.ServerNoPrerender)]
    [DataRow(Modes.WebAssemblyNoPrerender)]
    public async Task A_guarded_url_is_left_to_the_client_when_nothing_is_prerendered(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateNonRedirectingClient().GetAsync("/guarded");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task The_loader_runs_once_in_the_request_that_renders_its_page(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/data");
        var html = await response.Content.ReadAsStringAsync();

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
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/data");
        var html = await response.Content.ReadAsStringAsync();

        var state = RenderedHtml.WebAssemblyPersistedState(html);

        Assert.IsTrue(state.TryGetValue("Bit.Brouter|/data||0", out var persisted),
            $"PersistLoaderState wrote nothing for /data. Persisted keys: {string.Join(", ", state.Keys)}");
        StringAssert.Contains(persisted, "Bit.Brouter.Tests.Harness.HarnessData");
        StringAssert.Contains(persisted, RenderedHtml.TextOf(html, "data-scope")!);
    }

    [TestMethod]
    public async Task Static_rendering_persists_no_loader_state()
    {
        using var response = await HarnessHostFactory.Shared(Modes.Ssr).CreateClient().GetAsync("/data");

        var state = RenderedHtml.WebAssemblyPersistedState(await response.Content.ReadAsStringAsync());

        Assert.IsFalse(state.Keys.Any(k => k.StartsWith("Bit.Brouter|", StringComparison.Ordinal)),
            "Loader state was persisted for a page no interactive runtime will ever pick up.");
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task A_failing_loader_renders_the_route_error_content(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/broken");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "id=\"route-error\">harness loader failure<");
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Auto)]
    public async Task The_script_module_is_served_as_javascript_with_every_export_Brouter_imports(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/_content/Bit.Brouter/bit-brouter.js");

        // Brouter swallows a failed import, so a missing module never surfaces as an error: every
        // JS-backed feature just stops working. Only the response shows it.
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/javascript", response.Content.Headers.ContentType?.MediaType,
            "The module URL was answered by something other than the static asset (the app's catch-all page?).");

        var script = await response.Content.ReadAsStringAsync();
        foreach (var export in new[] { "wireConditionalPreventDefault", "wirePreload", "beginViewTransition", "completeViewTransition", "setConfirmExternalNavigation", "saveScrollPosition", "applyNavigationEffects" })
        {
            StringAssert.Contains(script, export, $"bit-brouter.js does not export {export}.");
        }
    }
}
