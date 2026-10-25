using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// axe-core's WCAG 2.2 A and AA rules, run inside the demo's pages and with tours open on them: what a person with
/// a screen reader or a keyboard meets, checked by the engine accessibility audits use, not by reading the markup.
/// Passing them is necessary, not sufficient: README "Accessibility" lists what was checked by hand.
/// </summary>
[TestClass]
public sealed class AccessibilityTests : BonboardingTest
{
    private static readonly AxeRunOptions WcagAA = new()
    {
        RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"] },
    };

    /// <summary>Fails with every violation, the rule and the elements it found, so a failure says what to fix.</summary>
    private static async Task AssertNoViolationsAsync(IPage page, string where)
    {
        // axe reads colours as they are painted, and a card still fading in is painted blended with the page behind
        // it: its text fails contrast against a background it never has once shown. Every animation and transition
        // that ends is let finish first; the ones that loop (the spinner, a beacon's pulse) never would.
        await page.EvaluateAsync(
            "() => Promise.all(document.getAnimations().filter(a => a.effect?.getComputedTiming().iterations !== Infinity).map(a => a.finished.catch(() => null)))");

        AxeResult result = await page.RunAxe(WcagAA);
        if (result.Violations.Length == 0) return;

        var report = new System.Text.StringBuilder($"{result.Violations.Length} accessibility violation(s) on {where}:");
        foreach (AxeResultItem violation in result.Violations)
        {
            report.Append($"\n- {violation.Id} ({violation.Impact}): {violation.Help}");
            foreach (AxeResultNode node in violation.Nodes.Take(5))
            {
                string html = node.Html.Length > 160 ? node.Html[..160] + "..." : node.Html;
                IEnumerable<string> why = (node.Any ?? []).Concat(node.All ?? []).Concat(node.None ?? []).Select(check => check.Message).Distinct();
                report.Append($"\n    {node.Target}: {html}\n      {string.Join("; ", why)}");
            }
        }

        Assert.Fail(report.ToString());
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/getting-started")]
    [DataRow("/placement")]
    [DataRow("/dynamic")]
    [DataRow("/flows")]
    [DataRow("/lifecycle")]
    [DataRow("/guidance")]
    [DataRow("/workspace")]
    [DataRow("/workspace/settings")]
    [DataRow("/theming")]
    [DataRow("/headless")]
    [DataRow("/events")]
    [DataRow("/mcp-server")]
    public async Task Every_demo_page_meets_WCAG_2_2_AA(string path)
    {
        var page = await OpenAsync(path);
        await AssertNoViolationsAsync(page, path);
    }

    [TestMethod]
    [DataRow(ColorScheme.Light)]
    [DataRow(ColorScheme.Dark)]
    public async Task A_step_with_a_spotlight_meets_WCAG_2_2_AA(ColorScheme scheme)
    {
        var options = DefaultContextOptions();
        options.ColorScheme = scheme;
        var page = await OpenAsync("/", options);

        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");
        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForStepAsync(page, "Create a project");

        await AssertNoViolationsAsync(page, $"the welcome tour's second step ({scheme})");
    }

    [TestMethod]
    [DataRow(ColorScheme.Light)]
    [DataRow(ColorScheme.Dark)]
    public async Task A_beacons_hint_and_a_checklist_meet_WCAG_2_2_AA(ColorScheme scheme)
    {
        var options = DefaultContextOptions();
        options.ColorScheme = scheme;
        var page = await OpenAsync("/guidance", options);

        await page.Locator("[data-bonboarding-beacon='import-hint']").ClickAsync();
        await WaitForStepAsync(page, "Import is here");

        await AssertNoViolationsAsync(page, $"the guidance page with a hint open ({scheme})");
    }

    [TestMethod]
    public async Task The_waiting_card_meets_WCAG_2_2_AA()
    {
        var page = await OpenAsync("/dynamic");
        await page.GetByRole(AriaRole.Button, new() { Name = "Wait, then skip" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bit-bonboarding-waiting")).ToBeVisibleAsync();

        await AssertNoViolationsAsync(page, "the waiting card");
    }
}
