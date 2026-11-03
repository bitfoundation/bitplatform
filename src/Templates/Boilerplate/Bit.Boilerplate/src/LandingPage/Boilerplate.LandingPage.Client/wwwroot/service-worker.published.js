// bit version: 11.0.0-pre-16
// https://github.com/bitfoundation/bitplatform/tree/develop/src/Bswup

self.assetsInclude = [];
// Country flags the landing page never shows, and link previews that only crawlers fetch
self.assetsExclude = [
    /_content\/Bit\.BlazorUI\.Extras\/flags/,
    /images\/og\//
];
// Files the server project serves, which the client's assets manifest doesn't list
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

// Every page still comes prerendered from the server; only the app's files are served from the cache
self.mode = 'AlwaysPrerender';

self.enableCacheControl = false;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
