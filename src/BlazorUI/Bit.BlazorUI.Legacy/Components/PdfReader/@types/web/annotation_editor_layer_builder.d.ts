declare type AnnotationEditorLayerBuilderOptions = {
    uiManager?: AnnotationEditorUIManager | undefined;
    pdfPage: PDFPageProxy;
    l10n?: IL10n | undefined;
    structTreeLayer?: StructTreeLayerBuilder;
    accessibilityManager?: TextAccessibilityManager | undefined;
    annotationLayer?: AnnotationLayer | undefined;
    textLayer?: any;
    drawLayer?: any;
    onAppend?: Function | undefined;
};
/**
 * @typedef {Object} AnnotationEditorLayerBuilderOptions
 * @property {AnnotationEditorUIManager} [uiManager]
 * @property {PDFPageProxy} pdfPage
 * @property {IL10n} [l10n]
 * @property {StructTreeLayerBuilder} [structTreeLayer]
 * @property {TextAccessibilityManager} [accessibilityManager]
 * @property {AnnotationLayer} [annotationLayer]
 * @property {TextLayer} [textLayer]
 * @property {DrawLayer} [drawLayer]
 * @property {function} [onAppend]
 */
declare class AnnotationEditorLayerBuilder {
    /**
     * @param {AnnotationEditorLayerBuilderOptions} options
     */
    constructor(options: AnnotationEditorLayerBuilderOptions);
    pdfPage: PDFPageProxy;
    accessibilityManager: TextAccessibilityManager | undefined;
    l10n: IL10n | GenericL10n | undefined;
    annotationEditorLayer: AnnotationEditorLayer | null;
    div: HTMLDivElement | null;
    _cancelled: boolean;
    /**
     * @param {PageViewport} viewport
     * @param {string} intent (default value is 'display')
     */
    render(viewport: PageViewport, intent?: string): Promise<void>;
    cancel(): void;
    hide(): void;
    show(): void;
}
