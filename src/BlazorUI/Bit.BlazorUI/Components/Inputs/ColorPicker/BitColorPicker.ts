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

            // Coalesces pointermove events to at most one .NET interop call per animation
            // frame. Fast drags can fire many pointermove events between frames, so we keep
            // only the latest event and flush it via requestAnimationFrame.
            let latestMoveArgs: object | null = null;
            let rafId = 0;

            const flushMove = () => {
                rafId = 0;
                if (dragging === false || latestMoveArgs === null) return;
                const args = latestMoveArgs;
                latestMoveArgs = null;
                dotnetObj.invokeMethodAsync(pointerMoveHandler, args);
            };

            const endDrag = (e: PointerEvent) => {
                if (dragging === false) return;
                dragging = false;
                latestMoveArgs = null;
                if (rafId !== 0) {
                    cancelAnimationFrame(rafId);
                    rafId = 0;
                }
                dotnetObj.invokeMethodAsync(pointerUpHandler, ColorPicker.extractArgs(e as MouseEvent));
            };

            saturationPicker?.addEventListener('pointerdown', () => {
                dragging = true;
            }, { signal: bitController.controller.signal });

            document.addEventListener('pointermove', e => {
                if (dragging === false) return;
                latestMoveArgs = ColorPicker.extractArgs(e as MouseEvent);
                if (rafId === 0) {
                    rafId = requestAnimationFrame(flushMove);
                }
            }, { signal: bitController.controller.signal });

            document.addEventListener('pointerup', e => endDrag(e as PointerEvent), { signal: bitController.controller.signal });

            // Reset drag state when the pointer sequence is cancelled (e.g. browser gesture,
            // focus loss) so a stale `dragging` flag doesn't keep firing move interop calls.
            document.addEventListener('pointercancel', e => endDrag(e as PointerEvent), { signal: bitController.controller.signal });

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
