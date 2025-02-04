window['bit-bswup.progress version'] = '9.4.0-pre-03';

; (function () {
    (window as any).startBswupProgress = (autoReload: boolean,
        showLogs: boolean,
        showAssets: boolean,
        appContainerSelector: string,
        hideApp: boolean,
        handler?: string) => {

        const appEl = document.querySelector(appContainerSelector) as HTMLElement;
        const bswupEl = document.getElementById('bit-bswup');
        const progressEl = document.getElementById('bit-bswup-progress-bar');
        const percentEl = document.getElementById('bit-bswup-percent');
        const assetsEl = document.getElementById('bit-bswup-assets');
        const reloadButton = document.getElementById('bit-bswup-reload');

        const appElOriginalDisplay = appEl && appEl.style.display;

        (window as any).bitBswupHandler = bitBswupHandler;
        const handlerFn = (handler ? window[handler] : undefined) as (message: any, data: any) => void;

        function bitBswupHandler(message, data) {
            handleInternal(message, data);

            try {
                handlerFn?.(message, data);
            } catch (err) {
                console.error(err);
            }

            function handleInternal(message, data) {
                switch (message) {
                    case BswupMessage.updateFound: return showLogs ? console.log('an update found.') : undefined;

                    case BswupMessage.stateChanged: return showLogs ? console.log('state has changed to:', data.currentTarget.state) : undefined;

                    case BswupMessage.activate: return showLogs ? console.log('new version activated:', data.version) : undefined;

                    case BswupMessage.downloadStarted:
                        hideApp && appEl && (appEl.style.display = 'none');
                        bswupEl && (bswupEl.style.display = 'block');
                        return showLogs ? console.log('downloading assets started:', data?.version) : undefined;

                    case BswupMessage.downloadProgress:
                        hideApp && appEl && (appEl.style.display = 'none');
                        bswupEl && (bswupEl.style.display = 'block');

                        if (showAssets && assetsEl) {
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
                        return showLogs ? console.log('asset downloaded:', data) : undefined;

                    case BswupMessage.downloadFinished:
                        if (autoReload || data.firstInstall) {
                            data.reload().then(() => {
                                hideApp && appEl && (appEl.style.display = appElOriginalDisplay);
                                bswupEl && (bswupEl.style.display = 'none');
                            });
                        } else {
                            reloadButton && (reloadButton.style.display = 'block');
                            reloadButton && (reloadButton.onclick = data.reload);
                        }
                        return showLogs ? console.log('downloading assets finished.') : undefined;

                    case BswupMessage.updateReady:
                        if (autoReload) {
                            data.reload();
                        } else {
                            reloadButton && (reloadButton.style.display = 'inline');
                            reloadButton && (reloadButton.onclick = data.reload);
                        }
                        return showLogs ? console.log('new update is ready.') : undefined;
                }
            }
        }

    };

}());