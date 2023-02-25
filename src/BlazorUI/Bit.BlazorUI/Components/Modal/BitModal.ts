class BitModal {
    public static toggleScroll(selector: string, isOpen: boolean) {
        const element = document.querySelector(selector) as HTMLElement;

        if (!element) return 0;

        element.style.overflow = isOpen ? "hidden" : "auto";

        return element.scrollTop;
    }

    private static DragDropListeners: any = {};

    public static setupDragDrop(containerId: string, dragElementSelector: string) {
        BitModal.removeDragDrop(containerId, dragElementSelector);
        const listeners: any = {};
        BitModal.DragDropListeners[containerId] = listeners;

        const element = document.getElementById(containerId)! as HTMLElement;
        const dragElement = document.querySelector(dragElementSelector)! as HTMLElement;

        let x = 0;
        let y = 0;

        listeners['pointerdown'] = handlePointerDown;
        dragElement.addEventListener('pointerdown', handlePointerDown);
        dragElement.style.cursor = 'move';

        function handlePointerDown(e: PointerEvent) {
            //e.preventDefault();

            x = e.clientX;
            y = e.clientY;

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

    public static removeDragDrop(id: string, dragElementSelector: string) {
        const listeners = BitModal.DragDropListeners[id];
        if (!listeners) return;

        const dragElement = document.querySelector(dragElementSelector)! as HTMLElement;

        dragElement.removeEventListener('pointerdown', listeners['pointerdown']);
        dragElement.style.cursor = '';

        document.removeEventListener('pointermove', listeners['pointermove']);

        document.removeEventListener('pointerup', listeners['pointerup']);
        //document.removeEventListener('pointerout', listeners['pointerup']);
        //document.removeEventListener('pointerleave', listeners['pointerup']);

        delete listeners['pointerdown'];
        delete listeners['pointermove'];
        delete listeners['pointerup'];
        delete BitModal.DragDropListeners[id];
    }
}
