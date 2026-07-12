namespace BitBlazorUI {
    export class PdfViewer {
        private static _rezoomTimers = new WeakMap<HTMLElement, number>();
        private static readonly _CAPS: CanvasLineCap[] = ["butt", "round", "square"];
        private static readonly _JOINS: CanvasLineJoin[] = ["miter", "round", "bevel"];

        public static getViewport(container: HTMLElement) {
            if (!container) {
                return { width: 0, height: 0 };
            }
            return { width: container.clientWidth, height: container.clientHeight };
        }

        public static scrollToPage(container: HTMLElement, pageNumber: number) {
            if (!container) {
                return;
            }
            const target = container.querySelector(`[data-page='${pageNumber}']`);
            if (target) {
                PdfViewer.scrollWithin(container, target, "start", "smooth");
                // Render the destination immediately so jumps don't land on a placeholder.
                PdfViewer.scheduleRender(container, (container as any).__bitPdvDotnet);
            }
        }

        // Scrolls `target` into view by scrolling ONLY `container` — unlike
        // scrollIntoView, which also scrolls every scrollable ancestor (including
        // the hosting page, yanking the whole document around when the viewer is
        // embedded mid-page).
        private static scrollWithin(container: HTMLElement, target: Element, block: "start" | "center" | "nearest", behavior: ScrollBehavior = "auto") {
            const cRect = container.getBoundingClientRect();
            const tRect = target.getBoundingClientRect();

            let top = container.scrollTop;
            if (block === "start") {
                top += tRect.top - cRect.top;
            } else if (block === "center") {
                top += tRect.top - cRect.top - (container.clientHeight - tRect.height) / 2;
            } else if (tRect.top < cRect.top) {
                top += tRect.top - cRect.top;
            } else if (tRect.bottom > cRect.bottom) {
                top += Math.min(tRect.top - cRect.top, tRect.bottom - cRect.bottom);
            }

            let left = container.scrollLeft;
            if (tRect.left < cRect.left) {
                left += tRect.left - cRect.left;
            } else if (tRect.right > cRect.right) {
                left += Math.min(tRect.left - cRect.left, tRect.right - cRect.right);
            }

            if (top !== container.scrollTop || left !== container.scrollLeft) {
                container.scrollTo({ top, left, behavior });
            }
        }

        // Throttles render passes to one per animation frame.
        private static scheduleRender(container: HTMLElement, dotnetRef: any) {
            if (!container || !dotnetRef || (container as any).__bitPdvRenderScheduled) {
                return;
            }
            (container as any).__bitPdvRenderScheduled = true;
            requestAnimationFrame(() => {
                (container as any).__bitPdvRenderScheduled = false;
                PdfViewer.renderVisiblePages(container, dotnetRef);
            });
        }

        // Computes which pages intersect the viewport (expanded by a buffer) and asks
        // .NET to render any that are not rendered yet.
        private static renderVisiblePages(container: HTMLElement, dotnetRef: any) {
            const pages = container.querySelectorAll("[data-page]");
            if (!pages.length) {
                return;
            }
            const rect = container.getBoundingClientRect();
            const buffer = Math.max(container.clientHeight * 1.5, 800);
            const lo = rect.top - buffer;
            const hi = rect.bottom + buffer;

            const needed: number[] = [];
            pages.forEach(page => {
                const r = page.getBoundingClientRect();
                if (r.bottom >= lo && r.top <= hi) {
                    const n = parseInt(page.getAttribute("data-page") || "", 10);
                    if (!Number.isNaN(n)) {
                        needed.push(n);
                    }
                }
            });
            if (needed.length) {
                dotnetRef.invokeMethodAsync("EnsurePagesRendered", needed);
            }
        }

        public static registerScrollSpy(container: HTMLElement, dotnetRef: any) {
            if (!container) {
                return;
            }
            PdfViewer.disposeScrollSpy(container);
            (container as any).__bitPdvDotnet = dotnetRef;

            const ratios = new Map<Element, number>();
            const observer = new IntersectionObserver(
                (entries) => {
                    for (const entry of entries) {
                        ratios.set(entry.target, entry.isIntersecting ? entry.intersectionRatio : 0);
                    }
                    let best: Element | null = null;
                    let bestRatio = 0;
                    ratios.forEach((ratio, el) => {
                        if (ratio > bestRatio) {
                            bestRatio = ratio;
                            best = el;
                        }
                    });
                    if (best) {
                        const n = parseInt((best as Element).getAttribute("data-page") || "", 10);
                        if (!Number.isNaN(n)) {
                            dotnetRef.invokeMethodAsync("OnPageVisible", n);
                        }
                    }
                },
                { root: container, threshold: [0, 0.25, 0.5, 0.75, 1] }
            );

            container.querySelectorAll("[data-page]").forEach((p) => observer.observe(p));
            (container as any).__bitPdvObserver = observer;

            // Lazy rendering: on every scroll (throttled to animation frames) work out
            // which pages fall within the viewport plus a generous buffer and ask .NET
            // to render any that are still placeholders. This fills the surface ahead of
            // the user like the browser's built-in viewer, instead of rendering the whole
            // document up front. A geometry check is used (rather than a second
            // IntersectionObserver with rootMargin) because it fires reliably on every
            // scroll for an element scroll-container.
            const onScroll = () => PdfViewer.scheduleRender(container, dotnetRef);
            container.addEventListener("scroll", onScroll, { passive: true });
            (container as any).__bitPdvScroll = onScroll;
            PdfViewer.scheduleRender(container, dotnetRef); // initial fill

            // Delegate clicks on internal-link hotspots ([data-bit-pdv-page]) to page nav.
            const onClick = (e: MouseEvent) => {
                const target = e.target as HTMLElement;
                const hot = target.closest && target.closest("[data-bit-pdv-page]");
                if (hot) {
                    const n = parseInt(hot.getAttribute("data-bit-pdv-page") || "", 10);
                    if (!Number.isNaN(n)) {
                        e.preventDefault();
                        PdfViewer.scrollToPage(container, n);
                        dotnetRef.invokeMethodAsync("OnPageVisible", n);
                    }
                }
            };
            container.addEventListener("click", onClick);
            (container as any).__bitPdvClick = onClick;

            // Ctrl+wheel (and pinch, which browsers report as ctrl+wheel) zooms.
            const onWheel = (e: WheelEvent) => {
                if (e.ctrlKey) {
                    e.preventDefault();
                    dotnetRef.invokeMethodAsync("OnWheelZoom", e.deltaY);
                }
            };
            container.addEventListener("wheel", onWheel, { passive: false });
            (container as any).__bitPdvWheel = onWheel;

            // Notify .NET when the container resizes (used for fit-to-width/page).
            if (typeof ResizeObserver !== "undefined") {
                const resize = new ResizeObserver(() => {
                    dotnetRef.invokeMethodAsync("OnViewportResized");
                    PdfViewer.scheduleRender(container, dotnetRef);
                });
                resize.observe(container);
                (container as any).__bitPdvResize = resize;
            }
        }

        public static disposeScrollSpy(container: HTMLElement) {
            if (!container) {
                return;
            }
            PdfViewer.clearSearch(container);
            const c = container as any;
            if (c.__bitPdvObserver) {
                c.__bitPdvObserver.disconnect();
                c.__bitPdvObserver = null;
            }
            if (c.__bitPdvScroll) {
                container.removeEventListener("scroll", c.__bitPdvScroll);
                c.__bitPdvScroll = null;
            }
            if (c.__bitPdvClick) {
                container.removeEventListener("click", c.__bitPdvClick);
                c.__bitPdvClick = null;
            }
            if (c.__bitPdvWheel) {
                container.removeEventListener("wheel", c.__bitPdvWheel);
                c.__bitPdvWheel = null;
            }
            c.__bitPdvDotnet = null;
            if (c.__bitPdvResize) {
                c.__bitPdvResize.disconnect();
                c.__bitPdvResize = null;
            }
        }

        // ----- Thumbnail sidebar (its own lazy-render cycle) -----
        //
        // The sidebar renders thumbnails on demand as they scroll into its own viewport,
        // independent of the main surface. Its element only exists in the DOM while the
        // panel is open, so registration is driven from .NET after that element renders.

        private static scheduleThumbRender(container: HTMLElement, dotnetRef: any) {
            if (!container || !dotnetRef || (container as any).__bitPdvThumbScheduled) {
                return;
            }
            (container as any).__bitPdvThumbScheduled = true;
            requestAnimationFrame(() => {
                (container as any).__bitPdvThumbScheduled = false;
                PdfViewer.renderVisibleThumbs(container, dotnetRef);
            });
        }

        // Works out which thumbnails fall within the sidebar viewport (plus a buffer)
        // and asks .NET to render any that are still placeholders.
        private static renderVisibleThumbs(container: HTMLElement, dotnetRef: any) {
            const thumbs = container.querySelectorAll("[data-thumb]");
            if (!thumbs.length) {
                return;
            }
            const rect = container.getBoundingClientRect();
            const buffer = Math.max(container.clientHeight, 400);
            const lo = rect.top - buffer;
            const hi = rect.bottom + buffer;

            const needed: number[] = [];
            thumbs.forEach(thumb => {
                const r = thumb.getBoundingClientRect();
                if (r.bottom >= lo && r.top <= hi) {
                    const n = parseInt(thumb.getAttribute("data-thumb") || "", 10);
                    if (!Number.isNaN(n)) {
                        needed.push(n);
                    }
                }
            });
            if (needed.length) {
                dotnetRef.invokeMethodAsync("EnsureThumbsRendered", needed);
            }
        }

        public static registerThumbSpy(container: HTMLElement, dotnetRef: any) {
            if (!container) {
                return;
            }
            PdfViewer.disposeThumbSpy(container);
            (container as any).__bitPdvThumbDotnet = dotnetRef;
            const onScroll = () => PdfViewer.scheduleThumbRender(container, dotnetRef);
            container.addEventListener("scroll", onScroll, { passive: true });
            (container as any).__bitPdvThumbScroll = onScroll;
            PdfViewer.scheduleThumbRender(container, dotnetRef); // initial fill
        }

        public static disposeThumbSpy(container: HTMLElement) {
            if (!container) {
                return;
            }
            const c = container as any;
            if (c.__bitPdvThumbScroll) {
                container.removeEventListener("scroll", c.__bitPdvThumbScroll);
                c.__bitPdvThumbScroll = null;
            }
            c.__bitPdvThumbDotnet = null;
            c.__bitPdvThumbScheduled = false;
        }

        // Keeps the active thumbnail visible in the sidebar as the current page changes.
        // Scrolls the minimum amount (block:"nearest") so it never fights the user, then
        // nudges the lazy renderer to fill anything the scroll brought into view.
        public static scrollThumbIntoView(container: HTMLElement, pageNumber: number) {
            if (!container) {
                return;
            }
            const target = container.querySelector(`[data-thumb='${pageNumber}']`);
            if (target) {
                PdfViewer.scrollWithin(container, target, "nearest");
                if ((container as any).__bitPdvThumbDotnet) {
                    PdfViewer.scheduleThumbRender(container, (container as any).__bitPdvThumbDotnet);
                }
            }
        }

        // Streams the document bytes from .NET (via a DotNetStreamReference) into a Blob
        // and triggers a download, avoiding a multi-megabyte base64 string over SignalR.
        public static async download(fileName: string, streamRef: any) {
            const buffer = await streamRef.arrayBuffer();
            const blob = new Blob([buffer], { type: "application/pdf" });
            const url = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = url;
            link.download = fileName || "document.pdf";
            document.body.appendChild(link);
            link.click();
            link.remove();
            setTimeout(() => URL.revokeObjectURL(url), 10000);
        }

        // Corrects each text run's horizontal extent to the PDF-computed advance stored
        // in its data-w attribute. Because runs are laid out with a substitute font when
        // the real font isn't embedded, their natural width differs from the PDF's; this
        // scales each run (via the --bit-pdv-sx custom property consumed by its transform)
        // so it occupies exactly its intended advance, restoring correct word spacing.
        public static async correctTextWidths(container: HTMLElement) {
            if (!container) {
                return;
            }
            // Wait for any @font-face fonts to load so measurements are stable.
            try {
                if (document.fonts && document.fonts.ready) {
                    await document.fonts.ready;
                }
            } catch { /* ignore */ }

            const spans = Array.prototype.slice.call(container.querySelectorAll("span[data-w]")) as HTMLElement[];
            // Batch all reads before all writes to avoid layout thrashing.
            const naturalWidths = spans.map((s) => s.offsetWidth);
            for (let i = 0; i < spans.length; i++) {
                const target = parseFloat(spans[i].getAttribute("data-w") || "");
                const natural = naturalWidths[i];
                if (natural > 0 && target > 0) {
                    spans[i].style.setProperty("--bit-pdv-sx", (target / natural).toString());
                }
            }
        }

        public static toggleFullscreen(element: HTMLElement) {
            if (!element) {
                return;
            }
            if (document.fullscreenElement) {
                document.exitFullscreen();
            } else if (element.requestFullscreen) {
                element.requestFullscreen();
            }
        }

        // Prints the rendered pages at their true physical size by cloning each page
        // into a hidden iframe (one sheet per page) and invoking the browser dialog.
        public static async print(container: HTMLElement) {
            if (!container) {
                return;
            }
            const pages = container.querySelectorAll("[data-page] .bit-pdv-html-page");
            if (!pages.length) {
                return;
            }

            const frame = document.createElement("iframe");
            frame.setAttribute("aria-hidden", "true");
            frame.style.cssText = "position:fixed;right:0;bottom:0;width:0;height:0;border:0";
            document.body.appendChild(frame);

            const doc = (frame.contentDocument || frame.contentWindow?.document)!;
            doc.open();
            doc.write(
                "<!DOCTYPE html><html><head><meta charset='utf-8'><style>" +
                "@page{margin:0}html,body{margin:0;padding:0;background:#fff}" +
                ".bit-pdv-sheet{position:relative;overflow:hidden;page-break-after:always;break-after:page}" +
                ".bit-pdv-sheet:last-child{page-break-after:auto;break-after:auto}" +
                "</style></head><body></body></html>");
            doc.close();

            // Html-mode glyphs resolve through the document-wide embedded @font-face
            // rules kept in <style> elements inside the viewer surface; without them the
            // print document falls back to default fonts with wrong metrics.
            container.querySelectorAll("style").forEach((style) => {
                doc.head.appendChild(doc.importNode(style, true));
            });

            const ptToPx = 96 / 72; // PDF points to CSS pixels for physical-size output
            for (const inner of Array.prototype.slice.call(pages) as HTMLElement[]) {
                const el = inner;
                const w = parseFloat(el.style.width) || 0;
                const h = parseFloat(el.style.height) || 0;
                const sheet = doc.createElement("div");
                sheet.className = "bit-pdv-sheet";
                sheet.style.width = (w * ptToPx).toFixed(2) + "px";
                sheet.style.height = (h * ptToPx).toFixed(2) + "px";
                sheet.innerHTML = el.outerHTML;
                const clone = sheet.firstElementChild as HTMLElement | null;
                if (clone) {
                    clone.style.transform = "scale(" + ptToPx + ")";
                    clone.style.transformOrigin = "top left";
                    // A cloned <canvas> loses its pixels: substitute a snapshot image so
                    // canvas-mode pages print their painted content. Pages with a cached
                    // display list re-rasterize at print resolution — the screen-resolution
                    // bitmap is sized for on-screen zoom and prints blurry.
                    const srcCanvases = el.querySelectorAll("canvas[data-bit-pdv-canvas]");
                    const dstCanvases = clone.querySelectorAll("canvas[data-bit-pdv-canvas]");
                    for (let i = 0; i < srcCanvases.length; i++) {
                        const src = srcCanvases[i] as HTMLCanvasElement;
                        const dst = dstCanvases[i];
                        if (!dst) {
                            continue;
                        }
                        try {
                            const img = doc.createElement("img");
                            img.src = (await PdfViewer.rasterizeForPrint(src)) || src.toDataURL();
                            img.style.cssText = src.style.cssText;
                            dst.replaceWith(img);
                        } catch { /* tainted or unpainted canvas: leave the clone as-is */ }
                    }
                }
                doc.body.appendChild(sheet);
            }

            const cleanup = () => setTimeout(() => frame.remove(), 1000);
            if (frame.contentWindow) {
                frame.contentWindow.addEventListener("afterprint", cleanup, { once: true });
            }
            // The copied @font-face fonts load lazily after layout; wait for them
            // (bounded, in case a face is rejected) so the print snapshot uses the
            // embedded faces instead of fallbacks.
            const start = () => {
                try {
                    frame.contentWindow!.focus();
                    frame.contentWindow!.print();
                } catch {
                    frame.remove();
                }
            };
            setTimeout(() => {
                const fonts = doc.fonts;
                if (fonts && fonts.ready) {
                    Promise.race([
                        fonts.ready,
                        new Promise((resolve) => setTimeout(resolve, 3000))
                    ]).then(start, start);
                } else {
                    start();
                }
            }, 100);
        }

        // Re-rasterizes a canvas-mode page at print resolution (300 dpi) from the
        // display list cached by paintCanvasPages, so printouts stay crisp instead of
        // upscaling the screen-resolution bitmap. Returns null when no display list is
        // cached or rasterization fails (the caller falls back to a plain snapshot).
        private static async rasterizeForPrint(src: HTMLCanvasElement): Promise<string | null> {
            const cache = (src as any).__bitPdvOps;
            if (!cache) {
                return null;
            }
            try {
                const off = document.createElement("canvas");
                (off as any).__bitPdvOps = cache; // shares the decoded-image cache
                await PdfViewer.replayOps(off, 1, 300 / 72); // device px per PDF point at 300 dpi
                return off.toDataURL();
            } catch {
                return null;
            }
        }

        // ----- Canvas rendering (display-list replay) -----

        // Replays each page's display list (produced by the C# canvas renderer) onto its
        // <canvas data-bit-pdv-canvas> placeholder. `pages` is [{page, w, h, ops}] with
        // ops a JSON array of drawing ops; the op's first element is its code:
        //   ["g", path, evenOdd]                            save + clip
        //   ["G"]                                           restore
        //   ["f", path, evenOdd, color, alpha, blend]       fill
        //   ["s", path, color, width, cap, join, miter,     stroke
        //         dash, phase, alpha, blend]
        //   ["i", src, a,b,c,d,e,f, alpha, blend, pix]      image (matrix: pixel->device)
        //   ["t", text, size, family, bold, italic,         text (matrix: em->device,
        //         a,b,c,d,e,f, fill, stroke, strokeW,        origin on the baseline)
        //         targetW, alpha, blend, ls, ws]
        //   ["sh", kind, coords, stops, alpha, blend, bbox]  gradient fill of the clip
        // Path data is SVG syntax, consumed directly by Path2D.
        public static async paintCanvasPages(container: HTMLElement, pages: any[], scale: number) {
            if (!container || !pages) {
                return;
            }
            // Embedded @font-face fonts must be loaded before fillText can use them.
            try {
                if (document.fonts && document.fonts.ready) {
                    await document.fonts.ready;
                }
            } catch { /* ignore */ }

            for (const p of pages) {
                const canvas = container.querySelector('[data-page="' + p.page + '"] canvas[data-bit-pdv-canvas]') as HTMLCanvasElement | null;
                if (!canvas || !p.ops) {
                    continue;
                }
                let ops: any[];
                try {
                    ops = JSON.parse(p.ops);
                } catch {
                    continue;
                }
                // Cache the display list (and decoded images) on the element so zoom
                // changes can re-rasterize without another interop round-trip.
                (canvas as any).__bitPdvOps = { ops, w: p.w, h: p.h, images: new Map() };
                await PdfViewer.replayOps(canvas, scale || 1);
            }
        }

        // Re-rasterizes every painted canvas at the new zoom so text and lines stay
        // crisp instead of being CSS-upscaled (the CSS-scaled bitmap shows instantly,
        // the sharp re-render replaces it when zooming settles).
        // Debounced per container: zoom buttons and pinches arrive in bursts.
        public static rezoomCanvases(container: HTMLElement, scale: number) {
            if (!container) {
                return;
            }
            clearTimeout(PdfViewer._rezoomTimers.get(container));
            PdfViewer._rezoomTimers.set(container, setTimeout(() => {
                container.querySelectorAll("canvas[data-bit-pdv-canvas]").forEach((canvas) => {
                    if ((canvas as any).__bitPdvOps) {
                        PdfViewer.replayOps(canvas as HTMLCanvasElement, scale || 1);
                    }
                });
            }, 180) as unknown as number);
        }

        private static async replayOps(canvas: HTMLCanvasElement, scale: number, pixelRatio?: number) {
            const { ops, w, h, images } = (canvas as any).__bitPdvOps as { ops: any[][], w: number, h: number, images: Map<string, HTMLImageElement> };
            // Rasterize at devicePixelRatio x zoom so the backing store matches the
            // on-screen pixel density (the element is CSS-scaled by --bit-pdv-scale).
            // Print passes an explicit pixelRatio to rasterize at printer resolution.
            // Cap the backing store to stay inside browser canvas limits on large pages.
            const dpr = pixelRatio || Math.min(window.devicePixelRatio || 1, 3);
            let px = dpr * Math.max(scale, 0.1);
            px = Math.min(px, 8192 / w, 8192 / h, Math.sqrt(16777216 / (w * h)));
            canvas.width = Math.max(1, Math.round(w * px));
            canvas.height = Math.max(1, Math.round(h * px));
            const ctx = canvas.getContext("2d");
            if (!ctx) {
                return;
            }

            // @font-face faces load lazily — only when DOM text uses them — and canvas
            // fillText never waits for (or reliably triggers) a load: it draws with the
            // fallback immediately. In canvas mode no DOM references the embedded
            // families, so without an explicit load the FIRST paint renders tofu until
            // something replays. Force-load every face the ops use before drawing.
            try {
                if (document.fonts && document.fonts.load) {
                    const fonts = new Set<string>();
                    for (const o of ops) {
                        if (o[0] === "t") {
                            fonts.add((o[5] ? "italic " : "") + (o[4] ? "bold " : "") + "12px " + o[3]);
                        }
                    }
                    await Promise.all([...fonts].map((f) => document.fonts.load(f).catch(() => { })));
                }
            } catch { /* ignore */ }

            // Preload any images not already decoded (reused across zoom re-renders) so
            // the replay itself is synchronous and in order.
            await Promise.all(ops.filter((o) => o[0] === "i" && !images.has(o[1])).map((o) =>
                new Promise<void>((resolve) => {
                    const img = new Image();
                    img.onload = () => { images.set(o[1], img); resolve(); };
                    img.onerror = () => resolve();
                    img.src = o[1];
                })));

            ctx.setTransform(px, 0, 0, px, 0, 0);
            let depth = 0;

            const setPaintState = (alpha: any, blend: any) => {
                ctx.globalAlpha = typeof alpha === "number" ? alpha : 1;
                ctx.globalCompositeOperation = blend ? blend : "source-over";
            };

            for (const op of ops) {
                try {
                    switch (op[0]) {
                        case "g": {
                            ctx.save();
                            depth++;
                            ctx.clip(new Path2D(op[1]), op[2] ? "evenodd" : "nonzero");
                            break;
                        }
                        case "G": {
                            // Guarded: content hidden by optional-content groups can drop
                            // one side of a save/restore pair.
                            if (depth > 0) {
                                ctx.restore();
                                depth--;
                            }
                            break;
                        }
                        case "f": {
                            setPaintState(op[4], op[5]);
                            ctx.fillStyle = op[3];
                            ctx.fill(new Path2D(op[1]), op[2] ? "evenodd" : "nonzero");
                            break;
                        }
                        case "s": {
                            setPaintState(op[9], op[10]);
                            ctx.strokeStyle = op[2];
                            ctx.lineWidth = op[3];
                            ctx.lineCap = PdfViewer._CAPS[op[4]] || "butt";
                            ctx.lineJoin = PdfViewer._JOINS[op[5]] || "miter";
                            ctx.miterLimit = op[6] || 10;
                            ctx.setLineDash(op[7] || []);
                            ctx.lineDashOffset = op[8] || 0;
                            ctx.stroke(new Path2D(op[1]));
                            ctx.setLineDash([]);
                            break;
                        }
                        case "i": {
                            // ["i", src, a, b, c, d, e, f, alpha, blend, pixelated]
                            const img = images.get(op[1]);
                            if (!img) {
                                break;
                            }
                            setPaintState(op[8], op[9]);
                            ctx.save();
                            ctx.transform(op[2], op[3], op[4], op[5], op[6], op[7]);
                            ctx.imageSmoothingEnabled = !op[10];
                            ctx.drawImage(img, 0, 0);
                            ctx.restore();
                            break;
                        }
                        case "t": {
                            const [, text, size, family, bold, italic, a, b, c, d, e, f,
                                fill, stroke, strokeW, targetW, alpha, blend, ls, ws] = op;
                            setPaintState(alpha, blend);
                            ctx.save();
                            ctx.font = (italic ? "italic " : "") + (bold ? "bold " : "") + size + "px " + family;
                            ctx.textBaseline = "alphabetic";
                            if ("letterSpacing" in ctx) {
                                (ctx as any).letterSpacing = (ls || 0) + "px";
                                (ctx as any).wordSpacing = (ws || 0) + "px";
                            }
                            ctx.transform(a, b, c, d, e, f);
                            // Width-correct the run to its PDF-computed advance (the same
                            // scaleX(--bit-pdv-sx) mechanism as the HTML text layer, inline).
                            if (targetW > 0.01) {
                                const natural = ctx.measureText(text).width;
                                if (natural > 0) {
                                    ctx.scale(targetW / natural, 1);
                                }
                            }
                            if (fill) {
                                ctx.fillStyle = fill;
                                ctx.fillText(text, 0, 0);
                            }
                            if (stroke) {
                                ctx.strokeStyle = stroke;
                                ctx.lineWidth = strokeW || 1;
                                ctx.strokeText(text, 0, 0);
                            }
                            ctx.restore();
                            break;
                        }
                        case "sh": {
                            const [, kind, coords, stops, alpha, blend, bbox] = op;
                            setPaintState(alpha, blend);
                            ctx.save();
                            if (bbox) {
                                ctx.clip(new Path2D(bbox));
                            }
                            if (kind === 0) {
                                ctx.fillStyle = stops; // sampled solid fallback
                            } else {
                                const g = kind === 2
                                    ? ctx.createLinearGradient(coords[0], coords[1], coords[2], coords[3])
                                    : ctx.createRadialGradient(coords[0], coords[1], coords[2], coords[0], coords[1], coords[3]);
                                for (const s of stops) {
                                    g.addColorStop(s[0], s[1]);
                                }
                                ctx.fillStyle = g;
                            }
                            ctx.fillRect(0, 0, w, h);
                            ctx.restore();
                            break;
                        }
                    }
                } catch {
                    // One malformed op must not abort the page; skip it.
                }
            }

            while (depth > 0) {
                ctx.restore();
                depth--;
            }
            setPaintState(1, "");
        }

        // ----- Text search (CSS Custom Highlight API) -----

        private static ensureSearchStyles() {
            if (document.getElementById("bit-pdv-search-style")) {
                return;
            }
            const style = document.createElement("style");
            style.id = "bit-pdv-search-style";
            style.textContent =
                "::highlight(bit-pdv-search){background:var(--bit-clr-wrn,#ffe066);color:var(--bit-clr-wrn-text,#000)}" +
                "::highlight(bit-pdv-search-current){background:var(--bit-clr-swr,#ff8f00);color:var(--bit-clr-swr-text,#000)}";
            document.head.appendChild(style);
        }

        private static searchSupported() {
            return typeof (globalThis as any).Highlight !== "undefined" && typeof CSS !== "undefined" && !!(CSS as any).highlights;
        }

        private static locate(nodes: { node: Node; start: number }[], pos: number) {
            for (const entry of nodes) {
                if (pos >= entry.start && pos <= entry.start + (entry.node.nodeValue || "").length) {
                    return { node: entry.node, offset: pos - entry.start };
                }
            }
            return null;
        }

        private static buildRange(nodes: { node: Node; start: number }[], start: number, end: number) {
            const a = PdfViewer.locate(nodes, start);
            const b = PdfViewer.locate(nodes, end);
            if (!a || !b) {
                return null;
            }
            const range = document.createRange();
            try {
                range.setStart(a.node, a.offset);
                range.setEnd(b.node, b.offset);
            } catch {
                return null;
            }
            return range;
        }

        // Finds every case-insensitive occurrence of `query` across all pages and
        // registers a highlight. Returns the match count, or -1 if unsupported.
        public static searchAll(container: HTMLElement, query: string) {
            PdfViewer.clearSearch(container);
            if (!container || !query) {
                return 0;
            }
            if (!PdfViewer.searchSupported()) {
                return -1;
            }
            PdfViewer.ensureSearchStyles();

            const needle = query.toLowerCase();
            const ranges: Range[] = [];

            container.querySelectorAll("[data-page]").forEach((page) => {
                // Search only the coalesced selection layer ([data-bit-pdv-sel]) — it
                // holds the real Unicode in reading order. The painted layer beneath is
                // presentational (real glyphs or Private-Use codepoints) and would
                // otherwise double-count.
                const walker = document.createTreeWalker(page, NodeFilter.SHOW_TEXT, {
                    acceptNode(n) {
                        return n.parentElement && n.parentElement.hasAttribute("data-bit-pdv-sel")
                            ? NodeFilter.FILTER_ACCEPT
                            : NodeFilter.FILTER_REJECT;
                    },
                });
                const nodes: { node: Node; start: number }[] = [];
                let text = "";
                let node: Node | null;
                while ((node = walker.nextNode())) {
                    nodes.push({ node, start: text.length });
                    text += node.nodeValue;
                }
                const haystack = text.toLowerCase();
                let idx = haystack.indexOf(needle);
                while (idx !== -1) {
                    const range = PdfViewer.buildRange(nodes, idx, idx + needle.length);
                    if (range) {
                        ranges.push(range);
                    }
                    idx = haystack.indexOf(needle, idx + needle.length);
                }
            });

            (container as any).__bitPdvRanges = ranges;
            if (ranges.length) {
                (CSS as any).highlights.set("bit-pdv-search", new (globalThis as any).Highlight(...ranges));
            }
            return ranges.length;
        }

        // Marks the match at `index` as current and scrolls it into view.
        public static gotoMatch(container: HTMLElement, index: number) {
            const ranges = container && (container as any).__bitPdvRanges;
            if (!ranges || !ranges.length || !PdfViewer.searchSupported()) {
                return;
            }
            const i = ((index % ranges.length) + ranges.length) % ranges.length;
            const range = ranges[i] as Range;
            (CSS as any).highlights.set("bit-pdv-search-current", new (globalThis as any).Highlight(range));
            const el = range.startContainer.parentElement;
            if (el) {
                PdfViewer.scrollWithin(container, el, "center", "smooth");
            }
        }

        public static clearSearch(container: HTMLElement) {
            if (PdfViewer.searchSupported()) {
                (CSS as any).highlights.delete("bit-pdv-search");
                (CSS as any).highlights.delete("bit-pdv-search-current");
            }
            if (container) {
                (container as any).__bitPdvRanges = null;
            }
        }
    }
}
