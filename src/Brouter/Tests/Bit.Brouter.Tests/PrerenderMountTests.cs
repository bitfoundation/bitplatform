using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Pins the initial mount against the REAL static prerenderer (<see cref="HtmlRenderer"/> - the
/// same renderer that produces prerendered HTML in a Blazor Web App), rather than bUnit's
/// interactive renderer. The distinction matters: the prerenderer serializes its HTML as soon as
/// <c>WaitForQuiescenceAsync</c> completes, and that only awaits tasks returned from component
/// lifecycle methods. Work posted to the dispatcher outside a lifecycle task simply never lands in
/// the output, so a mount that "settles eventually" under bUnit can still prerender as an empty
/// router. <c>InitialMountTests</c> covers the interactive side; this covers what the server emits.
/// </summary>
[TestClass]
public class PrerenderMountTests
{
    private sealed class PrerenderNavigationManager : NavigationManager
    {
        public PrerenderNavigationManager(string uri) => Initialize("http://localhost/", uri);

        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    private sealed class NoopNavigationInterception : INavigationInterception
    {
        public Task EnableNavigationInterceptionAsync() => Task.CompletedTask;
    }

    // Mirrors real static SSR, where JS interop is unavailable: any call must fail rather than
    // quietly succeed, so a regression that starts depending on interop during prerender is caught.
    private sealed class UnavailableJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException("JavaScript interop is not available during prerendering.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException("JavaScript interop is not available during prerendering.");
    }

    private static async Task<string> PrerenderAsync<THost>(string url) where THost : IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBitBrouterServices();
        services.AddScoped<NavigationManager>(_ => new PrerenderNavigationManager(url));
        services.AddScoped<INavigationInterception, NoopNavigationInterception>();
        services.AddScoped<IJSRuntime, UnavailableJSRuntime>();
        await using var provider = services.BuildServiceProvider();

        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<THost>();
            return output.ToHtmlString();
        });
    }

    [TestMethod]
    public async Task Prerender_emits_the_matched_flat_route()
    {
        var html = await PrerenderAsync<InitialMountHost>("http://localhost/home");

        StringAssert.Contains(html, "im-content");
    }

    [TestMethod]
    public async Task Prerender_emits_the_matched_nested_route_chain()
    {
        var html = await PrerenderAsync<InitialMountNestedHost>("http://localhost/docs/guide/intro");

        StringAssert.Contains(html, "im-content");
    }

    [TestMethod]
    public async Task Prerender_emits_a_route_declared_behind_a_wrapper_component()
    {
        var html = await PrerenderAsync<InitialMountWrappedHost>("http://localhost/home");

        StringAssert.Contains(html, "im-content");
    }

    [TestMethod]
    public async Task Prerender_emits_routes_behind_a_wrapper_chain_deeper_than_the_sentinel_tolerance()
    {
        // Five stacked wrappers out-wait the boot sentinel's quiet rounds, so the initial navigation
        // fires before these routes registered and only the late-registration rematch can correct
        // it. That correction has to be owned by a component lifecycle (BrouterRematchRunner) for
        // the prerenderer to wait for it - fired detached, this prerenders as an empty router.
        var html = await PrerenderAsync<InitialMountDeepWrappedHost>("http://localhost/home");

        StringAssert.Contains(html, "im-content");
    }

    [TestMethod]
    public async Task Prerender_emits_nothing_routed_when_no_route_matches()
    {
        var html = await PrerenderAsync<InitialMountHost>("http://localhost/no-such-page");

        Assert.IsFalse(html.Contains("im-content"), "an unmatched URL must not emit routed content");
        Assert.IsFalse(html.Contains("im-other"), "an unmatched URL must not emit routed content");
    }
}
