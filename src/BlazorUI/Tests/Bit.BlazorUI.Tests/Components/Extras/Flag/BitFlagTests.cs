using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Flag;

[TestClass]
public class BitFlagTests : BunitTestContext
{
    [TestMethod]
    public void BitFlagShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitFlag>();

        var root = component.Find(".bit-flg");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual(0, component.FindAll(".bit-flg-emj").Count);
        Assert.IsFalse(root.HasAttribute("role"));
        Assert.IsFalse(root.HasAttribute("tabindex"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitFlagShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitFlagShouldRespectTitle()
    {
        const string title = "Netherlands Flag";

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        Assert.AreEqual(title, component.Find(".bit-flg").GetAttribute("title"));
    }



    // ---------------------------------------------------------------- resolving the country

    [TestMethod]
    public void BitFlagShouldRenderFromCountry()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/NL-flat-16.webp");
    }

    [TestMethod,
        DataRow("ca"),
        DataRow("CA"),
        DataRow("Ca"),
        DataRow(" ca ")]
    public void BitFlagShouldRenderFromIso2(string iso2)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, iso2);
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/CA-flat-16.webp");
    }

    [TestMethod,
        DataRow("usa"),
        DataRow("USA"),
        DataRow(" Usa ")]
    public void BitFlagShouldRenderFromIso3(string iso3)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso3, iso3);
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/US-flat-16.webp");
    }

    [TestMethod,
        DataRow("81"),
        DataRow("+81"),
        DataRow("0081"),
        DataRow(" +81 "),
        DataRow("(81)")]
    public void BitFlagShouldRenderFromCode(string code)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Code, code);
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/JP-flat-16.webp");
    }

    [TestMethod,
        DataRow("Netherlands"),
        DataRow("netherlands"),
        DataRow(" Netherlands ")]
    public void BitFlagShouldRenderFromName(string name)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Name, name);
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/NL-flat-16.webp");
    }

    [TestMethod]
    public void BitFlagShouldPreferCountryOverEveryOtherWayOfNamingOne()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.Iso2, "ca");
            parameters.Add(p => p.Iso3, "USA");
            parameters.Add(p => p.Code, "81");
            parameters.Add(p => p.Name, "Brazil");
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/NL-flat-16.webp");
    }

    [TestMethod]
    public void BitFlagShouldPreferIso2OverIso3AndCodeAndName()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "ca");
            parameters.Add(p => p.Iso3, "USA");
            parameters.Add(p => p.Code, "81");
            parameters.Add(p => p.Name, "Brazil");
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/CA-flat-16.webp");
    }

    [TestMethod]
    public void BitFlagShouldPreferIso3OverCodeAndName()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso3, "USA");
            parameters.Add(p => p.Code, "81");
            parameters.Add(p => p.Name, "Brazil");
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/US-flat-16.webp");
    }

    [TestMethod]
    public void BitFlagShouldPreferCodeOverName()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Code, "81");
            parameters.Add(p => p.Name, "Brazil");
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/JP-flat-16.webp");
    }

    [TestMethod]
    public void BitFlagShouldRenderACountryOfItsOwnMakingFromItsIso2()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, new BitCountry("Kosovo", "383", "XK", "XKX"));
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/XK-flat-16.webp");
    }

    [TestMethod,
        DataRow("zz"),
        DataRow("z"),
        DataRow("")]
    public void BitFlagShouldRenderNoImageForAnUnknownIso2(string iso2)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, iso2);
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
    }

    [TestMethod]
    public void BitFlagShouldRenderNoImageWhenCountryNotFound()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Code, "0000");
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
    }

    [TestMethod]
    public void BitFlagShouldRenderNoImageForACountryWithAnEmptyIso2()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, new BitCountry("Nowhere", "0", string.Empty, string.Empty));
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
    }

    [TestMethod]
    public void BitFlagShouldFollowTheCountryChangingAfterRender()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/NL-flat-16.webp");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/JP-flat-16.webp");
    }



    // ---------------------------------------------------------------- the image itself

    [TestMethod]
    public void BitFlagShouldLoadItsImageLazilyByDefault()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
        });

        var image = component.Find("img");

        Assert.AreEqual("lazy", image.GetAttribute("loading"));
        Assert.AreEqual("async", image.GetAttribute("decoding"));
    }

    [TestMethod,
        DataRow(BitImageLoading.Eager, "eager"),
        DataRow(BitImageLoading.Lazy, "lazy")]
    public void BitFlagShouldRespectLoading(BitImageLoading loading, string expected)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Loading, loading);
        });

        Assert.AreEqual(expected, component.Find("img").GetAttribute("loading"));
    }

    [TestMethod]
    public void BitFlagShouldRespectSrc()
    {
        const string src = "/my-flags/nl.svg";

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Src, src);
        });

        Assert.AreEqual(src, component.Find("img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitFlagShouldRenderTheSrcEvenWithoutACountry()
    {
        const string src = "/my-flags/unknown.svg";

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Src, src);
        });

        Assert.AreEqual(src, component.Find("img").GetAttribute("src"));
    }



    // ---------------------------------------------------------------- the fallback

    [TestMethod]
    public void BitFlagShouldRenderTheFallbackTemplateWhenTheCountryIsUnknown()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "zz");
            parameters.Add(p => p.FallbackTemplate, (RenderFragment)(builder => builder.AddContent(0, "?")));
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual("?", component.Find(".bit-flg-fbk").TextContent);
    }

    [TestMethod]
    public void BitFlagShouldNotRenderTheFallbackTemplateWhileThereIsAFlag()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.FallbackTemplate, (RenderFragment)(builder => builder.AddContent(0, "?")));
        });

        Assert.AreEqual(1, component.FindAll("img").Count);
        Assert.AreEqual(0, component.FindAll(".bit-flg-fbk").Count);
    }

    [TestMethod]
    public void BitFlagShouldFallBackWhenTheImageFailsToLoad()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Src, "/not-there.png");
            parameters.Add(p => p.FallbackTemplate, (RenderFragment)(builder => builder.AddContent(0, "!")));
        });

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual("!", component.Find(".bit-flg-fbk").TextContent);
    }

    [TestMethod]
    public void BitFlagShouldTryAgainWhenTheSourceChangesAfterAnError()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Src, "/not-there.png");
        });

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.AreEqual(0, component.FindAll("img").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Src, "/there.png");
        });

        Assert.AreEqual("/there.png", component.Find("img").GetAttribute("src"));
    }



    // ---------------------------------------------------------------- the emoji flag

    [TestMethod]
    public void BitFlagShouldRenderTheEmojiFlag()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Country, BitCountries.Netherlands);
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual("\U0001F1F3\U0001F1F1", component.Find(".bit-flg-emj").TextContent);
        Assert.AreEqual("true", component.Find(".bit-flg-emj").GetAttribute("aria-hidden"));
        Assert.IsTrue(component.Find(".bit-flg").ClassList.Contains("bit-flg-emo"));
    }

    [TestMethod]
    public void BitFlagShouldRenderTheEmojiFlagOfACodeThePackagedImagesDoNotCover()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Iso2, "xk");
        });

        Assert.AreEqual("\U0001F1FD\U0001F1F0", component.Find(".bit-flg-emj").TextContent);
    }

    [TestMethod]
    public void BitFlagShouldPreferTheEmojiOverTheSrc()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Src, "/my-flags/nl.svg");
            parameters.Add(p => p.Iso2, "nl");
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual(1, component.FindAll(".bit-flg-emj").Count);
    }

    [TestMethod]
    public void BitFlagShouldRenderNoEmojiWhenTheCountryIsUnknownAndTheCodeIsNotTwoLetters()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Code, "0000");
        });

        Assert.AreEqual(0, component.FindAll(".bit-flg-emj").Count);
        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-emo"));
    }



    // ---------------------------------------------------------------- naming the flag

    [TestMethod]
    public void BitFlagShouldBeDecorativeByDefault()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
        });

        Assert.AreEqual(string.Empty, component.Find("img").GetAttribute("alt"));
        Assert.IsFalse(component.Find(".bit-flg").HasAttribute("role"));
    }

    [TestMethod]
    public void BitFlagShouldRespectAlt()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.Alt, "Ships to the Netherlands");
        });

        Assert.AreEqual("Ships to the Netherlands", component.Find("img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitFlagShouldRespectAutoAlt()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.AutoAlt, true);
        });

        Assert.AreEqual("Netherlands", component.Find("img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitFlagShouldPreferAltOverAutoAlt()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.AutoAlt, true);
            parameters.Add(p => p.Alt, "Written by the page");
        });

        Assert.AreEqual("Written by the page", component.Find("img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitFlagShouldNameTheImageWithTheAriaLabelWhenThereIsNoAlt()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.AriaLabel, "The Netherlands");
        });

        Assert.AreEqual("The Netherlands", component.Find("img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitFlagShouldStayDecorativeWhenAutoAltHasNoCountryToTakeANameFrom()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Src, "/my-flags/nl.svg");
            parameters.Add(p => p.AutoAlt, true);
        });

        Assert.AreEqual(string.Empty, component.Find("img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitFlagShouldRespectAutoTitle()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.AutoTitle, true);
        });

        Assert.AreEqual("Netherlands", component.Find(".bit-flg").GetAttribute("title"));
    }

    [TestMethod]
    public void BitFlagShouldPreferTitleOverAutoTitle()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.AutoTitle, true);
            parameters.Add(p => p.Title, "Written by the page");
        });

        Assert.AreEqual("Written by the page", component.Find(".bit-flg").GetAttribute("title"));
    }

    [TestMethod]
    public void BitFlagShouldNameTheRootWhileTheEmojiIsWhatIsDrawn()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.AutoAlt, true);
            parameters.Add(p => p.Country, BitCountries.Netherlands);
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual("img", root.GetAttribute("role"));
        Assert.AreEqual("Netherlands", root.GetAttribute("aria-label"));
        Assert.AreEqual("true", component.Find(".bit-flg-emj").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitFlagShouldLeaveAnUnnamedEmojiFlagOutOfTheAccessibilityTree()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Country, BitCountries.Netherlands);
        });

        var root = component.Find(".bit-flg");

        Assert.IsFalse(root.HasAttribute("role"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
    }



    // ---------------------------------------------------------------- the clickable flag

    [TestMethod]
    public async Task BitFlagShouldRespectOnClick()
    {
        var clicked = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual("button", root.GetAttribute("role"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
        Assert.IsTrue(root.ClassList.Contains("bit-flg-clk"));

        await root.ClickAsync(new MouseEventArgs());

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public async Task BitFlagShouldNotRespondToAClickWhileDisabled()
    {
        var clicked = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));
        Assert.IsFalse(root.ClassList.Contains("bit-flg-clk"));

        await root.ClickAsync(new MouseEventArgs());

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public async Task BitFlagShouldActivateOnEnter()
    {
        var clicked = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        await component.Find(".bit-flg").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public async Task BitFlagShouldActivateOnTheSpaceItSawPressed()
    {
        var clicked = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        var root = component.Find(".bit-flg");

        await root.KeyUpAsync(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(0, clicked, "a Space released without having been pressed here is not an activation");

        await root.KeyDownAsync(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(0, clicked, "a Space activates on the way up rather than on the way down");

        await root.KeyUpAsync(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public async Task BitFlagShouldForgetAHeldSpaceOnBlur()
    {
        var clicked = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        var root = component.Find(".bit-flg");

        await root.KeyDownAsync(new KeyboardEventArgs { Key = " " });
        await root.BlurAsync(new FocusEventArgs());
        await root.KeyUpAsync(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public async Task BitFlagShouldNotAnswerTheKeyboardWhileDisabled()
    {
        var clicked = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        var root = component.Find(".bit-flg");

        await root.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        await root.KeyDownAsync(new KeyboardEventArgs { Key = " " });
        await root.KeyUpAsync(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitFlagShouldNameTheButtonRatherThanThePictureInsideIt()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.AutoAlt, true);
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.OnClick, () => { });
        });

        Assert.AreEqual("Netherlands", component.Find(".bit-flg").GetAttribute("aria-label"));
        Assert.AreEqual(string.Empty, component.Find("img").GetAttribute("alt"));
    }


    [TestMethod]
    public void BitFlagShouldNameAnUnnamedButtonWithTheCountryItShows()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
            parameters.Add(p => p.OnClick, () => { });
        });

        Assert.AreEqual("Netherlands", component.Find(".bit-flg").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFlagShouldLeaveAnUnnamedButtonWithNoCountryUnnamed()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Src, "/my-flags/nl.svg");
            parameters.Add(p => p.OnClick, () => { });
        });

        Assert.IsFalse(component.Find(".bit-flg").HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFlagShouldRespectTabIndexWithoutAClickHandler()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.Country, BitCountries.Netherlands);
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual("3", root.GetAttribute("tabindex"));
        Assert.IsFalse(root.HasAttribute("role"));
    }



    // ---------------------------------------------------------------- the shape and the size

    [TestMethod,
        DataRow(BitSize.Small, "bit-flg-sm"),
        DataRow(BitSize.Medium, "bit-flg-md"),
        DataRow(BitSize.Large, "bit-flg-lg")]
    public void BitFlagShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-flg").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitFlagShouldRespectWidthAndHeight()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Width, "4rem");
            parameters.Add(p => p.Height, "2rem");
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "width:4rem");
        StringAssert.Contains(style, "height:2rem");
        StringAssert.Contains(style, "--bit-flg-siz:2rem");
    }

    [TestMethod]
    public void BitFlagShouldSizeItselfFromAWidthAlone()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Width, "4rem");
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-flg-siz:4rem");
        Assert.IsFalse(style.Contains("height:"));
    }

    [TestMethod]
    public void BitFlagShouldRespectRounded()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Rounded, true);
        });

        Assert.IsTrue(component.Find(".bit-flg").ClassList.Contains("bit-flg-rnd"));
    }

    [TestMethod]
    public void BitFlagShouldPreferCircularOverRounded()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Rounded, true);
            parameters.Add(p => p.Circular, true);
        });

        var classList = component.Find(".bit-flg").ClassList;

        Assert.IsTrue(classList.Contains("bit-flg-cir"));
        Assert.IsFalse(classList.Contains("bit-flg-rnd"));
    }

    [TestMethod]
    public void BitFlagShouldRespectBorderedAndShadowAndGrayscale()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Bordered, true);
            parameters.Add(p => p.Shadow, true);
            parameters.Add(p => p.Grayscale, true);
        });

        var classList = component.Find(".bit-flg").ClassList;

        Assert.IsTrue(classList.Contains("bit-flg-brd"));
        Assert.IsTrue(classList.Contains("bit-flg-shd"));
        Assert.IsTrue(classList.Contains("bit-flg-gry"));
    }

    [TestMethod]
    public void BitFlagShouldFollowTheShapeChangingAfterRender()
    {
        var component = RenderComponent<BitFlag>();

        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-cir"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Circular, true);
        });

        Assert.IsTrue(component.Find(".bit-flg").ClassList.Contains("bit-flg-cir"));
    }



    // ---------------------------------------------------------------- styling and the root element

    [TestMethod]
    public void BitFlagShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Style, "padding:1rem");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = component.Find(".bit-flg");

        StringAssert.Contains(root.GetAttribute("style"), "padding:1rem");
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
    }

    [TestMethod]
    public void BitFlagShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Classes, new BitFlagClassStyles { Root = "custom-root", Image = "custom-image" });
            parameters.Add(p => p.Styles, new BitFlagClassStyles { Root = "opacity:0.5", Image = "object-fit:contain" });
        });

        var root = component.Find(".bit-flg");
        var image = component.Find("img");

        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style"), "opacity:0.5");
        Assert.IsTrue(image.ClassList.Contains("custom-image"));
        StringAssert.Contains(image.GetAttribute("style"), "object-fit:contain");
    }

    [TestMethod]
    public void BitFlagShouldRespectClassesForTheEmojiAndTheFallback()
    {
        var emoji = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Classes, new BitFlagClassStyles { Emoji = "custom-emoji" });
        });

        Assert.IsTrue(emoji.Find(".bit-flg-emj").ClassList.Contains("custom-emoji"));

        var fallback = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "zz");
            parameters.Add(p => p.Classes, new BitFlagClassStyles { Fallback = "custom-fallback" });
            parameters.Add(p => p.FallbackTemplate, (RenderFragment)(builder => builder.AddContent(0, "?")));
        });

        Assert.IsTrue(fallback.Find(".bit-flg-fbk").ClassList.Contains("custom-fallback"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, ""),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitFlagShouldRespectVisibility(BitVisibility visibility, string expected)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        if (expected.HasValue())
        {
            StringAssert.Contains(style, expected);
        }
        else
        {
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
    }

    [TestMethod,
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Auto, "auto")]
    public void BitFlagShouldRespectDir(BitDir dir, string expected)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitFlagShouldRespectId()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Id, "my-flag");
        });

        Assert.AreEqual("my-flag", component.Find(".bit-flg").GetAttribute("id"));
    }

    [TestMethod,
        DataRow("data-country", "nl"),
        DataRow("aria-describedby", "hint")
    ]
    public void BitFlagShouldRespectHtmlAttributes(string name, string value)
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes (as real markup would) rather than via the builder,
        // which rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitFlag>(0);
            builder.AddAttribute(1, name, value);
            builder.CloseComponent();
        });

        Assert.AreEqual(value, component.Find(".bit-flg").GetAttribute(name));
    }

    [TestMethod]
    public void BitFlagShouldLeaveASplattedTitleAndRoleAlone()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitFlag>(0);
            builder.AddAttribute(1, nameof(BitFlag.Iso2), "nl");
            builder.AddAttribute(2, "title", "Splatted title");
            builder.AddAttribute(3, "role", "presentation");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual("Splatted title", root.GetAttribute("title"));
        Assert.AreEqual("presentation", root.GetAttribute("role"));
    }

    [TestMethod]
    public void BitFlagShouldLeaveASplattedAriaLabelToNameTheFlag()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitFlag>(0);
            builder.AddAttribute(1, nameof(BitFlag.Iso2), "nl");
            builder.AddAttribute(2, "aria-label", "The Netherlands");
            builder.CloseComponent();
        });

        Assert.AreEqual("The Netherlands", component.Find("img").GetAttribute("alt"));
        Assert.AreEqual("The Netherlands", component.Find(".bit-flg").GetAttribute("aria-label"));
    }
}
