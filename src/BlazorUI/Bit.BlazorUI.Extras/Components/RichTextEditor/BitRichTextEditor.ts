namespace BitBlazorUI {

    export class RichTextEditor {
        private static _editors: { [key: string]: QuillEditor } = {};

        private static _toolbarFullOptions = [
            ['bold', 'italic', 'underline', 'strike'],
            ['blockquote', 'code-block', 'link'],
            ['image', 'video', 'formula'],
            [{ 'list': 'ordered' }, { 'list': 'bullet' }, { 'list': 'check' }],
            [{ 'script': 'sub' }, { 'script': 'super' }],
            [{ 'indent': '-1' }, { 'indent': '+1' }],
            [{ 'header': [1, 2, 3, 4, 5, 6, false] }],
            [{ 'color': [] }, { 'background': [] }],
            [{ 'font': [] }],
            [{ 'size': ['small', false, 'large', 'huge'] }],
            [{ 'align': [] }],
            [{ 'direction': 'rtl' }],
            ['clean']
        ];

        private static _toolbarMinOptions = [
            ['bold', 'italic', 'underline', 'strike'],
            ['blockquote', 'code-block'],
            [{ 'list': 'ordered' }, { 'list': 'bullet' }, { 'list': 'check' }],
            [{ 'script': 'sub' }, { 'script': 'super' }],
            [{ 'direction': 'rtl' }],
        ];

        public static setup(
            id: string,
            dotnetObj: DotNetObject,
            editorContainer: HTMLElement,
            toolbarContainer: HTMLElement | undefined,
            theme: string,
            placeholder: string,
            readOnly: boolean,
            fullToolbar: boolean,
            toolbarStyle: string,
            toolbarClass: string) {

            const quill = new Quill(editorContainer, {
                modules: {
                    toolbar: toolbarContainer || (fullToolbar ? RichTextEditor._toolbarFullOptions : RichTextEditor._toolbarMinOptions)
                },
                theme,
                placeholder,
                readOnly
            });

            if (!toolbarContainer && (toolbarStyle || toolbarClass)) {
                const toolbar = document.getElementById(id)?.querySelector('.ql-toolbar') as HTMLElement;
                toolbarStyle && toolbar?.setAttribute('style', toolbarStyle);
                toolbarClass && toolbar?.classList.add(toolbarClass);
            }

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

        public static setText(id: string, text: string) {
            const editor = RichTextEditor._editors[id];
            if (!editor) return;

            return editor.quill.setText(text);
        }

        public static setHtml(id: string, html: string) {
            const editor = RichTextEditor._editors[id];
            if (!editor) return;

            return editor.quill.root.innerHTML = html;
        }

        public static setContent(id: string, content: string) {
            const editor = RichTextEditor._editors[id];
            if (!editor) return;

            try {
                editor.quill.setContents(JSON.parse(content));
            } catch { }
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