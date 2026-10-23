using System.Globalization;
using System.Text;
using Bit.Bmage.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// Measures the editing tools in a real browser, through the demo's /performance page, for the WebAssembly columns of
/// docs/performance.md and of that page. It measures rather than checks, so it is opt-in: set
/// <c>BMAGE_E2E_MEASURE=1</c> and point <c>BMAGE_E2E_HOST_URL</c> at a Release publish of the demo, compiled ahead of
/// time (the default) or with <c>-p:RunAOTCompilation=false</c> for the interpreter.
/// </summary>
[TestClass]
public class PerformanceMeasurements : DemoTest
{
    [TestMethod]
    public async Task MeasureTheEditingTools()
    {
        if (!E2EEnvironment.Measure) Assert.Inconclusive("Measurements run only with BMAGE_E2E_MEASURE=1; see docs/performance.md.");
        var page = await OpenAsync("performance");
        var report = new StringBuilder().AppendLine(CultureInfo.InvariantCulture, $"# {E2EEnvironment.HostUrl ?? "a demo the suite started"}, {E2EEnvironment.Browser} {page.Context.Browser?.Version}");

        // The page's first benchmark too: its rows have published interpreter and AOT numbers, so they show whether this
        // machine and browser measure like the ones those came from.
        report.AppendLine("## calibration");
        await RunAsync(page, "Measure this browser", "Run the benchmark", report);
        report.AppendLine("## editing tools");
        await RunAsync(page, "Editing tools in this browser", "Run the editing tools", report);

        await File.WriteAllTextAsync(E2EEnvironment.MeasureOutput, report.ToString());
        TestContext.WriteLine($"Measurements written to {E2EEnvironment.MeasureOutput}:{Environment.NewLine}{report}");
    }

    /// <summary>Runs one of the page's benchmarks and appends its rows, the operation and this browser's time.</summary>
    private static async Task RunAsync(IPage page, string cardTitle, string button, StringBuilder report)
    {
        var card = page.Locator(".bb-card", new PageLocatorOptions { HasText = cardTitle });
        await card.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = button }).ClickAsync();
        var status = card.Locator("[role=status]");
        await status.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 60_000 });
        await status.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 1_800_000 });

        var rows = card.Locator("tbody tr");
        var count = await rows.CountAsync();
        Assert.IsGreaterThan(0, count, $"{button} produced no results");
        for (var i = 0; i < count; i++)
        {
            var name = await rows.Nth(i).Locator("th").InnerTextAsync();
            var elapsed = await rows.Nth(i).Locator("td").First.InnerTextAsync();
            report.Append(name).Append('\t').AppendLine(elapsed);
        }
    }
}
