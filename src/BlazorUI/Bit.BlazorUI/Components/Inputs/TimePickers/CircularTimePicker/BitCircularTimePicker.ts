﻿namespace BitBlazorUI {
    class BitController {
        id: string = BitBlazorUI.Utils.uuidv4();
        controller = new AbortController();
        dotnetObj: DotNetObject | undefined;
    }

    export class CircularTimePicker {
        private static _bitControllers: BitController[] = [];

        public static registerEvent(event: string, dotnetObj: DotNetObject, methodName: string): string {
            const bitController = new BitController();
            bitController.dotnetObj = dotnetObj;

            document.addEventListener(event, e => {
                e.preventDefault();
                e.stopPropagation();
                dotnetObj.invokeMethodAsync(methodName, CircularTimePicker.extractArgs(e as MouseEvent));
            }, { signal: bitController.controller.signal });

            CircularTimePicker._bitControllers.push(bitController);

            return bitController.id;
        }

        public static abort(id: string, dispose: boolean): void {
            const bitController = CircularTimePicker._bitControllers.find(bc => bc.id == id);
            bitController?.controller.abort();

            if (dispose) {
                bitController?.dotnetObj?.dispose();
            }

            CircularTimePicker._bitControllers = CircularTimePicker._bitControllers.filter(bc => bc.id != id);
        }

        private static extractArgs(e: MouseEvent): object {
            return { ClientX: e.clientX, ClientY: e.clientY };
        }
    }
}