## bit Blazor Service-Worker Update Progress (bit Bswup)

To use the bit Bswup, please follow these steps:

1. Install the `Bit.Bswup` nuget package:
```bat
dotnet add package Bit.Bswup
```

2. Enable static file caching. You can follow the below code in the `Startup.cs` file:

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (env.IsDevelopment() is false)
        {
            // https://bitplatform.dev/templates/cache-mechanism
            ctx.Context.Response.GetTypedHeaders().CacheControl = new()
            {
                MaxAge = TimeSpan.FromDays(7),
                Public = true
            };
        }
    }
});
```

3. In the default document (`index.html`, `App.razor`), add `autostart="false"` to the script tag for the Blazor script:

```html
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
```

4. Also In the default document (`index.html`, `App.razor`), add the  `bit-bswup.js` script tag after the Blazor script tag with needed options:

```html
<script src="_content/Bit.Bswup/bit-bswup.js"
        scope="/"
        log="verbose"
        sw="service-worker.js"
        handler="bitBswupHandler"
        updateInterval="3600"
        updateOnVisibility="true"></script>
```

- `scope`: The scope of the service-worker ([read more](https://developer.chrome.com/docs/workbox/service-worker-lifecycle/#scope)). Defaults to `/`. A service-worker can only control URLs beneath its own folder unless the server sends a `Service-Worker-Allowed` header, so if your app is mounted on a sub-path (e.g. `https://host/myapp/`) set this to that sub-path. If the browser refuses the configured scope, Bswup automatically retries with the default scope - the folder containing the service-worker script - so the app keeps working with offline support rather than losing the service-worker entirely; the fallback is reported as a warning in the console. The scope also namespaces the caches: buckets are named `bit-bswup:<scope-path> - <version>`, so several Bswup apps mounted under different scopes on one origin keep fully independent caches (each app only ever migrates and prunes its own; **changed in v-10-5-0** - the previous scope-less name `bit-bswup - <version>` made sibling apps evict each other's caches on every update). On upgrade, entries from a legacy-named bucket are migrated into the scoped bucket without re-downloading, and the legacy bucket is then cleaned up; on a multi-app origin a not-yet-upgraded sibling may lose its legacy bucket once during that transition (the old behavior did this on every update) and refills it on its next load.
- `log`: The log level of the Bswup logger. Available options are: `none`, `error`, `warn`, `info`, `verbose`, and `debug`. Each level includes everything above it (e.g. `info` also shows `warn` and `error`). Defaults to `warn`. Use `none` to silence all output.
- `sw`: The file path of the service-worker file. Defaults to `service-worker.js`.
- `handler`: The name of the global handler function for the service-worker events. Defaults to `bitBswupHandler` - which is also the name the built-in progress script (`bit-bswup.progress.js`, see the `BswupProgress` section below) registers, so the two wire up without configuration. The handler is re-resolved until found, so it may be registered after `bit-bswup.js` loads. **If no handler is ever registered, Bswup still completes a first install on its own** (it drives the finish handshake itself so the app boots instead of waiting for the stall watchdog); updates are simply left staged until the next full restart.
- `blazorScript`: The path of the Blazor entry-point script (the one you added `autostart="false"` to in step 3). When omitted, Bswup auto-detects both the Blazor Web App script (`_framework/blazor.web.js`) and the standalone Blazor WebAssembly script (`_framework/blazor.webassembly.js`), so you only need to set this if your script lives at a non-default path. Matching is fingerprint-tolerant: the fingerprinted names that .NET 9+ emits when the script is referenced through `@Assets["..."]` / the ImportMap (e.g. `_framework/blazor.web.<fingerprint>.js`) are recognized automatically, both for the auto-detected defaults and for an explicitly configured `blazorScript` value.
- `updateInterval`: Number of seconds between automatic update checks. By default the browser only re-checks the service worker on navigation and roughly every 24 hours, so a long-lived SPA tab can run a stale version for a long time. Set this to a positive number (e.g. `3600` for hourly) to have Bswup call `reg.update()` on a timer. Checks are skipped while the tab is in the background (the browser throttles those timers anyway) and resume when it becomes visible again. Omit or set to `0` to disable (the default).
- `updateOnVisibility`: When set to `true`, Bswup checks for an update every time the tab returns to the foreground (the `visibilitychange` event). This is a lightweight way to catch updates right when a user comes back to a tab they left open. Disabled by default.
- `stallTimeout`: Number of seconds of complete service-worker *silence* (no message, no lifecycle event) after which, on a **first install** only, Bswup stops waiting and starts Blazor directly from the network. This is the last line of defense against install failures that report nothing - most notably the browser terminating the service worker mid-install (browsers cap how long an install may run; Chromium kills it after ~5 minutes) - which would otherwise leave the app frozen behind the splash forever, since a first install only starts Blazor once the install completes. The page is uncontrolled at that point, so it behaves exactly as if no service worker existed, and the install is retried on the next load. Every progress message resets the timer, so a slow-but-healthy download never triggers it - only true silence does. Defaults to `60`; set `0` to disable. Updates are unaffected: the app is already running when an update stalls.
- `persistStorage`: When set to `true`, Bswup asks the browser to make the origin's storage persistent (`navigator.storage.persist()`) at startup. By default everything Bswup caches lives in *best-effort* storage: browsers silently reclaim it under disk pressure, and Safari deletes **all** storage for a site that has not been interacted with for seven days - the user comes back offline to an app that no longer boots. Persistent storage exempts the origin from that eviction. Disabled by default because the request can show a permission prompt (Firefox) and grant odds are engagement-based elsewhere; for the best odds, leave this off and call `BitBswup.persistStorage()` yourself at a high-signal moment (see the JavaScript API below).

