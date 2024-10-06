// bit version: 8.12.0-pre-02
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.addEventListener('push', function (event) {

    const data = event.data.json();

    self.registration.showNotification(data.notification.title, {

        body: data.notification.body,

        icon: '/images/icons/bit-icon-512.png'

    });

});

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
        url: "_framework/blazor.web.js"
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