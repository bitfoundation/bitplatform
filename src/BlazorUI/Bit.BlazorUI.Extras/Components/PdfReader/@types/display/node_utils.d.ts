export class NodeCanvasFactory extends BaseCanvasFactory {
    /**
     * @ignore
     */
    _createCanvas(width: any, height: any): any;
}
export class NodeCMapReaderFactory extends BaseCMapReaderFactory {
    /**
     * @ignore
     */
    _fetchData(url: any, compressionType: any): any;
}
export class NodeFilterFactory extends BaseFilterFactory {
}
export class NodePackages {
    static get promise(): any;
    static get(name: any): any;
}
export class NodeStandardFontDataFactory extends BaseStandardFontDataFactory {
    /**
     * @ignore
     */
    _fetchData(url: any): any;
}
import { BaseCanvasFactory } from "./base_factory.js";
import { BaseCMapReaderFactory } from "./base_factory.js";
import { BaseFilterFactory } from "./base_factory.js";
import { BaseStandardFontDataFactory } from "./base_factory.js";
