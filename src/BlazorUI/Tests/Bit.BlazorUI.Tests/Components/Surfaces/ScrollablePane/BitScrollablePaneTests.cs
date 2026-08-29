using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.ScrollablePane;

[TestClass]
public class BitScrollablePaneTests : BunitTestContext
{
    private const string Setup = "BitBlazorUI.ScrollablePane.setup";
    private const string Update = "BitBlazorUI.ScrollablePane.update";
    private const string Refresh = "BitBlazorUI.ScrollablePane.refresh";
    private const string Dispose = "BitBlazorUI.ScrollablePane.dispose";
    private const string AutoScroll = "BitBlazorUI.ScrollablePane.autoScroll";
    private const string ScrollToEnd = "BitBlazorUI.ScrollablePane.scrollToEnd";
    private const string ScrollToStart = "BitBlazorUI.ScrollablePane.scrollToStart";
    private const string ScrollTo = "BitBlazorUI.ScrollablePane.scrollTo";
    private const string ScrollBy = "BitBlazorUI.ScrollablePane.scrollBy";
    private const string ScrollToElement = "BitBlazorUI.ScrollablePane.scrollToElement";
    private const string GetOffset = "BitBlazorUI.ScrollablePane.getOffset";



    #region rendering

    [TestMethod]
    public void BitScrollablePaneShouldRenderChildContent()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.AddChildContent("<p class=\"pane-content\">ScrollablePane Content</p>");
        });

        component.MarkupMatches(@"
<div class=""bit-scp"" id:ignore>
    <p class=""pane-content"">
        ScrollablePane Content
    </p>
</div>");
    }

    [TestMethod]
    public void BitScrollablePaneShouldRenderBodyAsAnAliasOfChildContent()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Body, "<p class=\"pane-body\">Body</p>");
        });

        Assert.AreEqual("Body", component.Find(".pane-body").TextContent);
    }

    [TestMethod]
    public void BitScrollablePaneChildContentShouldWinOverBody()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Body, "<p class=\"pane-body\">Body</p>");
            parameters.AddChildContent("<p class=\"pane-content\">Child</p>");
        });

        Assert.AreEqual(1, component.FindAll(".pane-content").Count);
        Assert.AreEqual(0, component.FindAll(".pane-body").Count);
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitScrollablePaneHtmlAttributesTest>();

        var root = component.Find(".bit-scp");

        Assert.AreEqual("bit", root.GetAttribute("data-val-test"));
        Assert.AreEqual("test-scrollable-pane", root.GetAttribute("id"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitScrollablePaneShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-scp");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-pane");
            parameters.Add(p => p.Style, "background:red");
        });

        var root = component.Find(".bit-scp");

        Assert.IsTrue(root.ClassList.Contains("custom-pane"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("background:red"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, ""),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitScrollablePaneShouldRespectVisibility(BitVisibility visibility, string expected)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;

        if (expected.HasValue())
        {
            Assert.IsTrue(style.Contains(expected));
        }
        else
        {
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectDir()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-scp");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    #endregion



    #region accessibility

    [TestMethod]
    public void BitScrollablePaneShouldRenderNoRoleOrTabIndexByDefault()
    {
        var component = RenderComponent<BitScrollablePane>();

        var root = component.Find(".bit-scp");

        Assert.IsFalse(root.HasAttribute("role"));
        Assert.IsFalse(root.HasAttribute("tabindex"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectFocusable()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Focusable, true);
        });

        Assert.AreEqual("0", component.Find(".bit-scp").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectTabIndexOverFocusable()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Focusable, true);
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", component.Find(".bit-scp").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRenderTabIndexWithoutFocusable()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "0");
        });

        Assert.AreEqual("0", component.Find(".bit-scp").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldTakeADisabledPaneOutOfTheTabOrder()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Focusable, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.AreEqual("-1", component.Find(".bit-scp").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectRoleAndAriaLabel()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Role, "region");
            parameters.Add(p => p.AriaLabel, "Release notes");
        });

        var root = component.Find(".bit-scp");

        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("Release notes", root.GetAttribute("aria-label"));
    }

    #endregion



    #region sizing styles

    [TestMethod]
    public void BitScrollablePaneShouldRespectDimensions()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Width, "200px");
            parameters.Add(p => p.Height, "120px");
        });

        var style = component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("width:200px"));
        Assert.IsTrue(style.Contains("height:120px"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectMinAndMaxDimensions()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.MinWidth, "10px");
            parameters.Add(p => p.MinHeight, "20px");
            parameters.Add(p => p.MaxWidth, "30px");
            parameters.Add(p => p.MaxHeight, "40px");
        });

        var style = component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("min-width:10px"));
        Assert.IsTrue(style.Contains("min-height:20px"));
        Assert.IsTrue(style.Contains("max-width:30px"));
        Assert.IsTrue(style.Contains("max-height:40px"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectAutoSizes()
    {
        var width = RenderComponent<BitScrollablePane>(p => p.Add(x => x.AutoWidth, true));
        var height = RenderComponent<BitScrollablePane>(p => p.Add(x => x.AutoHeight, true));
        var size = RenderComponent<BitScrollablePane>(p => p.Add(x => x.AutoSize, true));

        Assert.IsTrue(StyleOf(width).Contains("width:auto"));
        Assert.IsTrue(StyleOf(height).Contains("height:auto"));
        Assert.IsTrue(StyleOf(size).Contains("height:auto;width:auto"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectFitSizes()
    {
        var width = RenderComponent<BitScrollablePane>(p => p.Add(x => x.FitWidth, true));
        var height = RenderComponent<BitScrollablePane>(p => p.Add(x => x.FitHeight, true));
        var size = RenderComponent<BitScrollablePane>(p => p.Add(x => x.FitSize, true));

        Assert.IsTrue(StyleOf(width).Contains("width:fit-content"));
        Assert.IsTrue(StyleOf(height).Contains("height:fit-content"));
        Assert.IsTrue(StyleOf(size).Contains("height:fit-content;width:fit-content"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectFullSizes()
    {
        var width = RenderComponent<BitScrollablePane>(p => p.Add(x => x.FullWidth, true));
        var height = RenderComponent<BitScrollablePane>(p => p.Add(x => x.FullHeight, true));
        var size = RenderComponent<BitScrollablePane>(p => p.Add(x => x.FullSize, true));

        Assert.IsTrue(StyleOf(width).Contains("width:100%"));
        Assert.IsTrue(StyleOf(height).Contains("height:100%"));
        Assert.IsTrue(StyleOf(size).Contains("height:100%;width:100%"));
    }

    #endregion



    #region overflow styles

    [TestMethod,
        DataRow(BitOverflow.Auto, "overflow:auto"),
        DataRow(BitOverflow.Hidden, "overflow:hidden"),
        DataRow(BitOverflow.Scroll, "overflow:scroll"),
        DataRow(BitOverflow.Visible, "overflow:visible")]
    public void BitScrollablePaneShouldRespectOverflowStyles(BitOverflow overflow, string expected)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Overflow, overflow);
        });

        Assert.IsTrue(StyleOf(component).Contains(expected));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectOverflowXStyles()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OverflowX, BitOverflow.Scroll);
        });

        Assert.IsTrue(StyleOf(component).Contains("overflow-x:scroll"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectOverflowYStyles()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OverflowY, BitOverflow.Visible);
        });

        Assert.IsTrue(StyleOf(component).Contains("overflow-y:visible"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectOverflowXYStyles()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OverflowX, BitOverflow.Scroll);
            parameters.Add(p => p.OverflowY, BitOverflow.Visible);
        });

        var style = StyleOf(component);

        Assert.IsTrue(style.Contains("overflow-x:scroll"));
        Assert.IsTrue(style.Contains("overflow-y:visible"));
    }

    [TestMethod]
    public void BitScrollablePaneAxisOverflowShouldWinOverTheShorthand()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Overflow, BitOverflow.Scroll);
            parameters.Add(p => p.OverflowX, BitOverflow.Auto);
        });

        var style = StyleOf(component);

        // An explicitly Auto axis has to be written out, or it would keep whatever the shorthand set.
        Assert.IsTrue(style.IndexOf("overflow:scroll") < style.IndexOf("overflow-x:auto"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectHorizontal()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
        });

        var root = component.Find(".bit-scp");
        var style = root.GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(root.ClassList.Contains("bit-scp-hor"));
        Assert.IsTrue(style.Contains("overflow-x:auto"));
        Assert.IsTrue(style.Contains("overflow-y:hidden"));
    }

    [TestMethod]
    public void BitScrollablePaneOverflowShouldWinOverHorizontal()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.OverflowY, BitOverflow.Auto);
        });

        var style = StyleOf(component);

        Assert.IsTrue(style.IndexOf("overflow-y:hidden") < style.IndexOf("overflow-y:auto"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectNoScroll()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Overflow, BitOverflow.Scroll);
            parameters.Add(p => p.NoScroll, true);
        });

        var style = StyleOf(component);

        Assert.IsTrue(style.IndexOf("overflow:scroll") < style.IndexOf("overflow:hidden"));
    }

    [TestMethod,
        DataRow(BitOverscroll.Auto, "overscroll-behavior:auto"),
        DataRow(BitOverscroll.Contain, "overscroll-behavior:contain"),
        DataRow(BitOverscroll.None, "overscroll-behavior:none")]
    public void BitScrollablePaneShouldRespectOverscroll(BitOverscroll overscroll, string expected)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Overscroll, overscroll);
        });

        Assert.IsTrue(StyleOf(component).Contains(expected));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectOverscrollAxes()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OverscrollX, BitOverscroll.None);
            parameters.Add(p => p.OverscrollY, BitOverscroll.Contain);
        });

        var style = StyleOf(component);

        Assert.IsTrue(style.Contains("overscroll-behavior-x:none"));
        Assert.IsTrue(style.Contains("overscroll-behavior-y:contain"));
    }

    #endregion



    #region scrollbar styles

    [TestMethod,
        DataRow(BitScrollbarGutter.Auto, ""),
        DataRow(BitScrollbarGutter.Stable, "scrollbar-gutter:stable"),
        DataRow(BitScrollbarGutter.BothEdges, "scrollbar-gutter:stable both-edges")]
    public void BitScrollablePaneShouldRespectGutter(BitScrollbarGutter gutter, string expected)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Gutter, gutter);
        });

        var style = StyleOf(component);

        if (expected.HasValue())
        {
            Assert.IsTrue(style.Contains(expected));
        }
        else
        {
            Assert.IsFalse(style.Contains("scrollbar-gutter"));
        }
    }

    [TestMethod,
        DataRow(BitScrollbarWidth.Auto, ""),
        DataRow(BitScrollbarWidth.Thin, "scrollbar-width:thin"),
        DataRow(BitScrollbarWidth.None, "scrollbar-width:none")]
    public void BitScrollablePaneShouldRespectScrollbarWidth(BitScrollbarWidth scrollbarWidth, string expected)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.ScrollbarWidth, scrollbarWidth);
        });

        var style = StyleOf(component);

        if (expected.HasValue())
        {
            Assert.IsTrue(style.Contains(expected));
        }
        else
        {
            Assert.IsFalse(style.Contains("scrollbar-width"));
        }
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectScrollbarColor()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.ScrollbarColor, "red blue");
        });

        Assert.IsTrue(StyleOf(component).Contains("scrollbar-color:red blue"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectScrollPadding()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.ScrollPadding, "3rem");
        });

        Assert.IsTrue(StyleOf(component).Contains("scroll-padding:3rem"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectModern()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Modern, true);
        });

        Assert.IsTrue(component.Find(".bit-scp").ClassList.Contains("bit-scp-mod"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectSmooth()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Smooth, true);
        });

        Assert.IsTrue(component.Find(".bit-scp").ClassList.Contains("bit-scp-smt"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectAutoHideScrollbar()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Modern, true);
            parameters.Add(p => p.AutoHideScrollbar, true);
        });

        var root = component.Find(".bit-scp");

        Assert.IsTrue(root.ClassList.Contains("bit-scp-mod"));
        Assert.IsTrue(root.ClassList.Contains("bit-scp-ahs"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectFadeAndFadeSize()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
            parameters.Add(p => p.FadeSize, "3rem");
        });

        var root = component.Find(".bit-scp");

        Assert.IsTrue(root.ClassList.Contains("bit-scp-fad"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("--bit-scp-fsz:3rem"));
    }

    #endregion



    #region snapping

    [TestMethod,
        DataRow(BitScrollSnap.None, "scroll-snap-type:none"),
        DataRow(BitScrollSnap.Proximity, "scroll-snap-type:both proximity"),
        DataRow(BitScrollSnap.Mandatory, "scroll-snap-type:both mandatory")]
    public void BitScrollablePaneShouldRespectSnap(BitScrollSnap snap, string expected)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Snap, snap);
        });

        Assert.IsTrue(StyleOf(component).Contains(expected));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotSnapByDefault()
    {
        var component = RenderComponent<BitScrollablePane>();

        Assert.IsFalse(StyleOf(component).Contains("scroll-snap-type"));
    }

    [TestMethod,
        DataRow(BitScrollSnapAlign.None, "bit-scp-sna-non"),
        DataRow(BitScrollSnapAlign.Start, "bit-scp-sna-str"),
        DataRow(BitScrollSnapAlign.Center, "bit-scp-sna-cnt"),
        DataRow(BitScrollSnapAlign.End, "bit-scp-sna-end")]
    public void BitScrollablePaneShouldRespectSnapAlign(BitScrollSnapAlign align, string expected)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.SnapAlign, align);
        });

        Assert.IsTrue(component.Find(".bit-scp").ClassList.Contains(expected));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotAlignSnapPositionsByDefault()
    {
        var classes = RenderComponent<BitScrollablePane>().Find(".bit-scp").ClassList;

        Assert.IsFalse(classes.Any(c => c.StartsWith("bit-scp-sna-")));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectSnapStop()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Snap, BitScrollSnap.Mandatory);
            parameters.Add(p => p.SnapAlign, BitScrollSnapAlign.Start);
            parameters.Add(p => p.SnapStop, true);
        });

        var classes = component.Find(".bit-scp").ClassList;

        Assert.IsTrue(classes.Contains("bit-scp-sns"));
        Assert.IsTrue(classes.Contains("bit-scp-sna-str"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotStopOnEverySnapPositionByDefault()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Snap, BitScrollSnap.Mandatory);
        });

        Assert.IsFalse(component.Find(".bit-scp").ClassList.Contains("bit-scp-sns"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotSetupJsForSnapStop()
    {
        // scroll-snap-stop is the browser's own, so it costs no listener, no observer and no round trip.
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Snap, BitScrollSnap.Mandatory);
            parameters.Add(p => p.SnapStop, true);
        });

        Assert.AreEqual(0, InvocationCount(Setup));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotSetupJsForSnapping()
    {
        // Snapping is the browser's own, so it costs no listener, no observer and no round trip.
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Snap, BitScrollSnap.Mandatory);
            parameters.Add(p => p.SnapAlign, BitScrollSnapAlign.Center);
        });

        Assert.AreEqual(0, InvocationCount(Setup));
    }

    #endregion



    #region browser side setup

    [TestMethod]
    public void BitScrollablePaneShouldNotSetupJsWhenNothingNeedsIt()
    {
        RenderComponent<BitScrollablePane>();

        Assert.AreEqual(0, InvocationCount(Setup));
    }

    [TestMethod,
        DataRow("Fade"),
        DataRow("AutoScroll"),
        DataRow("DragScroll"),
        DataRow("HorizontalWheel"),
        DataRow("PreserveScroll"),
        DataRow("OnScroll"),
        DataRow("OnScrollStart"),
        DataRow("OnScrollEnd"),
        DataRow("OnReachedTop"),
        DataRow("OnReachedBottom"),
        DataRow("OnReachedLeft"),
        DataRow("OnReachedRight")]
    public void BitScrollablePaneShouldSetupJsForEveryBrowserSideFeature(string feature)
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            switch (feature)
            {
                case "Fade": parameters.Add(p => p.Fade, true); break;
                case "AutoScroll": parameters.Add(p => p.AutoScroll, true); break;
                case "DragScroll": parameters.Add(p => p.DragScroll, true); break;
                case "HorizontalWheel": parameters.Add(p => p.HorizontalWheel, true); break;
                case "PreserveScroll": parameters.Add(p => p.PreserveScroll, true); break;
                case "OnScroll": parameters.Add(p => p.OnScroll, _ => { }); break;
                case "OnScrollStart": parameters.Add(p => p.OnScrollStart, _ => { }); break;
                case "OnScrollEnd": parameters.Add(p => p.OnScrollEnd, _ => { }); break;
                case "OnReachedTop": parameters.Add(p => p.OnReachedTop, () => { }); break;
                case "OnReachedBottom": parameters.Add(p => p.OnReachedBottom, () => { }); break;
                case "OnReachedLeft": parameters.Add(p => p.OnReachedLeft, () => { }); break;
                case "OnReachedRight": parameters.Add(p => p.OnReachedRight, () => { }); break;
            }
        });

        Assert.AreEqual(1, InvocationCount(Setup));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectDragScroll()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.DragScroll, true);
        });

        Assert.IsTrue(component.Find(".bit-scp").ClassList.Contains("bit-scp-drg"));
        Assert.AreEqual(true, Option(SetupOptions(), "Drag"));
        Assert.AreEqual(false, Option(SetupOptions(), "Momentum"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectDragMomentum()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.DragScroll, true);
            parameters.Add(p => p.DragMomentum, true);
        });

        Assert.AreEqual(true, Option(SetupOptions(), "Momentum"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotSetupJsForDragMomentumAlone()
    {
        // A glide is what a released DRAG carries on with, so on its own it has nothing to carry on from.
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.DragMomentum, true);
        });

        Assert.AreEqual(0, InvocationCount(Setup));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectHorizontalWheel()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.HorizontalWheel, true);
        });

        Assert.AreEqual(true, Option(SetupOptions(), "Wheel"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldTellTheBrowserSideThatItIsNotToBeScrolled()
    {
        // overflow:hidden stops the reader's own gestures and nothing else: the drag and the sideways
        // wheel both move the pane through the scrolling API, which goes on working on a clipped element.
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.NoScroll, true);
            parameters.Add(p => p.DragScroll, true);
            parameters.Add(p => p.HorizontalWheel, true);
        });

        Assert.AreEqual(true, Option(SetupOptions(), "NoScroll"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotOfferTheGrabCursorOnAPaneThatIsNotToBeScrolled()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.NoScroll, true);
            parameters.Add(p => p.DragScroll, true);
        });

        Assert.IsFalse(component.Find(".bit-scp").ClassList.Contains("bit-scp-drg"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectPreserveScroll()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.PreserveScroll, true);
        });

        Assert.AreEqual(true, Option(SetupOptions(), "Preserve"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotPreserveTheScrollByDefault()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
        });

        Assert.AreEqual(false, Option(SetupOptions(), "Preserve"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldHandOverTheScrollStartAndEndFlags()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnScrollStart, _ => { });
            parameters.Add(p => p.OnScrollEnd, _ => { });
        });

        var options = SetupOptions();

        Assert.AreEqual(true, Option(options, "ScrollStart"));
        Assert.AreEqual(true, Option(options, "ScrollEnd"));
        Assert.AreEqual(false, Option(options, "Scroll"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotWatchTheContentForADragOrAWheelAlone()
    {
        // Neither of the two interactions is drawn from the size of the content, so a pane that asked
        // only for them has nothing to re-measure after a render of it.
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.DragScroll, true);
            parameters.Add(p => p.HorizontalWheel, true);
        });

        component.Render();

        Assert.AreEqual(0, InvocationCount(Refresh));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotSetupJsWhenDisabled()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.AreEqual(0, InvocationCount(Setup));
    }

    [TestMethod]
    public void BitScrollablePaneShouldHandOverTheOptionsItWasGiven()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
            parameters.Add(p => p.ReachOffset, 40);
            parameters.Add(p => p.ScrollThrottle, 120);
            parameters.Add(p => p.AutoScrollThreshold, 12);
            parameters.Add(p => p.OnReachedBottom, () => { });
        });

        var options = SetupOptions();

        Assert.AreEqual(true, Option(options, "Fade"));
        Assert.AreEqual(40, Option(options, "Offset"));
        Assert.AreEqual(120, Option(options, "Throttle"));
        Assert.AreEqual(12, Option(options, "AutoScrollThreshold"));
        Assert.AreEqual(true, Option(options, "Bottom"));
        Assert.AreEqual(false, Option(options, "Top"));
        Assert.AreEqual(false, Option(options, "Scroll"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldHandOverSmoothAndTheScrollFlag()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Smooth, true);
            parameters.Add(p => p.OnScroll, _ => { });
        });

        var options = SetupOptions();

        Assert.AreEqual(true, Option(options, "Smooth"));
        Assert.AreEqual(true, Option(options, "Scroll"));
        Assert.AreEqual(false, Option(options, "Fade"));
        Assert.AreEqual(false, Option(options, "AutoScroll"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldClampNegativeOffsets()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
            parameters.Add(p => p.ReachOffset, -5);
            parameters.Add(p => p.ScrollThrottle, -5);
            parameters.Add(p => p.AutoScrollThreshold, -5);
        });

        var options = SetupOptions();

        Assert.AreEqual(0, Option(options, "Offset"));
        Assert.AreEqual(0, Option(options, "Throttle"));
        Assert.AreEqual(0, Option(options, "AutoScrollThreshold"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldUpdateTheBrowserSideOnlyWhenAnOptionChanged()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
        });

        component.Render();

        Assert.AreEqual(0, InvocationCount(Update));

        component.Render(parameters => parameters.Add(p => p.ReachOffset, 100));

        Assert.AreEqual(1, InvocationCount(Update));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotCallTheBrowserSideAgainForARenderThatChangedNothing()
    {
        // What a change of the content does to the fade and to the edge callbacks is the browser side's
        // own to notice - it observes the content, attributes and all - so a render that changed none of
        // the options is worth no round trip, however often the page around the pane re-renders.
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
        });

        component.Render();
        component.Render();

        Assert.AreEqual(1, InvocationCount(Setup));
        Assert.AreEqual(0, InvocationCount(Update));
        Assert.AreEqual(0, InvocationCount(Refresh));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotRefreshTheBrowserSideForAPaneThatOnlyReportsItsPosition()
    {
        // A refresh raises a measurement, a measurement is what a report follows, and a page that renders
        // on OnScroll would be re-rendered by its own report for as long as it lives.
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnScroll, _ => { });
        });

        component.Render();
        component.Render();

        Assert.AreEqual(0, InvocationCount(Refresh));
    }

    [TestMethod]
    public void BitScrollablePaneShouldTearTheBrowserSideDownWhenTheLastFeatureGoesAway()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
        });

        Assert.AreEqual(1, InvocationCount(Setup));

        component.Render(parameters => parameters.Add(p => p.Fade, false));

        Assert.AreEqual(1, InvocationCount(Dispose));
    }

    [TestMethod]
    public async Task BitScrollablePaneShouldDisposeTheBrowserSideOnTeardown()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
        });

        await component.Instance.DisposeAsync();

        Assert.AreEqual(1, InvocationCount(Dispose));
    }

    #endregion



    #region auto scroll

    [TestMethod]
    public void BitScrollablePaneShouldAutoScrollAfterRender()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.AutoScroll, true);
        });

        Assert.AreEqual(1, InvocationCount(AutoScroll));
    }

    [TestMethod]
    public void BitScrollablePaneShouldOnlyAskTheBrowserSideToPinOnce()
    {
        // The first pinning is the one this side has to ask for, and it is forced: a pane that starts out
        // with content already in it belongs at the end of it, and the browser side has nothing to compare
        // against on its very first measurement. Every pinning after that is its own answer to content it
        // watches for, so a render is worth no round trip of its own.
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.AutoScroll, true);
        });

        component.Render();
        component.Render();

        var invocations = InvocationsOf(AutoScroll);

        Assert.AreEqual(1, invocations.Length);
        Assert.AreEqual(true, invocations[0].Arguments[1]);
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotAutoScrollWhenItIsOff()
    {
        RenderComponent<BitScrollablePane>();

        Assert.AreEqual(0, InvocationCount(AutoScroll));
    }

    [TestMethod]
    public void BitScrollablePaneShouldForceTheFirstAutoScrollAgainAfterItWasTurnedOff()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.AutoScroll, true);
        });

        component.Render(parameters => parameters.Add(p => p.AutoScroll, false));
        component.Render(parameters => parameters.Add(p => p.AutoScroll, true));

        var invocations = InvocationsOf(AutoScroll);

        Assert.AreEqual(2, invocations.Length);
        Assert.AreEqual(true, invocations[0].Arguments[1]);
        Assert.AreEqual(true, invocations[1].Arguments[1]);
    }

    #endregion



    #region the initial position

    [TestMethod]
    public void BitScrollablePaneShouldOpenAtTheInitialPosition()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.InitialScrollLeft, 40);
            parameters.Add(p => p.InitialScrollTop, 250);
        });

        var invocation = InvocationsOf(ScrollTo).Single();

        Assert.AreEqual(40d, invocation.Arguments[1]);
        Assert.AreEqual(250d, invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitScrollablePaneShouldOpenAtTheInitialPositionOfOneAxisAlone()
    {
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.InitialScrollTop, 120);
        });

        var invocation = InvocationsOf(ScrollTo).Single();

        Assert.IsNull(invocation.Arguments[1]);
        Assert.AreEqual(120d, invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotAnimateTheInitialPosition()
    {
        // A pane that slid into place from the top as it appeared would be an animation nobody asked for.
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Smooth, true);
            parameters.Add(p => p.InitialScrollTop, 120);
        });

        Assert.AreEqual(false, InvocationsOf(ScrollTo).Single().Arguments[3]);
    }

    [TestMethod]
    public void BitScrollablePaneShouldApplyTheInitialPositionOnlyOnce()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.InitialScrollTop, 120);
        });

        component.Render(parameters => parameters.Add(p => p.InitialScrollTop, 300));

        Assert.AreEqual(1, InvocationCount(ScrollTo));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotScrollAnywhereWithoutAnInitialPosition()
    {
        RenderComponent<BitScrollablePane>();

        Assert.AreEqual(0, InvocationCount(ScrollTo));
    }

    [TestMethod]
    public void BitScrollablePaneShouldLeaveTheInitialPositionToAutoScroll()
    {
        // A pane pinned to the end of its content has already been told where to open.
        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.AutoScroll, true);
            parameters.Add(p => p.InitialScrollTop, 120);
        });

        Assert.AreEqual(0, InvocationCount(ScrollTo));
        Assert.AreEqual(1, InvocationCount(AutoScroll));
    }

    #endregion



    #region the scrolling api

    [TestMethod]
    public async Task BitScrollablePaneScrollToEndShouldCallJs()
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.ScrollToEnd();

        Assert.AreEqual(false, InvocationsOf(ScrollToEnd).Single().Arguments[1]);
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollToEndShouldFollowTheSmoothParameter()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Smooth, true);
        });

        await component.Instance.ScrollToEnd();

        Assert.AreEqual(true, InvocationsOf(ScrollToEnd).Single().Arguments[1]);
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollToEndShouldHonorAnExplicitSmooth()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Smooth, true);
        });

        await component.Instance.ScrollToEnd(false);

        Assert.AreEqual(false, InvocationsOf(ScrollToEnd).Single().Arguments[1]);
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollToStartShouldCallJs()
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.ScrollToStart();

        Assert.AreEqual(1, InvocationCount(ScrollToStart));
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollToShouldCallJsWithBothOffsets()
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.ScrollTo(null, 200);

        var arguments = InvocationsOf(ScrollTo).Single().Arguments;

        Assert.IsNull(arguments[1]);
        Assert.AreEqual(200d, arguments[2]);
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollByShouldCallJsWithBothDistances()
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.ScrollBy(10, -20);

        var arguments = InvocationsOf(ScrollBy).Single().Arguments;

        Assert.AreEqual(10d, arguments[1]);
        Assert.AreEqual(-20d, arguments[2]);
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollToElementShouldCallJs()
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.ScrollToElement("row-3", 16);

        var arguments = InvocationsOf(ScrollToElement).Single().Arguments;

        Assert.AreEqual("row-3", arguments[1]);
        Assert.AreEqual(16d, arguments[2]);
        Assert.AreEqual("start", arguments[4]);
    }

    [TestMethod,
        DataRow(BitScrollAlignment.Start, "start"),
        DataRow(BitScrollAlignment.Center, "center"),
        DataRow(BitScrollAlignment.End, "end"),
        DataRow(BitScrollAlignment.Nearest, "nearest")]
    public async Task BitScrollablePaneScrollToElementShouldHandOverTheAlignment(BitScrollAlignment alignment, string expected)
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.ScrollToElement("row-3", alignment: alignment);

        Assert.AreEqual(expected, InvocationsOf(ScrollToElement).Single().Arguments[4]);
    }

    [TestMethod]
    public async Task BitScrollablePaneFocusAsyncShouldFocusTheRootElement()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Focusable, true);
        });

        await component.InvokeAsync(async () => await component.Instance.FocusAsync());

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollToElementShouldIgnoreAnEmptyId()
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.ScrollToElement(string.Empty);

        Assert.AreEqual(0, InvocationCount(ScrollToElement));
    }

    [TestMethod]
    public async Task BitScrollablePaneGetScrollOffsetShouldCallJs()
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.GetScrollOffset();

        Assert.AreEqual(1, InvocationCount(GetOffset));
    }

    [TestMethod]
    public async Task BitScrollablePaneRefreshShouldDoNothingWithoutABrowserSide()
    {
        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.Refresh();

        Assert.AreEqual(0, InvocationCount(Refresh));
    }

    [TestMethod]
    public async Task BitScrollablePaneRefreshShouldCallJsWhenThereIsABrowserSide()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
        });

        var before = InvocationCount(Refresh);

        await component.Instance.Refresh();

        Assert.AreEqual(before + 1, InvocationCount(Refresh));
    }

    #endregion



    #region callbacks

    [TestMethod]
    public async Task BitScrollablePaneShouldInvokeOnScroll()
    {
        BitScrollOffset? reported = null;

        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnScroll, offset => reported = offset);
        });

        await component.InvokeAsync(() => component.Instance._OnScroll(new BitScrollOffset { Top = 40, ScrollHeight = 200, ClientHeight = 100 }));

        Assert.IsNotNull(reported);
        Assert.AreEqual(40, reported!.Top);
        Assert.AreEqual(100, reported.MaxTop);
    }

    [TestMethod]
    public async Task BitScrollablePaneShouldSupportAParameterlessOnScrollHandler()
    {
        var scrolled = false;

        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnScroll, () => scrolled = true);
        });

        await component.InvokeAsync(() => component.Instance._OnScroll(new BitScrollOffset()));

        Assert.IsTrue(scrolled);
    }

    [TestMethod,
        DataRow("top"),
        DataRow("bottom"),
        DataRow("left"),
        DataRow("right")]
    public async Task BitScrollablePaneShouldRouteEachEdgeToItsOwnCallback(string edge)
    {
        string? reached = null;

        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnReachedTop, () => reached = "top");
            parameters.Add(p => p.OnReachedBottom, () => reached = "bottom");
            parameters.Add(p => p.OnReachedLeft, () => reached = "left");
            parameters.Add(p => p.OnReachedRight, () => reached = "right");
        });

        await component.InvokeAsync(() => component.Instance._OnReached(edge));

        Assert.AreEqual(edge, reached);
    }

    [TestMethod]
    public async Task BitScrollablePaneShouldIgnoreAnUnknownEdge()
    {
        var reached = false;

        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnReachedTop, () => reached = true);
        });

        await component.InvokeAsync(() => component.Instance._OnReached("nowhere"));

        Assert.IsFalse(reached);
    }

    [TestMethod]
    public async Task BitScrollablePaneShouldInvokeOnScrollStartAndOnScrollEnd()
    {
        double? started = null;
        double? ended = null;

        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnScrollStart, offset => started = offset.Top);
            parameters.Add(p => p.OnScrollEnd, offset => ended = offset.Top);
        });

        await component.InvokeAsync(() => component.Instance._OnScrollStart(new BitScrollOffset { Top = 10 }));
        await component.InvokeAsync(() => component.Instance._OnScrollEnd(new BitScrollOffset { Top = 90 }));

        Assert.AreEqual(10d, started);
        Assert.AreEqual(90d, ended);
    }

    [TestMethod]
    public async Task BitScrollablePaneShouldIgnoreANullScrollStartOrEndOffset()
    {
        var calls = 0;

        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnScrollStart, _ => calls++);
            parameters.Add(p => p.OnScrollEnd, _ => calls++);
        });

        await component.InvokeAsync(() => component.Instance._OnScrollStart(null!));
        await component.InvokeAsync(() => component.Instance._OnScrollEnd(null!));

        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public async Task BitScrollablePaneShouldIgnoreANullScrollOffset()
    {
        var scrolled = false;

        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnScroll, () => scrolled = true);
        });

        await component.InvokeAsync(() => component.Instance._OnScroll(null!));

        Assert.IsFalse(scrolled);
    }

    #endregion



    #region the scroll offset

    [TestMethod]
    public void BitScrollOffsetShouldDeriveTheVerticalAnswers()
    {
        var offset = new BitScrollOffset { Top = 50, ScrollHeight = 300, ClientHeight = 100 };

        Assert.AreEqual(200, offset.MaxTop);
        Assert.IsTrue(offset.ScrollableY);
        Assert.IsFalse(offset.AtTop);
        Assert.IsFalse(offset.AtBottom);
        Assert.AreEqual(0.25, offset.PercentY);
    }

    [TestMethod]
    public void BitScrollOffsetShouldReportBothEdgesOfAnAxisWithNothingToScroll()
    {
        var offset = new BitScrollOffset { Top = 0, ScrollHeight = 100, ClientHeight = 100 };

        Assert.AreEqual(0, offset.MaxTop);
        Assert.IsFalse(offset.ScrollableY);
        Assert.IsTrue(offset.AtTop);
        Assert.IsTrue(offset.AtBottom);
        Assert.AreEqual(0, offset.PercentY);
    }

    [TestMethod]
    public void BitScrollOffsetShouldFoldTheSignOfARightToLeftPaneAway()
    {
        var offset = new BitScrollOffset { Left = -120, ScrollWidth = 400, ClientWidth = 200, Rtl = true };

        Assert.AreEqual(200, offset.MaxLeft);
        Assert.AreEqual(80, offset.OffsetLeft);
        Assert.AreEqual(0.4, offset.PercentX);
        Assert.IsFalse(offset.AtLeft);
        Assert.IsFalse(offset.AtRight);
    }

    [TestMethod]
    public void BitScrollOffsetShouldReadANegativeOffsetOfALeftToRightPaneAsOverscroll()
    {
        // Which way the pane reads is the only thing that folds the sign away. A negative scrollLeft on a
        // pane that reads left to right is the elastic overscroll of one being bounced past its left edge,
        // and folding it over would report a pane at the very start as standing at the very end.
        var offset = new BitScrollOffset { Left = -5, ScrollWidth = 400, ClientWidth = 200 };

        Assert.AreEqual(-5, offset.OffsetLeft);
        Assert.IsTrue(offset.AtLeft);
        Assert.IsFalse(offset.AtRight);
        Assert.AreEqual(0, offset.PercentX);
    }

    [TestMethod]
    public void BitScrollOffsetShouldReadTheStartOfARightToLeftPaneAsItsRightEdge()
    {
        // The one reading the sign cannot settle on its own: a scrollLeft of 0 is the visual LEFT edge of
        // a left to right pane and the visual RIGHT edge of a right to left one, which is what Rtl says.
        var offset = new BitScrollOffset { Left = 0, ScrollWidth = 400, ClientWidth = 200, Rtl = true };

        Assert.AreEqual(200, offset.OffsetLeft);
        Assert.IsFalse(offset.AtLeft);
        Assert.IsTrue(offset.AtRight);
        Assert.AreEqual(1, offset.PercentX);
    }

    [TestMethod]
    public void BitScrollOffsetShouldReadTheEndOfARightToLeftPaneAsItsLeftEdge()
    {
        var offset = new BitScrollOffset { Left = -200, ScrollWidth = 400, ClientWidth = 200, Rtl = true };

        Assert.AreEqual(0, offset.OffsetLeft);
        Assert.IsTrue(offset.AtLeft);
        Assert.IsFalse(offset.AtRight);
        Assert.AreEqual(0, offset.PercentX);
    }

    [TestMethod]
    public void BitScrollOffsetShouldReportTheEdgesOfALeftToRightPane()
    {
        var start = new BitScrollOffset { Left = 0, ScrollWidth = 400, ClientWidth = 200 };
        var end = new BitScrollOffset { Left = 200, ScrollWidth = 400, ClientWidth = 200 };

        Assert.IsTrue(start.AtLeft);
        Assert.IsFalse(start.AtRight);
        Assert.IsFalse(end.AtLeft);
        Assert.IsTrue(end.AtRight);
    }

    [TestMethod]
    public void BitScrollOffsetShouldGiveTheEdgesAPixelOfSlack()
    {
        // A scroll offset is fractional at a fractional zoom level, so a pane that is visibly at its edge
        // is a fraction of a pixel away from it and an exact comparison would say it is not there.
        var top = new BitScrollOffset { Top = 0.4, ScrollHeight = 300, ClientHeight = 100 };
        var bottom = new BitScrollOffset { Top = 199.6, ScrollHeight = 300, ClientHeight = 100 };
        var right = new BitScrollOffset { Left = 199.5, ScrollWidth = 400, ClientWidth = 200 };

        Assert.IsTrue(top.AtTop);
        Assert.IsFalse(top.AtBottom);
        Assert.IsTrue(bottom.AtBottom);
        Assert.IsFalse(bottom.AtTop);
        Assert.IsTrue(right.AtRight);
        Assert.IsFalse(right.AtLeft);
    }

    [TestMethod]
    public void BitScrollOffsetShouldNotCallAPaneWellShortOfAnEdgeAtIt()
    {
        var offset = new BitScrollOffset { Top = 4, ScrollHeight = 300, ClientHeight = 100 };

        Assert.IsFalse(offset.AtTop);
        Assert.IsFalse(offset.AtBottom);
    }

    [TestMethod]
    public void BitScrollOffsetShouldDeriveTheDirectionOfTheMoveItCarries()
    {
        var down = new BitScrollOffset { DeltaTop = 24, DeltaLeft = -8 };
        var up = new BitScrollOffset { DeltaTop = -24, DeltaLeft = 8 };

        Assert.IsTrue(down.ScrollingDown);
        Assert.IsFalse(down.ScrollingUp);
        Assert.IsTrue(down.ScrollingLeft);
        Assert.IsFalse(down.ScrollingRight);

        Assert.IsTrue(up.ScrollingUp);
        Assert.IsFalse(up.ScrollingDown);
        Assert.IsTrue(up.ScrollingRight);
        Assert.IsFalse(up.ScrollingLeft);
    }

    [TestMethod]
    public void BitScrollOffsetShouldReportNoDirectionForAReportThatCarriesNoMove()
    {
        // A position read on demand, and the one the start or the end of a scroll carries, has nothing to
        // have moved from - it must not read as a move in either direction.
        var offset = new BitScrollOffset { Top = 50, ScrollHeight = 300, ClientHeight = 100 };

        Assert.AreEqual(0, offset.DeltaTop);
        Assert.AreEqual(0, offset.DeltaLeft);
        Assert.IsFalse(offset.ScrollingUp);
        Assert.IsFalse(offset.ScrollingDown);
        Assert.IsFalse(offset.ScrollingLeft);
        Assert.IsFalse(offset.ScrollingRight);
    }

    #endregion



    private static string StyleOf<T>(IRenderedComponent<T> component) where T : Microsoft.AspNetCore.Components.IComponent
    {
        return component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;
    }

    private Bunit.JSRuntimeInvocation[] InvocationsOf(string identifier)
    {
        return Context.JSInterop.Invocations.Where(i => i.Identifier == identifier).ToArray();
    }

    private int InvocationCount(string identifier)
    {
        return Context.JSInterop.Invocations.Count(i => i.Identifier == identifier);
    }

    // The options class is internal to the library, so the handed over instance is read by name rather
    // than cast: what these tests are checking is the value each parameter turns into, not the type.
    private object SetupOptions()
    {
        return InvocationsOf(Setup).Single().Arguments[3]!;
    }

    private static object? Option(object options, string name)
    {
        return options.GetType().GetProperty(name)!.GetValue(options);
    }
}
