namespace BitBlazorUI {
    // The scene the engine of the BitResourceTimeline (BitResourceTimelineEngine.ts, buildScene) hands its renderer
    // every frame, as the renderers read it: a plain display list for one frame, already culled to the viewport, with
    // every coordinate in CSS pixels of viewport space. Types alone - they compile to nothing.

    /** A rectangle of the scene. */
    export interface ResourceTimelineSceneRect {
        x: number;
        y: number;
        width: number;
        height: number;
    }

    /** A filled rectangle: a surface stripe, cell or band, an axis tint, a bar's edge (delay) bar. */
    export interface ResourceTimelineSceneBand extends ResourceTimelineSceneRect {
        color: string;
    }

    /** A horizontal grid line (a null color is colors.grid). */
    export interface ResourceTimelineSceneHLine {
        y: number;
        color: string | null;
        rowIndex?: number;
    }

    /** A row line across the resource column, drawn only for a row that has a line color of its own. */
    export interface ResourceTimelineSceneRowLine {
        y: number;
        color: string;
    }

    /** A vertical grid line (a null color is colors.grid). */
    export interface ResourceTimelineSceneVLine {
        x: number;
        ts?: number | null;
        color: string | null;
    }

    /** A day of the date row: the separator at its start and its pinned title, each null when not shown. */
    export interface ResourceTimelineSceneDay {
        sepX: number | null;
        sepTs?: number | null;
        sepColor: string | null;
        label: string | null;
        labelX: number;
        labelY: number;
    }

    /** An hour tick of the hour row (or of the UTC row) and its label. */
    export interface ResourceTimelineSceneTick {
        x: number;
        ts: number;
        label: string;
        labelY: number;
    }

    /** A text placed canvas-style: (x, y) is the anchor the align and the baseline are relative to. */
    export interface ResourceTimelineSceneLabel {
        text: string;
        x: number;
        y: number;
        align: 'left' | 'center' | 'right';
        baseline: 'top' | 'middle' | 'bottom';
        /** A section's text carries its own color; the other labels are colors.barLabel. */
        color?: string | null;
    }

    /** An icon image of a bar, its box already fitted to the image's aspect ratio. */
    export interface ResourceTimelineSceneIcon extends ResourceTimelineSceneRect {
        source: string;
    }

    /** A section of a bar: a fill, a 1px border, or both (a null fill leaves the bar's own showing). */
    export interface ResourceTimelineSceneSection extends ResourceTimelineSceneRect {
        color: string | null;
        borderColor: string | null;
    }

    /** A bar and its decorations, each decoration null when the bar has none. */
    export interface ResourceTimelineSceneBar extends ResourceTimelineSceneRect {
        id: string;
        color: string;
        selected: boolean;
        className: string;
        edges: ResourceTimelineSceneBand[] | null;
        sections: ResourceTimelineSceneSection[] | null;
        outline: ResourceTimelineSceneRect | null;
        focusRing: ResourceTimelineSceneRect | null;
        icons: ResourceTimelineSceneIcon[] | null;
        labels: ResourceTimelineSceneLabel[] | null;
    }

    /** The +N marker of a cluster whose bars did not all fit. */
    export interface ResourceTimelineSceneOverflow extends ResourceTimelineSceneRect {
        text: string;
    }

    /** A row of the resource column. */
    export interface ResourceTimelineSceneResourceRow {
        midY: number;
        leftPad: number;
        name: string;
        hasChildren: boolean;
        collapsed: boolean;
    }

    /** The preview of the allocation being edited, with its time readout. */
    export interface ResourceTimelineSceneGhost extends ResourceTimelineSceneRect {
        color: string;
        label: string | null;
    }

    /** The size of the viewport and of its sticky axes. */
    export interface ResourceTimelineSceneViewport {
        width: number;
        height: number;
        axisWidth: number;
        axisHeight: number;
        dateRowHeight: number;
        /** The divider between the UTC row and the zone row, or null when the UTC row is off. */
        utcRowY: number | null;
    }

