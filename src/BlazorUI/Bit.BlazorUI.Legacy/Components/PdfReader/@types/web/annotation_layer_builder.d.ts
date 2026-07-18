declare type AnnotationLayerBuilderOptions = {
    pdfPage: PDFPageProxy;
    annotationStorage?: AnnotationStorage | undefined;
    /**
     * - Path for image resources, mainly
     * for annotation icons. Include trailing slash.
     */
    imageResourcesPath?: string | undefined;
    renderForms: boolean;
    linkService: IPDFLinkService;
    downloadManager?: IDownloadManager | undefined;
    enableScripting?: boolean | undefined;
    hasJSActionsPromise?: Promise<boolean> | undefined;
    fieldObjectsPromise?: Promise<{
        [x: string]: Object[];
    } | null> | undefined;
    annotationCanvasMap?: Map<string, HTMLCanvasElement> | undefined;
    accessibilityManager?: TextAccessibilityManager | undefined;
    annotationEditorUIManager?: AnnotationEditorUIManager | undefined;
    onAppend?: Function | undefined;
};
/**
 * @typedef {Object} AnnotationLayerBuilderOptions
 * @property {PDFPageProxy} pdfPage
 * @property {AnnotationStorage} [annotationStorage]
 * @property {string} [imageResourcesPath] - Path for image resources, mainly
 *   for annotation icons. Include trailing slash.
 * @property {boolean} renderForms
 * @property {IPDFLinkService} linkService
 * @property {IDownloadManager} [downloadManager]
 * @property {boolean} [enableScripting]
 * @property {Promise<boolean>} [hasJSActionsPromise]
 * @property {Promise<Object<string, Array<Object>> | null>}
 *   [fieldObjectsPromise]
 * @property {Map<string, HTMLCanvasElement>} [annotationCanvasMap]
 * @property {TextAccessibilityManager} [accessibilityManager]
 * @property {AnnotationEditorUIManager} [annotationEditorUIManager]
 * @property {function} [onAppend]
 */
declare class AnnotationLayerBuilder {
    /**
     * @param {AnnotationLayerBuilderOptions} options
     */
    constructor({ pdfPage, linkService, downloadManager, annotationStorage, imageResourcesPath, renderForms, enableScripting, hasJSActionsPromise, fieldObjectsPromise, annotationCanvasMap, accessibilityManager, annotationEditorUIManager, onAppend, }: AnnotationLayerBuilderOptions);
    pdfPage: PDFPageProxy;
    linkService: IPDFLinkService;
    downloadManager: IDownloadManager | undefined;
    imageResourcesPath: string;
    renderForms: boolean;
    annotationStorage: AnnotationStorage;
    enableScripting: boolean;
    _hasJSActionsPromise: Promise<boolean>;
    _fieldObjectsPromise: Promise<{
        [x: string]: Object[];
    } | null>;
    _annotationCanvasMap: Map<string, HTMLCanvasElement>;
    _accessibilityManager: TextAccessibilityManager;
    _annotationEditorUIManager: AnnotationEditorUIManager;
    annotationLayer: AnnotationLayer | null;
    div: HTMLDivElement | null;
    _cancelled: boolean;
    _eventBus: any;
    /**
     * @param {PageViewport} viewport
     * @param {Object} options
     * @param {string} intent (default value is 'display')
     * @returns {Promise<void>} A promise that is resolved when rendering of the
     *   annotations is complete.
     */
    render(viewport: PageViewport, options: Object, intent?: string): Promise<void>;
    cancel(): void;
    hide(): void;
    hasEditableAnnotations(): boolean;
}
