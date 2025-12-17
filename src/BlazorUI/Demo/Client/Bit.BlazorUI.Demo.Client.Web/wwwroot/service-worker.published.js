// bit version: 10.2.1-pre-02
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,
    /Bit\.BlazorUI\.Demo.Client\.Web\.styles\.css$/
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