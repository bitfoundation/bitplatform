using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiCollapsible at /e2e/aicollapsible in a real browser: a closed panel whose body is not rendered, its real
/// button header opening and closing it by the pointer and by the keyboard (Tab, Enter, Space) and announcing its state,
/// a bound and an unbound Open, HeaderTemplate and a SuffixTemplate kept out of the toggle's name, Disabled, the
/// Styles/Classes and the --bit-Accordion-* variables reaching the panel, and right-to-left.
/// </summary>
[TestClass]
public class BitAiCollapsibleE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aicollapsible";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Header(string id) => Page.Locator($"#{id} button.bit-acd-hdr");

    private ILocator Panel(string id) => Page.Locator($"#{id} .bit-acd-con");

    private ILocator Chevron(string id) => Page.Locator($"#{id} .bit-acd-eiw");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    [TestMethod]
    public async Task Closed_TheHeaderIsAButtonInAHeading_AndTheBodyIsNotRendered()
    {
        await OpenAsync(Route);

        var header = Header("clp-basic");
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(header).ToHaveAttributeAsync("type", "button");
        await Expect(header).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Page.Locator("#clp-basic [role=heading]")).ToHaveAttributeAsync("aria-level", "3");
        await Expect(Page.Locator("#clp-basic").GetByRole(AriaRole.Button, new() { Name = "Thinking", Exact = true })).ToHaveCountAsync(1);

        var panelId = await Panel("clp-basic").GetAttributeAsync("id");
        await Expect(header).ToHaveAttributeAsync("aria-controls", panelId!);
        await Expect(Panel("clp-basic")).ToHaveAttributeAsync("role", "region");
        await Expect(Page.Locator("#clp-basic-body")).ToHaveCountAsync(0);

        // The chevron leads, the way a disclosure panel reads.
        var chevron = await Chevron("clp-basic").RectAsync();
        var title = await Page.Locator("#clp-basic .bit-acd-ttl").RectAsync();
        Assert.IsTrue(chevron.Right <= title.Left + 1, "The chevron sits before the title.");
    }

    [TestMethod]
    public async Task APress_OpensAndClosesThePanel_AndRaisesOpenChanged()
    {
        await OpenAsync(Route);

        await Header("clp-basic").ClickAsync();

        await Expect(Header("clp-basic")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#clp-basic-body")).ToBeVisibleAsync();
        await Expect(Page.Locator("#clp-basic-body")).ToHaveTextAsync("The model weighed three options before answering.");
        await Expect(Text("clp-basic-changes")).ToHaveTextAsync("True");
        await Expect(Chevron("clp-basic")).ToHaveClassAsync(HasClass("bit-ico--r180"));

        await Header("clp-basic").ClickAsync();

        await Expect(Header("clp-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#clp-basic-body")).ToHaveCountAsync(0);
        await Expect(Text("clp-basic-changes")).ToHaveTextAsync("True,False");
        await Expect(Chevron("clp-basic")).Not.ToHaveClassAsync(HasClass("bit-ico--r180"));
    }

    [TestMethod]
    public async Task Keyboard_TabReachesTheHeader_AndEnterAndSpaceToggleIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#clp-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Header("clp-basic")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Header("clp-basic")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#clp-basic-body")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync(" ");
        await Expect(Header("clp-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#clp-basic-body")).ToHaveCountAsync(0);
        await Expect(Header("clp-basic")).ToBeFocusedAsync();
        await Expect(Text("clp-basic-changes")).ToHaveTextAsync("True,False");

        // The focus ring of a keyboard focus is drawn.
        Assert.IsTrue(await Header("clp-basic").EvaluateAsync<bool>(
            "e => { const s = getComputedStyle(e); return (s.boxShadow && s.boxShadow !== 'none') || (s.outlineStyle !== 'none' && parseFloat(s.outlineWidth) > 0); }"),
            "The focused header draws a focus ring.");
    }

    [TestMethod]
    public async Task Bound_OpenFollowsThePage_AndThePanelReportsBack()
    {
        await OpenAsync(Route);

        await Expect(Header("clp-bound")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#clp-bound-body")).ToBeVisibleAsync();

        await Header("clp-bound").ClickAsync();
        await Expect(Text("clp-bound-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#clp-bound-body")).ToHaveCountAsync(0);

        await Page.Locator("#clp-bound-toggle").ClickAsync();
        await Expect(Text("clp-bound-open")).ToHaveTextAsync("True");
        await Expect(Header("clp-bound")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#clp-bound-body")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Unbound_AnOpenPanelClosesItselfWhenToggled()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#clp-unbound-body")).ToBeVisibleAsync();

        await Header("clp-unbound").ClickAsync();

        await Expect(Header("clp-unbound")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#clp-unbound-body")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Templates_TheHeaderTemplateNamesTheToggle_AndTheSuffixStaysOutOfIt()
    {
        await OpenAsync(Route);

        var header = Header("clp-templates");
        await Expect(header.Locator("#clp-custom-header")).ToHaveTextAsync("Custom header");
        await Expect(Page.Locator("#clp-templates").GetByRole(AriaRole.Button, new() { Name = "Custom header", Exact = true })).ToHaveCountAsync(1);

        // The suffix is beside the heading, not inside the button.
        await Expect(Page.Locator("#clp-templates button #clp-suffix")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#clp-templates [role=heading] #clp-suffix")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#clp-suffix")).ToHaveTextAsync("3 steps");
        var suffix = await Page.Locator("#clp-suffix").RectAsync();
        var title = await Page.Locator("#clp-custom-header").RectAsync();
        Assert.IsTrue(suffix.Left >= title.Right, "The suffix ends the header row.");

        await header.ClickAsync();
        await Expect(Page.Locator("#clp-templates-body")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Disabled_TheToggleIsUnavailable_AndThePanelStaysClosed()
    {
        await OpenAsync(Route);

        await Expect(Root("clp-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        var header = Header("clp-dis");
        await Expect(header).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(header).ToHaveAttributeAsync("tabindex", "-1");

        await header.ClickAsync(new() { Force = true });
        await header.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // A panel that opens, after it, proves the presses were answered.
        await Header("clp-basic").ClickAsync();
        await Expect(Text("clp-basic-changes")).ToHaveTextAsync("True");
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#clp-dis-body")).ToHaveCountAsync(0);
        await Expect(Text("clp-dis-changes")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Style_TheStylesAndClasses_AndTheAccordionsVariables_ReachThePanel()
    {
        await OpenAsync(Route);

        await Expect(Root("clp-style")).ToHaveClassAsync(HasClass("clp-custom-root"));
        await Expect(Header("clp-style")).ToHaveClassAsync(HasClass("clp-custom-header"));
        await Expect(Header("clp-style")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");

        await Expect(Page.Locator("#clp-accordion-var .bit-acd-ttl")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        // The root lets a wide body wrap inside it rather than stretch the track it sits in.
        await Expect(Root("clp-basic")).ToHaveCSSAsync("min-width", "0px");
        await Expect(Page.Locator("#clp-basic .bit-acd")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri)"));
    }

    [TestMethod]
    public async Task Rtl_TheChevronLeadsOnTheRight_ExplicitOrInherited()
    {
        await OpenAsync(Route);

        await Expect(Root("clp-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "clp-rtl", "clp-rtl-inherited" })
        {
            await Expect(Header(id)).ToHaveCSSAsync("direction", "rtl");
            var chevron = await Chevron(id).RectAsync();
            var title = await Page.Locator($"#{id} .bit-acd-ttl").RectAsync();
            Assert.IsTrue(chevron.Left >= title.Right - 1, $"#{id}: the chevron is on the right of the title.");
        }

        var ltrChevron = await Chevron("clp-ltr").RectAsync();
        var ltrTitle = await Page.Locator("#clp-ltr .bit-acd-ttl").RectAsync();
        Assert.IsTrue(ltrChevron.Right <= ltrTitle.Left + 1, "Left to right, the chevron is on the left of the title.");

        await Header("clp-rtl").ClickAsync();
        await Expect(Header("clp-rtl")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#clp-rtl .bit-acd-con p")).ToHaveTextAsync("بدنه");
        await Expect(Page.Locator("#clp-rtl .bit-acd-con")).ToHaveCSSAsync("direction", "rtl");
    }
}
