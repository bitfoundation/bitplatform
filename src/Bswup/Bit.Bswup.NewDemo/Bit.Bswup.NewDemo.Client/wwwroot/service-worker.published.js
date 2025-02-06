// bit version: 9.4.0-pre-03

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
        "url": "_framework/blazor.web.js"
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
