using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiAgentStatusBadge at /e2e/aiagentstatusbadge in a real browser: the word and the color of every status,
/// the polite live region that stays on the page while the status changes inside it, a role and a politeness of the
/// page's, the --bit-Tag-* variables reaching the neutral statuses only, Styles and Classes, Disabled and RTL.
/// </summary>
[TestClass]
public class BitAiAgentStatusBadgeE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiagentstatusbadge";

    private ILocator Badge(string id) => Page.Locator($"#{id}");

    private ILocator Tag(string id) => Page.Locator($"#{id} > .bit-tag");

    [TestMethod]
    public async Task Statuses_EachStatus_IsAWordOfTheStringTable()
    {
        await OpenAsync(Route);

        foreach (var (id, word) in new[]
        {
            ("asb-idle", "Idle"), ("asb-planning", "Planning"), ("asb-running", "Running"), ("asb-waitingforinput", "Waiting for your answer"),
            ("asb-completed", "Done"), ("asb-failed", "Failed"), ("asb-cancelled", "Stopped"),
        })
        {
            await Expect(Badge(id)).ToHaveClassAsync(HasClass("bit-aiasb"));
            await Expect(Badge(id)).ToHaveTextAsync(word);
            await Expect(Tag(id)).ToHaveCountAsync(1);
        }
    }

    [TestMethod]
    public async Task Statuses_TheColorOfTheTag_SaysWhatTheWordDoes()
    {
        await OpenAsync(Route);

        await Expect(Tag("asb-waitingforinput")).ToHaveClassAsync(HasClass("bit-tag-wrn"));
        await Expect(Tag("asb-completed")).ToHaveClassAsync(HasClass("bit-tag-suc"));
        await Expect(Tag("asb-failed")).ToHaveClassAsync(HasClass("bit-tag-err"));
        await Expect(Tag("asb-waitingforinput")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-wrn)"));
        await Expect(Tag("asb-completed")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-suc)"));
        await Expect(Tag("asb-failed")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));

        // A run at work keeps the default look of a tag; an idle or a stopped one is a neutral tag.
        await Expect(Tag("asb-running")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(Tag("asb-planning")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        foreach (var id in new[] { "asb-running", "asb-planning" })
        {
            await Expect(Tag(id)).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-tag-(wrn|suc|err|dft-ter)(\s|$)"));
        }

        foreach (var id in new[] { "asb-idle", "asb-cancelled" })
        {
            await Expect(Tag(id)).ToHaveClassAsync(HasClass("bit-tag-dft-ter"));
            await Expect(Tag(id)).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-ter)"));
        }
    }

    [TestMethod]
    public async Task Live_AChangingStatus_IsAnnouncedPolitelyFromOneRegionThatStays()
    {
        await OpenAsync(Route);

        var badge = Badge("asb-live");
        await Expect(badge).ToHaveAttributeAsync("role", "status");
        await Expect(badge).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(badge).ToHaveTextAsync("Idle");

        // The region must already be on the page when its words change, or a screen reader hears nothing: it is the
        // same element through every transition.
        await badge.EvaluateAsync("e => e.__e2eRegion = 'kept'");

        foreach (var (status, word) in new[] { ("Planning", "Planning"), ("Running", "Running"), ("WaitingForInput", "Waiting for your answer"), ("Running", "Running"), ("Completed", "Done") })
        {
            await Page.Locator("#btn-live-next").ClickAsync();
            await Expect(Page.Locator("#asb-live-status")).ToHaveTextAsync(status);
            await Expect(badge).ToHaveTextAsync(word);
            Assert.AreEqual("kept", await badge.EvaluateAsync<string?>("e => e.__e2eRegion ?? null"), $"The live region stayed through {status}.");
            await Expect(badge).ToHaveAttributeAsync("role", "status");
        }

        await Expect(Tag("asb-live")).ToHaveClassAsync(HasClass("bit-tag-suc"));
    }

    [TestMethod]
    public async Task Splatted_ARoleAndAPolitenessOfThePage_AreHonored()
    {
        await OpenAsync(Route);

        await Expect(Badge("asb-splatted")).ToHaveAttributeAsync("role", "alert");
        await Expect(Badge("asb-splatted")).ToHaveAttributeAsync("aria-live", "assertive");
        await Expect(Badge("asb-splatted")).ToHaveTextAsync("Failed");
    }

    [TestMethod]
    public async Task CssVariables_TheTagsVariables_ReachTheNeutralStatusesButNeverTakeAColorAway()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "asb-vars-running", "asb-vars-idle" })
        {
            await Expect(Tag(id)).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
            await Expect(Tag(id)).ToHaveCSSAsync("color", "rgb(250, 251, 252)");
        }

        await Expect(Tag("asb-vars-completed")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-suc)"));
        await Expect(Tag("asb-vars-failed")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheRootAndTheTag()
    {
        await OpenAsync(Route);

        await Expect(Badge("asb-styles")).ToHaveClassAsync(HasClass("e2e-asb-root"));
        await Expect(Badge("asb-styles")).ToHaveCSSAsync("margin-left", "7px");
        await Expect(Tag("asb-styles")).ToHaveClassAsync(HasClass("e2e-asb-tag"));
        await Expect(Tag("asb-styles")).ToHaveCSSAsync("letter-spacing", "3px");

        // An inline box of its own, so it sits on the line of a header beside a title.
        await Expect(Badge("asb-styles")).ToHaveCSSAsync("display", "inline-flex");
        await Expect(Badge("asb-styles")).ToHaveCSSAsync("vertical-align", "middle");
    }

    [TestMethod]
    public async Task Disabled_TheTag_IsDisabledAndTheRegionStays()
    {
        await OpenAsync(Route);

        await Expect(Tag("asb-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Tag("asb-disabled")).Not.ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-suc)"));
        await Expect(Badge("asb-disabled")).ToHaveAttributeAsync("role", "status");
        await Expect(Badge("asb-disabled")).ToHaveTextAsync("Done");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrAnInheritedDirection_ReadsRightToLeft()
    {
        await OpenAsync(Route);

        await Expect(Badge("asb-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Badge("asb-rtl")).ToHaveCSSAsync("direction", "rtl");
        await Expect(Badge("asb-rtl-inherited")).ToHaveCSSAsync("direction", "rtl");
        await Expect(Badge("asb-running")).ToHaveCSSAsync("direction", "ltr");
    }
}
