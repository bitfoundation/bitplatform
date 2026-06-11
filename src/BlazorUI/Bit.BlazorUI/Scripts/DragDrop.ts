namespace BitBlazorUI {
    export class DragDrop {
        private static _listeners: { [key: string]: any } = {};

        public static setup(key: string, containerSelector: string, dragElementSelector: string) {
            DragDrop.remove(key, dragElementSelector);

            const element = document.querySelector(containerSelector) as HTMLElement;
            const dragElement = document.querySelector(dragElementSelector) as HTMLElement;
            if (!element || !dragElement) return;

            const listeners: any = {};
            DragDrop._listeners[key] = listeners;

            let x = 0;
            let y = 0;

            listeners['pointerdown'] = handlePointerDown;
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

            const dragElement = document.querySelector(dragElementSelector) as HTMLElement;
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
            delete DragDrop._listeners[key];
        }
    }
}
