namespace BitBlazorUI {
    export class Draggables {
        private static _abortControllers: { [key: string]: AbortController } = {};

        public static enableDrag(id: string, selector: string | undefined) {
            Draggables.disableDrag(id);

            const ac = new AbortController();
            Draggables._abortControllers[id] = ac;

            const element = document.getElementById(id)! as HTMLElement;
            const dragElement = selector ? document.querySelector(selector)! as HTMLElement : element;
            const origCursor = dragElement.style.cursor;

            let x = 0;
            let y = 0;

            dragElement.addEventListener('pointerdown', handlePointerDown, { signal: ac.signal });

            function handlePointerDown(e: PointerEvent) {
                //e.preventDefault();

                x = e.clientX;
                y = e.clientY;

                document.addEventListener('pointerup', handlePointerUp, { signal: ac.signal });
                document.addEventListener('pointermove', handlePointerMove, { signal: ac.signal });

                dragElement.style.cursor = 'grabbing';
                dragElement.classList.add('bit-nta');
            }

            function handlePointerMove(e: PointerEvent) {
                //e.preventDefault();

                element.style.left = `${element.offsetLeft - (x - e.clientX)}px`;
                element.style.top = `${element.offsetTop - (y - e.clientY)}px`;

                element.style.right = 'unset';
                element.style.bottom = 'unset';

                x = e.clientX;
                y = e.clientY;
            }

            function handlePointerUp(e: PointerEvent) {
                //e.preventDefault();

                document.removeEventListener('pointerup', handlePointerUp);
                document.removeEventListener('pointermove', handlePointerMove);

                dragElement.style.cursor = origCursor;
                dragElement.classList.remove('bit-nta');
            }
        }

        public static disableDrag(id: string) {
            const ac = Draggables._abortControllers[id];
            if (!ac) return;

            ac.abort();

            delete Draggables._abortControllers[id];
        }
    }
}