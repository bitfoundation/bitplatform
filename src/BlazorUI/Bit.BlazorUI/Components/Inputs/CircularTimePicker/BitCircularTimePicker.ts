namespace BitBlazorUI {
    export class CircularTimePicker {
        private static _bitControllers: BitController[] = [];

        private static readonly _navigationKeys =
            ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End', ' '];

        public static setup(
            dotnetObj: DotNetObject,
            clock: HTMLElement,
            input: HTMLInputElement,
            pointerDownHandler: string,
            pointerMoveHandler: string,
            pointerUpHandler: string): string {

            const bitController = new BitController();
            bitController.dotnetObj = dotnetObj;

            // Tracks whether a drag started on the clock face. The document-level pointermove
            // listener only invokes .NET while a drag is active, so a picker sitting idle on the
            // page does not turn every pointer move into a JS-interop call.
            let dragging = false;

            // Coalesces pointermove events to at most one .NET interop call per animation frame.
            // A fast drag fires many moves between frames, so only the latest one is kept and
            // flushed via requestAnimationFrame.
            let latestMoveArgs: number[] | null = null;
            let rafId = 0;

            // The hit test is done here rather than on the .NET side so that a drag costs a single
            // one-way interop call per frame instead of an extra round-trip for the bounding rect,
            // and so what .NET receives is the position on the dial itself - an angle clockwise
            // from 12 o'clock and a distance as a fraction of the radius - instead of raw client
            // coordinates it cannot interpret without measuring the DOM again.
            const extractArgs = (e: PointerEvent): number[] | null => {
                const rect = clock.getBoundingClientRect();

                const radius = Math.min(rect.width, rect.height) / 2;
                if (radius <= 0) return null; // the dial is not laid out (hidden callout), so there is nothing to hit test against

                const x = e.clientX - (rect.left + rect.width / 2);
                const y = e.clientY - (rect.top + rect.height / 2);

                let angle = Math.atan2(x, -y) * 180 / Math.PI;
                if (angle < 0) angle += 360;

                return [angle, Math.sqrt(x * x + y * y) / radius];
            };

            const flushMove = () => {
                rafId = 0;
                if (dragging === false || latestMoveArgs === null) return;

                const args = latestMoveArgs;
                latestMoveArgs = null;
                dotnetObj.invokeMethodAsync(pointerMoveHandler, args[0], args[1]);
            };

            const endDrag = () => {
                if (dragging === false) return;

                if (rafId !== 0) {
                    cancelAnimationFrame(rafId);
                    rafId = 0;
                }

                // A pointer that comes up within the same frame as the last move leaves that move
                // unsent, and the position it carries is the one the drag was released at - so it is
                // flushed here, while the drag still counts as on, rather than the pick landing a frame
                // short of where the hand was let go.
                flushMove();

                dragging = false;
                latestMoveArgs = null;

                dotnetObj.invokeMethodAsync(pointerUpHandler);
            };

            clock.addEventListener('pointerdown', e => {
                // Only the primary button drags the hand: a right- or middle-click on the dial keeps
                // what it does everywhere else - a context menu, an auto-scroll - instead of being
                // swallowed by a pick that was never started.
                if ((e as PointerEvent).button !== 0) return;

                const args = extractArgs(e as PointerEvent);
                if (args === null) return;

                // Scoped to the dial itself, so the drag does not turn into a text selection or a
                // native image drag while it lasts - unlike a document-wide preventDefault, which
                // would take those away from the rest of the page too. Moving the focus is part of
                // what it cancels, so the dial takes it explicitly instead: a clock that was just
                // dragged has to be the thing the arrow keys go on to move.
                e.preventDefault();
                clock.focus({ preventScroll: true });

                dragging = true;
                dotnetObj.invokeMethodAsync(pointerDownHandler, args[0], args[1]);
            }, { signal: bitController.controller.signal });

            // Bound to the document rather than to the dial so a drag that wanders off the clock -
            // or ends outside of it - is still tracked to its end.
            document.addEventListener('pointermove', e => {
                if (dragging === false) return;

                const args = extractArgs(e as PointerEvent);
                if (args === null) return;

                latestMoveArgs = args;
                if (rafId === 0) {
                    rafId = requestAnimationFrame(flushMove);
                }
            }, { signal: bitController.controller.signal });

            // Blazor cannot preventDefault per key, so the keys the dial acts on are stopped here instead -
            // otherwise every arrow press would scroll the page underneath the callout as well as move the
            // pointer. Tab and the rest are deliberately left alone so focus can still leave the dial.
            clock.addEventListener('keydown', e => {
                if (CircularTimePicker._navigationKeys.indexOf((e as KeyboardEvent).key) < 0) return;

                e.preventDefault();
            }, { signal: bitController.controller.signal });

            // Shift+wheel over the focused dial is a value change on the .NET side, so the horizontal scroll
            // the browser would do with it is cancelled here - Blazor's own wheel handler cannot, being
            // registered passively. Whether the dial is taking the wheel at all is read off the element
            // itself, which .NET keeps up to date as part of its render, so no state has to be pushed over
            // interop for it; the focus requirement is the same one the .NET handler applies.
            clock.addEventListener('wheel', e => {
                if ((e as WheelEvent).shiftKey === false) return;
                if (clock.dataset.bitWheel !== '1') return;
                if (document.activeElement !== clock) return;

                e.preventDefault();
            }, { passive: false, signal: bitController.controller.signal });

            // The field works the callout with the very keys the browser scrolls the page with, so their
            // defaults are stopped here too - a key the picker has just opened its callout with must not also
            // scroll the page out from under it. The space bar only counts while the field is read-only, which
            // is exactly when the picker takes it: an editable one types a space with it instead.
            input?.addEventListener('keydown', e => {
                const key = (e as KeyboardEvent).key;

                if (key !== 'ArrowUp' && key !== 'ArrowDown' && (key !== ' ' || input.readOnly === false)) return;

                e.preventDefault();
            }, { signal: bitController.controller.signal });

            document.addEventListener('pointerup', endDrag, { signal: bitController.controller.signal });

            // Resets the drag state when the pointer sequence is cancelled (a browser gesture, a
            // lost capture) so a stale `dragging` flag does not keep firing move interop calls.
            document.addEventListener('pointercancel', endDrag, { signal: bitController.controller.signal });

            CircularTimePicker._bitControllers.push(bitController);

            return bitController.id;
        }

        public static dispose(id: string): void {
            const bitController = CircularTimePicker._bitControllers.find(bc => bc.id == id);
            bitController?.controller.abort();

            //bitController?.dotnetObj?.dispose(); // the DotNetObjectReference is owned and disposed by the component itself

            CircularTimePicker._bitControllers = CircularTimePicker._bitControllers.filter(bc => bc.id != id);
        }
    }
}
