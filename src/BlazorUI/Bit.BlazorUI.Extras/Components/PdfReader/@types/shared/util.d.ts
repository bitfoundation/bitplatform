declare const AbortException_base: any;
/**
 * Error used to indicate task cancellation.
 */
declare class AbortException extends AbortException_base {
    [x: string]: any;
    constructor(msg: any);
}
declare namespace AnnotationActionEventType {
    let E: string;
    let X: string;
    let D: string;
    let U: string;
    let Fo: string;
    let Bl: string;
    let PO: string;
    let PC: string;
    let PV: string;
    let PI: string;
    let K: string;
    let F: string;
    let V: string;
    let C: string;
}
declare namespace AnnotationBorderStyleType {
    let SOLID: number;
    let DASHED: number;
    let BEVELED: number;
    let INSET: number;
    let UNDERLINE: number;
}
declare namespace AnnotationEditorParamsType {
    let RESIZE: number;
    let CREATE: number;
    let FREETEXT_SIZE: number;
    let FREETEXT_COLOR: number;
    let FREETEXT_OPACITY: number;
    let INK_COLOR: number;
    let INK_THICKNESS: number;
    let INK_OPACITY: number;
    let HIGHLIGHT_COLOR: number;
    let HIGHLIGHT_DEFAULT_COLOR: number;
    let HIGHLIGHT_THICKNESS: number;
    let HIGHLIGHT_FREE: number;
    let HIGHLIGHT_SHOW_ALL: number;
}
declare const AnnotationEditorPrefix: "pdfjs_internal_editor_";
declare namespace AnnotationEditorType {
    let DISABLE: number;
    let NONE: number;
    let FREETEXT: number;
    let HIGHLIGHT: number;
    let STAMP: number;
    let INK: number;
}
declare namespace AnnotationFieldFlag {
    let READONLY: number;
    let REQUIRED: number;
    let NOEXPORT: number;
    let MULTILINE: number;
    let PASSWORD: number;
    let NOTOGGLETOOFF: number;
    let RADIO: number;
    let PUSHBUTTON: number;
    let COMBO: number;
    let EDIT: number;
    let SORT: number;
    let FILESELECT: number;
    let MULTISELECT: number;
    let DONOTSPELLCHECK: number;
    let DONOTSCROLL: number;
    let COMB: number;
    let RICHTEXT: number;
    let RADIOSINUNISON: number;
    let COMMITONSELCHANGE: number;
}
declare namespace AnnotationFlag {
    let INVISIBLE: number;
    let HIDDEN: number;
    let PRINT: number;
    let NOZOOM: number;
    let NOROTATE: number;
    let NOVIEW: number;
    let READONLY_1: number;
    //export { READONLY_1 as READONLY };
    let LOCKED: number;
    let TOGGLENOVIEW: number;
    let LOCKEDCONTENTS: number;
}
declare namespace AnnotationMode {
    let DISABLE_1: number;
    //export { DISABLE_1 as DISABLE };
    let ENABLE: number;
    let ENABLE_FORMS: number;
    let ENABLE_STORAGE: number;
}
declare const AnnotationPrefix: "pdfjs_internal_id_";
declare namespace AnnotationReplyType {
    let GROUP: string;
    let REPLY: string;
}
declare namespace AnnotationType {
    let TEXT: number;
    let LINK: number;
    let FREETEXT_1: number;
    //export { FREETEXT_1 as FREETEXT };
    let LINE: number;
    let SQUARE: number;
    let CIRCLE: number;
    let POLYGON: number;
    let POLYLINE: number;
    let HIGHLIGHT_1: number;
    //export { HIGHLIGHT_1 as HIGHLIGHT };
    let UNDERLINE_1: number;
    //export { UNDERLINE_1 as UNDERLINE };
    let SQUIGGLY: number;
    let STRIKEOUT: number;
    let STAMP_1: number;
    //export { STAMP_1 as STAMP };
    let CARET: number;
    let INK_1: number;
    //export { INK_1 as INK };
    let POPUP: number;
    let FILEATTACHMENT: number;
    let SOUND: number;
    let MOVIE: number;
    let WIDGET: number;
    let SCREEN: number;
    let PRINTERMARK: number;
    let TRAPNET: number;
    let WATERMARK: number;
    let THREED: number;
    let REDACT: number;
}
declare function assert(cond: any, msg: any): void;
/**
 * @type {any}
 */
