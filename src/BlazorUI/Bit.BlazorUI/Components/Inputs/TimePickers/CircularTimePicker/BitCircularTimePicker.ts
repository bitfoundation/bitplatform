namespace BitBlazorUI {
    class BitController {
        id: string = Date.now().toString();
        controller = new AbortController();
    }

    export class CircularTimePicker {
        private static _bitControllers: BitController[] = [];

        public static registerEvent(event: string, dotnetObj: DotNetObject, methodName: string): string {
            const bitController = new BitController();

            const listenerOptions = { signal: bitController.controller.signal };

            document.addEventListener(event, e => {
                dotnetObj.invokeMethodAsync(methodName, CircularTimePicker.extractArgs(e as MouseEvent));
            }, listenerOptions);

            CircularTimePicker._bitControllers.push(bitController);

            return bitController.id;
        }

        public static abort(id: string): void {
            CircularTimePicker._bitControllers.find(ac => ac.id == id)?.controller?.abort();
        }

        private static extractArgs(e: MouseEvent): object {
            return { ClientX: e.clientX, ClientY: e.clientY };
        }
    }
}