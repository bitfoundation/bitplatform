namespace BitBlazorUI {

    type ClusterOptions = {
        /** Screen distance, in pixels, within which markers are gathered into one cluster. */
        radius: number;
        /** Above this zoom every marker is drawn individually. */
        maxZoom: number;
        /** A cell holding fewer than this many markers is drawn as individual markers. */
        minPoints: number;
        /** Fill of the cluster bubble. Null takes the theme's, off the map's cluster probe. */
        color: string | null;
        /** Colour of the count drawn inside the bubble. Null takes the theme's, off the map's cluster probe. */
        textColor: string | null;
        /** Skip markers outside the viewport (padded by `radius`) entirely. */
        cullOffscreen: boolean;
        /** Upper bound on how many individual markers may be handed to the provider at once. */
        maxRenderedMarkers: number;
        /** Accessible name of a bubble, with {0} standing for the count. Localized by .NET. */
        ariaLabelFormat: string;
    };

    type SourceMarker = { id: string, payload: any };

    /** The colours a bubble is drawn in, resolved to plain CSS colours an image can be painted with. */
    type BubbleColors = { fill: string, text: string, ring: string };

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

        /**
         * Drops the clustering state without handing anything back to the provider. Teardown
         * only: the map itself is destroyed straight afterwards, so re-syncing the whole
         * unclustered set into it first would draw every marker for nothing.
         */
        public static discard(id: string) {
            delete BitMapCluster._maps[id];
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

            const colors = BitMapCluster._colors(id, s.options);

            const rendered = zoom >= s.options.maxZoom
                ? BitMapCluster._capped(visible, s.options.maxRenderedMarkers)
                : BitMapCluster._cluster(visible, zoom, s.options, colors);

            // Re-syncing identical markers would tear down and rebuild every DOM marker, losing
            // any open popup and the keyboard focus along with it.
            //
            // The count and the centroid are part of the signature, not just the id: a bubble's id
            // is the marker that seeded it, which does not change as others join and leave it during a
            // pan - so an id-only signature would leave a bubble labelled with a count it no longer
            // stands for, and a count-only one would leave it at the centroid of a membership it has
            // since swapped (one marker out, another in, the count unchanged).
            //
            // The colours lead it too, so a theme or scheme switched since the last render repaints
            // the bubbles on the next settled view rather than leaving them in the old palette.
            const signature = `${colors.fill};${colors.text};${colors.ring}|` + rendered
                .map(m => `${m.id}:${m.members?.length ?? 0}:${m.payload?.lat ?? ''},${m.payload?.lng ?? ''}`)
                .join('|');
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
        public static expand(id: string, clusterId: string, paddingPixels: number, zoom: boolean = true, animate: boolean = true): number {
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
                provider?.fitBounds?.(id, swLat, swLng, neLat, neLng, paddingPixels, undefined, animate);
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

        /**
         * The bubble's colours: the ones .NET was given, or else the theme's, read off the probe the
         * component renders inside the map. A stylesheet cannot reach into an image, so this is how the
         * --bit-Map-cluster-* variables - and the tokens they fall back to - get into the bubble.
         * Computed colours come back as rgb(), which an SVG image paints as reliably as any literal.
         */
        private static _colors(id: string, options: ClusterOptions): BubbleColors {
            let fill = '#3388ff', text = '#ffffff', ring = '#ffffff';
            const probe = BitMapHelpers.probe(id);
            if (probe) {
                try {
                    const style = getComputedStyle(probe);
                    fill = style.backgroundColor || fill;
                    text = style.color || text;
                    ring = style.borderTopColor || ring;
                } catch { /* keep the defaults */ }
            }
            // .NET's colors are resolved too: an image is painted in plain colors, so a theme variable
            // such as var(--bit-clr-sec) has to be computed before it can reach one.
            return {
                fill: BitMapHelpers.resolveColor(id, options.color) ?? fill,
                text: BitMapHelpers.resolveColor(id, options.textColor) ?? text,
                ring,
            };
        }

        /**
         * Greedy distance clustering, the approach of Leaflet.markercluster and supercluster: each marker
         * not yet taken gathers every untaken marker within the radius of it. Cutting the screen into a
         * fixed grid instead splits a dense spot along the cell lines into several bubbles drawn on top of
         * one another. A grid of radius-sized cells is still used, but only as the index that keeps the
         * neighbour search to the 3x3 cells around a marker.
         *
         * The markers are visited in their own order, so a bubble is seeded - and named - by the same
         * marker from one render to the next, which keeps its id stable while the map pans.
         */
        private static _cluster(markers: SourceMarker[], zoom: number, options: ClusterOptions, colors: BubbleColors): any[] {
            const radius = Math.max(1, options.radius);
            const minPoints = Math.max(2, options.minPoints);

            const points: { marker: SourceMarker, x: number, y: number, taken: boolean }[] = [];
            const index: { [cell: string]: number[] } = {};
            for (const marker of markers) {
                const lat = marker.payload?.lat;
                const lng = marker.payload?.lng;
                if (typeof lat !== 'number' || typeof lng !== 'number') continue;
                const [x, y] = BitMapCluster._project(lat, lng, zoom);
                (index[`${Math.floor(x / radius)}:${Math.floor(y / radius)}`] ??= []).push(points.length);
                points.push({ marker, x, y, taken: false });
            }

            const result: any[] = [];
            for (const seed of points) {
                if (seed.taken) continue;

                const near: typeof points = [];
                const cx = Math.floor(seed.x / radius), cy = Math.floor(seed.y / radius);
                for (let dx = -1; dx <= 1; dx++) {
                    for (let dy = -1; dy <= 1; dy++) {
                        for (const i of index[`${cx + dx}:${cy + dy}`] ?? []) {
                            const p = points[i];
                            if (!p.taken && Math.hypot(p.x - seed.x, p.y - seed.y) <= radius) near.push(p);
                        }
                    }
                }

                // Too few to be worth a bubble: the seed is drawn as itself, and its neighbours stay free
                // to join a bubble seeded by someone else.
                if (near.length < minPoints) {
                    seed.taken = true;
                    result.push(seed.marker);
                    continue;
                }

                for (const p of near) p.taken = true;
                const members = near.map(p => p.marker);
                const key = seed.marker.id;

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
                        iconUrl: BitMapCluster._bubbleIcon(count, size, colors),
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
        private static _bubbleIcon(count: number, size: number, colors: BubbleColors): string {
            const label = count < 1000 ? `${count}` : `${Math.floor(count / 1000)}k+`;
            const half = size / 2;
            const fontSize = Math.max(10, Math.round(size / 2.8));
            const svg =
                `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">` +
                `<circle cx="${half}" cy="${half}" r="${half - 2}" fill="${colors.fill}" fill-opacity="0.85" stroke="${colors.ring}" stroke-width="2"/>` +
                `<text x="${half}" y="${half}" fill="${colors.text}" font-family="sans-serif" font-size="${fontSize}" ` +
                `font-weight="600" text-anchor="middle" dominant-baseline="central">${label}</text>` +
                `</svg>`;
            // URI-encoded rather than btoa, which refuses non-Latin-1 characters, and safe inside an
            // unquoted CSS url() too - which is how the GL providers paint a custom icon.
            return BitMapHelpers.svgDataUri(svg);
        }
    }
}