    /** The resolved colors of the scene's config. */
    export interface ResourceTimelineSceneColors {
        contentBg: string;
        axisBg: string;
        axisBorder: string;
        tick: string;
        label: string;
        dateLabel: string;
        grid: string;
        barLabel: string;
        barSelectedBorder: string;
        now: string;
        selectionFill: string;
        selectionBorder: string;
        focus: string;
        nonWorking: string;
        [key: string]: string;
    }

    /** The snapshot of the engine's config a scene carries: what the renderers read of it, and the rest of it. */
    export interface ResourceTimelineSceneConfig {
        colors: ResourceTimelineSceneColors;
        barLabelFont: string;
        dateLabelFont: string;
        hourLabelFont: string;
        resourceLabelFont: string;
        resourceGroupFont: string;
        resourceChevronFont: string;
        resourceChevronGap: number;
        [key: string]: any;
    }

    /** One frame's display list. */
    export interface ResourceTimelineScene {
        config: ResourceTimelineSceneConfig;
        viewport: ResourceTimelineSceneViewport;
        days: ResourceTimelineSceneDay[];
        /** The axis-zone hour row. */
        hourTicks: ResourceTimelineSceneTick[];
        /** The optional UTC row (empty when off). */
        utcTicks: ResourceTimelineSceneTick[];
        gridH: ResourceTimelineSceneHLine[];
        gridV: ResourceTimelineSceneVLine[];
        /** Row lines inside the resource column, only for rows that have their own line color. */
        resourceAxisLines: ResourceTimelineSceneRowLine[] | null;
        /** Null when the HTML resource-column template overlay is active. */
        resourceRows: ResourceTimelineSceneResourceRow[] | null;
        bars: ResourceTimelineSceneBar[];
        overflow: ResourceTimelineSceneOverflow[] | null;
        nonWorking: ResourceTimelineSceneRect[] | null;
        /** Content-pane fills, in paint order. */
        surface: ResourceTimelineSceneBand[] | null;
        /** Tints of the date row, under its labels. */
        surfaceTimeAxis: ResourceTimelineSceneBand[] | null;
        /** Tints of the resource column, under its labels. */
        surfaceResourceAxis: ResourceTimelineSceneBand[] | null;
        nowX: number | null;
        marquee: ResourceTimelineSceneRect | null;
        ghost: ResourceTimelineSceneGhost | null;
    }

    // Canvas renderer for the BitResourceTimeline engine: immediate-mode paint
    // of the scene built by the engine each frame (see BitResourceTimelineEngine.ts
    // for the scene shape, and BitResourceTimelineTypes.ts for the renderer
    // contract). This is the default renderer and the fastest one for large,
    // dense datasets.
    //
    // The renderer owns everything canvas-specific: the <canvas> surface element,
    // the HiDPI backing store (sized in device pixels for crisp output) and the
    // device-pixel ResizeObserver that tracks devicePixelRatio changes.
    export class ResourceTimelineCanvasRenderer implements ResourceTimelineRenderer {
        public wrapper: HTMLElement;
        public host: ResourceTimelineRendererHost;
        public surface: HTMLCanvasElement;
        public ctx: CanvasRenderingContext2D;

        private _cssW: number;
        private _cssH: number;
        private _deviceW: number;
        private _deviceH: number;
        private _scaleX: number;
        private _scaleY: number;
        private _resizeObserver: ResizeObserver | null;
        private _observesDevicePixels: boolean;

