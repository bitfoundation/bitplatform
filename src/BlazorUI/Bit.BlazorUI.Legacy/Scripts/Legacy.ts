namespace BitBlazorUI.Legacy {
    export class Utils {
        private static _initScriptsPromises: { [key: string]: Promise<unknown> } = {};
        public static async initScripts(scripts: string[], isModule: boolean) {
            const key = scripts.join('|');
            if (Utils._initScriptsPromises[key] !== undefined) {
                return Utils._initScriptsPromises[key];
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

            Utils._initScriptsPromises[key] = promise;
            return promise;

            async function addScript(url: string) {
                return Utils.loadWithCorsFallback(url, crossOrigin => new Promise((res, rej) => {
                    const script = document.createElement('script');
                    script.src = url;
                    if (isModule) {
                        script.type = 'module';
                    }
                    if (crossOrigin) {
                        script.crossOrigin = 'anonymous';
                    }
                    script.onload = () => res(script);
                    script.onerror = () => { script.remove(); rej(new Error(`Failed to load script: ${url}`)); };
                    document.body.appendChild(script);
                }));
            }
        }

        private static _initStylesheetsPromises: { [key: string]: Promise<unknown> } = {};
        public static async initStylesheets(stylesheets: string[]) {
            const key = stylesheets.join('|');
            if (Utils._initStylesheetsPromises[key] !== undefined) {
                return Utils._initStylesheetsPromises[key];
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

            Utils._initStylesheetsPromises[key] = promise;
            return promise;

            async function addStylesheet(url: string) {
                return Utils.loadWithCorsFallback(url, crossOrigin => new Promise((res, rej) => {
                    const link = document.createElement('link');
                    link.href = url;
                    link.rel = 'stylesheet';
                    if (crossOrigin) {
                        link.crossOrigin = 'anonymous';
                    }
                    link.onload = () => res(link);
                    link.onerror = () => { link.remove(); rej(new Error(`Failed to load stylesheet: ${url}`)); };
                    document.head.appendChild(link);
                }));
            }
        }

        /**
         * Loads a resource in no-cors mode first and, when that fails for a cross-origin URL,
         * retries once in CORS mode (crossorigin="anonymous").
         *
         * When the page is cross-origin isolated with Cross-Origin-Embedder-Policy: require-corp
         * (the only COEP value Safari supports, needed for the multi-threaded WebAssembly runtime),
         * a cross-origin script/stylesheet is blocked unless its response carries a
         * Cross-Origin-Resource-Policy header or it is requested in CORS mode. Most CDNs send
         * Access-Control-Allow-Origin: * but not CORP, so the CORS retry makes them load. Under
         * COEP: credentialless (Chromium/Firefox) the first attempt succeeds and no retry happens,
         * so hosts without CORS headers keep working there.
         */
        private static async loadWithCorsFallback<T>(url: string, load: (crossOrigin: boolean) => Promise<T>): Promise<T> {
            try {
                return await load(false);
            } catch (e) {
                if (!Utils.isCrossOrigin(url)) throw e;
                return await load(true);
            }
        }

        private static isCrossOrigin(url: string): boolean {
            try {
                return new URL(url, document.baseURI).origin !== window.location.origin;
            } catch {
                return false;
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
