const BitBswup = {} as any;
BitBswup.version = window['bit-bswup version'] = '9.4.0-pre-03';

declare const Blazor: any;

BitBswup.checkForUpdate = async () => {
    if (!('serviceWorker' in navigator)) {
        return console.warn('no serviceWorker in navigator');
    }

    const reg = await navigator.serviceWorker.getRegistration();
    const result = await reg.update();
    return result;
}

BitBswup.forceRefresh = async () => {
    if (!('serviceWorker' in navigator)) {
        return console.warn('no serviceWorker in navigator');
    }

    const cacheKeys = await caches.keys();
    const cachePromises = cacheKeys.filter(key => key.startsWith('bit-bswup') || key.startsWith('blazor-resources')).map(key => caches.delete(key));
    await Promise.all(cachePromises);

    const regs = await navigator.serviceWorker.getRegistrations();
    const regPromises = regs.map(r => r.unregister());
    await Promise.all(regPromises);

    window.location.reload();
}

;(function () {
    const bitBswupScript = document.currentScript;

    window.addEventListener('DOMContentLoaded', runBswup); // important event!

    function runBswup() {
        if (!('serviceWorker' in navigator)) {
            return warn('no serviceWorker in navigator');
        }

        const options = extract();

        info('starting...');

        startBlazor();

        navigator.serviceWorker.register(options.sw, { scope: options.scope, updateViaCache: 'none' }).then(prepareRegistration);
        navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
        navigator.serviceWorker.addEventListener('message', handleMessage);

        let reload: () => void;
        let blazorStartResolver: (value: unknown) => void;
        function prepareRegistration(reg) {
            reload = () => {
                if (navigator.serviceWorker.controller) {
                    reg.waiting && reg.waiting.postMessage('SKIP_WAITING');
                    return Promise.resolve();
                }

                if (reg.active) {
                    reg.active.postMessage('CLAIM_CLIENTS');
                    return new Promise((res, _) => blazorStartResolver = res);
                }

                window.location.reload();
            };

            if (reg.waiting) {
                info('registration waiting:', reg.waiting);
                if (reg.installing) {
                    info('registration installing:', reg.installing);
                } else {
                    info('registration is ready:', reg.waiting);
                    handle(BswupMessage.updateReady, { reload });
                }
            }

            reg.addEventListener('updatefound', function (e) {
                info('update found', e);
                handle(BswupMessage.updateFound, e);

                if (!reg.installing) {
                    warn('no registration.installing found!');
                    return;
                }

                reg.installing.addEventListener('statechange', function (e) {
                    info('state chnaged', e, 'eventPhase:', e.eventPhase, 'currentTarget.state:', e.currentTarget.state);
                    handle(BswupMessage.stateChanged, e);

                    if (!reg.waiting) return;

                    if (navigator.serviceWorker.controller) {
                        info('update finished.'); // not first install
                    } else {
                        info('initialization finished.'); // first install
                    }
                });
            });
        }

        function handleControllerChange(e) {
            info('controller changed.', e);
        }

        function handleMessage(e) {
            if (e.data === 'WAITING_SKIPPED') {
                window.location.reload();
                return;
            }

            if (e.data === 'CLIENTS_CLAIMED') {
                Blazor.start().then(() => {
                    blazorStartResolver(undefined);
                    e.source.postMessage('BLAZOR_STARTED');
                });
                return;
            }

            const message = JSON.parse(e.data);
            const { type, data } = message;

            if (type === 'install') {
                handle(BswupMessage.downloadStarted, data);
            }

            if (type === 'progress') {
                handle(BswupMessage.downloadProgress, data);
                if (data.percent >= 100) {
                    const firstInstall = !(navigator.serviceWorker.controller);
                    handle(BswupMessage.downloadFinished, { reload, firstInstall });
                }
            }

            if (type === 'bypass') {
                const firstInstall = data?.firstTime || !(navigator.serviceWorker.controller);
                handle(BswupMessage.downloadFinished, { reload, firstInstall });
            }

            if (type === 'activate') {
                handle(BswupMessage.activate, data);
            }
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
                scope: '/',
                log: 'none',
                sw: 'service-worker.js',
                handlerName: 'bitBswupHandler',
                blazorScript: '_framework/blazor.web.js',
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
            options.handlerName = (handlerAttribute && handlerAttribute.value) || options.handlerName;

            const blazorScriptAttribute = bitBswupScript.attributes['blazorScript'];
            options.blazorScript = (blazorScriptAttribute && blazorScriptAttribute.value) || options.blazorScript;

            return options;
        }

        function handle(...args: any[]) {
            if (!options.handler) {
                options.handler = window[options.handlerName];

                if (!options.handler || typeof options.handler !== 'function') {
                    warn('progress handler not found or is not a function!');
                    options.handler = () => { };
                }
            }

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

        function warn(...args: any[]) {
            console.warn(...['BitBswup:', ...args]);
        }
    }
}());

interface BswupOptions {
    log: 'none' | 'info' | 'verbose' | 'debug' | 'error'
    sw: string
    scope: string
    handlerName: string
    blazorScript: string
    handler?(...args: any[]): void
}

const BswupMessage = {
    downloadStarted: 'DOWNLOAD_STARTED',
    downloadProgress: 'DOWNLOAD_PROGRESS',
    downloadFinished: 'DOWNLOAD_FINISHED',
    activate: 'ACTIVATE',
    updateInstalled: 'UPDATE_INSTALLED',
    updateReady: 'UPDATE_READY',
    updateFound: 'UPDATE_FOUND',
    stateChanged: 'STATE_CHANGED'
};