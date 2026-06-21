var BitBswup = BitBswup || {};
BitBswup.version = window['bit-bswup version'] = '10.5.0-pre-02';

// Idempotency guard. bit-bswup.js wires up a DOMContentLoaded handler (and through it
// the service-worker registration, event listeners, update timers and reload handlers)
// and assigns the public BitBswup.* API - all as side effects that run the moment the
// script is parsed. If the script is included more than once (e.g. a stray duplicate
// <script src="...bit-bswup.js"> tag, or a second injection after startup) every one of
// those would run again: duplicate listeners, a second polling timer, double reloads,
// and the load-time `checkForUpdate` fallback clobbering the registration-aware version
// installed by the first load. Run the setup exactly once; later inclusions are a no-op.
if (!BitBswup.initialized) {
    BitBswup.initialized = true;

    (function () {
        // Level ordering (lowest priority first). A message is logged when its
        // level is at or below the configured threshold. `none` silences everything.
        const logLevels: { [k: string]: number } = {
            none: 0,
            error: 1,
            warn: 2,
            info: 3,
            verbose: 4,
            debug: 5,
        };

        // Default Blazor entry-point scripts to auto-detect when `blazorScript` is
        // not set explicitly. Covers both the .NET 8+ Blazor Web App template
        // (blazor.web.js) and the standalone Blazor WebAssembly template
        // (blazor.webassembly.js), so the same setup works without extra config.
        const defaultBlazorScripts = ['_framework/blazor.web.js', '_framework/blazor.webassembly.js'];

        // document.currentScript is only set while a *classic* script is executing
        // synchronously during initial parse. It is null for module scripts, async/deferred
        // execution, or when bit-bswup.js is injected dynamically after load - in which case
        // reading bitBswupScript.attributes later would throw. Fall back to locating the
        // script tag by src so attribute-based configuration still works, and tolerate its
        // absence (extract() guards every attribute read against a null element).
        const bitBswupScript = document.currentScript
            || (function () {
                const scripts = [].slice.call(document.scripts) as HTMLScriptElement[];
                return scripts.find(s => s.src && s.src.indexOf('bit-bswup.js') !== -1) || null;
            }());

        if (document.readyState === 'loading') {
            window.addEventListener('DOMContentLoaded', runBswup); // important event!
        } else {
            runBswup();
        }

        function runBswup() {
            const options = extract();

            info('starting...');

            if (!('serviceWorker' in navigator)) {
                startBlazor(true);
                return warn('no serviceWorker in navigator');
            }

            startBlazor();

            let reload: () => Promise<void>;
            let cleanup: () => void;
            let blazorStartResolver: (value: unknown) => void;

            // Captured once the registration resolves so the polling helpers (timer /
            // visibilitychange) and the page-facing BitBswup.checkForUpdate() can all call
            // reg.update() against the same registration without re-resolving it each time.
            let registration: ServiceWorkerRegistration;
            let updateTimer: ReturnType<typeof setInterval>;

            // Guards against reloading more than once. A single update can surface through
            // several channels (the 'WAITING_SKIPPED' message to the initiating tab and a
            // 'controllerchange' in every tab once the new worker claims clients); they all
            // funnel through reloadOnce() so the page navigates exactly one time.
            let refreshing = false;

            // Snapshot of "was an active worker already present when registration resolved".
            // This is the stable signal for first-install vs update. Reading
            // navigator.serviceWorker.controller at message time is NOT reliable: controller
            // is null whenever the current navigation wasn't served by the SW - most notably
            // on a hard reload (Ctrl+Shift+R) - even when an active worker exists. Using that
            // as the discriminator makes Bswup mistake every hard reload for a first install.
            let hadActiveWorkerAtStartup = false;

            try {
                navigator.serviceWorker
                    .register(options.sw, { scope: options.scope, updateViaCache: 'none' })
                    .then(prepareRegistration)
                    .catch((err) => {
                        startBlazor(true);
                        error('serviceWorker register promise failed', err);
                    });
                navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
                navigator.serviceWorker.addEventListener('message', handleMessage);
            } catch (e) {
                startBlazor(true);
                error('serviceWorker registration failed', e);
            }

            function prepareRegistration(reg) {
                // Capture the install/update discriminator exactly once, at the moment the
                // registration resolves. reg.active being set here means a previous version
                // was already installed => this is an update; otherwise it's a first install.
                hadActiveWorkerAtStartup = !!reg.active;

                // Keep the resolved registration around so checkForUpdate() (page API) and the
                // optional polling helpers can drive reg.update() without re-resolving it.
                registration = reg;
                setupUpdatePolling(reg);

                // Replace the load-time fallback (which re-resolves the registration on every
                // call and can't report results) with the registration-aware implementation
                // now that we have a live registration to work against.
                BitBswup.checkForUpdate = checkForUpdate;

                reload = () => {
                    // An update is staged (a new worker finished installing and is waiting).
                    // Tell it to skip waiting; the resulting 'WAITING_SKIPPED' message triggers
                    // the page reload. We deliberately keep the returned promise *pending*: the
                    // page is about to navigate away, so resolving early would let callers run
                    // teardown (e.g. hiding the splash) against a page that's already reloading.
                    if (reg.waiting) {
                        reg.waiting.postMessage('SKIP_WAITING');
                        return new Promise<void>(() => { });
                    }

                    // First install: a worker is active but not yet controlling this page.
                    // Ask it to claim clients; once 'CLIENTS_CLAIMED' arrives we start Blazor
                    // and resolve this promise so callers can finalize (e.g. hide the splash).
                    if (reg.active) {
                        reg.active.postMessage('CLAIM_CLIENTS');
                        return new Promise<void>((res) => blazorStartResolver = res as (value: unknown) => void);
                    }

                    // No worker to coordinate with - fall back to a plain reload.
                    window.location.reload();
                    return new Promise<void>(() => { });
                };

                cleanup = () => {
                    // Stop the opt-in update-poll timer so it doesn't keep calling
                    // reg.update() after teardown (it was previously left running for the
                    // life of the page with no way to clear it).
                    if (updateTimer) {
                        clearInterval(updateTimer);
                        updateTimer = undefined as any;
                    }
                    reg.waiting?.postMessage('CLEAN_UP');
                    reg.active?.postMessage('CLEAN_UP');
                };

                // The page can be loaded without a controlling service worker even though
                // a registration already exists - most notably on a hard reload (Ctrl+F5 /
                // Shift+Reload), which bypasses the SW for the navigation request. In that
                // case no install/activate runs, no controllerchange fires, and the normal
                // startBlazor() call above is a no-op (controller is null), so Blazor would
                // never start. If the SW is already active we can safely force-start Blazor
                // here; assets are served from network for this page and the SW will keep
                // the cache fresh in the background.
                if (!navigator.serviceWorker.controller && reg.active && !reg.installing) {
                    info('uncontrolled page with active registration (e.g. hard reload) - force starting Blazor.');
                    startBlazor(true);
                }

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
                        debug('state changed', e, 'eventPhase:', e.eventPhase, 'currentTarget.state:', e.currentTarget.state);
                        handle(BswupMessage.stateChanged, e);

                        if (!reg.waiting) return;

                        if (hadActiveWorkerAtStartup) {
                            info('update finished.'); // not first install
                        } else {
                            info('initialization finished.'); // first install
                        }

                        // Notify listeners that an update is staged and ready. The
                        // registration-time check only fires updateReady for updates already
                        // waiting on load; updates discovered in the same session surface here
                        // instead, so emit it for them too.
                        handle(BswupMessage.updateReady, { reload });
                    });
                });
            }

            function handleControllerChange(e) {
                info('controller changed.', e);

                // A new service worker has taken control of this page. This fires in three
                // situations:
                //   1. This tab triggered the update (clicked "reload") - handleMessage already
                //      reloads on 'WAITING_SKIPPED', so reloadOnce() here is a harmless no-op.
                //   2. First install, where we deliberately claim clients to start Blazor. In
                //      that case there was no previously-controlling worker, so we must NOT
                //      reload (doing so would refresh the splash mid-startup).
                //   3. A *sibling* tab accepted an update: its worker called skipWaiting and
                //      claimed every client, so this tab is now controlled by a newer worker
                //      while still running the old app code and (more dangerously) the old
                //      worker's cache has been swapped out underneath it. Mixing old app JS
                //      with new-version assets corrupts boot config / DLL hashes, so this tab
                //      must reload to re-sync. See "Stuff I wish I'd known about service
                //      workers" on the controllerchange reload pattern.
                //
                // We distinguish case 2 from case 3 with hadActiveWorkerAtStartup: a controller
                // change only signals a real *update* when a worker was already active when this
                // page started. First install never had one, so we skip the reload there.
                if (!hadActiveWorkerAtStartup) {
                    info('controller changed on first install - not reloading.');
                    return;
                }

                reloadOnce();
            }

            // Reload the page exactly once. Multiple update signals (the initiating tab's
            // 'WAITING_SKIPPED' message and the 'controllerchange' event raised in every tab)
            // can race; this guard ensures only the first one wins so the page doesn't reload
            // repeatedly.
            function reloadOnce() {
                if (refreshing) return;
                refreshing = true;
                window.location.reload();
            }

            function handleMessage(e) {
                if (e.data === 'START_BLAZOR') {
                    startBlazor(true);
                    return;
                }

                if (e.data === 'WAITING_SKIPPED') {
                    // The worker we asked to skip waiting has activated. Reload to pick up the
                    // new version. reloadOnce() coordinates with the 'controllerchange' that
                    // also fires once the new worker claims this client, so we reload only once.
                    reloadOnce();
                    return;
                }

                if (e.data === 'CLIENTS_CLAIMED') {
                    // First-install claim succeeded. Start Blazor through the guarded path
                    // (script-tag/autostart checks, single-start, missing-global protection)
                    // instead of calling Blazor.start() directly. Capture e.source up front
                    // because it can be nulled out by the time the start promise settles.
                    const source = e.source;
                    const startPromise = startBlazor(true);

                    const onStarted = () => {
                        blazorStartResolver?.(undefined);
                        source?.postMessage('BLAZOR_STARTED');
                    };

                    if (startPromise) {
                        startPromise.then(onStarted);
                    } else {
                        // Blazor couldn't be started (missing/misconfigured script, or the
                        // global isn't available). Still resolve the reload() promise so the
                        // page UI (e.g. the splash) doesn't hang waiting forever, and notify the
                        // worker so its post-start cache top-up can proceed.
                        onStarted();
                    }
                    return;
                }

                if (e.data === 'UNREGISTER') {
                    navigator.serviceWorker.getRegistrations().then(regs => {
                        const regPromises = regs.map(r => r.unregister());
                        Promise.all(regPromises).then(() => window.location.reload());
                    });
                    return;
                }

                // Everything past the known string commands above is expected to be a
                // JSON-encoded status message from the service worker (sendMessage stringifies
                // objects). Other senders - browser extensions, unrelated workers, or future
                // protocol additions - can post arbitrary payloads, and JSON.parse would throw
                // on a non-JSON string (or a non-string value), aborting this handler. Parse
                // defensively and ignore anything we don't recognize instead of throwing.
                let message: any;
                try {
                    message = JSON.parse(e.data);
                } catch {
                    verbose('ignoring non-JSON service worker message:', e.data);
                    return;
                }

                if (!message || typeof message !== 'object') {
                    verbose('ignoring unexpected service worker message shape:', message);
                    return;
                }

                const { type, data } = message;

                if (type === 'install') {
                    handle(BswupMessage.downloadStarted, data);
                }

                if (type === 'progress') {
                    handle(BswupMessage.downloadProgress, data);

                    if (data.percent >= 100) {
                        const firstInstall = !hadActiveWorkerAtStartup;
                        handle(BswupMessage.downloadFinished, { reload, cleanup, firstInstall });
                    }
                }

                if (type === 'error') {
                    error('install error:', data);
                    handle(BswupMessage.error, { ...data, reload });
                }

                if (type === 'bypass') {
                    const firstInstall = data?.firstTime || !hadActiveWorkerAtStartup;
                    handle(BswupMessage.downloadFinished, { reload, cleanup, firstInstall });
                }

                if (type === 'activate') {
                    handle(BswupMessage.activate, data);
                }
            }

            // ============================================================

            // Opt-in update polling. The browser only re-checks the service worker script on
            // navigation and roughly every 24h, so a long-lived SPA tab can run a stale version
            // for a long time. When configured, we proactively call reg.update() on a timer
            // and/or whenever the tab returns to the foreground. This only *checks*; if a new
            // version is found the normal install flow (updatefound -> updateReady) takes over.
            function setupUpdatePolling(reg: ServiceWorkerRegistration) {
                const intervalSeconds = Number(options.updateInterval) || 0;
                if (intervalSeconds > 0) {
                    info(`update polling enabled - every ${intervalSeconds}s.`);
                    updateTimer = setInterval(() => {
                        // Skip background tabs: browsers heavily throttle their timers and the
                        // request would be wasted. The visibilitychange check below catches up
                        // the moment the tab is focused again.
                        if (document.visibilityState !== 'visible') {
                            verbose('update poll tick skipped - tab not visible.');
                            return;
                        }
                        verbose('update poll tick - checking for update.');
                        checkForUpdate();
                    }, intervalSeconds * 1000);
                }

                if (options.updateOnVisibility) {
                    info('update-on-visibility enabled.');
                    document.addEventListener('visibilitychange', () => {
                        if (document.visibilityState === 'visible') {
                            verbose('tab became visible - checking for update.');
                            checkForUpdate();
                        }
                    });
                }
            }

            // Registration-aware update check used by the timer, the visibility handler, and
            // the page-facing BitBswup.checkForUpdate(). Unlike the load-time fallback it can
            // report the outcome: if nothing new is staged after the check it emits
            // updateNotFound so callers can stop a spinner / show an "up to date" message.
            async function checkForUpdate(): Promise<void> {
                if (!registration) {
                    warn('checkForUpdate called before the service worker registration was ready.');
                    return;
                }

                info('checking for update...');

                try {
                    await registration.update();

                    // reg.update() resolves once the server has responded and the byte-compare
                    // is done, but the browser does not necessarily set reg.installing (nor fire
                    // 'updatefound') synchronously on the same microtask. Reading it immediately
                    // can therefore report "no update" even while an install is about to start,
                    // producing a spurious updateNotFound that races the updatefound event. Yield
                    // a macrotask first so a freshly-found worker has a chance to surface.
                    await new Promise(resolve => setTimeout(resolve, 0));

                    // A new worker installing/waiting means an update was found; the existing
                    // 'updatefound' listener already drives updateFound/stateChanged/updateReady.
                    // Nothing installing or waiting means we're already on the latest version,
                    // which is exactly the "finished, found nothing" case the page can't infer
                    // on its own - so announce it explicitly.
                    if (!registration.installing && !registration.waiting) {
                        info('no update found.');
                        handle(BswupMessage.updateNotFound);
                    }
                } catch (err) {
                    error('checkForUpdate failed', err);
                    // A failed registration.update() is a transient, non-fatal condition (offline,
                    // server hiccup, throttled background tab) - the app is already running fine.
                    // Emit a non-blocking updateCheckFailed message rather than the install-path
                    // 'error', so the default progress handler doesn't hide the app or show the
                    // install-failed UI. The payload still carries the reason/message so callers
                    // that care can react.
                    handle(BswupMessage.updateCheckFailed, { reason: 'update', message: String((err && (err as any).message) || err), reload });
                }
            }

            // ============================================================

            // Tracks the single Blazor.start() invocation. A first install (CLIENTS_CLAIMED),
            // a controlled/hard-reload load, and the explicit force paths can all reach the
            // start logic; Blazor.start() may only be called once (a second call rejects), so
            // funnel every path through startBlazorCore() and remember the in-flight/settled
            // promise to hand back to later callers.
            let blazorStarted = false;
            let blazorStartPromise: Promise<unknown> | undefined;

            // Actually start Blazor, exactly once. Returns the start promise (or the existing
            // one on repeat calls), or undefined when Blazor is unavailable so callers can
            // decide how to proceed instead of crashing on a missing global.
            function startBlazorCore(): Promise<unknown> | undefined {
                if (blazorStarted) return blazorStartPromise;

                // `Blazor` is a declared global, but it only exists once the Blazor script has
                // loaded. typeof guards against a ReferenceError if we're invoked too early or
                // the script is missing/misconfigured.
                if (typeof Blazor === 'undefined' || typeof Blazor.start !== 'function') {
                    error('Blazor.start is not available - cannot start Blazor (is the Blazor script loaded?).');
                    return undefined;
                }

                blazorStarted = true;
                try {
                    // Normalize to a real Promise so callers can always .then() the result even
                    // if a future Blazor returns something non-thenable.
                    blazorStartPromise = Promise.resolve(Blazor.start());
                } catch (err) {
                    blazorStarted = false;
                    error('Blazor.start() threw', err);
                    return undefined;
                }
                return blazorStartPromise;
            }

            function startBlazor(forceStart = false): Promise<unknown> | undefined {
                const scriptTags = [].slice.call(document.scripts);

                // `blazorScript` may be a single path (explicitly configured) or a list
                // of candidates to auto-detect. Normalize to an array and match the first
                // script tag whose src contains any of the candidates.
                const candidates = Array.isArray(options.blazorScript) ? options.blazorScript : [options.blazorScript];

                const blazorWasmScriptTag = scriptTags.find(s => s.src && candidates.some(c => s.src.indexOf(c) !== -1));
                if (!blazorWasmScriptTag) {
                    warn(`blazor script (${candidates.join(' or ')}) not found!`);
                    return undefined;
                }

                const autostart = blazorWasmScriptTag.attributes['autostart'];
                if (!autostart || autostart.value !== 'false') {
                    warn('no "autostart=false" found on the blazor script tag!');
                    return undefined;
                }

                if (forceStart || navigator?.serviceWorker?.controller) {
                    return startBlazorCore();
                }

                return undefined;
            }

            function extract(): BswupOptions {
                const defaultoptions = {
                    scope: '/',
                    log: 'warn',
                    sw: 'service-worker.js',
                    handlerName: 'bitBswupHandler',
                    blazorScript: defaultBlazorScripts,
                }

                // bitBswupScript may be null (see the currentScript fallback above) when the
                // script can't be located - e.g. dynamic injection. Use an empty attribute bag
                // in that case so every read below safely yields undefined and the defaults /
                // window[optionsName] config still apply.
                const attrs: any = (bitBswupScript && bitBswupScript.attributes) || {};

                const optionsAttribute = attrs['options'];
                const optionsName = (optionsAttribute || {}).value || 'bitBswup';
                const options = Object.assign({}, defaultoptions, window[optionsName]) as BswupOptions;

                const logAttribute = attrs['log'];
                options.log = (logAttribute && logAttribute.value) || options.log;

                const swAttribute = attrs['sw'];
                options.sw = (swAttribute && swAttribute.value) || options.sw;

                const scopeAttribute = attrs['scope'];
                options.scope = (scopeAttribute && scopeAttribute.value) || options.scope;

                const handlerAttribute = attrs['handler'];
                options.handlerName = (handlerAttribute && handlerAttribute.value) || options.handlerName;

                const blazorScriptAttribute = attrs['blazorScript'];
                options.blazorScript = (blazorScriptAttribute && blazorScriptAttribute.value) || options.blazorScript || defaultBlazorScripts;

                // Polling is opt-in: absent attributes leave the options untouched so the
                // default (no timer, no visibility check) is preserved.
                const updateIntervalAttribute = attrs['updateInterval'];
                if (updateIntervalAttribute) options.updateInterval = Number(updateIntervalAttribute.value);

                const updateOnVisibilityAttribute = attrs['updateOnVisibility'];
                if (updateOnVisibilityAttribute) options.updateOnVisibility = updateOnVisibilityAttribute.value === 'true';

                return options;
            }

            function handle(...args: any[]) {
                // Resolve the handler from window[handlerName] on every call until a real
                // function is found, then cache it. Caching a no-op fallback (the old
                // behavior) permanently disabled the handler whenever the very first Bswup
                // event fired before bit-bswup.progress.js had assigned window.bitBswupHandler
                // - a load-order race between the two scripts. Re-resolving each time the
                // handler is still missing lets a late-registered handler take effect.
                if (!options.handler || typeof options.handler !== 'function') {
                    const resolved = window[options.handlerName];
                    if (typeof resolved === 'function') {
                        options.handler = resolved;
                    } else {
                        warn('progress handler not found or is not a function!');
                        return;
                    }
                }

                options.handler(...args);
            }

            function shouldLog(level: 'error' | 'warn' | 'info' | 'verbose' | 'debug'): boolean {
                // Normalize the configured level so values like `Info` or `WARN` still match the
                // lowercase logLevels keys instead of silently falling back to the default.
                const configured = logLevels[String(options.log).toLowerCase()];
                // Unknown values fall back to `warn` (matches the documented default behavior).
                const threshold = configured == null ? logLevels.warn : configured;
                return logLevels[level] <= threshold;
            }

            function error(...args: any[]) {
                if (!shouldLog('error')) return;
                console.error(...['BitBswup:', ...args]);
            }

            function warn(...args: any[]) {
                if (!shouldLog('warn')) return;
                console.warn(...['BitBswup:', ...args]);
            }

            function info(...args: any[]) {
                if (!shouldLog('info')) return;
                console.info(...['BitBswup:', ...args]);
            }

            function verbose(...args: any[]) {
                if (!shouldLog('verbose')) return;
                console.log(...['BitBswup:', ...args]);
            }

            function debug(...args: any[]) {
                if (!shouldLog('debug')) return;
                console.debug(...['BitBswup:', ...args]);
            }
        }
    }());

    // Load-time fallback. This is replaced by the registration-aware implementation (which
    // can report updateNotFound) once runBswup resolves the service worker registration. It
    // stays as the public entry point so the API is callable even before registration
    // completes, and on browsers without service worker support.
    BitBswup.checkForUpdate = async (): Promise<void> => {
        if (!('serviceWorker' in navigator)) {
            return console.warn('no serviceWorker in navigator');
        }

        const reg = await navigator.serviceWorker.getRegistration();
        await reg?.update();
    }

    // `forceRefresh` is the last-resort "reset" when a client is wedged. Because it is a full
    // reset it now clears *every* CacheStorage bucket by default - not just the Bswup and
    // Blazor framework caches - so app-owned caches (Workbox add-ons, app-data, third-party
    // API caches, etc.) can't survive and re-poison the freshly reloaded app. Callers that
    // need to be selective can pass a filter:
    //   - string:   prefix match against the cache name (e.g. 'bit-bswup')
    //   - RegExp:   tested against the cache name
    //   - function: predicate receiving the cache name, return true to delete
    // Anything else (or omitted) means "clear all".
    BitBswup.forceRefresh = async (cacheFilter?: string | RegExp | ((key: string) => boolean)): Promise<void> => {
        if (!('serviceWorker' in navigator)) {
            return console.warn('no serviceWorker in navigator');
        }

        const shouldDelete =
            typeof cacheFilter === 'function' ? cacheFilter :
            cacheFilter instanceof RegExp ? (key: string) => {
                cacheFilter.lastIndex = 0;
                return cacheFilter.test(key);
            } :
            typeof cacheFilter === 'string' ? (key: string) => key.startsWith(cacheFilter) :
            () => true;

        const cacheKeys = await caches.keys();
        const cachePromises = cacheKeys.filter(shouldDelete).map(key => caches.delete(key));
        await Promise.all(cachePromises);

        const regs = await navigator.serviceWorker.getRegistrations();
        const regPromises = regs.map(r => r.unregister());
        await Promise.all(regPromises);

        window.location.reload();
    }

    BitBswup.skipWaiting = async (): Promise<boolean> => {
        if (!('serviceWorker' in navigator)) {
            console.warn('no serviceWorker in navigator');
            return false;
        }

        const reg = await navigator.serviceWorker.getRegistration();

        if (reg?.waiting) {
            reg.waiting.postMessage('SKIP_WAITING');
            return true;
        }

        return false;
    }
}

