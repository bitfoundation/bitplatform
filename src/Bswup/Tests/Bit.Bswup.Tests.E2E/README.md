# Bit.Bswup cross-host test suites

The JS unit tests in `bit-bswup-js` run the shipped bundles inside a fake browser: a `vm` sandbox with
hand-written `caches`, `fetch`, `clients` and `navigator.serviceWorker` doubles. That covers the logic of
the page script, the service worker and the progress UI in depth, and leaves out everything that only a
real browser and a real host decide:

- the service-worker lifecycle itself - registration, scope rules, `updatefound`, the waiting slot,
  `skipWaiting`, `clients.claim()`, `controllerchange`, and the order those events really arrive in;
- CacheStorage, SRI and `fetch()` as a browser implements them (what a rejected integrity check looks
  like, which headers a cached response keeps);
- whether the host actually serves what its service-worker manifest lists, byte for byte;
- Blazor starting under Bswup in each render mode, a standalone app on a sub-path, and the published output.

Because Bswup degrades instead of throwing (a failed install leaves the app running from the network, a
failed update leaves the old version running), these suites assert **effects** - what got cached, whether
the page is controlled, how many documents the tab loaded, which requests reached the server - not the
absence of errors.

## Layout

| Project | What it is |
|---|---|
| `Bit.Bswup.Tests.Harness` | Razor class library: the pages every host renders, `harness.js` (records every Bswup message, counts document loads) and `harness-extra.json` (a precached asset the suites fault). net8.0 / net9.0 / net10.0. |
| `Bit.Bswup.Tests.Harness.Web.Client` | The WebAssembly client of the Blazor Web App; generates the root app's `service-worker-assets.js`. |
| `Bit.Bswup.Tests.Harness.Wasm` | A standalone WebAssembly app (`index.html`, a hand-written `bitBswupHandler`), served under `/standalone/`. |
| `Bit.Bswup.Tests.Harness.Web` | One ASP.NET Core host for both apps, the Blazor Web App in the render mode picked with `--BswupHarness:Mode=`: `wasm`, `wasm-noprerender`, `auto`, `server`. Also the test-controlled server side of a deployment (see below). |
| `Bit.Bswup.Tests.Hosting` | In-process HTTP tests (`WebApplicationFactory`) against the harness host and the FullSample. Runs in the regular CI job on all three target frameworks. |
| `Bit.Bswup.Tests.E2E` | Playwright suites in Chromium. |

### One origin per test

Service worker registrations and CacheStorage belong to an origin, so every test runs on an origin of its
own: `http://<session>.localhost:<port>`. Chromium resolves every `*.localhost` name to the loopback address
and treats it as a secure context, so all tests share one host process per render mode. The host keys the
state of a test by that host name, and the suite drives it through `/_harness/` endpoints:

| Endpoint | Effect |
|---|---|
| `PUT /_harness/options` | The worker script (`bswup`, `cleanup`, `missing`), its `self.*` settings, the `bit-bswup.js` script-tag options, the `BswupProgress` parameters and placement. |
| `PUT /_harness/version` | Publishes a new release: `service-worker.js` bytes and the manifest version change, hashed assets do not. |
| `PUT /_harness/offline` | Drops every other request, as a lost connection does. |
| `POST` / `DELETE /_harness/faults` | Delays, drops, fails (status) or corrupts the requests matching a pattern, a number of times. |
| `GET` / `DELETE /_harness/requests` | Every request the browser made on the test's origin. |

## What runs where

