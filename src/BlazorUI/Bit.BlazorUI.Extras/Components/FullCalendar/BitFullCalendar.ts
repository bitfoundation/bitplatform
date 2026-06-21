namespace BitBlazorUI {
    export class FullCalendar {
        public static scrollToHour(elementId: string, hour: number, pixelsPerHour: number | null): boolean {
            const el = document.getElementById(elementId);
            if (!el) return false;
            const pxPerHour = pixelsPerHour ?? 96;
            const top = hour * pxPerHour;
            if (typeof el.scrollTo === "function") {
                el.scrollTo({ top: top, behavior: "auto" });
            } else {
                el.scrollTop = top;
            }
            return true;
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

        public static scrollAgendaToDate(scrollContainerId: string, dateIso: string): boolean {
            const container = document.getElementById(scrollContainerId);
            if (!container) return false;
            const nodes = container.querySelectorAll('[data-agenda-date="' + dateIso + '"]');
            if (!nodes.length) return false;

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
            return true;
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
                let startSucceeded = false;
                let pendingEnd = false;

                try {
                    el.setPointerCapture(e.pointerId);
                } catch { /* older browsers */ }

                const flushMove = () => {
                    rafId = null;
                    const deltaMinutes = Math.round((latestY - startY) * minPerPixel);
                    return dotNetRef.invokeMethodAsync("OnResizeMove", direction, deltaMinutes);
                };

                const onPointerMove = (ev: PointerEvent) => {
                    if (ev.pointerId !== activePointerId) return;
                    latestY = ev.clientY;
                    // Don't emit move events until resize-start has been acknowledged by Blazor.
                    if (!startSucceeded) return;
                    if (rafId == null) {
                        rafId = requestAnimationFrame(() => {
                            void flushMove();
                        });
                    }
                };

                const endResize = async (ev?: PointerEvent) => {
                    if (ev && activePointerId != null && ev.pointerId !== activePointerId) return;
                    // A pointer release before resize-start completes is deferred and replayed afterwards.
                    if (!startSucceeded) { pendingEnd = true; return; }
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

                // Attach listeners before awaiting OnResizeStart so a fast pointer release is not missed.
                document.addEventListener("pointermove", onPointerMove);
                document.addEventListener("pointerup", endResize);
                document.addEventListener("pointercancel", endResize);

                try {
                    await dotNetRef.invokeMethodAsync("OnResizeStart", direction);
                } catch {
                    // Resize-start failed: detach the listeners we just attached so they don't
                    // dangle and release any captured pointer.
                    document.removeEventListener("pointermove", onPointerMove);
                    document.removeEventListener("pointerup", endResize);
                    document.removeEventListener("pointercancel", endResize);
                    try {
                        if (activePointerId != null && typeof el.releasePointerCapture === "function")
                            el.releasePointerCapture(activePointerId);
                    } catch { }
                    activePointerId = null;
                    return;
                }
                startSucceeded = true;
                // Replay a pointer release that happened before start completed.
                if (pendingEnd) await endResize();
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
                let startSucceeded = false;
                let pendingEnd = false;

                try { el.setPointerCapture(e.pointerId); } catch { /* older browsers */ }

                const flushMove = () => {
                    rafId = null;
                    const deltaPx = latestX - startX;
                    return dotNetRef.invokeMethodAsync("OnResizeMove", direction, deltaPx);
                };

                const onPointerMove = (ev: PointerEvent) => {
                    if (ev.pointerId !== activePointerId) return;
                    latestX = ev.clientX;
                    // Don't emit move events until resize-start has been acknowledged by Blazor.
                    if (!startSucceeded) return;
                    if (rafId == null) {
                        rafId = requestAnimationFrame(() => { void flushMove(); });
                    }
                };

                const endResize = async (ev?: PointerEvent) => {
                    if (ev && activePointerId != null && ev.pointerId !== activePointerId) return;
                    // A pointer release before resize-start completes is deferred and replayed afterwards.
                    if (!startSucceeded) { pendingEnd = true; return; }
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

                // Attach listeners before awaiting OnResizeStart so a fast pointer release is not missed.
                document.addEventListener("pointermove", onPointerMove);
                document.addEventListener("pointerup", endResize);
                document.addEventListener("pointercancel", endResize);

                try {
                    await dotNetRef.invokeMethodAsync("OnResizeStart", direction);
                } catch {
                    // Resize-start failed: detach the listeners we just attached so they don't
                    // dangle and release any captured pointer.
                    document.removeEventListener("pointermove", onPointerMove);
                    document.removeEventListener("pointerup", endResize);
                    document.removeEventListener("pointercancel", endResize);
                    try {
                        if (activePointerId != null && typeof el.releasePointerCapture === "function")
                            el.releasePointerCapture(activePointerId);
                    } catch { }
                    activePointerId = null;
                    return;
                }
                startSucceeded = true;
                // Replay a pointer release that happened before start completed.
                if (pendingEnd) await endResize();
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
