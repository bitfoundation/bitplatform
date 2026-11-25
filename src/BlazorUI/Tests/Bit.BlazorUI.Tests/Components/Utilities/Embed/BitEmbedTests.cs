using System;
using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Embed;

[TestClass, DoNotParallelize]
public class BitEmbedTests : BunitTestContext
{
    private const string VIDEO = "https://www.youtube-nocookie.com/embed/abc123";

    private sealed class GermanStrings : BitStrings
    {
        public override string LoadFormat => "{0} laden";
    }

    private sealed class BrokenStrings : BitStrings
    {
        public override string LoadFormat => "Load {title}";
    }

    [TestCleanup]
    public void ResetStrings() => BitStrings.Current = null;

    private IRenderedComponent<BitEmbed> Render(Action<ComponentParameterCollectionBuilder<BitEmbed>>? parameters = null, string? src = VIDEO)
        => RenderComponent<BitEmbed>(p =>
        {
            p.Add(x => x.Src, src);
            p.Add(x => x.Title, "Getting started");
            parameters?.Invoke(p);
        });

    private static readonly RenderFragment Thumbnail = builder => builder.AddMarkupContent(0, "<img class=\"thumb\" src=\"thumb.jpg\" alt=\"\" />");

    [TestMethod]
    public void BitEmbedShouldFrameALazyIframeOfItsSourceAndTitle()
    {
        var com = Render();

        var root = com.Find(".bit-emb");
        var frame = com.Find(".bit-emb > iframe.bit-emb-frm");

        Assert.AreEqual(1, root.Children.Length);
        Assert.AreEqual(VIDEO, frame.GetAttribute("src"));
        Assert.AreEqual("Getting started", frame.GetAttribute("title"));
        Assert.AreEqual("lazy", frame.GetAttribute("loading"));

        // Nothing is allowed, lifted or restricted that the app did not ask for.
        Assert.IsFalse(frame.HasAttribute("allow"));
        Assert.IsFalse(frame.HasAttribute("allowfullscreen"));
        Assert.IsFalse(frame.HasAttribute("referrerpolicy"));
        Assert.IsFalse(frame.HasAttribute("sandbox"));

        // An unset AspectRatio and MaxWidth publish nothing, so the stylesheet's defaults and the public variables apply.
        Assert.IsTrue(string.IsNullOrEmpty(root.GetAttribute("style")));
    }

    [TestMethod]
    public void BitEmbedShouldLoadEagerlyWhenAsked()
    {
        var com = Render(p => p.Add(x => x.Loading, BitImageLoading.Eager));

        Assert.AreEqual("eager", com.Find(".bit-emb-frm").GetAttribute("loading"));
    }

    [TestMethod]
    public void BitEmbedShouldRenderTheIframeAttributesItIsGiven()
    {
        var com = Render(p =>
        {
            p.Add(x => x.Allow, "autoplay; encrypted-media; picture-in-picture");
            p.Add(x => x.AllowFullScreen, true);
            p.Add(x => x.ReferrerPolicy, BitImageReferrerPolicy.StrictOriginWhenCrossOrigin);
            p.Add(x => x.Sandbox, "allow-scripts allow-same-origin");
        });

        var frame = com.Find(".bit-emb-frm");

        Assert.AreEqual("autoplay; encrypted-media; picture-in-picture", frame.GetAttribute("allow"));
        Assert.IsTrue(frame.HasAttribute("allowfullscreen"));
        Assert.AreEqual("strict-origin-when-cross-origin", frame.GetAttribute("referrerpolicy"));
        Assert.AreEqual("allow-scripts allow-same-origin", frame.GetAttribute("sandbox"));
    }

    [TestMethod]
    public void BitEmbedShouldRenderAnEmptySandboxThatAppliesEveryRestriction()
    {
        var com = Render(p => p.Add(x => x.Sandbox, ""));

        Assert.AreEqual("", com.Find(".bit-emb-frm").GetAttribute("sandbox"));
    }

