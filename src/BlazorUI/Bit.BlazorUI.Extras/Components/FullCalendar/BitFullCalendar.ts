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

            // Guard against duplicate handlers when init is invoked more than once on the same element.
            const boundKey = "__bitFcResizeBound";
            if ((el as any)[boundKey]) return;
            (el as any)[boundKey] = true;

            const pixelsPerHour = 96;
            const minPerPixel = 60 / pixelsPerHour;

            // A resize is serialized per event (per dotNetRef), not per handle: the C# event block
            // wires both resize handles (top/bottom) to the same dotNetRef, so this shared flag
            // ensures only one resize runs at a time across both handles. Cleared on end/cancel/abort.
            const activeKey = "__bitFcResizeActive";

            el.addEventListener("pointerdown", (e: PointerEvent) => {
                if (e.button !== 0) return;
                if ((dotNetRef as any)[activeKey]) return;
                e.preventDefault();
                e.stopPropagation();

                (dotNetRef as any)[activeKey] = true;
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
                            flushMove().catch(() => { /* transient interop failure while reporting resize move; safe to ignore */ });
                        });
                    }
                };

                const endResizeAsync = async (ev?: PointerEvent) => {
                    if (ev && activePointerId != null && ev.pointerId !== activePointerId) return;
                    // A pointer release before resize-start completes is deferred and replayed afterwards.
                    // Capture the release coordinate now so the replayed delta reflects where the pointer
                    // actually was, even when no move event fired between deferral and replay.
                    if (!startSucceeded) { if (ev) latestY = ev.clientY; pendingEnd = true; return; }
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

                        // Keep the per-event resize guard held until finalization completes so a new
                        // resize can't start before OnResizeEnd has finished committing the change.
                        // Reset the guard in a finally so a thrown OnResizeEnd can't leave the event
                        // permanently blocked from starting a new resize.
                        try {
                            await dotNetRef.invokeMethodAsync("OnResizeEnd");
                        } finally {
                            activePointerId = null;
                            (dotNetRef as any)[activeKey] = false;
                        }
                    }
                };

                // Non-async wrapper so the DOM listeners can't surface unhandled promise rejections.
                const endResize = (ev?: PointerEvent) => { void endResizeAsync(ev).catch(() => { /* ignore transient interop failure on resize end */ }); };

                // Attach listeners before awaiting OnResizeStart so a fast pointer release is not missed.
                document.addEventListener("pointermove", onPointerMove);
                document.addEventListener("pointerup", endResize);
                document.addEventListener("pointercancel", endResize);

                // Run the async start handshake without making the pointerdown listener itself async,
                // wrapping it so any rejection is swallowed rather than becoming an unhandled rejection.
                void (async () => {
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
                        (dotNetRef as any)[activeKey] = false;
                        return;
                    }
                    startSucceeded = true;
                    // Replay a pointer release that happened before start completed.
                    if (pendingEnd) await endResizeAsync();
                })().catch(() => { /* defensive: never surface an unhandled rejection from resize start */ });
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

            // Guard against duplicate handlers when init is invoked more than once on the same element.
            const boundKey = "__bitFcResizeHorizontalBound";
            if ((el as any)[boundKey]) return;
            (el as any)[boundKey] = true;

            // A resize is serialized per event (per dotNetRef), not per handle: BitFcTimelineEventBlock
            // wires both resize handles (start/end) to the same dotNetRef, so this shared flag ensures
            // only one resize runs at a time across both handles. Cleared on end/cancel/abort.
            const activeKey = "__bitFcResizeActive";

            el.addEventListener("pointerdown", (e: PointerEvent) => {
                if (e.button !== 0) return;
                if ((dotNetRef as any)[activeKey]) return;
                e.preventDefault();
                e.stopPropagation();

                (dotNetRef as any)[activeKey] = true;
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
                        rafId = requestAnimationFrame(() => { flushMove().catch(() => { /* transient interop failure while reporting resize move; safe to ignore */ }); });
                    }
                };

                const endResizeAsync = async (ev?: PointerEvent) => {
                    if (ev && activePointerId != null && ev.pointerId !== activePointerId) return;
                    // A pointer release before resize-start completes is deferred and replayed afterwards.
                    // Capture the release coordinate now so the replayed delta reflects where the pointer
                    // actually was, even when no move event fired between deferral and replay.
                    if (!startSucceeded) { if (ev) latestX = ev.clientX; pendingEnd = true; return; }
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

                        // Keep the per-event resize guard held until finalization completes so a new
                        // resize can't start before OnResizeEnd has finished committing the change.
                        // Reset the guard in a finally so a thrown OnResizeEnd can't leave the event
                        // permanently blocked from starting a new resize.
                        try {
                            await dotNetRef.invokeMethodAsync("OnResizeEnd");
                        } finally {
                            activePointerId = null;
                            (dotNetRef as any)[activeKey] = false;
                        }
                    }
                };

                // Non-async wrapper so the DOM listeners can't surface unhandled promise rejections.
                const endResize = (ev?: PointerEvent) => { void endResizeAsync(ev).catch(() => { /* ignore transient interop failure on resize end */ }); };

                // Attach listeners before awaiting OnResizeStart so a fast pointer release is not missed.
                document.addEventListener("pointermove", onPointerMove);
                document.addEventListener("pointerup", endResize);
                document.addEventListener("pointercancel", endResize);

                // Run the async start handshake without making the pointerdown listener itself async,
                // wrapping it so any rejection is swallowed rather than becoming an unhandled rejection.
                void (async () => {
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
                        (dotNetRef as any)[activeKey] = false;
                        return;
                    }
                    startSucceeded = true;
                    // Replay a pointer release that happened before start completed.
                    if (pendingEnd) await endResizeAsync();
                })().catch(() => { /* defensive: never surface an unhandled rejection from resize start */ });
            });
        }

        public static isMobile(): boolean {
            return window.innerWidth <= 768;
        }

        /**
         * Focus management for the calendar's modal dialogs. Stores the element that was focused
         * before the dialog opened, moves focus into the dialog, and keeps Tab/Shift+Tab navigation
         * contained within it. Pair every setupDialog call with teardownDialog so focus is restored
         * to the previously focused element when the dialog closes.
         */
        private static dialogFocusState = new WeakMap<HTMLElement, { previous: Element | null; handler: (e: KeyboardEvent) => void }>();

        private static getDialogFocusable(container: HTMLElement): HTMLElement[] {
            const selector = 'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';
            return Array.from(container.querySelectorAll<HTMLElement>(selector))
                .filter(el => !el.hasAttribute('disabled') && (el.offsetParent !== null || el.getClientRects().length > 0));
        }

        public static setupDialog(container: HTMLElement): void {
            if (!container || FullCalendar.dialogFocusState.has(container)) return;

            const previous = document.activeElement;

            const handler = (e: KeyboardEvent) => {
                if (e.key !== 'Tab') return;
                const focusable = FullCalendar.getDialogFocusable(container);
                if (focusable.length === 0) {
                    e.preventDefault();
                    container.focus();
                    return;
                }
                const first = focusable[0];
                const last = focusable[focusable.length - 1];
                const active = document.activeElement as HTMLElement | null;
                if (e.shiftKey) {
                    if (active === first || active == null || !container.contains(active)) {
                        e.preventDefault();
                        last.focus();
                    }
                } else if (active === last || active == null || !container.contains(active)) {
                    e.preventDefault();
                    first.focus();
                }
            };

            container.addEventListener('keydown', handler);
            FullCalendar.dialogFocusState.set(container, { previous, handler });

            const focusable = FullCalendar.getDialogFocusable(container);
            (focusable[0] ?? container).focus();
        }

        public static teardownDialog(container: HTMLElement): void {
            if (!container) return;
            const state = FullCalendar.dialogFocusState.get(container);
            if (!state) return;

            container.removeEventListener('keydown', state.handler);
            FullCalendar.dialogFocusState.delete(container);

            const previous = state.previous as HTMLElement | null;
            if (previous && typeof previous.focus === 'function' && document.contains(previous)) {
                previous.focus();
            }
        }

        public static getLocalStorage(key: string): string | null {
            // localStorage access can throw a DOMException when storage is disabled or unavailable
            // (private mode, blocked third-party storage, quota policies). Degrade gracefully.
            try {
                return localStorage.getItem(key);
            } catch {
                return null;
            }
        }

        public static setLocalStorage(key: string, value: string) {
            try {
                localStorage.setItem(key, value);
            } catch {
                /* storage unavailable/blocked: no-op so preference persistence fails silently */
            }
        }
    }
}
