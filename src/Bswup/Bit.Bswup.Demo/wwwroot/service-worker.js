// bit version: 10.6.0-pre-01

// Development service worker of the Bswup docs site. The site dogfoods Bswup even during
// development so the full first-install / update experience is visible on plain F5.
self.assetsExclude = [/\.scp\.css$/];
self.caseInsensitiveUrl = true;

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
