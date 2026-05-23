namespace BitBlazorUI {

    /**
     * ArcGIS Maps SDK for JavaScript 5.0 provider. Loads the SDK as an ES module via the
     * <c>$arcgis.import()</c> helper exposed by the CDN, then drives a simple MapView.
     */
    export class BitMapArcGis {
        private static _esriPromise: Promise<any> | null = null;

        private static _maps: { [id: string]: {
            esri: any, view: any, map: any, dotnetObj: DotNetObject | null | undefined,
            markerLayer: any, markers: { [k: string]: any },
            layers: { [k: string]: { graphic: any, kind: string } },
            geoJsonLayers: { [k: string]: { graphics: any[] } },
            tileOverlays: { [k: string]: any },
            scaleBar: any,
        } } = {};

        public static async init(id: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            const esri = await BitMapArcGis._loadEsri();
            const o = options || {};

            if (o.apiKey) esri.esriConfig.apiKey = o.apiKey;

            const map = new esri.EsriMap({ basemap: o.basemapId || 'osm' });
            const view = new esri.MapView({
                container: element,
                map,
                center: [o.center?.lng ?? -0.09, o.center?.lat ?? 51.505],
                zoom: o.zoom ?? 4,
                navigation: {
                    actionMap: {
                        mouseWheel: o.scrollWheelZoom !== false ? 'zoom' : null,
                        dragPrimary: o.dragging !== false ? 'pan' : null,
                    },
                },
            });

            const markerLayer = new esri.GraphicsLayer({ listMode: 'hide' });
            map.add(markerLayer);

            const state = {
                esri, view, map, dotnetObj,
                markerLayer,
                markers: {} as any,
                layers: {} as any,
                geoJsonLayers: {} as any,
                tileOverlays: {} as any,
                scaleBar: null as any,
            };

            BitMapArcGis._ensureScaleBar(state, !!o.showScaleControl);
            BitMapArcGis._wireEvents(state);
            BitMapArcGis._maps[id] = state;

            await view.when();

            if (o.zoomControl === false) { try { const w = view.ui.find('zoom'); if (w) view.ui.remove(w); } catch { /* ignore */ } }
            if (o.attributionControl === false) { try { const w = view.ui.find('attribution'); if (w) view.ui.remove(w); } catch { /* ignore */ } }

            BitMapArcGis._notifyView(state);
        }

        public static sync(id: string, options: any) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            const o = options || {};
            const currentCenter = s.view.center;
            const lng = o.center ? o.center.lng : (currentCenter?.longitude ?? 0);
            const lat = o.center ? o.center.lat : (currentCenter?.latitude ?? 0);
            s.view.goTo({ center: [lng, lat], zoom: o.zoom ?? s.view.zoom }, { animate: false }).catch(() => {});
            if (o.basemapId && o.basemapId !== s.map.basemap?.id) s.map.basemap = o.basemapId;
            // Only touch the scale bar when caller explicitly supplied the flag,
            // so partial updates don't reset the user's existing setting.
            if (Object.prototype.hasOwnProperty.call(o, 'showScaleControl')) {
                BitMapArcGis._ensureScaleBar(s, !!o.showScaleControl);
            }

            // Reapply interaction flags only for keys the caller explicitly provided.
            const actionMap = s.view.navigation?.actionMap;
            if (actionMap) {
                if (Object.prototype.hasOwnProperty.call(o, 'scrollWheelZoom')) {
                    actionMap.mouseWheel = o.scrollWheelZoom !== false ? 'zoom' : null;
                }
                if (Object.prototype.hasOwnProperty.call(o, 'dragging')) {
                    actionMap.dragPrimary = o.dragging !== false ? 'pan' : null;
                }
            }
        }

        public static dispose(id: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            try {
                if (s.scaleBar) { s.scaleBar.destroy(); }
                s.view.destroy();
            } catch { /* ignore */ }
            s.dotnetObj = null;
            delete BitMapArcGis._maps[id];
        }

        public static invalidateSize(id: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            
            // Keep the resize scoped to the target ArcGIS view instead of broadcasting
            // a global window resize event that can affect unrelated components.
            const container = s.view.container as HTMLElement | null | undefined;
            container?.getBoundingClientRect();
            if (typeof s.view.resize === 'function') {
                s.view.resize();
            }
        }

        public static getView(id: string) {
            const s = BitMapArcGis._require(id);
            return BitMapArcGis._readView(s);
        }

        public static setView(id: string, lat: number, lng: number, zoom: number | null, animate: boolean) {
            const s = BitMapArcGis._require(id);
            s.view.goTo({ center: [lng, lat], zoom: zoom ?? s.view.zoom }, animate === false ? { animate: false } : {}).catch(() => {});
        }

        public static flyTo(id: string, lat: number, lng: number, zoom: number | null) {
            const s = BitMapArcGis._require(id);
            s.view.goTo({ center: [lng, lat], zoom: zoom ?? s.view.zoom }, { duration: 1200, easing: 'in-out-expo' }).catch(() => {});
        }

        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number) {
            const s = BitMapArcGis._require(id);
            const pad = paddingPx ?? 48;
            const latFrac = ((neLat - swLat) * pad) / 300;
            const lngFrac = ((neLng - swLng) * pad) / 400;
            const ext = new s.esri.Extent({
                xmin: swLng - lngFrac, ymin: swLat - latFrac,
                xmax: neLng + lngFrac, ymax: neLat + latFrac,
                spatialReference: { wkid: 4326 },
            });
            s.view.goTo(ext).catch(() => {});
        }

        public static fitBoundsToMarkers(id: string, paddingPx: number) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            const geoms = s.markerLayer.graphics.toArray().map((g: any) => g.geometry).filter(Boolean);
            if (geoms.length === 0) return;
            const pad = paddingPx ?? 48;
            s.view.goTo(geoms, { padding: { top: pad, right: pad, bottom: pad, left: pad } }).catch(() => {});
        }

        public static addMarker(id: string, markerId: string, opts: any) {
            const s = BitMapArcGis._require(id);
            const esri = s.esri;
            const sym = opts.iconUrl
                ? new esri.PictureMarkerSymbol({ url: opts.iconUrl, width: opts.iconWidth || 32, height: opts.iconHeight || 32 })
                : new esri.SimpleMarkerSymbol({
                    color: [51, 136, 255, 255],
                    outline: { color: [255, 255, 255, 255], width: 2 },
                    size: 14,
                });
            const graphic = new esri.Graphic({
                geometry: new esri.Point({ longitude: opts.lng, latitude: opts.lat }),
                symbol: sym,
                attributes: { markerId, popupHtml: opts.popupHtml || '', popupText: opts.popupText || '', title: opts.title || '' },
            });
            const existing = s.markers[markerId];
            if (existing) try { s.markerLayer.remove(existing); } catch { /* ignore */ }
            s.markerLayer.add(graphic);
            s.markers[markerId] = graphic;
        }

        public static removeMarker(id: string, markerId: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            const g = s.markers[markerId];
            if (g) { s.markerLayer.remove(g); delete s.markers[markerId]; }
        }

        public static clearMarkers(id: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            s.markerLayer.removeAll();
            s.markers = {};
        }

        public static syncMarkers(id: string, markerIds: string[], markers: any[]) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            s.markerLayer.removeAll();
            s.markers = {};
            const len = Math.min(markerIds?.length ?? 0, markers?.length ?? 0);
            for (let i = 0; i < len; i++) BitMapArcGis.addMarker(id, markerIds[i], markers[i]);
        }

        public static setMarkerPosition(id: string, markerId: string, lat: number, lng: number) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            const g = s.markers[markerId];
            if (g) g.geometry = new s.esri.Point({ longitude: lng, latitude: lat });
        }

        public static openMarkerPopup(id: string, markerId: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            const g = s.markers[markerId];
            if (!g) return;
            const html = g.attributes?.popupHtml;
            const text = g.attributes?.popupText;
            if (html) {
                s.view.popup.open({ content: html, title: g.attributes?.title || '', location: g.geometry });
            } else if (text) {
                const el = document.createElement('span');
                el.textContent = text;
                s.view.popup.open({ content: el, title: g.attributes?.title || '', location: g.geometry });
            }
        }

        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapArcGis._require(id);
            const esri = s.esri;
            const g = new esri.Graphic({
                geometry: new esri.Polyline({
                    paths: [latlngs.map(p => [p.lng, p.lat])],
                    spatialReference: { wkid: 4326 },
                }),
                symbol: BitMapArcGis._lineSym(esri, style),
                attributes: { layerId, bmVectorKind: 'polyline' },
            });
            BitMapArcGis._setLayer(s, layerId, g, 'polyline');
        }

        public static addPolygon(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapArcGis._require(id);
            const esri = s.esri;
            const ring = latlngs.map(p => [p.lng, p.lat]);
            const g = new esri.Graphic({
                geometry: new esri.Polygon({ rings: [ring], spatialReference: { wkid: 4326 } }),
                symbol: BitMapArcGis._fillSym(esri, style),
                attributes: { layerId, bmVectorKind: 'polygon' },
            });
            BitMapArcGis._setLayer(s, layerId, g, 'polygon');
        }

        public static addCircle(id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            const s = BitMapArcGis._require(id);
            const esri = s.esri;
            const ring = BitMapHelpers.circleRingLngLat(lat, lng, radiusMeters);
            const g = new esri.Graphic({
                geometry: new esri.Polygon({ rings: [ring], spatialReference: { wkid: 4326 } }),
                symbol: BitMapArcGis._fillSym(esri, style),
                attributes: { layerId, bmVectorKind: 'circle' },
            });
            BitMapArcGis._setLayer(s, layerId, g, 'circle');
        }

        public static addRectangle(id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            const s = BitMapArcGis._require(id);
            const esri = s.esri;
            const ring = [
                [swLng, swLat], [neLng, swLat], [neLng, neLat], [swLng, neLat], [swLng, swLat],
            ];
            const g = new esri.Graphic({
                geometry: new esri.Polygon({ rings: [ring], spatialReference: { wkid: 4326 } }),
                symbol: BitMapArcGis._fillSym(esri, style),
                attributes: { layerId, bmVectorKind: 'rectangle' },
            });
            BitMapArcGis._setLayer(s, layerId, g, 'rectangle');
        }

        public static addGeoJson(id: string, layerId: string, geoJsonString: string, style: any) {
            const s = BitMapArcGis._require(id);
            const esri = s.esri;
            let gj: any;
            try { gj = JSON.parse(geoJsonString); } catch { throw new Error('Invalid GeoJSON string'); }
            const features = gj.type === 'FeatureCollection' ? gj.features
                : gj.type === 'Feature' ? [gj]
                : [{ type: 'Feature', geometry: gj, properties: {} }];
            const graphics: any[] = [];
            const processGeometry = (geometry: any, props: any) => {
                if (!geometry) return;
                const t = geometry.type;
                if (t === 'Point') {
                    graphics.push(new esri.Graphic({
                        geometry: new esri.Point({ longitude: geometry.coordinates[0], latitude: geometry.coordinates[1] }),
                        symbol: new esri.SimpleMarkerSymbol({ color: [51, 136, 255, 255], outline: { color: [255, 255, 255, 255], width: 2 }, size: 8 }),
                        attributes: props,
                    }));
                } else if (t === 'MultiPoint') {
                    for (const coord of geometry.coordinates) {
                        graphics.push(new esri.Graphic({
                            geometry: new esri.Point({ longitude: coord[0], latitude: coord[1] }),
                            symbol: new esri.SimpleMarkerSymbol({ color: [51, 136, 255, 255], outline: { color: [255, 255, 255, 255], width: 2 }, size: 8 }),
                            attributes: props,
                        }));
                    }
                } else if (t === 'LineString') {
                    graphics.push(new esri.Graphic({
                        geometry: new esri.Polyline({ paths: [geometry.coordinates], spatialReference: { wkid: 4326 } }),
                        symbol: BitMapArcGis._lineSym(esri, style),
                        attributes: props,
                    }));
                } else if (t === 'MultiLineString') {
                    graphics.push(new esri.Graphic({
                        geometry: new esri.Polyline({ paths: geometry.coordinates, spatialReference: { wkid: 4326 } }),
                        symbol: BitMapArcGis._lineSym(esri, style),
                        attributes: props,
                    }));
                } else if (t === 'Polygon') {
                    graphics.push(new esri.Graphic({
                        geometry: new esri.Polygon({ rings: geometry.coordinates, spatialReference: { wkid: 4326 } }),
                        symbol: BitMapArcGis._fillSym(esri, style),
                        attributes: props,
                    }));
                } else if (t === 'MultiPolygon') {
                    for (const rings of geometry.coordinates) {
                        graphics.push(new esri.Graphic({
                            geometry: new esri.Polygon({ rings, spatialReference: { wkid: 4326 } }),
                            symbol: BitMapArcGis._fillSym(esri, style),
                            attributes: props,
                        }));
                    }
                } else if (t === 'GeometryCollection') {
                    for (const inner of geometry.geometries || []) {
                        processGeometry(inner, props);
                    }
                }
            };
            for (const f of features) {
                if (!f.geometry) continue;
                const props = { ...(f.properties || {}), layerId, bmKind: 'geojson' };
                processGeometry(f.geometry, props);
            }
            BitMapArcGis._removeGeoJsonLayer(s, layerId);
            for (const g of graphics) s.view.graphics.add(g);
            s.geoJsonLayers[layerId] = { graphics };
        }

        public static removeLayer(id: string, layerId: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            const e = s.layers[layerId];
            if (e) { s.view.graphics.remove(e.graphic); delete s.layers[layerId]; }
            BitMapArcGis._removeGeoJsonLayer(s, layerId);
        }

        public static clearVectorLayers(id: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            for (const k in s.layers) s.view.graphics.remove(s.layers[k].graphic);
            s.layers = {};
            for (const k in s.geoJsonLayers) BitMapArcGis._removeGeoJsonLayer(s, k);
        }

        public static addTileOverlay(id: string, opts: any) {
            const s = BitMapArcGis._require(id);
            const existing = s.tileOverlays[opts.id];
            if (existing) { s.map.remove(existing); delete s.tileOverlays[opts.id]; }
            const esri = s.esri;
            const tl = new esri.WebTileLayer({
                urlTemplate: (opts.urlTemplate || '').replace('{s}', 'a'),
                copyright: opts.attribution || '',
                opacity: opts.opacity ?? 1,
            });
            s.map.add(tl);
            s.tileOverlays[opts.id] = tl;
        }

        public static removeTileOverlay(id: string, overlayId: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            const tl = s.tileOverlays[overlayId];
            if (tl) { s.map.remove(tl); delete s.tileOverlays[overlayId]; }
        }

        // ---- helpers ----

        private static _require(id: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) throw new Error(`BitMapArcGis: unknown map id '${id}'`);
            return s;
        }

        private static _setLayer(s: any, layerId: string, graphic: any, kind: string) {
            const existing = s.layers[layerId];
            if (existing) s.view.graphics.remove(existing.graphic);
            s.view.graphics.add(graphic);
            s.layers[layerId] = { graphic, kind };
        }

        private static _removeGeoJsonLayer(s: any, layerId: string) {
            const e = s.geoJsonLayers[layerId];
            if (!e) return;
            for (const g of e.graphics) s.view.graphics.remove(g);
            delete s.geoJsonLayers[layerId];
        }

        private static _lineSym(esri: any, style: any) {
            const st = BitMapHelpers.readPathStyle(style);
            return new esri.SimpleLineSymbol({
                color: BitMapArcGis._rgbaArr(st.color, st.opacity),
                width: st.weight,
                style: st.dashArray ? 'dash' : 'solid',
            });
        }

        private static _fillSym(esri: any, style: any) {
            const st = BitMapHelpers.readPathStyle(style);
            return new esri.SimpleFillSymbol({
                color: BitMapArcGis._rgbaArr(st.fillColor, st.fillOpacity),
                outline: new esri.SimpleLineSymbol({
                    color: BitMapArcGis._rgbaArr(st.color, st.opacity),
                    width: st.weight,
                    style: st.dashArray ? 'dash' : 'solid',
                }),
            });
        }

        private static _rgbaArr(hex: string, alpha: number): number[] {
            const a = Math.round(alpha * 255);
            if (!hex) return [51, 136, 255, a];
            let h = hex.replace('#', '');
            if (h.length === 3) h = h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
            const n = parseInt(h, 16);
            if (Number.isNaN(n)) return [51, 136, 255, a];
            return [(n >> 16) & 255, (n >> 8) & 255, n & 255, a];
        }

        private static _ensureScaleBar(s: any, show: boolean) {
            if (show && !s.scaleBar) {
                s.scaleBar = new s.esri.ScaleBar({ view: s.view, unit: 'dual' });
                s.view.ui.add(s.scaleBar, 'bottom-left');
            } else if (!show && s.scaleBar) {
                s.view.ui.remove(s.scaleBar);
                s.scaleBar.destroy();
                s.scaleBar = null;
            }
        }

        private static _readView(s: any) {
            const view = s.view, esri = s.esri;
            const center = view.center;
            const ext = view.extent;
            let geoExt: any = null;
            try {
                const sr = view.spatialReference;
                if (sr && (sr.isWebMercator || sr.wkid === 3857 || sr.wkid === 102100)) {
                    geoExt = esri.webMercatorUtils?.webMercatorToGeographic?.(ext) ?? ext;
                } else {
                    geoExt = ext;
                }
            } catch { geoExt = null; }
            return {
                center: { lat: center?.latitude ?? 0, lng: center?.longitude ?? 0 },
                zoom: view.zoom ?? 0,
                bounds: geoExt
                    ? { southWest: { lat: geoExt.ymin, lng: geoExt.xmin }, northEast: { lat: geoExt.ymax, lng: geoExt.xmax } }
                    : { southWest: { lat: 0, lng: 0 }, northEast: { lat: 0, lng: 0 } },
            };
        }

        private static _notifyView(s: any) {
            if (!s.dotnetObj) return;
            queueMicrotask(() => s.dotnetObj.invokeMethodAsync('OnViewChanged', BitMapArcGis._readView(s)));
        }

        private static _wireEvents(s: any) {
            const view = s.view, dn = s.dotnetObj;
            view.on('click', (event: any) => {
                view.hitTest(event).then((response: any) => {
                    let hit = false;
                    for (const r of response.results) {
                        const g = r.graphic;
                        const a = g?.attributes;
                        if (!a) continue;
                        if (a.markerId && s.markers[a.markerId]) {
                            hit = true;
                            if (dn) dn.invokeMethodAsync('OnMarkerClick', a.markerId);
                            if (a.popupHtml) {
                                view.popup.open({ content: a.popupHtml, title: a.title || '', location: g.geometry });
                            } else if (a.popupText) {
                                const el = document.createElement('span');
                                el.textContent = a.popupText;
                                view.popup.open({ content: el, title: a.title || '', location: g.geometry });
                            }
                            break;
                        }
                        if (a.bmKind === 'geojson' && a.layerId && s.geoJsonLayers[a.layerId]) {
                            hit = true;
                            const props = { ...a }; delete props.layerId; delete props.bmKind;
                            if (dn) dn.invokeMethodAsync('OnGeoJsonFeatureClick', a.layerId, props);
                            break;
                        }
                        if (a.bmVectorKind && a.layerId && s.layers[a.layerId]) {
                            hit = true;
                            if (dn && event.mapPoint) {
                                dn.invokeMethodAsync('OnVectorClick', a.layerId, a.bmVectorKind, {
                                    lat: event.mapPoint.latitude, lng: event.mapPoint.longitude,
                                });
                            }
                            break;
                        }
                    }
                    if (!hit && dn && event.mapPoint) {
                        dn.invokeMethodAsync('OnClick', { lat: event.mapPoint.latitude, lng: event.mapPoint.longitude });
                    }
                });
            });
            view.on('double-click', (event: any) => {
                if (dn && event.mapPoint) dn.invokeMethodAsync('OnDoubleClick', { lat: event.mapPoint.latitude, lng: event.mapPoint.longitude });
            });
            let viewTimer: any = null;
            s.esri.reactiveUtils?.watch(
                () => [view.center, view.zoom],
                () => { clearTimeout(viewTimer); viewTimer = setTimeout(() => BitMapArcGis._notifyView(s), 80); }
            );
        }

        private static async _loadEsri(): Promise<any> {
            if (BitMapArcGis._esriPromise) return BitMapArcGis._esriPromise;
            BitMapArcGis._esriPromise = (async () => {
                await BitMapHelpers.waitForGlobal('$arcgis', () => typeof (globalThis as any).$arcgis?.import === 'function');
                const $arcgis = (globalThis as any).$arcgis;
                const imp = (path: string) => $arcgis.import(path);
                const [
                    EsriMap, MapView, GraphicsLayer, WebTileLayer,
                    Graphic, Point, Polyline, Polygon, Extent,
                    SimpleMarkerSymbol, PictureMarkerSymbol,
                    SimpleLineSymbol, SimpleFillSymbol,
                    webMercatorUtils, ScaleBar, esriConfig, reactiveUtils,
                ] = await Promise.all([
                    imp('@arcgis/core/Map.js'),
                    imp('@arcgis/core/views/MapView.js'),
                    imp('@arcgis/core/layers/GraphicsLayer.js'),
                    imp('@arcgis/core/layers/WebTileLayer.js'),
                    imp('@arcgis/core/Graphic.js'),
                    imp('@arcgis/core/geometry/Point.js'),
                    imp('@arcgis/core/geometry/Polyline.js'),
                    imp('@arcgis/core/geometry/Polygon.js'),
                    imp('@arcgis/core/geometry/Extent.js'),
                    imp('@arcgis/core/symbols/SimpleMarkerSymbol.js'),
                    imp('@arcgis/core/symbols/PictureMarkerSymbol.js'),
                    imp('@arcgis/core/symbols/SimpleLineSymbol.js'),
                    imp('@arcgis/core/symbols/SimpleFillSymbol.js'),
                    imp('@arcgis/core/geometry/support/webMercatorUtils.js'),
                    imp('@arcgis/core/widgets/ScaleBar.js'),
                    imp('@arcgis/core/config.js'),
                    imp('@arcgis/core/core/reactiveUtils.js'),
                ]);
                return {
                    esriConfig, EsriMap, MapView, GraphicsLayer, WebTileLayer,
                    Graphic, Point, Polyline, Polygon, Extent,
                    SimpleMarkerSymbol, PictureMarkerSymbol,
                    SimpleLineSymbol, SimpleFillSymbol,
                    webMercatorUtils, ScaleBar, reactiveUtils,
                };
            })();
            return BitMapArcGis._esriPromise;
        }
    }
}
