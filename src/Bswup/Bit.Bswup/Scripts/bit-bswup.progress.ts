; (function () {
    (window as any).startBswupProgress = (autoReload: boolean, showLogs: boolean, showAssets: boolean, appContainerSelector: string, hideApp: boolean) => {
        var appEl = document.querySelector(appContainerSelector) as HTMLElement;
        var bswupEl = document.getElementById('bit-bswup');
        var progressEl = document.getElementById('bit-bswup-progress-bar');
        var percentEl = document.getElementById('bit-bswup-percent');
        var assetsEl = document.getElementById('bit-bswup-assets');
        var reloadButton = document.getElementById('bit-bswup-reload');
        (window as any).bitBswupHandler = bitBswupHandler;

        function bitBswupHandler(type, data) {
            switch (type) {
                case 'updatefound':
                    return showLogs ? console.log('new version is downloading...') : undefined;
                case 'statechange':
                    return showLogs ? console.log('new version state has changed to:', data.currentTarget.state) : undefined;
                case 'controllerchange':
                    return showLogs ? console.log('sw controller changed:', data) : undefined;
                case 'installing':
                    hideApp && appEl && (appEl.style.display = 'none');
                    bswupEl && (bswupEl.style.display = 'block');
                    return showLogs ? console.log('installing new version:', data.version) : undefined;
                case 'installed':
                    return showLogs ? console.log('new version installed:', data.version) : undefined;
                case 'progress':
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
                    if (percent >= 100) {
                        if (autoReload) {
                            data.reload();
                        } else {
                            reloadButton && (reloadButton.style.display = 'inline');
                            reloadButton && (reloadButton.onclick = data.reload);
                        }
                    }
                    return showLogs ? console.log('asset downloaded:', data) : undefined;
                case 'activate':
                    return showLogs ? console.log('new version activated:', data.version) : undefined;
            }
        }
    };

}());