namespace BitBlazorUI {

    /**
     * OpenLayers provider. Loads OpenLayers ES modules from esm.sh on first init.
     * Mirrors the public surface used by every BitMap provider.
     */
    export class BitMapOpenLayers {
        private static readonly _OL_VER = '10.5.0';
        private static _olLoadPromise: Promise<any> | null = null;

        private static _maps: { [id: string]: {
            ol: any, map: any, dotnetObj: DotNetObject | null | undefined,
            baseTileLayer: any, markers: { [k: string]: any },
            layers: { [k: string]: any }, tileOverlays: { [k: string]: any },
            scaleLine: any, zIndexCounter: number,
            markerSource: any, markerLayer: any,
        } } = {};

        public static async init(id: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            const ol = await BitMapOpenLayers._loadOl();
            const o = options || {};
            const lng0 = o.center?.lng ?? -0.09, lat0 = o.center?.lat ?? 51.505;
            const zoom = o.zoom ?? 13;

            const baseTile = new ol.TileLayer({
                source: new ol.XYZ({
                    url: (o.tileUrl || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png').replace('{s}', 'a'),
                    maxZoom: o.tileMaxZoom ?? 19,
                    attributions: o.tileAttribution || '',
                }),
                opacity: o.tileOpacity ?? 1,
            });

            const map = new ol.Map({
                target: element,
                layers: [baseTile],
                view: new ol.View({
                    center: ol.fromLonLat([lng0, lat0]),
                    zoom,
                    minZoom: o.minZoom ?? undefined,
                    maxZoom: o.maxZoom ?? undefined,
                }),
                controls: ol.defaults({
                    attribution: o.attributionControl !== false,
                    zoom: o.zoomControl !== false,
                    rotate: false,
                }),
            });

            const markerSource = new ol.VectorSource();
            const markerLayer = new ol.VectorLayer({ source: markerSource, zIndex: 900 });
            map.addLayer(markerLayer);

            const state = {
                ol, map, dotnetObj,
                baseTileLayer: baseTile,
                markers: {} as any,
                layers: {} as any,
                tileOverlays: {} as any,
                scaleLine: null as any,
                zIndexCounter: 100,
                markerSource, markerLayer,
            };

            BitMapOpenLayers._ensureScale(state, !!o.showScaleControl, !!o.scaleControlImperial);
            BitMapOpenLayers._applyInteractions(state, o);
            BitMapOpenLayers._wireEvents(state);

            BitMapOpenLayers._maps[id] = state;
            queueMicrotask(() => map.updateSize());
        }

        public static sync(id: string, options: any) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            const ol = s.ol, view = s.map.getView();
            const o = options || {};
            const lng0 = o.center?.lng ?? -0.09, lat0 = o.center?.lat ?? 51.505;
            view.setCenter(ol.fromLonLat([lng0, lat0]));
            if (o.zoom != null) view.setZoom(o.zoom);

            s.baseTileLayer.setSource(new ol.XYZ({
                url: (o.tileUrl || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png').replace('{s}', 'a'),
                maxZoom: o.tileMaxZoom ?? 19,
                attributions: o.tileAttribution || '',
            }));
            s.baseTileLayer.setOpacity(o.tileOpacity ?? 1);

            BitMapOpenLayers._ensureScale(s, !!o.showScaleControl, !!o.scaleControlImperial);
            BitMapOpenLayers._applyInteractions(s, o);
        }

        public static dispose(id: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            try {
                for (const k in s.tileOverlays) s.map.removeLayer(s.tileOverlays[k]);
                if (s.scaleLine) s.map.removeControl(s.scaleLine);
                s.map.setTarget(null);
            } catch { /* ignore */ }
            s.dotnetObj = null;
            delete BitMapOpenLayers._maps[id];
        }

        public static invalidateSize(id: string) {
            const s = BitMapOpenLayers._maps[id];
            if (s) s.map.updateSize();
        }

        public static getView(id: string) {
            const s = BitMapOpenLayers._require(id);
            return BitMapOpenLayers._readView(s);
        }

        public static setView(id: string, lat: number, lng: number, zoom: number | null, _animate: boolean) {
            const s = BitMapOpenLayers._require(id);
            const v = s.map.getView();
            v.setCenter(s.ol.fromLonLat([lng, lat]));
            v.setZoom(zoom ?? v.getZoom() ?? 0);
        }

        public static flyTo(id: string, lat: number, lng: number, zoom: number | null) {
            const s = BitMapOpenLayers._require(id);
            const v = s.map.getView();
            v.animate({ center: s.ol.fromLonLat([lng, lat]), zoom: zoom ?? v.getZoom(), duration: 1200 });
        }

        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number) {
            const s = BitMapOpenLayers._require(id);
            const ol = s.ol;
            const extent = ol.transformExtent([Math.min(swLng, neLng), Math.min(swLat, neLat), Math.max(swLng, neLng), Math.max(swLat, neLat)], 'EPSG:4326', 'EPSG:3857');
            const pad = paddingPx ?? 48;
            s.map.getView().fit(extent, { padding: [pad, pad, pad, pad], maxZoom: 18, duration: 0 });
        }

