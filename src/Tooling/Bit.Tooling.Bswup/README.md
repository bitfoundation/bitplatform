## Bit Blazor Serviceworker Update Progress (BitBswup)

to use BitBswup, please follow these steps:

1.Install the `Bit.Tooling.Bswup` nuget package
2. Disable static file caching.You can follow below code in `Startup.cs` file
```csharp

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };
        }
    });
}

```
**In the default document based:**(`index.html` or `_Host.cshtml`)

4.Add an `autostart = "false"` attribute and value to the <script> tag for the Blazor script.

```html

<script src="_framework/blazor.webassembly.js" autostart="false"></script>

```

5. Add the `Bit.Tooling.Bswup` reference after the <script> tag for the Blazor script.
```html

<script src="_content/Bit.Tooling.Bswup/bit-bswup.js"
            scope="/"
            log="verbose"
            sw="service-worker.js"
            handler="bitBswupHandler"></script>

```

- scope: The scope of the service worker determines which files the service worker controls.
- log: The log level for log provider. log options: `info`, `verbose`, `debug`, `error`
-sw: The sw is name and path service worker file.
- handler: The name of handler for the service worker events

6. Add a handler in the simplest way possible, like the below code. or you can add a handler with a process bar like the bitBswupHandler on the sample in the index.html file of the demo project in this repo.

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
            reloadButton.style.display = 'block';
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
7.Configure additional settings in the service worker file (based on the sample shown in the `service-worker.js` file of the demo project)

**Service Worker * *
- `self.assetsInclude`: The list of files or regex of files to be cached.
- `self.assetsExclude`: The list of files or regex of files that should not be cached.
- `self.defaultUrl`: The default page url.When use `_Host.cshtml` set `/`
- `self.prohibitedUrls`: The list of files or regex of files that should not be accessed.
- `self.assetsUrl`: The url address of service worker assets.
- `self.externalAssets`: The list of external assets.If don't use `index.html` for default url you should add this `{ "url": "/" }` item.
- `self.caseInsensitiveUrl`: If set true you can check case insensitive url in the cache process.
- `self.serverHandledUrls`: The list of urls or regex that do not enter the service worker process. ex. `api`
- `self.serverRenderedUrls`: The list of urls or regex that should be cached by the server after rendering. ex. `about.html`