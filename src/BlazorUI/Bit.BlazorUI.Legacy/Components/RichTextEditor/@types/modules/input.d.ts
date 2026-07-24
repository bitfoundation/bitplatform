declare class Input extends Module {
    constructor(quill: Quill, options: Record<string, never>);
    private deleteRange;
    private replaceText;
    private handleBeforeInput;
    private handleCompositionStart;
}