        public static fitBoundsToMarkers(id: string, paddingPx: number) {
            const s = BitMapOpenLayers._require(id);
            const ext = s.markerSource.getExtent();
            if (!ext || !Number.isFinite(ext[0])) return;
            const pad = paddingPx ?? 48;
            s.map.getView().fit(ext, { padding: [pad, pad, pad, pad], maxZoom: 18, duration: 0 });
        }

        public static addMarker(id: string, markerId: string, opts: any) {
            const s = BitMapOpenLayers._require(id);
            const ol = s.ol;
            const f = new ol.Feature({
                geometry: new ol.Point(ol.fromLonLat([opts.lng, opts.lat])),
                markerId, popupHtml: opts.popupHtml || '', title: opts.title || '',
                draggable: !!opts.draggable,
            });
            f.setId(markerId);
            f.setStyle(BitMapOpenLayers._markerStyle(ol, opts));
            s.markerSource.addFeature(f);
            const existing = s.markers[markerId];
            if (existing) try { s.markerSource.removeFeature(existing); } catch { /* ignore */ }
            s.markers[markerId] = f;
        }

        public static removeMarker(id: string, markerId: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            const f = s.markers[markerId];
            if (f) { try { s.markerSource.removeFeature(f); } catch { /* ignore */ } delete s.markers[markerId]; }
        }

        public static clearMarkers(id: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            s.markerSource.clear();
            s.markers = {};
        }

        public static setMarkerPosition(id: string, markerId: string, lat: number, lng: number) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            const f = s.markers[markerId];
            if (f) f.getGeometry().setCoordinates(s.ol.fromLonLat([lng, lat]));
        }

