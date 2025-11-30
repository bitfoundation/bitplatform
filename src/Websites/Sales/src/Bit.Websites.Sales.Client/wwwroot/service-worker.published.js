// bit version: 10.2.0-pre-01
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/
];
self.externalAssets = [
    {
        "url": "Bit.Websites.Sales.Server.styles.css"
    },
    {
        "url": "Bit.Websites.Sales.Client.bundle.scp.css"
    },
    {
        url: "_framework/bit.blazor.web.es2019.js"
    }
];

self.serverHandledUrls = [
    /\/api\//
];

self.mode = 'AlwaysPrerender';
self.enableCacheControl = false;
self.enableIntegrityCheck = false;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');