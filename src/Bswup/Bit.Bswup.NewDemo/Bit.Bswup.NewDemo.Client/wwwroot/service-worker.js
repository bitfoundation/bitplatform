// bit version: 9.4.0-pre-03

// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
//self.addEventListener('fetch', () => { });

self.assetsInclude = [];
self.assetsExclude = [
    /Bit\.Bswup\.NewDemo\.Client\.styles\.css$/
];
self.defaultUrl = '/';
self.prohibitedUrls = [];
self.assetsUrl = '/service-worker-assets.js';

// more about SRI (Subresource Integrity) here: https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity
// online tool to generate integrity hash: https://www.srihash.org/   or   https://laysent.github.io/sri-hash-generator/
// using only js to generate hash: https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest
self.externalAssets = [
    {
        "url": "/"
    },
    {
        "url": "app.css"
    },
    {
        "url": "_framework/blazor.web.js?v=9.0.0"
    },
    {
        "url": "Bit.Bswup.NewDemo.styles.css"
    },
    {
        "url": "Bit.Bswup.NewDemo.Client.bundle.scp.css"
    }
];

self.caseInsensitiveUrl = true;

self.serverHandledUrls = [/\/api\//];
self.serverRenderedUrls = [/\/privacy$/];

self.noPrerenderQuery = 'no-prerender=true';

self.isPassive = true;
self.disablePassiveFirstBoot = true;

//self.enableDiagnostics = true;
//self.enableFetchDiagnostics = true;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
