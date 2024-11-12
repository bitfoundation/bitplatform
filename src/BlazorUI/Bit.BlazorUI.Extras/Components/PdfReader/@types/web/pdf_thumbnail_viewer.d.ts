declare type PDFThumbnailViewerOptions = {
    /**
     * - The container for the thumbnail
     * elements.
     */
    container: HTMLDivElement;
    /**
     * - The application event bus.
     */
    eventBus: EventBus;
    /**
     * - The navigation/linking service.
     */
    linkService: IPDFLinkService;
    /**
     * - The rendering queue object.
     */
    renderingQueue: PDFRenderingQueue;
    /**
     * - Overwrites background and foreground colors
     * with user defined ones in order to improve readability in high contrast
     * mode.
     */
    pageColors?: Object | undefined;
    /**
     * - The AbortSignal for the window
     * events.
     */
    abortSignal?: AbortSignal | undefined;
    /**
     * - Enables hardware acceleration for
     * rendering. The default value is `false`.
     */
    enableHWA?: boolean | undefined;
};
/**
 * @typedef {Object} PDFThumbnailViewerOptions
 * @property {HTMLDivElement} container - The container for the thumbnail
 *   elements.
 * @property {EventBus} eventBus - The application event bus.
 * @property {IPDFLinkService} linkService - The navigation/linking service.
 * @property {PDFRenderingQueue} renderingQueue - The rendering queue object.
 * @property {Object} [pageColors] - Overwrites background and foreground colors
 *   with user defined ones in order to improve readability in high contrast
 *   mode.
 * @property {AbortSignal} [abortSignal] - The AbortSignal for the window
 *   events.
 * @property {boolean} [enableHWA] - Enables hardware acceleration for
 *   rendering. The default value is `false`.
 */
/**
 * Viewer control to display thumbnails for pages in a PDF document.
 */
declare class PDFThumbnailViewer {
    /**
     * @param {PDFThumbnailViewerOptions} options
     */
    constructor({ container, eventBus, linkService, renderingQueue, pageColors, abortSignal, enableHWA, }: PDFThumbnailViewerOptions);
    container: HTMLDivElement;
    eventBus: EventBus;
    linkService: IPDFLinkService;
    renderingQueue: PDFRenderingQueue;
    pageColors: Object | null;
    enableHWA: boolean;
    scroll: {
        right: boolean;
        down: boolean;
        lastX: any;
        lastY: any;
        _eventHandler: (evt: any) => void;
    };
    getThumbnail(index: any): any;
    scrollThumbnailIntoView(pageNumber: any): void;
    _currentPageNumber: any;
    set pagesRotation(rotation: any);
    get pagesRotation(): any;
    _pagesRotation: any;
    cleanup(): void;
    _thumbnails: any[] | undefined;
    _pageLabels: any[] | null | undefined;
    /**
     * @param {PDFDocumentProxy} pdfDocument
     */
    setDocument(pdfDocument: PDFDocumentProxy): void;
    pdfDocument: PDFDocumentProxy | undefined;
    /**
     * @param {Array|null} labels
     */
    setPageLabels(labels: any[] | null): void;
    forceRendering(): boolean;
}
