declare abstract class Module<T extends {} = {}> {
    quill: Quill;
    protected options: Partial<T>;
    static DEFAULTS: {};
    constructor(quill: Quill, options?: Partial<T>);
}
