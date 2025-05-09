namespace BitBlazorUI {
    export class PdfReader {
        private static _bitPdfReaders = new Map<string, BitPdfReaderConfig>();

        public static async setup(config: BitPdfReaderConfig) {
            const { pdfjsLib } = globalThis as unknown as { pdfjsLib: PdfJsLib };

            const loadingTask = pdfjsLib.getDocument(config.url);
            const pdfDoc = await loadingTask.promise;
            config.pdfDoc = pdfDoc;
            config.isRendering = [];
            PdfReader._bitPdfReaders.set(config.id, config);

            return pdfDoc.numPages;
        }

        public static async refreshPage(config: BitPdfReaderConfig, pageNumber: number) {
            let oldConfig = PdfReader._bitPdfReaders.get(config.id);
            if (oldConfig) {
                PdfReader._bitPdfReaders.set(config.id, Object.assign(oldConfig, config));
            } else {
                PdfReader.setup(config);
            }

            await PdfReader.renderPage(config.id, pageNumber);
        }

        public static async renderPage(id: string, pageNumber: number) {
            const config = PdfReader._bitPdfReaders.get(id);

            if (!config || !config.pdfDoc) return;

            if (config.isRendering[pageNumber]) return;
            if (pageNumber < 1 || pageNumber > config.pdfDoc.numPages) return;

            config.isRendering[pageNumber] = true;
            try {
                const page = await config.pdfDoc.getPage(pageNumber);

                const pixelRatio = (config.autoScale && window.devicePixelRatio) || 1;
                const scale = config.scale * pixelRatio;

                const viewport = page.getViewport({ scale: scale });

                let canvas = document.getElementById(config.id) as HTMLCanvasElement;
                if (!canvas) {
                    canvas = document.getElementById(`${config.id}-${pageNumber}`) as HTMLCanvasElement;
                }

                if (!canvas) return;

                const context = canvas.getContext('2d')!;
                canvas.width = viewport.width;
                canvas.height = viewport.height;

                canvas.style.width = `${viewport.width / pixelRatio}px`;
                canvas.style.height = `${viewport.height / pixelRatio}px`;

                const renderContext: RenderParameters = {
                    canvasContext: context,
                    viewport: viewport
                };

                const renderTask = page.render(renderContext);
                await renderTask.promise;

            } catch (err: any) {
                if (err.name !== 'RenderingCancelledException') {
                    throw err;
                }
            } finally {
                config.isRendering[pageNumber] = false;
            }
        }

        public static dispose(id: string) {
            const config = PdfReader._bitPdfReaders.get(id);
            if (!config) return;

            config.pdfDoc?.destroy();
            PdfReader._bitPdfReaders.delete(id);
        }
    }
}

declare type PdfJsLib = {
    getDocument: (src?: string | URL | TypedArray | ArrayBuffer | DocumentInitParameters) => PDFDocumentLoadingTask;
    GlobalWorkerOptions: GlobalWorkerOptions
}

declare type BitPdfReaderConfig = {
    id: string;
    url: string;
    scale: number;
    autoScale: boolean;
    pdfDoc?: PDFDocumentProxy;
    isRendering: boolean[];
}