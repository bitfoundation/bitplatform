namespace BitBlazorUI {
    export class MarkdownViewer {
        public static checkScriptLoaded(script: string) {
            return window.marked !== undefined;
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