using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the badge, the dot and the suffix template of a BitNav item: they sit at the far end of
/// the row whichever way it reads, the text before them gives up its room to an ellipsis while they stay whole, the rail
/// pins the badge to the corner of the icon, and the badge keeps its own colors in every state of the item. The bUnit
/// tests pin the markup; these read the layout each engine draws.
///
/// Run them with BROWSER=firefox and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitNavBadgeBrowserTests
public class BitNavBadgeBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/nav-badge";

    private const string BadgeColor = "rgb(1, 2, 3)";
    private const string BadgeBackground = "rgb(4, 5, 6)";

    private const int Inbox = 1;
    private const int Long = 2;
    private const int Dot = 3;
    private const int Plain = 4;

    [TestMethod]
    public async Task BitNav_Badge_SitsAtTheFarEndOfTheRow()
    {
        await Open(Page);

        var item = await Rect(Row("ltr", Inbox, ".bit-nav-itm"));
        var badge = await Rect(Row("ltr", Inbox, ".bit-nav-bdg"));
        var text = await Rect(Row("ltr", Inbox, ".bit-nav-itx"));

        Assert.AreEqual(item.Right, badge.Right, 1, "The badge is not at the end of the row.");
        Assert.IsTrue(text.Right < badge.Left, "The badge is not after the text.");
    }

    [TestMethod]
    public async Task BitNav_Badge_SitsAtTheFarEndOfARightToLeftRow()
    {
        await Open(Page);

        foreach (var section in new[] { "rtl", "rtl-inherited" })
        {
            var item = await Rect(Row(section, Inbox, ".bit-nav-itm"));
            var badge = await Rect(Row(section, Inbox, ".bit-nav-bdg"));
            var text = await Rect(Row(section, Inbox, ".bit-nav-itx"));

            Assert.AreEqual(item.Left, badge.Left, 1, $"The badge is not at the end of the row in {section}.");
            Assert.IsTrue(badge.Right < text.Left, $"The badge is not after the text in {section}.");
        }
    }

    [TestMethod]
    public async Task BitNav_Badge_StaysWholeWhileTheTextBeforeItIsClipped()
    {
        await Open(Page);

        var item = await Rect(Row("ltr", Long, ".bit-nav-itm"));
        var badge = await Rect(Row("ltr", Long, ".bit-nav-bdg"));
        var text = await Rect(Row("ltr", Long, ".bit-nav-itx"));

        Assert.IsTrue(await Page.EvalOnSelectorAsync<bool>(Row("ltr", Long, ".bit-nav-itx"), "e => e.scrollWidth > e.clientWidth"),
            "The text was expected to overflow the row.");
        Assert.IsTrue(badge.Right <= item.Right + 0.5 && badge.Left >= item.Left, "The badge left the row.");
        Assert.IsTrue(text.Right <= badge.Left, "The text runs under the badge.");
        Assert.IsTrue(await Page.EvalOnSelectorAsync<bool>(Row("ltr", Long, ".bit-nav-bdg"), "e => e.scrollWidth <= e.clientWidth"),
            "The badge itself is clipped.");
    }

    [TestMethod]
    public async Task BitNav_SuffixTemplate_SitsAheadOfTheBadge()
    {
        await Open(Page);

        var suffix = await Rect(Row("ltr", Inbox, ".suffix"));
        var badge = await Rect(Row("ltr", Inbox, ".bit-nav-bdg"));
        var text = await Rect(Row("ltr", Inbox, ".bit-nav-itx"));

        Assert.IsTrue(text.Right < suffix.Left && suffix.Right <= badge.Left, "The suffix is not between the text and the badge.");
    }

    [TestMethod]
    public async Task BitNav_Badge_FollowsATemplateInTheNormalMode()
    {
        await Open(Page);

        var item = await Rect(Row("template", Inbox, ".bit-nav-itm"));
        var badge = await Rect(Row("template", Inbox, ".bit-nav-bdg"));
        var custom = await Rect(Row("template", Inbox, ".custom"));

        Assert.AreEqual(item.Right, badge.Right, 1, "The badge is not at the end of the row.");
        Assert.IsTrue(custom.Right < badge.Left, "The badge is not after the template.");
    }

    [TestMethod]
    public async Task BitNav_Dot_IsRound()
    {
        await Open(Page);

        var dot = await Rect(Row("ltr", Dot, ".bit-nav-bdt"));

        Assert.IsTrue(dot.Width > 0, "The dot is not drawn.");
        Assert.AreEqual(dot.Width, dot.Height, 0.5, "The dot is not round.");
        await Expect(Page.Locator(Row("ltr", Dot, ".bit-nav-bdt"))).ToHaveCSSAsync("background-color", BadgeBackground);
    }

    [TestMethod]
    public async Task BitNav_Badge_IsPinnedToTheCornerOfTheIconInTheRail()
    {
        await Open(Page);

        var item = await Rect(Row("rail", Inbox, ".bit-nav-itm"));
        var icon = await Rect(Row("rail", Inbox, ".bit-nav-iic"));
        var badge = await Rect(Row("rail", Inbox, ".bit-nav-bdg"));

        Assert.IsTrue(badge.Width > 0, "The rail does not draw the badge.");
        // Inside the item, over the upper end corner of the icon rather than beside it.
        Assert.IsTrue(badge.Left >= item.Left && badge.Right <= item.Right + 0.5 && badge.Top >= item.Top && badge.Bottom <= item.Bottom + 0.5,
            "The badge left the item.");
        Assert.IsTrue(badge.Top < icon.Top + icon.Height / 2, "The badge is not at the top of the icon.");
        Assert.IsTrue(badge.Right > icon.Right, "The badge is not at the end corner of the icon.");
        Assert.AreEqual(0, await Page.Locator(Row("rail", Inbox, ".suffix")).CountAsync());
    }

    [TestMethod]
    public async Task BitNav_Badge_KeepsItsColorsAtRestSelectedAndHovered()
    {
        await Open(Page);

        // The first item is the selected one.
        await AssertBadgeColors(Row("ltr", Inbox, ".bit-nav-bdg"));
        await AssertBadgeColors(Row("ltr", Long, ".bit-nav-bdg"));

        await Page.HoverAsync(Row("ltr", Long, ""));
        await Page.WaitForFunctionAsync("s => document.querySelector(s)?.matches(':hover')", Row("ltr", Long, ""));
        await AssertBadgeColors(Row("ltr", Long, ".bit-nav-bdg"));

        await Page.HoverAsync(Row("ltr", Inbox, ""));
        await Page.WaitForFunctionAsync("s => document.querySelector(s)?.matches(':hover')", Row("ltr", Inbox, ""));
        await AssertBadgeColors(Row("ltr", Inbox, ".bit-nav-bdg"));
    }

    [TestMethod]
    public async Task BitNav_ItemWithoutABadge_RendersNoEnd()
    {
        await Open(Page);

        Assert.AreEqual(0, await Page.Locator(Row("rtl", Plain, ".bit-nav-sfx")).CountAsync());
    }

    private async Task AssertBadgeColors(string selector)
    {
        await Expect(Page.Locator(selector)).ToHaveCSSAsync("color", BadgeColor);
        await Expect(Page.Locator(selector)).ToHaveCSSAsync("background-color", BadgeBackground);
    }

    /// <summary>A part of the n-th top-level item of a section's nav, or the item itself for an empty part.</summary>
    private static string Row(string section, int index, string part)
    {
        var item = $"#{section} .bit-nav > .bit-nav-lst > li:nth-child({index}) .bit-nav-ict";
        return part.Length == 0 ? item : $"{item} {part}";
    }

    private async Task<Box> Rect(string selector)
    {
        var json = await Page.EvalOnSelectorAsync<string>(selector, @"e => {
            const r = e.getBoundingClientRect();
            return JSON.stringify({ left: r.left, right: r.right, top: r.top, bottom: r.bottom, width: r.width, height: r.height });
        }");

        return JsonSerializer.Deserialize<Box>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static async Task Open(IPage page)
    {
        await page.GotoAsync($"{BaseUrl}{PageUrl}");
        await page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Ready'");
        await Expect(page.Locator(Row("ltr", Inbox, ".bit-nav-bdg"))).ToHaveCountAsync(1);
        await page.AddStyleTagAsync(new() { Content = ".bit-nav, .bit-nav * { transition: none !important; }" });
    }

    private sealed record Box(double Left, double Right, double Top, double Bottom, double Width, double Height);
}
