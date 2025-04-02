namespace BitBlazorUI {

    export class RichTextEditor {
        private static _editors: { [key: string]: QuillEditor } = {};

        public static setup(
            id: string,
            dotnetObj: DotNetObject,
            editorContainer: HTMLElement,
            toolbarContainer: HTMLElement,
            theme: string) {
            const toolbarOptions = [
                ['bold', 'italic', 'underline', 'strike'],        // toggled buttons
                ['blockquote', 'code-block'],
                ['link', 'image', 'video', 'formula'],

                [{ 'header': 1 }, { 'header': 2 }],               // custom button values
                [{ 'list': 'ordered' }, { 'list': 'bullet' }, { 'list': 'check' }],
                [{ 'script': 'sub' }, { 'script': 'super' }],      // superscript/subscript
                [{ 'indent': '-1' }, { 'indent': '+1' }],          // outdent/indent
                [{ 'direction': 'rtl' }],                         // text direction

                [{ 'size': ['small', false, 'large', 'huge'] }],  // custom dropdown
                [{ 'header': [1, 2, 3, 4, 5, 6, false] }],

                [{ 'color': [] }, { 'background': [] }],          // dropdown with defaults from theme
                [{ 'font': [] }],
                [{ 'align': [] }],

                ['clean']                                         // remove formatting button
            ];
            const quill = new Quill(editorContainer, {
                modules: {
                    //toolbar: toolbarContainer
                    toolbar: toolbarOptions
                },
                theme: theme
            });

            const editor: QuillEditor = { id, dotnetObj, quill };

            RichTextEditor._editors[id] = editor;
        }

        public static getText(id: string) {
            const editor = RichTextEditor._editors[id];
            if (!editor) return;

            return editor.quill.getText();
        }

        public static getHtml(id: string) {
            const editor = RichTextEditor._editors[id];
            if (!editor) return;

            return editor.quill.root.innerHTML;
        }

        public static getContent(id: string) {
            const editor = RichTextEditor._editors[id];
            if (!editor) return;

            return JSON.stringify(editor.quill.getContents());
        }

        public static dispose(id: string) {
            if (!RichTextEditor._editors[id]) return;

            delete RichTextEditor._editors[id];
        }
    }

    interface QuillEditor {
        id: string;
        quill: Quill;
        dotnetObj: DotNetObject;
    }
}