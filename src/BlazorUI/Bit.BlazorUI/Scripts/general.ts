(BitBlazorUI as any).version = (window as any)['bit-blazorui version'] = '10.7.0-pre-02';

// bit-trim: always
// The one script every bundle keeps: the version stamp above, and what nearly every other script builds on. What used
// to be added to the page here as it loaded - the listeners of the open callouts, the claim of an Escape, the tooltips'
// Escape, the composition and the arrow keys - is added by the script it belongs to (Callouts.ts, Utils.Escape.ts,
// Utils.Tooltips.ts, Utils.Keys.ts), so an app trimmed down to the components it renders is given only the ones those
// components need, in the order the bundle loads them in.

interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

namespace BitBlazorUI {
    export class BitController {
        id: string = Utils.uuidv4();
        controller = new AbortController();
        dotnetObj: DotNetObject | undefined;
    }

    export namespace Utils {
        export function uuidv4(): string {
            try {
                const result = Utils.guidTemplate.replace(/[018]/g, (c) => {
                    const n = +c;
                    const random = crypto.getRandomValues(new Uint8Array(1));
                    const result = (n ^ random[0] & 15 >> n / 4);
                    return result.toString(16);
                });
                return result;
            } catch (e) {
                console.error("BitBlazorUI.Utils.uuidv4:", e);
                return '';
            }
        }
        // https://stackoverflow.com/questions/105034/how-to-create-a-guid-uuid/#2117523
        export const guidTemplate = '10000000-1000-4000-8000-100000000000';
    }
}
