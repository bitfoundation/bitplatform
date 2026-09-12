namespace BitBlazorUI {
    export class Extras {
        public static applyRootClasses(cssClasses: string[], cssVariables: any) {
            cssClasses?.forEach(c => document.documentElement.classList.add(c));
            Object.keys(cssVariables).forEach(key => document.documentElement.style.setProperty(key, cssVariables[key]));
        }

        public static goToTop(element: HTMLElement, behavior: ScrollBehavior | undefined) {
            if (!element) return;

            behavior ??= undefined;

            element.scrollTo({ top: 0, behavior });
        }

        public static scrollBy(element: HTMLElement, x: number, y: number) {
            if (!element) return;

            element.scrollBy(x, y);
        }

        // Attaches (or updates) a deterministic keydown listener that calls preventDefault
        // for the provided keys. Unlike Blazor's `@onkeydown:preventDefault` binding -- whose
        // value is evaluated at render time and therefore only applies to the *next* key event
        // -- this evaluates the actual key of the *current* event, so stale state can never
        // block typing, Space, or Tab.
        public static setPreventKeys(element: HTMLElement, keys: string[]) {
            if (!element) return;

            const el = element as any;
            el.bitPreventKeys = keys ?? [];

            if (!el.bitPreventKeysHandler) {
                el.bitPreventKeysHandler = (e: KeyboardEvent) => {
                    // A key pressed with a modifier is a different gesture (Ctrl+Home jumps to the
                    // start of a text, Alt+ArrowDown is a browser shortcut), so only the bare key
                    // the component actually handles is suppressed.
                    if (e.shiftKey || e.ctrlKey || e.altKey || e.metaKey) return;

                    const ks: string[] = el.bitPreventKeys ?? [];
                    if (ks.indexOf(e.key) !== -1) {
                        e.preventDefault();
                    }
                };
                element.addEventListener('keydown', el.bitPreventKeysHandler);
            }
        }

        public static disposePreventKeys(element: HTMLElement) {
            if (!element) return;

            const el = element as any;
            if (el.bitPreventKeysHandler) {
                element.removeEventListener('keydown', el.bitPreventKeysHandler);
                delete el.bitPreventKeysHandler;
            }
            delete el.bitPreventKeys;
        }

        // Writes a reformatted text back into an input without throwing the caret to the end of it.
        // A phone number laid out over a mask is rewritten on almost every keystroke, and assigning
        // `value` alone always parks the caret after the last character, which makes editing the
        // middle of a number impossible. The caret is therefore expressed as "the nth digit of the
        // text" before the write and put back on the same digit afterwards, so the separators the
        // mask inserts or removes around it never move it.
        public static setInputValue(element: HTMLInputElement, value: string) {
            if (!element) return;

            value ??= '';

            const typed = element.value ?? '';
            const focused = document.activeElement === element;
            const start = focused ? element.selectionStart : null;

            if (typed !== value) {
                element.value = value;
            }

            if (!focused || start === null) return;

            const digits = Extras.countPhoneDigits(typed, start);
            const total = Extras.countPhoneDigits(value, value.length);

            let caret = value.length;

            if (digits < total) {
                caret = 0;
                let seen = 0;
                for (let i = 0; i < value.length; i++) {
                    if (!Extras.isPhoneDigit(value[i])) continue;
                    seen++;
                    if (seen === digits) {
                        caret = i + 1;
                        break;
                    }
                }
            }

            try {
                element.setSelectionRange(caret, caret);
            } catch (e) { /* an input type that has no text selection to set */ }
        }

        // The characters a phone number is actually made of, which are the ones that survive the
        // reformatting: everything else is a separator the mask owns rather than the user.
        private static isPhoneDigit(char: string) {
            return (char >= '0' && char <= '9') || char === '+';
        }

        private static countPhoneDigits(text: string, end: number) {
            let count = 0;
            const last = Math.min(end, text.length);
            for (let i = 0; i < last; i++) {
                if (Extras.isPhoneDigit(text[i])) count++;
            }
            return count;
        }

        // Scrolls the option element into the visible area of its scroll container using
        // 'nearest' so keyboard navigation keeps the active item on screen with minimal movement.
        public static scrollOptionIntoView(optionId: string) {
            if (!optionId) return;

            const element = document.getElementById(optionId);
            if (!element) return;

            try {
                element.scrollIntoView({ block: 'nearest', inline: 'nearest' });
            } catch (e) { console.error('BitBlazorUI.Extras.scrollOptionIntoView:', e); }
        }

        private static _initScriptsPromises: { [key: string]: Promise<unknown> } = {};
        public static async initScripts(scripts: string[], isModule: boolean) {
            const key = scripts.join('|');
            if (Extras._initScriptsPromises[key] !== undefined) {
                return Extras._initScriptsPromises[key];
            }

            const allScripts = Array.from(document.scripts).map(s => s.src);
            const notAddedScripts = scripts.filter(s => !allScripts.find(as => as.includes(s)));

            if (notAddedScripts.length == 0) return Promise.resolve();

            const promise = new Promise(async (res: any, rej: any) => {
                try {
                    await Promise.all(notAddedScripts.map(addScript));
                    res();
                } catch (e: any) {
                    rej(e);
                }
            });

            Extras._initScriptsPromises[key] = promise;
            return promise;

            async function addScript(url: string) {
                return new Promise((res, rej) => {
                    const script = document.createElement('script');
                    script.src = url;
                    if (isModule) {
                        script.type = 'module';
                    }
                    script.onload = res;
                    script.onerror = rej;
                    document.body.appendChild(script);
                })
            }
        }

        private static _initStylesheetsPromises: { [key: string]: Promise<unknown> } = {};
        public static async initStylesheets(stylesheets: string[], isModule: boolean) {
            const key = stylesheets.join('|');
            if (Extras._initStylesheetsPromises[key] !== undefined) {
                return Extras._initStylesheetsPromises[key];
            }

            const allStylesheets = Array.from(document.links).filter(l => l.rel === 'stylesheet').map(s => s.href);
            const notAddedStylesheets = stylesheets.filter(s => !allStylesheets.find(as => as.includes(s)));

            if (notAddedStylesheets.length == 0) return Promise.resolve();

            const promise = new Promise(async (res: any, rej: any) => {
                try {
                    await Promise.all(notAddedStylesheets.map(addStylesheet));
                    res();
                } catch (e: any) {
                    rej(e);
                }
            });

            Extras._initStylesheetsPromises[key] = promise;
            return promise;

            async function addStylesheet(url: string) {
                return new Promise((res, rej) => {
                    const link = document.createElement('link');
                    link.href = url;
                    link.rel = 'stylesheet';
                    link.onload = res;
                    link.onerror = rej;
                    document.head.appendChild(link);
                })
            }
        }


        public static invokeJs<T>(identifier: string, ...args: unknown[]): Promise<T> {
            identifier ??= '';
            identifier = identifier.trim();

            if (!identifier || identifier.length === 0) {
                throw new Error("Identifier must not be empty.");
            }

            const parts = identifier.split(".");

            let target = globalThis as unknown;

            const startIndex = parts[0] === "window" ? 1 : 0;

            for (let i = startIndex; i < parts.length - 1; i++) {
                const part = parts[i];
                if (target == null || typeof target !== "object") {
                    throw new Error(`Cannot read property '${part}' of ${target}`);
                }
                target = (target as Record<string, unknown>)[part];
            }

            const fnName = parts[parts.length - 1];
            const fn = (target as Record<string, unknown>)[fnName];

            if (typeof fn !== "function") {
                throw new Error(`'${identifier}' is not a function.`);
            }

            return Promise.resolve(fn.apply(target, args) as T);
        }
    }
}