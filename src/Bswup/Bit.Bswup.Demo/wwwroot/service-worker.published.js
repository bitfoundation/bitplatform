// bit version: 10.5.0

self.assetsExclude = [/\.scp\.css$/];
self.caseInsensitiveUrl = true;

//// Resiliency knobs (see the Bswup README for details):
//self.errorTolerance = 'strict';           // abort the install if any asset fails ('lax' = best-effort lazy-fill, the default)
//self.maxRetries = 2;                      // extra download attempts on transient failures (408/429/5xx, dropped connections)
//self.retryDelay = 300;                    // base backoff in ms between those retries (exponential, with jitter)
//self.enableIntegrityCheck = true;         // attach SRI hashes so tampered assets are rejected (requires byte-identical serving)
//self.cacheVersion = '2026.07.28-abc1234'; // pin/bump the cache bucket independently of the asset manifest

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
