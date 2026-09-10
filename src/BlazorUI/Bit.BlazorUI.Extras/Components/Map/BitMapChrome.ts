namespace BitBlazorUI {

    type ChromeOptions = {
        /** BitBlazorUI.<jsObjectName> of the active provider, used to call back into invalidateSize. */
        jsObjectName: string;
        /** Keep the canvas sized to its container without the consumer calling InvalidateSize. */
        autoResize: boolean;
        /** Require ctrl/meta + wheel (desktop) and two fingers (touch) before the map may zoom/pan. */
        cooperativeGestures: boolean;
        /** Element id of the hint shown while a blocked gesture is attempted. */
        hintId: string | null;
        /** Element id of the hint's text node; the desktop/touch wording is swapped into it. */
        hintTextId: string | null;
        /** Wording shown when a bare wheel gesture is blocked. Already localized by .NET. */
        wheelHint: string | null;
        /** Wording shown when a one-finger drag is blocked. Already localized by .NET. */
        touchHint: string | null;
        /** Move focus out of the map canvas when Escape is pressed (WCAG 2.1.2). */
        escapeToExit: boolean;
    };

    type ChromeState = {
        options: ChromeOptions;
        root: HTMLElement;
        canvas: HTMLElement;
        dotnetObj: DotNetObject | null | undefined;
        resizeObserver: ResizeObserver | null;
        resizeFrame: number | null;
        lastWidth: number;
        lastHeight: number;
        hintTimer: any;
        listeners: { target: EventTarget, type: string, handler: any, capture: boolean }[];
        /** Element pinned to a geographic coordinate, and the frame loop keeping it there. */
        anchor: { elementId: string, element: HTMLElement | null, lat: number, lng: number, frame: number | null, deadline: number } | null;
    };

    /**
     * Visibility observers still waiting for their container to be scrolled to, keyed by the id of
     * the canvas they watch. Held so a map that is torn down before it ever became visible can
     * stop observing - otherwise the observer, its callback and the element it holds outlive the
     * component that asked the question.
     */
    const pendingVisibilityObservers: { [canvasId: string]: { observer: IntersectionObserver, resolve: () => void } } = {};

    /**
     * Provider-agnostic chrome that sits above whichever mapping library is active.
     *
     * Every backend BitMap supports (Leaflet, MapLibre, Mapbox, OpenLayers, ArcGIS,
     * Azure Maps, Cesium) has a different - and differently incomplete - story for
     * container resizing, scroll-jacking and keyboard escape. Implementing those once,
     * here, against the DOM the component itself renders means all seven behave the
     * same instead of inheriting each vendor's gaps.
     */
    export class BitMapChrome {
        private static _instances: { [id: string]: ChromeState } = {};

        /**
         * Whether the browser can give us a WebGL context of at least `requiredVersion` (1 or 2).
         * The GL-backed providers render a permanently blank canvas without one, so the component
         * checks this first and shows its unsupported state instead - and the version matters:
         * MapLibre 4 runs on WebGL 1 where Mapbox GL JS v3 refuses to start.
         */
        public static hasWebGl(requiredVersion: number): boolean {
            try {
                const required = requiredVersion >= 2 ? 2 : 1;
                const canvas = document.createElement('canvas');
                const gl2 = canvas.getContext('webgl2');
                const context = gl2 ?? (required === 2 ? null : canvas.getContext('webgl'));
                if (!context) return false;
                // Release the probe's context immediately. Browsers cap how many may be live at
                // once (roughly 8-16) and drop the least recently used one without warning, so a
                // probe that kept its context would eventually blank a real map to answer a
                // question about whether maps can be drawn at all.
                try { context.getExtension('WEBGL_lose_context')?.loseContext(); } catch { /* ignore */ }
                return true;
            } catch {
                return false;
            }
        }

        /** Whether the user asked for reduced motion, so camera moves can jump instead of fly. */
        public static prefersReducedMotion(): boolean {
            try {
                return globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches === true;
            } catch {
                return false;
            }
        }

        /**
         * Resolves once the map container has entered (or is already near) the viewport.
         * Lets the component defer constructing a map - hundreds of KB of library plus a
         * burst of tile requests - until it can actually be seen.
         */
        public static waitForVisible(canvasId: string, element: HTMLElement | null | undefined, rootMargin: string): Promise<void> {
            const target = element ?? document.getElementById(canvasId);
            if (!target) return Promise.resolve();
            if (typeof IntersectionObserver !== 'function') return Promise.resolve();

            // A second wait for the same canvas supersedes the first - a re-render that re-entered
            // initialization must not leave the earlier observer running.
            BitMapChrome.cancelWaitForVisible(canvasId);

            return new Promise<void>(resolve => {
                let settled = false;
                const finish = () => {
                    if (settled) return;
                    settled = true;
                    observer.disconnect();
                    delete pendingVisibilityObservers[canvasId];
                    resolve();
                };
                const observer = new IntersectionObserver(entries => {
                    if (entries.some(e => e.isIntersecting)) finish();
                }, { rootMargin: rootMargin || '200px' });
                pendingVisibilityObservers[canvasId] = { observer, resolve: finish };
                observer.observe(target);
            });
        }

        /**
         * Stops a pending visibility wait and lets its promise settle, so a component disposed
         * while still below the fold does not leave an observer - and an awaiting .NET task -
         * alive for the life of the page.
         */
        public static cancelWaitForVisible(canvasId: string) {
            const pending = pendingVisibilityObservers[canvasId];
            if (!pending) return;
            delete pendingVisibilityObservers[canvasId];
            try { pending.observer.disconnect(); } catch { /* ignore */ }
            pending.resolve();
        }

        /**
         * Reads the browser's geolocation once. Returned to .NET so the component can pan
         * there; the browser's own permission prompt is the consent gate.
         */
        public static locate(enableHighAccuracy: boolean, timeoutMs: number, maximumAgeMs: number): Promise<{ lat: number, lng: number, accuracy: number }> {
            return new Promise((resolve, reject) => {
                if (!navigator.geolocation) {
                    reject(new Error('Geolocation is not available in this browser.'));
                    return;
                }
                navigator.geolocation.getCurrentPosition(
                    pos => resolve({ lat: pos.coords.latitude, lng: pos.coords.longitude, accuracy: pos.coords.accuracy }),
                    err => reject(new Error(err?.message || 'Geolocation request failed.')),
                    { enableHighAccuracy, timeout: timeoutMs, maximumAge: maximumAgeMs });
            });
        }

        /**
         * Takes the map's own container fullscreen - not the page - so the consumer's overlay
         * content and controls go with it.
         *
         * Must be reached from a real user gesture: browsers only honour the request inside the
         * transient activation window a click opens. A Blazor Server round-trip normally still
         * lands inside it, but a slow circuit can miss it, and the browser then simply refuses.
         */
        public static async requestFullscreen(id: string): Promise<boolean> {
            const s = BitMapChrome._instances[id];
            if (!s) return false;
            try {
                await s.root.requestFullscreen();
                return true;
            } catch {
                return false;
            }
        }

        public static async exitFullscreen(id: string): Promise<boolean> {
            const s = BitMapChrome._instances[id];
            if (!s || document.fullscreenElement !== s.root) return false;
            try {
                await document.exitFullscreen();
                return true;
            } catch {
                return false;
            }
        }

        public static isFullscreen(id: string): boolean {
            const s = BitMapChrome._instances[id];
            return !!s && document.fullscreenElement === s.root;
        }

        public static attach(id: string, canvasId: string, element: HTMLElement | null | undefined,
                             dotnetObj: DotNetObject | null | undefined, options: ChromeOptions) {
            BitMapChrome.detach(id);

            const canvas = element ?? document.getElementById(canvasId);
            if (!canvas) return;
            const root = (canvas.parentElement as HTMLElement) ?? canvas;

            const state: ChromeState = {
                options, root, canvas, dotnetObj,
                resizeObserver: null,
                resizeFrame: null,
                lastWidth: canvas.clientWidth,
                lastHeight: canvas.clientHeight,
                hintTimer: null,
                listeners: [],
                anchor: null,
            };
            BitMapChrome._instances[id] = state;

            BitMapChrome._applyAutoResize(id, state);
            BitMapChrome._applyCooperativeGestures(state);
            BitMapChrome._applyEscapeToExit(state);
            BitMapChrome._applyContextLossReporting(state);
            BitMapChrome._applyFullscreenReporting(id, state);
        }

        /**
         * Pins an element to a geographic coordinate, following the map as it moves.
         *
         * This is what lets a popup's content stay Blazor-rendered. Handing that DOM to the
         * mapping library's own popup would move it out of the container Blazor patches by
         * position, and the next render of that region would corrupt or throw. So the element
         * stays exactly where Blazor put it and only its transform is driven from here - a
         * property Blazor never sets, and therefore never fights over.
         *
         * The position is recomputed per animation frame rather than per map event: a drag moves
         * the map continuously, and anything slower visibly lags behind the marker.
         */
        public static trackAnchor(id: string, elementId: string, lat: number, lng: number) {
            const s = BitMapChrome._instances[id];
            if (!s) return;

            BitMapChrome.untrackAnchor(id);

            const anchor = {
                elementId,
                // Resolved inside the loop rather than here: .NET asks for the anchor as soon as
                // it has requested a re-render, and Blazor renders on its own schedule - so the
                // element usually does not exist yet at this point.
                element: null as HTMLElement | null,
                lat, lng,
                frame: null as number | null,
                deadline: Date.now() + 5_000,
            };
            s.anchor = anchor;

            const provider = () => (globalThis as any).BitBlazorUI?.[s.options.jsObjectName];
            const step = () => {
                if (s.anchor !== anchor) return;

                if (!anchor.element) {
                    anchor.element = document.getElementById(elementId);
                    if (!anchor.element) {
                        // Give the render a bounded number of frames to land, then give up rather
                        // than spin forever over an element that is never coming.
                        if (Date.now() > anchor.deadline) { s.anchor = null; return; }
                        anchor.frame = requestAnimationFrame(step);
                        return;
                    }
                }

                let point: { x: number, y: number } | null = null;
                try { point = provider()?.project?.(id, anchor.lat, anchor.lng) ?? null; } catch { /* ignore */ }
                if (point) {
                    anchor.element.style.transform = `translate(${Math.round(point.x)}px, ${Math.round(point.y)}px)`;
                    anchor.element.style.visibility = '';
                } else {
                    // Off-screen, or behind the globe on a 3D provider. Hide rather than park it
                    // at a stale position, which would read as a popup for the wrong place.
                    anchor.element.style.visibility = 'hidden';
                }
                anchor.frame = requestAnimationFrame(step);
            };
            step();
        }

        public static untrackAnchor(id: string) {
            const s = BitMapChrome._instances[id];
            if (!s?.anchor) return;
            if (s.anchor.frame !== null) cancelAnimationFrame(s.anchor.frame);
            s.anchor = null;
        }

        public static detach(id: string) {
            const s = BitMapChrome._instances[id];
            if (!s) return;
            BitMapChrome.untrackAnchor(id);
            if (s.resizeObserver) { try { s.resizeObserver.disconnect(); } catch { /* ignore */ } }
            if (s.resizeFrame !== null) cancelAnimationFrame(s.resizeFrame);
            if (s.hintTimer) clearTimeout(s.hintTimer);
            for (const l of s.listeners) {
                try { l.target.removeEventListener(l.type, l.handler, l.capture); } catch { /* ignore */ }
            }
            s.dotnetObj = null;
            delete BitMapChrome._instances[id];
        }

        // ---- helpers ----

        private static _listen(s: ChromeState, target: EventTarget, type: string, handler: any, options?: AddEventListenerOptions) {
            target.addEventListener(type, handler, options);
            s.listeners.push({ target, type, handler, capture: options?.capture === true });
        }

        /**
         * A CSS-only container resize never fires `window.resize`, so a map in a flex/grid
         * panel silently keeps a stale canvas. ResizeObserver catches those, and it also
         * catches the classic 0x0 case: a map inside a hidden tab initializes at zero size
         * and stays grey until something tells it to re-measure.
         */
        private static _applyAutoResize(id: string, s: ChromeState) {
            if (!s.options.autoResize || typeof ResizeObserver !== 'function') return;

            const observer = new ResizeObserver(() => {
                // Coalesce into a frame: a drag-resize fires the observer continuously, and
                // each notification would otherwise trigger a full map re-layout.
                if (s.resizeFrame !== null) return;
                s.resizeFrame = requestAnimationFrame(() => {
                    s.resizeFrame = null;
                    const width = s.canvas.clientWidth;
                    const height = s.canvas.clientHeight;
                    if (width === s.lastWidth && height === s.lastHeight) return;
                    s.lastWidth = width;
                    s.lastHeight = height;
                    if (width <= 0 || height <= 0) return;
                    const provider = (globalThis as any).BitBlazorUI?.[s.options.jsObjectName];
                    try { provider?.invalidateSize?.(id); } catch { /* ignore */ }
                });
            });
            try { observer.observe(s.root); } catch { /* ignore */ }
            s.resizeObserver = observer;
        }

        /**
         * Cooperative gestures: an embedded map inside a scrolling page must not swallow
         * the page scroll. A bare wheel gesture is stopped before the mapping library sees
         * it (capture phase) and the page keeps scrolling; ctrl/meta + wheel is let through
         * so zooming is still one gesture away. Touch mirrors it: one finger scrolls the
         * page, two fingers pan the map.
         */
        private static _applyCooperativeGestures(s: ChromeState) {
            if (!s.options.cooperativeGestures) return;

            const wheel = (e: WheelEvent) => {
                if (e.ctrlKey || e.metaKey) {
                    BitMapChrome._hideHint(s);
                    return;
                }
                // stopPropagation only - NOT preventDefault - so the page still scrolls.
                e.stopPropagation();
                BitMapChrome._showHint(s, s.options.wheelHint);
            };
            // Listen on the PARENT, in the capture phase. Registering on the canvas itself would
            // not work: at the event target the capture flag is ignored and listeners run in
            // registration order, and the mapping library registered its own during init - before
            // this ran. Capturing one level up is what actually gets us there first.
            // `passive: false` is required because a wheel listener the browser assumes is passive
            // cannot reliably interfere with the gesture.
            BitMapChrome._listen(s, s.root, 'wheel', wheel, { capture: true, passive: false });

            const touchMove = (e: TouchEvent) => {
                if (e.touches.length > 1) {
                    BitMapChrome._hideHint(s);
                    return;
                }
                e.stopPropagation();
                BitMapChrome._showHint(s, s.options.touchHint);
            };
            BitMapChrome._listen(s, s.root, 'touchmove', touchMove, { capture: true, passive: false });

            const touchEnd = () => BitMapChrome._hideHint(s);
            BitMapChrome._listen(s, s.root, 'touchend', touchEnd, { capture: true });

            const leave = () => BitMapChrome._hideHint(s);
            BitMapChrome._listen(s, s.root, 'mouseleave', leave, { capture: true });
        }

        private static _showHint(s: ChromeState, text: string | null) {
            if (!s.options.hintId || !text) return;
            const hint = document.getElementById(s.options.hintId);
            if (!hint) return;
            const textNode = s.options.hintTextId ? document.getElementById(s.options.hintTextId) : null;
            if (textNode && textNode.textContent !== text) textNode.textContent = text;
            hint.classList.add('bit-map-gesture-hint-vis');
            clearTimeout(s.hintTimer);
            s.hintTimer = setTimeout(() => BitMapChrome._hideHint(s), 1500);
        }

        private static _hideHint(s: ChromeState) {
            if (!s.options.hintId) return;
            clearTimeout(s.hintTimer);
            s.hintTimer = null;
            document.getElementById(s.options.hintId)?.classList.remove('bit-map-gesture-hint-vis');
        }

        /**
         * WCAG 2.1.2 (No Keyboard Trap): a focused map consumes the arrow keys, so there
         * has to be a documented way out that is not "keep pressing Tab past every marker".
         * Escape returns focus to the document flow.
         */
        private static _applyEscapeToExit(s: ChromeState) {
            if (!s.options.escapeToExit) return;
            const keydown = (e: KeyboardEvent) => {
                if (e.key !== 'Escape') return;
                e.stopPropagation();
                try {
                    // Focus may be on any focusable descendant - a marker button, a provider's own
                    // control - not only the canvas, so blur whatever inside the map actually holds
                    // it. Otherwise Escape leaves the user exactly where the trap was.
                    const active = document.activeElement;
                    if (active instanceof HTMLElement && s.root.contains(active)) active.blur();
                    else s.canvas.blur();
                } catch { /* ignore */ }
            };
            // Same reasoning as the gesture listeners: capture on the parent, so a provider that
            // binds Escape for its own purposes cannot consume it first.
            BitMapChrome._listen(s, s.root, 'keydown', keydown, { capture: true });
        }

        /**
         * Browsers cap the number of simultaneous WebGL contexts (roughly 8-16), and the
         * least-recently-used one is dropped silently when the cap is hit - a map simply
         * goes black with no error. Reporting the loss lets the component show its error
         * state, and the restore lets it recover instead of staying blank.
         */
        /**
         * Reports entering and leaving fullscreen, including the routes that never go through our
         * own API - the Escape key, the browser's own control, or another element taking over.
         */
        private static _applyFullscreenReporting(id: string, s: ChromeState) {
            const onChange = () => {
                const isFullscreen = document.fullscreenElement === s.root;
                // The container just changed size by a large factor, and only ResizeObserver would
                // otherwise catch it - which AutoResize may have turned off.
                const provider = (globalThis as any).BitBlazorUI?.[s.options.jsObjectName];
                try { provider?.invalidateSize?.(id); } catch { /* ignore */ }
                s.dotnetObj?.invokeMethodAsync('OnFullscreenChanged', isFullscreen);
            };
            // fullscreenchange fires on the document, not on the element that went fullscreen.
            BitMapChrome._listen(s, document, 'fullscreenchange', onChange);
        }

        private static _applyContextLossReporting(s: ChromeState) {
            const lost = (e: Event) => {
                // Calling preventDefault is what makes a restore possible at all.
                e.preventDefault();
                s.dotnetObj?.invokeMethodAsync('OnRenderContextLost');
            };
            const restored = () => s.dotnetObj?.invokeMethodAsync('OnRenderContextRestored');
            // The GL canvas is created by the provider inside our container, so listen on
            // the container in the capture phase - the events do not bubble.
            BitMapChrome._listen(s, s.canvas, 'webglcontextlost', lost, { capture: true });
            BitMapChrome._listen(s, s.canvas, 'webglcontextrestored', restored, { capture: true });
        }
    }
}
