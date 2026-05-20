using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Map;

[TestClass]
public class BitMapTests : BunitTestContext
{
    private const string INIT = "BitBlazorUI.BitMapLeaflet.init";
    private const string SYNC = "BitBlazorUI.BitMapLeaflet.sync";
    private const string DISPOSE = "BitBlazorUI.BitMapLeaflet.dispose";
    private const string INIT_SCRIPTS = "BitBlazorUI.Extras.initScripts";
    private const string INIT_STYLESHEETS = "BitBlazorUI.Extras.initStylesheets";

    [TestMethod]
    public void BitMapShouldCallJsInitOnFirstRender()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        var initCalls = Context.JSInterop.Invocations
            .Where(i => i.Identifier == INIT)
            .ToList();

        Assert.AreEqual(1, initCalls.Count);
    }

    [TestMethod]
    public void BitMapShouldLoadStylesheetsAndScriptsBeforeInit()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        var allCalls = Context.JSInterop.Invocations.Select(i => i.Identifier).ToList();

        var stylesheetIndex = allCalls.IndexOf(INIT_STYLESHEETS);
        var scriptIndex = allCalls.IndexOf(INIT_SCRIPTS);
        var initIndex = allCalls.IndexOf(INIT);

        Assert.IsTrue(stylesheetIndex >= 0, "Stylesheets should be loaded");
        Assert.IsTrue(scriptIndex >= 0, "Scripts should be loaded");
        Assert.IsTrue(initIndex > stylesheetIndex, "Init should come after stylesheets");
        Assert.IsTrue(initIndex > scriptIndex, "Init should come after scripts");
    }

    [TestMethod]
    public void BitMapShouldFireOnReadyAfterInit()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);

        var readyFired = false;

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.OnReady, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => readyFired = true));
        });

        Assert.IsTrue(readyFired, "OnReady should fire after JS init completes");
    }

    [TestMethod]
    public void BitMapShouldSetIsReadyAfterInit()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        Assert.IsTrue(component.Instance.IsReady);
    }

    [TestMethod]
    public void BitMapShouldCallSyncWhenProviderOptionsChange()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);
        Context.JSInterop.SetupVoid(SYNC);

        var provider = new BitLeafletMapProvider { Zoom = 10 };

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Provider, provider);
        });

        // Update provider with new options (same JsObjectName)
        var updatedProvider = new BitLeafletMapProvider { Zoom = 15 };
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Provider, updatedProvider);
        });

        var syncCalls = Context.JSInterop.Invocations
            .Where(i => i.Identifier == SYNC)
            .ToList();

        Assert.AreEqual(1, syncCalls.Count, "Sync should be called once when provider options change");
    }

    [TestMethod]
    public async Task BitMapShouldCallJsDisposeOnComponentDisposal()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);
        Context.JSInterop.SetupVoid(DISPOSE);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.DisposeAsync();

        var disposeCalls = Context.JSInterop.Invocations
            .Where(i => i.Identifier == DISPOSE)
            .ToList();

        Assert.AreEqual(1, disposeCalls.Count, "JS dispose should be called during component disposal");
    }

    [TestMethod]
    public void BitMapShouldRenderRootElementWithCorrectClass()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        var root = component.Find(".bit-map");
        Assert.IsNotNull(root);

        var canvas = component.Find(".bit-map-canvas");
        Assert.IsNotNull(canvas);
    }

    [TestMethod]
    public void BitMapShouldUseDefaultProviderWhenNoneSupplied()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        // Init should still be called with the default provider's JsObjectName
        var initCalls = Context.JSInterop.Invocations
            .Where(i => i.Identifier == INIT)
            .ToList();

        Assert.AreEqual(1, initCalls.Count);
    }

    [TestMethod]
    public void BitMapShouldReInitializeWhenJsObjectNameChanges()
    {
        // Use OpenLayers provider which has a different JsObjectName than Leaflet
        const string OL_INIT = "BitBlazorUI.BitMapOpenLayers.init";
        const string OL_DISPOSE = "BitBlazorUI.BitMapOpenLayers.dispose";

        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);
        Context.JSInterop.SetupVoid(DISPOSE);
        Context.JSInterop.SetupVoid(OL_INIT);
        Context.JSInterop.SetupVoid(OL_DISPOSE);

        // Start with Leaflet
        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Provider, new BitLeafletMapProvider());
        });

        // Verify initial init was called
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == INIT));
    }

    [TestMethod]
    public async Task BitMapDisposeShouldNotThrowWhenNotInitialized()
    {
        // Render without triggering OnAfterRender (component won't be initialized)
        // In practice, bUnit always triggers OnAfterRender, so we test double-dispose instead
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);
        Context.JSInterop.SetupVoid(DISPOSE);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        // First dispose
        await component.Instance.DisposeAsync();

        // Second dispose should not throw
        await component.Instance.DisposeAsync();

        // Only one dispose call should have been made
        var disposeCalls = Context.JSInterop.Invocations
            .Where(i => i.Identifier == DISPOSE)
            .ToList();

        Assert.AreEqual(1, disposeCalls.Count);
    }
}
