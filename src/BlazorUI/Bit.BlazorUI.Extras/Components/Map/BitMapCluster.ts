namespace BitBlazorUI {

    type ClusterOptions = {
        /** Grid size in screen pixels. Points that land in the same cell become one cluster. */
        radius: number;
        /** Above this zoom every marker is drawn individually. */
        maxZoom: number;
        /** A cell holding fewer than this many markers is drawn as individual markers. */
        minPoints: number;
        /** Fill of the cluster bubble. */
        color: string;
        /** Colour of the count drawn inside the bubble. */
        textColor: string;
        /** Skip markers outside the viewport (padded by `radius`) entirely. */
        cullOffscreen: boolean;
        /** Upper bound on how many individual markers may be handed to the provider at once. */
        maxRenderedMarkers: number;
        /** Accessible name of a bubble, with {0} standing for the count. Localized by .NET. */
        ariaLabelFormat: string;
    };

    type SourceMarker = { id: string, payload: any };

    type ClusterState = {
        jsObjectName: string;
        options: ClusterOptions;
        markers: SourceMarker[];
        /** Members of each cluster currently on the map, so a click can zoom to fit them. */
        clusters: { [clusterId: string]: SourceMarker[] };
        /** What was last handed to the provider, so an unchanged view re-renders nothing. */
        lastSignature: string;
    };

    /**
     * Provider-agnostic marker clustering.
     *
     * None of the seven backends BitMap supports agrees on clustering: Leaflet needs a plugin,
     * the GL pair cluster inside a GeoJSON source (which does not apply to DOM markers), and the
     * rest each have their own model or none at all. Clustering once here - in screen space, from
     * the marker set the component already holds - gives every backend the same behaviour, and
     * keeps the C# API a single `Clustering` parameter rather than seven provider-specific ones.
     *
     * The result is handed back to whichever provider is active through its ordinary
     * `syncMarkers`, so cluster bubbles are just markers as far as the backend is concerned.
     */
    export class BitMapCluster {
        /** Prefix that marks a marker as a cluster bubble rather than one of the caller's markers. */
        public static readonly clusterIdPrefix = '__bitmap_cluster_';

        private static _maps: { [id: string]: ClusterState } = {};

        public static configure(id: string, jsObjectName: string, options: ClusterOptions) {
            const existing = BitMapCluster._maps[id];
            BitMapCluster._maps[id] = {
                jsObjectName,
                options,
                markers: existing?.markers ?? [],
                clusters: {},
                lastSignature: '',
            };
            BitMapCluster.render(id);
        }

        /**
         * Turns clustering off and hands the full marker set back to the provider, so the map is
         * left in the same state it would have been in had clustering never been enabled.
         */
        public static disable(id: string) {
            const s = BitMapCluster._maps[id];
            if (!s) return;
            delete BitMapCluster._maps[id];
            BitMapCluster._syncToProvider(s.jsObjectName, id, s.markers);
        }

        /** Replaces the source set. The component owns it; this layer only decides what is drawn. */
        public static setMarkers(id: string, markerIds: string[], markers: any[]) {
            const s = BitMapCluster._maps[id];
            if (!s) return;
            const length = Math.min(markerIds?.length ?? 0, markers?.length ?? 0);
            const next: SourceMarker[] = [];
            for (let i = 0; i < length; i++) {
                next.push({ id: markerIds[i], payload: markers[i] });
            }
            s.markers = next;
            // A changed source set always re-renders, even if the viewport did not move.
            s.lastSignature = '';
            BitMapCluster.render(id);
        }

        /** Recomputes the clusters for the current viewport and pushes the result to the provider. */
        public static render(id: string) {
            const s = BitMapCluster._maps[id];
            if (!s) return;

            const provider = BitMapCluster._provider(s.jsObjectName);
            if (!provider?.getView) return;

            let view: any;
            try { view = provider.getView(id); } catch { return; }
            if (!view) return;

            const zoom: number = view.zoom ?? 0;
            const bounds = view.bounds;

            const visible = s.options.cullOffscreen && bounds
                ? s.markers.filter(m => BitMapCluster._isWithin(m, bounds, zoom, s.options.radius))
                : s.markers;

            const rendered = zoom >= s.options.maxZoom
                ? BitMapCluster._capped(visible, s.options.maxRenderedMarkers)
                : BitMapCluster._cluster(visible, zoom, s.options);

            // Re-syncing identical markers would tear down and rebuild every DOM marker, losing
            // any open popup and the keyboard focus along with it.
            //
            // The count is part of the signature, not just the id: a bubble's id is its grid cell,
            // which does not change as markers enter and leave that cell during a pan - so an
            // id-only signature would leave a bubble labelled with a count it no longer stands for.
            const signature = rendered.map(m => `${m.id}:${m.members?.length ?? 0}`).join('|');
            if (signature === s.lastSignature) return;
            s.lastSignature = signature;

            s.clusters = {};
            for (const marker of rendered) {
                if (marker.members) s.clusters[marker.id] = marker.members;
            }

            BitMapCluster._syncToProvider(s.jsObjectName, id, rendered);
        }

        /** True when the id belongs to a cluster bubble this layer generated. */
        public static isClusterId(markerId: string): boolean {
            return typeof markerId === 'string' && markerId.startsWith(BitMapCluster.clusterIdPrefix);
        }

        /**
         * Zooms to fit the members of a cluster. Returns the number of markers it contained, or 0
         * when the id is unknown - which is how the caller tells a cluster click from a real one.
         */
        public static expand(id: string, clusterId: string, paddingPixels: number, zoom: boolean = true): number {
            const s = BitMapCluster._maps[id];
            const members = s?.clusters[clusterId];
            if (!s || !members || members.length === 0) return 0;

            // The count is reported either way. A consumer who handles the click themselves still
            // needs to know how big the bubble was, and reading it back separately would cost a
            // second round-trip for something already in hand.
            if (!zoom) return members.length;

            let swLat = 90, swLng = 180, neLat = -90, neLng = -180;
            for (const member of members) {
                const lat = member.payload?.lat ?? 0;
                const lng = member.payload?.lng ?? 0;
                if (lat < swLat) swLat = lat;
                if (lat > neLat) neLat = lat;
                if (lng < swLng) swLng = lng;
                if (lng > neLng) neLng = lng;
            }

            const provider = BitMapCluster._provider(s.jsObjectName);
            try {
                // Every marker sharing one coordinate gives a zero-area box, which fitBounds
                // resolves to the maximum zoom. Nudge it open so the view lands somewhere useful.
                const epsilon = 1e-4;
                if (neLat - swLat < epsilon) { swLat -= epsilon; neLat += epsilon; }
                if (neLng - swLng < epsilon) { swLng -= epsilon; neLng += epsilon; }
                provider?.fitBounds?.(id, swLat, swLng, neLat, neLng, paddingPixels);
            } catch { /* ignore */ }

            return members.length;
        }

        // ---- helpers ----

        private static _provider(jsObjectName: string): any {
            return (globalThis as any).BitBlazorUI?.[jsObjectName];
        }

        private static _syncToProvider(jsObjectName: string, id: string, markers: SourceMarker[]) {
            const provider = BitMapCluster._provider(jsObjectName);
            if (!provider?.syncMarkers) return;
            try {
                provider.syncMarkers(id, markers.map(m => m.id), markers.map(m => m.payload));
            } catch { /* ignore - a provider that rejects the batch keeps its previous markers */ }
        }

        /**
         * Web Mercator projection to pixel space at a given zoom. Clustering has to happen in
         * screen space, not in degrees: a fixed degree radius covers wildly different distances
         * at the equator and near the poles, so it would over-cluster at high latitudes.
         */
        private static _project(lat: number, lng: number, zoom: number): [number, number] {
            const scale = 256 * Math.pow(2, zoom);
            const x = ((lng + 180) / 360) * scale;
            // Clamp before the log so a marker at a pole does not project to infinity.
            const siny = Math.min(Math.max(Math.sin((lat * Math.PI) / 180), -0.9999), 0.9999);
            const y = (0.5 - Math.log((1 + siny) / (1 - siny)) / (4 * Math.PI)) * scale;
            return [x, y];
        }

        private static _isWithin(marker: SourceMarker, bounds: any, zoom: number, radius: number): boolean {
            const lat = marker.payload?.lat;
            const lng = marker.payload?.lng;
            if (typeof lat !== 'number' || typeof lng !== 'number') return true;

            // Pad by the cluster radius so a marker just off-screen still merges with an on-screen
            // one; otherwise a cluster's count would change as it crossed the edge of the viewport.
            const padDegreesLat = (radius / (256 * Math.pow(2, zoom))) * 180;
            const south = Math.min(bounds.southWest?.lat ?? -90, bounds.northEast?.lat ?? 90) - padDegreesLat;
            const north = Math.max(bounds.southWest?.lat ?? -90, bounds.northEast?.lat ?? 90) + padDegreesLat;
            if (lat < south || lat > north) return false;

            const west = bounds.southWest?.lng ?? -180;
            const east = bounds.northEast?.lng ?? 180;
            const padDegreesLng = padDegreesLat;
            if (west <= east) {
                return lng >= west - padDegreesLng && lng <= east + padDegreesLng;
            }
            // The viewport crosses the antimeridian, so the accepted range is the union of the
            // two halves rather than the span between them.
            return lng >= west - padDegreesLng || lng <= east + padDegreesLng;
        }

        private static _capped(markers: SourceMarker[], max: number): any[] {
            return markers.length <= max ? markers : markers.slice(0, max);
        }

        private static _cluster(markers: SourceMarker[], zoom: number, options: ClusterOptions): any[] {
            const cells: { [key: string]: SourceMarker[] } = {};
            const radius = Math.max(1, options.radius);

            for (const marker of markers) {
                const lat = marker.payload?.lat;
                const lng = marker.payload?.lng;
                if (typeof lat !== 'number' || typeof lng !== 'number') continue;
                const [x, y] = BitMapCluster._project(lat, lng, zoom);
                const key = `${Math.floor(x / radius)}:${Math.floor(y / radius)}`;
                (cells[key] ??= []).push(marker);
            }

            const result: any[] = [];
            for (const key of Object.keys(cells)) {
                const members = cells[key];
                if (members.length < Math.max(2, options.minPoints)) {
                    for (const member of members) result.push(member);
                    continue;
                }

                let latSum = 0, lngSum = 0;
                for (const member of members) {
                    latSum += member.payload.lat;
                    lngSum += member.payload.lng;
                }
                const count = members.length;
                const lat = latSum / count;
                const lng = lngSum / count;
                const size = BitMapCluster._bubbleSize(count);

                result.push({
                    id: `${BitMapCluster.clusterIdPrefix}${key}`,
                    members,
                    payload: {
                        lat, lng,
                        // The count is the accessible name as well as the label: a bubble that
                        // announces as "marker" tells a screen-reader user nothing. The wording
                        // comes from .NET, where it can be translated with every other label.
                        title: `${count}`,
                        alt: BitMapCluster._bubbleLabel(count, options),
                        focusable: true,
                        draggable: false,
                        iconUrl: BitMapCluster._bubbleIcon(count, size, options),
                        iconWidth: size,
                        iconHeight: size,
                        // A bubble is a disc, not a pin: the coordinate it stands for is at its
                        // centre, so it is anchored there rather than at the bottom edge every
                        // pin-shaped icon defaults to.
                        iconAnchorX: Math.round(size / 2),
                        iconAnchorY: Math.round(size / 2),
                        opacity: 1,
                    },
                });
            }

            return result;
        }

        /** The bubble's accessible name, from the format .NET supplied. */
        private static _bubbleLabel(count: number, options: ClusterOptions): string {
            const format = typeof options.ariaLabelFormat === 'string' && options.ariaLabelFormat.length > 0
                ? options.ariaLabelFormat
                : 'Cluster of {0} markers';
            return format.split('{0}').join(`${count}`);
        }

        /** Bubble diameter grows with the log of the count, so 10 and 10,000 stay distinguishable. */
        private static _bubbleSize(count: number): number {
            return Math.round(Math.min(64, 28 + Math.log10(Math.max(1, count)) * 14));
        }

        /**
         * Draws the bubble as an inline SVG data URI rather than a DOM element, because that is
         * the one icon mechanism all seven providers already accept (`iconUrl`).
         */
        private static _bubbleIcon(count: number, size: number, options: ClusterOptions): string {
            const label = count < 1000 ? `${count}` : `${Math.floor(count / 1000)}k+`;
            const half = size / 2;
            const fontSize = Math.max(10, Math.round(size / 2.8));
            const svg =
                `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">` +
                `<circle cx="${half}" cy="${half}" r="${half - 2}" fill="${options.color}" fill-opacity="0.85" stroke="#ffffff" stroke-width="2"/>` +
                `<text x="${half}" y="${half}" fill="${options.textColor}" font-family="sans-serif" font-size="${fontSize}" ` +
                `font-weight="600" text-anchor="middle" dominant-baseline="central">${label}</text>` +
                `</svg>`;
            // encodeURIComponent rather than btoa: the colours come from .NET and may be any CSS
            // colour, including non-Latin-1 characters that btoa refuses.
            return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
        }
    }
}
