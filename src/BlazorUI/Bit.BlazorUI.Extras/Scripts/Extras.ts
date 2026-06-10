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
            // Match by the full absolute URL (origin + path + query + hash, resolved against the document
            // base) so that scripts from different origins (e.g. distinct CDNs) or with different query
            // strings (e.g. "?v=1" vs "?v=2") are treated as distinct rather than being conflated by a
            // shared pathname. Resolving against baseURI also avoids a substring like "lib.js" matching
            // "mylib.js". Use the same normalized form as the cache key so relative/absolute equivalents
            // hit the same entry.
            const normalize = (u: string) => {
                try { return new URL(u, document.baseURI).href; }
                catch { return u; }
            };
            const targetUrl = normalize(url);

            const existingPromise = Extras._scriptPromises[targetUrl];
            if (existingPromise !== undefined) return existingPromise;

            // A tag we didn't add is host-provided. If the document has finished loading, any non-async
            // script has already executed, so it is safe to treat as ready. Otherwise the tag may still be
            // loading (e.g. a deferred/async CDN script the host inserted), so await its load/error event
            // instead of assuming readiness from the mere presence of the <script> tag. Waiting is gated on
            // document.readyState so we never block on a 'load' event that has already fired.
            const existingTag = Array.from(document.scripts).find(s => !!s.src && normalize(s.src) === targetUrl);
            if (existingTag) {
                const ready = document.readyState === 'complete'
                    ? Promise.resolve()
                    : new Promise<void>((res) => {
                        existingTag.addEventListener('load', () => res(), { once: true });
                        // A failed host script shouldn't hang every awaiting caller; resolve and let the
                        // missing global surface as the usual "not a function" error at the call site.
                        existingTag.addEventListener('error', () => res(), { once: true });
                        // Final backstop: the window load event fires once all initial resources settle.
                        window.addEventListener('load', () => res(), { once: true });
                    });
                Extras._scriptPromises[targetUrl] = ready;
                return ready;
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

            Extras._scriptPromises[targetUrl] = promise;

            // Don't cache a rejected load: a later retry should be able to attempt the script again.
            promise.catch(() => { delete Extras._scriptPromises[targetUrl]; });

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