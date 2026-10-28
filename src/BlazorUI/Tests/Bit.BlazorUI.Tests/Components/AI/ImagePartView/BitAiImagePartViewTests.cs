using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ImagePartView;

[TestClass]
public class BitAiImagePartViewTests : BitAiTestBase
{
    [TestMethod]
    public void BitAiImagePartViewShouldRenderTheImageInAFigure()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat", Width = 640, Height = 480 };

        var component = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, part));

        var root = component.Find(".bit-aiipv");
        Assert.AreEqual("FIGURE", root.TagName);
        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));

        var image = root.QuerySelector("img.bit-aiipv-img")!;
        Assert.AreEqual("https://img.x.dev/cat.png", image.GetAttribute("src"));
        Assert.AreEqual("A cat", image.GetAttribute("alt"));
        Assert.AreEqual("640", image.GetAttribute("width"));
        Assert.AreEqual("480", image.GetAttribute("height"));
        Assert.AreEqual("lazy", image.GetAttribute("loading"));
        Assert.AreEqual("async", image.GetAttribute("decoding"));

        Assert.IsEmpty(component.FindAll("figcaption"));
    }

    [TestMethod]
    public void HowTheImageLoadsIsWrittenAheadOfWhatItLoads()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat", Width = 640, Height = 480 };

        var image = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, part)).Find("img");
        var names = image.Attributes.Select(a => a.Name).ToList();

        // Blazor sets the attributes of a new element in the order they are written, and an engine handed the source
        // before the loading attribute starts the fetch right then: a lazy image would load at once, wherever it is.
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("loading"));
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("decoding"));
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("width"));
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("height"));
    }

    [TestMethod]
    public void AnImageWithoutAlternativeTextIsMarkedDecorativeAndHasNoSize()
    {
        var component = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, new BitAiImagePart("https://img.x.dev/cat.png")));

        var image = component.Find("img");

        Assert.AreEqual(string.Empty, image.GetAttribute("alt"));
        Assert.IsFalse(image.HasAttribute("width"));
        Assert.IsFalse(image.HasAttribute("height"));
    }

    [TestMethod]
    public void TheCaptionIsRenderedWhenThePartHasOne()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Caption = "A cat on a mat" };

        var component = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, part));

        Assert.AreEqual("A cat on a mat", component.Find(".bit-aiipv > figcaption.bit-aiipv-cap").TextContent);
    }

    [TestMethod]
    public void TheAllowPolicyShowsTheImage()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat" };

        var component = RenderImage(part, BitAiImagePolicy.Allow);

        Assert.AreEqual("https://img.x.dev/cat.png", component.Find("img.bit-aiipv-img").GetAttribute("src"));
        Assert.IsEmpty(component.FindAll("button"));
        Assert.IsEmpty(component.FindAll(".bit-aiipv-imb"));
    }

    [TestMethod]
    public void TheClickToLoadPolicyShowsAButtonThatLoadsTheImage()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat", Width = 640, Height = 480 };

        var component = RenderImage(part, BitAiImagePolicy.ClickToLoad);

        // Nothing is fetched until the user asks.
        Assert.IsEmpty(component.FindAll("img"));
        var placeholder = component.Find(".bit-aiipv > button.bit-acb");
        Assert.AreEqual("Load image: A cat", placeholder.TextContent.Trim());
        Assert.AreEqual("https://img.x.dev/cat.png", placeholder.GetAttribute("title"));

        // The glyph is drawn from the package's own icon subset.
        var icon = placeholder.QuerySelector(".bit-acb-ico")!;
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--Photo2"));

        placeholder.Click();

        var image = component.Find("img.bit-aiipv-img");
        Assert.AreEqual("https://img.x.dev/cat.png", image.GetAttribute("src"));
        Assert.AreEqual("A cat", image.GetAttribute("alt"));
        Assert.AreEqual("640", image.GetAttribute("width"));
        Assert.IsEmpty(component.FindAll("button.bit-acb"));
    }

    // The button pressed is gone once the image is shown, and the focus would drop to the body of the document with it.
    [TestMethod]
    public void TheImageTheUserLoadedTakesTheFocusOffThePlaceholder()
    {
        Services.AddSingleton(BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad });
        var component = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat" }));

        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);

        component.Find("button.bit-acb").Click();

        Assert.AreEqual("-1", component.Find("img.bit-aiipv-img").GetAttribute("tabindex"));

        var focus = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];
        Assert.HasCount(1, focus);
        Assert.AreNotEqual(component.Instance.RootElement.Id, ((Microsoft.AspNetCore.Components.ElementReference)focus[0].Arguments[0]!).Id);

        // Rendered again, the image keeps the focus where it is.
        component.Render(p => p.Add(c => c.Class, "again"));

        Assert.HasCount(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void AnImageThePolicyShowsAtOnceTakesNoFocus()
    {
        var component = RenderImage(new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat" }, BitAiImagePolicy.Allow);

        Assert.IsFalse(component.Find("img.bit-aiipv-img").HasAttribute("tabindex"));
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void APlaceholderWithoutAlternativeTextIsNamedAfterTheHostTheImageComesFrom()
    {
        var remote = RenderImage(new BitAiImagePart("https://img.x.dev/cat.png"), BitAiImagePolicy.ClickToLoad);
        var relative = RenderImage(new BitAiImagePart("/img/cat.png") { Alt = " " }, BitAiImagePolicy.ClickToLoad);

        Assert.AreEqual("Load image: img.x.dev", remote.Find("button.bit-acb").TextContent.Trim());

        // A relative image is fetched from the app's own host, the base address of the document.
        Assert.AreEqual("Load image: localhost", relative.Find("button.bit-acb").TextContent.Trim());
        Assert.AreEqual("/img/cat.png", relative.Find("button.bit-acb").GetAttribute("title"));
    }

    [TestMethod]
    public void APartHandedAnotherImageAsksAgain()
    {
        Services.AddSingleton(BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad });
        var component = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat" }));

        component.Find("button.bit-acb").Click();
        Assert.IsNotNull(component.Find("img"));

        component.Render(p => p.Add(c => c.Part, new BitAiImagePart("https://img.x.dev/dog.png") { Alt = "A dog" }));

        Assert.IsEmpty(component.FindAll("img"));
        Assert.AreEqual("Load image: A dog", component.Find("button.bit-acb").TextContent.Trim());
    }

    [TestMethod]
    public void TheBlockPolicyShowsTheAlternativeTextInPlaceOfTheImage()
    {
        var withAlt = RenderImage(new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat" }, BitAiImagePolicy.Block);
        var withoutAlt = RenderImage(new BitAiImagePart("https://img.x.dev/cat.png"), BitAiImagePolicy.Block);

        Assert.IsEmpty(withAlt.FindAll("img"));
        Assert.IsEmpty(withAlt.FindAll("button"));
        var blocked = withAlt.Find(".bit-aiipv > span.bit-aiipv-imb");
        Assert.AreEqual("A cat", blocked.TextContent);

        // With nothing to say about the image, nothing is left of it.
        Assert.IsEmpty(withoutAlt.FindAll("img"));
        Assert.IsEmpty(withoutAlt.FindAll(".bit-aiipv-imb"));
        Assert.AreEqual(0, withoutAlt.Find(".bit-aiipv").ChildElementCount);
    }

    [TestMethod]
    public void TheHostListPolicyShowsARelativeImageOfTheAppsOwnHost()
    {
        // bUnit's base address is http://localhost/.
        var own = RenderImage(new BitAiImagePart("/img/cat.png") { Alt = "A cat" }, BitAiImagePolicy.AllowListedHosts("localhost"));
        var cdn = RenderImage(new BitAiImagePart("/img/cat.png") { Alt = "A cat" }, BitAiImagePolicy.AllowListedHosts("cdn.x.dev"));
        var listed = RenderImage(new BitAiImagePart("https://cdn.x.dev/cat.png") { Alt = "A cat" }, BitAiImagePolicy.AllowListedHosts("cdn.x.dev"));

        Assert.AreEqual("/img/cat.png", own.Find("img.bit-aiipv-img").GetAttribute("src"));
        Assert.IsEmpty(cdn.FindAll("img"));
        Assert.AreEqual("Load image: A cat", cdn.Find("button.bit-acb").TextContent.Trim());
        Assert.AreEqual("https://cdn.x.dev/cat.png", listed.Find("img.bit-aiipv-img").GetAttribute("src"));
    }

    [TestMethod]
    public void ARelativeImageRendersWhereThereIsNoBaseAddressToRead()
    {
        // A navigation manager that was never initialized - a view an HtmlRenderer of the app's own renders - throws on
        // its base address. The view never reads it unless a host list needs it, and then takes it for unknown.
        Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager>(new UninitializedNavigationManager());

        var allowed = RenderImage(new BitAiImagePart("/img/cat.png") { Alt = "A cat" }, BitAiImagePolicy.Allow);
        var listed = RenderImage(new BitAiImagePart("/img/cat.png"), BitAiImagePolicy.AllowListedHosts("localhost"));

        Assert.AreEqual("/img/cat.png", allowed.Find("img.bit-aiipv-img").GetAttribute("src"));
        Assert.IsEmpty(listed.FindAll("img"));
        Assert.AreEqual("Load image: /img/cat.png", listed.Find("button.bit-acb").TextContent.Trim());
    }

    [TestMethod]
    public void InlineImageDataIsShownUnlessThePolicyBlocksItOrItsData()
    {
        const string data = "data:image/png;base64,iVBORw0KGgo=";

        Assert.AreEqual(data, RenderImage(new BitAiImagePart(data), BitAiImagePolicy.ClickToLoad).Find("img").GetAttribute("src"));
        Assert.AreEqual(data, RenderImage(new BitAiImagePart(data), BitAiImagePolicy.AllowListedHosts("cdn.x.dev")).Find("img").GetAttribute("src"));
        Assert.IsEmpty(RenderImage(new BitAiImagePart(data) { Alt = "x" }, BitAiImagePolicy.Block).FindAll("img"));
        Assert.IsEmpty(RenderImage(new BitAiImagePart(data) { Alt = "x" }, BitAiImagePolicy.Allow with { AllowDataImages = false }).FindAll("img"));
    }

    [TestMethod,
        DataRow(BitAiImageMode.Allow),
        DataRow(BitAiImageMode.ClickToLoad),
        DataRow(BitAiImageMode.Block),
        DataRow(BitAiImageMode.AllowListedHosts)]
    public void TheCaptionIsRenderedUnderEveryPolicy(BitAiImageMode mode)
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat", Caption = "A cat on a mat" };

        var component = RenderImage(part, Policy(mode));

        Assert.AreEqual("A cat on a mat", component.Find(".bit-aiipv > figcaption.bit-aiipv-cap").TextContent);
    }

    [TestMethod,
        DataRow(BitAiImageMode.Allow),
        DataRow(BitAiImageMode.ClickToLoad),
        DataRow(BitAiImageMode.Block),
        DataRow(BitAiImageMode.AllowListedHosts)]
    public void AScriptSourceNeverReachesTheImageUnderAnyPolicy(BitAiImageMode mode)
    {
        var part = new BitAiImagePart("javascript:alert(document.cookie)") { Alt = "x" };

        var component = RenderImage(part, Policy(mode));

        // The blank pixel stands in for it, which fetches nothing and so is shown under every policy.
        Assert.IsFalse(component.Markup.Contains("javascript:", StringComparison.OrdinalIgnoreCase));
        StringAssert.StartsWith(component.Find("img.bit-aiipv-img").GetAttribute("src"), "data:image/gif;base64,");
    }

    [TestMethod]
    public void ThePlaceholderAndTheBlockedImageTakeTheClassesAndStylesOfTheirParts()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat" };
        var classes = new BitAiImagePartViewClassStyles { ImagePlaceholder = new() { Root = "c-placeholder", Icon = "c-icon" }, BlockedImage = "c-blocked" };
        var styles = new BitAiImagePartViewClassStyles { ImagePlaceholder = new() { Root = "margin: 1px" }, BlockedImage = "margin: 2px" };

        var asked = RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, part)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad })
            .Add(c => c.Classes, classes)
            .Add(c => c.Styles, styles));
        var blocked = RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, part)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block })
            .Add(c => c.Classes, classes)
            .Add(c => c.Styles, styles));

        var placeholder = asked.Find("button.bit-acb");
        Assert.IsTrue(placeholder.ClassList.Contains("c-placeholder"));
        StringAssert.Contains(placeholder.GetAttribute("style"), "margin: 1px");

        // The app's icon classes beside the glyph's.
        var icon = asked.Find("button.bit-acb .bit-acb-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--Photo2"));
        Assert.IsTrue(icon.ClassList.Contains("c-icon"));

        var span = blocked.Find(".bit-aiipv-imb");
        Assert.IsTrue(span.ClassList.Contains("c-blocked"));
        Assert.AreEqual("margin: 2px", span.GetAttribute("style"));
    }

    [TestMethod]
    public void ThePolicyIsReadFromTheRegisteredOptionsAndFromAProvider()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat" };
        Services.AddSingleton(BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block });

        var registered = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, part));
        var provided = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad })
            .AddChildContent<BitAiImagePartView>(c => c.Add(x => x.Part, part)));

        Assert.AreEqual("A cat", registered.Find(".bit-aiipv-imb").TextContent);
        Assert.AreEqual("Load image: A cat", provided.Find("button.bit-acb").TextContent.Trim());
    }

    private IRenderedComponent<BitAiImagePartView> RenderImage(BitAiImagePart part, BitAiImagePolicy policy) =>
        RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, part)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Images = policy }));

    private sealed class UninitializedNavigationManager : Microsoft.AspNetCore.Components.NavigationManager
    {
        protected override void NavigateToCore(string uri, Microsoft.AspNetCore.Components.NavigationOptions options) => throw new NotSupportedException();
    }

    private static BitAiImagePolicy Policy(BitAiImageMode mode) => mode switch
    {
        BitAiImageMode.ClickToLoad => BitAiImagePolicy.ClickToLoad,
        BitAiImageMode.Block => BitAiImagePolicy.Block,
        BitAiImageMode.AllowListedHosts => BitAiImagePolicy.AllowListedHosts("cdn.x.dev"),
        _ => BitAiImagePolicy.Allow,
    };

    [TestMethod]
    public void BitAiImagePartViewShouldApplyTheClassesAndStylesOfItsParts()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Caption = "caption" };

        var component = RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, part)
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: red")
            .Add(c => c.Classes, new() { Root = "c-root", Image = "c-img", Caption = "c-cap" })
            .Add(c => c.Styles, new() { Image = "margin: 1px", Caption = "margin: 2px" }));

        var root = component.Find(".bit-aiipv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.IsTrue(component.Find(".bit-aiipv-img").ClassList.Contains("c-img"));
        Assert.AreEqual("margin: 1px", component.Find(".bit-aiipv-img").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aiipv-cap").ClassList.Contains("c-cap"));
        Assert.AreEqual("margin: 2px", component.Find(".bit-aiipv-cap").GetAttribute("style"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiImagePartViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, new BitAiImagePart("https://img.x.dev/cat.png"))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aiipv").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl")]
    public void BitAiImagePartViewShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, new BitAiImagePart("https://img.x.dev/cat.png"))
            .Add(c => c.Dir, dir));

        Assert.AreEqual(expected, component.Find(".bit-aiipv").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitAiImagePartViewShouldTakeTheCascadedParams()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png");

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitAiImagePartViewParams { Class = "cascaded", Classes = new() { Image = "cascaded-img" } }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiImagePartView>(0);
                builder.AddAttribute(1, nameof(BitAiImagePartView.Part), part);
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-aiipv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aiipv-img").ClassList.Contains("cascaded-img"));
        Assert.AreEqual("BitParams.BitAiImagePartView", new BitAiImagePartViewParams().Name);
    }
}
