class Bit {
    static currentDropDownCalloutId = "";
    static currentCallout: BitCalloutComponent;
    static currentDropDownCalloutResponsiveModeIsEnabled = false;

    static init() {
        Bit.currentCallout = new BitCalloutComponent();
    }

    static setProperty(element: Record<string, any>, property: string, value: any): void {
        element[property] = value;
    }

    static getProperty(element: Record<string, any>, property: string): string | null {
        return element[property];
    }

    static getClientHeight(element: HTMLElement): number {
        return element.clientHeight;
    }

    static getBoundingClientRect(element: HTMLElement): DOMRect {
        return element.getBoundingClientRect();
    }

    static scrollElementIntoView(targetElementId: string) {
        const element = document.getElementById(targetElementId);

        if (element != null) {
            element.scrollIntoView({
                behavior: "smooth",
                block: "start",
                inline: "nearest"
            });
        }
    }

    static closeCurrentCalloutIfExists(calloutId: string, overlayId: string, obj: DotNetObject | null) {
        if (Bit.currentCallout.calloutId.length === 0 || Bit.currentCallout.overlayId.length === 0) {
            Bit.currentCallout.update(calloutId, overlayId, obj);
            return;
        }

        if (calloutId !== Bit.currentCallout.calloutId && overlayId !== Bit.currentCallout.overlayId) {
            const callout = document.getElementById(Bit.currentCallout.calloutId);
            if (callout == null)
                return;

            const overlay = document.getElementById(Bit.currentCallout.overlayId);
            if (overlay == null)
                return;

            callout.style.display = "none";
            overlay.style.display = "none";
            Bit.currentCallout.objRef?.invokeMethodAsync("CloseCallout");
            Bit.currentCallout.update(calloutId, overlayId, obj);
        }
    }

    static selectText(element: HTMLInputElement) {
        element.select();
    }

    static setStyle(element: HTMLElement, key: string, value: string) {
        (element.style as any)[key] = value;
    }

    static preventDefault(element: HTMLElement, event: string) {
        element.addEventListener(event, e => e.preventDefault(), { passive: false });
    }

    static getComputedTransform(element: HTMLElement) {
        const computedStyle = window.getComputedStyle(element);
        const matrix = computedStyle.getPropertyValue('transform');
        const matched = matrix.match(/matrix\((.+)\)/);

        if (matched && matched.length > 1) {
            const splitted = matched[1].split(',');
            return {
                ScaleX: +splitted[0],
                SkewY: +splitted[1],
                SkewX: +splitted[2],
                ScaleY: +splitted[3],
                TranslateX: +splitted[4],
                TranslateY: +splitted[5]
            }
        }

        return null;
    }

    static registerResizeObserver(element: HTMLElement, obj: DotNetObject, method: string = "resized") {
        const observer = new ResizeObserver(entries => {
            const entry = entries[0];
            if (!entry) return;
            obj.invokeMethodAsync(method, entry.contentRect);
        });

        observer.observe(element);
    }

    // https://stackoverflow.com/questions/105034/how-to-create-a-guid-uuid/#2117523
    private static guidTemplate = '10000000-1000-4000-8000-100000000000';
    static uuidv4(): string {
        const result = this.guidTemplate.replace(/[018]/g, (c) => {
            const n = +c;
            const random = crypto.getRandomValues(new Uint8Array(1));
            const result = (n ^ random[0] & 15 >> n / 4);
            return result.toString(16);
        });
        return result;
    }
}