namespace BitBlazorUI {
    // The interop surface of the BitResourceTimeline: one engine per component, kept by the component's id. The engine
    // (BitResourceTimelineEngine.ts) holds all the behavior of the timeline and paints through the renderer its options
    // name; this file wires the two together, feeds the engine the theme in force on the component, and is the one script
    // the component calls into, which is what keeps the engine and the renderers in an app that uses the component.
    export class ResourceTimeline {
        private static _instances = new Map<string, ResourceTimelineInstance>();

        public static setup(id: string, viewport: HTMLElement, dotnetObj: DotNetObject, options: any, texts: any, disabled: boolean) {
            ResourceTimeline.dispose(id);

            if (!viewport) return null;

            const instance = new ResourceTimelineInstance(viewport, dotnetObj, options, texts, disabled);
            ResourceTimeline._instances.set(id, instance);

            return instance.engine.getLayout();
        }

        public static setOptions(id: string, options: any, texts: any, disabled: boolean) {
            const instance = ResourceTimeline._instances.get(id);
            if (!instance) return null;

            instance.setOptions(options, texts, disabled);

            return instance.engine.getLayout();
        }

        public static enableResourceTemplate(id: string, overlayInner: HTMLElement) {
            ResourceTimeline._instances.get(id)?.engine.enableResourceTemplate(overlayInner);
        }

        public static disableResourceTemplate(id: string) {
            ResourceTimeline._instances.get(id)?.engine.disableResourceTemplate();
        }

        public static enableTooltipTemplate(id: string) {
            ResourceTimeline._instances.get(id)?.engine.enableTooltipTemplate();
        }

        public static disableTooltipTemplate(id: string) {
            ResourceTimeline._instances.get(id)?.engine.disableTooltipTemplate();
        }

        // Places the overlay of the TooltipTemplate, rendered at the pointer, the way the built-in tooltip places itself
        // (ResourceTimelineTooltip.place): beside the pointer, flipped to its other side where it would overflow the
        // viewport, and kept within it. The gap from the pointer is the translate of its stylesheet (.bit-rtm-tov), or
        // whatever an app's style puts there instead, so it is read rather than assumed; and the box is measured as it
        // is laid out, so a containing block other than the viewport (a transformed ancestor) is accounted for as well.
        public static placeTooltipOverlay(id: string, overlay: HTMLElement, clientX: number, clientY: number) {
            if (!ResourceTimeline._instances.has(id) || !overlay || !overlay.isConnected) return;

            const style = overlay.style;
            style.left = clientX + 'px';
            style.top = clientY + 'px';

            let gapX = 0;
            let gapY = 0;
            try {
                const transform = getComputedStyle(overlay).transform;
                if (transform && transform !== 'none') {
                    const matrix = new DOMMatrixReadOnly(transform);
                    gapX = matrix.m41;
                    gapY = matrix.m42;
                }
            } catch { /* an unparsable transform: placed without a gap */ }

            // How far from where its left and top ask for it the overlay is drawn: the translate, plus the offset of its
            // containing block.
            const rect = overlay.getBoundingClientRect();
            const shiftX = rect.left - clientX;
            const shiftY = rect.top - clientY;

            const { left, top } = ResourceTimelineTooltip.place(rect.width, rect.height, clientX, clientY, gapX, gapY);
            style.left = (left - shiftX) + 'px';
            style.top = (top - shiftY) + 'px';
        }

        public static setData(id: string, resources: any[], start: number, end: number, allocations: any[]) {
            ResourceTimeline._instances.get(id)?.engine.setData(resources, start, end, allocations);
        }

        public static clearData(id: string) {
            ResourceTimeline._instances.get(id)?.engine.clearData();
        }

        public static beginData(id: string, resources: any[], start: number, end: number, expectedCount: number) {
            ResourceTimeline._instances.get(id)?.engine.beginData(resources, start, end, expectedCount);
        }

        public static appendAllocations(id: string, allocations: any[]) {
            ResourceTimeline._instances.get(id)?.engine.appendAllocations(allocations);
        }

        public static endData(id: string) {
            ResourceTimeline._instances.get(id)?.engine.endData();
        }

        public static beginWindowed(id: string, resources: any[], start: number, end: number, session: number) {
            ResourceTimeline._instances.get(id)?.engine.beginWindowed(resources, start, end, session);
        }

        public static getVisibleWindow(id: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.getVisibleWindow() : null;
        }

        public static applyAllocationWindow(id: string, requestId: number, allocations: any[], start: number, end: number, session: number) {
            ResourceTimeline._instances.get(id)?.engine.applyAllocationWindow(requestId, allocations, start, end, session);
        }

        // Resolves once the engine has painted the frame of what it was last given, so the loading overlay of the
        // component goes away with the bars on screen.
        public static whenRendered(id: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.whenRendered() : Promise.resolve();
        }

        public static upsertAllocations(id: string, allocations: any[]) {
            ResourceTimeline._instances.get(id)?.engine.upsertAllocations(allocations);
        }

        public static removeAllocations(id: string, ids: string[]) {
            ResourceTimeline._instances.get(id)?.engine.removeAllocations(ids);
        }

