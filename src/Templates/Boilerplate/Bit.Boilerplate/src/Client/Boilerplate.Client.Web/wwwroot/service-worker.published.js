//+:cnd:noEmit
// bit version: 9.1.1
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
        //#if (framework == "net9.0")
        url: "_framework/blazor.web.js"
        //#else
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
        url: "_framework/blazor.web.js"
            //#if (IsInsideProjectTemplate == true)
            */
            //#endif
        //#endif
    },
    {
        "url": "Boilerplate.Server.Web.styles.css"
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
];

self.defaultUrl = "/";
self.isPassive = true;
self.errorTolerance = 'lax';
self.caseInsensitiveUrl = true;
self.noPrerenderQuery = 'no-prerender=true';


// on apps with Prerendering enabled, to have the best experience for the end user un-comment the following line.
// more info: https://bitplatform.dev/bswup/service-worker
// self.disablePassiveFirstBoot = true;


self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');