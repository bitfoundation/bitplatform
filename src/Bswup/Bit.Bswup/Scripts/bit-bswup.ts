var BitBswup: any = BitBswup || {};
BitBswup.version = (window as any)['bit-bswup version'] = '10.6.0-pre-02';

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

            // Declared FIRST, before any code path that can start Blazor. startBlazor() runs
            // just below - and on a controlled page (the normal steady state) it reaches
            // startBlazorCore(), whose disarmStallWatchdog() reads this state. With the
            // declarations further down, that read landed in the let temporal dead zone and
            // crashed runBswup with a ReferenceError on every controlled load where the
            // Blazor script had already evaluated. The watchdog functions themselves are
            // function declarations (hoisted); only this mutable state is order-sensitive.
            let stallTimer: ReturnType<typeof setTimeout> | undefined;
            let stallArmed = false;

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

            // Resolved at the end of prepareRegistration, once the registration-aware
            // reload/cleanup implementations below have replaced the interim ones - and ALSO
            // resolved (with registrationFailed set) when register() fails for good, so the
            // interim implementations never hang forever: a secondary tab whose own
            // register() rejected still receives the sibling install's broadcasts, and its
            // splash teardown must not wait on a registration that will never come.
            let registrationFailed = false;
            let resolveRegistrationReady: () => void;
            const registrationReady = new Promise<void>(res => { resolveRegistrationReady = res; });

            // Interim implementations for the window before the registration resolves.
            // A secondary tab opened during a first install receives the worker's broadcasts
            // immediately, but its own register() promise cannot resolve until the running
            // install job finishes (register jobs for one scope are serialized behind it) -
            // so downloadFinished can dispatch here BEFORE prepareRegistration has assigned
            // the real reload/cleanup. Handing handlers an undefined reload made the built-in
            // splash teardown throw (data.reload is not a function): the splash froze at 100%
            // over an app that the claim's controllerchange had meanwhile booted. The interim
            // reload waits for the registration; when the app is by then already running with
            // nothing staged (exactly that secondary-tab case - the claim completed the first
            // install), it resolves so callers can finalize (hide the splash); otherwise it
            // defers to the real implementation, which `reload`/`cleanup` point at by then.
            let reload: () => Promise<void> = () => registrationReady.then(() => {
                if (registrationFailed) {
                    // No registration will exist in this page's lifetime, so the real reload
                    // can never be installed; a full reload restarts the whole registration
                    // flow and is the only meaningful "try again" left.
                    window.location.reload();
                    return;
                }
                // The controller check separates the two "running app, nothing staged"
                // states: a page CLAIMED by a completed first install (controller set -
                // finalize, nothing to activate) versus a page force-started after a fatal
                // first-install failure (controller null - a Retry wired to this reload must
                // fall through to the real implementation, whose no-worker fallback reloads
                // to retry the install, not silently resolve into a dead button).
                if (blazorStarted && navigator.serviceWorker.controller && registration && !registration.waiting && !registration.installing) return;
                return reload();
            });
            let cleanup: () => void = () => { registrationReady.then(() => { if (!registrationFailed) cleanup(); }); };
            // Every first-install reload() call queues its resolver here; CLIENTS_CLAIMED resolves
            // ALL of them, not just the last. A single downloadFinished can reach reload() more
            // than once (the built-in progress handler and a user handler both act on the same
            // payload, or a worker emits two completion signals), and a last-writer-wins single
            // resolver would strand every earlier reload() promise pending forever - hanging any
            // caller that awaits it to tear down its splash.
            const blazorStartResolvers: Array<(value: unknown) => void> = [];

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

            // Whether an ACTIVE worker is known to exist for this app - the first-install vs
            // update discriminator used everywhere below. It starts as a snapshot taken when
            // the registration resolves ("was a worker already active when this page
            // started?") and - crucially - flips to true the moment the first install
            // completes and takes control of this page (CLIENTS_CLAIMED / controllerchange /
            // WAITING_SKIPPED). Without the flip, a long-lived tab whose session BEGAN with
            // the first install kept classifying every LATER real update as another first
            // install: updateReady was never emitted, downloadFinished carried
            // firstInstall: true - whose auto-reload posted CLAIM_CLIENTS at the OLD active
            // worker, wiping the freshly staged update's cache - and a sibling tab's update
            // claim took the start-Blazor no-op path instead of the mandatory reload,
            // leaving old app code running against new-version caches.
            // Reading navigator.serviceWorker.controller at message time instead is NOT
            // reliable: controller is null whenever the current navigation wasn't served by
            // the SW - most notably on a hard reload (Ctrl+Shift+R) - even when an active
            // worker exists; that mistake made every hard reload look like a first install.
            //
            // Seeded from the controller RIGHT AWAY (not just when the registration resolves):
            // the controllerchange/message listeners go live several tasks before register()
            // settles, and a sibling tab accepting an update in that window would otherwise be
            // misclassified as a first-install claim on this (controlled!) page - skipping the
            // mandatory resync reload. A one-time positive controller is proof enough of an
            // active worker; the registration snapshot below only ever upgrades this to true
            // (hard reload: controller null, reg.active set), never back to false.
            let hasActiveWorker = !!navigator.serviceWorker.controller;

            // ============================================================

            // First-install stall watchdog. Every failure mode that *reports* something is
            // already handled elsewhere: a fatal error message force-starts Blazor,
            // CLIENTS_CLAIMED starts it, a worker that goes redundant is caught in the
            // statechange handler below. This timer is the last line of defense for failures
            // that report NOTHING: the browser terminating the worker mid-install (its
            // waitUntil extension is capped, Chromium at ~5 minutes), a register() that never
            // settles, a message channel that silently dies. On a first install the page
            // deliberately waits behind the splash for the install to finish before starting
            // Blazor - so if the install dies silently, nothing would EVER start the app.
            // When no sign of life (a worker message, updatefound, a statechange) arrives for
            // stallTimeout seconds, boot Blazor from the network: the page is uncontrolled at
            // that point, so it behaves exactly as if no service worker existed, and an
            // install that is in fact still running just continues in the background
            // (startBlazorCore is idempotent, so a late CLIENTS_CLAIMED is a no-op).
            // Armed only when the page starts uncontrolled and disarmed when the resolved
            // registration reveals a previously-active worker: updates never need it - the
            // app is already running when an update stalls. Configure via the stallTimeout
            // script attribute (seconds); 0 disables. (State lives at the top of runBswup -
            // see the TDZ note there.)
            function armStallWatchdog() {
                stallArmed = true;
                bumpStallWatchdog();
            }

            // Push the deadline out. Called on every sign of life from the registration or
            // the worker, so the watchdog only fires after true end-to-end silence - a slow
            // but healthy download keeps resetting it with each progress message.
            function bumpStallWatchdog() {
                if (!stallArmed) return;
                const seconds = Number(options.stallTimeout);
                if (!(seconds > 0)) return;
                if (stallTimer !== undefined) clearTimeout(stallTimer);
                stallTimer = setTimeout(() => {
                    disarmStallWatchdog();
                    // Only blazorStarted gates the rescue: a page that was claimed but whose
                    // CLIENTS_CLAIMED reply was lost is controlled yet unbooted - exactly the
                    // kind of silent death this watchdog exists for.
                    if (blazorStarted) return;
                    warn(`no service worker activity for ${seconds}s during a first install - starting Blazor without waiting for it.`);
                    startBlazor(true);
                }, seconds * 1000);
            }

            function disarmStallWatchdog() {
                stallArmed = false;
                if (stallTimer !== undefined) {
                    clearTimeout(stallTimer);
                    stallTimer = undefined;
                }
            }

            // ============================================================

            try {
                registerServiceWorker();
                navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
                navigator.serviceWorker.addEventListener('message', handleMessage);
                // Armed before registration resolves so even a register() that never settles
                // is covered; prepareRegistration disarms it when this turns out to be an
                // update (an active worker already existed).
                if (!navigator.serviceWorker.controller) armStallWatchdog();
            } catch (e) {
                registrationFailed = true;
                resolveRegistrationReady();
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
                            registrationFailed = true;
                            resolveRegistrationReady();
                            startBlazor(true);
                            return error('serviceWorker register promise failed', err);
                        }

                        warn(`serviceWorker registration was not allowed for scope "${options.scope}" - retrying with the default scope (the folder containing "${options.sw}").`, err);

                        navigator.serviceWorker
                            .register(options.sw, { updateViaCache: 'none' })
                            .then(prepareRegistration)
                            .catch((retryErr) => {
                                registrationFailed = true;
                                resolveRegistrationReady();
                                startBlazor(true);
                                error('serviceWorker register promise failed', retryErr);
                            });
                    });
            }

            function prepareRegistration(reg: ServiceWorkerRegistration) {
                // Upgrade the install/update discriminator when the registration resolves.
                // reg.active being set here means a previous version was already installed =>
                // this is an update; otherwise it's a first install (until that first install
                // completes and flips the flag - see hasActiveWorker above). Never downgraded:
                // a controller seen at startup, or a first-install claim that already
                // completed, both outrank a registration snapshot.
                hasActiveWorker = hasActiveWorker || !!reg.active;

                // The stall watchdog is a first-install boot guarantee. With a previously
                // active worker the app either starts normally (the controlled path, or the
                // uncontrolled hard-reload force-start below) or is already running when a
                // background update stalls - so it must not fire.
                if (hasActiveWorker) disarmStallWatchdog();

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
                    if (hasActiveWorker) {
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
                        blazorStartResolvers.push(res as (value: unknown) => void);
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
                    // 'redundant' is terminal: a worker already in it will never fire another
                    // statechange, so subscribing below would leave this promise pending for
                    // good. The listener tests the same condition - check it once up front.
                    if (installing.state === 'redundant') return Promise.resolve(null);

                    return new Promise((resolve) => {
                        // Named + removed on settle: reload() can be invoked more than once
                        // (retry buttons), and a leaked listener would fire on every later
                        // state change of this worker for the life of the page.
                        const onStateChange = () => {
                            if (reg.waiting) {
                                installing.removeEventListener('statechange', onStateChange);
                                return resolve(reg.waiting);
                            }
                            if (installing.state === 'redundant') {
                                installing.removeEventListener('statechange', onStateChange);
                                return resolve(null);
                            }
                        };
                        installing.addEventListener('statechange', onStateChange);
                    });
                }

                // Resolves once the registration's worker holds the active slot ('activating'
                // is enough - clients.claim() only requires being the active worker), or with
                // null when there is no worker in the pipeline / it went redundant.
                function whenActive(): Promise<ServiceWorker | null> {
                    if (reg.active) return Promise.resolve(reg.active);

                    const pending = reg.waiting || reg.installing;
                    if (!pending) return Promise.resolve(null);
                    // Terminal state, same reasoning as whenStaged.
                    if (pending.state === 'redundant') return Promise.resolve(null);

                    return new Promise((resolve) => {
                        // Removed on settle for the same reason as whenStaged's listener.
                        const onStateChange = () => {
                            if (reg.active) {
                                pending.removeEventListener('statechange', onStateChange);
                                return resolve(reg.active);
                            }
                            if (pending.state === 'redundant') {
                                pending.removeEventListener('statechange', onStateChange);
                                return resolve(null);
                            }
                        };
                        pending.addEventListener('statechange', onStateChange);
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
                    // CLEAN_UP goes to the ACTIVE worker only. Previous versions also posted
                    // it to reg.waiting, whose deleteOldCaches() spares only its own (new)
                    // cache - deleting the active worker's live cache out from under this
                    // still-running page (old app code + new-version bytes = corrupted boot
                    // config / DLL hashes). The worker now refuses that case on its own, but
                    // the page should not ask for it in the first place. Safe to call at any
                    // time: with an update staged the active worker declines and the pruning
                    // happens on activation instead.
                    reg.active?.postMessage('CLEAN_UP');
                };

                // The registration-aware reload/cleanup are in place: release any calls the
                // interim implementations queued while register() was still pending.
                resolveRegistrationReady();

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

                if (reg.installing) {
                    // An install was already mid-flight when this page loaded: its
                    // 'updatefound' fired before our listener below existed, so without
                    // watching it here nothing in this tab would ever observe the worker
                    // reaching 'installed' - updateReady, stateChanged, and the redundant
                    // first-install rescue were all silently lost for pages opened during a
                    // download.
                    info('registration already installing at load:', reg.installing);
                    watchInstallingWorker(reg.installing);
                }

                if (reg.waiting) {
                    info('registration waiting:', reg.waiting);
                    if (reg.installing) {
                        info('registration installing:', reg.installing);
                    } else {
                        info('registration is ready:', reg.waiting);
                        // Same one-announcement-per-staged-worker marker as the statechange
                        // path (see watchInstallingWorker).
                        (reg.waiting as any)._bitBswupUpdateReadyAnnounced = true;
                        handle(BswupMessage.updateReady, { reload });
                    }
                }

                reg.addEventListener('updatefound', function (e: any) {
                    info('update found', e);
                    handle(BswupMessage.updateFound, e);
                    bumpStallWatchdog();

                    if (!reg.installing) {
                        warn('no registration.installing found!');
                        return;
                    }

                    watchInstallingWorker(reg.installing);
                });

                // Follows one installing worker through its state changes: reports
                // stateChanged, rescues a silently-dying first install (redundant), and
                // announces updateReady once a real update reaches the waiting slot. Used for
                // workers discovered via 'updatefound' AND for one already installing when
                // the registration resolves.
                function watchInstallingWorker(installingWorker: ServiceWorker) {
                    // Both attachment paths can see the SAME worker: for an install triggered
                    // by this page's own register() call, the spec queues the tasks as
                    // installing-attribute-set -> register-promise-resolved -> updatefound, so
                    // the at-load check AND the updatefound listener each run for that one
                    // worker. Duplicate statechange listeners delivered every stateChanged
                    // twice and - worse - announced updateReady twice, making an autoReload
                    // handler post SKIP_WAITING twice. Watch each worker exactly once.
                    if ((installingWorker as any)._bitBswupWatched) return;
                    (installingWorker as any)._bitBswupWatched = true;
                    installingWorker.addEventListener('statechange', function (e: any) {
                        debug('state changed', e, 'eventPhase:', e.eventPhase, 'currentTarget.state:', e.currentTarget.state);
                        handle(BswupMessage.stateChanged, e);
                        bumpStallWatchdog();

                        // A first-install worker that dies without reporting anything - the
                        // browser terminated it mid-install (waitUntil cap), or the install
                        // rejected without a fatal error message getting through - goes
                        // 'redundant' as its only observable signal. Nothing else will ever
                        // start the app in that case: no CLAIM_CLIENTS handshake, no fatal
                        // error to trigger the force-start. Boot from the network now; this
                        // is idempotent when an error message already force-started Blazor,
                        // and the install itself is retried on the next load.
                        if (e.currentTarget.state === 'redundant' && !hasActiveWorker && !navigator.serviceWorker.controller) {
                            disarmStallWatchdog();
                            warn('first-install service worker went redundant - starting Blazor without a service worker.');
                            startBlazor(true);
                            return;
                        }

                        if (!reg.waiting) return;

                        if (!hasActiveWorker) {
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

                        // Notify listeners that an update is staged and ready - exactly once
                        // per staged worker. Statechange listeners outlive the waiting slot:
                        // when a staged worker B is superseded by a newer install C, B's
                        // 'redundant' statechange fires while reg.waiting already points at C,
                        // and without the marker B's listener would re-announce C right after
                        // C's own 'installed' transition announced it (double prompts; two
                        // SKIP_WAITINGs under autoReload). The marker lives on the STAGED
                        // worker, so the one case that must still announce on a 'redundant'
                        // event keeps working: a worker dying while an un-announced older
                        // update sits in the waiting slot (staged-at-load with an install in
                        // flight) announces that older update here for the first time.
                        if ((reg.waiting as any)._bitBswupUpdateReadyAnnounced) return;
                        (reg.waiting as any)._bitBswupUpdateReadyAnnounced = true;
                        handle(BswupMessage.updateReady, { reload });
                    });
                }
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
                // We distinguish case 2 from case 3 with hasActiveWorker: a controller
                // change only signals a real *update* when an active worker was already known.
                // First install never had one, so we skip the reload there - and
                // instead make sure the app boots. Being claimed on a first install means the
                // install completed and this page is expected to start Blazor, but the
                // CLIENTS_CLAIMED reply only goes to the ONE tab that posted CLAIM_CLIENTS: a
                // sibling tab opened in the window between the 100% broadcast and the claim
                // receives only this controllerchange - no progress stream to finish, no
                // CLIENTS_CLAIMED - so without starting here it would sit behind the splash
                // until the stall watchdog fired. startBlazorCore is idempotent, so in the
                // initiating tab (where CLIENTS_CLAIMED also lands) this is a no-op or a
                // harmless head start.
                if (!hasActiveWorker) {
                    info('controller changed on first install - starting Blazor instead of reloading.');
                    // The first install now controls this page: from here on this session is a
                    // controlled one, and any LATER install cycle is a real update that must
                    // announce updateReady and reload - not repeat the first-install flow.
                    hasActiveWorker = true;
                    startBlazor(true);
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
                // Any message from the worker is proof the install pipeline is alive; only
                // true end-to-end silence should let the stall watchdog fire.
                bumpStallWatchdog();

                if (e.data === 'START_BLAZOR') {
                    // No shipped worker sends this command anymore. It is kept deliberately:
                    // it is a public escape hatch (anything holding a client handle can nudge
                    // the app to boot through the guarded start path), and the test suite
                    // drives startBlazor(true) through it. Not dead code - do not remove.
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
                    if (!hasActiveWorker) {
                        // Same transition as the controllerchange path: the first install has
                        // activated and claimed this page, so later cycles are real updates.
                        hasActiveWorker = true;
                        startBlazor(true);
                        return;
                    }
                    // Reload only when actually controlled. The broadcast also reaches
                    // uncontrolled tabs - a hard-reloaded page during an update, or an
                    // SW-free page during a cleanup-worker teardown - which already run
                    // network-fresh code; reloading them only discards their in-page state.
                    // Make sure they are booted instead (idempotent when already running).
                    if (navigator.serviceWorker.controller) {
                        reloadOnce();
                    } else {
                        startBlazor(true);
                    }
                    return;
                }

                if (e.data === 'CLIENTS_CLAIMED') {
                    // First-install claim succeeded. Start Blazor through the guarded path
                    // (script-tag/autostart checks, single-start, missing-global protection)
                    // instead of calling Blazor.start() directly. Capture e.source up front
                    // because it can be nulled out by the time the start promise settles.
                    // The claim also completes the first install for this page - flip the
                    // discriminator so later install cycles are treated as real updates.
                    hasActiveWorker = true;
                    const source = e.source;

                    // Announce the completed claim handshake BEFORE starting Blazor. This is
                    // the moment a still-pending first-install reload() stops meaning "the
                    // CLAIM_CLIENTS handshake may have been lost" (where re-invoking reload()
                    // is a genuine retry) and starts meaning "Blazor is booting" (where there
                    // is nothing to retry - only to wait). Handlers can't read that transition
                    // off the reload() promise, which deliberately resolves only once the
                    // start settles: the built-in progress UI uses this message to cancel its
                    // stalled-reload fallback timer, which otherwise surfaced a spurious
                    // "update ready" button (and screen-reader announcement) whenever
                    // Blazor.start() - a full runtime download in passive mode - outlasted
                    // the timer on a slow connection. This message is NOT the SW 'activate'
                    // broadcast: activation happens even when the claim reply is then lost,
                    // so only this end-to-end signal is proof the handshake finished.
                    handle(BswupMessage.firstInstallClaimed);

                    const startPromise = startBlazor(true);

                    const onStarted = () => {
                        // Resolve (and clear) every queued first-install reload() promise, not
                        // just the most recent - see blazorStartResolvers.
                        blazorStartResolvers.splice(0).forEach(resolve => resolve(undefined));
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
                    //
                    // Reload ONLY when this page is currently controlled: the reload is what
                    // detaches a controlled page from the worker being removed. An
                    // uncontrolled page is already running SW-free, and reloading it while
                    // the served HTML still registers the cleanup worker would re-register,
                    // re-activate, re-message UNREGISTER - an infinite reload loop. Routed
                    // through reloadOnce so it coordinates with the controllerchange reload
                    // that the cleanup worker's takeover also triggers.
                    const finishUnregister = () => {
                        if (navigator.serviceWorker.controller) {
                            reloadOnce();
                        } else {
                            // An uncontrolled page has nothing to detach from - and while
                            // a cleanup worker is deployed, this teardown signal is also
                            // the earliest moment the page knows no install is coming.
                            // Boot right away instead of waiting for the delayed
                            // WAITING_SKIPPED nudge (or, worst case, the stall watchdog):
                            // this keeps app startup instant for the whole
                            // cleanup-deployment window.
                            startBlazor(true);
                        }
                    };
                    navigator.serviceWorker.getRegistration()
                        .then(reg => reg?.unregister())
                        .then(finishUnregister)
                        // Either call can reject (storage errors, a registration removed
                        // concurrently). Recovering matters more than the unregister itself:
                        // without this the promise rejected unhandled and finishUnregister
                        // never ran, so a controlled page stayed attached to the worker being
                        // torn down and an uncontrolled one waited on the stall watchdog to
                        // boot. Run the same recovery either way.
                        .catch(err => {
                            warn('UNREGISTER teardown failed - recovering anyway', err);
                            finishUnregister();
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
                    // firstInstall rides along on every UI-facing message (not just
                    // downloadFinished/error) so the built-in progress UI can tell a
                    // first-install splash - the whole UI, worth taking the screen over - from
                    // a background update, where painting a full-viewport overlay on top of a
                    // healthy running app blocks it for no reason.
                    handle(BswupMessage.downloadStarted, { ...data, firstInstall: !hasActiveWorker });
                }

                if (type === 'progress') {
                    handle(BswupMessage.downloadProgress, { ...data, firstInstall: !hasActiveWorker });

                    // Guard the deref: a stray/foreign 'progress' message (the JSON-parse guard
                    // above exists precisely because unrelated senders can post here) may carry no
                    // data, and `data.percent` would throw out of the whole message handler. The
                    // sibling branches ('bypass' uses data?.firstTime, 'error' uses data && ...)
                    // already guard; this one did not.
                    if (data && data.percent >= 100) {
                        const firstInstall = !hasActiveWorker;
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
                    handle(BswupMessage.error, { ...data, reload, firstInstall: !hasActiveWorker });

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
                    if (data && data.fatal !== false && !hasActiveWorker && !navigator.serviceWorker.controller) {
                        warn('install failed before the app could start - starting Blazor without a service worker.');
                        startBlazor(true);
                    }
                }

                if (type === 'bypass') {
                    const firstInstall = data?.firstTime || !hasActiveWorker;
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
            //
            // Overlapping calls share one in-flight check (the public API is documented as
            // safe to call as often as you like): the check is reg.update() followed by a
            // grace wait and an installing/waiting inspection, so two interleaved runs could
            // each read the registration mid-way through the other's update and announce a
            // spurious updateNotFound - or double-announce the same outcome. Joining the
            // in-flight promise gives every concurrent caller the same single check/report.
            let updateCheckInFlight: Promise<void> | undefined;
            function checkForUpdate(): Promise<void> {
                if (updateCheckInFlight) {
                    verbose('update check already in flight - joining it.');
                    return updateCheckInFlight;
                }
                updateCheckInFlight = runUpdateCheck().finally(() => updateCheckInFlight = undefined);
                return updateCheckInFlight;
            }

            async function runUpdateCheck(): Promise<void> {
                if (!registration) {
                    warn('checkForUpdate called before the service worker registration was ready.');
                    return;
                }

                info('checking for update...');

                try {
                    await registration.update();

                    // A new worker installing/waiting means an update was found; the existing
                    // 'updatefound' listener already drives updateFound/stateChanged/updateReady,
                    // so there is nothing more to say here. Empty slots are NOT yet proof of
                    // "no update", though: reg.update() resolves once the server has responded
                    // and the byte-compare is done, but the browser queues setting
                    // reg.installing (and firing 'updatefound') as a separate task - so reading
                    // the slots immediately, or after the single setTimeout(0) yield used
                    // before, could still announce a spurious updateNotFound while an install
                    // was about to start (misleading any "you're up to date" UI built on it).
                    // Wait for whichever comes first: the registration's own 'updatefound' (an
                    // update IS coming - stay silent), or a short grace deadline with the slots
                    // still empty (genuinely up to date - announce it). The deadline only ever
                    // delays the negative report; a found update is reported by the install
                    // pipeline the moment it fires.
                    if (!registration.installing && !registration.waiting && !(await updateSurfaced())) {
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

            // Grace period runUpdateCheck allows a freshly-found worker to surface after
            // reg.update() resolves (see there). Generous next to the single queued task
            // engines actually need, and it only ever delays the "up to date" announcement -
            // never the reporting of a found update.
            const UPDATE_FOUND_GRACE_MS = 300;

            // Resolves true as soon as the registration surfaces a new worker: 'updatefound'
            // firing, or - belt and braces for an event that ran before our listener attached -
            // the installing/waiting slot being filled when the deadline re-checks. Resolves
            // false when the grace deadline passes with nothing there. The temporary listener
            // is removed on settle either way; removeEventListener is optional-chained because
            // exotic registration doubles may not implement it.
            function updateSurfaced(): Promise<boolean> {
                return new Promise((resolve) => {
                    const settle = (found: boolean) => {
                        clearTimeout(timer);
                        registration.removeEventListener?.('updatefound', onUpdateFound);
                        resolve(found);
                    };
                    const onUpdateFound = () => settle(true);
                    const timer = setTimeout(() => settle(!!(registration.installing || registration.waiting)), UPDATE_FOUND_GRACE_MS);
                    registration.addEventListener('updatefound', onUpdateFound);
                });
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
                // Blazor is starting: the watchdog's job (guarantee the app boots) is done.
                disarmStallWatchdog();
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
                    // Seconds of first-install silence before the stall watchdog boots Blazor
                    // from the network (see armStallWatchdog). 0 disables.
                    stallTimeout: 60,
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

                const stallTimeoutAttribute = attrs['stallTimeout'];
                if (stallTimeoutAttribute) options.stallTimeout = Number(stallTimeoutAttribute.value);

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
                        // Boot guarantee for the no-handler case. The first-install handshake
                        // is normally driven by a handler calling data.reload() on
                        // downloadFinished; with no handler registered at all (progress script
                        // absent, custom handler never assigned) nothing would ever post
                        // CLAIM_CLIENTS and the app would sit behind the splash until the
                        // stall watchdog fired a minute later. Drive the completion here
                        // instead. Deliberately first-install only: auto-activating an UPDATE
                        // would force a reload the app never consented to - an unhandled
                        // update simply stays staged, exactly as if a handler ignored
                        // updateReady, and activates on the next full restart.
                        const message = args[0];
                        const data = args[1];
                        if (message === BswupMessage.downloadFinished && data && data.firstInstall && typeof data.reload === 'function') {
                            warn('no progress handler found - completing the first install automatically.');
                            data.reload();
                            return;
                        }
                        warn('progress handler not found or is not a function!');
                        return;
                    }
                }

                try {
                    options.handler!(...args);
                } catch (err) {
                    // An app handler that throws must not break the update pipeline. The 100%
                    // downloadProgress and downloadFinished dispatch in one synchronous block,
                    // so an uncaught exception between them would swallow the very message
                    // whose reload() completes a first install - hanging the app behind the
                    // splash until the stall watchdog for a bug in page code.
                    error('the progress handler threw', err);
                }
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
    //   - omitted:  this app's own Bswup caches (scope-qualified) + legacy scope-less Bswup
    //               buckets + the Blazor caches; a sibling app's scoped buckets are spared
    //   - string:   prefix match against the cache name (e.g. 'bit-bswup')
    //   - RegExp:   tested against the cache name
    //   - function: predicate receiving the cache name, return true to delete
    // Pass `() => true` to clear every cache on the origin.
    BitBswup.forceRefresh = async (cacheFilter?: string | RegExp | ((key: string) => boolean)): Promise<void> => {
        if (!('serviceWorker' in navigator)) {
            return console.warn('no serviceWorker in navigator');
        }

        // Resolved up front (it is also what gets unregistered below): the default cache
        // filter derives this app's own cache namespace from the registration's scope, the
        // same way the service worker names its buckets (see SCOPE_PATH in bit-bswup.sw.ts).
        const reg = await navigator.serviceWorker.getRegistration();

        // Default set: Blazor's framework caches, Bswup's legacy scope-less buckets
        // (`bit-bswup - <version>`, pre-scoping releases), and THIS app's scoped buckets
        // (`bit-bswup:<scope-path> - <version>`). A sibling Bswup app's scoped buckets on the
        // same origin are left intact - resetting app A must not wipe app B's offline story.
        // When the scope is unknown (no registration to read), fall back to every bit-bswup
        // bucket: this is a last-resort reset, and on a wedged client "reset too much" beats
        // "reset nothing".
        const defaultFilter = (key: string) => {
            if (key.startsWith('blazor-resources')) return true;
            if (key.startsWith('bit-bswup - ')) return true;
            try {
                return key.startsWith(`bit-bswup:${new URL((reg as any).scope).pathname} - `);
            } catch {
                return key.startsWith('bit-bswup');
            }
        };

        const shouldDelete =
            typeof cacheFilter === 'function' ? cacheFilter :
                cacheFilter instanceof RegExp ? (key: string) => {
                    // Reset lastIndex: a /g or /y pattern is stateful across .test() calls and
                    // would otherwise skip cache names depending on the previous match.
                    cacheFilter.lastIndex = 0;
                    return cacheFilter.test(key);
                } :
                    typeof cacheFilter === 'string' ? (key: string) => key.startsWith(cacheFilter) :
                        defaultFilter;

        const cacheKeys = await caches.keys();
        const cachePromises = cacheKeys.filter(shouldDelete).map(key => caches.delete(key));
        await Promise.all(cachePromises);

        // Only unregister Bswup's own service worker, not every same-origin registration.
        // getRegistrations() returns workers for unrelated apps sharing this origin (other
        // scopes / mounted sub-apps), and tearing those down would break them. getRegistration()
        // (no arg) resolves the registration whose scope controls this page - i.e. this app's
        // own worker - so the reset stays scoped to Bswup.
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
    // Raised when the first-install CLIENTS_CLAIMED handshake completes end-to-end, just
    // before Blazor is started. Distinct from downloadFinished (whose reload() initiates the
    // handshake) and from activate (the SW activates even when the claim reply is lost):
    // after this message a pending reload() promise means "the app is booting", not "the
    // handshake stalled". Consumed by the built-in progress UI to cancel its stalled-reload
    // fallback; update flows never raise it.
    firstInstallClaimed: 'FIRST_INSTALL_CLAIMED',
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
    stallTimeout?: number
    handler?(...args: any[]): void
}