        constructor(wrapper: HTMLElement, host: ResourceTimelineRendererHost) {
            this.wrapper = wrapper;
            this.host = host;

            this.surface = document.createElement('canvas');
            this.surface.className = 'bit-rtm-srf';
            wrapper.appendChild(this.surface);
            this.ctx = this.surface.getContext('2d', { alpha: false }) as CanvasRenderingContext2D;

            // Canvas backing-store size in device pixels (for crisp HiDPI output)
            // and the resulting CSS-pixel -> device-pixel scale factors.
            this._cssW = 0;
            this._cssH = 0;
            this._deviceW = 0;
            this._deviceH = 0;
            this._scaleX = 1;
            this._scaleY = 1;

            // Observe the canvas backing-store size in device pixels (via
            // device-pixel-content-box where supported) so devicePixelRatio
            // changes (browser zoom, moving between monitors) are picked up.
            this._resizeObserver = new ResizeObserver((entries: ResizeObserverEntry[]) => {
                for (const entry of entries) {
                    const dpc = entry.devicePixelContentBoxSize && entry.devicePixelContentBoxSize[0];
                    if (dpc) {
                        this._deviceW = dpc.inlineSize;
                        this._deviceH = dpc.blockSize;
                    }
                }
                // Re-apply the backing store at the new density and repaint.
                // Changing canvas.width/height clears the bitmap; paint synchronously
                // when that happens so a blank frame is never presented.
                if (this._cssW > 0 && this._cssH > 0) {
                    if (this._applyBackingStore()) {
                        if (typeof this.host.paintNow === 'function') this.host.paintNow();
                        else this.host.requestRender();
                    } else {
                        this.host.requestRender();
                    }
                }
            });
            try {
                this._resizeObserver.observe(this.surface, { box: 'device-pixel-content-box' });
                this._observesDevicePixels = true;
            } catch {
                // Safari does not support device-pixel-content-box; fall back to
                // CSS pixels multiplied by devicePixelRatio on each resize.
                this._resizeObserver.disconnect();
                this._resizeObserver = null;
                this._observesDevicePixels = false;
            }
        }

        // Returns true when the device-pixel backing store was resized (and thus
        // cleared). Callers that need a continuous picture should paint immediately.
        public resize(cssW: number, cssH: number): boolean {
            this._cssW = cssW;
            this._cssH = cssH;
            this.surface.style.width = cssW + 'px';
            this.surface.style.height = cssH + 'px';
            return this._applyBackingStore();
        }

        // Sizes the backing store in device pixels. The scale is capped so very
        // high DPR displays (some phones report 3-4) don't multiply the fill cost.
        // Returns true when width/height were written (bitmap cleared by the browser).
        private _applyBackingStore(): boolean {
            const cssW = this._cssW;
            const cssH = this._cssH;
            if (cssW === 0 || cssH === 0) return false;

            const maxScale = 2;
            let deviceW: number, deviceH: number;
            if (this._observesDevicePixels &&
                this._deviceW > 0 && this._deviceH > 0 && this._deviceW <= cssW * maxScale) {
                // Exact device-pixel size from the observer: pixel-perfect.
                deviceW = this._deviceW;
                deviceH = this._deviceH;
            } else {
                const scale = Math.min(maxScale, window.devicePixelRatio || 1);
                deviceW = Math.round(cssW * scale);
                deviceH = Math.round(cssH * scale);
            }

            let cleared = false;
            if (this.surface.width !== deviceW || this.surface.height !== deviceH) {
                // Assigning width/height resets the bitmap to transparent black.
                this.surface.width = deviceW;
                this.surface.height = deviceH;
                cleared = true;
            }
            this._scaleX = deviceW / cssW;
            this._scaleY = deviceH / cssH;
            return cleared;
        }

        public render(scene: ResourceTimelineScene): void {
            if (this.surface.width === 0 || this.surface.height === 0) return;
            const ctx = this.ctx;

            // Draw in CSS pixels: the transform maps CSS-pixel drawing onto the
            // (possibly higher-resolution) device-pixel backing store.
            //
            // No clearRect: the context is opaque (alpha: false) and _drawBackground
            // immediately fills the entire viewport, so clearing first was a wasted
            // full-surface fill - on a 4K display at 2x that is ~33M pixels a frame.
            ctx.setTransform(this._scaleX, 0, 0, this._scaleY, 0, 0);

            // z-order: background -> surface -> non-working -> grid -> bars ->
            // now line -> sticky axes -> marquee -> edit ghost (matches the scene contract).
            this._drawBackground(scene);
            this._drawSurface(scene);
            this._drawNonWorking(scene);
            this._drawGrid(scene);
            this._drawBars(scene);
            this._drawOverflow(scene);
            this._drawNowLine(scene);
            this._drawTimeAxis(scene);
            this._drawResourceAxis(scene);
            this._drawMarquee(scene);
            this._drawGhost(scene);
        }

