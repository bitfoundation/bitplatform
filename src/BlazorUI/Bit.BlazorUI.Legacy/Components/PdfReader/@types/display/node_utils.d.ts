declare class NodeCanvasFactory extends BaseCanvasFactory {
    /**
     * @ignore
     */
    _createCanvas(width: any, height: any): any;
}
declare class NodeCMapReaderFactory extends BaseCMapReaderFactory {
    /**
     * @ignore
     */
    _fetchData(url: any, compressionType: any): any;
}
declare class NodeFilterFactory extends BaseFilterFactory {
}
declare class NodePackages {
    static get promise(): any;
    static get(name: any): any;
}
declare class NodeStandardFontDataFactory extends BaseStandardFontDataFactory {
    /**
     * @ignore
     */
    _fetchData(url: any): any;
}
