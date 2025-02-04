var BitBup = BitBup || {};
BitBup.version = window['bit-bup version'] = '9.4.0-pre-03';

declare const Blazor: any;

; (function () {
    const bitBupScript = document.currentScript;

    if (!Blazor) {
        console.warn('BitBup: no Blazor found!');
        return;
    }

    window.addEventListener('DOMContentLoaded', runBup);


    function runBup() {
        let counter = 0;
        const fetchPromises = [];

        const options = extract();

        info('starting...');

        startBlazor();

        function startBlazor() {
            const scriptTags = [].slice.call(document.scripts);

            const blazorWasmScriptTag = scriptTags.find(s => s.src && s.src.indexOf(options.blazorScript) !== -1);
            if (!blazorWasmScriptTag) {
                warn(`blazor script (${options.blazorScript}) not found!`);
                return;
            }

            const autostart = blazorWasmScriptTag.attributes['autostart'];
            if (!autostart || autostart.value !== 'false') {
                warn('no "autostart=false" found on the blazor script tag!');
                return;
            }

            Blazor.start({ loadBootResource });
        }

        function extract(): BupOptions {
            const defaultoptions = {
                log: 'info',
                handler: (...args: any[]) => { },
                blazorScript: '_framework/blazor.webassembly.js',
                integrityCheck: false,
            }
            const optionsAttribute = (bitBupScript.attributes)['options'];
            const optionsName = (optionsAttribute || {}).value || 'bitBup';
            const options = (window[optionsName] || defaultoptions) as BupOptions;

            const logAttribute = bitBupScript.attributes['log'];
            options.log = (logAttribute && logAttribute.value) || options.log;

            const handlerAttribute = bitBupScript.attributes['handler'];
            const handlerName = (handlerAttribute && handlerAttribute.value) || 'bitBupHandler';
            options.handler = (window[handlerName] || options.handler) as (...args: any[]) => void;

            const blazorScriptAttribute = bitBupScript.attributes['blazorScript'];
            options.blazorScript = (blazorScriptAttribute && blazorScriptAttribute.value) || options.blazorScript;

            const integrityCheckAttribute = bitBupScript.attributes['integrityCheck'];
            options.integrityCheck = ((integrityCheckAttribute && integrityCheckAttribute.value) === 'true') || options.integrityCheck;

            if (!options.handler || typeof options.handler !== 'function') {
                console.warn('BitBup: progress handler not found or is not a function!');
                options.handler = undefined;
            }

            return options;
        }

        function loadBootResource(type, name, url, integrity) {
            if (type === 'manifest') return url; // since this is the file containing the resources list lets the blazor itself handle it
            if (type === 'dotnetjs') return url; // blazor itself handles this specific resource and needs to have its url

            const resourceUrl = `${url}?v=${integrity}`;
            const requestInit: RequestInit = options.integrityCheck
                ? { cache: 'no-store', integrity: integrity, headers: [['cache-control', 'public, max-age=3153600']] }
                : { cache: 'no-store', headers: [['cache-control', 'public, max-age=3153600']] };

            const response = fetch(resourceUrl, requestInit);

            fetchPromises.push(response);

            response.then(_ => {
                if (counter === 0) {
                    handle('start');
                }

                //if (fetchPromises.length < 10) return;

                const percent = 100 * (++counter) / fetchPromises.length;

                handle('progress', { percent, type, name, url, integrity, index: counter });

                if (percent >= 100) {
                    handle('end');
                }
            });

            return response;
        }

        function handle(...args: any[]) {
            options.handler && options.handler(...args);
        }

        // TODO: apply log options: info, verbode, debug, error, ...
        //function info(...texts: string[]) {
        //    console.log(`%cBitBup: ${texts.join('\n')}`, 'color:lightblue');
        //}
        function info(...args: any[]) {
            if (options.log === 'none') return;
            console.info(...['BitBup:', ...args]);
        }
        function warn(text: string) {
            console.warn(`BitBup: ${text}`);
        }
        function err(e: any) {
            console.error('BitBup:', e);
        }
    }

}());

interface BupOptions {
    log: 'none' | 'info' | 'verbose' | 'debug' | 'error'
    handler(...args: any[]): void
    blazorScript: string
    integrityCheck: boolean
}