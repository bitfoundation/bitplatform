// bit version: 9.12.0
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/
];
self.externalAssets = [
    {
        "url": "Bit.Websites.Careers.Server.styles.css"
    },
    {
        "url": "Bit.Websites.Careers.Client.bundle.scp.css"
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