// Shared message-type constants. Kept as a top-level global (rather than scoped inside the
// guard above) so the companion bit-bswup.progress.js script can read the same object, and
// declared with the `||` idempotent pattern - instead of `const` - so a duplicate inclusion
// of bit-bswup.js doesn't throw a "BswupMessage has already been declared" redeclaration
// error before the guard can take effect.
var BswupMessage = BswupMessage || {
    downloadStarted: 'DOWNLOAD_STARTED',
    downloadProgress: 'DOWNLOAD_PROGRESS',
    downloadFinished: 'DOWNLOAD_FINISHED',
    activate: 'ACTIVATE',
    updateReady: 'UPDATE_READY',
    updateFound: 'UPDATE_FOUND',
    updateNotFound: 'UPDATE_NOT_FOUND',
    updateCheckFailed: 'UPDATE_CHECK_FAILED',
    stateChanged: 'STATE_CHANGED',
    error: 'ERROR'
};

declare const Blazor: { start: () => Promise<unknown> }

interface BswupOptions {
    log: 'none' | 'error' | 'warn' | 'info' | 'verbose' | 'debug'
    sw: string
    scope: string
    handlerName: string
    blazorScript: string | string[]
    updateInterval?: number
    updateOnVisibility?: boolean
    handler?(...args: any[]): void
}
