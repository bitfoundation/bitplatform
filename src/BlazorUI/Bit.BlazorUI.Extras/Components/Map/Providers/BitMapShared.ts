namespace BitBlazorUI {

    export type BitMapLL = { lat: number, lng: number };
    export type BitMapBounds = { southWest: BitMapLL, northEast: BitMapLL };

    /** Helpers shared by every BitMap provider implementation. */
    export class BitMapHelpers {
        private static _tileCrossOrigin: string | undefined = undefined;

        /**
         * crossOrigin value to create tile layers with, or undefined for the default no-cors mode.
         *
         * Tiles start out in no-cors mode so tile servers that send neither CORS nor CORP headers
         * keep working (requesting those in CORS mode would block them). When the page is
         * cross-origin isolated with Cross-Origin-Embedder-Policy: require-corp - the only COEP
         * value WebKit understands, and what the multi-threaded WebAssembly runtime needs there -
         * a cross-origin tile without a Cross-Origin-Resource-Policy header is blocked in no-cors
         * mode instead, so the first tile layer that fails to load a single tile flips this to
         * 'anonymous' and redraws itself in CORS mode (OSM, Carto, OpenTopoMap... all send
         * Access-Control-Allow-Origin: *). Mirrors the no-cors then CORS retry the Extras/Legacy
         * script and stylesheet loaders do.
         */
        static get tileCrossOrigin(): string | undefined {
            return BitMapHelpers._tileCrossOrigin;
        }

        /** True when the tile url template points at an origin other than the page's. */
        static isCrossOriginTileUrl(urlTemplate: string): boolean {
            try {
                // {s} is Leaflet's subdomain placeholder and can sit in the host, so it has to be
                // substituted before the URL parses; every other placeholder is in the path.
                const url = (urlTemplate || '').replace('{s}', 'a');
                return new URL(url, document.baseURI).origin !== location.origin;
            } catch {
                return false;
            }
        }

        /**
         * Creates the tile-load listeners implementing the no-cors then CORS retry for a single
         * tile layer. `retry` is invoked at most once, and only when every tile of that layer
         * failed: a layer that loaded at least one tile is talking to a reachable server, so a
         * later error is an ordinary missing/failing tile and must not switch the whole page to
         * CORS mode (which would break tile servers that send no CORS headers).
         */
        static createTileCorsFallback(urlTemplate: string, retry: () => void) {
            // A layer created once the page already switched to CORS mode has nothing to retry.
            const enabled = !BitMapHelpers._tileCrossOrigin
                && self.crossOriginIsolated
                && BitMapHelpers.isCrossOriginTileUrl(urlTemplate);
            let loaded = false;
            let retried = false;
            return {
                onTileLoad: () => { loaded = true; },
                onTileError: () => {
                    if (enabled === false || loaded || retried) return;
                    retried = true;
                    BitMapHelpers._tileCrossOrigin = 'anonymous';
                    retry();
                },
            };
        }

        /** Convert a CSS hex color + alpha (0..1) to an rgba() string. */
        static hexToRgba(hex: string | undefined, alpha: number): string {
            if (!hex || typeof hex !== 'string') return `rgba(51,136,255,${alpha})`;
            let h = hex.replace('#', '');
            if (h.length === 3) h = h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
            const n = parseInt(h, 16);
            if (Number.isNaN(n)) return `rgba(51,136,255,${alpha})`;
            return `rgba(${(n >> 16) & 255},${(n >> 8) & 255},${n & 255},${alpha})`;
        }

        /** Default stroke + fill payload used when style is null. */
        static defaultPathStyle() {
            return {
                color: '#3388ff',
                weight: 3,
                opacity: 1,
                fillColor: '#3388ff',
                fillOpacity: 0.2,
                dashArray: undefined as string | undefined,
            };
        }

        /** Normalize a path style object so each field has a value. */
        static readPathStyle(style: any) {
            const d = BitMapHelpers.defaultPathStyle();
            if (!style) return d;
            return {
                color: style.color ?? d.color,
                weight: style.weight ?? d.weight,
                opacity: style.opacity ?? d.opacity,
                fillColor: style.fillColor ?? style.color ?? d.fillColor,
                fillOpacity: style.fillOpacity ?? d.fillOpacity,
                dashArray: style.dashArray ?? d.dashArray,
            };
        }

        /** Approximate a circle as a closed polygon ring of [lng,lat] coords (geographic). */
        static circleRingLngLat(lat: number, lng: number, radiusMeters: number, points = 64): [number, number][] {
            // Guard against non-finite or absurdly large inputs that would make the loop
            // either run forever (e.g. Infinity) or produce a ring big enough to OOM the
            // tab. Cap to a generous upper bound; 4096 segments is far more than any
            // visual use case needs and still bounded.
            if (!Number.isFinite(points)) {
                points = 64;
            }
            points = Math.max(1, Math.min(4096, Math.floor(points)));
            const R = 6371000;
            const ring: [number, number][] = [];
            const lat1 = (lat * Math.PI) / 180;
            const lng1 = (lng * Math.PI) / 180;
            const angular = radiusMeters / R;
            for (let i = 0; i <= points; i++) {
                const bearing = (i / points) * 2 * Math.PI;
                const lat2 = Math.asin(
                    Math.sin(lat1) * Math.cos(angular) + Math.cos(lat1) * Math.sin(angular) * Math.cos(bearing)
                );
                const lng2 =
                    lng1 +
                    Math.atan2(
                        Math.sin(bearing) * Math.sin(angular) * Math.cos(lat1),
                        Math.cos(angular) - Math.sin(lat1) * Math.sin(lat2)
                    );
                ring.push([(lng2 * 180) / Math.PI, (lat2 * 180) / Math.PI]);
            }
            return ring;
        }

        /** Wait for a global to become defined. */
        static async waitForGlobal(name: string, predicate: () => boolean, timeoutMs = 30_000): Promise<void> {
            const t0 = Date.now();
            while (true) {
                if (predicate()) return;
                if (Date.now() - t0 > timeoutMs) {
                    throw new Error(`Timed out waiting for ${name} global to be defined.`);
                }
                await new Promise(r => setTimeout(r, 50));
            }
        }

        /**
         * Resolves the map canvas element. Falls back to a plain id lookup using the
         * canvas id passed from the BitMap component when Blazor's element-reference
         * reviver returns null (which can happen under some render-batching conditions,
         * especially with multiple BitMap instances). If even that returns null we
         * briefly poll the DOM to let any pending render batch land before giving up.
         */
        static async resolveMapCanvas(canvasId: string, element: HTMLElement | null | undefined, timeoutMs = 5_000): Promise<HTMLElement> {
            if (element) return element;
            const t0 = Date.now();
            while (true) {
                const byId = document.getElementById(canvasId);
                if (byId) return byId;
                if (Date.now() - t0 > timeoutMs) {
                    throw new Error(`BitMap canvas element with id '${canvasId}' was not found in the DOM.`);
                }
                await new Promise(r => setTimeout(r, 16));
            }
        }
    }
}