        private _drawBackground(scene: ResourceTimelineScene): void {
            const { colors } = scene.config;
            const v = scene.viewport;
            const contentWidth = v.width - v.axisWidth;
            const contentHeight = v.height - v.axisHeight;

            this.ctx.fillStyle = colors.contentBg;
            this.ctx.fillRect(v.axisWidth, v.axisHeight, contentWidth, contentHeight);

            this.ctx.fillStyle = colors.axisBg;
            this.ctx.fillRect(0, 0, v.axisWidth, v.axisHeight);
            this.ctx.fillRect(0, v.axisHeight, v.axisWidth, contentHeight);
            this.ctx.fillRect(v.axisWidth, 0, contentWidth, v.axisHeight);
        }

        // One fillStyle per run of the same color. Surface rects are already in
        // paint order (rows, then columns, then bands — or checker cells).
        private _drawSurface(scene: ResourceTimelineScene): void {
            this._fillBands(scene.surface);
        }

        private _fillBands(bands: ResourceTimelineSceneBand[] | null | undefined): void {
            if (!bands || !bands.length) return;
            const ctx = this.ctx;
            let style: string | null = null;
            for (const b of bands) {
                if (b.color !== style) {
                    style = b.color;
                    ctx.fillStyle = style;
                }
                ctx.fillRect(b.x, b.y, b.width, b.height);
            }
        }

        private _drawNonWorking(scene: ResourceTimelineScene): void {
            const bands = scene.nonWorking;
            if (!bands || !bands.length) return;
            this.ctx.fillStyle = scene.config.colors.nonWorking || 'rgba(0, 0, 0, 0.06)';
            for (const b of bands) {
                this.ctx.fillRect(b.x, b.y, b.width, b.height);
            }
        }

        // Lines that share a color go into one path. A run of the default grid
        // color stays a single stroke; a colored line starts a new one. The
        // half-pixel offset puts a 1px line inside a pixel row rather than
        // straddling two, which is what made the grid look blurry.
        private _drawGrid(scene: ResourceTimelineScene): void {
            const ctx = this.ctx;
            const v = scene.viewport;
            const fallback = scene.config.colors.grid;
            ctx.lineWidth = 1;
            this._strokeHLines(scene.gridH, v.axisWidth, v.width, fallback,
                (line) => line.y, (line) => line.color);
            this._strokeVLines(scene.gridV, v.axisHeight, v.height, fallback,
                (line) => line.x, (line) => line.color);
        }

        private _strokeHLines<T>(lines: T[], x0: number, x1: number, fallback: string,
            yOf: (line: T) => number, colorOf: (line: T) => string | null | undefined): void {
            const ctx = this.ctx;
            let style: string | null = null;
            let open = false;
            const end = () => { if (open) { ctx.stroke(); open = false; } };
            for (let i = 0; i < lines.length; i++) {
                const color = colorOf(lines[i]) || fallback;
                if (color !== style) {
                    end();
                    ctx.beginPath();
                    ctx.strokeStyle = color;
                    style = color;
                    open = true;
                }
                const py = Math.round(yOf(lines[i])) + 0.5;
                ctx.moveTo(x0, py);
                ctx.lineTo(x1, py);
            }
            end();
        }

        private _strokeVLines<T>(lines: T[], y0: number, y1: number, fallback: string,
            xOf: (line: T) => number, colorOf: (line: T) => string | null | undefined): void {
            const ctx = this.ctx;
            let style: string | null = null;
            let open = false;
            const end = () => { if (open) { ctx.stroke(); open = false; } };
            for (let i = 0; i < lines.length; i++) {
                const color = colorOf(lines[i]) || fallback;
                if (color !== style) {
                    end();
                    ctx.beginPath();
                    ctx.strokeStyle = color;
                    style = color;
                    open = true;
                }
                const px = Math.round(xOf(lines[i])) + 0.5;
                ctx.moveTo(px, y0);
                ctx.lineTo(px, y1);
            }
            end();
        }

