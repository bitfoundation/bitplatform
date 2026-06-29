namespace BitBlazorUI {
    export class ColorPicker {
        private static _bitControllers: BitController[] = [];

        public static setup(dotnetObj: DotNetObject, saturationPicker: HTMLElement, pointerUpHandler: string, pointerMoveHandler: string): string {
            const bitController = new BitController();
            bitController.dotnetObj = dotnetObj;

            // Tracks whether a drag started on the saturation picker. The document-level
            // pointermove listener only invokes .NET while a drag is active, which avoids
            // flooding the JS-interop with a call for every pointer move on the page.
            let dragging = false;

            saturationPicker?.addEventListener('pointerdown', () => {
                dragging = true;
            }, { signal: bitController.controller.signal });

            document.addEventListener('pointermove', e => {
                if (dragging === false) return;
                dotnetObj.invokeMethodAsync(pointerMoveHandler, ColorPicker.extractArgs(e as MouseEvent));
            }, { signal: bitController.controller.signal });

            document.addEventListener('pointerup', e => {
                if (dragging === false) return;
                dragging = false;
                dotnetObj.invokeMethodAsync(pointerUpHandler, ColorPicker.extractArgs(e as MouseEvent));
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
