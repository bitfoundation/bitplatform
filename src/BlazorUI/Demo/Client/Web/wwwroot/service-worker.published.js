// bit version: 7.1.0-pre-03
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,
];

self.externalAssets = [
    {
        "url": "/"
    },
    {
        "url": "https://www.googletagmanager.com/gtag/js?id=G-G1ET5L69QF"
    }
];

self.serverHandledUrls = [
    /\/api\//,
    /\/odata\//,
    /\/jobs\//,
    /\/core\//,
    /\/signalr\//,
    /\/healthchecks-ui/,
    /\/healthz/,
    /\/swagger/,
    /\/api.fda.gov/
];

self.isPassive = true;
self.defaultUrl = "/";
self.caseInsensitiveUrl = true;
self.noPrerenderQuery = 'no-prerender=true';

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
