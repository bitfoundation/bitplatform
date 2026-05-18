namespace BitBlazorUI {

    /**
     * CesiumJS provider — 3D globe. Loads CesiumJS from the official CDN and drives a Viewer.
     * Markers and vector layers are added as Entities on the Viewer's main entity collection.
     */
    export class BitMapCesium {
        private static _maps: { [id: string]: {
            Cesium: any, viewer: any, dotnetObj: DotNetObject | null | undefined,
            markers: { [k: string]: any },
            layers: { [k: string]: { entity: any, kind: string } },
            geoJsonLayers: { [k: string]: any },   // DataSource refs
            tileOverlays: { [k: string]: any },
        } } = {};

        public static async init(id: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            await BitMapHelpers.waitForGlobal('Cesium', () => typeof (globalThis as any).Cesium?.Viewer === 'function');
            const Cesium = (globalThis as any).Cesium;
            const o = options || {};

            // Default to OSM imagery if no ion token
            if (o.ionAccessToken) {
                Cesium.Ion.defaultAccessToken = o.ionAccessToken;
            }

            // Cesium 1.104+ deprecated imageryProvider in Viewer constructor;
            // use baseLayer instead. createWorldTerrain() is also deprecated in
            // favor of createWorldTerrainAsync().
            const baseLayer = await (async () => {
                if (o.imageryStyle === 'bing_aerial' && o.ionAccessToken) {
                    // Use Cesium's default Bing imagery via Ion
                    return undefined; // Viewer will use Ion default when baseLayer is undefined and token is set
                }
                if (o.imageryStyle === 'none') {
                    return false as any; // false disables the base imagery layer
                }
                // OSM tiles
                const osmProvider = new Cesium.UrlTemplateImageryProvider({
                    url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
                    subdomains: ['a', 'b', 'c'],
                    credit: '© OpenStreetMap contributors',
                    maximumLevel: 19,
                });
                return Cesium.ImageryLayer
                    ? new Cesium.ImageryLayer(osmProvider)
                    : osmProvider; // fallback for older Cesium builds
            })();

            // Terrain: use async API (Cesium 1.104+)
            let terrainProvider: any;
            if (o.terrainEnabled && o.ionAccessToken) {
                terrainProvider = Cesium.createWorldTerrainAsync
                    ? await Cesium.createWorldTerrainAsync()
                    : Cesium.createWorldTerrain();
            } else {
                terrainProvider = new Cesium.EllipsoidTerrainProvider();
            }

            const viewerOptions: any = {
                animation: !!o.animationWidget,
                timeline: !!o.timelineWidget,
                baseLayerPicker: !!o.baseLayerPicker,
                navigationHelpButton: !!o.navigationHelpButton,
                homeButton: !!o.homeButton,
                fullscreenButton: !!o.fullscreenButton,
                geocoder: !!o.geocoder,
                infoBox: o.infoBox !== false,
                sceneModePicker: false,
                terrainProvider,
            };

            // Use baseLayer (Cesium 1.104+) instead of deprecated imageryProvider
            if (baseLayer !== undefined) {
                viewerOptions.baseLayer = baseLayer;
            }

            const viewer = new Cesium.Viewer(element, viewerOptions);

            try {
                viewer.scene.shadowMap.enabled = !!o.shadowsEnabled;
            } catch { /* ignore */ }

            try {
                if (o.sceneMode === 'scene2d') viewer.scene.morphTo2D(0);
                else if (o.sceneMode === 'columbus') viewer.scene.morphToColumbusView(0);
            } catch { /* ignore */ }

            // Initial camera
            const lat = o.center?.lat ?? 51.505;
            const lng = o.center?.lng ?? -0.09;
            const altitude = o.altitude ?? BitMapCesium._zoomToAltitude(o.zoom ?? 4);
            viewer.camera.setView({ destination: Cesium.Cartesian3.fromDegrees(lng, lat, altitude) });

            const state = {
                Cesium, viewer, dotnetObj,
                markers: {} as any, layers: {} as any, geoJsonLayers: {} as any, tileOverlays: {} as any,
            };
            BitMapCesium._wireEvents(state);
            BitMapCesium._maps[id] = state;
            BitMapCesium._notifyView(state);
        }

        public static sync(id: string, options: any) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            const o = options || {};
            const lat = o.center?.lat ?? 51.505;
            const lng = o.center?.lng ?? -0.09;
            const altitude = o.altitude ?? BitMapCesium._zoomToAltitude(o.zoom ?? 4);
            s.viewer.camera.flyTo({
                destination: s.Cesium.Cartesian3.fromDegrees(lng, lat, altitude),
                duration: 0,
            });
        }

        public static dispose(id: string) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            try { s.viewer.destroy(); } catch { /* ignore */ }
            s.dotnetObj = null;
            delete BitMapCesium._maps[id];
        }

        public static invalidateSize(id: string) {
            const s = BitMapCesium._maps[id];
            if (s) try { s.viewer.resize(); } catch { /* ignore */ }
        }

        public static getView(id: string) {
            return BitMapCesium._readView(BitMapCesium._require(id));
        }

        public static setView(id: string, lat: number, lng: number, zoom: number | null, _animate: boolean) {
            const s = BitMapCesium._require(id);
            const altitude = BitMapCesium._zoomToAltitude(zoom ?? 4);
            s.viewer.camera.setView({ destination: s.Cesium.Cartesian3.fromDegrees(lng, lat, altitude) });
        }

        public static flyTo(id: string, lat: number, lng: number, zoom: number | null) {
            const s = BitMapCesium._require(id);
            const altitude = BitMapCesium._zoomToAltitude(zoom ?? 4);
            s.viewer.camera.flyTo({ destination: s.Cesium.Cartesian3.fromDegrees(lng, lat, altitude), duration: 1.5 });
        }

        public static fitBounds(id: string, swLat: number, swLng: number, neLat: number, neLng: number, _paddingPx: number) {
            const s = BitMapCesium._require(id);
            const Cesium = s.Cesium;
            s.viewer.camera.flyTo({
                destination: Cesium.Rectangle.fromDegrees(Math.min(swLng, neLng), Math.min(swLat, neLat), Math.max(swLng, neLng), Math.max(swLat, neLat)),
                duration: 0,
            });
        }

        public static fitBoundsToMarkers(id: string, _paddingPx: number) {
            const s = BitMapCesium._require(id);
            const ents = Object.values(s.markers);
            if (ents.length === 0) return;
            try { s.viewer.flyTo(ents, { duration: 1.0 }); } catch { /* ignore */ }
        }

        public static addMarker(id: string, markerId: string, opts: any) {
            const s = BitMapCesium._require(id);
            const Cesium = s.Cesium;
            const existing = s.markers[markerId];
            if (existing) try { s.viewer.entities.remove(existing); } catch { /* ignore */ }
            const billboard = opts.iconUrl ? {
                image: opts.iconUrl,
                width: opts.iconWidth || 32,
                height: opts.iconHeight || 32,
                verticalOrigin: Cesium.VerticalOrigin.BOTTOM,
            } : {
                image: BitMapCesium._defaultPin(),
                width: 27, height: 41,
                verticalOrigin: Cesium.VerticalOrigin.BOTTOM,
            };
            const ent = s.viewer.entities.add({
                id: `bm-marker-${id}-${markerId}`,
                position: Cesium.Cartesian3.fromDegrees(opts.lng, opts.lat),
                billboard,
                label: opts.title ? { text: opts.title, font: '12px sans-serif', pixelOffset: new Cesium.Cartesian2(0, -50) } : undefined,
                description: opts.popupHtml || undefined,
                _bmMarkerId: markerId,
            });
            s.markers[markerId] = ent;
        }

        public static removeMarker(id: string, markerId: string) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            const e = s.markers[markerId];
            if (e) { s.viewer.entities.remove(e); delete s.markers[markerId]; }
        }

        public static clearMarkers(id: string) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            for (const k in s.markers) s.viewer.entities.remove(s.markers[k]);
            s.markers = {};
        }

        public static setMarkerPosition(id: string, markerId: string, lat: number, lng: number) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            const e = s.markers[markerId];
            if (e) e.position = s.Cesium.Cartesian3.fromDegrees(lng, lat);
        }

        public static openMarkerPopup(id: string, markerId: string) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            const e = s.markers[markerId];
            if (e) s.viewer.selectedEntity = e;
        }

        public static addPolyline(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapCesium._require(id);
            const Cesium = s.Cesium;
            const st = BitMapHelpers.readPathStyle(style);
            const positions = Cesium.Cartesian3.fromDegreesArray(latlngs.flatMap(p => [p.lng, p.lat]));
            const ent = s.viewer.entities.add({
                id: `bm-poly-${id}-${layerId}`,
                polyline: { positions, width: st.weight, material: BitMapCesium._color(Cesium, st.color, st.opacity) },
                _bmLayerId: layerId, _bmVectorKind: 'polyline',
            });
            BitMapCesium._setLayer(s, layerId, ent, 'polyline');
        }

        public static addPolygon(id: string, layerId: string, latlngs: BitMapLL[], style: any) {
            const s = BitMapCesium._require(id);
            const Cesium = s.Cesium;
            const st = BitMapHelpers.readPathStyle(style);
            const hierarchy = Cesium.Cartesian3.fromDegreesArray(latlngs.flatMap(p => [p.lng, p.lat]));
            const ent = s.viewer.entities.add({
                id: `bm-polygon-${id}-${layerId}`,
                polygon: {
                    hierarchy,
                    material: BitMapCesium._color(Cesium, st.fillColor, st.fillOpacity),
                    outline: true,
                    outlineColor: BitMapCesium._color(Cesium, st.color, st.opacity),
                },
                _bmLayerId: layerId, _bmVectorKind: 'polygon',
            });
            BitMapCesium._setLayer(s, layerId, ent, 'polygon');
        }

        public static addCircle(id: string, layerId: string, lat: number, lng: number, radiusMeters: number, style: any) {
            const s = BitMapCesium._require(id);
            const Cesium = s.Cesium;
            const st = BitMapHelpers.readPathStyle(style);
            const ent = s.viewer.entities.add({
                id: `bm-circle-${id}-${layerId}`,
                position: Cesium.Cartesian3.fromDegrees(lng, lat),
                ellipse: {
                    semiMajorAxis: radiusMeters,
                    semiMinorAxis: radiusMeters,
                    material: BitMapCesium._color(Cesium, st.fillColor, st.fillOpacity),
                    outline: true,
                    outlineColor: BitMapCesium._color(Cesium, st.color, st.opacity),
                },
                _bmLayerId: layerId, _bmVectorKind: 'circle',
            });
            BitMapCesium._setLayer(s, layerId, ent, 'circle');
        }

        public static addRectangle(id: string, layerId: string, swLat: number, swLng: number, neLat: number, neLng: number, style: any) {
            const s = BitMapCesium._require(id);
            const Cesium = s.Cesium;
            const st = BitMapHelpers.readPathStyle(style);
            const ent = s.viewer.entities.add({
                id: `bm-rect-${id}-${layerId}`,
                rectangle: {
                    coordinates: Cesium.Rectangle.fromDegrees(swLng, swLat, neLng, neLat),
                    material: BitMapCesium._color(Cesium, st.fillColor, st.fillOpacity),
                    outline: true,
                    outlineColor: BitMapCesium._color(Cesium, st.color, st.opacity),
                },
                _bmLayerId: layerId, _bmVectorKind: 'rectangle',
            });
            BitMapCesium._setLayer(s, layerId, ent, 'rectangle');
        }

        public static async addGeoJson(id: string, layerId: string, geoJsonString: string, style: any) {
            const s = BitMapCesium._require(id);
            const Cesium = s.Cesium;
            const st = BitMapHelpers.readPathStyle(style);
            let gj: any;
            try { gj = JSON.parse(geoJsonString); } catch { throw new Error('Invalid GeoJSON string'); }
            const ds = await Cesium.GeoJsonDataSource.load(gj, {
                stroke: BitMapCesium._color(Cesium, st.color, st.opacity),
                fill: BitMapCesium._color(Cesium, st.fillColor, st.fillOpacity),
                strokeWidth: st.weight,
            });
            (ds as any)._bmLayerId = layerId;
            // Tag each entity with metadata so click handler can bridge to .NET
            const entities = ds.entities.values;
            for (let i = 0; i < entities.length; i++) {
                const ent = entities[i];
                (ent as any)._bmLayerId = layerId;
                (ent as any)._bmKind = 'geojson';
            }
            const existingDs = s.geoJsonLayers[layerId];
            if (existingDs) try { s.viewer.dataSources.remove(existingDs, true); } catch { /* ignore */ }
            await s.viewer.dataSources.add(ds);
            s.geoJsonLayers[layerId] = ds;
        }

        public static removeLayer(id: string, layerId: string) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            const e = s.layers[layerId];
            if (e) { s.viewer.entities.remove(e.entity); delete s.layers[layerId]; }
            const ds = s.geoJsonLayers[layerId];
            if (ds) { try { s.viewer.dataSources.remove(ds, true); } catch { /* ignore */ } delete s.geoJsonLayers[layerId]; }
        }

        public static clearVectorLayers(id: string) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            for (const k in s.layers) s.viewer.entities.remove(s.layers[k].entity);
            s.layers = {};
            for (const k in s.geoJsonLayers) try { s.viewer.dataSources.remove(s.geoJsonLayers[k], true); } catch { /* ignore */ }
            s.geoJsonLayers = {};
        }

        public static addTileOverlay(id: string, opts: any) {
            const s = BitMapCesium._require(id);
            const Cesium = s.Cesium;
            const existingTile = s.tileOverlays[opts.id];
            if (existingTile) try { s.viewer.imageryLayers.remove(existingTile, true); } catch { /* ignore */ }
            const layer = s.viewer.imageryLayers.addImageryProvider(new Cesium.UrlTemplateImageryProvider({
                url: (opts.urlTemplate || '').replace('{s}', 'a'),
                credit: opts.attribution || '',
                maximumLevel: opts.maxZoom ?? 19,
            }));
            layer.alpha = opts.opacity ?? 1;
            s.tileOverlays[opts.id] = layer;
        }

        public static removeTileOverlay(id: string, overlayId: string) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            const layer = s.tileOverlays[overlayId];
            if (layer) { try { s.viewer.imageryLayers.remove(layer, true); } catch { /* ignore */ } delete s.tileOverlays[overlayId]; }
        }

        // ---- helpers ----

        private static _require(id: string) {
            const s = BitMapCesium._maps[id];
            if (!s) throw new Error(`BitMapCesium: unknown map id '${id}'`);
            return s;
        }

        private static _setLayer(s: any, layerId: string, entity: any, kind: string) {
            const existing = s.layers[layerId];
            if (existing) try { s.viewer.entities.remove(existing.entity); } catch { /* ignore */ }
            s.layers[layerId] = { entity, kind };
        }

        private static _color(Cesium: any, hex: string, alpha: number) {
            try {
                return Cesium.Color.fromCssColorString(hex || '#3388ff').withAlpha(alpha);
            } catch {
                return new Cesium.Color(0.2, 0.53, 1.0, alpha);
            }
        }

        private static _zoomToAltitude(zoom: number): number {
            if (zoom == null || isNaN(zoom)) return 10_000_000;
            return Math.max(10, 20_000_000 / Math.pow(2, zoom));
        }

        private static _altitudeToZoom(alt: number): number {
            if (!alt || alt <= 0) return 1;
            return Math.max(0, Math.min(21, Math.log2(20_000_000 / alt)));
        }

        private static _defaultPin(): string {
            return "data:image/svg+xml;charset=utf-8," +
                "<svg xmlns='http://www.w3.org/2000/svg' width='27' height='41' viewBox='0 0 27 41'>" +
                "<path fill='%23e53935' d='M13.5 0C6.04 0 0 6.04 0 13.5c0 10.125 13.5 27.5 13.5 27.5S27 23.625 27 13.5C27 6.04 20.96 0 13.5 0z'/>" +
                "<circle cx='13.5' cy='13.5' r='5' fill='%23fff'/></svg>";
        }

        private static _readView(s: any) {
            const Cesium = s.Cesium, viewer = s.viewer;
            const cam = viewer.camera;
            const carto = Cesium.Cartographic.fromCartesian(cam.positionWC);
            const lat = Cesium.Math.toDegrees(carto.latitude);
            const lng = Cesium.Math.toDegrees(carto.longitude);
            const altitude = carto.height;
            const zoom = BitMapCesium._altitudeToZoom(altitude);
            const rect = viewer.camera.computeViewRectangle();
            const bounds = rect ? {
                southWest: { lat: Cesium.Math.toDegrees(rect.south), lng: Cesium.Math.toDegrees(rect.west) },
                northEast: { lat: Cesium.Math.toDegrees(rect.north), lng: Cesium.Math.toDegrees(rect.east) },
            } : { southWest: { lat: 0, lng: 0 }, northEast: { lat: 0, lng: 0 } };
            return { center: { lat, lng }, zoom, bounds };
        }

        private static _notifyView(s: any) {
            if (!s.dotnetObj) return;
            queueMicrotask(() => s.dotnetObj.invokeMethodAsync('OnViewChanged', BitMapCesium._readView(s)));
        }

        private static _wireEvents(s: any) {
            const Cesium = s.Cesium, viewer = s.viewer, dn = s.dotnetObj;

            const handler = new Cesium.ScreenSpaceEventHandler(viewer.scene.canvas);
            handler.setInputAction((click: any) => {
                const picked = viewer.scene.pick(click.position);
                if (picked && picked.id) {
                    const ent = picked.id;
                    const mid = ent._bmMarkerId;
                    if (mid && s.markers[mid]) { if (dn) dn.invokeMethodAsync('OnMarkerClick', mid); return; }
                    const lid = ent._bmLayerId;
                    const kind = ent._bmVectorKind;
                    // GeoJSON feature click
                    if (lid && ent._bmKind === 'geojson' && s.geoJsonLayers[lid]) {
                        if (dn) {
                            const props = ent.properties ? ent.properties.getValue(Cesium.JulianDate.now()) : {};
                            dn.invokeMethodAsync('OnGeoJsonFeatureClick', lid, props || {});
                        }
                        return;
                    }
                    // Vector layer click
                    if (lid && s.layers[lid]) {
                        const carte = viewer.camera.pickEllipsoid(click.position, viewer.scene.globe.ellipsoid);
                        if (carte && dn) {
                            const c = Cesium.Cartographic.fromCartesian(carte);
                            dn.invokeMethodAsync('OnVectorClick', lid, kind, { lat: Cesium.Math.toDegrees(c.latitude), lng: Cesium.Math.toDegrees(c.longitude) });
                        }
                        return;
                    }
                }
                const carte = viewer.camera.pickEllipsoid(click.position, viewer.scene.globe.ellipsoid);
                if (carte && dn) {
                    const c = Cesium.Cartographic.fromCartesian(carte);
                    dn.invokeMethodAsync('OnClick', { lat: Cesium.Math.toDegrees(c.latitude), lng: Cesium.Math.toDegrees(c.longitude) });
                }
            }, Cesium.ScreenSpaceEventType.LEFT_CLICK);

            handler.setInputAction((click: any) => {
                const carte = viewer.camera.pickEllipsoid(click.position, viewer.scene.globe.ellipsoid);
                if (carte && dn) {
                    const c = Cesium.Cartographic.fromCartesian(carte);
                    dn.invokeMethodAsync('OnDoubleClick', { lat: Cesium.Math.toDegrees(c.latitude), lng: Cesium.Math.toDegrees(c.longitude) });
                }
            }, Cesium.ScreenSpaceEventType.LEFT_DOUBLE_CLICK);

            let viewTimer: any = null;
            viewer.camera.moveEnd.addEventListener(() => {
                clearTimeout(viewTimer);
                viewTimer = setTimeout(() => BitMapCesium._notifyView(s), 80);
            });
        }
    }
}
