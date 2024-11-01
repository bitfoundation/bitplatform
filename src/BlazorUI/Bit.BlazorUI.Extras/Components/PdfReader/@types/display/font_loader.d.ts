export class FontFaceObject {
    constructor(translatedData: any, { disableFontFace, inspectFont }: {
        disableFontFace?: boolean | undefined;
        inspectFont?: null | undefined;
    });
    compiledGlyphs: any;
    disableFontFace: boolean;
    _inspectFont: any;
    createNativeFontFace(): FontFace | null;
    createFontFaceRule(): string | null;
    getPathGenerator(objs: any, character: any): any;
}
export class FontLoader {
    constructor({ ownerDocument, styleElement, }: {
        ownerDocument?: Document | undefined;
        styleElement?: null | undefined;
    });
    _document: Document;
    nativeFontFaces: Set<any>;
    styleElement: HTMLStyleElement | null;
    loadingRequests: any[] | undefined;
    loadTestFontId: number | undefined;
    addNativeFontFace(nativeFontFace: any): void;
    removeNativeFontFace(nativeFontFace: any): void;
    insertRule(rule: any): void;
    clear(): void;
    loadSystemFont({ systemFontInfo: info, _inspectFont }: {
        systemFontInfo: any;
        _inspectFont: any;
    }): Promise<void>;
    bind(font: any): Promise<void>;
    get isFontLoadingAPISupported(): any;
    get isSyncFontLoadingSupported(): any;
    _queueLoadingCallback(callback: any): {
        done: boolean;
        complete: () => void;
        callback: any;
    };
    get _loadTestFont(): any;
    _prepareFontLoadEvent(font: any, request: any): void;
    #private;
}
