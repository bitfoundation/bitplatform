// [mirror] src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js
// bit version: 11.0.0-pre-16
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
self.assetsExclude = [
    /_content\/Bit\.BlazorUI\.Extras\/flags/,
    /images\/og\//
];
self.externalAssets = [
    {
        url: "_framework/bit.blazor.web.es2019.js"
    },
    {
        url: "Boilerplate.LandingPage.Server.styles.css"
    }
];

self.serverHandledUrls = [
    /\/api\//,
    /\/alive/,
    /\/download/,
    /\/sitemap.xml/,
    /\/robots.txt/,
    /\/llms.txt/
];

self.mode = 'AlwaysPrerender';

self.enableCacheControl = false;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
