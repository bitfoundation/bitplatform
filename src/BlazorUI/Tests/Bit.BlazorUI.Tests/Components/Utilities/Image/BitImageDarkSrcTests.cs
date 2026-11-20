using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ErrorEventArgs = Microsoft.AspNetCore.Components.Web.ErrorEventArgs;

namespace Bit.BlazorUI.Tests.Components.Utilities.Image;

/// <summary>
/// The image of the dark color scheme: a second img for DarkSrc / DarkSrcset, which the stylesheet shows in place of
/// the one of Src while the scheme is dark (bit-img-dsc / bit-img-lsc), each image loading, falling back and failing
/// on its own, and every part of the frame that follows the state rendered per scheme where the two differ.
/// </summary>
[TestClass]
public class BitImageDarkSrcTests : BunitTestContext
{
    private IRenderedComponent<BitImage> RenderPair(System.Action<ComponentParameterCollectionBuilder<BitImage>>? more = null)
    {
        return RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "light.png");
            parameters.Add(p => p.DarkSrc, "dark.png");
            more?.Invoke(parameters);
        });
    }

    private static AngleSharp.Dom.IElement Light(IRenderedComponent<BitImage> component) => component.Find(".bit-img-img.bit-img-lsc");

    private static AngleSharp.Dom.IElement Dark(IRenderedComponent<BitImage> component) => component.Find(".bit-img-img.bit-img-dsc");

    [TestMethod]
    public void BitImageShouldRenderNoSecondImgWithoutADarkSource()
    {
        var component = RenderComponent<BitImage>(parameters => parameters.Add(p => p.Src, "light.png"));

        Assert.AreEqual(1, component.FindAll(".bit-img-img").Count);
        Assert.AreEqual(0, component.FindAll(".bit-img-lsc, .bit-img-dsc").Count);
    }

    [TestMethod]
    public void BitImageShouldRenderAnImgPerColorScheme()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.Alt, "A lighthouse");
            parameters.Add(p => p.Srcset, "light-2x.png 2x");
            parameters.Add(p => p.DarkSrcset, "dark-2x.png 2x");
            parameters.Add(p => p.Sizes, "50vw");
            parameters.Add(p => p.Loading, BitImageLoading.Lazy);
        });

        var images = component.FindAll(".bit-img-img");

        Assert.AreEqual(2, images.Count);

        var light = Light(component);
        var dark = Dark(component);

        Assert.AreEqual("light.png", light.GetAttribute("src"));
        Assert.AreEqual("light-2x.png 2x", light.GetAttribute("srcset"));
        Assert.AreEqual("dark.png", dark.GetAttribute("src"));
        Assert.AreEqual("dark-2x.png 2x", dark.GetAttribute("srcset"));

        // Everything but the sources is the same image's.
        foreach (var img in images)
        {
            Assert.AreEqual("A lighthouse", img.GetAttribute("alt"));
            Assert.AreEqual("50vw", img.GetAttribute("sizes"));
            Assert.AreEqual("lazy", img.GetAttribute("loading"));
        }

        Assert.AreEqual(component.Instance.ImageElement.Id, light.GetAttribute("blazor:elementReference"));
    }

    [TestMethod]
    public void BitImageShouldRenderTheDarkImgForADarkSrcsetAlone()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "light.png");
            parameters.Add(p => p.DarkSrcset, "dark-1x.png 1x, dark-2x.png 2x");
        });

        var dark = Dark(component);

        Assert.AreEqual("dark-1x.png 1x, dark-2x.png 2x", dark.GetAttribute("srcset"));
        Assert.IsFalse(dark.HasAttribute("src"));
    }

    [TestMethod]
    public void BitImageShouldKeepTheIdAndTheSourcesOfImageAttributesOffTheDarkImg()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.ImageAttributes, new Dictionary<string, object>
            {
                ["id"] = "hero",
                ["srcset"] = "splatted-2x.png 2x",
                ["data-x"] = "1",
            });
        });

        var light = Light(component);
        var dark = Dark(component);

        Assert.AreEqual("hero", light.GetAttribute("id"));
        Assert.AreEqual("splatted-2x.png 2x", light.GetAttribute("srcset"));

        Assert.IsFalse(dark.HasAttribute("id"));
        Assert.IsFalse(dark.HasAttribute("srcset"));
        Assert.AreEqual("dark.png", dark.GetAttribute("src"));
        Assert.AreEqual("1", dark.GetAttribute("data-x"));
    }

    [TestMethod]
    public void BitImageShouldLeaveTheSourcesToTheImgOfSrc()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.Sources, [new BitImageSource { Srcset = "light.avif", Type = "image/avif" }]);
        });

        Assert.AreEqual(1, component.FindAll("picture > img.bit-img-lsc").Count);
        Assert.AreEqual(1, component.FindAll(".bit-img > img.bit-img-dsc").Count);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitImageShouldBeLoadedByTheImageOfEitherScheme(bool dark)
    {
        var loaded = 0;

        var component = RenderPair(parameters => parameters.Add(p => p.OnLoad, () => loaded++));

        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-hid"));
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-hid"));

        (dark ? Dark(component) : Light(component)).TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.AreEqual(1, loaded);
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-vis"));
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-vis"));
    }

    [TestMethod]
    public void BitImageShouldFallBackPerImage()
    {
        var component = RenderPair(parameters => parameters.Add(p => p.FallbackSrc, "fallback.png"));

        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual("fallback.png", Dark(component).GetAttribute("src"));
        Assert.AreEqual("light.png", Light(component).GetAttribute("src"));
        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
    }

    [TestMethod]
    public void BitImageShouldNotHideTheImageThatArrivedWhileTheOtherFallsBack()
    {
        var states = new List<BitImageState>();

        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.FallbackSrc, "fallback.png");
            parameters.Add(p => p.OnLoadingStateChange, (BitImageState s) => states.Add(s));
        });

        Light(component).TriggerEvent("onload", new ProgressEventArgs());
        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual("fallback.png", Dark(component).GetAttribute("src"));
        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        CollectionAssert.AreEqual(new[] { BitImageState.Loaded }, states);
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-vis"));
    }

    [TestMethod]
    public void BitImageShouldShowTheFailureOfOneImageInItsOwnSchemeOnly()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.Alt, "A lighthouse");
            parameters.Add(p => p.PlaceholderSrc, "tiny.png");
            parameters.Add(p => p.ErrorTemplate, (RenderFragment)(b => b.AddContent(0, "error!")));
        });

        Light(component).TriggerEvent("onload", new ProgressEventArgs());
        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());

        // The image of the light scheme is there, so the component is loaded and that image is shown.
        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-vis"));

        // The dark one has failed, and stands in as failed in its own scheme alone.
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-hid"));

        var template = component.Find(".bit-img-tpl");
        Assert.IsTrue(template.ClassList.Contains("bit-img-dsc"));
        Assert.AreEqual("error!", template.TextContent.Trim());

        var placeholder = component.Find(".bit-img-plc");
        Assert.IsTrue(placeholder.ClassList.Contains("bit-img-dsc"));
    }

    [TestMethod]
    public void BitImageShouldAnnounceTheAltOfTheFailedImageInItsOwnSchemeOnly()
    {
        var component = RenderPair(parameters => parameters.Add(p => p.Alt, "A lighthouse"));

        Dark(component).TriggerEvent("onload", new ProgressEventArgs());
        Light(component).TriggerEvent("onerror", new ErrorEventArgs());

        var alt = component.Find(".bit-img-alt");

        Assert.IsTrue(alt.ClassList.Contains("bit-img-lsc"));
        Assert.AreEqual("A lighthouse", alt.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitImageShouldKeepAFailureItsOwnOnceTheOtherImageArrives()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.ErrorTemplate, (RenderFragment)(b => b.AddContent(0, "error!")));
        });

        // Nothing has arrived yet, so the failure is the component's.
        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual(BitImageState.Error, component.Instance.LoadingState);

        var template = component.Find(".bit-img-tpl");
        Assert.IsFalse(template.ClassList.Contains("bit-img-dsc"));
        Assert.IsFalse(template.ClassList.Contains("bit-img-lsc"));

        // The image of the other scheme arriving loads it, and leaves the failure to the scheme of the failed one.
        Light(component).TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-vis"));
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-hid"));
        Assert.IsTrue(component.Find(".bit-img-tpl").ClassList.Contains("bit-img-dsc"));
    }

    [TestMethod]
    public void BitImageShouldRenderTheTemplatesOnceWhileBothImagesShareTheState()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.PlaceholderSrc, "tiny.png");
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(b => b.AddContent(0, "loading...")));
        });

        Assert.AreEqual(1, component.FindAll(".bit-img-tpl").Count);
        Assert.AreEqual(1, component.FindAll(".bit-img-plc").Count);
        Assert.AreEqual(0, component.FindAll(".bit-img-tpl.bit-img-lsc, .bit-img-tpl.bit-img-dsc, .bit-img-plc.bit-img-lsc, .bit-img-plc.bit-img-dsc").Count);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitImageShouldKeepTheStateWhenTheSourceOfOneSchemeAloneChanges(bool dark)
    {
        var component = RenderPair();

        Light(component).TriggerEvent("onload", new ProgressEventArgs());

        component.Render(parameters =>
        {
            if (dark)
            {
                parameters.Add(p => p.DarkSrc, "dark-2.png");
            }
            else
            {
                parameters.Add(p => p.Src, "light-2.png");
            }
        });

        // The image on screen may be the one that did not change, which has no load event coming.
        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.AreEqual(dark ? "dark-2.png" : "dark.png", Dark(component).GetAttribute("src"));
        Assert.AreEqual(dark ? "light.png" : "light-2.png", Light(component).GetAttribute("src"));
    }

    [TestMethod]
    public void BitImageShouldReturnToLoadingWhenTheSourcesOfBothSchemesChange()
    {
        var component = RenderPair();

        Light(component).TriggerEvent("onload", new ProgressEventArgs());

        component.Render(parameters =>
        {
            parameters.Add(p => p.Src, "light-2.png");
            parameters.Add(p => p.DarkSrc, "dark-2.png");
        });

        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
    }

    [TestMethod]
    public void BitImageShouldOfferTheFallbackAgainToAnImageWhoseSourceChanges()
    {
        var component = RenderPair(parameters => parameters.Add(p => p.FallbackSrc, "fallback.png"));

        Light(component).TriggerEvent("onload", new ProgressEventArgs());
        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());
        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());

        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-hid"));

        component.Render(parameters => parameters.Add(p => p.DarkSrc, "dark-2.png"));

        Assert.AreEqual("dark-2.png", Dark(component).GetAttribute("src"));
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-vis"));

        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());
        Assert.AreEqual("fallback.png", Dark(component).GetAttribute("src"));
    }

    [TestMethod]
    public void BitImageShouldTakeTheFailureOfItsImageAsItsStateOnceTheDarkSourceIsGone()
    {
        var component = RenderPair();

        Dark(component).TriggerEvent("onload", new ProgressEventArgs());
        Light(component).TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);

        component.Render(parameters => parameters.Add(p => p.DarkSrc, null));

        Assert.AreEqual(1, component.FindAll(".bit-img-img").Count);
        Assert.AreEqual(BitImageState.Error, component.Instance.LoadingState);
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-hid"));
    }

    [TestMethod]
    public async Task BitImageShouldReloadBothImages()
    {
        var component = RenderPair(parameters => parameters.Add(p => p.FallbackSrc, "fallback.png"));

        Light(component).TriggerEvent("onload", new ProgressEventArgs());
        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());
        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());

        await component.InvokeAsync(component.Instance.ReloadAsync);

        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
        Assert.AreEqual("light.png", Light(component).GetAttribute("src"));
        Assert.AreEqual("dark.png", Dark(component).GetAttribute("src"));
        Assert.IsFalse(component.Markup.Contains("bit-img-tpl"));
    }

    [TestMethod]
    public void BitImageShouldMakeBothImgsTheSameButton()
    {
        var clicks = 0;

        var component = RenderPair(parameters => parameters.Add(p => p.OnClick, () => clicks++));

        foreach (var img in component.FindAll(".bit-img-img"))
        {
            Assert.AreEqual("button", img.GetAttribute("role"));
            Assert.AreEqual("0", img.GetAttribute("tabindex"));
        }

        Dark(component).Click();
        Light(component).Click();

        Assert.AreEqual(2, clicks);

        // The Space key is kept from scrolling the page on whichever of the two is focused.
        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.registerPreventKeys"];

        Assert.AreEqual(2, invocations.Count);
        CollectionAssert.AreEquivalent(new[] { " ", "Spacebar" }, (string[])invocations[1].Arguments[1]!);
    }

    [TestMethod]
    public async Task BitImageShouldAskBothImgsToTakeTheFocus()
    {
        var component = RenderPair(parameters => parameters.Add(p => p.OnClick, () => { }));

        await component.InvokeAsync(() => component.Instance.FocusAsync(true).AsTask());

        var invocations = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];

        // The browser focuses nothing out of the layout, so the one of the scheme on screen is the one that answers;
        // the img of Src is asked last.
        Assert.AreEqual(2, invocations.Count);
        Assert.AreEqual(component.Instance.ImageElement, invocations[1].Arguments[0]);
        Assert.IsTrue(invocations.All(i => (bool)i.Arguments[1]! is true));
    }

    [TestMethod]
    public async Task BitImageShouldPrerenderBothImgsForTheStylesheetToChooseBetween()
    {
        var html = await Prerenderer.RenderAsync<BitImage>(new Dictionary<string, object?>
        {
            [nameof(BitImage.Src)] = "light.png",
            [nameof(BitImage.DarkSrc)] = "dark.png",
        });

        StringAssert.Contains(html, @"src=""light.png""");
        StringAssert.Contains(html, @"src=""dark.png""");
        StringAssert.Contains(html, "bit-img-lsc");
        StringAssert.Contains(html, "bit-img-dsc");
        Assert.DoesNotContain("bit-img-hid", html);
    }
}
