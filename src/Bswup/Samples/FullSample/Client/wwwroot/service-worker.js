// bit version: 10.6.0-pre-01

// Development service worker of the FullSample. Unlike the standard Blazor template - whose
// dev worker is a no-op so caching never hides source changes - this sample runs the full
// Bswup engine even in development (see the importScripts at the bottom), so the complete
// first-install / update experience is exercised on plain F5, matching the other samples.

self.assetsInclude = [];
// The client's scoped-css bundle is in this app's asset manifest but is never served: in a
// Blazor Web App the host project merges the client's scoped styles into its own
// <HostAssembly>.styles.css. Precaching it would fail with a 404, so it's excluded here and
// the two files the page actually loads are precached through externalAssets instead.
self.assetsExclude = [/^Bit\.Bswup\.FullSample\.Client\.styles\.css$/, /weather\.json$/];
self.defaultUrl = '/';
self.prohibitedUrls = [];
// self.assetsUrl is deliberately NOT set: since v-10-6-0 it defaults to a relative
// 'service-worker-assets.js', resolved next to this service-worker file - which keeps
// sub-path-mounted apps working. Set it explicitly only when the file lives elsewhere.

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
    // The host's scoped-css bundle (linked from App.razor) and the client bundle it @imports.
    // Both are served by the host project, so they're absent from the client asset manifest.
    {
        "url": "Bit.Bswup.FullSample.Server.styles.css"
    },
    {
        "url": "Bit.Bswup.FullSample.Client.bundle.scp.css"
    },
    // A Blazor Web App boots through blazor.web.js, not the blazor.webassembly.js that the
    // client's asset manifest lists - it belongs to the host project, so the manifest never
    // sees it and the app cannot start offline without this entry.
    {
        "url": "_framework/blazor.web.js"
    },
    // blazor.web.js then loads the WASM resource list from a fingerprinted
    // resource-collection.<hash>.js, named by the host at build time. The concrete name is
    // unknown here and changes every build, so it is matched with a RegExp and cached lazily
    // on first fetch (see the externalAssets notes in the Bswup README).
    {
        "url": /\/_framework\/resource-collection\.[^\/]*\.js$/
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
