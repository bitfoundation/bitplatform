namespace BitBlazorUI {

    type LeafletTileOptions = {
        tileUrl: string;
        tileMaxZoom: number;
        tileAttribution: string;
        tileOpacity: number;
    };

    type LeafletState = {
        L: any;
        map: any;
        dotnetObj: DotNetObject | null | undefined;
        markers: { [id: string]: any };
        layers: { [id: string]: any };
        tileOverlays: { [id: string]: any };
        baseTileLayer: any;
        scaleControl: any;
        _tileOptions: LeafletTileOptions;
        _scaleControlImperial: boolean;
        _scaleControlEnabled: boolean;
        _zoomControl: any;
        _zoomControlEnabled: boolean;
        _viewTimer: any;
        /** Whether a marker's double-click is the marker's: only while OnMarkerDoubleClick has a handler. */
        markerDoubleClick: boolean;
        isDisposed: boolean;
    };

    let leafletDefaultIconPatched = false;

    function patchLeafletDefaultIcon(L: any) {
        if (leafletDefaultIconPatched) return;
        leafletDefaultIconPatched = true;
        const base = "_content/Bit.BlazorUI.Extras/leaflet/images/";
        try {
            delete L.Icon.Default.prototype._getIconUrl;
            L.Icon.Default.mergeOptions({
                iconRetinaUrl: `${base}marker-icon-2x.png`,
                iconUrl: `${base}marker-icon.png`,
                shadowUrl: `${base}marker-shadow.png`,
            });
        } catch { /* ignore */ }
    }

    // bit-trim: owners BitLeafletMapProvider
    // Called through the JsObjectName of its provider, which BitMap puts together into the identifier it calls (see
    // BitMapJsRuntimeExtensions), so no interop identifier names this script: the provider type an app passes keeps it.
    export class BitMapLeaflet {
        /**
         * Leaflet pans and zooms from the keyboard while its container - the canvas - has the focus, so
         * BitMapChrome leaves the keys to it. Its handler listens on the document and does not check
         * whether a key was already handled, so the chrome's own would make every press count twice.
         */
        public static readonly handlesCanvasKeyboard = true;

        private static _maps: { [id: string]: LeafletState } = {};

        public static async init(id: string, canvasId: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            element = await BitMapHelpers.resolveMapCanvas(canvasId, element);

            const L = (globalThis as any).L;
            if (!L) throw new Error("Leaflet is not loaded.");

            patchLeafletDefaultIcon(L);
            const o = options || {};
            const center: [number, number] = [o.center?.lat ?? 51.505, o.center?.lng ?? -0.09];
            const zoom: number = o.zoom ?? 13;

            const zoomControlEnabled = o.zoomControl !== false;
            const map = BitMapLeaflet._keepTabIndex(element, () => L.map(element, {
                center, zoom,
                minZoom: o.minZoom ?? undefined,
                maxZoom: o.maxZoom ?? undefined,
                zoomControl: zoomControlEnabled,
                attributionControl: o.attributionControl !== false,
                scrollWheelZoom: o.scrollWheelZoom !== false,
                doubleClickZoom: o.doubleClickZoom !== false,
                boxZoom: o.boxZoom !== false,
                dragging: o.dragging !== false,
                keyboard: o.keyboardNavigation !== false,
                // Spread last so an escape-hatch entry wins over what the component models.
                ...(o.additionalOptions || {}),
            }));

            // The default tileUrl is OpenStreetMap, which contractually requires the
            // standard attribution. When the caller leaves tileUrl unset (so we serve
            // OSM tiles) we substitute the OSM attribution unless the caller supplied
            // a non-empty replacement. For any other tileUrl we honor the caller's
            // tileAttribution exactly (including an empty string) so custom basemaps
            // don't end up incorrectly crediting OSM.
            const defaultOsmTileUrl = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";
            const defaultOsmAttribution = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';
            const effectiveTileUrl: string = o.tileUrl || defaultOsmTileUrl;
            const callerAttribution = (typeof o.tileAttribution === 'string' && o.tileAttribution.length > 0)
                ? o.tileAttribution
                : null;
            const tileOptions: LeafletTileOptions = {
                tileUrl: effectiveTileUrl,
                tileMaxZoom: o.tileMaxZoom ?? 19,
                tileAttribution: callerAttribution
                    ?? (effectiveTileUrl === defaultOsmTileUrl ? defaultOsmAttribution : (typeof o.tileAttribution === 'string' ? o.tileAttribution : '')),
                tileOpacity: o.tileOpacity ?? 1,
            };
            const baseTileLayer = L.tileLayer(tileOptions.tileUrl, {
                maxZoom: tileOptions.tileMaxZoom,
                attribution: tileOptions.tileAttribution,
                opacity: tileOptions.tileOpacity,
            }).addTo(map);

            const state: LeafletState = {
                L, map, dotnetObj,
                markers: {}, layers: {}, tileOverlays: {},
                baseTileLayer, scaleControl: null,
                _tileOptions: tileOptions,
                _scaleControlImperial: !!o.scaleControlImperial,
                _scaleControlEnabled: !!o.showScaleControl,
                _zoomControl: (map as any).zoomControl ?? null,
                _zoomControlEnabled: zoomControlEnabled,
                _viewTimer: null,
                markerDoubleClick: false,
                isDisposed: false,
            };

            if (o.maxBounds !== undefined) BitMapLeaflet._applyMaxBounds(state, o.maxBounds);
            BitMapLeaflet._ensureScaleControl(state, state._scaleControlEnabled, state._scaleControlImperial);

            if (dotnetObj) {
                // Every handler reads state.dotnetObj rather than closing over the
                // `dotnetObj` argument: dispose() nulls the field, so a listener that
                // is still in flight (or a debounced view notification that has already
                // been scheduled) can bail out instead of invoking a released .NET
                // reference - which surfaces as an unhandled JS error in the browser.
                map.on('click', (e: any) => {
                    if (state.isDisposed || !state.dotnetObj) return;
                    state.dotnetObj.invokeMethodAsync('OnClick', { lat: e.latlng.lat, lng: e.latlng.lng });
                });
                map.on('dblclick', (e: any) => {
                    if (state.isDisposed || !state.dotnetObj) return;
                    state.dotnetObj.invokeMethodAsync('OnDoubleClick', { lat: e.latlng.lat, lng: e.latlng.lng });
                });
                map.on('contextmenu', (e: any) => {
                    if (o.suppressBrowserContextMenu) e.originalEvent?.preventDefault?.();
                    if (state.isDisposed || !state.dotnetObj) return;
                    state.dotnetObj.invokeMethodAsync('OnContextMenu', { lat: e.latlng.lat, lng: e.latlng.lng });
                });
                // Coalesce move/zoom notifications on a short timer. A pan fires
                // `moveend` and `zoomend` back to back, and each notification is a
                // separate interop round-trip (a SignalR message under Blazor Server),
                // so debouncing keeps a drag from flooding the circuit. Matches the
                // 80ms window the ArcGIS / Azure Maps / Cesium providers already use.
                const notify = () => {
                    if (state.isDisposed) return;
                    clearTimeout(state._viewTimer);
                    state._viewTimer = setTimeout(() => {
                        if (state.isDisposed || !state.dotnetObj) return;
                        state.dotnetObj.invokeMethodAsync('OnViewChanged', BitMapLeaflet._readView(state.map));
                    }, BitMapHelpers.viewNotifyDebounceMs);
                };
                map.on('moveend', notify);
                map.on('zoomend', notify);
            }

            BitMapLeaflet._maps[id] = state;
            queueMicrotask(() => map.invalidateSize());
        }

        public static sync(id: string, options: any) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            const L = s.L;
            const o = options || {};

            // Update view only when center or zoom is explicitly provided
            if (o.center && o.zoom != null) {
                s.map.setView([o.center.lat, o.center.lng], o.zoom, { animate: false });
            } else if (o.center) {
                s.map.setView([o.center.lat, o.center.lng], s.map.getZoom(), { animate: false });
            } else if (o.zoom != null) {
                s.map.setZoom(o.zoom, { animate: false });
            }

            // Only recreate the base tile layer when tile options actually changed.
            // Mirror init's URL-keyed attribution rule so an attribution from a previous
            // OSM-default base layer doesn't leak onto a newly-set custom tileUrl.
            const defaultOsmTileUrl = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";
            const defaultOsmAttribution = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';
            const nextTileUrl = o.tileUrl ?? s._tileOptions.tileUrl;
            const callerAttrProvided = typeof o.tileAttribution === 'string';
            const callerAttrNonEmpty = callerAttrProvided && (o.tileAttribution as string).length > 0;
            const urlChanged = nextTileUrl !== s._tileOptions.tileUrl;
            let nextTileAttribution: string;
            if (callerAttrNonEmpty) {
                nextTileAttribution = o.tileAttribution as string;
            } else if (callerAttrProvided || urlChanged) {
                // Caller explicitly cleared attribution, or the tile URL changed without a
                // matching attribution update - recompute strictly from the URL.
                nextTileAttribution = nextTileUrl === defaultOsmTileUrl
                    ? defaultOsmAttribution
                    : (callerAttrProvided ? (o.tileAttribution as string) : '');
            } else {
                nextTileAttribution = s._tileOptions.tileAttribution;
            }
            const next: LeafletTileOptions = {
                tileUrl: nextTileUrl,
                tileMaxZoom: o.tileMaxZoom ?? s._tileOptions.tileMaxZoom,
                tileAttribution: nextTileAttribution,
                tileOpacity: o.tileOpacity ?? s._tileOptions.tileOpacity,
            };
            const tileChanged =
                next.tileUrl !== s._tileOptions.tileUrl ||
                next.tileMaxZoom !== s._tileOptions.tileMaxZoom ||
                next.tileAttribution !== s._tileOptions.tileAttribution ||
                next.tileOpacity !== s._tileOptions.tileOpacity;
            if (tileChanged) {
                if (s.baseTileLayer) s.map.removeLayer(s.baseTileLayer);
                s.baseTileLayer = L.tileLayer(next.tileUrl, {
                    maxZoom: next.tileMaxZoom,
                    attribution: next.tileAttribution,
                    opacity: next.tileOpacity,
                }).addTo(s.map);
                s._tileOptions = next;
            }
            if (s.baseTileLayer) {
                try { s.baseTileLayer.bringToBack(); } catch { /* ignore */ }
            }

            for (const key in s.tileOverlays) {
                try { s.tileOverlays[key].bringToFront(); } catch { /* ignore */ }
            }

            // Reapply interaction toggles when explicitly provided
            if (o.scrollWheelZoom !== undefined) o.scrollWheelZoom ? s.map.scrollWheelZoom.enable() : s.map.scrollWheelZoom.disable();
            if (o.doubleClickZoom !== undefined) o.doubleClickZoom ? s.map.doubleClickZoom.enable() : s.map.doubleClickZoom.disable();
            if (o.boxZoom !== undefined) o.boxZoom ? s.map.boxZoom.enable() : s.map.boxZoom.disable();
            if (o.dragging !== undefined) o.dragging ? s.map.dragging.enable() : s.map.dragging.disable();
            if (o.keyboardNavigation !== undefined) {
                BitMapLeaflet._keepTabIndex(s.map.getContainer(), () => o.keyboardNavigation ? s.map.keyboard.enable() : s.map.keyboard.disable());
            }

            // Zoom limits are constructor options in Leaflet, so a provider that changes
            // MinZoom/MaxZoom after init has to be pushed through the setters or the new
            // bounds are silently ignored. Apply min before max so an ordered pair never
            // passes through a transient state where min > max.
            const hasMinZoom = Object.prototype.hasOwnProperty.call(o, 'minZoom');
            const hasMaxZoom = Object.prototype.hasOwnProperty.call(o, 'maxZoom');
            if (hasMinZoom) {
                try { s.map.setMinZoom(o.minZoom ?? 0); } catch { /* ignore */ }
            }
            if (hasMaxZoom) {
                try { s.map.setMaxZoom(o.maxZoom ?? Infinity); } catch { /* ignore */ }
            }

            // zoomControl / attributionControl are also constructor-only in Leaflet.
            // Add or remove the control instances so toggling them on a live map works.
            if (Object.prototype.hasOwnProperty.call(o, 'zoomControl')) {
                BitMapLeaflet._ensureZoomControl(s, o.zoomControl !== false);
            }
            if (Object.prototype.hasOwnProperty.call(o, 'attributionControl')) {
                BitMapLeaflet._ensureAttributionControl(s, o.attributionControl !== false);
            }

            // Only touch maxBounds when explicitly provided to avoid clearing existing settings on partial updates.
            if (o.maxBounds !== undefined) BitMapLeaflet._applyMaxBounds(s, o.maxBounds);
            // Only touch scale control when caller actually supplied either flag; preserve existing state otherwise.
            const hasShow = Object.prototype.hasOwnProperty.call(o, 'showScaleControl');
            const hasImperial = Object.prototype.hasOwnProperty.call(o, 'scaleControlImperial');
            if (hasShow || hasImperial) {
                if (hasShow) s._scaleControlEnabled = !!o.showScaleControl;
                if (hasImperial) s._scaleControlImperial = !!o.scaleControlImperial;
                BitMapLeaflet._ensureScaleControl(s, s._scaleControlEnabled, s._scaleControlImperial);
            }
        }

        public static dispose(id: string) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            // Mark disposed and drop the .NET handle before tearing the map down so a
            // debounced view notification that is already queued short-circuits instead
            // of invoking a released DotNetObjectReference.
            s.isDisposed = true;
            s.dotnetObj = null;
            if (s._viewTimer) { clearTimeout(s._viewTimer); s._viewTimer = null; }
            try {
                for (const key in s.tileOverlays) s.map.removeLayer(s.tileOverlays[key]);
                if (s.scaleControl) s.map.removeControl(s.scaleControl);
                s.map.remove();
            } catch { /* ignore */ }
            delete BitMapLeaflet._maps[id];
        }

        public static invalidateSize(id: string) {
            const s = BitMapLeaflet._maps[id];
            if (s) s.map.invalidateSize({ animate: false });
        }

        public static getView(id: string) {
            return BitMapLeaflet._readView(BitMapLeaflet._require(id).map);
        }

        public static setView(id: string, lat: number, lng: number, zoom: number | null, animate: boolean) {
            const s = BitMapLeaflet._require(id);
            s.map.setView([lat, lng], zoom ?? s.map.getZoom(), { animate: animate !== false });
        }

        public static flyTo(id: string, lat: number, lng: number, zoom: number | null) {
            const s = BitMapLeaflet._require(id);
            s.map.flyTo([lat, lng], zoom ?? s.map.getZoom(), { duration: 1.2 });
        }

        /**
         * Projects a geographic coordinate to a pixel offset inside the map container.
         *
         * This is what lets BitMap anchor its own DOM - a Blazor-rendered popup, say - to a place
         * on the map without handing that DOM to the mapping library, which would take it out of
         * Blazor's hands. Returns null when the coordinate is not currently on screen.
         */
        public static project(id: string, lat: number, lng: number): { x: number, y: number } | null {
            const s = BitMapLeaflet._maps[id];
            if (!s) return null;
            try {
                const point = s.map.latLngToContainerPoint([lat, lng]);
                return { x: point.x, y: point.y };
            } catch {
                return null;
            }
        }

        public static zoomBy(id: string, delta: number, animate: boolean) {
            const s = BitMapLeaflet._require(id);
            s.map.setZoom(s.map.getZoom() + delta, { animate: animate !== false });
        }

        public static panBy(id: string, dx: number, dy: number, animate: boolean) {
            const s = BitMapLeaflet._require(id);
            s.map.panBy([dx, dy], { animate: animate !== false });
        }

        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number, maxZoom?: number, animate: boolean = true) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const pad = paddingPx ?? 48;
            s.map.fitBounds(L.latLngBounds(L.latLng(swLat, swLng), L.latLng(neLat, neLng)),
                { padding: [pad, pad], maxZoom: maxZoom ?? 18, animate });
        }

        public static fitBoundsToMarkers(id: string, paddingPx: number, maxZoom?: number, animate: boolean = true) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const layers = Object.values(s.markers);
            if (layers.length === 0) return;
            const b = L.featureGroup(layers).getBounds();
            if (!b.isValid()) return;
            const pad = paddingPx ?? 48;
            s.map.fitBounds(b, { padding: [pad, pad], maxZoom: maxZoom ?? 18, animate });
        }

        public static addMarker(id: string, markerId: string, opts: any) {
            opts = BitMapHelpers.withDefaultIcon(id, opts);
            const s = BitMapLeaflet._require(id);
            // A marker already on the map is updated where it stands rather than replaced: a new one would come
            // with a new element, which takes the keyboard focus, an open popup or tooltip, and the second click
            // of a double-click with the old one.
            const existing = s.markers[markerId];
            if (existing) {
                // Leaflet pans the map to an open popup as it moves or its content is set (autoPan). An update does
                // not: that is spent as the popup opens, and doing it on every update would pull the map back to the
                // popup each time, fighting a user who has dragged elsewhere.
                BitMapHelpers.withOption(existing.getPopup()?.options, 'autoPan', false,
                    () => BitMapLeaflet._updateMarker(s, existing, opts));
                return;
            }
            const L = s.L;
            const icon = BitMapLeaflet._icon(L, opts);
            const markerOpts: any = {
                draggable: !!opts.draggable,
                title: opts.title || undefined,
                // Leaflet renders a marker as an <img>, so `alt` is its accessible name.
                // Falling back to title keeps a marker that only set a hover label from
                // announcing as an unlabelled image.
                alt: opts.alt || opts.title || undefined,
                keyboard: opts.focusable !== false,
                opacity: opts.opacity ?? 1,
                riseOnHover: !!opts.riseOnHover,
                zIndexOffset: opts.zIndexOffset ?? 0,
            };
            if (icon) markerOpts.icon = icon;

            const m = L.marker([opts.lat, opts.lng], markerOpts);
            // What the marker was last built from, which an update compares against (_updateMarker).
            m._bmOpts = opts;
            // Leaflet opens a hover tooltip on focus through listeners it adds to the element each time one is bound,
            // and never takes away: a tooltip an update binds again would leave the old ones behind, to open a tooltip
            // that is gone and close one that has turned permanent. The marker listens once instead (below), for the
            // tooltip it has at the time.
            m._addFocusListeners = () => { /* see the focus and blur listeners below */ };
            BitMapLeaflet._bindPopup(m, opts);
            BitMapLeaflet._bindTooltip(m, opts, null);
            m.addTo(s.map);

            // Read s.dotnetObj at dispatch time; capturing it here would keep invoking a
            // DotNetObjectReference that dispose() has already released.
            const isClusterBubble = BitMapCluster.isClusterId(markerId);
            m.on('click', (e: any) => {
                if (s.isDisposed) return;
                // A double-click on a bubble is one click of it: the first of the pair has expanded it already.
                if (isClusterBubble && e?.originalEvent?.detail > 1) return;
                s.dotnetObj?.invokeMethodAsync('OnMarkerClick', markerId);
            });
            // Listening is what keeps the double-click off the map: it makes the marker a target of the event, and a
            // marker's mouse events stop at the marker (bubblingMouseEvents is false for markers) instead of going on to
            // the map. Without it the map is the only target, so it raises OnDoubleClick and zooms in - which a marker
            // leaves it while OnMarkerDoubleClick has no handler, and a bubble never does.
            if (isClusterBubble) {
                m.on('dblclick', () => { /* the click of the pair that expands the bubble has been reported */ });
            } else {
                m._bmDoubleClick = () => {
                    if (s.isDisposed) return;
                    s.dotnetObj?.invokeMethodAsync('OnMarkerDoubleClick', markerId);
                };
                if (s.markerDoubleClick) m.on('dblclick', m._bmDoubleClick);
            }

            // Leaflet gives a marker a tabindex but renders it as an <img>, which no browser
            // activates on Enter or Space - so a keyboard user can focus the marker and then
            // do nothing with it. Synthesize the activation the element should already have.
            // Listened for whether or not the marker is focusable now, since an update can make it so.
            const element = m.getElement?.();
            if (element) {
                if (opts.focusable !== false) element.setAttribute('role', 'button');
                element.addEventListener('keydown', (ev: KeyboardEvent) => {
                    if (m._bmOpts.focusable === false) return;
                    if (ev.key !== 'Enter' && ev.key !== ' ' && ev.key !== 'Spacebar') return;
                    ev.preventDefault();
                    m.fire('click');
                    if (m.getPopup()) m.openPopup();
                });
                // The hover tooltip opens on focus too, as Leaflet's own listeners would have it (see above).
                const hoverTooltip = () => !!m.getTooltip() && !m.getTooltip().options.permanent;
                element.addEventListener('focus', () => { if (hoverTooltip()) m.openTooltip(); });
                element.addEventListener('blur', () => { if (hoverTooltip()) m.closeTooltip(); });
            }
            // Only a draggable marker raises it, and an update can make one draggable.
            m.on('dragend', (e: any) => {
                if (s.isDisposed) return;
                const p = e.target.getLatLng();
                s.dotnetObj?.invokeMethodAsync('OnMarkerDragEnd', markerId, { lat: p.lat, lng: p.lng });
            });
            s.markers[markerId] = m;
        }

        public static removeMarker(id: string, markerId: string) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            const m = s.markers[markerId];
            if (m) { s.map.removeLayer(m); delete s.markers[markerId]; }
        }

        public static clearMarkers(id: string) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            for (const key in s.markers) s.map.removeLayer(s.markers[key]);
            s.markers = {};
        }

        public static syncMarkers(id: string, markerIds: string[], markers: any[]) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            BitMapHelpers.syncMarkers(s.markers, markerIds, markers,
                markerId => BitMapLeaflet.removeMarker(id, markerId),
                (markerId, opts) => BitMapLeaflet.addMarker(id, markerId, opts),
                ids => BitMapHelpers.orderElements(ids.map(markerId => s.markers[markerId]?.getElement?.())));
        }

        /**
         * Makes a marker's double-click the marker's (OnMarkerDoubleClick) or leaves it the map's. Listening for it is
         * what takes it from the map (see addMarker), so the markers already on the map start or stop listening.
         */
        public static setMarkerDoubleClick(id: string, enabled: boolean) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            s.markerDoubleClick = enabled;
            for (const key in s.markers) {
                const m = s.markers[key];
                if (!m._bmDoubleClick) continue;
                m.off('dblclick', m._bmDoubleClick);
                if (enabled) m.on('dblclick', m._bmDoubleClick);
            }
        }

        public static setMarkerPosition(id: string, markerId: string, lat: number, lng: number) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            const m = s.markers[markerId];
            if (m) m.setLatLng([lat, lng]);
        }

        public static openMarkerPopup(id: string, markerId: string) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            const m = s.markers[markerId];
            if (m && m.getPopup()) m.openPopup();
        }

        /** Closes whichever marker popup is open. Returns whether there was one. */
        public static closeMarkerPopup(id: string): boolean {
            const s = BitMapLeaflet._maps[id];
            if (!s) return false;
            for (const key in s.markers) {
                const m = s.markers[key];
                if (m.isPopupOpen?.()) {
                    m.closePopup();
                    return true;
                }
            }
            return false;
        }

        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const pl = L.polyline(latlngs.map(p => [p.lat, p.lng]), BitMapLeaflet._pathStyle(style, false)).addTo(s.map);
            BitMapLeaflet._wireVectorClick(s, pl, layerId, 'polyline');
            BitMapLeaflet._setLayer(s, layerId, pl);
        }

        public static addPolygon(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const poly = L.polygon(latlngs.map(p => [p.lat, p.lng]), BitMapLeaflet._pathStyle(style)).addTo(s.map);
            BitMapLeaflet._wireVectorClick(s, poly, layerId, 'polygon');
            BitMapLeaflet._setLayer(s, layerId, poly);
        }

        public static addCircle(id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const c = L.circle([lat, lng], { radius: radiusMeters, ...BitMapLeaflet._pathStyle(style) }).addTo(s.map);
            BitMapLeaflet._wireVectorClick(s, c, layerId, 'circle');
            BitMapLeaflet._setLayer(s, layerId, c);
        }

        public static addRectangle(id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const r = L.rectangle(L.latLngBounds(L.latLng(swLat, swLng), L.latLng(neLat, neLng)),
                BitMapLeaflet._pathStyle(style)).addTo(s.map);
            BitMapLeaflet._wireVectorClick(s, r, layerId, 'rectangle');
            BitMapLeaflet._setLayer(s, layerId, r);
        }

        public static addGeoJson(id: string, layerId: string, geoJsonString: string, style: any) {
            style = BitMapHelpers.resolvePathStyle(id, style);
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            let gj: any;
            try { gj = JSON.parse(geoJsonString); }
            catch { throw new Error("BitMapLeaflet.addGeoJson: invalid GeoJSON string."); }
            const layer = L.geoJSON(gj, {
                style: (feature: any) => BitMapLeaflet._pathStyle(style, !/LineString$/.test(feature?.geometry?.type ?? '')),
                // Default L.geoJSON renders Point/MultiPoint features as a vanilla
                // L.marker which ignores the path style passed above. Provide a
                // pointToLayer that wraps each point as a styled circleMarker so
                // points pick up the same color/weight/opacity as polylines and
                // polygons in the same GeoJSON document.
                pointToLayer(_feature: any, latlng: any) {
                    return L.circleMarker(latlng, BitMapLeaflet._pathStyle(style));
                },
                onEachFeature(feature: any, lyr: any) {
                    lyr.on('click', (e: any) => {
                        L.DomEvent.stopPropagation(e);
                        if (s.isDisposed) return;
                        s.dotnetObj?.invokeMethodAsync('OnGeoJsonFeatureClick', layerId, feature?.properties || {});
                    });
                },
            }).addTo(s.map);
            BitMapLeaflet._setLayer(s, layerId, layer);
        }

        public static removeLayer(id: string, layerId: string) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            const lyr = s.layers[layerId];
            if (lyr) { s.map.removeLayer(lyr); delete s.layers[layerId]; }
        }

        public static clearVectorLayers(id: string) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            for (const key in s.layers) s.map.removeLayer(s.layers[key]);
            s.layers = {};
        }

        public static addTileOverlay(id: string, opts: any) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const existing = s.tileOverlays[opts.id];
            if (existing) {
                s.map.removeLayer(existing);
                delete s.tileOverlays[opts.id];
            }
            const tl = L.tileLayer(opts.urlTemplate, {
                opacity: opts.opacity ?? 1,
                zIndex: opts.zIndex ?? 100,
                minZoom: opts.minZoom ?? 0,
                maxZoom: opts.maxZoom ?? 19,
                // Leaflet is the only backend that rotates through subdomains itself, so it keeps
                // the {s} placeholder rather than being handed pre-expanded URLs.
                subdomains: BitMapHelpers.readSubdomains(opts.subdomains),
                attribution: opts.attribution || "",
            });
            tl.addTo(s.map);
            s.tileOverlays[opts.id] = tl;
        }

        public static removeTileOverlay(id: string, overlayId: string) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            const tl = s.tileOverlays[overlayId];
            if (tl) { s.map.removeLayer(tl); delete s.tileOverlays[overlayId]; }
        }

        // ---- helpers ----

        private static _require(id: string): LeafletState {
            const s = BitMapLeaflet._maps[id];
            if (!s) throw new Error(`BitMapLeaflet: unknown map id '${id}'`);
            return s;
        }

        private static _setLayer(s: LeafletState, layerId: string, layer: any) {
            const existing = s.layers[layerId];
            if (existing) s.map.removeLayer(existing);
            s.layers[layerId] = layer;
        }

        /** A marker's icon, or undefined - which leaves it Leaflet's own pin - when it has no image. */
        private static _icon(L: any, opts: any): any {
            if (!opts.iconUrl) return undefined;
            const w = opts.iconWidth ?? 32;
            const h = opts.iconHeight ?? 32;
            // The point of the image that sits on the coordinate. Bottom-centre by default,
            // which is where a pin's tip is; a dot-shaped icon passes its own centre instead.
            const [ax, ay] = BitMapHelpers.readIconAnchor(opts, w, h);
            const offsets = BitMapHelpers.popupOffsets(opts, w, h);
            return L.icon({
                iconUrl: opts.iconUrl,
                iconSize: [w, h],
                iconAnchor: [ax, ay],
                popupAnchor: offsets['bottom'],
                // Beside the icon's head, as Leaflet's own pin has it - without one the tooltip
                // points at the coordinate, which is the tip of a pin, under the pin itself.
                tooltipAnchor: [offsets['left'][0] + 3, offsets['left'][1]],
            });
        }

        /**
         * Brings a marker on the map in line with a new payload without replacing it (see addMarker), touching only
         * what changed. Leaflet draws a new icon into the <img> the marker already has, so even a new icon keeps the
         * element.
         */
        private static _updateMarker(s: LeafletState, m: any, opts: any) {
            const was = m._bmOpts;
            m._bmOpts = opts;
            const changed = (...fields: string[]) => BitMapHelpers.markerChanged(was, opts, ...fields);

            // Measured against where the marker is rather than the last payload: a drag or setMarkerPosition moves a
            // marker without one.
            const at = m.getLatLng();
            if (at.lat !== opts.lat || at.lng !== opts.lng) m.setLatLng([opts.lat, opts.lng]);

            const element = m.getElement?.();
            if (changed('title', 'alt')) {
                m.options.title = opts.title || undefined;
                m.options.alt = opts.alt || opts.title || undefined;
                if (element) {
                    if (opts.title) element.title = opts.title;
                    else element.removeAttribute('title');
                    element.alt = opts.alt || opts.title || '';
                }
            }
            if (changed('focusable')) {
                const focusable = opts.focusable !== false;
                m.options.keyboard = focusable;
                if (element) {
                    if (focusable) {
                        element.tabIndex = 0;
                        element.setAttribute('role', 'button');
                    } else {
                        element.removeAttribute('tabindex');
                        element.removeAttribute('role');
                    }
                }
            }
            if (changed('riseOnHover')) {
                // Leaflet only starts listening as it sets the icon up, so the listeners are added or removed here.
                const rise = { mouseover: m._bringToFront, mouseout: m._resetZIndex };
                m.options.riseOnHover = !!opts.riseOnHover;
                if (opts.riseOnHover) {
                    m.on(rise);
                } else {
                    m.off(rise);
                    m._resetZIndex?.();
                }
            }
            if (changed('draggable')) {
                m.options.draggable = !!opts.draggable;
                if (opts.draggable) m.dragging?.enable();
                else m.dragging?.disable();
            }
            if (changed(...BitMapHelpers.markerIconFields)) {
                m.setIcon(BitMapLeaflet._icon(s.L, opts) ?? new s.L.Icon.Default());
                // An open popup or tooltip points from the edge of the icon, which may have moved.
                if (m.isPopupOpen?.()) m.getPopup().update();
                if (m.isTooltipOpen?.()) m.getTooltip().update();
            }
            if (changed('opacity')) m.setOpacity(opts.opacity ?? 1);
            if (changed('zIndexOffset')) m.setZIndexOffset(opts.zIndexOffset ?? 0);
            if (changed(...BitMapHelpers.markerPopupFields)) BitMapLeaflet._bindPopup(m, opts);
            if (changed(...BitMapHelpers.markerTooltipFields)) BitMapLeaflet._bindTooltip(m, opts, was);
        }

        /** Binds a marker's popup, or gives the one it has new content - which keeps an open popup open. */
        private static _bindPopup(m: any, opts: any) {
            const content = opts.popupHtml || BitMapLeaflet._textContent(opts.popupText);
            if (!content) {
                // Unbinding alone would leave an open popup on the map, with no marker left to close it.
                if (m.getPopup()) m.closePopup().unbindPopup();
            } else if (m.getPopup()) {
                m.setPopupContent(content);
            } else {
                m.bindPopup(content);
            }
        }

        /**
         * Binds a marker's tooltip, or gives the one it has new content. How a tooltip opens is fixed as it is bound,
         * so a change to that binds it again - and opens it again while the marker is hovered or focused, as it was.
         */
        private static _bindTooltip(m: any, opts: any, was: any) {
            const content = opts.tooltipHtml || BitMapLeaflet._textContent(opts.tooltipText);
            const tooltip = m.getTooltip();
            if (content && tooltip && !BitMapHelpers.markerChanged(was, opts, 'tooltipPermanent', 'tooltipDirection')) {
                m.setTooltipContent(content);
                return;
            }
            if (tooltip) {
                m.unbindTooltip();
                // Leaflet points the marker at a tooltip as it opens one (aria-describedby), and leaves it pointing
                // there once the tooltip is gone.
                const element = m.getElement?.();
                const describedBy = tooltip.getElement?.()?.id;
                if (describedBy && element?.getAttribute('aria-describedby') === describedBy) element.removeAttribute('aria-describedby');
            }
            if (!content) return;
            // A permanent one opens as it is bound to a marker on the map.
            m.bindTooltip(content, {
                permanent: !!opts.tooltipPermanent,
                direction: opts.tooltipDirection || 'auto',
            });
            if (!opts.tooltipPermanent && BitMapHelpers.isEngaged(m.getElement?.())) m.openTooltip();
        }

        /** Plain text as an element, so it reaches the popup or tooltip as text rather than markup. Null when empty. */
        private static _textContent(text: any): HTMLElement | null {
            if (!text) return null;
            const el = document.createElement('span');
            el.textContent = text;
            return el;
        }

        private static _wireVectorClick(s: LeafletState, layer: any, layerId: string, kind: string) {
            const L = s.L;
            layer.on('click', (e: any) => {
                L.DomEvent.stopPropagation(e);
                if (s.isDisposed) return;
                s.dotnetObj?.invokeMethodAsync('OnVectorClick', layerId, kind, { lat: e.latlng.lat, lng: e.latlng.lng });
            });
        }

        /**
         * Runs a Leaflet call that may enable its keyboard handler, which makes its container a tab stop by
         * setting a non-positive tabIndex to 0 - undoing a TabIndex of the component's, such as -1 for a
         * decorative map, that Blazor will not write again while its own value is unchanged.
         */
        private static _keepTabIndex<T>(element: HTMLElement, action: () => T): T {
            const tabIndex = element.getAttribute('tabindex');
            try {
                return action();
            } finally {
                if (tabIndex !== null && element.getAttribute('tabindex') !== tabIndex) element.setAttribute('tabindex', tabIndex);
            }
        }

        /** A shape's Leaflet options. An open line is never filled: Leaflet would fill the area its ends enclose. */
        private static _pathStyle(style: any, closed: boolean = true) {
            if (!style) return {};
            return {
                color: style.color ?? '#3388ff',
                weight: style.weight ?? 3,
                opacity: style.opacity ?? 1,
                // `fill: false` is not the same as a zero fill opacity: an invisible fill is
                // still rendered and still hit-tested, so an outline-only shape needs the flag.
                fill: closed && style.fill !== false,
                fillColor: style.fillColor ?? style.color ?? '#3388ff',
                fillOpacity: style.fillOpacity ?? 0.2,
                dashArray: style.dashArray ?? undefined,
                dashOffset: style.dashOffset ?? undefined,
                lineCap: style.lineCap ?? 'round',
                lineJoin: style.lineJoin ?? 'round',
            };
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

        private static _applyMaxBounds(s: LeafletState, mb: BitMapBounds | null | undefined) {
            const L = s.L;
            if (!mb) {
                try { s.map.setMaxBounds(null); } catch { /* ignore */ }
                return;
            }
            s.map.setMaxBounds(L.latLngBounds(
                L.latLng(mb.southWest.lat, mb.southWest.lng),
                L.latLng(mb.northEast.lat, mb.northEast.lng),
            ));
        }

        private static _ensureZoomControl(s: LeafletState, show: boolean) {
            if (s._zoomControlEnabled === show) return;
            const L = s.L;
            if (show) {
                s._zoomControl = L.control.zoom().addTo(s.map);
            } else if (s._zoomControl) {
                try { s.map.removeControl(s._zoomControl); } catch { /* ignore */ }
                s._zoomControl = null;
            }
            s._zoomControlEnabled = show;
        }

        private static _ensureAttributionControl(s: LeafletState, show: boolean) {
            const L = s.L;
            const current = s.map.attributionControl;
            if (show && !current) {
                s.map.attributionControl = L.control.attribution().addTo(s.map);
            } else if (!show && current) {
                try { s.map.removeControl(current); } catch { /* ignore */ }
                s.map.attributionControl = null;
            }
        }

        private static _ensureScaleControl(s: LeafletState, show: boolean, imperial: boolean) {
            const L = s.L;
            if (s.scaleControl) {
                s.map.removeControl(s.scaleControl);
                s.scaleControl = null;
            }
            if (show) {
                s.scaleControl = L.control.scale({ imperial, metric: true }).addTo(s.map);
            }
        }
    }
}

// bit-lazy: teardown BitMapLeaflet.dispose
