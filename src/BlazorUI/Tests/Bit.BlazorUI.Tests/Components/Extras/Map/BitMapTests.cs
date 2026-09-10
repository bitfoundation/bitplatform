using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    private const string ZOOM_BY = "BitBlazorUI.BitMapLeaflet.zoomBy";
    private const string PAN_BY = "BitBlazorUI.BitMapLeaflet.panBy";
    private const string SET_VIEW = "BitBlazorUI.BitMapLeaflet.setView";
    private const string FLY_TO = "BitBlazorUI.BitMapLeaflet.flyTo";
    private const string ADD_MARKER = "BitBlazorUI.BitMapLeaflet.addMarker";
    private const string ADD_POLYLINE = "BitBlazorUI.BitMapLeaflet.addPolyline";
    private const string REMOVE_MARKER = "BitBlazorUI.BitMapLeaflet.removeMarker";
    private const string SYNC_MARKERS = "BitBlazorUI.BitMapLeaflet.syncMarkers";
    private const string CLUSTER_CONFIGURE = "BitBlazorUI.BitMapCluster.configure";
    private const string CLUSTER_DISABLE = "BitBlazorUI.BitMapCluster.disable";
    private const string CLUSTER_SET_MARKERS = "BitBlazorUI.BitMapCluster.setMarkers";
    private const string CLUSTER_RENDER = "BitBlazorUI.BitMapCluster.render";
    private const string CLUSTER_EXPAND = "BitBlazorUI.BitMapCluster.expand";
    private const string CLUSTER_ID = "__bitmap_cluster_4:7";
    private const string REMOVE_LAYER = "BitBlazorUI.BitMapLeaflet.removeLayer";
    private const string ADD_TILE_OVERLAY = "BitBlazorUI.BitMapLeaflet.addTileOverlay";
    private const string REMOVE_TILE_OVERLAY = "BitBlazorUI.BitMapLeaflet.removeTileOverlay";
    private const string REQUEST_FULLSCREEN = "BitBlazorUI.BitMapChrome.requestFullscreen";
    private const string TRACK_ANCHOR = "BitBlazorUI.BitMapChrome.trackAnchor";
    private const string UNTRACK_ANCHOR = "BitBlazorUI.BitMapChrome.untrackAnchor";
    private const string CHROME_ATTACH = "BitBlazorUI.BitMapChrome.attach";
    private const string CHROME_DETACH = "BitBlazorUI.BitMapChrome.detach";
    private const string CHROME_WAIT_VISIBLE = "BitBlazorUI.BitMapChrome.waitForVisible";
    private const string CHROME_HAS_WEBGL = "BitBlazorUI.BitMapChrome.hasWebGl";
    private const string CHROME_REDUCED_MOTION = "BitBlazorUI.BitMapChrome.prefersReducedMotion";
    private const string CHROME_LOCATE = "BitBlazorUI.BitMapChrome.locate";
    private const string CHROME_CANCEL_WAIT_VISIBLE = "BitBlazorUI.BitMapChrome.cancelWaitForVisible";
    private const string FIT_BOUNDS = "BitBlazorUI.BitMapLeaflet.fitBounds";
    private const string FIT_BOUNDS_TO_MARKERS = "BitBlazorUI.BitMapLeaflet.fitBoundsToMarkers";
    private const string GET_VIEW = "BitBlazorUI.BitMapLeaflet.getView";
    private const string PROJECT = "BitBlazorUI.BitMapLeaflet.project";

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

    /// <summary>
    /// Sets up the two capability probes a mount makes, so the map reaches its Ready state.
    /// <para>
    /// Nothing else needs declaring: the context runs in Loose mode, so every other call resolves
    /// to a default immediately. Only these two need real answers, and both are invoked without
    /// arguments - which is what lets a bare identifier setup match them. Any setup for a call
    /// that <em>does</em> carry arguments needs an invocation matcher (<c>_ =&gt; true</c>), and a
    /// void handler registered that way stays pending until it is completed, so declare one only
    /// when the test needs it to fail.
    /// </para>
    /// </summary>
    private void SetupSuccessfulMount()
    {
        Context.JSInterop.Setup<bool>(CHROME_HAS_WEBGL, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(CHROME_REDUCED_MOTION).SetResult(false);
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
        component.Render(parameters =>
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
        // - silently swapping to a default-constructed provider - would surprise consumers).
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(INIT);
        Context.JSInterop.SetupVoid(SYNC);
        Context.JSInterop.SetupVoid(DISPOSE);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Provider, new BitLeafletMapProvider { Zoom = 10 });
        });

        component.Render(parameters =>
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
        component.Render(parameters =>
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

        // Swap to provider B - the marker should be re-applied via B's JS object.
        component.Render(parameters =>
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
        Context.JSInterop.SetupVoid("BitBlazorUI.BitMapLeaflet.invalidateSize", _ => true)
            .SetException(new Microsoft.JSInterop.JSException("boom"));

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        // Must not throw - the JSException is caught inside SafeInvokeAsync.
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


    [TestMethod]
    public void BitMapShouldReportLoadStateTransitions()
    {
        // The consumer needs to tell "still downloading the library" apart from "the library
        // failed" and from "this browser cannot render it at all", so every transition is
        // reported rather than a single boolean flipping at the end.
        SetupSuccessfulMount();

        var states = new List<BitMapLoadState>();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.OnLoadStateChanged,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<BitMapLoadState>(this, s => states.Add(s)));
        });

        CollectionAssert.Contains(states, BitMapLoadState.Loading);
        CollectionAssert.Contains(states, BitMapLoadState.Ready);
        Assert.AreEqual(BitMapLoadState.Ready, component.Instance.LoadState);
        Assert.IsNull(component.Instance.LoadError);
    }

    [TestMethod]
    public void BitMapShouldReportUnsupportedWhenWebGlIsUnavailableForAGlProvider()
    {
        // A WebGL-backed provider on a browser without WebGL paints a permanently blank canvas.
        // The component has to check up front, or the user just sees an empty box.
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.Setup<bool>(CHROME_HAS_WEBGL, _ => true).SetResult(false);

        var component = RenderComponent<BitMap<WebGlTestProvider>>();

        Assert.AreEqual(BitMapLoadState.Unsupported, component.Instance.LoadState);
        Assert.IsFalse(component.Instance.IsReady);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.WebGlTestProvider.init"),
            "A provider that needs WebGL must not be initialized when the browser has none");
        Assert.IsNotNull(component.Find(".bit-map-status"), "The unsupported state must be visible, not a blank canvas");
    }

    [TestMethod]
    public void BitMapShouldInitializeGlProviderWhenWebGlIsAvailable()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid("BitBlazorUI.WebGlTestProvider.init");
        Context.JSInterop.SetupVoid(CHROME_ATTACH);
        Context.JSInterop.Setup<bool>(CHROME_HAS_WEBGL, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(CHROME_REDUCED_MOTION).SetResult(false);

        var component = RenderComponent<BitMap<WebGlTestProvider>>();

        Assert.AreEqual(BitMapLoadState.Ready, component.Instance.LoadState);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.WebGlTestProvider.init"));
    }

    [TestMethod]
    public void BitMapShouldReportFailedStateWhenInitThrows()
    {
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        // The matcher is required: bUnit routes an invocation that carries arguments only to a
        // handler registered with one, so a bare SetupVoid(INIT) would never fire this exception.
        Context.JSInterop.SetupVoid(INIT, _ => true).SetException(new Microsoft.JSInterop.JSException("boom"));

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        Assert.AreEqual(BitMapLoadState.Failed, component.Instance.LoadState);
        Assert.IsFalse(component.Instance.IsReady);
        Assert.IsNotNull(component.Instance.LoadError);
        Assert.IsNotNull(component.Find(".bit-map-status"), "A failed init must render the error state");
    }

    [TestMethod]
    public void BitMapShouldReportFailedStateWhenTheProviderScriptCannotBeLoaded()
    {
        // A CDN outage must surface as the component's error state. Letting it propagate out of
        // OnAfterRenderAsync would tear down the Blazor Server circuit - the whole page - over a
        // single failed script tag.
        // Only the failing call is set up explicitly. A void handler registered with a matcher
        // stays pending until it is completed, so setting up the calls that are meant to succeed
        // would stall the mount instead of letting Loose mode resolve them immediately.
        Context.JSInterop.SetupVoid(INIT_SCRIPTS, _ => true)
            .SetException(new Microsoft.JSInterop.JSException("Failed to fetch"));

        var errors = new List<BitMapInteropErrorArgs>();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.OnInteropError,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<BitMapInteropErrorArgs>(this, e => errors.Add(e)));
        });

        Assert.AreEqual(BitMapLoadState.Failed, component.Instance.LoadState);
        Assert.IsFalse(component.Instance.IsReady);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == INIT),
            "The map must not be initialized when its library failed to load");
        Assert.IsTrue(errors.Any(e => e.Source == BitMapInteropErrorSource.ScriptLoad));
    }

    [TestMethod]
    public void BitMapShouldAttachChromeWithTheConfiguredOptions()
    {
        SetupSuccessfulMount();

        RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.CooperativeGestures, true);
            parameters.Add(p => p.AutoResize, false);
            parameters.Add(p => p.EscapeToExit, false);
            parameters.Add(p => p.CooperativeGesturesWheelHint, "ctrl + scroll");
        });

        var attach = Context.JSInterop.Invocations.Single(i => i.Identifier == CHROME_ATTACH);
        var options = (Dictionary<string, object?>)attach.Arguments[4]!;

        Assert.AreEqual("BitMapLeaflet", options["jsObjectName"]);
        Assert.AreEqual(false, options["autoResize"]);
        Assert.AreEqual(true, options["cooperativeGestures"]);
        Assert.AreEqual(false, options["escapeToExit"]);
        Assert.AreEqual("ctrl + scroll", options["wheelHint"]);
        Assert.IsNotNull(options["hintId"], "The hint element id must be sent when cooperative gestures are on");
    }

    [TestMethod]
    public void BitMapShouldNotSendHintIdsWhenCooperativeGesturesAreOff()
    {
        SetupSuccessfulMount();

        RenderComponent<BitMap<BitLeafletMapProvider>>();

        var attach = Context.JSInterop.Invocations.Single(i => i.Identifier == CHROME_ATTACH);
        var options = (Dictionary<string, object?>)attach.Arguments[4]!;

        Assert.AreEqual(false, options["cooperativeGestures"]);
        Assert.IsNull(options["hintId"]);
    }

    [TestMethod]
    public async Task BitMapShouldDetachChromeOnDisposal()
    {
        // The chrome owns observers and DOM listeners of its own. They have to come off before
        // the provider tears the container down, or they outlive the map.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.DisposeAsync();

        var identifiers = Context.JSInterop.Invocations.Select(i => i.Identifier).ToList();
        Assert.AreEqual(1, identifiers.Count(i => i == CHROME_DETACH));
        Assert.IsTrue(identifiers.LastIndexOf(CHROME_DETACH) < identifiers.LastIndexOf(DISPOSE),
            "The chrome must be detached before the provider disposes the map");
    }

    [TestMethod]
    public void BitMapShouldWaitForVisibilityBeforeInitWhenLazyLoadIsEnabled()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(CHROME_WAIT_VISIBLE);

        RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.LazyLoad, true);
            parameters.Add(p => p.LazyLoadRootMargin, "400px");
        });

        var identifiers = Context.JSInterop.Invocations.Select(i => i.Identifier).ToList();
        var waitIndex = identifiers.IndexOf(CHROME_WAIT_VISIBLE);
        Assert.IsTrue(waitIndex >= 0, "LazyLoad must wait for the container to become visible");
        Assert.IsTrue(waitIndex < identifiers.IndexOf(INIT), "The map must not be created before it is visible");

        var wait = Context.JSInterop.Invocations.First(i => i.Identifier == CHROME_WAIT_VISIBLE);
        Assert.AreEqual("400px", wait.Arguments[2]);
    }

    [TestMethod]
    public void BitMapShouldNotWaitForVisibilityWhenLazyLoadIsDisabled()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(CHROME_WAIT_VISIBLE);

        RenderComponent<BitMap<BitLeafletMapProvider>>();

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == CHROME_WAIT_VISIBLE));
    }

    [TestMethod]
    public void BitMapShouldShareTheAssetCacheAcrossProviderTypes()
    {
        // The cache lives on a non-generic type on purpose: static state inside
        // BitMap<TMapProvider> is per closed generic, so two maps over different provider types
        // that share a script URL would each pay their own interop round-trip.
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid("BitBlazorUI.SharedAssetProviderA.init");
        Context.JSInterop.SetupVoid("BitBlazorUI.SharedAssetProviderB.init");
        Context.JSInterop.SetupVoid(CHROME_ATTACH);
        Context.JSInterop.Setup<bool>(CHROME_HAS_WEBGL, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(CHROME_REDUCED_MOTION).SetResult(false);

        RenderComponent<BitMap<SharedAssetProviderA>>();
        var afterFirst = Context.JSInterop.Invocations.Count(i => i.Identifier == INIT_SCRIPTS);

        RenderComponent<BitMap<SharedAssetProviderB>>();

        Assert.AreEqual(1, afterFirst);
        Assert.AreEqual(afterFirst, Context.JSInterop.Invocations.Count(i => i.Identifier == INIT_SCRIPTS),
            "A second provider type sharing the same script URL must not re-request it");
    }

    [TestMethod]
    public void BitMapShouldRenderAccessibilityLandmarks()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Map of Berlin");
            parameters.Add(p => p.KeyboardInstructions, "Arrows pan, plus/minus zoom, Escape leaves.");
        });

        var root = component.Find(".bit-map");
        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("interactive map", root.GetAttribute("aria-roledescription"));
        Assert.AreEqual("Map of Berlin", root.GetAttribute("aria-label"));

        var canvas = component.Find(".bit-map-canvas");
        Assert.AreEqual("0", canvas.GetAttribute("tabindex"));
        Assert.AreEqual("Map of Berlin", canvas.GetAttribute("aria-label"));

        // The instructions element must be the one aria-describedby points at, or the association
        // is silently broken and screen-reader users get no keyboard help at all.
        var describedBy = canvas.GetAttribute("aria-describedby");
        Assert.IsFalse(string.IsNullOrEmpty(describedBy));
        var help = component.Find($"[id=\"{describedBy}\"]");
        Assert.AreEqual("Arrows pan, plus/minus zoom, Escape leaves.", help.TextContent.Trim());

        var live = component.Find(".bit-map-live");
        Assert.AreEqual("polite", live.GetAttribute("aria-live"));
        Assert.AreEqual("true", live.GetAttribute("aria-atomic"));
    }

    [TestMethod]
    public void BitMapShouldRenderTheGestureHintOnlyWhenCooperativeGesturesAreEnabled()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        Assert.AreEqual(0, component.FindAll(".bit-map-gesture-hint").Count);

        component.Render(parameters => parameters.Add(p => p.CooperativeGestures, true));
        Assert.AreEqual(1, component.FindAll(".bit-map-gesture-hint").Count);
        Assert.AreEqual("true", component.Find(".bit-map-gesture-hint").GetAttribute("aria-hidden"),
            "The hint duplicates what the keyboard instructions already say, so it stays out of the a11y tree");
    }

    [TestMethod]
    public async Task BitMapZoomHelpersShouldCallZoomByWithTheRightDelta()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(ZOOM_BY);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.ZoomIn();
        await component.Instance.ZoomOut(2);

        var calls = Context.JSInterop.Invocations.Where(i => i.Identifier == ZOOM_BY).ToList();
        Assert.AreEqual(2, calls.Count);
        Assert.AreEqual(1d, calls[0].Arguments[1]);
        Assert.AreEqual(-2d, calls[1].Arguments[1]);
    }

    [TestMethod]
    public async Task BitMapPanByShouldForwardThePixelOffset()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(PAN_BY);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.PanBy(-120, 40);

        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == PAN_BY);
        Assert.AreEqual(-120d, call.Arguments[1]);
        Assert.AreEqual(40d, call.Arguments[2]);
    }

    [TestMethod]
    public async Task BitMapShouldJumpInsteadOfFlyingUnderReducedMotion()
    {
        // Under a reduced-motion preference the destination matters and the journey does not, so
        // FlyTo degrades to an instant move rather than a shorter animation.
        SetupSuccessfulMount();
        Context.JSInterop.Setup<bool>(CHROME_REDUCED_MOTION).SetResult(true);
        Context.JSInterop.SetupVoid(SET_VIEW);
        Context.JSInterop.SetupVoid(FLY_TO);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.FlyTo(new(10, 20), 8);

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == FLY_TO));
        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == SET_VIEW);
        Assert.AreEqual(false, call.Arguments[4], "The jump must not animate");
    }

    [TestMethod]
    public async Task BitMapShouldStillAnimateAnEssentialFlightUnderReducedMotion()
    {
        SetupSuccessfulMount();
        Context.JSInterop.Setup<bool>(CHROME_REDUCED_MOTION).SetResult(true);
        Context.JSInterop.SetupVoid(FLY_TO);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.FlyTo(new(10, 20), 8, essential: true);

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == FLY_TO));
    }

    [TestMethod]
    public async Task BitMapShouldAnimateFlyToWhenReducedMotionIsNotRequested()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(FLY_TO);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.FlyTo(new(10, 20), 8);

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == FLY_TO));
    }

    [TestMethod]
    public async Task BitMapLocateShouldReturnThePositionAndRecentreTheMap()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(SET_VIEW);
        Context.JSInterop.Setup<JsonElement>(CHROME_LOCATE, _ => true)
            .SetResult(JsonSerializer.Deserialize<JsonElement>("""{"lat":48.8566,"lng":2.3522,"accuracy":25}"""));

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        var result = await component.Instance.Locate();

        Assert.IsNotNull(result);
        Assert.AreEqual(48.8566, result.Position.Latitude);
        Assert.AreEqual(2.3522, result.Position.Longitude);
        Assert.AreEqual(25d, result.AccuracyMeters);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == SET_VIEW));
    }

    [TestMethod]
    public async Task BitMapLocateShouldReturnNullWhenTheBrowserRefuses()
    {
        // A denied permission prompt is an ordinary outcome, not an exception - the caller gets a
        // null and can carry on.
        SetupSuccessfulMount();
        Context.JSInterop.Setup<JsonElement>(CHROME_LOCATE, _ => true)
            .SetException(new Microsoft.JSInterop.JSException("User denied Geolocation"));

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        var result = await component.Instance.Locate();

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task BitMapShouldSendMarkerAccessibilityAndAppearanceFields()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(ADD_MARKER);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddMarker(new BitMapMarker
        {
            Id = "hq",
            Position = new(1, 2),
            Alt = "Head office",
            Opacity = 0.5,
            RiseOnHover = true,
        });

        var payload = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == ADD_MARKER).Arguments[2]!;

        Assert.AreEqual("Head office", payload["alt"]);
        Assert.AreEqual(0.5d, payload["opacity"]);
        Assert.AreEqual(true, payload["riseOnHover"]);
    }

    [TestMethod]
    public async Task BitMapShouldSendTheExtendedPathStyleFields()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(ADD_POLYLINE);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddPolyline("route", [new(0, 0), new(1, 1)], new BitMapVectorPathStyle
        {
            DashArray = "4 2",
            DashOffset = "3",
            LineCap = BitMapLineCap.Square,
            LineJoin = BitMapLineJoin.Bevel,
            Fill = false,
        });

        var style = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == ADD_POLYLINE).Arguments[3]!;

        Assert.AreEqual("4 2", style["dashArray"]);
        Assert.AreEqual("3", style["dashOffset"]);
        Assert.AreEqual("square", style["lineCap"]);
        Assert.AreEqual("bevel", style["lineJoin"]);
        Assert.AreEqual(false, style["fill"]);
    }

    [TestMethod]
    public async Task BitMapShouldExposeTheIdsOfWhatIsOnTheMap()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(ADD_MARKER);
        Context.JSInterop.SetupVoid(ADD_POLYLINE);
        Context.JSInterop.SetupVoid("BitBlazorUI.BitMapLeaflet.addTileOverlay");
        Context.JSInterop.SetupVoid("BitBlazorUI.BitMapLeaflet.removeMarker");

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance.AddMarker(new BitMapMarker { Id = "b", Position = new(1, 1) });
        await component.Instance.AddPolyline("route", [new(0, 0), new(1, 1)]);
        await component.Instance.AddTileOverlay(new BitMapTileOverlay
        {
            Id = "labels",
            UrlTemplate = "https://example.com/{z}/{x}/{y}.png",
        });

        CollectionAssert.AreEqual(new[] { "a", "b" }, component.Instance.MarkerIds.ToArray());
        CollectionAssert.AreEqual(new[] { "route" }, component.Instance.LayerIds.ToArray());
        CollectionAssert.AreEqual(new[] { "labels" }, component.Instance.TileOverlayIds.ToArray());

        await component.Instance.RemoveMarker("a");
        CollectionAssert.AreEqual(new[] { "b" }, component.Instance.MarkerIds.ToArray());
    }

    [TestMethod]
    public async Task BitMapShouldRejectAMarkerWithABlankId()
    {
        // A whitespace id looks distinct in source but is indistinguishable at runtime, so it
        // silently overwrites whatever else used it.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await component.Instance.AddMarker(new BitMapMarker { Id = "  ", Position = new(0, 0) }));
    }

    [TestMethod]
    public async Task BitMapShouldRejectANonFiniteOrNegativeCircleRadius()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await component.Instance.AddCircle("c", new(0, 0), double.NaN));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await component.Instance.AddCircle("c", new(0, 0), -1));
    }

    [TestMethod]
    public async Task BitMapShouldRejectGeometriesWithTooFewPoints()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await component.Instance.AddPolyline("l", [new(0, 0)]));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await component.Instance.AddPolygon("p", [new(0, 0), new(1, 1)]));
    }

    [TestMethod]
    public async Task BitMapShouldRejectAnOutOfRangeZoom()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await component.Instance.SetView(new(0, 0), double.NaN));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await component.Instance.SetView(new(0, 0), 99));
    }

    [TestMethod]
    public async Task BitMapShouldRejectNegativePadding()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await component.Instance.FitBoundsToMarkers(-1));
    }

    [TestMethod]
    public void BitMapMarkerShouldClampOpacityAndFallBackOnNonFiniteValues()
    {
        Assert.AreEqual(1d, new BitMapMarker { Id = "a", Position = new(0, 0) }.Opacity);
        Assert.AreEqual(1d, new BitMapMarker { Id = "a", Position = new(0, 0), Opacity = 5 }.Opacity);
        Assert.AreEqual(0d, new BitMapMarker { Id = "a", Position = new(0, 0), Opacity = -1 }.Opacity);
        // A NaN opacity is a bug in the caller's maths, not a request to hide the marker.
        Assert.AreEqual(1d, new BitMapMarker { Id = "a", Position = new(0, 0), Opacity = double.NaN }.Opacity);
    }

    [TestMethod]
    public void BitMapVectorPathStyleShouldDefaultToRoundCapsAndAFilledShape()
    {
        var style = new BitMapVectorPathStyle();

        Assert.AreEqual(BitMapLineCap.Round, style.LineCap);
        Assert.AreEqual(BitMapLineJoin.Round, style.LineJoin);
        Assert.IsTrue(style.Fill);
        Assert.IsNull(style.DashOffset);
    }

    [TestMethod]
    public void BitMapShouldApplyABoundCameraOnceTheMapIsReady()
    {
        // Center/Zoom are assigned before the map exists, so they have to be replayed after init
        // rather than dropped.
        SetupSuccessfulMount();

        RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Bind(p => p.Center, new BitMapLatLng(48.8566, 2.3522), _ => { });
            parameters.Bind(p => p.Zoom, 9d, _ => { });
        });

        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == SET_VIEW);
        Assert.AreEqual(48.8566, call.Arguments[1]);
        Assert.AreEqual(2.3522, call.Arguments[2]);
        Assert.AreEqual(9d, call.Arguments[3]);
        Assert.AreEqual(false, call.Arguments[4], "A bound camera jumps rather than animating");
    }

    [TestMethod]
    public void BitMapShouldMoveTheMapWhenABoundCameraChanges()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Bind(p => p.Center, new BitMapLatLng(0, 0), _ => { });
        });

        var before = Context.JSInterop.Invocations.Count(i => i.Identifier == SET_VIEW);

        component.Render(parameters => parameters.Bind(p => p.Center, new BitMapLatLng(10, 20), _ => { }));

        var calls = Context.JSInterop.Invocations.Where(i => i.Identifier == SET_VIEW).ToList();
        Assert.AreEqual(before + 1, calls.Count);
        Assert.AreEqual(10d, calls[^1].Arguments[1]);
        Assert.AreEqual(20d, calls[^1].Arguments[2]);
    }

    [TestMethod]
    public void BitMapShouldApplyBothHalvesOfTheCameraInOneCall()
    {
        // Center and Zoom changing in the same render must cost one interop round-trip, not two.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Bind(p => p.Center, new BitMapLatLng(0, 0), _ => { });
            parameters.Bind(p => p.Zoom, 4d, _ => { });
        });

        var before = Context.JSInterop.Invocations.Count(i => i.Identifier == SET_VIEW);

        component.Render(parameters =>
        {
            parameters.Bind(p => p.Center, new BitMapLatLng(10, 20), _ => { });
            parameters.Bind(p => p.Zoom, 11d, _ => { });
        });

        Assert.AreEqual(before + 1, Context.JSInterop.Invocations.Count(i => i.Identifier == SET_VIEW));
    }

    [TestMethod]
    public async Task BitMapShouldWriteTheMapsCameraBackToTheBoundParameters()
    {
        SetupSuccessfulMount();

        BitMapLatLng? boundCenter = new(0, 0);
        double? boundZoom = 4;

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Bind(p => p.Center, boundCenter, v => boundCenter = v);
            parameters.Bind(p => p.Zoom, boundZoom, v => boundZoom = v);
        });

        await component.Instance._OnViewChanged(ViewPayload(51.5, -0.12, 13));

        Assert.AreEqual(51.5, boundCenter!.Value.Latitude);
        Assert.AreEqual(-0.12, boundCenter!.Value.Longitude);
        Assert.AreEqual(13d, boundZoom);
    }

    [TestMethod]
    public async Task BitMapShouldNotMoveTheMapAgainAfterWritingTheCameraBack()
    {
        // The loop that has to be broken: the map moves, that writes Center back, the parent
        // re-renders with the new Center, and a naive implementation moves the map again.
        SetupSuccessfulMount();

        BitMapLatLng? boundCenter = new(0, 0);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Bind(p => p.Center, boundCenter, v => boundCenter = v);
        });

        var before = Context.JSInterop.Invocations.Count(i => i.Identifier == SET_VIEW);

        await component.Instance._OnViewChanged(ViewPayload(51.5, -0.12, 13));

        // Re-render with exactly what the map reported, as the parent's binding would.
        component.Render(parameters => parameters.Bind(p => p.Center, boundCenter, v => boundCenter = v));

        Assert.AreEqual(before, Context.JSInterop.Invocations.Count(i => i.Identifier == SET_VIEW),
            "Echoing the map's own camera back at it must not trigger another move");
    }

    [TestMethod]
    public void BitMapShouldNotTouchAnUnboundZoomWhenTheCameraIsWrittenBack()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Bind(p => p.Center, new BitMapLatLng(0, 0), _ => { });
        });

        component.Instance._OnViewChanged(ViewPayload(51.5, -0.12, 13)).GetAwaiter().GetResult();

        Assert.IsNull(component.Instance.Zoom,
            "An unbound Zoom must stay null rather than silently becoming a requested camera");
    }

    [TestMethod]
    public void BitMapShouldRenderTheDeclarativeMarkerCollection()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
            ]);
        });

        CollectionAssert.AreEqual(new[] { "a", "b" }, component.Instance.MarkerIds.ToArray());
    }

    [TestMethod]
    public void BitMapShouldOnlyTouchTheMarkersThatActuallyChanged()
    {
        // Re-adding an unchanged marker is not just wasted work: it closes any popup that marker
        // has open and drops its keyboard focus.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
                new BitMapMarker { Id = "c", Position = new(2, 2) },
                new BitMapMarker { Id = "d", Position = new(3, 3) },
            ]);
        });

        // The first application of a collection is itself a wholesale change, so it batches.
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC_MARKERS));

        var before = Context.JSInterop.Invocations.Count(i => i.Identifier == ADD_MARKER);
        var syncsBefore = Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC_MARKERS);

        // Same collection contents, one marker moved.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(9, 9) },
                new BitMapMarker { Id = "c", Position = new(2, 2) },
                new BitMapMarker { Id = "d", Position = new(3, 3) },
            ]);
        });

        var added = Context.JSInterop.Invocations.Where(i => i.Identifier == ADD_MARKER).Skip(before).ToList();
        Assert.AreEqual(1, added.Count, "Only the moved marker should be re-sent");
        Assert.AreEqual("b", added[0].Arguments[1]);
        Assert.AreEqual(syncsBefore, Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC_MARKERS),
            "A single moved marker is not enough to justify replacing the whole set");
    }

    [TestMethod]
    public void BitMapShouldRemoveMarkersDroppedFromTheCollection()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
                new BitMapMarker { Id = "c", Position = new(2, 2) },
                new BitMapMarker { Id = "d", Position = new(3, 3) },
            ]);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
                new BitMapMarker { Id = "c", Position = new(2, 2) },
            ]);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_MARKER));
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, component.Instance.MarkerIds.ToArray());
    }

    [TestMethod]
    public void BitMapShouldBatchAWholesaleMarkerReplacement()
    {
        // Past roughly half the set, one batched replace beats a stream of individual calls.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
                new BitMapMarker { Id = "c", Position = new(2, 2) },
            ]);
        });

        var syncsBefore = Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC_MARKERS);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "x", Position = new(4, 4) },
                new BitMapMarker { Id = "y", Position = new(5, 5) },
                new BitMapMarker { Id = "z", Position = new(6, 6) },
            ]);
        });

        Assert.AreEqual(syncsBefore + 1, Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC_MARKERS));
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_MARKER),
            "A wholesale replacement is one batched call, not three removes plus three adds");
        CollectionAssert.AreEqual(new[] { "x", "y", "z" }, component.Instance.MarkerIds.ToArray());
    }

    [TestMethod]
    public void BitMapShouldNotCallIntoJsWhenTheMarkerCollectionIsUnchanged()
    {
        SetupSuccessfulMount();

        BitMapMarker[] markers =
        [
            new BitMapMarker { Id = "a", Position = new(0, 0) },
            new BitMapMarker { Id = "b", Position = new(1, 1) },
        ];

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Markers, markers);
        });

        var before = Context.JSInterop.Invocations.Count;

        // A new collection instance holding equal markers - the common case when a parent
        // re-projects its data on every render.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
            ]);
        });

        Assert.AreEqual(before, Context.JSInterop.Invocations.Count,
            "Markers that compare equal must not be re-sent");
    }

    [TestMethod]
    public async Task BitMapShouldSendTheFocusableFlagWithEachMarker()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance.AddMarker(new BitMapMarker { Id = "b", Position = new(1, 1), Focusable = false });

        var calls = Context.JSInterop.Invocations.Where(i => i.Identifier == ADD_MARKER).ToList();
        Assert.AreEqual(true, ((Dictionary<string, object?>)calls[0].Arguments[2]!)["focusable"],
            "Markers are keyboard-reachable by default");
        Assert.AreEqual(false, ((Dictionary<string, object?>)calls[1].Arguments[2]!)["focusable"]);
    }

    [TestMethod]
    public void BitMapMarkerShouldCompareByValue()
    {
        var a = new BitMapMarker { Id = "x", Position = new(1, 2), Title = "T" };
        var b = new BitMapMarker { Id = "x", Position = new(1, 2), Title = "T" };
        var c = b with { Position = new(3, 4) };

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        // `with` has to carry every property across, or a dragged marker silently loses its popup.
        Assert.AreEqual("T", c.Title);
    }

    [TestMethod]
    public void BitMapShouldConfigureTheClusteringLayerWithTheGivenOptions()
    {
        SetupSuccessfulMount();

        RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering
            {
                RadiusPixels = 90,
                MaxZoom = 12,
                MinPoints = 5,
                Color = "#ff0000",
                CullOffscreen = false,
            });
        });

        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == CLUSTER_CONFIGURE);
        Assert.AreEqual("BitMapLeaflet", call.Arguments[1]);

        var options = (Dictionary<string, object?>)call.Arguments[2]!;
        Assert.AreEqual(90, options["radius"]);
        Assert.AreEqual(12d, options["maxZoom"]);
        Assert.AreEqual(5, options["minPoints"]);
        Assert.AreEqual("#ff0000", options["color"]);
        Assert.AreEqual(false, options["cullOffscreen"]);
    }

    [TestMethod]
    public async Task BitMapShouldRouteMarkersThroughTheClusteringLayerRatherThanTheProvider()
    {
        // While clustering is on, what the provider draws is decided by the clustering layer, so
        // the component must not also push markers straight at it - that would double-render them.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance.AddMarker(new BitMapMarker { Id = "b", Position = new(1, 1) });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == ADD_MARKER));
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC_MARKERS));

        var pushes = Context.JSInterop.Invocations.Where(i => i.Identifier == CLUSTER_SET_MARKERS).ToList();
        var lastIds = (string[])pushes[^1].Arguments[1]!;
        CollectionAssert.AreEqual(new[] { "a", "b" }, lastIds);
        CollectionAssert.AreEqual(new[] { "a", "b" }, component.Instance.MarkerIds.ToArray());
    }

    [TestMethod]
    public async Task BitMapShouldPushTheRemainingSetWhenAClusteredMarkerIsRemoved()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance.AddMarker(new BitMapMarker { Id = "b", Position = new(1, 1) });
        await component.Instance.RemoveMarker("a");

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_MARKER));
        var pushes = Context.JSInterop.Invocations.Where(i => i.Identifier == CLUSTER_SET_MARKERS).ToList();
        CollectionAssert.AreEqual(new[] { "b" }, (string[])pushes[^1].Arguments[1]!);
    }

    [TestMethod]
    public async Task BitMapShouldRaiseClusterClickInsteadOfMarkerClickForABubble()
    {
        // A cluster bubble is drawn as a marker, so its click arrives on the marker channel. It is
        // not one of the caller's markers, and reporting it as one would hand them an id that
        // matches nothing in their data.
        SetupSuccessfulMount();

        var markerClicks = new List<string>();
        var clusterClicks = new List<BitMapClusterClickArgs>();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering());
            parameters.Add(p => p.OnMarkerClick,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, id => markerClicks.Add(id)));
            parameters.Add(p => p.OnClusterClick,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<BitMapClusterClickArgs>(this, e => clusterClicks.Add(e)));
        });

        await component.Instance._OnMarkerClick(CLUSTER_ID);

        Assert.AreEqual(0, markerClicks.Count);
        Assert.AreEqual(1, clusterClicks.Count);
        Assert.AreEqual(CLUSTER_ID, clusterClicks[0].ClusterId);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == CLUSTER_EXPAND));
    }

    [TestMethod]
    public async Task BitMapShouldStillRaiseMarkerClickForARealMarkerWhileClustering()
    {
        SetupSuccessfulMount();

        var markerClicks = new List<string>();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering());
            parameters.Add(p => p.OnMarkerClick,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, id => markerClicks.Add(id)));
        });

        await component.Instance._OnMarkerClick("a");

        CollectionAssert.AreEqual(new[] { "a" }, markerClicks);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == CLUSTER_EXPAND));
    }

    [TestMethod]
    public async Task BitMapShouldNotZoomOnAClusterClickWhenZoomOnClickIsOff()
    {
        // The layer is still asked to resolve the bubble - that is where the member count comes
        // from, and a consumer handling the click themselves still needs it - but it is told not
        // to move the camera.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering { ZoomOnClick = false });
        });

        await component.Instance._OnMarkerClick(CLUSTER_ID);

        var expand = Context.JSInterop.Invocations.Single(i => i.Identifier == CLUSTER_EXPAND);
        Assert.AreEqual(false, expand.Arguments[3]);
    }

    [TestMethod]
    public async Task BitMapShouldAskTheClusterLayerToZoomWhenZoomOnClickIsOn()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering { ZoomOnClick = true, ExpandPaddingPixels = 24 });
        });

        await component.Instance._OnMarkerClick(CLUSTER_ID);

        var expand = Context.JSInterop.Invocations.Single(i => i.Identifier == CLUSTER_EXPAND);
        Assert.AreEqual(24, expand.Arguments[2]);
        Assert.AreEqual(true, expand.Arguments[3]);
    }

    [TestMethod]
    public async Task BitMapShouldReportTheClusterCountEvenWhenItDoesNotZoom()
    {
        // The count is what the callback is for. Losing it because the consumer turned the zoom
        // off would leave them with an id and nothing to do with it.
        SetupSuccessfulMount();

        Context.JSInterop.Setup<int>(CLUSTER_EXPAND, _ => true).SetResult(7);

        BitMapClusterClickArgs? clicked = null;
        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering { ZoomOnClick = false });
            parameters.Add(p => p.OnClusterClick, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<BitMapClusterClickArgs>(this, a => clicked = a));
        });

        await component.Instance._OnMarkerClick(CLUSTER_ID);

        Assert.IsNotNull(clicked);
        Assert.AreEqual(CLUSTER_ID, clicked.ClusterId);
        Assert.AreEqual(7, clicked.Count);
    }

    [TestMethod]
    public async Task BitMapShouldRecomputeClustersWhenTheViewSettles()
    {
        // Clusters are grouped in screen space, so they are only correct for the zoom they were
        // computed at.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        await component.Instance._OnViewChanged(ViewPayload(10, 20, 8));

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == CLUSTER_RENDER));
    }

    [TestMethod]
    public async Task BitMapShouldNotRecomputeClustersWhenClusteringIsOff()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance._OnViewChanged(ViewPayload(10, 20, 8));

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == CLUSTER_RENDER));
    }

    [TestMethod]
    public void BitMapShouldHandTheMarkersBackToTheProviderWhenClusteringIsTurnedOff()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        component.Render(parameters => parameters.Add<BitMapClustering?>(p => p.Clustering, null));

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == CLUSTER_DISABLE));
    }

    [TestMethod]
    public void BitMapShouldRepointTheClusteringLayerAfterAProviderSwap()
    {
        // The clustering layer renders through a named JS object, so a swap to a different backend
        // has to re-point it - otherwise it keeps syncing markers into the disposed one.
        Context.JSInterop.Setup<bool>(CHROME_HAS_WEBGL, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(CHROME_REDUCED_MOTION).SetResult(false);

        var component = RenderComponent<BitMap<TestMapProviderA>>(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderA());
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        component.Render(parameters => parameters.Add(p => p.Provider, new TestMapProviderB()));

        var configures = Context.JSInterop.Invocations.Where(i => i.Identifier == CLUSTER_CONFIGURE).ToList();
        Assert.AreEqual(2, configures.Count);
        Assert.AreEqual("TestProviderA", configures[0].Arguments[1]);
        Assert.AreEqual("TestProviderB", configures[1].Arguments[1]);
    }

    [TestMethod]
    public async Task BitMapShouldReleaseTheClusteringLayerOnDisposal()
    {
        // The layer holds the whole marker set, so leaving it behind leaks it for the lifetime of
        // the page.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        await component.Instance.DisposeAsync();

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == CLUSTER_DISABLE));
    }

    [TestMethod]
    public async Task BitMapShouldHideAndRestoreAVectorLayerWithoutLosingIt()
    {
        // Hiding keeps the definition, so showing again costs no re-declaration - and unlike a
        // zero opacity, a hidden layer stops hit-testing rather than swallowing clicks.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddPolyline("route", [new(0, 0), new(1, 1)]);

        Assert.IsTrue(component.Instance.IsLayerVisible("route"));

        await component.Instance.SetLayerVisible("route", false);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_LAYER));
        Assert.IsFalse(component.Instance.IsLayerVisible("route"));
        CollectionAssert.Contains(component.Instance.LayerIds.ToArray(), "route",
            "A hidden layer is still part of the map's definition");

        var addsBefore = Context.JSInterop.Invocations.Count(i => i.Identifier == ADD_POLYLINE);
        await component.Instance.SetLayerVisible("route", true);

        Assert.AreEqual(addsBefore + 1, Context.JSInterop.Invocations.Count(i => i.Identifier == ADD_POLYLINE));
        Assert.IsTrue(component.Instance.IsLayerVisible("route"));
    }

    [TestMethod]
    public async Task BitMapShouldIgnoreARedundantVisibilityChange()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddPolyline("route", [new(0, 0), new(1, 1)]);

        await component.Instance.SetLayerVisible("route", true);

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_LAYER));
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == ADD_POLYLINE),
            "Showing an already-visible layer must not redraw it");
    }

    [TestMethod]
    public async Task BitMapShouldRestyleALayerWithoutRestatingItsGeometry()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddPolyline("route", [new(0, 0), new(1, 1)]);

        await component.Instance.SetLayerStyle("route", new BitMapVectorPathStyle { Color = "#ff0000", Weight = 8 });

        var calls = Context.JSInterop.Invocations.Where(i => i.Identifier == ADD_POLYLINE).ToList();
        Assert.AreEqual(2, calls.Count);

        var path = (object[])calls[^1].Arguments[2]!;
        Assert.AreEqual(2, path.Length, "The geometry has to survive a restyle");

        var style = (Dictionary<string, object?>)calls[^1].Arguments[3]!;
        Assert.AreEqual("#ff0000", style["color"]);
        Assert.AreEqual(8d, style["weight"]);
    }

    [TestMethod]
    public async Task BitMapShouldNotDrawARestyledLayerThatIsHidden()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddPolyline("route", [new(0, 0), new(1, 1)]);
        await component.Instance.SetLayerVisible("route", false);

        var before = Context.JSInterop.Invocations.Count(i => i.Identifier == ADD_POLYLINE);
        await component.Instance.SetLayerStyle("route", new BitMapVectorPathStyle { Color = "#00ff00" });

        Assert.AreEqual(before, Context.JSInterop.Invocations.Count(i => i.Identifier == ADD_POLYLINE),
            "A hidden layer keeps the new style for when it is shown, but is not drawn now");

        // ...and the style it was given while hidden is the one it comes back with.
        await component.Instance.SetLayerVisible("route", true);
        var style = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Last(i => i.Identifier == ADD_POLYLINE).Arguments[3]!;
        Assert.AreEqual("#00ff00", style["color"]);
    }

    [TestMethod]
    public async Task BitMapShouldRejectVisibilityAndStyleChangesForAnUnknownLayer()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await component.Instance.SetLayerVisible("nope", false));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await component.Instance.SetLayerStyle("nope", null));
    }

    [TestMethod]
    public void BitMapShouldNotReplayAHiddenLayerOnAProviderSwap()
    {
        // Replaying a hidden layer visible would silently undo the caller's choice.
        Context.JSInterop.Setup<bool>(CHROME_HAS_WEBGL, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(CHROME_REDUCED_MOTION).SetResult(false);

        var component = RenderComponent<BitMap<TestMapProviderA>>(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderA());
            parameters.Add(p => p.ReplayStateOnProviderSwap, true);
        });

        component.Instance.AddPolyline("visible", [new(0, 0), new(1, 1)]).GetAwaiter().GetResult();
        component.Instance.AddPolyline("hidden", [new(2, 2), new(3, 3)]).GetAwaiter().GetResult();
        component.Instance.SetLayerVisible("hidden", false).GetAwaiter().GetResult();

        component.Render(parameters => parameters.Add(p => p.Provider, new TestMapProviderB()));

        var replayed = Context.JSInterop.Invocations
            .Where(i => i.Identifier == "BitBlazorUI.TestProviderB.addPolyline")
            .Select(i => (string)i.Arguments[1]!)
            .ToList();

        CollectionAssert.AreEqual(new[] { "visible" }, replayed);
    }

    [TestMethod]
    public async Task BitMapShouldHideAndRestoreATileOverlay()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddTileOverlay(new BitMapTileOverlay
        {
            Id = "labels",
            UrlTemplate = "https://example.com/{z}/{x}/{y}.png",
        });

        await component.Instance.SetTileOverlayVisible("labels", false);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_TILE_OVERLAY));
        Assert.IsFalse(component.Instance.IsTileOverlayVisible("labels"));

        await component.Instance.SetTileOverlayVisible("labels", true);
        Assert.AreEqual(2, Context.JSInterop.Invocations.Count(i => i.Identifier == ADD_TILE_OVERLAY));
        Assert.IsTrue(component.Instance.IsTileOverlayVisible("labels"));
    }

    [TestMethod]
    public async Task BitMapShouldChangeATileOverlaysOpacity()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddTileOverlay(new BitMapTileOverlay
        {
            Id = "radar",
            UrlTemplate = "https://example.com/{z}/{x}/{y}.png",
        });

        await component.Instance.SetTileOverlayOpacity("radar", 0.35);

        var payload = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Last(i => i.Identifier == ADD_TILE_OVERLAY).Arguments[1]!;
        Assert.AreEqual(0.35, payload["opacity"]);
    }

    [TestMethod]
    public async Task BitMapShouldSendTheTileOverlaysZoomWindowAndSubdomains()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddTileOverlay(new BitMapTileOverlay
        {
            Id = "radar",
            UrlTemplate = "https://{s}.example.com/{z}/{x}/{y}.png",
            MinZoom = 4,
            MaxZoom = 12,
            Subdomains = "abcd",
        });

        var payload = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == ADD_TILE_OVERLAY).Arguments[1]!;
        Assert.AreEqual(4, payload["minZoom"]);
        Assert.AreEqual(12, payload["maxZoom"]);
        Assert.AreEqual("abcd", payload["subdomains"]);
    }

    [TestMethod]
    public void BitMapTileOverlayShouldRejectAnInvertedZoomWindow()
    {
        var overlay = new BitMapTileOverlay
        {
            Id = "x",
            UrlTemplate = "https://example.com/{z}/{x}/{y}.png",
            MinZoom = 12,
            MaxZoom = 4,
        };

        Assert.Throws<ArgumentException>(overlay.Validate);
    }

    [TestMethod]
    public void BitMapTileOverlayShouldRejectAnEmptySubdomainListWhenTheUrlNeedsOne()
    {
        // A {s} placeholder with nothing to substitute produces a URL that 404s on every tile.
        var overlay = new BitMapTileOverlay
        {
            Id = "x",
            UrlTemplate = "https://{s}.example.com/{z}/{x}/{y}.png",
            Subdomains = "   ",
        };

        Assert.Throws<ArgumentException>(overlay.Validate);
    }

    [TestMethod]
    public async Task BitMapShouldRequestFullscreenOnItsOwnContainer()
    {
        SetupSuccessfulMount();
        Context.JSInterop.Setup<bool>(REQUEST_FULLSCREEN, _ => true).SetResult(true);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        Assert.IsTrue(await component.Instance.RequestFullscreen());
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == REQUEST_FULLSCREEN));
    }

    [TestMethod]
    public async Task BitMapShouldTrackFullscreenChangesTheUserMakesThemselves()
    {
        // Escape and the browser's own control never go through our API, so the state has to come
        // from the fullscreenchange event rather than from what we asked for.
        SetupSuccessfulMount();

        var reported = new List<bool>();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.OnFullscreenChanged,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<bool>(this, v => reported.Add(v)));
        });

        Assert.IsFalse(component.Instance.IsFullscreen);

        await component.Instance._OnFullscreenChanged(true);
        Assert.IsTrue(component.Instance.IsFullscreen);

        await component.Instance._OnFullscreenChanged(false);
        Assert.IsFalse(component.Instance.IsFullscreen);

        CollectionAssert.AreEqual(new[] { true, false }, reported);
    }

    [TestMethod]
    public async Task BitMapShouldRenderATextAlternativeForItsMarkers()
    {
        // A map is a picture: its markers are drawn, not written, so no amount of labelling the
        // canvas makes them reachable. The table is the non-visual equivalent.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerListMode, BitMapMarkerListMode.Visible);
            parameters.Add(p => p.MarkerListCaption, "Our offices");
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "kyiv", Position = new(50.45, 30.52), Alt = "Kyiv office" });
        await component.Instance.AddMarker(new BitMapMarker { Id = "lviv", Position = new(49.84, 24.03), Alt = "Lviv office" });

        Assert.AreEqual("Our offices", component.Find(".bit-map-marker-table caption").TextContent.Trim());

        var rows = component.FindAll(".bit-map-marker-table tbody tr");
        Assert.AreEqual(2, rows.Count);
        StringAssert.Contains(rows[0].TextContent, "Kyiv office");
        // The component formats coordinates in the current culture, so the expectation has to too.
        StringAssert.Contains(rows[0].TextContent, 50.45.ToString("F5"));
        StringAssert.Contains(rows[1].TextContent, "Lviv office");
    }

    [TestMethod]
    public void BitMapShouldNotRenderTheMarkerListByDefault()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        Assert.AreEqual(0, component.FindAll(".bit-map-marker-list").Count);
    }

    [TestMethod]
    public async Task BitMapShouldHideTheMarkerListVisuallyInScreenReaderOnlyMode()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerListMode, BitMapMarkerListMode.ScreenReaderOnly);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });

        var list = component.Find(".bit-map-marker-list");
        // bit-map-help clips it out of sight while leaving it in the accessibility tree - unlike
        // display:none, which would remove it from both.
        StringAssert.Contains(list.GetAttribute("class"), "bit-map-help");
        Assert.IsNull(list.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public async Task BitMapShouldNameEachMarkerListRowByWhatTheMarkerIsCalled()
    {
        // Falling back to the id would announce a database key; the button label has to name the
        // place, and has to differ per row or the buttons are indistinguishable out of context.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerListMode, BitMapMarkerListMode.Visible);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a1", Position = new(0, 0), Alt = "Alt name" });
        await component.Instance.AddMarker(new BitMapMarker { Id = "b2", Position = new(1, 1), Title = "Title name" });
        await component.Instance.AddMarker(new BitMapMarker { Id = "c3", Position = new(2, 2), PopupText = "Popup name" });
        await component.Instance.AddMarker(new BitMapMarker { Id = "d4", Position = new(3, 3) });

        var names = component.FindAll(".bit-map-marker-table tbody th").Select(e => e.TextContent.Trim()).ToArray();
        CollectionAssert.AreEqual(new[] { "Alt name", "Title name", "Popup name", "d4" }, names);

        var actionLabel = component.FindAll(".bit-map-marker-table-action")[0].GetAttribute("aria-label");
        StringAssert.Contains(actionLabel, "Alt name");
    }

    [TestMethod]
    public async Task BitMapShouldBringAMarkerIntoViewFromItsListRow()
    {
        SetupSuccessfulMount();

        var clicked = new List<string>();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerListMode, BitMapMarkerListMode.Visible);
            parameters.Add(p => p.MarkerListZoom, 14d);
            parameters.Add(p => p.OnMarkerClick,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, id => clicked.Add(id)));
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "kyiv", Position = new(50.45, 30.52), Alt = "Kyiv" });

        component.Find(".bit-map-marker-table-action").Click();

        var setView = Context.JSInterop.Invocations.Single(i => i.Identifier == SET_VIEW);
        Assert.AreEqual(50.45, setView.Arguments[1]);
        Assert.AreEqual(14d, setView.Arguments[3]);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.BitMapLeaflet.openMarkerPopup"));
        // A keyboard user reaching the marker through the list gets the same event a mouse user
        // gets by clicking the pin.
        CollectionAssert.AreEqual(new[] { "kyiv" }, clicked);
    }

    [TestMethod]
    public async Task BitMapShouldKeepTheMarkerListInStepWithImperativeChanges()
    {
        // Imperative calls mutate the snapshot without touching a parameter, so nothing else would
        // tell Blazor the table is stale.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerListMode, BitMapMarkerListMode.Visible);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance.AddMarker(new BitMapMarker { Id = "b", Position = new(1, 1) });
        Assert.AreEqual(2, component.FindAll(".bit-map-marker-table tbody tr").Count);

        await component.Instance.RemoveMarker("a");
        Assert.AreEqual(1, component.FindAll(".bit-map-marker-table tbody tr").Count);

        await component.Instance.ClearMarkers();
        Assert.AreEqual(0, component.FindAll(".bit-map-marker-table tbody tr").Count);
    }

    [TestMethod]
    public async Task BitMapShouldRenderACustomMarkerListTemplate()
    {
        SetupSuccessfulMount();

        Microsoft.AspNetCore.Components.RenderFragment<IReadOnlyList<BitMapMarker>> template = markers => builder =>
        {
            builder.OpenElement(0, "ul");
            builder.AddAttribute(1, "class", "custom-list");
            foreach (var marker in markers)
            {
                builder.OpenElement(2, "li");
                builder.AddContent(3, marker.Id);
                builder.CloseElement();
            }
            builder.CloseElement();
        };

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerListMode, BitMapMarkerListMode.Visible);
            parameters.Add(p => p.MarkerListTemplate, template);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "only", Position = new(0, 0) });

        Assert.AreEqual(0, component.FindAll(".bit-map-marker-table").Count);
        Assert.AreEqual("only", component.Find(".custom-list li").TextContent.Trim());
    }

    [TestMethod]
    public void BitMapShouldForwardProviderAdditionalOptionsToTheLibrary()
    {
        // No wrapper models every option of seven libraries; without an escape hatch the only
        // remedy for a missing one is a new release.
        SetupSuccessfulMount();

        var provider = new BitLeafletMapProvider();
        provider.AdditionalOptions["zoomAnimation"] = false;
        provider.AdditionalOptions["wheelPxPerZoomLevel"] = 120;

        RenderComponent<BitMap<BitLeafletMapProvider>>(parameters => parameters.Add(p => p.Provider, provider));

        var options = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == INIT).Arguments[4]!;
        var additional = (Dictionary<string, object?>)options["additionalOptions"]!;

        Assert.AreEqual(false, additional["zoomAnimation"]);
        Assert.AreEqual(120, additional["wheelPxPerZoomLevel"]);
    }

    [TestMethod]
    public void BitMapShouldOmitAdditionalOptionsWhenNoneWereGiven()
    {
        SetupSuccessfulMount();

        RenderComponent<BitMap<BitLeafletMapProvider>>();

        var options = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == INIT).Arguments[4]!;
        Assert.IsFalse(options.ContainsKey("additionalOptions"));
    }

    [TestMethod]
    public async Task BitMapShouldTreatReAddingAHiddenLayerAsShowingIt()
    {
        // Adding under an id that was hidden draws it, so the hidden flag has to go with it -
        // otherwise IsLayerVisible reports false for a layer that is plainly on screen, and a
        // later provider swap would skip replaying it.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddPolyline("route", [new(0, 0), new(1, 1)]);
        await component.Instance.SetLayerVisible("route", false);
        Assert.IsFalse(component.Instance.IsLayerVisible("route"));

        await component.Instance.AddPolyline("route", [new(2, 2), new(3, 3)]);

        Assert.IsTrue(component.Instance.IsLayerVisible("route"));
    }

    [TestMethod]
    public async Task BitMapShouldTreatReAddingAHiddenTileOverlayAsShowingIt()
    {
        SetupSuccessfulMount();

        var overlay = new BitMapTileOverlay { Id = "labels", UrlTemplate = "https://example.com/{z}/{x}/{y}.png" };

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddTileOverlay(overlay);
        await component.Instance.SetTileOverlayVisible("labels", false);
        Assert.IsFalse(component.Instance.IsTileOverlayVisible("labels"));

        await component.Instance.AddTileOverlay(overlay);

        Assert.IsTrue(component.Instance.IsTileOverlayVisible("labels"));
    }

    [TestMethod]
    public async Task BitMapSetZoomShouldReuseTheLastReportedCentreRatherThanReadingItBack()
    {
        // Reading the centre back over interop would double the cost of a single camera command.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance._OnViewChanged(ViewPayload(12, 34, 5));

        var getViewsBefore = Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.BitMapLeaflet.getView");

        await component.Instance.SetZoom(9);

        Assert.AreEqual(getViewsBefore, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.BitMapLeaflet.getView"));
        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == SET_VIEW);
        Assert.AreEqual(12d, call.Arguments[1]);
        Assert.AreEqual(34d, call.Arguments[2]);
        Assert.AreEqual(9d, call.Arguments[3]);
    }

    [TestMethod]
    public async Task BitMapShouldReportARightClickWithItsCoordinate()
    {
        SetupSuccessfulMount();

        var positions = new List<BitMapLatLng>();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.OnContextMenu,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<BitMapLatLng>(this, p => positions.Add(p)));
        });

        await component.Instance._OnContextMenu(
            JsonSerializer.Deserialize<JsonElement>("""{"lat":51.5,"lng":-0.12}"""));

        Assert.AreEqual(1, positions.Count);
        Assert.AreEqual(51.5, positions[0].Latitude);
        Assert.AreEqual(-0.12, positions[0].Longitude);
    }

    [TestMethod]
    public void BitMapShouldLeaveTheBrowserContextMenuAloneByDefault()
    {
        // Suppressing it costs the user "open in new tab", "copy image" and their assistive-tech
        // equivalents, so it is opt-in rather than a side effect of handling the event.
        SetupSuccessfulMount();

        RenderComponent<BitMap<BitLeafletMapProvider>>();

        var options = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == INIT).Arguments[4]!;
        Assert.AreEqual(false, options["suppressBrowserContextMenu"]);
    }

    [TestMethod]
    public void BitMapShouldForwardTheContextMenuSuppressionOption()
    {
        SetupSuccessfulMount();

        RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Provider, new BitLeafletMapProvider { SuppressBrowserContextMenu = true });
        });

        var options = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == INIT).Arguments[4]!;
        Assert.AreEqual(true, options["suppressBrowserContextMenu"]);
    }

    [TestMethod]
    public async Task BitMapShouldSendTooltipFieldsWithEachMarker()
    {
        // Tooltips are rendered by five of the seven providers, so the payload carries them
        // unconditionally and each backend decides what it can do with them.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddMarker(new BitMapMarker
        {
            Id = "a",
            Position = new(0, 0),
            TooltipText = "Plain text",
            TooltipPermanent = true,
            TooltipDirection = BitMapTooltipDirection.Right,
        });

        var payload = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == ADD_MARKER).Arguments[2]!;

        Assert.AreEqual("Plain text", payload["tooltipText"]);
        Assert.AreEqual(true, payload["tooltipPermanent"]);
        Assert.AreEqual("right", payload["tooltipDirection"]);
    }

    [TestMethod]
    public async Task BitMapShouldKeepMarkupAndPlainTextTooltipsApart()
    {
        // The two travel in separate fields on purpose: the providers write text through
        // textContent and markup through innerHTML, so collapsing them would silently turn
        // user-supplied text into an injection point.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddMarker(new BitMapMarker
        {
            Id = "a",
            Position = new(0, 0),
            TooltipHtml = (Microsoft.AspNetCore.Components.MarkupString)"<b>Bold</b>",
            TooltipText = "<script>alert(1)</script>",
        });

        var payload = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == ADD_MARKER).Arguments[2]!;

        Assert.AreEqual("<b>Bold</b>", payload["tooltipHtml"]);
        Assert.AreEqual("<script>alert(1)</script>", payload["tooltipText"]);
    }

    [TestMethod]
    public async Task BitMapShouldOpenABlazorRenderedPopupWhenAMarkerIsClicked()
    {
        // The popup's content is ordinary Blazor markup, which is the whole point: Blazor escapes
        // what it renders, so a popup built from user data cannot become an injection point the
        // way an HTML string can.
        SetupSuccessfulMount();

        Microsoft.AspNetCore.Components.RenderFragment<BitMapMarker> template = marker => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "popup-name");
            builder.AddContent(2, marker.Alt);
            builder.CloseElement();
        };

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, template);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "kyiv", Position = new(50.45, 30.52), Alt = "Kyiv <b>office</b>" });

        Assert.AreEqual(0, component.FindAll(".bit-map-popup").Count);

        await component.Instance._OnMarkerClick("kyiv");

        Assert.AreEqual(1, component.FindAll(".bit-map-popup").Count);
        // Rendered as text, not markup: the angle brackets survive as characters.
        Assert.AreEqual("Kyiv <b>office</b>", component.Find(".popup-name").TextContent);
        Assert.AreEqual("kyiv", component.Instance.OpenPopupMarker?.Id);

        var anchor = Context.JSInterop.Invocations.Single(i => i.Identifier == TRACK_ANCHOR);
        Assert.AreEqual(50.45, anchor.Arguments[2]);
        Assert.AreEqual(30.52, anchor.Arguments[3]);
    }

    [TestMethod]
    public async Task BitMapShouldNotOpenAPopupWithoutATemplate()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();
        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });

        await component.Instance._OnMarkerClick("a");

        Assert.AreEqual(0, component.FindAll(".bit-map-popup").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == TRACK_ANCHOR));
    }

    [TestMethod]
    public async Task BitMapShouldCloseThePopupWhenTheMapIsClicked()
    {
        SetupSuccessfulMount();

        var closed = 0;

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.OnPopupClosed,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => closed++));
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance._OnMarkerClick("a");
        Assert.AreEqual(1, component.FindAll(".bit-map-popup").Count);

        await component.Instance._OnClick(JsonSerializer.Deserialize<JsonElement>("""{"lat":1,"lng":1}"""));

        Assert.AreEqual(0, component.FindAll(".bit-map-popup").Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == UNTRACK_ANCHOR));
        Assert.AreEqual(1, closed);
    }

    [TestMethod]
    public async Task BitMapShouldCloseThePopupWhenItsMarkerIsRemoved()
    {
        // A popup for a marker that is no longer on the map would hang in space.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance._OnMarkerClick("a");

        await component.Instance.RemoveMarker("a");

        Assert.IsNull(component.Instance.OpenPopupMarker);
        Assert.AreEqual(0, component.FindAll(".bit-map-popup").Count);
    }

    [TestMethod]
    public void BitMapShouldReAnchorAnOpenPopupWhenItsMarkerMoves()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Markers, [new BitMapMarker { Id = "a", Position = new(0, 0) }]);
        });

        component.Instance._OnMarkerClick("a").GetAwaiter().GetResult();
        var anchorsBefore = Context.JSInterop.Invocations.Count(i => i.Identifier == TRACK_ANCHOR);

        component.Render(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Markers, [new BitMapMarker { Id = "a", Position = new(10, 20) }]);
        });

        var anchors = Context.JSInterop.Invocations.Where(i => i.Identifier == TRACK_ANCHOR).ToList();
        Assert.AreEqual(anchorsBefore + 1, anchors.Count, "The popup has to follow its marker");
        Assert.AreEqual(10d, anchors[^1].Arguments[2]);
        Assert.AreEqual(20d, anchors[^1].Arguments[3]);
    }

    [TestMethod]
    public void BitMapShouldCloseThePopupWhenItsMarkerLeavesTheBoundCollection()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
                new BitMapMarker { Id = "c", Position = new(2, 2) },
            ]);
        });

        component.Instance._OnMarkerClick("a").GetAwaiter().GetResult();
        Assert.IsNotNull(component.Instance.OpenPopupMarker);

        component.Render(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "b", Position = new(1, 1) },
                new BitMapMarker { Id = "c", Position = new(2, 2) },
            ]);
        });

        Assert.IsNull(component.Instance.OpenPopupMarker);
    }

    [TestMethod]
    public async Task BitMapPopupShouldBeADialogWithACloseButtonAndAnAccessibleName()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.PopupCloseLabel, "Dismiss");
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0), Alt = "Kyiv office" });
        await component.Instance._OnMarkerClick("a");

        var popup = component.Find(".bit-map-popup");
        Assert.AreEqual("dialog", popup.GetAttribute("role"));
        // Named after the place rather than "popup", so a screen reader announces which one opened.
        Assert.AreEqual("Kyiv office", popup.GetAttribute("aria-label"));

        var close = component.Find(".bit-map-popup-close");
        Assert.AreEqual("Dismiss", close.GetAttribute("aria-label"));

        close.Click();
        Assert.AreEqual(0, component.FindAll(".bit-map-popup").Count);
    }

    [TestMethod]
    public async Task BitMapPopupShouldCloseOnEscape()
    {
        // WCAG 2.1.2: a popup you can open with the keyboard has to be closable with it.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance._OnMarkerClick("a");

        component.Find(".bit-map-popup").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-map-popup").Count);
    }

    [TestMethod]
    public async Task BitMapMarkerListShouldOpenTheTemplatePopupWhenOneIsConfigured()
    {
        // A row in the list should reach the same popup a click on the pin reaches - the
        // provider's own popup would be a different, emptier thing.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerListMode, BitMapMarkerListMode.Visible);
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(1, 2), Alt = "Somewhere" });

        component.Find(".bit-map-marker-table-action").Click();

        Assert.AreEqual("a", component.Instance.OpenPopupMarker?.Id);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.BitMapLeaflet.openMarkerPopup"),
            "With a template configured the provider's own popup is not the one to open");
    }

    [TestMethod]
    public async Task BitMapMarkerListShouldFallBackToTheProviderPopupWithoutATemplate()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerListMode, BitMapMarkerListMode.Visible);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(1, 2) });

        component.Find(".bit-map-marker-table-action").Click();

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.BitMapLeaflet.openMarkerPopup"));
    }

    // ---------------------------------------------------------------- geographic helpers

    [TestMethod]
    public void BitMapLatLngShouldMeasureGreatCircleDistance()
    {
        // London to Paris, which every mapping library agrees is a hair under 344 km.
        var london = new BitMapLatLng(51.5074, -0.1278);
        var paris = new BitMapLatLng(48.8566, 2.3522);

        Assert.AreEqual(343_500, london.DistanceTo(paris), 2_000);
        // Symmetric, and zero against itself.
        Assert.AreEqual(london.DistanceTo(paris), paris.DistanceTo(london), 1e-6);
        Assert.AreEqual(0, london.DistanceTo(london), 1e-9);
    }

    [TestMethod]
    public void BitMapLatLngShouldTravelAlongABearing()
    {
        // One degree of latitude is the radius times one degree in radians, by definition.
        var moved = new BitMapLatLng(0, 0).Offset(BitMapLatLng.EarthRadiusMeters * Math.PI / 180, 0);

        Assert.AreEqual(1, moved.Latitude, 1e-9);
        Assert.AreEqual(0, moved.Longitude, 1e-9);
        // And the trip back measures what it cost.
        Assert.AreEqual(BitMapLatLng.EarthRadiusMeters * Math.PI / 180, new BitMapLatLng(0, 0).DistanceTo(moved), 1e-3);
    }

    [TestMethod]
    public void BitMapLatLngShouldWrapPastTheAntimeridian()
    {
        // Travelling east from just short of 180 lands just past -180, not at an out-of-range
        // longitude the constructor would reject.
        var moved = new BitMapLatLng(0, 179.9).Offset(50_000, 90);

        Assert.IsTrue(moved.Longitude < 0, $"expected a wrapped longitude, got {moved.Longitude}");
        Assert.IsTrue(moved.Longitude > -180);
    }

    [TestMethod]
    public void BitMapLatLngShouldCompareWithATolerance()
    {
        var a = new BitMapLatLng(51.5074, -0.1278);
        var b = new BitMapLatLng(51.50740000001, -0.12780000001);

        Assert.IsFalse(a == b, "an exact comparison should still see two different values");
        Assert.IsTrue(a.IsCloseTo(b, 1e-6));
        Assert.IsFalse(a.IsCloseTo(new BitMapLatLng(51.6, -0.1278), 1e-6));
    }

    [TestMethod]
    public void BitMapLatLngShouldBuildABoxAroundARadius()
    {
        var center = new BitMapLatLng(51.5074, -0.1278);
        var bounds = center.ToBounds(1_000);

        Assert.IsTrue(bounds.Contains(center));
        Assert.IsTrue(bounds.Center.IsCloseTo(center, 1e-6));
        Assert.IsTrue(bounds.Contains(center.Offset(900, 0)));
        Assert.IsFalse(bounds.Contains(center.Offset(2_000, 0)));
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldExposeItsCornersAndCentre()
    {
        var bounds = new BitMapLatLngBounds(new(0, 0), new(10, 20));

        Assert.AreEqual(new BitMapLatLng(10, 0), bounds.NorthWest);
        Assert.AreEqual(new BitMapLatLng(0, 20), bounds.SouthEast);
        Assert.AreEqual(new BitMapLatLng(5, 10), bounds.Center);
        Assert.AreEqual(10, bounds.LatitudeSpan, 1e-9);
        Assert.AreEqual(20, bounds.LongitudeSpan, 1e-9);
        Assert.IsFalse(bounds.IsPoint);
        Assert.IsFalse(bounds.CrossesAntimeridian);
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldMeasureABoxThatCrossesTheAntimeridian()
    {
        // 170E to 170W: twenty degrees the short way round, and a centre on the antimeridian
        // itself rather than at Greenwich.
        var bounds = new BitMapLatLngBounds(new(0, 170), new(10, -170));

        Assert.IsTrue(bounds.CrossesAntimeridian);
        Assert.AreEqual(20, bounds.LongitudeSpan, 1e-9);
        Assert.AreEqual(180, Math.Abs(bounds.Center.Longitude), 1e-9);
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldTestContainment()
    {
        var bounds = new BitMapLatLngBounds(new(0, 0), new(10, 20));

        Assert.IsTrue(bounds.Contains(new BitMapLatLng(5, 10)));
        Assert.IsTrue(bounds.Contains(new BitMapLatLng(0, 0)), "edges count as inside");
        Assert.IsFalse(bounds.Contains(new BitMapLatLng(5, 30)));
        Assert.IsFalse(bounds.Contains(new BitMapLatLng(-1, 10)));

        Assert.IsTrue(bounds.Contains(new BitMapLatLngBounds(new(1, 1), new(9, 19))));
        Assert.IsFalse(bounds.Contains(new BitMapLatLngBounds(new(1, 1), new(9, 21))));
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldTestContainmentAcrossTheAntimeridian()
    {
        var bounds = new BitMapLatLngBounds(new(0, 170), new(10, -170));

        Assert.IsTrue(bounds.Contains(new BitMapLatLng(5, 175)));
        Assert.IsTrue(bounds.Contains(new BitMapLatLng(5, -175)));
        // The complement - almost the whole globe - is outside, which is the whole point of
        // treating an inverted longitude pair as a crossing rather than an error.
        Assert.IsFalse(bounds.Contains(new BitMapLatLng(5, 0)));
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldTestIntersection()
    {
        var bounds = new BitMapLatLngBounds(new(0, 0), new(10, 10));

        Assert.IsTrue(bounds.Intersects(new BitMapLatLngBounds(new(5, 5), new(15, 15))));
        Assert.IsTrue(bounds.Intersects(new BitMapLatLngBounds(new(-5, -5), new(5, 5))));
        Assert.IsTrue(bounds.Intersects(bounds));
        Assert.IsFalse(bounds.Intersects(new BitMapLatLngBounds(new(20, 20), new(30, 30))));
        Assert.IsFalse(bounds.Intersects(new BitMapLatLngBounds(new(0, 20), new(10, 30))),
            "overlapping latitudes alone are not an intersection");
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldGrowToIncludeAPoint()
    {
        var bounds = new BitMapLatLngBounds(new(0, 0), new(10, 10));

        var extended = bounds.Extend(new BitMapLatLng(20, -5));
        Assert.AreEqual(new BitMapLatLng(0, -5), extended.SouthWest);
        Assert.AreEqual(new BitMapLatLng(20, 10), extended.NorthEast);

        // A point already inside changes nothing at all.
        Assert.AreEqual(bounds, bounds.Extend(new BitMapLatLng(5, 5)));
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldKeepItsCrossingWhenExtended()
    {
        // Widening a Pacific box must not flip it into the complementary box that wraps the long
        // way round the globe - which is exactly what a naive min/max would do.
        var bounds = new BitMapLatLngBounds(new(0, 170), new(10, -170));

        var extended = bounds.Extend(new BitMapLatLng(5, 165));

        Assert.IsTrue(extended.CrossesAntimeridian);
        Assert.AreEqual(165, extended.SouthWest.Longitude, 1e-9);
        Assert.AreEqual(-170, extended.NorthEast.Longitude, 1e-9);
        Assert.IsTrue(extended.Contains(new BitMapLatLng(5, 165)));
        Assert.IsFalse(extended.Contains(new BitMapLatLng(5, 0)));
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldPadItself()
    {
        var padded = new BitMapLatLngBounds(new(0, 0), new(10, 10)).Pad(0.1);

        Assert.AreEqual(-1, padded.SouthWest.Latitude, 1e-9);
        Assert.AreEqual(-1, padded.SouthWest.Longitude, 1e-9);
        Assert.AreEqual(11, padded.NorthEast.Latitude, 1e-9);
        Assert.AreEqual(11, padded.NorthEast.Longitude, 1e-9);

        // Latitudes clamp at the poles rather than throwing on the way out.
        var clamped = new BitMapLatLngBounds(new(-80, 0), new(80, 10)).Pad(1);
        Assert.AreEqual(-90, clamped.SouthWest.Latitude, 1e-9);
        Assert.AreEqual(90, clamped.NorthEast.Latitude, 1e-9);
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldBeBuiltFromCoordinatesAndMarkers()
    {
        var bounds = BitMapLatLngBounds.FromCoordinates([new(1, 2), new(3, 4), new(-5, 6)]);
        Assert.AreEqual(new BitMapLatLng(-5, 2), bounds.SouthWest);
        Assert.AreEqual(new BitMapLatLng(3, 6), bounds.NorthEast);

        var fromMarkers = BitMapLatLngBounds.FromMarkers([
            new BitMapMarker { Id = "a", Position = new(1, 2) },
            new BitMapMarker { Id = "b", Position = new(3, 4) },
        ]);
        Assert.AreEqual(new BitMapLatLng(1, 2), fromMarkers.SouthWest);
        Assert.AreEqual(new BitMapLatLng(3, 4), fromMarkers.NorthEast);

        Assert.ThrowsExactly<ArgumentException>(() => BitMapLatLngBounds.FromCoordinates([]));
        Assert.ThrowsExactly<ArgumentNullException>(() => BitMapLatLngBounds.FromCoordinates(null!));
    }

    [TestMethod]
    public void BitMapLatLngBoundsShouldExposeTheWholeWorld()
    {
        Assert.IsTrue(BitMapLatLngBounds.World.Contains(new BitMapLatLng(0, 0)));
        Assert.IsTrue(BitMapLatLngBounds.World.Contains(new BitMapLatLng(-90, -180)));
        Assert.IsTrue(BitMapLatLngBounds.World.Contains(new BitMapLatLng(90, 180)));
    }

    // ---------------------------------------------------------------- camera additions

    [TestMethod]
    public async Task BitMapFitBoundsShouldForwardThePaddingAndTheZoomCeiling()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(FIT_BOUNDS);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.FitBounds(new BitMapLatLngBounds(new(0, 0), new(10, 10)), 24, 12);

        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == FIT_BOUNDS);
        Assert.AreEqual(24, call.Arguments[5]);
        Assert.AreEqual(12d, call.Arguments[6]);
    }

    [TestMethod]
    public async Task BitMapFitBoundsToMarkersShouldForwardTheZoomCeiling()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(FIT_BOUNDS_TO_MARKERS);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.FitBoundsToMarkers(16, 9);

        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == FIT_BOUNDS_TO_MARKERS);
        Assert.AreEqual(16, call.Arguments[1]);
        Assert.AreEqual(9d, call.Arguments[2]);
    }

    [TestMethod]
    public async Task BitMapFitBoundsShouldRejectAnOutOfRangeZoomCeiling()
    {
        SetupSuccessfulMount();
        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            async () => await component.Instance.FitBounds(BitMapLatLngBounds.World, 0, 99));
    }

    [TestMethod]
    public async Task BitMapPanToShouldKeepTheCurrentZoom()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(SET_VIEW);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.PanTo(new BitMapLatLng(1, 2));

        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == SET_VIEW);
        Assert.AreEqual(1d, call.Arguments[1]);
        Assert.AreEqual(2d, call.Arguments[2]);
        Assert.IsNull(call.Arguments[3], "no zoom means keep the one the map already has");
    }

    [TestMethod]
    public async Task BitMapProjectShouldReturnThePixelPositionOfACoordinate()
    {
        SetupSuccessfulMount();
        Context.JSInterop.Setup<JsonElement>(PROJECT, _ => true)
            .SetResult(JsonSerializer.Deserialize<JsonElement>("""{ "x": 12.5, "y": 34 }"""));

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        var point = await component.Instance.Project(new BitMapLatLng(1, 2));

        Assert.IsNotNull(point);
        Assert.AreEqual(12.5, point.Value.X);
        Assert.AreEqual(34, point.Value.Y);
    }

    [TestMethod]
    public async Task BitMapProjectShouldReturnNullForACoordinateThatIsNotOnScreen()
    {
        // The providers answer null for a coordinate outside the viewport - or, on the 3D
        // backend, behind the globe. That is an answer, not a failure.
        SetupSuccessfulMount();
        Context.JSInterop.Setup<JsonElement>(PROJECT, _ => true)
            .SetResult(JsonSerializer.Deserialize<JsonElement>("null"));

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        Assert.IsNull(await component.Instance.Project(new BitMapLatLng(1, 2)));
    }

    [TestMethod]
    public async Task BitMapShouldApplyABoundZoomEvenWithNoBoundCentre()
    {
        // Zoom alone is a legitimate binding, and before the map has reported a viewport there is
        // no centre to restate - so the zoom has to be pushed on its own rather than dropped.
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(SET_VIEW);
        Context.JSInterop.Setup<JsonElement>(GET_VIEW, _ => true).SetResult(ViewPayload(0, 0, 3));

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Bind(p => p.Zoom, (double?)9, v => { });
        });

        await Task.Yield();

        var call = Context.JSInterop.Invocations.Last(i => i.Identifier == SET_VIEW);
        Assert.AreEqual(9d, call.Arguments[3]);
    }

    // ---------------------------------------------------------------- tile overlays

    [TestMethod]
    public async Task BitMapShouldClearEveryTileOverlay()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(ADD_TILE_OVERLAY);
        Context.JSInterop.SetupVoid(REMOVE_TILE_OVERLAY);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddTileOverlay(new BitMapTileOverlay { Id = "radar", UrlTemplate = "https://t/{z}/{x}/{y}.png" });
        await component.Instance.AddTileOverlay(new BitMapTileOverlay { Id = "labels", UrlTemplate = "https://t/{z}/{x}/{y}.png" });

        await component.Instance.ClearTileOverlays();

        Assert.AreEqual(2, Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_TILE_OVERLAY));
        Assert.AreEqual(0, component.Instance.TileOverlayIds.Count);
    }

    [TestMethod]
    public async Task BitMapShouldNotRemoveAnAlreadyHiddenOverlayWhenClearing()
    {
        // A hidden overlay is not on the map, so removing it again would be a wasted round-trip -
        // but it still has to leave the snapshot.
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(ADD_TILE_OVERLAY);
        Context.JSInterop.SetupVoid(REMOVE_TILE_OVERLAY);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddTileOverlay(new BitMapTileOverlay { Id = "radar", UrlTemplate = "https://t/{z}/{x}/{y}.png" });
        await component.Instance.SetTileOverlayVisible("radar", false);

        var before = Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_TILE_OVERLAY);
        await component.Instance.ClearTileOverlays();

        Assert.AreEqual(before, Context.JSInterop.Invocations.Count(i => i.Identifier == REMOVE_TILE_OVERLAY));
        Assert.AreEqual(0, component.Instance.TileOverlayIds.Count);
        Assert.IsFalse(component.Instance.IsTileOverlayVisible("radar"));
    }

    // ---------------------------------------------------------------- marker icon anchor

    [TestMethod]
    public async Task BitMapShouldSendTheMarkersIconAnchor()
    {
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(ADD_MARKER);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddMarker(new BitMapMarker
        {
            Id = "dot",
            Position = new(1, 2),
            IconUrl = "https://example.com/dot.png",
            IconWidth = 24,
            IconHeight = 24,
            IconAnchorX = 12,
            IconAnchorY = 12,
        });

        var payload = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == ADD_MARKER).Arguments[2]!;

        Assert.AreEqual(12, payload["iconAnchorX"]);
        Assert.AreEqual(12, payload["iconAnchorY"]);
    }

    [TestMethod]
    public async Task BitMapShouldLeaveTheIconAnchorUnsetWhenItIsNotGiven()
    {
        // Unset means "the library's own default", which is a pin's tip - not a zero offset.
        SetupSuccessfulMount();
        Context.JSInterop.SetupVoid(ADD_MARKER);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>();

        await component.Instance.AddMarker(new BitMapMarker { Id = "pin", Position = new(1, 2) });

        var payload = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == ADD_MARKER).Arguments[2]!;

        Assert.IsNull(payload["iconAnchorX"]);
        Assert.IsNull(payload["iconAnchorY"]);
    }

    // ---------------------------------------------------------------- clustering

    [TestMethod]
    public void BitMapShouldSendTheClusterBubbleLabelFormatToTheClusteringLayer()
    {
        // The bubble is a keyboard-reachable marker, so its accessible name is user-facing text -
        // and has to be translatable like every other label on the component.
        SetupSuccessfulMount();

        RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.Clustering, new BitMapClustering { AriaLabelFormat = "{0} Orte" });
        });

        var options = (Dictionary<string, object?>)Context.JSInterop.Invocations
            .Single(i => i.Identifier == CLUSTER_CONFIGURE).Arguments[2]!;

        Assert.AreEqual("{0} Orte", options["ariaLabelFormat"]);
    }

    [TestMethod]
    public async Task BitMapShouldCloseThePopupWhenAClusteredMarkerIsRemoved()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance.OpenPopup("a");
        Assert.IsNotNull(component.Instance.OpenPopupMarker);

        await component.Instance.RemoveMarker("a");

        Assert.IsNull(component.Instance.OpenPopupMarker, "a popup for a marker that is gone would hang in space");
    }

    [TestMethod]
    public async Task BitMapShouldCloseThePopupWhenClusteredMarkersAreCleared()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance.OpenPopup("a");

        await component.Instance.ClearMarkers();

        Assert.IsNull(component.Instance.OpenPopupMarker);
    }

    [TestMethod]
    public void BitMapShouldNotDoubleDrawMarkersWhenAClusteredProviderIsSwapped()
    {
        // The clustering layer is re-pointed at the new backend and handed the whole set, so
        // replaying each marker on top of it would draw every one twice.
        const string A_INIT = "BitBlazorUI.TestProviderA.init";
        const string A_DISPOSE = "BitBlazorUI.TestProviderA.dispose";
        const string B_INIT = "BitBlazorUI.TestProviderB.init";
        const string B_ADD_MARKER = "BitBlazorUI.TestProviderB.addMarker";

        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(A_INIT);
        Context.JSInterop.SetupVoid(A_DISPOSE);
        Context.JSInterop.SetupVoid(B_INIT);
        Context.JSInterop.SetupVoid(B_ADD_MARKER);

        var component = RenderComponent<BitMap<TestMapProviderA>>(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderA());
            parameters.Add(p => p.ReplayStateOnProviderSwap, true);
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        component.Instance.AddMarker(new BitMapMarker { Id = "x", Position = new(0, 0) }).GetAwaiter().GetResult();

        component.Render(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderB());
            parameters.Add(p => p.ReplayStateOnProviderSwap, true);
            parameters.Add(p => p.Clustering, new BitMapClustering());
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == B_ADD_MARKER),
            "the clustering layer owns what the provider draws");
        Assert.IsTrue(Context.JSInterop.Invocations.Count(i => i.Identifier == CLUSTER_SET_MARKERS) > 0);
    }

    // ---------------------------------------------------------------- provider swap

    [TestMethod]
    public void BitMapShouldReapplyTheDeclarativeMarkersAfterAProviderSwap()
    {
        // Markers passed as a collection are the component's own state, not something the
        // consumer applied imperatively - so they come back whether or not the replay opt-in is
        // set. Without this a swap silently empties the map.
        const string A_INIT = "BitBlazorUI.TestProviderA.init";
        const string A_DISPOSE = "BitBlazorUI.TestProviderA.dispose";
        const string B_INIT = "BitBlazorUI.TestProviderB.init";
        const string B_SYNC_MARKERS = "BitBlazorUI.TestProviderB.syncMarkers";
        const string A_ADD_MARKER = "BitBlazorUI.TestProviderA.addMarker";
        const string A_SYNC_MARKERS = "BitBlazorUI.TestProviderA.syncMarkers";

        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(A_INIT);
        Context.JSInterop.SetupVoid(A_DISPOSE);
        Context.JSInterop.SetupVoid(B_INIT);
        Context.JSInterop.SetupVoid(A_ADD_MARKER);
        Context.JSInterop.SetupVoid(A_SYNC_MARKERS);
        Context.JSInterop.SetupVoid(B_SYNC_MARKERS);

        var markers = new List<BitMapMarker>
        {
            new() { Id = "a", Position = new(0, 0) },
            new() { Id = "b", Position = new(1, 1) },
        };

        var component = RenderComponent<BitMap<TestMapProviderA>>(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderA());
            parameters.Add(p => p.Markers, markers);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Provider, new TestMapProviderB());
            parameters.Add(p => p.Markers, markers);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == B_SYNC_MARKERS),
            "the bound collection must be pushed to the new backend");
        CollectionAssert.AreEqual(new[] { "a", "b" }, component.Instance.MarkerIds.ToArray());
    }

    // ---------------------------------------------------------------- popup

    [TestMethod]
    public async Task BitMapPopupShouldBeFocusableWithoutBeingATabStop()
    {
        // A dialog nobody is standing in is a dialog whose Escape handler never fires. It is
        // focused on open, which needs a tabindex - but it must not add a tab stop of its own.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
        });

        await component.Instance.AddMarker(new BitMapMarker { Id = "a", Position = new(0, 0) });
        await component.Instance._OnMarkerClick("a");

        Assert.AreEqual("-1", component.Find(".bit-map-popup").GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitMapShouldKeepAnOpenPopupInStepAcrossAWholesaleMarkerReplacement()
    {
        // A replacement large enough to take the batched path is exactly when an open popup is
        // most likely to be pointing at a marker that is gone.
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
            ]);
        });

        await component.Instance._OnMarkerClick("a");
        Assert.IsNotNull(component.Instance.OpenPopupMarker);

        var batchesBefore = Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC_MARKERS);

        // Everything changes, so the reconciler takes the single batched replace.
        component.Render(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "x", Position = new(5, 5) },
                new BitMapMarker { Id = "y", Position = new(6, 6) },
            ]);
        });

        Assert.AreEqual(batchesBefore + 1, Context.JSInterop.Invocations.Count(i => i.Identifier == SYNC_MARKERS));
        Assert.IsNull(component.Instance.OpenPopupMarker);
    }

    [TestMethod]
    public async Task BitMapShouldReAnchorAnOpenPopupAcrossAWholesaleMarkerReplacement()
    {
        SetupSuccessfulMount();

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(0, 0) },
                new BitMapMarker { Id = "b", Position = new(1, 1) },
            ]);
        });

        await component.Instance._OnMarkerClick("a");
        var anchorsBefore = Context.JSInterop.Invocations.Count(i => i.Identifier == TRACK_ANCHOR);

        component.Render(parameters =>
        {
            parameters.Add(p => p.MarkerPopupTemplate, EmptyPopupTemplate);
            parameters.Add(p => p.Markers,
            [
                new BitMapMarker { Id = "a", Position = new(9, 9) },
                new BitMapMarker { Id = "c", Position = new(2, 2) },
            ]);
        });

        Assert.IsNotNull(component.Instance.OpenPopupMarker);
        Assert.AreEqual(9d, component.Instance.OpenPopupMarker.Position.Latitude);
        Assert.IsTrue(Context.JSInterop.Invocations.Count(i => i.Identifier == TRACK_ANCHOR) > anchorsBefore,
            "the popup has to follow its marker to its new position");
    }

    // ---------------------------------------------------------------- lazy load teardown

    [TestMethod]
    public async Task BitMapShouldStopWatchingForVisibilityWhenDisposedBeforeItEverAppears()
    {
        // The visibility observer is the only thing holding the map's container while it waits
        // below the fold. Nothing else would ever take it down.
        Context.JSInterop.SetupVoid(INIT_STYLESHEETS);
        Context.JSInterop.SetupVoid(INIT_SCRIPTS);
        Context.JSInterop.SetupVoid(CHROME_CANCEL_WAIT_VISIBLE);

        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.LazyLoad, true);
        });

        await component.Instance.DisposeAsync();

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == CHROME_CANCEL_WAIT_VISIBLE));
    }

    /// <summary>A popup body with no content of its own, for tests that only care about the shell.</summary>
    private static readonly Microsoft.AspNetCore.Components.RenderFragment<BitMapMarker> EmptyPopupTemplate =
        _ => builder => builder.AddContent(0, string.Empty);

    /// <summary>Builds the payload shape the providers send to <c>OnViewChanged</c>.</summary>
    private static JsonElement ViewPayload(double lat, double lng, double zoom)
        // Invariant, not the current culture: a comma-decimal culture would format 51.5 as "51,5"
        // and hand JsonSerializer a payload that is no longer valid JSON.
        => JsonSerializer.Deserialize<JsonElement>(FormattableString.Invariant($$"""
            {
              "center": { "lat": {{lat}}, "lng": {{lng}} },
              "zoom": {{zoom}},
              "bounds": {
                "southWest": { "lat": {{lat - 1}}, "lng": {{lng - 1}} },
                "northEast": { "lat": {{lat + 1}}, "lng": {{lng + 1}} }
              }
            }
            """));

    private sealed class WebGlTestProvider : BitMapProviderBase
    {
        public override string Key => "webgl-test";
        public override string JsObjectName => "WebGlTestProvider";
        public override BitMapWebGlRequirement WebGlRequirement => BitMapWebGlRequirement.WebGl;
        public override object BuildOptionsPayload() => GetCommonOptions();
    }

    private class SharedAssetProviderA : BitMapProviderBase
    {
        public override string Key => "shared-a";
        public override string JsObjectName => "SharedAssetProviderA";
        public override IReadOnlyList<string> Scripts => ["https://cdn.example.com/shared-map.js"];
        public override object BuildOptionsPayload() => GetCommonOptions();
    }

    private sealed class SharedAssetProviderB : SharedAssetProviderA
    {
        public override string Key => "shared-b";
        public override string JsObjectName => "SharedAssetProviderB";
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