- `options`: The name of a global configuration object to read settings from. Defaults to `bitBswup`. Every option above can also be supplied as a property of that object (e.g. `window.bitBswup = { sw: 'my-sw.js', updateInterval: 3600 }` before the script loads); the object is merged over the built-in defaults first, and any script-tag attribute then overrides the matching property. This is the way to configure Bswup when the script is injected dynamically, where attribute-based configuration may not be readable.

> You can remove any of these attributes, and use the default values mentioned above.

5. Add a handler function like the below code to handle multiple events of the Bswup, or you can follow the full sample code which is provided in the Demo projects of this repo.

```js
const appEl = document.getElementById('app');
const bswupEl = document.getElementById('bit-bswup');
const progressBar = document.getElementById('bit-bswup-progress-bar');
const reloadButton = document.getElementById('bit-bswup-reload');

function bitBswupHandler(type, data) {
    switch (type)
    {
        case BswupMessage.updateFound: return console.log('an update found.');

        case BswupMessage.stateChanged: return console.log('state has changed to:', data.currentTarget.state);

        case BswupMessage.activate: return console.log('new version activated:', data.version);

        case BswupMessage.downloadStarted: 
            appEl.style.display = 'none';
            bswupEl.style.display = 'block';
            return console.log('downloading assets started:', data?.version);

        case BswupMessage.downloadProgress:
            // data.percent is 0-100, data.index is the 1-based count of assets handled so far,
            // and data.asset describes the asset that just finished: `url` (the path as declared
            // in service-worker-assets.js / externalAssets), `reqUrl` (the absolute URL it was
            // fetched from) and `hash` when the asset has one.
            const percent = Math.round(data.percent);
            progressBar.style.width = `${percent}%`;
            return console.log('asset downloaded:', data.asset.url, data);

        case BswupMessage.downloadFinished:
            // data.reload activates the staged version (first install: claims + starts Blazor
            // with no reload; update: SKIP_WAITING + reload). data.cleanup (optional) asks the
            // active service worker to prune this app's stale cache buckets right away; it is
            // safe to call at any time - the worker declines while an update is staged or
            // staging (pruning then happens automatically on activation), and it never touches
            // another app's caches. Most apps never need it: the same pruning already runs on
            // activation and after every accepted update.
            if (data.firstInstall) {
                data.reload().then(() => {
                    appEl.style.display = 'block';
                    bswupEl.style.display = 'none';
                });
            } else {
                reloadButton.style.display = 'block';
                reloadButton.onclick = data.reload;
            }
            return console.log('downloading assets finished.');

        case BswupMessage.updateReady:
            reloadButton.style.display = 'block';
            reloadButton.onclick = data.reload;
            return console.log('new update ready.');

        case BswupMessage.updateNotFound:
            return console.log('checked for an update, already on the latest version.');

        case BswupMessage.error:
            // Structured install failure. data.reason is one of 'manifest' | 'integrity' |
            // 'fetch' | 'cache' | 'request' | 'install-incomplete' | 'install-aborted' |
            // 'install-infra' (the install died before/while touching CacheStorage - storage
            // pressure, a broken private mode - always fatal); data.message is human readable,
            // and data.url / data.hash point at the offending asset when known.
            //
            // data.fatal says whether the install actually stopped. Under the default 'lax'
            // tolerance a failed asset is reported with `fatal: false` - the install still
            // succeeds and that asset is fetched from the network on first use - so treat it
            // as a warning, not a dead app. `fatal: true` (an invalid manifest, an abort under
            // errorTolerance 'strict', or an 'install-infra' failure) means no usable staged
            // version is available to this page. Note that a worker may still have been
            // installed: a lax 'install-infra' failure resolves the install so the worker can
            // keep serving as a network pass-through.
            //
            // data.firstInstall distinguishes where a fatal failure landed. `true`: it happened
            // before the app ever booted - Bswup starts the app without a service worker so it
            // still boots, and the built-in progress UI shows its failure panel. `false`: a
            // background *update* failed - the app keeps running on the current version (the
            // previous service worker keeps serving), so the built-in UI just clears any
            // in-progress download splash and stays out of the way.
            if (data.fatal === false) {
                console.warn('Bswup asset skipped:', data.reason, data.message, data);
                return;
            }
            console.error('Bswup install error:', data.reason, data.message,
                ...(data.url ? [`url: ${data.url}`] : []),
                ...(data.hash ? [`hash: ${data.hash}`] : []),
                data);
            return;
    }
}
```

