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
