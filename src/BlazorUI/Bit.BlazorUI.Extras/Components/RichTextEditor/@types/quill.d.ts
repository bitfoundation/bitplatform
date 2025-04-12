declare const globalRegistry: Registry;
/**
 * Options for initializing a Quill instance
 */
interface QuillOptions {
    theme?: string;
    debug?: DebugLevel | boolean;
    registry?: Registry;
    /**
     * Whether to disable the editing
     * @default false
     */
    readOnly?: boolean;
    /**
     * Placeholder text to display when the editor is empty
     * @default ""
     */
    placeholder?: string;
    bounds?: HTMLElement | string | null;
    modules?: Record<string, unknown>;
    /**
     * A list of formats that are recognized and can exist within the editor contents.
     * `null` means all formats are allowed.
     * @default null
     */
    formats?: string[] | null;
}
/**
 * Similar to QuillOptions, but with all properties expanded to their default values,
 * and all selectors resolved to HTMLElements.
 */
interface ExpandedQuillOptions extends Omit<QuillOptions, 'theme' | 'formats'> {
    theme: ThemeConstructor;
    registry: Registry;
    container: HTMLElement;
    modules: Record<string, unknown>;
    bounds?: HTMLElement | null;
    readOnly: boolean;
}

declare class Quill {
    static DEFAULTS: {
        bounds: null;
        modules: {
            clipboard: boolean;
            keyboard: boolean;
            history: boolean;
            uploader: boolean;
        };
        placeholder: string;
        readOnly: false;
        registry: Registry;
        theme: string;
    };
    static events: {
        readonly EDITOR_CHANGE: "editor-change";
        readonly SCROLL_BEFORE_UPDATE: "scroll-before-update";
        readonly SCROLL_BLOT_MOUNT: "scroll-blot-mount";
        readonly SCROLL_BLOT_UNMOUNT: "scroll-blot-unmount";
        readonly SCROLL_OPTIMIZE: "scroll-optimize";
        readonly SCROLL_UPDATE: "scroll-update";
        readonly SCROLL_EMBED_UPDATE: "scroll-embed-update";
        readonly SELECTION_CHANGE: "selection-change";
        readonly TEXT_CHANGE: "text-change";
        readonly COMPOSITION_BEFORE_START: "composition-before-start";
        readonly COMPOSITION_START: "composition-start";
        readonly COMPOSITION_BEFORE_END: "composition-before-end";
        readonly COMPOSITION_END: "composition-end";
    };
    static sources: {
        readonly API: "api";
        readonly SILENT: "silent";
        readonly USER: "user";
    };
    static version: string;
    static imports: Record<string, unknown>;
    static debug(limit: DebugLevel | boolean): void;
    static find(node: Node, bubble?: boolean): Blot | Quill | null;
    static import(name: 'core/module'): typeof Module;
    static import(name: `themes/${string}`): typeof Theme;
    static import(name: 'parchment'): any;
    static import(name: 'delta'): typeof Delta;
    static import(name: string): unknown;
    static register(targets: Record<string, RegistryDefinition | Record<string, unknown> | Theme | Module | Function>, overwrite?: boolean): void;
    static register(target: RegistryDefinition, overwrite?: boolean): void;
    static register(path: string, target: any, overwrite?: boolean): void;
    container: HTMLElement;
    root: HTMLDivElement;
    scroll: Scroll;
    emitter: Emitter;
    protected allowReadOnlyEdits: boolean;
    editor: Editor;
    composition: Composition;
    selection: QuillSelection;
    theme: Theme;
    keyboard: Keyboard;
    clipboard: QuillClipboard;
    history: QuillHistory;
    uploader: Uploader;
    options: ExpandedQuillOptions;
    constructor(container: HTMLElement | string, options?: QuillOptions);
    addContainer(container: string, refNode?: Node | null): HTMLDivElement;
    addContainer(container: HTMLElement, refNode?: Node | null): HTMLElement;
    blur(): void;
    deleteText(range: QuillRange, source?: EmitterSource): Delta;
    deleteText(index: number, length: number, source?: EmitterSource): Delta;
    disable(): void;
    editReadOnly<T>(modifier: () => T): T;
    enable(enabled?: boolean): void;
    focus(options?: {
        preventScroll?: boolean;
    }): void;
    format(name: string, value: unknown, source?: EmitterSource): Delta;
    formatLine(index: number, length: number, formats: Record<string, unknown>, source?: EmitterSource): Delta;
    formatLine(index: number, length: number, name: string, value?: unknown, source?: EmitterSource): Delta;
    formatText(range: QuillRange, name: string, value: unknown, source?: EmitterSource): Delta;
    formatText(index: number, length: number, name: string, value: unknown, source?: EmitterSource): Delta;
    formatText(index: number, length: number, formats: Record<string, unknown>, source?: EmitterSource): Delta;
    getBounds(index: number | QuillRange, length?: number): Bounds | null;
    getContents(index?: number, length?: number): Delta;
    getFormat(index?: number, length?: number): {
        [format: string]: unknown;
    };
    getFormat(range?: QuillRange): {
        [format: string]: unknown;
    };
    getIndex(blot: Blot): number;
    getLength(): number;
    getLeaf(index: number): [LeafBlot | null, number];
    getLine(index: number): [Block | BlockEmbed | null, number];
    getLines(range: QuillRange): (Block | BlockEmbed)[];
    getLines(index?: number, length?: number): (Block | BlockEmbed)[];
    getModule(name: string): unknown;
    getSelection(focus: true): QuillRange;
    getSelection(focus?: boolean): QuillRange | null;
    getSemanticHTML(range: QuillRange): string;
    getSemanticHTML(index?: number, length?: number): string;
    getText(range?: QuillRange): string;
    getText(index?: number, length?: number): string;
    hasFocus(): boolean;
    insertEmbed(index: number, embed: string, value: unknown, source?: EmitterSource): Delta;
    insertText(index: number, text: string, source?: EmitterSource): Delta;
    insertText(index: number, text: string, formats: Record<string, unknown>, source?: EmitterSource): Delta;
    insertText(index: number, text: string, name: string, value: unknown, source?: EmitterSource): Delta;
    isEnabled(): boolean;
    off(...args: Parameters<(typeof Emitter)['prototype']['off']>): Emitter;
    on(event: (typeof Emitter)['events']['TEXT_CHANGE'], handler: (delta: Delta, oldContent: Delta, source: EmitterSource) => void): Emitter;
    on(event: (typeof Emitter)['events']['SELECTION_CHANGE'], handler: (range: QuillRange, oldRange: QuillRange, source: EmitterSource) => void): Emitter;
    on(event: (typeof Emitter)['events']['EDITOR_CHANGE'], handler: (...args: [
        (typeof Emitter)['events']['TEXT_CHANGE'],
        Delta,
        Delta,
        EmitterSource
    ] | [
        (typeof Emitter)['events']['SELECTION_CHANGE'],
        QuillRange,
        QuillRange,
        EmitterSource
    ]) => void): Emitter;
    on(event: string, ...args: unknown[]): Emitter;
    once(...args: Parameters<(typeof Emitter)['prototype']['once']>): Emitter;
    removeFormat(index: number, length: number, source?: EmitterSource): Delta;
    scrollRectIntoView(rect: Rect): void;
    /**
     * @deprecated Use Quill#scrollSelectionIntoView() instead.
     */
    scrollIntoView(): void;
    /**
     * Scroll the current selection into the visible area.
     * If the selection is already visible, no scrolling will occur.
     */
    scrollSelectionIntoView(): void;
    setContents(delta: Delta | Op[], source?: EmitterSource): Delta;
    setSelection(range: QuillRange | null, source?: EmitterSource): void;
    setSelection(index: number, source?: EmitterSource): void;
    setSelection(index: number, length?: number, source?: EmitterSource): void;
    setSelection(index: number, source?: EmitterSource): void;
    setText(text: string, source?: EmitterSource): Delta;
    update(source?: EmitterSource): void;
    updateContents(delta: Delta | Op[], source?: EmitterSource): Delta;
}
declare function expandConfig(containerOrSelector: HTMLElement | string, options: QuillOptions): ExpandedQuillOptions;
type NormalizedIndexLength = [
    number,
    number,
    Record<string, unknown>,
    EmitterSource
];
declare function overload(index: number, source?: EmitterSource): NormalizedIndexLength;
declare function overload(index: number, length: number, source?: EmitterSource): NormalizedIndexLength;
declare function overload(index: number, length: number, format: string, value: unknown, source?: EmitterSource): NormalizedIndexLength;
declare function overload(index: number, length: number, format: Record<string, unknown>, source?: EmitterSource): NormalizedIndexLength;
declare function overload(range: QuillRange, source?: EmitterSource): NormalizedIndexLength;
declare function overload(range: QuillRange, format: string, value: unknown, source?: EmitterSource): NormalizedIndexLength;
declare function overload(range: QuillRange, format: Record<string, unknown>, source?: EmitterSource): NormalizedIndexLength;
