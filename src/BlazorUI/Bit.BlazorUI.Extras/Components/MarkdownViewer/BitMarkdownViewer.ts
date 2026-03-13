namespace BitBlazorUI {
    export class MarkdownViewer {
        public static checkScriptLoaded(script: string) {
            return window.marked !== undefined;
        }

        public static parse(md: string) {
            let html = marked.parse(md, { async: false });

            return html;
        }

        public static async parseAsync(md: string, middleware?: string) {
            let html = await marked.parse(md, { async: true });

            if (middleware) {
                try {
                    html = await Extras.invokeJs(middleware, html);
                } catch (err) {
                    console.error(err);
                }
            }

            return html;
        }
    }
}