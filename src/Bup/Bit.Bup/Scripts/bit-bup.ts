; (function () {
    const bitBupScript = document.currentScript;

    if (!Blazor) {
        console.warn('BitBup: no Blazor found!');
        return;
    }

    window.addEventListener('load', runBup);
    (window as any).startBupProgress = startBupProgress;

    // ==============================================================


    function runBup() {
        let counter = 0;
        const fetchPromises = [];

        const options = extract();

        info('starting...');

        startBlazor();

        function startBlazor() {
            const scriptTags = [].slice.call(document.scripts);

            const blazorWasmScriptTag = scriptTags.find(s => s.src && s.src.indexOf('_framework/blazor.webassembly.js') !== -1);
            if (!blazorWasmScriptTag) {
                warn('"blazor.webassembly.js" script tag not found!');
                return;
            }

            const autostart = blazorWasmScriptTag.attributes['autostart'];
            if (!autostart || autostart.value !== 'false') {
                warn('no "autostart=false" found on "blazor.webassembly.js" script tag!');
                return;
            }

            Blazor.start({
                loadBootResource: loadBootResource
            })
        }

        function extract(): BupOptions {
            const optionsAttribute = (bitBupScript.attributes)['options'];
            const optionsName = (optionsAttribute || {}).value || 'bitBup';
            const options = (window[optionsName] || {}) as BupOptions;

            const logAttribute = bitBupScript.attributes['log'];
            options.log = (logAttribute && logAttribute.value) || options.log || 'info';

            const handlerAttribute = bitBupScript.attributes['handler'];
            const handlerName = (handlerAttribute && handlerAttribute.value) || 'bitBupHandler';
            options.handler = (window[handlerName] || options.handler) as (...args: any[]) => void;

            if (!options.handler || typeof options.handler !== 'function') {
                console.warn('BitBup: progress handler not found or is not a function!');
                options.handler = undefined;
            }

            return options;
        }

        function loadBootResource(type, name, url, integrity) {
            if (type === 'dotnetjs') return url; // blazor itself handles this specific resource and needs to have its url
            if (type === 'manifest') return url; // since this is the file containing the resources list lets the blazor itself handle it

            var response = fetch(url, {
                cache: 'no-cache',
                integrity: integrity
            });

            fetchPromises.push(response);

            response.then(_ => {
                if (counter === 0) {
                    handle('start');
                }

                var percent = 100 * (++counter) / fetchPromises.length;

                try {
                    handle('progress', { percent, type, name, url, integrity, index: counter });
                } catch (e) {
                    err(e);
                }

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

    function startBupProgress(showLogs: boolean, showAssets: boolean, appContainerSelector: string) {
        var appEl = document.querySelector(appContainerSelector) as HTMLElement;
        var progressEl = document.getElementById('bit-bup');
        var progressBar = document.getElementById('bit-bup-progress-bar');
        var percentLabel = document.getElementById('bit-bup-percent');
        var assetsUl = document.getElementById('bit-bup-assets');
        (window as any).bitBupHandler = bitBupHandler;

        function bitBupHandler(type, data) {
            switch (type) {
                case 'start':
                    appEl.style.display = 'none';
                    progressEl.style.display = 'block';
                    return showLogs ? console.log('downloading resources started.') : undefined;
                case 'progress':
                    if (showAssets) {
                        const li = document.createElement('li');
                        li.innerHTML = `${data.index}: <b>[${data.type}] ${data.name}</b>: ${data.url} (${data.integrity})`
                        assetsUl.prepend(li);
                    }
                    const percent = Math.round(data.percent);
                    progressBar.style.width = `${percent}%`;
                    percentLabel.innerHTML = `${percent}%`;
                    return showLogs ? console.log('resource downloaded:', data) : undefined;
                case 'end':
                    appEl.style.display = 'block';
                    progressEl.style.display = 'none';
                    return showLogs ? console.log('downloading resources ended.') : undefined;
            }
        }
    };

}());


declare const Blazor: any;

interface BupOptions {
    log: 'none' | 'info' | 'verbose' | 'debug' | 'error'
    handler(...args: any[]): void
}