namespace BitBlazorUI {

    /**
     * Azure Maps Web SDK v3 provider. Loads atlas.min.js from the Microsoft CDN
     * and drives an <c>atlas.Map</c>.
     */
    export class BitMapAzureMaps {
        private static _maps: { [id: string]: {
            atlas: any, map: any, dotnetObj: DotNetObject | null | undefined,
            markers: { [k: string]: any },
            layers: { [k: string]: { source: any, layerIds: string[] } },
            geoJsonLayers: { [k: string]: { source: any, layerIds: string[] } },
            tileOverlays: { [k: string]: string },
            zoomControl: any, scaleControl: any,
        } } = {};

        public static async init(id: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            await BitMapHelpers.waitForGlobal('atlas', () => typeof (globalThis as any).atlas?.Map === 'function');
            const atlas = (globalThis as any).atlas;
            const o = options || {};

            const map = new atlas.Map(element, {
                center: [o.center?.lng ?? -0.09, o.center?.lat ?? 51.505],
                zoom: o.zoom ?? 4,
                style: o.style || 'road',
                language: 'en-US',
                authOptions: { authType: 'subscriptionKey', subscriptionKey: o.subscriptionKey || '' },
                showLogo: o.attributionControl !== false,
                showFeedbackLink: false,
                disableTelemetry: true,
                scrollZoomInteraction: o.scrollWheelZoom !== false,
                dragPanInteraction: o.dragging !== false,
                dblClickZoomInteraction: o.doubleClickZoom !== false,
                keyboardInteraction: o.keyboardNavigation !== false,
                ...(o.minZoom != null ? { minZoom: o.minZoom } : {}),
                ...(o.maxZoom != null ? { maxZoom: o.maxZoom } : {}),
            });

            await new Promise<void>(resolve => map.events.add('ready', () => resolve()));

            const state = {
                atlas, map, dotnetObj,
                markers: {} as any, layers: {} as any, geoJsonLayers: {} as any, tileOverlays: {} as any,
                zoomControl: null as any, scaleControl: null as any,
            };
            BitMapAzureMaps._ensureZoom(state, o.zoomControl !== false);
            BitMapAzureMaps._ensureScale(state, !!o.showScaleControl);
            BitMapAzureMaps._wireEvents(state);

            BitMapAzureMaps._maps[id] = state;
            BitMapAzureMaps._notifyView(state);
        }

        public static sync(id: string, options: any) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            const o = options || {};
            s.map.setCamera({ center: [o.center?.lng ?? -0.09, o.center?.lat ?? 51.505], zoom: o.zoom ?? s.map.getCamera().zoom, type: 'jump' });
            if (o.style) s.map.setStyle({ style: o.style });
            s.map.setUserInteraction({
                scrollZoomInteraction: o.scrollWheelZoom !== false,
                dragPanInteraction: o.dragging !== false,
                dblClickZoomInteraction: o.doubleClickZoom !== false,
                keyboardInteraction: o.keyboardNavigation !== false,
            });
            BitMapAzureMaps._ensureZoom(s, o.zoomControl !== false);
            BitMapAzureMaps._ensureScale(s, !!o.showScaleControl);
        }

