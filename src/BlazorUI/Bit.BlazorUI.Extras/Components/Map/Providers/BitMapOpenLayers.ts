namespace BitBlazorUI {

    /**
     * OpenLayers provider. Loads OpenLayers ES modules from esm.sh on first init.
     * Mirrors the public surface used by every BitMap provider.
     */
    export class BitMapOpenLayers {
        private static _olLoadPromise: Promise<any> | null = null;

        private static _maps: { [id: string]: {
            ol: any, map: any, dotnetObj: DotNetObject | null | undefined,
            baseTileLayer: any, markers: { [k: string]: any },
            layers: { [k: string]: any }, tileOverlays: { [k: string]: any },
            scaleLine: any, zIndexCounter: number,
            markerSource: any, markerLayer: any,
            popupOverlay: any, popupElement: HTMLElement, popupContentElement: HTMLElement,
            translateInteraction: any,
            tileUrl: string, tileMaxZoom: number, tileAttribution: string, tileOpacity: number,
            scaleEnabled: boolean, scaleImperial: boolean,
            scrollWheelZoom: boolean, doubleClickZoom: boolean,
            dragging: boolean, boxZoom: boolean, keyboardNavigation: boolean,
        } } = {};

        private static readonly _defaultTileUrl = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
        private static readonly _osmAttribution = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';

        private static _resolveTileUrl(o: any): string {
            return (o.tileUrl || BitMapOpenLayers._defaultTileUrl).replace('{s}', 'a');
        }

        // When the resolved tileUrl is the built-in OSM URL and the caller didn't
        // supply a non-empty attribution, fall back to the standard OSM credit
        // (OSM's tile usage policy contractually requires it). Mirrors the URL-keyed
        // attribution rule in BitMapLeaflet so the two providers behave consistently
        // when consumers leave tileUrl/tileAttribution unset.
        private static _resolveTileAttribution(tileUrl: string, tileAttribution: any): string {
            if (typeof tileAttribution === 'string' && tileAttribution.length > 0) return tileAttribution;
            if (tileUrl === BitMapOpenLayers._defaultTileUrl) return BitMapOpenLayers._osmAttribution;
            return typeof tileAttribution === 'string' ? tileAttribution : '';
        }

        public static async init(id: string, canvasId: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            element = await BitMapHelpers.resolveMapCanvas(canvasId, element);
            const ol = await BitMapOpenLayers._loadOl();
            const o = options || {};
            const lng0 = o.center?.lng ?? -0.09, lat0 = o.center?.lat ?? 51.505;
            const zoom = o.zoom ?? 13;

            const tileUrl = BitMapOpenLayers._resolveTileUrl(o);
            const tileMaxZoom = o.tileMaxZoom ?? 19;
            const tileAttribution = BitMapOpenLayers._resolveTileAttribution(tileUrl, o.tileAttribution);
            const tileOpacity = o.tileOpacity ?? 1;

            const baseTile = new ol.TileLayer({
                source: new ol.XYZ({
                    url: tileUrl,
                    maxZoom: tileMaxZoom,
                    attributions: tileAttribution,
                }),
                opacity: tileOpacity,
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

            // Create popup overlay element. Styles live in BitMap.scss
            // (.bit-map-ol-popup, .bit-map-ol-popup__close, .bit-map-ol-popup__content)
            // so we don't rely on inline styles that strict CSP would block, and so
            // consumers can theme the popup from their own stylesheets.
            const popupElement = document.createElement('div');
            popupElement.className = 'bit-map-ol-popup bit-map-ol-popup--hidden';

            const popupCloser = document.createElement('button');
            popupCloser.type = 'button';
            popupCloser.textContent = '\u00d7';
            popupCloser.className = 'bit-map-ol-popup__close';
            popupCloser.setAttribute('aria-label', 'Close popup');
            popupElement.appendChild(popupCloser);

            const popupContent = document.createElement('div');
            popupContent.className = 'bit-map-ol-popup__content';
            popupElement.appendChild(popupContent);

            // Stop pointer/click events on the popup from bubbling to the map. Without this,
            // interacting with the popup UI (links, buttons, the close button) can reach the
            // map's singleclick handler and either fire OnClick or immediately re-close the popup.
            const stopPropagation = (e: Event) => e.stopPropagation();
            popupElement.addEventListener('pointerdown', stopPropagation);
            popupElement.addEventListener('mousedown', stopPropagation);
            popupElement.addEventListener('click', stopPropagation);
            popupElement.addEventListener('dblclick', stopPropagation);

            const popupOverlay = new ol.Overlay({
                element: popupElement,
                autoPan: true,
                positioning: 'bottom-center',
                offset: [0, -12],
            });
            map.addOverlay(popupOverlay);

            popupCloser.addEventListener('click', (e) => {
                e.stopPropagation();
                popupOverlay.setPosition(undefined);
                popupElement.classList.add('bit-map-ol-popup--hidden');
            });

            const scaleEnabled = !!o.showScaleControl;
            const scaleImperial = !!o.scaleControlImperial;
            const scrollWheelZoom = o.scrollWheelZoom !== false;
            const doubleClickZoom = o.doubleClickZoom !== false;
            const dragging = o.dragging !== false;
            const boxZoom = o.boxZoom !== false;
            const keyboardNavigation = o.keyboardNavigation !== false;

            const state = {
                ol, map, dotnetObj,
                baseTileLayer: baseTile,
                markers: {} as any,
                layers: {} as any,
                tileOverlays: {} as any,
                scaleLine: null as any,
                zIndexCounter: 100,
                markerSource, markerLayer,
                popupOverlay, popupElement, popupContentElement: popupContent,
                translateInteraction: null as any,
                tileUrl, tileMaxZoom, tileAttribution, tileOpacity,
                scaleEnabled, scaleImperial,
                scrollWheelZoom, doubleClickZoom, dragging, boxZoom, keyboardNavigation,
            };

            BitMapOpenLayers._ensureScale(state, scaleEnabled, scaleImperial);
            BitMapOpenLayers._applyInteractions(state, {
                scrollWheelZoom, doubleClickZoom, dragging, boxZoom, keyboardNavigation,
            });
            BitMapOpenLayers._wireEvents(state);

            // Add Translate interaction for draggable markers
            const translate = new ol.Translate({
                filter: (feature: any) => feature.get && feature.get('draggable') === true,
                layers: [markerLayer],
            });
            translate.on('translateend', (evt: any) => {
                const features = evt.features?.getArray?.() || [];
                for (const f of features) {
                    const mid = f.get('markerId');
                    if (mid && dotnetObj) {
                        const coords = ol.toLonLat(f.getGeometry().getCoordinates());
                        dotnetObj.invokeMethodAsync('OnMarkerDragEnd', mid, { lat: coords[1], lng: coords[0] });
                    }
                }
            });
            map.addInteraction(translate);
            state.translateInteraction = translate;

            BitMapOpenLayers._maps[id] = state;
            queueMicrotask(() => map.updateSize());
        }

        public static sync(id: string, options: any) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            const ol = s.ol, view = s.map.getView();
            const o = options || {};
            if (o.center != null) view.setCenter(ol.fromLonLat([o.center.lng, o.center.lat]));
            if (o.zoom != null) view.setZoom(o.zoom);

            // Only recreate the base tile source when tile-defining options actually change;
            // otherwise we'd force a full tile reload on every sync (e.g. when only center/zoom
            // or interaction toggles change). Prefer caller-supplied values, then fall back to
            // the stored state so a partial sync doesn't reset a previously-applied custom basemap.
            const nextTileUrl = o.tileUrl != null
                ? BitMapOpenLayers._resolveTileUrl(o)
                : s.tileUrl;
            const nextTileMaxZoom = o.tileMaxZoom ?? s.tileMaxZoom;
            // Mirror init's URL-keyed attribution rule so an attribution from a
            // previous OSM-default base layer doesn't leak onto a newly-set custom
            // tileUrl, and a switch back to the OSM default re-applies the OSM
            // credit when the caller left tileAttribution unset.
            const callerAttrProvided = typeof o.tileAttribution === 'string';
            const callerAttrNonEmpty = callerAttrProvided && (o.tileAttribution as string).length > 0;
            const urlChanged = nextTileUrl !== s.tileUrl;
            let nextTileAttribution: string;
            if (callerAttrNonEmpty) {
                nextTileAttribution = o.tileAttribution as string;
            } else if (callerAttrProvided || urlChanged) {
                nextTileAttribution = BitMapOpenLayers._resolveTileAttribution(
                    nextTileUrl,
                    callerAttrProvided ? o.tileAttribution : '');
            } else {
                nextTileAttribution = s.tileAttribution;
            }
            if (nextTileUrl !== s.tileUrl ||
                nextTileMaxZoom !== s.tileMaxZoom ||
                nextTileAttribution !== s.tileAttribution) {
                s.baseTileLayer.setSource(new ol.XYZ({
                    url: nextTileUrl,
                    maxZoom: nextTileMaxZoom,
                    attributions: nextTileAttribution,
                }));
                s.tileUrl = nextTileUrl;
                s.tileMaxZoom = nextTileMaxZoom;
                s.tileAttribution = nextTileAttribution;
            }
            const nextTileOpacity = o.tileOpacity ?? s.tileOpacity;
            if (nextTileOpacity !== s.tileOpacity) {
                s.baseTileLayer.setOpacity(nextTileOpacity);
                s.tileOpacity = nextTileOpacity;
            }

            // Only touch the scale bar when caller explicitly supplied either flag,
            // so a partial sync doesn't toggle visibility or units off.
            const hasShow = Object.prototype.hasOwnProperty.call(o, 'showScaleControl');
            const hasImperial = Object.prototype.hasOwnProperty.call(o, 'scaleControlImperial');
            if (hasShow || hasImperial) {
                if (hasShow) s.scaleEnabled = !!o.showScaleControl;
                if (hasImperial) s.scaleImperial = !!o.scaleControlImperial;
                BitMapOpenLayers._ensureScale(s, s.scaleEnabled, s.scaleImperial);
            }

            // Only re-apply interactions for keys the caller explicitly provided so
            // omitted flags are treated as "unchanged" rather than re-enabled defaults.
            const interactionFlags: any = {};
            let anyInteractionTouched = false;
            for (const key of ['scrollWheelZoom', 'doubleClickZoom', 'dragging', 'boxZoom', 'keyboardNavigation']) {
                if (Object.prototype.hasOwnProperty.call(o, key)) {
                    const v = o[key] !== false;
                    interactionFlags[key] = v;
                    (s as any)[key] = v;
                    anyInteractionTouched = true;
                } else {
                    interactionFlags[key] = (s as any)[key];
                }
            }
            if (anyInteractionTouched) {
                BitMapOpenLayers._applyInteractions(s, interactionFlags);
            }
        }

        public static dispose(id: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            try {
                if (s.translateInteraction) s.map.removeInteraction(s.translateInteraction);
                for (const k in s.tileOverlays) s.map.removeLayer(s.tileOverlays[k]);
                if (s.scaleLine) s.map.removeControl(s.scaleLine);
                s.map.removeOverlay(s.popupOverlay);
                s.popupElement.remove();
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
                markerId, popupHtml: opts.popupHtml || '', popupText: opts.popupText || '', title: opts.title || '',
                draggable: !!opts.draggable,
            });
            f.setId(markerId);
            f.setStyle(BitMapOpenLayers._markerStyle(ol, opts));
            const existing = s.markers[markerId];
            if (existing) try { s.markerSource.removeFeature(existing); } catch { /* ignore */ }
            s.markerSource.addFeature(f);
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

        public static syncMarkers(id: string, markerIds: string[], markers: any[]) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            s.markerSource.clear();
            s.markers = {};
            const len = Math.min(markerIds?.length ?? 0, markers?.length ?? 0);
            for (let i = 0; i < len; i++) BitMapOpenLayers.addMarker(id, markerIds[i], markers[i]);
        }

        public static setMarkerPosition(id: string, markerId: string, lat: number, lng: number) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            const f = s.markers[markerId];
            if (f) f.getGeometry().setCoordinates(s.ol.fromLonLat([lng, lat]));
        }

        public static openMarkerPopup(id: string, markerId: string) {
            const s = BitMapOpenLayers._maps[id];
            if (!s) return;
            const f = s.markers[markerId];
            if (!f) return;
            BitMapOpenLayers._showPopupForFeature(s, f);
        }

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
            const existing = s.tileOverlays[opts.id];
            if (existing) {
                s.map.removeLayer(existing);
                delete s.tileOverlays[opts.id];
            }
            const tl = new ol.TileLayer({
                source: new ol.XYZ({
                    url: (opts.urlTemplate || '').replace('{s}', 'a'),
                    maxZoom: opts.maxZoom ?? 19,
                    attributions: opts.attribution || '',
                }),
                opacity: opts.opacity ?? 1,
                zIndex: opts.zIndex ?? 100,
            });
            s.tileOverlays[opts.id] = tl;
            s.map.addLayer(tl);
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

        private static _applyInteractions(s: any, flags: any) {
            const map = s.map;
            const interactions = map.getInteractions().getArray();
            for (const interaction of interactions) {
                const name = interaction.constructor?.name || '';
                if (name === 'MouseWheelZoom' || name.includes('MouseWheel')) {
                    if (flags.scrollWheelZoom !== undefined) interaction.setActive(!!flags.scrollWheelZoom);
                } else if (name === 'DoubleClickZoom' || name.includes('DoubleClick')) {
                    if (flags.doubleClickZoom !== undefined) interaction.setActive(!!flags.doubleClickZoom);
                } else if (name === 'DragPan' || name.includes('DragPan')) {
                    if (flags.dragging !== undefined) interaction.setActive(!!flags.dragging);
                } else if (name === 'DragZoom' || name.includes('DragZoom')) {
                    if (flags.boxZoom !== undefined) interaction.setActive(!!flags.boxZoom);
                } else if (name === 'KeyboardPan' || name === 'KeyboardZoom' || name.includes('Keyboard')) {
                    if (flags.keyboardNavigation !== undefined) interaction.setActive(!!flags.keyboardNavigation);
                }
            }
        }

        private static _markerStyle(ol: any, opts: any) {
            if (opts.iconUrl) {
                const iconOpts: any = {
                    src: opts.iconUrl,
                    anchor: [0.5, 1], anchorXUnits: 'fraction', anchorYUnits: 'fraction',
                };
                // OpenLayers' ol.Icon asserts when both explicit width/height and a scale
                // are provided - pick one path: honor caller-supplied dimensions when
                // present, otherwise fall back to the default 1:1 scale.
                if (opts.iconWidth || opts.iconHeight) {
                    if (opts.iconWidth) iconOpts.width = opts.iconWidth;
                    if (opts.iconHeight) iconOpts.height = opts.iconHeight;
                } else {
                    iconOpts.scale = 1;
                }
                return new ol.Style({
                    image: new ol.Icon(iconOpts),
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
            // Accept commas and/or any whitespace as separators (e.g. "4 2" or "4, 2"),
            // and drop empty/NaN tokens so a stray separator can't poison the dash array.
            let lineDash: number[] | undefined;
            if (st.dashArray) {
                const parsed = String(st.dashArray)
                    .split(/[\s,]+/)
                    .map((x: string) => parseFloat(x))
                    .filter((n: number) => Number.isFinite(n));
                if (parsed.length > 0) lineDash = parsed;
            }
            return new ol.Stroke({
                color: BitMapHelpers.hexToRgba(st.color, st.opacity),
                width: st.weight,
                lineDash,
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

        private static _showPopupForFeature(s: any, feature: any) {
            const html = feature.get('popupHtml') || '';
            const text = feature.get('popupText') || '';
            if (!html && !text) return;

            const contentEl = s.popupContentElement;
            // popupHtml is rendered raw to match every other BitMap provider's contract
            // (Leaflet's bindPopup, Mapbox/MapLibre setHTML, ArcGis popup.open, Cesium
            // entity.description, Azure Maps Popup content). Sanitization is the
            // caller's responsibility; pass untrusted strings through popupText, which
            // is written via textContent below and is safe from XSS.
            if (html) {
                contentEl.innerHTML = html;
            } else {
                contentEl.textContent = text;
            }

            const coords = feature.getGeometry().getCoordinates();
            s.popupElement.classList.remove('bit-map-ol-popup--hidden');
            s.popupOverlay.setPosition(coords);
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
                            BitMapOpenLayers._showPopupForFeature(s, feature);
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
                if (!hit) {
                    // Close popup when clicking elsewhere on the map
                    s.popupOverlay.setPosition(undefined);
                    s.popupElement.classList.add('bit-map-ol-popup--hidden');
                }
            });
            map.on('dblclick', (evt: any) => {
                if (!dn) return;
                const ll = ol.toLonLat(evt.coordinate);
                dn.invokeMethodAsync('OnDoubleClick', { lat: ll[1], lng: ll[0] });
            });
            map.on('moveend', () => {
                // Snapshot the .NET handle so the microtask can verify it's still
                // associated with a live state. dispose() nulls s.dotnetObj, so we
                // bail out if it has been replaced (or cleared) by the time the
                // microtask runs - otherwise we'd invoke a disposed DotNetObject.
                const capturedDn = s.dotnetObj;
                if (!capturedDn) return;
                queueMicrotask(() => {
                    if (s.dotnetObj !== capturedDn) return;
                    capturedDn.invokeMethodAsync('OnViewChanged', BitMapOpenLayers._readView(s));
                });
            });
        }

        private static async _loadOl(): Promise<any> {
            if (BitMapOpenLayers._olLoadPromise) return BitMapOpenLayers._olLoadPromise;
            // The CSP-friendly loader (a real ES module shipped under
            // _content/Bit.BlazorUI.Extras/openlayers/bit-map-ol-loader.js) performs the
            // actual dynamic import()s and exposes the resolved bundle on
            // globalThis.__bitMapOlBundle. We just await it here so this concatenated
            // (non-module) bundle never has to call import() directly.
            const p = (async () => {
                await BitMapHelpers.waitForGlobal('__bitMapOlBundle', () => !!(globalThis as any).__bitMapOlBundle, 30_000);
                return await (globalThis as any).__bitMapOlBundle;
            })();
            // Clear the memoized promise on failure so callers can retry the OL load
            // instead of being permanently stuck on a rejected promise.
            p.catch(() => { if (BitMapOpenLayers._olLoadPromise === p) BitMapOpenLayers._olLoadPromise = null; });
            BitMapOpenLayers._olLoadPromise = p;
            return p;
        }
    }
}
