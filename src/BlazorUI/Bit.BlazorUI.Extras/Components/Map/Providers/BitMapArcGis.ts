namespace BitBlazorUI {

    /**
     * ArcGIS Maps SDK for JavaScript 5.0 provider. Loads the SDK as an ES module via the
     * <c>$arcgis.import()</c> helper exposed by the CDN, then drives a simple MapView.
     */
    // bit-trim: owners BitArcGisMapProvider
    // Called through the JsObjectName of its provider, which BitMap puts together into the identifier it calls (see
    // BitMapJsRuntimeExtensions), so no interop identifier names this script: the provider type an app passes keeps it.
    export class BitMapArcGis {
        private static _esriPromise: Promise<any> | null = null;

        private static _maps: { [id: string]: {
            esri: any, view: any, map: any, dotnetObj: DotNetObject | null | undefined,
            markerLayer: any, markers: { [k: string]: any },
            layers: { [k: string]: { graphic: any, kind: string } },
            geoJsonLayers: { [k: string]: { graphics: any[] } },
            tileOverlays: { [k: string]: any },
            scaleBar: any,
            suppressBrowserContextMenu: boolean,
            /**
             * BitMapProviderBase.DoubleClickZoom. MapView's actionMap has no entry for it, so the view's double-click
             * handler honours it (_wireEvents).
             */
            doubleClickZoom: boolean,
            /** Whether a marker's double-click is the marker's: only while OnMarkerDoubleClick has a handler. */
            markerDoubleClick: boolean,
            /** The marker the view's popup was last opened for - the one it shows while it is visible. */
            popupMarkerId?: string | null,
            dragHandlersWired?: boolean,
        } } = {};

        /** What each marker's graphic was last built from, which an update compares against (_updateMarker). */
        private static _built = new WeakMap<object, any>();

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

            // Keyboard navigation needs nothing here: BitMapChrome pans and zooms from the
            // keyboard on the canvas, and takes the view surface whose handlers ArcGIS would
            // otherwise use out of the tab order.

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
                suppressBrowserContextMenu: !!o.suppressBrowserContextMenu,
                doubleClickZoom: o.doubleClickZoom !== false,
                markerDoubleClick: false,
            };

            BitMapArcGis._ensureScaleBar(state, !!o.showScaleControl);
            BitMapArcGis._wireEvents(state);
            BitMapArcGis._maps[id] = state;

            try {
                await view.when();
            } catch (err) {
                // view.when() rejected after we registered the partially-built state.
                // Roll back so a stale instance isn't left in BitMapArcGis._maps and
                // the DOM listener + scaleBar wired by _wireEvents / _ensureScaleBar
                // are torn down. Then rethrow so initialization still fails upstream.
                try { if (state.scaleBar) state.scaleBar.destroy(); } catch { /* ignore */ }
                // The contextmenu listener goes on the view's container rather than the element
                // above, so it needs the same removal dispose() does - otherwise a failed init
                // leaves a handler on the DOM node holding a destroyed view.
                const ctxHandler = (view as any).__bmContextMenuHandler;
                if (ctxHandler) {
                    const ctxContainer = view.container as HTMLElement | null | undefined;
                    if (ctxContainer) try { ctxContainer.removeEventListener('contextmenu', ctxHandler); } catch { /* ignore */ }
                    (view as any).__bmContextMenuHandler = null;
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

            // doubleClickZoom isn't part of actionMap; the view's double-click handler reads it.
            if (Object.prototype.hasOwnProperty.call(o, 'doubleClickZoom')) {
                s.doubleClickZoom = o.doubleClickZoom !== false;
            }
        }

        public static dispose(id: string) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;

            // Remove the DOM listener _wireEvents attached to the container BEFORE
            // destroying the view (which may null out s.view.container), so the
            // contextmenu handler doesn't outlive the map and stay attached to the
            // user's element.
            const container = s.view?.container as HTMLElement | null | undefined;
            if (container) {
                const view = s.view as any;
                if (view.__bmContextMenuHandler) {
                    try { container.removeEventListener('contextmenu', view.__bmContextMenuHandler); } catch { /* ignore */ }
                    view.__bmContextMenuHandler = null;
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

        /**
         * Projects a geographic coordinate to a pixel offset inside the map container.
         *
         * This is what lets BitMap anchor its own DOM - a Blazor-rendered popup, say - to a place
         * on the map without handing that DOM to the mapping library, which would take it out of
         * Blazor's hands. Returns null when the coordinate is not currently on screen.
         */
        public static project(id: string, lat: number, lng: number): { x: number, y: number } | null {
            const s = BitMapArcGis._maps[id];
            if (!s) return null;
            try {
                const screenPoint = s.view.toScreen(new s.esri.Point({ longitude: lng, latitude: lat }));
                return screenPoint ? { x: screenPoint.x, y: screenPoint.y } : null;
            } catch {
                return null;
            }
        }

        public static zoomBy(id: string, delta: number, animate: boolean) {
            const s = BitMapArcGis._require(id);
            s.view.goTo({ zoom: (s.view.zoom ?? 0) + delta }, animate === false ? { animate: false } : {}).catch(() => {});
        }

        public static panBy(id: string, dx: number, dy: number, animate: boolean) {
            const s = BitMapArcGis._require(id);
            // MapView has no pixel-space pan, so convert the screen offset from the view
            // centre into a map point and recentre on it.
            const width = s.view.width ?? 0, height = s.view.height ?? 0;
            if (!width || !height) return;
            const target = s.view.toMap({ x: width / 2 + dx, y: height / 2 + dy });
            if (!target) return;
            s.view.goTo({ center: target }, animate === false ? { animate: false } : {}).catch(() => {});
        }

        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number, _maxZoom?: number, animate: boolean = true) {
            const s = BitMapArcGis._require(id);
            const pad = paddingPx ?? 48;
            const latFrac = ((neLat - swLat) * pad) / 300;
            const lngFrac = ((neLng - swLng) * pad) / 400;
            const ext = new s.esri.Extent({
                xmin: swLng - lngFrac, ymin: swLat - latFrac,
                xmax: neLng + lngFrac, ymax: neLat + latFrac,
                spatialReference: { wkid: 4326 },
            });
            s.view.goTo(ext, { animate }).catch(() => {});
        }

        public static fitBoundsToMarkers(id: string, paddingPx: number, _maxZoom?: number, animate: boolean = true) {
            const s = BitMapArcGis._maps[id];
            if (!s) return;
            const geoms = s.markerLayer.graphics.toArray().map((g: any) => g.geometry).filter(Boolean);
            if (geoms.length === 0) return;
            const pad = paddingPx ?? 48;
            s.view.goTo(geoms, { padding: { top: pad, right: pad, bottom: pad, left: pad }, animate }).catch(() => {});
        }

        public static addMarker(id: string, markerId: string, opts: any) {
            opts = BitMapHelpers.withDefaultIcon(id, opts);
            const s = BitMapArcGis._require(id);
            const esri = s.esri;
            // A marker already on the map is updated where it stands rather than replaced, as on every provider:
            // taking the graphic off the layer and putting a new one on would make it flicker.
            const existing = s.markers[markerId];
            if (existing) {
                BitMapArcGis._updateMarker(s, markerId, existing, opts);
            } else {
                const graphic = new esri.Graphic({
                    geometry: new esri.Point({ longitude: opts.lng, latitude: opts.lat }),
                    symbol: BitMapArcGis._markerSymbol(esri, opts),
                    attributes: BitMapArcGis._markerAttributes(markerId, opts),
                });
                BitMapArcGis._built.set(graphic, opts);
                s.markerLayer.add(graphic);
                s.markers[markerId] = graphic;
            }

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
            BitMapHelpers.syncMarkers(s.markers, markerIds, markers,
                markerId => BitMapArcGis.removeMarker(id, markerId),
                (markerId, opts) => BitMapArcGis.addMarker(id, markerId, opts),
                ids => {
                    // A layer draws its graphics in the order of its collection, the last on top.
                    const graphics = s.markerLayer.graphics;
                    ids.forEach((markerId, k) => {
                        const g = s.markers[markerId];
                        if (g && graphics.getItemAt(k) !== g) graphics.reorder(g, k);
                    });
                });
        }

        /** Makes a marker's double-click the marker's (OnMarkerDoubleClick) or leaves it the map's. */
        public static setMarkerDoubleClick(id: string, enabled: boolean) {
            const s = BitMapArcGis._maps[id];
            if (s) s.markerDoubleClick = enabled;
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
            if (g) BitMapArcGis._openMarkerPopup(s, g);
        }

        /** Closes the view's popup, if it is open. Returns whether it was. */
        public static closeMarkerPopup(id: string): boolean {
            const view = BitMapArcGis._maps[id]?.view;
            if (!view?.popup?.visible) return false;
            if (typeof view.closePopup === 'function') view.closePopup();
            else view.popup.close?.();
            return true;
        }

        /**
         * A picture symbol reads the payload of a data URI as base64 whatever the URI says, so a
         * URI-encoded one - the default pin, a cluster bubble, most inline SVG icons - draws nothing.
         * Re-encoded as base64; any other URL is left alone.
         */
        private static _base64DataUri(url: string): string {
            const m = /^data:([^;,]+)(;charset=[^;,]+)?,(.*)$/is.exec(url ?? '');
            if (!m) return url;
            try {
                const bytes = new TextEncoder().encode(decodeURIComponent(m[3]));
                let binary = '';
                for (const b of bytes) binary += String.fromCharCode(b);
                return `data:${m[1]};base64,${btoa(binary)}`;
            } catch {
                return url;
            }
        }

        /**
         * Opens the view's popup. ArcGIS 4.27 moved this to the view itself and 5.0 dropped the popup's
         * own open(): the popup is created on demand now, so before the first open there is no widget
         * to call it on.
         */
        private static _openPopup(view: any, options: any) {
            if (typeof view.openPopup === 'function') view.openPopup(options);
            else view.popup?.open?.(options);
        }

        /** Opens the view's popup on a marker, when the marker has something to show in one. */
        private static _openMarkerPopup(s: any, g: any) {
            const a = g.attributes;
            const content = BitMapArcGis._popupContent(a);
            if (!content) return;
            BitMapArcGis._openPopup(s.view, { content, title: a.title || '', location: g.geometry });
            s.popupMarkerId = a.markerId;
        }

        /** What a marker's popup shows, or null when it has nothing to: PopupText goes in as text, never as markup. */
        private static _popupContent(a: any): string | HTMLElement | null {
            if (a?.popupHtml) return a.popupHtml;
            if (!a?.popupText) return null;
            const el = document.createElement('span');
            el.textContent = a.popupText;
            return el;
        }

        /**
         * A picture symbol is centred on its point; the offsets move the icon's anchor - a pin's tip by
         * default - onto the coordinate instead, as on every other provider. Pixels, not ArcGIS' points.
         */
        private static _markerSymbol(esri: any, opts: any): any {
            const w = opts.iconWidth || 32, h = opts.iconHeight || 32;
            const [ax, ay] = BitMapHelpers.readIconAnchor(opts, w, h);
            return new esri.PictureMarkerSymbol({
                url: BitMapArcGis._base64DataUri(opts.iconUrl),
                width: `${w}px`,
                height: `${h}px`,
                xoffset: `${w / 2 - ax}px`,
                yoffset: `${ay - h / 2}px`,
            });
        }

        private static _markerAttributes(markerId: string, opts: any): any {
            const w = opts.iconWidth || 32, h = opts.iconHeight || 32;
            const [ax, ay] = BitMapHelpers.readIconAnchor(opts, w, h);
            return {
                markerId, popupHtml: opts.popupHtml || '', popupText: opts.popupText || '',
                title: opts.title || '', draggable: !!opts.draggable,
                // Where the icon is drawn around its point, for the hit test a double-click needs (_markerAt).
                bmIconBox: [w, h, ax, ay],
            };
        }

        /**
         * Brings a marker's graphic in line with a new payload without replacing it (see addMarker), touching only
         * what changed - a new symbol is a new image for the layer to draw.
         */
        private static _updateMarker(s: any, markerId: string, g: any, opts: any) {
            const was = BitMapArcGis._built.get(g);
            BitMapArcGis._built.set(g, opts);
            const changed = (...fields: string[]) => BitMapHelpers.markerChanged(was, opts, ...fields);

            // Measured against where the marker is rather than the last payload: a drag or setMarkerPosition moves a
            // marker without one.
            const moved = g.geometry?.latitude !== opts.lat || g.geometry?.longitude !== opts.lng;
            if (moved) g.geometry = new s.esri.Point({ longitude: opts.lng, latitude: opts.lat });
            const iconChanged = changed(...BitMapHelpers.markerIconFields);
            if (iconChanged) g.symbol = BitMapArcGis._markerSymbol(s.esri, opts);
            const popupChanged = changed(...BitMapHelpers.markerPopupFields, 'title');
            if (iconChanged || popupChanged || changed('draggable')) g.attributes = BitMapArcGis._markerAttributes(markerId, opts);

            // The view's popup open on this marker was opened with what it said and where it was then, so it follows:
            // to where it went, with what it now says - or away, when it has nothing left to say.
            const popup = s.view.popup;
            if (s.popupMarkerId !== markerId || !popup?.visible || (!moved && !popupChanged)) return;
            const content = BitMapArcGis._popupContent(g.attributes);
            if (!content) {
                if (typeof s.view.closePopup === 'function') s.view.closePopup();
                else popup.close?.();
                return;
            }
            try {
                if (popupChanged) {
                    popup.content = content;
                    popup.title = g.attributes.title || '';
                }
                if (moved) popup.location = g.geometry;
            } catch { /* ignore */ }
        }

        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
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
            style = BitMapHelpers.resolvePathStyle(id, style);
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
            style = BitMapHelpers.resolvePathStyle(id, style);
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
            style = BitMapHelpers.resolvePathStyle(id, style);
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
            style = BitMapHelpers.resolvePathStyle(id, style);
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
            // WebTileLayer has its own subdomain support, spelled {subDomain}, so the {s}
            // placeholder is translated rather than expanded.
            const subDomains = BitMapHelpers.readSubdomains(opts.subdomains);
            const urlTemplate = (opts.urlTemplate || '').split('{s}').join('{subDomain}');
            const tl = new esri.WebTileLayer({
                urlTemplate,
                subDomains,
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

        /** Build a point symbol from a BitMap path style, so point features take the shape colors too. */
        private static _pointSym(esri: any, style: any) {
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

        /** An esri color array; its alpha is 0-1, the color's own multiplied by the opacity. */
        private static _rgbaArr(color: string, alpha: number): number[] {
            const [r, g, b, a] = BitMapHelpers.parseColor(color) ?? [51, 136, 255, 1];
            return [r, g, b, a * alpha];
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
            // yet - and even if it resolves later, releaseActive() ensures the
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

        /**
         * The id of the marker a point of the view is on, or null: the topmost marker whose icon covers it, unless a
         * shape or a GeoJSON feature is drawn over it there. Those are the view's own graphics, drawn above every
         * layer - the markers' included - and the click goes by hitTest, which answers with the topmost, so a marker
         * under a shape is the shape's to click and to double-click alike. Worked out synchronously from where each
         * graphic is drawn, where hitTest answers in a promise.
         */
        private static _markerAt(s: any, x: number, y: number): string | null {
            if (typeof x !== 'number' || typeof y !== 'number') return null;
            // Zoomed out far enough, a wrapping view draws the world - and each graphic - more than once side by side,
            // where toScreen places only one copy; the others are a whole world's width of pixels away from it.
            const sr = s.view.spatialReference;
            const worldWidth = sr?.isWrappable && sr.isWebMercator && s.view.resolution > 0
                ? 2 * 20037508.342789244 / s.view.resolution
                : 0;
            // Drawn in the layer's order, so the last one covering the point is the one on top.
            const graphics = s.markerLayer.graphics.toArray();
            for (let i = graphics.length - 1; i >= 0; i--) {
                const g = graphics[i];
                const a = g?.attributes;
                if (!a?.markerId || s.markers[a.markerId] !== g || !Array.isArray(a.bmIconBox)) continue;
                const p = s.view.toScreen(g.geometry);
                if (!p) continue;
                const [w, h, ax, ay] = a.bmIconBox;
                const top = p.y - ay;
                if (y < top || y > top + h) continue;
                let dx = x - (p.x - ax);
                if (worldWidth > w) dx = ((dx % worldWidth) + worldWidth) % worldWidth;
                if (dx < 0 || dx > w) continue;
                for (const shape of s.view.graphics.toArray()) {
                    if (BitMapArcGis._covers(s, shape, x, y, worldWidth)) return null;
                }
                return a.markerId;
            }
            return null;
        }

        /**
         * Whether one of the view's own graphics - a shape or a GeoJSON feature - is drawn over a point of the view: a
         * polygon's fill or outline, a line's stroke or a point's symbol, give or take the few pixels hitTest allows.
         */
        private static _covers(s: any, graphic: any, x: number, y: number, worldWidth: number): boolean {
            const geometry = graphic?.geometry;
            const symbol = graphic?.symbol;
            // Symbol sizes are in points.
            const pixels = (points: any) => (Number(points) || 0) * 4 / 3;
            const slack = 3;
            // Where the point falls on each copy of the world that has some of [left, right] in it.
            const copies = (left: number, right: number) => {
                if (!(worldWidth > 0)) return x >= left && x <= right ? [x] : [];
                const xs: number[] = [];
                for (let k = Math.ceil((left - x) / worldWidth); x + k * worldWidth <= right; k++) xs.push(x + k * worldWidth);
                return xs;
            };

            if (geometry?.type === 'point') {
                const p = s.view.toScreen(geometry);
                if (!p) return false;
                const radius = pixels(symbol?.size) / 2 + pixels(symbol?.outline?.width) / 2 + slack;
                return copies(p.x - radius, p.x + radius).some(cx => Math.hypot(cx - p.x, y - p.y) <= radius);
            }

            const isPolygon = geometry?.type === 'polygon';
            const lines: number[][][] | undefined = isPolygon ? geometry.rings : geometry?.type === 'polyline' ? geometry.paths : undefined;
            if (!lines) return false;
            const screenLines: { x: number, y: number }[][] = [];
            let left = Infinity, right = -Infinity;
            for (const line of lines) {
                const screenLine: { x: number, y: number }[] = [];
                for (const [lng, lat] of line) {
                    const p = s.view.toScreen(new s.esri.Point({ longitude: lng, latitude: lat }));
                    if (!p) continue;
                    screenLine.push(p);
                    left = Math.min(left, p.x);
                    right = Math.max(right, p.x);
                }
                screenLines.push(screenLine);
            }
            const halfStroke = pixels(isPolygon ? symbol?.outline?.width : symbol?.width) / 2 + slack;

            for (const cx of copies(left - halfStroke, right + halfStroke)) {
                let inside = false;
                for (const line of screenLines) {
                    // A ring closes back on its first vertex whether or not it repeats it.
                    const edges = isPolygon ? line.length : line.length - 1;
                    for (let i = 0; i < edges; i++) {
                        const a = line[i], b = line[(i + 1) % line.length];
                        if (BitMapArcGis._distanceToSegment(cx, y, a, b) <= halfStroke) return true;
                        // Even-odd: a ray to the right of the point crosses the edge.
                        if (isPolygon && (a.y > y) !== (b.y > y) && cx < a.x + (y - a.y) * (b.x - a.x) / (b.y - a.y)) inside = !inside;
                    }
                }
                if (inside) return true;
            }
            return false;
        }

        private static _distanceToSegment(x: number, y: number, a: { x: number, y: number }, b: { x: number, y: number }): number {
            const dx = b.x - a.x, dy = b.y - a.y;
            const lengthSquared = dx * dx + dy * dy;
            const t = lengthSquared > 0 ? Math.max(0, Math.min(1, ((x - a.x) * dx + (y - a.y) * dy) / lengthSquared)) : 0;
            return Math.hypot(x - (a.x + t * dx), y - (a.y + t * dy));
        }

        private static _wireEvents(s: any) {
            const view = s.view;
            // Read s.dotnetObj at dispatch time. dispose() nulls it, and the hitTest
            // promise below resolves asynchronously - a captured handle would invoke a
            // DotNetObjectReference that has already been released.
            const dn = () => s.dotnetObj;
            view.on('click', (event: any) => {
                view.hitTest(event).then((response: any) => {
                    let hit = false;
                    for (const r of response.results) {
                        const g = r.graphic;
                        const a = g?.attributes;
                        if (!a) continue;
                        if (a.markerId && s.markers[a.markerId]) {
                            hit = true;
                            dn()?.invokeMethodAsync('OnMarkerClick', a.markerId);
                            BitMapArcGis._openMarkerPopup(s, g);
                            break;
                        }
                        if (a._bmKind === 'geojson' && a._bmLayerId && s.geoJsonLayers[a._bmLayerId]) {
                            hit = true;
                            const props = { ...a }; delete props._bmLayerId; delete props._bmKind;
                            dn()?.invokeMethodAsync('OnGeoJsonFeatureClick', a._bmLayerId, props);
                            break;
                        }
                        if (a.bmVectorKind && a.layerId && s.layers[a.layerId]) {
                            hit = true;
                            if (event.mapPoint) {
                                dn()?.invokeMethodAsync('OnVectorClick', a.layerId, a.bmVectorKind, {
                                    lat: event.mapPoint.latitude, lng: event.mapPoint.longitude,
                                });
                            }
                            break;
                        }
                    }
                    if (!hit && event.mapPoint) {
                        dn()?.invokeMethodAsync('OnClick', { lat: event.mapPoint.latitude, lng: event.mapPoint.longitude });
                    }
                });
            });
            // MapView exposes no contextmenu event, so it comes off the container element and
            // the screen point is converted with the view's own projection.
            const container = view.container as HTMLElement | null | undefined;
            if (container) {
                const handler = (evt: MouseEvent) => {
                    if (s.suppressBrowserContextMenu) evt.preventDefault();
                    const target = dn();
                    if (!target) return;
                    const rect = container.getBoundingClientRect();
                    const point = view.toMap({ x: evt.clientX - rect.left, y: evt.clientY - rect.top });
                    if (!point) return;
                    target.invokeMethodAsync('OnContextMenu', { lat: point.latitude, lng: point.longitude });
                };
                container.addEventListener('contextmenu', handler);
                (view as any).__bmContextMenuHandler = handler;
            }

            view.on('double-click', (event: any) => {
                // The view zooms in on a double-click unless its propagation is stopped before this handler returns -
                // which is where DoubleClickZoom is honoured, and too early for hitTest's promise, hence _markerAt.
                if (!s.doubleClickZoom) event.stopPropagation();
                // A bubble always keeps its double-click; a marker only while OnMarkerDoubleClick has a handler. With
                // neither in play there is nothing to look for.
                const markerId = s.markerDoubleClick || BitMapCluster.hasClusterIds(s.markers)
                    ? BitMapArcGis._markerAt(s, event.x, event.y)
                    : null;
                if (markerId && BitMapCluster.isClusterId(markerId)) {
                    event.stopPropagation();
                    // The view holds a click back until it knows it is not the start of a double-click, and drops both
                    // clicks of one - so the click that expands a cluster bubble is reported now, or it never is.
                    dn()?.invokeMethodAsync('OnMarkerClick', markerId);
                    return;
                }
                if (markerId && s.markerDoubleClick) {
                    event.stopPropagation();
                    dn()?.invokeMethodAsync('OnMarkerDoubleClick', markerId);
                    return;
                }
                if (event.mapPoint) dn()?.invokeMethodAsync('OnDoubleClick', { lat: event.mapPoint.latitude, lng: event.mapPoint.longitude });
            });
            // A double-click with Control held zooms out, and reaches the handlers registered for that modifier.
            view.on('double-click', ['Control'], (event: any) => {
                if (!s.doubleClickZoom) event.stopPropagation();
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

// bit-lazy: teardown BitMapArcGis.dispose
