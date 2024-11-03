declare type PdfJsLib = {
    getDocument: (src?: string | URL | TypedArray | ArrayBuffer | DocumentInitParameters) => PDFDocumentLoadingTask;
    GlobalWorkerOptions: GlobalWorkerOptions
}

declare type BitPdfReaderConfig = {
    id: string;
    url: string;
    renderAllPages: boolean;
    scale: number;
    initialPageNumber: number;
    pdfDoc?: PDFDocumentProxy;
}

namespace BitBlazorUI {
    export class BitPdfReader {
        private static _initPromise?: Promise<unknown>;
        private static _bitPdfReaders = new Map<string, BitPdfReaderConfig>();

        public static async init(scripts: string[]) {
            if (BitPdfReader._initPromise) {
                await BitPdfReader._initPromise;
            }

            const allScripts = Array.from(document.scripts).map(s => s.src);
            const notAppenedScripts = scripts.filter(s => !allScripts.find(as => as.endsWith(s)));

            if (notAppenedScripts.length == 0) return Promise.resolve();

            const promise = new Promise(async (resolve: any, reject: any) => {
                try {
                    for (let url of notAppenedScripts) await addScript(url);
                    resolve();
                } catch (e: any) {
                    reject(e);
                }
            });
            BitPdfReader._initPromise = promise;
            return promise;

            async function addScript(url: string) {
                return new Promise((res, rej) => {
                    const script = document.createElement('script');
                    script.src = url;
                    script.type = 'module';
                    script.onload = res;
                    script.onerror = rej;
                    document.body.appendChild(script);
                })
            }
        }

        public static async setup(config: BitPdfReaderConfig) {
            var { pdfjsLib } = globalThis as unknown as { pdfjsLib: PdfJsLib };

            var loadingTask = pdfjsLib.getDocument(config.url);
            const pdfDoc = await loadingTask.promise;
            config.pdfDoc = pdfDoc;
            BitPdfReader._bitPdfReaders.set(config.id, config);

            var pageNumber = config.initialPageNumber;
            await this.renderPage(config.id, pageNumber);
        }

        public static async renderPage(id: string, pageNumber: number) {
            const config = BitPdfReader._bitPdfReaders.get(id);
            if (!config || !config.pdfDoc) return;

            const page = await config.pdfDoc.getPage(pageNumber);

            var scale = config.scale;
            var viewport = page.getViewport({ scale: scale });

            var canvas = document.getElementById(config.id) as HTMLCanvasElement;
            var context = canvas.getContext('2d')!;
            canvas.height = viewport.height;
            canvas.width = viewport.width;

            var renderContext: RenderParameters = {
                canvasContext: context,
                viewport: viewport
            };

            var renderTask = page.render(renderContext);
            await renderTask.promise;
        }
    }
}