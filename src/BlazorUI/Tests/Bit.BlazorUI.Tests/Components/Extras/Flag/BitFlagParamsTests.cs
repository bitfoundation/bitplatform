using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Flag;

/// <summary>
/// Covers what a BitParams ancestor sets for every flag under it through a <see cref="BitFlagParams"/>, and the
/// <see cref="BitFlag.SrcPattern"/> it is the natural way of handing to a whole country picker.
/// </summary>
[TestClass]
public class BitFlagParamsTests : BunitTestContext
{
    private const string PackagedNetherlands = "_content/Bit.BlazorUI.Extras/flags/NL-flat-16.webp";

    [TestMethod]
    public void BitFlagParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitFlag", BitFlagParams.ParamName);
        Assert.AreEqual(BitFlagParams.ParamName, new BitFlagParams().Name);
    }

    [TestMethod]
    public void BitFlagShouldTakeTheShapeAndTheSizeFromTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams
        {
            Rounded = true,
            Bordered = true,
            Shadow = true,
            Grayscale = true,
            Height = "2rem",
            Class = "cascaded-class",
        });

        var root = component.Find(".bit-flg");

        Assert.IsTrue(root.ClassList.Contains("bit-flg-rnd"));
        Assert.IsTrue(root.ClassList.Contains("bit-flg-brd"));
        Assert.IsTrue(root.ClassList.Contains("bit-flg-shd"));
        Assert.IsTrue(root.ClassList.Contains("bit-flg-gry"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));

        // A shaped packaged flag is cut to the flag, which the cascaded shape has to reach as well as the class.
        Assert.IsTrue(root.ClassList.Contains("bit-flg-crp"));
        StringAssert.Contains(root.GetAttribute("style"), "height:2rem");
        StringAssert.Contains(root.GetAttribute("style"), "--bit-flg-crp-w:");
    }

    [TestMethod]
    public void BitFlagShouldTakeTheSizeClassFromTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams { Size = BitSize.Large });

        Assert.IsTrue(component.Find(".bit-flg").ClassList.Contains("bit-flg-lg"));
    }

    [TestMethod]
    public void BitFlagOwnValuesShouldWinOverTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams { Rounded = true, Height = "2rem", Size = BitSize.Large },
                                         flag =>
                                         {
                                             flag.Add(p => p.Rounded, false);
                                             flag.Add(p => p.Height, "3rem");
                                             flag.Add(p => p.Size, BitSize.Small);
                                         });

        var root = component.Find(".bit-flg");

        Assert.IsFalse(root.ClassList.Contains("bit-flg-rnd"));
        Assert.IsTrue(root.ClassList.Contains("bit-flg-sm"));
        StringAssert.Contains(root.GetAttribute("style"), "height:3rem");
    }

    [TestMethod]
    public void BitFlagShouldTakeTheNamingFromTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams { AutoAlt = true, AutoTitle = true });

        Assert.AreEqual("Netherlands", component.Find("img").GetAttribute("alt"));
        Assert.AreEqual("Netherlands", component.Find(".bit-flg").GetAttribute("title"));
    }

    [TestMethod]
    public void BitFlagShouldTakeTheEmojiFromTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams { Emoji = true });

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual(BitCountries.Netherlands.Emoji, component.Find(".bit-flg-emj").TextContent);
        Assert.IsTrue(component.Find(".bit-flg").ClassList.Contains("bit-flg-emo"));
    }

    [TestMethod]
    public void BitFlagShouldTakeTheImageSetFromTheCascadeOverACascadingValue()
    {
        var component = RenderComponent<CascadingValue<BitFlagImageSet>>(parameters =>
        {
            parameters.Add(p => p.Value, BitFlagImageSet.Flat);
            parameters.AddChildContent<BitParams>(bitParams =>
            {
                bitParams.Add(p => p.Parameters, new List<IBitComponentParams>
                {
                    new BitFlagParams { ImageSet = BitFlagImageSet.Shiny, ImageSize = BitFlagImageSize.Size32 }
                });
                bitParams.AddChildContent<BitFlag>(flag => flag.Add(p => p.Iso2, "NL"));
            });
        });

        Assert.AreEqual("_content/Bit.BlazorUI.Assets/flags/NL-shiny-32.webp", component.Find("img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitFlagOwnImageSetShouldWinOverTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams { ImageSet = BitFlagImageSet.Shiny, ImageSize = BitFlagImageSize.Size32 },
                                         flag => flag.Add(p => p.ImageSet, BitFlagImageSet.Flat));

        Assert.AreEqual("_content/Bit.BlazorUI.Assets/flags/NL-flat-32.webp", component.Find("img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitFlagShouldTakeTheLoadingAndTheFitFromTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams { Loading = BitImageLoading.Eager, Fit = BitImageFit.Contain, AspectRatio = "4/3" });

        Assert.AreEqual("eager", component.Find("img").GetAttribute("loading"));

        var root = component.Find(".bit-flg");

        Assert.IsTrue(root.ClassList.Contains("bit-flg-cnt"));
        Assert.IsTrue(root.ClassList.Contains("bit-flg-asp"));
        StringAssert.Contains(root.GetAttribute("style"), "aspect-ratio:4/3");
    }

    [TestMethod]
    public void BitFlagShouldTakeTheFallbackTemplateFromTheCascade()
    {
        RenderFragment fallback = builder => builder.AddContent(0, "?");

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitFlagParams { FallbackTemplate = fallback } });
            parameters.AddChildContent<BitFlag>(flag => flag.Add(p => p.Iso2, "zz"));
        });

        Assert.AreEqual("?", component.Find(".bit-flg-fbk").TextContent.Trim());
    }

    [TestMethod]
    public void BitFlagShouldTakeTheClassesAndStylesFromTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams
        {
            Classes = new() { Root = "cascaded-root", Image = "cascaded-image" },
            Styles = new() { Root = "color:red", Image = "opacity:0.5" },
        });

        var root = component.Find(".bit-flg");
        var img = component.Find("img");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(img.ClassList.Contains("cascaded-image"));
        StringAssert.Contains(root.GetAttribute("style"), "color:red");
        StringAssert.Contains(img.GetAttribute("style"), "opacity:0.5");
    }

    /// <summary>
    /// The cascaded attributes fill in the ones the flag did not set, into a copy: the dictionary the page handed
    /// the flag may be shared with other flags, or kept by the page itself, and must not collect them.
    /// </summary>
    [TestMethod]
    public void BitFlagShouldMergeCascadedImageAttributesWithoutTouchingItsOwn()
    {
        Dictionary<string, object> own = new() { ["data-own"] = "own", ["data-both"] = "own" };

        var component = RenderWithParams(new BitFlagParams { ImageAttributes = new() { ["data-cascaded"] = "cascaded", ["data-both"] = "cascaded" } },
                                         flag => flag.Add(p => p.ImageAttributes, own));

        var img = component.Find("img");

        Assert.AreEqual("own", img.GetAttribute("data-own"));
        Assert.AreEqual("own", img.GetAttribute("data-both"));
        Assert.AreEqual("cascaded", img.GetAttribute("data-cascaded"));

        Assert.AreEqual(2, own.Count);
        Assert.IsFalse(own.ContainsKey("data-cascaded"));
    }

    /// <summary>
    /// The cascaded values are read again on every render, so a change to the parameters object reaches the flag
    /// the next time its parent renders it, class and style included.
    /// </summary>
    [TestMethod]
    public void BitFlagShouldFollowAChangedCascade()
    {
        var flagParams = new BitFlagParams { Rounded = false, Height = "2rem" };

        var renders = 0;

        RenderFragment Flag() => builder =>
        {
            builder.OpenComponent<BitFlag>(0);
            builder.AddAttribute(1, nameof(BitFlag.Iso2), "NL");
            builder.AddAttribute(2, nameof(BitFlag.Title), $"render {++renders}");
            builder.CloseComponent();
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { flagParams });
            parameters.Add(p => p.ChildContent, Flag());
        });

        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-rnd"));

        flagParams.Rounded = true;
        flagParams.Height = "3rem";

        component.Render(parameters => parameters.Add(p => p.ChildContent, Flag()));

        var root = component.Find(".bit-flg");

        Assert.IsTrue(root.ClassList.Contains("bit-flg-rnd"));
        StringAssert.Contains(root.GetAttribute("style"), "height:3rem");
    }

    /// <summary>
    /// A flag the page hands no ImageAttributes of its own is not handed them again on the next render, so the
    /// cascade of every render is merged into the flag's own and never into the last merge: a cascaded attribute
    /// taken away since goes, and one changed since takes its new value.
    /// </summary>
    [TestMethod]
    public void BitFlagShouldFollowAChangedCascadeOfImageAttributes()
    {
        var flagParams = new BitFlagParams { ImageAttributes = new() { ["data-changed"] = "first", ["data-removed"] = "first" } };

        var renders = 0;

        RenderFragment Flag() => builder =>
        {
            builder.OpenComponent<BitFlag>(0);
            builder.AddAttribute(1, nameof(BitFlag.Iso2), "NL");
            builder.AddAttribute(2, nameof(BitFlag.Title), $"render {++renders}");
            builder.CloseComponent();
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { flagParams });
            parameters.Add(p => p.ChildContent, Flag());
        });

        Assert.AreEqual("first", component.Find("img").GetAttribute("data-changed"));
        Assert.AreEqual("first", component.Find("img").GetAttribute("data-removed"));

        flagParams.ImageAttributes = new() { ["data-changed"] = "second" };

        component.Render(parameters => parameters.Add(p => p.ChildContent, Flag()));

        var img = component.Find("img");

        Assert.AreEqual("second", img.GetAttribute("data-changed"));
        Assert.IsFalse(img.HasAttribute("data-removed"));
    }

    [TestMethod]
    public void BitFlagShouldTakeTheSrcPatternFromTheCascade()
    {
        var component = RenderWithParams(new BitFlagParams { SrcPattern = "/flags/{iso2}.svg" });

        Assert.AreEqual("/flags/nl.svg", component.Find("img").GetAttribute("src"));
    }



    // ---------------------------------------------------------------- SrcPattern

    [TestMethod,
        DataRow("https://flagcdn.com/{iso2}.svg", "https://flagcdn.com/nl.svg"),
        DataRow("/flags/{ISO2}.png", "/flags/NL.png"),
        DataRow("/flags/{iso3}.svg", "/flags/nld.svg"),
        DataRow("/flags/{ISO3}-{iso2}.svg", "/flags/NLD-nl.svg"),
        DataRow("/flags/netherlands.svg", "/flags/netherlands.svg")]
    public void BitFlagShouldWriteTheCodesIntoTheSrcPattern(string pattern, string expected)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "nl");
            parameters.Add(p => p.SrcPattern, pattern);
        });

        Assert.AreEqual(expected, component.Find("img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitFlagSrcShouldWinOverTheSrcPattern()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "NL");
            parameters.Add(p => p.Src, "own.svg");
            parameters.Add(p => p.SrcPattern, "/flags/{iso2}.svg");
        });

        Assert.AreEqual("own.svg", component.Find("img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitFlagEmojiShouldWinOverTheSrcPattern()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "NL");
            parameters.Add(p => p.Emoji, true);
            parameters.Add(p => p.SrcPattern, "/flags/{iso2}.svg");
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
        Assert.AreEqual(1, component.FindAll(".bit-flg-emj").Count);
    }

    [TestMethod]
    public void BitFlagSrcPatternShouldWinOverTheImageSet()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "NL");
            parameters.Add(p => p.ImageSet, BitFlagImageSet.Shiny);
            parameters.Add(p => p.SrcPattern, "/flags/{iso2}.svg");
        });

        var img = component.Find("img");

        Assert.AreEqual("/flags/nl.svg", img.GetAttribute("src"));
        Assert.IsFalse(img.HasAttribute("srcset"));
    }

    [TestMethod]
    public void BitFlagSrcPatternShouldDrawNothingForAnUnknownCountry()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "zz");
            parameters.Add(p => p.SrcPattern, "/flags/{iso2}.svg");
        });

        Assert.AreEqual(0, component.FindAll("img").Count);
    }

    [TestMethod]
    public void BitFlagSrcPatternShouldBeDrawnAsGivenRatherThanCutToTheFlag()
    {
        // The packaged and the set images are cut to the box their flag is drawn in; an image of the page's own
        // has no known box, so a shaped frame keeps the image whole, the way it keeps a Src.
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "NL");
            parameters.Add(p => p.Rounded, true);
            parameters.Add(p => p.SrcPattern, "/flags/{iso2}.svg");
        });

        Assert.IsFalse(component.Find(".bit-flg").ClassList.Contains("bit-flg-crp"));
    }

    [TestMethod]
    public void BitFlagSrcPatternThatFailsShouldFallBackToThePackagedFlag()
    {
        var errors = 0;

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "NL");
            parameters.Add(p => p.SrcPattern, "/flags/{iso2}.svg");
            parameters.Add(p => p.OnError, () => errors++);
        });

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.AreEqual(PackagedNetherlands, component.Find("img").GetAttribute("src"));
        Assert.AreEqual(1, errors);
    }

    [TestMethod]
    public void BitFlagSrcPatternShouldKeepAnImageAttributesSrcSet()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "NL");
            parameters.Add(p => p.SrcPattern, "/flags/{iso2}.svg");
            parameters.Add(p => p.ImageAttributes, new Dictionary<string, object> { ["srcset"] = "/flags/nl@2x.svg 2x" });
        });

        Assert.AreEqual("/flags/nl@2x.svg 2x", component.Find("img").GetAttribute("srcset"));

        // The packaged flag standing in for a failed one is the one image there is.
        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        Assert.IsFalse(component.Find("img").HasAttribute("srcset"));
    }

    [TestMethod]
    public void BitFlagSrcPatternShouldFollowTheCountry()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "NL");
            parameters.Add(p => p.SrcPattern, "/flags/{iso2}.svg");
        });

        component.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        component.Render(parameters => parameters.Add(p => p.Iso2, "JP"));

        // A new country is a new image, so the error of the previous one is forgotten.
        Assert.AreEqual("/flags/jp.svg", component.Find("img").GetAttribute("src"));
    }



    private IRenderedComponent<BitParams> RenderWithParams(BitFlagParams flagParams, Action<ComponentParameterCollectionBuilder<BitFlag>>? flag = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { flagParams });
            parameters.AddChildContent<BitFlag>(builder =>
            {
                builder.Add(p => p.Iso2, "NL");
                flag?.Invoke(builder);
            });
        });
    }
}
