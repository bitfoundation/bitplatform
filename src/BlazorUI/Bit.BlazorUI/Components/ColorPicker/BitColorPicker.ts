class BitColorPicker {
    static listOfAbortControllers: BitAbortController[] = [];

    static registerOnWindowPointerUpEvent(dotnetHelper: DotNetObject, callback: string): string {
        const controller = new BitAbortController();

        const listenerOptions = new BitEventListenerOptions();
        listenerOptions.signal = controller.abortController.signal;

        document.addEventListener('pointerup', e => {
            const eventArgs = BitColorPicker.toPointerEventArgsMapper(e);
            dotnetHelper.invokeMethodAsync(callback, eventArgs);
        }, listenerOptions);

        BitColorPicker.listOfAbortControllers.push(controller)

        return controller.id;
    }

    static registerOnWindowPointerMoveEvent(dotnetHelper: DotNetObject, callback: string): string {
        const controller = new BitAbortController();

        const listenerOptions = new BitEventListenerOptions();
        listenerOptions.signal = controller.abortController.signal;

        document.addEventListener('pointermove', e => {
            const eventArgs = BitColorPicker.toPointerEventArgsMapper(e);
            dotnetHelper.invokeMethodAsync(callback, eventArgs);
        }, listenerOptions);

        BitColorPicker.listOfAbortControllers.push(controller)

        return controller.id;
    }

    static toPointerEventArgsMapper(e: MouseEvent): object {
        return {
            altKey: e.altKey,
            button: e.button,
            buttons: e.buttons,
            clientX: e.clientX,
            clientY: e.clientY,
            ctrlKey: e.ctrlKey,
            detail: e.detail,
            metaKey: e.metaKey,
            offsetX: e.offsetX,
            offsetY: e.offsetY,
            screenY: e.screenY,
            screenX: e.screenX,
            shiftKey: e.shiftKey,
            type: e.type
        };
    }

    static abortProcedure(id: string): void {
        const aborController = BitColorPicker.listOfAbortControllers.find(ac => ac.id == id)?.abortController;

        if (aborController) {
            aborController.abort();
        }
    }
}

class BitEventListenerOptions implements EventListenerOptions {
    capture?: boolean;
    signal?: AbortSignal;
}

class BitAbortController {
    id: string = Date.now().toString();
    abortController = new AbortController();
}