        public static dispose(id: string) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            try { s.map.dispose(); } catch { /* ignore */ }
            s.dotnetObj = null;
            delete BitMapAzureMaps._maps[id];
        }

        public static invalidateSize(id: string) {
            const s = BitMapAzureMaps._maps[id];
            if (s) s.map.resize();
        }

        public static getView(id: string) {
            const s = BitMapAzureMaps._require(id);
            const cam = s.map.getCamera();
            const c = cam.center ?? [0, 0];
            const b = cam.bounds;
            return {
                center: { lat: c[1], lng: c[0] },
                zoom: cam.zoom ?? 0,
                bounds: b
                    ? { southWest: { lat: b[1], lng: b[0] }, northEast: { lat: b[3], lng: b[2] } }
                    : { southWest: { lat: 0, lng: 0 }, northEast: { lat: 0, lng: 0 } },
            };
        }

        public static setView(id: string, lat: number, lng: number, zoom: number | null, animate: boolean) {
            const s = BitMapAzureMaps._require(id);
            s.map.setCamera({ center: [lng, lat], zoom: zoom ?? s.map.getCamera().zoom, type: animate === false ? 'jump' : 'ease' });
        }

        public static flyTo(id: string, lat: number, lng: number, zoom: number | null) {
            const s = BitMapAzureMaps._require(id);
            s.map.setCamera({ center: [lng, lat], zoom: zoom ?? s.map.getCamera().zoom, type: 'fly', duration: 1200 });
        }

        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, paddingPx: number) {
            const s = BitMapAzureMaps._require(id);
            const pad = paddingPx ?? 48;
            s.map.setCamera({ bounds: [swLng, swLat, neLng, neLat], padding: { top: pad, right: pad, bottom: pad, left: pad }, type: 'ease' });
        }

        public static fitBoundsToMarkers(id: string, paddingPx: number) {
            const s = BitMapAzureMaps._require(id);
            const positions: number[][] = [];
            for (const k in s.markers) {
                const p = s.markers[k].marker.getOptions().position;
                if (p) positions.push(p);
            }
            if (positions.length === 0) return;
            const bounds = s.atlas.data.BoundingBox.fromPositions(positions);
            const pad = paddingPx ?? 48;
            s.map.setCamera({ bounds, padding: { top: pad, right: pad, bottom: pad, left: pad }, type: 'ease' });
        }

        public static addMarker(id: string, markerId: string, opts: any) {
            const s = BitMapAzureMaps._require(id);
            const atlas = s.atlas;
            const popup = opts.popupHtml
                ? new atlas.Popup({ content: `<div style="padding:6px 8px;">${opts.popupHtml}</div>`, pixelOffset: [0, -28], closeButton: true })
                : null;
            const markerOpts: any = { position: [opts.lng, opts.lat], draggable: !!opts.draggable };
            if (opts.iconUrl) {
                const w = opts.iconWidth || 32, h = opts.iconHeight || 32;
                markerOpts.htmlContent =
                    `<div style="position:relative;width:${w}px;height:${h}px;transform:translate(-50%,-100%);">` +
                    `<img src="${opts.iconUrl}" width="${w}" height="${h}" alt="" /></div>`;
            }
            if (opts.title) markerOpts.title = opts.title;
            if (popup) markerOpts.popup = popup;

            const marker = new atlas.HtmlMarker(markerOpts);
            const existing = s.markers[markerId];
            if (existing) try { s.map.markers.remove(existing.marker); } catch { /* ignore */ }
            s.map.markers.add(marker);
            s.markers[markerId] = { marker, popup };

            if (s.dotnetObj) {
                const dn = s.dotnetObj;
                s.map.events.add('click', marker, (e: any) => {
                    e.originalEvent?.stopPropagation?.();
                    dn.invokeMethodAsync('OnMarkerClick', markerId);
                    if (popup) marker.togglePopup();
                });
                if (opts.draggable) {
                    s.map.events.add('dragend', marker, () => {
                        const p = marker.getOptions().position ?? [0, 0];
                        dn.invokeMethodAsync('OnMarkerDragEnd', markerId, { lat: p[1], lng: p[0] });
                    });
                }
            }
        }

        public static removeMarker(id: string, markerId: string) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            const e = s.markers[markerId];
            if (!e) return;
            if (e.popup) e.popup.remove();
            s.map.markers.remove(e.marker);
            delete s.markers[markerId];
        }

        public static clearMarkers(id: string) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            for (const k in s.markers) {
                if (s.markers[k].popup) s.markers[k].popup.remove();
                s.map.markers.remove(s.markers[k].marker);
            }
            s.markers = {};
        }

        public static setMarkerPosition(id: string, markerId: string, lat: number, lng: number) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            const e = s.markers[markerId];
            if (e) e.marker.setOptions({ position: [lng, lat] });
        }

        public static openMarkerPopup(id: string, markerId: string) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            const e = s.markers[markerId];
            if (!e?.popup) return;
            const pos = e.marker.getOptions().position;
            if (pos) e.popup.setOptions({ position: pos });
            e.popup.open(s.map);
        }

        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapAzureMaps._require(id);
            const st = BitMapHelpers.readPathStyle(style);
            const feature = {
                type: 'Feature',
                geometry: { type: 'LineString', coordinates: latlngs.map(p => [p.lng, p.lat]) },
                properties: { _bmLayerId: layerId, _bmKind: 'vector', _bmVectorKind: 'polyline' },
            };
            BitMapAzureMaps._addVectorLayer(s, layerId, [feature], [{
                id: layerId, type: 'line',
                options: {
                    strokeColor: BitMapHelpers.hexToRgba(st.color, st.opacity),
                    strokeWidth: st.weight,
                    strokeDashArray: BitMapAzureMaps._dashArr(st.dashArray),
                },
            }], 'polyline');
        }

        public static addPolygon(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapAzureMaps._require(id);
            const st = BitMapHelpers.readPathStyle(style);
            const ring = latlngs.map(p => [p.lng, p.lat]);
            if (ring.length && (ring[0][0] !== ring[ring.length - 1][0] || ring[0][1] !== ring[ring.length - 1][1])) ring.push(ring[0]);
            const feature = {
                type: 'Feature',
                geometry: { type: 'Polygon', coordinates: [ring] },
                properties: { _bmLayerId: layerId, _bmKind: 'vector', _bmVectorKind: 'polygon' },
            };
            BitMapAzureMaps._addVectorLayer(s, layerId, [feature], [
                { id: `${layerId}-fill`, type: 'polygon', options: { fillColor: BitMapHelpers.hexToRgba(st.fillColor, st.fillOpacity) } },
                { id: `${layerId}-outline`, type: 'line', options: { strokeColor: BitMapHelpers.hexToRgba(st.color, st.opacity), strokeWidth: st.weight, strokeDashArray: BitMapAzureMaps._dashArr(st.dashArray) } },
            ], 'polygon');
        }

        public static addCircle(id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            const s = BitMapAzureMaps._require(id);
            const st = BitMapHelpers.readPathStyle(style);
            const ring = BitMapHelpers.circleRingLngLat(lat, lng, radiusMeters);
            const feature = {
                type: 'Feature',
                geometry: { type: 'Polygon', coordinates: [ring] },
                properties: { _bmLayerId: layerId, _bmKind: 'vector', _bmVectorKind: 'circle' },
            };
            BitMapAzureMaps._addVectorLayer(s, layerId, [feature], [
                { id: `${layerId}-fill`, type: 'polygon', options: { fillColor: BitMapHelpers.hexToRgba(st.fillColor, st.fillOpacity) } },
                { id: `${layerId}-outline`, type: 'line', options: { strokeColor: BitMapHelpers.hexToRgba(st.color, st.opacity), strokeWidth: st.weight } },
            ], 'circle');
        }

        public static addRectangle(id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            const s = BitMapAzureMaps._require(id);
            const st = BitMapHelpers.readPathStyle(style);
            const ring = [[swLng, swLat], [neLng, swLat], [neLng, neLat], [swLng, neLat], [swLng, swLat]];
            const feature = {
                type: 'Feature',
                geometry: { type: 'Polygon', coordinates: [ring] },
                properties: { _bmLayerId: layerId, _bmKind: 'vector', _bmVectorKind: 'rectangle' },
            };
            BitMapAzureMaps._addVectorLayer(s, layerId, [feature], [
                { id: `${layerId}-fill`, type: 'polygon', options: { fillColor: BitMapHelpers.hexToRgba(st.fillColor, st.fillOpacity) } },
                { id: `${layerId}-outline`, type: 'line', options: { strokeColor: BitMapHelpers.hexToRgba(st.color, st.opacity), strokeWidth: st.weight, strokeDashArray: BitMapAzureMaps._dashArr(st.dashArray) } },
            ], 'rectangle');
        }

        public static addGeoJson(id: string, layerId: string, geoJsonString: string, style: any) {
            const s = BitMapAzureMaps._require(id);
            BitMapAzureMaps._removeExisting(s, layerId);
            const st = BitMapHelpers.readPathStyle(style);
            let gj: any;
            try { gj = JSON.parse(geoJsonString); } catch { throw new Error('Invalid GeoJSON string'); }
            const augment = (f: any) => ({ ...f, properties: { ...(f.properties ?? {}), _bmLayerId: layerId, _bmKind: 'geojson' } });
            const data = gj.type === 'FeatureCollection'
                ? { ...gj, features: gj.features.map(augment) }
                : gj.type === 'Feature'
                    ? { type: 'FeatureCollection', features: [augment(gj)] }
                    : { type: 'FeatureCollection', features: [augment({ type: 'Feature', geometry: gj, properties: {} })] };

            const ds = new s.atlas.source.DataSource();
            s.map.sources.add(ds);
            ds.add(data);

            const polygonLayer = new s.atlas.layer.PolygonLayer(ds, `${layerId}-fill`, {
                fillColor: BitMapHelpers.hexToRgba(st.fillColor, st.fillOpacity),
                filter: ['any', ['==', ['geometry-type'], 'Polygon'], ['==', ['geometry-type'], 'MultiPolygon']],
            });
            const lineLayer = new s.atlas.layer.LineLayer(ds, `${layerId}-line`, {
                strokeColor: BitMapHelpers.hexToRgba(st.color, st.opacity),
                strokeWidth: st.weight,
                strokeDashArray: BitMapAzureMaps._dashArr(st.dashArray),
            });
            const bubbleLayer = new s.atlas.layer.BubbleLayer(ds, `${layerId}-bubble`, {
                color: BitMapHelpers.hexToRgba(st.color, 1),
                radius: 6, strokeColor: '#ffffff', strokeWidth: 2,
                filter: ['==', ['geometry-type'], 'Point'],
            });
            s.map.layers.add([polygonLayer, lineLayer, bubbleLayer]);
            s.geoJsonLayers[layerId] = { source: ds, layerIds: [`${layerId}-fill`, `${layerId}-line`, `${layerId}-bubble`] };
        }

        public static removeLayer(id: string, layerId: string) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            const info = s.layers[layerId] ?? s.geoJsonLayers[layerId];
            if (!info) return;
            for (const lid of info.layerIds) try { s.map.layers.remove(lid); } catch { /* ignore */ }
            try { s.map.sources.remove(info.source); } catch { /* ignore */ }
            delete s.layers[layerId];
            delete s.geoJsonLayers[layerId];
        }

        public static clearVectorLayers(id: string) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            for (const k of Object.keys(s.layers).slice()) BitMapAzureMaps.removeLayer(id, k);
            for (const k of Object.keys(s.geoJsonLayers).slice()) BitMapAzureMaps.removeLayer(id, k);
        }

        public static addTileOverlay(id: string, opts: any) {
            const s = BitMapAzureMaps._require(id);
            const tlId = `_bm_tile_${opts.id}`;
            const tl = new s.atlas.layer.TileLayer({
                tileUrl: (opts.urlTemplate || '').replace('{s}', 'a'),
                opacity: opts.opacity ?? 1,
                maxSourceZoom: opts.maxZoom ?? 19,
            }, tlId);
            s.map.layers.add(tl);
            s.tileOverlays[opts.id] = tlId;
        }

        public static removeTileOverlay(id: string, overlayId: string) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) return;
            const tlId = s.tileOverlays[overlayId];
            if (tlId) { try { s.map.layers.remove(tlId); } catch { /* ignore */ } delete s.tileOverlays[overlayId]; }
        }

        // ---- helpers ----

        private static _require(id: string) {
            const s = BitMapAzureMaps._maps[id];
            if (!s) throw new Error(`BitMapAzureMaps: unknown map id '${id}'`);
            return s;
        }

        private static _removeExisting(s: any, layerId: string) {
            const info = s.layers[layerId] ?? s.geoJsonLayers[layerId];
            if (!info) return;
            for (const lid of info.layerIds) try { s.map.layers.remove(lid); } catch { /* ignore */ }
            try { s.map.sources.remove(info.source); } catch { /* ignore */ }
            delete s.layers[layerId];
            delete s.geoJsonLayers[layerId];
        }

        private static _addVectorLayer(s: any, layerId: string, features: any[], layerDefs: any[], _kind: string) {
            BitMapAzureMaps._removeExisting(s, layerId);
            const ds = new s.atlas.source.DataSource();
            s.map.sources.add(ds);
            ds.add(features);
            const layerIds: string[] = [];
            for (const def of layerDefs) {
                let lyr: any;
                if (def.type === 'line') lyr = new s.atlas.layer.LineLayer(ds, def.id, def.options);
                else if (def.type === 'polygon') lyr = new s.atlas.layer.PolygonLayer(ds, def.id, def.options);
                else if (def.type === 'bubble') lyr = new s.atlas.layer.BubbleLayer(ds, def.id, def.options);
                if (lyr) { s.map.layers.add(lyr); layerIds.push(def.id); }
            }
            s.layers[layerId] = { source: ds, layerIds };
        }

        private static _dashArr(dash: string | undefined) {
            if (!dash) return undefined;
            const a = dash.split(/[\s,]+/).map(Number).filter(n => !isNaN(n));
            return a.length > 0 ? a : undefined;
        }

        private static _ensureZoom(s: any, show: boolean) {
            if (show && !s.zoomControl) {
                s.zoomControl = new s.atlas.control.ZoomControl();
                s.map.controls.add(s.zoomControl, { position: 'top-right' });
            } else if (!show && s.zoomControl) {
                s.map.controls.remove(s.zoomControl); s.zoomControl = null;
            }
        }

        private static _ensureScale(s: any, show: boolean) {
            if (show && !s.scaleControl) {
                s.scaleControl = new s.atlas.control.ScaleControl();
                s.map.controls.add(s.scaleControl, { position: 'bottom-left' });
            } else if (!show && s.scaleControl) {
                s.map.controls.remove(s.scaleControl); s.scaleControl = null;
            }
        }

        private static _wireEvents(s: any) {
            const map = s.map, dn = s.dotnetObj;
            map.events.add('click', (e: any) => {
                if (e.shapes && e.shapes.length > 0) {
                    const shape = e.shapes[0];
                    const props = shape.getProperties ? shape.getProperties() : (shape.properties ?? {});
                    if (props._bmKind === 'geojson' && s.geoJsonLayers[props._bmLayerId]) {
                        if (dn) {
                            const clean: any = {};
                            for (const [k, v] of Object.entries(props)) if (!k.startsWith('_bm')) clean[k] = v;
                            dn.invokeMethodAsync('OnGeoJsonFeatureClick', props._bmLayerId, clean);
                        }
                        return;
                    }
                    if (props._bmKind === 'vector' && s.layers[props._bmLayerId]) {
                        if (dn) {
                            const pos = e.position ?? [0, 0];
                            dn.invokeMethodAsync('OnVectorClick', props._bmLayerId, props._bmVectorKind || 'vector', { lat: pos[1], lng: pos[0] });
                        }
                        return;
                    }
                }
                if (dn && e.position) dn.invokeMethodAsync('OnClick', { lat: e.position[1], lng: e.position[0] });
            });
            map.events.add('dblclick', (e: any) => {
                if (dn && e.position) dn.invokeMethodAsync('OnDoubleClick', { lat: e.position[1], lng: e.position[0] });
            });
            let viewTimer: any = null;
            map.events.add('moveend', () => { clearTimeout(viewTimer); viewTimer = setTimeout(() => BitMapAzureMaps._notifyView(s), 80); });
        }

        private static _notifyView(s: any) {
            if (!s.dotnetObj) return;
            const cam = s.map.getCamera();
            if (!cam) return;
            const c = cam.center ?? [0, 0];
            const b = cam.bounds;
            queueMicrotask(() => s.dotnetObj.invokeMethodAsync('OnViewChanged', {
                center: { lat: c[1], lng: c[0] },
                zoom: cam.zoom ?? 0,
                bounds: b
                    ? { southWest: { lat: b[1], lng: b[0] }, northEast: { lat: b[3], lng: b[2] } }
                    : { southWest: { lat: 0, lng: 0 }, northEast: { lat: 0, lng: 0 } },
            }));
        }
    }
}
