namespace BitBlazorUI {

    type GlState = {
        gl: any;            // global namespace (maplibregl or mapboxgl)
        map: any;
        dotnetObj: DotNetObject | null | undefined;
        markers: { [id: string]: { marker: any } };
        vectorCatalog: { [id: string]: { sourceId: string, layerIds: string[], handlers: { layerId: string, handler: any }[] } };
        tileOverlayCatalog: { [id: string]: { sourceId: string, layerId: string } };
        navControl: any;
        lastStyleUrl: string;
        isDisposed: boolean;
        viewListeners?: { notify: () => void };
    };

    /**
     * Common implementation shared by MapLibre GL and Mapbox GL providers.
     * Subclasses call BitMapGlBase.create(...) and BitMapGlBase.&lt;method&gt;(...) on a
     * provider-tagged map id; the global namespace (maplibregl / mapboxgl) is read from
     * globalThis at call time.
     */
    export class BitMapGlBase {
        protected static _stores: { [provider: string]: { [id: string]: GlState } } = {};

        protected static _store(provider: string): { [id: string]: GlState } {
            if (!BitMapGlBase._stores[provider]) BitMapGlBase._stores[provider] = {};
            return BitMapGlBase._stores[provider];
        }

        public static async init(provider: string, glGlobalName: string, defaultStyleUrl: string,
                                 id: string, canvasId: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            element = await BitMapHelpers.resolveMapCanvas(canvasId, element);

            // Wait for the global to be available (script may still be initializing after onload)
            if (!(globalThis as any)[glGlobalName]) {
                await BitMapHelpers.waitForGlobal(glGlobalName, () => !!(globalThis as any)[glGlobalName], 10_000);
            }
            const gl = (globalThis as any)[glGlobalName];
            if (!gl) throw new Error(`${glGlobalName} is not loaded.`);

            const o = options || {};
            const accessToken = o.accessToken;
            if (accessToken !== undefined && glGlobalName === 'mapboxgl') gl.accessToken = accessToken || '';

            const center: [number, number] = [o.center?.lng ?? -0.09, o.center?.lat ?? 51.505];
            const zoom: number = o.zoom ?? 13;
            const styleUrl: string = o.styleUrl || defaultStyleUrl;

            const map = new gl.Map({
                container: element,
                style: styleUrl,
                center, zoom,
                minZoom: o.minZoom ?? undefined,
                maxZoom: o.maxZoom ?? undefined,
                attributionControl: o.attributionControl !== false,
            });

            await new Promise<void>((resolve, reject) => {
                let settled = false;
                const timeoutHandle = setTimeout(() => {
                    if (settled) return;
                    settled = true;
                    reject(new Error('GL map load timeout (30s)'));
                }, 30000);
                map.once('load', () => {
                    if (settled) return;
                    settled = true;
                    clearTimeout(timeoutHandle);
                    resolve();
                });
                map.once('error', (e: any) => {
                    if (settled) return;
                    settled = true;
                    clearTimeout(timeoutHandle);
                    reject(e?.error ?? new Error('Map error'));
                });
            });

            BitMapGlBase._applyMaxBounds(map, o.maxBounds);
            BitMapGlBase._applyInteractivity(map, o);

            const state: GlState = {
                gl, map, dotnetObj,
                markers: {},
                vectorCatalog: {},
                tileOverlayCatalog: {},
                navControl: null,
                lastStyleUrl: styleUrl,
                isDisposed: false,
            };
            BitMapGlBase._ensureNavControl(state, o);

            if (dotnetObj) {
                map.on('click', (e: any) => {
                    // Suppress map-level OnClick when the click hits any registered
                    // vector/geojson layer; those layers have their own click handlers
                    // and OnClick is contractually "not on a marker or vector layer".
                    const vectorLayerIds = BitMapGlBase._collectVectorLayerIds(state);
                    if (vectorLayerIds.length > 0) {
                        try {
                            const hits = map.queryRenderedFeatures(e.point, { layers: vectorLayerIds });
                            if (hits && hits.length > 0) return;
                        } catch { /* ignore - fall through and fire OnClick */ }
                    }
                    if (state.isDisposed || !state.dotnetObj) return;
                    state.dotnetObj.invokeMethodAsync('OnClick', { lat: e.lngLat.lat, lng: e.lngLat.lng });
                });
                map.on('dblclick', (e: any) => {
                    if (state.isDisposed || !state.dotnetObj) return;
                    state.dotnetObj.invokeMethodAsync('OnDoubleClick', { lat: e.lngLat.lat, lng: e.lngLat.lng });
                });
                // The microtask can run after dispose() has nulled state.dotnetObj
                // and removed the map. Guard the lambda so it never invokes a disposed
                // .NET object and never reads from a disposed map. dispose() also
                // unwires these listeners explicitly via state.viewListeners.
                const notify = () => queueMicrotask(() => {
                    if (state.isDisposed || !state.dotnetObj) return;
                    state.dotnetObj.invokeMethodAsync('OnViewChanged', BitMapGlBase._readView(map));
                });
                map.on('moveend', notify);
                map.on('zoomend', notify);
                state.viewListeners = { notify };
            }

            BitMapGlBase._store(provider)[id] = state;
            queueMicrotask(() => map.resize());
        }

