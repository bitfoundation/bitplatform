declare const Blazor: any;

; (function () {
    const bitBswupScript = document.currentScript;

    window.addEventListener('load', runBswup); // very important: use only the "load" event!!!

    function runBswup() {
        if (!('serviceWorker' in navigator)) {
            return warn('no serviceWorker in navigator');
        }

        const options = extract();

        info('starting...');

        startBlazor();

        navigator.serviceWorker.register(options.sw, { scope: options.scope }).then(prepareRegistration);
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

            const message = JSON.parse(e.data);
            const { type, data } = message;

            if (type === 'CLIENTS_CLAIMED') {
                if (!data.isPassive) {
                    Blazor.start().then(() => blazorStartResolver(undefined));
                    return;
                }

                let counter = 0;
                const fetchPromises = [];
                const totalAssets = data.totalAssets;
                Blazor.start({ loadBootResource });

                return;

                function loadBootResource(type, name, url, integrity) {
                    if (type === 'dotnetjs') return url; // blazor itself handles this specific resource and needs to have its url
                    if (type === 'manifest') return url; // since this is the file containing the resources list lets the blazor itself handle it

                    const response = fetch(url);

                    fetchPromises.push(response);

                    response.then(_ => {
                        if (counter === 0) {
                            handle(BswupMessage.downloadStarted, {});
                        }

                        const internalPercent = 100 * (++counter) / fetchPromises.length;

                        const percent = 100 * (counter + 2) / totalAssets;
                        handle(BswupMessage.downloadProgress, { index: counter + 2, asset: { url, integrity }, percent });

                        if (internalPercent >= 100) {
                            e.source.postMessage(JSON.stringify({ type: 'BLAZOR_ASSETS_DOWNLOADED', data: { totalBlazorAssets: counter + 2 } }));
                        }
                    });

                    return response;
                }
            }

            if (type === 'install') {
                handle(BswupMessage.downloadStarted, data);
            }

            if (type === 'progress') {
                if (!data.isPassive) {
                    handle(BswupMessage.downloadProgress, data);
                }
                if (data.percent >= 100) {
                    if (data.firstTimePassive) {
                        handle(BswupMessage.downloadFinished, { firstTimePassive: true });
                        return;
                    }
                    const firstInstall = !(navigator.serviceWorker.controller);
                    handle(BswupMessage.downloadFinished, { reload, firstInstall });
                }
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

        function warn(...args: any[]) {
            console.warn(...['BitBswup:', ...args]);
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