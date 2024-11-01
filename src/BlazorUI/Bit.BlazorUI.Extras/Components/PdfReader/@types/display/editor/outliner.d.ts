export class FreeOutliner {
    static "__#22@#MIN_DIST": number;
    static "__#22@#MIN_DIFF": number;
    static "__#22@#MIN": number;
    constructor({ x, y }: {
        x: any;
        y: any;
    }, box: any, scaleFactor: any, thickness: any, isLTR: any, innerMargin?: number);
    get free(): boolean;
    isEmpty(): boolean;
    add({ x, y }: {
        x: any;
        y: any;
    }): boolean;
    toSVGPath(): string;
    getOutlines(): FreeHighlightOutline;
    #private;
}
export class Outliner {
    /**
     * Construct an outliner.
     * @param {Array<Object>} boxes - An array of axis-aligned rectangles.
     * @param {number} borderWidth - The width of the border of the boxes, it
     *   allows to make the boxes bigger (or smaller).
     * @param {number} innerMargin - The margin between the boxes and the
     *   outlines. It's important to not have a null innerMargin when we want to
     *   draw the outline else the stroked outline could be clipped because of its
     *   width.
     * @param {boolean} isLTR - true if we're in LTR mode. It's used to determine
     *   the last point of the boxes.
     */
    constructor(boxes: Array<Object>, borderWidth?: number, innerMargin?: number, isLTR?: boolean);
    getOutlines(): HighlightOutline;
    #private;
}
declare class FreeHighlightOutline extends Outline {
    constructor(outline: any, points: any, box: any, scaleFactor: any, innerMargin: any, isLTR: any);
    serialize([blX, blY, trX, trY]: [any, any, any, any], rotation: any): {
        outline: number[];
        points: number[][];
    };
    get box(): null;
    getNewOutline(thickness: any, innerMargin: any): FreeHighlightOutline;
    #private;
}
declare class HighlightOutline extends Outline {
    constructor(outlines: any, box: any);
    /**
     * Serialize the outlines into the PDF page coordinate system.
     * @param {Array<number>} _bbox - the bounding box of the annotation.
     * @param {number} _rotation - the rotation of the annotation.
     * @returns {Array<Array<number>>}
     */
    serialize([blX, blY, trX, trY]: Array<number>, _rotation: number): Array<Array<number>>;
    get box(): any;
    #private;
}
declare class Outline {
    /**
     * @returns {string} The SVG path of the outline.
     */
    toSVGPath(): string;
    /**
     * @type {Object|null} The bounding box of the outline.
     */
    get box(): Object | null;
    serialize(_bbox: any, _rotation: any): void;
    get free(): any;
}
export {};
