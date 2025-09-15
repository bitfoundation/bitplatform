// bit version: 9.12.0-pre-02
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/
];
self.externalAssets = [
    {
        "url": "Bit.Websites.Platform.Server.styles.css"
    },
    {
        "url": "Bit.Websites.Platform.Client.bundle.scp.css"
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