declare const BaseException: any;
declare const BASELINE_FACTOR: number;
declare function bytesToString(bytes: any): string;
declare namespace CMapCompressionType {
    let NONE_1: number;
    //export { NONE_1 as NONE };
    let BINARY: number;
}
/**
 * Attempts to create a valid absolute URL.
 *
 * @param {URL|string} url - An absolute, or relative, URL.
 * @param {URL|string} [baseUrl] - An absolute URL.
 * @param {Object} [options]
 * @returns Either a valid {URL}, or `null` otherwise.
 */
declare function createValidAbsoluteUrl(url: URL | string, baseUrl?: string | URL | undefined, options?: Object | undefined): URL | null;
declare namespace DocumentActionEventType {
    let WC: string;
    let WS: string;
    let DS: string;
    let WP: string;
    let DP: string;
}
declare class FeatureTest {
    static get isLittleEndian(): any;
    static get isEvalSupported(): any;
    static get isOffscreenCanvasSupported(): any;
    static get platform(): any;
    static get isCSSRoundSupported(): any;
}
declare const FONT_IDENTITY_MATRIX: number[];
declare namespace FontRenderOps {
    let BEZIER_CURVE_TO: number;
    let MOVE_TO: number;
    let LINE_TO: number;
    let QUADRATIC_CURVE_TO: number;
    let RESTORE: number;
    let SAVE: number;
    let SCALE: number;
    let TRANSFORM: number;
    let TRANSLATE: number;
}
declare const FormatError_base: any;
/**
 * Error caused during parsing PDF data.
 */