> **Breaking Change - updates no longer auto-reload by default.** The built-in `BswupProgress`
> component's `AutoReload` parameter now defaults to `false`: when an update finishes
> downloading, the reload button is shown and the new version activates when the user accepts
> it, instead of every open tab reloading immediately - an unprompted reload discards whatever
> in-page state the user has mid-session. Set `AutoReload="true"` on the component to restore
> the old behavior. First installs are unaffected: they always complete the seamless
> claim-and-start flow (no reload) regardless of this setting.

> **Multi-tab updates:** Service workers are single-instance per origin, so accepting an
> update in one tab activates the new version for every open tab. When that happens, Bswup
> has the new worker claim all clients and each *other* tab reloads itself automatically
> (via the `controllerchange` event) onto the new version. This keeps every tab consistent
> and avoids the classic failure where an old tab keeps running old app code while its
> asset requests are served from the new version's cache (mismatched boot config / DLL
> hashes). The first install is exempt: claiming a client for the first time starts Blazor
> and does not trigger a reload.

6. Configure additional settings in the service-worker file like the following code:

```js
self.assetsInclude = [/\/data\.db$/];
self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.defaultUrl = '/';
self.prohibitedUrls = [/\/admin\//];
self.serverHandledUrls = [/\/api\//];
self.serverRenderedUrls = [/\/privacy$/];
self.externalAssets = [
    {
        "url": "/"
    },
    {
        "url": "https://www.googletagmanager.com/gtag/js?id=G-G123456789"
    }
];
self.assetsUrl = '/service-worker-assets.js';
self.noPrerenderQuery = 'no-prerender=true';
self.cacheVersion = '2026.05.31-abc1234';

self.caseInsensitiveUrl = true;
self.ignoreDefaultInclude = true;
self.ignoreDefaultExclude = true;
self.isPassive = true;
self.enableIntegrityCheck = true;
self.enableDiagnostics = true;
self.enableFetchDiagnostics = true;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
```

The most important line here is the last line which is the only mandatory config in this file that imports the Bswup service-worker file:

```js
self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
```

> **Security note - the service worker is part of your trusted base.** Unlike the assets in
> `service-worker-assets.js` (which Bswup verifies with Subresource Integrity), the
> service-worker script itself cannot be integrity-pinned: browsers do not support an
> `integrity` option on `navigator.serviceWorker.register()`, and `importScripts()` has no
> SRI mechanism either. This is not Bswup-specific - Workbox and every other SW library share
> the limitation - but it matters because a service worker can intercept every request, so a
> tampered `service-worker.js` or `bit-bswup.sw.js` is effectively persistent, fully-privileged
> XSS. Treat the origin/CDN that serves these two files as part of your trusted computing base:
> serve them over HTTPS from an origin you control, and apply a strict Content-Security-Policy.
>
> To keep clients from getting stuck on a stale worker, Bswup registers with
> `updateViaCache: 'none'`, which tells the browser to bypass the HTTP cache for the
> service-worker script **and** the scripts it pulls in via `importScripts()` during update
> checks (the browser default, `'imports'`, would still serve imported scripts from the HTTP
> cache). That covers the whole `service-worker.js` -> `bit-bswup.sw.js` -> `service-worker-assets.js`
> import chain. As defense-in-depth - and because `updateViaCache` support is uneven (older
> Safari/iOS in particular) and intermediary proxies are not bound by it - also configure your
> server to send `Cache-Control: no-cache` (or `no-store`) for `service-worker.js` and
> `_content/Bit.Bswup/bit-bswup.sw.js` so every fetch revalidates against the origin.

The other settings are:

> **How the URL-matching lists are matched.** `assetsInclude`, `assetsExclude`, `prohibitedUrls`,
> `serverHandledUrls` and `serverRenderedUrls` all accept the same two kinds of entry:
> - a **`RegExp`** (e.g. `/\/admin\//`) is used as the pattern it is - this is what the
>   examples below use, and what you want for anything non-trivial;
> - a **string** (e.g. `'/admin/'`) is matched **literally** as a substring of the URL. It is
>   regex-escaped, so `'v1.0'` matches only `v1.0` and never `v1X0`.
>
> Prefer a `RegExp` whenever you need anchoring, alternation or wildcards - a literal string
> cannot express "ends with" or "starts with". Note that a string is a *substring* match, so
> `'app.css'` matches `/css/app.css` anywhere in the URL; anchor with a `RegExp` such as
> `/\/app\.css$/` if that is too broad.

