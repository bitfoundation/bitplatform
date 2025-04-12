declare const SHORTKEY: string;

interface Context {
    collapsed: boolean;
    empty: boolean;
    offset: number;
    prefix: string;
    suffix: string;
    format: Record<string, unknown>;
    event: KeyboardEvent;
    line: BlockEmbed | BlockBlot;
}

interface BindingObject extends Partial<Omit<Context, 'prefix' | 'suffix' | 'format'>> {
    key: number | string | string[];
    shortKey?: boolean | null;
    shiftKey?: boolean | null;
    altKey?: boolean | null;
    metaKey?: boolean | null;
    ctrlKey?: boolean | null;
    prefix?: RegExp;
    suffix?: RegExp;
    format?: Record<string, unknown> | string[];
    handler?: (this: {
        quill: Quill;
    }, range: QuillRange, curContext: Context, binding: NormalizedBinding) => boolean | void;
}

type Binding = BindingObject | string | number;

interface NormalizedBinding extends Omit<BindingObject, 'key' | 'shortKey'> {
    key: string | number;
}

interface KeyboardOptions {
    bindings: Record<string, Binding>;
}

declare class Keyboard extends Module<KeyboardOptions> {
    static DEFAULTS: KeyboardOptions;
    static match(evt: KeyboardEvent, binding: BindingObject): boolean;
    bindings: Record<string, NormalizedBinding[]>;
    constructor(quill: Quill, options: Partial<KeyboardOptions>);
    addBinding(keyBinding: Binding, context?: Required<BindingObject['handler']> | Partial<Omit<BindingObject, 'key' | 'handler'>>, handler?: Required<BindingObject['handler']> | Partial<Omit<BindingObject, 'key' | 'handler'>>): void;
    listen(): void;
    handleBackspace(range: QuillRange, context: Context): void;
    handleDelete(range: QuillRange, context: Context): void;
    handleDeleteRange(range: QuillRange): void;
    handleEnter(range: QuillRange, context: Context): void;
}

declare function normalize(binding: Binding): BindingObject | null;

declare function deleteRange({ quill, range }: {
    quill: Quill;
    range: QuillRange;
}): void;
