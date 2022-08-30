// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup#service-worker

self.assetsInclude = [
    /\.svg$/,
    /FabExMDL*/
];

self.assetsExclude = [
    /\.scp\.css$/,
    /bit\.blazorui\.cupertino\.min\.css$/,
    /bit\.blazorui\.cupertino\.css$/,
    /bit\.blazorui\.fluent\.min\.css$/,
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.material\.min\.css$/,
    /bit\.blazorui\.material\.css$/,
    /app\.css/
];

self.externalAssets = [
    {
        "url": "/"
    },
];

self.serverHandledUrls = [
    /\/api\//,
    /\/odata\//,
    /\/jobs\//,
    /\/core\//,
    /\/signalr\//,
    /\/healthchecks-ui/,
    /\/healthz/,
    /\/swagger/
];

self.defaultUrl = "/";

self.caseInsensitiveUrl = true;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
