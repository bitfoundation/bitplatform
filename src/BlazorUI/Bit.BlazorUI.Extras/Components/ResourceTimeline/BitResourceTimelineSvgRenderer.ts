namespace BitBlazorUI {
    // SVG renderer for the BitResourceTimeline engine: renders the scene built
    // by the engine (see BitResourceTimelineEngine.ts for the scene shape, and
    // BitResourceTimelineTypes.ts for the renderer contract) into an <svg>
    // element. The scene is already culled to the viewport, so the element count
    // stays bounded by what is visible.
    //
    // Compared to canvas, SVG output is resolution-independent (crisp at any
    // zoom/DPR without backing-store management), styleable and inspectable in
    // devtools, and copy/print friendly. Canvas remains faster for very dense
    // scenes.
    //
    // The group structure below is created once and kept; its children are pooled
    // and reused across frames (see BitResourceTimelineNodePool.ts), so a
    // steady-state frame creates no elements and writes only the attributes that
    // actually changed.

    const SVG_NS = 'http://www.w3.org/2000/svg';

    // Unique clip-path id prefix per renderer instance so multiple timelines on
    // one page never collide.
    let instanceCounter = 0;

    function el(name: string, attrs?: { [key: string]: string }): SVGElement {
        const node = document.createElementNS(SVG_NS, name);
        if (attrs) {
            for (const key of Object.keys(attrs)) node.setAttribute(key, attrs[key]);
        }
        return node;
    }

    // Maps the scene's canvas-style align/baseline to SVG text attributes. A
    // bottom anchor is the bottom edge of the text, descenders included, as it is
    // for canvas and the HTML renderer - not the alphabetic baseline 'auto' is.
    const ANCHOR: { [align: string]: string } = { left: 'start', center: 'middle', right: 'end' };
    const BASELINE: { [baseline: string]: string } = { top: 'hanging', middle: 'central', bottom: 'text-after-edge' };

    /** An element of one of the SVG renderer's pools. */
    type SvgPooledNode = SVGElement & ResourceTimelinePooledNodeState;
    type SvgNodePool = ResourceTimelineNodePool<SvgPooledNode>;

    // Takes a bar's data-bar-id off an element of the bar-shape pool that is
    // something else this frame (an edge, a section, an outline) or that the frame
    // did not use, so a reused element never surfaces a stale id.
    function clearBarIdentity(node: SvgPooledNode): void {
        if (node.__barId !== undefined) {
            node.removeAttribute('data-bar-id');
            node.__barId = undefined;
        }
    }

    type SvgRendererPools = {
        bg: SvgNodePool;
        surface: SvgNodePool;
        nonWorking: SvgNodePool;
        gridH: SvgNodePool;
        gridV: SvgNodePool;
        barShapes: SvgNodePool;
        barIcons: SvgNodePool;
        barLabels: SvgNodePool;
        overflowShapes: SvgNodePool;
        overflowLabels: SvgNodePool;
        now: SvgNodePool;
        axisBg: SvgNodePool;
        axisTints: SvgNodePool;
        axisLines: SvgNodePool;
        axisDayLabels: SvgNodePool;
        axisTicks: SvgNodePool;
        axisTickLabels: SvgNodePool;
        resourceAxis: SvgNodePool;
        resourceTints: SvgNodePool;
        resourceBorder: SvgNodePool;
        resourceLines: SvgNodePool;
        resChevrons: SvgNodePool;
        resLabels: SvgNodePool;
        overlayRects: SvgNodePool;
        overlayLabels: SvgNodePool;
    };

    export class ResourceTimelineSvgRenderer implements ResourceTimelineRenderer {
        public wrapper: HTMLElement;
        public host: ResourceTimelineRendererHost;
        public surface: SVGSVGElement;

        private _contentClipId: string;
        private _axisClipId: string;
        private _timeAxisClipId: string;
        private _tickLabelClipId: string;
        private _contentClipRect: SVGElement;
        private _axisClipRect: SVGElement;
        private _timeAxisClipRect: SVGElement;
        private _tickLabelClipRect: SVGElement;
        private _bgGroup: SVGElement;
        private _surfaceGroup: SVGElement;
        private _washGroup: SVGElement;
        private _gridGroup: SVGElement;
        private _barsGroup: SVGElement;
        private _nowGroup: SVGElement;
        private _timeAxisGroup: SVGElement;
        private _timeAxisFillGroup: SVGElement;
        private _timeAxisTintGroup: SVGElement;
        private _timeAxisMarkGroup: SVGElement;
        private _timeAxisTickLabelGroup: SVGElement;
        private _timeAxisLabelGroup: SVGElement;
        private _resourceAxisGroup: SVGElement;
        private _resourceFillGroup: SVGElement;
        private _resourceTintGroup: SVGElement;
        private _resourceMarkGroup: SVGElement;
        private _resourceLabelGroup: SVGElement;
        private _overlayGroup: SVGElement;
        private _pools: SvgRendererPools;

        constructor(wrapper: HTMLElement, host: ResourceTimelineRendererHost) {
            this.wrapper = wrapper;
            this.host = host;

            this.surface = el('svg') as SVGSVGElement;
            this.surface.setAttribute('class', 'bit-rtm-srf');
            wrapper.appendChild(this.surface);

            const id = ++instanceCounter;
            this._contentClipId = `bit-rtm-svg-content-clip-${id}`;
            this._axisClipId = `bit-rtm-svg-axis-clip-${id}`;
            this._timeAxisClipId = `bit-rtm-svg-time-axis-clip-${id}`;
            this._tickLabelClipId = `bit-rtm-svg-tick-label-clip-${id}`;

            // Persistent structure: <defs> with the clip paths, then one group
            // per layer in paint order, each backed by a pool.
            this._contentClipRect = el('rect');
            this._axisClipRect = el('rect');
            this._timeAxisClipRect = el('rect');
            this._tickLabelClipRect = el('rect');
            const defs = el('defs');
            const contentClip = el('clipPath', { id: this._contentClipId });
            contentClip.appendChild(this._contentClipRect);
            const axisClip = el('clipPath', { id: this._axisClipId });
            axisClip.appendChild(this._axisClipRect);
            const timeAxisClip = el('clipPath', { id: this._timeAxisClipId });
            timeAxisClip.appendChild(this._timeAxisClipRect);
            const tickLabelClip = el('clipPath', { id: this._tickLabelClipId });
            tickLabelClip.appendChild(this._tickLabelClipRect);
            defs.appendChild(contentClip);
            defs.appendChild(axisClip);
            defs.appendChild(timeAxisClip);
            defs.appendChild(tickLabelClip);
            this.surface.appendChild(defs);

            const group = (attrs?: { [key: string]: string }): SVGElement => {
                const g = el('g', attrs);
                this.surface.appendChild(g);
                return g;
            };
            this._bgGroup = group();
            // Own groups so a growing stripe list stays between the content fill
            // and the non-working wash, instead of appending over later siblings.
            this._surfaceGroup = group();
            this._washGroup = group();
            this._gridGroup = group();
            // Clipped to the content area, as the overlays are: the axes painted
            // later cover the rest of a bar's overhang, but not the top-start corner.
            this._barsGroup = group({ 'clip-path': `url(#${this._contentClipId})` });
            this._nowGroup = group();
            this._timeAxisGroup = group();
            // Fill, then column tints, then ticks, then the hour labels, then day
            // labels. The label group is last so a date-row tint cannot cover the
            // titles (and so the titles stay above the axis fill). The hour (and
            // UTC) labels are clipped to the time axis, so one centered on a tick
            // at the edge of the resource column does not paint into the corner.
            this._timeAxisFillGroup = el('g');
            this._timeAxisTintGroup = el('g');
            this._timeAxisMarkGroup = el('g');
            this._timeAxisTickLabelGroup = el('g', { 'clip-path': `url(#${this._tickLabelClipId})` });
            this._timeAxisLabelGroup = el('g', { 'clip-path': `url(#${this._timeAxisClipId})` });
            this._timeAxisGroup.appendChild(this._timeAxisFillGroup);
            this._timeAxisGroup.appendChild(this._timeAxisTintGroup);
            this._timeAxisGroup.appendChild(this._timeAxisMarkGroup);
            this._timeAxisGroup.appendChild(this._timeAxisTickLabelGroup);
            this._timeAxisGroup.appendChild(this._timeAxisLabelGroup);
            this._resourceAxisGroup = group();
            this._resourceFillGroup = el('g');
            this._resourceTintGroup = el('g');
            this._resourceMarkGroup = el('g');
            this._resourceAxisGroup.appendChild(this._resourceFillGroup);
            this._resourceAxisGroup.appendChild(this._resourceTintGroup);
            this._resourceAxisGroup.appendChild(this._resourceMarkGroup);
            this._resourceLabelGroup = group({ 'clip-path': `url(#${this._axisClipId})` });
            this._overlayGroup = group({ 'clip-path': `url(#${this._contentClipId})` });

            // One pool per homogeneous run of elements - see BitResourceTimelineNodePool.ts
            // for why a pool must not be fed a varying mix of tags. A bar's shapes - its
            // edges, its rect, its sections, its outline and focus ring, all <rect>s -
            // share one pool and are acquired bar by bar, so each bar's outline paints
            // before the next bar, which covers it where they overlap, as on the
            // canvas. Only the element acquired as a bar rect carries a data-bar-id
            // (see clearBarIdentity).
            const pool = (parent: SVGElement, onHide?: (node: SvgPooledNode) => void) =>
                new ResourceTimelineNodePool<SvgPooledNode>(parent, SVG_NS, onHide);
            this._pools = {
                bg: pool(this._bgGroup),
                surface: pool(this._surfaceGroup),
                nonWorking: pool(this._washGroup),
                gridH: pool(this._gridGroup),
                gridV: pool(this._gridGroup),
                barShapes: pool(this._barsGroup, clearBarIdentity),
                barIcons: pool(this._barsGroup),
                barLabels: pool(this._barsGroup),
                // The +N markers, above every bar label, as the canvas draws them after the bars.
                overflowShapes: pool(this._barsGroup),
                overflowLabels: pool(this._barsGroup),
                now: pool(this._nowGroup),
                axisBg: pool(this._timeAxisFillGroup),
                axisTints: pool(this._timeAxisTintGroup),
                axisLines: pool(this._timeAxisMarkGroup),
                axisDayLabels: pool(this._timeAxisLabelGroup),
                axisTicks: pool(this._timeAxisMarkGroup),
                axisTickLabels: pool(this._timeAxisTickLabelGroup),
                resourceAxis: pool(this._resourceFillGroup),
                resourceTints: pool(this._resourceTintGroup),
                resourceBorder: pool(this._resourceMarkGroup),
                resourceLines: pool(this._resourceMarkGroup),
                resChevrons: pool(this._resourceLabelGroup),
                resLabels: pool(this._resourceLabelGroup),
                overlayRects: pool(this._overlayGroup),
                overlayLabels: pool(this._overlayGroup)
            };
        }

        public resize(cssW: number, cssH: number): void {
            this.surface.style.width = cssW + 'px';
            this.surface.style.height = cssH + 'px';
            this.surface.setAttribute('viewBox', `0 0 ${cssW} ${cssH}`);
        }

        public render(scene: ResourceTimelineScene): void {
            const v = scene.viewport;

            // Keep the clip regions in sync with the (possibly changed) layout.
            // Content clip: the scrollable area under/right of the sticky axes.
            ResourceTimelineNodePool.applyAttrs(this._contentClipRect, {
                x: v.axisWidth, y: v.axisHeight,
                width: Math.max(0, v.width - v.axisWidth),
                height: Math.max(0, v.height - v.axisHeight)
            });
            // Axis clip: the resource column below the time axis, so long labels
            // don't bleed across the divider into the content area.
            ResourceTimelineNodePool.applyAttrs(this._axisClipRect, {
                x: 0, y: v.axisHeight,
                width: Math.max(0, v.axisWidth - 1),
                height: Math.max(0, v.height - v.axisHeight)
            });
            // Time-axis clip: a pushed-out day title can sit left of the content
            // area; clip so it slides behind the resource column.
            ResourceTimelineNodePool.applyAttrs(this._timeAxisClipRect, {
                x: v.axisWidth, y: 0,
                width: Math.max(0, v.width - v.axisWidth),
                height: Math.max(0, v.dateRowHeight)
            });
            // Hour-label clip: the whole height of the time axis, the hour row(s)
            // under the date row included, from the edge of the resource column.
            ResourceTimelineNodePool.applyAttrs(this._tickLabelClipRect, {
                x: v.axisWidth, y: 0,
                width: Math.max(0, v.width - v.axisWidth),
                height: Math.max(0, v.axisHeight)
            });

            for (const key in this._pools) this._pools[key as keyof SvgRendererPools].begin();

            this._buildBackground(scene);
            this._buildSurface(scene);
            this._buildNonWorking(scene);
            this._buildGrid(scene);
            this._buildBars(scene);
            this._buildNowLine(scene);
            this._buildTimeAxis(scene);
            this._buildResourceAxis(scene);
            this._buildOverlays(scene);

            for (const key in this._pools) this._pools[key as keyof SvgRendererPools].end();
        }

        // Every attribute this renderer ever sets on a rect is written here, so a
        // reused element never keeps a stroke or opacity from a previous frame.
        private _rect(pool: SvgNodePool, x: number, y: number, width: number, height: number,
            fill?: string | null, stroke?: string | null, strokeWidth?: number, dash?: string | null,
            fillOpacity?: number): SvgPooledNode {
            const node = pool.acquire('rect');
            ResourceTimelineNodePool.applyAttrs(node, {
                x, y,
                width: Math.max(0, width),
                height: Math.max(0, height),
                fill: fill || 'none',
                'fill-opacity': fillOpacity === undefined ? '1' : String(fillOpacity),
                stroke: stroke || 'none',
                'stroke-width': strokeWidth === undefined ? '1' : String(strokeWidth),
                'stroke-dasharray': dash || 'none'
            });
            return node;
        }

        private _line(pool: SvgNodePool, x1: number, y1: number, x2: number, y2: number,
            stroke: string, width?: number): SvgPooledNode {
            const node = pool.acquire('line');
            ResourceTimelineNodePool.applyAttrs(node, {
                x1, y1, x2, y2,
                stroke,
                'stroke-width': width === undefined ? 1 : width,
                'shape-rendering': 'crispEdges'
            });
            return node;
        }

        private _text(pool: SvgNodePool, content: string, x: number, y: number,
            font: string, fill: string, align: string, baseline: string): SvgPooledNode {
            const node = pool.acquire('text');
            ResourceTimelineNodePool.applyAttrs(node, {
                x, y, fill,
                'text-anchor': ANCHOR[align] || 'start',
                'dominant-baseline': BASELINE[baseline] || 'auto'
            });
            if (node.__font !== font) {
                node.style.font = font;
                node.__font = font;
            }
            ResourceTimelineNodePool.setText(node, content);
            return node;
        }

        private _buildBackground(scene: ResourceTimelineScene): void {
            const pool = this._pools.bg;
            const colors = scene.config.colors;
            const v = scene.viewport;
            const contentWidth = v.width - v.axisWidth;
            const contentHeight = v.height - v.axisHeight;

            this._rect(pool, v.axisWidth, v.axisHeight, contentWidth, contentHeight, colors.contentBg);
            this._rect(pool, 0, 0, v.axisWidth, v.axisHeight, colors.axisBg);
            this._rect(pool, 0, v.axisHeight, v.axisWidth, contentHeight, colors.axisBg);
            this._rect(pool, v.axisWidth, 0, contentWidth, v.axisHeight, colors.axisBg);
        }

        private _buildSurface(scene: ResourceTimelineScene): void {
            const bands = scene.surface;
            if (!bands || !bands.length) return;
            for (const b of bands) {
                this._rect(this._pools.surface, b.x, b.y, b.width, b.height, b.color);
            }
        }

        private _buildNonWorking(scene: ResourceTimelineScene): void {
            const bands = scene.nonWorking;
            if (!bands || !bands.length) return;
            const fill = scene.config.colors.nonWorking || 'rgba(0, 0, 0, 0.06)';
            for (const b of bands) {
                this._rect(this._pools.nonWorking, b.x, b.y, b.width, b.height, fill);
            }
        }

        private _buildGrid(scene: ResourceTimelineScene): void {
            const grid = scene.config.colors.grid;
            const v = scene.viewport;
            for (const line of scene.gridH) {
                this._line(this._pools.gridH, v.axisWidth, line.y, v.width, line.y, line.color || grid);
            }
            for (const line of scene.gridV) {
                this._line(this._pools.gridV, line.x, v.axisHeight, line.x, v.height, line.color || grid);
            }
        }

        private _buildBars(scene: ResourceTimelineScene): void {
            const p = this._pools;
            const c = scene.config;

            for (const bar of scene.bars) {
                if (bar.edges) {
                    for (const edge of bar.edges) {
                        clearBarIdentity(this._rect(p.barShapes, edge.x, edge.y, edge.width, edge.height, edge.color));
                    }
                }

                const barEl = this._rect(p.barShapes, bar.x, bar.y, bar.width, bar.height, bar.color);
                if (barEl.__barId !== bar.id) {
                    barEl.setAttribute('data-bar-id', bar.id);
                    barEl.__barId = bar.id;
                }

                if (bar.sections) {
                    for (const s of bar.sections) {
                        // A bordered section is inset by half the stroke, which
                        // keeps the 1px border inside it and the fill still
                        // reaching its edges under the stroke.
                        if (s.borderColor) {
                            clearBarIdentity(this._rect(p.barShapes, s.x + 0.5, s.y + 0.5, s.width - 1, s.height - 1,
                                s.color, s.borderColor, 1));
                        } else {
                            clearBarIdentity(this._rect(p.barShapes, s.x, s.y, s.width, s.height, s.color));
                        }
                    }
                }

                if (bar.outline) {
                    clearBarIdentity(this._rect(p.barShapes, bar.outline.x, bar.outline.y, bar.outline.width, bar.outline.height,
                        null, c.colors.barSelectedBorder, 2));
                }

                if (bar.focusRing) {
                    clearBarIdentity(this._rect(p.barShapes, bar.focusRing.x, bar.focusRing.y, bar.focusRing.width, bar.focusRing.height,
                        null, c.colors.focus, 2, '3 2'));
                }

                if (bar.icons) {
                    for (const icon of bar.icons) {
                        // Only emit an <image> once the engine's cache reports the
                        // source as loaded, so a not-yet-decoded icon doesn't flash
                        // in as a broken box. The cache load triggers a re-render.
                        const cached = this.host.getImage(icon.source);
                        if (!cached || !cached.complete || cached.naturalWidth === 0) continue;
                        const img = p.barIcons.acquire('image');
                        ResourceTimelineNodePool.applyAttrs(img, {
                            href: icon.source,
                            x: icon.x, y: icon.y,
                            width: icon.width, height: icon.height,
                            // The scene already fitted the box to the image's
                            // aspect ratio.
                            preserveAspectRatio: 'none'
                        });
                    }
                }

                if (bar.labels) {
                    for (const label of bar.labels) {
                        this._text(p.barLabels, label.text, label.x, label.y,
                            c.barLabelFont, label.color || c.colors.barLabel, label.align, label.baseline);
                    }
                }
            }

            for (const n of scene.overflow || []) {
                this._rect(p.overflowShapes, n.x, n.y, n.width, n.height,
                    c.colors.axisBg, c.colors.axisBorder, 1);
                this._text(p.overflowLabels, n.text, n.x + 3, n.y + n.height / 2,
                    c.barLabelFont, c.colors.dateLabel, 'left', 'middle');
            }
        }

        private _buildNowLine(scene: ResourceTimelineScene): void {
            if (scene.nowX == null) return;
            const pool = this._pools.now;
            const colors = scene.config.colors;
            const v = scene.viewport;
            const x = scene.nowX;

            const line = pool.acquire('line');
            ResourceTimelineNodePool.applyAttrs(line, {
                x1: x, y1: v.axisHeight, x2: x, y2: v.height,
                stroke: colors.now, 'stroke-width': 2
            });
            // Small triangular marker at the top of the line.
            const marker = pool.acquire('path');
            ResourceTimelineNodePool.applyAttrs(marker, {
                d: `M ${x - 4} ${v.axisHeight} L ${x + 4} ${v.axisHeight} L ${x} ${v.axisHeight + 5} Z`,
                fill: colors.now
            });
        }

        private _buildTimeAxis(scene: ResourceTimelineScene): void {
            const p = this._pools;
            const colors = scene.config.colors;
            const v = scene.viewport;
            const startX = v.axisWidth;

            this._rect(p.axisBg, startX, 0, v.width - startX, v.axisHeight, colors.axisBg);
            const timeTints = scene.surfaceTimeAxis;
            if (timeTints) {
                for (const b of timeTints) {
                    this._rect(p.axisTints, b.x, b.y, b.width, b.height, b.color);
                }
            }
            // Bottom border of the whole axis, the date/hour row divider, then the
            // day separators - all lines, so they share one pool.
            this._line(p.axisLines, startX, v.axisHeight, v.width, v.axisHeight, colors.axisBorder);
            this._line(p.axisLines, startX, v.dateRowHeight, v.width, v.dateRowHeight, colors.axisBorder);
            if (v.utcRowY != null) {
                // Divider between the UTC row and the zone row below it.
                this._line(p.axisLines, startX, v.utcRowY, v.width, v.utcRowY, colors.axisBorder);
            }
            for (const day of scene.days) {
                if (day.sepX != null) {
                    this._line(p.axisLines, day.sepX, 0, day.sepX, v.dateRowHeight,
                        day.sepColor || colors.axisBorder);
                }
            }
            for (const day of scene.days) {
                if (day.label != null) {
                    this._text(p.axisDayLabels, day.label, day.labelX, day.labelY,
                        scene.config.dateLabelFont, colors.dateLabel, 'left', 'middle');
                }
            }

            // The optional UTC row is drawn exactly like the zone row, with its
            // ticks rising from its own baseline (the divider above the zone row)
            // rather than the bottom of the axis.
            for (const tick of scene.hourTicks) {
                this._line(p.axisTicks, tick.x, v.axisHeight - 8, tick.x, v.axisHeight, colors.tick);
            }
            for (const tick of scene.utcTicks) {
                this._line(p.axisTicks, tick.x, v.utcRowY! - 8, tick.x, v.utcRowY!, colors.tick);
            }
            for (const tick of scene.hourTicks) {
                this._text(p.axisTickLabels, tick.label, tick.x, tick.labelY,
                    scene.config.hourLabelFont, colors.label, 'center', 'middle');
            }
            for (const tick of scene.utcTicks) {
                this._text(p.axisTickLabels, tick.label, tick.x, tick.labelY,
                    scene.config.hourLabelFont, colors.label, 'center', 'middle');
            }
        }

        private _buildResourceAxis(scene: ResourceTimelineScene): void {
            const c = scene.config;
            const colors = c.colors;
            const v = scene.viewport;
            const startY = v.axisHeight;

            this._rect(this._pools.resourceAxis, 0, startY, v.axisWidth, v.height - startY, colors.axisBg);
            const rowTints = scene.surfaceResourceAxis;
            if (rowTints) {
                for (const b of rowTints) {
                    this._rect(this._pools.resourceTints, b.x, b.y, b.width, b.height, b.color);
                }
            }
            const rowLines = scene.resourceAxisLines;
            if (rowLines) {
                for (const line of rowLines) {
                    this._line(this._pools.resourceLines, 0, line.y, v.axisWidth, line.y, line.color);
                }
            }
            this._line(this._pools.resourceBorder, v.axisWidth, startY, v.axisWidth, v.height, colors.axisBorder);

            // When the HTML resource-column template overlay is active it renders
            // the labels/chevrons; only the axis background/border is painted.
            if (!scene.resourceRows) return;

            const p = this._pools;
            for (const row of scene.resourceRows) {
                if (row.hasChildren) {
                    this._text(p.resChevrons, row.collapsed ? '▶' : '▼', row.leftPad, row.midY,
                        c.resourceChevronFont, colors.label, 'left', 'middle');
                    this._text(p.resLabels, row.name, row.leftPad + c.resourceChevronGap, row.midY,
                        c.resourceGroupFont, colors.dateLabel, 'left', 'middle');
                } else {
                    this._text(p.resLabels, row.name, row.leftPad, row.midY,
                        c.resourceLabelFont, colors.label, 'left', 'middle');
                }
            }
        }

        private _buildOverlays(scene: ResourceTimelineScene): void {
            const rects = this._pools.overlayRects;
            const c = scene.config;
            const colors = c.colors;

            const m = scene.marquee;
            if (m) {
                this._rect(rects, m.x, m.y, m.width, m.height, colors.selectionFill);
                this._rect(rects, m.x + 0.5, m.y + 0.5, m.width, m.height,
                    null, colors.selectionBorder, 1);
            }

            const g = scene.ghost;
            if (g) {
                this._rect(rects, g.x, g.y, g.width, g.height, g.color, null, undefined, null, 0.7);
                this._rect(rects, g.x + 0.5, g.y - 0.5, g.width, g.height + 1,
                    null, c.colors.barSelectedBorder, 1, '4 2');
                // Time readout above the ghost for precise feedback while dragging
                // (below it next to the time axis, see labelBelow).
                if (g.label) {
                    this._text(this._pools.overlayLabels, g.label, g.x,
                        g.labelBelow ? g.y + g.height + 2 : g.y - 2,
                        c.barLabelFont, colors.dateLabel, 'left', g.labelBelow ? 'top' : 'bottom');
                }
            }
        }

        public dispose(): void {
            for (const key in this._pools) this._pools[key as keyof SvgRendererPools].clear();
            this.surface.remove();
        }
    }
}
