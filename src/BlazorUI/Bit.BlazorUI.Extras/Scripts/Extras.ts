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

        // Scrolls the element into the visible area of its scroll container using
        // 'nearest' so keyboard navigation keeps the active item on screen with minimal movement.
        public static scrollElementIntoView(elementId: string) {
            if (!elementId) return;

            const element = document.getElementById(elementId);
            if (!element) return;

            try {
                element.scrollIntoView({ block: 'nearest', inline: 'nearest' });
            } catch (e) { console.error('BitBlazorUI.Extras.scrollElementIntoView:', e); }
        }
        
        public static async initScripts(scripts: string[], isModule: boolean) {
            // Resolve only when every script has actually executed. Loading is tracked per-url so that
            // concurrent callers (e.g. several components, or a re-mount) await the same execution instead
            // of a second caller seeing the <script> tag in the DOM and assuming it is already usable.
            //
            // Classic (non-module) scripts share a single global scope and execute in load order, not
            // insertion order, so loading them concurrently can run a dependent before its dependency
            // (BitChart's DateAdapterScripts, BitRichTextEditor's quill module scripts). To preserve the
            // guaranteed execution order those callers rely on, classic scripts are awaited sequentially.
            // ES modules resolve their own dependencies via import, so they are loaded concurrently.
            if (isModule) {
                const promises: Promise<void>[] = [];
                for (const s of scripts ?? []) {
                    promises.push(Extras.loadResource('script', s, true));
                }
                const results = await Promise.allSettled(promises);
                const failure = results.find((r): r is PromiseRejectedResult => r.status === 'rejected');
                if (failure) {
                    throw failure.reason;
                }
                return;
            }

            for (const s of scripts ?? []) {
                await Extras.loadResource('script', s, false);
            }
        }

        public static async initStylesheets(stylesheets: string[]) {
            // Resolve only when every stylesheet has actually loaded. Loading is tracked per-url so that
            // concurrent callers (e.g. several components, or a re-mount) await the same load instead
            // of a second caller seeing the <link> tag in the DOM and assuming it is already usable.
            // Kick off in array order (each call appends its <link> synchronously, preserving cascade
            // precedence) but await all settlements so one failure does not suppress the rest.
            const promises: Promise<void>[] = [];
            for (const s of stylesheets ?? []) {
                promises.push(Extras.loadResource('stylesheet', s));
            }
            const results = await Promise.allSettled(promises);
            const failure = results.find((r): r is PromiseRejectedResult => r.status === 'rejected');
            if (failure) {
                throw failure.reason;
            }
        }

        private static _scriptPromises: { [url: string]: Promise<void> } = {};
        private static _stylesheetPromises: { [url: string]: Promise<void> } = {};

        private static normalizeResourceUrl(url: string): string {
            try { return new URL(url, document.baseURI).href; }
            catch { return url; }
        }

        private static isHostStylesheetApplied(link: HTMLLinkElement): boolean {
            // After the document finishes loading, a stylesheet link with no .sheet failed to apply (404, CORS, etc.).
            return link.sheet !== null;
        }

        private static isHostScriptLoaded(script: HTMLScriptElement): boolean {
            if (script.hasAttribute('data-bit-load-failed')) {
                return false;
            }

            const src = script.src;
            if (!src) {
                return true;
            }

            // After the document finishes loading, Resource Timing (when exposed) distinguishes a fetched
            // script from a 404/network failure. We can only verify when a numeric HTTP status is present:
            // cross-origin entries without Timing-Allow-Origin report responseStatus 0, and browsers that
            // don't implement responseStatus (e.g. Safari, older Firefox) report undefined. In both cases
            // we cannot tell success from failure, so we assume the host tag succeeded.
            const url = Extras.normalizeResourceUrl(src);
            const entries = performance.getEntriesByName(url, 'resource') as PerformanceResourceTiming[];
            if (entries.length === 0) {
                return true;
            }

            const status = entries[entries.length - 1].responseStatus;
            if (status == null || status === 0) {
                return true;
            }

            return status >= 200 && status < 400;
        }

        private static loadResourceError(kind: 'script' | 'stylesheet', url: string): Error {
            return new Error(`Failed to load ${kind}: ${url}`);
        }

        private static awaitHostResource(element: HTMLElement, kind: 'script' | 'stylesheet', url: string): Promise<void> {
            if (document.readyState === 'complete') {
                if (kind === 'stylesheet' && !Extras.isHostStylesheetApplied(element as HTMLLinkElement)) {
                    return Promise.reject(Extras.loadResourceError(kind, url));
                }
                if (kind === 'script' && !Extras.isHostScriptLoaded(element as HTMLScriptElement)) {
                    return Promise.reject(Extras.loadResourceError(kind, url));
                }
                return Promise.resolve();
            }

            // An already-applied stylesheet has a non-null .sheet at any readyState, so short-circuit
            // instead of waiting on a 'load' event that may have already fired. We intentionally do NOT
            // apply the equivalent isHostScriptLoaded check here: it relies on Resource Timing, which has
            // no entry for an in-flight script before the document is 'complete', so it would resolve
            // prematurely for a host script that is still loading. Such scripts fall through to the
            // load/error/window listeners below, which await real readiness.
            if (kind === 'stylesheet' && Extras.isHostStylesheetApplied(element as HTMLLinkElement)) {
                return Promise.resolve();
            }

            return new Promise<void>((res, rej) => {
                const onError = () => {
                    element.setAttribute('data-bit-load-failed', '');
                    settle();
                };
                // Remove all three listeners as soon as one of them settles the Promise, so the closure
                // isn't kept alive by the still-registered listeners (notably the window 'load' one, which
                // may otherwise never fire). { once: true } only removes the listener that actually fired.
                const cleanup = () => {
                    element.removeEventListener('load', settle);
                    element.removeEventListener('error', onError);
                    window.removeEventListener('load', settle);
                };
                const settle = () => {
                    cleanup();
                    if (kind === 'stylesheet' && !Extras.isHostStylesheetApplied(element as HTMLLinkElement)) {
                        rej(Extras.loadResourceError(kind, url));
                        return;
                    }
                    if (kind === 'script' && !Extras.isHostScriptLoaded(element as HTMLScriptElement)) {
                        rej(Extras.loadResourceError(kind, url));
                        return;
                    }
                    res();
                };
                element.addEventListener('load', settle, { once: true });
                // On failure, mark the tag (so findExistingResource skips it on a later lookup) and let
                // settle() reject. Rejecting keeps the cache from being poisoned with a broken resource and
                // lets loadResource inject a fresh, working tag on retry. Scripts and stylesheets behave
                // identically here, matching the readyState === 'complete' branch above.
                element.addEventListener('error', onError, { once: true });
                // Final backstop: the window load event fires once all initial resources settle.
                window.addEventListener('load', settle, { once: true });
            });
        }

        // Classic <script> reuse must be limited to tags the browser actually executes as JavaScript.
        // A bare (typeless) or JS-typed script runs; data blocks like type="application/json" do not, so
        // they must never satisfy a classic-script lookup. The empty/absent type defaults to JavaScript.
        // This is the HTML spec's set of JavaScript MIME types; 'module' is intentionally excluded since
        // it's handled by the dedicated module branch.
        private static isExecutableClassicScriptType(type: string): boolean {
            const t = (type ?? '').trim().toLowerCase();
            if (t === '') return true;
            return [
                'text/javascript',
                'application/javascript',
                'application/ecmascript',
                'text/ecmascript',
                'application/x-ecmascript',
                'application/x-javascript',
                'text/javascript1.0',
                'text/javascript1.1',
                'text/javascript1.2',
                'text/javascript1.3',
                'text/javascript1.4',
                'text/javascript1.5',
                'text/jscript',
                'text/livescript',
                'text/x-ecmascript',
                'text/x-javascript',
            ].indexOf(t) !== -1;
        }

        private static findExistingResource(kind: 'script' | 'stylesheet', targetUrl: string, isModule?: boolean): HTMLElement | undefined {
            if (kind === 'script') {
                // Match the script type too: a classic script must not be reused when a module script is
                // requested (or vice versa), since they produce different <script> tags and execution semantics.
                // For the classic case, only reuse tags whose type is an executable JavaScript type so
                // non-executable tags (e.g. application/json) can never satisfy the lookup.
                const wantModule = !!isModule;
                return Array.from(document.scripts).find(s => !!s.src
                    && Extras.normalizeResourceUrl(s.src) === targetUrl
                    && (wantModule ? s.type === 'module' : Extras.isExecutableClassicScriptType(s.type))
                    && !s.hasAttribute('data-bit-load-failed')
                    && !(document.readyState === 'complete' && !Extras.isHostScriptLoaded(s)));
            }

            return Array.from(document.querySelectorAll<HTMLLinkElement>('link[rel="stylesheet"]'))
                .find(l => !!l.href
                    && Extras.normalizeResourceUrl(l.href) === targetUrl
                    && !l.hasAttribute('data-bit-load-failed')
                    && !(document.readyState === 'complete' && !Extras.isHostStylesheetApplied(l)));
        }

        private static createResourceElement(kind: 'script' | 'stylesheet', url: string, isModule?: boolean): HTMLElement {
            if (kind === 'script') {
                const script = document.createElement('script');
                script.src = url;
                if (isModule) {
                    script.type = 'module';
                }
                return script;
            }

            const link = document.createElement('link');
            link.href = url;
            link.rel = 'stylesheet';
            return link;
        }

        private static appendResourceElement(kind: 'script' | 'stylesheet', element: HTMLElement): void {
            (kind === 'script' ? document.body : document.head).appendChild(element);
        }

        private static loadResource(kind: 'script' | 'stylesheet', url: string, isModule?: boolean): Promise<void> {
            // Track each resource by url. Loads resolve only after the 'load' event (scripts after they
            // execute, stylesheets after they are applied), so concurrent/duplicate callers await the real
            // readiness rather than assuming it from the presence of a tag in the DOM.
            // Match by the full absolute URL (origin + path + query + hash, resolved against the document
            // base) so that resources from different origins or with different query strings are treated
            // as distinct. Resolving against baseURI also avoids substring collisions like "lib.js" matching
            // "mylib.js". Use the same normalized form as the cache key so relative/absolute equivalents
            // hit the same entry.
            const cache = kind === 'script' ? Extras._scriptPromises : Extras._stylesheetPromises;
            const targetUrl = Extras.normalizeResourceUrl(url);
            // The DOM lookup still matches by targetUrl, but the cache key for scripts also folds in the
            // isModule flag so the same URL loaded as a classic script vs a module script are cached as
            // distinct entries (they produce different <script> tags and execution semantics).
            const cacheKey = kind === 'script' ? `${targetUrl}\n${isModule ? 'module' : 'classic'}` : targetUrl;

            const existingPromise = cache[cacheKey];
            if (existingPromise !== undefined) return existingPromise;

            // A tag we didn't add is host-provided. If the document has finished loading, verify the
            // resource actually applied/executed before treating it as ready. Otherwise the tag may still
            // be loading (e.g. a deferred/async CDN tag the host inserted), so await its load/error event
            // instead of assuming readiness from the mere presence of the tag. Waiting is gated on
            // document.readyState so we never block on a 'load' event that has already fired.
            // Host resources that failed to apply/load are skipped here so a working tag can be injected below.
            const existingTag = Extras.findExistingResource(kind, targetUrl, isModule);
            if (existingTag) {
                const ready = Extras.awaitHostResource(existingTag, kind, url);
                // Drop the cache entry before delegating so the retry doesn't read this very promise back
                // out of the cache (which would create a Promises/A+ chaining cycle and reject with a
                // confusing TypeError). The failed host tag is marked data-bit-load-failed, so
                // findExistingResource skips it and the retry injects a fresh tag.
                const withRetry = ready.catch(() => {
                    delete cache[cacheKey];
                    return Extras.loadResource(kind, url, isModule);
                });
                cache[cacheKey] = withRetry;
                withRetry.catch(() => { delete cache[cacheKey]; });
                return withRetry;
            }

            const promise = new Promise<void>((res, rej) => {
                const element = Extras.createResourceElement(kind, url, isModule);
                element.addEventListener('load', () => res());
                // error is supported on <link rel="stylesheet"> in all browsers bitBlazorUI targets.
                element.addEventListener('error', () => {
                    // Remove the failed tag so a retry creates a fresh one instead of matching this
                    // broken element via findExistingResource (which would report readiness on complete).
                    element.remove();
                    rej(Extras.loadResourceError(kind, url));
                });
                Extras.appendResourceElement(kind, element);
            });

            cache[cacheKey] = promise;

            // Don't cache a rejected load: a later retry should be able to attempt the resource again.
            promise.catch(() => { delete cache[cacheKey]; });

            return promise;
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