namespace BitBlazorUI {

    type GlMarker = {
        marker: any;
        /** The hover tooltip: a second Popup, added on hover and removed on leave (see _createTooltip). */
        tooltip?: any;
        /** What the marker was last built from, which an update compares against (_updateMarker). */
        opts: any;
    };

    type GlState = {
        gl: any;            // global namespace (maplibregl or mapboxgl)
        map: any;
        dotnetObj: DotNetObject | null | undefined;
        markers: { [id: string]: GlMarker };
        vectorCatalog: { [id: string]: { sourceId: string, layerIds: string[], handlers: { layerId: string, handler: any }[] } };
        tileOverlayCatalog: { [id: string]: { sourceId: string, layerId: string } };
        navControl: any;
        lastStyleUrl: string;
        isDisposed: boolean;
        viewTimer: any;
        viewListeners?: { notify: () => void };
        /** Whether a marker's double-click is the marker's: only while OnMarkerDoubleClick has a handler. */
        markerDoubleClick: boolean;
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
                // Spread last so an escape-hatch entry wins over what the component models.
                ...(o.additionalOptions || {}),
            });

            try {
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
            } catch (err) {
                // The gl.Map was constructed before we started waiting, so a load
                // failure/timeout would otherwise strand a live map that holds a WebGL
                // context. Browsers cap the number of simultaneous WebGL contexts (~8-16),
                // so leaking one per failed init eventually kills every map on the page.
                try { map.remove(); } catch { /* ignore */ }
                throw err;
            }

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
                viewTimer: null,
                markerDoubleClick: false,
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
                    // A marker's element sits in the canvas container, where the library listens, so a double-click on
                    // it arrives here too. Preventing this event, rather than stopping the DOM one on the element, is what
                    // keeps the library's double-click zoom off it while the page's own listeners still hear it. A bubble
                    // always keeps its double-click; a marker only while OnMarkerDoubleClick has a handler.
                    const markerId = BitMapGlBase._markerIdOf(state, e.originalEvent?.target);
                    if (markerId !== null && (state.markerDoubleClick || BitMapCluster.isClusterId(markerId))) {
                        e.preventDefault();
                        // A bubble's has been reported already, as the click of the pair that expanded it.
                        if (state.isDisposed || BitMapCluster.isClusterId(markerId)) return;
                        state.dotnetObj?.invokeMethodAsync('OnMarkerDoubleClick', markerId);
                        return;
                    }
                    if (state.isDisposed || !state.dotnetObj) return;
                    state.dotnetObj.invokeMethodAsync('OnDoubleClick', { lat: e.lngLat.lat, lng: e.lngLat.lng });
                });
                map.on('contextmenu', (e: any) => {
                    if (o.suppressBrowserContextMenu) e.originalEvent?.preventDefault?.();
                    if (state.isDisposed || !state.dotnetObj) return;
                    state.dotnetObj.invokeMethodAsync('OnContextMenu', { lat: e.lngLat.lat, lng: e.lngLat.lng });
                });
                // The microtask can run after dispose() has nulled state.dotnetObj
                // and removed the map. Guard the lambda so it never invokes a disposed
                // .NET object and never reads from a disposed map. dispose() also
                // unwires these listeners explicitly via state.viewListeners.
                // Coalesce the move/zoom burst a single drag produces into one interop
                // round-trip per idle window (each notification is a SignalR message
                // under Blazor Server). Matches every other provider's 80ms window.
                const notify = () => {
                    if (state.isDisposed) return;
                    clearTimeout(state.viewTimer);
                    state.viewTimer = setTimeout(() => {
                        if (state.isDisposed || !state.dotnetObj) return;
                        state.dotnetObj.invokeMethodAsync('OnViewChanged', BitMapGlBase._readView(map));
                    }, BitMapHelpers.viewNotifyDebounceMs);
                };
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
            // Zoom limits are constructor options; without the setters a provider that
            // changes MinZoom/MaxZoom after init would be silently ignored.
            if (has('minZoom')) {
                try { map.setMinZoom(o.minZoom ?? null); } catch { /* ignore */ }
            }
            if (has('maxZoom')) {
                try { map.setMaxZoom(o.maxZoom ?? null); } catch { /* ignore */ }
            }
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
            if (s.viewTimer) { clearTimeout(s.viewTimer); s.viewTimer = null; }
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
                    BitMapGlBase._destroyMarker(s.markers[key]);
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

        /**
         * Projects a geographic coordinate to a pixel offset inside the map container.
         *
         * This is what lets BitMap anchor its own DOM - a Blazor-rendered popup, say - to a place
         * on the map without handing that DOM to the mapping library, which would take it out of
         * Blazor's hands. Returns null when the coordinate is not currently on screen.
         */
        public static project(provider: string, id: string, lat: number, lng: number): { x: number, y: number } | null {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return null;
            try {
                const point = s.map.project([lng, lat]);
                return { x: point.x, y: point.y };
            } catch {
                return null;
            }
        }

        public static zoomBy(provider: string, id: string, delta: number, animate: boolean) {
            const s = BitMapGlBase._require(provider, id);
            const target = s.map.getZoom() + delta;
            if (animate === false) s.map.jumpTo({ zoom: target });
            else s.map.easeTo({ zoom: target, essential: true });
        }

        public static panBy(provider: string, id: string, dx: number, dy: number, animate: boolean) {
            const s = BitMapGlBase._require(provider, id);
            // `essential` is an animation option, not event data - passed as the third argument it
            // is handed to the emitted events and a reduced-motion browser still skips the pan.
            s.map.panBy([dx, dy], { duration: animate === false ? 0 : 300, essential: true });
        }

        public static fitBounds(provider: string, id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number, maxZoom?: number, animate: boolean = true) {
            const s = BitMapGlBase._require(provider, id);
            s.map.fitBounds([[swLng, swLat], [neLng, neLat]], { padding: paddingPx ?? 48, maxZoom: maxZoom ?? 18, animate });
        }

        public static fitBoundsToMarkers(provider: string, id: string, paddingPx: number, maxZoom?: number, animate: boolean = true) {
            const s = BitMapGlBase._require(provider, id);
            const ids = Object.keys(s.markers);
            if (ids.length === 0) return;
            const b = new s.gl.LngLatBounds();
            for (const k of ids) b.extend(s.markers[k].marker.getLngLat());
            s.map.fitBounds(b, { padding: paddingPx ?? 48, maxZoom: maxZoom ?? 18, animate });
        }

        public static addMarker(provider: string, id: string, markerId: string, opts: any) {
            opts = BitMapHelpers.withDefaultIcon(id, opts);
            const s = BitMapGlBase._require(provider, id);
            // A marker already on the map is updated where it stands rather than replaced: a new one would come
            // with a new element, which takes the keyboard focus, an open popup or tooltip, and the second click
            // of a double-click with the old one.
            const existing = s.markers[markerId];
            if (existing) {
                BitMapGlBase._updateMarker(s, existing, opts);
                return;
            }
            const gl = s.gl;

            const el = document.createElement('div');
            BitMapGlBase._applyIcon(el, opts);
            el.style.backgroundSize = 'contain';
            el.style.cursor = 'pointer';
            const marker = new gl.Marker({ element: el, draggable: !!opts.draggable, offset: BitMapGlBase._iconOffset(opts) })
                .setLngLat([opts.lng, opts.lat]).addTo(s.map);
            const row: GlMarker = { marker, opts };

            const element = marker.getElement();
            if (element) {
                // What tells a double-click the map hears apart as this marker's (_markerIdOf).
                element._bmMarkerId = markerId;
                // Labelled before the popup is set: the library makes the element a tab stop for its popup
                // unless it already is one, and takes that tabindex away again as the popup goes.
                BitMapGlBase._applyLabel(element, opts, null);
            }
            BitMapGlBase._syncPopup(s, row, null);
            if (opts.opacity != null && opts.opacity !== 1) BitMapGlBase._setOpacity(marker, opts.opacity);

            // Read s.dotnetObj at dispatch time rather than capturing it: dispose() nulls
            // the field, and a stale capture would invoke a released .NET reference.
            // A marker's double-click is told apart by the map's own dblclick listener (see init).
            element?.addEventListener('click', (ev: MouseEvent) => {
                // Kept from the map so a marker click is not also a map click - but the library opens
                // a marker's popup from the map's click, so the popup is toggled here instead.
                ev.stopPropagation();
                if (s.isDisposed) return;
                // A double-click on a bubble is one click of it: the first of the pair has expanded it already.
                if (ev.detail > 1 && BitMapCluster.isClusterId(markerId)) return;
                s.dotnetObj?.invokeMethodAsync('OnMarkerClick', markerId);
                if (marker.getPopup()) marker.togglePopup();
            });
            // These read the row at dispatch time as well: an update can make the marker focusable, give it a
            // tooltip or make that tooltip permanent, and none of them builds the element again.
            element?.addEventListener('keydown', (ev: KeyboardEvent) => {
                if (row.opts.focusable === false) return;
                if (ev.key !== 'Enter' && ev.key !== ' ' && ev.key !== 'Spacebar') return;
                ev.preventDefault();
                if (s.isDisposed) return;
                s.dotnetObj?.invokeMethodAsync('OnMarkerClick', markerId);
                if (typeof marker.togglePopup === 'function' && marker.getPopup()) marker.togglePopup();
            });
            // The tooltip opens on focus as well as on hover: the marker is a keyboard tab stop, and a
            // label a keyboard user cannot reach is not much of a label.
            const show = () => { if (!row.opts.tooltipPermanent) BitMapGlBase._showTooltip(s, row); };
            const hide = () => { if (!row.opts.tooltipPermanent) BitMapGlBase._hideTooltip(row); };
            element?.addEventListener('mouseenter', show);
            element?.addEventListener('mouseleave', hide);
            element?.addEventListener('focus', show);
            element?.addEventListener('blur', hide);
            // Only a draggable marker raises it, and an update can make one draggable.
            marker.on('dragend', () => {
                if (s.isDisposed) return;
                const p = marker.getLngLat();
                s.dotnetObj?.invokeMethodAsync('OnMarkerDragEnd', markerId, { lat: p.lat, lng: p.lng });
            });

            BitMapGlBase._syncTooltip(s, row, null);
            s.markers[markerId] = row;
        }

        public static removeMarker(provider: string, id: string, markerId: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            const row = s.markers[markerId];
            if (row) { BitMapGlBase._destroyMarker(row); delete s.markers[markerId]; }
        }

        public static clearMarkers(provider: string, id: string) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            for (const key in s.markers) BitMapGlBase._destroyMarker(s.markers[key]);
            s.markers = {};
        }

        public static syncMarkers(provider: string, id: string, markerIds: string[], markers: any[]) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            BitMapHelpers.syncMarkers(s.markers, markerIds, markers,
                markerId => BitMapGlBase.removeMarker(provider, id, markerId),
                (markerId, opts) => BitMapGlBase.addMarker(provider, id, markerId, opts));
        }

        /** Makes a marker's double-click the marker's (OnMarkerDoubleClick) or leaves it the map's. */
        public static setMarkerDoubleClick(provider: string, id: string, enabled: boolean) {
            const s = BitMapGlBase._store(provider)[id];
            if (s) s.markerDoubleClick = enabled;
        }

        public static setMarkerPosition(provider: string, id: string, markerId: string, lat: number, lng: number) {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return;
            const row = s.markers[markerId];
            if (!row) return;
            row.marker.setLngLat([lng, lat]);
            // A permanent tooltip is positioned independently of its marker, so it has to follow.
            if (row.tooltip) try { row.tooltip.setLngLat([lng, lat]); } catch { /* ignore */ }
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

        /** Closes whichever marker popup is open. Returns whether there was one. */
        public static closeMarkerPopup(provider: string, id: string): boolean {
            const s = BitMapGlBase._store(provider)[id];
            if (!s) return false;
            for (const key in s.markers) {
                const popup = s.markers[key].marker.getPopup();
                if (popup?.isOpen?.()) {
                    popup.remove();
                    return true;
                }
            }
            return false;
        }

        public static addPolyline(provider: string, id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
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
                layout: BitMapGlBase._lineLayout(style),
                paint: BitMapGlBase._linePaint(style),
            });
            s.vectorCatalog[layerId] = { sourceId, layerIds: [lineId], handlers: [] };
            const h = BitMapGlBase._wireVectorClick(s, lineId, layerId, 'polyline');
            if (h) s.vectorCatalog[layerId].handlers.push(h);
        }

        public static addPolygon(provider: string, id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
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
            style = BitMapHelpers.resolvePathStyle(id, style);
            const s = BitMapGlBase._require(provider, id);
            BitMapGlBase._removeVector(s, layerId);
            const ring = BitMapHelpers.circleRingLngLat(lat, lng, radiusMeters);
            BitMapGlBase._addPolygonLayer(s, id, layerId, ring, style, 'circle');
        }

        public static addRectangle(provider: string, id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
            const s = BitMapGlBase._require(provider, id);
            BitMapGlBase._removeVector(s, layerId);
            const ring: [number, number][] = [
                [swLng, swLat], [neLng, swLat], [neLng, neLat], [swLng, neLat], [swLng, swLat],
            ];
            BitMapGlBase._addPolygonLayer(s, id, layerId, ring, style, 'rectangle');
        }

        public static addGeoJson(provider: string, id: string, layerId: string, geoJsonString: string, style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
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
            const handlers: { layerId: string, handler: any }[] = [];
            const handler = (e: any) => {
                if (s.isDisposed || !e.features?.[0]) return;
                s.dotnetObj?.invokeMethodAsync('OnGeoJsonFeatureClick', layerId, e.features[0].properties || {});
            };
            s.map.on('click', fillId, handler);
            s.map.on('click', lineId, handler);
            s.map.on('click', circleId, handler);
            handlers.push({ layerId: fillId, handler });
            handlers.push({ layerId: lineId, handler });
            handlers.push({ layerId: circleId, handler });
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
            // A GL raster source takes a list of tile URLs, so an {s} placeholder becomes one
            // entry per subdomain and the sharding is preserved.
            const urls = BitMapHelpers.expandSubdomains(opts.urlTemplate || '', opts.subdomains);
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
                type: 'raster', tiles: urls, tileSize: 256,
                attribution: opts.attribution || '',
                minzoom: opts.minZoom ?? 0,
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

        /**
         * Removes a GL marker along with the popup bound to it. `Marker.remove()` only
         * detaches the marker element - a popup that is currently open stays parented to
         * the map container, so clearing markers would leave orphaned popups behind.
         */
        private static _destroyMarker(row: { marker: any, tooltip?: any }) {
            try {
                const popup = typeof row.marker.getPopup === 'function' ? row.marker.getPopup() : null;
                if (popup) try { popup.remove(); } catch { /* ignore */ }
            } catch { /* ignore */ }
            // The tooltip is a second popup owned by this marker; it is parented to the map, so
            // removing the marker alone would strand it.
            if (row.tooltip) try { row.tooltip.remove(); } catch { /* ignore */ }
            try { row.marker.remove(); } catch { /* ignore */ }
        }

        /** The id of the marker whose element holds a DOM node - the target of a map event - or null. */
        private static _markerIdOf(s: GlState, node: any): string | null {
            const container = s.map.getCanvasContainer();
            for (; node && node !== container; node = node.parentNode) {
                const markerId = node._bmMarkerId;
                if (typeof markerId === 'string' && s.markers[markerId]?.marker.getElement() === node) return markerId;
            }
            return null;
        }

        /**
         * Brings a marker on the map in line with a new payload without replacing it (see addMarker), touching only
         * what changed. The element is the marker's own <div>, so a new icon is drawn into it.
         */
        private static _updateMarker(s: GlState, row: GlMarker, opts: any) {
            const was = row.opts;
            row.opts = opts;
            const changed = (...fields: string[]) => BitMapHelpers.markerChanged(was, opts, ...fields);
            const marker = row.marker;
            const element = marker.getElement();

            // Measured against where the marker is rather than the last payload: a drag or setMarkerPosition moves a
            // marker without one.
            const at = marker.getLngLat();
            if (at.lat !== opts.lat || at.lng !== opts.lng) {
                marker.setLngLat([opts.lng, opts.lat]);
                // A permanent tooltip is positioned independently of its marker, so it has to follow.
                if (row.tooltip) try { row.tooltip.setLngLat([opts.lng, opts.lat]); } catch { /* ignore */ }
            }
            const iconChanged = changed('iconUrl', 'iconWidth', 'iconHeight', 'iconAnchorX', 'iconAnchorY');
            if (iconChanged) {
                if (element) BitMapGlBase._applyIcon(element, opts);
                marker.setOffset?.(BitMapGlBase._iconOffset(opts));
            }
            if (element && changed('title', 'alt', 'focusable')) BitMapGlBase._applyLabel(element, opts, was);
            if (changed('opacity')) BitMapGlBase._setOpacity(marker, opts.opacity ?? 1);
            if (changed('draggable')) marker.setDraggable?.(!!opts.draggable);
            // The popup and the tooltip point from the edge of the icon, so a new icon moves them too.
            if (iconChanged || changed('popupHtml', 'popupText')) BitMapGlBase._syncPopup(s, row, was);
            if (iconChanged || changed('tooltipHtml', 'tooltipText', 'tooltipPermanent', 'tooltipDirection')) BitMapGlBase._syncTooltip(s, row, was);
        }

        /** Draws a marker's icon into its element. Every marker has one by now - its own, or the default pin withDefaultIcon drew. */
        private static _applyIcon(element: HTMLElement, opts: any) {
            element.style.width = `${opts.iconWidth || 32}px`;
            element.style.height = `${opts.iconHeight || 32}px`;
            // Quoted: an unquoted url() ends at the first parenthesis or space in the address.
            element.style.backgroundImage = `url("${String(opts.iconUrl).replace(/["\\]/g, c => '%' + c.charCodeAt(0).toString(16))}")`;
        }

        /**
         * A GL marker built from a custom element is centred on the coordinate, where
         * Leaflet anchors the image's bottom-centre. Translate the anchor into the offset
         * that reproduces it, so the same marker lands in the same place on every backend.
         */
        private static _iconOffset(opts: any): [number, number] {
            const w = opts.iconWidth || 32;
            const h = opts.iconHeight || 32;
            const [ax, ay] = BitMapHelpers.readIconAnchor(opts, w, h);
            return [Math.round(w / 2) - ax, Math.round(h / 2) - ay];
        }

        /**
         * A marker element's hover label, and what a screen reader and the keyboard make of it. A label is only
         * taken away when the previous payload had one, so a marker built without one keeps what the library gave it.
         */
        private static _applyLabel(element: HTMLElement, opts: any, was: any) {
            if (opts.title) element.setAttribute('title', opts.title);
            else if (was?.title) element.removeAttribute('title');
            // GL markers are anonymous <div>s, so without a label a screen reader reads a
            // row of identical, meaningless entries.
            const accessibleName = opts.alt || opts.title;
            if (accessibleName) element.setAttribute('aria-label', accessibleName);
            else if (was?.alt || was?.title) element.removeAttribute('aria-label');
            if (opts.focusable !== false) {
                // A GL marker is an anonymous <div>: not focusable, not activatable, and
                // unlabelled. Give it the three things a button has so a keyboard user can
                // reach it and a screen reader can name it.
                element.tabIndex = 0;
                element.setAttribute('role', 'button');
            } else {
                // Not a tab stop, whatever the library made of it for a popup.
                element.removeAttribute('tabindex');
                if (accessibleName) element.setAttribute('role', 'img');
                else element.removeAttribute('role');
            }
        }

        /**
         * Gives a marker its popup, or the one it has new content - which keeps an open popup open. PopupHtml takes
         * precedence; PopupText uses safe setText to avoid XSS. The popup's tip sits on the edge of the icon facing
         * it, however large the icon is, rather than over it.
         */
        private static _syncPopup(s: GlState, row: GlMarker, was: any) {
            const opts = row.opts, marker = row.marker;
            const popup = marker.getPopup();
            const offset = BitMapHelpers.popupOffsets(opts, opts.iconWidth || 32, opts.iconHeight || 32);
            if (popup && (opts.popupHtml || opts.popupText)) {
                if (BitMapHelpers.markerChanged(was, opts, 'popupHtml', 'popupText')) BitMapGlBase._replaceContent(popup, opts.popupHtml, opts.popupText);
                popup.setOffset?.(offset);
                return;
            }
            if (popup) {
                marker.setPopup(null);
            } else if (opts.popupHtml || opts.popupText) {
                const next = new s.gl.Popup({ offset });
                BitMapGlBase._setContent(next, opts.popupHtml, opts.popupText);
                marker.setPopup(next);
            } else {
                return;
            }
            // Setting or dropping a popup rewrites the element's tabindex and role, so the marker's own go back on.
            const element = marker.getElement();
            if (element) BitMapGlBase._applyLabel(element, opts, opts);
        }

        /**
         * Gives a GL marker the hover tooltip Leaflet has natively, built from a second Popup that is added on hover
         * and removed on leave (see addMarker), or the one it has new content. Its anchor and offset are fixed as it
         * is built, so a change to those builds it again - and opens it again while the marker is hovered or focused.
         */
        private static _syncTooltip(s: GlState, row: GlMarker, was: any) {
            const opts = row.opts;
            const hasContent = !!(opts.tooltipHtml || opts.tooltipText);
            if (row.tooltip && hasContent && !BitMapHelpers.markerChanged(was, opts,
                'tooltipPermanent', 'tooltipDirection', 'iconWidth', 'iconHeight', 'iconAnchorX', 'iconAnchorY')) {
                BitMapGlBase._setContent(row.tooltip, opts.tooltipHtml, opts.tooltipText);
                return;
            }
            BitMapGlBase._hideTooltip(row);
            row.tooltip = hasContent ? BitMapGlBase._createTooltip(s, opts) : undefined;
            if (opts.tooltipPermanent || BitMapHelpers.isEngaged(row.marker.getElement())) BitMapGlBase._showTooltip(s, row);
        }

        private static _createTooltip(s: GlState, opts: any): any {
            // A GL anchor names the side of the popup that touches the point, the opposite of the side of the
            // marker TooltipPlacement names: a tooltip above the marker is one anchored at its bottom.
            const anchors: { [direction: string]: string } = { top: 'bottom', bottom: 'top', left: 'right', right: 'left', center: 'center' };
            const tooltip = new s.gl.Popup({
                closeButton: false,
                closeOnClick: false,
                focusAfterOpen: false,
                // Clear of the icon on whichever side the tooltip opens, not a fixed radius around the
                // coordinate - which is a pin's tip, so the tooltip would cover the pin.
                offset: BitMapHelpers.popupOffsets(opts, opts.iconWidth || 32, opts.iconHeight || 32),
                className: 'bit-map-gl-tooltip',
                // 'auto' is spelled as "let the library decide", which is the absent option.
                anchor: anchors[opts.tooltipDirection],
            });
            BitMapGlBase._setContent(tooltip, opts.tooltipHtml, opts.tooltipText);
            return tooltip;
        }

        private static _showTooltip(s: GlState, row: GlMarker) {
            if (s.isDisposed || !row.tooltip) return;
            try { row.tooltip.setLngLat(row.marker.getLngLat()).addTo(s.map); } catch { /* ignore */ }
        }

        private static _hideTooltip(row: GlMarker) {
            if (row.tooltip) try { row.tooltip.remove(); } catch { /* ignore */ }
        }

        /** HTML takes precedence; text goes through setText, which is safe from XSS. */
        private static _setContent(popup: any, html: any, text: any) {
            if (html) popup.setHTML(String(html));
            else popup.setText(String(text));
        }

        /**
         * New content for a popup that may be open. The library moves the focus into an open popup whenever its
         * content is set, as it does when one opens - which an update must not do, as it would take the focus off
         * the marker that has it. focusAfterOpen is what it checks, so it is off for the duration.
         */
        private static _replaceContent(popup: any, html: any, text: any) {
            const options = popup.options;
            if (!options) {
                BitMapGlBase._setContent(popup, html, text);
                return;
            }
            // The library's defaults sit on the prototype of its options, so an option that was never set is
            // deleted again rather than set to what it read as.
            const own = Object.prototype.hasOwnProperty.call(options, 'focusAfterOpen');
            const focusAfterOpen = options.focusAfterOpen;
            options.focusAfterOpen = false;
            try {
                BitMapGlBase._setContent(popup, html, text);
            } finally {
                if (own) options.focusAfterOpen = focusAfterOpen;
                else delete options.focusAfterOpen;
            }
        }

        private static _setOpacity(marker: any, opacity: number) {
            // setOpacity exists on MapLibre 4+/Mapbox 3+; fall back to the element for older builds.
            if (typeof marker.setOpacity === 'function') marker.setOpacity(String(opacity));
            else {
                const element = marker.getElement();
                if (element) element.style.opacity = String(opacity);
            }
        }

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

        /** line-cap / line-join are layout properties in the GL style spec, not paint ones. */
        private static _lineLayout(style: any) {
            return {
                'line-join': style?.lineJoin ?? 'round',
                'line-cap': style?.lineCap ?? 'round',
            };
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
            const handler = (e: any) => {
                if (s.isDisposed) return;
                s.dotnetObj?.invokeMethodAsync('OnVectorClick', layerId, kind, { lat: e.lngLat.lat, lng: e.lngLat.lng });
            };
            s.map.on('click', glLayerId, handler);
            return { layerId: glLayerId, handler };
        }
    }
}
