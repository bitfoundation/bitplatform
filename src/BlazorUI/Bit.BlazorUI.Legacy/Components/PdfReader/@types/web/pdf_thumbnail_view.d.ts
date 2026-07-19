declare type PDFThumbnailViewOptions = {
    /**
     * - The viewer element.
     */
    container: HTMLDivElement;
    /**
     * - The application event bus.
     */
    eventBus: EventBus;
    /**
     * - The thumbnail's unique ID (normally its number).
     */
    id: number;
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
     * - Enables hardware acceleration for
     * rendering. The default value is `false`.
     */
    enableHWA?: boolean | undefined;
};
/**
 * @implements {IRenderableView}
 */
declare class PDFThumbnailView implements IRenderableView {
    /**
     * @param {PDFThumbnailViewOptions} options
     */
    constructor({ container, eventBus, id, defaultViewport, optionalContentConfigPromise, linkService, renderingQueue, pageColors, enableHWA, }: PDFThumbnailViewOptions);
    id: number;
    renderingId: string;
    pageLabel: string | null;
    pdfPage: any;
    rotation: number;
    viewport: PageViewport;
    pdfPageRotate: number;
    _optionalContentConfigPromise: Promise<OptionalContentConfig> | null;
    pageColors: Object | null;
    enableHWA: boolean;
    eventBus: EventBus;
    linkService: IPDFLinkService;
    renderingQueue: PDFRenderingQueue;
    renderTask: any;
    renderingState: number;
    resume: (() => void) | null;
    anchor: HTMLAnchorElement;
    div: HTMLDivElement;
    _placeholderImg: HTMLDivElement;
    canvasWidth: number | undefined;
    canvasHeight: number | undefined;
    scale: number | undefined;
    setPdfPage(pdfPage: any): void;
    reset(): void;
    update({ rotation }: {
        rotation?: null | undefined;
    }): void;
    /**
     * PLEASE NOTE: Most likely you want to use the `this.reset()` method,
     *              rather than calling this one directly.
     */
    cancelRendering(): void;
    image: HTMLImageElement | undefined;
    draw(): Promise<any>;
    setImage(pageView: any): void;
    /**
     * @param {string|null} label
     */
    setPageLabel(label: string | null): void;
}
/**
 * @typedef {Object} PDFThumbnailViewOptions
 * @property {HTMLDivElement} container - The viewer element.
 * @property {EventBus} eventBus - The application event bus.
 * @property {number} id - The thumbnail's unique ID (normally its number).
 * @property {PageViewport} defaultViewport - The page viewport.
 * @property {Promise<OptionalContentConfig>} [optionalContentConfigPromise] -
 *   A promise that is resolved with an {@link OptionalContentConfig} instance.
 *   The default value is `null`.
 * @property {IPDFLinkService} linkService - The navigation/linking service.
 * @property {PDFRenderingQueue} renderingQueue - The rendering queue object.
 * @property {Object} [pageColors] - Overwrites background and foreground colors
 *   with user defined ones in order to improve readability in high contrast
 *   mode.
 * @property {boolean} [enableHWA] - Enables hardware acceleration for
 *   rendering. The default value is `false`.
 */
declare class TempImageFactory {
    static "__#72@#tempCanvas": null;
    static getCanvas(width: any, height: any): (HTMLCanvasElement | CanvasRenderingContext2D | null)[];
    static destroyCanvas(): void;
}
