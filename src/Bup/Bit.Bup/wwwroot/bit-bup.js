var __spreadArray = (this && this.__spreadArray) || function (to, from, pack) {
    if (pack || arguments.length === 2) for (var i = 0, l = from.length, ar; i < l; i++) {
        if (ar || !(i in from)) {
            if (!ar) ar = Array.prototype.slice.call(from, 0, i);
            ar[i] = from[i];
        }
    }
    return to.concat(ar || Array.prototype.slice.call(from));
};
;
(function () {
    var bitBupScript = document.currentScript;
    if (!Blazor) {
        console.warn('BitBup: no Blazor found!');
        return;
    }
    window.addEventListener('load', runBup);
    window.startBupProgress = startBupProgress;
    // ==============================================================
    function runBup() {
        var counter = 0;
        var fetchPromises = [];
        var options = extract();
        info('starting...');
        startBlazor();
        function startBlazor() {
            var scriptTags = [].slice.call(document.scripts);
            var blazorWasmScriptTag = scriptTags.find(function (s) { return s.src && s.src.indexOf('_framework/blazor.webassembly.js') !== -1; });
            if (!blazorWasmScriptTag) {
                warn('"blazor.webassembly.js" script tag not found!');
                return;
            }
            var autostart = blazorWasmScriptTag.attributes['autostart'];
            if (!autostart || autostart.value !== 'false') {
                warn('no "autostart=false" found on "blazor.webassembly.js" script tag!');
                return;
            }
            Blazor.start({
                loadBootResource: loadBootResource
            });
        }
        function extract() {
            var optionsAttribute = (bitBupScript.attributes)['options'];
            var optionsName = (optionsAttribute || {}).value || 'bitBup';
            var options = (window[optionsName] || {});
            var logAttribute = bitBupScript.attributes['log'];
            options.log = (logAttribute && logAttribute.value) || options.log || 'info';
            var handlerAttribute = bitBupScript.attributes['handler'];
            var handlerName = (handlerAttribute && handlerAttribute.value) || 'bitBupHandler';
            options.handler = (window[handlerName] || options.handler);
            if (!options.handler || typeof options.handler !== 'function') {
                console.warn('BitBup: progress handler not found or is not a function!');
                options.handler = undefined;
            }
            return options;
        }
        function loadBootResource(type, name, url, integrity) {
            if (type === 'dotnetjs')
                return url; // blazor itself handles this specific resource and needs to have its url
            if (type === 'manifest')
                return url; // since this is the file containing the resources list lets the blazor itself handle it
            var response = fetch(url, {
                cache: 'no-cache',
                integrity: integrity
            });
            fetchPromises.push(response);
            response.then(function (_) {
                if (counter === 0) {
                    handle('start');
                }
                var percent = 100 * (++counter) / fetchPromises.length;
                try {
                    handle('progress', { percent: percent, type: type, name: name, url: url, integrity: integrity, index: counter });
                }
                catch (e) {
                    err(e);
                }
                if (percent >= 100) {
                    handle('end');
                }
            });
            return response;
        }
        function handle() {
            var args = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                args[_i] = arguments[_i];
            }
            options.handler && options.handler.apply(options, args);
        }
        // TODO: apply log options: info, verbode, debug, error, ...
        //function info(...texts: string[]) {
        //    console.log(`%cBitBup: ${texts.join('\n')}`, 'color:lightblue');
        //}
        function info() {
            var args = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                args[_i] = arguments[_i];
            }
            if (options.log === 'none')
                return;
            console.info.apply(console, __spreadArray(['BitBup:'], args, true));
        }
        function warn(text) {
            console.warn("BitBup: ".concat(text));
        }
        function err(e) {
            console.error('BitBup:', e);
        }
    }
    function startBupProgress(showLogs, showAssets, appContainerSelector) {
        var appEl = document.querySelector(appContainerSelector);
        var progressEl = document.getElementById('bit-bup');
        var progressBar = document.getElementById('bit-bup-progress-bar');
        var percentLabel = document.getElementById('bit-bup-percent');
        var assetsUl = document.getElementById('bit-bup-assets');
        window.bitBupHandler = bitBupHandler;
        function bitBupHandler(type, data) {
            switch (type) {
                case 'start':
                    appEl.style.display = 'none';
                    progressEl.style.display = 'block';
                    return showLogs ? console.log('downloading resources started.') : undefined;
                case 'progress':
                    if (showAssets) {
                        var li = document.createElement('li');
                        li.innerHTML = "".concat(data.index, ": <b>[").concat(data.type, "] ").concat(data.name, "</b>: ").concat(data.url, " (").concat(data.integrity, ")");
                        assetsUl.prepend(li);
                    }
                    var percent = Math.round(data.percent);
                    progressBar.style.width = "".concat(percent, "%");
                    percentLabel.innerHTML = "".concat(percent, "%");
                    return showLogs ? console.log('resource downloaded:', data) : undefined;
                case 'end':
                    appEl.style.display = 'block';
                    progressEl.style.display = 'none';
                    return showLogs ? console.log('downloading resources ended.') : undefined;
            }
        }
    }
    ;
}());
