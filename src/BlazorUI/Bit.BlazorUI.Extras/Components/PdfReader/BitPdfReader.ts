declare type PdfJsLib = {
    getDocument: (src?: string | URL | TypedArray | ArrayBuffer | DocumentInitParameters) => PDFDocumentLoadingTask;
    GlobalWorkerOptions: GlobalWorkerOptions
}

namespace BitBlazorUI {
    export class BitPdfReader {
        private static _initPromise?: Promise<unknown>;
        private static _bitPdfReaders = new Map<string, PDFDocumentProxy>();

        public static async initPdfJs(scripts: string[]) {
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

        public static async setupPdf(id: string, url: string) {
            var { pdfjsLib } = globalThis as unknown as { pdfjsLib: PdfJsLib };

            // The workerSrc property shall be specified.
            //pdfjsLib.GlobalWorkerOptions.workerSrc = '//mozilla.github.io/pdf.js/build/pdf.worker.mjs';

            // Asynchronous download of PDF
            var loadingTask = pdfjsLib.getDocument(url);
            loadingTask.promise.then(function (pdf) {
                console.log('PDF loaded');

                BitPdfReader._bitPdfReaders.set(id, pdf);

                // Fetch the first page
                var pageNumber = 1;
                pdf.getPage(pageNumber).then(function (page) {
                    console.log('Page loaded');

                    var scale = 1.5;
                    var viewport = page.getViewport({ scale: scale });

                    // Prepare canvas using PDF page dimensions
                    var canvas = document.getElementById(id) as HTMLCanvasElement;
                    var context = canvas.getContext('2d')!;
                    canvas.height = viewport.height;
                    canvas.width = viewport.width;

                    // Render PDF page into canvas context
                    var renderContext: RenderParameters = {
                        canvasContext: context,
                        viewport: viewport
                    };
                    var renderTask = page.render(renderContext);
                    renderTask.promise.then(function () {
                        console.log('Page rendered');
                    });
                });
            }, function (reason) {
                // PDF loading error
                console.error(reason);
            });
        }
    }
}