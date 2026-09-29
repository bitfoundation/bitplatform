// bit version: 11.0.0-pre-01
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,

    // the flag image sets of Bit.BlazorUI.Assets: thousands of images, fetched as the flags are drawn
    /_content\/Bit\.BlazorUI\.Assets\/flags/,

    /^_framework\/blazor\.webassembly\.js$/
];
self.externalAssets = [
    {
        "url": "Bit.Websites.Sales.Server.styles.css"
    },
    {
        "url": "Bit.Websites.Sales.Client.bundle.scp.css"
    },
    {
        url: "_framework/blazor.web.js"
    }
];

self.serverHandledUrls = [
    /\/api\//
];

self.mode = 'AlwaysPrerender';
self.enableCacheControl = false;
self.enableIntegrityCheck = false;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');