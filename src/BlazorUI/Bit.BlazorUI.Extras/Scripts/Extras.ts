namespace BitBlazorUI {
    export class Extras {
        public static applyRootClasses(cssClasses: string[], cssVariables: any) {
            cssClasses?.forEach(c => document.documentElement.classList.add(c));
            Object.keys(cssVariables).forEach(key => document.documentElement.style.setProperty(key, cssVariables[key]));
        }

        // A behavior handed in from C# overrides the scroll-behavior of the element, so the stylesheet
        // rule that takes the animation off under the reduced motion preference is not consulted at all
        // for these moves - the preference has to be read here instead, the same way every animated
        // component of the library reads it. Left undefined, the element (and therefore the stylesheet)
        // still decides, which is why only an asked-for animation is downgraded.
        private static behave(element: HTMLElement, behavior: ScrollBehavior | undefined): ScrollBehavior | undefined {
            if (behavior !== 'smooth') return behavior ?? undefined;

            return Extras.animates(element) ? 'smooth' : 'instant';
        }

        private static animates(element: HTMLElement): boolean {
            try {
                // The class opts a whole SUBTREE out of the preference, so an ancestor carrying it counts
                // for the element inside it - which is what the ForceAnimation of a container around it
                // is asking for.
                if (element.closest('.bit-fam')) return true;

                return matchMedia('(prefers-reduced-motion: reduce)').matches === false;
            } catch {
                return true;
            }
        }

        public static goToTop(element: HTMLElement, behavior: ScrollBehavior | undefined) {
            if (!element) return;

            element.scrollTo({ top: 0, behavior: Extras.behave(element, behavior) });
        }

        // scrollHeight is the FULL height of the content, so handing it over as the target lets the
        // browser clamp it to wherever the last scrollable pixel actually is - which is the same answer
        // as scrollHeight - clientHeight without this side having to read a second property for it.
        public static goToBottom(element: HTMLElement, behavior: ScrollBehavior | undefined) {
            if (!element) return;

            element.scrollTo({ top: element.scrollHeight, behavior: Extras.behave(element, behavior) });
        }

        // A null axis is left where it stands rather than being sent to 0, which is what makes one call
        // able to serve "scroll to this row", "scroll to this column" and "scroll to both" alike.
        public static scrollTo(element: HTMLElement, left: number | null, top: number | null, behavior: ScrollBehavior | undefined) {
            if (!element) return;

            element.scrollTo({
                left: left ?? element.scrollLeft,
                top: top ?? element.scrollTop,
                behavior: Extras.behave(element, behavior)
            });
        }

        public static scrollBy(element: HTMLElement, x: number, y: number, behavior?: ScrollBehavior | undefined) {
            if (!element) return;

            element.scrollBy({ left: x, top: y, behavior: Extras.behave(element, behavior) });
        }

        // Attaches (or updates) a deterministic keydown listener that calls preventDefault
        // for the provided keys. Unlike Blazor's `@onkeydown:preventDefault` binding -- whose
        // value is evaluated at render time and therefore only applies to the *next* key event
        // -- this evaluates the actual key of the *current* event, so stale state can never
        // block typing, Space, or Tab.
        //
        // The two optional selectors narrow the listener to the elements the keys really belong to,
        // for a container that also holds content of its own: targetSelector is what the key must
        // have been pressed on, and scopeSelector the container that element must belong to - so a
        // list of panels can suppress the page scroll of its own headers without touching the same
        // keys pressed inside a panel, or on the headers of another list nested in one.
        public static setPreventKeys(element: HTMLElement, keys: string[], targetSelector?: string, scopeSelector?: string) {
            if (!element) return;

            const el = element as any;
            el.bitPreventKeys = keys ?? [];
            el.bitPreventKeysTarget = targetSelector;
            el.bitPreventKeysScope = scopeSelector;

            if (!el.bitPreventKeysHandler) {
                el.bitPreventKeysHandler = (e: KeyboardEvent) => {
                    // A key pressed with a modifier is a different gesture (Ctrl+Home jumps to the
                    // start of a text, Alt+ArrowDown is a browser shortcut), so only the bare key
                    // the component actually handles is suppressed.
                    if (e.shiftKey || e.ctrlKey || e.altKey || e.metaKey) return;

                    const ks: string[] = el.bitPreventKeys ?? [];
                    if (ks.indexOf(e.key) === -1) return;

                    const target: string | undefined = el.bitPreventKeysTarget;
                    if (target) {
                        const node = e.target as Element;
                        if (!node || typeof node.matches !== 'function' || !node.matches(target)) return;

                        const scope: string | undefined = el.bitPreventKeysScope;
                        if (scope && node.closest(scope) !== element) return;
                    }

                    e.preventDefault();
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
            delete el.bitPreventKeysTarget;
            delete el.bitPreventKeysScope;
        }

        // Brings the element into view with the least movement that puts it there ('nearest'), so a panel
        // that opens below the fold is shown without the page jumping under a reader who could already see
        // it. The smooth scroll is a courtesy rather than a requirement, so it is dropped for a reader who
        // has asked for less motion.
        public static scrollIntoView(element: HTMLElement) {
            if (!element) return;

            try {
                const reduced = typeof window.matchMedia === 'function'
                    && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

                element.scrollIntoView({
                    behavior: reduced ? 'auto' : 'smooth',
                    block: 'nearest',
                    inline: 'nearest'
                });
            } catch (e) { console.error('BitBlazorUI.Extras.scrollIntoView:', e); }
        }

        // Answers with the indexes of the provided elements in the order they appear in the document, so a
        // component that cannot tell the order of the children it was given in markup - Blazor hands a child
        // its parameters again only when one of them has actually changed, so a child of nothing but
        // constants can sit a render out without reporting anything - can read it back from what was
        // rendered. An element that is not in the document is left out, which the caller reads as a miss.
        public static getElementsOrder(elements: HTMLElement[]): number[] {
            if (!elements) return [];

            const indexes = elements
                .map((el, i) => ({ el, i }))
                .filter(e => e.el && e.el.isConnected);

            indexes.sort((a, b) => {
                if (a.el === b.el) return 0;

                // DOCUMENT_POSITION_FOLLOWING (4) is set when b comes after a in the document.
                return (a.el.compareDocumentPosition(b.el) & Node.DOCUMENT_POSITION_FOLLOWING) ? -1 : 1;
            });

            return indexes.map(e => e.i);
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

        // Puts text on the clipboard, throwing when it could not be done so that the caller can tell.
        // The async Clipboard API is the only one that works without a user gesture heuristic, but it
        // is unavailable outside a secure context and in a few older browsers, so a hidden textarea and
        // the deprecated execCommand stand in for it there rather than leaving the copy silently undone.
        public static async copyToClipboard(text: string) {
            text ??= '';

            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return;
            }

            const textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.setAttribute('readonly', '');
            textarea.setAttribute('aria-hidden', 'true');
            // Pinned to the top-left of the viewport rather than left to flow: a textarea that is
            // selected while it sits below the fold scrolls the page away from whatever the copy button
            // belonged to. The size keeps it off the layout without making it unselectable.
            textarea.style.position = 'fixed';
            textarea.style.top = '0';
            textarea.style.left = '0';
            textarea.style.width = '1px';
            textarea.style.height = '1px';
            textarea.style.padding = '0';
            textarea.style.border = 'none';
            textarea.style.opacity = '0';
            textarea.style.pointerEvents = 'none';
            document.body.appendChild(textarea);

            // Selecting the textarea takes the focus off whatever the user was on, so it is put back.
            const previouslyFocused = document.activeElement as HTMLElement | null;

            try {
                textarea.select();
                if (!document.execCommand('copy')) {
                    throw new Error('the copy command was rejected');
                }
            } finally {
                document.body.removeChild(textarea);
                try { previouslyFocused?.focus?.(); } catch { }
            }
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

            // A rejected load is not remembered: caching it would make one CDN hiccup
            // permanent for the life of the document, so a later mount could never retry.
            Extras._initScriptsPromises[key] = promise.catch((e: any) => {
                delete Extras._initScriptsPromises[key];
                throw e;
            });
            return Extras._initScriptsPromises[key];

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

            // A rejected load is not remembered: caching it would make one CDN hiccup
            // permanent for the life of the document, so a later mount could never retry.
            Extras._initStylesheetsPromises[key] = promise.catch((e: any) => {
                delete Extras._initStylesheetsPromises[key];
                throw e;
            });
            return Extras._initStylesheetsPromises[key];

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