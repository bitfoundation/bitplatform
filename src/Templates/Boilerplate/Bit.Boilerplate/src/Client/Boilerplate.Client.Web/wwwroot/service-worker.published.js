//+:cnd:noEmit
// bit version: 9.6.0-pre-03
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

//#if (notification == true)

self.addEventListener('push', function (event) {

    const data = event.data.json();

    self.registration.showNotification(data.title, {

        body: data.message,

        icon: '/images/icons/bit-icon-512.png'

    });

});

//#endif

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,
    // If a PDF viewer is needed in the PWA, remove these two lines:
    /pdfjs-4\.7\.76\.js$/,
    /pdfjs-4\.7\.76-worker\.js$/
];
self.externalAssets = [
    {
        "url": "/"
    },
    {
        url: "_framework/blazor.web.js"
    },
    {
        "url": "Boilerplate.Server.Web.styles.css"
    },
    {
        "url": "Boilerplate.Client.Web.bundle.scp.css"
    }
];

self.serverHandledUrls = [
    /\/api\//,
    /\/odata\//,
    /\/jobs\//,
    /\/core\//,
    /\/healthchecks-ui/,
    /\/healthz/,
    /\/swagger/,
    /\/signin-/,
    /\/.well-known/,
    /\/sitemap.xml/,
    //#if (module == "Sales")
    /\/products.xml/,
    //#endif
    /\/sitemap_index.xml/
];

self.prerenderMode = 'none'; // Demo: https://adminpanel.bitplatform.dev/ (No-Prerendering + Offline support)

// On apps with Prerendering enabled, to have the best experience for the end user un-comment one of the following lines:
// self.prerenderMode = 'always'; // Demo: https://sales.bitplatform.dev/ (Always show pre-render without offline support)
// self.prerenderMode = 'initial'; // Demo: https://todo.bitplatform.dev/ (Pre-Render on first site visit + Offline support)

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');