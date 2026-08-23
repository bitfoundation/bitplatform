namespace BitBlazorUI {

    export type BitMapLL = { lat: number, lng: number };
    export type BitMapBounds = { southWest: BitMapLL, northEast: BitMapLL };

    /** Helpers shared by every BitMap provider implementation. */
    export class BitMapHelpers {
        /**
         * Origins whose tiles failed in no-cors mode and are therefore requested in CORS mode from
         * now on. Keyed per origin, not a single page-wide flag: one tile host switching to CORS
         * says nothing about another, and a layer pointed at a host that sends CORP but no
         * Access-Control-Allow-Origin would be broken - not fixed - by being dragged along. An
         * entry is retracted again when the CORS-mode attempt fails just as completely, since the
         * evidence that wrote it (every tile of one layer failing) is also what a layer that 404s
         * or 401s its whole batch looks like - see createTileCorsFallback.
         */
        private static _corsTileOrigins: { [origin: string]: true } = {};

        /**
         * How long a layer is given, after its first tile error, to prove itself reachable before
         * the origin is switched to CORS mode. Long enough for the rest of the initial batch of
         * tiles to land, short enough that a genuinely blocked layer redraws without a visible wait.
         */
        private static readonly _tileCorsRetryDelay = 1_000;

        /** Origin of a tile url template, or null when it does not parse. */
        private static tileOrigin(urlTemplate: string): string | null {
            try {
                // {s} is Leaflet's subdomain placeholder and can sit in the host, so it has to be
                // substituted before the URL parses; every other placeholder is in the path.
                const url = (urlTemplate || '').replace('{s}', 'a');
                return new URL(url, document.baseURI).origin;
            } catch {
                return null;
            }
        }

        /**
         * crossOrigin value to create a tile layer with, or undefined for the default no-cors mode.
         *
         * Tiles start out in no-cors mode so tile servers that send neither CORS nor CORP headers
         * keep working (requesting those in CORS mode would block them). When the page is
         * cross-origin isolated with Cross-Origin-Embedder-Policy: require-corp - the only COEP
         * value WebKit understands, and what the multi-threaded WebAssembly runtime needs there -
         * a cross-origin tile without a Cross-Origin-Resource-Policy header is blocked in no-cors
         * mode instead, so the first layer on that host whose whole initial batch of tiles fails
         * marks the host and redraws itself in CORS mode (OSM, Carto, OpenTopoMap... all send
         * Access-Control-Allow-Origin: *). Mirrors the no-cors then CORS retry the Extras/Legacy
         * script and stylesheet loaders do.
         */
        static tileCrossOrigin(urlTemplate: string): string | undefined {
            const origin = BitMapHelpers.tileOrigin(urlTemplate);
            return origin !== null && BitMapHelpers._corsTileOrigins[origin] ? 'anonymous' : undefined;
        }

        /**
         * Creates the tile-load listeners implementing the no-cors then CORS retry for a single
         * tile layer. `retry` is invoked at most once, and only when every tile of that layer
         * failed: a layer that loaded at least one tile is talking to a reachable server, so a
         * later error is an ordinary missing/failing tile and must not switch that host to CORS
         * mode (which would break a tile server that sends no CORS headers). The decision is
         * therefore deferred by `_tileCorsRetryDelay` from the first error, giving the rest of
         * the initial batch of tiles - which a layer requests in parallel, so their results
         * interleave - the chance to disprove it.
         *
         * `retry` returns false when it declined to redraw - the layer it was wired to has moved
         * on meanwhile - in which case the mark is rolled back: it is the redrawn layer that
         * verifies the mark, so a mark nothing redrew is a mark nothing would ever disprove.
         *
         * `isVerification` marks the fallback of that redrawn, CORS-mode layer, and is passed in
         * by the provider rather than inferred from the origin already being marked. That layer
         * does not retry, it verifies: "every tile of this layer failed" is a heuristic - a layer
         * whose whole batch 404s (wrong path template) or 401s (unauthenticated tileset) looks
         * exactly like a COEP block from here - so if the CORS-mode attempt fails just as fully,
         * CORS mode is not what this host needed and the origin-wide mark is retracted. Every
         * other layer on a marked origin merely consumes the mark and watches nothing: were it to
         * verify as well, an unrelated overlay 404ing its own batch would retract a mark another
         * layer had just proved right. This cannot ping-pong: retracting performs no retry, and
         * each fallback acts at most once.
         */
        static createTileCorsFallback(urlTemplate: string, retry: () => boolean | void, isVerification: boolean = false) {
            const origin = BitMapHelpers.tileOrigin(urlTemplate);
            // Compared against true explicitly: crossOriginIsolated is undefined on browsers that
            // predate it (Safari < 15.2, Chrome < 87, Firefox < 79), and `x && undefined` yields
            // undefined - which a `=== false` guard would wave through, flipping those browsers to
            // CORS mode on the first ordinary tile error even though nothing there blocks no-cors
            // tiles in the first place. lib.dom types it as boolean, so only the runtime knows.
            const eligible = origin !== null
                && origin !== location.origin
                && self.crossOriginIsolated === true;
            const verifying = eligible && isVerification;
            const marking = eligible && !isVerification && BitMapHelpers._corsTileOrigins[origin!] !== true;
            if (!verifying && !marking) {
                // Neither role applies, so this layer's tile errors decide nothing about the origin.
                const noop = () => { };
                return { onTileLoad: noop, onTileError: noop };
            }
            let loaded = false;
            let acted = false;
            let scheduled = false;
            return {
                onTileLoad: () => { loaded = true; },
                onTileError: () => {
                    if (loaded || acted || scheduled) return;
                    // The first error settles nothing on its own: the tiles of a layer are
                    // requested in parallel, so an ordinary missing tile can report back before
                    // any of its siblings has finished loading. Acting on it would switch a
                    // perfectly reachable host to CORS mode - and break it for good when it
                    // sends CORP but no Access-Control-Allow-Origin. Wait for the rest of the
                    // batch instead and act only if none of it loaded, which is the COEP
                    // signature: every tile of the layer blocked, not just one.
                    scheduled = true;
                    setTimeout(() => {
                        if (loaded || acted) return;
                        acted = true;
                        if (verifying) {
                            // CORS mode did not help this layer either - retract the origin-wide
                            // decision instead of leaving the host poisoned for everyone else.
                            // Guarded on the mark still standing so a concurrent layer that just
                            // re-established it is not undone.
                            if (BitMapHelpers._corsTileOrigins[origin!] === true) {
                                delete BitMapHelpers._corsTileOrigins[origin!];
                            }
                            return;
                        }
                        // Whether the mark is ours to retract below. Two layers of the same host
                        // can both be markers - they are created before either has acted - so the
                        // one that acts second must not undo the first one's verified mark.
                        const wasMarked = BitMapHelpers._corsTileOrigins[origin!] === true;
                        // Written before the retry, not after: the retry rebuilds the layer, and
                        // what it rebuilds with is read back out of this very map by
                        // tileCrossOrigin().
                        BitMapHelpers._corsTileOrigins[origin!] = true;
                        // No longer inside the tile event, so the map may have been disposed
                        // meanwhile; a failing redraw must not surface as an unhandled error.
                        let retried = false;
                        try { retried = retry() !== false; } catch { /* ignore */ }
                        if (retried || wasMarked) return;
                        // Nothing was redrawn, so nothing will verify the mark - and the layer that
                        // would have is gone. Leaving it standing would hand every later layer on
                        // this host CORS mode for good on the strength of one unproven batch.
                        if (BitMapHelpers._corsTileOrigins[origin!] === true) {
                            delete BitMapHelpers._corsTileOrigins[origin!];
                        }
                    }, BitMapHelpers._tileCorsRetryDelay);
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
