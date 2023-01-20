; (function () {
    (window as any).startBupProgress = (showLogs: boolean, showAssets: boolean, appContainerSelector: string, hideApp: boolean, autoHide: boolean) => {
        const appEl = document.querySelector(appContainerSelector) as HTMLElement;
        const bupEl = document.getElementById('bit-bup');
        const progressEl = document.getElementById('bit-bup-progress-bar');
        const percentEl = document.getElementById('bit-bup-percent');
        const assetsEl = document.getElementById('bit-bup-assets');
        (window as any).bitBupHandler = bitBupHandler;

        function bitBupHandler(type, data) {
            switch (type) {
                case 'start':
                    hideApp && appEl && (appEl.style.display = 'none');
                    bupEl && (bupEl.style.display = 'block');
                    return showLogs ? console.log('downloading resources started.') : undefined;
                case 'progress':
                    if (showAssets && assetsEl) {
                        const li = document.createElement('li');
                        li.innerHTML = `${data.index}: <b>[${data.type}] ${data.name}</b>: ${data.url} (${data.integrity})`
                        assetsEl.prepend(li);
                    }
                    const percent = Math.round(data.percent);
                    const perStr = `${percent}%`;
                    bupEl && bupEl.style.setProperty('--bit-bup-percent', perStr)
                    bupEl && bupEl.style.setProperty('--bit-bup-percent-text', `"${perStr}"`)
                    progressEl && (progressEl.style.width = perStr);
                    percentEl && (percentEl.innerHTML = perStr);
                    return showLogs ? console.log('resource downloaded:', data) : undefined;
                case 'end':
                    hideApp && appEl && (appEl.style.display = 'block');
                    autoHide && bupEl && (bupEl.style.display = 'none');
                    return showLogs ? console.log('downloading resources ended.') : undefined;
            }
        }
    };

}());
