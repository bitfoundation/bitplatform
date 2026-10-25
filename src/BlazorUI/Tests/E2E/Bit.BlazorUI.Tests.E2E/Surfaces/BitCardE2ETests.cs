using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives BitCard (/e2e/card) in a real browser: the padded box and its dimensions (Width, Height, the Min/Max bounds,
/// FullWidth / FullHeight / FullSize, ScrollableBody on a plain and on a sectioned card), the header (Title, Subtitle,
/// IconName, HeadingLevel, IconTemplate, HeaderTemplate), Actions / Footer / FloatingActions / Divider, the cover
/// (ImageUrl, ImageHeight, ImagePosition, ImageAlt, ImageLoading, CoverRatio, Reversed, Cover, CoverOverlay, Horizontal,
/// CoverWidth), the clickable card (the stretched button, pointer and keyboard, Actions and Footer kept apart, Disabled,
/// StopPropagation, FocusAsync, hover and press shading), the bound and ready-made Selected ring, the linked card
/// (Href, Target, Rel, Download, the name and description of the link, a disabled link), Loading with the skeleton and
/// its wave under reduced motion, Elevation / NoShadow / Hoverable, Outlined / Border / Background / Square / NoPadding,
/// the ARIA roles a card takes, the public CSS variables, the BitParams cascade, every Color with every Variant, external
/// icons, Size, Style / Class / Styles / Classes, RTL, the page's activation handlers dropped while disabled,
/// Visibility, HtmlAttributes, prerendering, re-rendering and unmounting.
/// </summary>
[TestClass]
public class BitCardE2ETests : E2ETestBase
{
    private const string Url = "/e2e/card";
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private ILocator El(string id) => Page.Locator($"#{id}");
    private ILocator Part(string id, string cssClass) => Page.Locator($"#{id} .{cssClass}").First;
    private ILocator Stretched(string id) => Page.Locator($"#{id} > .bit-crd-lnk");

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheCards()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"crd-basic\"");
        StringAssert.Contains(html, "class=\"bit-crd-lnk");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"crd-loading\"[^>]*aria-busy=\"true\""), "The loading card is not prerendered busy.");
        StringAssert.Contains(html, "bit-crd-skb");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_APaddedSurface_OnlyAsWideAsItsContent()
    {
        await OpenAsync(Url);

        var card = El("crd-basic");
        Assert.IsTrue(Px(await StyleAsync(card, "padding-top")) > 0);
        Assert.IsTrue(Px(await StyleAsync(card, "border-top-left-radius")) > 0);
        Assert.AreNotEqual("none", await StyleAsync(card, "box-shadow"));
        await Expect(card).Not.ToHaveClassAsync(HasClass("bit-crd-sct"));
        Assert.AreEqual(await StyleAsync(El("crd-bg-secondary"), "background-color"), await StyleAsync(card, "background-color"));

        var box = await BoxAsync(card);
        var content = await BoxAsync(El("crd-basic-content"));
        var section = await BoxAsync(El("basic"));
        Assert.IsTrue(box.Width < section.Width / 2, "The card is not as narrow as its content.");
        Near(box.Width, content.Width + 2 * Px(await StyleAsync(card, "padding-left")), "fit to the content");
    }

    [TestMethod]
    public async Task Dimensions_WidthHeightAndScrollableBody_BoundTheCardAndScrollTheRest()
    {
        await OpenAsync(Url);

        var box = await BoxAsync(El("crd-dim"));
        Near(224, box.Width, "Width 14rem");
        Near(128, box.Height, "Height 8rem");
        await Expect(El("crd-dim")).ToHaveCSSAsync("overflow-y", "auto");
        Assert.IsTrue(await El("crd-dim").EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"));

        await El("crd-dim").EvaluateAsync("el => el.scrollTop = 50");
        Assert.IsTrue(await El("crd-dim").EvaluateAsync<double>("el => el.scrollTop") > 0);
    }

    [TestMethod]
    public async Task Dimensions_MinAndMaxBounds_HoldTheCard()
    {
        await OpenAsync(Url);

        Near(160, (await BoxAsync(El("crd-min"))).Width, "MinWidth 10rem");
        Near(288, (await BoxAsync(El("crd-max"))).Width, "MaxWidth 18rem");
        Assert.IsTrue((await BoxAsync(El("crd-minh"))).Height >= 95.5, "MinHeight 6rem");
        Near(80, (await BoxAsync(El("crd-maxh"))).Height, "MaxHeight 5rem");
        Assert.IsTrue(await El("crd-maxh").EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"));
    }

    [TestMethod]
    public async Task ScrollableBody_OnASectionedCard_ScrollsOnlyTheBody()
    {
        await OpenAsync(Url);

        var card = El("crd-scb");
        var body = Part("crd-scb", "bit-crd-bdy");
        Near(192, (await BoxAsync(card)).Height, "Height 12rem");
        await Expect(body).ToHaveCSSAsync("overflow-y", "auto");
        Assert.IsTrue(await body.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"));

        var footer = await BoxAsync(El("crd-scb-footer"));
        var cardBox = await BoxAsync(card);
        Assert.IsTrue(footer.Y + footer.Height <= cardBox.Y + cardBox.Height + 0.5, "The footer is pushed out of the card.");

        var title = await BoxAsync(Part("crd-scb", "bit-crd-ttl"));
        await body.EvaluateAsync("el => el.scrollTop = 100");
        Near(title.Y, (await BoxAsync(Part("crd-scb", "bit-crd-ttl"))).Y, "the header moved");
        Assert.IsTrue(await body.EvaluateAsync<double>("el => el.scrollTop") > 0);
    }

    [TestMethod]
    public async Task FullSizeFullWidthFullHeight_StretchTheCardToItsContainer()
    {
        await OpenAsync(Url);

        var one = await BoxAsync(El("crd-fs-1"));
        var two = await BoxAsync(El("crd-fs-2"));
        var three = await BoxAsync(El("crd-fs-3"));
        Near(two.Height, one.Height, "one height for the row");
        Near(two.Height, three.Height, "one height for the row");
        Near(one.Width, three.Width, "the cells' width");
        Assert.IsTrue(one.Width > 200);

        Near(500, (await BoxAsync(El("crd-fw"))).Width, "FullWidth");
        Near(150, (await BoxAsync(El("crd-fh"))).Height, "FullHeight");
        await Expect(El("crd-fw")).ToHaveClassAsync(HasClass("bit-crd-fwi"));
        await Expect(El("crd-fh")).ToHaveClassAsync(HasClass("bit-crd-fhe"));
    }

    [TestMethod]
    public async Task Header_TitleAndSubtitle_AreTwoLinesOfTheHeader()
    {
        await OpenAsync(Url);

        await Expect(El("crd-header")).ToHaveClassAsync(HasClass("bit-crd-sct"));
        await Expect(El("crd-header-ttl")).ToHaveTextAsync("bit BlazorUI");
        await Expect(El("crd-header-sub")).ToHaveTextAsync("Native Blazor components");
        await Expect(El("crd-header-ttl")).ToHaveCSSAsync("font-weight", "600");
        await Expect(El("crd-header-ttl")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        Assert.AreNotEqual(await StyleAsync(El("crd-header-ttl"), "color"), await StyleAsync(El("crd-header-sub"), "color"));
        Assert.IsTrue((await BoxAsync(El("crd-header-sub"))).Y > (await BoxAsync(El("crd-header-ttl"))).Y);
        Assert.IsTrue((await BoxAsync(El("crd-header-body"))).Y > (await BoxAsync(El("crd-header-sub"))).Y);
    }

    [TestMethod]
    public async Task HeadingLevelAndIconName_MakeTheTitleAHeadingLedByAGlyph()
    {
        await OpenAsync(Url);

        await Expect(El("crd-icon-ttl")).ToHaveAttributeAsync("role", "heading");
        await Expect(El("crd-icon-ttl")).ToHaveAttributeAsync("aria-level", "3");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Deployment", Level = 3 })).ToHaveCountAsync(1);

        var icon = Page.Locator("#crd-icon i.bit-crd-hic");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Rocket"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.IsTrue((await BoxAsync(icon)).X < (await BoxAsync(El("crd-icon-ttl"))).X);

        // A level outside 1..6 is not a heading.
        await Expect(El("crd-badlevel-ttl")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
    }

    [TestMethod]
    public async Task IconTemplate_ReplacesTheGlyphOnly_AndHeaderTemplateTheWholeHeader()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#crd-icontpl .bit-crd-hic #crd-icontpl-avatar")).ToBeVisibleAsync();
        await Expect(El("crd-icontpl-ttl")).ToHaveTextAsync("Ada Lovelace");

        await Expect(Page.Locator("#crd-hdrtpl .bit-crd-hdr #crd-hdrtpl-content")).ToBeVisibleAsync();
        await Expect(Page.Locator("#crd-hdrtpl .bit-crd-ttl")).ToHaveCountAsync(0);
        await Expect(El("crd-hdrtpl-ttl")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ActionsAndFooter_SitBesideTheTitleAndUnderTheBody_AndWorkOnTheirOwn()
    {
        await OpenAsync(Url);

        var title = await BoxAsync(El("crd-actions-ttl"));
        var action = await BoxAsync(El("crd-act-btn"));
        var body = await BoxAsync(El("crd-actions-body"));
        var footer = await BoxAsync(El("crd-ftr-btn"));
        Assert.IsTrue(action.X > title.X + title.Width, "The actions are not at the end of the header.");
        Assert.IsTrue(footer.Y > body.Y + body.Height - 1, "The footer is not under the body.");

        await El("crd-act-btn").ClickAsync();
        await El("crd-ftr-btn").ClickAsync();
        await Expect(El("act-clicks")).ToHaveTextAsync("1");
        await Expect(El("ftr-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Divider_DrawsAHairlineBetweenTheParts()
    {
        await OpenAsync(Url);

        await Expect(El("crd-divider")).ToHaveClassAsync(HasClass("bit-crd-dvd"));
        var parts = Page.Locator("#crd-divider > .bit-crd-mai > *");
        await Expect(parts).ToHaveCountAsync(3);
        await Expect(parts.Nth(0)).ToHaveCSSAsync("border-top-width", "0px");
        await Expect(parts.Nth(1)).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(parts.Nth(2)).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(Page.Locator("#crd-header > .bit-crd-mai > *").Nth(1)).ToHaveCSSAsync("border-top-width", "0px");
    }

    [TestMethod]
    public async Task FloatingActions_FloatOverTheTopEndCorner()
    {
        await OpenAsync(Url);

        var card = await BoxAsync(El("crd-floating"));
        var fab = await BoxAsync(El("crd-fab"));
        Assert.IsTrue(fab.Y - card.Y < 20, "Not at the top of the card.");
        Assert.IsTrue(card.X + card.Width - (fab.X + fab.Width) < 20, "Not at the end of the card.");
        await Expect(Page.Locator("#crd-floating > .bit-crd-fac")).ToHaveCSSAsync("position", "absolute");

        await El("crd-fab").ClickAsync();
        await Expect(El("crd-starred")).ToHaveTextAsync("True");
        await Expect(El("crd-fab")).ToHaveTextAsync("Unstar");
    }

    [TestMethod]
    public async Task Cover_ImageUrlRunsEdgeToEdge_CroppedByImageHeightAndFramedByImagePosition()
    {
        await OpenAsync(Url);

        var card = El("crd-cover");
        var cover = Page.Locator("#crd-cover > .bit-crd-cvr");
        var img = Page.Locator("#crd-cover > .bit-crd-cvr > img.bit-crd-img");

        await Expect(img).ToHaveAttributeAsync("src", "samples/surfaces-card-cover.svg");
        await Expect(img).ToHaveAttributeAsync("alt", "A mountain at dusk");
        await PollAsync(() => img.EvaluateAsync<bool>("el => el.complete && el.naturalWidth > 0"), loaded => loaded, "the image never loaded");
        Near(144, (await BoxAsync(img)).Height, "ImageHeight 9rem");
        await Expect(img).ToHaveCSSAsync("object-position", "50% 0%");
        await Expect(img).ToHaveCSSAsync("object-fit", "cover");

        var cardBox = await BoxAsync(card);
        var coverBox = await BoxAsync(cover);
        Near(cardBox.Width, coverBox.Width, "full bleed");
        Near(cardBox.Y, coverBox.Y, "at the head of the card");
        Assert.IsTrue((await BoxAsync(El("crd-cover-ttl"))).Y > coverBox.Y + coverBox.Height - 1, "The header is not under the cover.");
    }

    [TestMethod]
    public async Task Cover_WithoutImageAlt_IsDecorative_AndImageLoadingAndCoverRatioReachIt()
    {
        await OpenAsync(Url);

        var img = Page.Locator("#crd-ratio img.bit-crd-img");
        await Expect(img).ToHaveAttributeAsync("alt", "");
        await Expect(img).ToHaveAttributeAsync("loading", "lazy");
        await Expect(El("crd-ratio")).ToHaveClassAsync(HasClass("bit-crd-cra"));

        var cover = await BoxAsync(Page.Locator("#crd-ratio > .bit-crd-cvr"));
        Near(cover.Width * 9 / 16, cover.Height, "16 / 9");
    }

    [TestMethod]
    public async Task Reversed_CoverTemplateAndCoverOnly_LayTheCoverOut()
    {
        await OpenAsync(Url);

        var reversedCover = await BoxAsync(Page.Locator("#crd-reversed > .bit-crd-cvr"));
        var reversedMain = await BoxAsync(Page.Locator("#crd-reversed > .bit-crd-mai"));
        Assert.IsTrue(reversedCover.Y > reversedMain.Y, "Reversed does not put the cover last.");

        await Expect(Page.Locator("#crd-cover-tpl > .bit-crd-cvr > #crd-cover-tpl-art")).ToBeVisibleAsync();
        await Expect(Page.Locator("#crd-cover-tpl img")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#crd-cover-only > .bit-crd-cvr")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#crd-cover-only > .bit-crd-mai")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Horizontal_PutsTheCoverBesideTheContent_CoverWidthSizesIt_ReversedAndRtlMoveIt()
    {
        await OpenAsync(Url);

        var card = await BoxAsync(El("crd-hrz"));
        var cover = await BoxAsync(Page.Locator("#crd-hrz > .bit-crd-cvr"));
        var main = await BoxAsync(Page.Locator("#crd-hrz > .bit-crd-mai"));
        Assert.IsTrue(cover.X < main.X, "The cover is not first.");
        Near(card.Width * 0.33, cover.Width, "a third of the card", 2);
        Near(cover.Y, main.Y, "on one row");

        Near(128, (await BoxAsync(Page.Locator("#crd-hrz-cw > .bit-crd-cvr"))).Width, "CoverWidth 8rem");

        var revCover = await BoxAsync(Page.Locator("#crd-hrz-rev > .bit-crd-cvr"));
        var revMain = await BoxAsync(Page.Locator("#crd-hrz-rev > .bit-crd-mai"));
        Assert.IsTrue(revCover.X > revMain.X, "Reversed does not put the cover at the end.");

        var rtlCover = await BoxAsync(Page.Locator("#crd-hrz-rtl > .bit-crd-cvr"));
        var rtlMain = await BoxAsync(Page.Locator("#crd-hrz-rtl > .bit-crd-mai"));
        Assert.IsTrue(rtlCover.X > rtlMain.X, "RTL does not mirror the horizontal card.");
    }

    [TestMethod]
    public async Task OnClick_AStretchedButtonCoversTheCard_AndAClickAnywhereOnItFires()
    {
        await OpenAsync(Url);

        var card = El("crd-click");
        var button = Stretched("crd-click");
        await Expect(card).ToHaveClassAsync(HasClass("bit-crd-int"));
        await Expect(card).ToHaveClassAsync(HasClass("bit-crd-btn"));
        await Expect(card).ToHaveCSSAsync("cursor", "pointer");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button).ToHaveAttributeAsync("aria-labelledby", "crd-click-ttl");
        await Expect(button).ToHaveAttributeAsync("aria-describedby", "crd-click-sub");
        await Expect(button).ToHaveCSSAsync("pointer-events", "none");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Clickable", Exact = true })).ToHaveAccessibleDescriptionAsync("Press me");

        var cardBox = await BoxAsync(card);
        var buttonBox = await BoxAsync(button);
        Near(cardBox.Width, buttonBox.Width, "covers the width");
        Near(cardBox.Height, buttonBox.Height, "covers the height");

        await El("crd-click-body").ClickAsync();
        await Expect(El("click-count")).ToHaveTextAsync("1");
        await Expect(El("click-button")).ToHaveTextAsync("0");

        // Off the content, at the end of the body row: the surface itself is the card too.
        var body = await BoxAsync(Page.Locator("#crd-click .bit-crd-bdy"));
        await Page.Mouse.ClickAsync((float)(cardBox.X + cardBox.Width - 6), (float)(body.Y + body.Height / 2));
        await Expect(El("click-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task OnClick_EnterAndSpaceOnTheStretchedButton_PressTheCard()
    {
        await OpenAsync(Url);

        await Stretched("crd-click").FocusAsync();
        await Expect(Stretched("crd-click")).ToHaveCSSAsync("outline-style", "solid");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(El("click-count")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync(" ");
        await Expect(El("click-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task OnClick_TheActionsAndTheFooter_DoNotClickTheCard()
    {
        await OpenAsync(Url);

        await El("crd-click-body").ClickAsync();
        await Expect(El("click-count")).ToHaveTextAsync("1");

        await El("crd-click-reset").ClickAsync();
        await Expect(El("click-count")).ToHaveTextAsync("0");

        await El("crd-click-footer").ClickAsync();
        await Expect(El("click-footer")).ToHaveTextAsync("1");
        await El("crd-click-footer").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(El("click-footer")).ToHaveTextAsync("2");
        await Expect(El("click-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Disabled_AClickableCardAnswersNothing_AndLeavesTheTabOrder()
    {
        await OpenAsync(Url);

        var card = El("crd-click-dis");
        await Expect(card).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(card).ToHaveCSSAsync("pointer-events", "none");
        await Expect(card).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(Stretched("crd-click-dis")).ToHaveAttributeAsync("disabled", "");
        await Expect(Stretched("crd-click-dis")).ToHaveAttributeAsync("aria-disabled", "true");

        var box = await BoxAsync(card);
        await Page.Mouse.ClickAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        await card.DispatchEventAsync("click");
        await NextFramesAsync();
        await Expect(El("disabled-clicks")).ToHaveTextAsync("0");
        Assert.AreNotEqual(await StyleAsync(El("crd-click"), "color"), await StyleAsync(card, "color"));
    }

    [TestMethod]
    public async Task Selected_Bound_TurnsTheCardIntoAToggleWithARing()
    {
        await OpenAsync(Url);

        var card = El("crd-sel");
        var button = Stretched("crd-sel");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(card).ToHaveCSSAsync("outline-style", "none");

        await El("crd-sel-ttl").ClickAsync();
        await Expect(El("sel-value")).ToHaveTextAsync("True");
        await Expect(card).ToHaveClassAsync(HasClass("bit-crd-sel"));
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(card).ToHaveCSSAsync("outline-style", "solid");
        Assert.IsTrue(Px(await StyleAsync(card, "outline-width")) >= 2);

        await El("crd-sel-ttl").ClickAsync();
        await Expect(El("sel-value")).ToHaveTextAsync("False");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");

        await El("btn-select").ClickAsync();
        await Expect(card).ToHaveClassAsync(HasClass("bit-crd-sel"));
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");

        await button.FocusAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(El("sel-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Selected_ReadyMade_DrawsTheRing()
    {
        await OpenAsync(Url);

        await Expect(El("crd-sel-fixed")).ToHaveClassAsync(HasClass("bit-crd-sel"));
        await Expect(El("crd-sel-fixed")).ToHaveCSSAsync("outline-style", "solid");
        await Expect(Stretched("crd-sel-fixed")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task StopPropagation_KeepsTheClickFromTheAncestors()
    {
        await OpenAsync(Url);

        await El("crd-stop-ttl").ClickAsync();
        await Expect(El("stop-clicks")).ToHaveTextAsync("1");
        await Expect(El("parent-clicks")).ToHaveTextAsync("0");

        await El("crd-nostop-ttl").ClickAsync();
        await Expect(El("stop-clicks")).ToHaveTextAsync("2");
        await Expect(El("parent-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task FocusAsync_FocusesTheStretchedControl_OrTheRoot()
    {
        await OpenAsync(Url);

        await El("btn-focus-card").ClickAsync();
        await Expect(Stretched("crd-click")).ToBeFocusedAsync();

        await El("btn-focus-plain").ClickAsync();
        await Expect(El("crd-focus-plain")).ToBeFocusedAsync();
        await Expect(El("crd-focus-plain")).ToHaveAttributeAsync("tabindex", "0");
    }

    [TestMethod]
    public async Task Hover_AClickableCardShadesAndLifts_AHoverableOneOnlyLifts_APlainOneNeither()
    {
        await OpenAsync(Url);

        var click = El("crd-click");
        var restBg = await StyleAsync(click, "background-color");
        var restShadow = await StyleAsync(click, "box-shadow");
        await El("crd-click-ttl").HoverAsync();
        await PollAsync(() => StyleAsync(click, "background-color"), bg => bg != restBg, "the hover does not shade the clickable card");
        await PollAsync(() => StyleAsync(click, "box-shadow"), s => s != restShadow, "the hover does not lift the clickable card");

        var hoverable = El("crd-hoverable");
        var hoverableBg = await StyleAsync(hoverable, "background-color");
        var hoverableShadow = await StyleAsync(hoverable, "box-shadow");
        await El("crd-hoverable-ttl").HoverAsync();
        await PollAsync(() => StyleAsync(hoverable, "box-shadow"), s => s != hoverableShadow, "Hoverable does not lift the card");
        Assert.AreEqual(hoverableBg, await StyleAsync(hoverable, "background-color"));

        var plain = El("crd-plain");
        var plainShadow = await StyleAsync(plain, "box-shadow");
        await El("crd-plain-ttl").HoverAsync();
        await SettleAsync(plain);
        Assert.AreEqual(plainShadow, await StyleAsync(plain, "box-shadow"));
    }

    [TestMethod]
    public async Task Press_AClickableCardShadesDeeperWhilePressed()
    {
        await OpenAsync(Url);

        var click = El("crd-click");
        await El("crd-click-ttl").HoverAsync();
        await PollAsync(() => click.EvaluateAsync<bool>("el => el.matches(':hover')"), hovered => hovered, "the card is not hovered");
        await SettleAsync(click);
        var hoverBg = await StyleAsync(click, "background-color");

        await Page.Mouse.DownAsync();
        await PollAsync(() => StyleAsync(click, "background-color"), bg => bg != hoverBg, "the press does not shade the card");
        await Page.Mouse.UpAsync();
        await Expect(El("click-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Link_AStretchedAnchorNamedByTheTitle_LeadsSomewhere_WhileTheActionsStayApart()
    {
        await OpenAsync(Url);

        var link = Stretched("crd-link");
        await Expect(link).ToHaveAttributeAsync("href", "e2e/card?navigated=1");
        await Expect(link).ToHaveAttributeAsync("aria-labelledby", "crd-link-ttl");
        await Expect(link).ToHaveAttributeAsync("aria-describedby", "crd-link-sub");
        await Expect(link).Not.ToHaveAttributeAsync("rel", new Regex(".*"));
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "bit BlazorUI", Exact = true })).ToHaveAccessibleDescriptionAsync("blazorui.bitplatform.dev");
        await Expect(El("crd-link")).ToHaveClassAsync(HasClass("bit-crd-int"));
        await Expect(El("crd-link")).Not.ToHaveClassAsync(HasClass("bit-crd-btn"));

        await El("crd-link-pin").ClickAsync();
        await Expect(El("pinned")).ToHaveTextAsync("True");
        Assert.IsFalse(Page.Url.Contains("navigated=1"), "The action navigated with the card.");

        // The anchor lies over the body, so the click on the body lands on it.
        var body = await BoxAsync(El("crd-link-body"));
        Assert.AreEqual("bit-crd-lnk", await Page.EvaluateAsync<string>("([x, y]) => document.elementFromPoint(x, y).className.trim()", new[] { body.X + 5, body.Y + body.Height / 2 }));
        await Page.Mouse.ClickAsync((float)(body.X + 5), (float)(body.Y + body.Height / 2));
        await Page.WaitForURLAsync(new Regex("navigated=1"));
    }

    [TestMethod]
    public async Task Link_TargetRelAndDownload_ReachTheAnchor()
    {
        await OpenAsync(Url);

        await Expect(Stretched("crd-link-blank")).ToHaveAttributeAsync("target", "_blank");
        await Expect(Stretched("crd-link-blank")).ToHaveAttributeAsync("rel", "noopener");
        await Expect(Stretched("crd-link-rel")).ToHaveAttributeAsync("rel", "nofollow noopener");
        await Expect(Stretched("crd-link-opener")).ToHaveAttributeAsync("rel", "opener");
        await Expect(Stretched("crd-link-dl")).ToHaveAttributeAsync("download", "cover.svg");

        var link = Stretched("crd-link-aria");
        await Expect(link).ToHaveAttributeAsync("aria-label", "Read the guide");
        await Expect(link).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Expect(El("crd-link-aria")).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(El("crd-link-aria")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
    }

    [TestMethod]
    public async Task Link_Disabled_TakesTheHrefOffAndKeepsTheLinkRole()
    {
        await OpenAsync(Url);

        var link = Stretched("crd-link-dis");
        await Expect(El("crd-link-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(link).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(link).ToHaveAttributeAsync("role", "link");
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(link).ToHaveAttributeAsync("tabindex", "-1");

        await El("crd-link-dis").DispatchEventAsync("click");
        await NextFramesAsync();
        Assert.IsFalse(Page.Url.Contains("navigated=1"));
    }

    [TestMethod]
    public async Task Loading_StandsASkeletonInForTheBody_AndReportsBusy()
    {
        await OpenAsync(Url);

        var card = El("crd-loading");
        await Expect(card).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(card).ToHaveClassAsync(HasClass("bit-crd-ldg"));
        await Expect(El("crd-loading-ttl")).ToBeVisibleAsync();
        await Expect(El("crd-loading-body")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#crd-loading .bit-crd-skl")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator("#crd-loading .bit-crd-skb")).ToHaveCountAsync(3);
        Assert.AreEqual("bit-crd-skb-anm", await PseudoStyleAsync(Page.Locator("#crd-loading .bit-crd-skb").First, "::after", "animation-name"));
        var bars = Page.Locator("#crd-loading .bit-crd-skb");
        Assert.IsTrue((await BoxAsync(bars.Nth(2))).Width < (await BoxAsync(bars.Nth(0))).Width, "The last bar is not shorter.");

        await Expect(El("crd-loading-custom")).ToBeVisibleAsync();
        await Expect(Page.Locator("#crd-loading-tpl .bit-crd-skb")).ToHaveCountAsync(0);

        await El("btn-loading").ClickAsync();

        await Expect(El("crd-loading-body")).ToBeVisibleAsync();
        await Expect(El("crd-loading-tpl-body")).ToBeVisibleAsync();
        await Expect(card).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(Page.Locator("#crd-loading .bit-crd-skb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Loading_ReducedMotion_StopsTheWave_UnlessForceAnimation()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Url);

        Assert.AreEqual("none", await PseudoStyleAsync(Page.Locator("#crd-loading .bit-crd-skb").First, "::after", "animation-name"));
        await Expect(El("crd-loading-force")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.AreEqual("bit-crd-skb-anm", await PseudoStyleAsync(Page.Locator("#crd-loading-force .bit-crd-skb").First, "::after", "animation-name"));
    }

    [TestMethod]
    public async Task Elevation_EveryLevel_HasAShadowOfItsOwn()
    {
        await OpenAsync(Url);

        var shadows = new List<string>();
        foreach (var level in new[] { 0, 1, 4, 8, 9, 16, 24 })
        {
            await El($"btn-elev-{level}").ClickAsync();
            await Expect(El("crd-elev")).ToHaveClassAsync(HasClass($"bit-crd-e{level}"));
            await SettleAsync(El("crd-elev"));
            var shadow = await StyleAsync(El("crd-elev"), "box-shadow");
            Assert.AreEqual(level == 0 ? "none" : await ShadowTokenAsync(level), shadow, $"Elevation {level}");
            shadows.Add(shadow);
        }

        // The theme's ramp climbs in steps, so 1 and 4 share one; every step is a shadow of its own.
        Assert.AreEqual(6, shadows.Distinct().Count(), string.Join(" | ", shadows));
    }

    [TestMethod]
    public async Task Elevation_LiftsAClickableCardOnHover_NoShadowFlattensIt()
    {
        await OpenAsync(Url);

        var card = El("crd-elev-hover");
        var rest = await StyleAsync(card, "box-shadow");
        await El("crd-elev-hover-ttl").HoverAsync();
        await PollAsync(() => StyleAsync(card, "box-shadow"), s => s != rest, "Elevation does not lift on hover");

        await Expect(El("crd-noshadow")).ToHaveClassAsync(HasClass("bit-crd-nsd"));
        await Expect(El("crd-noshadow")).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task OutlinedSquareAndNoPadding_ChangeTheBox()
    {
        await OpenAsync(Url);

        await Expect(El("crd-outlined")).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(El("crd-outlined")).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(El("crd-outlined")).ToHaveCSSAsync("box-shadow", "none");
        await Expect(El("crd-plain")).ToHaveCSSAsync("border-top-width", "0px");

        await Expect(El("crd-square")).ToHaveCSSAsync("border-top-left-radius", "0px");
        Assert.IsTrue(Px(await StyleAsync(El("crd-outlined"), "border-top-left-radius")) > 0);

        await Expect(El("crd-nopad")).ToHaveCSSAsync("padding-top", "0px");
        var card = await BoxAsync(El("crd-nopad"));
        var content = await BoxAsync(El("crd-nopad-content"));
        Near(card.X + 1, content.X, "the content touches the edge");
    }

    [TestMethod]
    public async Task Border_EveryColorKind_DrawsItsOwnOutline()
    {
        await OpenAsync(Url);

        var expected = new (string Name, string Class)[] { ("primary", "bit-crd-bpr"), ("secondary", "bit-crd-bsr"), ("tertiary", "bit-crd-btr"), ("transparent", "bit-crd-brr") };
        var colors = new List<string>();
        foreach (var (name, cssClass) in expected)
        {
            var card = El($"crd-br-{name}");
            await Expect(card).ToHaveClassAsync(HasClass("bit-crd-brd"));
            await Expect(card).ToHaveClassAsync(HasClass(cssClass));
            await Expect(card).ToHaveCSSAsync("border-top-width", "1px");
            colors.Add(await StyleAsync(card, "border-top-color"));
        }

        Assert.AreEqual(4, colors.Distinct().Count(), string.Join(" | ", colors));
        Assert.AreEqual(Transparent, colors[3]);
    }

    [TestMethod]
    public async Task Background_EveryColorKind_PaintsItsOwnSurface()
    {
        await OpenAsync(Url);

        var expected = new (string Name, string Class)[] { ("primary", "bit-crd-bpg"), ("secondary", "bit-crd-bsg"), ("tertiary", "bit-crd-btg"), ("transparent", "bit-crd-brg") };
        var fills = new List<string>();
        foreach (var (name, cssClass) in expected)
        {
            await Expect(El($"crd-bg-{name}")).ToHaveClassAsync(HasClass(cssClass));
            fills.Add(await StyleAsync(El($"crd-bg-{name}"), "background-color"));
        }

        Assert.AreEqual(4, fills.Distinct().Count(), string.Join(" | ", fills));
        Assert.AreEqual(Transparent, fills[3]);
    }

    [TestMethod]
    public async Task Accessibility_AnAriaLabelOrALabelledBy_MakesAPlainCardANamedGroup()
    {
        await OpenAsync(Url);

        await Expect(El("crd-a11y-label")).ToHaveAttributeAsync("role", "group");
        await Expect(El("crd-a11y-label")).ToHaveAttributeAsync("aria-label", "Release notes");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Release notes" })).ToHaveCountAsync(1);

        await Expect(El("crd-a11y-lb")).ToHaveAttributeAsync("role", "group");
        await Expect(El("crd-a11y-lb")).ToHaveAttributeAsync("aria-labelledby", "external-label");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Labelled from outside" })).ToHaveCountAsync(1);

        await Expect(El("crd-basic")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
    }

    [TestMethod]
    public async Task Accessibility_ALinkedCardInANamedList_ReadsAsItsTitleAndHeading()
    {
        await OpenAsync(Url);

        var list = Page.GetByRole(AriaRole.List, new() { Name = "Latest guides" });
        await Expect(list.GetByRole(AriaRole.Link, new() { Name = "Theming" })).ToHaveAccessibleDescriptionAsync("5 min read");
        await Expect(list.GetByRole(AriaRole.Heading, new() { Name = "Theming", Level = 3 })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Accessibility_AnOptionRole_MakesTheRootTheControl_AnsweringEnterAndSpace()
    {
        await OpenAsync(Url);

        var card = El("crd-option");
        await Expect(card).ToHaveAttributeAsync("role", "option");
        await Expect(card).ToHaveAttributeAsync("tabindex", "0");
        // A Selected handed one way and false is no selected state to report.
        await Expect(card).Not.ToHaveAttributeAsync("aria-selected", new Regex(".*"));
        await Expect(Stretched("crd-option")).ToHaveCountAsync(0);
        await Expect(El("crd-option-ttl")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await card.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(El("option-clicks")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync(" ");
        await Expect(El("option-clicks")).ToHaveTextAsync("2");
        await card.ClickAsync();
        await Expect(El("option-clicks")).ToHaveTextAsync("3");

        await El("btn-option-select").ClickAsync();
        await Expect(card).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.GetByRole(AriaRole.Option, new() { Name = "Option card An option.", Selected = true })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Accessibility_AStructuralRole_KeepsTheStretchedButton()
    {
        await OpenAsync(Url);

        await Expect(El("crd-listitem")).ToHaveAttributeAsync("role", "listitem");
        await Expect(Stretched("crd-listitem")).ToHaveCountAsync(1);
        await Expect(El("crd-listitem")).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));

        await El("crd-listitem-ttl").ClickAsync();
        await Expect(El("listitem-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Accessibility_AClickableCardWithNoTitle_IsNamedByWhatItSays()
    {
        await OpenAsync(Url);

        await Expect(Stretched("crd-body-named")).ToHaveAttributeAsync("aria-labelledby", "crd-body-named");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Named by its body." })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReshapeTheCards_AndAParameterWinsOverThem()
    {
        await OpenAsync(Url);

        var card = El("crd-var-1");
        await Expect(card).ToHaveCSSAsync("border-top-left-radius", "16px");
        await Expect(card).ToHaveCSSAsync("box-shadow", "none");
        await Expect(card).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(card).ToHaveCSSAsync("border-top-color", "rgb(1, 100, 200)");
        await Expect(Page.Locator("#crd-var-1 > .bit-crd-mai > .bit-crd-hdr")).ToHaveCSSAsync("padding-left", "20px");

        await El("crd-var-1-ttl").HoverAsync();
        await Expect(card).ToHaveCSSAsync("box-shadow", "rgb(0, 0, 0) 0px 0px 10px 0px");

        var wins = El("crd-var-wins");
        Assert.AreEqual(await StyleAsync(El("crd-br-primary"), "border-top-color"), await StyleAsync(wins, "border-top-color"));
        await Expect(wins).ToHaveCSSAsync("border-top-left-radius", "0px");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RecolorAndRetypeTheCards()
    {
        await OpenAsync(Url);

        var card = El("crd-var-brand");
        await Expect(card).ToHaveCSSAsync("background-color", "rgb(30, 27, 75)");
        await Expect(card).ToHaveCSSAsync("color", "rgb(224, 231, 255)");
        await Expect(El("crd-var-brand-sub")).ToHaveCSSAsync("color", "rgb(165, 180, 252)");
        await Expect(El("crd-var-brand-sub")).ToHaveCSSAsync("font-size", "11px");
        await Expect(El("crd-var-brand-ttl")).ToHaveCSSAsync("font-weight", "700");
        await Expect(El("crd-var-brand-ttl")).ToHaveCSSAsync("font-size", "22px");
        await Expect(Page.Locator("#crd-var-brand i.bit-crd-hic")).ToHaveCSSAsync("font-size", "30px");
        await Expect(Page.Locator("#crd-var-brand > .bit-crd-mai")).ToHaveCSSAsync("row-gap", "7px");
        await Expect(Page.Locator("#crd-var-brand > .bit-crd-mai > *").Nth(1)).ToHaveCSSAsync("border-top-color", "rgb(67, 56, 202)");

        var selectable = El("crd-var-sel");
        await El("crd-var-sel-ttl").ClickAsync();
        await Expect(selectable).ToHaveCSSAsync("outline-color", "rgb(251, 191, 36)");
        await Expect(selectable).ToHaveCSSAsync("background-color", "rgb(49, 46, 129)");
    }

    [TestMethod]
    public async Task CoverOverlay_LaysTheCoverBehindTheContent_UnderTheScrim()
    {
        await OpenAsync(Url);

        var card = await BoxAsync(El("crd-overlay"));
        var cover = Page.Locator("#crd-overlay > .bit-crd-cvr");
        await Expect(El("crd-overlay")).ToHaveClassAsync(HasClass("bit-crd-ovl"));
        await Expect(cover).ToHaveCSSAsync("position", "absolute");
        var coverBox = await BoxAsync(cover);
        Near(card.Width, coverBox.Width, "the cover fills the card");
        Near(card.Height, coverBox.Height, "the cover fills the card");
        StringAssert.Contains(await PseudoStyleAsync(cover, "::after", "background-image"), "linear-gradient");
        await Expect(Page.Locator("#crd-overlay > .bit-crd-mai")).ToHaveCSSAsync("position", "relative");
        await Expect(El("crd-overlay-ttl")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        Near(card.Height, 224, "Height 14rem");
    }

    [TestMethod]
    public async Task BitParams_FillsTheUnsetParameters_AndTheCardsOwnValuesWin()
    {
        await OpenAsync(Url);

        await Expect(El("crd-par-1")).ToHaveClassAsync(HasClass("bit-crd-otl"));
        await Expect(El("crd-par-1")).ToHaveClassAsync(HasClass("bit-crd-sm"));
        await Expect(El("crd-par-1")).ToHaveClassAsync(HasClass("bit-crd-dvd"));
        await Expect(El("crd-par-1")).ToHaveCSSAsync("border-top-width", "1px");

        await Expect(El("crd-par-own")).ToHaveClassAsync(HasClass("bit-crd-lg"));
        await Expect(El("crd-par-own")).Not.ToHaveClassAsync(HasClass("bit-crd-sm"));
        await Expect(El("crd-par-own")).ToHaveClassAsync(HasClass("bit-crd-otl"));
        Assert.IsTrue(Px(await StyleAsync(El("crd-par-own-ttl"), "font-size")) > Px(await StyleAsync(El("crd-par-1-ttl"), "font-size")));
    }

    [TestMethod]
    public async Task Color_EveryRole_PaintsEveryVariant()
    {
        await OpenAsync(Url);

        var suffixes = new[] { "pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err", "pbg", "sbg", "tbg", "pfg", "sfg", "tfg", "pbr", "sbr", "tbr" };
        var semanticFills = new List<string>();

        foreach (var suffix in suffixes)
        {
            var fill = El($"crd-c-{suffix}-fill");
            var outline = El($"crd-c-{suffix}-outline");
            var text = El($"crd-c-{suffix}-text");
            var unset = El($"crd-c-{suffix}-default");

            await Expect(fill).ToHaveClassAsync(HasClass($"bit-crd-{suffix}"));
            await Expect(fill).ToHaveClassAsync(HasClass("bit-crd-vfl"));
            await Expect(outline).ToHaveClassAsync(HasClass("bit-crd-vot"));
            await Expect(text).ToHaveClassAsync(HasClass("bit-crd-vtx"));
            await Expect(unset).ToHaveClassAsync(HasClass("bit-crd-vfl"));

            var fillBg = await StyleAsync(fill, "background-color");
            Assert.AreEqual(fillBg, await StyleAsync(outline, "border-top-color"), $"{suffix}: the outline is not drawn in the role color.");
            Assert.AreEqual(fillBg, await StyleAsync(unset, "background-color"), $"{suffix}: an unset Variant is not Fill.");
            await Expect(outline).ToHaveCSSAsync("border-top-width", "1px");
            await Expect(outline).ToHaveCSSAsync("box-shadow", "none");
            await Expect(text).ToHaveCSSAsync("background-image", new Regex("^linear-gradient"));
            await Expect(text).ToHaveCSSAsync("box-shadow", "none");

            if (semanticFills.Count < 8) semanticFills.Add(fillBg);
        }

        Assert.AreEqual(8, semanticFills.Distinct().Count(), string.Join(" | ", semanticFills));
    }

    [TestMethod]
    public async Task Color_AVariantWithoutAColor_PublishesNothing_AndADisabledOneTakesTheDisabledColors()
    {
        await OpenAsync(Url);

        await Expect(El("crd-variant-only")).Not.ToHaveClassAsync(new Regex(@"bit-crd-v(fl|ot|tx)"));
        await Expect(El("crd-variant-only")).ToHaveCSSAsync("border-top-width", "0px");

        await Expect(El("crd-c-dis-fill")).ToHaveClassAsync(HasClass("bit-dis"));
        Assert.AreNotEqual(await StyleAsync(El("crd-c-pri-fill"), "background-color"), await StyleAsync(El("crd-c-dis-fill"), "background-color"));
        Assert.AreNotEqual(await StyleAsync(El("crd-c-suc-outline"), "border-top-color"), await StyleAsync(El("crd-c-dis-outline"), "border-top-color"));
    }

    [TestMethod]
    public async Task ExternalIcons_RenderTheGivenClasses()
    {
        await OpenAsync(Url);

        var css = Page.Locator("#crd-ext-css i.bit-crd-hic");
        await Expect(css).ToHaveClassAsync(HasClass("fa-house"));
        await Expect(css).ToHaveClassAsync(HasClass("e2e-ext"));
        await Expect(css).Not.ToHaveClassAsync(HasClass("bit-icon"));
        await Expect(Page.Locator("#crd-ext-bi i.bit-crd-hic")).ToHaveClassAsync(HasClass("bi-github"));
        await Expect(Page.Locator("#crd-ext-bi i.bit-crd-hic")).ToHaveClassAsync(HasClass("bi"));
        await Expect(Page.Locator("#crd-ext-fa i.bit-crd-hic")).ToHaveClassAsync(HasClass("fa-rocket"));
        await Expect(Page.Locator("#crd-ext-fa i.bit-crd-hic")).ToHaveClassAsync(HasClass("fa-solid"));
    }

    [TestMethod]
    public async Task Size_EverySize_ScalesThePaddingTheGapsAndTheType()
    {
        await OpenAsync(Url);

        var paddings = new List<double>();
        var titles = new List<double>();
        var subtitles = new List<double>();
        var icons = new List<double>();
        foreach (var (id, cssClass) in new[] { ("crd-sm", "bit-crd-sm"), ("crd-md", "bit-crd-md"), ("crd-lg", "bit-crd-lg") })
        {
            await Expect(El(id)).ToHaveClassAsync(HasClass(cssClass));
            paddings.Add(Px(await StyleAsync(Page.Locator($"#{id} > .bit-crd-mai > .bit-crd-hdr"), "padding-left")));
            titles.Add(Px(await StyleAsync(El($"{id}-ttl"), "font-size")));
            subtitles.Add(Px(await StyleAsync(El($"{id}-sub"), "font-size")));
            icons.Add(Px(await StyleAsync(Page.Locator($"#{id} i.bit-crd-hic"), "font-size")));
        }

        Assert.IsTrue(paddings[0] < paddings[1] && paddings[1] < paddings[2], string.Join(", ", paddings));
        Assert.IsTrue(titles[0] < titles[1] && titles[1] < titles[2], string.Join(", ", titles));
        Assert.IsTrue(subtitles[0] < subtitles[1] && subtitles[1] < subtitles[2], string.Join(", ", subtitles));
        Assert.IsTrue(icons[0] < icons[2] && icons[0] <= icons[1], string.Join(", ", icons));
    }

    [TestMethod]
    public async Task StyleAndClass_ApplyToTheRoot()
    {
        await OpenAsync(Url);

        await Expect(El("crd-style")).ToHaveCSSAsync("border-top-color", "rgb(147, 112, 219)");
        await Expect(El("crd-style")).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(El("crd-class")).ToHaveClassAsync(HasClass("e2e-crd-class"));
        await Expect(El("crd-class")).ToHaveCSSAsync("outline-style", "dashed");
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Url);

        await Expect(El("crd-styles")).ToHaveCSSAsync("border-top-color", "rgb(0, 139, 139)");
        await Expect(Page.Locator("#crd-styles .bit-crd-hdr")).ToHaveCSSAsync("border-bottom-color", "rgb(0, 139, 139)");
        await Expect(El("crd-styles-ttl")).ToHaveCSSAsync("color", "rgb(0, 139, 139)");
        await Expect(El("crd-styles-sub")).ToHaveCSSAsync("color", "rgb(0, 100, 100)");
        await Expect(Page.Locator("#crd-styles i.bit-crd-hic")).ToHaveCSSAsync("color", "rgb(0, 139, 139)");
        await Expect(Page.Locator("#crd-styles .bit-crd-htx")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Page.Locator("#crd-styles .bit-crd-bdy")).ToHaveCSSAsync("font-weight", "700");
        await Expect(Page.Locator("#crd-styles .bit-crd-ftr")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Page.Locator("#crd-styles .bit-crd-mai")).ToHaveCSSAsync("background-color", "rgb(240, 255, 255)");
        await Expect(Page.Locator("#crd-styles .bit-crd-cvr")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(Page.Locator("#crd-styles .bit-crd-img")).ToHaveCSSAsync("border-top-left-radius", "4px");
        await Expect(Page.Locator("#crd-styles .bit-crd-act")).ToHaveCSSAsync("padding-top", "3px");

        await Expect(El("crd-styles-sel")).ToHaveCSSAsync("background-color", "rgb(255, 228, 225)");
        await Expect(Stretched("crd-styles-sel")).ToHaveCSSAsync("outline-offset", "5px");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart_AndSelectedOnlyWhileSelected()
    {
        await OpenAsync(Url);

        await Expect(El("crd-classes")).ToHaveClassAsync(HasClass("e2e-crd-root"));
        await Expect(El("crd-classes")).ToHaveCSSAsync("letter-spacing", "1px");
        await Expect(El("crd-classes-ttl")).ToHaveClassAsync(HasClass("e2e-crd-title"));
        await Expect(El("crd-classes-ttl")).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#crd-classes i.bit-crd-hic")).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(Page.Locator("#crd-classes .bit-crd-bdy")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Stretched("crd-classes")).ToHaveClassAsync(HasClass("e2e-crd-link"));
        await Expect(El("crd-classes")).Not.ToHaveClassAsync(HasClass("e2e-crd-selected"));

        await El("crd-classes-ttl").ClickAsync();
        await Expect(El("crd-classes")).ToHaveClassAsync(HasClass("e2e-crd-selected"));
        await Expect(El("crd-classes")).ToHaveCSSAsync("text-decoration-line", "underline");
    }

    [TestMethod]
    public async Task Rtl_DirRtl_MirrorsTheHeader()
    {
        await OpenAsync(Url);

        await Expect(El("crd-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(El("crd-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await AssertMirroredAsync("crd-rtl", "crd-rtl-act");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_MirrorsTheHeader()
    {
        await OpenAsync(Url);

        await Expect(El("crd-rtl-inh")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await AssertMirroredAsync("crd-rtl-inh", "crd-rtl-inh-act");
    }

    [TestMethod]
    public async Task Disabled_DropsThePagesActivationHandlers_AndKeepsTheOnesThatFollowThePointer()
    {
        await OpenAsync(Url);

        var card = El("crd-dis-handler");
        await Expect(card).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(card).ToHaveCSSAsync("pointer-events", "none");

        // A screen reader activates what it announces by dispatching a click, which pointer-events cannot stop.
        await card.DispatchEventAsync("click");
        var box = await BoxAsync(card);
        await Page.Mouse.ClickAsync((float)(box.X + 10), (float)(box.Y + 10));
        await El("crd-dis-inner").DispatchEventAsync("click");
        await card.DispatchEventAsync("mouseover");

        await Expect(El("page-hovers")).ToHaveTextAsync("1");
        await Expect(El("inner-clicks")).ToHaveTextAsync("1");
        await Expect(El("page-clicks")).ToHaveTextAsync("0");

        await El("btn-enable-handler").ClickAsync();
        await Expect(card).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await El("crd-dis-handler-ttl").HoverAsync();
        await Expect(El("page-hovers")).ToHaveTextAsync("2");
        await El("crd-dis-inner").ClickAsync();
        await Expect(El("inner-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Enabled_ThePagesOwnOnclick_RunsOnACardWithoutAClickOfItsOwn()
    {
        await OpenAsync(Url);

        await El("crd-en-handler-ttl").ClickAsync();
        await Expect(El("enabled-page-clicks")).ToHaveTextAsync("1");

        await El("btn-enable-handler").ClickAsync();
        await El("crd-dis-handler-ttl").ClickAsync();
        await Expect(El("page-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        await Expect(El("crd-vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(El("crd-vis-hidden")).ToBeHiddenAsync();
        Assert.IsTrue((await BoxAsync(El("crd-vis-hidden"))).Height > 0);
        await Expect(El("crd-vis-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(El("crd-attrs")).ToHaveAttributeAsync("data-e2e", "custom");
        await Expect(El("crd-attrs")).ToHaveAttributeAsync("data-extra", "from-dictionary");
    }

    [TestMethod]
    public async Task Rerender_ParametersChangedByThePage_UpdateTheCard()
    {
        await OpenAsync(Url);

        await Expect(El("crd-rr-ttl")).ToHaveTextAsync("Before");
        await Expect(El("crd-rr")).ToHaveClassAsync(HasClass("bit-crd-sm"));
        var before = await StyleAsync(El("crd-rr"), "background-color");

        await El("btn-rr").ClickAsync();

        await Expect(El("crd-rr-ttl")).ToHaveTextAsync("After");
        await Expect(El("crd-rr")).ToHaveClassAsync(HasClass("bit-crd-lg"));
        await Expect(El("crd-rr")).ToHaveClassAsync(HasClass("bit-crd-err"));
        await Expect(El("crd-rr")).ToHaveClassAsync(HasClass("bit-crd-vfl"));
        await PollAsync(() => StyleAsync(El("crd-rr"), "background-color"), bg => bg != before, "the Color did not repaint the card");
    }

    [TestMethod]
    public async Task Unmount_RemovesTheCardWithoutAnError()
    {
        await OpenAsync(Url);

        await Expect(El("crd-um")).ToHaveCountAsync(1);
        await El("btn-unmount").ClickAsync();
        await Expect(El("crd-um")).ToHaveCountAsync(0);
        await El("btn-unmount").ClickAsync();
        await Expect(El("crd-um")).ToHaveCountAsync(1);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task AssertMirroredAsync(string id, string actionId)
    {
        var icon = await BoxAsync(Page.Locator($"#{id} i.bit-crd-hic"));
        var title = await BoxAsync(El($"{id}-ttl"));
        var action = await BoxAsync(El(actionId));

        Assert.IsTrue(icon.X > title.X, $"The icon ({icon.X}) does not lead from the right of the title ({title.X}).");
        Assert.IsTrue(action.X + action.Width <= title.X, $"The actions ({action.X}) are not at the left end of the header ({title.X}).");
    }

    // The box-shadow a --bit-shd-N token of the theme resolves to.
    private Task<string> ShadowTokenAsync(int level)
    {
        return Page.EvaluateAsync<string>(@"n => {
            const probe = document.createElement('div');
            probe.style.boxShadow = `var(--bit-shd-${n})`;
            document.body.appendChild(probe);
            const value = getComputedStyle(probe).boxShadow;
            probe.remove();
            return value;
        }", level);
    }

    // Waits for the transitions the last state change started on the element to finish.
    private Task SettleAsync(ILocator locator)
    {
        return PollAsync(() => locator.EvaluateAsync<bool>("el => el.getAnimations().length === 0"), done => done, "the transitions never finished");
    }

    private static async Task<LayoutBox> BoxAsync(ILocator locator)
    {
        var box = await locator.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.x, r.y, r.width, r.height]; }");
        return new(box[0], box[1], box[2], box[3]);
    }

    private static Task<string> StyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>("(el, p) => getComputedStyle(el).getPropertyValue(p)", property);
    }

    private static Task<string> PseudoStyleAsync(ILocator locator, string pseudo, string property)
    {
        return locator.EvaluateAsync<string>("(el, [s, p]) => getComputedStyle(el, s).getPropertyValue(p)", new[] { pseudo, property });
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= tolerance, $"{what}: expected {expected}, got {actual}.");
    }

    private async Task<T> PollAsync<T>(Func<Task<T>> read, Func<T, bool> done, string message)
    {
        var started = DateTime.UtcNow;
        T value;
        do
        {
            value = await read();
            if (done(value)) return value;
            await NextFramesAsync();
        } while (DateTime.UtcNow - started < TimeSpan.FromMilliseconds(DefaultTimeout));

        Assert.Fail($"{message} (last value: {value})");
        return value;
    }

    private sealed record LayoutBox(double X, double Y, double Width, double Height);
}
