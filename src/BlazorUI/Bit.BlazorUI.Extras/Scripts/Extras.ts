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
    }
}