        public static clearSelection(id: string) {
            ResourceTimeline._instances.get(id)?.engine.clearSelection();
        }

        public static selectBars(id: string, ids: string[], additive: boolean) {
            ResourceTimeline._instances.get(id)?.engine.selectBars(ids, additive);
        }

        public static scrollToAllocation(id: string, allocationId: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? !!instance.engine.scrollToAllocation(allocationId) : false;
        }

        public static scrollToResource(id: string, resourceId: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? !!instance.engine.scrollToResource(resourceId) : false;
        }

        public static getSelectedBarIds(id: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.getSelectedBarIds() : [];
        }

        public static goToNow(id: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? !!instance.engine.goToNow() : false;
        }

        public static scrollToTime(id: string, time: number) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? !!instance.engine.scrollToTime(time) : false;
        }

        public static panByDays(id: string, days: number, panToDayStart: boolean | null) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? !!instance.engine.panByDays(days, panToDayStart) : false;
        }

        public static zoomIn(id: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.zoomIn() : 0;
        }

        public static zoomOut(id: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.zoomOut() : 0;
        }

        public static zoomToDays(id: string, days: number) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.zoomToDays(days) : 0;
        }

        public static setPixelsPerHour(id: string, pixelsPerHour: number | null) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.setPixelsPerHour(pixelsPerHour) : 0;
        }

        public static resetZoom(id: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.resetZoom() : 0;
        }

        public static getPixelsPerHour(id: string) {
            const instance = ResourceTimeline._instances.get(id);

            return instance ? instance.engine.getPixelsPerHour() : 0;
        }

        public static toggleGroup(id: string, resourceId: string) {
            ResourceTimeline._instances.get(id)?.engine.toggleGroup(resourceId);
        }

        public static dispose(id: string) {
            const instance = ResourceTimeline._instances.get(id);
            if (!instance) return;

            ResourceTimeline._instances.delete(id);
            instance.dispose();
        }
    }

    // The theme colors the engine reads, by the key of its colors option, and the private variable of the stylesheet
    // (BitResourceTimeline.scss) each one is resolved from - the public --bit-ResourceTimeline-* variable, or the theme
    // token it falls back to.
    const THEME_COLORS: [string, string][] = [
        ['contentBg', '--bit-rtm-thm-content-bg'],
        ['axisBg', '--bit-rtm-thm-axis-bg'],
        ['axisBorder', '--bit-rtm-thm-axis-border'],
        ['tick', '--bit-rtm-thm-tick'],
        ['label', '--bit-rtm-thm-label'],
        ['dateLabel', '--bit-rtm-thm-date-label'],
        ['grid', '--bit-rtm-thm-grid'],
        ['bar', '--bit-rtm-thm-bar'],
        ['barSelected', '--bit-rtm-thm-bar-selected'],
        ['barSelectedBorder', '--bit-rtm-thm-bar-selected-border'],
        ['barLabel', '--bit-rtm-thm-bar-label'],
        ['now', '--bit-rtm-thm-now'],
        ['selectionFill', '--bit-rtm-thm-selection-fill'],
        ['selectionBorder', '--bit-rtm-thm-selection-border'],
        ['focus', '--bit-rtm-thm-focus'],
        ['tooltipBg', '--bit-rtm-thm-tooltip-bg'],
        ['tooltipText', '--bit-rtm-thm-tooltip-text'],
        ['nonWorking', '--bit-rtm-thm-non-working'],
    ];

    // The texts of the component that only its markup uses (BitResourceTimelineTexts), left out of the ones handed to
    // the engine, which warns about a text it does not know.
    const MARKUP_TEXTS = ['ariaLabel', 'roleDescription', 'loading', 'expandGroupFormat', 'collapseGroupFormat'];

    // The attributes of the root and its ancestors a theme is switched through: a class or an inline style carrying
    // variables, the bit-theme scope (and the bit-theme-overlay of an inline theme), the theme-scoping data-*theme /
    // -scheme / -mode ones. Any other attribute (an aria-expanded, a data-state) changes no color, so its mutations are
    // not worth a read of the theme.
    const THEME_ATTRIBUTE = /^(class|style|bit-theme|bit-theme-overlay|data-.*(theme|scheme|mode))$/;

    class ResourceTimelineInstance {
        public engine: ResourceTimelineEngine;

        private _root: HTMLElement;
        private _probe: HTMLElement | null = null;
        private _themeFingerprint = '';
        private _themeFrame: number | null = null;
        private _themeObserver: MutationObserver | null = null;
        private _schemeQuery: MediaQueryList | null = null;
        private _onThemeChange: () => void;

        constructor(viewport: HTMLElement, dotnetObj: DotNetObject, options: any, texts: any, disabled: boolean) {
            this._root = viewport.parentElement || viewport;

            const registry: ResourceTimelineRendererRegistry = {
                canvas: ResourceTimelineCanvasRenderer,
                svg: ResourceTimelineSvgRenderer,
                html: ResourceTimelineHtmlRenderer
            };

            this.engine = new ResourceTimelineEngine(viewport, dotnetObj, ResourceTimelineInstance.engineOptions(options, texts, disabled), registry);

            this._onThemeChange = () => this.scheduleThemeCheck();

            // Read before the first frame (no data has arrived yet), so the timeline never paints in the built-in colors
            // first and in the theme's a frame later.
            this.applyTheme(true);
            this.watchTheme();
        }

        public setOptions(options: any, texts: any, disabled: boolean) {
            this.engine.setOptions(ResourceTimelineInstance.engineOptions(options, texts, disabled));

            // The colors are read again with the options, which covers what no observer sees (a stylesheet rule the page
            // swapped in).
            this.applyTheme(false);
        }

        public dispose() {
            if (this._themeFrame !== null) {
                cancelAnimationFrame(this._themeFrame);
                this._themeFrame = null;
            }

            this._themeObserver?.disconnect();
            this._themeObserver = null;

            if (this._schemeQuery) {
                try {
                    this._schemeQuery.removeEventListener('change', this._onThemeChange);
                } catch { /* an engine without the EventTarget form of MediaQueryList */ }
                this._schemeQuery = null;
            }

            this._probe?.remove();
            this._probe = null;

            this.engine.dispose();
        }

        // The options as the engine takes them: the Options of the component, its texts (less the ones its markup uses)
        // and its disabled state, in one object.
        private static engineOptions(options: any, texts: any, disabled: boolean) {
            const result: any = { ...(options || {}) };

            if (texts) {
                const engineTexts: any = {};
                for (const key of Object.keys(texts)) {
                    if (MARKUP_TEXTS.indexOf(key) < 0) {
                        engineTexts[key] = texts[key];
                    }
                }
                result.texts = engineTexts;
            }

            result.disabled = !!disabled;

            return result;
        }

        private applyTheme(force: boolean) {
            const theme = this.readTheme();
            if (!theme) return;

            const fingerprint = ResourceTimelineInstance.fingerprint(theme);
            if (!force && fingerprint === this._themeFingerprint) return;

            this._themeFingerprint = fingerprint;
            this.engine.setTheme(theme);
        }

        // The theme colors and font family in force on the root of the component, resolved by the browser the way the
        // page itself resolves them.
        private readTheme(): ResourceTimelineTheme | null {
            const probe = this.ensureProbe();
            if (!probe) return null;

            const colors: { [key: string]: string } = {};

            try {
                for (const [key, variable] of THEME_COLORS) {
                    probe.style.backgroundColor = `var(${variable})`;

                    const value = getComputedStyle(probe).backgroundColor;

                    if (value && value !== 'transparent' && !/^rgba\([^)]*,\s*0\)$/.test(value)) {
                        colors[key] = value;
                    }
                }

                const fontFamily = getComputedStyle(this._root).fontFamily || null;

                return { colors, fontFamily };
            } catch {
                return null;
            }
        }

        private ensureProbe() {
            if (this._probe && this._probe.isConnected) return this._probe;

            if (!this._root.isConnected) return null;

            const probe = document.createElement('span');
            probe.className = 'bit-rtm-prb';
            probe.setAttribute('aria-hidden', 'true');
            this._root.appendChild(probe);
            this._probe = probe;

            return probe;
        }

        private static fingerprint(theme: ResourceTimelineTheme) {
            return THEME_COLORS.map(([key]) => theme.colors[key] || '').join('|') + '|' + (theme.fontFamily || '');
        }

        // A theme is switched on the document (a bit-theme attribute, a class or a style of the html or the body element),
        // on a region of the page around the timeline (a [bit-theme] scope, a BitThemeProvider's inline variables, a
        // class of an ancestor), on the root itself (its Class or Style), or by the color scheme of the system. Nothing
        // reports a change of a variable, so the theme attributes (THEME_ATTRIBUTE) of the root and of every ancestor up
        // to the html element are watched, and the colors read again after the frame every mutation of one switch has
        // landed in: the fingerprint of what was read decides whether anything actually changed. A change of the
        // stylesheet rules alone mutates no attribute, and is read again with the next options instead.
        private watchTheme() {
            try {
                this._themeObserver = new MutationObserver(records => {
                    for (const record of records) {
                        if (record.attributeName && THEME_ATTRIBUTE.test(record.attributeName)) {
                            this.scheduleThemeCheck();
                            return;
                        }
                    }
                });
                for (let el: Element | null = this._root; el; el = el.parentElement) {
                    this._themeObserver.observe(el, { attributes: true });
                }
            } catch { /* no observer: the theme is still read on every change of the options */ }

            try {
                const scheme = globalThis.matchMedia?.('(prefers-color-scheme: dark)');
                if (scheme) {
                    scheme.addEventListener('change', this._onThemeChange);
                    this._schemeQuery = scheme;
                }
            } catch { /* an engine without the EventTarget form of MediaQueryList */ }
        }

        private scheduleThemeCheck() {
            if (this._themeFrame !== null) return;

            this._themeFrame = requestAnimationFrame(() => {
                this._themeFrame = null;
                this.applyTheme(false);
            });
        }
    }
}

// bit-lazy: teardown ResourceTimeline.dispose
