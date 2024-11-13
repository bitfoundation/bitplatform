declare type PDFPageViewOptions = {
    /**
     * - The viewer element.
     */
    container?: HTMLDivElement | undefined;
    /**
     * - The application event bus.
     */
    eventBus: EventBus;
    /**
     * - The page unique ID (normally its number).
     */
    id: number;
    /**
     * - The page scale display.
     */
    scale?: number | undefined;
    /**
     * - The page viewport.
     */
    defaultViewport: PageViewport;
    /**
     * -
     * A promise that is resolved with an {@link OptionalContentConfig} instance.
     * The default value is `null`.
     */
    optionalContentConfigPromise?: Promise<OptionalContentConfig> | undefined;
    /**
     * - The rendering queue object.
     */
    renderingQueue?: PDFRenderingQueue | undefined;
    /**
     * - Controls if the text layer used for
     * selection and searching is created. The constants from {TextLayerMode}
     * should be used. The default value is `TextLayerMode.ENABLE`.
     */
    textLayerMode?: number | undefined;
    /**
     * - Controls if the annotation layer is
     * created, and if interactive form elements or `AnnotationStorage`-data are
     * being rendered. The constants from {@link AnnotationMode} should be used;
     * see also {@link RenderParameters} and {@link GetOperatorListParameters}.
     * The default value is `AnnotationMode.ENABLE_FORMS`.
     */
    annotationMode?: number | undefined;
    /**
     * - Path for image resources, mainly
     * for annotation icons. Include trailing slash.
     */
    imageResourcesPath?: string | undefined;
    /**
     * - The maximum supported canvas size in
     * total pixels, i.e. width * height. Use `-1` for no limit, or `0` for
     * CSS-only zooming. The default value is 4096 * 8192 (32 mega-pixels).
     */
    maxCanvasPixels?: number | undefined;
    /**
     * - Overwrites background and foreground colors
     * with user defined ones in order to improve readability in high contrast
     * mode.
     */
    pageColors?: Object | undefined;
    /**
     * - Localization service.
     */
    l10n?: IL10n | undefined;
    /**
     * - The object that is used to lookup
     * the necessary layer-properties.
     */
    layerProperties?: Object | undefined;
    /**
     * - Enables hardware acceleration for
     * rendering. The default value is `false`.
     */
    enableHWA?: boolean | undefined;
};
/**
 * @implements {IRenderableView}
 */
declare class PDFPageView implements IRenderableView {
    /**
     * @param {PDFPageViewOptions} options
     */
    constructor(options: PDFPageViewOptions);
    id: number;
    renderingId: string;
    pdfPage: any;
    pageLabel: string | null;
    rotation: number;
    scale: number;
    viewport: PageViewport;
    pdfPageRotate: number;
    _optionalContentConfigPromise: Promise<OptionalContentConfig> | null;
    imageResourcesPath: string;
    maxCanvasPixels: any;
    pageColors: Object | null;
    eventBus: EventBus;
    renderingQueue: PDFRenderingQueue | undefined;
    l10n: IL10n | GenericL10n | undefined;
    renderTask: any;
    resume: (() => void) | null;
    _isStandalone: boolean | undefined;
    _container: HTMLDivElement | undefined;
    _annotationCanvasMap: any;
    annotationLayer: AnnotationLayerBuilder | null;
    annotationEditorLayer: any;
    textLayer: TextLayerBuilder | null;
    zoomLayer: ParentNode | null;
    xfaLayer: XfaLayerBuilder | null;
    structTreeLayer: any;
    drawLayer: any;
    div: HTMLDivElement;
    set renderingState(state: number);
    get renderingState(): number;
    setPdfPage(pdfPage: any): void;
    destroy(): void;
    hasEditableAnnotations(): boolean;
    get _textHighlighter(): any;
    /**
     * @private
     */
    private _resetZoomLayer;
    reset({ keepZoomLayer, keepAnnotationLayer, keepAnnotationEditorLayer, keepXfaLayer, keepTextLayer, }?: {
        keepZoomLayer?: boolean | undefined;
        keepAnnotationLayer?: boolean | undefined;
        keepAnnotationEditorLayer?: boolean | undefined;
        keepXfaLayer?: boolean | undefined;
        keepTextLayer?: boolean | undefined;
    }): void;
    toggleEditingMode(isEditing: any): void;
    /**
     * @typedef {Object} PDFPageViewUpdateParameters
     * @property {number} [scale] The new scale, if specified.
     * @property {number} [rotation] The new rotation, if specified.
     * @property {Promise<OptionalContentConfig>} [optionalContentConfigPromise]
     *   A promise that is resolved with an {@link OptionalContentConfig}
     *   instance. The default value is `null`.
     * @property {number} [drawingDelay]
     */
    /**
     * Update e.g. the scale and/or rotation of the page.
     * @param {PDFPageViewUpdateParameters} params
     */
    update({ scale, rotation, optionalContentConfigPromise, drawingDelay, }: {
        /**
         * The new scale, if specified.
         */
        scale?: number | undefined;
        /**
         * The new rotation, if specified.
         */
        rotation?: number | undefined;
        /**
         * A promise that is resolved with an {@link OptionalContentConfig}instance. The default value is `null`.
         */
        optionalContentConfigPromise?: Promise<OptionalContentConfig> | undefined;
        drawingDelay?: number | undefined;
    }): void;
    /**
     * PLEASE NOTE: Most likely you want to use the `this.reset()` method,
     *              rather than calling this one directly.
     */
    cancelRendering({ keepAnnotationLayer, keepAnnotationEditorLayer, keepXfaLayer, keepTextLayer, cancelExtraDelay, }?: {
        keepAnnotationLayer?: boolean | undefined;
        keepAnnotationEditorLayer?: boolean | undefined;
        keepXfaLayer?: boolean | undefined;
        keepTextLayer?: boolean | undefined;
        cancelExtraDelay?: number | undefined;
    }): void;
    cssTransform({ target, redrawAnnotationLayer, redrawAnnotationEditorLayer, redrawXfaLayer, redrawTextLayer, hideTextLayer, }: {
        target: any;
        redrawAnnotationLayer?: boolean | undefined;
        redrawAnnotationEditorLayer?: boolean | undefined;
        redrawXfaLayer?: boolean | undefined;
        redrawTextLayer?: boolean | undefined;
        hideTextLayer?: boolean | undefined;
    }): void;
    get width(): number;
    get height(): number;
    getPagePoint(x: any, y: any): any[];
    draw(): Promise<any>;
    canvas: HTMLCanvasElement | undefined;
    outputScale: OutputScale | undefined;
    /**
     * @param {string|null} label
     */
    setPageLabel(label: string | null): void;
    /**
     * For use by the `PDFThumbnailView.setImage`-method.
     * @ignore
     */
    get thumbnailCanvas(): HTMLCanvasElement | null | undefined;
}