- `assetsInclude`: The list of file names from the assets list to **include** when the Bswup tries to store them in the cache storage (regex supported).
- `assetsExclude`: The list of file names from the assets list to **exclude** when the Bswup tries to store them in the cache storage (regex supported).
- `externalAssets`: The list of external assets to cache that are not included in the auto-generated assets file. For example, if you're not using `index.html` (like `_host.cshtml`), then you should add `{ "url": "/" }`. Accepted entry shapes: an object with a `url` (a concrete string, or a `RegExp` for server-generated names unknown ahead of time), a bare string (shorthand for `{ "url": "..." }`), or a bare `RegExp`; a single value also works without the array. An entry may carry a `hash` alongside its `url` - an SRI digest (`sha256-...`) that participates in the `?v=` cache busting and, when `enableIntegrityCheck` is on, in Subresource Integrity verification, exactly like a manifest asset. Entries whose `url` cannot be parsed as a request URL are skipped with a non-fatal `request` error instead of breaking the worker. Cross-origin entries (like the Google Tag Manager example above) are fetched in CORS mode first; when the host does not send CORS headers, Bswup retries with a `no-cors` request and caches the resulting *opaque* response so the asset still works offline (script and img tags consume opaque responses normally). This fallback is skipped for assets with an integrity check enabled, since an opaque body cannot be verified. Note that browsers deliberately pad opaque responses in storage-quota accounting (Chromium reserves several megabytes per entry), so prefer CORS-enabled hosts when you control them. Media assets work too: requests carrying a `Range` header (audio/video elements) are answered with a real `206 Partial Content` sliced from the cached full body when the asset is cached (Safari refuses to play media served as a `200` in response to a ranged request); when it is not cached yet, the ranged request goes to the network with its `Range` header intact so the server can answer `206` itself, and partial responses are never written to the cache - only full bodies are. Entries cached for `RegExp` patterns (server-generated file names unknown ahead of time, e.g. `_framework/resource-collection.<hash>.js`) are kept across updates so the app still boots offline, but only the newest three generations per pattern survive each update - older fingerprints are evicted so the cache cannot grow without bound.
- `defaultUrl`: The default page URL, served from cache for navigation requests (the SPA fallback). Defaults to `index.html`; use `/` when using `_Host.cshtml`. The value must match an entry that actually exists in `service-worker-assets.js` or `externalAssets`; the comparison uses *resolved* URLs, so equivalent spellings match (`'index.html'` and `'/index.html'` are the same resource for a root-mounted app - they differ, correctly, for an app mounted on a sub-path). When nothing matches, offline navigation cannot work (navigations silently pass through to the network) and Bswup logs a `defaultUrl ... matches no asset` warning to the console at startup. Navigations whose URL is itself a managed asset are served that asset instead of the default document (**changed in v-10-5-0**): opening `/manifest.json` or an image directly in a tab shows that file, while route URLs (`/counter`, ...) match no asset and still get the app shell. If your host answers the shell URL with a redirect (for example `/` &rarr; `/index.html`, common on Cloudflare Pages, Netlify, and some reverse proxies), Bswup rebuilds that response before serving it to a navigation so the browser does not reject the followed redirect with *"a redirected response was used for a request whose redirect mode is not follow"* - offline deep-link navigation keeps working regardless.
- `assetsUrl`: The file path of the service-worker assets file generated at compile time (the default file name is `service-worker-assets.js`). The default is resolved relative to the service-worker script's own location - which is also where Blazor publishes the file - so it works unchanged for apps mounted on a sub-path (`https://host/myapp/`). Set it explicitly only when the file lives somewhere else; a leading `/` makes the path origin-absolute.
- `prohibitedUrls`: The list of file names that should not be accessed (regex supported). Matching requests are answered by the service-worker with `403 Forbidden` and a short `text/plain` body, for every HTTP method. **Changed in v-10-5-0:** previous versions answered `405 Method Not Allowed`; if your code detects a blocked URL by checking the status, look for `403`. **This is a client-side convenience, not a security boundary:** enforcement happens only inside the service worker, which is bypassed whenever the page is not controlled (the very first visit, a hard reload / Shift+Reload, browsers without service-worker support) and by any client that talks to the server directly. Access control for these URLs must be enforced on the server.
- `caseInsensitiveUrl`: Enables case-insensitive URL checking. This applies both to the asset cache matching and to every URL-matching regex list (`prohibitedUrls`, `serverHandledUrls`, `serverRenderedUrls`, `assetsInclude`, `assetsExclude`): when enabled, those patterns are compiled with the `i` flag so e.g. `prohibitedUrls: [/\/admin\//]` also blocks `/ADMIN/`. Patterns that already specify the `i` flag are left unchanged.
- `serverHandledUrls`: The list of URLs that do not enter the service-worker offline process and will be handled only by server (regex supported). such as `/api`, `/swagger`, ...
- `serverRenderedUrls`: The list of URLs that should be rendered by the server and not client while navigating (regex supported). such as `/about.html`, `/privacy`, ...
- `noPrerenderQuery`: The query string attached to the default document request to disable the prerendering from the server so an unwanted prerendered result not be cached.
- `ignoreDefaultInclude`: Ignores the default asset **includes** array which is provided by the Bswup itself which is like this: 
    ```js
    [/\.dll$/, /\.wasm/, /\.pdb/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.woff2$/, /\.ttf$/, /\.webp$/]
    ```
    Note that `/\.wasm/`, `/\.pdb/` and `/\.html/` are deliberately unanchored (mirroring the standard Blazor template), so related variants such as `foo.wasm.br` also match when they appear in the manifest.
- `ignoreDefaultExclude`: Ignores the default asset **excludes** array which is provided by the Bswup itself which is like this: 
    ```js
    [
        /^_content\/Bit\.Bswup\/bit-bswup\.sw\.js$/,
        /^_content\/Bit\.Bswup\/bit-bswup\.sw\.min\.js$/,
        /^_content\/Bit\.Bswup\/bit-bswup\.sw-cleanup\.js$/,
        /^_content\/Bit\.Bswup\/bit-bswup\.sw-cleanup\.min\.js$/,
        /^service-worker\.js$/,
    ]
    ```
    **Keep in mind** that caching service-worker related files will corrupt the update cycle of the service-worker. Only the browser should handle these files.
