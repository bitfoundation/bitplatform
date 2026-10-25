using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitCheckbox on <c>/e2e/checkbox</c> (and <c>/e2e/checkbox/autofocus</c>): the prerendered markup,
/// toggling by pointer and keyboard, the per-state icons, every label placement, FullWidth / NoWrap, the description,
/// the indeterminate and three-state cycles, one-way and two-way binding of Value and Indeterminate, the templates,
/// every callback with its order and arguments, cancellation, StopPropagation, read-only / required, EditForm
/// validation (message, aria-invalid, NoValidate, InputHtmlAttributes, Name), the accessibility attributes and focus,
/// the busy states, the BitParams cascade, every color and size against the computed styles, the external icons,
/// Style / Class / Styles / Classes, the public CSS variables (on the instance and on an ancestor), RTL (explicit and
/// inherited), the disabled-markup and activation contracts, runtime re-renders, unmounting and reduced motion.
/// </summary>
[TestClass]
public class BitCheckboxE2ETests : E2ETestBase
{
    private const string Url = "/e2e/checkbox";

    private static readonly (string Suffix, string Token)[] Colors =
    [
        ("pri", "pri"), ("sec", "sec"), ("ter", "ter"), ("inf", "inf"), ("suc", "suc"), ("wrn", "wrn"), ("swr", "swr"),
        ("err", "err"), ("pbg", "bg-pri"), ("sbg", "bg-sec"), ("tbg", "bg-ter"), ("pfg", "fg-pri"), ("sfg", "fg-sec"),
        ("tfg", "fg-ter"), ("pbr", "brd-pri"), ("sbr", "brd-sec"), ("tbr", "brd-ter")
    ];

    private static readonly Regex NotNone = new("^(?!none$).+");

