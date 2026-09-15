using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        DataRow("UK"),
        DataRow("uk")]
    public void BitFlagShouldRenderTheReservedUkCodeAsTheUnitedKingdom(string iso2)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, iso2);
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/GB-flat-16.webp");
    }

    [TestMethod,
        DataRow("Czechia", "CZ"),
        DataRow("Holland", "NL"),
        DataRow("USA", "US"),
        DataRow("Curaçao", "CW"),
        DataRow("Guinea-Bissau", "GW")]
    public void BitFlagShouldRenderFromAnAlternativeNameOrSpelling(string name, string expectedIso2)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Name, name);
        });

        StringAssert.Contains(component.Find("img").GetAttribute("src"), $"flags/{expectedIso2}-flat-16.webp");
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

    [TestMethod]
    public void BitFlagShouldRespectImageAttributes()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageAttributes, new Dictionary<string, object>
            {
                { "referrerpolicy", "no-referrer" },
                // Written before everything the flag decides, so the flag's own src still wins.
                { "src", "/hijacked.png" }
            });
        });

        var image = component.Find("img");

        Assert.AreEqual("no-referrer", image.GetAttribute("referrerpolicy"));
        StringAssert.Contains(image.GetAttribute("src"), "flags/NL-flat-16.webp");
    }

    [TestMethod]
    public void BitFlagShouldDefaultTheDraggableAndDecodingOfItsImage()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
        });

        var image = component.Find("img");

        Assert.AreEqual("false", image.GetAttribute("draggable"));
        Assert.AreEqual("async", image.GetAttribute("decoding"));
    }

    [TestMethod]
    public void BitFlagShouldLetTheImageAttributesOverrideTheDefaultsOfItsImage()
    {
        // The two the flag only defaults, rather than decides, are left exactly as the page gave them.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageAttributes, new Dictionary<string, object>
            {
                { "draggable", "true" },
                { "decoding", "sync" }
            });
        });

        var image = component.Find("img");

        Assert.AreEqual("true", image.GetAttribute("draggable"));
        Assert.AreEqual("sync", image.GetAttribute("decoding"));
    }

    [TestMethod]
    public async Task BitFlagShouldRespectOnLoad()
    {
        var loaded = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.OnLoad, () => loaded++);
        });

        await component.Find("img").TriggerEventAsync("onload", new ProgressEventArgs());

        Assert.AreEqual(1, loaded);
    }

    [TestMethod]
    public void BitFlagShouldListenForNoLoadNobodyAskedFor()
    {
        // A list of two hundred flags would otherwise cost a round trip per row of a Blazor Server app
        // the moment they finish arriving.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
        });

        Assert.ThrowsExactly<MissingEventHandlerException>(
            () => component.Find("img").TriggerEvent("onload", new ProgressEventArgs()));
    }

    [TestMethod]
    public async Task BitFlagShouldRespectOnError()
    {
        var failed = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.OnError, () => failed++);
        });

        await component.Find("img").TriggerEventAsync("onerror", new ProgressEventArgs());

        Assert.AreEqual(1, failed);
    }



    // ---------------------------------------------------------------- the image sets

    private const string AssetsFlags = "_content/Bit.BlazorUI.Assets/flags/";

    [TestMethod]
    public void BitFlagShouldDrawThePackagedImageWithoutAnImageSet()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
        });

        var image = component.Find("img");

        Assert.AreEqual("_content/Bit.BlazorUI.Extras/flags/NL-flat-16.webp", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));
    }

    [TestMethod,
        DataRow(BitFlagImageSet.Flat, "flat"),
        DataRow(BitFlagImageSet.Shiny, "shiny")]
    public void BitFlagShouldOfferEverySizeOfTheImageSet(BitFlagImageSet set, string name)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageSet, set);
        });

        var image = component.Find("img");

        // Drawn at the 16 pixels of the medium size, every size of the set is described by how many of its
        // pixels fall on one pixel of the flag, and the browser fetches only the one the screen needs.
        Assert.AreEqual($"{AssetsFlags}NL-{name}-16.webp", image.GetAttribute("src"));
        Assert.AreEqual($"{AssetsFlags}NL-{name}-16.webp 1x, {AssetsFlags}NL-{name}-24.webp 1.5x, " +
                        $"{AssetsFlags}NL-{name}-32.webp 2x, {AssetsFlags}NL-{name}-48.webp 3x, " +
                        $"{AssetsFlags}NL-{name}-64.webp 4x",
                        image.GetAttribute("srcset"));
    }

    [TestMethod,
        DataRow("Small", "16", "1.33x 2x 2.67x 4x 5.33x"),
        DataRow("Large", "24", "0.8x 1.2x 1.6x 2.4x 3.2x"),
        DataRow("Height:3rem", "48", "0.33x 0.5x 0.67x 1x 1.33x"),
        DataRow("Width:20px", "24", "0.8x 1.2x 1.6x 2.4x 3.2x"),
        DataRow("Width:40px;Height:20px", "48", "0.4x 0.6x 0.8x 1.2x 1.6x"),
        DataRow("Height:2rem;AspectRatio:3/2", "48", "0.33x 0.5x 0.67x 1x 1.33x"),
        DataRow("Width:24px;AspectRatio:1/2", "48", "0.33x 0.5x 0.67x 1x 1.33x")]
    public void BitFlagShouldDescribeTheImageSetBySizeOfTheFlag(string sizing, string expectedSrcSize, string expectedDensities)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);

            foreach (var part in sizing.Split(';'))
            {
                var pair = part.Split(':');

                switch (pair[0])
                {
                    case "Small": parameters.Add(p => p.Size, BitSize.Small); break;
                    case "Large": parameters.Add(p => p.Size, BitSize.Large); break;
                    case "Height": parameters.Add(p => p.Height, pair[1]); break;
                    case "Width": parameters.Add(p => p.Width, pair[1]); break;
                    default: parameters.Add(p => p.AspectRatio, pair[1]); break;
                }
            }
        });

        var image = component.Find("img");
        var densities = image.GetAttribute("srcset")!.Split(", ").Select(candidate => candidate[(candidate.LastIndexOf(' ') + 1)..]);

        Assert.AreEqual($"{AssetsFlags}NL-flat-{expectedSrcSize}.webp", image.GetAttribute("src"));
        Assert.AreEqual(expectedDensities, string.Join(" ", densities));
    }

    [TestMethod,
        DataRow("50%"),
        DataRow("2em"),
        DataRow("calc(1rem + 2px)")]
    public void BitFlagShouldDrawTheLargestImageOfTheSetForASizeItCannotRead(string height)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Height, height);
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
        });

        var image = component.Find("img");

        Assert.AreEqual($"{AssetsFlags}NL-flat-64.webp", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));
    }

    [TestMethod,
        DataRow(BitFlagImageSize.Size16, "16"),
        DataRow(BitFlagImageSize.Size24, "24"),
        DataRow(BitFlagImageSize.Size32, "32"),
        DataRow(BitFlagImageSize.Size48, "48"),
        DataRow(BitFlagImageSize.Size64, "64")]
    public void BitFlagShouldDrawTheImageSizeItIsGiven(BitFlagImageSize imageSize, string expectedSize)
    {
        // A flag 3rem tall would otherwise be offered every size and draw the 48 pixel image by default.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Height, "3rem");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Shiny);
            parameters.Add(p => p.ImageSize, imageSize);
        });

        var image = component.Find("img");

        Assert.AreEqual($"{AssetsFlags}NL-shiny-{expectedSize}.webp", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));
    }

    [TestMethod,
        DataRow(BitFlagImageSize.Size32, "32", "--bit-flg-crp-x:1;--bit-flg-crp-y:6;--bit-flg-crp-w:30;--bit-flg-crp-h:20;--bit-flg-crp-s:32"),
        DataRow(BitFlagImageSize.Size16, "16", "--bit-flg-crp-x:1;--bit-flg-crp-y:2;--bit-flg-crp-w:14;--bit-flg-crp-h:11")]
    public void BitFlagShouldCutAShapedFrameToTheImageSizeItIsGiven(BitFlagImageSize imageSize, string expectedSize, string expectedStyle)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Bordered, true);
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
            parameters.Add(p => p.ImageSize, imageSize);
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        Assert.AreEqual($"{AssetsFlags}JP-flat-{expectedSize}.webp", component.Find("img").GetAttribute("src"));
        StringAssert.Contains(style, expectedStyle);

        // The stylesheet already takes a box to be in the pixels of a 16 pixel image.
        Assert.AreEqual(expectedSize != "16", style.Contains("--bit-flg-crp-s"));
    }

    [TestMethod]
    public void BitFlagShouldDrawThePackagedImageForAnImageSizeWithoutAnImageSet()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageSize, BitFlagImageSize.Size64);
        });

        var image = component.Find("img");

        Assert.AreEqual("_content/Bit.BlazorUI.Extras/flags/NL-flat-16.webp", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));
    }

    [TestMethod]
    public void BitFlagShouldFollowTheImageSizeChangingAfterRender()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
            parameters.Add(p => p.ImageSize, BitFlagImageSize.Size24);
        });

        Assert.AreEqual($"{AssetsFlags}NL-flat-24.webp", component.Find("img").GetAttribute("src"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.ImageSize, (BitFlagImageSize?)null);
        });

        var image = component.Find("img");

        Assert.AreEqual($"{AssetsFlags}NL-flat-16.webp", image.GetAttribute("src"));
        Assert.IsTrue(image.HasAttribute("srcset"));
    }

    [TestMethod]
    public void BitFlagShouldTakeTheCascadedImageSet()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<BitFlagImageSet>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<BitFlagImageSet>.Value), BitFlagImageSet.Shiny);
            builder.AddAttribute(2, nameof(CascadingValue<BitFlagImageSet>.ChildContent), (RenderFragment)(child =>
            {
                child.OpenComponent<BitFlag>(0);
                child.AddAttribute(1, nameof(BitFlag.Iso2), "nl");
                child.CloseComponent();

                child.OpenComponent<BitFlag>(2);
                child.AddAttribute(3, nameof(BitFlag.Iso2), "jp");
                child.AddAttribute(4, nameof(BitFlag.ImageSet), (BitFlagImageSet?)BitFlagImageSet.Flat);
                child.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var images = component.FindAll("img");

        Assert.AreEqual($"{AssetsFlags}NL-shiny-16.webp", images[0].GetAttribute("src"));

        // One set on the flag itself wins over the cascaded one.
        Assert.AreEqual($"{AssetsFlags}JP-flat-16.webp", images[1].GetAttribute("src"));
    }

    [TestMethod]
    public void BitFlagShouldFollowTheImageSetChangingAfterRender()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
        });

        Assert.AreEqual($"{AssetsFlags}NL-flat-16.webp", component.Find("img").GetAttribute("src"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.ImageSet, (BitFlagImageSet?)null);
        });

        var image = component.Find("img");

        Assert.AreEqual("_content/Bit.BlazorUI.Extras/flags/NL-flat-16.webp", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));
    }

    [TestMethod]
    public void BitFlagShouldPreferTheSrcOverTheImageSet()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Src, "/my-flags/nl.svg");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
        });

        var image = component.Find("img");

        Assert.AreEqual("/my-flags/nl.svg", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));
    }

    [TestMethod]
    public void BitFlagShouldPreferTheEmojiOverTheImageSet()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual(1, component.FindAll(".bit-flg-emj").Count);
    }

    [TestMethod]
    public void BitFlagShouldNotAskTheImageSetForACountryItDoesNotCover()
    {
        // Asked of the sets, a code they do not cover would only cost a failed request before the packaged
        // image stood in for it.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, new BitCountry("Kosovo", "383", "XK", "XKX"));
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
        });

        var image = component.Find("img");

        Assert.AreEqual("_content/Bit.BlazorUI.Extras/flags/XK-flat-16.webp", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));
    }

    [TestMethod]
    public void BitFlagShouldFallBackToThePackagedFlagWhenTheImageSetFails()
    {
        var failed = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Shiny);
            parameters.Add(p => p.OnError, () => failed++);
            parameters.Add(p => p.FallbackTemplate, (RenderFragment)(builder => builder.AddContent(0, "!")));
        });

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        // The package not being installed is the likeliest reason for it, and the flag that ships with the
        // component is still there to draw.
        var image = component.Find("img");

        Assert.AreEqual("_content/Bit.BlazorUI.Extras/flags/NL-flat-16.webp", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual("!", component.Find(".bit-flg-fbk").TextContent);
        Assert.AreEqual(2, failed);
    }

    [TestMethod]
    public void BitFlagShouldCutAShapedFrameToTheImageOfTheSetThatCoversItTwiceOver()
    {
        // The box a frame is cut to differs from one size of the image to the next, so a cut frame is drawn
        // from one image rather than offered all of them: at the 16 pixels of the medium size, the flag of
        // the 48 pixel image is the first one 32 pixels tall.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Bordered, true);
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
        });

        var root = component.Find(".bit-flg");
        var image = component.Find("img");

        Assert.AreEqual($"{AssetsFlags}JP-flat-48.webp", image.GetAttribute("src"));
        Assert.IsFalse(image.HasAttribute("srcset"));
        Assert.IsTrue(root.ClassList.Contains("bit-flg-crp"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-flg-crp-x:1;--bit-flg-crp-y:8;--bit-flg-crp-w:46;--bit-flg-crp-h:32;--bit-flg-crp-s:48");
    }

    [TestMethod]
    public void BitFlagShouldCutACircularFrameToTheLargestImageOfTheSetWhereNoneCoversItTwiceOver()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Height, "2rem");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Shiny);
        });

        Assert.AreEqual($"{AssetsFlags}JP-shiny-64.webp", component.Find("img").GetAttribute("src"));
        StringAssert.Contains(component.Find(".bit-flg").GetAttribute("style"), "--bit-flg-crp-x:12;--bit-flg-crp-y:12;--bit-flg-crp-w:40;--bit-flg-crp-h:40;--bit-flg-crp-s:64");
    }

    [TestMethod]
    public void BitFlagShouldCutAFrameGivenAWidthAloneToTheImageOfTheSetThatCoversItsWidth()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Rounded, true);
            parameters.Add(p => p.Width, "1rem");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
        });

        Assert.AreEqual($"{AssetsFlags}JP-flat-48.webp", component.Find("img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitFlagShouldCutTheFrameToThePackagedFlagOnceItStandsInForTheImageSet()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Bordered, true);
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
        });

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        var root = component.Find(".bit-flg");
        var style = root.GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(root.ClassList.Contains("bit-flg-crp"));
        StringAssert.Contains(style, "--bit-flg-crp-x:1;--bit-flg-crp-y:2;--bit-flg-crp-w:14;--bit-flg-crp-h:11");
        Assert.IsFalse(style.Contains("--bit-flg-crp-s"));
    }

    [TestMethod]
    public void BitFlagShouldKeepASrcSetOfThePageOwnOnlyWithItsSrc()
    {
        var withSrc = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Src, "/my-flags/nl.png");
            parameters.Add(p => p.ImageAttributes, new Dictionary<string, object> { { "srcset", "/my-flags/nl@2x.png 2x" } });
        });

        Assert.AreEqual("/my-flags/nl@2x.png 2x", withSrc.Find("img").GetAttribute("srcset"));

        // Written over the packaged flag, it would be drawn in place of the very flag standing in for it.
        withSrc.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.IsFalse(withSrc.Find("img").HasAttribute("srcset"));

        var withSet = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Flat);
            parameters.Add(p => p.ImageAttributes, new Dictionary<string, object> { { "srcset", "/my-flags/nl@2x.png 2x" } });
        });

        StringAssert.StartsWith(withSet.Find("img").GetAttribute("srcset"), $"{AssetsFlags}NL-flat-16.webp 1x");
    }

    [TestMethod]
    public void BitFlagShouldOnlyPointAtImagesTheAssetsPackageShips()
    {
        // The images of the sets ship in the Bit.BlazorUI.Assets package, and nothing but their names ties
        // the flag to them. The test project writes the names of the files the package holds beside the
        // tests, and every image any flag offers the browser has to be one of them - with none left over.
        var shipped = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "assets-flags.txt"))
                          .Where(line => line.HasValue())
                          .ToHashSet(StringComparer.Ordinal);

        var offered = new HashSet<string>(StringComparer.Ordinal);

        foreach (var country in BitCountries.All)
        {
            foreach (var set in Enum.GetValues<BitFlagImageSet>())
            {
                var component = RenderComponent<BitFlag>(parameters =>
                {
                    parameters.Add(p => p.Country, country);
                    parameters.Add(p => p.ImageSet, set);
                });

                foreach (var candidate in component.Find("img").GetAttribute("srcset")!.Split(", "))
                {
                    var url = candidate[..candidate.IndexOf(' ')];

                    Assert.IsTrue(url.StartsWith(AssetsFlags, StringComparison.Ordinal), url);

                    offered.Add(url[AssetsFlags.Length..]);
                }

                // Every size a page can pin a flag to has to be one of them too.
                foreach (var imageSize in Enum.GetValues<BitFlagImageSize>())
                {
                    var pinned = RenderComponent<BitFlag>(parameters =>
                    {
                        parameters.Add(p => p.Country, country);
                        parameters.Add(p => p.ImageSet, set);
                        parameters.Add(p => p.ImageSize, imageSize);
                    });

                    var url = pinned.Find("img").GetAttribute("src")!;

                    Assert.IsTrue(url.StartsWith(AssetsFlags, StringComparison.Ordinal), url);

                    offered.Add(url[AssetsFlags.Length..]);
                }
            }
        }

        var missing = offered.Where(name => shipped.Contains(name) is false).Order().ToArray();
        var unused = shipped.Where(name => offered.Contains(name) is false).Order().ToArray();

        Assert.AreEqual(0, missing.Length, $"Not shipped: {string.Join(", ", missing.Take(10))}");
        Assert.AreEqual(0, unused.Length, $"Shipped but never offered: {string.Join(", ", unused.Take(10))}");
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
    public void BitFlagShouldFallBackToThePackagedFlagWhenTheSrcFails()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Src, "/my-flags/nl.svg");
            parameters.Add(p => p.FallbackTemplate, (RenderFragment)(builder => builder.AddContent(0, "!")));
        });

        Assert.AreEqual("/my-flags/nl.svg", component.Find("img").GetAttribute("src"));

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        // A set of the page's own that does not cover a country is better answered with the flag that
        // ships than with nothing at all.
        StringAssert.Contains(component.Find("img").GetAttribute("src"), "flags/NL-flat-16.webp");
        Assert.AreEqual(0, component.FindAll(".bit-flg-fbk").Count);

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual("!", component.Find(".bit-flg-fbk").TextContent);
    }

    [TestMethod]
    public void BitFlagShouldNotFallBackToTheImageThatJustFailed()
    {
        // There is no second image where the one that failed is the packaged flag itself, so a single
        // failure is the end of it rather than the same request being made again.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
        });

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.AreEqual(0, component.FindAll("img").Count);
    }

    [TestMethod]
    public void BitFlagShouldTryTheNewPackagedFlagWhenTheCountryChangesAfterAnError()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Src, "/my-flags/flag.svg");
        });

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);
        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.AreEqual(0, component.FindAll("img").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
        });

        Assert.AreEqual("/my-flags/flag.svg", component.Find("img").GetAttribute("src"));
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

    [TestMethod,
        DataRow("Circular"),
        DataRow("Rounded"),
        DataRow("Bordered"),
        DataRow("Shadow")]
    public void BitFlagShouldKeepTheFrameOfAShapedEmojiFlagSquare(string shape)
    {
        // A circle drawn around a box grown to fit a glyph is not a circle, so a shaped frame keeps
        // the size it was asked for and crops the glyph the way it crops a picture.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Iso2, "nl");

            switch (shape)
            {
                case "Circular": parameters.Add(p => p.Circular, true); break;
                case "Rounded": parameters.Add(p => p.Rounded, true); break;
                case "Bordered": parameters.Add(p => p.Bordered, true); break;
                default: parameters.Add(p => p.Shadow, true); break;
            }
        });

        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-emo"));
        Assert.AreEqual(1, component.FindAll(".bit-flg-emj").Count);
    }

    [TestMethod]
    public void BitFlagShouldRenderTheSubdivisionEmojiFlag()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.Iso2, "GB-SCT");
        });

        Assert.AreEqual("\U0001F3F4\U000E0067\U000E0062\U000E0073\U000E0063\U000E0074\U000E007F",
                        component.Find(".bit-flg-emj").TextContent);
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
        Assert.AreEqual("true", root.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitFlagShouldLeaveAnUnnamedFallbackOutOfTheAccessibilityTree()
    {
        // The fallback is text and text is read out wherever it is, so a decorative one - a question
        // mark standing in for a country that resolved to nothing - is hidden rather than announced.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "zz");
            parameters.Add(p => p.FallbackTemplate, (RenderFragment)(builder => builder.AddContent(0, "?")));
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual("true", root.GetAttribute("aria-hidden"));
        Assert.IsFalse(root.HasAttribute("role"));
    }

    [TestMethod]
    public void BitFlagShouldNameTheRootWhileTheFallbackIsWhatIsDrawn()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "xk");
            parameters.Add(p => p.Alt, "Kosovo");
            parameters.Add(p => p.FallbackTemplate, (RenderFragment)(builder => builder.AddContent(0, "XK")));
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual("img", root.GetAttribute("role"));
        Assert.AreEqual("Kosovo", root.GetAttribute("aria-label"));
        Assert.IsFalse(root.HasAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitFlagShouldLeaveADecorativeImageInTheAccessibilityTreeWithAnEmptyAlt()
    {
        // An image says it is decorative with an empty alt of its own, so the frame around it is not
        // hidden the way the emoji flag and the fallback are.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
        });

        Assert.IsFalse(component.Find(".bit-flg").HasAttribute("aria-hidden"));
        Assert.AreEqual(string.Empty, component.Find("img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitFlagShouldLeaveASplattedAriaHiddenAlone()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitFlag>(0);
            builder.AddAttribute(1, nameof(BitFlag.Emoji), true);
            builder.AddAttribute(2, nameof(BitFlag.Iso2), "nl");
            builder.AddAttribute(3, "aria-hidden", "false");
            builder.CloseComponent();
        });

        Assert.AreEqual("false", component.Find(".bit-flg").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitFlagShouldBeNamedByASplattedAriaLabelledby()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitFlag>(0);
            builder.AddAttribute(1, nameof(BitFlag.Iso2), "nl");
            builder.AddAttribute(2, "aria-labelledby", "the-caption");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-flg");

        // The label is written on the frame, so the frame is what takes the img role for it while the
        // picture inside keeps the empty alt of a decoration.
        Assert.AreEqual("img", root.GetAttribute("role"));
        Assert.IsFalse(root.HasAttribute("aria-hidden"));
        Assert.AreEqual(string.Empty, component.Find("img").GetAttribute("alt"));
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

        var root = component.Find(".bit-flg");

        Assert.IsFalse(root.HasAttribute("aria-label"));

        // "button" with nothing to read out is a dead stop for a screen reader, so the role is only
        // written once the flag has a name to announce with it.
        Assert.IsFalse(root.HasAttribute("role"));

        // It is still the control it was: it answers the pointer and stays in the tab order.
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
        Assert.IsTrue(root.ClassList.Contains("bit-flg-clk"));
    }

    [TestMethod]
    public async Task BitFlagShouldStillAnswerAClickWithoutAName()
    {
        var clicked = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Src, "/my-flags/nl.svg");
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        await component.Find(".bit-flg").ClickAsync(new MouseEventArgs());

        Assert.AreEqual(1, clicked);
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
    public void BitFlagShouldRespectAspectRatio()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Height, "2rem");
            parameters.Add(p => p.AspectRatio, "3/2");
        });

        var root = component.Find(".bit-flg");
        var style = root.GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "aspect-ratio:3/2");
        StringAssert.Contains(style, "height:2rem");

        // The square width every other flag is given has to come off, or the ratio would have nothing
        // left to decide.
        Assert.IsTrue(root.ClassList.Contains("bit-flg-asp"));
        Assert.IsFalse(style.Contains("width:"));
    }

    [TestMethod]
    public void BitFlagShouldFreeTheHeightForARatioGivenAWidth()
    {
        // A ratio needs one of the two lengths left for it to work out, and where the width is the one
        // that was given it is the height that has to go.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Width, "3rem");
            parameters.Add(p => p.AspectRatio, "3/2");
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "width:3rem");
        StringAssert.Contains(style, "height:auto");
        StringAssert.Contains(style, "aspect-ratio:3/2");
    }

    [TestMethod]
    public void BitFlagShouldNotWriteAnAspectRatioClassWithoutOne()
    {
        var component = RenderComponent<BitFlag>();

        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-asp"));
    }

    [TestMethod,
        DataRow(BitImageFit.None, "bit-flg-non"),
        DataRow(BitImageFit.Center, "bit-flg-ctr"),
        DataRow(BitImageFit.CenterContain, "bit-flg-cct"),
        DataRow(BitImageFit.CenterCover, "bit-flg-ccv"),
        DataRow(BitImageFit.Contain, "bit-flg-cnt"),
        DataRow(BitImageFit.Cover, "bit-flg-cvr"),
        DataRow(BitImageFit.Fill, "bit-flg-fil"),
        DataRow(BitImageFit.ScaleDown, "bit-flg-scd")]
    public void BitFlagShouldRespectFit(BitImageFit fit, string expectedClass)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.Fit, fit);
        });

        Assert.IsTrue(component.Find(".bit-flg").ClassList.Contains(expectedClass));
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

    [TestMethod,
        DataRow("Rounded"),
        DataRow("Bordered"),
        DataRow("Shadow")]
    public void BitFlagShouldCutAShapedFrameToThePackagedFlag(string shape)
    {
        // The packaged image draws the flag in its own proportions inside a square, so a frame shaped to
        // the square would round, border and shadow the empty space around the flag.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");

            switch (shape)
            {
                case "Rounded": parameters.Add(p => p.Rounded, true); break;
                case "Bordered": parameters.Add(p => p.Bordered, true); break;
                default: parameters.Add(p => p.Shadow, true); break;
            }
        });

        var root = component.Find(".bit-flg");

        Assert.IsTrue(root.ClassList.Contains("bit-flg-crp"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-flg-crp-x:1;--bit-flg-crp-y:2;--bit-flg-crp-w:14;--bit-flg-crp-h:11");
    }

    [TestMethod]
    public void BitFlagShouldCutACircularFrameToTheMiddleOfThePackagedFlag()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Circular, true);
        });

        var root = component.Find(".bit-flg");

        Assert.IsTrue(root.ClassList.Contains("bit-flg-crp"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-flg-crp-x:2.5;--bit-flg-crp-y:2;--bit-flg-crp-w:11;--bit-flg-crp-h:11");
    }

    [TestMethod,
        DataRow("CH", "--bit-flg-crp-x:2;--bit-flg-crp-y:2;--bit-flg-crp-w:12;--bit-flg-crp-h:12"),
        DataRow("NP", "--bit-flg-crp-x:3;--bit-flg-crp-y:1;--bit-flg-crp-w:10;--bit-flg-crp-h:13"),
        DataRow("RE", "--bit-flg-crp-x:0;--bit-flg-crp-y:3;--bit-flg-crp-w:16;--bit-flg-crp-h:10"),
        DataRow("va", "--bit-flg-crp-x:2;--bit-flg-crp-y:2;--bit-flg-crp-w:12;--bit-flg-crp-h:12")]
    public void BitFlagShouldCutAShapedFrameToAFlagOfItsOwnProportions(string iso2, string expected)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, iso2);
            parameters.Add(p => p.Bordered, true);
        });

        StringAssert.Contains(component.Find(".bit-flg").GetAttribute("style"), expected);
    }

    [TestMethod]
    public void BitFlagShouldNotCutAnUnshapedFrame()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Grayscale, true);
        });

        var root = component.Find(".bit-flg");

        Assert.IsFalse(root.ClassList.Contains("bit-flg-crp"));
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("--bit-flg-crp"));
    }

    [TestMethod,
        DataRow("AspectRatio"),
        DataRow("WidthAndHeight"),
        DataRow("Src"),
        DataRow("Emoji")]
    public void BitFlagShouldNotCutAFrameWhoseShapeIsNotTheFlags(string reason)
    {
        // A ratio or both lengths are a shape the page asked for, a Src of the page's own is drawn exactly
        // as given, and the emoji flag is no image at all.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Bordered, true);

            switch (reason)
            {
                case "AspectRatio": parameters.Add(p => p.AspectRatio, "3/2"); break;
                case "WidthAndHeight":
                    parameters.Add(p => p.Width, "4rem");
                    parameters.Add(p => p.Height, "2rem");
                    break;
                case "Src": parameters.Add(p => p.Src, "/flags/jp.svg"); break;
                default: parameters.Add(p => p.Emoji, true); break;
            }
        });

        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-crp"));
    }

    [TestMethod]
    public void BitFlagShouldFreeTheHeightOfACutFrameGivenAWidth()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Rounded, true);
            parameters.Add(p => p.Width, "3rem");
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "width:3rem");
        StringAssert.Contains(style, "height:auto");
    }

    [TestMethod]
    public void BitFlagShouldCutTheFrameOnceThePackagedFlagStandsInForAFailedSrc()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
            parameters.Add(p => p.Bordered, true);
            parameters.Add(p => p.Src, "/not-a-real-flag.png");
        });

        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-crp"));

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.IsTrue(component.Find(".bit-flg").ClassList.Contains("bit-flg-crp"));
    }

    [TestMethod]
    public void BitFlagShouldFollowTheCutChangingAfterRender()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "jp");
        });

        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-crp"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Iso2, "ch");
            parameters.Add(p => p.Bordered, true);
        });

        var root = component.Find(".bit-flg");

        Assert.IsTrue(root.ClassList.Contains("bit-flg-crp"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-flg-crp-w:12;--bit-flg-crp-h:12");
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
