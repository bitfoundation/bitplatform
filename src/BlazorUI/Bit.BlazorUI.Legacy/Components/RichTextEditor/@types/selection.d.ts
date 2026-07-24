type NativeRange = AbstractRange;
interface NormalizedRange {
    start: {
        node: NativeRange['startContainer'];
        offset: NativeRange['startOffset'];
    };
    end: {
        node: NativeRange['endContainer'];
        offset: NativeRange['endOffset'];
    };
    native: NativeRange;
}
interface Bounds {
    bottom: number;
    height: number;
    left: number;
    right: number;
    top: number;
    width: number;
}
declare class QuillRange {
    index: number;
    length: number;
    constructor(index: number, length?: number);
}
declare class QuillSelection {
    scroll: Scroll;
    emitter: Emitter;
    composing: boolean;
    mouseDown: boolean;
    root: HTMLElement;
    cursor: Cursor;
    savedRange: QuillRange;
    lastRange: QuillRange | null;
    lastNative: NormalizedRange | null;
    constructor(scroll: Scroll, emitter: Emitter);
    handleComposition(): void;
    handleDragging(): void;
    focus(): void;
    format(format: string, value: unknown): void;
    getBounds(index: number, length?: number): DOMRect | {
        bottom: number;
        height: number;
        left: number;
        right: number;
        top: number;
        width: number;
    } | null;
    getNativeRange(): NormalizedRange | null;
    getRange(): [QuillRange, NormalizedRange] | [null, null];
    hasFocus(): boolean;
    normalizedToRange(range: NormalizedRange): QuillRange;
    normalizeNative(nativeRange: NativeRange): {
        start: {
            node: Node;
            offset: number;
        };
        end: {
            node: Node;
            offset: number;
        };
        native: AbstractRange;
    } | null;
    rangeToNative(range: QuillRange): [Node | null, number, Node | null, number];
    setNativeRange(startNode: Node | null, startOffset?: number, endNode?: Node | null, endOffset?: number | undefined, force?: boolean): void;
    setRange(range: QuillRange | null, force: boolean, source?: EmitterSource): void;
    setRange(range: QuillRange | null, source?: EmitterSource): void;
    update(source?: EmitterSource): void;
}
