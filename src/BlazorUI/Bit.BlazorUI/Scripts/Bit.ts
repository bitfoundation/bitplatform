﻿class Bit {

    public static MIN_MOBILE_WIDTH = 320;
    public static MAX_MOBILE_WIDTH = 600;

    static setProperty(element: Record<string, any>, property: string, value: any): void {
        if (!element) return;
        element[property] = value;
    }

    static getProperty(element: Record<string, any>, property: string): string | null {
        return element?.[property];
    }

    static getClientHeight(element: HTMLElement): number {
        return element?.clientHeight;
    }

    static getBoundingClientRect(element: HTMLElement): DOMRect {
        return element?.getBoundingClientRect();
    }

    static scrollElementIntoView(targetElementId: string) {
        const element = document.getElementById(targetElementId);
        if (!element) return;

        element.scrollIntoView({
            behavior: "smooth",
            block: "start",
            inline: "nearest"
        });
    }

    static selectText(element: HTMLInputElement) {
        element?.select();
    }

    static setStyle(element: HTMLElement, key: string, value: string) {
        if (!element || !element.style) return;
        (element.style as any)[key] = value;
    }

    static preventDefault(element: HTMLElement, event: string) {
        element?.addEventListener(event, e => e.preventDefault(), { passive: false });
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

    static toggleOverflow(selector: string, isHidden: boolean) {
        const element = document.querySelector(selector) as HTMLElement;

        if (!element) return 0;

        element.style.overflow = isHidden ? "hidden" : "";

        return element.scrollTop;
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