namespace BitBlazorUI {
    export class ColorPicker {
        private static _bitControllers: BitController[] = [];

        public static registerEvent(event: string, dotnetObj: DotNetObject, methodName: string): string {
            const bitController = new BitController();
            bitController.dotnetObj = dotnetObj;

            document.addEventListener(event, e => {
                dotnetObj.invokeMethodAsync(methodName, ColorPicker.extractArgs(e as MouseEvent));
            }, { signal: bitController.controller.signal });

            ColorPicker._bitControllers.push(bitController);

            return bitController.id;
        }

        public static abort(id: string, dispose: boolean): void {
            const bitController = ColorPicker._bitControllers.find(bc => bc.id == id);
            bitController?.controller.abort();

            if (dispose) {
                bitController?.dotnetObj?.dispose();
            }

            ColorPicker._bitControllers = ColorPicker._bitControllers.filter(bc => bc.id != id);
        }

        private static extractArgs(e: MouseEvent): object {
            return { ClientX: e.clientX, ClientY: e.clientY };
        }
    }
}