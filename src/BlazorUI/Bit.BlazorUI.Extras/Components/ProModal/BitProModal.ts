namespace BitBlazorUI {
    export class ProModal {
        private static _dragDropListeners: any = {};

        public static setupDragDrop(containerSelector: string, dragElementSelector: string) {
            ProModal.removeDragDrop(containerSelector, dragElementSelector);
            const listeners: any = {};
            ProModal._dragDropListeners[containerSelector] = listeners;

            const element = document.querySelector(containerSelector) as HTMLElement;
            const dragElement = document.querySelector(dragElementSelector) as HTMLElement;
            if (!element || !dragElement) return;

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

        public static removeDragDrop(containerSelector: string, dragElementSelector: string) {
            const listeners = ProModal._dragDropListeners[containerSelector];
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
            delete ProModal._dragDropListeners[containerSelector];
        }
    }
}