    private ILocator Root(string id) => Page.Locator($"#{id}");
    private ILocator Input(string id) => Page.Locator($"#{id} .bit-chb-inp");
    private ILocator Label(string id) => Page.Locator($"#{id} .bit-chb-lbl");
    private ILocator Box(string id) => Page.Locator($"#{id} .bit-chb-box");
    private ILocator Icon(string id) => Page.Locator($"#{id} .bit-chb-ico");
    private ILocator Text(string id) => Page.Locator($"#{id} .bit-chb-txt");
    private ILocator Span(string id) => Page.Locator($"#{id}");

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const d = document.createElement('div');
            d.style.color = v;
            document.body.appendChild(d);
            const c = getComputedStyle(d).color;
            d.remove();
            return c;
        }", cssColor);
    }

    private static Task<string> StyleOfAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
    }

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no layout box.");
        return box;
    }

    private static double Px(string value) => double.Parse(value.Replace("px", string.Empty), CultureInfo.InvariantCulture);

    private Task WaitForIndeterminatePropertyAsync(string id, bool value)
    {
        return Page.WaitForFunctionAsync($"() => document.querySelector('#{id} .bit-chb-inp').indeterminate === {(value ? "true" : "false")}");
    }

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheCheckboxBeforeTheCircuitStarts()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"cb-basic\"");
        StringAssert.Contains(html, "bit-chb-inp");
        StringAssert.Contains(html, "Basic checkbox");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"cb-default-checked\"[^>]*class=\"[^\"]*bit-chb-ckd"), "The checked-by-default checkbox is prerendered checked.");

        await OpenAsync(Url);
        await Expect(Input("cb-basic")).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Basic_ClickingTheLabelOrTheBox_TogglesTheUncontrolledCheckbox()
    {
        await OpenAsync(Url);

        await Text("cb-basic").ClickAsync();

        await Expect(Input("cb-basic")).ToBeCheckedAsync();
        await Expect(Root("cb-basic")).ToHaveClassAsync(HasClass("bit-chb-ckd"));
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Span("basic-last")).ToHaveTextAsync("True");

        await Box("cb-basic").ClickAsync();

        await Expect(Input("cb-basic")).Not.ToBeCheckedAsync();
        await Expect(Root("cb-basic")).Not.ToHaveClassAsync(HasClass("bit-chb-ckd"));
        await Expect(Span("basic-change-count")).ToHaveTextAsync("2");
        await Expect(Span("basic-last")).ToHaveTextAsync("False");
        await Expect(Input("cb-basic")).ToHaveAttributeAsync("type", "checkbox");
    }

    [TestMethod]
    public async Task Basic_DefaultValue_StartsCheckedAndStaysUncontrolled()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-default-checked")).ToBeCheckedAsync();
        await Expect(Input("cb-default-checked")).ToHaveAttributeAsync("value", "true");

        await Text("cb-default-checked").ClickAsync();

        await Expect(Input("cb-default-checked")).Not.ToBeCheckedAsync();
        await Expect(Input("cb-default-checked")).ToHaveAttributeAsync("value", "false");
    }

    [TestMethod]
    public async Task Basic_Title_IsTheNativeTooltipOfTheLabel()
    {
        await OpenAsync(Url);

        await Expect(Label("cb-title")).ToHaveAttributeAsync("title", "The native tooltip of the checkbox");
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndIgnoresClicks()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("cb-disabled")).ToBeDisabledAsync();
        await Expect(Input("cb-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Root("cb-disabled")).ToHaveCSSAsync("pointer-events", "none");

        await Text("cb-disabled").ClickAsync(new() { Force = true });
        await Text("cb-disabled").DispatchEventAsync("click");
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Input("cb-disabled")).Not.ToBeCheckedAsync();
        await Expect(Input("cb-disabled-checked")).ToBeCheckedAsync();

        var disabled = await ResolveColorAsync("var(--bit-clr-pri-dis)");
        await Expect(Box("cb-disabled-checked")).ToHaveCSSAsync("background-color", disabled);
        await Expect(Root("cb-disabled")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-pri-dis-text)"));
    }

    [TestMethod]
    public async Task Icons_CheckIconName_RendersTheNamedGlyphShownOnlyOnceChecked()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Icon("cb-icon-check")).ToHaveClassAsync(HasClass("bit-icon--Heart"));
        await Expect(Icon("cb-icon-default")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Icon("cb-icon-check")).ToHaveCSSAsync("opacity", "0");

        await Text("cb-icon-check").ClickAsync();
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Icon("cb-icon-check")).ToHaveCSSAsync("opacity", "1");
        await Expect(Icon("cb-icon-check")).ToHaveClassAsync(HasClass("bit-icon--Heart"));
    }

    [TestMethod]
    public async Task Icons_UncheckedIconName_IsVisibleWhileUncheckedAndGivesWayToTheCheckMark()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Root("cb-icon-unchecked")).ToHaveClassAsync(HasClass("bit-chb-uci"));
        await Expect(Icon("cb-icon-unchecked")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await Expect(Icon("cb-icon-unchecked")).ToHaveCSSAsync("opacity", "1");

        await Text("cb-icon-unchecked").ClickAsync();

        await Expect(Icon("cb-icon-unchecked")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Icon("cb-icon-unchecked")).Not.ToHaveClassAsync(HasClass("bit-icon--Cancel"));
    }

    [TestMethod]
    public async Task Icons_IndeterminateIconName_ReplacesTheFilledSquare()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-icon-ind")).ToHaveClassAsync(HasClass("bit-chb-cii"));
        await Expect(Icon("cb-icon-ind")).ToHaveClassAsync(HasClass("bit-icon--Remove"));
        await Expect(Icon("cb-icon-ind")).Not.ToHaveClassAsync(HasClass("bit-chb-idi"));

        // The default mixed state draws a filled square half the size of the box.
        await Expect(Icon("cb-dis-ind")).ToHaveClassAsync(HasClass("bit-chb-idi"));
        var square = await BoxOfAsync(Icon("cb-ind"));
        var box = await BoxOfAsync(Box("cb-ind"));
        Assert.AreEqual(box.Width / 2, square.Width, 1.5, "The square of the mixed state is half the box.");
    }

    [TestMethod]
    public async Task Icons_DisabledCustomIcon_KeepsItsGlyphInTheDisabledColors()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-icon-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Icon("cb-icon-disabled")).ToHaveClassAsync(HasClass("bit-icon--WavingHand"));
        await Expect(Icon("cb-icon-disabled")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-pri-dis-text)"));
    }

    [TestMethod]
    public async Task Icons_CheckIconAriaLabel_NamesTheGlyphOnlyWhileItIsShown()
    {
        await OpenAsync(Url);

        await Expect(Icon("cb-icon-named")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Icon("cb-icon-named")).Not.ToHaveAttributeAsync("role", "img");

        await Text("cb-icon-named").ClickAsync();

        await Expect(Icon("cb-icon-named")).ToHaveAttributeAsync("role", "img");
        await Expect(Icon("cb-icon-named")).ToHaveAttributeAsync("aria-label", "Favorite");
        await Expect(Icon("cb-icon-named")).Not.ToHaveAttributeAsync("aria-hidden", "true");

        await Expect(Icon("cb-icon-default")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task LabelPlacement_EachValue_PutsTheLabelOnThatSideOfTheBox()
    {
        await OpenAsync(Url);

        var cases = new (string Id, string? Class, string Side)[]
        {
            ("cb-place-end", "bit-chb-lnd", "end"),
            ("cb-place-start", "bit-chb-lst", "start"),
            ("cb-place-top", "bit-chb-ltp", "top"),
            ("cb-place-bottom", "bit-chb-lbt", "bottom"),
            ("cb-place-reversed", "bit-chb-rvs", "start"),
            ("cb-place-reversed-end", "bit-chb-lnd", "end"),
            ("cb-place-left", null, "end"),
        };

        foreach (var (id, cls, side) in cases)
        {
            if (cls is not null) await Expect(Root(id)).ToHaveClassAsync(HasClass(cls));

            var box = await BoxOfAsync(Box(id));
            var text = await BoxOfAsync(Text(id));

            switch (side)
            {
                case "end":
                    Assert.IsTrue(box.X + box.Width <= text.X + 0.5, $"{id}: the label is after the box.");
                    break;
                case "start":
                    Assert.IsTrue(text.X + text.Width <= box.X + 0.5, $"{id}: the label is before the box.");
                    break;
                case "top":
                    Assert.IsTrue(text.Y + text.Height <= box.Y + 0.5, $"{id}: the label is above the box.");
                    break;
                case "bottom":
                    Assert.IsTrue(box.Y + box.Height <= text.Y + 0.5, $"{id}: the label is below the box.");
                    break;
            }
        }

        // The physical placements carry no class of their own and leave the default layout.
        await Expect(Root("cb-place-left")).Not.ToHaveClassAsync(new Regex("bit-chb-l(nd|st|tp|bt)"));
        await Expect(Root("cb-place-reversed-end")).ToHaveClassAsync(HasClass("bit-chb-rvs"));
    }

    [TestMethod]
    public async Task FullWidth_StretchesAcrossTheContainerAndPushesTheBoxToTheFarEdge()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-fw")).ToHaveClassAsync(HasClass("bit-chb-fwi"));

        var panel = await BoxOfAsync(Page.Locator("#fw-panel"));
        var root = await BoxOfAsync(Root("cb-fw"));
        var box = await BoxOfAsync(Box("cb-fw"));
        var text = await BoxOfAsync(Text("cb-fw"));

        Assert.AreEqual(panel.Width, root.Width, 1, "The checkbox spans its container.");
        Assert.AreEqual(panel.X + panel.Width, box.X + box.Width, 1, "The box sits at the far edge.");
        Assert.AreEqual(panel.X, text.X, 1, "The label sits at the reading edge.");
    }

    [TestMethod]
    public async Task NoWrap_KeepsTheLabelOnOneLineAndEndsItWithAnEllipsis()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-nowrap")).ToHaveClassAsync(HasClass("bit-chb-nwr"));
        await Expect(Text("cb-nowrap")).ToHaveCSSAsync("white-space", "nowrap");
        await Expect(Text("cb-nowrap")).ToHaveCSSAsync("text-overflow", "ellipsis");

        var overflows = await Text("cb-nowrap").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth");
        Assert.IsTrue(overflows, "The label is cut off.");

        var wrapped = await BoxOfAsync(Text("cb-wrap"));
        var single = await BoxOfAsync(Text("cb-nowrap"));
        Assert.IsTrue(wrapped.Height > single.Height * 1.5, "Without NoWrap the same label wraps onto several lines.");
        await Expect(Label("cb-nowrap")).ToHaveAttributeAsync("title", "Send me a weekly digest of everything that happened in my workspace");
    }

    [TestMethod]
    public async Task Description_RendersASecondLineThatDescribesTheInputAndIsNotAClickTarget()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#cb-desc .bit-chb-des");
        await Expect(description).ToHaveTextAsync("The subscription is renewed one day before it expires.");
        await Expect(Root("cb-desc")).ToHaveClassAsync(HasClass("bit-chb-hds"));

        var descriptionId = await description.GetAttributeAsync("id");
        await Expect(Input("cb-desc")).ToHaveAttributeAsync("aria-describedby", descriptionId!);
        await Expect(Page.GetByRole(AriaRole.Checkbox, new() { Name = "Auto renew", Exact = true }).First).ToHaveAccessibleDescriptionAsync("The subscription is renewed one day before it expires.");

        await description.ClickAsync();
        await Text("cb-desc").ClickAsync();
        await Expect(Span("desc-value")).ToHaveTextAsync("True");
        await description.ClickAsync();
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Span("desc-value")).ToHaveTextAsync("True");

        var text = await BoxOfAsync(Text("cb-desc"));
        var line = await BoxOfAsync(description);
        Assert.IsTrue(line.Y >= text.Y + text.Height - 1, "The description sits under the label.");
        Assert.AreEqual(text.X, line.X, 1.5, "The description lines up with the label rather than the box.");
    }

    [TestMethod]
    public async Task DescriptionTemplate_RendersItsMarkupUnderTheLabel()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cb-desc-tpl .bit-chb-des #desc-tpl-link")).ToBeVisibleAsync();
        await Expect(Page.Locator("#cb-desc-tpl .bit-chb-des")).ToContainTextAsync("before you agree.");
    }

    [TestMethod]
    public async Task Indeterminate_Bound_ShowsTheMixedStateAndTheFirstClickClearsIt()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-ind")).ToHaveClassAsync(HasClass("bit-chb-ind"));
        await Expect(Input("cb-ind")).ToHaveAttributeAsync("aria-checked", "mixed");
        await WaitForIndeterminatePropertyAsync("cb-ind", true);
        await Expect(Icon("cb-ind")).ToHaveClassAsync(HasClass("bit-chb-idi"));

        await Text("cb-ind").ClickAsync();

        await Expect(Span("ind-ind")).ToHaveTextAsync("False");
        await Expect(Span("ind-value")).ToHaveTextAsync("True");
        await Expect(Root("cb-ind")).Not.ToHaveClassAsync(HasClass("bit-chb-ind"));
        await Expect(Root("cb-ind")).ToHaveClassAsync(HasClass("bit-chb-ckd"));
        await Expect(Input("cb-ind")).Not.ToHaveAttributeAsync("aria-checked", "mixed");
        await WaitForIndeterminatePropertyAsync("cb-ind", false);

        await Page.Locator("#btn-ind-set").ClickAsync();

        await Expect(Input("cb-ind")).ToHaveAttributeAsync("aria-checked", "mixed");
        await WaitForIndeterminatePropertyAsync("cb-ind", true);
        await Expect(Page.GetByRole(AriaRole.Checkbox, new() { Name = "Indeterminate checkbox", Exact = true })).ToBeCheckedAsync(new() { Indeterminate = true });
    }

    [TestMethod]
    public async Task DefaultIndeterminate_StartsMixedAndIsClearedByAClick()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-def-ind")).ToHaveClassAsync(HasClass("bit-chb-ind"));
        await WaitForIndeterminatePropertyAsync("cb-def-ind", true);

        await Text("cb-def-ind").ClickAsync();

        await Expect(Root("cb-def-ind")).Not.ToHaveClassAsync(HasClass("bit-chb-ind"));
        await Expect(Input("cb-def-ind")).ToBeCheckedAsync();
        await WaitForIndeterminatePropertyAsync("cb-def-ind", false);
    }

    [TestMethod]
    public async Task Indeterminate_Disabled_DrawsTheSquareInTheDisabledColor()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-dis-ind")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Root("cb-dis-ind")).ToHaveClassAsync(HasClass("bit-chb-ind"));
        await Expect(Icon("cb-dis-ind")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri-dis)"));
        await Expect(Icon("cb-ind")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
    }

    [TestMethod]
    public async Task Indeterminate_SelectAll_ReflectsAPartiallySelectedList()
    {
        await OpenAsync(Url);

        await Text("cb-apple").ClickAsync();

        await Expect(Span("fruits-state")).ToHaveTextAsync("True,False,False");
        await Expect(Root("cb-select-all")).ToHaveClassAsync(HasClass("bit-chb-ind"));
        await Expect(Input("cb-select-all")).ToHaveAttributeAsync("aria-checked", "mixed");

        await Text("cb-banana").ClickAsync();
        await Text("cb-orange").ClickAsync();

        await Expect(Span("fruits-state")).ToHaveTextAsync("True,True,True");
        await Expect(Root("cb-select-all")).Not.ToHaveClassAsync(HasClass("bit-chb-ind"));
        await Expect(Input("cb-select-all")).ToBeCheckedAsync();

        await Text("cb-select-all").ClickAsync();

        await Expect(Span("fruits-state")).ToHaveTextAsync("False,False,False");
        await Expect(Input("cb-select-all")).Not.ToBeCheckedAsync();
        await Expect(Input("cb-apple")).Not.ToBeCheckedAsync();

        await Text("cb-select-all").ClickAsync();
        await Expect(Span("fruits-state")).ToHaveTextAsync("True,True,True");
        await Expect(Input("cb-orange")).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task ThreeState_EachClick_CyclesUncheckedCheckedIndeterminate()
    {
        await OpenAsync(Url);

        await Expect(Span("three-state")).ToHaveTextAsync("False,False");

        await Text("cb-three").ClickAsync();
        await Expect(Span("three-state")).ToHaveTextAsync("True,False");
        await Expect(Input("cb-three")).ToBeCheckedAsync();

        await Text("cb-three").ClickAsync();
        await Expect(Span("three-state")).ToHaveTextAsync("False,True");
        await Expect(Input("cb-three")).ToHaveAttributeAsync("aria-checked", "mixed");
        await WaitForIndeterminatePropertyAsync("cb-three", true);
        await Expect(Input("cb-three")).Not.ToBeCheckedAsync();

        await Text("cb-three").ClickAsync();
        await Expect(Span("three-state")).ToHaveTextAsync("False,False");
        await WaitForIndeterminatePropertyAsync("cb-three", false);
        await Expect(Input("cb-three")).Not.ToBeCheckedAsync();

        // The keyboard goes around the same cycle.
        await Input("cb-three").FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Span("three-state")).ToHaveTextAsync("True,False");
    }

    [TestMethod]
    public async Task ThreeState_NullableModel_CyclesFalseTrueNull()
    {
        await OpenAsync(Url);

        await Expect(Span("nullable-state")).ToHaveTextAsync("False");

        await Text("cb-nullable").ClickAsync();
        await Expect(Span("nullable-state")).ToHaveTextAsync("True");

        await Text("cb-nullable").ClickAsync();
        await Expect(Span("nullable-state")).ToHaveTextAsync("null");
        await Expect(Root("cb-nullable")).ToHaveClassAsync(HasClass("bit-chb-ind"));

        await Text("cb-nullable").ClickAsync();
        await Expect(Span("nullable-state")).ToHaveTextAsync("False");
        await Expect(Root("cb-nullable")).Not.ToHaveClassAsync(HasClass("bit-chb-ind"));
        await Expect(Input("cb-nullable")).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Binding_FixedValue_ClicksCannotMoveIt()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-fixed")).ToBeCheckedAsync();

        await Text("cb-fixed").ClickAsync();
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Input("cb-fixed")).ToBeCheckedAsync();
        await Expect(Root("cb-fixed")).ToHaveClassAsync(HasClass("bit-chb-ckd"));
    }

    [TestMethod]
    public async Task Binding_OneWayValue_FollowsThePageAndRefusesClicks()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-oneway")).Not.ToBeCheckedAsync();

        await Page.Locator("#btn-oneway").ClickAsync();
        await Expect(Input("cb-oneway")).ToBeCheckedAsync();
        await Expect(Root("cb-oneway")).ToHaveClassAsync(HasClass("bit-chb-ckd"));

        await Text("cb-oneway").ClickAsync();
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Input("cb-oneway")).ToBeCheckedAsync();

        await Page.Locator("#btn-oneway").ClickAsync();
        await Expect(Input("cb-oneway")).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Binding_TwoWayValue_FlowsBothWays()
    {
        await OpenAsync(Url);

        await Text("cb-twoway").ClickAsync();
        await Expect(Span("twoway-value")).ToHaveTextAsync("True");

        await Page.Locator("#btn-twoway").ClickAsync();
        await Expect(Span("twoway-value")).ToHaveTextAsync("False");
        await Expect(Input("cb-twoway")).Not.ToBeCheckedAsync();
        await Expect(Root("cb-twoway")).Not.ToHaveClassAsync(HasClass("bit-chb-ckd"));

        await Page.Locator("#btn-twoway").ClickAsync();
        await Expect(Input("cb-twoway")).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Binding_FixedIndeterminate_StaysMixedAfterAClick()
    {
        await OpenAsync(Url);

        await WaitForIndeterminatePropertyAsync("cb-fixed-ind", true);

        await Text("cb-fixed-ind").ClickAsync();

        // The value is unbound and moves; the one-way mixed state is put back.
        await Expect(Root("cb-fixed-ind")).ToHaveClassAsync(HasClass("bit-chb-ckd"));
        await Expect(Root("cb-fixed-ind")).ToHaveClassAsync(HasClass("bit-chb-ind"));
        await Expect(Input("cb-fixed-ind")).ToHaveAttributeAsync("aria-checked", "mixed");
        await WaitForIndeterminatePropertyAsync("cb-fixed-ind", true);
    }

    [TestMethod]
    public async Task Binding_OneWayIndeterminate_FollowsThePage()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-oneway-ind")).ToHaveClassAsync(HasClass("bit-chb-ind"));

        await Page.Locator("#btn-oneway-ind").ClickAsync();
        await Expect(Root("cb-oneway-ind")).Not.ToHaveClassAsync(HasClass("bit-chb-ind"));
        await WaitForIndeterminatePropertyAsync("cb-oneway-ind", false);

        await Page.Locator("#btn-oneway-ind").ClickAsync();
        await Expect(Root("cb-oneway-ind")).ToHaveClassAsync(HasClass("bit-chb-ind"));
        await WaitForIndeterminatePropertyAsync("cb-oneway-ind", true);
    }

    [TestMethod]
    public async Task Binding_TwoWayIndeterminate_FlowsBothWays()
    {
        await OpenAsync(Url);

        await Text("cb-twoway-ind").ClickAsync();
        await Expect(Span("twoway-ind-value")).ToHaveTextAsync("False");
        await Expect(Root("cb-twoway-ind")).Not.ToHaveClassAsync(HasClass("bit-chb-ind"));

        await Page.Locator("#btn-twoway-ind").ClickAsync();
        await Expect(Span("twoway-ind-value")).ToHaveTextAsync("True");
        await Expect(Root("cb-twoway-ind")).ToHaveClassAsync(HasClass("bit-chb-ind"));
        await WaitForIndeterminatePropertyAsync("cb-twoway-ind", true);
    }

    [TestMethod]
    public async Task LabelTemplate_RendersTheTemplateWhichStaysTheClickTarget()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cb-label-tpl .bit-chb-tpl #label-tpl-content")).ToHaveTextAsync("Label Template");
        await Expect(Page.Locator("#cb-label-tpl .bit-chb-txt")).ToHaveCountAsync(0);

        await Page.Locator("#label-tpl-content").ClickAsync();

        await Expect(Span("label-tpl-value")).ToHaveTextAsync("True");
        await Expect(Input("cb-label-tpl")).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task ChildContent_ReplacesTheBoxAndLabelAndKeepsTheBehaviourAndTheFocusRing()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-custom")).ToHaveClassAsync(HasClass("bit-chb-cct"));
        await Expect(Box("cb-custom")).ToHaveCountAsync(0);

        await Page.Locator("#custom-face").ClickAsync();
        await Expect(Span("custom-value")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#custom-face")).ToHaveTextAsync("X");

        await Page.Locator("#btn-before-custom").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("cb-custom")).ToBeFocusedAsync();
        await Expect(Label("cb-custom")).ToHaveCSSAsync("box-shadow", NotNone);

        await Page.Keyboard.PressAsync("Space");
        await Expect(Span("custom-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Events_Click_FiresOnClickOnChangingOnChangeInOrderAndOnce()
    {
        await OpenAsync(Url);

        await Text("cb-events").ClickAsync();
        await Expect(Page.Locator("#ev-counts")).ToContainTextAsync("click=1;changing=1;change=1;");
        await Expect(Page.Locator("#ev-log")).ToContainTextAsync("OnClick>OnChanging(True,False)>OnChange(True)");

        await Page.Locator("#btn-clear-log").ClickAsync();
        await Text("cb-events").ClickAsync();
        await Expect(Page.Locator("#ev-counts")).ToContainTextAsync("click=2;changing=2;change=2;");
        await Expect(Page.Locator("#ev-log")).ToContainTextAsync("OnClick>OnChanging(False,False)>OnChange(False)");
    }

    [TestMethod]
    public async Task Events_Focus_FiresOnFocusOnFocusInOnFocusOutAndOnBlur()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-before-events").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("cb-events")).ToBeFocusedAsync();
        await Expect(Page.Locator("#ev-counts")).ToContainTextAsync("focus=1;focusin=1;focusout=0;blur=0");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#btn-after-events")).ToBeFocusedAsync();
        await Expect(Page.Locator("#ev-counts")).ToContainTextAsync("focus=1;focusin=1;focusout=1;blur=1");
        await Expect(Page.Locator("#ev-counts")).ToContainTextAsync("click=0;changing=0;change=0;");
    }

    [TestMethod]
    public async Task OnChanging_Cancel_KeepsTheCheckboxInItsState()
    {
        await OpenAsync(Url);

        await Text("cb-guard").ClickAsync();
        await Expect(Span("guard-cancelled")).ToHaveTextAsync("1");
        await Expect(Input("cb-guard")).Not.ToBeCheckedAsync();
        await Expect(Span("guard-value")).ToHaveTextAsync("False");

        await Text("cb-allow").ClickAsync();
        await Text("cb-guard").ClickAsync();

        await Expect(Span("guard-value")).ToHaveTextAsync("True");
        await Expect(Input("cb-guard")).ToBeCheckedAsync();
        await Expect(Span("guard-cancelled")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task StopPropagation_KeepsTheClicksFromTheContainer()
    {
        await OpenAsync(Url);

        // The label click and the click it forwards to the input both bubble.
        await Text("cb-bubbles").ClickAsync();
        await Expect(Span("container-clicks")).ToHaveTextAsync("2");
        await Expect(Input("cb-bubbles")).ToBeCheckedAsync();

        await Page.Locator("#btn-reset-container").ClickAsync();
        await Expect(Span("container-clicks")).ToHaveTextAsync("0");

        await Text("cb-stops").ClickAsync();
        await Expect(Input("cb-stops")).ToBeCheckedAsync();
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Span("container-clicks")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task ReadOnly_IgnoresClicksAndSpaceButFollowsThePage()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-readonly")).ToHaveClassAsync(HasClass("bit-chb-rdl"));
        await Expect(Input("cb-readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Input("cb-readonly")).Not.ToBeDisabledAsync();

        await Text("cb-readonly").ClickAsync();
        await Input("cb-readonly").FocusAsync();
        await Expect(Input("cb-readonly")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Span("readonly-value")).ToHaveTextAsync("False");
        await Expect(Input("cb-readonly")).Not.ToBeCheckedAsync();

        await Page.Locator("#btn-readonly").ClickAsync();
        await Expect(Input("cb-readonly")).ToBeCheckedAsync();
        await Expect(Root("cb-readonly")).ToHaveClassAsync(HasClass("bit-chb-ckd"));
    }

    [TestMethod]
    public async Task Required_RendersTheNativeAttributeAndTheAsteriskOnPlainAndTemplatedLabels()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-required")).ToHaveAttributeAsync("required", "");
        await Expect(Input("cb-required")).Not.ToHaveAttributeAsync("aria-required", "true");
        await Expect(Root("cb-required")).ToHaveClassAsync(HasClass("bit-chb-req"));

        var asterisk = await Text("cb-required").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        StringAssert.Contains(asterisk, "*");

        var templated = await Page.Locator("#cb-required-tpl .bit-chb-tpl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        StringAssert.Contains(templated, "*");

        var color = await Text("cb-required").EvaluateAsync<string>("el => getComputedStyle(el, '::after').color");
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-req)"), color);

        // Named only by an AriaLabel, it has no label to hang the asterisk on.
        await Expect(Input("cb-required-arialabel")).ToHaveAttributeAsync("required", "");
        await Expect(Root("cb-required-arialabel")).Not.ToHaveClassAsync(HasClass("bit-chb-req"));
    }

    [TestMethod]
    public async Task Required_ReadOnlyOrDisabled_DropsTheNativeAttributeButAnnouncesTheRequirement()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-required-readonly")).Not.ToHaveAttributeAsync("required", "");
        await Expect(Input("cb-required-readonly")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Root("cb-required-readonly")).ToHaveClassAsync(HasClass("bit-chb-req"));

        await Expect(Input("cb-required-disabled")).Not.ToHaveAttributeAsync("required", "");
        await Expect(Input("cb-required-disabled")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Root("cb-required-disabled")).Not.ToHaveClassAsync(HasClass("bit-chb-req"));
    }

    [TestMethod]
    public async Task Validation_AnUncheckedRequiredTerm_RefusesTheSubmitAndMarksTheInputInvalid()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-terms")).Not.ToHaveAttributeAsync("aria-invalid", "true");

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Span("submit-result")).ToHaveTextAsync("invalid:1");
        await Expect(Page.Locator("#terms-error")).ToHaveTextAsync("You must agree to the terms and conditions.");
        await Expect(Input("cb-terms")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cb-terms")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Box("cb-terms")).ToHaveCSSAsync("border-color", await ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(Input("cb-terms")).ToHaveAttributeAsync("aria-describedby", new Regex(@"(^|\s)terms-error(\s|$)"));
        await Expect(Page.GetByRole(AriaRole.Checkbox, new() { Name = "I agree with the terms and conditions." })).ToHaveAccessibleDescriptionAsync("You must agree to the terms and conditions.");

        // NoValidate: the model has an error for it too, and the checkbox shows none of it.
        await Expect(Input("cb-novalidate")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cb-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        // The app's own aria-invalid is kept rather than overwritten, beside the other splatted attributes.
        await Expect(Input("cb-own-invalid")).ToHaveAttributeAsync("aria-invalid", "grammar");
        await Expect(Input("cb-own-invalid")).ToHaveAttributeAsync("data-test", "kept");
        await Expect(Root("cb-own-invalid")).ToHaveClassAsync(HasClass("bit-inv"));

        await Text("cb-terms").ClickAsync();

        await Expect(Page.Locator("#terms-error")).ToHaveTextAsync(string.Empty);
        await Expect(Input("cb-terms")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cb-terms")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Text("cb-novalidate").ClickAsync();
        await Text("cb-own-invalid").ClickAsync();
        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Span("submit-result")).ToHaveTextAsync("valid:1");
    }

    [TestMethod]
    public async Task Name_IsRenderedOnTheInput()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-named")).ToHaveAttributeAsync("name", "newsletter");
        await Expect(Input("cb-disabled")).Not.ToHaveAttributeAsync("name", new Regex(".*"));
    }

    [TestMethod]
    public async Task Keyboard_TabFocusesTheInputDrawsTheRingAndSpaceToggles()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-before-a11y").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Input("cb-a11y")).ToBeFocusedAsync();
        await Expect(Box("cb-a11y")).ToHaveCSSAsync("box-shadow", NotNone);
        await Expect(Box("cb-basic")).ToHaveCSSAsync("box-shadow", "none");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Span("a11y-value")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await Expect(Span("a11y-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task TabIndex_MinusOne_TakesTheCheckboxOutOfTheTabOrder()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-tabindex")).ToHaveAttributeAsync("tabindex", "-1");

        await Input("cb-a11y").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Input("cb-after-tabindex")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusAsync_MovesTheFocusToTheInput()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-focus").ClickAsync();

        await Expect(Input("cb-focus-target")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_FocusesTheCheckboxOnItsFirstRender()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}/autofocus");
        StringAssert.Contains(await response.TextAsync(), "autofocus");

        await OpenAsync($"{Url}/autofocus");

        await Expect(Input("cb-autofocus")).ToBeFocusedAsync();
        await Expect(Input("cb-autofocus-disabled")).Not.ToHaveAttributeAsync("autofocus", new Regex(".*"));
    }

    [TestMethod]
    public async Task Aria_LabelDescriptionLabelledbySetSizeAndControls_AreRenderedOnTheInput()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-arialabel")).ToHaveAttributeAsync("aria-label", "Select the row");
        await Expect(Page.GetByRole(AriaRole.Checkbox, new() { Name = "Select the row" })).ToHaveCountAsync(1);

        var hidden = Page.Locator("#cb-ariadesc .bit-chb-dsc");
        await Expect(hidden).ToHaveTextAsync("The subscription will be renewed one day before it expires.");
        Assert.AreEqual(1, Math.Round((await BoxOfAsync(hidden)).Width), "The aria description is visually hidden.");
        var visibleId = await Page.Locator("#cb-ariadesc .bit-chb-des").GetAttributeAsync("id");
        var hiddenId = await hidden.GetAttributeAsync("id");
        await Expect(Input("cb-ariadesc")).ToHaveAttributeAsync("aria-describedby", $"{visibleId} {hiddenId}");

        await Expect(Input("cb-labelledby")).ToHaveAttributeAsync("aria-labelledby", "newsletter-label");
        await Expect(Page.GetByRole(AriaRole.Checkbox, new() { Name = "Weekly newsletter" })).ToHaveCountAsync(1);

        await Expect(Input("cb-set3")).ToHaveAttributeAsync("aria-setsize", "10");
        await Expect(Input("cb-set3")).ToHaveAttributeAsync("aria-posinset", "3");
        await Expect(Input("cb-controls")).ToHaveAttributeAsync("aria-controls", "fruit-list");
    }

    [TestMethod]
    public async Task TargetSize_AnUnlabelledCheckbox_IsAtLeast24PixelsSquare()
    {
        await OpenAsync(Url);

        var root = await BoxOfAsync(Root("cb-arialabel"));
        var label = await BoxOfAsync(Label("cb-arialabel"));

        Assert.IsTrue(root.Width >= 24, $"The click target is {root.Width}px wide.");
        Assert.IsTrue(label.Height >= 24, $"The click target is {label.Height}px high.");
    }

    [TestMethod]
    public async Task AllowDisabledFocus_KeepsADisabledCheckboxReachableButInert()
    {
        await OpenAsync(Url);

        await Expect(Input("cb-dis-skipped")).ToBeDisabledAsync();
        await Expect(Input("cb-dis-focus")).Not.ToHaveAttributeAsync("disabled", new Regex(".*"));
        await Expect(Input("cb-dis-focus")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Root("cb-dis-focus")).ToHaveClassAsync(HasClass("bit-dis"));

        await Page.Locator("#btn-before-disabled-focus").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("cb-dis-focus")).ToBeFocusedAsync();
        await Expect(Box("cb-dis-focus")).ToHaveCSSAsync("box-shadow", NotNone);

        await Page.Keyboard.PressAsync("Space");
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Input("cb-dis-focus")).Not.ToBeCheckedAsync();
        await Expect(Span("dis-focus-changes")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Loading_ShowsASpinnerTurnsAwayClicksAndAnnouncesBusy()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-loading")).ToHaveClassAsync(HasClass("bit-chb-ldg"));
        await Expect(Page.Locator("#cb-loading .bit-chb-spn")).ToBeVisibleAsync();
        await Expect(Icon("cb-loading")).ToHaveCountAsync(0);
        await Expect(Input("cb-loading")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Input("cb-loading")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Input("cb-loading")).Not.ToHaveAttributeAsync("disabled", new Regex(".*"));
        await Expect(Page.Locator("#cb-loading .bit-chb-spn")).ToHaveCSSAsync("animation-name", "bit-spin");

        // Playwright counts a control announced as unavailable as not enabled, so the click is forced through.
        await Text("cb-loading").ClickAsync(new() { Force = true });
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Span("loading-changes")).ToHaveTextAsync("0");
        await Expect(Input("cb-loading")).Not.ToBeCheckedAsync();

        // The busy box keeps the footprint it had and the state it is in.
        await Expect(Input("cb-loading-checked")).ToBeCheckedAsync();
        await Expect(Page.Locator("#cb-loading-checked .bit-chb-spn")).ToBeVisibleAsync();
        await Expect(Root("cb-loading-ind")).ToHaveClassAsync(HasClass("bit-chb-ind"));
        var busy = await BoxOfAsync(Box("cb-loading"));
        var idle = await BoxOfAsync(Box("cb-basic"));
        Assert.AreEqual(idle.Width, busy.Width, 0.5);
    }

    [TestMethod]
    public async Task AutoLoading_IsBusyWhileTheChangeRunsAndDropsASecondClick()
    {
        await OpenAsync(Url);

        await Text("cb-autoloading").ClickAsync();

        await Expect(Span("auto-changing")).ToHaveTextAsync("1");
        await Expect(Root("cb-autoloading")).ToHaveClassAsync(HasClass("bit-chb-ldg"));
        await Expect(Page.Locator("#cb-autoloading .bit-chb-spn")).ToBeVisibleAsync();
        await Expect(Input("cb-autoloading")).ToHaveAttributeAsync("aria-busy", "true");

        await Text("cb-autoloading").ClickAsync(new() { Force = true });
        await Text("cb-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-release").ClickAsync();

        await Expect(Span("auto-saved")).ToHaveTextAsync("1");
        await Expect(Span("auto-value")).ToHaveTextAsync("True");
        await Expect(Root("cb-autoloading")).Not.ToHaveClassAsync(HasClass("bit-chb-ldg"));
        await Expect(Input("cb-autoloading")).ToBeCheckedAsync();
        await Expect(Span("auto-changing")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Cascading_BitParams_FillsWhatTheCheckboxLeavesUnset()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-cascade-1")).ToHaveClassAsync(HasClass("bit-chb-fwi"));
        await Expect(Root("cb-cascade-1")).ToHaveClassAsync(HasClass("bit-chb-rvs"));
        await Expect(Icon("cb-cascade-1")).ToHaveClassAsync(HasClass("bit-icon--Heart"));
        Assert.AreEqual(400, (await BoxOfAsync(Root("cb-cascade-1"))).Width, 1);

        await Expect(Root("cb-cascade-own")).Not.ToHaveClassAsync(HasClass("bit-chb-rvs"));
        await Expect(Root("cb-cascade-own")).ToHaveClassAsync(HasClass("bit-chb-fwi"));
        await Expect(Icon("cb-cascade-own")).ToHaveClassAsync(HasClass("bit-icon--Heart"));

        await Expect(Root("cb-cascade-outside")).Not.ToHaveClassAsync(HasClass("bit-chb-fwi"));
        await Expect(Icon("cb-cascade-outside")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
    }

    [TestMethod]
    public async Task Color_EachValue_FillsTheCheckedBoxTheMixedSquareAndTheDisabledBox()
    {
        await OpenAsync(Url);

        foreach (var (suffix, token) in Colors)
        {
            var id = $"cb-color-{suffix}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-chb-{suffix}"));

            var fill = await ResolveColorAsync($"var(--bit-clr-{token})");
            await Expect(Box(id)).ToHaveCSSAsync("background-color", fill);
            await Expect(Box(id)).ToHaveCSSAsync("border-color", fill);
            await Expect(Icon($"{id}-ind")).ToHaveCSSAsync("background-color", fill);
            await Expect(Box($"{id}-ind")).ToHaveCSSAsync("border-color", fill);

            var disabled = await ResolveColorAsync($"var(--bit-clr-{token}-dis)");
            await Expect(Box($"{id}-dis")).ToHaveCSSAsync("background-color", disabled);
        }

        await Expect(Root("cb-color-default")).Not.ToHaveClassAsync(HasClass("bit-chb-pri"));
        await Expect(Box("cb-color-default")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));

        // The fills differ from one role to the next (the primary against the error, the success against the warning).
        Assert.AreNotEqual(await StyleOfAsync(Box("cb-color-pri"), "background-color"), await StyleOfAsync(Box("cb-color-err"), "background-color"));
        Assert.AreNotEqual(await StyleOfAsync(Box("cb-color-suc"), "background-color"), await StyleOfAsync(Box("cb-color-wrn"), "background-color"));
    }

    [TestMethod]
    public async Task Color_AnExplicitColor_WinsOverTheCheckedBackgroundVariable()
    {
        await OpenAsync(Url);

        await Expect(Box("cb-color-wins")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task ExternalIcons_TheBitIconInfoOfEachState_RendersItsClasses()
    {
        await OpenAsync(Url);

        await Expect(Icon("cb-ext-string")).ToHaveClassAsync(HasClass("fa-house"));
        await Expect(Icon("cb-ext-string")).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(Icon("cb-ext-string")).Not.ToHaveClassAsync(HasClass("bit-icon"));
        await Expect(Icon("cb-ext-css")).ToHaveClassAsync(HasClass("fa-heart"));
        await Expect(Icon("cb-ext-fa")).ToHaveClassAsync(HasClass("fa-rocket"));
        await Expect(Icon("cb-ext-fa")).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(Icon("cb-ext-ind")).ToHaveClassAsync(HasClass("fa-minus"));
        await Expect(Root("cb-ext-ind")).ToHaveClassAsync(HasClass("bit-chb-cii"));
        await Expect(Icon("cb-ext-bi")).ToHaveClassAsync(HasClass("bi-gear-fill"));
        await Expect(Icon("cb-ext-bi")).ToHaveClassAsync(HasClass("bi"));
        await Expect(Icon("cb-ext-unchecked")).ToHaveClassAsync(HasClass("bi-app"));
        await Expect(Root("cb-ext-unchecked")).ToHaveClassAsync(HasClass("bit-chb-uci"));
        await Expect(Icon("cb-ext-wins")).ToHaveClassAsync(HasClass("bi-star"));
        await Expect(Icon("cb-ext-wins")).Not.ToHaveClassAsync(HasClass("bit-icon--Heart"));
    }

    [TestMethod]
    public async Task Size_EachValue_ScalesTheBoxAndTheLabel()
    {
        await OpenAsync(Url);

        foreach (var suffix in new[] { "sm", "md", "lg" })
        {
            await Expect(Root($"cb-size-{suffix}")).ToHaveClassAsync(HasClass($"bit-chb-{suffix}"));
        }

        var sm = await BoxOfAsync(Box("cb-size-sm"));
        var md = await BoxOfAsync(Box("cb-size-md"));
        var lg = await BoxOfAsync(Box("cb-size-lg"));
        var dflt = await BoxOfAsync(Box("cb-size-default"));

        Assert.IsTrue(sm.Width < md.Width && md.Width < lg.Width, $"Box sizes {sm.Width} < {md.Width} < {lg.Width}.");
        Assert.AreEqual(md.Width, dflt.Width, 0.5, "Medium is the default.");
        Assert.AreEqual(sm.Width, sm.Height, 0.5);

        var smFont = Px(await StyleOfAsync(Label("cb-size-sm"), "font-size"));
        var mdFont = Px(await StyleOfAsync(Label("cb-size-md"), "font-size"));
        var lgFont = Px(await StyleOfAsync(Label("cb-size-lg"), "font-size"));
        Assert.IsTrue(smFont < mdFont && mdFont < lgFont, $"Label sizes {smFont} < {mdFont} < {lgFont}.");

        var wins = await BoxOfAsync(Box("cb-size-wins"));
        Assert.AreEqual(lg.Width, wins.Width, 0.5, "An explicit Size wins over --bit-Checkbox-box-size.");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-style")).ToHaveCSSAsync("color", "rgb(30, 144, 255)");
        await Expect(Root("cb-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("cb-class")).ToHaveClassAsync(HasClass("bit-chb"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPartAndTheStateEntriesApplyOnlyInThatState()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-styles")).ToHaveCSSAsync("padding-top", "3px");
        await Expect(Page.Locator("#cb-styles .bit-chb-txt")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(Box("cb-styles")).ToHaveCSSAsync("border-top-left-radius", "50%");
        await Expect(Icon("cb-styles")).ToHaveCSSAsync("color", "rgb(255, 255, 0)");
        await Expect(Label("cb-styles")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Page.Locator("#cb-styles .bit-chb-des")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Root("cb-styles")).ToHaveCSSAsync("outline-style", "none");

        await Text("cb-styles").ClickAsync();

        await Expect(Root("cb-styles")).ToHaveCSSAsync("outline-color", "rgb(255, 20, 147)");
        await Expect(Root("cb-styles")).ToHaveCSSAsync("outline-style", "solid");

        await Expect(Root("cb-styles-ind")).ToHaveCSSAsync("outline-color", "rgb(0, 0, 255)");
        await Expect(Page.Locator("#cb-classes-loading .bit-chb-spn")).ToHaveCSSAsync("opacity", "0.5");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPartAndTheStateEntriesApplyOnlyInThatState()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-classes")).ToHaveClassAsync(HasClass("c-root"));
        await Expect(Label("cb-classes")).ToHaveClassAsync(HasClass("c-container"));
        await Expect(Box("cb-classes")).ToHaveClassAsync(HasClass("c-box"));
        await Expect(Icon("cb-classes")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(Text("cb-classes")).ToHaveClassAsync(HasClass("c-label"));
        await Expect(Page.Locator("#cb-classes .bit-chb-des")).ToHaveClassAsync(HasClass("c-desc"));
        await Expect(Root("cb-classes")).Not.ToHaveClassAsync(HasClass("c-checked"));

        await Text("cb-classes").ClickAsync();
        await Expect(Root("cb-classes")).ToHaveClassAsync(HasClass("c-checked"));

        await Text("cb-classes").ClickAsync();
        await Expect(Root("cb-classes")).Not.ToHaveClassAsync(HasClass("c-checked"));

        await Expect(Root("cb-classes-ind")).ToHaveClassAsync(HasClass("c-ind"));
        await Expect(Page.Locator("#cb-classes-loading .bit-chb-spn")).ToHaveClassAsync(HasClass("c-spinner"));
    }

    [TestMethod]
    public async Task CssVariables_OnTheInstance_RestyleTheBoxTheGlyphAndTheLabel()
    {
        await OpenAsync(Url);

        await Expect(Box("cb-var-own")).ToHaveCSSAsync("border-top-left-radius", "50%");
        await Expect(Box("cb-var-own")).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(Box("cb-var-own")).ToHaveCSSAsync("background-color", "rgb(102, 51, 153)");
        await Expect(Icon("cb-var-own")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");

        await Expect(Box("cb-var-size")).ToHaveCSSAsync("width", "28px");
        await Expect(Label("cb-var-size")).ToHaveCSSAsync("column-gap", "16px");
        await Expect(Label("cb-var-size")).ToHaveCSSAsync("font-size", "20px");
        await Expect(Label("cb-var-size")).ToHaveCSSAsync("font-weight", "700");
        Assert.AreEqual(14, (await BoxOfAsync(Icon("cb-var-ind"))).Width, 6, "The square follows the box.");

        await Expect(Icon("cb-var-ind")).ToHaveCSSAsync("background-color", "rgb(255, 140, 0)");
        await Expect(Box("cb-var-ind")).ToHaveCSSAsync("border-color", "rgb(255, 140, 0)");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_AreInheritedByEveryCheckboxInside()
    {
        await OpenAsync(Url);

        await Expect(Box("cb-var-anc-checked")).ToHaveCSSAsync("background-color", "rgb(0, 100, 0)");
        await Expect(Box("cb-var-anc-unchecked")).ToHaveCSSAsync("border-color", "rgb(0, 128, 0)");
        await Expect(Box("cb-var-anc-unchecked")).ToHaveCSSAsync("background-color", "rgb(240, 255, 240)");
        await Expect(Page.Locator("#cb-var-anc-unchecked .bit-chb-des")).ToHaveCSSAsync("color", "rgb(0, 128, 128)");
        await Expect(Root("cb-var-anc-unchecked")).ToHaveCSSAsync("color", "rgb(128, 0, 0)");

        var asterisk = await Text("cb-var-anc-unchecked").EvaluateAsync<string>("el => getComputedStyle(el, '::after').color");
        Assert.AreEqual("rgb(0, 0, 128)", asterisk);

        await Expect(Box("cb-var-disabled")).ToHaveCSSAsync("background-color", "rgb(200, 0, 0)");
        await Expect(Root("cb-var-disabled")).ToHaveCSSAsync("color", "rgb(0, 0, 200)");

        var inline = await BoxOfAsync(Label("cb-var-minheight"));
        var floor = await BoxOfAsync(Label("cb-ltr"));
        Assert.IsTrue(inline.Height < 24, $"--bit-Checkbox-min-height: 0 lets the target shrink ({inline.Height}px).");
        Assert.IsTrue(floor.Height >= 24, $"The default target is at least 24px ({floor.Height}px).");
    }

    [TestMethod]
    public async Task Rtl_ExplicitOrInherited_PutsTheBoxOnTheRight()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("cb-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "cb-rtl", "cb-rtl-inherited" })
        {
            var box = await BoxOfAsync(Box(id));
            var text = await BoxOfAsync(Text(id));
            Assert.IsTrue(box.X > text.X, $"{id}: the box is to the right of the label.");
        }

        await Expect(Root("cb-rtl-inherited")).Not.ToHaveAttributeAsync("dir", new Regex(".*"));

        var ltrBox = await BoxOfAsync(Box("cb-ltr"));
        var ltrText = await BoxOfAsync(Text("cb-ltr"));
        Assert.IsTrue(ltrBox.X < ltrText.X, "Left to right, the box is to the left of the label.");
    }

    [TestMethod]
    public async Task Base_IdVisibilityAndHtmlAttributes_AreAppliedToTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("cb-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Root("cb-hidden")).Not.ToBeVisibleAsync();
        await Expect(Root("cb-collapsed")).ToHaveCSSAsync("display", "none");

        await Expect(Root("cb-html-attrs")).ToHaveAttributeAsync("data-custom", "abc");
        await Expect(Root("cb-html-attrs")).ToHaveAttributeAsync("data-extra", "xyz");

        await Expect(Input("cb-input-attrs")).ToHaveAttributeAsync("data-input", "in");
        var descriptionId = await Page.Locator("#cb-input-attrs .bit-chb-des").GetAttributeAsync("id");
        await Expect(Input("cb-input-attrs")).ToHaveAttributeAsync("aria-describedby", $"basic {descriptionId}");
    }

    [TestMethod]
    public async Task Disabled_DropsThePageClickHandlersUntilEnabledAgain()
    {
        await OpenAsync(Url);

        await Text("cb-handler-disabled").ClickAsync(new() { Force = true });
        await Text("cb-handler-disabled").DispatchEventAsync("click");
        await Root("cb-handler-disabled").DispatchEventAsync("click");

        await Text("cb-handler-enabled").ClickAsync();
        await Expect(Span("enabled-root-clicks")).ToHaveTextAsync("2");
        await Expect(Span("disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Input("cb-handler-disabled")).Not.ToBeCheckedAsync();

        await Page.Locator("#btn-toggle-handler-disabled").ClickAsync();
        await Expect(Root("cb-handler-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await Text("cb-handler-disabled").ClickAsync();
        await Expect(Span("disabled-root-clicks")).ToHaveTextAsync("2");
        await Expect(Input("cb-handler-disabled")).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Runtime_ParameterChanges_ReRenderTheCheckbox()
    {
        await OpenAsync(Url);

        await Expect(Text("cb-runtime")).ToHaveTextAsync("Initial label");

        await Page.Locator("#btn-runtime-label").ClickAsync();
        await Expect(Text("cb-runtime")).ToHaveTextAsync("Changed label");

        await Page.Locator("#btn-runtime-color").ClickAsync();
        await Expect(Root("cb-runtime")).ToHaveClassAsync(HasClass("bit-chb-suc"));

        await Page.Locator("#btn-runtime-size").ClickAsync();
        await Expect(Root("cb-runtime")).ToHaveClassAsync(HasClass("bit-chb-lg"));

        await Page.Locator("#btn-runtime-value").ClickAsync();
        await Expect(Input("cb-runtime")).ToBeCheckedAsync();
        await Expect(Box("cb-runtime")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-suc)"));

        await Page.Locator("#btn-runtime-disable").ClickAsync();
        await Expect(Root("cb-runtime")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("cb-runtime")).ToBeDisabledAsync();
        await Expect(Input("cb-runtime")).ToHaveAttributeAsync("aria-disabled", "true");

        await Page.Locator("#btn-runtime-disable").ClickAsync();
        await Expect(Input("cb-runtime")).Not.ToBeDisabledAsync();
        await Expect(Input("cb-runtime")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await Text("cb-runtime").ClickAsync();
        await Expect(Span("runtime-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Unmount_RemovesTheCheckboxWithoutAnError()
    {
        await OpenAsync(Url);

        await Text("cb-runtime").ClickAsync();
        await Expect(Span("runtime-value")).ToHaveTextAsync("True");

        await Page.Locator("#btn-runtime-unmount").ClickAsync();
        await Expect(Root("cb-runtime")).ToHaveCountAsync(0);

        await Page.Locator("#btn-runtime-unmount").ClickAsync();
        await Expect(Input("cb-runtime")).ToBeCheckedAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheBoxTransitionUnlessForceAnimation()
    {
        await OpenAsync(Url);

        StringAssert.Contains(await StyleOfAsync(Box("cb-motion"), "transition-duration"), "0.1s");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(Box("cb-motion")).Not.ToHaveCSSAsync("transition-duration", new Regex(@"0\.1s"));
        await Expect(Root("cb-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        StringAssert.Contains(await StyleOfAsync(Box("cb-motion-forced"), "transition-duration"), "0.1s");
    }

    [TestMethod]
    public async Task Hover_PreviewsTheCheckMarkOnAnUncheckedBox()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Icon("cb-ltr")).ToHaveCSSAsync("opacity", "0");

        await Text("cb-ltr").HoverAsync();
        await Expect(Icon("cb-ltr")).ToHaveCSSAsync("opacity", "1");

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Icon("cb-ltr")).ToHaveCSSAsync("opacity", "0");
    }
}
