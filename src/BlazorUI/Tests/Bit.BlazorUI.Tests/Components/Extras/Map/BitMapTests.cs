using System;
using System.Linq;
using System.Threading.Tasks;
using Bit.BlazorUI;
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

    [TestInitialize]
    public void ResetAssetCache()
    {
        // BitMap dedupes script / stylesheet loads process-wide so the same provider URL
        // isn't re-requested when multiple maps mount in quick succession. Tests that
        // assert on initScripts/initStylesheets invocations need to reset that cache so
        // each test starts from a clean state.
        BitMap<BitLeafletMapProvider>.ResetAssetLoadCacheForTesting();
        BitMap<TestMapProviderA>.ResetAssetLoadCacheForTesting();
    }

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
    public void BitMapShouldDedupeAssetLoadsAcrossMounts()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);

        // First mount: stylesheets + scripts must be requested.
        RenderComponent<BitMap<BitLeafletMapProvider>>();
        var firstStylesheetCalls = Context.JSInterop.Invocations.Count(i => i.Identifier == INIT_STYLESHEETS);
        var firstScriptCalls = Context.JSInterop.Invocations.Count(i => i.Identifier == INIT_SCRIPTS);

        Assert.IsTrue(firstStylesheetCalls >= 1, "First mount should request stylesheets");
        Assert.IsTrue(firstScriptCalls >= 1, "First mount should request scripts");

        // Second mount: cache should kick in and skip redundant load round-trips.
        RenderComponent<BitMap<BitLeafletMapProvider>>();

        Assert.AreEqual(firstStylesheetCalls,
            Context.JSInterop.Invocations.Count(i => i.Identifier == INIT_STYLESHEETS),
            "Second mount must not re-request already-loaded stylesheets");
        Assert.AreEqual(firstScriptCalls,
            Context.JSInterop.Invocations.Count(i => i.Identifier == INIT_SCRIPTS),
            "Second mount must not re-request already-loaded scripts");
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
    public void BitMapShouldNotSyncWhenProviderResetToNull()
    {
        // Setting Provider back to null on a live component should be a no-op (the alternative
        // — silently swapping to a default-constructed provider — would surprise consumers).
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);
        Context.JSInterop.SetupVoid(SYNC);
        Context.JSInterop.SetupVoid(DISPOSE);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Provider, new BitLeafletMapProvider { Zoom = 10 });
        });

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add<BitLeafletMapProvider?>(p => p.Provider, null);
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC),
            "Resetting Provider to null on a live component must not trigger sync");
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == DISPOSE),
            "Resetting Provider to null on a live component must not dispose the active map");
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
        // Two test providers that share TMapProvider but expose different JsObjectName
        // values so swapping Provider drives the re-init branch in OnProviderSet
        // (dispose old + init new) instead of the sync branch.
        const string A_INIT = "BitBlazorUI.TestProviderA.init";
        const string A_DISPOSE = "BitBlazorUI.TestProviderA.dispose";
        const string B_INIT = "BitBlazorUI.TestProviderB.init";

        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(A_INIT);
        Context.JSInterop.SetupVoid(A_DISPOSE);
        Context.JSInterop.SetupVoid(B_INIT);

        // Start with provider A
        var component = RenderComponent<BitMap<TestMapProviderA>>(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderA());
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == A_INIT),
            "Provider A should be initialized on first render");

        // Swap to a derived provider with a different JsObjectName
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderB());
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == A_DISPOSE),
            "Provider A should be disposed when JsObjectName changes");
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == B_INIT),
            "Provider B should be initialized when JsObjectName changes");
    }

    [TestMethod]
    public void BitMapShouldReplayImperativeStateOnProviderSwap()
    {
        // When ReplayStateOnProviderSwap is true, markers/vector layers/tile overlays added
        // imperatively before the swap must be re-applied to the new provider's JS instance.
        const string A_INIT = "BitBlazorUI.TestProviderA.init";
        const string A_DISPOSE = "BitBlazorUI.TestProviderA.dispose";
        const string B_INIT = "BitBlazorUI.TestProviderB.init";
        const string A_ADD_MARKER = "BitBlazorUI.TestProviderA.addMarker";
        const string B_ADD_MARKER = "BitBlazorUI.TestProviderB.addMarker";

        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(A_INIT);
        Context.JSInterop.SetupVoid(A_DISPOSE);
        Context.JSInterop.SetupVoid(B_INIT);
        Context.JSInterop.SetupVoid(A_ADD_MARKER);
        Context.JSInterop.SetupVoid(B_ADD_MARKER);

        var component = RenderComponent<BitMap<TestMapProviderA>>(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderA());
            parameters.Add(p => p.ReplayStateOnProviderSwap, true);
        });

        // Add a marker against provider A.
        var addMarkerTask = component.Instance.AddMarker(new BitMapMarker { Id = "x", Position = new(0, 0) });
        Assert.IsTrue(addMarkerTask.IsCompleted);

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == A_ADD_MARKER));

        // Swap to provider B — the marker should be re-applied via B's JS object.
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderB());
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == B_ADD_MARKER),
            "Imperatively-added marker must be replayed on the new provider");
    }

    [TestMethod]
    public void BitMapShouldRejectInvalidJsObjectName()
    {
        // A provider whose JsObjectName breaks out of the JS identifier shape would be a
        // potential injection vector. Validation must throw before any interop call.
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);

        Assert.Throws<InvalidOperationException>(() =>
        {
            RenderComponent<BitMap<MaliciousJsObjectNameProvider>>();
        });
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldRejectInvertedLatitudes()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new BitMapLatLngBounds(new BitMapLatLng(50, 0), new BitMapLatLng(40, 1));
        });
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldAllowAntimeridianLongitudes()
    {
        // SouthWest.Longitude > NorthEast.Longitude is intentionally allowed: it expresses
        // a bounding box that crosses the antimeridian (e.g. 170°W → 170°E).
        var b = new BitMapLatLngBounds(new BitMapLatLng(0, 170), new BitMapLatLng(10, -170));
        Assert.AreEqual(170, b.SouthWest.Longitude);
        Assert.AreEqual(-170, b.NorthEast.Longitude);
    }

    [TestMethod]
    public void BitMapVectorPathStyleShouldClampNonFiniteOpacityToZero()
    {
        var s = new BitMapVectorPathStyle { Opacity = double.NaN };
        Assert.AreEqual(0d, s.Opacity);

        s.FillOpacity = double.PositiveInfinity;
        Assert.AreEqual(0d, s.FillOpacity);

        s.Weight = double.NaN;
        Assert.AreEqual(0d, s.Weight);
    }

    [TestMethod]
    public void BitMapMarkerShouldClampIconSizeToOne()
    {
        var m = new BitMapMarker { Id = "i", Position = new(0, 0), IconWidth = -5, IconHeight = 0 };
        Assert.AreEqual(1, m.IconWidth);
        Assert.AreEqual(1, m.IconHeight);
    }

    [TestMethod]
    public async Task BitMapShouldNotPropagateJsExceptionFromImperativeCalls()
    {
        // The component must never propagate JSException out of an imperative call (which
        // would tear down the page in Blazor Server). Instead the failure must be swallowed
        // and surfaced through OnInteropError. This test verifies the swallow-and-continue
        // contract; the OnInteropError plumbing is exercised by integration tests rather
        // than unit tests because bUnit's renderer dispatches event callbacks asynchronously.
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);
        Context.JSInterop.SetupVoid("BitBlazorUI.BitMapLeaflet.invalidateSize")
            .SetException(new Microsoft.JSInterop.JSException("boom"));

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        // Must not throw — the JSException is caught inside SafeInvokeAsync.
        await component.Instance.InvalidateSize();
    }

    [TestMethod]
    public async Task BitMapDisposeShouldBeIdempotent()
    {
        // bUnit always triggers OnAfterRender, so the component is always initialized
        // by the time we get here. Verify that disposing twice is safe and that the
        // underlying JS dispose is invoked exactly once.
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

    private class TestMapProviderA : BitMapProviderBase
    {
        public override string Key => "test-a";
        public override string JsObjectName => "TestProviderA";
        public override object BuildOptionsPayload() => GetCommonOptions();
    }

    private sealed class TestMapProviderB : TestMapProviderA
    {
        public override string Key => "test-b";
        public override string JsObjectName => "TestProviderB";
    }

    private sealed class MaliciousJsObjectNameProvider : BitMapProviderBase
    {
        public override string Key => "evil";
        public override string JsObjectName => "BitMapLeaflet'].malicious(['x";
        public override object BuildOptionsPayload() => GetCommonOptions();
    }
}
