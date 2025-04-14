// bit version: 9.7.0-pre-06
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
    }
];

self.serverHandledUrls = [
    /\/api\//,
    /\/swagger/,
    /\/api.fda.gov/
];

self.prerenderMode = 'always';
self.enableIntegrityCheck = false;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');