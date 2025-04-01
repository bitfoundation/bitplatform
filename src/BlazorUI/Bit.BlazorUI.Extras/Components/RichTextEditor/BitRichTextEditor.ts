namespace BitBlazorUI {

    export class RichTextEditor {
        private static _editors: { [key: string]: QuillEditor } = {};

        public static setup(
            id: string,
            dotnetObj: DotNetObject,
            editorContainer: HTMLElement,
            toolbarContainer: HTMLElement,
            theme: string) {
            const quill = new Quill(editorContainer, {
                modules: {
                    toolbar: toolbarContainer
                },
                theme: theme
            });

            const editor: QuillEditor = { id, dotnetObj, quill };

            RichTextEditor._editors[id] = editor;
        }

        public static getValue(id: string) {
            const editor = RichTextEditor._editors[id];
            if (!editor) return;

            //return editor.value;
        }

        public static dispose(id: string) {
            if (!RichTextEditor._editors[id]) return;

            //RichTextEditor._editors[id].dispose();

            delete RichTextEditor._editors[id];
        }
    }

    interface QuillEditor {
        id: string;
        quill: Quill;
        dotnetObj: DotNetObject;
    }
}