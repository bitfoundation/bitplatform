namespace BitBlazorUI {
    export class DragDrop {
        private static _listeners: { [key: string]: any } = {};

        public static setup(key: string, containerSelector: string, dragElementSelector: string) {
            DragDrop.remove(key, dragElementSelector);

            const element = document.querySelector(containerSelector) as HTMLElement;
            // The drag selector may point to the container itself (the default) or to a descendant.
            const dragElement = (element?.matches(dragElementSelector) ? element : element?.querySelector(dragElementSelector)) as HTMLElement;
            if (!element || !dragElement) return;

            const listeners: any = {};
            DragDrop._listeners[key] = listeners;

            let x = 0;
            let y = 0;

            listeners['pointerdown'] = handlePointerDown;
            listeners['dragElement'] = dragElement;
            dragElement.addEventListener('pointerdown', handlePointerDown);
            dragElement.style.cursor = 'move';
            dragElement.classList.add('bit-mdl-nta');

            function handlePointerDown(e: PointerEvent) {
                x = e.clientX;
                y = e.clientY;

                const { width } = element.getBoundingClientRect();
                element.style.width = `${width}px`;

                document.addEventListener('pointermove', handlePointerMove);
                listeners['pointermove'] = handlePointerMove;

                document.addEventListener('pointerup', handlePointerUp);
                listeners['pointerup'] = handlePointerUp;
            }

            function handlePointerMove(e: PointerEvent) {
                e.preventDefault();

                element.style.left = `${element.offsetLeft - (x - e.clientX)}px`;
                element.style.top = `${element.offsetTop - (y - e.clientY)}px`;

                x = e.clientX;
                y = e.clientY;
            }

            function handlePointerUp() {
                document.removeEventListener('pointermove', handlePointerMove);
                document.removeEventListener('pointerup', handlePointerUp);
            }
        }

        public static remove(key: string, dragElementSelector: string) {
            const listeners = DragDrop._listeners[key];
            if (!listeners) return;

            // Use the originally-bound drag element so cleanup still targets the
            // correct element even if the selector resolves differently now.
            const dragElement = (listeners['dragElement'] as HTMLElement) ?? (document.querySelector(dragElementSelector) as HTMLElement);
            if (dragElement) {
                dragElement.removeEventListener('pointerdown', listeners['pointerdown']);
                dragElement.style.cursor = '';
                dragElement.classList.remove('bit-mdl-nta');
            }

            document.removeEventListener('pointermove', listeners['pointermove']);
            document.removeEventListener('pointerup', listeners['pointerup']);

            delete listeners['pointerdown'];
            delete listeners['pointermove'];
            delete listeners['pointerup'];
            delete listeners['dragElement'];
            delete DragDrop._listeners[key];
        }
    }
}
