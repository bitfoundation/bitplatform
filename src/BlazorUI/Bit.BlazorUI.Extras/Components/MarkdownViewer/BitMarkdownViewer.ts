namespace BitBlazorUI {
    export class MarkdownViewer {
        public static checkScriptLoaded(script: string) {
            return window.marked !== undefined;
        }

        public static parse(md: string, middlewares?: string[]) {
            let html = marked.parse(md, { async: false });

            if (middlewares) {
                html = MarkdownViewer.applyMiddlewares(html, middlewares);
            }

            return html;
        }

        public static async parseAsync(md: string, middlewares?: string[]) {
            let html = await marked.parse(md, { async: true });

            if (middlewares) {
                html = MarkdownViewer.applyMiddlewares(html, middlewares);
            }

            return html;
        }



        private static applyMiddlewares(html: string, middlewares: string[]) {
            for (let i = 0; i < middlewares.length; i++) {
                let m = middlewares[i];

                if (!m) continue;

                try {
                    html = Extras.invokeJS(m, html);
                } catch (e) {
                    console.error(e);
                }
            }

            return html;
        }
    }
}