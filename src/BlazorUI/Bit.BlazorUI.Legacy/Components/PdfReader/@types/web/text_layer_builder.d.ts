declare type TextLayerBuilderOptions = {
    pdfPage: PDFPageProxy;
    /**
     * - Optional object that will handle
     * highlighting text from the find controller.
     */
    highlighter?: TextHighlighter | undefined;
    accessibilityManager?: TextAccessibilityManager | undefined;
    onAppend?: Function | undefined;
};
/**
 * @typedef {Object} TextLayerBuilderOptions
 * @property {PDFPageProxy} pdfPage
 * @property {TextHighlighter} [highlighter] - Optional object that will handle
 *   highlighting text from the find controller.
 * @property {TextAccessibilityManager} [accessibilityManager]
 * @property {function} [onAppend]
 */
/**
 * The text layer builder provides text selection functionality for the PDF.
 * It does this by creating overlay divs over the PDF's text. These divs
 * contain text that matches the PDF text they are overlaying.
 */
declare class TextLayerBuilder {
    static "__#68@#textLayers": Map<any, any>;
    static "__#68@#selectionChangeAbortController": null;
    static "__#68@#removeGlobalSelectionListener"(textLayerDiv: any): void;
    static "__#68@#enableGlobalSelectionListener"(): void;
    constructor({ pdfPage, highlighter, accessibilityManager, enablePermissions, onAppend, }: {
        pdfPage: any;
        highlighter?: null | undefined;
        accessibilityManager?: null | undefined;
        enablePermissions?: boolean | undefined;
        onAppend?: null | undefined;
    });
    pdfPage: any;
    highlighter: any;
    accessibilityManager: any;
    div: HTMLDivElement;
    /**
     * Renders the text layer.
     * @param {PageViewport} viewport
     * @param {Object} [textContentParams]
     */
    render(viewport: PageViewport, textContentParams?: Object | undefined): Promise<void>;
    hide(): void;
    show(): void;
    /**
     * Cancel rendering of the text layer.
     */
    cancel(): void;
}