        public static openMarkerPopup(_id: string, _markerId: string) { /* OpenLayers has no built-in popup; no-op */ }

        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapOpenLayers._require(id);
            const ol = s.ol;
            const coords = latlngs.map(p => ol.fromLonLat([p.lng, p.lat]));
            const f = new ol.Feature({ geometry: new ol.LineString(coords) });
            f.setStyle(new ol.Style({ stroke: BitMapOpenLayers._stroke(ol, style) }));
            BitMapOpenLayers._addVectorLayer(s, layerId, f, 'polyline');
        }

        public static addPolygon(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapOpenLayers._require(id);
            const ol = s.ol;
            const ring = latlngs.map(p => ol.fromLonLat([p.lng, p.lat]));
            if (ring.length && (ring[0][0] !== ring[ring.length - 1][0] || ring[0][1] !== ring[ring.length - 1][1])) ring.push(ring[0]);
            const f = new ol.Feature({ geometry: new ol.Polygon([ring]) });
            f.setStyle(new ol.Style({ stroke: BitMapOpenLayers._stroke(ol, style), fill: BitMapOpenLayers._fill(ol, style) }));
            BitMapOpenLayers._addVectorLayer(s, layerId, f, 'polygon');
        }

        public static addCircle(id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            const s = BitMapOpenLayers._require(id);
            const ol = s.ol;
            const ring = BitMapHelpers.circleRingLngLat(lat, lng, radiusMeters).map(p => ol.fromLonLat(p));
            const f = new ol.Feature({ geometry: new ol.Polygon([ring]) });
            f.setStyle(new ol.Style({ stroke: BitMapOpenLayers._stroke(ol, style), fill: BitMapOpenLayers._fill(ol, style) }));
            BitMapOpenLayers._addVectorLayer(s, layerId, f, 'circle');
        }

        public static addRectangle(id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            const s = BitMapOpenLayers._require(id);
            const ol = s.ol;
            const ring = [
                ol.fromLonLat([swLng, swLat]), ol.fromLonLat([neLng, swLat]),
                ol.fromLonLat([neLng, neLat]), ol.fromLonLat([swLng, neLat]),
                ol.fromLonLat([swLng, swLat]),
            ];
            const f = new ol.Feature({ geometry: new ol.Polygon([ring]) });
            f.setStyle(new ol.Style({ stroke: BitMapOpenLayers._stroke(ol, style), fill: BitMapOpenLayers._fill(ol, style) }));
            BitMapOpenLayers._addVectorLayer(s, layerId, f, 'rectangle');
        }

        public static addGeoJson(id: string, layerId: string, geoJsonString: string, style: any) {
            const s = BitMapOpenLayers._require(id);
            const ol = s.ol;
            let gj: any;
            try { gj = JSON.parse(geoJsonString); } catch { throw new Error('Invalid GeoJSON string'); }
            const features = new ol.GeoJSON().readFeatures(gj, { dataProjection: 'EPSG:4326', featureProjection: 'EPSG:3857' });
            const stroke = BitMapOpenLayers._stroke(ol, style);
            const fill = BitMapOpenLayers._fill(ol, style);
            const st = BitMapHelpers.readPathStyle(style);
            const styleFn = (feat: any) => {
                const t = feat.getGeometry().getType();
                if (t === 'Point' || t === 'MultiPoint') {
                    return new ol.Style({
                        image: new ol.CircleStyle({
                            radius: 7,
                            fill: new ol.Fill({ color: BitMapHelpers.hexToRgba(st.fillColor, st.fillOpacity) }),
                            stroke: new ol.Stroke({ color: BitMapHelpers.hexToRgba(st.color, st.opacity), width: st.weight }),
                        }),
                    });
                }
                return (t === 'LineString' || t === 'MultiLineString')
                    ? new ol.Style({ stroke })
                    : new ol.Style({ stroke, fill });
            };
            const layer = new ol.VectorLayer({
                source: new ol.VectorSource({ features }),
                style: styleFn,
                zIndex: ++s.zIndexCounter,
            });
            layer.set('layerId', layerId);
            layer.set('bmKind', 'geojson');
            s.map.addLayer(layer);
            BitMapOpenLayers._setLayer(s, layerId, layer);
        }

        public static removeLayer(id: string, layerId: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            const lyr = s.layers[layerId];
            if (lyr) { s.map.removeLayer(lyr); delete s.layers[layerId]; }
        }

        public static clearVectorLayers(id: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            for (const k in s.layers) s.map.removeLayer(s.layers[k]);
            s.layers = {};
        }

        public static addTileOverlay(id: string, opts: any) {
            const s = BitMapOpenLayers._require(id);
            const ol = s.ol;
            const tl = new ol.TileLayer({
                source: new ol.XYZ({
                    url: (opts.urlTemplate || '').replace('{s}', 'a'),
                    maxZoom: opts.maxZoom ?? 19,
                    attributions: opts.attribution || '',
                }),
                opacity: opts.opacity ?? 1,
                zIndex: opts.zIndex ?? 100,
            });
            s.map.addLayer(tl);
            const existing = s.tileOverlays[opts.id];
            if (existing) s.map.removeLayer(existing);
            s.tileOverlays[opts.id] = tl;
        }

        public static removeTileOverlay(id: string, overlayId: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            const tl = s.tileOverlays[overlayId];
            if (tl) { s.map.removeLayer(tl); delete s.tileOverlays[overlayId]; }
        }

        // ---- helpers ----

        private static _require(id: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) throw new Error(`BitMapOpenLayers: unknown map id '${id}'`);
            return s;
        }

        private static _readView(s: any) {
            const ol = s.ol, view = s.map.getView();
            const c3857 = view.getCenter();
            const c = c3857 ? ol.toLonLat(c3857) : [0, 0];
            const extent = view.calculateExtent(s.map.getSize());
            const sw = ol.toLonLat([extent[0], extent[1]]);
            const ne = ol.toLonLat([extent[2], extent[3]]);
            return {
                center: { lat: c[1], lng: c[0] },
                zoom: view.getZoom() ?? 0,
                bounds: {
                    southWest: { lat: sw[1], lng: sw[0] },
                    northEast: { lat: ne[1], lng: ne[0] },
                },
            };
        }

        private static _ensureScale(s: any, show: boolean, imperial: boolean) {
            const ol = s.ol;
            if (s.scaleLine) { s.map.removeControl(s.scaleLine); s.scaleLine = null; }
            if (show) {
                s.scaleLine = new ol.ScaleLine({ units: imperial ? 'us' : 'metric' });
                s.map.addControl(s.scaleLine);
            }
        }

        private static _applyInteractions(s: any, o: any) {
            const map = s.map;
            const interactions = map.getInteractions().getArray();
            for (const interaction of interactions) {
                const name = interaction.constructor?.name || '';
                if (name === 'MouseWheelZoom' || name.includes('MouseWheel')) {
                    interaction.setActive(o.scrollWheelZoom !== false);
                } else if (name === 'DoubleClickZoom' || name.includes('DoubleClick')) {
                    interaction.setActive(o.doubleClickZoom !== false);
                } else if (name === 'DragPan' || name.includes('DragPan')) {
                    interaction.setActive(o.dragging !== false);
                } else if (name === 'KeyboardPan' || name === 'KeyboardZoom' || name.includes('Keyboard')) {
                    interaction.setActive(o.keyboardNavigation !== false);
                }
            }
        }

        private static _markerStyle(ol: any, opts: any) {
            if (opts.iconUrl) {
                return new ol.Style({
                    image: new ol.Icon({
                        src: opts.iconUrl,
                        anchor: [0.5, 1], anchorXUnits: 'fraction', anchorYUnits: 'fraction',
                        scale: 1,
                    }),
                });
            }
            return new ol.Style({
                image: new ol.CircleStyle({
                    radius: 7,
                    fill: new ol.Fill({ color: '#3388ff' }),
                    stroke: new ol.Stroke({ color: '#ffffff', width: 2 }),
                }),
            });
        }

        private static _stroke(ol: any, style: any) {
            const st = BitMapHelpers.readPathStyle(style);
            return new ol.Stroke({
                color: BitMapHelpers.hexToRgba(st.color, st.opacity),
                width: st.weight,
                lineDash: st.dashArray ? st.dashArray.split(',').map((x: string) => parseFloat(x.trim())) : undefined,
            });
        }

        private static _fill(ol: any, style: any) {
            const st = BitMapHelpers.readPathStyle(style);
            return new ol.Fill({ color: BitMapHelpers.hexToRgba(st.fillColor, st.fillOpacity) });
        }

        private static _setLayer(s: any, layerId: string, layer: any) {
            const existing = s.layers[layerId];
            if (existing) s.map.removeLayer(existing);
            s.layers[layerId] = layer;
        }

        private static _addVectorLayer(s: any, layerId: string, feature: any, kind: string) {
            const ol = s.ol;
            const layer = new ol.VectorLayer({ source: new ol.VectorSource({ features: [feature] }), zIndex: ++s.zIndexCounter });
            layer.set('layerId', layerId);
            layer.set('bmVectorKind', kind);
            s.map.addLayer(layer);
            BitMapOpenLayers._setLayer(s, layerId, layer);
        }

        private static _wireEvents(s: any) {
            const ol = s.ol, map = s.map, dn = s.dotnetObj;
            map.on('singleclick', (evt: any) => {
                let hit = false;
                map.forEachFeatureAtPixel(
                    evt.pixel,
                    (feature: any, layer: any) => {
                        if (layer === s.markerLayer) {
                            hit = true;
                            const id = feature.get('markerId');
                            if (id && dn) dn.invokeMethodAsync('OnMarkerClick', id);
                            return true;
                        }
                        const lid = layer?.get('layerId');
                        if (lid && layer.get('bmKind') === 'geojson') {
                            hit = true;
                            const props: any = { ...feature.getProperties() };
                            delete props.geometry;
                            if (dn) dn.invokeMethodAsync('OnGeoJsonFeatureClick', lid, props);
                            return true;
                        }
                        if (lid) {
                            hit = true;
                            const ll = ol.toLonLat(evt.coordinate);
                            const kind = layer.get('bmVectorKind') || 'vector';
                            if (dn) dn.invokeMethodAsync('OnVectorClick', lid, kind, { lat: ll[1], lng: ll[0] });
                            return true;
                        }
                        return false;
                    },
                    { hitTolerance: 6 }
                );
                if (!hit && dn) {
                    const ll = ol.toLonLat(evt.coordinate);
                    dn.invokeMethodAsync('OnClick', { lat: ll[1], lng: ll[0] });
                }
            });
            map.on('dblclick', (evt: any) => {
                if (!dn) return;
                const ll = ol.toLonLat(evt.coordinate);
                dn.invokeMethodAsync('OnDoubleClick', { lat: ll[1], lng: ll[0] });
            });
            map.on('moveend', () => {
                if (!dn) return;
                queueMicrotask(() => dn.invokeMethodAsync('OnViewChanged', BitMapOpenLayers._readView(s)));
            });
        }

        private static async _loadOl(): Promise<any> {
            if (BitMapOpenLayers._olLoadPromise) return BitMapOpenLayers._olLoadPromise;
            BitMapOpenLayers._olLoadPromise = (async () => {
                // Import the full OL bundle from a single entry point so all classes
                // (Map, View, etc.) share the same module scope and instanceof checks work.
                const dynImport: (url: string) => Promise<any> = new Function('u', 'return import(u);') as any;
                const ol = await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}?bundle`);
                const olControl = await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}/control?bundle`);
                const olStyle = await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}/style?bundle`);
                const olGeom = await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}/geom?bundle`);
                const olSource = await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}/source?bundle`);
                const olLayer = await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}/layer?bundle`);
                const olFormat = await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}/format?bundle`);
                const olProj = await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}/proj?bundle`);
                // ol/interaction is loaded implicitly via the main bundle (default interactions).
                await dynImport(`https://esm.sh/ol@${BitMapOpenLayers._OL_VER}/interaction?bundle`);

                return {
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
                };
            })();
            return BitMapOpenLayers._olLoadPromise;
        }
    }
}
