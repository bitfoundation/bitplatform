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

        // Each image is shown by its own arrival: the other one is still on its way, and stays hidden until it is there.
        Assert.AreEqual(dark is false, Light(component).ClassList.Contains("bit-img-vis"));
        Assert.AreEqual(dark, Dark(component).ClassList.Contains("bit-img-vis"));

        // The other image arriving is the same picture in the other scheme, so the load is reported once for the pair.
        (dark ? Light(component) : Dark(component)).TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(1, loaded);
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-vis"));
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-vis"));

        // A new load of an image already there (a srcset swapping in another candidate) is reported as ever.
        Light(component).TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(2, loaded);
    }

    [TestMethod]
    public void BitImageShouldKeepThePlaceholderAndTheTemplateOfTheImageStillOnItsWay()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.FadeIn, true);
            parameters.Add(p => p.PlaceholderSrc, "tiny.png");
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(b => b.AddContent(0, "loading...")));
        });

        // The small dark picture arrives first, in a light page whose own picture is still downloading.
        Dark(component).TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);

        // The light image is not shown half-painted: it keeps its placeholder and its loading template, in its scheme.
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-hid"));
        Assert.IsTrue(component.Find(".bit-img-tpl").ClassList.Contains("bit-img-lsc"));
        Assert.IsTrue(component.Find(".bit-img-plc:not(.bit-img-pfo)").ClassList.Contains("bit-img-lsc"));

        // The dark one fades in, its own placeholder fading out over it.
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-fde"));
        Assert.IsTrue(component.Find(".bit-img-plc.bit-img-pfo").ClassList.Contains("bit-img-dsc"));

        // When the light image arrives it fades in as well, and its template goes.
        Light(component).TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-fde"));
        Assert.AreEqual(0, component.FindAll(".bit-img-tpl").Count);
    }

    [TestMethod]
    public void BitImageShouldLetThePlaceholderOfOneSchemeGoOnItsOwnFade()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.FadeIn, true);
            parameters.Add(p => p.PlaceholderSrc, "tiny.png");
        });

        Light(component).TriggerEvent("onload", new ProgressEventArgs());

        component.Find(".bit-img-plc.bit-img-lsc").TriggerEvent("onanimationend", new BitAnimationEventArgs { AnimationName = "bit-img-fade-out" });

        // The light placeholder has gone; the dark one still stands in for the image on its way.
        Assert.AreEqual(1, component.FindAll(".bit-img-plc").Count);
        Assert.IsTrue(component.Find(".bit-img-plc").ClassList.Contains("bit-img-dsc"));
    }

    [TestMethod]
    public void BitImageShouldPlayTheFadeOnceRatherThanOnEveryChangeOfTheScheme()
    {
        var component = RenderPair(parameters => parameters.Add(p => p.FadeIn, true));

        Light(component).TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-fde"));

        // An animation of the page's own on the img does not cut the fade short.
        Light(component).TriggerEvent("onanimationend", new BitAnimationEventArgs { AnimationName = "app-pulse" });

        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-fde"));

        // Once the fade has ended it is taken off, so the img coming back into the box tree does not play it again.
        Light(component).TriggerEvent("onanimationend", new BitAnimationEventArgs { AnimationName = "bit-img-fade-in" });

        Assert.IsFalse(Light(component).ClassList.Contains("bit-img-fde"));
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-vis"));

        // The dark image has its own fade to play when it arrives.
        Dark(component).TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-fde"));
        Assert.IsFalse(Light(component).ClassList.Contains("bit-img-fde"));
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

        // Nothing has arrived yet, so the failure is the component's - and the failed image's alone to show.
        Dark(component).TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual(BitImageState.Error, component.Instance.LoadingState);
        Assert.IsTrue(component.Find(".bit-img-tpl").ClassList.Contains("bit-img-dsc"));

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
        Dark(component).TriggerEvent("onload", new ProgressEventArgs());

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

        // The image whose source stayed keeps its place, and the new picture is hidden until it arrives.
        Assert.AreEqual(dark is false, Light(component).ClassList.Contains("bit-img-hid"));
        Assert.AreEqual(dark, Dark(component).ClassList.Contains("bit-img-hid"));
    }

    [TestMethod]
    public void BitImageShouldHideANewPictureOfOneSchemeUntilItArrives()
    {
        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.FadeIn, true);
            parameters.Add(p => p.PlaceholderSrc, "tiny.png");
        });

        Light(component).TriggerEvent("onload", new ProgressEventArgs());
        Dark(component).TriggerEvent("onload", new ProgressEventArgs());

        component.Render(parameters => parameters.Add(p => p.Src, "light-2.png"));

        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-hid"));
        Assert.IsTrue(component.Find(".bit-img-plc:not(.bit-img-pfo)").ClassList.Contains("bit-img-lsc"));
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-vis"));

        Light(component).TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-vis"));
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-fde"));
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
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-hid"));

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
    public void BitImageShouldKeepTheFailureOfItsImageOnceADarkSourceIsAdded()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "light.png");
            parameters.Add(p => p.ErrorTemplate, (RenderFragment)(b => b.AddContent(0, "error!")));
        });

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual(BitImageState.Error, component.Instance.LoadingState);

        component.Render(parameters => parameters.Add(p => p.DarkSrc, "dark.png"));
        Dark(component).TriggerEvent("onload", new ProgressEventArgs());

        // The light image failed before there was a second one, and still has: it is not shown as a broken img.
        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-hid"));
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-vis"));
        Assert.IsTrue(component.Find(".bit-img-tpl").ClassList.Contains("bit-img-lsc"));
    }

    [TestMethod]
    public void BitImageShouldTakeOverEachImageFromTheBrowserOnItsOwn()
    {
        var state = Context.AddBunitPersistentComponentState();
        state.Persist("BitPrerendered", true);

        // The img of Src is asked first: nothing of the light picture is on screen yet, while the browser has already
        // finished the dark one.
        var asked = new List<string>();

        int Order(JSRuntimeInvocation invocation)
        {
            var id = ((ElementReference)invocation.Arguments[0]!).Id;
            if (asked.Contains(id) is false) asked.Add(id);
            return asked.IndexOf(id);
        }

        Context.JSInterop.Setup<int>("BitBlazorUI.Utils.getImageProgress", i => Order(i) == 0).SetResult(0);
        Context.JSInterop.Setup<int>("BitBlazorUI.Utils.getImageProgress", i => Order(i) == 1).SetResult(2);

        var component = RenderPair(parameters =>
        {
            parameters.Add(p => p.FadeIn, true);
            parameters.Add(p => p.PlaceholderSrc, "tiny.png");
        });

        component.WaitForAssertion(() => Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState));

        // The dark image was on screen all along: no fade.
        Assert.IsTrue(Dark(component).ClassList.Contains("bit-img-vis"));
        Assert.IsFalse(Dark(component).ClassList.Contains("bit-img-fde"));

        // The light one is the component's to show: hidden behind its placeholder until it arrives.
        Assert.IsTrue(Light(component).ClassList.Contains("bit-img-hid"));
        Assert.AreEqual(1, component.FindAll(".bit-img-plc").Count);
        Assert.IsTrue(component.Find(".bit-img-plc").ClassList.Contains("bit-img-lsc"));
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
