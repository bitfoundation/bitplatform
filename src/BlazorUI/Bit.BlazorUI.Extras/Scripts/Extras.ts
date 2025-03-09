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

        private static _initScriptPromises: { [key: string]: Promise<unknown> } = {};
        public static async initScript(scripts: string[], isModule: boolean) {
            const key = scripts.join('|');
            if (Extras._initScriptPromises[key] !== undefined) {
                return await Extras._initScriptPromises[key];
            }

            const allScripts = Array.from(document.scripts).map(s => s.src);
            const notAddedScripts = scripts.filter(s => !allScripts.find(as => as.endsWith(s)));

            if (notAddedScripts.length == 0) return Promise.resolve();

            const promise = new Promise((resolve: any, reject: any) => {
                try {
                    (async function loadScripts() {
                        try {
                            await Promise.all(notAddedScripts.map(addScript));
                            resolve();
                        } catch (e: any) {
                            reject(e);
                        }
                    }());
                } catch (e: any) {
                    reject(e);
                }
            });
            Extras._initScriptPromises[key] = promise;
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
    }
}