    [TestMethod]
    public void BitEmbedShouldWriteAspectRatioAndMaxWidthAsItsPrivateVariables()
    {
        var com = Render(p =>
        {
            p.Add(x => x.AspectRatio, "4 / 3");
            p.Add(x => x.MaxWidth, "800px");
        });

        var style = com.Find(".bit-emb").GetAttribute("style") ?? "";

        StringAssert.Contains(style, "--bit-emb-ar:4 / 3");
        StringAssert.Contains(style, "--bit-emb-mxw:800px");

        com.Render(p =>
        {
            p.Add(x => x.AspectRatio, (string?)null);
            p.Add(x => x.MaxWidth, (string?)null);
        });

        Assert.IsTrue(string.IsNullOrEmpty(com.Find(".bit-emb").GetAttribute("style")));
    }

    [TestMethod]
    [DataRow("https://example.com/embed")]
    [DataRow("HTTP://example.com/embed")]
    [DataRow("//example.com/embed")]
    [DataRow("/videos/intro.html")]
    [DataRow("videos/intro.html")]
    [DataRow("intro.html?t=10:30")]
    [DataRow("#section:1")]
    public void BitEmbedShouldRenderAnHttpOrRelativeAddress(string src)
    {
        var com = Render(src: src);

        Assert.AreEqual(src, com.Find(".bit-emb-frm").GetAttribute("src"));
    }

    [TestMethod]
    [DataRow("javascript:alert(1)")]
    [DataRow("  JavaScript:alert(1)")]
    [DataRow("java\tscript:alert(1)")]
    [DataRow("java\nscript:alert(1)")]
    [DataRow("vbscript:msgbox(1)")]
    [DataRow("data:text/html,<script>alert(1)</script>")]
    [DataRow("blob:https://example.com/1234")]
    [DataRow("file:///etc/passwd")]
    [DataRow("")]
    [DataRow("   ")]
    public void BitEmbedShouldRefuseAnyOtherAddress(string src)
    {
        var com = Render(src: src);

        // The frame stays, empty: no iframe is rendered for an address that is not one an iframe may run.
        Assert.IsNotNull(com.Find(".bit-emb"));
        Assert.AreEqual(0, com.FindAll("iframe").Count);
    }

    [TestMethod]
    public void BitEmbedShouldFollowAChangeOfItsSource()
    {
        var com = Render();

        com.Render(p => p.Add(x => x.Src, "javascript:alert(1)"));

        Assert.AreEqual(0, com.FindAll("iframe").Count);

        com.Render(p => p.Add(x => x.Src, "https://example.com/other"));

        Assert.AreEqual("https://example.com/other", com.Find(".bit-emb-frm").GetAttribute("src"));
    }

    [TestMethod]
    public void BitEmbedShouldRenderThePlaceholderInPlaceOfTheIframeUntilItIsActivated()
    {
        var com = Render(p => p.Add(x => x.PlaceholderTemplate, Thumbnail));

        Assert.AreEqual(0, com.FindAll("iframe").Count);

        // A native button, so the keyboard and a screen reader reach it like any other, named by what it does.
        var placeholder = com.Find(".bit-emb > button.bit-emb-plh");

        Assert.AreEqual("button", placeholder.GetAttribute("type"));
        Assert.AreEqual("Load Getting started", placeholder.GetAttribute("aria-label"));
        Assert.IsNotNull(com.Find(".bit-emb-plh > img.thumb"));

        placeholder.Click();

        Assert.AreEqual(0, com.FindAll(".bit-emb-plh").Count);
        Assert.AreEqual(VIDEO, com.Find(".bit-emb-frm").GetAttribute("src"));

        // The iframe takes the focus of the button it replaced.
        Context.JSInterop.VerifyFocusAsyncInvoke();
    }

