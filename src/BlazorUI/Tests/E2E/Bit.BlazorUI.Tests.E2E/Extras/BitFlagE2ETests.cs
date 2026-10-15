using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitFlag at /e2e/flag in a real browser: the country it resolves from each way of naming one, the packaged
/// image, the image sets (served through a route, since the host carries no Bit.BlazorUI.Assets), Src, SrcPattern,
/// the fallbacks of a failed image, the emoji, every shape and its cut to the flag, AspectRatio and every Fit, the
/// accessible name and role in every mode, OnClick from the pointer and the keyboard with its grown hit box, the
/// disabled contract, FocusAsync, the BitParams cascade, Size, Style/Class/Styles/Classes, every --bit-Flag-*
/// variable, RTL, reduced motion, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitFlagE2ETests : E2ETestBase
{
    private const string PackagedPath = "_content/Bit.BlazorUI.Extras/flags/";
    private const string SetPath = "_content/Bit.BlazorUI.Assets/flags/";

    [TestMethod]
    public async Task Basic_ACountry_DrawsItsPackagedImageAtTheIconSize()
    {
        await OpenAsync("/e2e/flag");

        var img = Page.Locator("#flag-basic > img");
        await Expect(img).ToHaveAttributeAsync("src", $"{PackagedPath}IR-flat-16.webp");
        await Expect(img).ToHaveClassAsync(HasClass("bit-flg-img"));
        await Expect(Page.Locator("#flag-basic")).ToHaveClassAsync(HasClass("bit-flg"));
        await img.ScrollIntoViewIfNeededAsync();
        await WaitForImageAsync("#flag-basic > img");

        Assert.AreEqual(16, await img.EvaluateAsync<int>("i => i.naturalWidth"));

        var (w, h) = await SizeAsync("#flag-basic");
        Assert.AreEqual(16, w, 0.5);
        Assert.AreEqual(16, h, 0.5);

        var (w2, h2) = await SizeAsync("#flag-basic-height");
        Assert.AreEqual(32, w2, 0.5, "Height sets the width with it.");
        Assert.AreEqual(32, h2, 0.5);
    }

    [DataTestMethod]
    [DataRow("flag-iso2", "NL")]
    [DataRow("flag-iso3", "NL")]
    [DataRow("flag-code", "NL")]
    [DataRow("flag-name", "NL")]
    [DataRow("flag-holland", "NL")]
    [DataRow("flag-czechia", "CZ")]
    [DataRow("flag-curacao", "CW")]
    [DataRow("flag-uk", "GB")]
    [DataRow("flag-plus1", "US")]
    [DataRow("flag-first-wins", "NL")]
    [DataRow("flag-iso3-wins", "DE")]
    [DataRow("flag-country-wins", "JP")]
    public async Task Naming_EachWayOfNamingACountry_ResolvesTheRightFlag(string id, string iso2)
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator($"#{id} > img")).ToHaveAttributeAsync("src", $"{PackagedPath}{iso2}-flat-16.webp");
    }

    [TestMethod]
    public async Task Naming_ResolvedCountry_TitlesTheFlagWithItsName()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-holland")).ToHaveAttributeAsync("title", "Netherlands");
        await Expect(Page.Locator("#flag-czechia")).ToHaveAttributeAsync("title", "Czech Republic");
        await Expect(Page.Locator("#flag-plus1")).ToHaveAttributeAsync("title", "United States");
    }

    [TestMethod]
    public async Task Naming_UnknownCountry_DrawsNothingAndIsHidden()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-unknown");
        await Expect(flag).ToBeAttachedAsync();
        await Expect(flag.Locator("> *")).ToHaveCountAsync(0);
        await Expect(flag).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task Shape_Plain_IsAnUncutSquare()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-plain");
        await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-flg-crp"));
        var (w, h) = await SizeAsync("#flag-plain");
        Assert.AreEqual(32, w, 0.5);
        Assert.AreEqual(32, h, 0.5);
        Assert.AreEqual("0px", await ComputedStyleAsync("#flag-plain", "border-top-left-radius"));
        Assert.AreEqual("none", await ComputedStyleAsync("#flag-plain", "box-shadow"));
        Assert.AreEqual("none", await ComputedStyleAsync("#flag-plain", "filter"));
    }

    [TestMethod]
    public async Task Shape_Rounded_RoundsTheCornersAndCutsTheFrameToTheFlag()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-rounded");
        await Expect(flag).ToHaveClassAsync(HasClass("bit-flg-rnd"));
        await Expect(flag).ToHaveClassAsync(HasClass("bit-flg-crp"));
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#flag-rounded", "border-top-left-radius"));
        Assert.AreEqual("clip", await ComputedStyleAsync("#flag-rounded", "overflow-x"));

        // The flag of the 16px image is drawn in a 14x11 box: the frame takes those proportions from the height.
        var (w, h) = await SizeAsync("#flag-rounded");
        Assert.AreEqual(32, h, 0.5);
        Assert.AreEqual(32d * 14 / 11, w, 0.6);
    }

    [TestMethod]
    public async Task Shape_Circular_IsACircleCutOutOfTheFlag()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-circular")).ToHaveClassAsync(HasClass("bit-flg-cir"));
        var (w, h) = await SizeAsync("#flag-circular");
        Assert.AreEqual(32, w, 0.5);
        Assert.AreEqual(32, h, 0.5);
        var radius = ParsePx(await ComputedStyleAsync("#flag-circular", "border-top-left-radius"));
        Assert.IsTrue(radius >= 16, $"A circular flag's corner is at least half its size, was {radius}.");
    }

    [TestMethod]
    public async Task Shape_Bordered_DrawsABorderInsideTheFrame()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-bordered")).ToHaveClassAsync(HasClass("bit-flg-brd"));
        Assert.AreEqual("1px", await ComputedStyleAsync("#flag-bordered", "border-top-width"));
        Assert.AreEqual("solid", await ComputedStyleAsync("#flag-bordered", "border-top-style"));
        var (_, h) = await SizeAsync("#flag-bordered");
        Assert.AreEqual(32, h, 0.5, "The border takes no extra room.");
    }

    [TestMethod]
    public async Task Shape_ShadowAndGrayscale_PaintTheFrame()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-shadow")).ToHaveClassAsync(HasClass("bit-flg-shd"));
        Assert.AreNotEqual("none", await ComputedStyleAsync("#flag-shadow", "box-shadow"));

        await Expect(Page.Locator("#flag-grayscale")).ToHaveClassAsync(HasClass("bit-flg-gry"));
        Assert.AreEqual("grayscale(1)", await ComputedStyleAsync("#flag-grayscale", "filter"));

        var combo = Page.Locator("#flag-combo");
        await Expect(combo).ToHaveClassAsync(HasClass("bit-flg-cir"));
        await Expect(combo).ToHaveClassAsync(HasClass("bit-flg-brd"));
        await Expect(combo).ToHaveClassAsync(HasClass("bit-flg-shd"));
        Assert.AreEqual("1px", await ComputedStyleAsync("#flag-combo", "border-top-width"));
        Assert.AreNotEqual("none", await ComputedStyleAsync("#flag-combo", "box-shadow"));
    }

    [TestMethod]
    public async Task Emoji_DrawsTheRegionalIndicatorGlyphAndGrowsTheFrameToIt()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-emoji");
        var glyph = flag.Locator("> .bit-flg-emj");
        await Expect(glyph).ToHaveTextAsync("\U0001F1EE\U0001F1F7");
        await Expect(flag.Locator("img")).ToHaveCountAsync(0);
        await Expect(flag).ToHaveClassAsync(HasClass("bit-flg-emo"));
        await Expect(glyph).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(flag).ToHaveAttributeAsync("aria-hidden", "true");

        Assert.AreEqual("48px", await ComputedStyleAsync("#flag-emoji-height", "font-size"));

        // Unshaped, the frame is as wide as the glyph rather than the square of the size.
        var (frameWidth, _) = await SizeAsync("#flag-emoji-height");
        var (glyphWidth, _) = await SizeAsync("#flag-emoji-height > .bit-flg-emj");
        Assert.AreEqual(glyphWidth, frameWidth, 0.5);
    }

    [TestMethod]
    public async Task Emoji_Subdivision_DrawsTheTagSequenceOfScotland()
    {
        await OpenAsync("/e2e/flag");

        var expected = "\U0001F3F4" + string.Concat("gbsct".Select(c => char.ConvertFromUtf32(0xE0000 + c))) + "\U000E007F";
        await Expect(Page.Locator("#flag-emoji-sct > .bit-flg-emj")).ToHaveTextAsync(expected);
    }

    [TestMethod]
    public async Task Emoji_Shaped_KeepsTheSizeAskedForAndCropsTheGlyph()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-emoji-shaped");
        await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-flg-emo"));
        var (w, h) = await SizeAsync("#flag-emoji-shaped");
        Assert.AreEqual(48, w, 0.5);
        Assert.AreEqual(48, h, 0.5);
        Assert.AreEqual("hidden", await ComputedStyleAsync("#flag-emoji-shaped > .bit-flg-emj", "overflow-x"));
        Assert.AreEqual("flex", await ComputedStyleAsync("#flag-emoji-shaped > .bit-flg-emj", "display"));
    }

    [TestMethod]
    public async Task Accessibility_ImageFlags_AreDecorativeUntilNamed()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-decorative > img")).ToHaveAttributeAsync("alt", "");
        await Expect(Page.Locator("#flag-decorative")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Page.Locator("#flag-decorative")).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));

        await Expect(Page.Locator("#flag-autoalt > img")).ToHaveAttributeAsync("alt", "Canada");
        await Expect(Page.Locator("#flag-autoalt > img")).ToHaveAccessibleNameAsync("Canada");
        await Expect(Page.Locator("#flag-autoalt")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await Expect(Page.Locator("#flag-alt > img")).ToHaveAttributeAsync("alt", "Ships to Canada");
        await Expect(Page.Locator("#flag-arialabel > img")).ToHaveAttributeAsync("alt", "Aria named");
        await Expect(Page.Locator("#flag-arialabel")).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
    }

    [TestMethod]
    public async Task Accessibility_TitleAndAutoTitle_SetTheTooltip()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-autotitle")).ToHaveAttributeAsync("title", "Canada");
        await Expect(Page.Locator("#flag-title")).ToHaveAttributeAsync("title", "Custom title");
        await Expect(Page.Locator("#flag-decorative")).Not.ToHaveAttributeAsync("title", new Regex(".*"));
    }

    [TestMethod]
    public async Task Accessibility_EmojiFlag_IsAnImgWhenNamedAndHiddenWhenNot()
    {
        await OpenAsync("/e2e/flag");

        var named = Page.Locator("#flag-emoji-named");
        await Expect(named).ToHaveRoleAsync(AriaRole.Img);
        await Expect(named).ToHaveAccessibleNameAsync("Canada");
        await Expect(named).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));

        var unnamed = Page.Locator("#flag-emoji-unnamed");
        await Expect(unnamed).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(unnamed).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await Expect(Page.Locator("#flag-unknown-fallback-unnamed")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task Accessibility_AriaLabelledBy_MakesTheFrameTheNamedImg()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-labelledby");
        await Expect(flag).ToHaveAttributeAsync("aria-labelledby", "ext-label");
        await Expect(flag).ToHaveRoleAsync(AriaRole.Img);
        await Expect(flag).ToHaveAccessibleNameAsync("Flag named elsewhere");
        await Expect(flag.Locator("> img")).ToHaveAttributeAsync("alt", "");
    }

    [TestMethod]
    public async Task ImageSet_FlatAndShiny_OfferEverySizeAndDrawTheOneTheFrameNeeds()
    {
        await RouteImageSetsAsync();
        await OpenAsync("/e2e/flag");
        await Page.Locator("#btn-show-sets").ClickAsync();

        var flat = Page.Locator("#flag-set-flat > img");
        await Expect(flat).ToHaveAttributeAsync("src", $"{SetPath}BR-flat-48.webp");
        var srcSet = await flat.GetAttributeAsync("srcset");
        StringAssert.Contains(srcSet, $"{SetPath}BR-flat-16.webp 0.33x");
        StringAssert.Contains(srcSet, $"{SetPath}BR-flat-48.webp 1x");
        StringAssert.Contains(srcSet, $"{SetPath}BR-flat-64.webp 1.33x");

        await Expect(Page.Locator("#flag-set-shiny > img")).ToHaveAttributeAsync("src", $"{SetPath}BR-shiny-48.webp");

        await flat.ScrollIntoViewIfNeededAsync();
        await WaitForImageAsync("#flag-set-flat > img");
        StringAssert.EndsWith(await flat.EvaluateAsync<string>("i => i.currentSrc"), "BR-flat-48.webp");
        await Expect(flat).ToHaveAttributeAsync("src", $"{SetPath}BR-flat-48.webp");
    }

    [DataTestMethod]
    [DataRow("size16", 16)]
    [DataRow("size24", 24)]
    [DataRow("size32", 32)]
    [DataRow("size48", 48)]
    [DataRow("size64", 64)]
    public async Task ImageSet_ImageSize_PinsOneSize(string id, int size)
    {
        await RouteImageSetsAsync();
        await OpenAsync("/e2e/flag");
        await Page.Locator("#btn-show-sets").ClickAsync();

        var img = Page.Locator($"#flag-set-{id} > img");
        await Expect(img).ToHaveAttributeAsync("src", $"{SetPath}BR-flat-{size}.webp");
        await Expect(img).Not.ToHaveAttributeAsync("srcset", new Regex(".*"));
    }

    [TestMethod]
    public async Task ImageSet_Shaped_CutsToTheFlagOfTheSetImage()
    {
        await RouteImageSetsAsync();
        await OpenAsync("/e2e/flag");
        await Page.Locator("#btn-show-sets").ClickAsync();

        var flag = Page.Locator("#flag-set-shaped");
        await Expect(flag).ToHaveClassAsync(HasClass("bit-flg-crp"));
        await Expect(flag.Locator("> img")).ToHaveAttributeAsync("src", $"{SetPath}BR-flat-64.webp");
        StringAssert.Contains(await flag.GetAttributeAsync("style"), "--bit-flg-crp-s:64");
        var (w, h) = await SizeAsync("#flag-set-shaped");
        Assert.AreEqual(48, h, 0.5);
        Assert.IsTrue(w > h, "The frame takes the proportions of the flag rather than the square image.");
    }

    [TestMethod]
    public async Task ImageSet_CascadingValue_SetsTheSetForEveryFlagInsideAndAnOwnOneWins()
    {
        await RouteImageSetsAsync();
        await OpenAsync("/e2e/flag");
        await Page.Locator("#btn-show-sets").ClickAsync();

        await Expect(Page.Locator("#flag-set-cascaded > img")).ToHaveAttributeAsync("src", $"{SetPath}IR-shiny-32.webp");
        await Expect(Page.Locator("#flag-set-cascaded-own > img")).ToHaveAttributeAsync("src", $"{SetPath}JP-flat-32.webp");
    }

    [TestMethod]
    public async Task ImageSet_MissingAssetsPackage_FallsBackToThePackagedFlag()
    {
        // The failed request of the set image is the subject: the browser logs it.
        AllowConsoleErrors = true;

        await OpenAsync("/e2e/flag");
        await Page.Locator("#btn-show-failing").ClickAsync();

        var img = Page.Locator("#flag-failed-set > img");
        await img.ScrollIntoViewIfNeededAsync();
        await Expect(img).ToHaveAttributeAsync("src", $"{PackagedPath}JP-flat-16.webp");
        await Expect(img).Not.ToHaveAttributeAsync("srcset", new Regex(".*"));
        await Expect(Page.Locator("#failed-set-errors")).ToHaveTextAsync("1");
        await WaitForImageAsync("#flag-failed-set > img");
    }

    [TestMethod]
    public async Task Src_AnImageOfThePagesOwn_IsDrawnInsteadOfThePackagedOne()
    {
        await OpenAsync("/e2e/flag");

        var img = Page.Locator("#flag-src > img");
        StringAssert.StartsWith(await img.GetAttributeAsync("src"), "data:image/svg+xml");
        await Expect(Page.Locator("#flag-src")).Not.ToHaveClassAsync(HasClass("bit-flg-crp"));
        await img.ScrollIntoViewIfNeededAsync();
        await WaitForImageAsync("#flag-src > img");
    }

    [TestMethod]
    public async Task SrcPattern_WritesTheCodesInTheCaseThePlaceholderAsksFor()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-pattern-ca > img")).ToHaveAttributeAsync("src", "samples/flags/ca.svg");
        await Expect(Page.Locator("#flag-pattern-de > img")).ToHaveAttributeAsync("src", "samples/flags/de.svg");
        await Expect(Page.Locator("#flag-pattern-upper > img")).ToHaveAttributeAsync("src", "samples/flags/JPN.svg");

        await Page.Locator("#flag-pattern-ca").ScrollIntoViewIfNeededAsync();
        await WaitForImageAsync("#flag-pattern-ca > img");
        Assert.AreEqual(90, await Page.Locator("#flag-pattern-ca > img").EvaluateAsync<int>("i => i.naturalWidth"));
    }

    [TestMethod]
    public async Task SrcPattern_ACountryOfThePagesOwn_IsDrawnAndNamed()
    {
        await OpenAsync("/e2e/flag");

        var img = Page.Locator("#flag-pattern-eu > img");
        await Expect(img).ToHaveAttributeAsync("src", "samples/flags/eu.svg");
        await Expect(img).ToHaveAttributeAsync("alt", "European Union");

        // A pattern asking for a code the country lacks writes no url, and the country has no packaged flag.
        await Expect(Page.Locator("#flag-pattern-eu-iso3 > img")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#flag-pattern-eu-iso3 .eu-fallback")).ToHaveTextAsync("EU");
    }

    [TestMethod]
    public async Task FallbackTemplate_StandsInForAnUnknownCountryAndFillsTheFrame()
    {
        await OpenAsync("/e2e/flag");

        var fallback = Page.Locator("#flag-fallback-unknown > .bit-flg-fbk");
        await Expect(fallback).ToHaveTextAsync("?");
        var (fw, fh) = await SizeAsync("#flag-fallback-unknown > .bit-flg-fbk");
        var (w, h) = await SizeAsync("#flag-fallback-unknown");
        Assert.AreEqual(24, h, 0.5);
        Assert.IsTrue(fw <= w && fh <= h && fw >= w - 2.5, $"The fallback fills the frame inside its border ({fw}x{fh} in {w}x{h}).");
        Assert.AreEqual("12px", await ComputedStyleAsync("#flag-fallback-unknown > .bit-flg-fbk", "font-size"));
    }

    [TestMethod]
    public async Task Src_Failed_FallsBackToThePackagedFlagAndFiresOnErrorOnce()
    {
        AllowConsoleErrors = true;

        await OpenAsync("/e2e/flag");
        await Page.Locator("#btn-show-failing").ClickAsync();

        var img = Page.Locator("#flag-failed-src > img");
        await img.ScrollIntoViewIfNeededAsync();
        await Expect(img).ToHaveAttributeAsync("src", $"{PackagedPath}JP-flat-16.webp");
        await Expect(Page.Locator("#failed-src-errors")).ToHaveTextAsync("1");
        await WaitForImageAsync("#flag-failed-src > img");
        await NextFramesAsync();
        await Expect(Page.Locator("#failed-src-errors")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Src_FailedWithNoCountry_ShowsTheFallbackTemplate()
    {
        AllowConsoleErrors = true;

        await OpenAsync("/e2e/flag");
        await Page.Locator("#btn-show-failing").ClickAsync();

        await Page.Locator("#flag-failed-nocountry").ScrollIntoViewIfNeededAsync();
        await Expect(Page.Locator("#flag-failed-nocountry .error-fallback")).ToHaveTextAsync("!");
        await Expect(Page.Locator("#flag-failed-nocountry > img")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#failed-nocountry-errors")).ToHaveTextAsync("1");
    }

    [DataTestMethod]
    [DataRow("flag-asp-none", 32d, 32d, false)]
    [DataRow("flag-asp-32", 48d, 32d, true)]
    [DataRow("flag-asp-43", 42.67d, 32d, true)]
    [DataRow("flag-asp-width", 64d, 32d, true)]
    [DataRow("flag-asp-pkg-1", 32d, 32d, true)]
    [DataRow("flag-asp-pkg-2", 64d, 32d, true)]
    [DataRow("flag-wh", 64d, 32d, false)]
    public async Task AspectRatio_ReshapesTheFrame(string id, double width, double height, bool hasRatioClass)
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator($"#{id}");
        if (hasRatioClass)
        {
            await Expect(flag).ToHaveClassAsync(HasClass("bit-flg-asp"));
        }
        else
        {
            await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-flg-asp"));
        }

        var (w, h) = await SizeAsync($"#{id}");
        Assert.AreEqual(width, w, 0.6, $"{id} width");
        Assert.AreEqual(height, h, 0.6, $"{id} height");
    }

    [TestMethod]
    public async Task AspectRatio_PackagedFlag_IsCutToTheRatio()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-asp-pkg-2")).ToHaveClassAsync(HasClass("bit-flg-crp"));
        await Expect(Page.Locator("#flag-wh")).ToHaveClassAsync(HasClass("bit-flg-crp"));
        await Expect(Page.Locator("#flag-asp-32")).Not.ToHaveClassAsync(HasClass("bit-flg-crp"));
    }

    [DataTestMethod]
    [DataRow("none", "bit-flg-non", "none", "0% 0%")]
    [DataRow("center", "bit-flg-ctr", "none", "50% 50%")]
    [DataRow("contain", "bit-flg-cnt", "contain", "50% 50%")]
    [DataRow("cover", "bit-flg-cvr", "cover", "50% 50%")]
    [DataRow("fill", "bit-flg-fil", "fill", "50% 50%")]
    [DataRow("scaledown", "bit-flg-scd", "scale-down", "50% 50%")]
    [DataRow("centercontain", "bit-flg-cct", "scale-down", "50% 50%")]
    [DataRow("centercover", "bit-flg-ccv", "cover", "50% 50%")]
    public async Task Fit_EachValue_PlacesThePicture(string fit, string cssClass, string objectFit, string objectPosition)
    {
        await OpenAsync("/e2e/flag");

        var id = $"#flag-fit-{fit}";
        await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass(cssClass));
        Assert.AreEqual(objectFit, await ComputedStyleAsync($"{id} > img", "object-fit"));
        Assert.AreEqual(objectPosition, await ComputedStyleAsync($"{id} > img", "object-position"));
    }

    [TestMethod]
    public async Task Loading_LazyByDefaultEagerWhenAskedAndImageAttributesReachTheImg()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-lazy > img")).ToHaveAttributeAsync("loading", "lazy");
        await Expect(Page.Locator("#flag-lazy > img")).ToHaveAttributeAsync("draggable", "false");
        await Expect(Page.Locator("#flag-lazy > img")).ToHaveAttributeAsync("decoding", "async");

        var eager = Page.Locator("#flag-eager > img");
        await Expect(eager).ToHaveAttributeAsync("loading", "eager");
        await Expect(eager).ToHaveAttributeAsync("referrerpolicy", "no-referrer");
        await Expect(eager).ToHaveAttributeAsync("data-img", "yes");

        var overrides = Page.Locator("#flag-img-overrides > img");
        await Expect(overrides).ToHaveAttributeAsync("draggable", "true");
        await Expect(overrides).ToHaveAttributeAsync("decoding", "sync");
    }

    [TestMethod]
    public async Task OnLoad_FiresOncePerRender()
    {
        await OpenAsync("/e2e/flag");

        await Page.Locator("#btn-render-load").ClickAsync();
        await Expect(Page.Locator("#load-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-render-load").ClickAsync();
        await Expect(Page.Locator("#load-count")).ToHaveTextAsync("1");
        await WaitForImageAsync("#flag-onload > img");
        await NextFramesAsync();
        await Expect(Page.Locator("#load-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OnClick_MakesTheFlagANamedButton()
    {
        await OpenAsync("/e2e/flag");

        var fr = Page.Locator("#flag-click-fr");
        await Expect(fr).ToHaveRoleAsync(AriaRole.Button);
        await Expect(fr).ToHaveAccessibleNameAsync("France");
        await Expect(fr).ToHaveAttributeAsync("tabindex", "0");
        await Expect(fr).ToHaveClassAsync(HasClass("bit-flg-clk"));
        await Expect(fr).ToHaveAttributeAsync("aria-pressed", "false");
        Assert.AreEqual("pointer", await ComputedStyleAsync("#flag-click-fr", "cursor"));
        await Expect(fr.Locator("> img")).ToHaveAttributeAsync("alt", "");

        var named = Page.Locator("#flag-click-named");
        await Expect(named).ToHaveAccessibleNameAsync("Pick Japan");
        await Expect(named).ToHaveAttributeAsync("aria-label", "Pick Japan");

        // A flag that does nothing has no role and is not a tab stop.
        await Expect(Page.Locator("#flag-basic")).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
    }

    [TestMethod]
    public async Task OnClick_PointerClick_FiresOnceWithTheMouseArgs()
    {
        await OpenAsync("/e2e/flag");

        await Page.Locator("#flag-click-fr").ClickAsync();
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("France");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-detail")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#flag-click-fr")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#flag-click-fr")).Not.ToHaveClassAsync(HasClass("bit-flg-gry"));
        await Expect(Page.Locator("#flag-click-de")).ToHaveClassAsync(HasClass("bit-flg-gry"));

        await Page.Locator("#flag-click-de").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("Germany");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#last-shift")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task OnClick_EnterAndSpace_ActivateTheFlagOnceEach()
    {
        await OpenAsync("/e2e/flag");

        var de = Page.Locator("#flag-click-de");
        await de.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("Germany");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-detail")).ToHaveTextAsync("0");

        await Page.Locator("#flag-click-fr").FocusAsync();
        await Page.Keyboard.DownAsync(" ");
        await NextFramesAsync();
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
        await Page.Keyboard.UpAsync(" ");
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("France");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("2");

        await Page.Keyboard.PressAsync("Shift+Enter");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#last-shift")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("a");
        await NextFramesAsync();
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task OnClick_Space_DoesNotScrollThePage()
    {
        await OpenAsync("/e2e/flag");

        var fr = Page.Locator("#flag-click-fr");
        await fr.ScrollIntoViewIfNeededAsync();
        await fr.FocusAsync();
        var before = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
        await NextFramesAsync();
        Assert.AreEqual(before, await Page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [TestMethod]
    public async Task OnClick_SpaceReleasedAfterTheFocusLeft_DoesNotActivate()
    {
        await OpenAsync("/e2e/flag");

        await Page.Locator("#flag-click-fr").FocusAsync();
        await Page.Keyboard.DownAsync(" ");
        await NextFramesAsync();
        await Page.Locator("#flag-focusable").FocusAsync();
        await Page.Keyboard.UpAsync(" ");
        await NextFramesAsync();

        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("0");

        // And the next press on the flag starts afresh.
        await Page.Locator("#flag-click-fr").FocusAsync();
        await Page.Keyboard.UpAsync(" ");
        await NextFramesAsync();
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task OnClick_HitBox_GrowsTheTargetTo24PixelsWithoutGrowingTheFlag()
    {
        await OpenAsync("/e2e/flag");

        var small = Page.Locator("#flag-click-small");
        await small.ScrollIntoViewIfNeededAsync();
        var box = (await small.BoundingBoxAsync())!;
        Assert.AreEqual(16, box.Width, 0.5);

        // 3px left of the flag: outside its box, inside the 24px target around it.
        await Page.Mouse.ClickAsync(box.X - 3, box.Y + box.Height / 2);
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("Spain");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OnClick_HoverAndPress_DimTheFlag()
    {
        await OpenAsync("/e2e/flag");

        var de = Page.Locator("#flag-click-de");
        await Expect(de).ToHaveCSSAsync("opacity", "1");
        await de.HoverAsync();
        await Expect(de).ToHaveCSSAsync("opacity", "0.8");
        await Page.Mouse.DownAsync();
        await Expect(de).ToHaveCSSAsync("opacity", "0.6");
        await Page.Mouse.UpAsync();
        await Expect(de).ToHaveCSSAsync("opacity", "0.8");
    }

    [TestMethod]
    public async Task OnClick_KeyboardFocus_DrawsTheFocusRing()
    {
        await OpenAsync("/e2e/flag");

        await Page.Locator("#flag-click-fr").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#flag-click-de")).ToBeFocusedAsync();
        await Expect(Page.Locator("#flag-click-de")).Not.ToHaveCSSAsync("box-shadow", "none");
        await Expect(Page.Locator("#flag-click-fr")).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task Disabled_RefusesClicksAndKeys()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-click-disabled");
        await Expect(flag).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(flag).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(flag).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(flag).ToHaveRoleAsync(AriaRole.Button);
        await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-flg-clk"));
        await Expect(flag).ToHaveAttributeAsync("title", "Not shipped to Portugal yet");
        Assert.IsTrue(double.Parse(await ComputedStyleAsync("#flag-click-disabled", "opacity"), System.Globalization.CultureInfo.InvariantCulture) < 1);

        await flag.ClickAsync(new() { Force = true });
        await flag.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync(" ");
        await NextFramesAsync();

        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("(none)");
    }

    [TestMethod]
    public async Task Disabled_DropsThePagesActivationHandlersAndEnabledRunsThem()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-page-handler");
        await Expect(flag).ToHaveClassAsync(HasClass("bit-dis"));

        // A real click, a double click and a key: none of the page's activation handlers run, the hover one does.
        await flag.ClickAsync(new() { Force = true });
        await flag.DblClickAsync(new() { Force = true });
        await flag.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#page-hovers")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-keys")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-onclicks")).ToHaveTextAsync("0");

        // Enabled, every handler of the page runs - its onclick and onkeydown too: a flag with no OnClick writes no
        // handler of its own over them.
        await Page.Locator("#btn-toggle-page-handler").ClickAsync();
        await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await flag.ClickAsync();
        await Expect(Page.Locator("#page-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#page-onclicks")).ToHaveTextAsync("1");
        await flag.DblClickAsync();
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("1");
        await flag.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#page-keys")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_ReEnabledAtRuntime_ActivatesAndRunsThePagesHandlers()
    {
        await OpenAsync("/e2e/flag");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();

        var flag = Page.Locator("#flag-click-disabled");
        await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(flag).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
        await Expect(flag).ToHaveAttributeAsync("tabindex", "0");
        await Expect(flag).ToHaveClassAsync(HasClass("bit-flg-clk"));

        await flag.ClickAsync();
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("Portugal");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(flag).ToHaveClassAsync(HasClass("bit-dis"));
        await flag.ClickAsync(new() { Force = true });
        await NextFramesAsync();
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task TabOrder_SkipsDisabledAndNegativeTabIndexFlagsAndReachesATabIndexedOne()
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator("#flag-click-tabindex")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Page.Locator("#flag-focusable")).ToHaveAttributeAsync("tabindex", "0");

        await Page.Locator("#flag-click-small").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#flag-focusable")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusAsync_MovesTheFocusToTheFlag()
    {
        await OpenAsync("/e2e/flag");

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(Page.Locator("#flag-click-named")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("Japan");
    }

    [TestMethod]
    public async Task AllCountries_EveryCountryHasAPackagedFlagThatLoads()
    {
        await OpenAsync("/e2e/flag");

        var count = int.Parse(await Page.Locator("#all-count").InnerTextAsync());
        Assert.IsTrue(count > 200);
        await Expect(Page.Locator("#all-flags .bit-flg")).ToHaveCountAsync(count);
        await Expect(Page.Locator("#all-flags .bit-flg > img")).ToHaveCountAsync(count);

        var failed = await Page.EvaluateAsync<string[]>(@"async () => {
            const srcs = [...document.querySelectorAll('#all-flags .bit-flg > img')].map(i => i.getAttribute('src'));
            const results = await Promise.all(srcs.map(async s => (await fetch(s)).ok ? null : s));
            return results.filter(r => r);
        }");
        Assert.AreEqual(0, failed.Length, string.Join(", ", failed));
    }

    [TestMethod]
    public async Task Params_BitFlagParams_SetDefaultsAndAnOwnValueWins()
    {
        await OpenAsync("/e2e/flag");

        var ca = Page.Locator("#flag-params-ca");
        await Expect(ca).ToHaveClassAsync(HasClass("bit-flg-rnd"));
        await Expect(ca).ToHaveClassAsync(HasClass("bit-flg-brd"));
        await Expect(ca).ToHaveClassAsync(HasClass("params-class"));
        await Expect(ca).ToHaveAttributeAsync("title", "Canada");
        await Expect(ca.Locator("> img")).ToHaveAttributeAsync("alt", "Canada");
        var (_, h) = await SizeAsync("#flag-params-ca");
        Assert.AreEqual(32, h, 0.5);

        var own = Page.Locator("#flag-params-own");
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-flg-rnd"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-flg-brd"));
        var (_, h2) = await SizeAsync("#flag-params-own");
        Assert.AreEqual(24, h2, 0.5);
    }

    [DataTestMethod]
    [DataRow("sm", 12d)]
    [DataRow("md", 16d)]
    [DataRow("lg", 20d)]
    public async Task Size_EachValue_SetsTheIconSize(string size, double px)
    {
        await OpenAsync("/e2e/flag");

        await Expect(Page.Locator($"#flag-size-{size}")).ToHaveClassAsync(HasClass($"bit-flg-{size}"));

        // Bordered, so the frame is cut to the flag: the size is its height, the width follows the flag's 14:11.
        var (w, h) = await SizeAsync($"#flag-size-{size}");
        Assert.AreEqual(px, h, 0.5);
        Assert.AreEqual(px * 14 / 11, w, 0.6);
    }

    [TestMethod]
    public async Task Size_WidthAndHeight_WinOverIt()
    {
        await OpenAsync("/e2e/flag");

        var (w, h) = await SizeAsync("#flag-size-height-wins");
        Assert.AreEqual(40, w, 0.5);
        Assert.AreEqual(40, h, 0.5);

        var (w2, h2) = await SizeAsync("#flag-size-width");
        Assert.AreEqual(24, w2, 0.5);
        Assert.AreEqual(24, h2, 0.5);
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheFrameAndEachPart()
    {
        await OpenAsync("/e2e/flag");

        var (w, h) = await SizeAsync("#flag-style");
        Assert.AreEqual(48, w, 0.5);
        Assert.AreEqual(32, h, 0.5);
        Assert.AreEqual("2px", await ComputedStyleAsync("#flag-style", "border-top-width"));
        Assert.AreEqual("rgb(30, 144, 255)", await ComputedStyleAsync("#flag-style", "border-top-color"));

        await Expect(Page.Locator("#flag-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Page.Locator("#flag-class")).ToHaveClassAsync(HasClass("bit-flg"));

        var (sw, sh) = await SizeAsync("#flag-styles");
        Assert.AreEqual(48, sw, 0.5);
        Assert.AreEqual(32, sh, 0.5);
        Assert.AreEqual("contain", await ComputedStyleAsync("#flag-styles > img", "object-fit"));

        await Expect(Page.Locator("#flag-classes")).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(Page.Locator("#flag-classes > .bit-flg-fbk")).ToHaveClassAsync(HasClass("custom-fallback"));
        Assert.AreEqual("rgb(255, 0, 0)", await ComputedStyleAsync("#flag-classes > .bit-flg-fbk", "color"));

        await Expect(Page.Locator("#flag-classes-img > img")).ToHaveClassAsync(HasClass("custom-image"));
        await Expect(Page.Locator("#flag-classes-emoji > .bit-flg-emj")).ToHaveClassAsync(HasClass("custom-emoji"));
        Assert.AreEqual("rgb(0, 128, 0)", await ComputedStyleAsync("#flag-classes-emoji > .bit-flg-emj", "color"));
    }

    [TestMethod]
    public async Task CssVariables_OnOneFlag_RestyleItsShape()
    {
        await OpenAsync("/e2e/flag");

        Assert.AreEqual("6px", await ComputedStyleAsync("#flag-vars", "border-top-left-radius"));
        Assert.AreEqual("3px", await ComputedStyleAsync("#flag-vars", "border-top-width"));
        Assert.AreEqual("rgb(220, 20, 60)", await ComputedStyleAsync("#flag-vars", "border-top-color"));
        StringAssert.Contains(await ComputedStyleAsync("#flag-vars", "box-shadow"), "rgb(0, 0, 255) 0px 0px 0px 5px");
    }

    [TestMethod]
    public async Task CssVariables_OnAContainer_ReachEveryFlagInsideAndSizeLosesToTheParameter()
    {
        await OpenAsync("/e2e/flag");

        var (w, h) = await SizeAsync("#flag-var-size");
        Assert.AreEqual(32, w, 0.5);
        Assert.AreEqual(32, h, 0.5);

        var (sw, _) = await SizeAsync("#flag-var-sized");
        Assert.AreEqual(12, sw, 0.5, "Size wins over --bit-Flag-size.");

        Assert.AreEqual("grayscale(1) opacity(0.4)", await ComputedStyleAsync("#flag-var-gray", "filter"));
        StringAssert.Contains(await ComputedStyleAsync("#flag-var-emoji > .bit-flg-emj", "font-family"), "monospace");

        // Focused before any pointer interaction, so the focus is a keyboard-visible one.
        var click = Page.Locator("#flag-var-click");
        await click.FocusAsync();
        await Expect(click).ToBeFocusedAsync();
        StringAssert.Contains(await ComputedStyleAsync("#flag-var-click", "box-shadow"), "rgb(255, 0, 0)");

        await click.HoverAsync();
        await Expect(click).ToHaveCSSAsync("opacity", "0.5");
        await Page.Mouse.DownAsync();
        await Expect(click).ToHaveCSSAsync("opacity", "0.25");
        await Page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task Rtl_Dir_FlipsTheRowNeverTheFlag()
    {
        await OpenAsync("/e2e/flag");

        var rtl = Page.Locator("#flag-rtl");
        await Expect(rtl).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(rtl).ToHaveAttributeAsync("dir", "rtl");
        Assert.AreEqual("none", await ComputedStyleAsync("#flag-rtl > img", "transform"));
        Assert.AreEqual("none", await ComputedStyleAsync("#flag-rtl", "transform"));
        Assert.AreEqual("none", await ComputedStyleAsync("#flag-rtl > img", "scale"));

        var inherited = Page.Locator("#flag-rtl-inherited");
        await Expect(inherited).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.AreEqual("none", await ComputedStyleAsync("#flag-rtl-inherited > img", "transform"));
        Assert.IsTrue(await inherited.EvaluateAsync<bool>("el => el.matches(':dir(rtl)')"));

        // The row reads right to left: the first flag sits at its right end.
        var flagBox = (await rtl.BoundingBoxAsync())!;
        var textBox = (await Page.Locator("#rtl-text").BoundingBoxAsync())!;
        Assert.IsTrue(flagBox.X > textBox.X);

        await Expect(Page.Locator("#flag-ltr")).ToHaveAttributeAsync("dir", "ltr");
        await Expect(Page.Locator("#flag-ltr")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
    }

    [TestMethod]
    public async Task Base_VisibilityAndHtmlAttributes()
    {
        await OpenAsync("/e2e/flag");

        Assert.AreEqual("hidden", await ComputedStyleAsync("#flag-hidden", "visibility"));
        await Expect(Page.Locator("#flag-hidden")).ToBeHiddenAsync();
        Assert.AreEqual("none", await ComputedStyleAsync("#flag-collapsed", "display"));
        await Expect(Page.Locator("#flag-attrs")).ToHaveAttributeAsync("data-test", "flag-data");
        await Expect(Page.Locator("#flag-basic")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Runtime_ParameterChanges_ReRenderTheFlag()
    {
        await OpenAsync("/e2e/flag");

        var flag = Page.Locator("#flag-runtime");
        await Expect(flag.Locator("> img")).ToHaveAttributeAsync("src", $"{PackagedPath}DE-flat-16.webp");
        await Expect(flag).ToHaveAttributeAsync("title", "Germany");

        await Page.Locator("#btn-runtime-country").ClickAsync();
        await Expect(flag.Locator("> img")).ToHaveAttributeAsync("src", $"{PackagedPath}FR-flat-16.webp");
        await Expect(flag).ToHaveAttributeAsync("title", "France");
        await Expect(flag.Locator("> img")).ToHaveAttributeAsync("alt", "France");

        await Page.Locator("#btn-runtime-rounded").ClickAsync();
        await Expect(flag).ToHaveClassAsync(HasClass("bit-flg-rnd"));
        await Expect(flag).ToHaveClassAsync(HasClass("bit-flg-crp"));

        await Page.Locator("#btn-runtime-emoji").ClickAsync();
        await Expect(flag.Locator("> .bit-flg-emj")).ToHaveTextAsync("\U0001F1EB\U0001F1F7");
        await Expect(flag.Locator("> img")).ToHaveCountAsync(0);
        await Expect(flag).ToHaveRoleAsync(AriaRole.Img);
        await Expect(flag).ToHaveAccessibleNameAsync("France");
        await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-flg-crp"));

        await Page.Locator("#btn-runtime-emoji").ClickAsync();
        await Page.Locator("#btn-runtime-rounded").ClickAsync();
        await Expect(flag.Locator("> img")).ToHaveAttributeAsync("src", $"{PackagedPath}FR-flat-16.webp");
        await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-flg-rnd"));
        await Expect(flag).Not.ToHaveClassAsync(HasClass("bit-flg-crp"));
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheHoverTransitionAndForceAnimationRestoresIt()
    {
        await OpenAsync("/e2e/flag");

        Assert.AreEqual(0.1, Seconds(await ComputedStyleAsync("#flag-motion", "transition-duration")), 0.0001);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.IsTrue(Seconds(await ComputedStyleAsync("#flag-motion", "transition-duration")) < 0.001);
        Assert.AreEqual(0.1, Seconds(await ComputedStyleAsync("#flag-motion-forced", "transition-duration")), 0.0001);
        await Expect(Page.Locator("#flag-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
    }

    [TestMethod]
    public async Task Lifecycle_Prerendered_TheServerHtmlCarriesTheFlag()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/flag");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"flag-basic\"");
        StringAssert.Contains(html, $"{PackagedPath}IR-flat-16.webp");
        StringAssert.Contains(html, "id=\"status\">Loading<");
    }

    [TestMethod]
    public async Task Lifecycle_Unmounted_LeavesNoErrorBehind()
    {
        await OpenAsync("/e2e/flag");

        await Page.Locator("#flag-unmount-click").FocusAsync();
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#flag-unmount-click")).ToHaveCountAsync(0);

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Page.Locator("#flag-unmount-click")).ToHaveCountAsync(1);
        await Page.Locator("#flag-unmount-click").ClickAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task RouteImageSetsAsync()
    {
        // The image sets ship in Bit.BlazorUI.Assets, which the host does not carry: every request of a set image
        // is answered with a packaged flag image, so the set's urls load and stay drawn.
        var packaged = await (await Page.APIRequest.GetAsync($"{BaseUrl}/{PackagedPath}BR-flat-16.webp")).BodyAsync();
        await Page.RouteAsync("**/_content/Bit.BlazorUI.Assets/flags/**", route =>
            route.FulfillAsync(new() { Status = 200, ContentType = "image/webp", BodyBytes = packaged }));
    }

    private Task WaitForImageAsync(string selector)
    {
        return Page.WaitForFunctionAsync($"() => {{ const i = document.querySelector('{selector}'); return i && i.complete && i.naturalWidth > 0; }}");
    }

    private async Task<(double Width, double Height)> SizeAsync(string selector)
    {
        var size = await Page.Locator(selector).First.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.width, r.height]; }");
        return (size[0], size[1]);
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", string.Empty), System.Globalization.CultureInfo.InvariantCulture);

    private static double Seconds(string value)
    {
        var first = value.Split(',')[0].Trim();
        return first.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(first[..^2], System.Globalization.CultureInfo.InvariantCulture) / 1000
            : double.Parse(first[..^1], System.Globalization.CultureInfo.InvariantCulture);
    }
}
