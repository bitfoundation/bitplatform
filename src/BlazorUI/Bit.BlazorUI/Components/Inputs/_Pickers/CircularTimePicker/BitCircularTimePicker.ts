namespace BitBlazorUI {
    export class CircularTimePicker {
        private static _bitControllers: BitController[] = [];

        public static setup(dotnetObj: DotNetObject, pointerUpHandler: string, pointerMoveHandler: string): string {
            const bitController = new BitController();
            bitController.dotnetObj = dotnetObj;

            document.addEventListener('pointerup', e => {
                e.preventDefault();
                e.stopPropagation();
                dotnetObj.invokeMethodAsync(pointerUpHandler, CircularTimePicker.extractArgs(e as MouseEvent));
            }, { signal: bitController.controller.signal });

            document.addEventListener('pointermove', e => {
                e.preventDefault();
                e.stopPropagation();
                dotnetObj.invokeMethodAsync(pointerMoveHandler, CircularTimePicker.extractArgs(e as MouseEvent));
            }, { signal: bitController.controller.signal });

            CircularTimePicker._bitControllers.push(bitController);

            return bitController.id;
        }

        public static dispose(id: string): void {
            const bitController = CircularTimePicker._bitControllers.find(bc => bc.id == id);
            bitController?.controller.abort();

            //bitController?.dotnetObj?.dispose(); // it is called in the Swipes.ts for this component!

            CircularTimePicker._bitControllers = CircularTimePicker._bitControllers.filter(bc => bc.id != id);
        }

        private static extractArgs(e: MouseEvent): object {
            return { ClientX: e.clientX, ClientY: e.clientY };
        }
    }
}