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

        public static async initScripts(scripts: string[], isModule: boolean) {
            // Resolve only when every script has actually executed. Loading is tracked per-url so that
            // concurrent callers (e.g. several components, or a re-mount) await the same execution instead
            // of a second caller seeing the <script> tag in the DOM and assuming it is already usable.
            await Promise.all((scripts ?? []).map(s => Extras.loadScript(s, isModule)));
        }

        private static _scriptPromises: { [url: string]: Promise<void> } = {};
        private static loadScript(url: string, isModule: boolean): Promise<void> {
            // Track each script by url. Any script this method loads resolves only after its 'load'
            // event (i.e. after it has executed), so concurrent/duplicate callers await the real
            // execution rather than assuming readiness from the presence of the <script> tag.
            const existingPromise = Extras._scriptPromises[url];
            if (existingPromise !== undefined) return existingPromise;

            // A tag we didn't add is assumed to be host-provided and already executed by the time any
            // component runs (it is part of the initial document), so it is treated as ready.
            const alreadyOnPage = Array.from(document.scripts).some(s => s.src.includes(url));
            if (alreadyOnPage) {
                const resolved = Promise.resolve();
                Extras._scriptPromises[url] = resolved;
                return resolved;
            }

            const promise = new Promise<void>((res, rej) => {
                const script = document.createElement('script');
                script.src = url;
                if (isModule) {
                    script.type = 'module';
                }
                script.addEventListener('load', () => res());
                script.addEventListener('error', rej);
                document.body.appendChild(script);
            });

            Extras._scriptPromises[url] = promise;

            // Don't cache a rejected load: a later retry should be able to attempt the script again.
            promise.catch(() => { delete Extras._scriptPromises[url]; });

            return promise;
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