        public static sync(provider: string, id: string, options: any) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            const map = s.map;
            const o = options || {};
            const center: [number, number] = o.center ? [o.center.lng, o.center.lat] : [map.getCenter().lng, map.getCenter().lat];
            const zoom = o.zoom ?? map.getZoom();
            map.jumpTo({ center, zoom, essential: true });

            if (o.accessToken !== undefined && s.gl.accessToken !== undefined) {
                s.gl.accessToken = o.accessToken || '';
            }

            if (o.styleUrl && o.styleUrl !== s.lastStyleUrl) {
                s.lastStyleUrl = o.styleUrl;
                // Unregister click handlers before style swap to prevent dangling listeners
                for (const key of Object.keys(s.vectorCatalog)) {
                    for (const h of s.vectorCatalog[key].handlers) {
                        try { map.off('click', h.layerId, h.handler); } catch { /* ignore */ }
                    }
                }
                map.setStyle(o.styleUrl);
                map.once('styledata', () => {
                    s.vectorCatalog = {};
                    s.tileOverlayCatalog = {};
                });
            }

            // Only apply each of these helpers when the caller actually supplied the
            // corresponding option, otherwise a partial sync (e.g. style/token-only update)
            // would silently reset bounds/interactivity/nav-control to their defaults.
            const has = (k: string) => Object.prototype.hasOwnProperty.call(o, k);
            if (has('maxBounds')) {
                BitMapGlBase._applyMaxBounds(map, o.maxBounds);
            }
            if (has('scrollWheelZoom') || has('doubleClickZoom') || has('boxZoom')
                || has('dragPan') || has('dragging') || has('dragRotate') || has('keyboardNavigation')) {
                BitMapGlBase._applyInteractivity(map, o);
            }
            if (has('showNavigationControl')) {
                BitMapGlBase._ensureNavControl(s, o);
            }
        }

        public static dispose(provider: string, id: string) {
            const store = BitMapGlBase._store(provider);
            const s = store[id];
            if (!s) return;
            // Mark disposed first so any queued microtask notify can short-circuit
            // before invoking the .NET object.
            s.isDisposed = true;
            try {
                // Remove the view-change listeners so no further notifications can
                // be queued after dispose. map.remove() below also tears down all
                // listeners, but doing this explicitly defends against any callback
                // that might already be in flight before remove() runs.
                if (s.viewListeners) {
                    try { s.map.off('moveend', s.viewListeners.notify); } catch { /* ignore */ }
                    try { s.map.off('zoomend', s.viewListeners.notify); } catch { /* ignore */ }
                    s.viewListeners = undefined;
                }
                for (const key in s.markers) {
                    try { s.markers[key].marker.remove(); } catch { /* ignore */ }
                }
                if (s.navControl) {
                    try { s.map.removeControl(s.navControl); } catch { /* ignore */ }
                }
                s.map.remove();
            } catch { /* ignore */ }
            s.dotnetObj = null;
            delete store[id];
        }

        public static invalidateSize(provider: string, id: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (s) try { s.map.resize(); } catch { /* ignore */ }
        }

        public static getView(provider: string, id: string) {
            return BitMapGlBase._readView(BitMapGlBase._require(provider, id).map);
        }

