namespace BitBlazorUI {
    /**
     * Mapbox GL JS provider. Loads from the official Mapbox CDN; the C# layer adds the script tag.
     * Routes every method through BitMapGlBase using the 'mapbox' provider key.
     */
    export class BitMapMapbox {
        private static readonly _key = 'mapbox';
        private static readonly _global = 'mapboxgl';
        private static readonly _defaultStyle = 'mapbox://styles/mapbox/streets-v12';

        public static init(id: string, canvasId: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            return BitMapGlBase.init(BitMapMapbox._key, BitMapMapbox._global, BitMapMapbox._defaultStyle, id, canvasId, element, dotnetObj, options);
        }
        public static sync(id: string, options: any) { return BitMapGlBase.sync(BitMapMapbox._key, id, options); }
        public static dispose(id: string) { return BitMapGlBase.dispose(BitMapMapbox._key, id); }
        public static invalidateSize(id: string) { return BitMapGlBase.invalidateSize(BitMapMapbox._key, id); }
        public static getView(id: string) { return BitMapGlBase.getView(BitMapMapbox._key, id); }
        public static setView(id: string, lat: number, lng: number, zoom: number | null, animate: boolean) {
            return BitMapGlBase.setView(BitMapMapbox._key, id, lat, lng, zoom, animate);
        }
        public static flyTo(id: string, lat: number, lng: number, zoom: number | null) {
            return BitMapGlBase.flyTo(BitMapMapbox._key, id, lat, lng, zoom);
        }
        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number) {
            return BitMapGlBase.fitBounds(BitMapMapbox._key, id, swLat, swLng, neLat, neLng, paddingPx);
        }
        public static fitBoundsToMarkers(id: string, paddingPx: number) {
            return BitMapGlBase.fitBoundsToMarkers(BitMapMapbox._key, id, paddingPx);
        }
        public static addMarker(id: string, markerId: string, opts: any) { return BitMapGlBase.addMarker(BitMapMapbox._key, id, markerId, opts); }
        public static removeMarker(id: string, markerId: string) { return BitMapGlBase.removeMarker(BitMapMapbox._key, id, markerId); }
        public static clearMarkers(id: string) { return BitMapGlBase.clearMarkers(BitMapMapbox._key, id); }
        public static syncMarkers(id: string, markerIds: string[], markers: any[]) {
            return BitMapGlBase.syncMarkers(BitMapMapbox._key, id, markerIds, markers);
        }
        public static setMarkerPosition(id: string, markerId: string, lat: number, lng: number) {
            return BitMapGlBase.setMarkerPosition(BitMapMapbox._key, id, markerId, lat, lng);
        }
        public static openMarkerPopup(id: string, markerId: string) { return BitMapGlBase.openMarkerPopup(BitMapMapbox._key, id, markerId); }
        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            return BitMapGlBase.addPolyline(BitMapMapbox._key, id, layerId, latlngs, style);
        }
        public static addPolygon(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            return BitMapGlBase.addPolygon(BitMapMapbox._key, id, layerId, latlngs, style);
        }
        public static addCircle(id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            return BitMapGlBase.addCircle(BitMapMapbox._key, id, layerId, lat, lng, radiusMeters, style);
        }
        public static addRectangle(id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            return BitMapGlBase.addRectangle(BitMapMapbox._key, id, layerId, swLat, swLng, neLat, neLng, style);
        }
        public static addGeoJson(id: string, layerId: string, geoJsonString: string, style: any) {
            return BitMapGlBase.addGeoJson(BitMapMapbox._key, id, layerId, geoJsonString, style);
        }
        public static removeLayer(id: string, layerId: string) { return BitMapGlBase.removeLayer(BitMapMapbox._key, id, layerId); }
        public static clearVectorLayers(id: string) { return BitMapGlBase.clearVectorLayers(BitMapMapbox._key, id); }
        public static addTileOverlay(id: string, opts: any) { return BitMapGlBase.addTileOverlay(BitMapMapbox._key, id, opts); }
        public static removeTileOverlay(id: string, overlayId: string) { return BitMapGlBase.removeTileOverlay(BitMapMapbox._key, id, overlayId); }
    }
}
