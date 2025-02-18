namespace BitBlazorUI {
    export class ColorPicker {
        private static _bitControllers: BitController[] = [];

        public static setup(dotnetObj: DotNetObject, pointerUpHandler: string, pointerMoveHandler: string): string {
            const bitController = new BitController();
            bitController.dotnetObj = dotnetObj;

            document.addEventListener('pointerup', e => {
                dotnetObj.invokeMethodAsync(pointerUpHandler, ColorPicker.extractArgs(e as MouseEvent));
            }, { signal: bitController.controller.signal });

            document.addEventListener('pointermove', e => {
                dotnetObj.invokeMethodAsync(pointerMoveHandler, ColorPicker.extractArgs(e as MouseEvent));
            }, { signal: bitController.controller.signal });

            ColorPicker._bitControllers.push(bitController);

            return bitController.id;
        }

        public static dispose(id: string): void {
            const bitController = ColorPicker._bitControllers.find(bc => bc.id == id);
            bitController?.controller.abort();

            bitController?.dotnetObj?.dispose();

            ColorPicker._bitControllers = ColorPicker._bitControllers.filter(bc => bc.id != id);
        }

        private static extractArgs(e: MouseEvent): object {
            return { ClientX: e.clientX, ClientY: e.clientY };
        }
    }
}