declare const Blazor: any;

; (function () {
    const bitBswupScript = document.currentScript;

    window.addEventListener('load', runBswup);

    function runBswup() {
        if (!('serviceWorker' in navigator)) {
            return warn('no serviceWorker in navigator');
        }

        const options = extract();

        info('starting...');

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
                    handle('installed');
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
                        handle('installed', { reload: () => reload() });
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
                handle('progress', { ...data, reload: () => reload() });
            }

            if (type === 'installed') {
                handle('installed', data);
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

            const blazorWasmScriptTag = scriptTags.find(s => s.src && s.src.indexOf(options.blazorScript) !== -1);
            if (!blazorWasmScriptTag) {
                return warn(`blazor script (${options.blazorScript}) not found!`);
            }

            const autostart = blazorWasmScriptTag.attributes['autostart'];
            if (!autostart || autostart.value !== 'false') {
                return warn('no "autostart=false" found on the blazor script tag!');
            }

            if (navigator.serviceWorker.controller) {
                Blazor.start();
            }
        }

        function extract(): BswupOptions {
            const defaultoptions = {
                log: 'info',
                sw: 'service-worker.js',
                scope: '/',
                handler: (...args: any[]) => { },
                blazorScript: '_framework/blazor.webassembly.js',
            }

            const optionsAttribute = (bitBswupScript.attributes)['options'];
            const optionsName = (optionsAttribute || {}).value || 'bitBswup';
            const options = (window[optionsName] || defaultoptions) as BswupOptions;

            const logAttribute = bitBswupScript.attributes['log'];
            options.log = (logAttribute && logAttribute.value) || options.log;

            const swAttribute = bitBswupScript.attributes['sw'];
            options.sw = (swAttribute && swAttribute.value) || options.sw;

            const scopeAttribute = bitBswupScript.attributes['scope'];
            options.scope = (scopeAttribute && scopeAttribute.value) || options.scope;

            const handlerAttribute = bitBswupScript.attributes['handler'];
            const handlerName = (handlerAttribute && handlerAttribute.value) || 'bitBswupHandler';
            options.handler = (window[handlerName] || options.handler) as (...args: any[]) => void;

            const blazorScriptAttribute = bitBswupScript.attributes['blazorScript'];
            options.blazorScript = (blazorScriptAttribute && blazorScriptAttribute.value) || options.blazorScript;

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
        //function info(...texts: string[]) {
        //    console.log(`%cBitBSWUP: ${texts.join('\n')}`, 'color:lightblue');
        //}
        function info(...args: any[]) {
            if (options.log === 'none') return;
            console.info(...['BitBswup:', ...args]);
        }
        function warn(text: string) {
            console.warn(`BitBswup: ${text}`);
        }
    }
}());

interface BswupOptions {
    log: 'none' | 'info' | 'verbose' | 'debug' | 'error'
    sw: string
    scope: string
    handler(...args: any[]): void
    blazorScript: string
}