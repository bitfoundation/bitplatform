//+:cnd:noEmit
// bit version: 9.7.4-pre-03
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

    // If a PDF reader (https://blazorui.bitplatform.dev/components/pdfreader) is needed in the PWA, remove these two lines:
    /pdfjs-4\.7\.76\.js$/,
    /pdfjs-4\.7\.76-worker\.js$/,

    //#if (module != "Admin")
    /chartjs-2\.9\.4\.js$/,
    /chartjs-2\.9\.4-adapter\.js$/,

    // If a RichTextEditor (https://blazorui.bitplatform.dev/components/richtexteditor) is needed in the PWA, remove the following lines:
    /quill-2\.0\.3\.js$/,
    /quill.snow-2\.0\.3\.css$/,
    /quill.bubble-2\.0\.3\.css$/,
    //#endif

    // country flags
    /_content\/Bit\.BlazorUI\.Extras\/flags/,

    // https://github.com/orgs/bitfoundation/discussions/10238#discussioncomment-12493737
    /_content\/Bit\.BlazorES2019\/blazor\.server\.js$/,
    /_content\/Bit\.BlazorES2019\/blazor\.webview\.js$/,
    /_framework\/blazor\.web\.js$/,
    /_framework\/blazor\.webassembly\.js$/
];
self.externalAssets = [
    {
        "url": "/"
    },
    /* If you don't plan to support older browsers and prefer to use the original `blazor.web.js`, follow the instructions in this link: https://github.com/orgs/bitfoundation/discussions/10238#discussioncomment-12493737
    {
        url: "_framework/blazor.web.js"
    },
    */
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
    /\/core\//,
    /\/hangfire/,
    /\/healthchecks-ui/,
    /\/healthz/,
    /\/swagger/,
    /\/signin-/,
    /\/.well-known/,
    /\/sitemap.xml/,
    //#if (module == "Sales")
    /\/products.xml/,
    //#endif
    /\/sitemap_index.xml/,
    /\/hybrid-app-web-interop/
];

self.prerenderMode = 'none'; // Demo: https://adminpanel.bitplatform.dev/ (No-Prerendering + Offline support)

// On apps with Prerendering enabled, to have the best experience for the end user un-comment one of the following lines:
// self.prerenderMode = 'always'; // Demo: https://sales.bitplatform.dev/ (Always show pre-render without offline support)
// self.prerenderMode = 'initial'; // Demo: https://todo.bitplatform.dev/ (Pre-Render on first site visit + Offline support)

self.enableIntegrityCheck = false;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');