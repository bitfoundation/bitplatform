// The harness host never serves this file: it generates service-worker.js for each test session (see
// WorkerScript in Bit.Bswup.Tests.Harness.Web). It exists because the ServiceWorker item - which is
// what makes the build generate service-worker-assets.js - needs a source file, exactly as in an app.
self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
