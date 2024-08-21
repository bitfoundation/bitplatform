// bit version: 8.11.0-pre-03

// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
//self.addEventListener('fetch', () => { });

self.assetsInclude = [];
self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.defaultUrl = '/';
self.prohibitedUrls = [];
self.assetsUrl = '/service-worker-assets.js';

// more about SRI (Subresource Integrity) here: https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity
// online tool to generate integrity hash: https://www.srihash.org/   or   https://laysent.github.io/sri-hash-generator/
// using only js to generate hash: https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest
self.externalAssets = [
    //{
    //    "hash": "sha256-lDAEEaul32OkTANWkZgjgs4sFCsMdLsR5NJxrjVcXdo=",
    //    "url": "css/app.css"
    //},
    {
        "url": "/"
    },
    {
        "url": "https://www.googletagmanager.com/gtag/js?id=G-G1ET5L69QF"
    }
];

self.caseInsensitiveUrl = true;

self.serverHandledUrls = [/\/api\//];
self.serverRenderedUrls = [/\/privacy$/];

self.noPrerenderQuery = 'no-prerender=true';

self.isPassive = true;

//self.enableDiagnostics = true;
//self.enableFetchDiagnostics = true;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
