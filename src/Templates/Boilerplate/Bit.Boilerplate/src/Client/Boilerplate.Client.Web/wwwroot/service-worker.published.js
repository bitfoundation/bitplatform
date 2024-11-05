//+:cnd:noEmit
// bit version: 8.12.0-pre-10
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
        url: "_framework/blazor.web.js?ver=9.0.0-rc.2.24474.3"
        //#else
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
        url: "_framework/blazor.web.js?ver=8.0.403"
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
    /\/signalr\//,
    /\/healthchecks-ui/,
    /\/healthz/,
    /\/swagger/,
    /\/signin-/,
    /\/.well-known/,
    /\/sitemap.xml/,
];

self.defaultUrl = "/";
self.caseInsensitiveUrl = true;
self.noPrerenderQuery = 'no-prerender=true';
self.isPassive = self.disablePassiveFirstBoot = true;
self.errorTolerance = 'lax';

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');