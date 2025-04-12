interface HistoryOptions {
    userOnly: boolean;
    delay: number;
    maxStack: number;
}
interface StackItem {
    delta: Delta;
    range: QuillRange | null;
}
interface Stack {
    undo: StackItem[];
    redo: StackItem[];
}
declare class QuillHistory extends Module<HistoryOptions> {
    static DEFAULTS: HistoryOptions;
    lastRecorded: number;
    ignoreChange: boolean;
    stack: Stack;
    currentRange: QuillRange | null;
    constructor(quill: Quill, options: Partial<HistoryOptions>);
    change(source: 'undo' | 'redo', dest: 'redo' | 'undo'): void;
    clear(): void;
    cutoff(): void;
    record(changeDelta: Delta, oldDelta: Delta): void;
    redo(): void;
    transform(delta: Delta): void;
    undo(): void;
    protected restoreSelection(stackItem: StackItem): void;
}
declare function getLastChangeIndex(scroll: Scroll, delta: Delta): number;
