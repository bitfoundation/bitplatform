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
                // non-executable tags (e.g. application/json) can never satisfy the lookup. Also exclude
                // nomodule scripts: they don't execute in module-capable browsers, so reusing one would
                // falsely report the resource as loaded.
                const wantModule = !!isModule;
                return Array.from(document.scripts).find(s => !!s.src
                    && Extras.normalizeResourceUrl(s.src) === targetUrl
                    && (wantModule ? (s.type ?? '').trim().toLowerCase() === 'module' : (Extras.isExecutableClassicScriptType(s.type) && !s.noModule))
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