- `isPassive`: Enables the Bswup's passive mode. In this mode, the assets won't be cached in advance but rather upon initial request. Note that passive mode does not skip the full download entirely: on a *first* install, once Blazor has started, the service worker still tops up the cache in the background with every asset not yet fetched, so the app ends up fully offline-capable - what passive mode buys is that the first paint is never blocked behind a full precache. Assets being lazily fetched by the app while that top-up runs can be downloaded twice in that window (both writes land on the same cache keys, so this is a bandwidth cost, not a correctness issue).
- `enableIntegrityCheck`: Enables the default integrity check available in browsers by setting the `integrity` attribute of the request object created in the service-worker to fetch the assets.
- `errorTolerance`: Controls how the service worker reacts to asset download / cache failures during install. Possible values:
    - `lax` (default): best-effort install. Asset failures never fail the install; missing assets are filled in lazily on the first fetch (in both passive and non-passive modes). Failed assets are reported through the `error` message with `fatal: false` and are counted toward the progress so the bar can still reach 100%. This is the default because it tolerates optional `externalAssets` that may legitimately 404, and because a failed install on a *first* visit would otherwise leave the app with no service-worker to complete the startup handshake. The download runs under the install event's `waitUntil`, so the browser keeps the service worker alive for the whole download and the worker only reaches the *waiting* state - and `updateReady` is only announced - once the background fill has settled. (Browsers cap how long an install may run - Chromium at roughly 5 minutes; a download outlasting the cap fails the install cleanly and is retried on the next load, and on a first install the page's `stallTimeout` watchdog still boots the app from the network.)
    - `strict`: mirrors the standard Microsoft template / Workbox behavior. If any required asset fails to fetch or store during install, the install promise rejects, the partially populated cache is discarded, and the previous service-worker (if any) keeps serving the app. Failed assets are reported via the `error` message and are *not* counted toward the progress percentage, so 100% means every asset succeeded; the abort itself is reported once more with `reason: 'install-aborted'` and `fatal: true`. Choose this when a partial cache is unacceptable and you would rather stay on the previous version. On a first install (where there is no previous version to fall back to) Bswup starts the app without a service worker so it still boots from the network, and the install is retried on the next load.
