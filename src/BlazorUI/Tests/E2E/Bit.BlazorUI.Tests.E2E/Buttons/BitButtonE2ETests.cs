using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Buttons;

/// <summary>
/// The end-to-end tests of BitButton against /e2e/button, /e2e/button/float and /e2e/button/autofocus: the content
/// (primary and secondary text, templates, icons), every Variant, Color and Size with the colors and lengths the real
/// stylesheet resolves, Rounded, IconOnly and its accessible name, the loading states (IsLoading, AutoLoading,
/// Reclickable, LoadingDelay, the live region and the label placements), links (Href, Target, Rel, Download), the form
/// types inside an EditForm and through FormId, OnClick and StopPropagation, FullWidth and NoWrap, FixedColor, the
/// floating and draggable button (every FloatPosition, FloatOffset, FloatAbsolute, pointer drag, arrow keys, clamping,
/// reset on anchor change), the accessibility attributes, FocusAsync and AutoFocus, the disabled contract, the
/// BitParams cascade, Style/Class/Styles/Classes/HtmlAttributes, the public CSS variables and their variant twins,
/// RTL, Visibility, reduced motion, forced colors, prerendering, re-rendering and unmounting.
/// </summary>
[TestClass]
public class BitButtonE2ETests : E2ETestBase
{
    private const string Transparent = "rgba(0, 0, 0, 0)";

    public static IEnumerable<object[]> ColorRows => ButtonsE2EHelpers.ColorRows;

    private ILocator Btn(string id) => Page.Locator($"#{id}");

    private Task<string> Token(string token) => Page.ResolveColorAsync($"var(--bit-clr-{token})");

    // ---------- content ----------

    [TestMethod]
    public async Task Basic_ClickingTheButton_RaisesOnClickAndShowsItsLabel()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("basic-btn");

        await Expect(button).ToHaveTextAsync("Button");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn"));
        Assert.AreEqual("BUTTON", await button.EvaluateAsync<string>("el => el.tagName"));

        await button.ClickAsync();
        await Expect(Btn("basic-clicks")).ToHaveTextAsync("1");

