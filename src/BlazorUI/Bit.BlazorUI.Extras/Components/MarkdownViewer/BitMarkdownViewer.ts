namespace BitBlazorUI {
    export class MarkdownViewer {
        private static _initPromise?: Promise<unknown>;

        public static checkScript(script: string) {
            const allScripts = Array.from(document.scripts).map(s => s.src);
            return !!allScripts.find(as => as.includes(script));
        }

        public static parse(md: string) {
            const html = marked.parse(md, { async: false });
            return html;
        }

        public static async parseAsync(md: string) {
            const html = await marked.parse(md, { async: true });
            return html;
        }
    }
}