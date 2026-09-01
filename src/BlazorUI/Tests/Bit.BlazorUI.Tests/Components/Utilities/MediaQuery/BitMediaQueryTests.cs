using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.MediaQuery;

[TestClass]
public class BitMediaQueryTests : BunitTestContext
{
    [TestMethod]
    public void BitMediaQueryShouldRenderNothingWithoutAnyContent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.Add(p => p.OnChange, (bool _) => { });
        });

        Assert.AreEqual(string.Empty, component.Markup.Trim());
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderRootElementWhenContentProvided()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        var root = component.Find(".bit-mdq");
        Assert.IsNotNull(root);
        Assert.IsFalse(string.IsNullOrEmpty(root.Id));
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderNotMatchedContentInitially()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Matched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"matched\">Matched</div>")));
            parameters.Add(p => p.NotMatched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"notmatched\">NotMatched</div>")));
        });

        Assert.AreEqual(0, component.FindAll(".matched").Count);
        Assert.AreEqual(1, component.FindAll(".notmatched").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderMatchedContentInitiallyWithDefaultMatched()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.DefaultMatched, true);
            parameters.Add(p => p.Matched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"matched\">Matched</div>")));
            parameters.Add(p => p.NotMatched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"notmatched\">NotMatched</div>")));
        });

        Assert.AreEqual(1, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".notmatched").Count);
        Assert.IsTrue(component.Instance.IsMatched);
    }

    [TestMethod]
    public void BitMediaQueryShouldSwitchContentOnMatchChange()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Matched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"matched\">Matched</div>")));
            parameters.Add(p => p.NotMatched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"notmatched\">NotMatched</div>")));
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".notmatched").Count);

        component.InvokeAsync(() => component.Instance._OnMatchChange(false).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(0, component.FindAll(".matched").Count);
        Assert.AreEqual(1, component.FindAll(".notmatched").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderChildContentAsMatchedContent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<div class=\"child\">Child</div>");
        });

        Assert.AreEqual(0, component.FindAll(".child").Count);

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".child").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldPreferMatchedOverChildContent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Matched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"matched\">Matched</div>")));
            parameters.AddChildContent("<div class=\"child\">Child</div>");
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".child").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldInvokeOnChangeWhenJsNotifies()
    {
        bool? changed = null;
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.OnChange, (bool v) => changed = v);
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();
        Assert.IsTrue(changed);

        component.InvokeAsync(() => component.Instance._OnMatchChange(false).AsTask()).GetAwaiter().GetResult();
        Assert.IsFalse(changed);
    }

    [TestMethod]
    public void BitMediaQueryShouldExposeIsMatchedState()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        Assert.IsFalse(component.Instance.IsMatched);

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.IsTrue(component.Instance.IsMatched);
    }

    [TestMethod]
    public void BitMediaQueryShouldCallJsSetupWithCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.AreEqual("(max-width: 600px)", invocation.Arguments[1]);
        Assert.IsNull(invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitMediaQueryShouldCallJsSetupWithScreenQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.GtSm);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[1]);
        Assert.AreEqual("GtSm", invocation.Arguments[2]);
    }

    [TestMethod]
    [DataRow(BitScreenQuery.Xs, "Xs")]
    [DataRow(BitScreenQuery.LtXxl, "LtXxl")]
    [DataRow(BitScreenQuery.GtXl, "GtXl")]
    [DataRow(BitScreenQuery.SmToMd, "SmToMd")]
    [DataRow(BitScreenQuery.LgToXl, "LgToXl")]
    public void BitMediaQueryShouldPassScreenQueryNameToJs(BitScreenQuery screenQuery, string expectedName)
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, screenQuery);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[1]);
        Assert.AreEqual(expectedName, invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitMediaQueryShouldPreferCustomQueryOverScreenQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.AreEqual("(max-width: 600px)", invocation.Arguments[1]);
        Assert.IsNull(invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitMediaQueryShouldTreatBlankQueryAsAbsent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, " ");
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Lg);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[1]);
        Assert.AreEqual("Lg", invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotRepeatJsSetupForUnchangedCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render();

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.MediaQuery.setup"));
    }

    [TestMethod]
    public void BitMediaQueryShouldRepeatJsSetupForChangedCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 900px)");
        });

        var invocations = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.MediaQuery.setup").ToList();
        Assert.AreEqual(2, invocations.Count);
        Assert.AreEqual("(max-width: 900px)", invocations[1].Arguments[1]);
    }

    [TestMethod]
    public void BitMediaQueryShouldRepeatJsSetupForScreenQueryOnRerender()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render();

        // The effective query of a ScreenQuery is resolved on the JS side from the live theme
        // breakpoints, so setup is re-invoked on every render (JS reuses the listener when the
        // resolved expression is unchanged).
        Assert.AreEqual(2, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.MediaQuery.setup"));
    }

    [TestMethod]
    public void BitMediaQueryShouldSwitchBetweenScreenQueryAndCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
        });

        var invocations = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.MediaQuery.setup").ToList();
        Assert.AreEqual(2, invocations.Count);
        Assert.AreEqual("Md", invocations[0].Arguments[2]);
        Assert.AreEqual("(max-width: 600px)", invocations[1].Arguments[1]);
        Assert.IsNull(invocations[1].Arguments[2]);
    }

    [TestMethod]
    public void BitMediaQueryShouldReSetupWhenIdChanges()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Id, "first-id");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Id, "second-id");
        });

        // The JS listener is keyed by the element id, so a changed Id disposes the old key and
        // sets the listener up again under the new one.
        var disposeInvocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.dispose");
        Assert.AreEqual("first-id", disposeInvocation.Arguments[0]);

        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.MediaQuery.setup").ToList();
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual("first-id", setups[0].Arguments[0]);
        Assert.AreEqual("second-id", setups[1].Arguments[0]);
    }

    [TestMethod]
    public void BitMediaQueryShouldSetupAgainAfterQueryRemovedAndReadded()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, (string?)null);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.dispose");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 900px)");
        });

        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.MediaQuery.setup").ToList();
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual("(max-width: 900px)", setups[1].Arguments[1]);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotCallJsSetupWithoutAnyQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.AddChildContent("<span>content</span>");
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.MediaQuery.setup"));
    }

    [TestMethod]
    public void BitMediaQueryShouldCallJsDisposeWhenQueryRemoved()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, (string?)null);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.dispose");
    }

    [TestMethod]
    public void BitMediaQueryShouldCallJsDisposeOnDispose()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.dispose");
    }

    [TestMethod]
    public void BitMediaQueryShouldNotCallJsDisposeOnDisposeWithoutAnyQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.AddChildContent("<span>content</span>");
        });

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        // No listener was ever set up, so there is nothing to tear down on the JS side.
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.MediaQuery.dispose"));
    }

    [TestMethod]
    public void BitMediaQueryShouldNotInvokeOnChangeAfterDispose()
    {
        bool? changed = null;
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.OnChange, (bool v) => changed = v);
        });

        var instance = component.Instance;

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        // A notification racing the disposal must be ignored instead of rendering a disposed component.
        instance._OnMatchChange(true).GetAwaiter().GetResult();

        Assert.IsNull(changed);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderAriaLabel()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.AriaLabel, "media query region");
            parameters.AddChildContent("<span>content</span>");
        });

        var root = component.Find(".bit-mdq");
        Assert.AreEqual("media query region", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMediaQueryShouldRespectClassStyleIdAndDir()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "color: red;");
            parameters.Add(p => p.Id, "custom-id");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.AddChildContent("<span>content</span>");
        });

        var root = component.Find(".bit-mdq");
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.AreEqual("custom-id", root.Id);
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderWithoutRootElementWithNoWrapper()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Matched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"matched\">Matched</div>")));
            parameters.Add(p => p.NotMatched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"notmatched\">NotMatched</div>")));
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdq").Count);
        Assert.AreEqual(1, component.FindAll(".notmatched").Count);

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(0, component.FindAll(".bit-mdq").Count);
        Assert.AreEqual(1, component.FindAll(".matched").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderNothingWithNoWrapperWhenCollapsed()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
            parameters.Add(p => p.NotMatched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"notmatched\">NotMatched</div>")));
        });

        Assert.AreEqual(string.Empty, component.Markup.Trim());
    }

    [TestMethod]
    public void BitMediaQueryShouldRespectVisibility()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
            parameters.AddChildContent("<span>content</span>");
        });

        var root = component.Find(".bit-mdq");
        StringAssert.Contains(root.GetAttribute("style"), "display:none");
    }
}
