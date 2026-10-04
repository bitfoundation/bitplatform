namespace BitBlazorUI {

    export type BitMapLL = { lat: number, lng: number };
    export type BitMapBounds = { southWest: BitMapLL, northEast: BitMapLL };

    /** Helpers shared by every BitMap provider implementation. */
    export class BitMapHelpers {
        /**
         * Debounce window (ms) every provider uses before pushing an OnViewChanged
         * notification into .NET. A single drag emits a burst of move/zoom events and
         * each notification costs an interop round-trip (a SignalR message under
         * Blazor Server), so they are coalesced into one call per idle window.
         */
        static readonly viewNotifyDebounceMs = 80;

        /**
         * The element a map's theme colors are read off (.bit-map-probe in BitMap.scss): its fill is the
         * marker color, its stroke the shape color, and a color of the caller's is resolved on it.
         */
        static probe(mapId: string): HTMLElement | null {
            try {
                return document.getElementById(mapId)?.querySelector(':scope > .bit-map-probe') as HTMLElement | null ?? null;
            } catch {
                return null;
            }
        }

        /**
         * Resolves any CSS color - a name, hex, rgb(), hsl(), oklch(), color-mix(), a theme variable such as
         * var(--bit-clr-pri) - to the rgb() / rgba() form that a canvas, an SVG attribute and a WebGL style all
         * accept, by letting the browser compute it on the map's probe. The libraries each parse colors their
         * own way, several only hex, so nothing reaches them unresolved. Null when there is no color, or the
         * browser rejects it - a var() naming nothing included.
         */
        static resolveColor(mapId: string, color: string | null | undefined): string | null {
            if (!color || typeof color !== 'string') return null;
            const probe = BitMapHelpers.probe(mapId);
            if (!probe) return color; // no DOM to resolve against - hand it on as given
            try {
                if (globalThis.CSS?.supports && !CSS.supports('color', color)) return null;
                // A var() naming nothing leaves the property at its initial value, currentcolor - the probe's
                // own color, which is the cluster bubble's text color. The probe's color is a sentinel while
                // the color is read, so that case is told apart and taken as no color at all.
                probe.style.color = 'rgb(1, 2, 3)';
                const sentinel = getComputedStyle(probe).color;
                probe.style.outlineColor = color;
                const computed = getComputedStyle(probe).outlineColor;
                if (!computed || computed === sentinel) return null;
                return BitMapHelpers.toRgbString(computed);
            } catch {
                return color;
            } finally {
                probe.style.outlineColor = '';
                probe.style.color = '';
            }
        }

        private static _pixel: CanvasRenderingContext2D | null | undefined;

        /**
         * A computed color in the rgb() / rgba() form every library parses. A browser computes the legacy
         * syntaxes to rgb(), but the newer ones - color-mix(), oklch(), lab(), color() - stay in a space of
         * their own (color(srgb ...), oklch(...)), so those are painted onto a pixel and read back as plain
         * channels. Null when not even a canvas takes it.
         */
        static toRgbString(color: string | null | undefined): string | null {
            if (!color || typeof color !== 'string') return null;
            if (BitMapHelpers.parseColor(color)) return color;
            if (BitMapHelpers._pixel === undefined) {
                try {
                    const canvas = document.createElement('canvas');
                    canvas.width = canvas.height = 1;
                    BitMapHelpers._pixel = canvas.getContext('2d', { willReadFrequently: true }) as CanvasRenderingContext2D | null;
                } catch {
                    BitMapHelpers._pixel = null;
                }
            }
            const ctx = BitMapHelpers._pixel;
            if (!ctx) return null;
            try {
                // An assignment the canvas cannot parse is ignored, leaving the sentinel in place.
                ctx.fillStyle = '#010203';
                const sentinel = ctx.fillStyle;
                ctx.fillStyle = color;
                if (ctx.fillStyle === sentinel) return null;
                ctx.clearRect(0, 0, 1, 1);
                ctx.fillRect(0, 0, 1, 1);
                const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data;
                return a === 255 ? `rgb(${r}, ${g}, ${b})` : `rgba(${r}, ${g}, ${b}, ${Math.round(a / 255 * 1000) / 1000})`;
            } catch {
                return null;
            }
        }

        /** The theme's colors for markers and shapes, as the --bit-Map-marker-color / -vector-color variables say. */
        static themeColors(mapId: string): { marker: string, vector: string } {
            let marker = '#3388ff', vector = '#3388ff'; // reached only without a DOM to read
            const probe = BitMapHelpers.probe(mapId);
            if (probe) {
                try {
                    const style = getComputedStyle(probe);
                    marker = BitMapHelpers.toRgbString(style.fill) ?? marker;
                    vector = BitMapHelpers.toRgbString(style.stroke) ?? vector;
                } catch { /* keep the defaults */ }
            }
            return { marker, vector };
        }

        /** Parses a hex or rgb() / rgba() color into its channels (0-255) and alpha (0-1). */
        static parseColor(color: string | null | undefined): [number, number, number, number] | null {
            if (!color || typeof color !== 'string') return null;
            const c = color.trim();
            if (c.startsWith('#')) {
                let h = c.slice(1);
                if (h.length === 3 || h.length === 4) h = [...h].map(x => x + x).join('');
                if (h.length !== 6 && h.length !== 8) return null;
                const n = parseInt(h, 16);
                if (Number.isNaN(n)) return null;
                return h.length === 8
                    ? [(n >>> 24) & 255, (n >>> 16) & 255, (n >>> 8) & 255, (n & 255) / 255]
                    : [(n >> 16) & 255, (n >> 8) & 255, n & 255, 1];
            }
            const m = /^rgba?\(([^)]+)\)$/i.exec(c);
            if (!m) return null;
            const parts = m[1].split(/[\s,/]+/).filter(p => p.length > 0);
            if (parts.length < 3) return null;
            const channel = (p: string) => p.endsWith('%') ? parseFloat(p) * 2.55 : parseFloat(p);
            const [r, g, b] = parts.slice(0, 3).map(channel);
            const a = parts.length > 3 ? (parts[3].endsWith('%') ? parseFloat(parts[3]) / 100 : parseFloat(parts[3])) : 1;
            if (![r, g, b, a].every(Number.isFinite)) return null;
            return [Math.round(r), Math.round(g), Math.round(b), Math.min(1, Math.max(0, a))];
        }

        /** A color with an opacity applied on top of its own, as rgba(); blue when it cannot be parsed. */
        static toRgba(color: string | null | undefined, alpha: number): string {
            const [r, g, b, a] = BitMapHelpers.parseColor(color) ?? [51, 136, 255, 1];
            return `rgba(${r},${g},${b},${a * alpha})`;
        }

        /**
         * A path style with its colors resolved: the caller's, or the theme's shape color when none was given.
         * Called first thing by every method that draws a shape, so what follows only ever sees plain rgb().
         */
        static resolvePathStyle(mapId: string, style: any): any {
            const color = BitMapHelpers.resolveColor(mapId, style?.color) ?? BitMapHelpers.themeColors(mapId).vector;
            const fillColor = BitMapHelpers.resolveColor(mapId, style?.fillColor) ?? color;
            return { ...(style ?? {}), color, fillColor };
        }

        /**
         * The pin every provider draws for a marker without an IconUrl - the same one on all seven, in the
         * marker's Color or the theme's --bit-Map-marker-color, where each library would draw a pin of its own
         * in a fixed color of its own. It is handed back as the icon fields of the marker, so each provider
         * draws it down the path it already has for a custom icon.
         */
        static withDefaultIcon(mapId: string, opts: any): any {
            if (!opts || opts.iconUrl) return opts;
            const fill = BitMapHelpers.resolveColor(mapId, opts.color) ?? BitMapHelpers.themeColors(mapId).marker;
            const width = opts.iconWidth ?? 25;
            const height = opts.iconHeight ?? 41;
            const svg =
                `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="-1 -1 27 43">` +
                `<path d="M12.5 0C5.6 0 0 5.6 0 12.5 0 21.9 12.5 41 12.5 41S25 21.9 25 12.5C25 5.6 19.4 0 12.5 0z" fill="${fill}" stroke="rgba(0,0,0,0.35)" stroke-width="1"/>` +
                `<circle cx="12.5" cy="12.5" r="4.5" fill="#fff"/>` +
                `</svg>`;
            return { ...opts, iconUrl: BitMapHelpers.svgDataUri(svg), iconWidth: width, iconHeight: height };
        }

        /**
         * An SVG as a data URI that also survives an unquoted CSS url(): encodeURIComponent leaves the
         * parentheses of an rgb() and the apostrophe alone, and either one would end the url() early.
         */
        static svgDataUri(svg: string): string {
            return 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg).replace(/[()']/g, c => '%' + c.charCodeAt(0).toString(16).toUpperCase());
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
            // A non-finite or negative radius would propagate NaN through every
            // coordinate below and hand the renderer an unusable ring. Degenerate to a
            // zero-radius ring (a point) instead of drawing garbage.
            if (!Number.isFinite(radiusMeters) || radiusMeters < 0) {
                radiusMeters = 0;
            }
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

        /**
         * The point of a marker icon, in pixels from its top-left corner, that sits on the
         * coordinate. Defaults to bottom-centre - where a pin's tip is, and what every mapping
         * library defaults to - so an icon that is a dot rather than a pin has to say so.
         */
        static readIconAnchor(opts: any, width: number, height: number): [number, number] {
            const x = typeof opts?.iconAnchorX === 'number' && Number.isFinite(opts.iconAnchorX)
                ? opts.iconAnchorX
                : Math.round(width / 2);
            const y = typeof opts?.iconAnchorY === 'number' && Number.isFinite(opts.iconAnchorY)
                ? opts.iconAnchorY
                : height;
            return [Math.round(x), Math.round(y)];
        }

        /**
         * Where a popup or tooltip opened on a marker should point from, relative to the coordinate: the
         * icon's top edge for one above it, its bottom edge below, and beside its head - the top square of
         * the icon, a pin's round part - to either side. A popup aimed at the coordinate itself would sit
         * over the icon it belongs to. In the anchor names the GL libraries use, which say what side of
         * the popup touches the point: 'bottom' is a popup above the marker.
         */
        static popupOffsets(opts: any, width: number, height: number): { [anchor: string]: [number, number] } {
            const [ax, ay] = BitMapHelpers.readIconAnchor(opts, width, height);
            const cx = Math.round(width / 2 - ax);
            const top = -ay, bottom = height - ay, left = -ax, right = width - ax;
            const head = Math.round(-ay + Math.min(width, height) / 2);
            return {
                'center': [cx, Math.round(height / 2 - ay)],
                'bottom': [cx, top],
                'top': [cx, bottom],
                'left': [right, head],
                'right': [left, head],
                'bottom-left': [right, top],
                'bottom-right': [left, top],
                'top-left': [right, bottom],
                'top-right': [left, bottom],
            };
        }

        /** Split a subdomains option ("abc" or "a,b,c") into its individual values. */
        static readSubdomains(subdomains: string | undefined): string[] {
            const raw = (subdomains ?? 'abc').trim();
            if (!raw) return ['a'];
            // Accept both the CSV form and Leaflet's shorthand, where each character is a subdomain.
            const parts = raw.includes(',') ? raw.split(',') : raw.split('');
            const cleaned = parts.map(p => p.trim()).filter(p => p.length > 0);
            return cleaned.length > 0 ? cleaned : ['a'];
        }

        /**
         * Expands an {s} placeholder into one URL per subdomain. Only Leaflet has a subdomain
         * concept of its own; the other backends take a list of tile URLs instead, which comes to
         * the same thing - the source is still sharded across the hostnames.
         */
        static expandSubdomains(url: string, subdomains: string | undefined): string[] {
            if (!url) return [''];
            if (!url.includes('{s}')) return [url];
            return BitMapHelpers.readSubdomains(subdomains).map(sub => url.split('{s}').join(sub));
        }

        /** The first expansion of an {s} URL, for backends that accept only a single URL. */
        static firstSubdomainUrl(url: string, subdomains: string | undefined): string {
            return BitMapHelpers.expandSubdomains(url, subdomains)[0];
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
