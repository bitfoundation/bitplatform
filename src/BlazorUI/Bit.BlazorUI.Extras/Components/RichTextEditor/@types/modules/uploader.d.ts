interface UploaderOptions {
    mimetypes: string[];
    handler: (this: {
        quill: Quill;
    }, range: QuillRange, files: File[]) => void;
}

declare class Uploader extends Module<UploaderOptions> {
    static DEFAULTS: UploaderOptions;
    constructor(quill: Quill, options: Partial<UploaderOptions>);
    upload(range: QuillRange, files: FileList | File[]): void;
}
