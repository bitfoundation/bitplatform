# Bit.Brouter cross-host test suites

The unit tests in `Bit.Brouter.Tests` run Brouter inside bUnit: one interactive renderer, a fake
`NavigationManager`, and JS interop that silently returns `default` for every call. That covers the
routing logic well and leaves out everything that depends on the host:

- the 530 lines of `bit-brouter.ts` (scroll restoration, focus, view transitions, link preloading,
  the external-navigation prompt) never execute;
- `RendererInfo` is never populated, so Brouter treats every test as interactive and its
  static-rendering-only branches (the HTTP 404 propagation among them) never run;
- there is no HTTP response, so status codes, redirects and the persisted prerender state are invisible;
- there is no prerender-then-hydrate handover, no Auto runtime switch, no BlazorWebView, and no
  trimmed or AOT-compiled build.

Because Brouter swallows JS interop failures by design, most host-level breakage does not throw: a
module that fails to load simply switches scrolling, focus, transitions and preloading off. These
suites therefore assert **effects**, not the absence of errors.

## Layout

| Project | What it is |
|---|---|
| `Bit.Brouter.Tests.Harness` | Razor class library: one router and a page per feature, every element the tests touch carries an `id`. net8.0 / net9.0 / net10.0. |
| `Bit.Brouter.Tests.Harness.Web` (+ `.Client`) | One Blazor Web App serving the harness in any render mode, picked with `--BrouterHarness:Mode=`: `ssr`, `server`, `server-noprerender`, `wasm`, `wasm-noprerender`, `auto`, `auto-noprerender`. |
| `Bit.Brouter.Tests.Harness.Hybrid` | WinForms `BlazorWebView` host (same WebView core as MAUI). Windows only. |
| `Bit.Brouter.Tests.Hosting` | In-process HTTP tests (`WebApplicationFactory`) against the harness host in every mode and against the three hosting samples. Runs in the regular CI job on all three target frameworks. |
| `Bit.Brouter.Tests.E2E` | Playwright suites: every web render mode in Chromium, and the hybrid host through WebView2's DevTools port. |

## What runs where

| Area | Hosting (HTTP) | E2E web modes | E2E hybrid | Publish gate |
|---|---|---|---|---|
| Deep link rendered into the first response | yes | yes | - | trimmed + AOT |
| 404 status and not-found content for unmatched URLs / `NavigationManager.NotFound()` | yes | yes | content only | yes |
| Guard redirects and `RedirectTo` (302 during static rendering, client-side when interactive) | yes | yes | yes | yes |
| Loaders, route error content | yes | yes | yes | yes |
| `PersistLoaderState` written during prerender and restored instead of re-run | written | restored | - | yes |
| No JS interop attempted and no errors logged during static rendering | yes | module never requested | - | - |
| `bit-brouter.js` served, loads from the app base on a deep URL | served | loads | loads | yes |
| Link navigation without reload, back/forward, history state, `Replace` links | - | yes | yes | yes |
| Scroll to top, focus on navigate, fragment scrolling, Back restores scroll | - | yes | yes | yes |
| View transitions (push/pop/replace direction, never on initial load) | - | yes | yes | yes |
| Link preloading (intent, viewport) reusing the cached result | - | yes | yes | yes |
| Leave guards (link and browser Back), keep-alive | - | yes | yes | yes |
| Modified click keeps native new-tab behavior; `beforeunload` prompt | - | yes | not applicable | yes |
| Auto switching a later visit to WebAssembly | - | yes | - | yes |
| `BlazorWebView.StartPath` deep link | - | - | yes | - |
| The hosting samples boot and route | yes (net10.0) | - | - | - |

## Running locally

```bash
cd src/Brouter

# HTTP-level suite, all target frameworks
dotnet test Tests/Bit.Brouter.Tests.Hosting/Bit.Brouter.Tests.Hosting.csproj

# Browser suites (builds the harness hosts first; the hybrid host only on Windows)
dotnet build Tests/Bit.Brouter.Tests.E2E/Bit.Brouter.Tests.E2E.csproj
pwsh Tests/Bit.Brouter.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test Tests/Bit.Brouter.Tests.E2E/Bit.Brouter.Tests.E2E.csproj

# One mode only
dotnet test Tests/Bit.Brouter.Tests.E2E/Bit.Brouter.Tests.E2E.csproj --filter "FullyQualifiedName~.WebAssemblyModeTests."
```

Run a harness host by hand with one of its launch profiles (`ssr`, `server`, `wasm`, `auto`), or
`dotnet run --project Tests/Bit.Brouter.Tests.Harness.Web -- --BrouterHarness:Mode=server-noprerender`.

### Environment variables

| Variable | Effect |
|---|---|
| `BROUTER_E2E_FRAMEWORK` | Target framework of the web harness host: `net10.0` (default), `net9.0`, `net8.0`. |
| `BROUTER_E2E_CONFIGURATION` | Build configuration of the hosts; defaults to the test assembly's. |
| `BROUTER_E2E_SKIP_BUILD=1` | Do not build the hosts before the run. |
| `BROUTER_E2E_PUBLISHED_HOST` | Run a `dotnet publish` output of the web host instead of the build output. |
| `BROUTER_E2E_CHANNEL` / `BROUTER_E2E_EXECUTABLE` | Use an installed Chrome/Edge or a specific Chromium binary. |
| `BROUTER_E2E_HEADED=1` | Show the browser. |

### Publish gate (trimming / AOT)

```bash
# Publishes trimmed and fails on any trim/AOT analysis warning raised inside Bit.Brouter. Ignored:
# the framework assemblies' own warnings, and the IL2110/IL2111 the trimmer reports at every call site
# that sets a component-typed [Parameter] (Blazor's LayoutView.Layout raises the same ones).
bash Tests/Bit.Brouter.Tests.E2E/publish-gate.sh ../../artifacts/harness-trimmed
# AOT leg (needs the wasm-tools workload):
bash Tests/Bit.Brouter.Tests.E2E/publish-gate.sh ../../artifacts/harness-aot -p:RunAOTCompilation=true

BROUTER_E2E_PUBLISHED_HOST=$PWD/../../artifacts/harness-trimmed \
  dotnet test Tests/Bit.Brouter.Tests.E2E/Bit.Brouter.Tests.E2E.csproj --filter "FullyQualifiedName!~HybridModeTests"
```

CI runs all of the above in the E2E stages of `.github/workflows/bit.ci.Brouter.yml`.

## Hybrid notes

The hybrid suite starts `Bit.Brouter.Tests.Harness.Hybrid.exe` with
`--remote-debugging-port <port> --user-data-folder <private folder>`, which the host applies through
`BlazorWebViewInitializing` (the WebView2 API), then attaches with `ConnectOverCDPAsync`. A window
opens while it runs. The `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS` environment variable is not an
option: since WebView2 Runtime 150, an elevated process - such as a GitHub-hosted runner - ignores
`--remote-debugging-port` given that way.
Tests that would need a second window (modified clicks) or would close the WebView (`beforeunload`)
report Inconclusive there.

It needs the WebView2 Runtime (Windows 11 has it; Windows Server images, including GitHub's
`windows-2025`, may only have the Edge browser - CI installs the runtime first). When the WebView
cannot start, the host exits with the reason on stderr, and the failing test quotes it.