        private _drawBars(scene: ResourceTimelineScene): void {
            const ctx = this.ctx;
            const c = scene.config;
            // Assigning ctx.font re-parses the font string, so it is set once for
            // the whole pass rather than per labelled bar. fillStyle cannot be
            // hoisted the same way - each bar's fill overwrites it.
            let fontSet = false;

            for (const bar of scene.bars) {
                if (bar.edges) {
                    for (const edge of bar.edges) {
                        ctx.fillStyle = edge.color;
                        ctx.fillRect(edge.x, edge.y, edge.width, edge.height);
                    }
                }

                ctx.fillStyle = bar.color;
                ctx.fillRect(bar.x, bar.y, bar.width, bar.height);

                if (bar.sections) {
                    for (const section of bar.sections) {
                        if (section.color) {
                            ctx.fillStyle = section.color;
                            ctx.fillRect(section.x, section.y, section.width, section.height);
                        }
                        // Inset by half the line so the 1px border stays inside
                        // the section rather than straddling its edge.
                        if (section.borderColor) {
                            ctx.strokeStyle = section.borderColor;
                            ctx.lineWidth = 1;
                            ctx.strokeRect(section.x + 0.5, section.y + 0.5,
                                Math.max(0, section.width - 1), Math.max(0, section.height - 1));
                        }
                    }
                }

                if (bar.outline) {
                    ctx.strokeStyle = c.colors.barSelectedBorder;
                    ctx.lineWidth = 2;
                    ctx.strokeRect(bar.outline.x, bar.outline.y, bar.outline.width, bar.outline.height);
                }

                if (bar.focusRing) {
                    ctx.save();
                    ctx.strokeStyle = c.colors.focus;
                    ctx.lineWidth = 2;
                    ctx.setLineDash([3, 2]);
                    ctx.strokeRect(bar.focusRing.x, bar.focusRing.y, bar.focusRing.width, bar.focusRing.height);
                    ctx.restore();
                }

                if (bar.icons) {
                    for (const icon of bar.icons) {
                        const img = this.host.getImage(icon.source);
                        // Layout only emits icons whose image has loaded, but a
                        // cache eviction/reload keeps this guard cheap insurance.
                        if (!img || !img.complete || img.naturalWidth === 0) continue;
                        ctx.drawImage(img, icon.x, icon.y, icon.width, icon.height);
                    }
                }

                if (bar.labels) {
                    if (!fontSet) {
                        ctx.font = c.barLabelFont;
                        fontSet = true;
                    }
                    for (const label of bar.labels) {
                        // Section text carries its own color; other labels do not.
                        ctx.fillStyle = label.color || c.colors.barLabel;
                        ctx.textAlign = label.align;
                        ctx.textBaseline = label.baseline;
                        ctx.fillText(label.text, label.x, label.y);
                    }
                }
            }
        }

        private _drawOverflow(scene: ResourceTimelineScene): void {
            const labels = scene.overflow;
            if (!labels || !labels.length) return;
            const ctx = this.ctx;
            ctx.fillStyle = scene.config.colors.barLabel;
            ctx.font = scene.config.barLabelFont;
            ctx.textAlign = 'left';
            ctx.textBaseline = 'middle';
            for (const n of labels) {
                ctx.fillStyle = scene.config.colors.axisBg;
                ctx.fillRect(n.x, n.y, n.width, n.height);
                ctx.strokeStyle = scene.config.colors.axisBorder;
                ctx.strokeRect(n.x + 0.5, n.y + 0.5, n.width, n.height);
                ctx.fillStyle = scene.config.colors.dateLabel;
                ctx.fillText(n.text, n.x + 3, n.y + n.height / 2);
            }
        }

        private _drawNowLine(scene: ResourceTimelineScene): void {
            if (scene.nowX == null) return;
            const ctx = this.ctx;
            const v = scene.viewport;
            const x = scene.nowX;

            ctx.strokeStyle = scene.config.colors.now;
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(x, v.axisHeight);
            ctx.lineTo(x, v.height);
            ctx.stroke();

            // Small triangular marker at the top of the line.
            ctx.fillStyle = scene.config.colors.now;
            ctx.beginPath();
            ctx.moveTo(x - 4, v.axisHeight);
            ctx.lineTo(x + 4, v.axisHeight);
            ctx.lineTo(x, v.axisHeight + 5);
            ctx.closePath();
            ctx.fill();
        }

