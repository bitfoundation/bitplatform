# Bit.BlazorUI Performance Tests

This folder contains browser-based performance tests for Bit.BlazorUI components using Playwright.

## Structure

```
Performance/
├── Bit.BlazorUI.Tests.Performance.TestHost/                # Blazor Server application for hosting test pages
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   └── Pages/
│   │       ├── Home.razor
│   │       └── BitActionButtonPerf.razor
│   ├── Program.cs
│   └── Bit.BlazorUI.Tests.Performance.TestHost.csproj
│
├── Bit.BlazorUI.Tests.Performance/                         # Playwright test projects
│   ├── PerformanceTestBase.cs                              # Base class with test infrastructure
│   ├── BitActionButtonBrowserTests.cs
│   └── Bit.BlazorUI.Tests.Performance.csproj
│
└── README.md
```

## Prerequisites

1. .NET 10.0 SDK
2. Playwright browsers installed

## Setup

### Install Playwright Browsers

After building the Bit.BlazorUI.Tests.Performance project, install the required browsers:

```bash
pwsh bin/Debug/net10.0/playwright.ps1 install
```

Or on Linux/macOS:

```bash
./bin/Debug/net10.0/playwright.sh install
```

# Running Tests

## Test Host

The test host is a Blazor Server application that provides test pages for each component.

### Running the Test Host Manually

```bash
dotnet run --urls http://localhost:5280 -f net10.0
```

Then navigate to:
- http://localhost:5280 - Home page with links to all test pages
- http://localhost:5280/perf/action-button - BitActionButton performance test
- http://localhost:5280/perf/action-button/100 - BitActionButton with 100 components

### Test Page Controls

Each test page exposes:
- `#btn-render` - Button to trigger initial render
- `#btn-clear` - Button to clear all components
- `#btn-rerender` - Button to trigger re-render

Metrics are exposed via DOM elements:
- `#render-time` - Initial render time in milliseconds
- `#rerender-time` - Re-render time in milliseconds
- `#component-count` - Number of components rendered
- `#status` - Current status (Ready, Rendering, Rendered, etc.)

## Performance Tests

Run the following command in the `Bit.BlazorUI.Tests.Performance` folder:
```bash
dotnet test -f net10.0 --output detailed
```

### Run Specific Test Categories

```bash
# Initial render tests only
dotnet test --filter "TestCategory=InitialRender"

# Re-render tests only
dotnet test --filter "TestCategory=ReRender"

# Memory tests only
dotnet test --filter "TestCategory=Memory"

# Scalability tests only
dotnet test --filter "TestCategory=Scalability"

# Stress tests only
dotnet test --filter "TestCategory=Stress"
```

### Run with Specific Browser

```bash
dotnet test -- Playwright.BrowserName=firefox
dotnet test -- Playwright.BrowserName=webkit
```

### Run in Headed Mode (for debugging)

```bash
dotnet test -- Playwright.LaunchOptions.Headless=false
```

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

1. Check if port 5280 is available
2. Verify the TestHost project builds successfully
3. Check the test output for error messages

### Memory Tests Show

Memory API (`performance.memory`) is only available in Chromium-based browsers.
Run tests with Chromium browser for memory measurements.

### Tests Timeout

1. Increase timeout in `.runsettings`
2. Check if the test host is starting properly
3. Reduce component count for initial debugging

### Playwright Browsers Not Found

Run the Playwright install script:
```bash
pwsh bin/Debug/net10.0/playwright.ps1 install
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
   public class YourComponentBrowserTests : PerformanceTestBase
   {
       // Follow the pattern in BitActionButtonBrowserTests.cs
   }
   ```
