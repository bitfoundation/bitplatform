namespace BitBlazorUI {
    export class Draggables {
        private static _abortControllers: { [key: string]: AbortController } = {};

        public static enable(
            id: string,
            dotnetObj: DotNetObject | undefined,
            selector: string | undefined) {
            if (Draggables._abortControllers[id]) return;

            const ac = new AbortController();
            Draggables._abortControllers[id] = ac;

            const element = document.getElementById(id) as HTMLElement;
            const dragElement = selector ? document.querySelector(selector) as HTMLElement : element;

            if (!element || !dragElement) return;

            let x = 0;
            let y = 0;

            let sx = 0;
            let sy = 0;
            let thresholdDragged = false;

            dragElement.classList.add('bit-nta');
            const origCursor = dragElement.style.cursor;

            dragElement.addEventListener('pointerdown', handlePointerDown, { signal: ac.signal });

            // WCAG 2.2 SC 2.5.7 (Dragging Movements): everything a drag can do has to be reachable without
            // one. The arrow keys move the focused element by a step, Shift by a coarser one, so a keyboard
            // or switch user can push it off whatever it is covering. It is bound to the drag element rather
            // than the document, so nothing is hijacked until the element itself has the focus.
            dragElement.addEventListener('keydown', handleKeyDown, { signal: ac.signal });

            async function handlePointerDown(e: PointerEvent) {
                //e.preventDefault();
                //e.stopPropagation();

                x = sx = e.clientX;
                y = sy = e.clientY;
                thresholdDragged = false;

                document.addEventListener('pointerup', handlePointerUp, { signal: ac.signal });
                document.addEventListener('pointermove', handlePointerMove, { signal: ac.signal });

                dragElement.style.cursor = 'grabbing';

                try { await dotnetObj?.invokeMethodAsync('OnDragStart', x, y); } catch { }
            }

            async function handlePointerMove(e: PointerEvent) {
                //e.preventDefault();
                //e.stopPropagation();

                if (!thresholdDragged) {
                    const diffX = e.clientX - sx;
                    const diffY = e.clientY - sy;

                    thresholdDragged = Math.abs(diffX) > 5 || Math.abs(diffY) > 5;
                }

                if (!thresholdDragged) return;

                moveBy(e.clientX - x, e.clientY - y);

                x = e.clientX;
                y = e.clientY;

                try { await dotnetObj?.invokeMethodAsync('OnDragging', x, y); } catch { }
            }

            function handleKeyDown(e: KeyboardEvent) {
                const steps: { [key: string]: [number, number] } = {
                    ArrowLeft: [-1, 0],
                    ArrowRight: [1, 0],
                    ArrowUp: [0, -1],
                    ArrowDown: [0, 1],
                };

                const step = steps[e.key];
                if (!step || e.altKey || e.ctrlKey || e.metaKey) return;

                // The arrow keys scroll the page by default, and the element being moved is usually pinned
                // over what is scrolling, so the two would fight for the same press.
                e.preventDefault();

                const distance = e.shiftKey ? 24 : 8;

                moveBy(step[0] * distance, step[1] * distance);
            }

            // Both the pointer and the keys move the element by a delta, and both read where it is now the
            // same way: left/top mean the offset parent's box for an absolutely positioned element, and the
            // viewport for a fixed one - which is what its own rect is measured against, scrolling included.
            function moveBy(deltaX: number, deltaY: number) {
                const parent = element.offsetParent as HTMLElement | null;

                if (parent) {
                    move(element.offsetLeft + deltaX, element.offsetTop + deltaY, parent.clientWidth, parent.clientHeight);
                } else {
                    const rect = element.getBoundingClientRect();

                    move(rect.left + deltaX, rect.top + deltaY, document.documentElement.clientWidth, document.documentElement.clientHeight);
                }
            }

            // The one place the element's position is written, so a nudge lands exactly where a drag would:
            // pinned by its top-left corner, with the edges it may have been anchored to released. It is kept
            // inside the box it is positioned in, since a move that drops it past an edge leaves the user with
            // no way to reach it again - the pointer has nothing left to grab and the keys nothing focused.
            function move(left: number, top: number, boundsWidth: number, boundsHeight: number) {
                element.style.left = `${clamp(left, boundsWidth - element.offsetWidth)}px`;
                element.style.top = `${clamp(top, boundsHeight - element.offsetHeight)}px`;

                element.style.right = 'unset';
                element.style.bottom = 'unset';
            }

            // A box smaller than the element it holds has a negative maximum, which would otherwise clamp to
            // the far side instead of the near one.
            function clamp(value: number, max: number) {
                return Math.min(Math.max(value, 0), Math.max(max, 0));
            }

            async function handlePointerUp(e: PointerEvent) {
                //e.preventDefault();
                //e.stopPropagation();

                document.removeEventListener('pointerup', handlePointerUp);
                document.removeEventListener('pointermove', handlePointerMove);

                dragElement.style.cursor = origCursor;
                //dragElement.classList.remove('bit-nta');

                try { await dotnetObj?.invokeMethodAsync('OnDragEnd', x, y); } catch { }
            }
        }

        // Drops the position a drag or a nudge wrote, so the element goes back to wherever its own styles
        // anchor it. It is what a component calls when the anchor itself changes: the inline left/top outrank
        // every rule that positions the element, so without this they would silently win for good.
        public static reset(id: string) {
            const element = document.getElementById(id);
            if (!element) return;

            element.style.left = '';
            element.style.top = '';
            element.style.right = '';
            element.style.bottom = '';
        }

        public static disable(id: string) {
            const ac = Draggables._abortControllers[id];
            if (!ac) return;

            ac.abort();

            delete Draggables._abortControllers[id];
        }
    }
}