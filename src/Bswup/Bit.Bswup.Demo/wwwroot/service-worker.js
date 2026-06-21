// bit version: 10.5.0-pre-02

self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.caseInsensitiveUrl = true;

self.externalAssets = [
    {
        "url": "not-found/script.file.js"
    }
];
// 'lax' opts into best-effort installs: the demo intentionally references a non-existent
// asset to exercise the progress / error reporting UI. Under the default 'strict' setting
// that would abort the install. See README.md > errorTolerance.
self.errorTolerance = 'lax';

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
