// BevTools sessions register themselves in a process-wide resolver, so tests that create sessions are not isolated
// from each other. Running them sequentially keeps ambient-session resolution deterministic; the suite is fast
// enough that parallelism buys nothing.
[assembly: DoNotParallelize]
