# Bit.BlazorUI Performance Tests

This folder contains browser tests for Bit.BlazorUI components using Playwright: performance tests, and the browser
regression tests that need a real browser's layout, focus or timing.

## Structure

```
Performance/
├── Bit.BlazorUI.Tests.Performance.TestHost/                # Blazor Server application for hosting test pages
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   └── Pages/                                          # The pages the tests drive, and Home.razor
│   ├── Program.cs
│   └── Bit.BlazorUI.Tests.Performance.TestHost.csproj
│
├── Bit.BlazorUI.Tests.Performance/                         # The Playwright test project
│   ├── PerformanceTestBase.cs                              # Base class: starts the test host, owns the browser and the page
│   ├── RequiresBrowserAttribute.cs                         # Makes every class opt-in (RUN_BROWSER_TESTS=1)
│   ├── Bit*BrowserTests.cs                                 # The performance tests (BitActionButtonBrowserTests,
│   │                                                       # BitDropdownBrowserTests) and the browser regression tests
│   └── Bit.BlazorUI.Tests.Performance.csproj
│
├── Bit.BlazorUI.Tests.PdfPerformance/                      # A console benchmark of the PDF engine (see below)
│
└── README.md
```

## Prerequisites

The [buffered PDF engine benchmark](Bit.BlazorUI.Tests.PdfPerformance/README.md) is a separate console diagnostic for parsing/rendering and before/after byte-reading costs. It does not need Playwright or a test host.

1. The .NET SDK that `src/global.json` pins. A Debug build targets `net11.0` only; pass `-p:BuildAllTfms=true` to
   build `net10.0` as well.
2. Playwright browsers installed

## Setup

### Install Playwright Browsers

After building the Bit.BlazorUI.Tests.Performance project, install the required browsers:

```bash
pwsh bin/Debug/net11.0/playwright.ps1 install
```

Or on Linux/macOS:

```bash
./bin/Debug/net11.0/playwright.sh install
```

# Running Tests

## Test Host

The test host is a Blazor Server application that provides the pages the tests drive. The first test starts it
(`dotnet run` on the same target framework as the test run) unless a host already answers on its URL, which the run
then uses as it is.

### Running the Test Host Manually

In the `Bit.BlazorUI.Tests.Performance.TestHost` folder:

```bash
dotnet run --urls http://localhost:5280 -f net11.0
```

Then navigate to:
- http://localhost:5280 - Home page with links to the test pages
- http://localhost:5280/perf/action-button - BitActionButton performance test
- http://localhost:5280/perf/action-button/100 - BitActionButton with 100 components
- http://localhost:5280/perf/dropdown - BitDropdown performance test

### Test Page Controls

Each performance page (`/perf/action-button`, `/perf/dropdown`) exposes:
- `#btn-render` - Button to trigger initial render
- `#btn-clear` - Button to clear all components
- `#btn-rerender` - Button to trigger re-render

Metrics are exposed via DOM elements:
- `#render-time` - Initial render time in milliseconds
- `#rerender-time` - Re-render time in milliseconds
- `#component-count` - Number of components rendered
- `#status` - Current status (Ready, Rendering, Rendered, etc.)

## Performance Tests

Every test class here is marked `[RequiresBrowser]`, the performance ones included, so a run without
`RUN_BROWSER_TESTS=1` skips them all. Run the following command in the `Bit.BlazorUI.Tests.Performance` folder:
```bash
RUN_BROWSER_TESTS=1 dotnet test --filter "TestCategory=Performance" --output detailed
```

### Run Specific Test Categories

```bash
# Initial render tests only
RUN_BROWSER_TESTS=1 dotnet test --filter "TestCategory=InitialRender"

# Re-render tests only
RUN_BROWSER_TESTS=1 dotnet test --filter "TestCategory=ReRender"

# Memory tests only
RUN_BROWSER_TESTS=1 dotnet test --filter "TestCategory=Memory"

# Scalability tests only
RUN_BROWSER_TESTS=1 dotnet test --filter "TestCategory=Scalability"

# Stress tests only
RUN_BROWSER_TESTS=1 dotnet test --filter "TestCategory=Stress"
```

### Run with Specific Browser

The browser comes from the `BROWSER` environment variable: `chromium` (the default), `edge` (the installed Microsoft
Edge), `firefox` or `webkit`. The Microsoft.Testing.Platform runner does not pass runsettings through, so there is no
`.runsettings` to edit.

```bash
RUN_BROWSER_TESTS=1 BROWSER=firefox dotnet test
RUN_BROWSER_TESTS=1 BROWSER=webkit dotnet test
```

### Run in Headed Mode (for debugging)

```bash
RUN_BROWSER_TESTS=1 HEADED=1 dotnet test
```

### Run the Browser Regression Tests

Classes marked `[RequiresBrowser]` (see `RequiresBrowserAttribute.cs`) need a test host and installed Playwright
browsers, so they are skipped unless `RUN_BROWSER_TESTS=1` is set. They stay discoverable, so a name filter still
selects them:

```bash
RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitTextFieldBrowserTests
```

