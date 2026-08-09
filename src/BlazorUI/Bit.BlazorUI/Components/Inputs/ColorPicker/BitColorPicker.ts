namespace BitBlazorUI {
    export class ColorPicker {
        private static _bitControllers: BitController[] = [];

        // The keys the saturation area navigates with. They all scroll the page by default, which would
        // move the picker out from under the pointer while it is being driven from the keyboard, so their
        // default action is suppressed here. It is done in JavaScript rather than through
        // @onkeydown:preventDefault because that directive's value only takes effect from the next render
        // on - one press too late - and because it has to stay key-scoped so Tab still leaves the area.
        private static readonly NAVIGATION_KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End'];

        public static setup(dotnetObj: DotNetObject, saturationPicker: HTMLElement, pointerHandler: string, pointerUpHandler: string): string {
            const bitController = new BitController();
            bitController.dotnetObj = dotnetObj;
            const signal = bitController.controller.signal;

            // Tracks whether a drag started on the saturation area. The document-level pointermove
            // listener only invokes .NET while a drag is active, which avoids flooding the JS-interop
            // with a call for every pointer move on the page.
            let dragging = false;

            // Coalesces pointermove events to at most one .NET interop call per animation frame. Fast
            // drags can fire many pointermove events between frames, so we keep only the latest position
            // and flush it via requestAnimationFrame.
            let latestPosition: { x: number, y: number } | null = null;
            let rafId = 0;

            // The position is resolved to a fraction of the area here rather than in .NET: measuring the
            // element from .NET would cost a second interop round trip per frame, and the round trip that
            // returns the measurement is the one that cannot be coalesced away.
            const toRelativePosition = (e: PointerEvent) => {
                const rect = saturationPicker.getBoundingClientRect();

                if (rect.width === 0 || rect.height === 0) return { x: 0, y: 0 };

                return {
                    x: ColorPicker.clamp((e.clientX - rect.left) / rect.width),
                    y: ColorPicker.clamp((e.clientY - rect.top) / rect.height)
                };
            };

            const flushMove = () => {
                rafId = 0;
                if (dragging === false || latestPosition === null) return;
                const position = latestPosition;
                latestPosition = null;
                dotnetObj.invokeMethodAsync(pointerHandler, position.x, position.y);
            };

            const endDrag = () => {
                if (dragging === false) return;
                dragging = false;
                latestPosition = null;
                if (rafId !== 0) {
                    cancelAnimationFrame(rafId);
                    rafId = 0;
                }
                dotnetObj.invokeMethodAsync(pointerUpHandler);
            };

            saturationPicker?.addEventListener('pointerdown', e => {
                // Secondary and middle buttons open context menus and autoscroll rather than pick a color.
                if (e.button !== 0) return;

                dragging = true;

                // Suppresses the text selection and the native drag that a press on the gradient would
                // otherwise start. That also suppresses the focus the press would have moved, so the area
                // is focused explicitly - a picker clicked with the mouse should carry on from the keyboard.
                e.preventDefault();
                saturationPicker.focus();

                const position = toRelativePosition(e);
                dotnetObj.invokeMethodAsync(pointerHandler, position.x, position.y);
            }, { signal });

            saturationPicker?.addEventListener('keydown', e => {
                if (e.ctrlKey || e.altKey || e.metaKey) return;
                if (ColorPicker.NAVIGATION_KEYS.indexOf(e.key) < 0) return;
                e.preventDefault();
            }, { signal });

            document.addEventListener('pointermove', e => {
                if (dragging === false) return;
                latestPosition = toRelativePosition(e as PointerEvent);
                if (rafId === 0) {
                    rafId = requestAnimationFrame(flushMove);
                }
            }, { signal });

            document.addEventListener('pointerup', endDrag, { signal });

            // Reset drag state when the pointer sequence is cancelled (e.g. browser gesture, focus loss)
            // so a stale `dragging` flag doesn't keep firing move interop calls.
            document.addEventListener('pointercancel', endDrag, { signal });

            ColorPicker._bitControllers.push(bitController);

            return bitController.id;
        }

        /// Whether the browser can hand out the screen eyedropper. It is Chromium-only at the time of
        /// writing, so the button that opens it is only rendered where it would actually work.
        public static isEyeDropperSupported(): boolean {
            return typeof (window as any).EyeDropper === 'function';
        }

        /// Opens the browser's own eyedropper and resolves to the sampled sRGB color, or to null when the
        /// user dismisses it (which rejects with an AbortError) or the browser has none to open.
        public static async openEyeDropper(): Promise<string | null> {
            if (ColorPicker.isEyeDropperSupported() === false) return null;

            try {
                const result = await new (window as any).EyeDropper().open();
                return result?.sRGBHex ?? null;
            } catch {
                return null;
            }
        }

        public static dispose(id: string): void {
            const bitController = ColorPicker._bitControllers.find(bc => bc.id == id);
            bitController?.controller.abort();

            bitController?.dotnetObj?.dispose();

            ColorPicker._bitControllers = ColorPicker._bitControllers.filter(bc => bc.id != id);
        }

        private static clamp(value: number): number {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }
    }
}
