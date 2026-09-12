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
            // Every load is kicked off in array order before the first await so the downloads overlap.
            // Injected tags are created with async = false (see createResourceElement), which makes
            // dynamically inserted scripts execute in insertion order while downloading in parallel, so a
            // classic script that depends on an earlier one in the list still runs after it (ES modules
            // additionally resolve their own dependencies via import). A host-provided tag still in flight
            // in the middle of the list is not ordered relative to the injected ones - the same as with the
            // previous loader. Each promise clears its own cache entry on failure (see loadResource), so
            // nothing is lost when Promise.all rejects on the first failure.
            await Promise.all((scripts ?? []).map(s => Extras.loadResource('script', s, isModule)));
        }

        public static async initStylesheets(stylesheets: string[]) {
            // Resolve only when every stylesheet has actually loaded. Loading is tracked per-url so that
            // concurrent callers (e.g. several components, or a re-mount) await the same load instead
            // of a second caller seeing the <link> tag in the DOM and assuming it is already usable.
            // Kick off in array order (each call appends its <link> synchronously, preserving cascade
            // precedence) before the first await so the downloads overlap. Each promise clears its own
            // cache entry on failure (see loadResource), so nothing is lost when Promise.all rejects on
            // the first failure.
            await Promise.all((stylesheets ?? []).map(s => Extras.loadResource('stylesheet', s)));
        }

        private static _scriptPromises: { [key: string]: Promise<void> } = {};
        private static _stylesheetPromises: { [key: string]: Promise<void> } = {};

        // A resource is identified by origin + path, resolved against the document base. The query string
        // and hash are dropped on purpose: a host copy carrying a cache-buster
        // (<script src="https://cdn/x/mapbox-gl.js?v=2">) must be reused rather than re-injected, since
        // re-executing e.g. mapbox-gl would wipe the access token the host already set. Resolving against
        // baseURI makes relative/absolute equivalents share one key and avoids substring collisions like
        // "lib.js" matching "mylib.js". This one key is used both for the promise cache and for matching
        // host tags in findExistingResource.
        private static resourceKey(url: string): string {
            try {
                const resolved = new URL(url, document.baseURI);
                return resolved.origin + resolved.pathname;
            }
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

            // Once the script has been fetched, Resource Timing (when exposed) distinguishes a fetched
            // script from a 404/network failure. Entries are keyed by the exact URL that was fetched, so
            // the lookup uses the tag's own (already absolute) src, query string included, not the
            // origin + path key the loader matches tags by. We can only verify when a numeric HTTP status
            // is present: cross-origin entries without Timing-Allow-Origin report responseStatus 0, and
            // browsers that don't implement responseStatus (e.g. Safari, older Firefox) report undefined.
            // In both cases we cannot tell success from failure, so we assume the host tag succeeded.
            const entries = performance.getEntriesByName(src, 'resource') as PerformanceResourceTiming[];
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
            // instead of waiting on a 'load' event that may have already fired.
            if (kind === 'stylesheet' && Extras.isHostStylesheetApplied(element as HTMLLinkElement)) {
                return Promise.resolve();
            }

            // Likewise for a host script whose fetch has already completed: its load/error events fired
            // before we got here and never fire again, so waiting on them (and on the window 'load'
            // backstop) would stall init on every other resource of the page. A Resource Timing entry
            // exists once the fetch is done, and a fetched classic or async script has executed by the
            // time Blazor interop runs, so the same status check as the 'complete' branch applies. The
            // accepted edge is a defer script whose fetch finished but which has not executed yet: it
            // executes before DOMContentLoaded, which in practice precedes any Blazor interop call.
            // A script with no entry yet is still in flight and falls through to the listeners below,
            // which await real readiness.
            if (kind === 'script' && performance.getEntriesByName((element as HTMLScriptElement).src, 'resource').length > 0) {
                return Extras.isHostScriptLoaded(element as HTMLScriptElement)
                    ? Promise.resolve()
                    : Promise.reject(Extras.loadResourceError(kind, url));
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

        // Matches host tags by resourceKey (origin + path), so a host copy with a different query string
        // or hash is recognised as the same resource.
        private static findExistingResource(kind: 'script' | 'stylesheet', targetKey: string, isModule?: boolean): HTMLElement | undefined {
            if (kind === 'script') {
                // Match the script type too: a classic script must not be reused when a module script is
                // requested (or vice versa), since they produce different <script> tags and execution semantics.
                // For the classic case, only reuse tags whose type is an executable JavaScript type so
                // non-executable tags (e.g. application/json) can never satisfy the lookup. Also exclude
                // nomodule scripts: they don't execute in module-capable browsers, so reusing one would
                // falsely report the resource as loaded.
                const wantModule = !!isModule;
                return Array.from(document.scripts).find(s => !!s.src
                    && Extras.resourceKey(s.src) === targetKey
                    && (wantModule ? (s.type ?? '').trim().toLowerCase() === 'module' : (Extras.isExecutableClassicScriptType(s.type) && !s.noModule))
                    && !s.hasAttribute('data-bit-load-failed')
                    && !(document.readyState === 'complete' && !Extras.isHostScriptLoaded(s)));
            }

            return Array.from(document.querySelectorAll<HTMLLinkElement>('link[rel="stylesheet"]'))
                .find(l => !!l.href
                    && Extras.resourceKey(l.href) === targetKey
                    && !l.hasAttribute('data-bit-load-failed')
                    && !(document.readyState === 'complete' && !Extras.isHostStylesheetApplied(l)));
        }

        private static createResourceElement(kind: 'script' | 'stylesheet', url: string, isModule?: boolean): HTMLElement {
            if (kind === 'script') {
                const script = document.createElement('script');
                script.src = url;
                // Dynamically inserted scripts default to async = true and execute in whatever order they
                // finish downloading. Turning that off makes them execute in insertion order while still
                // downloading in parallel, which is what lets initScripts kick every load off at once and
                // still run a dependent classic script after its dependency.
                script.async = false;
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

        // Injects a fresh tag for the resource and resolves on its 'load' event (scripts after they
        // execute, stylesheets after they are applied).
        private static injectResource(kind: 'script' | 'stylesheet', url: string, isModule?: boolean): Promise<void> {
            return new Promise<void>((res, rej) => {
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
        }

        private static loadResource(kind: 'script' | 'stylesheet', url: string, isModule?: boolean): Promise<void> {
            // Track each resource by its origin + path key (see resourceKey). Loads resolve only after the
            // 'load' event (scripts after they execute, stylesheets after they are applied), so
            // concurrent/duplicate callers await the real readiness rather than assuming it from the
            // presence of a tag in the DOM.
            const cache = kind === 'script' ? Extras._scriptPromises : Extras._stylesheetPromises;
            const targetKey = Extras.resourceKey(url);
            // The DOM lookup matches by targetKey, but the cache key for scripts also folds in the
            // isModule flag so the same URL loaded as a classic script vs a module script are cached as
            // distinct entries (they produce different <script> tags and execution semantics).
            const cacheKey = kind === 'script' ? `${targetKey}\n${isModule ? 'module' : 'classic'}` : targetKey;

            const existingPromise = cache[cacheKey];
            if (existingPromise !== undefined) return existingPromise;

            // A tag we didn't add is host-provided. If the document has finished loading, verify the
            // resource actually applied/executed before treating it as ready. Otherwise the tag may still
            // be loading (e.g. a deferred/async CDN tag the host inserted), so await its load/error event
            // instead of assuming readiness from the mere presence of the tag. Waiting is gated on
            // document.readyState so we never block on a 'load' event that has already fired.
            // Host resources that failed to apply/load are skipped here so a working tag can be injected
            // instead; and if a host tag fails while awaited, it is marked data-bit-load-failed (so
            // findExistingResource skips it from then on) and a fresh tag is injected in its place.
            const existingTag = Extras.findExistingResource(kind, targetKey, isModule);
            const promise = existingTag
                ? Extras.awaitHostResource(existingTag, kind, url).catch(() => Extras.injectResource(kind, url, isModule))
                : Extras.injectResource(kind, url, isModule);

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