namespace BitBlazorUI {
    export class Overlay {
        public static toggleScroll(selector: string, visible: boolean) {
            const element = document.querySelector(selector) as HTMLElement;

            if (!element) return 0;

            element.style.overflow = visible ? "hidden" : "auto";

            return element.scrollTop;
        }
    }
}
