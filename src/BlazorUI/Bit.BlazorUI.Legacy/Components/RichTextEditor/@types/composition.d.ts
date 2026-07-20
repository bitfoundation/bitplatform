declare class Composition {
    private scroll;
    private emitter;
    isComposing: boolean;
    constructor(scroll: Scroll, emitter: Emitter);
    private setupListeners;
    private handleCompositionStart;
    private handleCompositionEnd;
}
