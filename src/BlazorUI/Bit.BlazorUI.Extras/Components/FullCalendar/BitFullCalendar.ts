namespace BitBlazorUI {
    export class FullCalendar {
        /**
         * The rendered height of one hour of a time grid. The stylesheet sizes every hour row from
         * --bit-bfc-hour-height, which a consumer may set in any unit (4rem, 5em, a calc()), so the height
         * is measured off a rendered row rather than parsed out of the property or assumed to be the
         * 96px default .NET compiles against.
         */
        private static hourHeightOf(el: Element, fallback: number | null): number {
            const scope = el.closest(".bit-bfc") ?? el;
            const row = (el.querySelector(".bit-bfc-hour-row") ?? scope.querySelector(".bit-bfc-hour-row")) as HTMLElement | null;
            const measured = row ? row.getBoundingClientRect().height : NaN;
            if (Number.isFinite(measured) && measured > 0) return measured;
            const declared = parseFloat(getComputedStyle(el).getPropertyValue("--bit-bfc-hour-height"));
            return Number.isFinite(declared) && declared > 0 ? declared : (fallback ?? 96);
        }

        public static scrollToHour(elementId: string, hour: number, pixelsPerHour: number | null): boolean {
            const el = document.getElementById(elementId);
            if (!el) return false;
            const pxPerHour = FullCalendar.hourHeightOf(el, pixelsPerHour);
            const top = hour * pxPerHour;
            if (typeof el.scrollTo === "function") {
                el.scrollTo({ top: top, behavior: "auto" });
            } else {
                el.scrollTop = top;
            }
            return true;
        }

        /**
         * Scrolls the calendar's time grid (day or week view) so the time <offsetHours> past the grid's first hour sits
         * at the top of what is visible, just under the sticky day header. Returns false when no time grid is shown.
         */
        public static scrollGridToTime(root: HTMLElement, offsetHours: number): boolean {
            const scroller = root?.querySelector<HTMLElement>('.bit-bfc-week-scroll, .bit-bfc-timegrid-wrapper');
            const firstRow = scroller?.querySelector<HTMLElement>('.bit-bfc-hour-row');
            if (!scroller || !firstRow) return false;

            const pxPerHour = firstRow.getBoundingClientRect().height;
            const rowsTop = firstRow.getBoundingClientRect().top - scroller.getBoundingClientRect().top + scroller.scrollTop;
            const header = scroller.querySelector<HTMLElement>('.bit-bfc-week-header');
            const covered = header ? header.getBoundingClientRect().height : 0;
            scroller.scrollTop = Math.max(0, rowsTop + offsetHours * pxPerHour - covered);
            return true;
        }

        /**
         * Scrolls the calendar's timeline so the point <offsetPx> along its time axis sits just past the sticky resource
         * gutter, in either writing direction. Returns false when no timeline is shown.
         */
        public static scrollTimelineToOffset(root: HTMLElement, offsetPx: number): boolean {
            const scroller = root?.querySelector<HTMLElement>('.bit-bfc-tl-scroll');
            if (!scroller) return false;

            // A right-to-left scroller counts scrollLeft down from 0 toward the end.
            const rtl = getComputedStyle(scroller).direction === 'rtl';
            scroller.scrollLeft = rtl ? -offsetPx : offsetPx;
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
                // Measured when the drag starts, so a grid re-scaled through --bit-bfc-hour-height (or
                // zoomed) converts the pointer travel at the scale it is actually drawn at.
                const minPerPixel = 60 / FullCalendar.hourHeightOf(el, 96);
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

        private static readonly ROVING_STOP = '.bit-bfc-body [data-bit-bfc-roving][tabindex="0"]';
        private static readonly GRID_KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End'];
        // The teardown of every root set up, by the key the component disposes it with. A key rather than the element,
        // because by the time a disposed component's call arrives its root has usually left the DOM, where an element
        // reference no longer resolves.
        private static roots = new Map<string, () => void>();

        /**
         * What the calendar's root needs from the DOM that Blazor's own events cannot give it:
         * - the browser's default for the keys the calendar handles is cancelled - Alt+Arrow on an event would
         *   otherwise also go Back a page, Space on a role="button" scroll the page, and an arrow key on a grid
         *   scroll the grid before the roving focus catches up with it;
         * - the focus is recovered when a re-render removes the element that held it (an event moved from the
         *   keyboard, a view switched from a nav link, an event deleted), instead of dropping onto the page.
         * Everything it adds is taken off again by disposeRoot with the same key.
         */
        public static setupRoot(root: HTMLElement, key: string): void {
            if (!root || !key || FullCalendar.roots.has(key)) return;

            let lastFocused: HTMLElement | null = null;
            let scheduled = false;

            const onFocusIn = (e: FocusEvent) => {
                const target = e.target as HTMLElement;
                lastFocused = target;
                // After the frame, so whatever scrolled the element into view (the browser on Tab, focusElement on
                // an arrow key) has already done so.
                requestAnimationFrame(() => FullCalendar.revealFromStickies(target));
            };
            // Tabbing (or a script moving the focus) out of the calendar is leaving it, not losing the focus.
            const onFocusOut = (e: FocusEvent) => {
                const next = e.relatedTarget as Node | null;
                if (next && root.contains(next) === false) lastFocused = null;
            };
            root.addEventListener('focusin', onFocusIn);
            root.addEventListener('focusout', onFocusOut);
            root.addEventListener('keydown', FullCalendar.preventHandledKeyDefaults);

            // A press outside the calendar is the user taking the focus elsewhere: whatever it closes on its way
            // out (the settings popup, a picker) must not pull the focus back in. A root found gone without having
            // been disposed (a circuit that never got to say so) tears itself down here.
            const onOutsidePress = (e: PointerEvent) => {
                if (root.isConnected === false) {
                    FullCalendar.disposeRoot(key);
                    return;
                }
                if (root.contains(e.target as Node) === false) lastFocused = null;
            };
            document.addEventListener('pointerdown', onOutsidePress, true);

            const observer = new MutationObserver(() => {
                if (scheduled) return;
                scheduled = true;
                requestAnimationFrame(() => {
                    scheduled = false;
                    if (lastFocused && lastFocused.isConnected === false && FullCalendar.isFocusOnPage()) {
                        FullCalendar.recoverFocus(root, lastFocused);
                    }
                });
            });
            observer.observe(root, { childList: true, subtree: true });

            FullCalendar.roots.set(key, () => {
                root.removeEventListener('focusin', onFocusIn);
                root.removeEventListener('focusout', onFocusOut);
                root.removeEventListener('keydown', FullCalendar.preventHandledKeyDefaults);
                document.removeEventListener('pointerdown', onOutsidePress, true);
                observer.disconnect();
                lastFocused = null;
            });
        }

        /** Takes off everything setupRoot added for the calendar with this key, so a disposed calendar keeps nothing alive. */
        public static disposeRoot(key: string): void {
            const teardown = FullCalendar.roots.get(key);
            if (!teardown) return;

            FullCalendar.roots.delete(key);
            teardown();
        }

        /**
         * Scrolls a focused element out from under the sticky parts of its scroller - the day header of the week
         * grid, the time header and the resource gutter of the timeline - which neither the browser's focus
         * scrolling nor scrollIntoView know are covering it (WCAG 2.4.11, focus not obscured).
         */
        private static revealFromStickies(el: HTMLElement): void {
            if (el.isConnected === false || document.activeElement !== el) return;

            const scroller = el.closest<HTMLElement>('.bit-bfc-week-scroll, .bit-bfc-tl-scroll');
            if (!scroller) return;

            const rect = el.getBoundingClientRect();

            const header = scroller.querySelector<HTMLElement>('.bit-bfc-week-header, .bit-bfc-tl-header-row');
            if (header && header.contains(el) === false) {
                const covered = header.getBoundingClientRect().bottom - rect.top;
                if (covered > 0) scroller.scrollTop -= covered;
            }

            const gutter = el.closest<HTMLElement>('.bit-bfc-tl-body-row')?.querySelector<HTMLElement>('.bit-bfc-tl-resource-cell');
            if (gutter && gutter.contains(el) === false) {
                const edge = gutter.getBoundingClientRect();
                // The gutter sits on the inline start: the left in a left-to-right calendar, the right otherwise.
                // Scrolling toward the start is scrollLeft going down in the first and up (toward 0) in the second.
                const rtl = getComputedStyle(scroller).direction === 'rtl';
                const covered = rtl ? rect.right - edge.left : edge.right - rect.left;
                if (covered > 0) scroller.scrollLeft += rtl ? covered : -covered;
            }
        }

        private static preventHandledKeyDefaults(e: KeyboardEvent): void {
            if (e.defaultPrevented || e.ctrlKey || e.metaKey) return;
            const target = e.target as HTMLElement | null;
            if (!target || typeof target.matches !== 'function') return;

            // Only the axes an event says it edits on - data-bit-bfc-move (Alt+Arrow) and data-bit-bfc-resize
            // (Shift+Arrow), each "x", "y" or "xy" - so Alt+Left still goes Back from an event that does nothing with
            // it, and Shift+Arrow still extends a selection on one that cannot be resized.
            if ((e.altKey || e.shiftKey) && e.key.startsWith('Arrow')) {
                const axes = (e.altKey ? target.getAttribute('data-bit-bfc-move') ?? '' : '')
                    + (e.shiftKey ? target.getAttribute('data-bit-bfc-resize') ?? '' : '');
                const axis = e.key === 'ArrowLeft' || e.key === 'ArrowRight' ? 'x' : 'y';
                if (axes.includes(axis)) {
                    e.preventDefault();
                    return;
                }
            }

            if ((e.key === ' ' || e.key === 'Spacebar') && target.tagName !== 'BUTTON' && target.getAttribute('role') === 'button') {
                e.preventDefault();
                return;
            }

            // A grid that does not page (data-bit-bfc-roving="nopage") leaves PageUp/PageDown to the page, and a group
            // walked by the arrows alone (data-bit-bfc-roving="arrows", a radio group) every key but those.
            const roving = target.getAttribute('data-bit-bfc-roving');
            if (e.altKey === false && roving !== null && FullCalendar.GRID_KEYS.includes(e.key)
                && (roving === 'arrows' ? e.key.startsWith('Arrow') : e.key.startsWith('Page') === false || roving !== 'nopage')) {
                e.preventDefault();
            }
        }

        /** True while nothing on the page holds the focus - the state an element removed under it leaves behind. */
        private static isFocusOnPage(): boolean {
            const active = document.activeElement;
            return active === null || active === document.body || active === document.documentElement;
        }

        /**
         * Puts the focus back inside the calendar after the element that held it left the DOM: on the same event
         * when it was re-rendered elsewhere, else on the tab stop of the grid now showing, else on the body.
         */
        private static recoverFocus(root: HTMLElement, lost: Element | null): void {
            if (!root || root.isConnected === false) return;

            // While a dialog is still open (an event list whose row was just deleted from it) the focus stays in the
            // topmost one, never behind it.
            const dialogs = root.querySelectorAll<HTMLElement>('.bit-bfc-dialog');
            const scope: HTMLElement = dialogs.length > 0 ? dialogs[dialogs.length - 1] : root;

            const key = lost?.getAttribute?.('data-bit-bfc-event');
            const find = (k: string | null | undefined): HTMLElement | null => {
                if (!k) return null;
                const escaped = typeof CSS !== 'undefined' && CSS.escape ? CSS.escape(k) : k.replace(/"/g, '\\"');
                return scope.querySelector<HTMLElement>(`[data-bit-bfc-event="${escaped}"]:not([tabindex="-1"]):not([aria-hidden="true"])`);
            };
            // An occurrence moved out of its series comes back as a one-off with a key of its own; the body says which.
            // The occurrence's own key is the fallback, for a move that was refused and so never happened.
            const body = root.querySelector<HTMLElement>('.bit-bfc-body');
            const movedTo = key && body?.getAttribute('data-bit-bfc-moved-from') === key ? body.getAttribute('data-bit-bfc-moved-to') : null;
            let target: HTMLElement | null = find(movedTo) ?? find(key);
            target ??= scope === root
                ? root.querySelector<HTMLElement>(FullCalendar.ROVING_STOP) ?? root.querySelector<HTMLElement>('.bit-bfc-body')
                : FullCalendar.getDialogFocusable(scope)[0] ?? scope;
            if (!target) return;

            try {
                target.focus({ preventScroll: true });
            } catch {
                target.focus();
            }
        }

        public static isMobile(): boolean {
            return window.innerWidth <= 768;
        }

        /**
         * Moves DOM focus onto the element with the supplied id, scrolling it into view inside its
         * own scroller. The date grids use a roving tabindex: only one cell is in the tab order at a
         * time and the arrow keys move both the tabbable cell and the focus, which is what this call
         * carries out after the re-render. Returns false when the element is no longer there.
         */
        public static focusElement(elementId: string): boolean {
            const el = document.getElementById(elementId);
            if (!el) return false;

            // preventScroll keeps the browser from yanking the page; the explicit scrollIntoView
            // below only nudges the nearest scrollable ancestor, which is the grid itself.
            try {
                el.focus({ preventScroll: true });
            } catch {
                el.focus();
            }

            if (typeof el.scrollIntoView === "function") {
                try {
                    el.scrollIntoView({ block: "nearest", inline: "nearest" });
                } catch {
                    /* older browsers ignore the options object; the focus above is enough */
                }
            }

            return true;
        }

        /**
         * Focus management for the calendar's modal dialogs. Stores the element that was focused
         * before the dialog opened, moves focus into the dialog, and keeps Tab/Shift+Tab navigation
         * contained within it. Pair every setupDialog call with teardownDialog so focus is restored
         * to the previously focused element when the dialog closes.
         */
        private static dialogFocusState = new WeakMap<HTMLElement, { previous: Element | null; root: HTMLElement | null; handler: (e: KeyboardEvent) => void }>();

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
            FullCalendar.dialogFocusState.set(container, { previous, root: container.closest<HTMLElement>('.bit-bfc'), handler });

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
                return;
            }

            // The element that opened the dialog is gone - the event was deleted, or saved and re-rendered - so the
            // focus goes to the same event where it now is, or to the grid, once the render has landed. Only while
            // the focus is still in the calendar or nowhere: a dialog closed by a click elsewhere keeps that click's.
            const root = state.root;
            if (!root) return;
            requestAnimationFrame(() => {
                const active = document.activeElement;
                if (FullCalendar.isFocusOnPage() || (active && root.contains(active))) {
                    FullCalendar.recoverFocus(root, previous);
                }
            });
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
