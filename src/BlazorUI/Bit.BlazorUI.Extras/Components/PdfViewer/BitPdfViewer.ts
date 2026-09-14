namespace BitBlazorUI {
    export class PdfViewer {
        private static _rezoomTimers = new WeakMap<HTMLElement, number>();
        // The controls that opened the currently trapped dialogs, innermost last, so
        // closing one hands focus back to whatever the reader was on when it opened.
        private static readonly _focusReturn: HTMLElement[] = [];
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

        // Scrolls `target` into view by scrolling ONLY `container` - unlike
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
            (container as any).__bitPdvRenderFrame = requestAnimationFrame(() => {
                (container as any).__bitPdvRenderScheduled = false;
                PdfViewer.renderVisiblePages(container, dotnetRef);
            });
        }

        // Scroll-driven rendering: rendering a page runs .NET on the UI thread (on
        // WASM there is only one thread), so a render pass on every scroll frame
        // freezes the scroll itself. Instead, while the user is actively scrolling,
        // allow at most one pass every 300ms, plus one trailing pass shortly after
        // scrolling settles - placeholders stay smooth mid-fling and fill in the
        // moment the user slows down (the behavior of native pdf viewers).
        private static throttleRender(container: HTMLElement, dotnetRef: any) {
            const c = container as any;
            clearTimeout(c.__bitPdvRenderDebounce);
            const now = performance.now();
            if (now - (c.__bitPdvLastRenderPass || 0) >= 300) {
                c.__bitPdvLastRenderPass = now;
                PdfViewer.scheduleRender(container, dotnetRef);
            } else {
                c.__bitPdvRenderDebounce = setTimeout(() => {
                    c.__bitPdvLastRenderPass = performance.now();
                    PdfViewer.scheduleRender(container, dotnetRef);
                }, 100);
            }
        }

        // Computes which pages intersect the viewport (expanded by a buffer) and asks
        // .NET to render any that are not rendered yet. Only pages still showing a
        // placeholder are reported, so scrolling across already-rendered pages costs
        // no interop round-trip at all (on WASM every call runs on the UI thread).
        // The pages element carries the document's direction; the surface around it may
        // not, so ask the one that actually lays the pages out.
        private static isRtl(container: HTMLElement) {
            const pages = container.querySelector(".bit-pdv-pages") as HTMLElement | null;
            return getComputedStyle(pages || container).direction === "rtl";
        }

        private static renderVisiblePages(container: HTMLElement, dotnetRef: any) {
            const pages = container.querySelectorAll("[data-page]");
            if (!pages.length) {
                return;
            }
            const rect = container.getBoundingClientRect();
            // Horizontal scrolling is the one layout whose pages do NOT run down the
            // page: measure along the axis they actually flow on, so the early exit
            // below stays valid (in every other mode - including wrapped - document
            // order is monotonic in `top`).
            const horizontal = container.getAttribute("data-bit-pdv-axis") === "h";
            // A right-to-left horizontal layout flows the other way along x: later pages
            // sit further LEFT, so raw left/right would break out of the loop at the very
            // first page once it scrolled off. Mirroring x keeps `near` monotonic in
            // document order, which is what the early break relies on.
            const rtl = horizontal && PdfViewer.isRtl(container);
            const extent = horizontal ? container.clientWidth : container.clientHeight;
            const buffer = Math.max(extent * 1.5, 800);
            const near = (r: DOMRect) => horizontal ? (rtl ? -r.right : r.left) : r.top;
            const far = (r: DOMRect) => horizontal ? (rtl ? -r.left : r.right) : r.bottom;
            const viewNear = horizontal ? (rtl ? -rect.right : rect.left) : rect.top;
            const viewFar = horizontal ? (rtl ? -rect.left : rect.right) : rect.bottom;
            const lo = viewNear - buffer;
            const hi = viewFar + buffer;

            // Pages actually on screen are requested before buffer pages (which are
            // ordered by distance from the viewport) - pages render one at a time on
            // the .NET side, so this ordering is what the user perceives as speed.
            const visible: number[] = [];
            const buffered: { n: number, d: number }[] = [];
            const mid = (viewNear + viewFar) / 2;
            for (let i = 0; i < pages.length; i++) {
                const page = pages[i];
                const r = page.getBoundingClientRect();
                // Page mode (and presentation) hides every page but the current one, and a
                // display:none element measures as all zeros: it is neither visible nor a
                // meaningful distance away, so measuring it would queue the WHOLE document
                // and defeat both the ordered early exit below and eviction.
                if (r.width === 0 && r.height === 0) {
                    continue;
                }
                if (near(r) > hi) {
                    break; // pages are laid out in order; everything after is further along
                }
                if (far(r) < lo || !page.querySelector(".bit-pdv-page-placeholder")) {
                    continue;
                }
                const n = parseInt(page.getAttribute("data-page") || "", 10);
                if (Number.isNaN(n)) {
                    continue;
                }
                if (far(r) >= viewNear && near(r) <= viewFar) {
                    visible.push(n);
                } else {
                    buffered.push({ n, d: Math.abs((near(r) + far(r)) / 2 - mid) });
                }
            }
            buffered.sort((a, b) => a.d - b.d);
            const needed = visible.concat(buffered.map(b => b.n));
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
                        // Only cross the interop boundary when the focused page actually
                        // changed - the observer fires on every threshold crossing while
                        // scrolling, and each call would otherwise run .NET on the UI thread.
                        if (!Number.isNaN(n) && (container as any).__bitPdvLastPage !== n) {
                            (container as any).__bitPdvLastPage = n;
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
            const onScroll = () => PdfViewer.throttleRender(container, dotnetRef);
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
                        // A link whose destination named a vertical position navigates
                        // through .NET, which knows the page height and zoom the offset
                        // has to be measured against; a plain one scrolls straight away.
                        const raw = hot.getAttribute("data-bit-pdv-top");
                        const top = raw === null ? null : parseFloat(raw);
                        (container as any).__bitPdvLastPage = n;
                        if (top !== null && !Number.isNaN(top)) {
                            dotnetRef.invokeMethodAsync("OnLinkNavigate", n, top);
                        } else {
                            PdfViewer.scrollToPage(container, n);
                            dotnetRef.invokeMethodAsync("OnPageVisible", n);
                        }
                    }
                    return;
                }
                // Presenting: a click anywhere on the slide advances, the way every
                // presentation tool works. A click on a link (handled above) or an
                // actual text selection is not an advance.
                const root = container.closest(".bit-pdv-presenting");
                if (root && !target.closest("a") && !window.getSelection()?.toString()) {
                    dotnetRef.invokeMethodAsync("OnShortcut", e.shiftKey ? "prev" : "next");
                }
            };
            container.addEventListener("click", onClick);
            (container as any).__bitPdvClick = onClick;

            // Ctrl+wheel (and pinch, which browsers report as ctrl+wheel) zooms, keeping
            // the point under the cursor where it is instead of jumping to the top-left.
            const onWheel = (e: WheelEvent) => {
                if (e.ctrlKey) {
                    e.preventDefault();
                    PdfViewer.stashZoomAnchor(container, e.clientX, e.clientY);
                    dotnetRef.invokeMethodAsync("OnWheelZoom", e.deltaY);
                }
            };
            container.addEventListener("wheel", onWheel, { passive: false });
            (container as any).__bitPdvWheel = onWheel;

            // Hand tool: while .bit-pdv-pan is on the surface, dragging scrolls it
            // instead of selecting. Pointer events cover mouse, pen and single-finger
            // touch alike; touch already pans natively, so only a mouse/pen primary
            // button is captured (capturing touch here would fight the native scroll).
            const pan = { active: false, id: -1, x: 0, y: 0, left: 0, top: 0 };
            const onPointerDown = (e: PointerEvent) => {
                if (!container.classList.contains("bit-pdv-pan") || e.button !== 0 || e.pointerType === "touch") {
                    return;
                }
                pan.active = true;
                pan.id = e.pointerId;
                pan.x = e.clientX;
                pan.y = e.clientY;
                pan.left = container.scrollLeft;
                pan.top = container.scrollTop;
                container.setPointerCapture(e.pointerId);
                e.preventDefault();
            };
            const onPointerMove = (e: PointerEvent) => {
                if (!pan.active || e.pointerId !== pan.id) {
                    return;
                }
                container.scrollLeft = pan.left - (e.clientX - pan.x);
                container.scrollTop = pan.top - (e.clientY - pan.y);
            };
            const onPointerUp = (e: PointerEvent) => {
                if (!pan.active || e.pointerId !== pan.id) {
                    return;
                }
                pan.active = false;
                try {
                    container.releasePointerCapture(e.pointerId);
                } catch { /* the pointer may already be gone */ }
            };
            container.addEventListener("pointerdown", onPointerDown);
            container.addEventListener("pointermove", onPointerMove);
            container.addEventListener("pointerup", onPointerUp);
            container.addEventListener("pointercancel", onPointerUp);
            (container as any).__bitPdvPan = { onPointerDown, onPointerMove, onPointerUp };

            // Two-finger pinch to zoom. Browsers report a trackpad pinch as ctrl+wheel
            // (handled above), but a touchscreen pinch arrives as two touch points and
            // is otherwise swallowed by the page's own zoom.
            const pinch = { active: false, distance: 0, scale: 1 };
            const spread = (t: TouchList) => Math.hypot(
                t[0].clientX - t[1].clientX, t[0].clientY - t[1].clientY);
            const onTouchStart = (e: TouchEvent) => {
                if (e.touches.length === 2) {
                    pinch.active = true;
                    pinch.distance = spread(e.touches);
                    pinch.scale = 1;
                }
            };
            const onTouchMove = (e: TouchEvent) => {
                if (!pinch.active || e.touches.length !== 2 || pinch.distance <= 0) {
                    return;
                }
                e.preventDefault();
                const ratio = spread(e.touches) / pinch.distance;
                PdfViewer.stashZoomAnchor(container,
                    (e.touches[0].clientX + e.touches[1].clientX) / 2,
                    (e.touches[0].clientY + e.touches[1].clientY) / 2);
                // Only cross the interop boundary once the pinch has actually moved a
                // step's worth: .NET clamps and re-renders, so a call per touchmove
                // frame would stall the UI thread on WebAssembly.
                if (ratio / pinch.scale > 1.1) {
                    pinch.scale *= 1.1;
                    dotnetRef.invokeMethodAsync("OnWheelZoom", -1);
                } else if (pinch.scale / ratio > 1.1) {
                    pinch.scale /= 1.1;
                    dotnetRef.invokeMethodAsync("OnWheelZoom", 1);
                }
            };
            const onTouchEnd = (e: TouchEvent) => {
                if (e.touches.length < 2) {
                    pinch.active = false;
                }
            };
            container.addEventListener("touchstart", onTouchStart, { passive: true });
            container.addEventListener("touchmove", onTouchMove, { passive: false });
            container.addEventListener("touchend", onTouchEnd, { passive: true });
            container.addEventListener("touchcancel", onTouchEnd, { passive: true });
            (container as any).__bitPdvTouch = { onTouchStart, onTouchMove, onTouchEnd };

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
            clearTimeout(c.__bitPdvRenderDebounce);
            c.__bitPdvRenderDebounce = null;
            c.__bitPdvLastRenderPass = 0;
            // A render pass queued for the next animation frame would invoke the
            // (about to be disposed) .NET reference; cancel it and clear the flag so
            // a re-registration can schedule its initial render normally.
            cancelAnimationFrame(c.__bitPdvRenderFrame);
            c.__bitPdvRenderFrame = null;
            c.__bitPdvRenderScheduled = false;
            if (c.__bitPdvClick) {
                container.removeEventListener("click", c.__bitPdvClick);
                c.__bitPdvClick = null;
            }
            if (c.__bitPdvWheel) {
                container.removeEventListener("wheel", c.__bitPdvWheel);
                c.__bitPdvWheel = null;
            }
            if (c.__bitPdvPan) {
                container.removeEventListener("pointerdown", c.__bitPdvPan.onPointerDown);
                container.removeEventListener("pointermove", c.__bitPdvPan.onPointerMove);
                container.removeEventListener("pointerup", c.__bitPdvPan.onPointerUp);
                container.removeEventListener("pointercancel", c.__bitPdvPan.onPointerUp);
                c.__bitPdvPan = null;
            }
            if (c.__bitPdvTouch) {
                container.removeEventListener("touchstart", c.__bitPdvTouch.onTouchStart);
                container.removeEventListener("touchmove", c.__bitPdvTouch.onTouchMove);
                container.removeEventListener("touchend", c.__bitPdvTouch.onTouchEnd);
                container.removeEventListener("touchcancel", c.__bitPdvTouch.onTouchEnd);
                c.__bitPdvTouch = null;
            }
            c.__bitPdvDotnet = null;
            c.__bitPdvLastPage = null;
            if (c.__bitPdvResize) {
                c.__bitPdvResize.disconnect();
                c.__bitPdvResize = null;
            }
        }

        // Remembers where the point being zoomed about sits in the content, as a
        // fraction of the scrollable extent plus its offset inside the viewport. Both
        // change when the pages are re-sized, which is why the fraction (not the pixel
        // offset) is what survives the zoom.
        private static stashZoomAnchor(container: HTMLElement, clientX: number, clientY: number) {
            const rect = container.getBoundingClientRect();
            const x = clientX - rect.left;
            const y = clientY - rect.top;
            (container as any).__bitPdvZoomAnchor = {
                x, y,
                fx: container.scrollWidth > 0 ? (container.scrollLeft + x) / container.scrollWidth : 0,
                fy: container.scrollHeight > 0 ? (container.scrollTop + y) / container.scrollHeight : 0,
            };
        }

        // Puts the stashed point back under the cursor now that the pages have been
        // re-sized. A no-op when nothing was stashed, so an ordinary zoom (a toolbar
        // button, a fit mode) still lands wherever it always did.
        public static restoreZoomAnchor(container: HTMLElement) {
            if (!container) {
                return;
            }
            const anchor = (container as any).__bitPdvZoomAnchor;
            (container as any).__bitPdvZoomAnchor = null;
            if (!anchor) {
                return;
            }
            container.scrollLeft = anchor.fx * container.scrollWidth - anchor.x;
            container.scrollTop = anchor.fy * container.scrollHeight - anchor.y;
        }

        // Scrolls a page to the top of the surface, then a further `offset` CSS pixels
        // down it - which is how a bookmark or link that points into the MIDDLE of a
        // long page lands where it means to instead of at the page's top edge.
        public static scrollToPageOffset(container: HTMLElement, pageNumber: number, offset: number) {
            if (!container) {
                return;
            }
            const target = container.querySelector(`[data-page='${pageNumber}']`);
            if (!target) {
                return;
            }
            const cRect = container.getBoundingClientRect();
            const tRect = target.getBoundingClientRect();
            container.scrollTo({
                top: container.scrollTop + (tRect.top - cRect.top) + (offset || 0),
                left: container.scrollLeft + Math.min(0, tRect.left - cRect.left),
                behavior: "smooth",
            });
            PdfViewer.scheduleRender(container, (container as any).__bitPdvDotnet);
        }

        // ----- Keyboard shortcuts -----
        //
        // Matched here rather than in .NET because only the event carries the target
        // (so typing in the find or page box is never mistaken for a shortcut) and
        // only preventDefault can stop the BROWSER's own Ctrl+P / Ctrl+F / Ctrl+S.
        // Every match is forwarded to .NET as a single command string.

        // Whether the event originates from something the user is typing into, where
        // a bare letter is text and not a command.
        private static isTypingTarget(target: EventTarget | null) {
            const el = target as HTMLElement | null;
            if (!el || !el.tagName) {
                return false;
            }
            const tag = el.tagName.toLowerCase();
            return tag === "input" || tag === "textarea" || tag === "select" || el.isContentEditable === true;
        }

        // Whether the viewer is showing one page (or spread) at a time - the layout in
        // which the surface does not scroll, so the arrow and space keys have nothing to
        // scroll and page instead. Presentation mode is that layout by definition.
        private static isPaged(root: HTMLElement) {
            return !!root.querySelector(".bit-pdv-pages.bit-pdv-single");
        }

        private static resolveShortcut(e: KeyboardEvent, root: HTMLElement): { command: string, prevent: boolean } | null {
            const mod = e.ctrlKey || e.metaKey;
            const typing = PdfViewer.isTypingTarget(e.target);

            if (mod && e.altKey && (e.key === "p" || e.key === "P")) {
                return { command: "presentation", prevent: true };
            }
            if (mod && !e.altKey) {
                switch (e.key) {
                    case "+": case "=": return { command: "zoomIn", prevent: true };
                    case "-": return { command: "zoomOut", prevent: true };
                    case "0": return { command: "actualSize", prevent: true };
                    case "f": case "F": return { command: "find", prevent: true };
                    case "g": case "G": return { command: e.shiftKey ? "findPrev" : "findNext", prevent: true };
                    case "p": case "P": return { command: "print", prevent: true };
                    case "s": case "S": return { command: "download", prevent: true };
                }
                return null;
            }

            if (e.key === "F4") {
                return { command: "sidebar", prevent: true };
            }
            if (e.key === "Escape") {
                // Escape must reach .NET even from the find box, whose own handler
                // closes it; the duplicate is harmless (the second call finds nothing
                // open) and it is what makes Escape work from anywhere else.
                return { command: "escape", prevent: false };
            }
            if (typing || e.altKey) {
                return null;
            }

            switch (e.key) {
                case "n": case "j": case "PageDown": return { command: "next", prevent: e.key !== "PageDown" };
                case "p": case "k": case "PageUp": return { command: "prev", prevent: e.key !== "PageUp" };
                case "Home": return { command: "first", prevent: true };
                case "End": return { command: "last", prevent: true };
                case "r": return { command: "rotateCw", prevent: true };
                case "R": return { command: "rotateCcw", prevent: true };
            }

            // One page at a time: the surface has nothing to scroll, so the keys that
            // would scroll it turn the page instead (and only then - in a scrolling
            // layout they must keep scrolling).
            if (PdfViewer.isPaged(root)) {
                switch (e.key) {
                    case "ArrowRight": case "ArrowDown": return { command: "next", prevent: true };
                    case "ArrowLeft": case "ArrowUp": return { command: "prev", prevent: true };
                    case " ": case "Spacebar":
                        return { command: e.shiftKey ? "prev" : "next", prevent: true };
                }
            }
            return null;
        }

        public static registerKeyboard(root: HTMLElement, dotnetRef: any) {
            if (!root || !dotnetRef) {
                return;
            }
            PdfViewer.disposeKeyboard(root);
            const onKeyDown = (e: KeyboardEvent) => {
                const hit = PdfViewer.resolveShortcut(e, root);
                if (!hit) {
                    return;
                }
                if (hit.prevent) {
                    e.preventDefault();
                }
                dotnetRef.invokeMethodAsync("OnShortcut", hit.command);
            };
            root.addEventListener("keydown", onKeyDown);
            (root as any).__bitPdvKeys = onKeyDown;
        }

        public static disposeKeyboard(root: HTMLElement) {
            if (!root) {
                return;
            }
            const r = root as any;
            if (r.__bitPdvKeys) {
                root.removeEventListener("keydown", r.__bitPdvKeys);
                r.__bitPdvKeys = null;
            }
        }

        // ----- Drag and drop -----
        //
        // A pdf dropped on the viewer is handed to the file input the open-file control
        // already renders, and a synthetic change event lets Blazor read it exactly as
        // it reads a picked file - no second path across interop for the bytes.

        public static registerDropZone(root: HTMLElement) {
            if (!root) {
                return;
            }
            PdfViewer.disposeDropZone(root);

            const isFileDrag = (e: DragEvent) =>
                !!e.dataTransfer && Array.prototype.indexOf.call(e.dataTransfer.types || [], "Files") !== -1;

            const onDragOver = (e: DragEvent) => {
                if (!isFileDrag(e)) {
                    return;
                }
                e.preventDefault();
                if (e.dataTransfer) {
                    e.dataTransfer.dropEffect = "copy";
                }
                root.classList.add("bit-pdv-dropping");
            };
            const onDragLeave = (e: DragEvent) => {
                // Only when the pointer actually left the viewer, not when it crossed
                // into one of its children (which fires dragleave on the parent too).
                if (!root.contains(e.relatedTarget as Node)) {
                    root.classList.remove("bit-pdv-dropping");
                }
            };
            const onDrop = (e: DragEvent) => {
                if (!isFileDrag(e)) {
                    return;
                }
                e.preventDefault();
                root.classList.remove("bit-pdv-dropping");
                const files = e.dataTransfer?.files;
                const input = root.querySelector("input.bit-pdv-file") as HTMLInputElement | null;
                if (!files || !files.length || !input) {
                    return;
                }
                const pdf = Array.prototype.find.call(files, (f: File) =>
                    f.type === "application/pdf" || /\.pdf$/i.test(f.name)) as File | undefined;
                if (!pdf) {
                    return;
                }
                try {
                    const carrier = new DataTransfer();
                    carrier.items.add(pdf);
                    input.files = carrier.files;
                    input.dispatchEvent(new Event("change", { bubbles: true }));
                } catch { /* a browser that forbids assigning .files cannot accept drops */ }
            };

            root.addEventListener("dragover", onDragOver);
            root.addEventListener("dragleave", onDragLeave);
            root.addEventListener("drop", onDrop);
            (root as any).__bitPdvDrop = { onDragOver, onDragLeave, onDrop };
        }

        public static disposeDropZone(root: HTMLElement) {
            if (!root) {
                return;
            }
            const r = root as any;
            if (r.__bitPdvDrop) {
                root.removeEventListener("dragover", r.__bitPdvDrop.onDragOver);
                root.removeEventListener("dragleave", r.__bitPdvDrop.onDragLeave);
                root.removeEventListener("drop", r.__bitPdvDrop.onDrop);
                r.__bitPdvDrop = null;
            }
            root.classList.remove("bit-pdv-dropping");
        }

        // Moves focus to an element, used after .NET opens a control that was not in
        // the DOM yet (the find box) so the user can type into it straight away.
        public static focus(element: HTMLElement) {
            if (element && element.focus) {
                element.focus();
            }
        }

        // The text the reader has selected inside the document surface, or "" when the
        // selection is empty or lies outside it (a selection elsewhere on the hosting
        // page is not the document's).
        public static getSelectedText(container: HTMLElement) {
            const selection = window.getSelection();
            if (!container || !selection || selection.rangeCount === 0 || selection.isCollapsed) {
                return "";
            }
            const range = selection.getRangeAt(0);
            return container.contains(range.commonAncestorContainer) ? selection.toString() : "";
        }

        // Drops the reader's selection, but only when it is inside the surface: clearing
        // one made elsewhere on the page would be none of the viewer's business.
        public static clearSelection(container: HTMLElement) {
            const selection = window.getSelection();
            if (!container || !selection || selection.rangeCount === 0) {
                return;
            }
            if (container.contains(selection.getRangeAt(0).commonAncestorContainer)) {
                selection.removeAllRanges();
            }
        }

        // Writes a value straight into an input. Blazor only re-emits a value attribute
        // that CHANGED, so an entry the component ignored (it moved nothing) would
        // otherwise stay in the box; going through the DOM puts it right without
        // re-creating the element, which would take the reader's focus with it.
        public static setValue(element: HTMLInputElement, value: string) {
            if (element && element.value !== value) {
                element.value = value;
            }
        }

        // Focuses a modal dialog and keeps Tab inside it. A modal the keyboard can tab
        // out of is a modal in name only: focus lands behind it, on controls the dialog
        // is covering. The listener lives on the dialog element, which .NET removes from
        // the DOM when the dialog closes, so it needs no separate teardown.
        public static trapFocus(dialog: HTMLElement) {
            if (!dialog) {
                return;
            }
            // Remember where focus was, so releaseFocus can put it back: a modal that
            // closes leaving focus on <body> drops a keyboard reader out of the viewer.
            const opener = document.activeElement as HTMLElement | null;
            if (opener && opener !== dialog && !dialog.contains(opener)) {
                PdfViewer._focusReturn.push(opener);
            } else {
                PdfViewer._focusReturn.push(null as any);
            }

            const focusables = () => Array.prototype.filter.call(
                dialog.querySelectorAll(
                    "a[href],button:not([disabled]),input:not([disabled]),select:not([disabled])," +
                    "textarea:not([disabled]),[tabindex]:not([tabindex='-1'])"),
                (el: HTMLElement) => el.offsetParent !== null || el === document.activeElement) as HTMLElement[];

            // The first control, or the dialog itself - which carries tabindex="-1" so
            // it can hold focus while the reader has not reached a control yet.
            const first = focusables()[0];
            (first || dialog).focus();

            const onKeyDown = (e: KeyboardEvent) => {
                if (e.key !== "Tab") {
                    return;
                }
                const items = focusables();
                if (!items.length) {
                    e.preventDefault();
                    return;
                }
                const head = items[0];
                const tail = items[items.length - 1];
                const active = document.activeElement;
                if (e.shiftKey && (active === head || active === dialog)) {
                    e.preventDefault();
                    tail.focus();
                } else if (!e.shiftKey && active === tail) {
                    e.preventDefault();
                    head.focus();
                }
            };
            dialog.addEventListener("keydown", onKeyDown);
        }

        // The other half of trapFocus: called once the dialog has left the DOM, it
        // returns focus to the control that opened it.
        public static releaseFocus() {
            const opener = PdfViewer._focusReturn.pop();
            if (opener && opener.isConnected && opener.focus) {
                opener.focus();
            }
        }

        // Follows the roving tabindex of the thumbnail listbox: arrowing changes the
        // active option, and focus has to move with it or the next key is lost.
        public static focusThumb(container: HTMLElement, pageNumber: number) {
            if (!container) {
                return;
            }
            const target = container.querySelector(`[data-thumb='${pageNumber}']`) as HTMLElement | null;
            if (target && target.focus) {
                target.focus({ preventScroll: true });
            }
        }

        // Moves focus onto a bookmark row by its paint order, which is how the tree's
        // arrow keys follow the tab stop .NET just moved.
        public static focusOutlineItem(root: HTMLElement, index: number) {
            if (!root) {
                return;
            }
            const target = root.querySelector(`[data-bit-pdv-outline='${index}']`) as HTMLElement | null;
            if (target && target.focus) {
                target.focus({ preventScroll: false });
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
            (container as any).__bitPdvThumbFrame = requestAnimationFrame(() => {
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
            for (let i = 0; i < thumbs.length; i++) {
                const thumb = thumbs[i];
                const r = thumb.getBoundingClientRect();
                if (r.top > hi) {
                    break; // thumbnails are stacked in order; everything after is further down
                }
                // Only thumbnails still showing a placeholder need .NET.
                if (r.bottom < lo || !thumb.querySelector(".bit-pdv-page-placeholder")) {
                    continue;
                }
                const n = parseInt(thumb.getAttribute("data-thumb") || "", 10);
                if (!Number.isNaN(n)) {
                    needed.push(n);
                }
            }
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
            // A thumb render queued for the next animation frame would invoke the
            // (about to be disposed) .NET reference; cancel it and clear the flag
            // so a re-registration can schedule its initial fill normally.
            cancelAnimationFrame(c.__bitPdvThumbFrame);
            c.__bitPdvThumbFrame = null;
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

            // Only spans not corrected yet: this runs after every lazily-rendered page,
            // and re-measuring the (up to tens of thousands of) spans of every already
            // rendered page on each pass would stall the UI thread while scrolling.
            // Evicted pages re-render fresh markup, so their spans re-qualify naturally.
            const spans = Array.prototype.slice.call(container.querySelectorAll("span[data-w]:not([data-bit-pdv-wc])")) as HTMLElement[];
            // Batch all reads before all writes to avoid layout thrashing.
            const naturalWidths = spans.map((s) => s.offsetWidth);
            for (let i = 0; i < spans.length; i++) {
                const target = parseFloat(spans[i].getAttribute("data-w") || "");
                const natural = naturalWidths[i];
                // A zero natural width means the span wasn't laid out (e.g. hidden);
                // leave it unmarked so a later pass can retry the measurement.
                if (natural > 0) {
                    spans[i].setAttribute("data-bit-pdv-wc", "");
                    if (target > 0) {
                        spans[i].style.setProperty("--bit-pdv-sx", (target / natural).toString());
                    }
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

        public static exitFullscreen() {
            if (document.fullscreenElement) {
                document.exitFullscreen();
            }
        }

        // Reports fullscreen transitions to .NET. The browser can leave fullscreen on
        // its own (Escape, or the user switching away), and presentation mode has to
        // hear about it or the two states drift apart.
        public static registerFullscreenSpy(root: HTMLElement, dotnetRef: any) {
            if (!root || !dotnetRef) {
                return;
            }
            PdfViewer.disposeFullscreenSpy(root);
            const onChange = () => dotnetRef.invokeMethodAsync("OnFullscreenChanged", document.fullscreenElement === root);
            document.addEventListener("fullscreenchange", onChange);
            (root as any).__bitPdvFsc = onChange;
        }

        public static disposeFullscreenSpy(root: HTMLElement) {
            if (!root) {
                return;
            }
            const r = root as any;
            if (r.__bitPdvFsc) {
                document.removeEventListener("fullscreenchange", r.__bitPdvFsc);
                r.__bitPdvFsc = null;
            }
        }

        // Prints the rendered pages at their true physical size by cloning each page
        // into a hidden iframe (one sheet per page) and invoking the browser dialog.
        public static async print(container: HTMLElement, from?: number, to?: number) {
            if (!container) {
                return;
            }
            // A range prints the sheets it names and nothing else; without one every
            // rendered page goes to the printer, as it always has.
            const lo = typeof from === "number" && from > 0 ? from : 1;
            const hi = typeof to === "number" && to > 0 ? to : Number.MAX_SAFE_INTEGER;
            const pages = Array.prototype.slice.call(
                container.querySelectorAll("[data-page] .bit-pdv-html-page")) as HTMLElement[];
            const selected = pages.filter((el) => {
                const host = el.closest("[data-page]");
                const n = parseInt(host?.getAttribute("data-page") || "", 10);
                return !Number.isNaN(n) && n >= lo && n <= hi;
            });
            if (!selected.length) {
                return;
            }

            const frame = document.createElement("iframe");
            frame.setAttribute("aria-hidden", "true");
            frame.style.cssText = "position:fixed;right:0;bottom:0;width:0;height:0;border:0";
            document.body.appendChild(frame);

            const doc = (frame.contentDocument || frame.contentWindow?.document)!;
            // The sheet size drives the paper the browser picks; without it a landscape
            // page is laid onto portrait paper and cropped. Taken from the first page in
            // the range, which is the size a single-format document has throughout.
            const first = selected[0];
            const pageW = (parseFloat(first.style.width) || 612) * (96 / 72);
            const pageH = (parseFloat(first.style.height) || 792) * (96 / 72);
            doc.open();
            doc.write(
                "<!DOCTYPE html><html><head><meta charset='utf-8'><style>" +
                "@page{size:" + pageW.toFixed(2) + "px " + pageH.toFixed(2) + "px;margin:0}" +
                "html,body{margin:0;padding:0;background:#fff}" +
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
            for (const inner of selected) {
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
                    // display list re-rasterize at print resolution - the screen-resolution
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

            // @font-face faces load lazily - only when DOM text uses them - and canvas
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
                "::highlight(bit-pdv-search){background:var(--bit-clr-wrn,#EDAE12);color:var(--bit-clr-wrn-text,#141414)}" +
                "::highlight(bit-pdv-search-current){background:var(--bit-clr-swr,#CE4207);color:var(--bit-clr-swr-text,#FFFFFF)}";
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

        // Folds a string's combining marks away, returning the folded text alongside a
        // map from each folded index back to the index it came from in the original.
        // The map is what lets a match found in folded text be highlighted over the
        // real DOM text. .NET folds identically (NFD, then drop the non-spacing marks),
        // so the counter and this highlighter agree on what matched.
        private static fold(text: string): { text: string, map: number[] } {
            let folded = "";
            const map: number[] = [];
            for (let i = 0; i < text.length; i++) {
                const parts = text[i].normalize("NFD");
                for (const ch of parts) {
                    // \p{Mn} is the non-spacing-mark class - the accents themselves.
                    if (/\p{Mn}/u.test(ch)) {
                        continue;
                    }
                    folded += ch;
                    map.push(i);
                }
            }
            map.push(text.length); // one past the end, so an end offset always maps
            return { text: folded, map };
        }

        // The one form both sides of the search compare over: folded (unless diacritics
        // are being matched), then every run of whitespace collapsed to a single space,
        // with a map from each canonical index back to the index it came from in the
        // DOM text. .NET canonicalizes its extracted page text identically - which is
        // what stops its '\n'-per-line, space-per-gap heuristics from disagreeing with
        // the selection layer's positional ones about how many matches a page holds.
        private static canonical(text: string, matchDiacritics: boolean): { text: string, map: number[] } {
            const base = matchDiacritics ? null : PdfViewer.fold(text);
            const src = base ? base.text : text;
            const at = (i: number) => base ? base.map[i] : i;

            let out = "";
            const map: number[] = [];
            let pendingSpace = false;
            let pendingAt = 0;
            for (let i = 0; i < src.length; i++) {
                if (/\s/.test(src[i])) {
                    if (!pendingSpace) {
                        pendingSpace = true;
                        pendingAt = at(i);
                    }
                    continue;
                }
                if (pendingSpace) {
                    out += " ";
                    map.push(pendingAt);
                    pendingSpace = false;
                }
                out += src[i];
                map.push(at(i));
            }
            if (pendingSpace) {
                out += " ";
                map.push(pendingAt);
            }
            map.push(at(src.length)); // one past the end, so an end offset always maps
            return { text: out, map };
        }

        // Whether the character at `index` of `text` can be part of a word, used to
        // reject a substring hit that sits inside a longer word in whole-word mode.
        private static isWordChar(text: string, index: number) {
            if (index < 0 || index >= text.length) {
                return false;
            }
            // Unicode-aware: letters, digits, marks and the underscore count as word
            // characters, so accented and non-Latin scripts behave like ASCII does.
            return /[\p{L}\p{N}\p{M}_]/u.test(text[index]);
        }

        // Paints every occurrence of `query` on the pages currently in the DOM and
        // marks the one at (currentPage, currentOrdinal) - the match .NET counted its
        // way to - optionally scrolling it into view. Counting happens in .NET over
        // the whole document; this only decorates what is rendered, so an unrendered
        // page costs nothing here.
        // `matchCase` compares case-sensitively; `wholeWord` rejects hits whose
        // neighbouring characters are word characters.
        public static highlight(container: HTMLElement, query: string, matchCase: boolean, wholeWord: boolean,
            matchDiacritics: boolean, highlightAll: boolean,
            currentPage: number, currentOrdinal: number, scrollToCurrent: boolean) {
            PdfViewer.clearSearch(container);
            if (!container || !query || !PdfViewer.searchSupported()) {
                return;
            }
            PdfViewer.ensureSearchStyles();

            let needle = PdfViewer.canonical(query, matchDiacritics).text;
            needle = matchCase ? needle : needle.toLowerCase();
            if (!needle) {
                return;
            }
            const ranges: Range[] = [];
            let current: Range | null = null;

            container.querySelectorAll("[data-page]").forEach((page) => {
                const pageNumber = parseInt(page.getAttribute("data-page") || "", 10);
                // Search only the coalesced selection layer ([data-bit-pdv-sel]) - it
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
                    // Each selection run is its own visual line, separated in the DOM by
                    // a <br> the walker never sees. .NET's extracted text marks the same
                    // break with '\n', so put one here too - the canonical form collapses
                    // both to the single space that keeps the two counts in step.
                    if (text.length) {
                        text += "\n";
                    }
                    nodes.push({ node, start: text.length });
                    text += node.nodeValue;
                }
                // Matching runs over the canonical text, and the map takes each hit's
                // offsets back to the real text the ranges address.
                const canonical = PdfViewer.canonical(text, matchDiacritics);
                const haystack = matchCase ? canonical.text : canonical.text.toLowerCase();
                const toSource = (pos: number) => canonical.map[Math.min(pos, canonical.map.length - 1)];
                let idx = haystack.indexOf(needle);
                let ordinal = 0;
                while (idx !== -1) {
                    const end = idx + needle.length;
                    const bounded = !wholeWord
                        || (!PdfViewer.isWordChar(haystack, idx - 1) && !PdfViewer.isWordChar(haystack, end));
                    if (bounded) {
                        const range = PdfViewer.buildRange(nodes, toSource(idx), toSource(end));
                        if (range) {
                            ranges.push(range);
                            if (pageNumber === currentPage && ordinal === currentOrdinal) {
                                current = range;
                            }
                        }
                        ordinal++;
                    }
                    // Advance by one when a whole-word hit was rejected, so an
                    // overlapping later occurrence is still found.
                    idx = haystack.indexOf(needle, bounded ? end : idx + 1);
                }
            });

            (container as any).__bitPdvRanges = ranges;
            // With "highlight all" off only the match being walked to is painted, so the
            // page reads as it does without a search running.
            if (ranges.length && highlightAll) {
                (CSS as any).highlights.set("bit-pdv-search", new (globalThis as any).Highlight(...ranges));
            }
            if (current) {
                (CSS as any).highlights.set("bit-pdv-search-current", new (globalThis as any).Highlight(current));
                if (scrollToCurrent) {
                    const el = (current as Range).startContainer.parentElement;
                    if (el) {
                        PdfViewer.scrollWithin(container, el, "center", "smooth");
                    }
                }
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
