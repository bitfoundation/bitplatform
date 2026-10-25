using Bit.Bink.Tests.E2E.Infrastructure;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// axe-core's WCAG 2.1 A and AA rules, run inside the demo's pages: what a person with a screen reader or a keyboard
/// meets, checked by the engine accessibility audits use, not by reading the markup.
/// </summary>
[TestClass]
public sealed class AccessibilityTests : BinkTest
{
    private static readonly AxeRunOptions WcagAA = new()
    {
        RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] },
    };

    /// <summary>Fails with every violation, the rule and the elements it found, so a failure says what to fix.</summary>
    private static async Task AssertNoViolationsAsync(IPage page, string where)
    {
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
    [DataRow("/signature")]
    [DataRow("/handwriting")]
    [DataRow("/whiteboard")]
    [DataRow("/diagram")]
    [DataRow("/image-annotation")]
    [DataRow("/document-annotation")]
    [DataRow("/collaboration")]
    [DataRow("/input-and-accessibility")]
    [DataRow("/serialization")]
    [DataRow("/performance")]
    [DataRow("/extending")]
    public async Task Every_demo_page_meets_WCAG_2_1_AA(string path)
    {
        var page = await OpenAsync(path);
        await AssertNoViolationsAsync(page, path);
    }

    [TestMethod]
    public async Task The_canvas_with_its_text_editor_open_meets_WCAG_2_1_AA()
    {
        // The inline editor is real HTML the canvas places over itself; it has to be labelled like any field.
        var page = await OpenAsync("/whiteboard");
        await page.Locator(".bb-segmented button", new PageLocatorOptions { HasTextString = "Text" }).First.ClickAsync();
        var box = (await page.Locator(".bit-bink-input").First.BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(box.X + 200, box.Y + 200);
        await page.Locator(".bit-bink-canvas textarea").WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        await AssertNoViolationsAsync(page, "the whiteboard with the text editor open");
    }
}
