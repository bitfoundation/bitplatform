using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class LoaderCacheTests : BunitTestContext
{
    [TestMethod]
    public void Fresh_cache_hit_skips_the_loader_on_return_navigation()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/cached");

        var cut = RenderComponent<CacheHost>(p => p.Add(x => x.StaleTime, TimeSpan.FromMinutes(5)));
        cut.WaitForAssertion(() => Assert.AreEqual("cached-1", cut.Find("[data-testid=payload]").TextContent));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=other]"));

        cut.InvokeAsync(() => brouter.Navigate("/cached"));

        cut.WaitForAssertion(() =>
        {
            // Rendered from cache: same payload, loader did not run again.
            Assert.AreEqual("cached-1", cut.Find("[data-testid=payload]").TextContent);
            Assert.AreEqual(1, cut.Instance.CachedLoaderRuns);
        });
    }

    [TestMethod]
    public void Stale_entry_renders_immediately_then_refreshes_in_background()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/cached");

        // StaleTime zero: every cached entry is immediately stale -> pure stale-while-revalidate.
        var cut = RenderComponent<CacheHost>(p => p.Add(x => x.StaleTime, TimeSpan.Zero));
        cut.WaitForAssertion(() => Assert.AreEqual("cached-1", cut.Find("[data-testid=payload]").TextContent));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=other]"));

        cut.InvokeAsync(() => brouter.Navigate("/cached"));

        // The background revalidation re-runs the loader and re-renders with fresh data.
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("cached-2", cut.Find("[data-testid=payload]").TextContent);
            Assert.AreEqual(2, cut.Instance.CachedLoaderRuns);
        });
    }

    [TestMethod]
    public void Blocking_mode_treats_stale_as_a_miss()
    {
        Services.Configure<BrouterOptions>(o => o.StaleReloadMode = BrouterStaleReloadMode.Blocking);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/cached");

        var cut = RenderComponent<CacheHost>(p => p.Add(x => x.StaleTime, TimeSpan.Zero));
        cut.WaitForAssertion(() => Assert.AreEqual("cached-1", cut.Find("[data-testid=payload]").TextContent));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=other]"));

        cut.InvokeAsync(() => brouter.Navigate("/cached"));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("cached-2", cut.Find("[data-testid=payload]").TextContent);
            Assert.AreEqual(2, cut.Instance.CachedLoaderRuns);
        });
    }

    [TestMethod]
    public void No_StaleTime_means_no_caching_loader_reruns_every_navigation()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/cached");

        var cut = RenderComponent<CacheHost>(); // StaleTime null
        cut.WaitForAssertion(() => Assert.AreEqual("cached-1", cut.Find("[data-testid=payload]").TextContent));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=other]"));
        cut.InvokeAsync(() => brouter.Navigate("/cached"));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("cached-2", cut.Find("[data-testid=payload]").TextContent);
            Assert.AreEqual(2, cut.Instance.CachedLoaderRuns);
        });
    }

    [TestMethod]
    public void ClearLoaderCache_forces_a_fresh_load()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/cached");

        var cut = RenderComponent<CacheHost>(p => p.Add(x => x.StaleTime, TimeSpan.FromMinutes(5)));
        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.Instance.CachedLoaderRuns));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=other]"));

        brouter.ClearLoaderCache();
        cut.InvokeAsync(() => brouter.Navigate("/cached"));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("cached-2", cut.Find("[data-testid=payload]").TextContent);
            Assert.AreEqual(2, cut.Instance.CachedLoaderRuns);
        });
    }

    [TestMethod]
    public void PreloadAsync_warms_the_cache_so_navigation_skips_the_loader()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/other");

        var cut = RenderComponent<CacheHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=other]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.PreloadAsync("/uncached").AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.Instance.UncachedLoaderRuns);
            Assert.IsTrue(cut.Instance.LastWasPreload!.Value);
        });

        cut.InvokeAsync(() => brouter.Navigate("/uncached"));

        cut.WaitForAssertion(() =>
        {
            // Rendered from the preloaded entry: the loader did not run a second time even though
            // the route itself declares no StaleTime (PreloadStaleTime covers preload entries).
            Assert.AreEqual("uncached-1", cut.Find("[data-testid=payload]").TextContent);
            Assert.AreEqual(1, cut.Instance.UncachedLoaderRuns);
        });
    }

    [TestMethod]
    public void Render_mode_link_preloads_without_any_navigation()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/other");

        var cut = RenderComponent<CacheHost>(p => p.Add(x => x.ShowRenderPreloadLink, true));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.Instance.UncachedLoaderRuns);
            Assert.IsTrue(cut.Instance.LastWasPreload!.Value);
            // Still on /other - preloading never navigates.
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
        });
    }
}
