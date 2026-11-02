namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiTokenUsageView at /e2e/aitokenusageview in a real browser: the tokens shortened by their size, the cost
/// in the symbol of its currency (four decimals for a fraction of a cent), the breakdown of its native title, the
/// throughput and the latency read off the metadata and switched on and off, the usage of a reply of the usage script,
/// a title of the page's, the theme tokens and every --bit-AiTokenUsageView-* variable, Styles and Classes, Disabled
/// and RTL.
/// </summary>
[TestClass]
public class BitAiTokenUsageViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aitokenusageview";

    private ILocator View(string id) => Page.Locator($"#{id}");

    private ILocator Tokens(string id) => Page.Locator($"#{id} > .bit-aituv-tok");

    private ILocator Cost(string id) => Page.Locator($"#{id} > .bit-aituv-cst");

    private ILocator Speed(string id) => Page.Locator($"#{id} > .bit-aituv-spd");

    private ILocator Latency(string id) => Page.Locator($"#{id} > .bit-aituv-ltc");

    [TestMethod]
    public async Task Basic_AUsage_IsReadAsItsTokensAndItsCostWithTheBreakdownAsItsTitle()
    {
        await OpenAsync(Route);

        await Expect(View("tuv-basic")).ToHaveClassAsync(HasClass("bit-aituv"));
        await Expect(Tokens("tuv-basic")).ToHaveTextAsync("1.5k tokens");
        await Expect(Cost("tuv-basic")).ToHaveTextAsync("$0.0081");
        await Expect(Speed("tuv-basic")).ToHaveCountAsync(0);
        await Expect(Latency("tuv-basic")).ToHaveCountAsync(0);
        await Expect(View("tuv-basic")).ToHaveAttributeAsync("title", "Input: 1,200\nOutput: 300\nCached: 400\nReasoning: 50");
    }

    [TestMethod]
    public async Task Cost_TheCurrency_PicksTheSymbolAndAFractionOfACentKeepsFourDecimals()
    {
        await OpenAsync(Route);

        await Expect(Cost("tuv-cost-usd")).ToHaveTextAsync("$1.50");
        await Expect(Cost("tuv-cost-default")).ToHaveTextAsync("$0.50");
        await Expect(Cost("tuv-cost-eur")).ToHaveTextAsync("€0.25");
        await Expect(Cost("tuv-cost-gbp")).ToHaveTextAsync("£2.00");
        await Expect(Cost("tuv-cost-other")).ToHaveTextAsync("JPY 120");
        await Expect(Cost("tuv-cost-fraction")).ToHaveTextAsync("$0.0042");
        await Expect(Cost("tuv-cost-cent")).ToHaveTextAsync("$0.01");

        // A provider that reports no cost leaves it out rather than showing a blank.
        await Expect(Cost("tuv-cost-none")).ToHaveCountAsync(0);
        await Expect(Tokens("tuv-cost-none")).ToHaveTextAsync("10 tokens");
    }

    [TestMethod]
    public async Task Tokens_TheCount_IsShortenedByItsSize()
    {
        await OpenAsync(Route);

        await Expect(Tokens("tuv-small")).ToHaveTextAsync("999 tokens");
        await Expect(Tokens("tuv-thousand")).ToHaveTextAsync("1k tokens");
        await Expect(Tokens("tuv-million")).ToHaveTextAsync("1.2M tokens");
        await Expect(Tokens("tuv-output-only")).ToHaveTextAsync("42 tokens");
        await Expect(View("tuv-output-only")).ToHaveAttributeAsync("title", "Output: 42");
        await Expect(View("tuv-million")).ToHaveAttributeAsync("title", "Input: 1,000,000\nOutput: 200,000");

        // An empty usage shows nothing at all, and no breakdown either.
        await Expect(View("tuv-empty")).ToHaveTextAsync("");
        await Expect(View("tuv-empty").Locator("span")).ToHaveCountAsync(0);
        Assert.AreEqual(string.Empty, await View("tuv-empty").GetAttributeAsync("title") ?? string.Empty);
    }

    [TestMethod]
    public async Task Speed_TheThroughputAndTheLatency_AreReadOffTheMetadata()
    {
        await OpenAsync(Route);

        // 85 output tokens over the 2 seconds after the first one.
        await Expect(Speed("tuv-speed")).ToHaveTextAsync("42.5 tok/s");
        await Expect(Latency("tuv-speed")).ToHaveCountAsync(0);

        // The latency is named for assistive technologies by hidden text before the bare duration it shows.
        await Expect(Speed("tuv-latency")).ToHaveTextAsync("42.5 tok/s");
        await Expect(Latency("tuv-latency")).ToHaveTextAsync("Time to first token 500ms");
        var label = await Latency("tuv-latency").Locator(".bit-aituv-vhd").BoundingBoxAsync();
        Assert.IsTrue(label is null || label.Width <= 1, "The name of the latency is visually hidden.");

        await Expect(Speed("tuv-no-speed")).ToHaveCountAsync(0);
        await Expect(Latency("tuv-no-speed")).ToHaveTextAsync("Time to first token 500ms");

        await Expect(Speed("tuv-latency-seconds")).ToHaveTextAsync("85 tok/s");
        await Expect(Latency("tuv-latency-seconds")).ToHaveTextAsync("Time to first token 1.5s");

        // The breakdown of the tokens is hidden text of the readout too, which the title alone is not to a keyboard or a
        // screen reader.
        await Expect(View("tuv-latency").Locator("> .bit-aituv-vhd")).ToHaveCountAsync(1);

        await Expect(Speed("tuv-no-metadata")).ToHaveCountAsync(0);
        await Expect(Latency("tuv-no-metadata")).ToHaveCountAsync(0);

        // The figures follow each other in a fixed order: tokens (and its hidden breakdown), cost, speed, latency.
        Assert.AreEqual("bit-aituv-tok,bit-aituv-vhd,bit-aituv-cst,bit-aituv-spd,bit-aituv-ltc",
            await View("tuv-latency").EvaluateAsync<string>("e => [...e.children].map(c => c.classList[0]).join(',')"));
    }

    [TestMethod]
    public async Task Session_TheUsageOfAReply_IsReadOutOnceTheTurnIsIn()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-session-ask").ClickAsync();

        await Expect(Page.Locator("#tuv-session-reply")).ToHaveTextAsync("This turn ran on the large model.");
        await Expect(Tokens("tuv-session")).ToHaveTextAsync("1.5k tokens");
        await Expect(Cost("tuv-session")).ToHaveTextAsync("$0.0081");
        await Expect(View("tuv-session")).ToHaveAttributeAsync("title", "Input: 1,200\nOutput: 300\nCached: 400");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Title_OneOfThePage_WinsOverTheBreakdown()
    {
        await OpenAsync(Route);

        await Expect(View("tuv-title")).ToHaveAttributeAsync("title", "The page's own breakdown");
        await Expect(Tokens("tuv-title")).ToHaveTextAsync("1.5k tokens");
    }

    [TestMethod]
    public async Task Theme_TheReadout_IsSmallSecondaryTextInDigitsOfOneWidth()
    {
        await OpenAsync(Route);

        var view = View("tuv-basic");
        await Expect(view).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(view).ToHaveCSSAsync("font-size", await view.ResolveLengthInAsync("var(--bit-tpg-fs-xs)"));
        await Expect(view).ToHaveCSSAsync("font-variant-numeric", "tabular-nums");
        await Expect(view).ToHaveCSSAsync("display", "inline-flex");
        await Expect(view).ToHaveCSSAsync("column-gap", await view.ResolveLengthInAsync("calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * 1.5)"));
        await Expect(Tokens("tuv-basic")).ToHaveCSSAsync("white-space", "nowrap");
        await Expect(Cost("tuv-basic")).ToHaveCSSAsync("white-space", "nowrap");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheReadout()
    {
        await OpenAsync(Route);

        await Expect(View("tuv-vars")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(View("tuv-vars")).ToHaveCSSAsync("font-size", "19px");
        await Expect(View("tuv-vars")).ToHaveCSSAsync("column-gap", "23px");
        await Expect(Cost("tuv-vars")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheRootAndEachFigure()
    {
        await OpenAsync(Route);

        await Expect(View("tuv-styles")).ToHaveClassAsync(HasClass("e2e-tuv-root"));
        await Expect(Tokens("tuv-styles")).ToHaveClassAsync(HasClass("e2e-tuv-tokens"));
        await Expect(Cost("tuv-styles")).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(Speed("tuv-styles")).ToHaveCSSAsync("font-weight", "700");
    }

    [TestMethod]
    public async Task Disabled_TheReadout_IsDrawnInTheDisabledColor()
    {
        await OpenAsync(Route);

        await Expect(View("tuv-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(View("tuv-disabled")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));
        await Expect(Tokens("tuv-disabled")).ToHaveTextAsync("1.5k tokens");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrAnInheritedDirection_PutsTheTokensFirstOnTheRight()
    {
        await OpenAsync(Route);

        await Expect(View("tuv-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "tuv-rtl", "tuv-rtl-inherited" })
        {
            var tokens = await Tokens(id).RectAsync();
            var cost = await Cost(id).RectAsync();
            Assert.IsTrue(tokens.Left > cost.Left, $"{id}: the tokens lead from the right.");
        }

        var ltrTokens = await Tokens("tuv-ltr").RectAsync();
        var ltrCost = await Cost("tuv-ltr").RectAsync();
        Assert.IsTrue(ltrTokens.Left < ltrCost.Left, "Left to right, the tokens lead from the left.");
    }
}