        private _drawTimeAxis(scene: ResourceTimelineScene): void {
            const ctx = this.ctx;
            const colors = scene.config.colors;
            const v = scene.viewport;
            const startX = v.axisWidth;
            const visibleEndX = v.width;
            const axisHeight = v.axisHeight;
            const dateRowHeight = v.dateRowHeight;

            ctx.fillStyle = colors.axisBg;
            ctx.fillRect(startX, 0, visibleEndX - startX, axisHeight);
            this._fillBands(scene.surfaceTimeAxis);

            // Bottom border of the whole axis.
            ctx.strokeStyle = colors.axisBorder;
            ctx.lineWidth = 1;
            ctx.beginPath();
            ctx.moveTo(startX, axisHeight);
            ctx.lineTo(visibleEndX, axisHeight);
            ctx.stroke();

            // Divider between the date row and the hour row(s), plus the one
            // between the UTC row and the zone row when both are shown.
            ctx.beginPath();
            ctx.moveTo(startX, dateRowHeight);
            ctx.lineTo(visibleEndX, dateRowHeight);
            if (v.utcRowY != null) {
                ctx.moveTo(startX, v.utcRowY);
                ctx.lineTo(visibleEndX, v.utcRowY);
            }
            ctx.stroke();

            // Date row: separators batched into one stroke, then the pinned labels.
            this._strokeVLines(
                scene.days.filter(day => day.sepX != null),
                0, dateRowHeight, colors.axisBorder,
                (day) => day.sepX as number, (day) => day.sepColor);

            ctx.font = scene.config.dateLabelFont;
            ctx.textBaseline = 'middle';
            ctx.textAlign = 'left';
            ctx.fillStyle = colors.dateLabel;
            // A pushed-out day title can sit left of the content area; clip so it
            // slides behind the resource column instead of painting over it.
            ctx.save();
            ctx.beginPath();
            ctx.rect(startX, 0, visibleEndX - startX, dateRowHeight);
            ctx.clip();
            for (const day of scene.days) {
                if (day.label != null) {
                    ctx.fillText(day.label, day.labelX, day.labelY);
                }
            }
            ctx.restore();

            // Hour row(s): ticks batched into one stroke, then the hour-of-day
            // labels. The optional UTC row is drawn exactly like the zone row, with
            // its ticks rising from its own baseline (the divider above the zone
            // row) rather than the bottom of the axis.
            ctx.strokeStyle = colors.tick;
            ctx.beginPath();
            for (const tick of scene.hourTicks) {
                const px = Math.round(tick.x) + 0.5;
                ctx.moveTo(px, axisHeight - 8);
                ctx.lineTo(px, axisHeight);
            }
            for (const tick of scene.utcTicks) {
                const px = Math.round(tick.x) + 0.5;
                ctx.moveTo(px, v.utcRowY! - 8);
                ctx.lineTo(px, v.utcRowY!);
            }
            ctx.stroke();

            ctx.textAlign = 'center';
            ctx.font = scene.config.hourLabelFont;
            ctx.fillStyle = colors.label;
            for (const tick of scene.hourTicks) {
                ctx.fillText(tick.label, tick.x, tick.labelY);
            }
            for (const tick of scene.utcTicks) {
                ctx.fillText(tick.label, tick.x, tick.labelY);
            }
        }

