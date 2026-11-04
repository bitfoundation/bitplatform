namespace BitBlazorUI {
    // HTML renderer for the BitResourceTimeline engine: renders the scene built
    // by the engine (see BitResourceTimelineEngine.ts for the scene shape, and
    // BitResourceTimelineTypes.ts for the renderer contract) as
    // absolutely-positioned DOM elements. The scene is already culled to the
    // viewport, so the element count stays bounded by what is visible.
    //
    // Compared to canvas, HTML output is styleable with plain CSS (each bar is a
    // real element carrying its allocation id in data-bar-id), inspectable in
    // devtools and naturally crisp at any DPR. Canvas remains faster for very dense
    // scenes.
    //
    // The layer containers below are created once and kept; their children are
    // pooled and reused across frames (see BitResourceTimelineNodePool.ts), so a
    // steady-state frame creates no elements and writes only the properties that
    // actually changed.
    //
    // All children are pointer-events: none (inherited from the frame root), so
    // pointer interaction flows to the surface element exactly as with the other
    // renderers and the engine's geometric hit-testing stays authoritative.

    /** An element of one of the HTML renderer's pools (and of its persistent containers). */
    type HtmlPooledNode = HTMLElement & ResourceTimelinePooledNodeState;
    type HtmlNodePool = ResourceTimelineNodePool<HtmlPooledNode>;

    type HtmlRendererPools = {
        bg: HtmlNodePool;
        surface: HtmlNodePool;
        nonWorking: HtmlNodePool;
        gridH: HtmlNodePool;
        gridV: HtmlNodePool;
        barShapes: HtmlNodePool;
        barIcons: HtmlNodePool;
        barLabels: HtmlNodePool;
        now: HtmlNodePool;
        axisTints: HtmlNodePool;
        resourceTints: HtmlNodePool;
        resourceLines: HtmlNodePool;
        axisLines: HtmlNodePool;
        axisDayLabels: HtmlNodePool;
        axisTicks: HtmlNodePool;
        axisTickLabels: HtmlNodePool;
        resChevrons: HtmlNodePool;
        resLabels: HtmlNodePool;
        overlayRects: HtmlNodePool;
        overlayLabels: HtmlNodePool;
    };

    function div(style: { [key: string]: string }): HtmlPooledNode {
        const node = document.createElement('div');
        Object.assign(node.style, style);
        return node;
    }

    function px(value: number): string {
        return value + 'px';
    }

    // Takes the identity of a bar (its data-bar-id and className) off an element of
    // the bar-shape pool that is something else this frame (an edge, a section, an
    // outline) or that the frame did not use, so a reused element never surfaces a
    // stale id or class.
    function clearBarIdentity(node: HtmlPooledNode): void {
        if (node.__barId !== undefined) {
            node.removeAttribute('data-bar-id');
            node.__barId = undefined;
        }
        if (node.__className !== undefined) {
            node.className = '';
            node.__className = undefined;
        }
    }

    // Maps the scene's canvas-style align/baseline onto CSS transforms so the
    // (x, y) anchor matches canvas text positioning.
    const ALIGN_TX: { [align: string]: string } = { left: '0', center: '-50%', right: '-100%' };
    const BASELINE_TY: { [baseline: string]: string } = { top: '0', middle: '-50%', bottom: '-100%' };

    export class ResourceTimelineHtmlRenderer implements ResourceTimelineRenderer {
        public wrapper: HTMLElement;
        public host: ResourceTimelineRendererHost;
        public surface: HTMLElement;

        private _frame: HtmlPooledNode;
        private _bgLayer: HtmlPooledNode;
        private _surfaceLayer: HtmlPooledNode;
        private _washLayer: HtmlPooledNode;
        private _gridLayer: HtmlPooledNode;
        private _barsClip: HtmlPooledNode;
        private _barsLayer: HtmlPooledNode;
        private _nowLayer: HtmlPooledNode;
        private _timeAxis: HtmlPooledNode;
        private _timeAxisInner: HtmlPooledNode;
        private _timeAxisTint: HtmlPooledNode;
        private _resourceAxis: HtmlPooledNode;
        private _resourceAxisInner: HtmlPooledNode;
        private _resourceAxisTint: HtmlPooledNode;
        private _overlayClip: HtmlPooledNode;
        private _overlayInner: HtmlPooledNode;
        private _pools: HtmlRendererPools;

        constructor(wrapper: HTMLElement, host: ResourceTimelineRendererHost) {
            this.wrapper = wrapper;
            this.host = host;

            this.surface = div({ overflow: 'hidden' });
            this.surface.className = 'bit-rtm-srf';
            wrapper.appendChild(this.surface);

            // Persistent frame root. Children never receive pointer events; the
            // surface is the interaction target.
            this._frame = div({ position: 'absolute', inset: '0', pointerEvents: 'none' });
            this.surface.appendChild(this._frame);

            // Layers, in paint order. Each owns a pool of reusable children.
            const layer = (): HtmlPooledNode => {
                const el = div({ position: 'absolute', inset: '0' });
                this._frame.appendChild(el);
                return el;
            };
            this._bgLayer = layer();
            // Separate layers so stripe counts can grow without covering the wash
            // or the grid (pools that share a parent append new nodes at the end).
            this._surfaceLayer = layer();
            this._washLayer = layer();
            this._gridLayer = layer();
            // The bars are clipped to the content area, as the marquee and the
            // ghost are: the axes painted later cover the rest of their overhang,
            // but not the top-start corner. The inner element shifts the
            // viewport-space coordinates back into the clip box.
            this._barsClip = div({ position: 'absolute', overflow: 'hidden' });
            this._barsLayer = div({ position: 'absolute' });
            this._barsClip.appendChild(this._barsLayer);
            this._frame.appendChild(this._barsClip);
            this._nowLayer = layer();

            // The two axes clip their own contents; an inner element shifts the
            // viewport-space coordinates back into the clip box.
            this._timeAxis = div({ position: 'absolute', overflow: 'hidden', boxSizing: 'border-box' });
            this._timeAxisInner = div({ position: 'absolute', top: '0' });
            this._timeAxis.appendChild(this._timeAxisInner);
            this._frame.appendChild(this._timeAxis);
            // Zero-size anchor, appended before the label pools, so column tints
            // sit under the day titles and share the inner's viewport coordinates.
            this._timeAxisTint = div({ position: 'absolute', left: '0', top: '0', width: '0', height: '0' });
            this._timeAxisInner.appendChild(this._timeAxisTint);

            this._resourceAxis = div({ position: 'absolute', overflow: 'hidden', boxSizing: 'border-box' });
            this._resourceAxisInner = div({ position: 'absolute', left: '0' });
            this._resourceAxis.appendChild(this._resourceAxisInner);
            this._frame.appendChild(this._resourceAxis);
            this._resourceAxisTint = div({ position: 'absolute', left: '0', top: '0', width: '0', height: '0' });
            this._resourceAxisInner.appendChild(this._resourceAxisTint);

            // Marquee and edit ghost, clipped to the content area.
            this._overlayClip = div({ position: 'absolute', overflow: 'hidden' });
            this._overlayInner = div({ position: 'absolute' });
            this._overlayClip.appendChild(this._overlayInner);
            this._frame.appendChild(this._overlayClip);

            // One pool per homogeneous run. A bar's shapes - its edges, its rect,
            // its sections, its outline and focus ring, all divs - share one pool
            // and are acquired bar by bar, so each bar's outline paints before the
            // next bar, which covers it where they overlap, as on the canvas. Only
            // the element acquired as a bar rect carries a data-bar-id (see
            // clearBarIdentity). Icons and labels have pools of their own above
            // every bar shape - the intended stacking anyway.
            this._pools = {
                bg: new ResourceTimelineNodePool(this._bgLayer),
                surface: new ResourceTimelineNodePool(this._surfaceLayer),
                nonWorking: new ResourceTimelineNodePool(this._washLayer),
                gridH: new ResourceTimelineNodePool(this._gridLayer),
                gridV: new ResourceTimelineNodePool(this._gridLayer),
                barShapes: new ResourceTimelineNodePool(this._barsLayer, null, clearBarIdentity),
                barIcons: new ResourceTimelineNodePool(this._barsLayer),
                barLabels: new ResourceTimelineNodePool(this._barsLayer),
                now: new ResourceTimelineNodePool(this._nowLayer),
                axisTints: new ResourceTimelineNodePool(this._timeAxisTint),
                resourceTints: new ResourceTimelineNodePool(this._resourceAxisTint),
                resourceLines: new ResourceTimelineNodePool(this._resourceAxisTint),
                axisLines: new ResourceTimelineNodePool(this._timeAxisInner),
                axisDayLabels: new ResourceTimelineNodePool(this._timeAxisInner),
                axisTicks: new ResourceTimelineNodePool(this._timeAxisInner),
                axisTickLabels: new ResourceTimelineNodePool(this._timeAxisInner),
                resChevrons: new ResourceTimelineNodePool(this._resourceAxisInner),
                resLabels: new ResourceTimelineNodePool(this._resourceAxisInner),
                overlayRects: new ResourceTimelineNodePool(this._overlayInner),
                overlayLabels: new ResourceTimelineNodePool(this._overlayInner)
            };
        }

        public resize(cssW: number, cssH: number): void {
            this.surface.style.width = px(cssW);
            this.surface.style.height = px(cssH);
        }

        public render(scene: ResourceTimelineScene): void {
            for (const key in this._pools) this._pools[key as keyof HtmlRendererPools].begin();

            this._buildBackground(scene);
            this._buildSurface(scene);
            this._buildNonWorking(scene);
            this._buildGrid(scene);
            this._buildBars(scene);
            this._buildNowLine(scene);
            this._buildTimeAxis(scene);
            this._buildResourceAxis(scene);
            this._buildOverlays(scene);

            for (const key in this._pools) this._pools[key as keyof HtmlRendererPools].end();
        }

        // A positioned rectangle from the given pool. Every style key this renderer
        // ever sets on a rect is written here, so a reused element never inherits a
        // border or opacity from whatever it was last frame. The fill is written as
        // background-color rather than the background shorthand, so a bar's
        // className can still paint a background-image (stripes, a hatch) over it.
        private _rect(pool: HtmlNodePool, x: number, y: number, width: number, height: number,
            background?: string | null, border?: string | null, opacity?: number): HtmlPooledNode {
            const node = pool.acquire('div');
            ResourceTimelineNodePool.applyStyle(node, {
                position: 'absolute',
                left: px(x),
                top: px(y),
                width: px(Math.max(0, width)),
                height: px(Math.max(0, height)),
                backgroundColor: background || '',
                border: border || '',
                boxSizing: border ? 'border-box' : '',
                opacity: opacity === undefined ? '' : String(opacity)
            });
            return node;
        }

        private _text(pool: HtmlNodePool, content: string, x: number, y: number,
            font: string, color: string, align: string, baseline: string): HtmlPooledNode {
            const node = pool.acquire('div');
            ResourceTimelineNodePool.applyStyle(node, {
                position: 'absolute',
                left: px(x),
                top: px(y),
                transform: `translate(${ALIGN_TX[align] || '0'}, ${BASELINE_TY[baseline] || '0'})`,
                font: font,
                color: color,
                whiteSpace: 'nowrap',
                lineHeight: '1'
            });
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
                this._rect(this._pools.gridH, v.axisWidth, line.y, v.width - v.axisWidth, 1, line.color || grid);
            }
            for (const line of scene.gridV) {
                this._rect(this._pools.gridV, line.x, v.axisHeight, 1, v.height - v.axisHeight, line.color || grid);
            }
        }

        private _buildBars(scene: ResourceTimelineScene): void {
            const p = this._pools;
            const c = scene.config;
            const v = scene.viewport;

            ResourceTimelineNodePool.applyStyle(this._barsClip, {
                left: px(v.axisWidth),
                top: px(v.axisHeight),
                width: px(Math.max(0, v.width - v.axisWidth)),
                height: px(Math.max(0, v.height - v.axisHeight))
            });
            ResourceTimelineNodePool.applyStyle(this._barsLayer, { left: px(-v.axisWidth), top: px(-v.axisHeight) });

            for (const bar of scene.bars) {
                if (bar.edges) {
                    for (const edge of bar.edges) {
                        clearBarIdentity(this._rect(p.barShapes, edge.x, edge.y, edge.width, edge.height, edge.color));
                    }
                }

                const barEl = this._rect(p.barShapes, bar.x, bar.y, bar.width, bar.height, bar.color);
                if (barEl.__barId !== bar.id) {
                    barEl.dataset.barId = bar.id;
                    barEl.__barId = bar.id;
                }
                const nextClass = bar.className || '';
                if (barEl.__className !== nextClass) {
                    barEl.className = nextClass;
                    barEl.__className = nextClass;
                }

                if (bar.sections) {
                    for (const s of bar.sections) {
                        clearBarIdentity(this._rect(p.barShapes, s.x, s.y, s.width, s.height, s.color,
                            s.borderColor ? `1px solid ${s.borderColor}` : null));
                    }
                }

                // A 2px stroke centred on the rect covers one extra pixel on each
                // side; a border-box div grown by 1px per side matches that.
                if (bar.outline) {
                    clearBarIdentity(this._rect(p.barShapes, bar.outline.x - 1, bar.outline.y - 1,
                        bar.outline.width + 2, bar.outline.height + 2,
                        null, `2px solid ${c.colors.barSelectedBorder}`));
                }

                if (bar.focusRing) {
                    clearBarIdentity(this._rect(p.barShapes, bar.focusRing.x - 1, bar.focusRing.y - 1,
                        bar.focusRing.width + 2, bar.focusRing.height + 2,
                        null, `2px dashed ${c.colors.focus}`));
                }

                if (bar.icons) {
                    for (const icon of bar.icons) {
                        // Go through the engine's image cache rather than assigning
                        // a fresh src per frame: one canonical load per source, and
                        // the src write is skipped entirely when unchanged. Matches
                        // the canvas renderer, which draws from the same cache.
                        const cached = this.host.getImage(icon.source);
                        if (!cached || !cached.complete || cached.naturalWidth === 0) continue;
                        const img = p.barIcons.acquire('img') as HTMLImageElement & ResourceTimelinePooledNodeState;
                        if (img.__src !== icon.source) {
                            img.src = icon.source;
                            img.alt = '';
                            img.__src = icon.source;
                        }
                        ResourceTimelineNodePool.applyStyle(img, {
                            position: 'absolute',
                            left: px(icon.x),
                            top: px(icon.y),
                            width: px(icon.width),
                            height: px(icon.height)
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
                clearBarIdentity(this._rect(p.barShapes, n.x, n.y, n.width, n.height,
                    c.colors.axisBg, `1px solid ${c.colors.axisBorder}`));
                this._text(p.barLabels, n.text, n.x + 3, n.y + n.height / 2,
                    c.barLabelFont, c.colors.dateLabel, 'left', 'middle');
            }
        }

        private _buildNowLine(scene: ResourceTimelineScene): void {
            if (scene.nowX == null) return;
            const pool = this._pools.now;
            const colors = scene.config.colors;
            const v = scene.viewport;
            const x = scene.nowX;

            this._rect(pool, x - 1, v.axisHeight, 2, v.height - v.axisHeight, colors.now);
            // Small triangular marker at the top of the line (CSS border triangle).
            const marker = pool.acquire('div');
            ResourceTimelineNodePool.applyStyle(marker, {
                position: 'absolute',
                left: px(x - 4),
                top: px(v.axisHeight),
                width: '0',
                height: '0',
                borderLeft: '4px solid transparent',
                borderRight: '4px solid transparent',
                borderTop: `5px solid ${colors.now}`
            });
        }

        private _buildTimeAxis(scene: ResourceTimelineScene): void {
            const p = this._pools;
            const colors = scene.config.colors;
            const v = scene.viewport;
            const startX = v.axisWidth;

            ResourceTimelineNodePool.applyStyle(this._timeAxis, {
                left: px(startX),
                top: '0',
                width: px(Math.max(0, v.width - startX)),
                height: px(v.axisHeight),
                background: colors.axisBg,
                borderBottom: `1px solid ${colors.axisBorder}`
            });
            // Children are positioned in viewport coordinates; shift them back.
            ResourceTimelineNodePool.applyStyle(this._timeAxisInner, { left: px(-startX) });
            const timeTints = scene.surfaceTimeAxis;
            if (timeTints) {
                for (const b of timeTints) {
                    this._rect(p.axisTints, b.x, b.y, b.width, b.height, b.color);
                }
            }

            // Divider between the date row and the hour row(s), the one between the
            // UTC row and the zone row when both are shown, then the day separators.
            this._rect(p.axisLines, startX, v.dateRowHeight, v.width - startX, 1, colors.axisBorder);
            if (v.utcRowY != null) {
                this._rect(p.axisLines, startX, v.utcRowY, v.width - startX, 1, colors.axisBorder);
            }
            for (const day of scene.days) {
                if (day.sepX != null) {
                    this._rect(p.axisLines, day.sepX, 0, 1, v.dateRowHeight,
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
                this._rect(p.axisTicks, tick.x, v.axisHeight - 8, 1, 8, colors.tick);
            }
            for (const tick of scene.utcTicks) {
                this._rect(p.axisTicks, tick.x, v.utcRowY! - 8, 1, 8, colors.tick);
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
            const p = this._pools;
            const c = scene.config;
            const colors = c.colors;
            const v = scene.viewport;
            const startY = v.axisHeight;

            ResourceTimelineNodePool.applyStyle(this._resourceAxis, {
                left: '0',
                top: px(startY),
                width: px(v.axisWidth),
                height: px(Math.max(0, v.height - startY)),
                background: colors.axisBg,
                borderRight: `1px solid ${colors.axisBorder}`
            });
            ResourceTimelineNodePool.applyStyle(this._resourceAxisInner, { top: px(-startY) });
            const rowTints = scene.surfaceResourceAxis;
            if (rowTints) {
                for (const b of rowTints) {
                    this._rect(p.resourceTints, b.x, b.y, b.width, b.height, b.color);
                }
            }
            const rowLines = scene.resourceAxisLines;
            if (rowLines) {
                for (const line of rowLines) {
                    this._rect(p.resourceLines, 0, line.y, v.axisWidth, 1, line.color);
                }
            }

            // When the HTML resource-column template overlay is active it renders
            // the labels/chevrons; only the axis background/border is painted.
            if (!scene.resourceRows) return;

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

        // Marquee and edit ghost live in a clip container over the content area so
        // they never overlap the sticky axes.
        private _buildOverlays(scene: ResourceTimelineScene): void {
            const rects = this._pools.overlayRects;
            const c = scene.config;
            const colors = c.colors;
            const v = scene.viewport;

            if (!scene.marquee && !scene.ghost) {
                ResourceTimelineNodePool.applyStyle(this._overlayClip, { display: 'none' });
                return;
            }

            ResourceTimelineNodePool.applyStyle(this._overlayClip, {
                display: '',
                left: px(v.axisWidth),
                top: px(v.axisHeight),
                width: px(Math.max(0, v.width - v.axisWidth)),
                height: px(Math.max(0, v.height - v.axisHeight))
            });
            ResourceTimelineNodePool.applyStyle(this._overlayInner, { left: px(-v.axisWidth), top: px(-v.axisHeight) });

            const m = scene.marquee;
            if (m) {
                this._rect(rects, m.x, m.y, m.width, m.height,
                    colors.selectionFill, `1px solid ${colors.selectionBorder}`);
            }

            const g = scene.ghost;
            if (g) {
                this._rect(rects, g.x, g.y, g.width, g.height, g.color, null, 0.7);
                this._rect(rects, g.x, g.y - 1, g.width, g.height + 2,
                    null, `1px dashed ${colors.barSelectedBorder}`);
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
            for (const key in this._pools) this._pools[key as keyof HtmlRendererPools].clear();
            this.surface.remove();
        }
    }
}
