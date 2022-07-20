; (function () {

    const options = extract();

    if (!('serviceWorker' in navigator)) {
        warn('no serviceWorker in navigator');
        return;
    }

    info('starting...')

    startBlazor();

    navigator.serviceWorker.register(options.sw, { scope: options.scope }).then(prepareRegistration);
    navigator.serviceWorker.addEventListener('message', handleMessage);
    navigator.serviceWorker.addEventListener('controllerchange', handleController);

    let reload: () => void;
    function prepareRegistration(reg) {
        reload = () => {
            if (navigator.serviceWorker.controller) {
                reg.waiting && reg.waiting.postMessage('SKIP_WAITING');
            } else {
                window.location.reload();
            }
        };

        if (reg.waiting) {
            if (reg.installing) {
                handle('installing', {});
            } else {
                handle('installed', { reload: () => reload() });
            }
        }

        reg.addEventListener('updatefound', function (e) {
            info('update found', e);
            handle('updatefound', e);
            if (!reg.installing) {
                warn('no registration.installing found!');
                return;
            }
            reg.installing.addEventListener('statechange', function (e) {
                info('state chnaged', e, 'eventPhase:', e.eventPhase, 'currentTarget.state:', e.currentTarget.state);
                handle('statechange', e);

                if (!reg.waiting) return;

                if (navigator.serviceWorker.controller) {
                    info('update finished.');
                } else {
                    info('initialization finished.');
                }
            });
        });
    }

    function handleMessage(e) {
        const message = JSON.parse(e.data);
        const type = message.type;
        const data = message.data;

        if (type === 'installing') {
            handle('installing', data);
        }

        if (type === 'progress') {
            handle('progress', data);
        }

        if (type === 'installed') {
            handle('installed', { ...data, reload: () => reload() });
        }

        if (type === 'activate') {
            handle('activate', data);
        }
    }

    var refreshing = false;
    function handleController(e) {
        info('controller changed.', e);
        handle('controllerchange', e);
        if (refreshing) {
            warn('app is already refreshing...');
            return;
        }
        refreshing = true;
        window.location.reload();
    }

    // ============================================================

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

        if (navigator.serviceWorker.controller) {
            Blazor.start();
        }
    }

    function extract(): BswupOptions {
        const bitBswupScript = document.currentScript;

        const optionsAttribute = (bitBswupScript.attributes)['options'];
        const optionsName = (optionsAttribute || {}).value || 'bitBswup';
        const options = (window[optionsName] || {}) as BswupOptions;

        const logAttribute = bitBswupScript.attributes['log'];
        options.log = (logAttribute && logAttribute.value) || options.log || 'info';

        const swAttribute = bitBswupScript.attributes['sw'];
        options.sw = (swAttribute && swAttribute.value) || options.sw || 'service-worker.js';

        const scopeAttribute = bitBswupScript.attributes['scope'];
        options.scope = (scopeAttribute && scopeAttribute.value) || options.scope || '/';

        const handlerAttribute = bitBswupScript.attributes['handler'];
        const handlerName = (handlerAttribute && handlerAttribute.value) || 'bitBswupHandler';
        options.handler = (window[handlerName] || options.handler) as (...args: any[]) => void;

        if (!options.handler || typeof options.handler !== 'function') {
            warn('progress handler not found or is not a function!');
            options.handler = undefined;
        }

        return options;
    }

    function handle(...args: any[]) {
        options.handler && options.handler(...args);
    }

    // TODO: apply log options: info, verbode, debug, error, ...
    function info(...texts: string[]) {
        console.log(`%cBitBSWUP: ${texts.join('\n')}`, 'color:lightblue');
    }
    function warn(text: string) {
        console.warn(`BitBSWUP:${text}`);
    }

    function startDefaultProgress() {
        var appEl = document.getElementById('app');
        var progressEl = document.getElementById('bit-bswup');
        var progressBar = document.getElementById('bit-bswup-progress-bar');
        var percentLabel = document.getElementById('bit-bswup-percent');
        var reloadButton = document.getElementById('bit-bswup-reload');
        var assetsUl = document.getElementById('bit-bswup-assets');
        (window as any).bitBswupHandler = bitBswupHandler;

        function bitBswupHandler(type, data) {
            switch (type) {
                case 'updatefound':
                    return console.log('new version is downloading...');
                case 'statechange':
                    return console.log('new version state has changed to:', data.currentTarget.state);
                case 'controllerchange':
                    return console.log('sw controller changed:', data);
                case 'installing':
                    appEl.style.display = 'none';
                    progressEl.style.display = 'block';
                    return console.log('installing new version:', data.version);
                case 'installed':
                    reloadButton.style.display = 'block';
                    console.log('new version installed:', data.version)
                    // data.reload();
                    reloadButton.onclick = data.reload;
                    return;
                case 'progress':
                    const li = document.createElement('li');
                    li.innerHTML = `${data.index}: <b>${data.asset.url}</b>: ${data.asset.hash}`
                    assetsUl.prepend(li);
                    const percent = Math.round(data.percent);
                    progressBar.style.width = `${percent}%`;
                    percentLabel.innerHTML = `${percent} %`;
                    return console.log('asset downloaded:', data);
                case 'activate':
                    return console.log('new version activated:', data.version);
            }
        }
    }
    (window as any).startDefaultProgress = startDefaultProgress;


}());

declare const Blazor: any;

interface BswupOptions {
    log: 'info' | 'verbose' | 'debug' | 'error'
    sw: string
    scope: string
    handler(...args: any[]): void
}