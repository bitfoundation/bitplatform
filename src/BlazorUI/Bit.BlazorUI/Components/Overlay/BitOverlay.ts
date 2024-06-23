﻿namespace BitBlazorUI {
    export class Overlay {
        public static toggleScroll(selector: string, isVisible: boolean) {
            const element = document.querySelector(selector) as HTMLElement;

            if (!element) return 0;

            element.style.overflow = isVisible ? "hidden" : "auto";

            return element.scrollTop;
        }
    }
}
