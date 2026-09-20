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

                move(element.offsetLeft - (x - e.clientX), element.offsetTop - (y - e.clientY));

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

                move(element.offsetLeft + step[0] * distance, element.offsetTop + step[1] * distance);
            }

            // The one place the element's position is written, so a nudge lands exactly where a drag would:
            // pinned by its top-left corner, with the edges it may have been anchored to released.
            function move(left: number, top: number) {
                element.style.left = `${left}px`;
                element.style.top = `${top}px`;

                element.style.right = 'unset';
                element.style.bottom = 'unset';
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

        public static disable(id: string) {
            const ac = Draggables._abortControllers[id];
            if (!ac) return;

            ac.abort();

            delete Draggables._abortControllers[id];
        }
    }
}