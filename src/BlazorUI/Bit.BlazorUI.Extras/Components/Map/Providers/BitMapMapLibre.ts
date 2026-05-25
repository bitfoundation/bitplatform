namespace BitBlazorUI {
    /**
     * MapLibre GL JS provider. Loads from a CDN; the C# layer adds the script tag.
     * Routes every method through BitMapGlBase using the 'maplibre' provider key.
     */
    export class BitMapMapLibre {
        private static readonly _key = 'maplibre';
        private static readonly _global = 'maplibregl';
        private static readonly _defaultStyle = 'https://demotiles.maplibre.org/style.json';

        public static init(id: string, canvasId: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            return BitMapGlBase.init(BitMapMapLibre._key, BitMapMapLibre._global, BitMapMapLibre._defaultStyle, id, canvasId, element, dotnetObj, options);
        }
        public static sync(id: string, options: any) { return BitMapGlBase.sync(BitMapMapLibre._key, id, options); }
        public static dispose(id: string) { return BitMapGlBase.dispose(BitMapMapLibre._key, id); }
        public static invalidateSize(id: string) { return BitMapGlBase.invalidateSize(BitMapMapLibre._key, id); }
        public static getView(id: string) { return BitMapGlBase.getView(BitMapMapLibre._key, id); }
        public static setView(id: string, lat: number, lng: number, zoom: number | null, animate: boolean) {
            return BitMapGlBase.setView(BitMapMapLibre._key, id, lat, lng, zoom, animate);
        }
        public static flyTo(id: string, lat: number, lng: number, zoom: number | null) {
            return BitMapGlBase.flyTo(BitMapMapLibre._key, id, lat, lng, zoom);
        }
        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number) {
            return BitMapGlBase.fitBounds(BitMapMapLibre._key, id, swLat, swLng, neLat, neLng, paddingPx);
        }
        public static fitBoundsToMarkers(id: string, paddingPx: number) {
            return BitMapGlBase.fitBoundsToMarkers(BitMapMapLibre._key, id, paddingPx);
        }
        public static addMarker(id: string, markerId: string, opts: any) { return BitMapGlBase.addMarker(BitMapMapLibre._key, id, markerId, opts); }
        public static removeMarker(id: string, markerId: string) { return BitMapGlBase.removeMarker(BitMapMapLibre._key, id, markerId); }
        public static clearMarkers(id: string) { return BitMapGlBase.clearMarkers(BitMapMapLibre._key, id); }
        public static syncMarkers(id: string, markerIds: string[], markers: any[]) {
            return BitMapGlBase.syncMarkers(BitMapMapLibre._key, id, markerIds, markers);
        }
        public static setMarkerPosition(id: string, markerId: string, lat: number, lng: number) {
            return BitMapGlBase.setMarkerPosition(BitMapMapLibre._key, id, markerId, lat, lng);
        }
        public static openMarkerPopup(id: string, markerId: string) { return BitMapGlBase.openMarkerPopup(BitMapMapLibre._key, id, markerId); }
        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            return BitMapGlBase.addPolyline(BitMapMapLibre._key, id, layerId, latlngs, style);
        }
        public static addPolygon(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            return BitMapGlBase.addPolygon(BitMapMapLibre._key, id, layerId, latlngs, style);
        }
        public static addCircle(id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            return BitMapGlBase.addCircle(BitMapMapLibre._key, id, layerId, lat, lng, radiusMeters, style);
        }
        public static addRectangle(id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            return BitMapGlBase.addRectangle(BitMapMapLibre._key, id, layerId, swLat, swLng, neLat, neLng, style);
        }
        public static addGeoJson(id: string, layerId: string, geoJsonString: string, style: any) {
            return BitMapGlBase.addGeoJson(BitMapMapLibre._key, id, layerId, geoJsonString, style);
        }
        public static removeLayer(id: string, layerId: string) { return BitMapGlBase.removeLayer(BitMapMapLibre._key, id, layerId); }
        public static clearVectorLayers(id: string) { return BitMapGlBase.clearVectorLayers(BitMapMapLibre._key, id); }
        public static addTileOverlay(id: string, opts: any) { return BitMapGlBase.addTileOverlay(BitMapMapLibre._key, id, opts); }
        public static removeTileOverlay(id: string, overlayId: string) { return BitMapGlBase.removeTileOverlay(BitMapMapLibre._key, id, overlayId); }
    }
}