| Area | Hosting (HTTP) | E2E, every host variant | E2E, WebAssembly host | Publish gate |
|---|---|---|---|---|
| Every precached manifest asset served, bytes matching its SRI hash (harness and FullSample) | yes | - | - | - |
| Bswup scripts and stylesheet served, host document leaves `Blazor.start` to Bswup | yes | - | - | - |
| `BswupProgress` attributes, update button outside the overlay, no inline script | yes | - | - | - |
| The `noPrerenderQuery` document carries no prerendered output | yes | - | - | - |
| First install: precache, claim and start in the same document, no worker scripts cached | - | yes | - | yes |
| Later visit served from the cache (no asset or document request) | - | yes | - | yes |
| Offline start and deep link from the cache | - | WebAssembly hosts | - | yes |
| Update: download, activate, reload once, prune the old bucket, migrate hashed assets, refresh hash-less ones | - | yes | - | yes |
| `checkForUpdate`: up to date, and a failed check while offline | - | yes | - | yes |
| Strict first-install abort still starts the app (failure panel); lax asset failure skipped, then cached on first use | - | yes | - | yes |
| `prohibitedUrls` (403, never reaches the server), `serverHandledUrls`, `serverRenderedUrls` | - | - | yes | yes |
| Asset navigation offline, `Range` requests answered with 206 from the cache | - | - | yes | yes |
| Passive mode, SRI accepted and tampered bytes rejected, transient failures retried, broken manifest, stall watchdog | - | - | yes | yes |
| `AutoReload="false"` prompt, several tabs, update staged at load, `updateInterval` polling, failed update, two apps on one origin | - | - | yes | yes |
| `forceRefresh()`, the cleanup worker, hard reload | - | - | yes | yes |
| Progress UI: splash and assets list, `ShowOnUpdate`, `HideApp`, interactively rendered `BswupProgress`, no handler at all | - | - | yes | yes |
| Refused root scope on a sub-path | - | - | yes | yes |
| Fingerprinted `blazor.web.js` (`@Assets`) | - | - | yes (net10.0+) | yes |

## Running locally

```bash
cd src/Bswup

# HTTP-level suite, all target frameworks
dotnet test Tests/Bit.Bswup.Tests.Hosting/Bit.Bswup.Tests.Hosting.csproj

# Browser suites (builds the harness host first)
dotnet build Tests/Bit.Bswup.Tests.E2E/Bit.Bswup.Tests.E2E.csproj
pwsh Tests/Bit.Bswup.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test Tests/Bit.Bswup.Tests.E2E/Bit.Bswup.Tests.E2E.csproj

# One class only
dotnet test Tests/Bit.Bswup.Tests.E2E/Bit.Bswup.Tests.E2E.csproj --filter "FullyQualifiedName~.UpdateTests."
```

When Playwright's browser download is not available, point the suite at an installed browser with
`BSWUP_E2E_CHANNEL=chrome` (or `msedge`) instead of running `playwright.ps1 install`.

Run the harness host by hand with one of its launch profiles (`wasm`, `wasm-noprerender`, `auto`, `server`),
or `dotnet run --project Tests/Bit.Bswup.Tests.Harness.Web -- --BswupHarness:Mode=server`. Opened at
`http://localhost:5390/` it is the "default" session; `http://anything.localhost:5390/` is a fresh one.

### Environment variables

| Variable | Effect |
|---|---|
| `BSWUP_E2E_FRAMEWORK` | Target framework of the harness host: `net10.0` (default), `net9.0`, `net8.0`. |
| `BSWUP_E2E_CONFIGURATION` | Build configuration of the host; defaults to the test assembly's. |
| `BSWUP_E2E_SKIP_BUILD=1` | Do not build the host before the run. |
| `BSWUP_E2E_PUBLISHED_HOST` | Run a `dotnet publish` output of the host instead of the build output. |
| `BSWUP_E2E_CHANNEL` / `BSWUP_E2E_EXECUTABLE` | Use an installed Chrome/Edge or a specific Chromium binary. |
| `BSWUP_E2E_HEADED=1` | Show the browser. |

### Publish gate

```bash
dotnet publish Tests/Bit.Bswup.Tests.Harness.Web -c Release -f net10.0 -p:TargetFrameworks=net10.0 -o ../../artifacts/bswup-harness
BSWUP_E2E_PUBLISHED_HOST=$PWD/../../artifacts/bswup-harness dotnet test Tests/Bit.Bswup.Tests.E2E/Bit.Bswup.Tests.E2E.csproj
```

The published host serves trimmed clients, pre-compressed assets and the manifests generated for publish;
the integrity-checked install proves the bytes it serves are the ones those manifests describe.

CI runs all of the above in `.github/workflows/bit.ci.Bswup.e2e.yml`.
