# Bit.Blex cross-host test suites

The unit tests in `Bit.Blex.Tests` drive the Blazor bridges against a fake `IJSRuntime`, so the logic of
the persistor, the DevTools connector and `<BlexProvider>` is covered without a browser. What only a real
host shows is everything around that logic:

- `bit-blex-storage.js` and `bit-blex-devtools.js` actually load from the package's static web assets;
- the browser's `localStorage`, and the `storage` event another tab fires, behave the way persistence and
  `SyncAcrossTabs` rely on;
- `<BlexProvider>` hands the prerendered state to the interactive render through `PersistentComponentState`
  in every render mode, and sequences hydration, history and the DevTools connection against a real renderer;
- a BlazorWebView page reload, an Auto runtime switch and a trimmed or AOT-compiled build do not break any of it.

These suites have already paid for themselves: the first run found that every prerendered request 500s in a
Blazor Web App that registers more than one interactive render mode, and that a BlazorWebView reload crashed
the hybrid app from `BlexBrowserStorage.DisposeAsync`.

## Layout

| Project | What it is |
|---|---|
| `Bit.Blex.Tests.Harness` | Razor class library: four stores and a page per feature, wrapped in `<BlexProvider>`; every element the tests touch carries an `id`. net8.0 / net9.0 / net10.0. |
| `Bit.Blex.Tests.Harness.Web` (+ `.Client`) | One Blazor Web App serving the harness in any render mode, picked with `--BlexHarness:Mode=`: `ssr`, `server`, `server-noprerender`, `wasm`, `wasm-noprerender`, `auto`, `auto-noprerender`. |
| `Bit.Blex.Tests.Harness.Hybrid` | WinForms `BlazorWebView` host (the WebView core MAUI Blazor Hybrid uses). Windows only. |
| `Bit.Blex.Tests.E2E` | Playwright suites: every web render mode in Chromium, and the hybrid host through WebView2's DevTools port. |

## What runs where

| Area | E2E web modes | E2E hybrid | Publish gate |
|---|---|---|---|
| Actions update a store and re-render its subscribers | yes | yes | trimmed + AOT |
| Persisted store written to `localStorage` and rehydrated after a reload | yes | yes | yes |
| `bit-blex-storage.js` served from `_content/Bit.Blex.Blazor/` | yes | yes | yes |
| A change in another tab reaches this one (`SyncAcrossTabs`) | yes | single window - inconclusive | yes |
| Undo / redo through `BlexHistory` | yes | yes | yes |
| A selector subscription ignores state it does not read | yes | yes | yes |
| A `Latest` effect superseded by a second run, and cancelled | yes | yes | yes |
| DevTools bridge: connect, `init`, `send` per action, `JUMP_TO_STATE` time-travel (fake extension) | yes | yes | yes |
| Store state in the prerendered response, and handed to the interactive render | prerendered modes | - | yes |
| A later Auto visit on WebAssembly rehydrates what the Server visit persisted | `auto` | - | yes |
| Static SSR renders store state and requests no Blex module | `ssr` | - | yes |

## Running locally

```bash
cd src/Blex

# Browser suites (builds the harness hosts first; the hybrid host only on Windows)
dotnet build Tests/Bit.Blex.Tests.E2E/Bit.Blex.Tests.E2E.csproj
pwsh Tests/Bit.Blex.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
cd Tests/Bit.Blex.Tests.E2E && dotnet test

# One mode only
dotnet test --filter "FullyQualifiedName~.WebAssemblyModeTests."
```

Run `dotnet test` from inside `src` so `src/global.json`'s Microsoft.Testing.Platform runner setting applies.
Run a harness host by hand with one of its launch profiles (`ssr`, `server`, `wasm`, `auto`), or
`dotnet run --project Tests/Bit.Blex.Tests.Harness.Web -- --BlexHarness:Mode=server-noprerender`.

### Environment variables

| Variable | Effect |
|---|---|
| `BLEX_E2E_FRAMEWORK` | Target framework of the web harness host: `net10.0` (default), `net9.0`, `net8.0`. |
| `BLEX_E2E_CONFIGURATION` | Build configuration of the hosts; defaults to the test assembly's. |
| `BLEX_E2E_SKIP_BUILD=1` | Do not build the hosts before the run. |
| `BLEX_E2E_PUBLISHED_HOST` | Run a `dotnet publish` output of the web host instead of the build output. |
| `BLEX_E2E_CHANNEL` / `BLEX_E2E_EXECUTABLE` | Use an installed Chrome/Edge or a specific Chromium binary. The channel defaults to `chromium`. |
| `BLEX_E2E_HEADED=1` | Show the browser. |

### Publish gate (trimming / AOT)

```bash
# Publishes trimmed and fails on any trim/AOT analysis warning raised inside Bit.Blex. Ignored: the framework
# assemblies' own warnings, and the ones raised in the harness - above all the IL2026/IL3050 the generated
# snapshot code raises for the reflection-based JSON serializer, which only the app can resolve.
bash Tests/Bit.Blex.Tests.E2E/publish-gate.sh ../../artifacts/harness-trimmed
# AOT leg (needs the wasm-tools workload):
bash Tests/Bit.Blex.Tests.E2E/publish-gate.sh ../../artifacts/harness-aot -p:RunAOTCompilation=true

BLEX_E2E_PUBLISHED_HOST=$PWD/../../artifacts/harness-trimmed \
  dotnet test Tests/Bit.Blex.Tests.E2E/Bit.Blex.Tests.E2E.csproj --filter "FullyQualifiedName!~HybridModeTests"
```

CI runs all of the above in the E2E stages of `.github/workflows/bit.ci.Blex.yml`.

## Hybrid notes

The hybrid suite starts `Bit.Blex.Tests.Harness.Hybrid.exe` with
`--remote-debugging-port <port> --user-data-folder <private folder>`, which the host applies through
`BlazorWebViewInitializing` (the WebView2 API), then attaches with `ConnectOverCDPAsync`. A window opens while it
runs. The tests share that one window: each clears the WebView's storage and navigates, which reloads the page
and gives the test a fresh DI scope - and exercises the reload path that used to crash the app. A failed hybrid
test quotes the app's own console output.

It needs the WebView2 Runtime (Windows 11 has it; Windows Server images, including GitHub's `windows-2025`, may
only have the Edge browser - CI installs the runtime first).
