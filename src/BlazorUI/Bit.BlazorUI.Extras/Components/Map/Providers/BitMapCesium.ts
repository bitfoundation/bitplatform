namespace BitBlazorUI {

    /**
     * CesiumJS provider - 3D globe. Loads CesiumJS from the official CDN and drives a Viewer.
     * Markers and vector layers are added as Entities on the Viewer's main entity collection.
     */
    export class BitMapCesium {
        private static _maps: { [id: string]: {
            Cesium: any, viewer: any, dotnetObj: DotNetObject | null | undefined,
            markers: { [k: string]: any },
            layers: { [k: string]: { entity: any, kind: string } },
            geoJsonLayers: { [k: string]: any },   // DataSource refs
            tileOverlays: { [k: string]: any },
            _cesiumHandler: any,
            _viewTimer: any,
            _moveEndCallback: any,
            _baseImageryLayer: any,
            ionAccessToken: string | undefined,
            imageryStyle: string | undefined,
            terrainEnabled: boolean,
            sceneMode: string | undefined,
            shadowsEnabled: boolean,
        } } = {};

        public static async init(id: string, canvasId: string, element: HTMLElement, dotnetObj: DotNetObject | null | undefined, options: any) {
            element = await BitMapHelpers.resolveMapCanvas(canvasId, element);
            await BitMapHelpers.waitForGlobal('Cesium', () => typeof (globalThis as any).Cesium?.Viewer === 'function');
            const Cesium = (globalThis as any).Cesium;
            const o = options || {};

            // Per-instance Ion access token. We deliberately do NOT mutate
            // Cesium.Ion.defaultAccessToken: that is process-global state shared by
            // every Viewer in the page, so a token set for one BitMap instance would
            // leak into unrelated viewers (and a later Viewer with a different token
            // would silently overwrite an earlier one). Pass o.ionAccessToken to each
            // Ion resource we construct instead.
            const ionAccessToken: string | undefined = o.ionAccessToken || undefined;

            // Cesium 1.104+ deprecated imageryProvider in Viewer constructor;
            // use baseLayer instead. createWorldTerrain() is also deprecated in
            // favor of createWorldTerrainAsync().
            const baseLayer = await (async () => {
                if ((o.imageryStyle === 'bing_aerial' || o.imageryStyle === 'bing_labels') && ionAccessToken) {
                    // Use Cesium's Ion-based Bing imagery. bing_aerial uses Ion asset 2
                    // (Bing Maps Aerial). bing_labels uses Ion asset 3 (Bing Maps
                    // Aerial with Labels).
                    const assetId = o.imageryStyle === 'bing_labels' ? 3 : 2;
                    if (Cesium.IonImageryProvider) {
                        const provider = await (Cesium.IonImageryProvider.fromAssetId
                            ? Cesium.IonImageryProvider.fromAssetId(assetId, { accessToken: ionAccessToken })
                            : new Cesium.IonImageryProvider({ assetId, accessToken: ionAccessToken }));
                        return Cesium.ImageryLayer
                            ? new Cesium.ImageryLayer(provider)
                            : provider;
                    }
                    return undefined;
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

            // Terrain: use async API (Cesium 1.104+). Build Ion world terrain
            // directly via CesiumTerrainProvider.fromIonAssetId so we can pass the
            // per-instance accessToken without relying on Cesium.Ion.defaultAccessToken.
            let terrainProvider: any;
            if (o.terrainEnabled && ionAccessToken) {
                if (Cesium.CesiumTerrainProvider?.fromIonAssetId) {
                    // Ion asset 1 = Cesium World Terrain
                    terrainProvider = await Cesium.CesiumTerrainProvider.fromIonAssetId(1, { accessToken: ionAccessToken });
                } else if (Cesium.createWorldTerrainAsync) {
                    terrainProvider = await Cesium.createWorldTerrainAsync();
                } else {
                    terrainProvider = Cesium.createWorldTerrain();
                }
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
                _cesiumHandler: null as any, _viewTimer: null as any, _moveEndCallback: null as any,
                _baseImageryLayer: null as any,
                ionAccessToken,
                imageryStyle: o.imageryStyle,
                terrainEnabled: !!o.terrainEnabled,
                sceneMode: o.sceneMode,
                shadowsEnabled: !!o.shadowsEnabled,
            };
            // Capture the base imagery layer (when present) so subsequent
            // _applyImagery() calls remove/replace exactly this layer instead of
            // unconditionally dropping imageryLayers.get(0), which could be a
            // user-added overlay if addTileOverlay was called before sync.
            try {
                const initialLayers = viewer?.imageryLayers;
                if (initialLayers && initialLayers.length > 0) {
                    state._baseImageryLayer = initialLayers.get(0);
                }
            } catch { /* ignore */ }
            BitMapCesium._wireEvents(state);
            BitMapCesium._maps[id] = state;
            BitMapCesium._notifyView(state);
        }

        public static sync(id: string, options: any) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            const Cesium = s.Cesium;
            const o = options || {};

            // ---- ion access token rotation ----
            // Detect token rotation BEFORE the imagery/terrain branches so that
            // Ion-backed resources (Bing imagery, world terrain) are recreated with
            // the new token even when neither imageryStyle nor terrainEnabled changed.
            // We compare against the stored token first, then update s.ionAccessToken
            // so the recreate paths below see the fresh value.
            const ionTokenChanged = Object.prototype.hasOwnProperty.call(o, 'ionAccessToken')
                && o.ionAccessToken !== s.ionAccessToken;
            if (ionTokenChanged) {
                s.ionAccessToken = o.ionAccessToken;
            }

            // ---- imagery ----
            // Replace the base imagery layer when o.imageryStyle is provided and
            // differs from the currently-applied style. We keep the logic in step
            // with init: bing_* uses an Ion provider (requires ionAccessToken); 'none'
            // disables imagery; anything else falls back to OSM tiles.
            // Also re-apply when only the Ion token rotated and the current style
            // depends on it (Bing imagery via Ion).
            const imageryStyleProvided = Object.prototype.hasOwnProperty.call(o, 'imageryStyle');
            const imageryStyleChanged = imageryStyleProvided && o.imageryStyle !== s.imageryStyle;
            const imageryUsesIon = (style: string | undefined) => style === 'bing_aerial' || style === 'bing_labels';
            if (imageryStyleChanged || (ionTokenChanged && imageryUsesIon(s.imageryStyle))) {
                const targetStyle = imageryStyleProvided ? o.imageryStyle : s.imageryStyle;
                BitMapCesium._applyImagery(s, targetStyle).catch(() => { /* ignore */ });
                s.imageryStyle = targetStyle;
            }

            // ---- terrain ----
            // Recreate the terrain provider when terrainEnabled flipped, or when the
            // Ion token rotated and terrain is currently enabled (so the Ion-backed
            // CesiumTerrainProvider is rebuilt with the fresh token).
            const terrainProvided = Object.prototype.hasOwnProperty.call(o, 'terrainEnabled');
            const terrainFlipped = terrainProvided && !!o.terrainEnabled !== s.terrainEnabled;
            if (terrainFlipped || (ionTokenChanged && s.terrainEnabled)) {
                const enabled = terrainProvided ? !!o.terrainEnabled : s.terrainEnabled;
                if (enabled && s.ionAccessToken && Cesium.CesiumTerrainProvider?.fromIonAssetId) {
                    Cesium.CesiumTerrainProvider.fromIonAssetId(1, { accessToken: s.ionAccessToken })
                        .then((tp: any) => { try { s.viewer.terrainProvider = tp; } catch { /* ignore */ } })
                        .catch(() => { /* ignore */ });
                } else if (enabled && Cesium.createWorldTerrainAsync) {
                    Cesium.createWorldTerrainAsync()
                        .then((tp: any) => { try { s.viewer.terrainProvider = tp; } catch { /* ignore */ } })
                        .catch(() => { /* ignore */ });
                } else {
                    try { s.viewer.terrainProvider = new Cesium.EllipsoidTerrainProvider(); } catch { /* ignore */ }
                }
                s.terrainEnabled = enabled;
            }

            // ---- scene mode ----
            if (Object.prototype.hasOwnProperty.call(o, 'sceneMode') && o.sceneMode !== s.sceneMode) {
                try {
                    if (o.sceneMode === 'scene2d') s.viewer.scene.morphTo2D(0);
                    else if (o.sceneMode === 'columbus') s.viewer.scene.morphToColumbusView(0);
                    else s.viewer.scene.morphTo3D(0);
                } catch { /* ignore */ }
                s.sceneMode = o.sceneMode;
            }

            // ---- shadows ----
            if (Object.prototype.hasOwnProperty.call(o, 'shadowsEnabled') && !!o.shadowsEnabled !== s.shadowsEnabled) {
                const v = !!o.shadowsEnabled;
                try { s.viewer.shadows = v; } catch { /* ignore */ }
                try { s.viewer.scene.shadowMap.enabled = v; } catch { /* ignore */ }
                s.shadowsEnabled = v;
            }

            // ---- camera ----
            let lat: number, lng: number, altitude: number;

            const currentCartographic = Cesium.Cartographic.fromCartesian(s.viewer.camera.position);

            if (o.center !== undefined && o.center !== null) {
                lat = o.center.lat;
                lng = o.center.lng;
            } else if (currentCartographic) {
                lat = Cesium.Math.toDegrees(currentCartographic.latitude);
                lng = Cesium.Math.toDegrees(currentCartographic.longitude);
            } else {
                lat = 51.505;
                lng = -0.09;
            }

            if (o.altitude !== undefined && o.altitude !== null) {
                altitude = o.altitude;
            } else if (o.zoom !== undefined && o.zoom !== null) {
                altitude = BitMapCesium._zoomToAltitude(o.zoom);
            } else if (currentCartographic) {
                altitude = currentCartographic.height;
            } else {
                altitude = BitMapCesium._zoomToAltitude(4);
            }

            s.viewer.camera.flyTo({
                destination: Cesium.Cartesian3.fromDegrees(lng, lat, altitude),
                duration: 0,
            });
        }

        private static async _applyImagery(s: any, imageryStyle: string | undefined) {
            const Cesium = s.Cesium;
            const layers = s.viewer.imageryLayers;
            if (!layers) return;

            const buildOsmLayer = () => {
                const osmProvider = new Cesium.UrlTemplateImageryProvider({
                    url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
                    subdomains: ['a', 'b', 'c'],
                    credit: '© OpenStreetMap contributors',
                    maximumLevel: 19,
                });
                return Cesium.ImageryLayer ? new Cesium.ImageryLayer(osmProvider) : null;
            };

            // Remove only the tracked base imagery layer, not blindly index 0:
            // user-added overlays may sit at index 0 if the original base layer
            // was disabled (imageryStyle === 'none' on init).
            try {
                if (s._baseImageryLayer && layers.contains?.(s._baseImageryLayer) !== false) {
                    layers.remove(s._baseImageryLayer, true);
                }
            } catch { /* ignore */ }
            s._baseImageryLayer = null;

            if (imageryStyle === 'none') {
                return;
            }

            if ((imageryStyle === 'bing_aerial' || imageryStyle === 'bing_labels') && s.ionAccessToken && Cesium.IonImageryProvider) {
                const assetId = imageryStyle === 'bing_labels' ? 3 : 2;
                try {
                    const provider = await (Cesium.IonImageryProvider.fromAssetId
                        ? Cesium.IonImageryProvider.fromAssetId(assetId, { accessToken: s.ionAccessToken })
                        : new Cesium.IonImageryProvider({ assetId, accessToken: s.ionAccessToken }));
                    const lyr = Cesium.ImageryLayer ? new Cesium.ImageryLayer(provider) : provider;
                    if (lyr) {
                        try { layers.add(lyr, 0); } catch { layers.add(lyr); }
                        s._baseImageryLayer = lyr;
                    }
                    return;
                } catch { /* fall through to OSM */ }
            }

            const osm = buildOsmLayer();
            if (osm) {
                try { layers.add(osm, 0); } catch { layers.add(osm); }
                s._baseImageryLayer = osm;
            }
        }

        public static dispose(id: string) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            if (s._cesiumHandler) { try { s._cesiumHandler.destroy(); } catch { /* ignore */ } s._cesiumHandler = null; }
            if (s._viewTimer) { clearTimeout(s._viewTimer); s._viewTimer = null; }
            if (s._moveEndCallback) { try { s.viewer.camera.moveEnd.removeEventListener(s._moveEndCallback); } catch { /* ignore */ } s._moveEndCallback = null; }
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
            const altitude = zoom != null
                ? BitMapCesium._zoomToAltitude(zoom)
                : s.viewer.camera.positionCartographic.height;
            s.viewer.camera.setView({ destination: s.Cesium.Cartesian3.fromDegrees(lng, lat, altitude) });
        }

        public static flyTo(id: string, lat: number, lng: number, zoom: number | null) {
            const s = BitMapCesium._require(id);
            const altitude = zoom != null
                ? BitMapCesium._zoomToAltitude(zoom)
                : s.viewer.camera.positionCartographic.height;
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
            // Cesium provider does not implement draggable markers yet.
            // Warn loudly so callers don't think Draggable=true is silently honored.
            // TODO: implement drag handling in _wireEvents (LEFT_DOWN/MOUSE_MOVE/LEFT_UP on
            // a picked marker entity) and emit dotnetObj.invokeMethodAsync('OnMarkerDragEnd', markerId, { lat, lng })
            // when the drag ends, mirroring the pattern in BitMapAzureMaps/BitMapMapLibre.
            if (opts && opts.draggable === true) {
                console.warn(`BitMapCesium: Draggable markers are not supported by the Cesium provider; marker '${markerId}' will be added as non-draggable.`);
            }
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
            // description is rendered as HTML in Cesium's InfoBox.
            // popupText is escaped to prevent XSS; popupHtml is passed raw (caller's responsibility).
            let description: string | undefined;
            if (opts.popupHtml) {
                description = opts.popupHtml;
            } else if (opts.popupText) {
                description = String(opts.popupText).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
            }
            const ent = s.viewer.entities.add({
                id: `bm-marker-${id}-${markerId}`,
                position: Cesium.Cartesian3.fromDegrees(opts.lng, opts.lat),
                billboard,
                label: opts.title ? { text: opts.title, font: '12px sans-serif', pixelOffset: new Cesium.Cartesian2(0, -50) } : undefined,
                description: description,
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

        public static syncMarkers(id: string, markerIds: string[], markers: any[]) {
            const s = BitMapCesium._maps[id];
            if (!s) return;
            for (const k in s.markers) s.viewer.entities.remove(s.markers[k]);
            s.markers = {};
            const len = Math.min(markerIds?.length ?? 0, markers?.length ?? 0);
            for (let i = 0; i < len; i++) BitMapCesium.addMarker(id, markerIds[i], markers[i]);
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
            const entId = `bm-poly-${id}-${layerId}`;
            BitMapCesium._removeEntityById(s, entId);
            const ent = s.viewer.entities.add({
                id: entId,
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
            const entId = `bm-polygon-${id}-${layerId}`;
            BitMapCesium._removeEntityById(s, entId);
            const ent = s.viewer.entities.add({
                id: entId,
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
            const entId = `bm-circle-${id}-${layerId}`;
            BitMapCesium._removeEntityById(s, entId);
            const ent = s.viewer.entities.add({
                id: entId,
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
            const entId = `bm-rect-${id}-${layerId}`;
            BitMapCesium._removeEntityById(s, entId);
            const ent = s.viewer.entities.add({
                id: entId,
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
            // The same layerId may previously have been used for a non-GeoJSON entity
            // (polyline/polygon/circle/rectangle) stored in s.layers. Drop that prior
            // representation too so reusing a layerId across types doesn't leave the
            // old entity on the viewer alongside the new GeoJSON dataSource.
            const priorEntity = s.layers[layerId];
            if (priorEntity) { try { s.viewer.entities.remove(priorEntity.entity); } catch { /* ignore */ } delete s.layers[layerId]; }
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
            // The same layerId may previously have been used for a GeoJSON dataSource
            // stored in s.geoJsonLayers. Drop that prior representation too so reusing a
            // layerId across types doesn't leave the old GeoJSON content rendered
            // alongside the new entity.
            const existingDs = s.geoJsonLayers[layerId];
            if (existingDs) { try { s.viewer.dataSources.remove(existingDs, true); } catch { /* ignore */ } delete s.geoJsonLayers[layerId]; }
            s.layers[layerId] = { entity, kind };
        }

        private static _removeEntityById(s: any, entId: string) {
            try {
                const ents = s.viewer.entities;
                if (typeof ents.removeById === 'function') {
                    ents.removeById(entId);
                    return;
                }
                const existing = ents.getById ? ents.getById(entId) : null;
                if (existing) ents.remove(existing);
            } catch { /* ignore */ }
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
            // Capture the view snapshot synchronously. dispose() can destroy s.viewer
            // and null s.dotnetObj before this microtask runs, so deferring _readView
            // would risk reading from a destroyed Cesium Viewer. We snapshot the
            // dotnet handle and the view payload here while everything is still alive.
            const dotnet = s.dotnetObj;
            const view = BitMapCesium._readView(s);
            queueMicrotask(() => {
                // Re-check the captured handle is still the live one. dispose() runs
                // synchronously between this scheduling point and the microtask draining,
                // and it nulls s.dotnetObj - so if the identity no longer matches the
                // .NET handle has been released and invokeMethodAsync would target a
                // disposed reference. Wrap in try/catch as a final safety net for the
                // race where dispose() runs after the identity check but before the
                // interop call resolves.
                if (s.dotnetObj !== dotnet) return;
                try {
                    dotnet.invokeMethodAsync('OnViewChanged', view);
                } catch { /* ignore - handle was disposed mid-flight */ }
            });
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

            const moveEndCallback = () => {
                clearTimeout(s._viewTimer);
                s._viewTimer = setTimeout(() => BitMapCesium._notifyView(s), 80);
            };
            viewer.camera.moveEnd.addEventListener(moveEndCallback);

            s._cesiumHandler = handler;
            s._viewTimer = null;
            s._moveEndCallback = moveEndCallback;
        }
    }
}
