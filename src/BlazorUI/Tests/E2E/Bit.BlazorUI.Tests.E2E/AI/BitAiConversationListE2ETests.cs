using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiConversationList at /e2e/aiconversationlist in a real browser: the navigation landmark, the groups of
/// recency as lists named by their headings, the open conversation marked with aria-current, choosing one by pointer and
/// by keyboard (Tab, Enter, Space, the focus ring), the new-conversation button, the delete button named after its
/// conversation (and the group that goes with its last one), ShowNewButton, no delete without OnDelete, "Untitled", a
/// long title cut with an ellipsis and the preview as its tooltip, Disabled, the theme tokens and the hover background
/// moving a variable rather than the paint, every --bit-AiConversationList-* variable, Styles/Classes, RTL, forced
/// colors and a preference for more contrast.
/// </summary>
[TestClass]
public class BitAiConversationListE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiconversationlist";

    private ILocator List(string id) => Page.Locator($"#{id}");

    private ILocator Item(string id, string title) => List(id).Locator("button.bit-aicvl-ibt", new() { HasText = title });

    private ILocator Delete(string id, string title) => List(id).Locator($"button[aria-label='Delete {title}']");

    private ILocator NewButton(string id) => List(id).Locator(":scope > .bit-acb");

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_TheConversations_AreGroupedByRecencyInListsNamedByTheirHeadings()
    {
        await OpenAsync(Route);

        var list = List("cvl-basic");
        Assert.AreEqual("NAV", await list.EvaluateAsync<string>("e => e.tagName"));
        await Expect(list).ToHaveAttributeAsync("aria-label", "Conversations");
        await Expect(Page.GetByRole(AriaRole.Navigation, new() { Name = "Conversations" }).First).ToBeVisibleAsync();

        var headings = list.Locator(".bit-aicvl-grp");
        await Expect(headings).ToHaveCountAsync(5);
        await Expect(headings.Nth(0)).ToHaveTextAsync("Today");
        await Expect(headings.Nth(1)).ToHaveTextAsync("Yesterday");
        await Expect(headings.Nth(2)).ToHaveTextAsync("Previous 7 days");
        await Expect(headings.Nth(3)).ToHaveTextAsync("Previous 30 days");
        await Expect(headings.Nth(4)).ToHaveTextAsync(new Regex(@"^\S+ \d{4}$"));
        await Expect(headings.First).ToHaveCSSAsync("text-transform", "uppercase");

        // Each group is a list named by its heading, holding its conversations newest first.
        var today = list.GetByRole(AriaRole.List, new() { Name = "Today" });
        await Expect(today.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        await Expect(today.Locator(".bit-aicvl-ttl")).ToHaveTextAsync(["Release notes", "Bug triage"]);
        await Expect(list.GetByRole(AriaRole.List, new() { Name = "Yesterday" }).Locator(".bit-aicvl-ttl")).ToHaveTextAsync(["Trip ideas"]);
        await Expect(list.GetByRole(AriaRole.List, new() { Name = "Previous 7 days" }).Locator(".bit-aicvl-ttl")).ToHaveTextAsync(["Budget review"]);
        await Expect(list.GetByRole(AriaRole.List, new() { Name = "Previous 30 days" }).Locator(".bit-aicvl-ttl")).ToHaveTextAsync(["Old project"]);

        // The preview of a conversation is its tooltip.
        await Expect(Item("cvl-basic", "Trip ideas")).ToHaveAttributeAsync("title", "A week in Lisbon");
        await Expect(Item("cvl-basic", "Trip ideas")).ToHaveAttributeAsync("type", "button");
    }

    [TestMethod]
    public async Task Basic_TheOpenConversation_IsCurrentAndSetApartByMoreThanItsColor()
    {
        await OpenAsync(Route);

        var open = Item("cvl-basic", "Release notes");
        await Expect(open).ToHaveAttributeAsync("aria-current", "true");
        await Expect(open).ToHaveClassAsync(HasClass("bit-aicvl-sel"));
        await Expect(Item("cvl-basic", "Bug triage")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));
        await Expect(List("cvl-basic").Locator("[aria-current='true']")).ToHaveCountAsync(1);

        await Expect(open).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri-tint)"));
        await Expect(open).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri-fg)"));
        var weight = int.Parse(await ComputedStyleAsync("#cvl-basic .bit-aicvl-sel", "font-weight"));
        var rest = int.Parse(await Item("cvl-basic", "Bug triage").EvaluateAsync<string>("e => getComputedStyle(e).fontWeight"));
        Assert.IsTrue(weight > rest, "The open conversation is drawn heavier.");
    }

    // ---------- select ----------

    [TestMethod]
    public async Task Select_AConversationClicked_BecomesTheCurrentOne()
    {
        await OpenAsync(Route);

        await Item("cvl-basic", "Trip ideas").ClickAsync();

        await Expect(Page.Locator("#cvl-selected")).ToHaveTextAsync("c3");
        await Expect(Item("cvl-basic", "Trip ideas")).ToHaveAttributeAsync("aria-current", "true");
        await Expect(Item("cvl-basic", "Release notes")).Not.ToHaveClassAsync(HasClass("bit-aicvl-sel"));
        await Expect(List("cvl-basic").Locator("[aria-current='true']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#cvl-log")).ToHaveTextAsync("select:c3");
    }

    [TestMethod]
    public async Task Select_WithTheKeyboard_TabMovesThroughTheRowsAndEnterOrSpaceOpens()
    {
        await OpenAsync(Route);

        // From the new button, Tab reaches the first conversation and then its delete button, then the next one.
        await NewButton("cvl-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Item("cvl-basic", "Release notes")).ToBeFocusedAsync();
        await Expect(Item("cvl-basic", "Release notes")).ToHaveCSSAsync("outline-style", "solid");
        await Expect(Item("cvl-basic", "Release notes")).ToHaveCSSAsync("outline-color", await Page.ResolveColorAsync("var(--bit-clr-pri-focus)"));

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Delete("cvl-basic", "Release notes")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Item("cvl-basic", "Bug triage")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#cvl-selected")).ToHaveTextAsync("c2");
        await Expect(Item("cvl-basic", "Bug triage")).ToHaveAttributeAsync("aria-current", "true");
        await Expect(Item("cvl-basic", "Bug triage")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Item("cvl-basic", "Trip ideas")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#cvl-selected")).ToHaveTextAsync("c3");
        await Expect(Page.Locator("#cvl-log")).ToHaveTextAsync("select:c2,select:c3");
    }

    // ---------- new ----------

    [TestMethod]
    public async Task New_PressedWithTheKeyboard_AddsAnUntitledConversationAndOpensIt()
    {
        await OpenAsync(Route);

        var button = NewButton("cvl-basic");
        await Expect(button).ToHaveTextAsync("New conversation");
        // The Add and Delete glyphs are the core font's own.
        var glyph = await button.Locator("i").First.GlyphAsync();
        Assert.IsFalse(glyph.Content is "none" or "normal" or "", $"The Add glyph draws a character, was {glyph.Content}.");
        await Expect(button.Locator("i").First).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(Delete("cvl-basic", "Release notes").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Delete"));

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#cvl-log")).ToHaveTextAsync("new");
        await Expect(Page.Locator("#cvl-selected")).ToHaveTextAsync("n1");
        var today = List("cvl-basic").GetByRole(AriaRole.List, new() { Name = "Today" });
        await Expect(today.Locator(".bit-aicvl-ttl")).ToHaveTextAsync(["Untitled", "Release notes", "Bug triage"]);
        await Expect(Item("cvl-basic", "Untitled")).ToHaveAttributeAsync("aria-current", "true");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#cvl-log")).ToHaveTextAsync("new,new");
    }

    // ---------- delete ----------

    [TestMethod]
    public async Task Delete_TheButtonOfAConversation_IsNamedAfterItAndRemovesItWithItsEmptyGroup()
    {
        await OpenAsync(Route);

        var delete = Delete("cvl-basic", "Trip ideas");
        await Expect(delete).ToHaveAttributeAsync("title", "Delete");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Delete Trip ideas" })).ToHaveCountAsync(1);

        await delete.ClickAsync();

        await Expect(Page.Locator("#cvl-log")).ToHaveTextAsync("delete:c3");
        await Expect(Item("cvl-basic", "Trip ideas")).ToHaveCountAsync(0);
        await Expect(List("cvl-basic").Locator(".bit-aicvl-grp", new() { HasText = "Yesterday" })).ToHaveCountAsync(0);
        await Expect(List("cvl-basic").Locator(".bit-aicvl-grp")).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task Delete_WithTheKeyboard_RemovesTheConversation()
    {
        await OpenAsync(Route);

        await Item("cvl-basic", "Budget review").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Delete("cvl-basic", "Budget review")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#cvl-log")).ToHaveTextAsync("delete:c4");
        await Expect(Item("cvl-basic", "Budget review")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cvl-selected")).ToHaveTextAsync("c1");

        // The button pressed went away with its conversation: the focus is on the one that took its place, not the page.
        await Expect(Item("cvl-basic", "Old project")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Plain_NoNewButtonAndNoOnDelete_RendersTheRowsAlone()
    {
        await OpenAsync(Route);

        await Expect(NewButton("cvl-plain")).ToHaveCountAsync(0);
        await Expect(List("cvl-plain").Locator("button[aria-label^='Delete']")).ToHaveCountAsync(0);
        await Expect(List("cvl-plain").Locator("button.bit-aicvl-ibt")).ToHaveCountAsync(2);
        await Expect(Item("cvl-plain", "Second conversation")).ToHaveAttributeAsync("aria-current", "true");

        // The row's button takes the whole width of its row.
        var row = await List("cvl-plain").Locator(".bit-aicvl-itm").First.RectAsync();
        var button = await Item("cvl-plain", "First conversation").RectAsync();
        Assert.AreEqual(row.Right - row.Left, button.Right - button.Left, 1);
    }

    // ---------- titles ----------

    [TestMethod]
    public async Task Titles_NoneIsUntitledAndALongOneIsCutWithAnEllipsis()
    {
        await OpenAsync(Route);

        await Expect(List("cvl-titles").Locator(".bit-aicvl-ttl").First).ToHaveTextAsync("Untitled");

        var title = List("cvl-titles").Locator(".bit-aicvl-ttl").Nth(1);
        await Expect(title).ToHaveCSSAsync("text-overflow", "ellipsis");
        await Expect(title).ToHaveCSSAsync("white-space", "nowrap");
        Assert.IsTrue(await title.EvaluateAsync<bool>("e => e.scrollWidth > e.clientWidth"), "The long title is cut.");
        await Expect(Item("cvl-titles", "A title far too long")).ToHaveAttributeAsync("title", "The preview of the long one");
        var row = await List("cvl-titles").Locator(".bit-aicvl-itm").Nth(1).RectAsync();
        var list = await List("cvl-titles").RectAsync();
        Assert.IsTrue(row.Right <= list.Right + 0.5, "The row stays inside the list.");
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_EveryButton_IsDisabledAndRaisesNothing()
    {
        await OpenAsync(Route);

        var list = List("cvl-disabled");
        await Expect(list).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(NewButton("cvl-disabled")).ToBeDisabledAsync();
        foreach (var title in new[] { "First conversation", "Second conversation" })
        {
            await Expect(Item("cvl-disabled", title)).ToBeDisabledAsync();
            await Expect(Item("cvl-disabled", title)).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(Delete("cvl-disabled", title)).ToBeDisabledAsync();
        }
        await Expect(Item("cvl-disabled", "First conversation")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));

        await Item("cvl-disabled", "First conversation").DispatchEventAsync("click");
        await Delete("cvl-disabled", "First conversation").DispatchEventAsync("click");
        await NewButton("cvl-disabled").DispatchEventAsync("click");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#cvl-disabled-log")).ToHaveTextAsync("0");
    }

    // ---------- hover ----------

    [TestMethod]
    public async Task Hover_ARow_TakesTheHoverBackgroundOfTheTheme()
    {
        await OpenAsync(Route);

        var row = Item("cvl-basic", "Bug triage");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(row).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        await row.HoverAsync();
        await Expect(row).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri-hover)"));
    }

    [TestMethod]
    public async Task Hover_ARowPaintedThroughClasses_KeepsThatPaintUnderThePointer()
    {
        await OpenAsync(Route);

        var row = Item("cvl-classes", "Second conversation");
        await Expect(row).ToHaveClassAsync(HasClass("e2e-cvl-paint"));
        await Expect(row).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");

        await row.HoverAsync();
        await Expect(row).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheRowsAndTheGroups()
    {
        await OpenAsync(Route);

        var list = List("cvl-var");
        await Expect(list).ToHaveCSSAsync("padding-top", "13px");
        await Expect(list).ToHaveCSSAsync("row-gap", "7px");
        await Expect(list.Locator(".bit-aicvl-lst")).ToHaveCSSAsync("row-gap", "7px");
        await Expect(list.Locator(".bit-aicvl-grp")).ToHaveCSSAsync("color", "rgb(1, 100, 1)");

        var open = Item("cvl-var", "First conversation");
        await Expect(open).ToHaveCSSAsync("color", "rgb(250, 250, 250)");
        await Expect(open).ToHaveCSSAsync("background-color", "rgb(20, 30, 40)");
        await Expect(open).ToHaveCSSAsync("border-top-left-radius", "9px");

        var other = Item("cvl-var", "Second conversation");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(other).ToHaveCSSAsync("color", "rgb(2, 3, 4)");
        await Expect(other).ToHaveCSSAsync("background-color", "rgb(240, 240, 200)");

        await other.HoverAsync();
        await Expect(other).ToHaveCSSAsync("background-color", "rgb(200, 240, 240)");
        await open.HoverAsync();
        await Expect(open).ToHaveCSSAsync("background-color", "rgb(60, 70, 80)");

        var first = await list.Locator(".bit-aicvl-itm").Nth(0).RectAsync();
        var second = await list.Locator(".bit-aicvl-itm").Nth(1).RectAsync();
        Assert.AreEqual(7, second.Top - first.Bottom, 1);
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheirParts()
    {
        await OpenAsync(Route);

        var list = List("cvl-classes");
        await Expect(list).ToHaveClassAsync(HasClass("e2e-cvl-root"));
        await Expect(list.Locator(".bit-aicvl-grp")).ToHaveClassAsync(HasClass("e2e-cvl-group"));
        await Expect(list.Locator(".bit-aicvl-grp")).ToHaveCSSAsync("letter-spacing", "5px");
        await Expect(list.Locator(".bit-aicvl-itm").First).ToHaveClassAsync(HasClass("e2e-cvl-item"));
        await Expect(list.Locator(".bit-aicvl-ttl").First).ToHaveClassAsync(HasClass("e2e-cvl-title"));
        await Expect(Item("cvl-classes", "First conversation")).ToHaveCSSAsync("outline-color", "rgb(7, 7, 7)");
        await Expect(Item("cvl-classes", "Second conversation")).ToHaveCSSAsync("outline-style", "none");
    }

    // ---------- rtl, forced colors, contrast ----------

    [TestMethod]
    public async Task Rtl_TheRows_ReadFromTheRight()
    {
        await OpenAsync(Route);

        var list = List("cvl-rtl");
        await Expect(list).ToHaveAttributeAsync("dir", "rtl");
        var button = await Item("cvl-rtl", "First conversation").RectAsync();
        var title = await Item("cvl-rtl", "First conversation").Locator(".bit-aicvl-ttl").RectAsync();
        var delete = await Delete("cvl-rtl", "First conversation").RectAsync();
        Assert.IsTrue(delete.Right <= button.Left + 1, "The delete button is at the left of the row.");

        var text = await Item("cvl-rtl", "First conversation").Locator(".bit-aicvl-ttl").EvaluateAsync<double>(
            "e => { const r = document.createRange(); r.selectNodeContents(e); return r.getBoundingClientRect().right; }");
        Assert.IsTrue(text > title.Right - 2, "The title starts at the right.");
    }

    [TestMethod]
    public async Task ForcedColors_TheOpenConversation_IsOutlinedInHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var open = Item("cvl-plain", "Second conversation");
        await Expect(open).ToHaveCSSAsync("outline-style", "solid");
        await Expect(open).ToHaveCSSAsync("outline-color", await Page.ResolveColorAsync("Highlight"));
    }

    [TestMethod]
    public async Task Contrast_More_OutlinesTheOpenConversation()
    {
        // Emulated before the page loads: Firefox does not re-apply the rules of a loaded stylesheet to a changed one.
        await Page.EmulateMediaAsync(new() { Contrast = Contrast.More });
        await OpenAsync(Route);
        if (await Page.MatchesMediaAsync("(prefers-contrast: more)") is false) Assert.Inconclusive("The engine does not emulate prefers-contrast.");

        var open = Item("cvl-plain", "Second conversation");
        await Expect(open).ToHaveCSSAsync("outline-style", "solid");
        await Expect(open).ToHaveCSSAsync("outline-color", await open.EvaluateAsync<string>("e => getComputedStyle(e).color"));
    }
}
