using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Image;

/// <summary>
/// What an image looks like before the page is interactive - prerendered, or statically rendered for good - and
/// how the interactive render takes it over. Nothing attaches a load handler to a prerendered or a static img,
/// so the image is left to the browser there rather than hidden until a load event that is never coming.
/// </summary>
[TestClass]
public class BitImagePrerenderTests : BunitTestContext
{
    [TestMethod]
    public async Task BitImageShouldPrerenderTheImageVisible()
    {
        var html = await Prerenderer.RenderAsync<BitImage>(new Dictionary<string, object?>
        {
            [nameof(BitImage.Src)] = "image.png",
            [nameof(BitImage.Alt)] = "A lighthouse",
        });

        StringAssert.Contains(html, @"src=""image.png""");
        Assert.DoesNotContain("bit-img-hid", html);

        // The img is on screen with its own alt, so there is no stand-in to announce in its place.
        Assert.DoesNotContain("bit-img-alt", html);
    }

    [TestMethod]
    public async Task BitImageShouldPrerenderALazyImageVisible()
    {
        var html = await Prerenderer.RenderAsync<BitImage>(new Dictionary<string, object?>
        {
            [nameof(BitImage.Src)] = "image.png",
            [nameof(BitImage.Loading)] = BitImageLoading.Lazy,
        });

        StringAssert.Contains(html, @"loading=""lazy""");
        Assert.DoesNotContain("bit-img-hid", html);
        Assert.DoesNotContain("bit-img-lzy", html);
    }

    [TestMethod]
    public async Task BitImageShouldPrerenderTheImageRatherThanTheLoadingTemplate()
    {
        var html = await Prerenderer.RenderAsync<BitImage>(new Dictionary<string, object?>
        {
            [nameof(BitImage.Src)] = "image.png",
            [nameof(BitImage.LoadingTemplate)] = (RenderFragment)(b => b.AddContent(0, "loading...")),
        });

        Assert.DoesNotContain("bit-img-hid", html);
        Assert.DoesNotContain("loading...", html);
    }

    [TestMethod]
    public async Task BitImageShouldPrerenderThePlaceholderBehindTheImage()
    {
        var html = await Prerenderer.RenderAsync<BitImage>(new Dictionary<string, object?>
        {
            [nameof(BitImage.Src)] = "image.png",
            [nameof(BitImage.PlaceholderSrc)] = "tiny.png",
        });

        StringAssert.Contains(html, @"src=""tiny.png""");
        StringAssert.Contains(html, "bit-img-nat");
        Assert.DoesNotContain("bit-img-hid", html);
    }

    [TestMethod]
    public async Task BitImageShouldNotPrerenderAFadeIn()
    {
        var html = await Prerenderer.RenderAsync<BitImage>(new Dictionary<string, object?>
        {
            [nameof(BitImage.Src)] = "image.png",
            [nameof(BitImage.FadeIn)] = true,
        });

        Assert.DoesNotContain("bit-img-hid", html);
        Assert.DoesNotContain("bit-img-fde", html);
    }

    [TestMethod]
    public async Task BitImageShouldPrerenderAnImageWithNothingToLoadHidden()
    {
        var html = await Prerenderer.RenderAsync<BitImage>(new Dictionary<string, object?>
        {
            [nameof(BitImage.Alt)] = "A lighthouse",
            [nameof(BitImage.LoadingTemplate)] = (RenderFragment)(b => b.AddContent(0, "loading...")),
        });

        StringAssert.Contains(html, "bit-img-hid");
        StringAssert.Contains(html, "loading...");
    }

