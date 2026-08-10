// bit version: 10.6.0-pre-01

self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.caseInsensitiveUrl = true;

self.externalAssets = [
    {
        "url": "not-found/script.file.js"
    }
];
// 'lax' is already the default; it is spelled out here because the sample intentionally
// references a non-existent asset to exercise the progress / error reporting UI, and under
// 'strict' that would abort the install. See README.md > errorTolerance.
self.errorTolerance = 'lax';

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
