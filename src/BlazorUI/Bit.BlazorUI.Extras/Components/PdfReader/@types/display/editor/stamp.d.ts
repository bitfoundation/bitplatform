/**
 * Basic text editor in order to create a FreeTex annotation.
 */
export class StampEditor extends AnnotationEditor {
    static _type: string;
    static _editorType: number;
    /** @inheritdoc */
    static initialize(l10n: any, uiManager: any): void;
    static get supportedTypes(): any;
    static get supportedTypesStr(): any;
    /** @inheritdoc */
    static isHandlingMimeForPasting(mime: any): any;
    /** @inheritdoc */
    static paste(item: any, parent: any): void;
    static computeTelemetryFinalData(data: any): {
        hasAltText: any;
        hasNoAltText: any;
    };
    /** @inheritdoc */
    static deserialize(data: any, parent: any, uiManager: any): Promise<AnnotationEditor | null>;
    constructor(params: any);
    /** @inheritdoc */
    get telemetryFinalData(): {
        type: string;
        hasAltText: boolean;
    };
    mlGuessAltText(imageData?: null, updateAltTextData?: boolean): Promise<any>;
    copyCanvas(maxDataDimension: any, maxPreviewDimension: any, createImageData?: boolean): {
        canvas: HTMLCanvasElement | null;
        width: any;
        height: any;
        imageData: {
            width: any;
            height: any;
            data: Uint8ClampedArray;
        } | null;
    };
    /** @inheritdoc */
    getImageForAltText(): null;
    /** @inheritdoc */
    serialize(isForCopying?: boolean, context?: null): Object | null;
    /** @inheritdoc */
    renderAnnotationElement(annotation: any): null;
    #private;
}
import { AnnotationEditor } from "./editor.js";
