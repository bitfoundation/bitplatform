﻿; (function () {

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

    var reload;
    function prepareRegistration(reg) {
        reload = function () {
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

    function extract() {
        const bitBswupScript = document.currentScript;

        const optionsAttribute = bitBswupScript.attributes['options'];
        const optionsName = (optionsAttribute || {}).value || 'bitBswup';
        const options = window[optionsName] || {};

        const logAttribute = bitBswupScript.attributes['log'];
        options.log = (logAttribute && logAttribute.value) || options.log || 'info';

        const swAttribute = bitBswupScript.attributes['sw'];
        options.sw = (swAttribute && swAttribute.value) || options.sw || 'service-worker.js';

        const scopeAttribute = bitBswupScript.attributes['scope'];
        options.scope = (scopeAttribute && scopeAttribute.value) || options.scope || '/';

        const handlerAttribute = bitBswupScript.attributes['handler'];
        const handlerName = (handlerAttribute && handlerAttribute.value) || 'bitBswupHandler';
        options.handler = window[handlerName] || options.handler;

        if (!options.handler || typeof options.handler !== 'function') {
            warn('progress handler not found or is not a function!');
            options.handler = undefined;
        }

        return options;
    }

    function handle() {
        options.handler && options.handler(...arguments);
    }

    // TODO: apply log options: info, verbode, debug, error, ...
    function info(text) {
        console.log(`%cBitBSWUP: ${text}`, 'color:lightblue');
    }
    function warn(text) {
        console.warn(`BitBSWUP:${text}`);
    }

}());