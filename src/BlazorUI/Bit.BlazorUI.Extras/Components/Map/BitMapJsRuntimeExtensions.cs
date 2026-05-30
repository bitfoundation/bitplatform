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

    public static ValueTask BitMapFitBounds(this IJSRuntime jsRuntime, string jsObjectName, string id,
                                            double swLat, double swLng,
                                            double neLat, double neLng,
                                            int paddingPixels)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.fitBounds", id, swLat, swLng, neLat, neLng, paddingPixels);
    }

    public static ValueTask BitMapFitBoundsToMarkers(this IJSRuntime jsRuntime, string jsObjectName, string id, int paddingPixels)
    {
        return jsRuntime.InvokeVoid($"BitBlazorUI.{jsObjectName}.fitBoundsToMarkers", id, paddingPixels);
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
}
