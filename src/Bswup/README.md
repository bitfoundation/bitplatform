## bit Blazor Service-Worker Update Progress (bit Bswup)

To use the bit Bswup, please follow these steps:

1. Install the `Bit.Bswup` nuget package:
```bat
dotnet add package Bit.Bswup
```

2. Disable static file caching. You can follow the below code in the `Startup.cs` file:

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
        {
            NoCache = true
        };
    }
});
```

3. In the default document (`index.html`, `_Host.cshtml`, or `_Layout.cshtml`), add `autostart="false"` to the script tag for the Blazor script:

```html
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
```

4. Also In the default document (`index.html`, `_Host.cshtml`, or `_Layout.cshtml`), add the  `bit-bswup.js` script tag after the Blazor script tag with needed options:

```html
<script src="_content/Bit.Bswup/bit-bswup.js"
        scope="/"
        log="verbose"
        sw="service-worker.js"
        handler="bitBswupHandler"></script>
```

- `scope`: The scope of the service-worker ([read more](https://developer.chrome.com/docs/workbox/service-worker-lifecycle/#scope)).
- `log`: The log level of the Bswup logger. available options are: `info`, `verbose`, `debug`, and `error`. (not implemented yet)
- `sw`: The file path of the service-worker file.
- `handler`: The name of the handler function for the service-worker events.

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
    }
}
```

6. Configure additional settings in the service-worker file like the following code:

```js
self.assetsInclude = [];
self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.defaultUrl = 'index.html';
self.prohibitedUrls = [];
self.assetsUrl = '/service-worker-assets.js';
self.externalAssets = [
    {
        "url": "/"
    },
    {
        "url": "https://www.googletagmanager.com/gtag/js?id=G-G1ET5L69QF"
    }
];

self.caseInsensitiveUrl = true;

self.serverHandledUrls = [/\/api\//];
self.serverRenderedUrls = [/\/privacy$/];

self.noPrerenderQuery = 'no-prerender=true';

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
```

The most important line of code which is the only mandatory config in this file is the last line of importing the Bswup service-worker file:

```js
self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
```

The other settings are:

- `self.assetsInclude`: The list of file names from the assets list to **include** when the Bswup tries to store them in the cache storage (regex supported).
- `self.assetsExclude`: The list of file names from the assets list to **exclude** when the Bswup tries to store them in the cache storage (regex supported).
- `self.externalAssets`: The list of external assets to cache that are not included in the auto-generated assets file. For example, if you're not using `index.html` (like `_host.cshtml`), then you should add `{ "url": "/" }`.
- `self.defaultUrl`: The default page URL. Use `/` when using `_Host.cshtml`.
- `self.assetsUrl`: The file path of the service-worker assets file generated at compile time (the default file name is `service-worker-assets.js`).
- `self.prohibitedUrls`: The list of file names that should not be accessed (regex supported).
- `self.caseInsensitiveUrl`: Enables the case insensitivity in the URL checking of the cache process.
- `self.serverHandledUrls`: The list of URLs that do not enter the service-worker offline process and will be handled only by server (regex supported). such as `/api`, `/swagger`, ...
- `self.serverRenderedUrls`: The list of URLs that should be rendered by the server and not client while navigating (regex supported). such as `/about.html`, `/privacy`, ...
- `self.noPrerenderQuery`: The query string attached to the default document request to disable the prerendering from the server so an unwanted prerendered result not be cached.
- `self.ignoreDefaultInclude`: Ignores the default asset **includes** array which is provided by the Bswup itself which is like this: 
    ```js
    [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.woff2$/, /\.ttf$/, /\.webp$/]
    ```
- `self.ignoreDefaultExclude`: Ignores the default asset **excludes** array which is provided by the Bswup itself which is like this: 
    ```js
    [/^_content\/Bit\.Bswup\/bit-bswup\.sw\.js$/, /^service-worker\.js$/]
    ```
    #### Keep in mind that caching service-worker related files will corrupt the update cycle of the service-worker. Only the browser should handle these files. 
- `self.isPassive`: Enables the Bswup's passive mode. In this mode, the assets won't be cached in advance but rather upon initial request.
- `self.precachedAssetsInclude`: The list of file names from the assets list to be **included** in the precache process of the passive mode (regex supported).
- `self.precachedAssetsExclude`: The list of file names from the assets list to be **excluded** from the precache process of the passive mode (regex supported).
- `self.precachedExternalAssets`: The list of external assets to store in the precache phase of the passive mode that are not included in the auto-generated assets file.
- `self.ignoreDefaultPrecach`: Ignores the default assets includes for precaching in passive mode. the default value is like this:
    ```js
    [new RegExp(`${DEFAULT_URL}$`), /manifest\.json$/, /blazor\.webassembly\.js$/, /bit-bswup\.js$/]
    ```
- `self.enableIntegrityCheck`: Enables the default integrity check available in browsers by setting the `integrity` attribute of the request object created in the service-worker to fetch the assets.