CI runs every test of `Bit.BlazorUI.Tests.Performance` with `RUN_BROWSER_TESTS=1`, in the `browser tests` job of
`.github/workflows/bit.ci.BlazorUI.e2e.yml`.

## Performance Thresholds

Default thresholds are defined in `PerformanceTestBase.Thresholds`:

| Component Count | Initial Render | Re-render | Memory |
|-----------------|----------------|-----------|--------|
| 10              | 500ms          | 200ms     | 50MB   |
| 100             | 1000ms         | 500ms     | 100MB  |
| 500             | 3000ms         | 1500ms    | 200MB  |
| 1000            | 5000ms         | 3000ms    | 400MB  |

These thresholds are intentionally generous to account for CI/CD environment variations.
Adjust as needed based on your performance requirements.

## Results

The performance tests also write what they measured to `performance-results_chromium_net11.0_20261107-142530.json` (the
browser, the framework and the UTC time of the run, so every run - a Chromium and a Firefox one sharing a directory
included - keeps a file of its own) in the run's results directory - `--results-directory`, which CI uploads with the
TRX, or `TestResults` beside the test app otherwise. No threshold reads it: it is there to compare one run with another.

- **What each test measured**, as rows of test, metric, value, unit and count (the components rendered, the cycles run):
  - `render.*` and `rerender.*`: `server` is the page's own stopwatch, from the click handler to `OnAfterRender`.
    `browser` is the same click timed in the browser, from the click reaching the page to the frame after the status
    showed it was done, which takes in the round trip, the DOM changes and their layout and paint. `domNodes` is the
    page's element count at the end. `longTasks` and `longTaskTime` are the main thread's tasks of 50 ms or more in
    between and how much of their time fell in between, which only Chromium-based browsers report. BitActionButton's
    scalability and 2000-component stress tests record their renders the same way.
  - `render.timePerComponentGrowth`: the scalability test's time per component at its largest count, over the cheapest.
  - `rerender.server.mean` and `.max`: BitActionButton's repeated re-renders.
  - `renderClearCycle.mean`: BitActionButton's render and clear cycles, as the test runner timed them.
  - `jsHeap` and `jsHeapIncrease`: BitActionButton's JS heap after rendering, also Chromium only.
  - `serverHeapGrowth.early` and `.late`: BitDropdown's mount and unmount windows; `serverHeapGrowth` alone, its heap
    growth after the count of cycles.
- **What it was measured on**:
  - the OS, the CPU model and its cores, the memory, and whether it ran on CI;
  - the browser and its version;
  - the SDK, and the runtime and configuration of the test run;
  - the commit;
  - the host's own account of itself from `/api/environment`: the configuration it was built in, its runtime, its GC
    mode, and the render mode of the performance pages.

The browser timing is a `performance.measure` named `bit-perf`, between the marks `bit-perf:start` and `bit-perf:end`,
so a trace of the page recorded in the browser's dev tools shows it as well.

### A baseline is measured in Release

The test run starts the host in the configuration the tests were built in, so a Release build of the tests measures a
Release host. A Debug host (CI's) is no baseline: the `configuration` of the host in the results says which one it was.

```bash
dotnet build Bit.BlazorUI.Tests.Performance -c Release -f net11.0
cd Bit.BlazorUI.Tests.Performance
RUN_BROWSER_TESTS=1 dotnet exec bin/Release/net11.0/Bit.BlazorUI.Tests.Performance.dll --filter "TestCategory=Performance" --results-directory ../perf-results
```

A host already answering on the URL is reused whatever it was built in, so stop one started by hand in Debug first.

## Troubleshooting

### Test Host Doesn't Start

1. Check if port 5280 is available - a host already answering there (another checkout's) is reused, so set `TEST_HOST_URL` (e.g. `http://localhost:5281`) to run against a host of your own
2. Verify the TestHost project builds successfully
3. Check the test output for error messages

### Memory Tests Show

Memory API (`performance.memory`) is only available in Chromium-based browsers.
Run tests with Chromium browser for memory measurements.

### Tests Timeout

1. Raise `PerformanceTestBase.DefaultTimeout`, or pass a longer `Timeout` to the individual wait call
2. Check if the test host is starting properly
3. Reduce component count for initial debugging

### Playwright Browsers Not Found

Run the Playwright install script:
```bash
pwsh bin/Debug/net11.0/playwright.ps1 install
```

## Adding Tests for New Components

1. Create a new test page in `Bit.BlazorUI.Tests.Performance.TestHost/Components/Pages/`:
   ```razor
   @page "/perf/your-component"
   @page "/perf/your-component/{Count:int}"
   
   <!-- Follow the pattern in BitActionButtonPerf.razor -->
   ```

2. Create a new test class in `Bit.BlazorUI.Tests.Performance` project:
   ```csharp
   [TestClass]
   [TestCategory("Performance")]
   [TestCategory("Browser")]
   [RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~YourComponentBrowserTests
   public class YourComponentBrowserTests : PerformanceTestBase
   {
       // Follow the pattern in BitActionButtonBrowserTests.cs
   }
   ```
