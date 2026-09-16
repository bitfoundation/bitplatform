// bit version: 10.6.0
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,
    /Bit\.BlazorUI\.Demo.Client\.Web\.styles\.css$/,

    // the flag image sets of Bit.BlazorUI.Assets: thousands of images, fetched as the flags are drawn
    /_content\/Bit\.BlazorUI\.Assets\/flags/
];
self.externalAssets = [
    {
        "url": "Bit.BlazorUI.Demo.Server.styles.css"
    },
    {
        url: "_framework/bit.blazor.web.es2019.js"
    }
];

self.serverHandledUrls = [
    /\/api\//,
    /\/swagger/,
    /\/api.fda.gov/
];

self.enableCacheControl = false;

self.mode = 'AlwaysPrerender';
self.enableIntegrityCheck = false;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');