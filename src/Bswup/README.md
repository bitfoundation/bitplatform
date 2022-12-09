## Bit Blazor Service Worker Update Progress (Bit Bswup)

To use BitBswup, please follow these steps:

1. Install the `Bit.Bswup` nuget package.

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

3. In the default document (`index.html`, `_Host.cshtml`, or `_Layout.cshtml`), add an `autostart="false"` attribute and value to the script tag for the Blazor script.

```html
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
```

4. Also In the default document (`index.html`, `_Host.cshtml`, or `_Layout.cshtml`), add the `Bit.Bswup` reference after the script tag for the Blazor script.

```html
<script src="_content/Bit.Bswup/bit-bswup.js"
        scope="/"
        log="verbose"
        sw="service-worker.js"
        handler="bitBswupHandler"></script>
```

- `scope`: The scope of the service worker ([read more](https://developer.chrome.com/docs/workbox/service-worker-lifecycle/#scope)).
- `log`: The log level of the Bswup logger. available options are: `info`, `verbose`, `debug`, and `error`. (not implemented yet)
- `sw`: The file path of the service worker file.
- `handler`: The name of the handler function for the service worker events.

> You can remove any of these attributes, and use the default values mentioned above.

5. Add a handler function like the below code to handle multiple events of the Bswup, or you can follow the full sample code which is provided in the Demo projects of this repo.

```js
function bitBswupHandler(type, data) {
    switch (type)
    {
        case 'updatefound':
            return console.log('new version is downloading...');
        case 'statechange':
            return console.log('new version state has changed to:', data.currentTarget.state);
        case 'controllerchange':
            return console.log('sw controller changed:', data);
        case 'installing':
            return console.log('installing new version:', data.version);
        case 'installed':
            console.log('new version installed:', data.version)
            data.reload();
            return;
        case 'progress':
            return console.log('asset downloaded:', data);
        case 'activate':
            return console.log('new version activated:', data.version);
    }
}
```

6. Configure additional settings in the service worker file like the following code:

```js
// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
//self.addEventListener('fetch', () => { });

self.assetsInclude = [];
self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.defaultUrl = 'index.html';
self.prohibitedUrls = [];
self.assetsUrl = '/service-worker-assets.js';

// more about SRI (Subresource Integrity) here: https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity
// online tool to generate integrity hash: https://www.srihash.org/   or   https://laysent.github.io/sri-hash-generator/
// using only js to generate hash: https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest
self.externalAssets = [
    //{
    //    "hash": "sha256-lDAEEaul32OkTANWkZgjgs4sFCsMdLsR5NJxrjVcXdo=",
    //    "url": "css/app.css"
    //},
    {
        "url": "/"
    },
];

self.caseInsensitiveUrl = true;

self.serverHandledUrls = [/\/api\//];
self.serverRenderedUrls = [/\/privacy$/];

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
```

The most important line of code which is the only mandatory config in this file is the last line of importing the Bswup service worker file:

```js
self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
```

The other settings are:

- `self.assetsInclude`: The list of file names to be cached (regex supported).
- `self.assetsExclude`: The list of file names that should not be cached (regex supported).
- `self.defaultUrl`: The default page url. Use `/` when using `_Host.cshtml`.
- `self.prohibitedUrls`: The list of file names that should not be accessed (regex supported).
- `self.assetsUrl`: The url address of service worker assets.
- `self.externalAssets`: The list of external assets. If you're not using `index.html` for the default url, then you should add this `{ "url": "/" }` item.
- `self.caseInsensitiveUrl`: If set true you can check case insensitive URL in the cache process.
- `self.serverHandledUrls`: The list of URLs or regex that do not enter the service worker process. ex. `api, swagger, ...`
- `self.serverRenderedUrls`: The list of URLs or regex that should be cached by the server after rendering. ex. `about.html`