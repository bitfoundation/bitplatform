namespace Bit.BlazorUI;

/// <summary>
/// Provider-agnostic JS runtime helpers for <see cref="BitMap{TMapProvider}"/>.
/// All calls go through <c>BitBlazorUI.&lt;JsObjectName&gt;.&lt;method&gt;</c> so each
/// provider exposes its own implementation under the same shape.
/// </summary>
internal static class BitMapJsRuntimeExtensions
{
    public static ValueTask BitMapInit<TProvider>(this IJSRuntime jsRuntime,
                                                  string jsObjectName,
                                                  string id,
                                                  string canvasId,
                                                  ElementReference element,
                                                  DotNetObjectReference<BitMap<TProvider>>? dotnetObj,
                                                  object options)
        where TProvider : class, IBitMapProvider, new()
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.init", id, canvasId, element, dotnetObj, options);
    }

    public static ValueTask BitMapSync(this IJSRuntime jsRuntime, string jsObjectName, string id, object options)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.sync", id, options);
    }

    public static ValueTask BitMapDispose(this IJSRuntime jsRuntime, string jsObjectName, string id)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.dispose", id);
    }

    public static ValueTask BitMapInvalidateSize(this IJSRuntime jsRuntime, string jsObjectName, string id)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.invalidateSize", id);
    }

    public static ValueTask<System.Text.Json.JsonElement> BitMapGetView(this IJSRuntime jsRuntime, string jsObjectName, string id)
    {
        return jsRuntime.Invoke<System.Text.Json.JsonElement>($"BitBlazorUI.{jsObjectName}.getView", id);
    }

    public static ValueTask BitMapSetView(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                          double lat, double lng, double? zoom, bool animate)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.setView", id, lat, lng, zoom, animate);
    }

    public static ValueTask BitMapFlyTo(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                        double lat, double lng, double? zoom)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.flyTo", id, lat, lng, zoom);
    }

    public static ValueTask BitMapZoomBy(this IJSRuntime jsRuntime, string jsObjectName, string id, double delta, bool animate)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.zoomBy", id, delta, animate);
    }

    public static ValueTask BitMapPanBy(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                        double offsetX, double offsetY, bool animate)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.panBy", id, offsetX, offsetY, animate);
    }

    public static ValueTask BitMapFitBounds(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                            double swLat, double swLng,
                                            double neLat, double neLng,
                                            int paddingPixels,
                                            double maxZoom)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.fitBounds", id, swLat, swLng, neLat, neLng, paddingPixels, maxZoom);
    }

    public static ValueTask BitMapFitBoundsToMarkers(this IJSRuntime jsRuntime, string jsObjectName, string id, int paddingPixels, double maxZoom)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.fitBoundsToMarkers", id, paddingPixels, maxZoom);
    }

    public static ValueTask<System.Text.Json.JsonElement> BitMapProject(this IJSRuntime jsRuntime, string jsObjectName, string id, double lat, double lng)
    {
        return jsRuntime.Invoke<System.Text.Json.JsonElement>($"BitBlazorUI.{jsObjectName}.project", id, lat, lng);
    }

    public static ValueTask BitMapAddMarker(this IJSRuntime jsRuntime, string jsObjectName, string id, string markerId, object marker)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.addMarker", id, markerId, marker);
    }

    public static ValueTask BitMapRemoveMarker(this IJSRuntime jsRuntime, string jsObjectName, string id, string markerId)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.removeMarker", id, markerId);
    }

    public static ValueTask BitMapClearMarkers(this IJSRuntime jsRuntime, string jsObjectName, string id)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.clearMarkers", id);
    }

    public static ValueTask BitMapSyncMarkers(this IJSRuntime jsRuntime, string jsObjectName, string id, string[] markerIds, object[] markers)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.syncMarkers", id, markerIds, markers);
    }

    public static ValueTask BitMapSetMarkerPosition(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                                    string markerId, double lat, double lng)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.setMarkerPosition", id, markerId, lat, lng);
    }

    public static ValueTask BitMapOpenMarkerPopup(this IJSRuntime jsRuntime, string jsObjectName, string id, string markerId)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.openMarkerPopup", id, markerId);
    }

    public static ValueTask BitMapAddPolyline(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                              string layerId, object[] path, object? style)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.addPolyline", id, layerId, path, style);
    }

    public static ValueTask BitMapAddPolygon(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                             string layerId, object[] ring, object? style)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.addPolygon", id, layerId, ring, style);
    }

    public static ValueTask BitMapAddCircle(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                            string layerId, double lat, double lng, double radiusMeters, object? style)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.addCircle", id, layerId, lat, lng, radiusMeters, style);
    }

    public static ValueTask BitMapAddRectangle(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                               string layerId, double swLat, double swLng,
                                               double neLat, double neLng, object? style)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.addRectangle", id, layerId, swLat, swLng, neLat, neLng, style);
    }

    public static ValueTask BitMapAddGeoJson(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                             string layerId, string geoJson, object? style)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.addGeoJson", id, layerId, geoJson, style);
    }

    public static ValueTask BitMapRemoveLayer(this IJSRuntime jsRuntime, string jsObjectName, string id, string layerId)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.removeLayer", id, layerId);
    }

    public static ValueTask BitMapClearVectorLayers(this IJSRuntime jsRuntime, string jsObjectName, string id)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.clearVectorLayers", id);
    }

    public static ValueTask BitMapAddTileOverlay(this IJSRuntime jsRuntime, string jsObjectName, string id, object overlay)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.addTileOverlay", id, overlay);
    }

    public static ValueTask BitMapRemoveTileOverlay(this IJSRuntime jsRuntime, string jsObjectName, string id, string overlayId)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.removeTileOverlay", id, overlayId);
    }



    // ---- Provider-agnostic chrome (BitMapChrome.ts) ----------------------------------
    // These are deliberately NOT routed through the provider's JS object: resizing,
    // scroll-jacking, keyboard escape and WebGL support are handled once, above whichever
    // mapping library happens to be active, so all seven backends behave identically.

    public static ValueTask<bool> BitMapChromeHasWebGl(this IJSRuntime jsRuntime, int requiredVersion)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.BitMapChrome.hasWebGl", requiredVersion);
    }

    public static ValueTask<bool> BitMapChromePrefersReducedMotion(this IJSRuntime jsRuntime)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.BitMapChrome.prefersReducedMotion");
    }

    public static ValueTask BitMapChromeWaitForVisible(this IJSRuntime jsRuntime, string canvasId, ElementReference element, string rootMargin)
    {
        // The promise resolves only once the map scrolls into view, which may be minutes away or
        // never - so this one call opts out of the default interop timeout instead of failing a
        // lazy map that is simply still below the fold.
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapChrome.waitForVisible", CancellationToken.None, canvasId, element, rootMargin);
    }

    public static ValueTask BitMapChromeAttach<TProvider>(this IJSRuntime jsRuntime,
                                                          string id,
                                                          string canvasId,
                                                          ElementReference element,
                                                          DotNetObjectReference<BitMap<TProvider>>? dotnetObj,
                                                          object options)
        where TProvider : class, IBitMapProvider, new()
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapChrome.attach", id, canvasId, element, dotnetObj, options);
    }

    public static ValueTask BitMapChromeDetach(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapChrome.detach", id);
    }

    public static ValueTask BitMapChromeCancelWaitForVisible(this IJSRuntime jsRuntime, string canvasId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapChrome.cancelWaitForVisible", canvasId);
    }

    public static ValueTask BitMapChromeTrackAnchor(this IJSRuntime jsRuntime, string id, string elementId, double lat, double lng)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapChrome.trackAnchor", id, elementId, lat, lng);
    }

    public static ValueTask BitMapChromeUntrackAnchor(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapChrome.untrackAnchor", id);
    }

    public static ValueTask<bool> BitMapChromeRequestFullscreen(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.BitMapChrome.requestFullscreen", id);
    }

    public static ValueTask<bool> BitMapChromeExitFullscreen(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.BitMapChrome.exitFullscreen", id);
    }

    public static ValueTask BitMapClusterConfigure(this IJSRuntime jsRuntime, string id, string jsObjectName, object options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapCluster.configure", id, jsObjectName, options);
    }

    public static ValueTask BitMapClusterDisable(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapCluster.disable", id);
    }

    public static ValueTask BitMapClusterDiscard(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapCluster.discard", id);
    }

    public static ValueTask BitMapClusterSetMarkers(this IJSRuntime jsRuntime, string id, string[] markerIds, object[] markers)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapCluster.setMarkers", id, markerIds, markers);
    }

    public static ValueTask BitMapClusterRender(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitMapCluster.render", id);
    }

    public static ValueTask<int> BitMapClusterExpand(this IJSRuntime jsRuntime, string id, string clusterId, int paddingPixels, bool zoom)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.BitMapCluster.expand", id, clusterId, paddingPixels, zoom);
    }

    public static ValueTask<System.Text.Json.JsonElement> BitMapChromeLocate(this IJSRuntime jsRuntime,
                                                                            bool enableHighAccuracy,
                                                                            int timeoutMilliseconds,
                                                                            int maximumAgeMilliseconds)
    {
        return jsRuntime.Invoke<System.Text.Json.JsonElement>("BitBlazorUI.BitMapChrome.locate",
            enableHighAccuracy, timeoutMilliseconds, maximumAgeMilliseconds);
    }
}