declare class FormatError extends FormatError_base {
    [x: string]: any;
    constructor(msg: any);
}
declare function getModificationDate(date?: Date): string;
declare function getUuid(): string;
declare function getVerbosityLevel(): number;
declare const IDENTITY_MATRIX: number[];
declare namespace ImageKind {
    let GRAYSCALE_1BPP: number;
    let RGB_24BPP: number;
    let RGBA_32BPP: number;
}
declare function info(msg: any): void;
declare const InvalidPDFException_base: any;
declare class InvalidPDFException extends InvalidPDFException_base {
    [x: string]: any;
    constructor(msg: any);
}
declare function isArrayEqual(arr1: any, arr2: any): boolean;
declare const isNodeJS: any;
declare const LINE_DESCENT_FACTOR: 0.35;
declare const LINE_FACTOR: 1.35;
declare const MAX_IMAGE_SIZE_TO_CACHE: 10000000;
declare const MissingPDFException_base: any;
declare class MissingPDFException extends MissingPDFException_base {
    [x: string]: any;
    constructor(msg: any);
}
declare function normalizeUnicode(str: any): any;
declare function objectFromMap(map: any): any;
declare function objectSize(obj: any): number;
declare namespace OPS {
    let dependency: number;
    let setLineWidth: number;
    let setLineCap: number;
    let setLineJoin: number;
    let setMiterLimit: number;
    let setDash: number;
    let setRenderingIntent: number;
    let setFlatness: number;
    let setGState: number;
    let save: number;
    let restore: number;
    let transform: number;
    let moveTo: number;
    let lineTo: number;
    let curveTo: number;
    let curveTo2: number;
    let curveTo3: number;
    let closePath: number;
    let rectangle: number;
    let stroke: number;
    let closeStroke: number;
    let fill: number;
    let eoFill: number;
    let fillStroke: number;
    let eoFillStroke: number;
    let closeFillStroke: number;
    let closeEOFillStroke: number;
    let endPath: number;
    let clip: number;
    let eoClip: number;
    let beginText: number;
    let endText: number;
    let setCharSpacing: number;
    let setWordSpacing: number;
    let setHScale: number;
    let setLeading: number;
    let setFont: number;
    let setTextRenderingMode: number;
    let setTextRise: number;
    let moveText: number;
    let setLeadingMoveText: number;
    let setTextMatrix: number;
    let nextLine: number;
    let showText: number;
    let showSpacedText: number;
    let nextLineShowText: number;
    let nextLineSetSpacingShowText: number;
    let setCharWidth: number;
    let setCharWidthAndBounds: number;
    let setStrokeColorSpace: number;
    let setFillColorSpace: number;
    let setStrokeColor: number;
    let setStrokeColorN: number;
    let setFillColor: number;
    let setFillColorN: number;
    let setStrokeGray: number;
    let setFillGray: number;
    let setStrokeRGBColor: number;
    let setFillRGBColor: number;
    let setStrokeCMYKColor: number;
    let setFillCMYKColor: number;
    let shadingFill: number;
    let beginInlineImage: number;
    let beginImageData: number;
    let endInlineImage: number;
    let paintXObject: number;
    let markPoint: number;
    let markPointProps: number;
    let beginMarkedContent: number;
    let beginMarkedContentProps: number;
    let endMarkedContent: number;
    let beginCompat: number;
    let endCompat: number;
    let paintFormXObjectBegin: number;
    let paintFormXObjectEnd: number;
    let beginGroup: number;
    let endGroup: number;
    let beginAnnotation: number;
    let endAnnotation: number;
    let paintImageMaskXObject: number;
    let paintImageMaskXObjectGroup: number;
    let paintImageXObject: number;
    let paintInlineImageXObject: number;
    let paintInlineImageXObjectGroup: number;
    let paintImageXObjectRepeat: number;
    let paintImageMaskXObjectRepeat: number;
    let paintSolidColorImageMask: number;
    let constructPath: number;
    let setStrokeTransparent: number;
    let setFillTransparent: number;
}
declare namespace PageActionEventType {
    let O: string;
    let C_1: string;
    //export { C_1 as C };
}
declare const PasswordException_base: any;
declare class PasswordException extends PasswordException_base {
    [x: string]: any;
    constructor(msg: any, code: any);
    code: any;
}
declare namespace PasswordResponses {
    let NEED_PASSWORD: number;
    let INCORRECT_PASSWORD: number;
}
declare namespace PermissionFlag {
    let PRINT_1: number;
    //export { PRINT_1 as PRINT };
    let MODIFY_CONTENTS: number;
    let COPY: number;
    let MODIFY_ANNOTATIONS: number;
    let FILL_INTERACTIVE_FORMS: number;
    let COPY_FOR_ACCESSIBILITY: number;
    let ASSEMBLE: number;
    let PRINT_HIGH_QUALITY: number;
}
declare namespace RenderingIntentFlag {
    let ANY: number;
    let DISPLAY: number;
    let PRINT_2: number;
    //export { PRINT_2 as PRINT };
    let SAVE_1: number;
    //export { SAVE_1 as SAVE };
    let ANNOTATIONS_FORMS: number;
    let ANNOTATIONS_STORAGE: number;
    let ANNOTATIONS_DISABLE: number;
    let IS_EDITING: number;
    let OPLIST: number;
}
declare function setVerbosityLevel(level: any): void;
declare function shadow(obj: any, prop: any, value: any, nonSerializable?: boolean): any;
declare function string32(value: any): string;
declare function stringToBytes(str: any): Uint8Array;
declare function stringToPDFString(str: any): string;
declare function stringToUTF8String(str: any): string;
declare namespace TextRenderingMode {
    let FILL: number;
    let STROKE: number;
    let FILL_STROKE: number;
    let INVISIBLE_1: number;
    //export { INVISIBLE_1 as INVISIBLE };
    let FILL_ADD_TO_PATH: number;
    let STROKE_ADD_TO_PATH: number;
    let FILL_STROKE_ADD_TO_PATH: number;
    let ADD_TO_PATH: number;
    let FILL_STROKE_MASK: number;
    let ADD_TO_PATH_FLAG: number;
}
declare const UnexpectedResponseException_base: any;
declare class UnexpectedResponseException extends UnexpectedResponseException_base {
    [x: string]: any;
    constructor(msg: any, status: any);
    status: any;
}
declare const UnknownErrorException_base: any;
declare class UnknownErrorException extends UnknownErrorException_base {
    [x: string]: any;
    constructor(msg: any, details: any);
    details: any;
}
declare function unreachable(msg: any): void;
declare function utf8StringToString(str: any): string;
declare class Util {
    static makeHexColor(r: any, g: any, b: any): string;
    static scaleMinMax(transform: any, minMax: any): void;
    static transform(m1: any, m2: any): any[];
    static applyTransform(p: any, m: any): any[];
    static applyInverseTransform(p: any, m: any): number[];
    static getAxialAlignedBoundingBox(r: any, m: any): number[];
    static inverseTransform(m: any): number[];
    static singularValueDecompose2dScale(m: any): number[];
    static normalizeRect(rect: any): any;
    static intersect(rect1: any, rect2: any): number[] | null;
    static "__#1@#getExtremumOnCurve"(x0: any, x1: any, x2: any, x3: any, y0: any, y1: any, y2: any, y3: any, t: any, minMax: any): void;
    static "__#1@#getExtremum"(x0: any, x1: any, x2: any, x3: any, y0: any, y1: any, y2: any, y3: any, a: any, b: any, c: any, minMax: any): void;
    static bezierBoundingBox(x0: any, y0: any, x1: any, y1: any, x2: any, y2: any, x3: any, y3: any, minMax: any): any;
}
declare namespace VerbosityLevel {
    let ERRORS: number;
    let WARNINGS: number;
    let INFOS: number;
}
declare function warn(msg: any): void;
//export {};
