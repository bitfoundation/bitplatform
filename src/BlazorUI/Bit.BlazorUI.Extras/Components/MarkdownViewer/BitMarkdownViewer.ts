namespace BitBlazorUI {
    export class MarkdownViewer {
        private static _initPromise?: Promise<unknown>;

        public static async init(scripts: string[]) {
            if (MarkdownViewer._initPromise) {
                await MarkdownViewer._initPromise;
            }

            const allScripts = Array.from(document.scripts).map(s => s.src);
            const notAppenedScripts = scripts.filter(s => !allScripts.find(as => as.endsWith(s)));

            if (notAppenedScripts.length == 0) return Promise.resolve();

            const promise = new Promise(async (resolve: any, reject: any) => {
                try {
                    for (let url of notAppenedScripts) await addScript(url);
                    resolve();
                } catch (e: any) {
                    reject(e);
                }
            });
            MarkdownViewer._initPromise = promise;
            return promise;

            async function addScript(url: string) {
                return new Promise((res, rej) => {
                    const script = document.createElement('script');
                    script.src = url;
                    script.onload = res;
                    script.onerror = rej;
                    document.body.appendChild(script);
                })
            }
        }

        public static async parse(md: string) {
            const html = await marked.parse(md, { async: true });
            return html;
        }
    }
}