using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ErrorView;

[TestClass]
public class BitAiErrorViewTests : BitAiTestBase
{
    [TestMethod]
    public void TheErrorIsAnnouncedAsAnAlert()
    {
        var component = RenderComponent<BitAiErrorView>(p => p.Add(c => c.Error, new BitAiError("The model is overloaded.")));

        Assert.IsNotNull(component.Find("[role=alert]"));
        StringAssert.Contains(component.Find(".bit-aierv").TextContent, "Something went wrong");
        Assert.AreEqual("The model is overloaded.", component.Find(".bit-aierv-txt").TextContent);
    }

    [TestMethod]
    public void ACompactErrorIsTitledByItsOwnMessage()
    {
        var component = RenderComponent<BitAiErrorView>(p => p
            .Add(c => c.Error, new BitAiError("permission denied"))
            .Add(c => c.Compact, true));

        StringAssert.Contains(component.Markup, "permission denied");
        Assert.IsFalse(component.Markup.Contains("Something went wrong"));
        Assert.IsEmpty(component.FindAll(".bit-aierv-txt"));
    }

    [TestMethod]
    public void RetryIsOfferedOnlyWhenItCouldHelpAndSomeoneCanDoIt()
    {
        var retryable = new BitAiError("timeout") { IsRetryable = true };
        var final = new BitAiError("bad request");
        var retried = 0;

        var noHandler = RenderComponent<BitAiErrorView>(p => p.Add(c => c.Error, retryable));
        var notRetryable = RenderComponent<BitAiErrorView>(p => p
            .Add(c => c.Error, final)
            .Add(c => c.OnRetry, () => retried++));
        var both = RenderComponent<BitAiErrorView>(p => p
            .Add(c => c.Error, retryable)
            .Add(c => c.OnRetry, () => retried++));

        Assert.IsEmpty(noHandler.FindAll("button"));
        Assert.IsEmpty(notRetryable.FindAll("button"));

        var retry = both.Find(".bit-aierv-act button");
        Assert.AreEqual("Try again", retry.TextContent.Trim());
        Assert.AreEqual("button", retry.GetAttribute("type"));
        // Refresh is not a glyph of the core font, so it is drawn from the subset font of the package.
        Assert.IsNotNull(retry.QuerySelector(".bit-icon-ai.bit-icon-ai--Refresh"));

        retry.Click();

        Assert.AreEqual(1, retried);
    }

    [TestMethod]
    public void TheWaitBeforeARetryIsShown()
    {
        var component = RenderComponent<BitAiErrorView>(p => p
            .Add(c => c.Error, new BitAiError("slow down") { RetryAfter = TimeSpan.FromSeconds(30) }));

        Assert.IsFalse(string.IsNullOrWhiteSpace(component.Find(".bit-aierv-rta").TextContent));
    }

    [TestMethod]
    public void TheDetailIsBehindADisclosure()
    {
        var component = RenderComponent<BitAiErrorView>(p => p
            .Add(c => c.Error, new BitAiError("boom") { Detail = "at Foo.Bar()" }));

        var toggle = component.Find(".bit-aierv-act button");
        Assert.AreEqual("false", toggle.GetAttribute("aria-expanded"));
        Assert.IsEmpty(component.FindAll(".bit-aierv-dtl"));

        toggle.Click();

        var detail = component.Find(".bit-aierv-dtl");
        Assert.AreEqual("at Foo.Bar()", detail.TextContent);
        Assert.AreEqual("0", detail.GetAttribute("tabindex"));
        toggle = component.Find(".bit-aierv-act button");
        Assert.AreEqual("true", toggle.GetAttribute("aria-expanded"));
        Assert.AreEqual(detail.Id, toggle.GetAttribute("aria-controls"));

        toggle.Click();

        Assert.IsEmpty(component.FindAll(".bit-aierv-dtl"));
    }

    [TestMethod]
    public void ADisabledErrorViewRetriesNothing()
    {
        var retried = 0;

        var component = RenderComponent<BitAiErrorView>(p => p
            .Add(c => c.Error, new BitAiError("timeout") { IsRetryable = true, Detail = "x" })
            .Add(c => c.OnRetry, () => retried++)
            .Add(c => c.Disabled, true));

        Assert.IsTrue(component.Find(".bit-aierv").ClassList.Contains("bit-dis"));

        foreach (var button in component.FindAll("button"))
        {
            button.Click();
        }

        Assert.AreEqual(0, retried);
        Assert.IsEmpty(component.FindAll(".bit-aierv-dtl"));
    }

    // An error view holds nothing of the page's, so while it is disabled the page's activation handlers splatted on it
    // do not run; the ones that only follow the pointer still do.
    [TestMethod]
    public void ADisabledErrorViewDropsTheSplattedActivationHandlers()
    {
        var activations = 0;
        var hovers = 0;

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiErrorView>(0);
                builder.AddAttribute(1, nameof(BitAiErrorView.Error), new BitAiError("boom"));
                builder.AddAttribute(2, nameof(BitAiErrorView.Disabled), true);
                builder.AddAttribute(3, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => activations++));
                builder.AddAttribute(4, "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, () => hovers++));
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-aierv");

        // A handler that was dropped is one bUnit finds nothing to dispatch to.
        try { root.DoubleClick(); } catch (MissingEventHandlerException) { }
        root.MouseOver();

        Assert.AreEqual(0, activations);
        Assert.AreEqual(1, hovers);
    }

    [TestMethod]
    public void TheRootShouldRenderTheExpectedClassesAndAttributes()
    {
        var component = RenderComponent<BitAiErrorView>(p => p
            .Add(c => c.Error, new BitAiError("boom"))
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Id, "error")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "root-class", Text = "text-class" })
            .Add(c => c.Styles, new() { Root = "padding:2px" }));

        var root = component.Find(".bit-aierv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("error", root.Id);
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
        Assert.IsTrue(component.Find(".bit-aierv-txt").ClassList.Contains("text-class"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void TheRootShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiErrorView>(p => p
            .Add(c => c.Error, new BitAiError("boom"))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aierv").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }
}
