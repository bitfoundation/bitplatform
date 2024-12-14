namespace BitBlazorUI {
    export class Utils {
        public static MIN_MOBILE_WIDTH = 320;
        public static MAX_MOBILE_WIDTH = 600;

        public static getBodyWidth() {
            return document.body.offsetWidth;
        }

        public static throttle(fn: Function, delay: number) {
            let timeoutItd: number | null = null;

            return (...args: any[]) => {
                if (timeoutItd === null) {
                    try { fn(...args); } catch { }
                    if (delay > 0) {
                        timeoutItd = setTimeout(() => {
                            timeoutItd = null;
                        }, delay);
                    }
                }
            };
        }

        public static isTouchDevice() {
            try {
                const matchMedia = window.matchMedia("(pointer: coarse)").matches;
                const maxTouchPoints = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
                return matchMedia || maxTouchPoints;
            } catch {
                return false;
            }
        }

        public static setProperty(element: Record<string, any>, property: string, value: any): void {
            if (!element) return;

            try {
                element[property] = value;
            } catch { }
        }

        public static getProperty(element: Record<string, any>, property: string): string | null {
            if (!element) return null;

            try {
                return element[property].toString();
            } catch {
                return '';
            }
        }

        public static getBoundingClientRect(element: HTMLElement): Partial<DOMRect> {
            if (!element) return {};

            try {
                return element.getBoundingClientRect();
            } catch {
                return {};
            }
        }

        public static scrollElementIntoView(targetElementId: string) {
            const element = document.getElementById(targetElementId);
            if (!element) return;

            try {
                element.scrollIntoView({
                    behavior: "smooth",
                    block: "start",
                    inline: "nearest"
                });
            } catch { }
        }

        public static selectText(element: HTMLInputElement) {
            if (!element) return;

            try {
                element.select();
            } catch { }
        }

        public static setStyle(element: HTMLElement, key: string, value: string) {
            if (!element || !element.style) return;

            try {
                (element.style as any)[key] = value;
            } catch { }
        }

        public static toggleOverflow(selector: string, isHidden: boolean) {
            const element = document.querySelector(selector) as HTMLElement;

            if (!element) return 0;

            try {
                element.style.overflow = isHidden ? "hidden" : "";
                return element.scrollTop;
            } catch {
                return 0;
            }
        }

        public static uuidv4(): string {
            try {
                const result = this.guidTemplate.replace(/[018]/g, (c) => {
                    const n = +c;
                    const random = crypto.getRandomValues(new Uint8Array(1));
                    const result = (n ^ random[0] & 15 >> n / 4);
                    return result.toString(16);
                });
                return result;
            } catch {
                return '';
            }
        }
        // https://stackoverflow.com/questions/105034/how-to-create-a-guid-uuid/#2117523
        private static guidTemplate = '10000000-1000-4000-8000-100000000000';
    }
}