// Loaded as <script type="module"> by the OpenLayers provider via Extras.initScripts.
// Native dynamic import() inside an ES module is CSP-friendly — it does not require
// 'unsafe-eval', unlike the `new Function('return import(u)')` workaround that the
// concatenated, non-module BitBlazorUI bundle would otherwise need.
//
// The resolved bundle (each OpenLayers sub-module mapped to a flat object) is exposed
// on globalThis.__bitMapOlBundle as a Promise so the provider can await it from its
// non-module context.
//
// Keep OL_VER below in sync with `BitOpenLayersMapProvider.OpenLayersVersion` (which
// drives the stylesheet URL and the C# XML doc); both reference the same release.
const OL_VER = '10.5.0';
const u = (p) => `https://esm.sh/ol@${OL_VER}${p ? '/' + p : ''}?bundle`;

// Guard against re-execution: if this loader script is injected twice (e.g. two
// BitMap instances initializing concurrently before the first script tag finishes
// parsing), reuse the existing Promise so we don't issue a duplicate set of
// dynamic imports for the same OpenLayers bundle.
const p = globalThis.__bitMapOlBundle || Promise.all([
    import(u()),
    import(u('control')),
    import(u('style')),
    import(u('geom')),
    import(u('source')),
    import(u('layer')),
    import(u('format')),
    import(u('proj')),
    import(u('interaction')),
]).then(([ol, olControl, olStyle, olGeom, olSource, olLayer, olFormat, olProj, olInteraction]) => ({
    Map: ol.Map,
    View: ol.View,
    Overlay: ol.Overlay,
    Feature: ol.Feature,
    TileLayer: olLayer.Tile,
    VectorLayer: olLayer.Vector,
    XYZ: olSource.XYZ,
    VectorSource: olSource.Vector,
    Point: olGeom.Point,
    LineString: olGeom.LineString,
    Polygon: olGeom.Polygon,
    GeoJSON: olFormat.GeoJSON,
    Style: olStyle.Style,
    Fill: olStyle.Fill,
    Stroke: olStyle.Stroke,
    Icon: olStyle.Icon,
    CircleStyle: olStyle.Circle,
    ScaleLine: olControl.ScaleLine,
    defaults: olControl.defaults,
    fromLonLat: olProj.fromLonLat,
    toLonLat: olProj.toLonLat,
    transformExtent: olProj.transformExtent,
    Translate: olInteraction.Translate,
}));

// Clear the global on failure so future loader injections can retry the imports
// instead of being permanently stuck on a rejected promise. This handler only
// performs cleanup — it must not rethrow, otherwise it would surface a second
// unhandled rejection in addition to the original `p`. Awaiters receive the
// original rejection through `p` itself.
p.catch(() => {
    if (globalThis.__bitMapOlBundle === p) delete globalThis.__bitMapOlBundle;
});

globalThis.__bitMapOlBundle = p;
