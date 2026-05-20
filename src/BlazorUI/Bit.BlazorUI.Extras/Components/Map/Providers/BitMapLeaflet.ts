namespace BitBlazorUI {

    type LeafletState = {
        L: any;
        map: any;
        dotnetObj: DotNetObject | null | undefined;
        markers: { [id: string]: any };
        layers: { [id: string]: any };
        tileOverlays: { [id: string]: any };
        baseTileLayer: any;
        scaleControl: any;
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

    export class BitMapLeaflet {
        private static _maps: { [id: string]: LeafletState } = {};

        public static init(id: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            const L = (globalThis as any).L;
            if (!L) throw new Error("Leaflet is not loaded.");

            patchLeafletDefaultIcon(L);
            const o = options || {};
            const center: [number, number] = [o.center?.lat ?? 51.505, o.center?.lng ?? -0.09];
            const zoom: number = o.zoom ?? 13;

            const map = L.map(element, {
                center, zoom,
                minZoom: o.minZoom ?? undefined,
                maxZoom: o.maxZoom ?? undefined,
                zoomControl: o.zoomControl !== false,
                attributionControl: o.attributionControl !== false,
                scrollWheelZoom: o.scrollWheelZoom !== false,
                doubleClickZoom: o.doubleClickZoom !== false,
                boxZoom: o.boxZoom !== false,
                dragging: o.dragging !== false,
                keyboard: o.keyboardNavigation !== false,
            });

            const tileUrl = o.tileUrl || "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";
            const baseTileLayer = L.tileLayer(tileUrl, {
                maxZoom: o.tileMaxZoom ?? 19,
                attribution: o.tileAttribution || "",
                opacity: o.tileOpacity ?? 1,
            }).addTo(map);

            const state: LeafletState = {
                L, map, dotnetObj,
                markers: {}, layers: {}, tileOverlays: {},
                baseTileLayer, scaleControl: null,
            };

            BitMapLeaflet._applyMaxBounds(state, o.maxBounds);
            BitMapLeaflet._ensureScaleControl(state, !!o.showScaleControl, !!o.scaleControlImperial);

            if (dotnetObj) {
                map.on('click', (e: any) => dotnetObj.invokeMethodAsync('OnClick', { lat: e.latlng.lat, lng: e.latlng.lng }));
                map.on('dblclick', (e: any) => dotnetObj.invokeMethodAsync('OnDoubleClick', { lat: e.latlng.lat, lng: e.latlng.lng }));
                const notify = () => queueMicrotask(() => dotnetObj.invokeMethodAsync('OnViewChanged', BitMapLeaflet._readView(map)));
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

            if (s.baseTileLayer) s.map.removeLayer(s.baseTileLayer);
            s.baseTileLayer = L.tileLayer(
                o.tileUrl || "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
                { maxZoom: o.tileMaxZoom ?? 19, attribution: o.tileAttribution || "", opacity: o.tileOpacity ?? 1 }
            ).addTo(s.map);
            s.baseTileLayer.bringToBack();

            for (const key in s.tileOverlays) {
                try { s.tileOverlays[key].bringToFront(); } catch { /* ignore */ }
            }

            // Reapply interaction toggles when explicitly provided
            if (o.scrollWheelZoom !== undefined) o.scrollWheelZoom ? s.map.scrollWheelZoom.enable() : s.map.scrollWheelZoom.disable();
            if (o.doubleClickZoom !== undefined) o.doubleClickZoom ? s.map.doubleClickZoom.enable() : s.map.doubleClickZoom.disable();
            if (o.boxZoom !== undefined) o.boxZoom ? s.map.boxZoom.enable() : s.map.boxZoom.disable();
            if (o.dragging !== undefined) o.dragging ? s.map.dragging.enable() : s.map.dragging.disable();
            if (o.keyboardNavigation !== undefined) o.keyboardNavigation ? s.map.keyboard.enable() : s.map.keyboard.disable();

            BitMapLeaflet._applyMaxBounds(s, o.maxBounds);
            BitMapLeaflet._ensureScaleControl(s, !!o.showScaleControl, !!o.scaleControlImperial);
        }

        public static dispose(id: string) {
            const s = BitMapLeaflet._maps[id];
            if (!s) return;
            try {
                for (const key in s.tileOverlays) s.map.removeLayer(s.tileOverlays[key]);
                if (s.scaleControl) s.map.removeControl(s.scaleControl);
                s.map.remove();
            } catch { /* ignore */ }
            s.dotnetObj = null;
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

        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const pad = paddingPx ?? 48;
            s.map.fitBounds(L.latLngBounds(L.latLng(swLat, swLng), L.latLng(neLat, neLng)),
                { padding: [pad, pad], maxZoom: 18 });
        }

        public static fitBoundsToMarkers(id: string, paddingPx: number) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const layers = Object.values(s.markers);
            if (layers.length === 0) return;
            const b = L.featureGroup(layers).getBounds();
            if (!b.isValid()) return;
            const pad = paddingPx ?? 48;
            s.map.fitBounds(b, { padding: [pad, pad], maxZoom: 18 });
        }

        public static addMarker(id: string, markerId: string, opts: any) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            let icon: any | undefined;
            if (opts.iconUrl) {
                const w = opts.iconWidth || 32;
                const h = opts.iconHeight || 32;
                icon = L.icon({
                    iconUrl: opts.iconUrl,
                    iconSize: [w, h],
                    iconAnchor: [Math.floor(w / 2), h],
                    popupAnchor: [0, -h],
                });
            }
            const markerOpts: any = {
                draggable: !!opts.draggable,
                title: opts.title || undefined,
                zIndexOffset: opts.zIndexOffset ?? 0,
            };
            if (icon) markerOpts.icon = icon;

            const m = L.marker([opts.lat, opts.lng], markerOpts);
            if (opts.popupHtml) {
                m.bindPopup(opts.popupHtml);
            } else if (opts.popupText) {
                const el = document.createElement('span');
                el.textContent = opts.popupText;
                m.bindPopup(el);
            }
            if (opts.tooltipHtml) {
                m.bindTooltip(opts.tooltipHtml, {
                    permanent: !!opts.tooltipPermanent,
                    direction: opts.tooltipDirection || 'auto',
                });
            } else if (opts.tooltipText) {
                const el = document.createElement('span');
                el.textContent = opts.tooltipText;
                m.bindTooltip(el, {
                    permanent: !!opts.tooltipPermanent,
                    direction: opts.tooltipDirection || 'auto',
                });
            }
            m.addTo(s.map);

            if (s.dotnetObj) {
                const dn = s.dotnetObj;
                m.on('click', () => dn.invokeMethodAsync('OnMarkerClick', markerId));
                if (opts.draggable) {
                    m.on('dragend', (e: any) => {
                        const p = e.target.getLatLng();
                        dn.invokeMethodAsync('OnMarkerDragEnd', markerId, { lat: p.lat, lng: p.lng });
                    });
                }
            }
            const existing = s.markers[markerId];
            if (existing) s.map.removeLayer(existing);
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
            for (const key in s.markers) s.map.removeLayer(s.markers[key]);
            s.markers = {};
            const len = Math.min(markerIds?.length ?? 0, markers?.length ?? 0);
            for (let i = 0; i < len; i++) BitMapLeaflet.addMarker(id, markerIds[i], markers[i]);
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

        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const pl = L.polyline(latlngs.map(p => [p.lat, p.lng]), BitMapLeaflet._pathStyle(style)).addTo(s.map);
            BitMapLeaflet._wireVectorClick(s, pl, layerId, 'polyline');
            BitMapLeaflet._setLayer(s, layerId, pl);
        }

        public static addPolygon(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const poly = L.polygon(latlngs.map(p => [p.lat, p.lng]), BitMapLeaflet._pathStyle(style)).addTo(s.map);
            BitMapLeaflet._wireVectorClick(s, poly, layerId, 'polygon');
            BitMapLeaflet._setLayer(s, layerId, poly);
        }

        public static addCircle(id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const c = L.circle([lat, lng], { radius: radiusMeters, ...BitMapLeaflet._pathStyle(style) }).addTo(s.map);
            BitMapLeaflet._wireVectorClick(s, c, layerId, 'circle');
            BitMapLeaflet._setLayer(s, layerId, c);
        }

        public static addRectangle(id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            const r = L.rectangle(L.latLngBounds(L.latLng(swLat, swLng), L.latLng(neLat, neLng)),
                BitMapLeaflet._pathStyle(style)).addTo(s.map);
            BitMapLeaflet._wireVectorClick(s, r, layerId, 'rectangle');
            BitMapLeaflet._setLayer(s, layerId, r);
        }

        public static addGeoJson(id: string, layerId: string, geoJsonString: string, style: any) {
            const s = BitMapLeaflet._require(id);
            const L = s.L;
            let gj: any;
            try { gj = JSON.parse(geoJsonString); }
            catch { throw new Error("BitMapLeaflet.addGeoJson: invalid GeoJSON string."); }
            const dn = s.dotnetObj;
            const layer = L.geoJSON(gj, {
                style: () => BitMapLeaflet._pathStyle(style),
                onEachFeature(feature: any, lyr: any) {
                    if (dn) {
                        lyr.on('click', (e: any) => {
                            L.DomEvent.stopPropagation(e);
                            dn.invokeMethodAsync('OnGeoJsonFeatureClick', layerId, feature?.properties || {});
                        });
                    }
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
            const tl = L.tileLayer(opts.urlTemplate, {
                opacity: opts.opacity ?? 1,
                zIndex: opts.zIndex ?? 100,
                maxZoom: opts.maxZoom ?? 19,
                attribution: opts.attribution || "",
            });
            tl.addTo(s.map);
            const existing = s.tileOverlays[opts.id];
            if (existing) s.map.removeLayer(existing);
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

        private static _wireVectorClick(s: LeafletState, layer: any, layerId: string, kind: string) {
            if (!s.dotnetObj) return;
            const dn = s.dotnetObj;
            const L = s.L;
            layer.on('click', (e: any) => {
                L.DomEvent.stopPropagation(e);
                dn.invokeMethodAsync('OnVectorClick', layerId, kind, { lat: e.latlng.lat, lng: e.latlng.lng });
            });
        }

        private static _pathStyle(style: any) {
            if (!style) return {};
            return {
                color: style.color ?? '#3388ff',
                weight: style.weight ?? 3,
                opacity: style.opacity ?? 1,
                fillColor: style.fillColor ?? style.color ?? '#3388ff',
                fillOpacity: style.fillOpacity ?? 0.2,
                dashArray: style.dashArray ?? undefined,
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
