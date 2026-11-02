using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiArtifactPartView at /e2e/aiartifactpartview in a real browser: the artifact a part names, found in its
/// conversation and shown in a BitAiArtifactView; the placeholder tag of one the conversation does not hold, with its
/// glyph from the AI icon font; each turn of a session of the scripted transport keeping the version it produced; the
/// Styles and Classes handed down to the panel and the placeholder; RTL.
/// </summary>
[TestClass]
public class BitAiArtifactPartViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiartifactpartview";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator View(string id) => Root(id).Locator(":scope > .bit-aiatv");

    private ILocator Placeholder(string id) => Root(id).Locator(":scope > .bit-aiapv-plh");

    [TestMethod]
    public async Task Resolved_ThePart_ShowsTheArtifactOfItsConversation()
    {
        await OpenAsync(Route);

        await Expect(Root("apv-resolved")).ToHaveClassAsync(HasClass("bit-aiapv"));

        var panel = View("apv-resolved").Locator(":scope > .bit-aiatv-pnl");
        await Expect(panel).ToHaveAttributeAsync("data-artifact-id", "art-notes");
        await Expect(panel).ToHaveAttributeAsync("aria-label", "Meeting notes");
        await Expect(panel.Locator(".bit-aiatv-ttl")).ToHaveTextAsync("Meeting notes");
        await Expect(panel.Locator(".bit-aiatv-doc").GetByRole(AriaRole.Heading, new() { Name = "Meeting notes" })).ToBeVisibleAsync();
        await Expect(Placeholder("apv-resolved")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow("apv-missing", "art-deleted")]
    [DataRow("apv-no-conversation", "art-notes")]
    public async Task Missing_AnArtifactTheConversationDoesNotHold_IsNamedByAPlaceholder(string id, string artifactId)
    {
        await OpenAsync(Route);

        await Expect(View(id)).ToHaveCountAsync(0);

        var tag = Placeholder(id);
        await Expect(tag).ToBeVisibleAsync();
        await Expect(tag).ToHaveClassAsync(HasClass("bit-tag"));
        await Expect(tag).ToHaveTextAsync("Artifact");
        await Expect(tag).ToHaveAttributeAsync("title", artifactId);

        // A neutral outline tag, its glyph from the AI icon font (MapLayers is not in the core's).
        await Expect(tag).ToHaveClassAsync(HasClass("bit-tag-dft-ter"));
        await tag.Locator("i").First.AssertAiGlyphAsync("MapLayers");
    }

    [TestMethod]
    public async Task Version_AnEarlierTurn_KeepsShowingTheVersionItProduced()
    {
        await OpenAsync(Route);

        await Expect(View("apv-first").Locator(".bit-aiatv-ver .bit-drp-tcn")).ToHaveTextAsync("Version 1");
        await Expect(View("apv-first").Locator(".bit-aiatv-bdy code")).ToHaveTextAsync("{ \"retries\": 1 }");

        await Expect(View("apv-second").Locator(".bit-aiatv-ver .bit-drp-tcn")).ToHaveTextAsync("Version 2");
        await Expect(View("apv-second").Locator(".bit-aiatv-bdy code")).ToHaveTextAsync("{ \"retries\": 3 }");
    }

    [TestMethod]
    public async Task Turns_EachTurnOfASession_ShowsTheVersionItWrote()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-turn").ClickAsync();
        await Expect(Page.Locator("#apv-answers")).ToHaveTextAsync("1");
        await Expect(View("apv-turn-1-part").Locator(".bit-aiatv-ttl")).ToHaveTextAsync("Project README");
        await Expect(View("apv-turn-1-part").Locator(".bit-aiatv-doc").GetByRole(AriaRole.Heading, new() { Name = "Bit.BlazorUI.AI" })).ToBeVisibleAsync();
        await Expect(View("apv-turn-1-part").Locator(".bit-aiatv-ver")).ToHaveCountAsync(0);

        await Page.Locator("#btn-turn").ClickAsync();
        await Expect(Page.Locator("#apv-answers")).ToHaveTextAsync("2");

        // The revision is the second turn's; the first one still shows its first draft.
        await Expect(View("apv-turn-2-part").Locator(".bit-aiatv-ver .bit-drp-tcn")).ToHaveTextAsync("Version 2");
        await Expect(View("apv-turn-2-part").Locator(".bit-aiatv-doc").GetByRole(AriaRole.Heading, new() { Name = "Install" })).ToBeVisibleAsync();

        await Expect(View("apv-turn-1-part").Locator(".bit-aiatv-ver .bit-drp-tcn")).ToHaveTextAsync("Version 1");
        await Expect(View("apv-turn-1-part").Locator(".bit-aiatv-doc").GetByRole(AriaRole.Heading, new() { Name = "Install" })).ToHaveCountAsync(0);

        // Both name the one artifact of the conversation.
        await Expect(View("apv-turn-1-part").Locator(":scope > .bit-aiatv-pnl")).ToHaveAttributeAsync("data-artifact-id", "artifact-1");
        await Expect(View("apv-turn-2-part").Locator(":scope > .bit-aiatv-pnl")).ToHaveAttributeAsync("data-artifact-id", "artifact-1");
    }

    [TestMethod]
    public async Task Styles_TheClassesAndStyles_ReachThePanelAndThePlaceholder()
    {
        await OpenAsync(Route);

        await Expect(Root("apv-styled")).ToHaveClassAsync(HasClass("apv-root-class"));
        await Expect(Root("apv-styled")).ToHaveCSSAsync("margin-top", "3px");
        await Expect(View("apv-styled").Locator(":scope > .bit-aiatv-pnl")).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");
        await Expect(View("apv-styled").Locator(".bit-aiatv-ttl")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(View("apv-styled").Locator(".bit-aiatv-hdr")).ToHaveClassAsync(HasClass("apv-header-class"));

        await Expect(Placeholder("apv-styled-missing")).ToHaveClassAsync(HasClass("apv-placeholder-class"));
        await Expect(Placeholder("apv-styled-missing")).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
    }

    [TestMethod]
    public async Task Rtl_ThePartAndItsPlaceholder_ReadRightToLeft()
    {
        await OpenAsync(Route);

        await Expect(Root("apv-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(View("apv-rtl").Locator(":scope > .bit-aiatv-pnl")).ToHaveCSSAsync("direction", "rtl");

        var icon = await View("apv-rtl").Locator(".bit-aiatv-ico").RectAsync();
        var title = await View("apv-rtl").Locator(".bit-aiatv-ttl").RectAsync();
        Assert.IsTrue(icon.Left >= title.Right - 1, "The glyph of the kind is to the right of the title.");

        await Expect(Root("apv-rtl-missing")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Placeholder("apv-rtl-missing")).ToHaveCSSAsync("direction", "rtl");
        var tagIcon = await Placeholder("apv-rtl-missing").Locator("i").First.RectAsync();
        var tagText = await Placeholder("apv-rtl-missing").GetByText("Artifact").RectAsync();
        Assert.IsTrue(tagIcon.Left >= tagText.Right - 1, "The glyph of the placeholder is to the right of its text.");
    }
}
