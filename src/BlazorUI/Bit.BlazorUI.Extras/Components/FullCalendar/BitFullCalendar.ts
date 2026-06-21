namespace BitBlazorUI {
    export class FullCalendar {
        public static scrollToHour(elementId: string, hour: number, pixelsPerHour: number | null) {
            const el = document.getElementById(elementId);
            if (!el) return;
            const pxPerHour = pixelsPerHour ?? 96;
            const top = hour * pxPerHour;
            if (typeof el.scrollTo === "function") {
                el.scrollTo({ top: top, behavior: "auto" });
            } else {
                el.scrollTop = top;
            }
        }

        /**
         * Scrolls the timeline scroll container horizontally so the element marked with
         * data-bit-bfc-tl-scroll-target="true" sits just past the sticky resource gutter.
         * Direction-aware (works in both LTR and RTL layouts). Returns true if a target was
         * found and scroll was applied (or already in position), false otherwise.
         */
        public static scrollTimelineToTarget(scrollContainerId: string): boolean {
            const container = document.getElementById(scrollContainerId);
            if (!container) return false;
            const target = container.querySelector('[data-bit-bfc-tl-scroll-target="true"]');
            if (!target) return false;

            const gutter = container.querySelector('.bit-bfc-tl-corner');
            const gutterWidth = gutter ? gutter.getBoundingClientRect().width : 0;

            const cRect = container.getBoundingClientRect();
            const tRect = target.getBoundingClientRect();
            const isRtl = getComputedStyle(container).direction === "rtl";

            const delta = isRtl
                ? tRect.right - (cRect.right - gutterWidth)
                : tRect.left - (cRect.left + gutterWidth);
            if (Math.abs(delta) >= 0.5) {
                container.scrollLeft += delta;
            }
            return true;
        }

        public static scrollAgendaToDate(scrollContainerId: string, dateIso: string) {
            const container = document.getElementById(scrollContainerId);
            if (!container) return;
            const nodes = container.querySelectorAll('[data-agenda-date="' + dateIso + '"]');
            if (!nodes.length) return;

            let target = nodes[0];
            let bestTop = target.getBoundingClientRect().top;
            for (let i = 1; i < nodes.length; i++) {
                const top = nodes[i].getBoundingClientRect().top;
                if (top < bestTop) {
                    bestTop = top;
                    target = nodes[i];
                }
            }

            const containerRect = container.getBoundingClientRect();
            const targetRect = target.getBoundingClientRect();
            const scrollTop = container.scrollTop + (targetRect.top - containerRect.top);
            if (typeof container.scrollTo === "function") {
                container.scrollTo({ top: scrollTop, behavior: "auto" });
            } else {
                container.scrollTop = scrollTop;
            }
        }

        /**
         * Pointer resize for event blocks. Matches the idea of the reference calendar
         * (re-resizable client-side updates): coalesce pointer moves to animation frames,
         * capture the pointer, and await resize-start before tracking moves so Blazor state is ready.
         */
        public static initResize(dotNetRef: DotNetObject, elementId: string, direction: string) {
            const el = document.getElementById(elementId);
            if (!el) return;

            const pixelsPerHour = 96;
            const minPerPixel = 60 / pixelsPerHour;

            el.addEventListener("pointerdown", async (e: PointerEvent) => {
                if (e.button !== 0) return;
                e.preventDefault();
                e.stopPropagation();

                const startY = e.clientY;
                let latestY = startY;
                let rafId: number | null = null;
                let activePointerId: number | null = e.pointerId;
                let ended = false;

                try {
                    el.setPointerCapture(e.pointerId);
                } catch { /* older browsers */ }

                await dotNetRef.invokeMethodAsync("OnResizeStart", direction);

                const flushMove = () => {
                    rafId = null;
                    const deltaMinutes = Math.round((latestY - startY) * minPerPixel);
                    return dotNetRef.invokeMethodAsync("OnResizeMove", direction, deltaMinutes);
                };

                const onPointerMove = (ev: PointerEvent) => {
                    if (ev.pointerId !== activePointerId) return;
                    latestY = ev.clientY;
                    if (rafId == null) {
                        rafId = requestAnimationFrame(() => {
                            void flushMove();
                        });
                    }
                };

                const endResize = async (ev?: PointerEvent) => {
                    if (ev && activePointerId != null && ev.pointerId !== activePointerId) return;
                    if (ended) return;
                    ended = true;
                    document.removeEventListener("pointermove", onPointerMove);
                    document.removeEventListener("pointerup", endResize);
                    document.removeEventListener("pointercancel", endResize);

                    if (rafId != null) {
                        cancelAnimationFrame(rafId);
                        rafId = null;
                    }
                    const deltaMinutes = Math.round((latestY - startY) * minPerPixel);

                    try {
                        await dotNetRef.invokeMethodAsync("OnResizeMove", direction, deltaMinutes);
                    } finally {
                        try {
                            if (activePointerId != null && typeof el.releasePointerCapture === "function")
                                el.releasePointerCapture(activePointerId);
                        } catch { }

                        activePointerId = null;
                        await dotNetRef.invokeMethodAsync("OnResizeEnd");
                    }
                };

                document.addEventListener("pointermove", onPointerMove);
                document.addEventListener("pointerup", endResize);
                document.addEventListener("pointercancel", endResize);
            });
        }

        /**
         * Pointer resize for timeline event blocks along the horizontal time axis.
         * Sends raw pixel deltas to .NET; the C# side converts to minute deltas using the active
         * column's pixels-per-minute so the same handler works for hour-precision (day/week
         * timelines) and day-precision (month timeline). Events are always placed with absolute
         * `left:` from the left edge of the row, so a positive clientX delta always means
         * "later in time" regardless of writing direction.
         * direction is "start" (left edge of the event) or "end" (right edge of the event).
         */
        public static initResizeHorizontal(dotNetRef: DotNetObject, elementId: string, direction: string) {
            const el = document.getElementById(elementId);
            if (!el) return;

            el.addEventListener("pointerdown", async (e: PointerEvent) => {
                if (e.button !== 0) return;
                e.preventDefault();
                e.stopPropagation();

                const startX = e.clientX;
                let latestX = startX;
                let rafId: number | null = null;
                let activePointerId: number | null = e.pointerId;
                let ended = false;

                try { el.setPointerCapture(e.pointerId); } catch { /* older browsers */ }

                await dotNetRef.invokeMethodAsync("OnResizeStart", direction);

                const flushMove = () => {
                    rafId = null;
                    const deltaPx = latestX - startX;
                    return dotNetRef.invokeMethodAsync("OnResizeMove", direction, deltaPx);
                };

                const onPointerMove = (ev: PointerEvent) => {
                    if (ev.pointerId !== activePointerId) return;
                    latestX = ev.clientX;
                    if (rafId == null) {
                        rafId = requestAnimationFrame(() => { void flushMove(); });
                    }
                };

                const endResize = async (ev?: PointerEvent) => {
                    if (ev && activePointerId != null && ev.pointerId !== activePointerId) return;
                    if (ended) return;
                    ended = true;
                    document.removeEventListener("pointermove", onPointerMove);
                    document.removeEventListener("pointerup", endResize);
                    document.removeEventListener("pointercancel", endResize);

                    if (rafId != null) {
                        cancelAnimationFrame(rafId);
                        rafId = null;
                    }
                    const deltaPx = latestX - startX;

                    try {
                        await dotNetRef.invokeMethodAsync("OnResizeMove", direction, deltaPx);
                    } finally {
                        try {
                            if (activePointerId != null && typeof el.releasePointerCapture === "function")
                                el.releasePointerCapture(activePointerId);
                        } catch { }

                        activePointerId = null;
                        await dotNetRef.invokeMethodAsync("OnResizeEnd");
                    }
                };

                document.addEventListener("pointermove", onPointerMove);
                document.addEventListener("pointerup", endResize);
                document.addEventListener("pointercancel", endResize);
            });
        }

        public static isMobile(): boolean {
            return window.innerWidth <= 768;
        }

        public static getLocalStorage(key: string): string | null {
            return localStorage.getItem(key);
        }

        public static setLocalStorage(key: string, value: string) {
            localStorage.setItem(key, value);
        }
    }
}
