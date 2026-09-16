using System;
using System.Collections.Generic;
using System.IO;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Assets;

/// <summary>
/// Link and Script carry the versioned path over to the render that follows prerendering through a single
/// entry of the page's persistent state, keyed by the path alone. Two tags pointing at the same asset - or
/// simply two tags on the same page - must both end up in that one entry: a key per tag would make the
/// second registration collide with the first.
/// </summary>
[TestClass]
public class BitAssetVersionTests : BunitTestContext
{
    private const string StateKey = "BitAssetVersion";
    private const string CssPath = "/styles/app.css";
    private const string JsPath = "/scripts/app.js";

    private string webRoot = default!;

    [TestInitialize]
    public void SetupWebRoot()
    {
        webRoot = Path.Combine(Path.GetTempPath(), $"bit-assets-{Guid.NewGuid():N}");

        Directory.CreateDirectory(Path.Combine(webRoot, "styles"));
        Directory.CreateDirectory(Path.Combine(webRoot, "scripts"));

        File.WriteAllText(Path.Combine(webRoot, "styles", "app.css"), ".a{color:red}");
        File.WriteAllText(Path.Combine(webRoot, "scripts", "app.js"), "console.log(1)");

        Services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment(webRoot));
    }

    [TestCleanup]
    public void CleanupWebRoot()
    {
        // The base class initializes first, so a failure there leaves this one's web root unset.
        if (webRoot is null) return;

        try
        {
            Directory.Delete(webRoot, recursive: true);
        }
        catch (IOException) { }
    }

    [TestMethod]
    public void DuplicateLinksShouldRenderTheSameVersionedHref()
    {
        Context.AddBunitPersistentComponentState();

        var first = RenderLink(CssPath);
        var second = RenderLink(CssPath);

        var href = Href(first);

        Assert.IsTrue(href.StartsWith($"{CssPath}?v=sha256-", StringComparison.Ordinal), href);
        Assert.AreEqual(href, Href(second));
    }

    [TestMethod]
    public void DuplicateScriptsShouldRenderTheSameVersionedSrc()
    {
        Context.AddBunitPersistentComponentState();

        var first = RenderScript(JsPath);
        var second = RenderScript(JsPath);

        var src = Src(first);

        Assert.IsTrue(src.StartsWith($"{JsPath}?v=sha256-", StringComparison.Ordinal), src);
        Assert.AreEqual(src, Src(second));
    }

    [TestMethod]
    public void DuplicateLinksAndScriptsShouldCarryOverInASingleEntry()
    {
        var state = Context.AddBunitPersistentComponentState();

        var link = RenderLink(CssPath);
        RenderLink(CssPath);
        var script = RenderScript(JsPath);
        RenderScript(JsPath);

        // The second tag of an asset would throw here when each tag persisted under a key of its own.
        state.TriggerOnPersisting();

        Assert.IsTrue(state.TryTake<Dictionary<string, string>>(StateKey, out var persisted));
        Assert.AreEqual(2, persisted!.Count);
        Assert.AreEqual(Href(link), persisted[CssPath]);
        Assert.AreEqual(Src(script), persisted[JsPath]);
    }

    [TestMethod]
    public void DuplicateLinksAndScriptsShouldAllReadTheCarriedOverPaths()
    {
        var state = Context.AddBunitPersistentComponentState();

        // What a prerendering pass carried over: the whole page's assets under the one key.
        state.Persist(StateKey, new Dictionary<string, string>
        {
            [CssPath] = $"{CssPath}?v=sha256-restored-css",
            [JsPath] = $"{JsPath}?v=sha256-restored-js",
        });

        // The first tag takes the entry away from the state, so the ones after it are the regression:
        // they answer from what the first one restored rather than hashing the file again.
        Assert.AreEqual($"{CssPath}?v=sha256-restored-css", Href(RenderLink(CssPath)));
        Assert.AreEqual($"{CssPath}?v=sha256-restored-css", Href(RenderLink(CssPath)));
        Assert.AreEqual($"{JsPath}?v=sha256-restored-js", Src(RenderScript(JsPath)));
        Assert.AreEqual($"{JsPath}?v=sha256-restored-js", Src(RenderScript(JsPath)));
    }

    [TestMethod]
    public void ARefilledEntryShouldWinOverWhatAnEarlierOneRestored()
    {
        var state = Context.AddBunitPersistentComponentState();

        state.Persist(StateKey, new Dictionary<string, string> { [CssPath] = $"{CssPath}?v=sha256-restored-css" });

        Assert.AreEqual($"{CssPath}?v=sha256-restored-css", Href(RenderLink(CssPath)));

        // What an enhanced navigation refills the state with: the same path, hashed again after the file
        // changed. A tag rendering now must take that entry rather than answer from the first one's value.
        state.Persist(StateKey, new Dictionary<string, string> { [CssPath] = $"{CssPath}?v=sha256-refreshed-css" });

        Assert.AreEqual($"{CssPath}?v=sha256-refreshed-css", Href(RenderLink(CssPath)));
        Assert.AreEqual($"{CssPath}?v=sha256-refreshed-css", Href(RenderLink(CssPath)));
    }

    private IRenderedComponent<Link> RenderLink(string href)
    {
        return RenderComponent<Link>(parameters => Interactive(parameters.Add(p => p.Href, href)));
    }

    private IRenderedComponent<Script> RenderScript(string src)
    {
        return RenderComponent<Script>(parameters => Interactive(parameters.Add(p => p.Src, src)));
    }

    // The components only carry their path over where they render again after prerendering, which from
    // net9.0 on is what the assigned render mode says. On net8.0 the framework does not say, and they
    // carry it over from every render, so there is no mode to assign there - nor a bUnit API to assign it.
    private static ComponentParameterCollectionBuilder<TComponent> Interactive<TComponent>(
        ComponentParameterCollectionBuilder<TComponent> parameters)
        where TComponent : IComponent
    {
#if NET9_0_OR_GREATER
        parameters.SetAssignedRenderMode(Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveAuto);
#endif

        return parameters;
    }

    private static string Href(IRenderedComponent<Link> component) => component.Find("link").GetAttribute("href")!;

    private static string Src(IRenderedComponent<Script> component) => component.Find("script").GetAttribute("src")!;



    private sealed class TestWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRootPath;
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(webRootPath);
        public string ContentRootPath { get; set; } = webRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(webRootPath);
        public string ApplicationName { get; set; } = nameof(BitAssetVersionTests);
        public string EnvironmentName { get; set; } = "Test";
    }
}
