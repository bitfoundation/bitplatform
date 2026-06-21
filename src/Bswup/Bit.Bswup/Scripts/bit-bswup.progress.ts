window['bit-bswup.progress version'] = '10.5.0-pre-02';

// Default progress/splash UI for Bswup. This script registers the global
// `bitBswupHandler` that bit-bswup.ts calls with every BswupMessage, and drives the
// built-in splash markup (progress bar, percentage, asset log, reload/retry buttons,
// error panel) rendered by BswupProgress.razor. Apps can layer their own behavior by
// passing a custom `handler` name, which is invoked after the built-in handling.
(function () {
    // Live config overrides applied via BitBswupProgress.config(); each value, when set,
    // takes precedence over the corresponding argument passed to start().
    const _config: IBswupProgressConfigs = {};

    (window as any).BitBswupProgress = {
        start,
        config
    };

    // Initializes the splash UI and installs the message handler. Called once from the
    // generated startup markup with the app's display preferences:
    //   autoReload            - reload automatically when an update finishes, instead of
    //                           showing a manual reload button
    //   showLogs              - console.log lifecycle messages
    //   showAssets            - list each downloaded asset in the UI
    //   appContainerSelector  - element whose visibility is toggled while installing
    //   hideApp               - hide the app element during download
    //   autoHide              - hide the splash automatically when the download finishes
    //   handler               - optional name of a user handler invoked after the built-in one
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

        // The global handler bit-bswup.ts invokes for every lifecycle message. It runs the
        // built-in UI handling first, then forwards to the optional user handler (errors in
        // the user handler are caught so they can't break the splash).
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

                        if (showAssets_ && assetsEl && data.asset) {
                            // Build the row with the DOM API and textContent rather than
                            // innerHTML. The url/hash come from the asset manifest (build
                            // output), but assigning them via innerHTML would still treat any
                            // markup-like characters as HTML; textContent guarantees they are
                            // rendered as literal text, closing the injection surface for free.
                            const li = document.createElement('li');
                            const urlEl = document.createElement('b');
                            urlEl.textContent = data.asset.url;
                            li.append(`${data.index}: `, urlEl, `: ${data.asset.hash}`);
                            assetsEl.prepend(li);
                        }

                        const percent = Math.round(data.percent);
                        const perStr = `${percent}%`;
                        bswupEl && bswupEl.style.setProperty('--bit-bswup-percent', perStr)
                        bswupEl && bswupEl.style.setProperty('--bit-bswup-percent-text', `"${perStr}"`)
                        progressEl && (progressEl.style.width = `${percent}%`);
                        // Keep the ARIA value in sync with the visual bar so assistive
                        // technology announces progress, not just a static 0%.
                        progressEl && progressEl.setAttribute('aria-valuenow', String(percent));
                        percentEl && (percentEl.textContent = `${percent}%`);
                        return showLogs_ ? console.log('asset downloaded:', data) : undefined;

                    case BswupMessage.downloadFinished:
                        if (autoHide_) {
                            hideApp_ && appEl && (appEl.style.display = appElOriginalDisplay);
                            bswupEl && (bswupEl.style.display = 'none');
                        }

                        if (autoReload_ || data.firstInstall) {
                            data.reload().then(() => {
                                hideApp_ && appEl && (appEl.style.display = appElOriginalDisplay);
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

                        // A failed install supersedes any earlier "update ready" prompt. Leaving
                        // the reload button visible would invite the user to activate an update
                        // that has already failed, promoting a broken worker / caches. Hide and
                        // unwire it so the only actionable control is the (conditional) Retry.
                        if (reloadButton) {
                            reloadButton.style.display = 'none';
                            reloadButton.onclick = null;
                        }

                        // The error supersedes any in-flight progress. Hide the bar and the
                        // percentage so a stale partial value (e.g. "47%") isn't left sitting
                        // next to the failure message.
                        if (progressEl && progressEl.parentElement) progressEl.parentElement.style.display = 'none';
                        if (percentEl) percentEl.style.display = 'none';

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
                                // Some failures are deterministic - a plain reload re-fetches the
                                // same broken bytes and fails identically. A manifest that won't
                                // parse or an SRI/integrity mismatch needs a redeploy (or fixed
                                // CDN/proxy), not a retry. For those, hide the retry button so we
                                // don't invite a pointless reload loop; keep it for transient
                                // failures (network/fetch/cache) where reloading can genuinely help.
                                const nonRetriableReasons = ['manifest', 'integrity', 'install-incomplete'];
                                const isRetriable = !(data && nonRetriableReasons.indexOf(data.reason) !== -1);
                                if (isRetriable) {
                                    errorRetryButton.style.display = 'inline-block';
                                    errorRetryButton.onclick = () => {
                                        if (data && typeof data.reload === 'function') {
                                            data.reload();
                                        } else {
                                            window.location.reload();
                                        }
                                    };
                                } else {
                                    errorRetryButton.style.display = 'none';
                                    errorRetryButton.onclick = null;
                                }
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

    // Self-initialize from the data-* attributes rendered by the BswupProgress Razor
    // component. This replaces the inline <script> the component used to emit: an external
    // script reading the DOM is allowed under a strict Content-Security-Policy (script-src
    // 'self') and runs regardless of how the host page is rendered. Calling start() manually
    // is still supported - the data-bit-bswup-initialized guard keeps the two from clashing.
    function autoStart() {
        const el = document.getElementById('bit-bswup');
        if (!el || el.getAttribute('data-bit-bswup-config') !== 'true') return;
        if (el.getAttribute('data-bit-bswup-initialized') === 'true') return;
        el.setAttribute('data-bit-bswup-initialized', 'true');

        const bool = (name: string, fallback: boolean) => {
            const value = el.getAttribute(name);
            return value == null ? fallback : value === 'true';
        };

        const handlerAttr = el.getAttribute('data-bit-bswup-handler');

        start(
            bool('data-bit-bswup-auto-reload', true),
            bool('data-bit-bswup-show-logs', false),
            bool('data-bit-bswup-show-assets', false),
            el.getAttribute('data-bit-bswup-app-container') || '#app',
            bool('data-bit-bswup-hide-app', false),
            bool('data-bit-bswup-auto-hide', false),
            handlerAttr || undefined
        );
    }

    // The component element may be parsed after this script (it is rendered later in the
    // body), so defer to DOMContentLoaded when the document is still loading; otherwise the
    // element already exists and we can initialize immediately.
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', autoStart);
    } else {
        autoStart();
    }
}());

interface IBswupProgressConfigs {
    autoReload?: boolean | undefined;
    showLogs?: boolean | undefined;
    showAssets?: boolean | undefined;
    hideApp?: boolean | undefined;
    autoHide?: boolean | undefined;
};
