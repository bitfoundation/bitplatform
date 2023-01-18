; (function () {
    (window as any).startBupProgress = (showLogs: boolean, showAssets: boolean, appContainerSelector: string, hideApp: boolean) => {
        var appEl = document.querySelector(appContainerSelector) as HTMLElement;
        var bupEl = document.getElementById('bit-bup');
        var progressEl = document.getElementById('bit-bup-progress-bar');
        var percentEl = document.getElementById('bit-bup-percent');
        var assetsEl = document.getElementById('bit-bup-assets');
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
                    progressEl && (progressEl.style.width = `${percent}%`);
                    percentEl && (percentEl.innerHTML = `${percent}%`);
                    return showLogs ? console.log('resource downloaded:', data) : undefined;
                case 'end':
                    hideApp && appEl && (appEl.style.display = 'block');
                    bupEl && (bupEl.style.display = 'none');
                    return showLogs ? console.log('downloading resources ended.') : undefined;
            }
        }
    };

}());
