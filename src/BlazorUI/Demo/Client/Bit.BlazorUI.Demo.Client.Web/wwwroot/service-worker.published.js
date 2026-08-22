// bit version: 10.6.0-pre-02
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,
    /Bit\.BlazorUI\.Demo.Client\.Web\.styles\.css$/,
    // Dropped from the hashed manifest set and re-added hashless below - see the note there.
    /pdf\.js\/pdfjs-[\d.]+-worker\.js$/,
    /_framework\/dotnet\.native\.worker\.mjs$/
];
self.externalAssets = [
    {
        "url": "Bit.BlazorUI.Demo.Server.styles.css"
    },
    {
        url: "_framework/bit.blazor.web.es2019.js"
    },
    {
        // The multi-threaded .NET WebAssembly runtime spins its threads up as dedicated workers off
        // this script, so it faces the same embedder-policy check as the PDF.js worker below. It is
        // in the hashed manifest and its bytes never change, so an update migrates the cached
        // response verbatim - headers included - and a returning PWA visitor would keep being
        // served the pre-switch 'credentialless' copy against a 'require-corp' document, leaving
        // the threaded runtime unable to boot. The no-cache in Middlewares.cs governs the HTTP
        // cache only, not this Cache API copy, so the same hashless treatment is what fixes it.
        url: "_framework/dotnet.native.worker.mjs"
    },
    {
        // PDF.js runs its parser in a dedicated worker, and a dedicated worker's script response
        // has its Cross-Origin-Embedder-Policy compared against the owner document's ("check a
        // global object's embedder policy"): a mismatch turns the script into a network error.
        // This site's COEP changed (credentialless -> require-corp, see Middlewares.cs), and an
        // update keeps a cached response whose hash did not change - headers included - so the copy
        // taken before the switch would keep saying 'credentialless', which WebKit reads as
        // unsafe-none. Listing it hashless makes Bswup re-download it on every update, so its
        // headers stay in step with the server while it remains precached for offline use.
        url: "_content/Bit.BlazorUI.Legacy/pdf.js/pdfjs-4.7.76-worker.js"
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