    [TestMethod]
    public void BitImageShouldKeepAnImageTheBrowserHadAlreadyLoadedWhenItTakesItOver()
    {
        SetupImage(complete: true, naturalWidth: 640);

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.FadeIn, true);
            parameters.Add(p => p.PlaceholderSrc, "tiny.png");
        });

        var image = component.Find(".bit-img-img");

        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.IsTrue(image.ClassList.Contains("bit-img-vis"));
        Assert.IsFalse(image.ClassList.Contains("bit-img-nat"));

        // It was on screen all along, so there is no arrival to fade in and no placeholder left to fade out.
        Assert.IsFalse(image.ClassList.Contains("bit-img-fde"));
        Assert.AreEqual(0, component.FindAll(".bit-img-plc").Count);
    }

    [TestMethod]
    public void BitImageShouldReportTheLoadOfAnImageTheBrowserHadAlreadyLoadedOnce()
    {
        SetupImage(complete: true, naturalWidth: 640);

        var loaded = 0;
        var states = new List<BitImageState>();

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.OnLoad, () => loaded++);
            parameters.Add(p => p.OnLoadingStateChange, (BitImageState s) => states.Add(s));
        });

        // The load event of the img is still on its way, and it is what reports the load itself.
        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(1, loaded);
        CollectionAssert.AreEqual(new[] { BitImageState.Loaded }, states);
    }

    [TestMethod]
    public void BitImageShouldHideAnImageStillOnItsWayWhenItTakesItOver()
    {
        SetupImage(complete: false, naturalWidth: 0);

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(b => b.AddContent(0, "loading...")));
        });

        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-hid"));
        Assert.AreEqual(1, component.FindAll(".bit-img-tpl").Count);

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-vis"));
    }

    [TestMethod]
    public void BitImageShouldLeaveAnImageTheBrowserIsPaintingOnScreenWhenItTakesItOver()
    {
        SetupImage(complete: false, naturalWidth: 640);

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.FadeIn, true);
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(b => b.AddContent(0, "loading...")));
        });

        // Part of it is on screen already, so it is not taken away for the loading template only to come back.
        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
        Assert.IsFalse(component.Find(".bit-img-img").ClassList.Contains("bit-img-hid"));
        Assert.AreEqual(0, component.FindAll(".bit-img-tpl").Count);

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        // It was on screen as it arrived, so there is nothing to fade in.
        var image = component.Find(".bit-img-img");
        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.IsTrue(image.ClassList.Contains("bit-img-vis"));
        Assert.IsFalse(image.ClassList.Contains("bit-img-fde"));
        Assert.IsFalse(image.ClassList.Contains("bit-img-nat"));
    }

    [TestMethod]
    public void BitImageShouldNotTakeABrokenImageForALoadedOne()
    {
        // A failed image is complete as well; only the size it decoded tells the two apart.
        SetupImage(complete: true, naturalWidth: 0);

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
        });

        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-hid"));
    }

    [TestMethod]
    public void BitImageShouldShowAFallbackThatArrivesWhileItTakesTheImageOverAsItsOwn()
    {
        var state = Context.AddBunitPersistentComponentState();
        state.Persist("BitPrerendered", true);

        // The browser is still being asked about the image when its error event lands.
        var progress = Context.JSInterop.Setup<int>("BitBlazorUI.Utils.getImageProgress", _ => true);

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "missing.png");
            parameters.Add(p => p.FallbackSrc, "fallback.png");
            parameters.Add(p => p.FadeIn, true);
        });

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());

        // The fallback is a new image, so it is the component's from the start rather than the browser's until the
        // answer about the failed one lands - which would then take a fallback already painted away again.
        var image = component.Find(".bit-img-img");
        Assert.AreEqual("fallback.png", image.GetAttribute("src"));
        Assert.IsTrue(image.ClassList.Contains("bit-img-hid"));
        Assert.IsFalse(image.ClassList.Contains("bit-img-nat"));

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        // It arrives as the component's own, faded in.
        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-fde"));

        // The answer about the failed image that lands afterwards changes nothing.
        progress.SetResult(2);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
            Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-fde"));
        });
    }



    [TestMethod]
    public void BitImageShouldNotAskTheBrowserAboutAnImageOfAPageThatWasNeverPrerendered()
    {
        Context.AddBunitPersistentComponentState();

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.FadeIn, true);
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(b => b.AddContent(0, "loading...")));
        });

        // Nothing was on screen before this render, so the image is the component's from the start: hidden behind
        // its loading template, and faded in when it arrives.
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.getImageProgress"].Count);
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-hid"));
        Assert.AreEqual(1, component.FindAll(".bit-img-tpl").Count);

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-fde"));
    }



    // Answers the way Utils.getImageProgress reads an img: 2 finished, 1 painting, 0 nothing on screen (or broken),
    // to the interactive render that replaces a prerendered one, which the prerender carries over in the state.
    private void SetupImage(bool complete, int naturalWidth)
    {
        var state = Context.AddBunitPersistentComponentState();
        state.Persist("BitPrerendered", true);

        Context.JSInterop.Setup<int>("BitBlazorUI.Utils.getImageProgress", _ => true)
                         .SetResult(naturalWidth > 0 ? (complete ? 2 : 1) : 0);
    }
}
