﻿//+:cnd:noEmit
// bit version: 8.12.0
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
    /bit\.blazorui\.fluent-light\.css$/
];
self.externalAssets = [
    {
        "url": "/"
    },
    {
        //#if (framework == "net9.0")
        url: "_framework/blazor.web.js?ver=9.0.0"
        //#else
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
        url: "_framework/blazor.web.js?ver=8.0.11"
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


// on apps with Prerendering enabled, to have the best experience for the end user un-comment the following two lines.
// more info: https://bitplatform.dev/bswup/service-worker
// self.noPrerenderQuery = 'no-prerender=true';
// self.disablePassiveFirstBoot = true;


self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');