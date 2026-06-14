namespace BitBlazorUI {
    export class MarkdownViewer {
        public static checkScriptLoaded(script: string) {
            return window.marked !== undefined;
        }

        public static parse(md: string) {
            // The `async: false` option MUST remain. This method is FastInvoked (FastInvoke<string>)
            // from the .NET side, which requires a synchronous string return. marked.parse returns a
            // Promise when async is true, which would silently turn the FastInvoke call into a
            // fire-and-forget with no test catching the regression. Use parseAsync for async needs.
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