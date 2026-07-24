interface ThemeOptions {
    modules: Record<string, unknown> & {
        toolbar?: null | ToolbarProps;
    };
}

declare class Theme {
    protected quill: Quill;
    protected options: ThemeOptions;
    static DEFAULTS: ThemeOptions;
    static themes: {
        default: typeof Theme;
    };
    modules: ThemeOptions['modules'];
    constructor(quill: Quill, options: ThemeOptions);
    init(): void;
    addModule(name: 'clipboard'): QuillClipboard;
    addModule(name: 'keyboard'): Keyboard;
    addModule(name: 'uploader'): Uploader;
    addModule(name: 'history'): QuillHistory;
    addModule(name: string): unknown;
}

interface ThemeConstructor {
    new (quill: Quill, options: unknown): Theme;
    DEFAULTS: ThemeOptions;
}
