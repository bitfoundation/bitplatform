// bit version: 9.9.2

self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.caseInsensitiveUrl = true;
self.precachedAssetsInclude = [/favicon\.ico$/, /icon-512\.png$/, /bit-bw-64\.png$/];

self.externalAssets = [
    {
        "url": "not-found/script.file.js"
    }
];
self.errorTolerance = 'lax';

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
