// bit version: 9.7.0-pre-08
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
    }
];

self.serverHandledUrls = [
    /\/api\//
];

self.prerenderMode = 'always';
self.enableIntegrityCheck = false;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');