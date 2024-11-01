export class BaseCanvasFactory {
    constructor({ enableHWA }: {
        enableHWA?: boolean | undefined;
    });
    create(width: any, height: any): {
        canvas: void;
        context: any;
    };
    reset(canvasAndContext: any, width: any, height: any): void;
    destroy(canvasAndContext: any): void;
    /**
     * @ignore
     */
    _createCanvas(width: any, height: any): void;
    #private;
}
export class BaseCMapReaderFactory {
    constructor({ baseUrl, isCompressed }: {
        baseUrl?: null | undefined;
        isCompressed?: boolean | undefined;
    });
    baseUrl: any;
    isCompressed: boolean;
    fetch({ name }: {
        name: any;
    }): Promise<any>;
    /**
     * @ignore
     */
    _fetchData(url: any, compressionType: any): void;
}
export class BaseFilterFactory {
    addFilter(maps: any): string;
    addHCMFilter(fgColor: any, bgColor: any): string;
    addAlphaFilter(map: any): string;
    addLuminosityFilter(map: any): string;
    addHighlightHCMFilter(filterName: any, fgColor: any, bgColor: any, newFgColor: any, newBgColor: any): string;
    destroy(keepHCM?: boolean): void;
}
export class BaseStandardFontDataFactory {
    constructor({ baseUrl }: {
        baseUrl?: null | undefined;
    });
    baseUrl: any;
    fetch({ filename }: {
        filename: any;
    }): Promise<any>;
    /**
     * @ignore
     */
    _fetchData(url: any): void;
}
export class BaseSVGFactory {
    create(width: any, height: any, skipDimensions?: boolean): void;
    createElement(type: any): void;
    /**
     * @ignore
     */
    _createSVG(type: any): void;
}