- `maxRetries`: The number of *additional* download attempts after the first one when an asset fails transiently during install (a rejected fetch, or HTTP 408/429/5xx). Defaults to `2` (up to 3 total attempts). Deterministic failures - 404/403 and other permanent statuses, and Subresource Integrity mismatches - are never retried, since identical bytes would fail identically.
- `retryDelay`: The base backoff in milliseconds between those retries. Attempt *n* waits `retryDelay * 2^(n-1)` plus a random jitter (so a mass failure doesn't re-hit the origin in one synchronized burst). Defaults to `300`.
- `enableDiagnostics`: Enables diagnostics by pushing service-worker logs to the browser console.
- `enableFetchDiagnostics`: Enables fetch event diagnostics by pushing service-worker fetch event logs to the browser console.
- `disableHashlessAssetsUpdate`: Disables the update of hash-less assets. By default, Bswup automatically updates all hash-less assets (e.g. the external assets) every time an update is found for the app.
- `forcePrerender`: Forces the prerendering of the default document for every navigation request to ensure that the server always has the latest version of the app. This is useful when you have a server-rendered app and you want to make sure that the client always has the latest version of the app.
- `enableCacheControl`: Enables the cache-control mechanism by providing cache busting setting and header to each request (`cache:no-store` settings and `cache-control:no-cache` header). The `cache-control` header is only attached to same-origin requests: it is not a CORS-safelisted request header, so on a cross-origin asset it would force a preflight that most third-party hosts reject. Cross-origin requests rely on the `cache: no-store` option alone, which the CORS protocol never sees.
- `cacheVersion`: Overrides the value used to name the cache storage bucket (`bit-bswup:<scope-path> - <version>`; the scope-path qualifier is what keeps multiple Bswup apps on one origin from evicting each other's caches - see the `scope` option above). By default this tracks Blazor's `assetsManifest.version` (a hash over the published assets), which means the cache is rotated automatically whenever any asset hash changes - and *only* then. Set `cacheVersion` to take manual control: pin it to a stable string so noisy dev rebuilds that perturb asset hashes don't needlessly evict the whole cache (runtime `.dll`/`.wasm` included), or bump it to force a refresh when a meaningful change lives outside Blazor's asset manifest. Only the cache bucket name (`CACHE_NAME`) is affected. Per-asset cache busting (`?v=`) is set in `createNewAssetRequest()` from each asset's `asset.hash` (falling back to `assetsManifest.version`), and Subresource Integrity uses `asset.hash` when integrity checking is enabled. When unset (or not a non-empty string) it falls back to the manifest version. Tip: feed it a build-stamped value (commit SHA, build timestamp, or your app's informational version) so it bumps automatically per publish.
- `mode`: Determines the mode of the Bswup. A mode is a preset bundle of defaults for the individual settings above (`isPassive`, `defaultUrl`, `forcePrerender`, `errorTolerance`, `caseInsensitiveUrl`, `noPrerenderQuery`): it only fills settings you have not assigned yourself, so any explicit assignment in the service-worker file always wins over the preset - including explicit falsy values such as `caseInsensitiveUrl = false` or `noPrerenderQuery = ''`. Possible values are:
    - `NoPrerender`: Disables the prerendering of the default document for every navigation request.
    - `InitialPrerender`: Enables the prerendering of the default document only for the initial navigation request.
    - `AlwaysPrerender`: Enables the prerendering of the default document for every navigation request.
    - `FullOffline`: Enables the full offline mode where all assets are cached and served from the cache from the first time the app is loaded.

## The built-in progress UI (`BswupProgress`)

Instead of writing the step-5 handler yourself, you can use the built-in splash/progress UI. It consists of the `BswupProgress` Razor component (the markup: progress bar, percentage, asset log, reload button, failure panel) and the `bit-bswup.progress.js` script, which registers the default `bitBswupHandler` and drives that markup. Reference both in your host document:

```html
<link rel="stylesheet" href="_content/Bit.Bswup/bit-bswup.progress.css" />
...
<script src="_content/Bit.Bswup/bit-bswup.js" ...></script>
<script src="_content/Bit.Bswup/bit-bswup.progress.js"></script>
```

```razor
<BswupProgress AutoReload="false" ShowAssets="true" />
```

Component parameters (each maps to a `data-bit-bswup-*` attribute the script reads at load - the component emits no inline `<script>`, so it works under a strict Content-Security-Policy and when rendered by an interactive Blazor renderer):

- `AutoReload` (default `false`): reload automatically when an update finishes instead of showing the reload button. **Changed in v-10-5-0** - previous versions defaulted to `true`.
- `ShowLogs` (default `false`): log lifecycle messages to the console.
- `ShowAssets` (default `false`): list each downloaded asset inside the splash.
- `AppContainer` (default `#app`): selector of the element to hide while installing (used with `HideApp`). An invalid selector is tolerated - the splash still works, only the hiding is skipped.
- `HideApp` (default `false`): hide the app container while the first install downloads.
- `AutoHide` (default `false`): hide the splash automatically when the download finishes.
- `Handler`: name of an additional custom handler function invoked after the built-in handling, so you can layer your own behavior without replacing the UI. (Do not point it at `bitBswupHandler` itself - self-references are detected and ignored.)
- `ChildContent`: replaces the default splash markup with your own. The component keeps initializing automatically (it still emits the `data-bit-bswup-*` config), and the built-in behavior drives your markup through the documented element ids (`bit-bswup-progress-bar`, `bit-bswup-percent`, `bit-bswup-assets`, `bit-bswup-error`, ...) - include whichever you want driven. The update-ready button (`#bit-bswup-reload`) and its screen-reader status region are always rendered by the component itself, *outside* the overlay, even with custom content - they are the only way a finished update surfaces under the default `AutoReload="false"` - so a custom splash should not render its own copy of those two; restyle them by id instead.

Behavior worth knowing:
- The full-screen splash is **first-install only**. A background update downloads silently behind the running app (the overlay is never painted over it); when it finishes - or when an update is already staged at page load - the reload button appears on its own, with a screen-reader announcement through a `role="status"` region. Clicks outside the splash content pass through the overlay, so an app force-started under a failure panel stays usable.
- Runtime toggles are available via `BitBswupProgress.config({ autoReload, showLogs, showAssets, hideApp, autoHide })`.

## Backing out of Bswup (the cleanup worker)

To fully remove Bswup from a deployed app (dropping offline support, or recovering clients stuck on a broken worker/cache), replace the *content* of your `service-worker.js` **and** `service-worker.published.js` (the file deployed builds actually ship, via the `ServiceWorker` item's `PublishedContent` mapping) with:

```js
self.importScripts('_content/Bit.Bswup/bit-bswup.sw-cleanup.js');
```

On its next update check, every client installs this self-destructing worker instead: it activates immediately, purges this app's Bswup and Blazor caches, unregisters its own registration, and signals open tabs to detach. Tabs the previous worker controlled reload once; everything afterwards runs purely from the network, even while the page keeps referencing `bit-bswup.js` (each later load just repeats the register/self-unregister cycle silently, with no reloads). Once no client has loaded the old app for as long as your cache headers require, the `bit-bswup.js` script tag can be removed from the host document too.

## JavaScript API

Bswup exposes a small global `BitBswup` object on the page so you can drive the update lifecycle from your own code (a "check for updates" button, a custom poller, a "reset app" action, etc.):

- `BitBswup.checkForUpdate()`: Asks the browser to re-fetch the service-worker script and check for a new version. If a new version is found, the normal update flow runs (`updateFound` -> `stateChanged` -> `updateReady`/`downloadFinished`). If the app is already on the latest version, Bswup raises the `updateNotFound` event so you can stop a spinner or show an "up to date" message. If the check itself fails for a transient reason (offline, server hiccup, a throttled background tab), Bswup raises the non-blocking `updateCheckFailed` event instead of the install-path `error` event, so the default progress handler does **not** hide the app or show the install-failed UI; the payload still carries `reason`/`message` so you can surface it yourself. This is the registration-aware version that powers the built-in polling; it is safe to call as often as you like.
- `BitBswup.persistStorage()`: Requests durable, eviction-resistant storage for the origin via `navigator.storage.persist()` and resolves with a boolean saying whether storage is now persistent. Without it the caches are best-effort and can be reclaimed by the browser (see the `persistStorage` script-tag attribute above, which automates this request at startup). Calling it from a user gesture - after login, from an "install app" button - has the best chance of being granted. Safe to call repeatedly: an already-persistent origin resolves `true` without prompting again, and unsupported browsers resolve `false` with a console warning.
- `BitBswup.skipWaiting()`: If an update has finished downloading and is waiting, this activates it immediately (equivalent to calling the `reload` callback you receive in `updateReady`/`downloadFinished`). Returns `true` when there was a waiting worker to activate, otherwise `false`.
- `BitBswup.forceRefresh(cacheFilter?)`: Clears caches, unregisters the service worker controlling the current page, and reloads. Use this as a last-resort "reset" when a client gets into a bad state. It only removes this app's own registration (the one whose scope controls the current page, via `navigator.serviceWorker.getRegistration()`), not every same-origin service worker - so other apps or sub-apps mounted under different scopes on the same origin are left untouched. By default it clears only the caches this app and Blazor own: the app's scope-qualified Bswup buckets (`bit-bswup:<scope-path> - ...`), legacy scope-less `bit-bswup - ...` buckets, and `blazor-resources` caches - a sibling Bswup app's scoped buckets are spared, and app-owned CacheStorage buckets (Workbox add-ons, offline app-data, cached API responses) are left intact, since those can hold data with no other copy. To change what gets cleared, pass an optional `cacheFilter`: a string (prefix match against the cache name, e.g. `'bit-bswup'`), a `RegExp` (tested against the cache name), or a predicate function `(key) => boolean` that returns `true` for caches to delete. Pass `() => true` to wipe **every** cache on the origin:

```js
BitBswup.forceRefresh();                          // Bswup + Blazor caches (default)
BitBswup.forceRefresh(() => true);                // every cache on the origin
BitBswup.forceRefresh('bit-bswup');               // only Bswup's own caches
BitBswup.forceRefresh(/^(bit-bswup|my-app-data)/) // a specific set
```

### Polling for updates

By default a service worker is only re-checked by the browser on navigation and roughly every 24 hours, so a tab that stays open for a long time can keep running an old version. There are two ways to check more often:

1. Set `updateInterval` (and/or `updateOnVisibility`) on the script tag for built-in polling (see the options above). This is the simplest approach and requires no extra code.
2. Call `BitBswup.checkForUpdate()` yourself, for example from a timer or after a user action.

```js
// consume the returned promise so a failed check is reported, not an unhandled rejection
const checkNow = () => BitBswup.checkForUpdate()
    .catch(err => console.warn('update check failed:', err));

// check every hour from your own code (equivalent to updateInterval="3600")
setInterval(checkNow, 60 * 60 * 1000);

// or check whenever the user clicks a button, and react to the result
document.getElementById('check-updates').onclick = checkNow;
```

Either way, the result surfaces through your `bitBswupHandler`: a found update flows through `updateFound`/`updateReady`, "nothing new" flows through `updateNotFound`, and a transient check failure flows through `updateCheckFailed` (handle it the same way as the other events, e.g. stop your spinner and optionally show a "couldn't check right now" hint - the app keeps running on the current version):

```js
window.bitBswupHandler = (message, data) => {
    switch (message) {
        case 'UPDATE_NOT_FOUND': /* already up to date - stop the spinner */ break;
        case 'UPDATE_CHECK_FAILED': /* transient failure - keep running, optionally notify */ break;
        // updateFound / stateChanged / updateReady / downloadFinished drive the update UI
    }
};
```

> Built-in polling skips checks while the tab is in the background (the browser throttles
> those timers anyway); the next timer tick after the tab is foregrounded runs normally. For
> an *immediate* check the moment the user comes back, also set `updateOnVisibility="true"`.

## Upgrading to v-10-6-0

v-10-6-0 is a resilience-focused release. Most changes are internal hardening, but the following are visible when upgrading; each is described in detail in its section above.

**Behavior changes**

- `BswupProgress`'s `AutoReload` parameter now defaults to `false`: updates announce themselves through the reload button instead of reloading every tab unprompted. Set `AutoReload="true"` to restore the old behavior.
- `prohibitedUrls` matches are answered with `403 Forbidden` (previously `405 Method Not Allowed`). Code that detects a blocked URL by status must check for `403`.
- Cache buckets are scope-qualified: `bit-bswup:<scope-path> - <version>` (previously `bit-bswup - <version>`). Multiple Bswup apps on one origin no longer evict each other's caches; the migration from legacy-named buckets is automatic and does not re-download.
- Under the default `lax` tolerance, the asset download now runs inside the install event (`waitUntil`): the browser keeps the worker alive for the whole download, and `updateReady` is only raised once the new version is *fully* staged - previously it could fire while the download had barely started.
- **String entries** in the URL-matching lists (`assetsInclude`, `assetsExclude`, `prohibitedUrls`, `serverHandledUrls`, `serverRenderedUrls`) are now matched literally as substrings. Previous releases silently *ignored* string entries altogether - audit any strings sitting in those lists, because they take effect for the first time after upgrading.
- `assetsUrl` defaults to a path resolved against the service-worker script's own location (previously root-absolute), so apps mounted on a sub-path work without configuration.
- The `cleanup` callback (`CLEAN_UP`) only prunes when no update is staged or staging, and `BitBswup.forceRefresh()`'s default filter clears only this app's own, legacy, and Blazor caches - a sibling app's caches are spared by both. The same staged-or-staging guard now protects **every** cache-pruning path (activation, `SKIP_WAITING`, `CLAIM_CLIENTS`), so a prune can never race a newer install's freshly written bucket.
- Requests carrying a `Range` header are answered with real `206 Partial Content` slices from cached bodies (cached media now plays in Safari), and partial responses are never written to the cache.
- Updates found later in the same session as the first install are now treated as real updates: `updateReady` is raised, `downloadFinished` carries `firstInstall: false`, and accepting them runs the normal `SKIP_WAITING` flow. Previously a long-lived tab that started with the first install kept classifying every later update as another first install - suppressing `updateReady` and, with an auto-reloading handler, wiping the freshly staged cache.
- Navigations whose URL is itself a managed asset (e.g. opening `/manifest.json` or an image directly) are served that asset instead of the SPA default document.
- An update's re-download of the default document and of hash-less assets no longer deletes the existing cache entry first: if the refresh fails (offline mid-update), the previous copy keeps serving - including offline navigation - instead of disappearing.
- The built-in progress UI stays out of the way during background updates: the full-screen splash is first-install only (`downloadStarted`/`downloadProgress` payloads now carry `firstInstall` so custom handlers can do the same), the reload button lives *outside* the `#bit-bswup` overlay so it can appear without covering the app, it is announced via a `role="status"` live region, and the overlay no longer swallows clicks outside its content.
- A first install now completes even when no handler function is registered at all (previously the app waited out the full `stallTimeout` behind the splash).
- The cleanup worker (`bit-bswup.sw-cleanup.js`) unregisters its own registration during teardown (previously the registration could linger forever) and no longer claims clients; the page reloads on `UNREGISTER` only while it is actually controlled, which removes a reload-loop hazard when the HTML still registers the cleanup script.
- The passive-mode background top-up after the first boot is deterministic: it fills every asset still missing from the cache, instead of depending on whether a lazy-fill write happened to land first.
- The default asset excludes now cover all shipped worker-script variants (`bit-bswup.sw.min.js`, `bit-bswup.sw-cleanup.js`, `bit-bswup.sw-cleanup.min.js`), not just `bit-bswup.sw.js` and `service-worker.js`.
- A manifest or `externalAssets` entry whose URL cannot be parsed is skipped with a non-fatal `request` error instead of killing the whole service worker at startup; the error is reported once per install (not on every worker cold start).
- **Hand-written splash markup needs one migration step:** if you wrote the splash markup yourself (instead of using `BswupProgress`), move `<button id="bit-bswup-reload">` *outside* the `#bit-bswup` overlay and give it its own `z-index`, and optionally add `<span id="bit-bswup-reload-status" role="status">` (visually hidden) next to it. The built-in handling no longer reveals the overlay for updates, so a button left inside it would never become visible. (The `BswupProgress` component ships this layout already - including for custom `ChildContent`.)
- A navigation to a URL that only a `RegExp` externalAssets pattern matches is served the app shell, never the pattern asset - and a shell cache miss during a navigation is refilled from the shell's own URL, never from the navigated route's URL (whose route-specific response could otherwise be cached as the app shell).
- `WAITING_SKIPPED` and `UNREGISTER` never reload an *uncontrolled* page anymore (it already runs network-fresh code); they make sure the app is booted instead. Activating a **first install** through `BitBswup.skipWaiting()` now completes the seamless claim-and-start flow instead of reloading.
- A page that loads while an update is already mid-install now observes it: `updateReady` / `stateChanged` fire in that tab when the update finishes staging.
- An exception thrown by the app's `bitBswupHandler` no longer breaks the update pipeline (it is logged, and the remaining messages still dispatch).

**New capabilities**

- Update polling: the `updateInterval` / `updateOnVisibility` script attributes, a registration-aware `BitBswup.checkForUpdate()`, and the `updateNotFound` / `updateCheckFailed` events.
- Install robustness: `errorTolerance` (`'lax'` / `'strict'`), transient-failure retries (`maxRetries`, `retryDelay`), the `stallTimeout` first-install watchdog, and structured `error` payloads (`reason`, `fatal`, `firstInstall` - including the terminal `install-infra` reason).
- Storage: `persistStorage` / `BitBswup.persistStorage()` for eviction-resistant storage, and `cacheVersion` for manual control of cache-bucket rotation.
- Registration: automatic retry with the default scope when the browser rejects the configured `scope`, and fingerprint-tolerant Blazor entry-script detection for .NET 9+ `@Assets[...]` references.
