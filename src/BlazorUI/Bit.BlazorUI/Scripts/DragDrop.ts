namespace BitBlazorUI {
    export class DragDrops {
        private static _dragDropListeners: any = {};

        public static setup(id: string, dragElementSelector: string) {
            DragDrops.remove(id, dragElementSelector);
            const listeners: any = {};
            DragDrops._dragDropListeners[id] = listeners;

            const element = document.getElementById(id) as HTMLElement;
            if (!element) return;

            const dragElement = document.querySelector(dragElementSelector) as HTMLElement;
            if (!dragElement) return;

            let x = 0;
            let y = 0;

            listeners['pointerdown'] = handlePointerDown;
            dragElement.addEventListener('pointerdown', handlePointerDown);
            dragElement.style.cursor = 'move';
            dragElement.classList.add('bit-nta');

            function handlePointerDown(e: PointerEvent) {
                //e.preventDefault();

                x = e.clientX;
                y = e.clientY;

                const { width } = element.getBoundingClientRect();
                element.style.width = `${width}px`;

                document.addEventListener('pointermove', handlePointerMove);
                listeners['pointermove'] = handlePointerMove;

                document.addEventListener('pointerup', handlePointerUp);
                //document.addEventListener('pointerout', handlePointerUp);
                //document.addEventListener('pointerleave', handlePointerUp);
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
                //document.removeEventListener('pointerout', handlePointerUp);
                //document.removeEventListener('pointerleave', handlePointerUp);
            }
        }

        public static remove(id: string, dragElementSelector: string) {
            const listeners = DragDrops._dragDropListeners[id];
            if (!listeners) return;

            const dragElement = document.querySelector(dragElementSelector)! as HTMLElement;

            dragElement.removeEventListener('pointerdown', listeners['pointerdown']);
            dragElement.style.cursor = '';
            dragElement.classList.remove('bit-nta');

            document.removeEventListener('pointermove', listeners['pointermove']);

            document.removeEventListener('pointerup', listeners['pointerup']);
            //document.removeEventListener('pointerout', listeners['pointerup']);
            //document.removeEventListener('pointerleave', listeners['pointerup']);

            delete listeners['pointerdown'];
            delete listeners['pointermove'];
            delete listeners['pointerup'];
            delete DragDrops._dragDropListeners[id];
        }
    }
}
