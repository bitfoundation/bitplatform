declare function docProperties(pdfDocument: any): Promise<any>;
declare class GenericScripting {
    constructor(sandboxBundleSrc: any);
    _ready: Promise<any>;
    createSandbox(data: any): Promise<void>;
    dispatchEventInSandbox(event: any): Promise<void>;
    destroySandbox(): Promise<void>;
}
