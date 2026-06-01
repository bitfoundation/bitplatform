window['bit-bswup.progress version'] = '10.4.5';

(function () {
    const _config: IBswupProgressConfigs = {};

    (window as any).BitBswupProgress = {
        start,
        config
    };

    function start(autoReload: boolean,
        showLogs: boolean,
        showAssets: boolean,
        appContainerSelector: string,
        hideApp: boolean,
        autoHide: boolean,
        handler?: string) {

        const appEl = document.querySelector(appContainerSelector) as HTMLElement;
        const bswupEl = document.getElementById('bit-bswup');
        const progressEl = document.getElementById('bit-bswup-progress-bar');
        const percentEl = document.getElementById('bit-bswup-percent');
        const assetsEl = document.getElementById('bit-bswup-assets');
        const reloadButton = document.getElementById('bit-bswup-reload');
        const errorEl = document.getElementById('bit-bswup-error');
        const errorMessageEl = document.getElementById('bit-bswup-error-message');
        const errorDetailsEl = document.getElementById('bit-bswup-error-details');
        const errorRetryButton = document.getElementById('bit-bswup-error-retry');

        const appElOriginalDisplay = appEl && appEl.style.display;

        (window as any).bitBswupHandler = bitBswupHandler;
        const handlerFn = (handler ? window[handler] : undefined) as (message: any, data: any) => void;

        function bitBswupHandler(message: string, data: any) {
            handleInternal(message, data);

            try {
                handlerFn?.(message, data);
            } catch (err) {
                console.error(err);
            }

            function handleInternal(message: string, data: any) {
                const hideApp_ = _config.hideApp ?? hideApp;
                const showLogs_ = _config.showLogs ?? showLogs;
                const autoHide_ = _config.autoHide ?? autoHide;
                const showAssets_ = _config.showAssets ?? showAssets;
                const autoReload_ = _config.autoReload ?? autoReload;

                switch (message) {
                    case BswupMessage.updateFound: return showLogs_ ? console.log('an update found.') : undefined;

                    case BswupMessage.stateChanged: return showLogs_ ? console.log('state has changed to:', data.currentTarget.state) : undefined;

                    case BswupMessage.activate: return showLogs_ ? console.log('new version activated:', data.version) : undefined;

                    case BswupMessage.downloadStarted:
                        // commenting these lines to prevent showing empty progress when bypass is called in bswup.
                        // these two lines will always be called in the progress event.
                        //hideApp_ && appEl && (appEl.style.display = 'none');
                        //bswupEl && (bswupEl.style.display = 'block');
                        return showLogs_ ? console.log('downloading assets started:', data?.version) : undefined;

                    case BswupMessage.downloadProgress:
                        hideApp_ && appEl && (appEl.style.display = 'none');
                        bswupEl && (bswupEl.style.display = 'block');

                        if (showAssets_ && assetsEl) {
                            const li = document.createElement('li');
                            li.innerHTML = `${data.index}: <b>${data.asset.url}</b>: ${data.asset.hash}`
                            assetsEl.prepend(li);
                        }
                        const percent = Math.round(data.percent);
                        const perStr = `${percent}%`;
                        bswupEl && bswupEl.style.setProperty('--bit-bswup-percent', perStr)
                        bswupEl && bswupEl.style.setProperty('--bit-bswup-percent-text', `"${perStr}"`)
                        progressEl && (progressEl.style.width = `${percent}%`);
                        percentEl && (percentEl.innerHTML = `${percent}%`);
                        return showLogs_ ? console.log('asset downloaded:', data) : undefined;

                    case BswupMessage.downloadFinished:
                        if (autoHide_) {
                            hideApp && appEl && (appEl.style.display = appElOriginalDisplay);
                            bswupEl && (bswupEl.style.display = 'none');
                        }

                        if (autoReload_ || data.firstInstall) {
                            data.reload().then(() => {
                                hideApp && appEl && (appEl.style.display = appElOriginalDisplay);
                                bswupEl && (bswupEl.style.display = 'none');
                            });
                        } else {
                            reloadButton && (reloadButton.style.display = 'block');
                            reloadButton && (reloadButton.onclick = data.reload);
                        }
                        return showLogs_ ? console.log('downloading assets finished.') : undefined;

                    case BswupMessage.updateReady:
                        if (autoReload_) {
                            data.reload();
                        } else {
                            reloadButton && (reloadButton.style.display = 'inline');
                            reloadButton && (reloadButton.onclick = data.reload);
                        }
                        return showLogs_ ? console.log('new update is ready.') : undefined;

                    case BswupMessage.error:
                        // Reveal the install panel even if no progress event landed first
                        // (manifest validation failures fire before any progress message).
                        hideApp_ && appEl && (appEl.style.display = 'none');
                        bswupEl && (bswupEl.style.display = 'block');

                        if (errorEl) {
                            errorEl.style.display = 'block';
                            if (errorMessageEl) errorMessageEl.textContent = (data && data.message) || 'Service worker install failed.';
                            if (errorDetailsEl) {
                                const reasonText = data && data.reason ? `[${data.reason}] ` : '';
                                const urlText = data && data.url ? `\nasset: ${data.url}` : '';
                                const hashText = data && data.hash ? `\nhash: ${data.hash}` : '';
                                errorDetailsEl.textContent = `${reasonText}${urlText}${hashText}`.trim();
                            }
                            if (errorRetryButton) {
                                errorRetryButton.style.display = 'inline-block';
                                errorRetryButton.onclick = () => {
                                    if (data && typeof data.reload === 'function') {
                                        data.reload();
                                    } else {
                                        window.location.reload();
                                    }
                                };
                            }
                        }
                        // Always log errors regardless of showLogs - this is actionable info.
                        console.error('BitBswup install error:', data);
                        return;
                }
            }
        }
    };

    function config(newConfig: IBswupProgressConfigs) {
        Object.assign(_config, newConfig);

        // Keep the assets list visibility in sync when toggled at runtime.
        // The <ul> is server-rendered with an inline display style based on the
        // initial ShowAssets parameter, so flipping the config alone wouldn't
        // reveal/hide it without also updating the element here.
        if (newConfig.showAssets !== undefined) {
            const assetsEl = document.getElementById('bit-bswup-assets');
            if (assetsEl) assetsEl.style.display = newConfig.showAssets ? 'block' : 'none';
        }
    }
}());

interface IBswupProgressConfigs {
    autoReload?: boolean | undefined;
    showLogs?: boolean | undefined;
    showAssets?: boolean | undefined;
    hideApp?: boolean | undefined;
    autoHide?: boolean | undefined;
};
