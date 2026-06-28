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

- `scope`: The scope of the service-worker ([read more](https://developer.chrome.com/docs/workbox/service-worker-lifecycle/#scope)).
- `log`: The log level of the Bswup logger. Available options are: `none`, `error`, `warn`, `info`, `verbose`, and `debug`. Each level includes everything above it (e.g. `info` also shows `warn` and `error`). Defaults to `warn`. Use `none` to silence all output.
- `sw`: The file path of the service-worker file.
- `handler`: The name of the handler function for the service-worker events.
- `blazorScript`: The path of the Blazor entry-point script (the one you added `autostart="false"` to in step 3). When omitted, Bswup auto-detects both the Blazor Web App script (`_framework/blazor.web.js`) and the standalone Blazor WebAssembly script (`_framework/blazor.webassembly.js`), so you only need to set this if your script lives at a non-default path.
- `updateInterval`: Number of seconds between automatic update checks. By default the browser only re-checks the service worker on navigation and roughly every 24 hours, so a long-lived SPA tab can run a stale version for a long time. Set this to a positive number (e.g. `3600` for hourly) to have Bswup call `reg.update()` on a timer. Checks are skipped while the tab is in the background (the browser throttles those timers anyway) and resume when it becomes visible again. Omit or set to `0` to disable (the default).
- `updateOnVisibility`: When set to `true`, Bswup checks for an update every time the tab returns to the foreground (the `visibilitychange` event). This is a lightweight way to catch updates right when a user comes back to a tab they left open. Disabled by default.

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
            const percent = Math.round(data.percent);
            progressBar.style.width = `${percent}%`;
            return console.log('asset downloaded:', data);

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
            // 'fetch' | 'cache' | 'request' | 'install-incomplete'; data.message is human
            // readable, and data.url / data.hash point at the offending asset when known.
            console.error('Bswup install error:', data.reason, data.message,
                ...(data.url ? [`url: ${data.url}`] : []),
                ...(data.hash ? [`hash: ${data.hash}`] : []),
                data);
            return;
    }
}
```

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

- `assetsInclude`: The list of file names from the assets list to **include** when the Bswup tries to store them in the cache storage (regex supported).
- `assetsExclude`: The list of file names from the assets list to **exclude** when the Bswup tries to store them in the cache storage (regex supported).
- `externalAssets`: The list of external assets to cache that are not included in the auto-generated assets file. For example, if you're not using `index.html` (like `_host.cshtml`), then you should add `{ "url": "/" }`.
- `defaultUrl`: The default page URL. Use `/` when using `_Host.cshtml`.
- `assetsUrl`: The file path of the service-worker assets file generated at compile time (the default file name is `service-worker-assets.js`).
- `prohibitedUrls`: The list of file names that should not be accessed (regex supported).
- `caseInsensitiveUrl`: Enables case-insensitive URL checking. This applies both to the asset cache matching and to every URL-matching regex list (`prohibitedUrls`, `serverHandledUrls`, `serverRenderedUrls`, `assetsInclude`, `assetsExclude`): when enabled, those patterns are compiled with the `i` flag so e.g. `prohibitedUrls: [/\/admin\//]` also blocks `/ADMIN/`. Patterns that already specify the `i` flag are left unchanged.
- `serverHandledUrls`: The list of URLs that do not enter the service-worker offline process and will be handled only by server (regex supported). such as `/api`, `/swagger`, ...
- `serverRenderedUrls`: The list of URLs that should be rendered by the server and not client while navigating (regex supported). such as `/about.html`, `/privacy`, ...
- `noPrerenderQuery`: The query string attached to the default document request to disable the prerendering from the server so an unwanted prerendered result not be cached.
- `ignoreDefaultInclude`: Ignores the default asset **includes** array which is provided by the Bswup itself which is like this: 
    ```js
    [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.woff2$/, /\.ttf$/, /\.webp$/]
    ```
- `ignoreDefaultExclude`: Ignores the default asset **excludes** array which is provided by the Bswup itself which is like this: 
    ```js
    [/^_content\/Bit\.Bswup\/bit-bswup\.sw\.js$/, /^service-worker\.js$/]
    ```
    #### Keep in mind that caching service-worker related files will corrupt the update cycle of the service-worker. Only the browser should handle these files. 
- `isPassive`: Enables the Bswup's passive mode. In this mode, the assets won't be cached in advance but rather upon initial request.
- `enableIntegrityCheck`: Enables the default integrity check available in browsers by setting the `integrity` attribute of the request object created in the service-worker to fetch the assets.
- `errorTolerance`: Controls how the service worker reacts to asset download / cache failures during install. Possible values:
    - `strict` (default): mirrors the standard Microsoft template / Workbox behavior. If any required asset fails to fetch or store during install, the install promise rejects, the partially populated cache is discarded, and the previous service-worker (if any) keeps serving the app. Failed assets are reported via the `error` message and are *not* counted toward the progress percentage, so 100% means every asset succeeded.
    - `lax`: best-effort install. The install always succeeds; missing assets are filled in lazily on the first fetch (in both passive and non-passive modes). Failed assets are still reported as errors but are counted toward the progress so the bar can reach 100% even with failures. Use this only when you knowingly accept a partial cache, for example when listing optional `externalAssets` that may legitimately 404.
- `enableDiagnostics`: Enables diagnostics by pushing service-worker logs to the browser console.
- `enableFetchDiagnostics`: Enables fetch event diagnostics by pushing service-worker fetch event logs to the browser console.
- `disableHashlessAssetsUpdate`: Disables the update of hash-less assets. By default, Bswup automatically updates all hash-less assets (e.g. the external assets) every time an update is found for the app.
- `forcePrerender`: Forces the prerendering of the default document for every navigation request to ensure that the server always has the latest version of the app. This is useful when you have a server-rendered app and you want to make sure that the client always has the latest version of the app.
- `enableCacheControl`: Enables the cache-control mechanism by providing cache busting setting and header to each request (`cache:no-store` settings and `cache-control:no-cache` header).
- `cacheVersion`: Overrides the value used to name the cache storage bucket (`bit-bswup - <version>`). By default this tracks Blazor's `assetsManifest.version` (a hash over the published assets), which means the cache is rotated automatically whenever any asset hash changes - and *only* then. Set `cacheVersion` to take manual control: pin it to a stable string so noisy dev rebuilds that perturb asset hashes don't needlessly evict the whole cache (runtime `.dll`/`.wasm` included), or bump it to force a refresh when a meaningful change lives outside Blazor's asset manifest. Only the cache bucket name (`CACHE_NAME`) is affected. Per-asset cache busting (`?v=`) is set in `createNewAssetRequest()` from each asset's `asset.hash` (falling back to `assetsManifest.version`), and Subresource Integrity uses `asset.hash` when integrity checking is enabled. When unset (or not a non-empty string) it falls back to the manifest version. Tip: feed it a build-stamped value (commit SHA, build timestamp, or your app's informational version) so it bumps automatically per publish.
- `mode`: Determines the mode of the Bswup. Possible values are:
    - `NoPrerender`: Disables the prerendering of the default document for every navigation request.
    - `InitialPrerender`: Enables the prerendering of the default document only for the initial navigation request.
    - `AlwaysPrerender`: Enables the prerendering of the default document for every navigation request.
    - `FullOffline`: Enables the full offline mode where all assets are cached and served from the cache from the first time the app is loaded.

## JavaScript API

Bswup exposes a small global `BitBswup` object on the page so you can drive the update lifecycle from your own code (a "check for updates" button, a custom poller, a "reset app" action, etc.):

- `BitBswup.checkForUpdate()`: Asks the browser to re-fetch the service-worker script and check for a new version. If a new version is found, the normal update flow runs (`updateFound` -> `stateChanged` -> `updateReady`/`downloadFinished`). If the app is already on the latest version, Bswup raises the `updateNotFound` event so you can stop a spinner or show an "up to date" message. If the check itself fails for a transient reason (offline, server hiccup, a throttled background tab), Bswup raises the non-blocking `updateCheckFailed` event instead of the install-path `error` event, so the default progress handler does **not** hide the app or show the install-failed UI; the payload still carries `reason`/`message` so you can surface it yourself. This is the registration-aware version that powers the built-in polling; it is safe to call as often as you like.
- `BitBswup.skipWaiting()`: If an update has finished downloading and is waiting, this activates it immediately (equivalent to calling the `reload` callback you receive in `updateReady`/`downloadFinished`). Returns `true` when there was a waiting worker to activate, otherwise `false`.
- `BitBswup.forceRefresh(cacheFilter?)`: Clears caches, unregisters the service worker controlling the current page, and reloads. Use this as a last-resort "reset" when a client gets into a bad state. It only removes this app's own registration (the one whose scope controls the current page, via `navigator.serviceWorker.getRegistration()`), not every same-origin service worker - so other apps or sub-apps mounted under different scopes on the same origin are left untouched. By default it clears **every** CacheStorage bucket (Bswup, Blazor framework, and any app-owned caches such as Workbox add-ons or API caches) so nothing stale survives the reload. To narrow what gets cleared, pass an optional `cacheFilter`: a string (prefix match against the cache name, e.g. `'bit-bswup'`), a `RegExp` (tested against the cache name), or a predicate function `(key) => boolean` that returns `true` for caches to delete.

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
