/**
 * Basic draw editor in order to generate an Highlight annotation.
 */
export class HighlightEditor extends AnnotationEditor {
    static _defaultColor: null;
    static _defaultOpacity: number;
    static _defaultThickness: number;
    static _type: string;
    static _editorType: number;
    static _freeHighlightId: number;
    static _freeHighlight: null;
    static _freeHighlightClipId: string;
    static get _keyboardManager(): any;
    static computeTelemetryFinalData(data: any): {
        numberOfColors: any;
    };
    /** @inheritdoc */
    static initialize(l10n: any, uiManager: any): void;
    /** @inheritdoc */
    static updateDefaultParams(type: any, value: any): void;
    static get defaultPropertiesToUpdate(): (number | null)[][];
    static "__#25@#rotateBbox"({ x, y, width, height }: {
        x: any;
        y: any;
        width: any;
        height: any;
    }, angle: any): {
        x: any;
        y: any;
        width: any;
        height: any;
    };
    static startHighlighting(parent: any, isLTR: any, { target: textLayer, x, y }: {
        target: any;
        x: any;
        y: any;
    }): void;
    static "__#25@#highlightMove"(parent: any, event: any): void;
    static "__#25@#endHighlight"(parent: any, event: any): void;
    /** @inheritdoc */
    static deserialize(data: any, parent: any, uiManager: any): Promise<AnnotationEditor | null>;
    constructor(params: any);
    color: any;
    /** @inheritdoc */
    get telemetryInitialData(): {
        action: string;
        type: string;
        color: any;
        thickness: any;
        methodOfCreation: string;
    };
    /** @inheritdoc */
    get telemetryFinalData(): {
        type: string;
        color: any;
    };
    /** @inheritdoc */
    translateInPage(x: any, y: any): void;
    /** @inheritdoc */
    updateParams(type: any, value: any): void;
    /** @inheritdoc */
    get propertiesToUpdate(): any[][];
    /** @inheritdoc */
    fixAndSetPosition(): void;
    /** @inheritdoc */
    getRect(tx: any, ty: any): any[];
    /** @inheritdoc */
    rotate(angle: any): void;
    pointerover(): void;
    pointerleave(): void;
    _moveCaret(direction: any): void;
    /** @inheritdoc */
    serialize(isForCopying?: boolean): Object | null;
    /** @inheritdoc */
    renderAnnotationElement(annotation: any): null;
    #private;
}
import { AnnotationEditor } from "./editor.js";