        await button.ClickAsync();
        await button.ClickAsync();
        await Expect(Btn("basic-clicks")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task SecondaryText_GivenBesideTheLabel_RendersACompoundButtonWithASmallerSecondLine()
    {
        await OpenAsync("/e2e/button");

        var compound = Btn("basic-compound");

        await Expect(compound).ToHaveClassAsync(HasClass("bit-btn-hsc"));
        await Expect(compound.Locator(".bit-btn-prt")).ToHaveTextAsync("Primary text");
        await Expect(compound.Locator(".bit-btn-sct")).ToHaveTextAsync("this is the secondary text");
        await Expect(compound).ToHaveCSSAsync("align-items", "flex-start");

        var primarySize = await compound.Locator(".bit-btn-prt").PxAsync("font-size");
        var secondarySize = await compound.Locator(".bit-btn-sct").PxAsync("font-size");
        Assert.IsTrue(secondarySize < primarySize, $"The secondary line ({secondarySize}px) should be smaller than the primary one ({primarySize}px).");

        var primaryBox = await compound.Locator(".bit-btn-prt").BoxAsync();
        var secondaryBox = await compound.Locator(".bit-btn-sct").BoxAsync();
        Assert.IsTrue(secondaryBox.Y >= primaryBox.Y + primaryBox.Height - 1, "The secondary line sits under the primary one.");

        var secondaryOnly = Btn("basic-secondary-only");
        await Expect(secondaryOnly.Locator(".bit-btn-sct")).ToHaveTextAsync("secondary text only");
        await Expect(secondaryOnly.Locator(".bit-btn-prt")).ToHaveCountAsync(0);

        var empty = Btn("basic-empty");
        await Expect(empty).ToHaveClassAsync(HasClass("bit-btn-ntx"));
        await Expect(empty.Locator(".bit-btn-tcn")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Templates_PrimarySecondaryAndChildContent_RenderTheirMarkupInTheirSections()
    {
        await OpenAsync("/e2e/button");

        var child = Btn("tpl-child");
        await Expect(child.Locator(".bit-btn-prt .tpl-glyph")).ToHaveTextAsync("*");
        await Expect(child.Locator(".bit-btn-prt .tpl-text")).ToHaveTextAsync("A primary template");

        var both = Btn("tpl-both");
        await Expect(both).ToHaveClassAsync(HasClass("bit-btn-hsc"));
        await Expect(both.Locator(".bit-btn-prt .tpl-primary")).ToHaveTextAsync("Primary text");
        await Expect(both.Locator(".bit-btn-sct .tpl-secondary")).ToHaveTextAsync("A secondary template");
    }

    [TestMethod]
    public async Task LoadingTemplate_WhileLoading_ReplacesTheSpinnerAndKeepsTheNameAndTheAnnouncement()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("tpl-loading");
        await Expect(button.Locator(".tpl-custom-loading")).ToHaveCountAsync(0);
        await Expect(button.Locator("~ .bit-btn-sts")).ToHaveTextAsync("");

        await Btn("btn-toggle-tpl-loading").ClickAsync();

        await Expect(button.Locator(".bit-btn-ldg .tpl-custom-loading")).ToBeVisibleAsync();
        await Expect(button.Locator(".bit-btn-ldg .tpl-custom-loading")).ToHaveTextAsync("Wait...");
        await Expect(button.Locator(".bit-btn-ldg")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(button.Locator(".bit-btn-spn")).ToHaveCountAsync(0);
        await Expect(button.Locator(".bit-btn-lbl")).ToHaveCountAsync(0);
        await Expect(button).ToHaveAccessibleNameAsync("Click me");
        await Expect(Page.Locator("#tpl-loading + .bit-btn-sts")).ToHaveTextAsync("Announced");
        await Expect(Page.Locator("#tpl-loading + .bit-btn-sts")).ToHaveAttributeAsync("role", "status");

        await Btn("btn-toggle-tpl-loading").ClickAsync();
        await Expect(button.Locator(".tpl-custom-loading")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#tpl-loading + .bit-btn-sts")).ToHaveTextAsync("");
    }

    // ---------- variant, color, size, shape ----------

    [TestMethod]
    [DataRow("variant-default", "bit-btn-fil", "Fill")]
    [DataRow("variant-fill", "bit-btn-fil", "Fill")]
    [DataRow("variant-outline", "bit-btn-otl", "Outline")]
    [DataRow("variant-text", "bit-btn-txt", "Text")]
    public async Task Variant_EachValue_PaintsItsOwnLook(string id, string cssClass, string look)
    {
        await OpenAsync("/e2e/button");

        var button = Btn(id);
        var primary = await Token("pri");
        var onPrimary = await Token("pri-text");

        await Expect(button).ToHaveClassAsync(HasClass(cssClass));

        switch (look)
        {
            case "Fill":
                await Expect(button).ToHaveCSSAsync("background-color", primary);
                await Expect(button).ToHaveCSSAsync("color", onPrimary);
                await Expect(button).ToHaveCSSAsync("border-top-color", primary);
                break;
            case "Outline":
                await Expect(button).ToHaveCSSAsync("background-color", Transparent);
                await Expect(button).ToHaveCSSAsync("color", primary);
                await Expect(button).ToHaveCSSAsync("border-top-color", primary);
                break;
            default:
                await Expect(button).ToHaveCSSAsync("background-color", Transparent);
                await Expect(button).ToHaveCSSAsync("color", primary);
                await Expect(button).ToHaveCSSAsync("border-top-color", Transparent);
                break;
        }
    }

    [TestMethod]
    [DataRow("variant-fill", "Fill")]
    [DataRow("variant-outline", "Outline")]
    [DataRow("variant-text", "Text")]
    public async Task Variant_HoveringEachLook_FillsItWithTheHoverColor(string id, string look)
    {
        await OpenAsync("/e2e/button");

        var button = Btn(id);
        var hover = await Token("pri-hover");
        var onPrimary = await Token("pri-text");

        await button.HoverAsync();

        await Expect(button).ToHaveCSSAsync("background-color", hover);
        await Expect(button).ToHaveCSSAsync("color", onPrimary);

        if (look == "Text")
        {
            await Expect(button).ToHaveCSSAsync("border-top-color", Transparent);
        }
    }

    [TestMethod]
    [DataRow("variant-dis-fill", "Fill")]
    [DataRow("variant-dis-outline", "Outline")]
    [DataRow("variant-dis-text", "Text")]
    public async Task Variant_DisabledEachLook_MutesItAndIgnoresTheHover(string id, string look)
    {
        await OpenAsync("/e2e/button");

        var button = Btn(id);
        var disabled = await Token("pri-dis");
        var disabledText = await Token("pri-dis-text");

        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToHaveCSSAsync("color", disabledText);
        await Expect(button).ToHaveCSSAsync("background-color", look == "Fill" ? disabled : Transparent);
        await Expect(button).ToHaveCSSAsync("border-top-color", look == "Text" ? Transparent : disabled);
        await Expect(button).ToHaveCSSAsync("cursor", "default");

        var before = await button.StyleAsync("background-color");
        await button.HoverAsync(new() { Force = true });
        await NextFramesAsync();
        await Expect(button).ToHaveCSSAsync("background-color", before);
    }

    [TestMethod]
    public async Task Rounded_SetOrNot_DrawsPillOrButtonCorners()
    {
        await OpenAsync("/e2e/button");

        var full = await Page.ResolvePxAsync("var(--bit-shp-radius-full)");
        var buttonRadius = await Page.ResolvePxAsync("var(--bit-shp-radius-button)");

        await Expect(Btn("rounded-on")).ToHaveClassAsync(HasClass("bit-btn-rnd"));
        Assert.AreEqual(full, await Btn("rounded-on").PxAsync("border-top-left-radius"));

        await Expect(Btn("rounded-off")).Not.ToHaveClassAsync(HasClass("bit-btn-rnd"));
        Assert.AreEqual(buttonRadius, await Btn("rounded-off").PxAsync("border-top-left-radius"));
        Assert.AreNotEqual(full, buttonRadius);
    }

    [TestMethod]
    [DynamicData(nameof(ColorRows))]
    public async Task Color_EachValue_PaintsEveryVariantFromItsRole(string name, string suffix, string token, string onToken)
    {
        await OpenAsync("/e2e/button");

        var main = await Token(token);
        var on = await Token(onToken);
        var dis = await Token($"{token}-dis");
        var disText = await Token($"{token}-dis-text");

        var fill = Btn($"color-fill-{name}");
        await Expect(fill).ToHaveClassAsync(HasClass($"bit-btn-{suffix}"));
        await Expect(fill).ToHaveCSSAsync("background-color", main);
        await Expect(fill).ToHaveCSSAsync("color", on);
        await Expect(fill).ToHaveCSSAsync("border-top-color", main);

        var outline = Btn($"color-outline-{name}");
        await Expect(outline).ToHaveClassAsync(HasClass($"bit-btn-{suffix}"));
        await Expect(outline).ToHaveCSSAsync("color", main);
        await Expect(outline).ToHaveCSSAsync("border-top-color", main);
        await Expect(outline).ToHaveCSSAsync("background-color", Transparent);

        var text = Btn($"color-text-{name}");
        await Expect(text).ToHaveClassAsync(HasClass($"bit-btn-{suffix}"));
        await Expect(text).ToHaveCSSAsync("color", main);
        await Expect(text).ToHaveCSSAsync("border-top-color", Transparent);

        var disabled = Btn($"color-dis-{name}");
        await Expect(disabled).ToHaveCSSAsync("background-color", dis);
        await Expect(disabled).ToHaveCSSAsync("color", disText);

        var hover = await Token($"{token}-hover");
        await fill.HoverAsync();
        await Expect(fill).ToHaveCSSAsync("background-color", hover);
    }

    [TestMethod]
    public async Task Color_Unset_IsThePrimaryRoleWithoutAClassOfItsOwn()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("variant-default");
        var classes = await button.GetAttributeAsync("class") ?? string.Empty;

        foreach (var color in ButtonsE2EHelpers.Colors)
        {
            Assert.IsFalse(Regex.IsMatch(classes, $@"(^|\s)bit-btn-{color.Suffix}(\s|$)"), $"An unset Color renders no bit-btn-{color.Suffix}.");
        }

        await Expect(button).ToHaveCSSAsync("background-color", await Token("pri"));
    }

    [TestMethod]
    [DataRow("Small", "sm")]
    [DataRow("Medium", "md")]
    [DataRow("Large", "lg")]
    public async Task Size_EachValue_SetsTheControlHeightTypeIconAndSpinnerOfItsClass(string size, string suffix)
    {
        await OpenAsync("/e2e/button");

        var button = Btn($"size-{size}");
        var height = await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight(suffix));
        var iconSize = await Page.ResolvePxAsync($"var(--bit-siz-icon-{suffix})");
        var fontToken = suffix switch { "sm" => "xs", "md" => "sm", _ => "md" };
        var fontSize = await Page.ResolvePxAsync($"var(--bit-tpg-fs-{fontToken})");

        await Expect(button).ToHaveClassAsync(HasClass($"bit-btn-{suffix}"));
        Assert.AreEqual(height, await button.PxAsync("min-height"), "min-height");
        Assert.IsTrue((await button.BoxAsync()).Height >= height - 0.5, "The box is at least the control height.");
        Assert.AreEqual(fontSize, await button.Locator(".bit-btn-prt").PxAsync("font-size"), "font-size");
        Assert.AreEqual(iconSize, await button.Locator(".bit-btn-icn").PxAsync("font-size"), "icon size");

        var iconOnly = Btn($"size-icon-{size}");
        var box = await iconOnly.BoxAsync();
        ButtonsE2EHelpers.AreClose(box.Height, box.Width, 0.5, "An icon-only button is square");
        Assert.IsTrue(box.Height >= height - 0.5, "The icon-only square is at least the control height.");

        var spinner = Btn($"size-loading-{size}").Locator(".bit-btn-spn");
        Assert.AreEqual(iconSize, await spinner.PxAsync("width"), "spinner size");
    }

    [TestMethod]
    public async Task Size_Unset_IsTheMediumButtonWithoutAClassOfItsOwn()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("size-default");

        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-md"));
        Assert.AreEqual(await Btn("size-Medium").PxAsync("min-height"), await button.PxAsync("min-height"));
        Assert.IsTrue(await Btn("size-Small").PxAsync("min-height") < await button.PxAsync("min-height"));
        Assert.IsTrue(await Btn("size-Large").PxAsync("min-height") > await button.PxAsync("min-height"));
    }

    // ---------- icons ----------

    [TestMethod]
    public async Task IconName_AtStartOrEnd_SitsBeforeOrAfterTheLabel()
    {
        await OpenAsync("/e2e/button");

        var start = Btn("icon-start");
        var startIcon = start.Locator("i.bit-btn-icn");
        await Expect(startIcon).ToHaveClassAsync(HasClass("bit-icon--Emoji"));
        await Expect(startIcon).ToHaveClassAsync(HasClass("bit-icon"));
        await Expect(startIcon).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(start).ToHaveCSSAsync("flex-direction", "row");
        Assert.IsTrue((await startIcon.BoxAsync()).X < (await start.Locator(".bit-btn-prt").BoxAsync()).X, "Start puts the icon before the label.");

        var end = Btn("icon-end");
        await Expect(end).ToHaveClassAsync(HasClass("bit-btn-eni"));
        await Expect(end).ToHaveCSSAsync("flex-direction", "row-reverse");
        Assert.IsTrue((await end.Locator(".bit-btn-icn").BoxAsync()).X > (await end.Locator(".bit-btn-prt").BoxAsync()).X, "End puts the icon after the label.");

        var compound = Btn("icon-compound");
        var marginTop = await compound.Locator(".bit-btn-icn").PxAsync("margin-top");
        Assert.IsTrue(marginTop > 0, "A compound button drops the icon onto the line of the primary text.");
    }

    [TestMethod]
    public async Task IconUrl_GivenAnImage_RendersItAtTheIconSizeOnEitherSide()
    {
        await OpenAsync("/e2e/button");

        var image = Btn("icon-url").Locator("img.bit-btn-icnu");
        var iconSize = await Page.ResolvePxAsync("var(--bit-siz-icon-md)");

        await Expect(image).ToHaveAttributeAsync("src", new Regex("^data:image/svg\\+xml"));
        await Expect(image).ToHaveAttributeAsync("alt", "");
        await Expect(image).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(image).ToBeVisibleAsync();
        Assert.AreEqual(iconSize, (await image.BoxAsync()).Width, 0.5);
        Assert.IsTrue(await image.EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0"), "The image is loaded.");

        var end = Btn("icon-url-end");
        Assert.IsTrue((await end.Locator(".bit-btn-icnu").BoxAsync()).X > (await end.Locator(".bit-btn-prt").BoxAsync()).X);
    }

    [TestMethod]
    public async Task IconOnly_WithALabel_DrawsASquareAndKeepsTheLabelAsTheAccessibleName()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("icon-only");

        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-ntx"));
        await Expect(button.Locator(".bit-btn-tcn")).ToHaveCountAsync(0);
        await Expect(button.Locator(".bit-btn-srl")).ToHaveTextAsync("Add");
        await Expect(button).ToHaveAccessibleNameAsync("Add");
        Assert.AreEqual("1px", await button.Locator(".bit-btn-srl").StyleAsync("width"), "The label is visually hidden.");

        var box = await button.BoxAsync();
        ButtonsE2EHelpers.AreClose(box.Width, box.Height, 0.5, "An icon-only button is square");

        var named = Btn("icon-only-aria");
        await Expect(named).ToHaveAccessibleNameAsync("Delete");
        await Expect(named.Locator(".bit-btn-srl")).ToHaveCountAsync(0);

        var labelledBy = Btn("icon-only-labelledby");
        await Expect(labelledBy).ToHaveAttributeAsync("aria-labelledby", "icon-only-name");
        await Expect(labelledBy).ToHaveAccessibleNameAsync("Edit the item");
        await Expect(labelledBy.Locator(".bit-btn-srl")).ToHaveCountAsync(0);

        var circle = Btn("icon-only-rounded");
        var circleBox = await circle.BoxAsync();
        ButtonsE2EHelpers.AreClose(circleBox.Width, circleBox.Height, 0.5, "A rounded icon-only button is square");
        Assert.IsTrue(await circle.PxAsync("border-top-left-radius") >= circleBox.Width / 2, "Rounded makes the square a circle.");
    }

    [TestMethod]
    [DataRow("icon-ext-string", "fa-solid fa-house")]
    [DataRow("icon-ext-css", "fa-solid fa-heart")]
    [DataRow("icon-ext-fa", "fa-brands fa-github")]
    [DataRow("icon-ext-bi", "bi bi-gear-fill")]
    [DataRow("icon-ext-wins", "my-ext-icon")]
    public async Task Icon_ExternalLibraryClasses_AreRenderedOnTheIconElement(string id, string classes)
    {
        await OpenAsync("/e2e/button");

        var icon = Btn(id).Locator("i.bit-btn-icn");

        foreach (var cssClass in classes.Split(' '))
        {
            await Expect(icon).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon"));
    }

    // ---------- loading ----------

    [TestMethod]
    public async Task IsLoading_TurnedOn_StacksTheSpinnerOverTheContentAndRefusesTheClick()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("loading-toggle");
        var status = Page.Locator("#loading-toggle + .bit-btn-sts");
        var before = await button.BoxAsync();
        var contentWidth = await button.Locator(".bit-btn-tcn").EvaluateAsync<double>("el => el.getBoundingClientRect().width");

        await button.ClickAsync();
        await Expect(Btn("loading-clicks")).ToHaveTextAsync("1");
        await Expect(status).ToHaveTextAsync("");
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);

        await Btn("btn-toggle-loading").ClickAsync();

        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-lda"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-lnc"));
        await Expect(button).ToHaveCSSAsync("cursor", "default");
        await Expect(button.Locator(".bit-btn-spn")).ToBeVisibleAsync();
        await Expect(button.Locator(".bit-btn-lbl")).ToHaveTextAsync("Loading...");
        await Expect(button.Locator(".bit-btn-hcn")).ToHaveCSSAsync("opacity", "0");
        await Expect(button).ToHaveAccessibleNameAsync("Click me");
        await Expect(status).ToHaveTextAsync("Loading...");

        var spinning = await button.Locator(".bit-btn-spn").StyleAsync("animation-name");
        Assert.AreEqual("bit-spin", spinning);

        // the content stays in the flow under the spinner, so the box never shrinks below it; this one's label is
        // wider than "Click me", so the box grows to the label
        var during = await button.BoxAsync();
        Assert.IsTrue(during.Width >= before.Width - 0.5, "The loading state never shrinks the box.");
        ButtonsE2EHelpers.AreClose(contentWidth, await button.Locator(".bit-btn-hcn").EvaluateAsync<double>("el => el.getBoundingClientRect().width"), 0.5, "The hidden content keeps its width");

        await button.ClickAsync(new() { Force = true });
        await Btn("btn-toggle-loading").ClickAsync();

        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
        await Expect(button.Locator(".bit-btn-spn")).ToHaveCountAsync(0);
        await Expect(status).ToHaveTextAsync("");
        await Expect(Btn("loading-clicks")).ToHaveTextAsync("1");

        await button.ClickAsync();
        await Expect(Btn("loading-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Reclickable_WhileLoading_StaysAnAvailableControlThatRaisesOnClick()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("loading-reclickable");

        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).Not.ToHaveAttributeAsync("aria-disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-lnc"));
        await Expect(button).ToHaveCSSAsync("cursor", "pointer");

        await button.ClickAsync();
        await button.ClickAsync();
        await Expect(Btn("reclickable-loading-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    [DataRow("End", "row")]
    [DataRow("Start", "row-reverse")]
    [DataRow("Top", "column-reverse")]
    [DataRow("Bottom", "column")]
    public async Task LoadingLabelPlacement_EachValue_PlacesTheLabelAroundTheSpinner(string placement, string direction)
    {
        await OpenAsync("/e2e/button");

        var button = Btn($"loading-placement-{placement}");
        var container = button.Locator(".bit-btn-ldg");

        await Expect(container).ToHaveCSSAsync("flex-direction", direction);
        await Expect(button.Locator(".bit-btn-lbl")).ToHaveTextAsync($"{placement}...");

        var spinner = await button.Locator(".bit-btn-spn").BoxAsync();
        var label = await button.Locator(".bit-btn-lbl").BoxAsync();

        switch (placement)
        {
            case "End": Assert.IsTrue(label.X >= spinner.X + spinner.Width - 0.5, "End puts the label after the spinner."); break;
            case "Start": Assert.IsTrue(label.X + label.Width <= spinner.X + 0.5, "Start puts the label before the spinner."); break;
            case "Top": Assert.IsTrue(label.Y + label.Height <= spinner.Y + 0.5, "Top puts the label above the spinner."); break;
            default: Assert.IsTrue(label.Y >= spinner.Y + spinner.Height - 0.5, "Bottom puts the label under the spinner."); break;
        }
    }

    [TestMethod]
    public async Task AutoLoading_WhileOnClickRuns_ShowsTheLoadingStateAndRefusesASecondClick()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("auto-loading-btn");
        var before = await button.BoxAsync();

        await button.ClickAsync();
        await Expect(Btn("auto-clicks")).ToHaveTextAsync("1");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-lda"));
        await Expect(button.Locator(".bit-btn-spn")).ToBeVisibleAsync();

        // The spinner is drawn at the icon size with its stroke outside it on a page without a border-box reset (the
        // ring leaves box-sizing to the page), which is a pixel taller than a line of text in this host; the width
        // holds exactly.
        var during = await button.BoxAsync();
        ButtonsE2EHelpers.AreClose(before.Width, during.Width, 0.5, "The spinner is stacked over the content: the width holds");
        ButtonsE2EHelpers.AreClose(before.Height, during.Height, 1.5, "The spinner is stacked over the content: the height holds");

        await button.ClickAsync(new() { Force = true });
        await Btn("btn-release-auto").ClickAsync();

        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-lda"));
        await Expect(Btn("auto-clicks")).ToHaveTextAsync("1");

        await button.ClickAsync();
        await Expect(Btn("auto-clicks")).ToHaveTextAsync("2");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Btn("btn-release-auto").ClickAsync();
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
    }

    [TestMethod]
    public async Task AutoLoading_WhenTheHandlerIsCanceled_LeavesTheLoadingStateWithoutAnError()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("auto-loading-btn");

        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");

        await Btn("btn-cancel-auto").ClickAsync();

        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
        await button.ClickAsync();
        await Expect(Btn("auto-clicks")).ToHaveTextAsync("2");
        await Btn("btn-release-auto").ClickAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task AutoLoadingReclickable_ReClickedWhileLoading_RaisesOnClickWithTheLoadingStateAndKeepsLoading()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("reclickable-btn");

        await button.ClickAsync();
        await Expect(Btn("reclick-args")).ToHaveTextAsync("False");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).Not.ToHaveAttributeAsync("aria-disabled", ButtonsE2EHelpers.AnyValue);

        await button.ClickAsync();
        await Expect(Btn("reclick-args")).ToHaveTextAsync("False,True");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");

        await Btn("btn-release-reclick").ClickAsync();
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("reclick-args")).ToHaveTextAsync("False,True");
    }

    [TestMethod]
    public async Task IsLoading_BoundTwoWay_FollowsTheAutoLoadingRun()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("bind-loading-btn");

        await Expect(Btn("bound-loading")).ToHaveTextAsync("False");

        await button.ClickAsync();
        await Expect(Btn("bound-loading")).ToHaveTextAsync("True");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");

        await Btn("btn-release-bound").ClickAsync();
        await Expect(Btn("bound-loading")).ToHaveTextAsync("False");
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
    }

    [TestMethod]
    public async Task LoadingDelay_ALongRun_ShowsTheSpinnerOnlyOnceTheDelayIsUp()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("delay-slow");

        await button.EvaluateAsync(
            """
            el => {
                window.__delay = {};
                const start = performance.now();
                new MutationObserver(() => {
                    if (el.getAttribute('aria-busy') === 'true' && window.__delay.busy === undefined) window.__delay.busy = performance.now() - start;
                    if (el.classList.contains('bit-btn-lda') && window.__delay.spinner === undefined) window.__delay.spinner = performance.now() - start;
                }).observe(el, { attributes: true });
            }
            """);

        await button.ClickAsync();

        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-lda"));
        await Expect(button.Locator(".bit-btn-spn")).ToBeVisibleAsync();

        var busy = await Page.EvaluateAsync<double>("() => window.__delay.busy");
        var spinner = await Page.EvaluateAsync<double>("() => window.__delay.spinner");
        Assert.IsTrue(spinner - busy >= 450, $"The spinner came {spinner - busy}ms after the busy state; the delay is 500ms.");

        await Btn("btn-release-delay").ClickAsync();
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-lda"));
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
    }

    [TestMethod]
    public async Task LoadingDelay_ARunEndingInsideTheDelay_NeverShowsTheSpinner()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("delay-fast");

        await button.EvaluateAsync(
            """
            el => {
                window.__fast = { busy: false, spinner: false };
                new MutationObserver(() => {
                    if (el.getAttribute('aria-busy') === 'true') window.__fast.busy = true;
                    if (el.classList.contains('bit-btn-lda') || el.querySelector('.bit-btn-spn')) window.__fast.spinner = true;
                }).observe(el, { attributes: true, childList: true, subtree: true });
            }
            """);

        await button.ClickAsync();
        await Expect(Btn("delay-fast-done")).ToHaveTextAsync("1");
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__fast.busy"), "The busy state starts at once.");
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => window.__fast.spinner"), "A run that ends inside the delay never shows a spinner.");
    }

    // ---------- links ----------

    [TestMethod]
    public async Task Href_WithATarget_RendersAnAnchorThatOpensANewTabWithNoOpener()
    {
        await OpenAsync("/e2e/button");

        var link = Btn("href-blank");

        Assert.AreEqual("A", await link.EvaluateAsync<string>("el => el.tagName"));
        await Expect(link).ToHaveAttributeAsync("href", "/");
        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAttributeAsync("rel", "noopener");
        await Expect(link).Not.ToHaveAttributeAsync("type", ButtonsE2EHelpers.AnyValue);
        await Expect(link).ToHaveClassAsync(HasClass("bit-btn-fil"));
        await Expect(link).ToHaveCSSAsync("background-color", await Token("pri"));

        var popup = await Page.RunAndWaitForPopupAsync(() => link.ClickAsync());
        await popup.WaitForLoadStateAsync();
        Assert.AreEqual($"{BaseUrl}/", popup.Url);
        Assert.IsTrue(await popup.EvaluateAsync<bool>("() => window.opener === null"), "noopener leaves the new tab no opener.");
        await popup.CloseAsync();
    }

    [TestMethod]
    [DataRow("href-nofollow", "nofollow noopener")]
    [DataRow("href-noreferrer", "nofollow noreferrer")]
    [DataRow("href-opener", "opener")]
    [DataRow("href-same-tab", "nofollow")]
    [DataRow("href-handwritten-rel", "external noopener")]
    public async Task Rel_EachCombination_RendersTheRelWithNoOpenerAddedOnlyWhereNeeded(string id, string rel)
    {
        await OpenAsync("/e2e/button");

        await Expect(Btn(id)).ToHaveAttributeAsync("rel", rel);
    }

    [TestMethod]
    public async Task Href_HashOnly_CarriesNoRelWhateverRelSays()
    {
        await OpenAsync("/e2e/button");

        var link = Btn("href-in-page");

        await Expect(link).ToHaveAttributeAsync("href", "#href-target");
        await Expect(link).Not.ToHaveAttributeAsync("rel", ButtonsE2EHelpers.AnyValue);
        await Expect(link).Not.ToHaveAttributeAsync("target", ButtonsE2EHelpers.AnyValue);
    }

    [TestMethod]
    public async Task Href_Clicked_RaisesOnClickAndNavigates()
    {
        await OpenAsync("/e2e/button");

        await Btn("href-hash-nav").ClickAsync();

        await Expect(Btn("href-clicks")).ToHaveTextAsync("1");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/button#href-target$"));
        await Expect(Btn("href-hash-nav")).ToBeVisibleAsync();

        await Btn("href-hash-nav").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Btn("href-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Download_OnALink_RendersTheDownloadAttributeAndDownloadsTheFile()
    {
        await OpenAsync("/e2e/button");

        var link = Btn("href-download");

        await Expect(link).ToHaveAttributeAsync("download", "icon.svg");

        var download = await Page.RunAndWaitForDownloadAsync(() => link.ClickAsync());
        Assert.AreEqual("icon.svg", download.SuggestedFilename);
    }

    [TestMethod]
    public async Task Href_DisabledOrLoading_DropsTheHrefAndKeepsTheLinkFocusableAndNamed()
    {
        await OpenAsync("/e2e/button");

        var url = Page.Url;

        var disabled = Btn("href-disabled");
        await Expect(disabled).Not.ToHaveAttributeAsync("href", ButtonsE2EHelpers.AnyValue);
        await Expect(disabled).ToHaveAttributeAsync("role", "link");
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled).ToHaveAttributeAsync("tabindex", "0");
        await Expect(disabled).ToHaveClassAsync(HasClass("bit-dis"));

        await disabled.FocusAsync();
        await Expect(disabled).ToBeFocusedAsync();
        await disabled.ClickAsync(new() { Force = true });
        Assert.AreEqual(url, Page.Url, "A disabled link does not navigate.");

        var loading = Btn("href-loading");
        await Expect(loading).Not.ToHaveAttributeAsync("href", ButtonsE2EHelpers.AnyValue);
        await Expect(loading).ToHaveAttributeAsync("role", "link");
        await Expect(loading).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(loading).ToHaveAttributeAsync("tabindex", "0");
        await loading.FocusAsync();
        await Expect(loading).ToBeFocusedAsync();
    }

    // ---------- forms ----------

    [TestMethod]
    public async Task ButtonType_InsideAndOutsideAForm_RendersTheNativeTypeItDefaultsToOrIsGiven()
    {
        await OpenAsync("/e2e/button");

        await Expect(Btn("basic-btn")).ToHaveAttributeAsync("type", "button");
        await Expect(Btn("form-default")).ToHaveAttributeAsync("type", "submit");
        await Expect(Btn("form-submit")).ToHaveAttributeAsync("type", "submit");
        await Expect(Btn("form-reset")).ToHaveAttributeAsync("type", "reset");
        await Expect(Btn("form-button")).ToHaveAttributeAsync("type", "button");
        await Expect(Btn("type-outside-reset")).ToHaveAttributeAsync("type", "reset");
    }

    [TestMethod]
    public async Task Submit_WithAnInvalidThenAValidModel_RunsTheInvalidThenTheValidSubmit()
    {
        await OpenAsync("/e2e/button");

        await Btn("form-submit").ClickAsync();

        await Expect(Btn("form-invalid")).ToHaveTextAsync("1");
        await Expect(Btn("form-valid")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#form .validation-message")).ToBeVisibleAsync();

        await Page.Locator("#form-required input").FillAsync("hello");
        await Expect(Btn("form-model")).ToHaveTextAsync("hello");

        await Btn("form-default").ClickAsync();
        await Expect(Btn("form-valid")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#form .validation-message")).ToHaveCountAsync(0);

        await Page.Locator("#form-required input").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Btn("form-valid")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Reset_InsideTheForm_ClearsTheInputAndTheOnClickResetsTheModel()
    {
        await OpenAsync("/e2e/button");

        var input = Page.Locator("#form-required input");

        await input.FillAsync("to be cleared");
        await Expect(Btn("form-model")).ToHaveTextAsync("to be cleared");

        await Btn("form-reset").ClickAsync();

        await Expect(input).ToHaveValueAsync("");
        await Expect(Btn("form-model")).ToHaveTextAsync("");
        await Expect(Btn("form-valid")).ToHaveTextAsync("0");
        await Expect(Btn("form-invalid")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task ButtonTypeButton_InsideTheForm_RaisesOnClickWithoutSubmitting()
    {
        await OpenAsync("/e2e/button");

        await Btn("form-button").ClickAsync();

        await Expect(Btn("form-button-clicks")).ToHaveTextAsync("1");
        await Expect(Btn("form-valid")).ToHaveTextAsync("0");
        await Expect(Btn("form-invalid")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Submit_LoadingOrDisabledButFocusable_DoesNotSubmitTheForm()
    {
        await OpenAsync("/e2e/button");

        await Btn("form-loading").ClickAsync(new() { Force = true });

        var disabled = Btn("form-disabled-focusable");
        await Expect(disabled).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);
        await disabled.ClickAsync(new() { Force = true });
        await disabled.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");

        // a round trip through the form's own button, after which every press above has been handled
        await Btn("form-button").ClickAsync();
        await Expect(Btn("form-button-clicks")).ToHaveTextAsync("1");

        await Expect(Btn("form-invalid")).ToHaveTextAsync("0");
        await Expect(Btn("form-valid")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task FormId_OnAButtonOutsideTheForm_SubmitsThatFormAndPassesNativeAttributesThrough()
    {
        await OpenAsync("/e2e/button");

        var submit = Btn("ext-submit");

        await Expect(submit).ToHaveAttributeAsync("form", "ext-form");
        await Expect(submit).ToHaveAttributeAsync("formmethod", "post");
        Assert.IsTrue(await submit.HasAttributeAsync("formnovalidate"));

        await submit.ClickAsync();
        await Expect(Page.Locator("#form .validation-message, #ext-form ~ * .validation-message, #ext-form .validation-message").First).ToBeVisibleAsync();
        await Expect(Btn("ext-submits")).ToHaveTextAsync("0");

        await Page.Locator("#ext-required input").FillAsync("external");
        await submit.ClickAsync();
        await Expect(Btn("ext-submits")).ToHaveTextAsync("1");
    }

    // ---------- events ----------

    [TestMethod]
    public async Task OnClick_ByPointerEnterAndSpace_IsRaisedOncePerPressWithTheLoadingState()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("evt-click");

        await button.ClickAsync();
        await Expect(Btn("evt-clicks")).ToHaveTextAsync("1");
        await Expect(Btn("evt-last-arg")).ToHaveTextAsync("False");

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Btn("evt-clicks")).ToHaveTextAsync("2");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Btn("evt-clicks")).ToHaveTextAsync("3");

        await button.DblClickAsync();
        await Expect(Btn("evt-clicks")).ToHaveTextAsync("5");
    }

    [TestMethod]
    public async Task StopPropagation_SetOrNot_KeepsTheClickFromTheParentOrLetsItBubble()
    {
        await OpenAsync("/e2e/button");

        await Btn("evt-stop").ClickAsync();
        await Expect(Btn("evt-child-clicks")).ToHaveTextAsync("1");

        await Btn("evt-bubble").ClickAsync();
        await Expect(Btn("evt-child-clicks")).ToHaveTextAsync("2");
        await Expect(Btn("evt-parent-clicks")).ToHaveTextAsync("1");
    }

    // ---------- layout ----------

    [TestMethod]
    public async Task FullWidth_InAContainer_StretchesTheButtonAcrossIt()
    {
        await OpenAsync("/e2e/button");

        var container = await Btn("fw-container").BoxAsync();
        var full = Btn("fw-btn");

        await Expect(full).ToHaveClassAsync(HasClass("bit-btn-flw"));
        ButtonsE2EHelpers.AreClose(container.Width, (await full.BoxAsync()).Width, 0.5, "A full-width button is as wide as its container");
        Assert.IsTrue((await Btn("fw-off").BoxAsync()).Width < container.Width - 50, "A plain button is sized by its content.");
    }

    [TestMethod]
    public async Task NoWrap_OnALongLabel_KeepsEachLineOnOneLineWithAnEllipsis()
    {
        await OpenAsync("/e2e/button");

        var noWrap = Btn("nowrap-btn");
        var wrap = Btn("wrap-btn");
        var primary = noWrap.Locator(".bit-btn-prt");

        await Expect(noWrap).ToHaveClassAsync(HasClass("bit-btn-nwr"));
        await Expect(primary).ToHaveCSSAsync("white-space", "nowrap");
        await Expect(primary).ToHaveCSSAsync("text-overflow", "ellipsis");
        await Expect(noWrap.Locator(".bit-btn-sct")).ToHaveCSSAsync("text-overflow", "ellipsis");
        Assert.IsTrue(await primary.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The label overflows into the ellipsis.");
        ButtonsE2EHelpers.AreClose(200, (await noWrap.BoxAsync()).Width, 0.5, "The button stays inside its container");

        Assert.IsTrue((await wrap.BoxAsync()).Height > (await noWrap.BoxAsync()).Height, "Without NoWrap the label wraps onto more lines.");
    }

    [TestMethod]
    public async Task FixedColor_OnOutlineAndText_PaintsTheOnColorAtRestAndHoldsItOnHover()
    {
        await OpenAsync("/e2e/button");

        var main = await Token("bg-ter");
        var on = await Token("fg-pri");
        var hover = await Token("bg-ter-hover");

        await Expect(Btn("fixed-outline")).ToHaveClassAsync(HasClass("bit-btn-fxc"));
        await Expect(Btn("fixed-outline")).ToHaveCSSAsync("color", on);
        await Expect(Btn("notfixed-outline")).ToHaveCSSAsync("color", main);
        await Expect(Btn("fixed-text")).ToHaveCSSAsync("color", on);
        await Expect(Btn("notfixed-text")).ToHaveCSSAsync("color", main);
        await Expect(Btn("fixed-fill")).ToHaveCSSAsync("color", on);

        foreach (var id in new[] { "fixed-outline", "fixed-text", "fixed-fill" })
        {
            await Btn(id).HoverAsync();
            await Expect(Btn(id)).ToHaveCSSAsync("background-color", hover);
            await Expect(Btn(id)).ToHaveCSSAsync("color", on);
        }

        await Btn("notfixed-outline").HoverAsync();
        await Expect(Btn("notfixed-outline")).ToHaveCSSAsync("color", on);
    }

    // ---------- accessibility ----------

    [TestMethod]
    public async Task Title_AriaLabelAndAriaDescription_NameAndDescribeTheButton()
    {
        await OpenAsync("/e2e/button");

        await Expect(Btn("a11y-title")).ToHaveAttributeAsync("title", "Save your changes");
        await Expect(Btn("a11y-title")).ToHaveAccessibleNameAsync("Hover me");

        await Expect(Btn("a11y-aria-label")).ToHaveAttributeAsync("aria-label", "Close the dialog");
        await Expect(Btn("a11y-aria-label")).ToHaveAccessibleNameAsync("Close the dialog");

        var described = Btn("a11y-desc");
        await Expect(described).ToHaveAttributeAsync("aria-describedby", "a11y-extra a11y-desc-dsc");
        await Expect(Btn("a11y-desc-dsc")).ToHaveTextAsync("SVG, 12 kilobytes");
        Assert.AreEqual("1px", await Btn("a11y-desc-dsc").StyleAsync("width"), "The description is visually hidden.");
        await Expect(described).ToHaveAccessibleDescriptionAsync("An author description SVG, 12 kilobytes");
        await Expect(described).ToHaveAccessibleNameAsync("Download");
    }

    [TestMethod]
    public async Task AriaHiddenAndTabIndex_TakeTheButtonOutOfTheTabOrder()
    {
        await OpenAsync("/e2e/button");

        var hidden = Btn("a11y-hidden");
        await Expect(hidden).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(hidden).ToHaveAttributeAsync("tabindex", "-1");

        await Expect(Btn("a11y-tabindex")).ToHaveAttributeAsync("tabindex", "-1");

        await Btn("a11y-desc").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Btn("btn-focus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusAsync_CalledFromCode_MovesTheFocusOntoTheButton()
    {
        await OpenAsync("/e2e/button");

        await Btn("btn-focus").ClickAsync();

        await Expect(Btn("a11y-focus-target")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_OnTheFirstRender_FocusesTheButtonButNeverAHiddenOrUnfocusableOne()
    {
        await OpenAsync("/e2e/button/autofocus");

        await Expect(Btn("autofocus-btn")).ToHaveAttributeAsync("autofocus", "");
        await Expect(Btn("autofocus-hidden")).Not.ToHaveAttributeAsync("autofocus", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("autofocus-disabled")).Not.ToHaveAttributeAsync("autofocus", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("autofocus-btn")).ToBeFocusedAsync();
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_ByDefault_StaysFocusableReportsAriaDisabledAndRunsNoHandler()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("dis-focusable");

        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(button).ToHaveAttributeAsync("title", "Pick a plan first");

        await Page.Locator("#dis-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(button).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await button.ClickAsync(new() { Force = true });

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#dis-after")).ToBeFocusedAsync();

        // the toggled button below is enabled and counts its clicks; once it has, every press above has been handled
        await Btn("btn-toggle-disabled").ClickAsync();
        await Btn("dis-toggle").ClickAsync();
        await Expect(Btn("dis-toggle-clicks")).ToHaveTextAsync("1");

        await Expect(Btn("dis-clicks")).ToHaveTextAsync("0");
        await Expect(Btn("dis-page-events")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Disabled_WithoutAllowDisabledFocus_RendersTheNativeAttributeAndLeavesTheTabOrder()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("dis-native");

        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("disabled", "");
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));

        await button.ClickAsync(new() { Force = true });
        await Btn("btn-toggle-disabled").ClickAsync();
        await Btn("dis-toggle").ClickAsync();
        await Expect(Btn("dis-toggle-clicks")).ToHaveTextAsync("1");
        await Expect(Btn("dis-clicks")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Disabled_ToggledAtRuntime_RestoresTheHandlersTheColorsAndTheState()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("dis-toggle");
        var disabledBackground = await Token("pri-dis");
        var background = await Token("pri");

        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToHaveCSSAsync("background-color", disabledBackground);
        await button.ClickAsync(new() { Force = true });

        await Btn("btn-toggle-disabled").ClickAsync();

        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).Not.ToHaveAttributeAsync("aria-disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(button).ToHaveCSSAsync("background-color", background);
        await Expect(Btn("dis-toggle-page-events")).ToHaveTextAsync("0");
        await Expect(Btn("dis-toggle-clicks")).ToHaveTextAsync("0");

        await button.ClickAsync();
        await Expect(Btn("dis-toggle-clicks")).ToHaveTextAsync("1");
        await Expect(Btn("dis-toggle-page-events")).ToHaveTextAsync("1");

        await Btn("btn-toggle-disabled").ClickAsync();
        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await button.ClickAsync(new() { Force = true });
        await Btn("btn-toggle-disabled").ClickAsync();
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Btn("dis-toggle-clicks")).ToHaveTextAsync("1");
        await Expect(Btn("dis-toggle-page-events")).ToHaveTextAsync("1");
    }

    // ---------- params, styles, variables ----------

    [TestMethod]
    public async Task BitParams_AroundButtons_FillsInWhatEachLeftUnsetAndNothingElse()
    {
        await OpenAsync("/e2e/button");

        var inherit = Btn("params-inherit");
        foreach (var cssClass in new[] { "bit-btn-sm", "bit-btn-rnd", "bit-btn-otl", "bit-btn-sec" })
        {
            await Expect(inherit).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(inherit).ToHaveCSSAsync("border-top-color", await Token("sec"));
        await Expect(inherit).ToHaveCSSAsync("background-color", Transparent);

        var own = Btn("params-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-btn-fil"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-btn-sm"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-btn-rnd"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-btn-sec"));
        await Expect(own).ToHaveCSSAsync("background-color", await Token("err"));

        var outside = Btn("params-outside");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-btn-sm"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-btn-rnd"));
        await Expect(outside).ToHaveClassAsync(HasClass("bit-btn-fil"));
    }

    [TestMethod]
    public async Task StyleAndClass_OnTheRoot_ReachTheButtonElement()
    {
        await OpenAsync("/e2e/button");

        await Expect(Btn("sc-style")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Btn("sc-class")).ToHaveClassAsync(HasClass("e2e-custom-class"));
        await Expect(Btn("sc-class")).ToHaveClassAsync(HasClass("bit-btn"));

        var attrs = Btn("sc-attrs");
        await Expect(attrs).ToHaveAttributeAsync("data-e2e", "custom-attribute");
        await Expect(attrs).ToHaveAttributeAsync("data-from-dictionary", "yes");
    }

    [TestMethod]
    public async Task Styles_PerPart_ReachEveryPartOfTheButton()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("sc-styles");
        await Expect(button).ToHaveCSSAsync("outline-style", "dotted");
        await Expect(button.Locator(".bit-btn-icn")).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(button.Locator(".bit-btn-tcn")).ToHaveCSSAsync("padding-left", "7px");
        await Expect(button.Locator(".bit-btn-prt")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(button.Locator(".bit-btn-sct")).ToHaveCSSAsync("color", "rgb(0, 0, 255)");

        var loading = Btn("sc-styles-loading");
        await Expect(loading.Locator(".bit-btn-hcn")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(loading.Locator(".bit-btn-ldg")).ToHaveCSSAsync("padding-top", "5px");
        await Expect(loading.Locator(".bit-btn-spn")).ToHaveCSSAsync("border-top-color", "rgb(218, 165, 32)");
        await Expect(loading.Locator(".bit-btn-lbl")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
    }

    [TestMethod]
    public async Task Classes_PerPart_ReachEveryPartOfTheButton()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("sc-classes");
        await Expect(button).ToHaveClassAsync(HasClass("c-root"));
        await Expect(button.Locator(".bit-btn-icn")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(button.Locator(".bit-btn-tcn")).ToHaveClassAsync(HasClass("c-container"));
        await Expect(button.Locator(".bit-btn-prt")).ToHaveClassAsync(HasClass("c-primary"));
        await Expect(button.Locator(".bit-btn-sct")).ToHaveClassAsync(HasClass("c-secondary"));

        var loading = Btn("sc-classes-loading");
        await Expect(loading.Locator(".bit-btn-hcn")).ToHaveClassAsync(HasClass("c-hidden"));
        await Expect(loading.Locator(".bit-btn-ldg")).ToHaveClassAsync(HasClass("c-ldg"));
        await Expect(loading.Locator(".bit-btn-spn")).ToHaveClassAsync(HasClass("c-spinner"));
        await Expect(loading.Locator(".bit-btn-lbl")).ToHaveClassAsync(HasClass("c-label"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheDefaultsOfEveryButtonInside()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("vars-default");

        await Expect(button).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(button).ToHaveCSSAsync("border-top-color", "rgb(10, 20, 30)");
        await Expect(button).ToHaveCSSAsync("color", "rgb(250, 240, 230)");
        await Expect(button).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(button).ToHaveCSSAsync("min-height", "50px");
        await Expect(button).ToHaveCSSAsync("min-width", "222px");
        await Expect(button).ToHaveCSSAsync("padding-left", "30px");
        await Expect(button).ToHaveCSSAsync("padding-top", "3px");
        await Expect(button).ToHaveCSSAsync("column-gap", "13px");
        await Expect(button).ToHaveCSSAsync("border-top-width", "4px");
        await Expect(button).ToHaveCSSAsync("box-shadow", new Regex("rgb\\(255, 0, 0\\)"));
        await Expect(button).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(button).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(button).ToHaveCSSAsync("font-weight", "700");
        await Expect(button.Locator(".bit-btn-prt")).ToHaveCSSAsync("font-size", "20px");
        await Expect(button.Locator(".bit-btn-icn")).ToHaveCSSAsync("font-size", "25px");
        await Expect(button.Locator(".bit-btn-sct")).ToHaveCSSAsync("color", "rgb(1, 100, 1)");
        await Expect(button.Locator(".bit-btn-sct")).ToHaveCSSAsync("font-size", "9px");
    }

    [TestMethod]
    public async Task CssVariables_UnderAnExplicitColorSizeOrRounded_LoseToTheParameter()
    {
        await OpenAsync("/e2e/button");

        await Expect(Btn("vars-color")).ToHaveCSSAsync("background-color", await Token("err"));

        var small = await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight("sm"));
        Assert.AreEqual(small, await Btn("vars-size").PxAsync("min-height"));
        Assert.AreNotEqual(20, await Btn("vars-size").Locator(".bit-btn-prt").PxAsync("font-size"));

        var full = await Page.ResolvePxAsync("var(--bit-shp-radius-full)");
        Assert.AreEqual(full, await Btn("vars-rounded").PxAsync("border-top-left-radius"));
    }

    [TestMethod]
    public async Task CssVariables_VariantTwins_RestyleTheirOwnLookOnly()
    {
        await OpenAsync("/e2e/button");

        await Expect(Btn("vars-twin-outline")).ToHaveCSSAsync("border-top-color", "rgb(200, 0, 0)");
        await Expect(Btn("vars-twin-outline")).ToHaveCSSAsync("background-color", Transparent);
        await Expect(Btn("vars-twin-text")).ToHaveCSSAsync("color", "rgb(0, 0, 200)");
        await Expect(Btn("vars-twin-fill")).ToHaveCSSAsync("background-color", "rgb(0, 150, 0)");
        await Expect(Btn("vars-twin-fill")).Not.ToHaveCSSAsync("border-top-color", "rgb(200, 0, 0)");
        await Expect(Btn("vars-twin-fill")).Not.ToHaveCSSAsync("color", "rgb(0, 0, 200)");
        await Expect(Btn("vars-twin-outline")).Not.ToHaveCSSAsync("color", "rgb(0, 0, 200)");
    }

    [TestMethod]
    public async Task CssVariables_ForStates_RestyleTheHoverTheDisabledAndTheSpinner()
    {
        await OpenAsync("/e2e/button");

        await Expect(Btn("vars-disabled")).ToHaveCSSAsync("background-color", "rgb(77, 77, 77)");

        var spinner = Btn("vars-spinner").Locator(".bit-btn-spn");
        await Expect(spinner).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 255)");
        await Expect(spinner).ToHaveCSSAsync("width", "30px");

        var hover = Btn("vars-hover");
        await hover.HoverAsync();
        await Expect(hover).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        await Expect(hover).ToHaveCSSAsync("color", "rgb(255, 255, 0)");
    }

    // ---------- direction, visibility, motion ----------

    [TestMethod]
    public async Task Rtl_ExplicitOrInherited_MirrorsTheIconAndTheLoadingLabel()
    {
        await OpenAsync("/e2e/button");

        async Task<bool> IconAfterLabel(string id)
        {
            var button = Btn(id);
            return (await button.Locator(".bit-btn-icn").BoxAsync()).X > (await button.Locator(".bit-btn-prt").BoxAsync()).X;
        }

        Assert.IsFalse(await IconAfterLabel("rtl-ltr"), "LTR puts the icon on the left.");

        await Expect(Btn("rtl-explicit")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Btn("rtl-explicit")).ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue(await IconAfterLabel("rtl-explicit"), "An explicit RTL puts the icon on the right.");

        Assert.IsFalse(await IconAfterLabel("rtl-explicit-end"), "End in RTL puts the icon on the left.");

        await Expect(Btn("rtl-inherited")).Not.ToHaveAttributeAsync("dir", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue(await IconAfterLabel("rtl-inherited"), "An inherited RTL puts the icon on the right.");

        var loading = Btn("rtl-loading");
        var spinner = await loading.Locator(".bit-btn-spn").BoxAsync();
        var label = await loading.Locator(".bit-btn-lbl").BoxAsync();
        Assert.IsTrue(label.X + label.Width <= spinner.X + 0.5, "In RTL the loading label sits on the left of the spinner.");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheButtonKeepingOrDroppingItsBox()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("vis-btn");
        await Expect(button).ToBeVisibleAsync();

        await Btn("btn-vis-hidden").ClickAsync();
        await Expect(button).ToHaveCSSAsync("visibility", "hidden");
        await Expect(button).ToBeHiddenAsync();
        Assert.IsTrue((await button.EvaluateAsync<double>("el => el.getBoundingClientRect().width")) > 0, "Hidden keeps the box.");

        await Btn("btn-vis-collapsed").ClickAsync();
        await Expect(button).ToHaveCSSAsync("display", "none");
        Assert.AreEqual(0, await button.EvaluateAsync<double>("el => el.getBoundingClientRect().width"), "Collapsed drops the box.");

        await Btn("btn-vis-visible").ClickAsync();
        await Expect(button).ToBeVisibleAsync();
        await Expect(button).ToHaveCSSAsync("display", "inline-flex");
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_CollapsesTheTransitionsUnlessForceAnimationIsSet()
    {
        await OpenAsync("/e2e/button");

        var full = ButtonsE2EHelpers.ParseSeconds(await Btn("motion-default").StyleAsync("transition-duration"));
        Assert.IsTrue(full >= 0.05, $"The color transition runs at full length without the preference ({full}s).");
        var fullSpinner = ButtonsE2EHelpers.ParseSeconds(await Btn("motion-default").Locator(".bit-btn-spn").StyleAsync("animation-duration"));

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = ButtonsE2EHelpers.ParseSeconds(await Btn("motion-default").StyleAsync("transition-duration"));
        Assert.IsTrue(reduced < 0.001, $"Reduced motion collapses the transition ({reduced}s).");
        var reducedSpinner = ButtonsE2EHelpers.ParseSeconds(await Btn("motion-default").Locator(".bit-btn-spn").StyleAsync("animation-duration"));
        Assert.IsTrue(reducedSpinner > fullSpinner, $"Reduced motion slows the spinner down ({fullSpinner}s -> {reducedSpinner}s).");

        await Expect(Btn("motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        var forced = ButtonsE2EHelpers.ParseSeconds(await Btn("motion-forced").StyleAsync("transition-duration"));
        Assert.AreEqual(full, forced, 0.0001, "ForceAnimation restores the full transition.");
    }

    [TestMethod]
    public async Task ForcedColors_Active_PaintsADisabledButtonInGrayText()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync("/e2e/button");

        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var grayText = await Page.ResolveColorAsync("GrayText");
        await Expect(Btn("dis-focusable")).ToHaveCSSAsync("color", grayText);
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_ThePagesHtml_ContainsTheButtonsBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/button");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"basic-btn\"");
        StringAssert.Matches(html, new Regex(@"<button id=""basic-btn"" class=""[^""]*\bbit-btn\b"));
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
        StringAssert.Contains(html, "id=\"href-blank\"");
    }

    [TestMethod]
    public async Task Parameters_ChangedAtRuntime_ReRenderTheButton()
    {
        await OpenAsync("/e2e/button");

        var button = Btn("rerender-btn");
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-fil"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-md"));
        await Expect(button.Locator(".bit-btn-icn")).ToHaveCountAsync(0);

        await Btn("btn-rerender").ClickAsync();

        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-otl"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-lg"));
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-fil"));
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-md"));
        await Expect(button.Locator(".bit-btn-icn")).ToHaveClassAsync(HasClass("bit-icon--Emoji"));
        await Expect(button.Locator(".bit-btn-prt")).ToHaveTextAsync("After");
        await Expect(button.Locator(".bit-btn-sct")).ToHaveTextAsync("Now with a secondary line");
        await Expect(button).ToHaveCSSAsync("border-top-color", await Token("err"));
    }

    [TestMethod]
    public async Task Unmount_WhileAnAutoLoadingRunIsInFlight_LeavesNoErrorBehind()
    {
        await OpenAsync("/e2e/button");

        await Btn("mounted-btn").ClickAsync();
        await Expect(Btn("mounted-btn")).ToHaveAttributeAsync("aria-busy", "true");

        await Btn("btn-unmount").ClickAsync();
        await Expect(Btn("mounted-btn")).ToHaveCountAsync(0);

        await Expect(Btn("mounted-done")).ToHaveTextAsync("1");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();

        await Btn("btn-unmount").ClickAsync();
        await Expect(Btn("mounted-btn")).ToBeVisibleAsync();
        await Expect(Btn("mounted-btn")).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
    }
}