    [TestMethod]
    public void BitEmbedShouldNameThePlaceholderWithItsAriaLabelOrTheTranslatedStrings()
    {
        var com = Render(p =>
        {
            p.Add(x => x.PlaceholderTemplate, Thumbnail);
            p.Add(x => x.PlaceholderAriaLabel, "Play the getting started video");
        });

        Assert.AreEqual("Play the getting started video", com.Find(".bit-emb-plh").GetAttribute("aria-label"));

        BitStrings.Current = new GermanStrings();

        com.Render(p => p.Add(x => x.PlaceholderAriaLabel, (string?)null));

        Assert.AreEqual("Getting started laden", com.Find(".bit-emb-plh").GetAttribute("aria-label"));

        BitStrings.Current = null;

        com.Render(p => p.Add(x => x.Title, (string?)null));

        Assert.AreEqual("Load", com.Find(".bit-emb-plh").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitEmbedShouldNameThePlaceholderWithItsTitleWhenTheTranslatedFormatIsBroken()
    {
        BitStrings.Current = new BrokenStrings();

        var com = Render(p => p.Add(x => x.PlaceholderTemplate, Thumbnail));

        Assert.AreEqual("Getting started", com.Find(".bit-emb-plh").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitEmbedShouldAskAgainBeforeLoadingAnotherAddress()
    {
        var com = Render(p => p.Add(x => x.PlaceholderTemplate, Thumbnail));

        com.Find(".bit-emb-plh").Click();

        Assert.AreEqual(VIDEO, com.Find(".bit-emb-frm").GetAttribute("src"));

        // The click agreed to load one address: another one waits for its own.
        com.Render(p => p.Add(x => x.Src, "https://player.example.com/video/1?autoplay=1"));

        Assert.AreEqual(0, com.FindAll("iframe").Count);

        com.Find(".bit-emb-plh").Click();

        Assert.AreEqual("https://player.example.com/video/1?autoplay=1", com.Find(".bit-emb-frm").GetAttribute("src"));

        // The same address handed again is no new address, so the loaded embed stays.
        com.Render(p => p.Add(x => x.Src, " https://player.example.com/video/1?autoplay=1 "));

        Assert.AreEqual(0, com.FindAll(".bit-emb-plh").Count);
        Assert.AreEqual(1, com.FindAll("iframe").Count);
    }

    [TestMethod]
    public void BitEmbedShouldIgnoreAClickMadeOnThePlaceholderOfAnEarlierAddress()
    {
        var com = Render(p => p.Add(x => x.PlaceholderTemplate, Thumbnail));

        var staleHandlerId = ulong.Parse(com.Find(".bit-emb-plh").GetAttribute("blazor:onclick")!, CultureInfo.InvariantCulture);

        // A new address arrives while the click made on the placeholder of the first one is still in flight.
        com.Render(p => p.Add(x => x.Src, "https://player.example.com/video/1"));

        // The placeholder of the new address is a new button, so the handler of the old one is not redirected to it,
        // and a click it still delivers agreed to the old address alone.
        Assert.AreNotEqual(staleHandlerId, ulong.Parse(com.Find(".bit-emb-plh").GetAttribute("blazor:onclick")!, CultureInfo.InvariantCulture));

        Context.Renderer.DispatchEventAsync(staleHandlerId, new EventFieldInfo(), new MouseEventArgs(), ignoreUnknownEventHandlers: true).GetAwaiter().GetResult();

        Assert.AreEqual(0, com.FindAll("iframe").Count);
        Assert.AreEqual(1, com.FindAll(".bit-emb-plh").Count);

        com.Find(".bit-emb-plh").Click();

        Assert.AreEqual("https://player.example.com/video/1", com.Find(".bit-emb-frm").GetAttribute("src"));
    }

    [TestMethod]
    public void BitEmbedShouldRenderItsTabIndexOnThePlaceholderAndTheIframe()
    {
        var com = Render(p =>
        {
            p.Add(x => x.PlaceholderTemplate, Thumbnail);
            p.Add(x => x.TabIndex, "-1");
        });

        Assert.AreEqual("-1", com.Find(".bit-emb-plh").GetAttribute("tabindex"));

        com.Find(".bit-emb-plh").Click();

        Assert.AreEqual("-1", com.Find(".bit-emb-frm").GetAttribute("tabindex"));

        // Unset, the browser's own tab order applies to both.
        com.Render(p => p.Add(x => x.TabIndex, (string?)null));

        Assert.IsFalse(com.Find(".bit-emb-frm").HasAttribute("tabindex"));

        com.Render(p => p.Add(x => x.Src, "https://player.example.com/video/1"));

        Assert.IsFalse(com.Find(".bit-emb-plh").HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitEmbedShouldShowNoPlaceholderWithoutAnAddressToLoad()
    {
        var com = Render(p => p.Add(x => x.PlaceholderTemplate, Thumbnail), src: "javascript:alert(1)");

        Assert.AreEqual(0, com.FindAll(".bit-emb-plh").Count);
        Assert.AreEqual(0, com.FindAll("iframe").Count);

        com.Render(p => p.Add(x => x.Src, (string?)null));

        Assert.AreEqual(0, com.FindAll(".bit-emb-plh").Count);

        com.Render(p => p.Add(x => x.Src, VIDEO));

        Assert.AreEqual(1, com.FindAll(".bit-emb-plh").Count);
        Assert.AreEqual(0, com.FindAll("iframe").Count);
    }

    [TestMethod]
    public void BitEmbedShouldNotActivateADisabledPlaceholder()
    {
        var com = Render(p =>
        {
            p.Add(x => x.PlaceholderTemplate, Thumbnail);
            p.Add(x => x.Disabled, true);
        });

        var placeholder = com.Find(".bit-emb-plh");

        Assert.IsTrue(placeholder.HasAttribute("disabled"));
        Assert.AreEqual("true", placeholder.GetAttribute("aria-disabled"));
        Assert.IsTrue(com.Find(".bit-emb").ClassList.Contains("bit-dis"));

        placeholder.Click();

        Assert.AreEqual(0, com.FindAll("iframe").Count);

        com.Render(p => p.Add(x => x.Disabled, false));

        Assert.IsFalse(com.Find(".bit-emb-plh").HasAttribute("disabled"));
        Assert.IsNull(com.Find(".bit-emb-plh").GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitEmbedShouldApplyItsClassesAndStylesToItsParts()
    {
        var classes = new BitEmbedClassStyles { Root = "c-root", Placeholder = "c-plh", Frame = "c-frm" };
        var styles = new BitEmbedClassStyles { Root = "color:red", Placeholder = "color:green", Frame = "color:blue" };

        var com = Render(p =>
        {
            p.Add(x => x.PlaceholderTemplate, Thumbnail);
            p.Add(x => x.Classes, classes);
            p.Add(x => x.Styles, styles);
        });

        Assert.IsTrue(com.Find(".bit-emb").ClassList.Contains("c-root"));
        StringAssert.Contains(com.Find(".bit-emb").GetAttribute("style"), "color:red");
        Assert.IsTrue(com.Find(".bit-emb-plh").ClassList.Contains("c-plh"));
        Assert.AreEqual("color:green", com.Find(".bit-emb-plh").GetAttribute("style"));

        com.Find(".bit-emb-plh").Click();

        Assert.IsTrue(com.Find(".bit-emb-frm").ClassList.Contains("c-frm"));
        Assert.AreEqual("color:blue", com.Find(".bit-emb-frm").GetAttribute("style"));
    }

    [TestMethod]
    public void BitEmbedShouldRenderItsDirection()
    {
        var com = Render(p => p.Add(x => x.Dir, BitDir.Rtl));

        var root = com.Find(".bit-emb");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitEmbedTitleShouldBeEditorRequired()
    {
        var title = typeof(BitEmbed).GetProperty(nameof(BitEmbed.Title))!;

        Assert.IsTrue(title.GetCustomAttributes(typeof(EditorRequiredAttribute), false).Any());
    }
}
