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

- `scope`: The scope of the service-worker ([read more](https://developer.chrome.com/docs/workbox/service-worker-lifecycle/#scope)). Defaults to `/`. A service-worker can only control URLs beneath its own folder unless the server sends a `Service-Worker-Allowed` header, so if your app is mounted on a sub-path (e.g. `https://host/myapp/`) set this to that sub-path. If the browser refuses the configured scope, Bswup automatically retries with the default scope - the folder containing the service-worker script - so the app keeps working with offline support rather than losing the service-worker entirely; the fallback is reported as a warning in the console.
- `log`: The log level of the Bswup logger. Available options are: `none`, `error`, `warn`, `info`, `verbose`, and `debug`. Each level includes everything above it (e.g. `info` also shows `warn` and `error`). Defaults to `warn`. Use `none` to silence all output.
- `sw`: The file path of the service-worker file.
- `handler`: The name of the handler function for the service-worker events.
- `blazorScript`: The path of the Blazor entry-point script (the one you added `autostart="false"` to in step 3). When omitted, Bswup auto-detects both the Blazor Web App script (`_framework/blazor.web.js`) and the standalone Blazor WebAssembly script (`_framework/blazor.webassembly.js`), so you only need to set this if your script lives at a non-default path. Matching is fingerprint-tolerant: the fingerprinted names that .NET 9+ emits when the script is referenced through `@Assets["..."]` / the ImportMap (e.g. `_framework/blazor.web.<fingerprint>.js`) are recognized automatically, both for the auto-detected defaults and for an explicitly configured `blazorScript` value.
- `updateInterval`: Number of seconds between automatic update checks. By default the browser only re-checks the service worker on navigation and roughly every 24 hours, so a long-lived SPA tab can run a stale version for a long time. Set this to a positive number (e.g. `3600` for hourly) to have Bswup call `reg.update()` on a timer. Checks are skipped while the tab is in the background (the browser throttles those timers anyway) and resume when it becomes visible again. Omit or set to `0` to disable (the default).
- `updateOnVisibility`: When set to `true`, Bswup checks for an update every time the tab returns to the foreground (the `visibilitychange` event). This is a lightweight way to catch updates right when a user comes back to a tab they left open. Disabled by default.
- `persistStorage`: When set to `true`, Bswup asks the browser to make the origin's storage persistent (`navigator.storage.persist()`) at startup. By default everything Bswup caches lives in *best-effort* storage: browsers silently reclaim it under disk pressure, and Safari deletes **all** storage for a site that has not been interacted with for seven days - the user comes back offline to an app that no longer boots. Persistent storage exempts the origin from that eviction. Disabled by default because the request can show a permission prompt (Firefox) and grant odds are engagement-based elsewhere; for the best odds, leave this off and call `BitBswup.persistStorage()` yourself at a high-signal moment (see the JavaScript API below).

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
            // 'fetch' | 'cache' | 'request' | 'install-incomplete' | 'install-aborted';
            // data.message is human readable, and data.url / data.hash point at the offending
            // asset when known.
            //
            // data.fatal says whether the install actually stopped. Under the default 'lax'
            // tolerance a failed asset is reported with `fatal: false` - the install still
            // succeeds and that asset is fetched from the network on first use - so treat it
            // as a warning, not a dead app. Only `fatal: true` (an invalid manifest, or an
            // abort under errorTolerance 'strict') means no new version was installed.
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
self.assetsInclude = [/\data.db$/];
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
- `externalAssets`: The list of external assets to cache that are not included in the auto-generated assets file. For example, if you're not using `index.html` (like `_host.cshtml`), then you should add `{ "url": "/" }`. Cross-origin entries (like the Google Tag Manager example above) are fetched in CORS mode first; when the host does not send CORS headers, Bswup retries with a `no-cors` request and caches the resulting *opaque* response so the asset still works offline (script and img tags consume opaque responses normally). This fallback is skipped for assets with an integrity check enabled, since an opaque body cannot be verified. Note that browsers deliberately pad opaque responses in storage-quota accounting (Chromium reserves several megabytes per entry), so prefer CORS-enabled hosts when you control them. Entries cached for `RegExp` patterns (server-generated file names unknown ahead of time, e.g. `_framework/resource-collection.<hash>.js`) are kept across updates so the app still boots offline, but only the newest three generations per pattern survive each update - older fingerprints are evicted so the cache cannot grow without bound.
- `defaultUrl`: The default page URL. Use `/` when using `_Host.cshtml`. The value must match an entry that actually exists in `service-worker-assets.js` or `externalAssets`; the comparison uses *resolved* URLs, so equivalent spellings match (`'index.html'` and `'/index.html'` are the same resource for a root-mounted app - they differ, correctly, for an app mounted on a sub-path). When nothing matches, offline navigation cannot work (navigations silently pass through to the network) and Bswup logs a `defaultUrl ... matches no asset` warning to the console at startup.
- `assetsUrl`: The file path of the service-worker assets file generated at compile time (the default file name is `service-worker-assets.js`). The default is resolved relative to the service-worker script's own location - which is also where Blazor publishes the file - so it works unchanged for apps mounted on a sub-path (`https://host/myapp/`). Set it explicitly only when the file lives somewhere else; a leading `/` makes the path origin-absolute.
- `prohibitedUrls`: The list of file names that should not be accessed (regex supported). Matching requests are answered by the service-worker with `403 Forbidden` and a short `text/plain` body, for every HTTP method. **Changed in 10.5.0:** previous versions answered `405 Method Not Allowed`; if your code detects a blocked URL by checking the status, look for `403`. **This is a client-side convenience, not a security boundary:** enforcement happens only inside the service worker, which is bypassed whenever the page is not controlled (the very first visit, a hard reload / Shift+Reload, browsers without service-worker support) and by any client that talks to the server directly. Access control for these URLs must be enforced on the server.
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
    [/^_content\/Bit\.Bswup\/bit-bswup\.sw\.js$/, /^service-worker\.js$/]
    ```
    #### Keep in mind that caching service-worker related files will corrupt the update cycle of the service-worker. Only the browser should handle these files. 
- `isPassive`: Enables the Bswup's passive mode. In this mode, the assets won't be cached in advance but rather upon initial request. Note that passive mode does not skip the full download entirely: on a *first* install, once Blazor has started, the service worker still tops up the cache in the background with every asset not yet fetched, so the app ends up fully offline-capable - what passive mode buys is that the first paint is never blocked behind a full precache. Assets being lazily fetched by the app while that top-up runs can be downloaded twice in that window (both writes land on the same cache keys, so this is a bandwidth cost, not a correctness issue).
- `enableIntegrityCheck`: Enables the default integrity check available in browsers by setting the `integrity` attribute of the request object created in the service-worker to fetch the assets.
- `errorTolerance`: Controls how the service worker reacts to asset download / cache failures during install. Possible values:
    - `lax` (default): best-effort install. The install always succeeds; missing assets are filled in lazily on the first fetch (in both passive and non-passive modes). Failed assets are reported through the `error` message with `fatal: false` and are counted toward the progress so the bar can still reach 100%. This is the default because it tolerates optional `externalAssets` that may legitimately 404, and because a failed install on a *first* visit would otherwise leave the app with no service-worker to complete the startup handshake.
    - `strict`: mirrors the standard Microsoft template / Workbox behavior. If any required asset fails to fetch or store during install, the install promise rejects, the partially populated cache is discarded, and the previous service-worker (if any) keeps serving the app. Failed assets are reported via the `error` message and are *not* counted toward the progress percentage, so 100% means every asset succeeded; the abort itself is reported once more with `reason: 'install-aborted'` and `fatal: true`. Choose this when a partial cache is unacceptable and you would rather stay on the previous version. On a first install (where there is no previous version to fall back to) Bswup starts the app without a service worker so it still boots from the network, and the install is retried on the next load.
- `maxRetries`: The number of *additional* download attempts after the first one when an asset fails transiently during install (a rejected fetch, or HTTP 408/429/5xx). Defaults to `2` (up to 3 total attempts). Deterministic failures - 404/403 and other permanent statuses, and Subresource Integrity mismatches - are never retried, since identical bytes would fail identically.
- `retryDelay`: The base backoff in milliseconds between those retries. Attempt *n* waits `retryDelay * 2^(n-1)` plus a random jitter (so a mass failure doesn't re-hit the origin in one synchronized burst). Defaults to `300`.
- `enableDiagnostics`: Enables diagnostics by pushing service-worker logs to the browser console.
- `enableFetchDiagnostics`: Enables fetch event diagnostics by pushing service-worker fetch event logs to the browser console.
- `disableHashlessAssetsUpdate`: Disables the update of hash-less assets. By default, Bswup automatically updates all hash-less assets (e.g. the external assets) every time an update is found for the app.
- `forcePrerender`: Forces the prerendering of the default document for every navigation request to ensure that the server always has the latest version of the app. This is useful when you have a server-rendered app and you want to make sure that the client always has the latest version of the app.
- `enableCacheControl`: Enables the cache-control mechanism by providing cache busting setting and header to each request (`cache:no-store` settings and `cache-control:no-cache` header). The `cache-control` header is only attached to same-origin requests: it is not a CORS-safelisted request header, so on a cross-origin asset it would force a preflight that most third-party hosts reject. Cross-origin requests rely on the `cache: no-store` option alone, which the CORS protocol never sees.
- `cacheVersion`: Overrides the value used to name the cache storage bucket (`bit-bswup - <version>`). By default this tracks Blazor's `assetsManifest.version` (a hash over the published assets), which means the cache is rotated automatically whenever any asset hash changes - and *only* then. Set `cacheVersion` to take manual control: pin it to a stable string so noisy dev rebuilds that perturb asset hashes don't needlessly evict the whole cache (runtime `.dll`/`.wasm` included), or bump it to force a refresh when a meaningful change lives outside Blazor's asset manifest. Only the cache bucket name (`CACHE_NAME`) is affected. Per-asset cache busting (`?v=`) is set in `createNewAssetRequest()` from each asset's `asset.hash` (falling back to `assetsManifest.version`), and Subresource Integrity uses `asset.hash` when integrity checking is enabled. When unset (or not a non-empty string) it falls back to the manifest version. Tip: feed it a build-stamped value (commit SHA, build timestamp, or your app's informational version) so it bumps automatically per publish.
- `mode`: Determines the mode of the Bswup. A mode is a preset bundle of defaults for the individual settings above (`isPassive`, `defaultUrl`, `forcePrerender`, `errorTolerance`, `caseInsensitiveUrl`, `noPrerenderQuery`): it only fills settings you have not assigned yourself, so any explicit assignment in the service-worker file always wins over the preset - including explicit falsy values such as `caseInsensitiveUrl = false` or `noPrerenderQuery = ''`. Possible values are:
    - `NoPrerender`: Disables the prerendering of the default document for every navigation request.
    - `InitialPrerender`: Enables the prerendering of the default document only for the initial navigation request.
    - `AlwaysPrerender`: Enables the prerendering of the default document for every navigation request.
    - `FullOffline`: Enables the full offline mode where all assets are cached and served from the cache from the first time the app is loaded.

## JavaScript API

Bswup exposes a small global `BitBswup` object on the page so you can drive the update lifecycle from your own code (a "check for updates" button, a custom poller, a "reset app" action, etc.):

- `BitBswup.checkForUpdate()`: Asks the browser to re-fetch the service-worker script and check for a new version. If a new version is found, the normal update flow runs (`updateFound` -> `stateChanged` -> `updateReady`/`downloadFinished`). If the app is already on the latest version, Bswup raises the `updateNotFound` event so you can stop a spinner or show an "up to date" message. If the check itself fails for a transient reason (offline, server hiccup, a throttled background tab), Bswup raises the non-blocking `updateCheckFailed` event instead of the install-path `error` event, so the default progress handler does **not** hide the app or show the install-failed UI; the payload still carries `reason`/`message` so you can surface it yourself. This is the registration-aware version that powers the built-in polling; it is safe to call as often as you like.
- `BitBswup.persistStorage()`: Requests durable, eviction-resistant storage for the origin via `navigator.storage.persist()` and resolves with a boolean saying whether storage is now persistent. Without it the caches are best-effort and can be reclaimed by the browser (see the `persistStorage` script-tag attribute above, which automates this request at startup). Calling it from a user gesture - after login, from an "install app" button - has the best chance of being granted. Safe to call repeatedly: an already-persistent origin resolves `true` without prompting again, and unsupported browsers resolve `false` with a console warning.
- `BitBswup.skipWaiting()`: If an update has finished downloading and is waiting, this activates it immediately (equivalent to calling the `reload` callback you receive in `updateReady`/`downloadFinished`). Returns `true` when there was a waiting worker to activate, otherwise `false`.
- `BitBswup.forceRefresh(cacheFilter?)`: Clears caches, unregisters the service worker controlling the current page, and reloads. Use this as a last-resort "reset" when a client gets into a bad state. It only removes this app's own registration (the one whose scope controls the current page, via `navigator.serviceWorker.getRegistration()`), not every same-origin service worker - so other apps or sub-apps mounted under different scopes on the same origin are left untouched. By default it clears only the caches Bswup and Blazor own (names starting with `bit-bswup` or `blazor-resources`), leaving app-owned CacheStorage buckets - Workbox add-ons, offline app-data, cached API responses - intact, since those can hold data with no other copy. To change what gets cleared, pass an optional `cacheFilter`: a string (prefix match against the cache name, e.g. `'bit-bswup'`), a `RegExp` (tested against the cache name), or a predicate function `(key) => boolean` that returns `true` for caches to delete. Pass `() => true` to wipe **every** cache on the origin:

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
// check every hour from your own code (equivalent to updateInterval="3600")
setInterval(() => BitBswup.checkForUpdate(), 60 * 60 * 1000);

// or check whenever the user clicks a button, and react to the result
document.getElementById('check-updates').onclick = () => BitBswup.checkForUpdate();
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
> those timers anyway) and catches up automatically when the tab becomes visible again.
