class BitColorPicker {
    private static abortControllers: BitAbortController[] = [];

    public static register(event: string, dotnetObj: DotNetObject, methodName: string): string {
        const bitController = new BitAbortController();

        const listenerOptions = new BitEventListenerOptions();
        listenerOptions.signal = bitController.controller.signal;

        document.addEventListener(event, e => {
            dotnetObj.invokeMethodAsync(methodName, BitColorPicker.extractArgs(e as MouseEvent));
        }, listenerOptions);

        BitColorPicker.abortControllers.push(bitController)

        return bitController.id;
    }

    public static abort(id: string): void {
        BitColorPicker.abortControllers.find(ac => ac.id == id)?.controller?.abort();
    }

    private static extractArgs(e: MouseEvent): object {
        return { ClientX: e.clientX, ClientY: e.clientY };
    }
}

class BitEventListenerOptions implements EventListenerOptions {
    capture?: boolean;
    signal?: AbortSignal;
}

class BitAbortController {
    id: string = Date.now().toString();
    controller = new AbortController();
}