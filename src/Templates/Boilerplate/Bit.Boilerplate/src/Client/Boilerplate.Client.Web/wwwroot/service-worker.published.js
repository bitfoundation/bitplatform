//+:cnd:noEmit
// bit version: 10.5.0-pre-10
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

//#if (notification == true)

self.addEventListener('push', function (event) {

    const eventData = event.data.json();

    self.registration.showNotification(eventData.title, {

        data: eventData.data,
        body: eventData.message,
        icon: '/images/icons/bit-icon-512.png'

    });

});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const pageUrl = event.notification.data.pageUrl;
    if (pageUrl != null) {
        event.waitUntil(
            clients
                .matchAll({
                    type: 'window',
                    includeUncontrolled: true,
                })
                .then((clientList) => {
                    for (const client of clientList) {
                        if (!client.focus || !client.postMessage) continue;
                        client.postMessage({ key: 'PUBLISH_MESSAGE', message: 'NAVIGATE_TO', payload: pageUrl });
                        return client.focus();
                    }
                    return clients.openWindow(pageUrl);
                })
        );
    }
});

//#endif

self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,

    // country flags
    /_content\/Bit\.BlazorUI\.Extras\/flags/
];
self.externalAssets = [
    {
        "url": "/"
    },
    {
        url: "_framework/bit.blazor.web.es2019.js"
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
    /\/core\//,
    /\/hangfire/,
    /\/healthchecks-ui/,
    /\/healthz/,
    /\/health/,
    /\/alive/,
    /\/swagger/,
    /\/scalar/,
    /\/signin-/,
    /\/.well-known/,
    /\/sitemap.xml/,
    //#if (module == "Sales")
    /\/products.xml/,
    //#endif
    /\/sitemap_index.xml/,
    /\/llms.txt/,
    /\/web-interop-app.html/
];

// self.mode = 'FullOffline'; // Traditional PWA app that **first** downloads all assets and **then** runs the app.
// This ensures the app won't break if network connectivity is lost and the user navigates to a new page requiring lazy-loaded JS/WASM/image files.
// Recommended if the app primarily uses PWA for offline support and has local/offline database such as IndexedeDB or SQLite (Checkout Bit.Besql)
// Demo: https://todo-offline.bitplatform.cc/offline-todo

self.mode = 'NoPrerender'; // Modern PWA app that **starts immediately** and lazy-loads assets as needed.
// If network connectivity is lost and the user navigates to a new page requiring lazy-loaded JS/WASM/image files, the app might break.
// Recommended if the app uses PWA for benefits other than offline support, such as installability, push notifications, etc.
// Demo: https://adminpanel.bitplatform.dev/

// self.mode = 'InitialPrerender'; // If pre-rendering is enabled in the `Server.Web` configuration, this mode fetches the site's document only on the first load of the app.
// Useful for SEO-friendly apps, and to display content on the initial visit while files download. Subsequent visits avoid server pressure from pre-rendering.
// Demo: https://todo.bitplatform.dev/

// self.mode = 'AlwaysPrerender'; // If pre-rendering is enabled in the Server.Web configuration, this mode fetches the site's document on every load of the app.
// The reason behind fetching the document on every app load is that Blazor WebAssembly's runtime might takes some time to kick in on low-end mobile devices,
// so if the user refreshes the page or visits a new page, it shows the pre-rendered document while the Blazor WebAssembly runtime is loading.
// Downside: Increases server load due to frequent pre-rendering.
// Demo: https://sales.bitplatform.dev/

self.enableCacheControl = false; // false means origin's cache headers are respected, true means service worker would manage the cache headers.

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');