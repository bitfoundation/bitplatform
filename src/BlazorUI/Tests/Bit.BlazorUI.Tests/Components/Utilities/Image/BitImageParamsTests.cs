using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Image;

/// <summary>
/// Covers the BitParams cascade of the Image: what a BitImageParams fills in - the frame, the shape, the fit, the
/// hints the browser fetches by, the per-part Classes and Styles and every parameter of BitComponentBase - what it
/// leaves alone because the image wrote it for itself, and what it never carries (the picture and its events). The
/// sources, the fallback and the attribute merge have their own tests in BitImageTests.
/// </summary>
[TestClass]
public class BitImageParamsTests : BunitTestContext
{
    // The picture itself, what stands in for it, what is laid over it and what it reports are per image.
    private static readonly string[] _notCascaded =
    [
        nameof(BitImage.CascadingParameters),
        nameof(BitImage.Alt),
        nameof(BitImage.ChildContent),
        nameof(BitImage.ErrorTemplate),
        nameof(BitImage.LoadingTemplate),
        nameof(BitImage.OnClick),
        nameof(BitImage.OnError),
        nameof(BitImage.OnLoad),
        nameof(BitImage.OnLoadingStateChange),
        nameof(BitImage.PlaceholderSrc),
        nameof(BitImage.Sizes),
        nameof(BitImage.Sources),
        nameof(BitImage.Src),
        nameof(BitImage.Srcset),
        nameof(BitImage.Title),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitImageParams imageParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { imageParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitImage>(0);
                builder.AddAttribute(1, nameof(BitImage.Src), "image.png");
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitImageParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitImage)}", BitImageParams.ParamName);
        Assert.AreEqual("BitParams.BitImage", BitImageParams.ParamName);
    }

    [TestMethod]
    public void BitImageParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitImageParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitImageParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitImageParamsShouldCarryEveryParameterThatIsNotPerImage()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitImage).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Select(p => p.Name)
                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitImageParams).GetProperty(name), $"BitImageParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitImageParamsShouldNotCarryThePictureOrItsEvents()
    {
        foreach (var name in _notCascaded.Where(n => n != nameof(BitImage.CascadingParameters)))
        {
            Assert.IsNull(typeof(BitImageParams).GetProperty(name), $"BitImageParams carries {name}, which is per image.");
        }
    }

    [TestMethod]
    public void BitImageShouldApplyCascadedFrameClasses()
    {
        var component = RenderWithParams(new BitImageParams
        {
            Circular = true,
            Shadow = true,
            Bordered = true,
            MaximizeFrame = true,
            Fluid = true,
        });

        var frame = component.Find(".bit-img");

        Assert.IsTrue(frame.ClassList.Contains("bit-img-cir"));
        Assert.IsTrue(frame.ClassList.Contains("bit-img-shd"));
        Assert.IsTrue(frame.ClassList.Contains("bit-img-brd"));
        Assert.IsTrue(frame.ClassList.Contains("bit-img-max"));
        Assert.IsTrue(frame.ClassList.Contains("bit-img-flu"));
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-fli"));
    }

    [TestMethod]
    public void BitImageShouldPreferACascadedCircleOverACascadedRounding()
    {
        var component = RenderWithParams(new BitImageParams { Rounded = true, Circular = true });

        var frame = component.Find(".bit-img");

        Assert.IsTrue(frame.ClassList.Contains("bit-img-cir"));
        Assert.IsFalse(frame.ClassList.Contains("bit-img-rnd"));
    }

    [TestMethod]
    public void BitImageShouldApplyCascadedFrameSize()
    {
        var component = RenderWithParams(new BitImageParams { Width = "10rem", Height = "80", AspectRatio = "4/3" });

        var style = component.Find(".bit-img").GetAttribute("style");

        StringAssert.Contains(style, "width:10rem");
        // A bare number is a pixel count whichever way it arrives.
        StringAssert.Contains(style, "height:80px");
        StringAssert.Contains(style, "aspect-ratio:4/3");
    }

    [TestMethod]
    public void BitImageShouldApplyCascadedFitCoverAndPosition()
    {
        var component = RenderWithParams(new BitImageParams
        {
            ImageFit = BitImageFit.CenterCover,
            Cover = BitImageCover.Landscape,
            ImagePosition = "top",
        });

        var image = component.Find(".bit-img-img");

        Assert.IsTrue(component.Find(".bit-img").ClassList.Contains("bit-img-cen"));
        Assert.IsTrue(image.ClassList.Contains("bit-img-ccv"));
        Assert.IsTrue(image.ClassList.Contains("bit-img-lan"));
        Assert.IsFalse(image.ClassList.Contains("bit-img-por"));
        StringAssert.Contains(image.GetAttribute("style"), "object-position:top");
    }

    [TestMethod]
    public void BitImageShouldApplyCascadedBrowserHints()
    {
        var component = RenderWithParams(new BitImageParams
        {
            Loading = BitImageLoading.Lazy,
            Decoding = BitImageDecoding.Async,
            FetchPriority = BitImageFetchPriority.High,
            CrossOrigin = BitImageCrossOrigin.UseCredentials,
            ReferrerPolicy = BitImageReferrerPolicy.NoReferrer,
            Draggable = false,
        });

        var image = component.Find(".bit-img-img");

        Assert.AreEqual("lazy", image.GetAttribute("loading"));
        Assert.IsTrue(image.ClassList.Contains("bit-img-lzy"));
        Assert.AreEqual("async", image.GetAttribute("decoding"));
        Assert.AreEqual("high", image.GetAttribute("fetchpriority"));
        Assert.AreEqual("use-credentials", image.GetAttribute("crossorigin"));
        Assert.AreEqual("no-referrer", image.GetAttribute("referrerpolicy"));
        Assert.AreEqual("false", image.GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitImageShouldApplyCascadedStartVisible()
    {
        var component = RenderWithParams(new BitImageParams { StartVisible = true });

        var image = component.Find(".bit-img-img");

        Assert.IsTrue(image.ClassList.Contains("bit-img-vis"));
        Assert.IsFalse(image.ClassList.Contains("bit-img-hid"));
    }

    [TestMethod]
    public void BitImageShouldApplyCascadedFadeIn()
    {
        var component = RenderWithParams(new BitImageParams { FadeIn = true });

        // Nothing to fade in while the image is still hidden; the fade belongs to its arrival.
        Assert.IsFalse(component.Find(".bit-img-img").ClassList.Contains("bit-img-fde"));

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-fde"));
    }

    [TestMethod]
    public void BitImageShouldApplyCascadedClassesAndStylesToEveryPart()
    {
        var component = RenderWithParams(new BitImageParams
        {
            Classes = new() { Root = "root-class", Image = "image-class", Content = "content-class", LoadingTemplate = "loading-class" },
            Styles = new() { Root = "margin:1rem", Image = "padding:1rem", Content = "color:blue", LoadingTemplate = "color:red" },
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitImage.LoadingTemplate), (RenderFragment)(b => b.AddContent(0, "loading")));
            builder.AddAttribute(3, nameof(BitImage.ChildContent), (RenderFragment)(b => b.AddContent(0, "overlay")));
        });

        Assert.IsTrue(component.Find(".bit-img").ClassList.Contains("root-class"));
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("image-class"));
        Assert.IsTrue(component.Find(".bit-img-tpl").ClassList.Contains("loading-class"));
        Assert.IsTrue(component.Find(".bit-img-ovl").ClassList.Contains("content-class"));

        StringAssert.Contains(component.Find(".bit-img").GetAttribute("style"), "margin:1rem");
        StringAssert.Contains(component.Find(".bit-img-img").GetAttribute("style"), "padding:1rem");
        StringAssert.Contains(component.Find(".bit-img-tpl").GetAttribute("style"), "color:red");
        StringAssert.Contains(component.Find(".bit-img-ovl").GetAttribute("style"), "color:blue");
    }

    [TestMethod]
    public void BitImageShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitImageParams
        {
            AriaLabel = "A lighthouse",
            Class = "cascaded",
            Dir = BitDir.Rtl,
            Disabled = true,
            Id = "cascaded-id",
            Style = "color: red;",
            TabIndex = "2",
            Visibility = BitVisibility.Hidden,
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var frame = component.Find(".bit-img");
        var image = component.Find(".bit-img-img");

        Assert.IsTrue(frame.ClassList.Contains("cascaded"));
        Assert.IsTrue(frame.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(frame.ClassList.Contains("bit-dis"));
        Assert.AreEqual("rtl", frame.GetAttribute("dir"));
        Assert.AreEqual("cascaded-id", frame.Id);
        StringAssert.Contains(frame.GetAttribute("style"), "color: red;");
        StringAssert.Contains(frame.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("yes", frame.GetAttribute("data-cascaded"));

        // The label and the tab index name and reach the img, as the parameters of the same names do.
        Assert.AreEqual("A lighthouse", image.GetAttribute("aria-label"));
        Assert.AreEqual("2", image.GetAttribute("tabindex"));
        Assert.IsFalse(frame.HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitImageShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitImageParams
        {
            Circular = true,
            Shadow = true,
            Width = "10rem",
            ImageFit = BitImageFit.Cover,
            Loading = BitImageLoading.Lazy,
            Cover = BitImageCover.Landscape,
            Classes = new() { Root = "cascaded-root" },
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitImage.Circular), false);
            builder.AddAttribute(3, nameof(BitImage.Shadow), false);
            builder.AddAttribute(4, nameof(BitImage.Width), "20rem");
            builder.AddAttribute(5, nameof(BitImage.ImageFit), (BitImageFit?)BitImageFit.Contain);
            builder.AddAttribute(6, nameof(BitImage.Loading), (BitImageLoading?)BitImageLoading.Eager);
            builder.AddAttribute(7, nameof(BitImage.Cover), (BitImageCover?)BitImageCover.Portrait);
            builder.AddAttribute(8, nameof(BitImage.Classes), new BitImageClassStyles { Root = "own-root" });
        });

        var frame = component.Find(".bit-img");
        var image = component.Find(".bit-img-img");

        // A false written by the image is a decision, so the cascaded true does not turn the shape on.
        Assert.IsFalse(frame.ClassList.Contains("bit-img-cir"));
        Assert.IsFalse(frame.ClassList.Contains("bit-img-shd"));
        StringAssert.Contains(frame.GetAttribute("style"), "width:20rem");
        Assert.IsTrue(image.ClassList.Contains("bit-img-cnt"));
        Assert.IsFalse(image.ClassList.Contains("bit-img-cvr"));
        Assert.AreEqual("eager", image.GetAttribute("loading"));
        Assert.IsTrue(image.ClassList.Contains("bit-img-por"));
        Assert.IsTrue(frame.ClassList.Contains("own-root"));
        Assert.IsFalse(frame.ClassList.Contains("cascaded-root"));
    }

    [TestMethod]
    public void BitImageShouldGiveBackCascadedValuesWhenTheCascadeStopsCarryingThem()
    {
        var component = RenderWithParams(new BitImageParams { Rounded = true, Width = "10rem", Loading = BitImageLoading.Lazy });

        var instance = component.FindComponent<BitImage>().Instance;

        Assert.IsTrue(component.Find(".bit-img").ClassList.Contains("bit-img-rnd"));

        // The same number of params objects, so BitParams keeps its child instead of building a new one.
        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitImageParams() }));

        Assert.AreSame(instance, component.FindComponent<BitImage>().Instance);
        Assert.IsFalse(component.Find(".bit-img").ClassList.Contains("bit-img-rnd"));
        Assert.IsFalse(component.Find(".bit-img").HasAttribute("style"));
        Assert.IsFalse(component.Find(".bit-img-img").HasAttribute("loading"));
        Assert.IsFalse(instance.Rounded);
        Assert.IsNull(instance.Width);
        Assert.IsNull(instance.Loading);
    }

    [TestMethod]
    public void BitImageShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitImageParams());

        component.MarkupMatches(@"<div class=""bit-img"" id:ignore><img alt="""" src=""image.png"" class=""bit-img-img bit-img-por bit-img-hid"" /></div>");
    }
}