        private _drawResourceAxis(scene: ResourceTimelineScene): void {
            const ctx = this.ctx;
            const colors = scene.config.colors;
            const v = scene.viewport;
            const axisWidth = v.axisWidth;
            const startY = v.axisHeight;
            const visibleEndY = v.height;

            ctx.fillStyle = colors.axisBg;
            ctx.fillRect(0, startY, axisWidth, visibleEndY - startY);
            this._fillBands(scene.surfaceResourceAxis);
            const rowLines = scene.resourceAxisLines;
            if (rowLines && rowLines.length) {
                this._strokeHLines(rowLines, 0, axisWidth, colors.grid,
                    (line) => line.y, (line) => line.color);
            }

            ctx.strokeStyle = colors.axisBorder;
            ctx.lineWidth = 1;
            ctx.beginPath();
            ctx.moveTo(axisWidth, startY);
            ctx.lineTo(axisWidth, visibleEndY);
            ctx.stroke();

            // When an HTML resource-column template is active, the overlay draws
            // the labels/chevrons; only the axis background/border is painted.
            if (!scene.resourceRows) return;

            ctx.textBaseline = 'middle';

            // Clip labels to the axis so long (left-aligned, indented) names don't
            // bleed across the divider into the content area.
            ctx.save();
            ctx.beginPath();
            ctx.rect(0, startY, axisWidth - 1, visibleEndY - startY);
            ctx.clip();

            const c = scene.config;
            ctx.textAlign = 'left';
            for (const row of scene.resourceRows) {
                if (row.hasChildren) {
                    // Chevron: ▸ collapsed, ▾ expanded. Drawn left-aligned at the
                    // row's indent; clicking this band toggles the group.
                    ctx.fillStyle = colors.label;
                    ctx.font = c.resourceChevronFont;
                    ctx.fillText(row.collapsed ? '▶' : '▼', row.leftPad, row.midY);

                    ctx.fillStyle = colors.dateLabel;
                    ctx.font = c.resourceGroupFont;
                    ctx.fillText(row.name, row.leftPad + c.resourceChevronGap, row.midY);
                } else {
                    ctx.fillStyle = colors.label;
                    ctx.font = c.resourceLabelFont;
                    ctx.fillText(row.name, row.leftPad, row.midY);
                }
            }

            ctx.restore();
        }

        // Clips to the content area so the rectangle never overlaps the axes.
        private _clipContent(scene: ResourceTimelineScene): void {
            const v = scene.viewport;
            this.ctx.beginPath();
            this.ctx.rect(v.axisWidth, v.axisHeight, v.width - v.axisWidth, v.height - v.axisHeight);
            this.ctx.clip();
        }

        private _drawMarquee(scene: ResourceTimelineScene): void {
            const m = scene.marquee;
            if (!m) return;
            const ctx = this.ctx;
            const colors = scene.config.colors;

            ctx.save();
            this._clipContent(scene);
            ctx.fillStyle = colors.selectionFill;
            ctx.fillRect(m.x, m.y, m.width, m.height);
            ctx.strokeStyle = colors.selectionBorder;
            ctx.lineWidth = 1;
            ctx.strokeRect(m.x + 0.5, m.y + 0.5, m.width, m.height);
            ctx.restore();
        }

        private _drawGhost(scene: ResourceTimelineScene): void {
            const g = scene.ghost;
            if (!g) return;
            const ctx = this.ctx;
            const c = scene.config;

            ctx.save();
            this._clipContent(scene);

            ctx.globalAlpha = 0.7;
            ctx.fillStyle = g.color;
            ctx.fillRect(g.x, g.y, g.width, g.height);
            ctx.globalAlpha = 1;
            ctx.strokeStyle = c.colors.barSelectedBorder;
            ctx.lineWidth = 1;
            ctx.setLineDash([4, 2]);
            ctx.strokeRect(g.x + 0.5, g.y - 0.5, g.width, g.height + 1);
            ctx.setLineDash([]);

            // Time readout above the ghost for precise feedback while dragging.
            if (g.label) {
                ctx.font = c.barLabelFont;
                ctx.fillStyle = c.colors.dateLabel;
                ctx.textAlign = 'left';
                ctx.textBaseline = 'bottom';
                ctx.fillText(g.label, g.x, g.y - 2);
            }
            ctx.restore();
        }

        public dispose(): void {
            if (this._resizeObserver) {
                this._resizeObserver.disconnect();
                this._resizeObserver = null;
            }
            this.surface.remove();
        }
    }
}
