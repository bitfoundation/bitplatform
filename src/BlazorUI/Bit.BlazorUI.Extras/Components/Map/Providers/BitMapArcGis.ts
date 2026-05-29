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
            dragHandlersWired?: boolean,
        } } = {};

        public static async init(id: string, canvasId: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            element = await BitMapHelpers.resolveMapCanvas(canvasId, element);
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

            // ArcGIS MapView has no first-class actionMap entries for double-click zoom
            // or keyboard navigation, so honor those public options via best-effort
            // event interception instead of silently ignoring them. Without this
            // BitMapProviderBase.DoubleClickZoom / KeyboardNavigation = false would
            // be a no-op only on the ArcGIS provider.
            BitMapArcGis._applyInteractivity(view, element, o);

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

            try {
                await view.when();
            } catch (err) {
                // view.when() rejected after we registered the partially-built state.
                // Roll back so a stale instance isn't left in BitMapArcGis._maps and
                // the DOM listeners + scaleBar wired by _applyInteractivity / _ensureScaleBar
                // are torn down. Then rethrow so initialization still fails upstream.
                try { if (state.scaleBar) state.scaleBar.destroy(); } catch { /* ignore */ }
                if (element) {
                    const dblHandler = (view as any).__bmDblClickHandler;
                    const keyHandler = (view as any).__bmKeyHandler;
                    if (dblHandler) try { element.removeEventListener('dblclick', dblHandler, true); } catch { /* ignore */ }
                    if (keyHandler) try { element.removeEventListener('keydown', keyHandler, true); } catch { /* ignore */ }
                    // _applyInteractivity may have stashed the original tabindex and
                    // forced the container to '-1' so ArcGIS' built-in keyboard nav
                    // couldn't fire. Restore the prior value (or remove the attribute
                    // entirely when it was originally absent) so a failed init doesn't
                    // leave the user's element in a non-focusable state.
                    const prevTab = (view as any).__bmPrevTabIndex;
                    if (prevTab !== undefined) {
                        try {
                            if (prevTab === null) element.removeAttribute('tabindex');
                            else element.setAttribute('tabindex', prevTab);
                        } catch { /* ignore */ }
                        (view as any).__bmPrevTabIndex = undefined;
                    }
                }
                try { view?.destroy?.(); } catch { /* ignore */ }
                state.dotnetObj = null;
                delete BitMapArcGis._maps[id];
                throw err;
            }

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

            // doubleClickZoom and keyboardNavigation aren't part of actionMap, so
            // re-apply them via the same best-effort path used during init().
            if (Object.prototype.hasOwnProperty.call(o, 'doubleClickZoom')
                || Object.prototype.hasOwnProperty.call(o, 'keyboardNavigation')) {
                const container = s.view.container as HTMLElement | null | undefined;
                if (container) BitMapArcGis._applyInteractivity(s.view, container, o);
            }
        }

        public static dispose(id: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;

            // Remove DOM listeners that _applyInteractivity attached to the
            // container BEFORE destroying the view (which may null out
            // s.view.container), so dblclick/keydown handlers don't outlive
            // the map and stay attached to the user's element.
            const container = s.view?.container as HTMLElement | null | undefined;
            if (container) {
                const view = s.view as any;
                if (view.__bmDblClickHandler) {
                    try { container.removeEventListener('dblclick', view.__bmDblClickHandler, true); } catch { /* ignore */ }
                    view.__bmDblClickHandler = null;
                }
                if (view.__bmKeyHandler) {
                    try { container.removeEventListener('keydown', view.__bmKeyHandler, true); } catch { /* ignore */ }
                    view.__bmKeyHandler = null;
                }
                // Restore the original tabindex captured by _applyInteractivity. Without
                // this the container is left pinned at tabindex="-1" (or absent when it
                // was originally absent and we forced a value) after dispose, leaving
                // the user's element non-focusable for whatever replaces the map.
                if (view.__bmPrevTabIndex !== undefined) {
                    try {
                        if (view.__bmPrevTabIndex === null) container.removeAttribute('tabindex');
                        else container.setAttribute('tabindex', view.__bmPrevTabIndex);
                    } catch { /* ignore */ }
                    view.__bmPrevTabIndex = undefined;
                }
            }

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
                attributes: {
                    markerId, popupHtml: opts.popupHtml || '', popupText: opts.popupText || '',
                    title: opts.title || '', draggable: !!opts.draggable,
                },
            });
            const existing = s.markers[markerId];
            if (existing) try { s.markerLayer.remove(existing); } catch { /* ignore */ }
            s.markerLayer.add(graphic);
            s.markers[markerId] = graphic;

            if (opts.draggable) {
                BitMapArcGis._ensureDragHandlers(s);
            }
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
            if (ring.length > 0) {
                const a = ring[0];
                const b = ring[ring.length - 1];
                if (a[0] !== b[0] || a[1] !== b[1]) ring.push([a[0], a[1]]);
            }
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
                        symbol: BitMapArcGis._pointSym(esri, style),
                        attributes: props,
                    }));
                } else if (t === 'MultiPoint') {
                    for (const coord of geometry.coordinates) {
                        graphics.push(new esri.Graphic({
                            geometry: new esri.Point({ longitude: coord[0], latitude: coord[1] }),
                            symbol: BitMapArcGis._pointSym(esri, style),
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
                // Use reserved/prefixed keys for internal tagging so that user-supplied
                // GeoJSON properties (e.g. a feature with a literal "layerId" or "bmKind"
                // property) are not overwritten and silently stripped before being sent to .NET.
                const props = { ...(f.properties || {}), _bmLayerId: layerId, _bmKind: 'geojson' };
                processGeometry(f.geometry, props);
            }
            BitMapArcGis._removeGeoJsonLayer(s, layerId);
            // The same layerId may previously have been used for a non-GeoJSON vector
            // (polyline/polygon/circle/rectangle) stored in s.layers. Drop that prior
            // representation too so reusing a layerId across types doesn't leave the
            // old graphic on the map alongside the new GeoJSON content.
            const prior = s.layers[layerId];
            if (prior) { try { s.view.graphics.remove(prior.graphic); } catch { /* ignore */ } delete s.layers[layerId]; }
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
            // The same layerId may previously have been used for a GeoJSON layer stored
            // in s.geoJsonLayers. Drop that prior representation too so reusing a layerId
            // across types doesn't leave the old GeoJSON graphics rendered alongside the
            // new vector content.
            BitMapArcGis._removeGeoJsonLayer(s, layerId);
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

        /**
         * Build a point symbol from a BitMap path style. Falls back to the legacy
         * blue SimpleMarkerSymbol only when no style at all is supplied so callers
         * that pass a custom color/outline/opacity see them honored on point features.
         */
        private static _pointSym(esri: any, style: any) {
            if (!style) {
                return new esri.SimpleMarkerSymbol({
                    color: [51, 136, 255, 255],
                    outline: { color: [255, 255, 255, 255], width: 2 },
                    size: 8,
                });
            }
            const st = BitMapHelpers.readPathStyle(style);
            return new esri.SimpleMarkerSymbol({
                color: BitMapArcGis._rgbaArr(st.fillColor, st.fillOpacity),
                outline: {
                    color: BitMapArcGis._rgbaArr(st.color, st.opacity),
                    width: st.weight,
                },
                size: 8,
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

        /**
         * ArcGIS MapView's actionMap exposes mouseWheel/dragPrimary/etc. but has no
         * first-class entry for double-click zoom or keyboard navigation, so honor
         * BitMapProviderBase.DoubleClickZoom / KeyboardNavigation via best-effort
         * event interception. Only the keys the caller explicitly supplied are
         * applied, and previous overrides are torn down before new ones are wired
         * so partial sync()s don't leak listeners or pin tabIndex permanently.
         */
        private static _applyInteractivity(view: any, container: HTMLElement, o: any) {
            const has = (k: string) => Object.prototype.hasOwnProperty.call(o, k);

            if (has('doubleClickZoom')) {
                if (view.__bmDblClickHandler) {
                    container.removeEventListener('dblclick', view.__bmDblClickHandler, true);
                    view.__bmDblClickHandler = null;
                }
                if (o.doubleClickZoom === false) {
                    const handler = (ev: Event) => { ev.stopPropagation(); ev.preventDefault(); };
                    container.addEventListener('dblclick', handler, true);
                    view.__bmDblClickHandler = handler;
                }
            }

            if (has('keyboardNavigation')) {
                // ArcGIS' keyboard navigation requires a focusable container with a
                // tabIndex; if the caller opts out, also drop tabIndex so the view
                // can't receive focus and the SDK's key handlers can't fire.
                if (o.keyboardNavigation === false) {
                    if (view.__bmPrevTabIndex === undefined) {
                        view.__bmPrevTabIndex = container.getAttribute('tabindex');
                    }
                    container.setAttribute('tabindex', '-1');
                    if (!view.__bmKeyHandler) {
                        const handler = (ev: Event) => { ev.stopPropagation(); ev.preventDefault(); };
                        container.addEventListener('keydown', handler, true);
                        view.__bmKeyHandler = handler;
                    }
                } else {
                    if (view.__bmKeyHandler) {
                        container.removeEventListener('keydown', view.__bmKeyHandler, true);
                        view.__bmKeyHandler = null;
                    }
                    if (view.__bmPrevTabIndex !== undefined) {
                        if (view.__bmPrevTabIndex === null) container.removeAttribute('tabindex');
                        else container.setAttribute('tabindex', view.__bmPrevTabIndex);
                        view.__bmPrevTabIndex = undefined;
                    }
                }
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
            const dotnet = s.dotnetObj;
            if (!dotnet) return;
            queueMicrotask(() => {
                // dispose() may have run between scheduling and execution; only invoke if
                // the captured handle is still associated with a live state.
                if (s.dotnetObj !== dotnet) return;
                dotnet.invokeMethodAsync('OnViewChanged', BitMapArcGis._readView(s));
            });
        }

        /**
         * Wires per-view pointer handlers (once) so that markers added with
         * opts.draggable=true can be dragged by the user. Drag end fires
         * OnMarkerDragEnd through the dotnet bridge, matching the other providers.
         */
        private static _ensureDragHandlers(s: any) {
            if (s.dragHandlersWired) return;
            s.dragHandlersWired = true;

            const view = s.view;
            let active: any = null; // { graphic, markerId }
            // Per-pointer-down token used to discard hitTest promises that resolve
            // after the user has already released the pointer. Each pointer-down
            // bumps the counter; the resolver captures it in a closure and bails
            // out if the current token has moved on (i.e. a newer pointer-down or
            // a pointer-up/leave invalidated the gesture).
            let pointerToken = 0;

            // Restore any actionMap.dragPrimary override and clear `active`. Used by
            // pointer-up / pointer-leave to defend against a hitTest promise that
            // resolves AFTER the user has already released the pointer: without this
            // cleanup the hitTest's `active = ...; am.dragPrimary = null;` block
            // would leave panning permanently disabled and `active` permanently
            // pinned to a stale graphic.
            const releaseActive = () => {
                // Invalidate any in-flight hitTest from the just-completed gesture
                // so it can't re-acquire `active` after we've cleared it.
                pointerToken++;
                if (!active) return;
                const am = view.navigation?.actionMap;
                if (am && (active as any)._prevDrag !== undefined) {
                    am.dragPrimary = (active as any)._prevDrag;
                }
                active = null;
            };

            view.on('pointer-down', (event: any) => {
                const token = ++pointerToken;
                view.hitTest(event).then((response: any) => {
                    // Pointer-up / pointer-leave (or another pointer-down) bumped the
                    // token; this response is stale and must not mutate `active` or
                    // actionMap.dragPrimary, otherwise we'd leave panning disabled.
                    if (token !== pointerToken) return;
                    for (const r of response.results) {
                        const g = r.graphic;
                        const a = g?.attributes;
                        if (a && a.markerId && a.draggable && s.markers[a.markerId] === g) {
                            active = { graphic: g, markerId: a.markerId };
                            // Disable map panning while dragging this marker.
                            try { event.stopPropagation(); } catch { /* ignore */ }
                            const am = view.navigation?.actionMap;
                            if (am) { (active as any)._prevDrag = am.dragPrimary; am.dragPrimary = null; }
                            break;
                        }
                    }
                }).catch(() => { /* ignore */ });
            });

            // pointer-up / pointer-leave clear `active` and restore actionMap.dragPrimary
            // even if the corresponding pointer-down hitTest promise hasn't resolved
            // yet — and even if it resolves later, releaseActive() ensures the
            // override is undone the next time the pointer is released or leaves.
            view.on('pointer-up', () => releaseActive());
            view.on('pointer-leave', () => releaseActive());

            view.on('drag', (event: any) => {
                if (!active) return;
                try { event.stopPropagation(); } catch { /* ignore */ }
                if (event.action === 'end') {
                    const pt = view.toMap({ x: event.x, y: event.y });
                    if (pt) {
                        active.graphic.geometry = new s.esri.Point({ longitude: pt.longitude, latitude: pt.latitude });
                        if (s.dotnetObj) {
                            s.dotnetObj.invokeMethodAsync('OnMarkerDragEnd', active.markerId, { lat: pt.latitude, lng: pt.longitude });
                        }
                    }
                    const am = view.navigation?.actionMap;
                    if (am && (active as any)._prevDrag !== undefined) am.dragPrimary = (active as any)._prevDrag;
                    active = null;
                } else {
                    const pt = view.toMap({ x: event.x, y: event.y });
                    if (pt) {
                        active.graphic.geometry = new s.esri.Point({ longitude: pt.longitude, latitude: pt.latitude });
                    }
                }
            });
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
                        if (a._bmKind === 'geojson' && a._bmLayerId && s.geoJsonLayers[a._bmLayerId]) {
                            hit = true;
                            const props = { ...a }; delete props._bmLayerId; delete props._bmKind;
                            if (dn) dn.invokeMethodAsync('OnGeoJsonFeatureClick', a._bmLayerId, props);
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
            const p = (async () => {
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
            // Clear the memoized promise on failure so subsequent calls can retry the
            // SDK load instead of being stuck with a permanently rejected promise.
            p.catch(() => { if (BitMapArcGis._esriPromise === p) BitMapArcGis._esriPromise = null; });
            BitMapArcGis._esriPromise = p;
            return p;
        }
    }
}
