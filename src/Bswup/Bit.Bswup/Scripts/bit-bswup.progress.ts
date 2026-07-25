(window as any)['bit-bswup.progress version'] = '10.5.0';

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
    // Resolves a splash element at USE time, with a cache that re-resolves when the cached
    // node has been replaced. Capturing the elements once at start() froze the UI whenever an
    // interactive Blazor render swapped the splash subtree after initialization: the handler
    // kept driving the detached nodes (bar stuck at its last value, the reload button toggled
    // on an orphan) with no observer left to recover. isConnected is undefined on exotic node
    // fakes; only an explicit false (a real node that left the document) triggers
    // re-resolution, and the stale node is kept as a last resort when no replacement exists.
    const elementCache: { [id: string]: any } = {};
    function el(id: string) {
        const cached = elementCache[id];
        if (cached && cached.isConnected !== false) return cached;
        const fresh = document.getElementById(id);
        if (fresh) {
            elementCache[id] = fresh;
            return fresh;
        }
        return cached || null;
    }

    function start(autoReload: boolean,
        showLogs: boolean,
        showAssets: boolean,
        appContainerSelector: string,
        hideApp: boolean,
        autoHide: boolean,
        handler?: string) {

        // Install the global handler FIRST. Everything below touches the DOM, and a bad
        // AppContainer selector makes document.querySelector throw - previously that threw
        // BEFORE window.bitBswupHandler was assigned, so no handler ever registered, the
        // downloadFinished -> reload() handshake never ran, and a first install sat behind
        // the splash until the stall watchdog fired a minute later. The handler closes over
        // bindings initialized below, which is safe: messages arrive as async events and can
        // never run mid-start().
        (window as any).bitBswupHandler = bitBswupHandler;

        // Tolerate an invalid selector instead of aborting initialization: losing the
        // hide-the-app nicety costs a cosmetic overlap; losing the handler costs the boot.
        let appEl: HTMLElement | null = null;
        try {
            appEl = document.querySelector(appContainerSelector) as HTMLElement;
        } catch (err) {
            console.error('BitBswupProgress: invalid appContainer selector - continuing without app hiding:', appContainerSelector, err);
        }

        const appElOriginalDisplay = appEl && appEl.style.display;

        // Sets the reload button visible/wired and announces it. The button is hidden with
        // display:none, which removes it from the accessibility tree entirely - its
        // appearance is never announced on its own - so the always-present visually-hidden
        // role="status" region carries the announcement for screen readers.
        function showReloadButton(reload: any, display: string) {
            const reloadButton = el('bit-bswup-reload');
            const reloadStatusEl = el('bit-bswup-reload-status');
            reloadButton && (reloadButton.style.display = display);
            reloadButton && (reloadButton.onclick = reload);
            reloadStatusEl && (reloadStatusEl.textContent = 'A new version is ready to install.');
        }
        function hideReloadButton() {
            const reloadButton = el('bit-bswup-reload');
            const reloadStatusEl = el('bit-bswup-reload-status');
            if (reloadButton) {
                reloadButton.style.display = 'none';
                reloadButton.onclick = null;
            }
            reloadStatusEl && (reloadStatusEl.textContent = '');
        }

        // Grace period before an auto-reload that has not navigated is assumed stalled and the
        // manual reload button is surfaced as a fallback.
        const AUTO_RELOAD_FALLBACK_MS = 10000;

        // Drives an auto-reload while guaranteeing the user is never left with neither a reload nor
        // a button. On the UPDATE path reload() intentionally NEVER settles - the page is about to
        // navigate away (see bit-bswup.ts) - so relying on reload() rejecting to detect a stall
        // (WAITING_SKIPPED / controllerchange never arriving after SKIP_WAITING) can't work: the
        // .catch would never fire and an autoReload user would sit silently one version behind with
        // no prompt. Arm a timer that runs the caller's onFallback (reveal the manual button, and
        // whatever UI each site needs) if the page has not navigated within the grace period; the
        // SAME onFallback also runs if reload() rejects. A successful reload either unloads the page
        // (discarding the timer) or, on a first install, resolves reload() and clears it - then
        // onSettle runs so the caller can finalize (e.g. hide the splash).
        function autoReloadWithFallback(reload: any, onFallback: () => void, onSettle?: () => void) {
            const timer = setTimeout(onFallback, AUTO_RELOAD_FALLBACK_MS);
            // Promise.resolve tolerates a non-thenable reload() return too.
            Promise.resolve(typeof reload === 'function' ? reload() : undefined).then(() => {
                clearTimeout(timer);
                onSettle && onSettle();
            }).catch(() => {
                clearTimeout(timer);
                onFallback();
            });
        }

        // Resolve the optional user handler lazily rather than capturing window[handler] once
        // here at start(): if the host registers its handler after this script runs (a racey
        // script order), an early capture would bind `undefined` forever and the custom handler
        // would never fire. Re-resolve on each message until a real function is found, then
        // cache it.
        let handlerFn: ((message: any, data: any) => void) | undefined;
        function resolveHandler() {
            if (typeof handlerFn === 'function') return handlerFn;
            const candidate = handler ? window[handler] : undefined;
            // Handler="bitBswupHandler" - the very global this script registers - would make
            // the handler invoke itself until the stack blows, and handleInternal runs FIRST
            // at every depth, so ShowAssets would prepend thousands of duplicate rows per
            // message on the way down. Refuse self-references and cache a no-op so the
            // warning fires once, not per message.
            if (candidate === bitBswupHandler) {
                console.warn('BitBswupProgress: the custom handler resolves to the built-in bitBswupHandler itself - ignoring it (point Handler at a different function).');
                handlerFn = () => { };
                return handlerFn;
            }
            if (typeof candidate === 'function') handlerFn = candidate as (message: any, data: any) => void;
            return handlerFn;
        }

        // The global handler bit-bswup.ts invokes for every lifecycle message. It runs the
        // built-in UI handling first, then forwards to the optional user handler (errors in
        // the user handler are caught so they can't break the splash).
        function bitBswupHandler(message: string, data: any) {
            handleInternal(message, data);

            try {
                resolveHandler()?.(message, data);
            } catch (err) {
                console.error(err);
            }

            function handleInternal(message: string, data: any) {
                const hideApp_ = _config.hideApp ?? hideApp;
                const showLogs_ = _config.showLogs ?? showLogs;
                const autoHide_ = _config.autoHide ?? autoHide;
                const showAssets_ = _config.showAssets ?? showAssets;
                const autoReload_ = _config.autoReload ?? autoReload;

                // Resolved per message, not captured at start() - see el(): an interactive
                // render may have replaced the splash subtree since the previous message.
                const bswupEl = el('bit-bswup');
                const progressEl = el('bit-bswup-progress-bar');
                const percentEl = el('bit-bswup-percent');
                const assetsEl = el('bit-bswup-assets');
                const errorEl = el('bit-bswup-error');
                const errorMessageEl = el('bit-bswup-error-message');
                const errorDetailsEl = el('bit-bswup-error-details');
                const errorRetryButton = el('bit-bswup-error-retry');

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

                    case BswupMessage.downloadProgress: {
                        // Background updates (firstInstall === false) download behind a
                        // healthy running app. Painting the full-viewport splash over it
                        // blocked every click for the entire download - and the overlay root
                        // has no background, so it rendered as stray text on top of the live
                        // UI. The built-in overlay is therefore first-install-only; progress
                        // still reaches the user handler for apps that render their own
                        // indicator, and completion surfaces through the reload button. The
                        // strict === false check keeps the old take-over behavior when the
                        // flag is absent (an older bit-bswup.js still cached alongside this
                        // script).
                        if (data && data.firstInstall === false) {
                            return showLogs_ ? console.log('asset downloaded (background update):', data) : undefined;
                        }

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

                        // Guard against a non-finite percent: Math.round(undefined) is NaN, which
                        // would emit an invalid aria-valuenow="NaN" (assistive tech mis-announces
                        // it), a "NaN%" CSS var, and visible "NaN%" text. Clamp to a valid 0-100.
                        const rawPercent = Number(data.percent);
                        const percent = Number.isFinite(rawPercent) ? Math.max(0, Math.min(100, Math.round(rawPercent))) : 0;
                        const perStr = `${percent}%`;
                        bswupEl && bswupEl.style.setProperty('--bit-bswup-percent', perStr)
                        bswupEl && bswupEl.style.setProperty('--bit-bswup-percent-text', `"${perStr}"`)
                        progressEl && (progressEl.style.width = `${percent}%`);
                        // Keep the ARIA value in sync with the visual bar so assistive
                        // technology announces progress, not just a static 0%.
                        progressEl && progressEl.setAttribute('aria-valuenow', String(percent));
                        percentEl && (percentEl.textContent = `${percent}%`);
                        return showLogs_ ? console.log('asset downloaded:', data) : undefined;
                    }

                    case BswupMessage.downloadFinished:
                        if (autoHide_) {
                            hideApp_ && appEl && (appEl.style.display = appElOriginalDisplay);
                            bswupEl && (bswupEl.style.display = 'none');
                        }

                        if (autoReload_ || data.firstInstall) {
                            // On a stall (reload never navigates) or a reject, restore the splash
                            // and offer a manual retry wired to data.reload - the original
                            // reject-recovery behavior, now also covering a silent stall. On a
                            // clean resolve (first install completing) hide the splash instead.
                            autoReloadWithFallback(data.reload, () => {
                                bswupEl && (bswupEl.style.display = 'block');
                                showReloadButton(data.reload, 'block');
                            }, () => {
                                hideApp_ && appEl && (appEl.style.display = appElOriginalDisplay);
                                bswupEl && (bswupEl.style.display = 'none');
                            });
                        } else {
                            // The button lives OUTSIDE #bit-bswup (see BswupProgress.razor)
                            // precisely so this works without revealing the whole overlay
                            // over a running app.
                            showReloadButton(data.reload, 'block');
                        }
                        return showLogs_ ? console.log('downloading assets finished.') : undefined;

                    case BswupMessage.updateReady:
                        if (autoReload_) {
                            // Fall back to the manual reload button if the auto-reload rejects OR
                            // stalls without navigating - the update path's reload() never settles,
                            // so a silently-stalled skipWaiting would otherwise leave no prompt. No
                            // splash reveal here: an update runs behind a healthy app (the button
                            // lives outside #bit-bswup).
                            autoReloadWithFallback(data.reload, () => showReloadButton(data.reload, 'inline'));
                        } else {
                            // Shown without touching #bit-bswup: when an update is already
                            // staged at page load no progress event ever revealed the overlay,
                            // and unhiding a button inside a display:none parent rendered
                            // nothing - the user was never told an update was ready.
                            showReloadButton(data.reload, 'inline');
                        }
                        return showLogs_ ? console.log('new update is ready.') : undefined;

                    case BswupMessage.error:
                        // Always log errors regardless of showLogs - this is actionable info.
                        console.error('BitBswup install error:', data);

                        // Non-fatal failures (the default 'lax' tolerance) do not stop the
                        // install: the asset is skipped now and lazily fetched on first use. An
                        // optional externalAssets entry that 404s is the common case. Replacing
                        // a live progress bar with "Update failed to install" for something the
                        // app recovers from on its own would be actively misleading, so report
                        // it to the console (above) and to the user handler, and leave the
                        // splash alone. Only a fatal error - an invalid manifest, or a
                        // 'strict' abort - takes over the UI below.
                        if (data && data.fatal === false) return;

                        // A fatal failure during a background *update* must not hijack the UI
                        // either: the previous worker keeps serving and the running app is
                        // perfectly healthy - the only thing that failed is staging a new
                        // version. Covering a mid-session app with a failure panel (and, with
                        // hideApp, hiding the app container) would read as the app itself
                        // breaking. Instead, tear down any download UI the progress messages
                        // revealed (otherwise the splash would sit frozen at its last percent)
                        // and unwire the reload button so nothing invites activating the failed
                        // update; the failure stays on the console (above) and in the user
                        // handler. The strict `=== false` check keeps the old take-over
                        // behavior when the flag is absent (an older bit-bswup.js still cached
                        // alongside this script), and the failure panel below remains the
                        // first-install behavior, where nothing is running yet and the splash
                        // is the whole UI.
                        if (data && data.firstInstall === false) {
                            hideApp_ && appEl && (appEl.style.display = appElOriginalDisplay);
                            bswupEl && (bswupEl.style.display = 'none');
                            hideReloadButton();
                            return;
                        }

                        // Reveal the install panel even if no progress event landed first
                        // (manifest validation failures fire before any progress message).
                        hideApp_ && appEl && (appEl.style.display = 'none');
                        bswupEl && (bswupEl.style.display = 'block');

                        // A failed install supersedes any earlier "update ready" prompt. Leaving
                        // the reload button visible would invite the user to activate an update
                        // that has already failed, promoting a broken worker / caches. Hide and
                        // unwire it so the only actionable control is the (conditional) Retry.
                        hideReloadButton();

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
                                // 'install-aborted' is deliberately absent: a strict abort is
                                // usually triggered by a transient asset failure, and a reload
                                // genuinely can succeed on the next attempt.
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
                        return;
                }
            }
        }

        // Initialization completed: window.bitBswupHandler is installed and the splash is
        // wired up. Mark the element now (not in autoStart() before start() ran) so the flag
        // is only set after a successful start - a start() that threw can be retried - and so
        // manual BitBswupProgress.start(...) callers are tracked too, keeping the
        // DOMContentLoaded/MutationObserver paths from re-initializing.
        const initializedEl = el('bit-bswup');
        initializedEl && initializedEl.setAttribute('data-bit-bswup-initialized', 'true');
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

        const bool = (name: string, fallback: boolean) => {
            const value = el.getAttribute(name);
            return value == null ? fallback : value === 'true';
        };

        const handlerAttr = el.getAttribute('data-bit-bswup-handler');

        start(
            // The fallback only applies to hand-written config markup that omits the
            // attribute (the Razor component always renders it); it must track the
            // component's AutoReload default - false since v-10-5-0, see BswupProgress.razor.
            bool('data-bit-bswup-auto-reload', false),
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

    // How long to keep watching for a late-rendered #bit-bswup before giving up. The observer
    // below is subtree-wide on documentElement, and a Blazor app mutates the DOM continuously
    // for its entire lifetime, so leaving it attached forever means the browser keeps queueing
    // and delivering mutation records for a page that is never going to produce the element.
    // The window only has to cover app startup (the element is either in the host document from
    // the start, or injected when the app first renders); a minute is far beyond that even on a
    // slow first WebAssembly download, and anything later can still call
    // BitBswupProgress.start(...) directly.
    const OBSERVE_TIMEOUT = 60000;

    // The splash element can also appear *after* load - e.g. an interactive Blazor render
    // or any host that injects #bit-bswup once the app is mounted. A one-shot autoStart()
    // would have already returned (element missing) and never run again, so window
    // .bitBswupHandler would never be installed and BitBswupProgress would stay dark. Watch
    // for the element with a MutationObserver and initialize once it shows up; the
    // data-bit-bswup-initialized guard inside autoStart() keeps this from clashing with the
    // DOMContentLoaded/immediate path above.
    //
    // Disconnecting promptly matters as much as connecting: every path out of "there is still
    // something to wait for" tears the observer down, and a timer bounds the case where the
    // element never appears at all (an app that loads this script but never renders
    // BswupProgress).
    if (typeof MutationObserver !== 'undefined') {
        let observer: MutationObserver | undefined;
        let timeoutId: ReturnType<typeof setTimeout> | undefined;

        const stopObserving = () => {
            observer?.disconnect();
            observer = undefined;
            if (timeoutId !== undefined) {
                clearTimeout(timeoutId);
                timeoutId = undefined;
            }
        };

        observer = new MutationObserver(() => {
            const el = document.getElementById('bit-bswup');

            // Not rendered yet - this is the one case where we keep waiting.
            if (!el) return;

            if (el.getAttribute('data-bit-bswup-initialized') === 'true') return stopObserving();

            // An #bit-bswup that carries no config attributes is markup we don't own - a
            // hand-written splash driven by an explicit BitBswupProgress.start(...) call.
            // autoStart() declines it by design and always will, so there is nothing left to
            // watch for; staying attached would just burn a callback on every DOM mutation.
            if (el.getAttribute('data-bit-bswup-config') !== 'true') return stopObserving();

            autoStart();

            // Stop once initialization took hold. If start() threw, the flag is still unset and
            // we stay attached so the next mutation can retry.
            if (el.getAttribute('data-bit-bswup-initialized') === 'true') stopObserving();
        });

        const startObserving = () => {
            // autoStart() may have already succeeded on the DOMContentLoaded/immediate path, in
            // which case there is nothing to observe for in the first place.
            const el = document.getElementById('bit-bswup');
            if (el && el.getAttribute('data-bit-bswup-initialized') === 'true') return stopObserving();

            observer?.observe(document.documentElement, { childList: true, subtree: true });
            timeoutId = setTimeout(stopObserving, OBSERVE_TIMEOUT);
        };

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', startObserving);
        } else {
            startObserving();
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