        public static setView(provider: string, id: string, lat: number, lng: number, zoom: number | null, animate: boolean) {
            const s = BitMapGlBase._require(provider, id);
            const z = zoom ?? s.map.getZoom();
            if (animate === false) {
                s.map.jumpTo({ center: [lng, lat], zoom: z });
            } else {
                s.map.easeTo({ center: [lng, lat], zoom: z, essential: true });
            }
        }

        public static flyTo(provider: string, id: string, lat: number, lng: number, zoom: number | null) {
            const s = BitMapGlBase._require(provider, id);
            s.map.flyTo({ center: [lng, lat], zoom: zoom ?? s.map.getZoom(), essential: true });
        }

        public static fitBounds(provider: string, id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number) {
            const s = BitMapGlBase._require(provider, id);
            s.map.fitBounds([[swLng, swLat], [neLng, neLat]], { padding: paddingPx ?? 48, maxZoom: 18 });
        }

        public static fitBoundsToMarkers(provider: string, id: string, paddingPx: number) {
            const s = BitMapGlBase._require(provider, id);
            const ids = Object.keys(s.markers);
            if (ids.length === 0) return;
            const b = new s.gl.LngLatBounds();
            for (const k of ids) b.extend(s.markers[k].marker.getLngLat());
            s.map.fitBounds(b, { padding: paddingPx ?? 48, maxZoom: 18 });
        }

        public static addMarker(provider: string, id: string, markerId: string, opts: any) {
            const s = BitMapGlBase._require(provider, id);
            const gl = s.gl;
            const lat = opts.lat, lng = opts.lng;
            const draggable = !!opts.draggable;

            let marker: any;
            if (opts.iconUrl) {
                const el = document.createElement('div');
                el.style.width = `${opts.iconWidth || 32}px`;
                el.style.height = `${opts.iconHeight || 32}px`;
                el.style.backgroundImage = `url(${opts.iconUrl})`;
                el.style.backgroundSize = 'contain';
                el.style.cursor = 'pointer';
                marker = new gl.Marker({ element: el, draggable }).setLngLat([lng, lat]).addTo(s.map);
            } else {
                marker = new gl.Marker({ draggable }).setLngLat([lng, lat]).addTo(s.map);
            }

            // PopupHtml takes precedence; PopupText uses safe setText to avoid XSS.
            if (opts.popupHtml) {
                marker.setPopup(new gl.Popup({ offset: 25 }).setHTML(String(opts.popupHtml)));
            } else if (opts.popupText) {
                marker.setPopup(new gl.Popup({ offset: 25 }).setText(String(opts.popupText)));
            }
            if (opts.title) marker.getElement()?.setAttribute('title', opts.title);

            if (s.dotnetObj) {
                const dn = s.dotnetObj;
                marker.getElement()?.addEventListener('click', (ev: Event) => {
                    ev.stopPropagation();
                    dn.invokeMethodAsync('OnMarkerClick', markerId);
                });
                if (draggable) {
                    marker.on('dragend', () => {
                        const p = marker.getLngLat();
                        dn.invokeMethodAsync('OnMarkerDragEnd', markerId, { lat: p.lat, lng: p.lng });
                    });
                }
            }

            const existing = s.markers[markerId];
            if (existing) try { existing.marker.remove(); } catch { /* ignore */ }
            s.markers[markerId] = { marker };
        }

        public static removeMarker(provider: string, id: string, markerId: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            const row = s.markers[markerId];
            if (row) { row.marker.remove(); delete s.markers[markerId]; }
        }

        public static clearMarkers(provider: string, id: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            for (const key in s.markers) s.markers[key].marker.remove();
            s.markers = {};
        }

