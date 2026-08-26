using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Collapse;

[TestClass]
public class BitCollapseTests : BunitTestContext
{
    // The transition callbacks are raised on a timer and hand their value to a plain delegate rather than to a
    // component, so nothing re-renders when they land and WaitForAssertion - which only re-checks on a render -
    // would never look again. This polls the value itself instead.
    private static void WaitUntil(Func<bool> condition, int timeoutInMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutInMs);

        while (condition() is false && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(20);
        }
    }



    [TestMethod]
    public void BitCollapseShouldRenderRootElement()
    {
        var component = RenderComponent<BitCollapse>();

        var root = component.Find(".bit-col");

        Assert.IsNotNull(root);
        Assert.AreEqual("div", root.TagName.ToLower());
    }

    [TestMethod]
    public void BitCollapseShouldRenderChildContent()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello Collapse</div>");
        });

        var collapse = component.Find(".bit-col");

        Assert.IsNotNull(collapse);
        Assert.IsTrue(collapse.OuterHtml.Contains("Hello Collapse"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderTheContentInsideTheWrapper()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello Collapse</div>");
        });

        var wrapper = component.Find(".bit-col-con > .bit-col-wrp");

        Assert.IsTrue(wrapper.InnerHtml.Contains("Hello Collapse"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderTheBodyAlias()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Body, "<div>from body</div>");
        });

        Assert.IsTrue(component.Find(".bit-col-wrp").InnerHtml.Contains("from body"));
    }

    [TestMethod]
    public void BitCollapseChildContentShouldWinOverBody()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Body, "<div>from body</div>");
            parameters.AddChildContent("<div>from child content</div>");
        });

        var wrapper = component.Find(".bit-col-wrp");

        Assert.IsTrue(wrapper.InnerHtml.Contains("from child content"));
        Assert.IsFalse(wrapper.InnerHtml.Contains("from body"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderTheDefaultIdOnTheRoot()
    {
        var component = RenderComponent<BitCollapse>();

        Assert.AreEqual(component.Instance.UniqueId, component.Find(".bit-col").GetAttribute("id"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectId()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Id, "custom-id"));

        Assert.AreEqual("custom-id", component.Find(".bit-col").GetAttribute("id"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.AriaLabel, "More details"));

        Assert.AreEqual("More details", component.Find(".bit-col").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCollapseShouldNameTheContentRegionWithTheAriaLabel()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.AriaLabel, "Shipping details"));

        Assert.AreEqual("Shipping details", component.Find(".bit-col-con").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderNoAriaLabelOnTheContentRegionByDefault()
    {
        var component = RenderComponent<BitCollapse>();

        Assert.IsNull(component.Find(".bit-col-con").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitCollapseHtmlAttributesTest>();

        var root = component.Find(".bit-col");

        Assert.AreEqual("bit", root.GetAttribute("data-val-test"));
        Assert.AreEqual("test-collapse", root.GetAttribute("id"));
        Assert.AreEqual("test-collapse-content", component.Find(".bit-col-con").GetAttribute("id"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitCollapseExpandedStateShouldApplyCorrectClasses(bool expanded)
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Expanded, expanded);
            parameters.AddChildContent("<div>content</div>");
        });

        var content = component.Find(".bit-col-con");
        var root = component.Find(".bit-col");

        if (expanded)
        {
            Assert.IsTrue(content.ClassList.Contains("bit-col-cex"));
            Assert.IsFalse(content.ClassList.Contains("bit-col-cco"));
            Assert.IsTrue(root.ClassList.Contains("bit-col-exp"));
            Assert.IsFalse(root.ClassList.Contains("bit-col-col"));
        }
        else
        {
            Assert.IsTrue(content.ClassList.Contains("bit-col-cco"));
            Assert.IsFalse(content.ClassList.Contains("bit-col-cex"));
            Assert.IsTrue(root.ClassList.Contains("bit-col-col"));
            Assert.IsFalse(root.ClassList.Contains("bit-col-exp"));
        }
    }

    [TestMethod]
    public void BitCollapseShouldSwapTheStateClassesWhenExpandedChanges()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Expanded, false));

        Assert.IsTrue(component.Find(".bit-col-con").ClassList.Contains("bit-col-cco"));

        component.Render(parameters => parameters.Add(p => p.Expanded, true));

        Assert.IsTrue(component.Find(".bit-col-con").ClassList.Contains("bit-col-cex"));
        Assert.IsFalse(component.Find(".bit-col-con").ClassList.Contains("bit-col-cco"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectStylesAndClassesParameters()
    {
        var classes = new BitCollapseClassStyles { Root = "root-class", Content = "content-class", Expanded = "expanded-class", Wrapper = "wrapper-class" };
        var styles = new BitCollapseClassStyles { Root = "width:1px", Content = "width:2px", Expanded = "width:3px", Wrapper = "width:4px" };

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
            parameters.Add(p => p.Expanded, true);
            parameters.AddChildContent("<div>content</div>");
        });

        var root = component.Find(".bit-col");
        var content = component.Find(".bit-col-con");
        var wrapper = component.Find(".bit-col-wrp");

        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("expanded-class"));
        Assert.IsTrue(content.ClassList.Contains("content-class"));
        Assert.IsTrue(wrapper.ClassList.Contains("wrapper-class"));

        Assert.IsTrue(root.GetAttribute("style").Contains("width:1px"));
        Assert.IsTrue(root.GetAttribute("style").Contains("width:3px"));
        Assert.IsTrue(content.GetAttribute("style").Contains("width:2px"));
        Assert.IsTrue(wrapper.GetAttribute("style").Contains("width:4px"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectTheCollapsedClassAndStyle()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitCollapseClassStyles { Collapsed = "collapsed-class", Expanded = "expanded-class" });
            parameters.Add(p => p.Styles, new BitCollapseClassStyles { Collapsed = "width:5px", Expanded = "width:6px" });
            parameters.Add(p => p.Expanded, false);
        });

        var root = component.Find(".bit-col");

        Assert.IsTrue(root.ClassList.Contains("collapsed-class"));
        Assert.IsFalse(root.ClassList.Contains("expanded-class"));
        Assert.IsTrue(root.GetAttribute("style").Contains("width:5px"));
        Assert.IsFalse(root.GetAttribute("style").Contains("width:6px"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitCollapseShouldRespectHorizontal(bool horizontal)
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Horizontal, horizontal));

        Assert.AreEqual(horizontal, component.Find(".bit-col").ClassList.Contains("bit-col-hor"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitCollapseShouldRespectNoAnimation(bool noAnimation)
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.NoAnimation, noAnimation));

        Assert.AreEqual(noAnimation, component.Find(".bit-col").ClassList.Contains("bit-col-nan"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitCollapseShouldRespectNoFade(bool noFade)
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.NoFade, noFade));

        Assert.AreEqual(noFade, component.Find(".bit-col").ClassList.Contains("bit-col-nfd"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitCollapseShouldRespectNoPadding(bool noPadding)
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.NoPadding, noPadding));

        Assert.AreEqual(noPadding, component.Find(".bit-col").ClassList.Contains("bit-col-npd"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectCollapsedSize()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.CollapsedSize, "3rem"));

        var root = component.Find(".bit-col");

        Assert.IsTrue(root.ClassList.Contains("bit-col-pek"));
        Assert.IsTrue(root.GetAttribute("style").Contains("--bit-col-csz:3rem"));
    }

    [TestMethod]
    public void BitCollapseWithCollapsedSizeShouldNeverFade()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.CollapsedSize, "3rem"));

        Assert.IsTrue(component.Find(".bit-col").ClassList.Contains("bit-col-nfd"));
    }

    [TestMethod]
    public void BitCollapseWithNoCollapsedSizeShouldNotBeAPeek()
    {
        var component = RenderComponent<BitCollapse>();

        var root = component.Find(".bit-col");

        Assert.IsFalse(root.ClassList.Contains("bit-col-pek"));
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("--bit-col-csz"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectDurationDelayAndEasing()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Duration, 750);
            parameters.Add(p => p.Delay, 120);
            parameters.Add(p => p.Easing, "linear");
        });

        var style = component.Find(".bit-col").GetAttribute("style");

        Assert.IsTrue(style.Contains("--bit-col-dur:750ms"));
        Assert.IsTrue(style.Contains("--bit-col-del:120ms"));
        Assert.IsTrue(style.Contains("--bit-col-eas:linear"));
    }

    [TestMethod]
    public void BitCollapseShouldClampNegativeDurationAndDelay()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Duration, -100);
            parameters.Add(p => p.Delay, -50);
        });

        var style = component.Find(".bit-col").GetAttribute("style");

        Assert.IsTrue(style.Contains("--bit-col-dur:0ms"));
        Assert.IsTrue(style.Contains("--bit-col-del:0ms"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderNoTransitionVariablesByDefault()
    {
        var component = RenderComponent<BitCollapse>();

        var style = component.Find(".bit-col").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-col-dur"));
        Assert.IsFalse(style.Contains("--bit-col-del"));
        Assert.IsFalse(style.Contains("--bit-col-eas"));
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary, "bit-col-pbg")]
    [DataRow(BitColorKind.Secondary, "bit-col-sbg")]
    [DataRow(BitColorKind.Tertiary, "bit-col-tbg")]
    [DataRow(BitColorKind.Transparent, "bit-col-rbg")]
    public void BitCollapseShouldRespectBackground(BitColorKind background, string expectedClass)
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Background, background));

        Assert.IsTrue(component.Find(".bit-col").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitCollapseShouldRenderNoBackgroundClassByDefault()
    {
        var component = RenderComponent<BitCollapse>();

        var classList = component.Find(".bit-col").ClassList;

        Assert.IsFalse(classList.Contains("bit-col-pbg"));
        Assert.IsFalse(classList.Contains("bit-col-sbg"));
        Assert.IsFalse(classList.Contains("bit-col-tbg"));
        Assert.IsFalse(classList.Contains("bit-col-rbg"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderTheRegionRoleByDefault()
    {
        var component = RenderComponent<BitCollapse>();

        Assert.AreEqual("region", component.Find(".bit-col-con").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectRole()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Role, "group"));

        Assert.AreEqual("group", component.Find(".bit-col-con").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderNoRoleForAnEmptyRole()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Role, string.Empty));

        Assert.IsNull(component.Find(".bit-col-con").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectLabelledBy()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.LabelledBy, "trigger-id"));

        Assert.AreEqual("trigger-id", component.Find(".bit-col-con").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderNoAriaLabelledByByDefault()
    {
        var component = RenderComponent<BitCollapse>();

        Assert.IsNull(component.Find(".bit-col-con").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCollapseShouldMakeTheCollapsedContentInert()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Expanded, false));

        Assert.IsTrue(component.Find(".bit-col-con").HasAttribute("inert"));
    }

    [TestMethod]
    public void BitCollapseShouldNotMakeTheExpandedContentInert()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Expanded, true));

        Assert.IsFalse(component.Find(".bit-col-con").HasAttribute("inert"));
    }

    [TestMethod]
    public void BitCollapseShouldNotMakeAPeekInert()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Expanded, false);
            parameters.Add(p => p.CollapsedSize, "3rem");
        });

        Assert.IsFalse(component.Find(".bit-col-con").HasAttribute("inert"));
    }

    [TestMethod]
    public void BitCollapseShouldTakeTheCollapsedContentOutOfTheTabOrder()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Expanded, false));

        Assert.AreEqual("-1", component.Find(".bit-col-con").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCollapseShouldKeepTheExpandedContentInTheTabOrder()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Expanded, true));

        Assert.AreEqual("0", component.Find(".bit-col-con").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCollapseShouldKeepAPeekInTheTabOrder()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Expanded, false);
            parameters.Add(p => p.CollapsedSize, "3rem");
        });

        Assert.AreEqual("0", component.Find(".bit-col-con").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCollapseShouldTakeADisabledContentOutOfTheTabOrder()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Expanded, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.AreEqual("-1", component.Find(".bit-col-con").GetAttribute("tabindex"));
        Assert.IsTrue(component.Find(".bit-col").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderTheContentIdOnTheContentElement()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Id, "my-collapse"));

        Assert.AreEqual("my-collapse-content", component.Instance.ContentId);
        Assert.AreEqual("my-collapse-content", component.Find(".bit-col-con").GetAttribute("id"));
    }

    [TestMethod]
    public void BitCollapseShouldDeriveTheContentIdFromTheGeneratedId()
    {
        var component = RenderComponent<BitCollapse>();

        Assert.AreEqual($"{component.Instance.UniqueId}-content", component.Instance.ContentId);
        Assert.AreEqual(component.Instance.ContentId, component.Find(".bit-col-con").GetAttribute("id"));
    }

    [TestMethod]
    public void BitCollapseShouldRespectDefaultExpanded()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.DefaultExpanded, true));

        Assert.IsTrue(component.Instance.Expanded);
        Assert.IsTrue(component.Find(".bit-col-con").ClassList.Contains("bit-col-cex"));
    }

    [TestMethod]
    public void BitCollapseShouldIgnoreDefaultExpandedWhenExpandedIsSet()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.DefaultExpanded, true);
            parameters.Add(p => p.Expanded, false);
        });

        Assert.IsFalse(component.Instance.Expanded);
        Assert.IsTrue(component.Find(".bit-col-con").ClassList.Contains("bit-col-cco"));
    }

    [TestMethod]
    public async Task BitCollapseToggleAsyncShouldFlipTheState()
    {
        var component = RenderComponent<BitCollapse>();

        await component.InvokeAsync(() => component.Instance.ToggleAsync());

        Assert.IsTrue(component.Instance.Expanded);

        await component.InvokeAsync(() => component.Instance.ToggleAsync());

        Assert.IsFalse(component.Instance.Expanded);
    }

    [TestMethod]
    public async Task BitCollapseExpandAsyncAndCollapseAsyncShouldSetTheState()
    {
        var component = RenderComponent<BitCollapse>();

        await component.InvokeAsync(() => component.Instance.ExpandAsync());

        Assert.IsTrue(component.Instance.Expanded);

        await component.InvokeAsync(() => component.Instance.ExpandAsync());

        Assert.IsTrue(component.Instance.Expanded);

        await component.InvokeAsync(() => component.Instance.CollapseAsync());

        Assert.IsFalse(component.Instance.Expanded);
    }

    [TestMethod]
    public async Task BitCollapseShouldWriteBackToTheBoundValue()
    {
        var expanded = false;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Bind(p => p.Expanded, expanded, v => expanded = v);
        });

        await component.InvokeAsync(() => component.Instance.ToggleAsync());

        Assert.IsTrue(expanded);
    }

    [TestMethod]
    public async Task BitCollapseShouldCallOnChange()
    {
        var changes = new List<bool>();

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.OnChange, (bool v) => changes.Add(v));
        });

        await component.InvokeAsync(() => component.Instance.ToggleAsync());
        await component.InvokeAsync(() => component.Instance.ToggleAsync());

        Assert.AreEqual(2, changes.Count);
        Assert.IsTrue(changes[0]);
        Assert.IsFalse(changes[1]);
    }

    [TestMethod]
    public async Task BitCollapseShouldNotCallOnChangeWhenTheStateDoesNotChange()
    {
        var changes = 0;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.OnChange, (bool _) => changes++);
        });

        await component.InvokeAsync(() => component.Instance.CollapseAsync());

        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public async Task BitCollapseShouldNotToggleAnUnboundExpanded()
    {
        var changes = 0;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Expanded, false);
            parameters.Add(p => p.OnChange, (bool _) => changes++);
        });

        await component.InvokeAsync(() => component.Instance.ToggleAsync());

        Assert.IsFalse(component.Instance.Expanded);
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public async Task BitCollapseShouldNotToggleWhenDisabled()
    {
        var changes = 0;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnChange, (bool _) => changes++);
        });

        await component.InvokeAsync(() => component.Instance.ToggleAsync());

        Assert.IsFalse(component.Instance.Expanded);
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public void BitCollapseShouldNotRenderTheContentOfALazyCollapseUntilItIsExpanded()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.Expanded, false);
            parameters.AddChildContent("<div>lazy content</div>");
        });

        Assert.IsFalse(component.Markup.Contains("lazy content"));

        component.Render(parameters => parameters.Add(p => p.Expanded, true));

        Assert.IsTrue(component.Markup.Contains("lazy content"));
    }

    [TestMethod]
    public void BitCollapseShouldKeepTheContentOfALazyCollapseAfterItIsCollapsedAgain()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.Expanded, true);
            parameters.AddChildContent("<div>lazy content</div>");
        });

        Assert.IsTrue(component.Markup.Contains("lazy content"));

        component.Render(parameters => parameters.Add(p => p.Expanded, false));

        Assert.IsTrue(component.Markup.Contains("lazy content"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderTheContentOfANonLazyCollapseWhileItIsCollapsed()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Expanded, false);
            parameters.AddChildContent("<div>eager content</div>");
        });

        Assert.IsTrue(component.Markup.Contains("eager content"));
    }

    [TestMethod]
    public void BitCollapseShouldUnmountTheContentAfterItIsCollapsed()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.Expanded, true);
            parameters.AddChildContent("<div>unmounted content</div>");
        });

        Assert.IsTrue(component.Markup.Contains("unmounted content"));

        component.Render(parameters => parameters.Add(p => p.Expanded, false));

        component.WaitForAssertion(() => Assert.IsFalse(component.Markup.Contains("unmounted content")),
                                   TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void BitCollapseShouldMountTheContentAgainAfterItIsExpanded()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.Expanded, true);
            parameters.AddChildContent("<div>unmounted content</div>");
        });

        component.Render(parameters => parameters.Add(p => p.Expanded, false));

        component.WaitForAssertion(() => Assert.IsFalse(component.Markup.Contains("unmounted content")),
                                   TimeSpan.FromSeconds(2));

        component.Render(parameters => parameters.Add(p => p.Expanded, true));

        Assert.IsTrue(component.Markup.Contains("unmounted content"));
    }

    [TestMethod]
    public void BitCollapseShouldNotUnmountTheContentOfACollapseThatWasNeverExpanded()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.Expanded, false);
            parameters.AddChildContent("<div>never expanded</div>");
        });

        Assert.IsTrue(component.Markup.Contains("never expanded"));
    }

    [TestMethod]
    public void BitCollapseShouldNotUnmountTheContentOfAPeek()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.CollapsedSize, "3rem");
            parameters.Add(p => p.Expanded, true);
            parameters.AddChildContent("<div>peeked content</div>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.CollapsedSize, "3rem");
            parameters.Add(p => p.Expanded, false);
        });

        Thread.Sleep(200);

        component.Render();

        Assert.IsTrue(component.Markup.Contains("peeked content"));
    }

    [TestMethod]
    public void BitCollapseShouldRenderTheContentOfALazyPeekBeforeItIsEverExpanded()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.CollapsedSize, "3rem");
            parameters.Add(p => p.Expanded, false);
            parameters.AddChildContent("<div>peeked content</div>");
        });

        Assert.IsTrue(component.Markup.Contains("peeked content"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitCollapseShouldRespectNoClip(bool noClip)
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.NoClip, noClip);
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.Expanded, true);
        });

        Assert.AreEqual(noClip, component.Find(".bit-col").ClassList.Contains("bit-col-ncl"));
    }

    [TestMethod]
    public void BitCollapseShouldNotStopClippingWhileItIsCollapsed()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.NoClip, true);
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.Expanded, false);
        });

        Assert.IsFalse(component.Find(".bit-col").ClassList.Contains("bit-col-ncl"));
    }

    [TestMethod]
    public void BitCollapseShouldStopClippingOnlyOnceTheExpandTransitionHasFinished()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.NoClip, true);
            parameters.Add(p => p.Duration, 200);
            parameters.Add(p => p.Expanded, false);
        });

        component.Render(parameters => parameters.Add(p => p.Expanded, true));

        Assert.IsFalse(component.Find(".bit-col").ClassList.Contains("bit-col-ncl"));

        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-col").ClassList.Contains("bit-col-ncl")),
                                   TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void BitCollapseShouldStartClippingAgainWhenItStartsClosing()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.NoClip, true);
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.Expanded, true);
        });

        Assert.IsTrue(component.Find(".bit-col").ClassList.Contains("bit-col-ncl"));

        component.Render(parameters => parameters.Add(p => p.Expanded, false));

        Assert.IsFalse(component.Find(".bit-col").ClassList.Contains("bit-col-ncl"));
    }

    [TestMethod]
    public void BitCollapseShouldCallOnExpandedWhenTheExpandTransitionHasFinished()
    {
        var expandedCount = 0;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.OnExpanded, () => expandedCount++);
            parameters.Add(p => p.Expanded, false);
        });

        Assert.AreEqual(0, expandedCount);

        component.Render(parameters => parameters.Add(p => p.Expanded, true));

        WaitUntil(() => expandedCount == 1);

        Assert.AreEqual(1, expandedCount);
    }

    [TestMethod]
    public void BitCollapseShouldCallOnCollapsedWhenTheCollapseTransitionHasFinished()
    {
        var collapsedCount = 0;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.OnCollapsed, () => collapsedCount++);
            parameters.Add(p => p.Expanded, true);
        });

        Assert.AreEqual(0, collapsedCount);

        component.Render(parameters => parameters.Add(p => p.Expanded, false));

        WaitUntil(() => collapsedCount == 1);

        Assert.AreEqual(1, collapsedCount);
    }

    [TestMethod]
    public void BitCollapseShouldNotCallTheTransitionCallbacksForTheStateItStartsIn()
    {
        var expandedCount = 0;
        var collapsedCount = 0;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.OnExpanded, () => expandedCount++);
            parameters.Add(p => p.OnCollapsed, () => collapsedCount++);
            parameters.Add(p => p.Expanded, true);
        });

        Thread.Sleep(200);

        component.Render();

        Assert.AreEqual(0, expandedCount);
        Assert.AreEqual(0, collapsedCount);
    }

    [TestMethod]
    public async Task BitCollapseShouldCallTheTransitionCallbacksOfTheComponentDrivenChanges()
    {
        var expandedCount = 0;
        var collapsedCount = 0;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.NoAnimation, true);
            parameters.Add(p => p.OnExpanded, () => expandedCount++);
            parameters.Add(p => p.OnCollapsed, () => collapsedCount++);
        });

        await component.InvokeAsync(() => component.Instance.ExpandAsync());

        WaitUntil(() => expandedCount == 1);

        Assert.AreEqual(1, expandedCount);

        await component.InvokeAsync(() => component.Instance.CollapseAsync());

        WaitUntil(() => collapsedCount == 1);

        Assert.AreEqual(1, collapsedCount);
    }

    [TestMethod]
    public void BitCollapseShouldReportOnlyTheLastOfSeveralTransitionsInARow()
    {
        var expandedCount = 0;
        var collapsedCount = 0;

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Duration, 400);
            parameters.Add(p => p.OnExpanded, () => expandedCount++);
            parameters.Add(p => p.OnCollapsed, () => collapsedCount++);
            parameters.Add(p => p.Expanded, false);
        });

        component.Render(parameters => parameters.Add(p => p.Expanded, true));
        component.Render(parameters => parameters.Add(p => p.Expanded, false));
        component.Render(parameters => parameters.Add(p => p.Expanded, true));

        WaitUntil(() => expandedCount == 1);

        Assert.AreEqual(1, expandedCount);

        Assert.AreEqual(0, collapsedCount);
    }

    [TestMethod]
    public void BitCollapseShouldNotUnmountTheContentOfACollapseThatWasReopenedDuringTheTransition()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.Duration, 300);
            parameters.Add(p => p.Expanded, true);
            parameters.AddChildContent("<div>unmounted content</div>");
        });

        component.Render(parameters => parameters.Add(p => p.Expanded, false));
        component.Render(parameters => parameters.Add(p => p.Expanded, true));

        Thread.Sleep(600);

        component.Render();

        Assert.IsTrue(component.Markup.Contains("unmounted content"));
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, true)]
    [DataRow(BitDir.Ltr, false)]
    public void BitCollapseShouldRespectDir(BitDir dir, bool isRtl)
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Dir, dir));

        var root = component.Find(".bit-col");

        Assert.AreEqual(isRtl, root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual(dir.ToString().ToLower(), root.GetAttribute("dir"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitCollapseShouldRespectForceAnimation(bool forceAnimation)
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.ForceAnimation, forceAnimation));

        Assert.AreEqual(forceAnimation, component.Find(".bit-col").ClassList.Contains("bit-fam"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitCollapseShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.Add(p => p.Visibility, visibility));

        var style = component.Find(".bit-col").GetAttribute("style") ?? string.Empty;

        if (string.IsNullOrEmpty(expectedStyle) is false)
        {
            Assert.IsTrue(style.Contains(expectedStyle));
        }
        else
        {
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
    }

    [TestMethod]
    public void BitCollapseShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Style, "color:red");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = component.Find(".bit-col");

        Assert.IsTrue(root.GetAttribute("style").Contains("color:red"));
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
    }
}
