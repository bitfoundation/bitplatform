# Bit.BbScanner end-to-end tests

The decoder tests in `Bit.BbScanner.Tests` run the whole pipeline without a browser: every symbology is rendered,
transformed the way a camera transforms it and decoded in .NET. That covers the decoder well and leaves out
everything that only exists in a real browser:

- `bit-bbscanner.ts` never executes, so nothing proves that the camera opens, that frames are copied into the
  managed buffer, or that a track that ends is noticed;
- there is no WebAssembly runtime, so the zero-copy `[JSImport]` memory view path is never taken;
- there is no real permission prompt, no real `MediaStream` and no real lifecycle: superseded starts, navigation
  away mid-prompt, reloads and a camera taken away while scanning;
- the demo pages, which compose the scanner with real application code, never run.

This suite boots the demo (`Bit.BbScanner.Demo.Server`, which prerenders the pages and runs them on WebAssembly) and
drives it in Chromium with Playwright. The browser's camera is a generated Y4M video
(`--use-file-for-fake-video-capture`) rendered by the test kit's encoders, so everything below the sensor is real:
the WebAssembly runtime, the JavaScript module, a real `MediaStream`, the real frame loop and the real decoder.

## What it proves

| Suite | What it covers |
|---|---|
| `ScannerLifecycleTests` | The camera starts and scans continuously; every symbology in the clip (QR Code, Data Matrix, Code 128, EAN-13) is decoded; a blank clip reports nothing; stop releases the camera and start opens it again; pause keeps the camera and stops reporting; repeated start/stop cycles and a start superseded by another leave no camera running; navigating away disposes the scanner and releases the camera; reloads start cleanly; a denied permission is reported as an error; switching cameras keeps exactly one track open; a minute of scanning does not grow the heap without bound; frames never reach the render tree. |
| `ScannerInterfaceTests` | Every demo page loads without errors and an unknown route shows the not-found page; the scanner is a labelled region with named controls, announces results in a live region and is operable from the keyboard; the layout mirrors in a right-to-left document and fits a narrow phone screen; the scanner keeps the camera's aspect ratio so overlays line up; losing the camera mid-scan is reported rather than hanging; choosing the same image twice scans it twice; `DecodeImage` reads a buffer the page generated. |

Camera tracks are counted as the page opens and stops them, so a leaked stream fails a test rather than only
lighting up the camera indicator. Every page is prerendered, so each test waits until the page runs in the browser
(`data-interactive="true"` on the layout) before it touches the scanner.

## Running locally

From `src/BbScanner` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.BbScanner.Demo.Server.csproj
dotnet build Tests/Bit.BbScanner.Tests.E2E/Bit.BbScanner.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.BbScanner.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.BbScanner.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.ScannerInterfaceTests."
dotnet test --filter "FullyQualifiedName~DecodesEverySymbologyInTheClip"
```

CI runs the suite in `.github/workflows/bit.ci.BbScanner.e2e.yml`, which only runs when started by hand (Actions >
"bit platform CI - BbScanner E2E" > Run workflow). The pull-request pipeline, `bit.ci.BbScanner.yml`, runs the build,
decoder, component and MCP tests only. The E2E workflow does the same as above, in Release:

```bash
dotnet build src/BbScanner/Demo/Server/Bit.BbScanner.Demo.Server.csproj -c Release
dotnet build src/BbScanner/Tests/Bit.BbScanner.Tests.E2E/Bit.BbScanner.Tests.E2E.csproj -c Release
pwsh src/BbScanner/Tests/Bit.BbScanner.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/BbScanner/Tests/Bit.BbScanner.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename bbscanner-e2e.trx
```

The suite starts the demo server itself (`dotnet Bit.BbScanner.Demo.Server.dll --urls http://127.0.0.1:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers, shares it
between all tests and stops it when the run ends. Unlike most browser suites, every test launches a browser of its
own: the clip the fake camera plays and whether the permission prompt is accepted are launch arguments of the
browser process. The tests run one at a time, because each one waits for the decoder to keep up with a video in
real time.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the reason. On
a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BBSCANNER_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BBSCANNER_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BBSCANNER_E2E_CHANNEL` / `BBSCANNER_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary. Without either, the full Chromium build Playwright installed is used in its new headless mode (not the headless shell, which reports a denied camera permission as an unsupported API), falling back to an installed Chrome, then Edge. |
| `BBSCANNER_E2E_HEADED=1` | Show the browser. |
