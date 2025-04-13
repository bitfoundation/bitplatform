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

            const origCursor = dragElement.style.cursor;

            let x = 0;
            let y = 0;

            let sx = 0;
            let sy = 0;
            let thresholdDragged = false;

            dragElement.addEventListener('pointerdown', handlePointerDown, { signal: ac.signal });

            async function handlePointerDown(e: PointerEvent) {
                //e.preventDefault();
                //e.stopPropagation();

                x = sx = e.clientX;
                y = sy = e.clientY;
                thresholdDragged = false;

                document.addEventListener('pointerup', handlePointerUp, { signal: ac.signal });
                document.addEventListener('pointermove', handlePointerMove, { signal: ac.signal });

                dragElement.style.cursor = 'grabbing';
                dragElement.classList.add('bit-nta');

                try { await dotnetObj?.invokeMethodAsync('OnDragStart'); } catch { }
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

                element.style.left = `${element.offsetLeft - (x - e.clientX)}px`;
                element.style.top = `${element.offsetTop - (y - e.clientY)}px`;

                element.style.right = 'unset';
                element.style.bottom = 'unset';

                x = e.clientX;
                y = e.clientY;

                try { await dotnetObj?.invokeMethodAsync('OnDragging'); } catch { }
            }

            async function handlePointerUp(e: PointerEvent) {
                //e.preventDefault();
                //e.stopPropagation();

                document.removeEventListener('pointerup', handlePointerUp);
                document.removeEventListener('pointermove', handlePointerMove);

                dragElement.style.cursor = origCursor;
                dragElement.classList.remove('bit-nta');

                try { await dotnetObj?.invokeMethodAsync('OnDragEnd'); } catch { }
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