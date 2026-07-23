var BitBswup: any = BitBswup || {};
BitBswup.version = (window as any)['bit-bswup version'] = '10.5.0';

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

    // Requests durable (eviction-resistant) storage for this origin via
    // navigator.storage.persist() and resolves with whether it is now persistent. Best-effort
    // storage - the default - can be reclaimed by the browser under disk pressure, and Safari
    // erases all storage for a site after 7 days without interaction; either wipes the Bswup
    // caches and with them the whole offline capability. persist() only exists on Window (the
    // spec does not expose it to workers), which is why this lives in the page script. Calling
    // it from a user gesture (a login, an "install app" button) has the best grant odds; the
    // `persistStorage` script-tag attribute automates a startup-time request instead. Already
    // -persistent origins resolve true without re-prompting; unsupported browsers resolve
    // false with a console warning.
    //
    // Defined BEFORE the setup IIFE below, unlike the other BitBswup.* APIs: runBswup runs
    // synchronously inside that IIFE when the document has already loaded, and the
    // persistStorage option calls this function during that run.
    BitBswup.persistStorage = async (): Promise<boolean> => {
        try {
            const storage = (navigator as any).storage;
            if (!storage || typeof storage.persist !== 'function') {
                console.warn('BitBswup: persistent storage is not supported by this browser.');
                return false;
            }

            if (typeof storage.persisted === 'function' && await storage.persisted()) return true;

            return !!(await storage.persist());
        } catch (err) {
            console.warn('BitBswup: persistent storage request failed', err);
            return false;
        }
    }

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

        // Tracks the single Blazor.start() invocation. A first install (CLIENTS_CLAIMED),
        // a controlled/hard-reload load, and the explicit force paths can all reach the
        // start logic; Blazor.start() may only be called once (a second call rejects), so
        // funnel every path through startBlazorCore() and remember the in-flight/settled
        // promise to hand back to later callers.
        let blazorStarted = false;
        let blazorStartPromise: Promise<unknown> | undefined;

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

            if (options.persistStorage) {
                // Opt-in: ask the browser to make this origin's storage durable. Without it the
                // entire offline story sits in *best-effort* storage: under disk pressure
                // browsers reclaim CacheStorage silently, and Safari deletes ALL storage for a
                // site not interacted with for 7 days - the user comes back offline to an app
                // that simply no longer boots. Opt-in rather than default because the request
                // can show a permission prompt (Firefox) and grant odds are engagement-based
                // elsewhere; for better odds an app can leave this off and call
                // BitBswup.persistStorage() itself at a higher-signal moment (after login,
                // from an "install app" action).
                BitBswup.persistStorage().then((granted: boolean) =>
                    granted ? info('persistent storage granted.') : warn('persistent storage was not granted - cached assets remain evictable.'));
            }

            let reload: () => Promise<void>;
            let cleanup: () => void;
            let blazorStartResolver: (value: unknown) => void;

            // Captured once the registration resolves so the polling helpers (timer /
            // visibilitychange) and the page-facing BitBswup.checkForUpdate() can all call
            // reg.update() against the same registration without re-resolving it each time.
            let registration: ServiceWorkerRegistration;
            let updateTimer: ReturnType<typeof setInterval>;
            let visibilityHandler: (() => void) | undefined;

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
                registerServiceWorker();
                navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
                navigator.serviceWorker.addEventListener('message', handleMessage);
            } catch (e) {
                startBlazor(true);
                error('serviceWorker registration failed', e);
            }

            // Registers the service worker, retrying once without the configured scope if the
            // browser refuses it.
            //
            // A service worker may only control URLs beneath its own directory unless the
            // response carries a Service-Worker-Allowed header, so the default scope of '/' is
            // rejected outright for any app mounted on a sub-path (https://host/myapp/). That
            // used to be masked for apps that defined a partial `window.bitBswup` object: the
            // options were taken wholesale instead of merged, so `scope` came out undefined and
            // the browser quietly applied its own default. Now that options are merged with the
            // defaults (a fix - it also stops handlerName and blazorScript from coming out
            // undefined), those apps get the explicit '/' and the registration would fail.
            //
            // Retrying with the scope omitted reproduces exactly what the browser would have
            // done on its own - scope = the folder containing the service-worker script - so a
            // sub-path app keeps working instead of silently losing offline support. Apps that
            // legitimately serve a sub-directory worker at '/' via Service-Worker-Allowed are
            // untouched: their first attempt succeeds and the retry never runs.
            function registerServiceWorker() {
                navigator.serviceWorker
                    .register(options.sw, { scope: options.scope, updateViaCache: 'none' })
                    .then(prepareRegistration)
                    .catch((err) => {
                        // Only a scope rejection is worth retrying. Retrying on *any* failure
                        // would be actively harmful: a transient first attempt (or a 404 that
                        // later resolves) could then succeed on the narrower default scope,
                        // leaving the worker silently controlling less of the origin than
                        // configured - the kind of bug that only shows up as "offline mode
                        // doesn't work on some pages". The spec requires SecurityError for a
                        // scope the worker isn't allowed to control, and every engine follows
                        // it; matching on the error *name* keeps this precise. Deliberately no
                        // message-text probe: Chrome's script-fetch failures also contain the
                        // word "scope" ("Failed to register a ServiceWorker for scope (...): A
                        // bad HTTP response code (404) ..."), which would defeat the check.
                        if (!options.scope || !err || err.name !== 'SecurityError') {
                            startBlazor(true);
                            return error('serviceWorker register promise failed', err);
                        }

                        warn(`serviceWorker registration was not allowed for scope "${options.scope}" - retrying with the default scope (the folder containing "${options.sw}").`, err);

                        navigator.serviceWorker
                            .register(options.sw, { updateViaCache: 'none' })
                            .then(prepareRegistration)
                            .catch((retryErr) => {
                                startBlazor(true);
                                error('serviceWorker register promise failed', retryErr);
                            });
                    });
            }

            function prepareRegistration(reg: ServiceWorkerRegistration) {
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

                // reload() is handed to downloadFinished/updateReady/error handlers and must be
                // deterministic. The 100% progress message is sent just before the SW's install
                // promise resolves, so at the moment a caller reacts the new worker can still be
                // in 'installing' - the previous implementation read reg.waiting/reg.active at
                // that instant and picked between three different behaviors by racing the
                // lifecycle:
                //   - update:        a not-yet-waiting worker fell through to posting
                //                    CLAIM_CLIENTS at the OLD active worker, whose
                //                    deleteOldCaches() then wiped the freshly installed cache;
                //   - first install: 'installing' meant a hard window.location.reload(), and a
                //                    transiently-waiting worker meant SKIP_WAITING - both full
                //                    reloads where the designed flow (claim + start Blazor, no
                //                    reload) only happened when the activation race was won.
                // Instead, wait for the worker to reach the state each flow actually needs.
                reload = () => {
                    // Update: wait until the new worker is staged (waiting), then tell it to
                    // skip waiting; the resulting 'WAITING_SKIPPED' message (and the
                    // 'controllerchange' after the claim) reloads the page. The returned promise
                    // is deliberately kept *pending*: the page is about to navigate away, so
                    // resolving early would let callers run teardown (e.g. hiding the splash)
                    // against a page that's already reloading.
                    if (hadActiveWorkerAtStartup) {
                        whenStaged().then((waiting) => {
                            if (waiting) {
                                waiting.postMessage('SKIP_WAITING');
                            } else {
                                // Nothing staged and nothing installing (e.g. the Retry button
                                // after a failed update install): a plain reload re-checks for
                                // the update and retries it.
                                window.location.reload();
                            }
                        });
                        return new Promise<void>(() => { });
                    }

                    // First install: wait for activation (immediate once install completes - with
                    // no previous worker and no controlled clients the browser never leaves the
                    // worker waiting), then ask it to claim this page; once 'CLIENTS_CLAIMED'
                    // arrives we start Blazor and resolve this promise so callers can finalize
                    // (e.g. hide the splash) - no reload anywhere on this path.
                    return new Promise<void>((res) => {
                        blazorStartResolver = res as (value: unknown) => void;
                        whenActive().then((worker) => {
                            if (worker) {
                                worker.postMessage('CLAIM_CLIENTS');
                            } else {
                                // No worker in the pipeline at all (e.g. the Retry button after
                                // a fatal first-install failure): reload to retry the install.
                                window.location.reload();
                            }
                        });
                    });
                };

                // Resolves once the registration has a worker staged and waiting (state
                // 'installed'), or with null when there is nothing to wait for - no installing
                // worker, or the install failed and the worker went redundant.
                function whenStaged(): Promise<ServiceWorker | null> {
                    if (reg.waiting) return Promise.resolve(reg.waiting);

                    const installing = reg.installing;
                    if (!installing) return Promise.resolve(null);

                    return new Promise((resolve) => {
                        installing.addEventListener('statechange', () => {
                            if (reg.waiting) return resolve(reg.waiting);
                            if (installing.state === 'redundant') return resolve(null);
                        });
                    });
                }

                // Resolves once the registration's worker holds the active slot ('activating'
                // is enough - clients.claim() only requires being the active worker), or with
                // null when there is no worker in the pipeline / it went redundant.
                function whenActive(): Promise<ServiceWorker | null> {
                    if (reg.active) return Promise.resolve(reg.active);

                    const pending = reg.waiting || reg.installing;
                    if (!pending) return Promise.resolve(null);

                    return new Promise((resolve) => {
                        pending.addEventListener('statechange', () => {
                            if (reg.active) return resolve(reg.active);
                            if (pending.state === 'redundant') return resolve(null);
                        });
                    });
                }

                cleanup = () => {
                    // Stop the opt-in update-poll timer so it doesn't keep calling
                    // reg.update() after teardown (it was previously left running for the
                    // life of the page with no way to clear it).
                    if (updateTimer) {
                        clearInterval(updateTimer);
                        updateTimer = undefined as any;
                    }
                    if (visibilityHandler) {
                        document.removeEventListener('visibilitychange', visibilityHandler);
                        visibilityHandler = undefined;
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

                reg.addEventListener('updatefound', function (e: any) {
                    info('update found', e);
                    handle(BswupMessage.updateFound, e);

                    if (!reg.installing) {
                        warn('no registration.installing found!');
                        return;
                    }

                    reg.installing.addEventListener('statechange', function (e: any) {
                        debug('state changed', e, 'eventPhase:', e.eventPhase, 'currentTarget.state:', e.currentTarget.state);
                        handle(BswupMessage.stateChanged, e);

                        if (!reg.waiting) return;

                        if (!hadActiveWorkerAtStartup) {
                            // First install: the worker only passes through 'installed' (waiting)
                            // transiently - with no previous worker and no controlled clients the
                            // browser activates it immediately. This is NOT "an update is staged
                            // and ready": emitting updateReady here made autoReload handlers post
                            // SKIP_WAITING against a first install, turning the seamless
                            // claim-and-start flow into a needless full page reload whenever this
                            // race was won. Completion of a first install is signalled by
                            // downloadFinished instead.
                            info('initialization finished.');
                            return;
                        }

                        info('update finished.');

                        // Notify listeners that an update is staged and ready. The
                        // registration-time check only fires updateReady for updates already
                        // waiting on load; updates discovered in the same session surface here
                        // instead, so emit it for them too.
                        handle(BswupMessage.updateReady, { reload });
                    });
                });
            }

            function handleControllerChange(e: any) {
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

            function handleMessage(e: any) {
                if (e.data === 'START_BLAZOR') {
                    startBlazor(true);
                    return;
                }

                if (e.data === 'WAITING_SKIPPED') {
                    // The worker we asked to skip waiting has activated and claimed this page.
                    // On a first install (reachable via an explicit BitBswup.skipWaiting() call)
                    // there is no old version to swap out - a reload would only restart the
                    // splash - so start Blazor in place instead; startBlazorCore is idempotent,
                    // making this a no-op when the app is already running. For an update, reload
                    // to pick up the new version. reloadOnce() coordinates with the
                    // 'controllerchange' that also fires once the new worker claims this client,
                    // so we reload only once.
                    if (!hadActiveWorkerAtStartup) {
                        startBlazor(true);
                        return;
                    }
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
                        // Handle rejection too: if Blazor.start() rejects, onStarted would
                        // never run, leaving blazorStartResolver unresolved and the first-install
                        // reload() promise pending forever. Release it on failure as well.
                        startPromise.then(onStarted, (err) => {
                            error('Blazor.start() rejected', err);
                            onStarted();
                        });
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
                    // Only unregister Bswup's own service worker, not every same-origin
                    // registration. getRegistrations() returns workers for unrelated apps
                    // sharing this origin (other scopes / mounted sub-apps), and tearing those
                    // down would break them. getRegistration() (no arg) resolves the
                    // registration whose scope controls this page - this app's own worker.
                    navigator.serviceWorker.getRegistration().then(reg => {
                        Promise.resolve(reg?.unregister()).then(() => window.location.reload());
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
                    // firstInstall tells handlers (and the built-in progress UI) whether this
                    // failure happened before the app ever booted - where the splash is the
                    // whole UI and a failure panel is the right response - or during a
                    // background update, where the previous worker keeps serving a healthy
                    // running app that must not be covered by an install-failure panel. Same
                    // discriminator the downloadFinished payload already carries.
                    handle(BswupMessage.error, { ...data, reload, firstInstall: !hadActiveWorkerAtStartup });

                    // Last-resort boot guarantee. A fatal failure means the install aborted, so
                    // this worker never reaches 'active'. On a *first* install that is fatal to
                    // the page as well: nothing controls it, progress never reaches 100% (failed
                    // assets aren't counted), so downloadFinished -> reload() -> CLAIM_CLIENTS
                    // never happens and Blazor is never started - the app hangs behind the
                    // splash forever. Start it directly instead: the network still works, this
                    // page just serves everything from origin as if there were no service
                    // worker, and the next load can retry the install. startBlazorCore is
                    // idempotent, so this is a no-op when the app is already running (the update
                    // case, where the previous worker keeps serving normally).
                    if (data && data.fatal !== false && !hadActiveWorkerAtStartup && !navigator.serviceWorker.controller) {
                        warn('install failed before the app could start - starting Blazor without a service worker.');
                        startBlazor(true);
                    }
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
                    // Kept referenceable so cleanup() can remove it, same as the poll timer.
                    visibilityHandler = () => {
                        if (document.visibilityState === 'visible') {
                            verbose('tab became visible - checking for update.');
                            checkForUpdate();
                        }
                    };
                    document.addEventListener('visibilitychange', visibilityHandler);
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

                // An async rejection must release the single-start latch just like the
                // synchronous throw above. Without this, a rejected start (a boot-resource
                // fetch that failed, a bad boot config) left blazorStarted stuck at true, so
                // every later trigger - the fatal-error force-start, START_BLAZOR,
                // WAITING_SKIPPED on a first install - got the same dead promise back and the
                // app could never try again without a full reload. A failed start leaves the
                // Blazor runtime unbooted, so calling Blazor.start() again is a legitimate
                // retry (the "start may only be called once" rule applies to a runtime that
                // actually started); retrying is exactly what recovers a transient network
                // blip during boot-resource download. Callers still observe the rejection on
                // the promise returned below - this branch only resets the latch.
                blazorStartPromise.catch((err) => {
                    error('Blazor.start() rejected - releasing the start latch so a later trigger can retry.', err);
                    blazorStarted = false;
                    blazorStartPromise = undefined;
                });

                return blazorStartPromise;
            }

            // Whether a <script src> refers to the given Blazor entry-point script. A plain
            // substring test used to be enough, but .NET 9+ templates reference the script
            // through @Assets["_framework/blazor.web.js"] / the ImportMap, which emits a
            // fingerprinted name (_framework/blazor.web.<fingerprint>.js) that no longer
            // contains the literal candidate - and with autostart="false" a missed match means
            // Blazor is NEVER started: a dead app behind the splash. Accept an optional
            // `.fingerprint` segment between the file name and its extension, and require the
            // extension to end the path (or be followed by a query/fragment, e.g. the
            // `blazor.web.js?v=10.0.0` form) so lookalikes such as `blazor.website.js` or
            // `blazor.web.jsx` never match. The mandatory '.' before the fingerprint is also
            // what keeps the `blazor.web.js` candidate from matching `blazor.webassembly.js`.
            // Candidates without an extension keep the original substring semantics.
            function matchesBlazorScript(src: string, candidate: string): boolean {
                if (!candidate) return false;

                const dotIndex = candidate.lastIndexOf('.');
                if (dotIndex <= 0) return src.indexOf(candidate) !== -1;

                const base = candidate.slice(0, dotIndex);   // e.g. '_framework/blazor.web'
                const extension = candidate.slice(dotIndex); // e.g. '.js'
                const pattern = new RegExp(escapeForRegExp(base) + '(\\.[\\w-]+)?' + escapeForRegExp(extension) + '(?=$|[?#])');
                return pattern.test(src);
            }

            function escapeForRegExp(str: string): string {
                return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            }

            function startBlazor(forceStart = false): Promise<unknown> | undefined {
                const scriptTags = [].slice.call(document.scripts) as HTMLScriptElement[];

                // `blazorScript` may be a single path (explicitly configured) or a list
                // of candidates to auto-detect. Normalize to an array and match the first
                // script tag whose src matches any of the candidates (fingerprint-tolerant,
                // see matchesBlazorScript above).
                const candidates = Array.isArray(options.blazorScript) ? options.blazorScript : [options.blazorScript];

                const blazorWasmScriptTag = scriptTags.find(s => s.src && candidates.some(c => matchesBlazorScript(s.src, c)));
                if (!blazorWasmScriptTag) {
                    warn(`blazor script (${candidates.join(' or ')}) not found!`);
                    return undefined;
                }

                const autostart = (blazorWasmScriptTag as any).attributes['autostart'];
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

                const persistStorageAttribute = attrs['persistStorage'];
                if (persistStorageAttribute) options.persistStorage = persistStorageAttribute.value === 'true';

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
                    const resolved = (window as any)[options.handlerName];
                    if (typeof resolved === 'function') {
                        options.handler = resolved;
                    } else {
                        warn('progress handler not found or is not a function!');
                        return;
                    }
                }

                options.handler!(...args);
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

    // Cache buckets forceRefresh clears when the caller doesn't say otherwise: Bswup's own
    // version-suffixed caches and Blazor's framework cache. This is deliberately the same set
    // previous versions cleared.
    const DEFAULT_REFRESH_CACHE_PREFIXES = ['bit-bswup', 'blazor-resources'];

    // `forceRefresh` is the last-resort "reset" when a client is wedged: clear the caches,
    // unregister this app's service worker, reload.
    //
    // By default it clears only the caches Bswup and Blazor own. It is tempting to wipe every
    // CacheStorage bucket instead - a stale app-owned cache can in principle re-poison the
    // freshly reloaded app - but forceRefresh is a public API that apps already call, and
    // CacheStorage is shared with everything else on the origin: Workbox add-ons, offline
    // app-data, cached API responses, other sub-apps mounted on the same host. Silently
    // widening the blast radius on upgrade could destroy data an app deliberately persisted
    // (and, for offline-first apps, data with no other copy). Deleting more than the caller
    // asked for is not recoverable, so the destructive option is opt-in:
    //   - omitted:  the Bswup + Blazor caches (default, and the historical behavior)
    //   - string:   prefix match against the cache name (e.g. 'bit-bswup')
    //   - RegExp:   tested against the cache name
    //   - function: predicate receiving the cache name, return true to delete
    // Pass `() => true` to clear every cache on the origin.
    BitBswup.forceRefresh = async (cacheFilter?: string | RegExp | ((key: string) => boolean)): Promise<void> => {
        if (!('serviceWorker' in navigator)) {
            return console.warn('no serviceWorker in navigator');
        }

        const shouldDelete =
            typeof cacheFilter === 'function' ? cacheFilter :
                cacheFilter instanceof RegExp ? (key: string) => {
                    // Reset lastIndex: a /g or /y pattern is stateful across .test() calls and
                    // would otherwise skip cache names depending on the previous match.
                    cacheFilter.lastIndex = 0;
                    return cacheFilter.test(key);
                } :
                    typeof cacheFilter === 'string' ? (key: string) => key.startsWith(cacheFilter) :
                        (key: string) => DEFAULT_REFRESH_CACHE_PREFIXES.some(prefix => key.startsWith(prefix));

        const cacheKeys = await caches.keys();
        const cachePromises = cacheKeys.filter(shouldDelete).map(key => caches.delete(key));
        await Promise.all(cachePromises);

        // Only unregister Bswup's own service worker, not every same-origin registration.
        // getRegistrations() returns workers for unrelated apps sharing this origin (other
        // scopes / mounted sub-apps), and tearing those down would break them. getRegistration()
        // (no arg) resolves the registration whose scope controls this page - i.e. this app's
        // own worker - so the reset stays scoped to Bswup.
        const reg = await navigator.serviceWorker.getRegistration();
        await reg?.unregister();

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
var BswupMessage: any = BswupMessage || {
    downloadStarted: 'DOWNLOAD_STARTED',
    downloadProgress: 'DOWNLOAD_PROGRESS',
    downloadFinished: 'DOWNLOAD_FINISHED',
    activate: 'ACTIVATE',
    updateReady: 'UPDATE_READY',
    updateFound: 'UPDATE_FOUND',
    updateNotFound: 'UPDATE_NOT_FOUND',
    updateCheckFailed: 'UPDATE_CHECK_FAILED',
    stateChanged: 'STATE_CHANGED',
    error: 'ERROR',

    // DEPRECATED - kept only so existing handlers keep resolving it. This constant has never
    // been raised by any version of Bswup: it was declared, never emitted, and never
    // documented. Dropping it would turn a `case BswupMessage.updateInstalled:` in app code
    // into `case undefined:` - still dead, but now dead for a different reason, and one that
    // silently shifts if `message` were ever undefined. Keeping the original value costs a
    // line and guarantees those handlers behave exactly as they did before upgrading.
    // Use updateReady instead: it fires when a new version has finished installing and is
    // waiting to activate, which is what this name suggested. Do not add new uses.
    updateInstalled: 'UPDATE_INSTALLED'
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
    persistStorage?: boolean
    handler?(...args: any[]): void
}