        public static syncMarkers(provider: string, id: string, markerIds: string[], markers: any[]) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            for (const key in s.markers) s.markers[key].marker.remove();
            s.markers = {};
            const len = Math.min(markerIds?.length ?? 0, markers?.length ?? 0);
            for (let i = 0; i < len; i++) BitMapGlBase.addMarker(provider, id, markerIds[i], markers[i]);
        }

        public static setMarkerPosition(provider: string, id: string, markerId: string, lat: number, lng: number) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            const row = s.markers[markerId];
            if (row) row.marker.setLngLat([lng, lat]);
        }

        public static openMarkerPopup(provider: string, id: string, markerId: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            const row = s.markers[markerId];
            if (!row) return;
            const popup = row.marker.getPopup();
            if (!popup) return;
            if (typeof popup.isOpen === 'function') {
                if (!popup.isOpen()) row.marker.togglePopup();
            } else {
                row.marker.togglePopup();
            }
        }

        public static addPolyline(provider: string, id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapGlBase._require(provider, id);
            BitMapGlBase._removeVector(s, layerId);
            const sourceId = `bm-src-${id}-${layerId}`;
            const lineId = `bm-line-${id}-${layerId}`;
            s.map.addSource(sourceId, {
                type: 'geojson',
                data: { type: 'Feature', properties: {}, geometry: { type: 'LineString', coordinates: latlngs.map(p => [p.lng, p.lat]) } },
            });
            s.map.addLayer({
                id: lineId, type: 'line', source: sourceId,
                layout: { 'line-join': 'round', 'line-cap': 'round' },
                paint: BitMapGlBase._linePaint(style),
            });
            s.vectorCatalog[layerId] = { sourceId, layerIds: [lineId], handlers: [] };
            const h = BitMapGlBase._wireVectorClick(s, lineId, layerId, 'polyline');
            if (h) s.vectorCatalog[layerId].handlers.push(h);
        }

        public static addPolygon(provider: string, id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapGlBase._require(provider, id);
            BitMapGlBase._removeVector(s, layerId);
            const ring = latlngs.map(p => [p.lng, p.lat] as [number, number]);
            if (ring.length > 0) {
                const a = ring[0], b = ring[ring.length - 1];
                if (a[0] !== b[0] || a[1] !== b[1]) ring.push([a[0], a[1]]);
            }
            BitMapGlBase._addPolygonLayer(s, id, layerId, ring, style, 'polygon');
        }

        public static addCircle(provider: string, id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            const s = BitMapGlBase._require(provider, id);
            BitMapGlBase._removeVector(s, layerId);
            const ring = BitMapHelpers.circleRingLngLat(lat, lng, radiusMeters);
            BitMapGlBase._addPolygonLayer(s, id, layerId, ring, style, 'circle');
        }

        public static addRectangle(provider: string, id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            const s = BitMapGlBase._require(provider, id);
            BitMapGlBase._removeVector(s, layerId);
            const ring: [number, number][] = [
                [swLng, swLat], [neLng, swLat], [neLng, neLat], [swLng, neLat], [swLng, swLat],
            ];
            BitMapGlBase._addPolygonLayer(s, id, layerId, ring, style, 'rectangle');
        }

        public static addGeoJson(provider: string, id: string, layerId: string, geoJsonString: string, style: any) {
            let gj: any;
            try { gj = JSON.parse(geoJsonString); } catch { throw new Error('Invalid GeoJSON string'); }
            const s = BitMapGlBase._require(provider, id);
            BitMapGlBase._removeVector(s, layerId);
            const sourceId = `bm-src-${id}-${layerId}`;
            const fillId = `bm-fill-${id}-${layerId}`;
            const lineId = `bm-line-${id}-${layerId}`;
            const circleId = `bm-circle-${id}-${layerId}`;
            s.map.addSource(sourceId, { type: 'geojson', data: gj });
            s.map.addLayer({ id: fillId, type: 'fill', source: sourceId, paint: BitMapGlBase._fillPaint(style) });
            s.map.addLayer({ id: lineId, type: 'line', source: sourceId, paint: BitMapGlBase._linePaint(style) });
            s.map.addLayer({ id: circleId, type: 'circle', source: sourceId, paint: BitMapGlBase._circlePaint(style) });
            const dn = s.dotnetObj;
            const handlers: { layerId: string, handler: any }[] = [];
            if (dn) {
                const handler = (e: any) => {
                    if (e.features?.[0]) dn.invokeMethodAsync('OnGeoJsonFeatureClick', layerId, e.features[0].properties || {});
                };
                s.map.on('click', fillId, handler);
                s.map.on('click', lineId, handler);
                s.map.on('click', circleId, handler);
                handlers.push({ layerId: fillId, handler });
                handlers.push({ layerId: lineId, handler });
                handlers.push({ layerId: circleId, handler });
            }
            s.vectorCatalog[layerId] = { sourceId, layerIds: [fillId, lineId, circleId], handlers };
        }

        public static removeLayer(provider: string, id: string, layerId: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            BitMapGlBase._removeVector(s, layerId);
        }

        public static clearVectorLayers(provider: string, id: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            for (const key of Object.keys(s.vectorCatalog)) BitMapGlBase._removeVector(s, key);
        }

        public static addTileOverlay(provider: string, id: string, opts: any) {
            const s = BitMapGlBase._require(provider, id);
            const sourceId = `bm-raster-${id}-${opts.id}`;
            const layerId = `bm-raster-layer-${id}-${opts.id}`;
            const url = (opts.urlTemplate || '').replace('{s}', 'a');
            // Remove existing overlay if present so new options take effect.
            const existing = s.tileOverlayCatalog[opts.id];
            if (existing || s.map.getSource(sourceId)) {
                try {
                    if (s.map.getLayer(layerId)) s.map.removeLayer(layerId);
                    if (s.map.getSource(sourceId)) s.map.removeSource(sourceId);
                } catch { /* ignore */ }
                delete s.tileOverlayCatalog[opts.id];
            }
            s.map.addSource(sourceId, {
                type: 'raster', tiles: [url], tileSize: 256,
                attribution: opts.attribution || '',
                maxzoom: opts.maxZoom ?? 19,
            });
            s.map.addLayer({
                id: layerId, type: 'raster', source: sourceId,
                paint: { 'raster-opacity': opts.opacity ?? 1 },
            });
            s.tileOverlayCatalog[opts.id] = { sourceId, layerId };
        }

        public static removeTileOverlay(provider: string, id: string, overlayId: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            const row = s.tileOverlayCatalog[overlayId];
            if (!row) return;
            try {
                if (s.map.getLayer(row.layerId)) s.map.removeLayer(row.layerId);
                if (s.map.getSource(row.sourceId)) s.map.removeSource(row.sourceId);
            } catch { /* ignore */ }
            delete s.tileOverlayCatalog[overlayId];
        }

        // ---- helpers ----

        private static _require(provider: string, id: string): GlState {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) throw new Error(`${provider}: unknown map id '${id}'`);
            return s;
        }

        private static _collectVectorLayerIds(s: GlState): string[] {
            const ids: string[] = [];
            for (const key of Object.keys(s.vectorCatalog)) {
                const entry = s.vectorCatalog[key];
                for (const lid of entry.layerIds) {
                    if (s.map.getLayer && s.map.getLayer(lid)) ids.push(lid);
                }
            }
            return ids;
        }

        private static _readView(map: any) {
            const c = map.getCenter();
            const b = map.getBounds();
            return {
                center: { lat: c.lat, lng: c.lng },
                zoom: map.getZoom(),
                bounds: {
                    southWest: { lat: b.getSouthWest().lat, lng: b.getSouthWest().lng },
                    northEast: { lat: b.getNorthEast().lat, lng: b.getNorthEast().lng },
                },
            };
        }

        private static _applyMaxBounds(map: any, mb: any) {
            if (!mb) { try { map.setMaxBounds(null); } catch { /* ignore */ } return; }
            map.setMaxBounds([[mb.southWest.lng, mb.southWest.lat], [mb.northEast.lng, mb.northEast.lat]]);
        }

        private static _applyInteractivity(map: any, o: any) {
            if ('scrollWheelZoom' in o) {
                if (o.scrollWheelZoom === false) map.scrollZoom?.disable(); else map.scrollZoom?.enable();
            }
            if ('doubleClickZoom' in o) {
                if (o.doubleClickZoom === false) map.doubleClickZoom?.disable(); else map.doubleClickZoom?.enable();
            }
            if ('boxZoom' in o) {
                if (o.boxZoom === false) map.boxZoom?.disable(); else map.boxZoom?.enable();
            }
            if ('dragPan' in o || 'dragging' in o) {
                const dp = o.dragPan ?? o.dragging;
                if (dp === false) map.dragPan?.disable(); else map.dragPan?.enable();
            }
            if ('dragRotate' in o) {
                if (o.dragRotate === false) map.dragRotate?.disable(); else map.dragRotate?.enable();
            }
            if ('keyboardNavigation' in o && map.keyboard) {
                if (o.keyboardNavigation === false) map.keyboard.disable(); else map.keyboard.enable();
            }
        }

        private static _ensureNavControl(s: GlState, o: any) {
            const gl = s.gl;
            const show = o.showNavigationControl !== false;
            if (s.navControl) {
                try { s.map.removeControl(s.navControl); } catch { /* ignore */ }
                s.navControl = null;
            }
            if (show && gl.NavigationControl) {
                s.navControl = new gl.NavigationControl();
                s.map.addControl(s.navControl, 'top-right');
            }
        }

        private static _linePaint(style: any) {
            const st = BitMapHelpers.readPathStyle(style);
            const paint: any = {
                'line-color': st.color,
                'line-width': st.weight,
                'line-opacity': st.opacity,
            };
            if (st.dashArray) {
                // Accept commas and/or any whitespace as separators (e.g. "4 2" or "4, 2").
                const parts = String(st.dashArray)
                    .split(/[\s,]+/)
                    .map(t => t.trim())
                    .filter(t => t.length > 0)
                    .map(t => parseFloat(t))
                    .filter(n => Number.isFinite(n));
                if (parts.length > 0) {
                    paint['line-dasharray'] = parts;
                }
            }
            return paint;
        }

        private static _fillPaint(style: any) {
            const st = BitMapHelpers.readPathStyle(style);
            return {
                'fill-color': st.fillColor,
                'fill-opacity': st.fillOpacity,
                'fill-outline-color': st.color,
            };
        }

        private static _circlePaint(style: any) {
            const st = BitMapHelpers.readPathStyle(style);
            return {
                'circle-color': st.fillColor,
                'circle-opacity': st.fillOpacity,
                'circle-stroke-color': st.color,
                'circle-stroke-width': st.weight,
                'circle-stroke-opacity': st.opacity,
                'circle-radius': 5,
            };
        }

        private static _addPolygonLayer(s: GlState, mapId: string, layerId: string, ring: [number, number][], style: any, kind: string) {
            const sourceId = `bm-src-${mapId}-${layerId}`;
            const fillId = `bm-fill-${mapId}-${layerId}`;
            const lineId = `bm-line-${mapId}-${layerId}`;
            s.map.addSource(sourceId, { type: 'geojson', data: { type: 'Feature', properties: {}, geometry: { type: 'Polygon', coordinates: [ring] } } });
            s.map.addLayer({ id: fillId, type: 'fill', source: sourceId, paint: BitMapGlBase._fillPaint(style) });
            s.map.addLayer({ id: lineId, type: 'line', source: sourceId, paint: BitMapGlBase._linePaint(style) });
            const handlers: { layerId: string, handler: any }[] = [];
            const h1 = BitMapGlBase._wireVectorClick(s, fillId, layerId, kind);
            const h2 = BitMapGlBase._wireVectorClick(s, lineId, layerId, kind);
            if (h1) handlers.push(h1);
            if (h2) handlers.push(h2);
            s.vectorCatalog[layerId] = { sourceId, layerIds: [fillId, lineId], handlers };
        }

        private static _removeVector(s: GlState, layerId: string) {
            const entry = s.vectorCatalog[layerId];
            if (!entry) return;
            // Unregister event handlers before removing layers
            for (const h of entry.handlers) {
                try { s.map.off('click', h.layerId, h.handler); } catch { /* ignore */ }
            }
            for (const lid of entry.layerIds) {
                try { if (s.map.getLayer(lid)) s.map.removeLayer(lid); } catch { /* ignore */ }
            }
            try { if (s.map.getSource(entry.sourceId)) s.map.removeSource(entry.sourceId); } catch { /* ignore */ }
            delete s.vectorCatalog[layerId];
        }

        private static _wireVectorClick(s: GlState, glLayerId: string, layerId: string, kind: string): { layerId: string, handler: any } | null {
            if (!s.dotnetObj) return null;
            const dn = s.dotnetObj;
            const handler = (e: any) => {
                dn.invokeMethodAsync('OnVectorClick', layerId, kind, { lat: e.lngLat.lat, lng: e.lngLat.lng });
            };
            s.map.on('click', glLayerId, handler);
            return { layerId: glLayerId, handler };
        }
    }
}
