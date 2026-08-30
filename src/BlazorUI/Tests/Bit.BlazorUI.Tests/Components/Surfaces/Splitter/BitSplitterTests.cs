using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Splitter;

[TestClass]
public class BitSplitterTests : BunitTestContext
{
    [TestMethod]
    public void BitSplitterShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitSplitter>();

        component.MarkupMatches(@"
<div class=""bit-spl"" id:ignore>
    <div class=""bit-spl-pnl bit-spl-fpn"" id:ignore>
    </div>
    <div class=""bit-spl-gtr"" role=""separator"" tabindex=""0"" aria-orientation=""vertical"" aria-controls:ignore aria-valuemin=""0"" aria-valuemax=""100"">
        <div class=""bit-spl-gti"" aria-hidden=""true"">
        </div>
    </div>
    <div class=""bit-spl-pnl bit-spl-spn"" id:ignore>
    </div>
    <div class=""bit-spl-prv"" aria-hidden=""true"">
    </div>
</div>");
    }

    [TestMethod]
    public void BitSplitterShouldGivePanelsIdsDerivedFromTheRootId()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Id, "the-splitter");
        });

        Assert.AreEqual("the-splitter-fpn", component.Find(".bit-spl-fpn").Id);
        Assert.AreEqual("the-splitter-spn", component.Find(".bit-spl-spn").Id);
        Assert.AreEqual("the-splitter-fpn", component.Find(".bit-spl-gtr").GetAttribute("aria-controls"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectGutterSizeStyle()
    {
        var gutter = 20;
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterSize, gutter);
        });

        var root = component.Find(".bit-spl");
        var style = root.GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains($"--gutter-size:{gutter}px"));
    }

    [TestMethod]
    public void BitSplitterShouldClampNegativeSizesToZero()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterSize, -10);
            parameters.Add(p => p.FirstPanelSize, -20);
            parameters.Add(p => p.FirstPanelMinSize, -30);
            parameters.Add(p => p.SecondPanelMaxSize, -40);
        });

        var style = component.Find(".bit-spl").GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains("--gutter-size:0px"));
        Assert.IsTrue(style.Contains("--first-panel:0px"));
        Assert.IsTrue(style.Contains("--first-panel-min:0px"));
        Assert.IsTrue(style.Contains("--second-panel-max:0px"));
    }

    [TestMethod]
    public void BitSplitterShouldRenderGutterIconWhenProvided()
    {
        var iconName = "GripperDotsVertical";
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIconName, iconName);
        });

        var icon = component.Find(".bit-spl .bit-icon");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod]
    public void BitSplitterGutterIconShouldBeHiddenFromAssistiveTechnology()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIconName, "GripperDotsVertical");
        });

        Assert.AreEqual("true", component.Find(".bit-spl-gic").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSplitterIconShouldRenderWithBitIconInfo()
    {
        var iconName = "GripperDotsVertical";
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIcon, BitIconInfo.Bit(iconName));
        });

        var icon = component.Find(".bit-spl-gic");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod]
    public void BitSplitterIconShouldRenderExternalCssClasses()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIcon, BitIconInfo.Css("fa-solid fa-grip-vertical"));
        });

        var icon = component.Find(".bit-spl-gic");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-grip-vertical"));
    }

    [TestMethod]
    public void BitSplitterIconShouldRenderFontAwesomeClasses()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIcon, BitIconInfo.Fa("solid grip-lines-vertical"));
        });

        var icon = component.Find(".bit-spl-gic");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-grip-lines-vertical"));
    }

    [TestMethod]
    public void BitSplitterIconShouldRenderBootstrapIconClasses()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIcon, BitIconInfo.Bi("grip-vertical"));
        });

        var icon = component.Find(".bit-spl-gic");

        Assert.IsTrue(icon.ClassList.Contains("bi"));
        Assert.IsTrue(icon.ClassList.Contains("bi-grip-vertical"));
    }

    [TestMethod]
    public void BitSplitterGutterIconShouldTakePrecedenceOverGutterIconName()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIcon, BitIconInfo.Css("fa-solid fa-grip-vertical"));
            parameters.Add(p => p.GutterIconName, "GripperDotsVertical");
        });

        var icon = component.Find(".bit-spl-gic");

        // GutterIcon (BitIconInfo.Css) should take precedence
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-grip-vertical"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void BitSplitterIconShouldRenderGutterIconClassOnIconElement()
    {
        var iconName = "GripperDotsVertical";
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIconName, iconName);
        });

        var icon = component.Find(".bit-spl-gic");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod]
    public void BitSplitterGutterTemplateShouldTakeThePlaceOfTheIconAndTheGrip()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIconName, "GripperDotsVertical");
            parameters.Add(p => p.GutterTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"custom-gutter\"></span>")));
        });

        Assert.AreEqual(1, component.FindAll(".bit-spl-gtr .custom-gutter").Count);
        Assert.AreEqual(0, component.FindAll(".bit-spl-gic").Count);
        Assert.AreEqual(0, component.FindAll(".bit-spl-gti").Count);
    }

    [TestMethod]
    public void BitSplitterShouldRespectVerticalClass()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        var root = component.Find(".bit-spl");
        Assert.IsTrue(root.ClassList.Contains("bit-spl-vrt"));
    }

    [DataTestMethod,
     DataRow(false, "vertical"),
     DataRow(true, "horizontal")]
    public void BitSplitterShouldReportTheOrientationOfTheSeparatorItself(bool vertical, string expected)
    {
        // A splitter laid out in a column is separated by a horizontal bar and the other way round: what
        // aria-orientation reports is the separator, not the arrangement of the panels.
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Vertical, vertical);
        });

        Assert.AreEqual(expected, component.Find(".bit-spl-gtr").GetAttribute("aria-orientation"));
    }

    [TestMethod]
    public void BitSplitterShouldRenderPanelSizesAsCssVariables()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.FirstPanelSize, 128);
            parameters.Add(p => p.FirstPanelMaxSize, 256);
            parameters.Add(p => p.FirstPanelMinSize, 64);

            parameters.Add(p => p.SecondPanelSize, 200);
            parameters.Add(p => p.SecondPanelMaxSize, 300);
            parameters.Add(p => p.SecondPanelMinSize, 100);
        });

        var root = component.Find(".bit-spl");
        var style = root.GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains("--first-panel:128px"));
        Assert.IsTrue(style.Contains("--first-panel-max:256px"));
        Assert.IsTrue(style.Contains("--first-panel-min:64px"));

        Assert.IsTrue(style.Contains("--second-panel:200px"));
        Assert.IsTrue(style.Contains("--second-panel-max:300px"));
        Assert.IsTrue(style.Contains("--second-panel-min:100px"));
    }

    [TestMethod]
    public void BitSplitterAPinnedPanelShouldStopGrowing()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.SecondPanelSize, 200);
        });

        var style = component.Find(".bit-spl").GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains("--second-panel:200px"));
        Assert.IsTrue(style.Contains("--second-panel-grow:0"));
    }

    [TestMethod]
    public void BitSplitterTwoPinnedPanelsShouldLeaveTheSecondOneToFillTheSplitter()
    {
        // Two panels pinned to lengths cannot add up to a container of any other width, so the second one
        // keeps its size as a starting point and takes whatever is left over on top of it - a splitter with
        // a gap at its end would be the alternative.
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.FirstPanelSize, 128);
            parameters.Add(p => p.SecondPanelSize, 200);
        });

        var style = component.Find(".bit-spl").GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains("--first-panel:128px"));
        Assert.IsTrue(style.Contains("--first-panel-grow:0"));
        Assert.IsTrue(style.Contains("--second-panel:200px"));
        Assert.IsTrue(style.Contains("--second-panel-grow:1"));
    }

    [TestMethod]
    public void BitSplitterWithNoSizeShouldLeaveTheGrowthVariablesAlone()
    {
        var component = RenderComponent<BitSplitter>();

        var style = component.Find(".bit-spl").GetAttribute("style");

        Assert.IsTrue(string.IsNullOrEmpty(style) || style.Contains("--first-panel") is false);
    }

    [TestMethod]
    public void BitSplitterPercentShouldTakePrecedenceOverThePanelSizes()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Percent, 42.5);
            parameters.Add(p => p.FirstPanelSize, 128);
            parameters.Add(p => p.SecondPanelSize, 200);
        });

        var style = component.Find(".bit-spl").GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains("--first-panel:42.5%"));
        Assert.IsTrue(style.Contains("--first-panel-grow:0"));

        // The second panel takes what is left over; a pin on it as well could not add up at every width.
        Assert.IsFalse(style.Contains("--second-panel:"));
        Assert.IsFalse(style.Contains("--second-panel-grow:"));
    }

    [TestMethod]
    public void BitSplitterPercentShouldBeWrittenInTheInvariantCulture()
    {
        var original = CultureInfo.CurrentCulture;
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;

        try
        {
            var german = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = german;
            CultureInfo.DefaultThreadCurrentCulture = german;

            var component = RenderComponent<BitSplitter>(parameters =>
            {
                parameters.Add(p => p.Percent, 42.5);
            });

            var style = component.Find(".bit-spl").GetAttribute("style");

            Assert.IsNotNull(style);
            Assert.IsTrue(style.Contains("--first-panel:42.5%"), style);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
        }
    }

    [DataTestMethod,
     DataRow(-20d, "0"),
     DataRow(140d, "100"),
     DataRow(33d, "33")]
    public void BitSplitterShouldClampTheReportedPositionToTheAriaRange(double percent, string expected)
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Percent, percent);
        });

        var gutter = component.Find(".bit-spl-gtr");

        Assert.AreEqual(expected, gutter.GetAttribute("aria-valuenow"));
        Assert.AreEqual($"{expected}%", gutter.GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public void BitSplitterWithNoPercentShouldNotClaimAPosition()
    {
        var component = RenderComponent<BitSplitter>();

        var gutter = component.Find(".bit-spl-gtr");

        Assert.IsFalse(gutter.HasAttribute("aria-valuenow"));
        Assert.IsFalse(gutter.HasAttribute("aria-valuetext"));
        Assert.AreEqual("0", gutter.GetAttribute("aria-valuemin"));
        Assert.AreEqual("100", gutter.GetAttribute("aria-valuemax"));
    }

    [TestMethod]
    public void BitSplitterShouldRenderChildContentInPanels()
    {
        RenderFragment first = builder => builder.AddContent(0, "First Panel Content");
        RenderFragment second = builder => builder.AddContent(0, "Second Panel Content");

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.FirstPanel, first);
            parameters.Add(p => p.SecondPanel, second);
        });

        var firstPanel = component.Find(".bit-spl-fpn");
        var secondPanel = component.Find(".bit-spl-spn");

        Assert.IsTrue(firstPanel.TextContent.Contains("First Panel Content"));
        Assert.IsTrue(secondPanel.TextContent.Contains("Second Panel Content"));
    }

    [TestMethod]
    public void BitSplitterShouldPutTheAccessibleNameOnTheSeparator()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Resize the panels");
        });

        Assert.AreEqual("Resize the panels", component.Find(".bit-spl-gtr").GetAttribute("aria-label"));
    }

    [DataTestMethod,
     DataRow(true, true),
     DataRow(false, false)]
    public void BitSplitterThatCannotBeResizedShouldStopBeingAWidget(bool readOnly, bool isEnabled)
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.AriaLabel, "Resize the panels");
            parameters.Add(p => p.Percent, 40d);
        });

        var gutter = component.Find(".bit-spl-gtr");

        // A separator only becomes a widget once it can be moved, and none of the states of the widget form
        // are attributes the plain rule is allowed to carry.
        Assert.AreEqual("separator", gutter.GetAttribute("role"));
        Assert.IsFalse(gutter.HasAttribute("tabindex"));
        Assert.IsFalse(gutter.HasAttribute("aria-valuemin"));
        Assert.IsFalse(gutter.HasAttribute("aria-valuemax"));
        Assert.IsFalse(gutter.HasAttribute("aria-valuenow"));
        Assert.IsFalse(gutter.HasAttribute("aria-valuetext"));
        Assert.IsFalse(gutter.HasAttribute("aria-controls"));
        Assert.IsFalse(gutter.HasAttribute("aria-label"));

        // The orientation of the rule itself is the one thing it keeps.
        Assert.AreEqual("vertical", gutter.GetAttribute("aria-orientation"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectReadOnlyClass()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("bit-spl-rdo"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectIsEnabledClass()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectTabIndex()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", component.Find(".bit-spl-gtr").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectDir()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-spl");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitSplitterClassStyles
            {
                Root = "custom-root",
                FirstPanel = "custom-first",
                Gutter = "custom-gutter",
                GutterIndicator = "custom-grip",
                Preview = "custom-preview",
                SecondPanel = "custom-second",
            });
            parameters.Add(p => p.Styles, new BitSplitterClassStyles
            {
                Root = "color:red",
                FirstPanel = "color:green",
                Gutter = "color:blue",
                GutterIndicator = "color:teal",
                Preview = "color:brown",
                SecondPanel = "color:purple",
            });
        });

        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-spl").GetAttribute("style")!.Contains("color:red"));

        Assert.IsTrue(component.Find(".bit-spl-fpn").ClassList.Contains("custom-first"));
        Assert.AreEqual("color:green", component.Find(".bit-spl-fpn").GetAttribute("style"));

        Assert.IsTrue(component.Find(".bit-spl-gtr").ClassList.Contains("custom-gutter"));
        Assert.AreEqual("color:blue", component.Find(".bit-spl-gtr").GetAttribute("style"));

        Assert.IsTrue(component.Find(".bit-spl-gti").ClassList.Contains("custom-grip"));
        Assert.AreEqual("color:teal", component.Find(".bit-spl-gti").GetAttribute("style"));

        Assert.IsTrue(component.Find(".bit-spl-spn").ClassList.Contains("custom-second"));
        Assert.AreEqual("color:purple", component.Find(".bit-spl-spn").GetAttribute("style"));

        Assert.IsTrue(component.Find(".bit-spl-prv").ClassList.Contains("custom-preview"));
        Assert.AreEqual("color:brown", component.Find(".bit-spl-prv").GetAttribute("style"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectGutterIconClassAndStyleSlots()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterIconName, "GripperDotsVertical");
            parameters.Add(p => p.Classes, new BitSplitterClassStyles { GutterIcon = "custom-icon" });
            parameters.Add(p => p.Styles, new BitSplitterClassStyles { GutterIcon = "color:orange" });
        });

        var icon = component.Find(".bit-spl-gic");

        Assert.IsTrue(icon.ClassList.Contains("custom-icon"));
        Assert.AreEqual("color:orange", icon.GetAttribute("style"));
    }

    [TestMethod]
    public void BitSplitterCustomStylesShouldComeAfterTheComputedOnes()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterSize, 12);
            parameters.Add(p => p.Styles, new BitSplitterClassStyles { Root = "--gutter-size:99px" });
        });

        var style = component.Find(".bit-spl").GetAttribute("style")!;

        Assert.IsTrue(style.IndexOf("--gutter-size:12px") < style.IndexOf("--gutter-size:99px"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectCollapsedClassAndCollapsedSize()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.Collapsed, true);
            parameters.Add(p => p.CollapsedSize, 8);
        });

        var root = component.Find(".bit-spl");

        Assert.IsTrue(root.ClassList.Contains("bit-spl-col"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("--collapsed-size:8px"));
    }

    [TestMethod]
    public async Task BitSplitterCollapseShouldNotBeTurnedAwayByCollapsible()
    {
        // Collapsible is about what the reader may do to the gutter; the page can always fold its own
        // panel away.
        var component = RenderComponent<BitSplitter>();

        await component.InvokeAsync(() => component.Instance.Collapse());

        Assert.IsTrue(component.Instance.Collapsed);
        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("bit-spl-col"));
    }

    [TestMethod]
    public async Task BitSplitterACollapsedPanelShouldNotClaimTheOpenPosition()
    {
        var component = RenderComponent<BitSplitter>();

        await component.InvokeAsync(() => component.Instance.SetPercent(40));
        Assert.AreEqual("40", component.Find(".bit-spl-gtr").GetAttribute("aria-valuenow"));

        await component.InvokeAsync(() => component.Instance.Collapse());

        Assert.IsFalse(component.Find(".bit-spl-gtr").HasAttribute("aria-valuenow"));
    }

    [TestMethod]
    public async Task BitSplitterACancelledDragShouldLeaveThePositionAlone()
    {
        var component = RenderComponent<BitSplitter>();

        await component.InvokeAsync(() => component.Instance.SetPercent(40));
        await component.InvokeAsync(() => component.Instance.HandleResizeStart(40));
        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("bit-spl-drg"));

        await component.InvokeAsync(() => component.Instance.HandleResizeCancel(40));

        Assert.IsFalse(component.Find(".bit-spl").ClassList.Contains("bit-spl-drg"));
        Assert.AreEqual(40d, component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterADragOnAControlledSplitterShouldNotMoveThePosition()
    {
        var ended = 0d;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Percent, 40d);
            parameters.Add(p => p.OnResizeEnd, EventCallback.Factory.Create<double>(this, p => ended = p));
        });

        await component.InvokeAsync(() => component.Instance.HandleResizeEnd(70, false));

        // The page owns the position, so only the callback reports where the drag went.
        Assert.AreEqual(40d, component.Instance.Percent);
        Assert.AreEqual(70d, ended);
    }

    [TestMethod]
    public async Task BitSplitterCollapseAndExpandShouldMoveTheState()
    {
        var changes = 0;
        var lastValue = false;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.OnCollapsedChange, EventCallback.Factory.Create<bool>(this, v => { changes++; lastValue = v; }));
        });

        await component.InvokeAsync(() => component.Instance.Collapse());

        Assert.IsTrue(component.Instance.Collapsed);
        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("bit-spl-col"));
        Assert.AreEqual(1, changes);
        Assert.IsTrue(lastValue);

        // A second collapse is not a change and must not be reported as one.
        await component.InvokeAsync(() => component.Instance.Collapse());
        Assert.AreEqual(1, changes);

        await component.InvokeAsync(() => component.Instance.Expand());

        Assert.IsFalse(component.Instance.Collapsed);
        Assert.IsFalse(component.Find(".bit-spl").ClassList.Contains("bit-spl-col"));
        Assert.AreEqual(2, changes);
        Assert.IsFalse(lastValue);
    }

    [TestMethod]
    public async Task BitSplitterToggleCollapseShouldGoBothWays()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
        });

        await component.InvokeAsync(() => component.Instance.ToggleCollapse());
        Assert.IsTrue(component.Instance.Collapsed);

        await component.InvokeAsync(() => component.Instance.ToggleCollapse());
        Assert.IsFalse(component.Instance.Collapsed);
    }

    [TestMethod]
    public async Task BitSplitterExpandingShouldRestoreThePositionTheCollapseFoundIt()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(30));
        await component.InvokeAsync(() => component.Instance.Collapse());
        await component.InvokeAsync(() => component.Instance.SetPercent(80));
        await component.InvokeAsync(() => component.Instance.Expand());

        Assert.AreEqual(30d, component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterCollapsedShouldBeTwoWayBound()
    {
        var collapsed = false;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Bind(p => p.Collapsed, collapsed, v => collapsed = v);
        });

        await component.InvokeAsync(() => component.Instance.ToggleCollapse());

        Assert.IsTrue(collapsed);
    }

    [TestMethod]
    public async Task BitSplitterOneWayBoundCollapsedShouldStayWhereThePagePutIt()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.Collapsed, false);
        });

        await component.InvokeAsync(() => component.Instance.Collapse());

        Assert.IsFalse(component.Instance.Collapsed);
    }

    [DataTestMethod,
     DataRow(-5d, 0d),
     DataRow(150d, 100d),
     DataRow(64.25d, 64.25d)]
    public async Task BitSplitterSetPercentShouldClampToTheAriaRange(double value, double expected)
    {
        var component = RenderComponent<BitSplitter>();

        await component.InvokeAsync(() => component.Instance.SetPercent(value));

        Assert.AreEqual(expected, component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterPercentShouldBeTwoWayBound()
    {
        double? percent = null;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Bind(p => p.Percent, percent, v => percent = v);
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(70));

        Assert.AreEqual(70d, percent);
    }

    [TestMethod]
    public async Task BitSplitterResetSizeShouldHandTheLayoutBackToTheParameters()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.FirstPanelSize, 150);
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(70));
        Assert.IsTrue(component.Find(".bit-spl").GetAttribute("style")!.Contains("--first-panel:70%"));

        await component.InvokeAsync(() => component.Instance.ResetSize());

        Assert.IsNull(component.Instance.Percent);
        Assert.IsTrue(component.Find(".bit-spl").GetAttribute("style")!.Contains("--first-panel:150px"));
    }

    [TestMethod]
    public async Task BitSplitterShouldReportTheStartAndTheEndOfAResize()
    {
        var log = string.Empty;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.OnResizeStart, EventCallback.Factory.Create<double>(this, p => log += $"start:{p};"));
            parameters.Add(p => p.OnResize, EventCallback.Factory.Create<double>(this, p => log += $"move:{p};"));
            parameters.Add(p => p.OnResizeEnd, EventCallback.Factory.Create<double>(this, p => log += $"end:{p};"));
        });

        await component.InvokeAsync(() => component.Instance.HandleResizeStart(10));
        await component.InvokeAsync(() => component.Instance.HandleResize(20));
        await component.InvokeAsync(() => component.Instance.HandleResizeEnd(30, false));

        Assert.AreEqual("start:10;move:20;end:30;", log);
        Assert.AreEqual(30d, component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterShouldWearTheDraggingClassWhileTheGutterIsBeingDragged()
    {
        var component = RenderComponent<BitSplitter>();

        await component.InvokeAsync(() => component.Instance.HandleResizeStart(10));
        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("bit-spl-drg"));

        await component.InvokeAsync(() => component.Instance.HandleResizeEnd(30, false));
        Assert.IsFalse(component.Find(".bit-spl").ClassList.Contains("bit-spl-drg"));
    }

    [TestMethod]
    public async Task BitSplitterADragThatSnapsClosedShouldCollapseRatherThanResize()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(40));
        await component.InvokeAsync(() => component.Instance.HandleResizeEnd(2, true));

        Assert.IsTrue(component.Instance.Collapsed);

        // The position the panel had is kept rather than overwritten by the sliver it was dragged to, so
        // that expanding it again puts it back where it was.
        Assert.AreEqual(40d, component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterHandleToggleCollapseShouldFoldThePanelAway()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
        });

        await component.InvokeAsync(() => component.Instance.HandleToggleCollapse());

        Assert.IsTrue(component.Instance.Collapsed);
    }

    [TestMethod]
    public async Task BitSplitterHandleResetShouldClearThePercent()
    {
        var component = RenderComponent<BitSplitter>();

        await component.InvokeAsync(() => component.Instance.SetPercent(40));
        Assert.AreEqual(40d, component.Instance.Percent);

        await component.InvokeAsync(() => component.Instance.HandleReset());

        Assert.IsNull(component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterOneWayBoundPercentShouldStayWhereThePagePutIt()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Percent, 40d);
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(80));

        Assert.AreEqual(40d, component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterShouldTakeBackTheRememberedPosition()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.PersistKey, "a-key");
            parameters.Add(p => p.Collapsible, true);
        });

        await component.InvokeAsync(() => component.Instance.HandleRestore(35, true));

        Assert.AreEqual(35d, component.Instance.Percent);
        Assert.IsTrue(component.Instance.Collapsed);
    }

    [TestMethod]
    public async Task BitSplitterARememberedPositionShouldNotOverrideThePage()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.PersistKey, "a-key");
            parameters.Add(p => p.Percent, 20d);
        });

        await component.InvokeAsync(() => component.Instance.HandleRestore(35, false));

        Assert.AreEqual(20d, component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterARememberedPositionShouldBeClampedToTheAriaRange()
    {
        var component = RenderComponent<BitSplitter>();

        await component.InvokeAsync(() => component.Instance.HandleRestore(180, false));

        Assert.AreEqual(100d, component.Instance.Percent);
    }

    [TestMethod]
    public void BitSplitterShouldRespectHtmlAttributes()
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes (as real markup would) rather than via the builder,
        // which rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitSplitter>(0);
            builder.AddAttribute(1, "data-test", "splitter");
            builder.CloseComponent();
        });

        Assert.AreEqual("splitter", component.Find(".bit-spl").GetAttribute("data-test"));
    }

    [DataTestMethod,
     DataRow(BitVisibility.Visible, ""),
     DataRow(BitVisibility.Hidden, "visibility:hidden"),
     DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitSplitterShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-spl").GetAttribute("style") ?? string.Empty;

        // A visible splitter is not styled into view, it is simply never styled out of it - an empty
        // expectation would be contained in any style at all, so it is asserted as the absence of both.
        if (expectedStyle.Length == 0)
        {
            Assert.IsFalse(style.Contains("visibility:"), style);
            Assert.IsFalse(style.Contains("display:"), style);
        }
        else
        {
            Assert.IsTrue(style.Contains(expectedStyle), style);
        }
    }

    [TestMethod]
    public void BitSplitterShouldRoundALongPositionBeforeWritingItIntoTheStyleAttribute()
    {
        // A share the page worked out for itself arrives with as many digits as a double has; the style
        // attribute is given the same four decimals a drag reports back with.
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Percent, 100d / 3d);
        });

        var style = component.Find(".bit-spl").GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains("--first-panel:33.3333%"));
    }

    [TestMethod]
    public void BitSplitterShouldHandTheJavaScriptSideEverythingItActsOn()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.DragStep, 25);
            parameters.Add(p => p.SnapSize, 40);
            parameters.Add(p => p.KeyboardStep, 15);
            parameters.Add(p => p.CollapsedSize, 8);
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.LazyResize, true);
            parameters.Add(p => p.NoResetOnDoubleClick, true);
            parameters.Add(p => p.OnGutterDoubleClick, EventCallback.Factory.Create(this, () => { }));
        });

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.setup");

        Assert.AreEqual(8, setup.Arguments[11]);
        Assert.AreEqual(15, setup.Arguments[12]);
        Assert.AreEqual(25, setup.Arguments[13]);
        Assert.AreEqual(40, setup.Arguments[14]);
        Assert.AreEqual(true, setup.Arguments[15]);
        // The reset was turned off, nothing listens for the frames of a drag, and the double-click is
        // wanted whether or not it also resets the splitter.
        Assert.AreEqual(false, setup.Arguments[16]);
        Assert.AreEqual(false, setup.Arguments[17]);
        Assert.AreEqual(true, setup.Arguments[18]);

        Assert.IsFalse(component.Instance.Collapsed);
    }

    [TestMethod]
    public void BitSplitterShouldNotLetANegativeStepReachTheJavaScriptSide()
    {
        RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.DragStep, -25);
            parameters.Add(p => p.SnapSize, -40);
            parameters.Add(p => p.KeyboardStep, -15);
        });

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.setup");

        // A step of zero would leave the keyboard unable to move the gutter at all.
        Assert.AreEqual(1, setup.Arguments[12]);
        Assert.AreEqual(0, setup.Arguments[13]);
        Assert.AreEqual(0, setup.Arguments[14]);
    }

    [TestMethod]
    public void BitSplitterShouldTellTheJavaScriptSideOnlyAboutTheOptionsThatChanged()
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Splitter.setup", _ => true).SetResult("spl-1");

        var component = RenderComponent<BitSplitter>();

        // A render of the page around the splitter is not news for the drag engine.
        component.Render();
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Splitter.update"));

        component.Render(parameters => parameters.Add(p => p.Vertical, true));

        var update = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.update");

        Assert.AreEqual("spl-1", update.Arguments[0]);
        Assert.AreEqual(true, update.Arguments[1]);
    }

    [TestMethod]
    public void BitSplitterShouldSayInItsClassListWhetherThePanelMayBeFolded()
    {
        // What the gutter of a folded panel offers to do - open it again, or nothing at all - is a cursor,
        // and only the class list can tell the stylesheet which of the two it is.
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
        });

        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("bit-spl-cpb"));

        component.Render(parameters => parameters.Add(p => p.Collapsible, false));

        Assert.IsFalse(component.Find(".bit-spl").ClassList.Contains("bit-spl-cpb"));
    }

    [TestMethod]
    public void BitSplitterShouldCarryTheLineALazyDragMovesWhetherOrNotItDragsLazily()
    {
        // The drag belongs to the JavaScript side from the pointer down onwards, and there is no render of
        // the page between the two in which the line could be brought into being.
        var component = RenderComponent<BitSplitter>();

        var preview = component.Find(".bit-spl-prv");

        Assert.AreEqual("true", preview.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public async Task BitSplitterHandleGutterDoubleClickShouldReportTheDoubleClick()
    {
        var clicks = 0;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.OnGutterDoubleClick, EventCallback.Factory.Create(this, () => clicks++));
        });

        await component.InvokeAsync(() => component.Instance.HandleGutterDoubleClick());

        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public async Task BitSplitterOnCollapsingShouldBeAbleToLeaveThePanelAsItIs()
    {
        var changes = 0;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.OnCollapsedChange, EventCallback.Factory.Create<bool>(this, _ => changes++));
            parameters.Add(p => p.OnCollapsing, EventCallback.Factory.Create<BitSplitterCollapseArgs>(this, a => a.Cancel = true));
        });

        await component.InvokeAsync(() => component.Instance.Collapse());

        Assert.IsFalse(component.Instance.Collapsed);
        Assert.IsFalse(component.Find(".bit-spl").ClassList.Contains("bit-spl-col"));

        // A fold that did not happen is not one to report as having happened.
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public async Task BitSplitterOnCollapsingShouldSayWhichWayThePanelIsAboutToGo()
    {
        var collapsing = new List<bool>();

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.OnCollapsing, EventCallback.Factory.Create<BitSplitterCollapseArgs>(this, a => collapsing.Add(a.IsCollapsing)));
        });

        await component.InvokeAsync(() => component.Instance.Collapse());
        await component.InvokeAsync(() => component.Instance.Expand());

        CollectionAssert.AreEqual(new[] { true, false }, collapsing);
    }

    [TestMethod]
    public async Task BitSplitterOnCollapsingShouldSayWhatAskedForTheFold()
    {
        var reasons = new List<BitSplitterCollapseReason>();

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.OnCollapsing, EventCallback.Factory.Create<BitSplitterCollapseArgs>(this, a => reasons.Add(a.Reason)));
        });

        await component.InvokeAsync(() => component.Instance.Collapse());
        await component.InvokeAsync(() => component.Instance.HandleToggleCollapse());
        await component.InvokeAsync(() => component.Instance.HandleResizeEnd(2, true));
        await component.InvokeAsync(() => component.Instance.HandleToggleCollapse());
        await component.InvokeAsync(() => component.Instance.HandleRestore(null, true));

        CollectionAssert.AreEqual(new[]
        {
            BitSplitterCollapseReason.Method,
            BitSplitterCollapseReason.Gutter,
            BitSplitterCollapseReason.Drag,
            BitSplitterCollapseReason.Gutter,
            BitSplitterCollapseReason.Restore
        }, reasons);
    }

    [TestMethod]
    public async Task BitSplitterOnCollapsingShouldNotBeAskedAboutAFoldThatIsNotAChange()
    {
        var asked = 0;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.OnCollapsing, EventCallback.Factory.Create<BitSplitterCollapseArgs>(this, _ => asked++));
        });

        await component.InvokeAsync(() => component.Instance.Expand());
        Assert.AreEqual(0, asked);

        await component.InvokeAsync(() => component.Instance.Collapse());
        await component.InvokeAsync(() => component.Instance.Collapse());

        Assert.AreEqual(1, asked);
    }

    [TestMethod]
    public async Task BitSplitterACancelledSnapShouldLeaveTheSplitterWhereTheDragFoundIt()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.OnCollapsing, EventCallback.Factory.Create<BitSplitterCollapseArgs>(this, a => a.Cancel = true));
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(40));
        await component.InvokeAsync(() => component.Instance.HandleResizeEnd(2, true));

        // The panel did not fold, and the sliver the drag left it at is not a position it was allowed to
        // keep either.
        Assert.IsFalse(component.Instance.Collapsed);
        Assert.AreEqual(40d, component.Instance.Percent);
    }

    [TestMethod]
    public async Task BitSplitterShouldNotStartASecondFoldWhileOnCollapsingIsStillRunning()
    {
        var changes = 0;
        BitSplitter? splitter = null;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.OnCollapsedChange, EventCallback.Factory.Create<bool>(this, _ => changes++));
            parameters.Add(p => p.OnCollapsing, EventCallback.Factory.Create<BitSplitterCollapseArgs>(this, async _ =>
            {
                // The gutter answering a second press while the first is still being decided.
                if (splitter is not null) await splitter.ToggleCollapse();
            }));
        });

        splitter = component.Instance;

        await component.InvokeAsync(() => component.Instance.Collapse());

        Assert.IsTrue(component.Instance.Collapsed);
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public async Task BitSplitterAnAbandonedResizeShouldStillCloseTheOneItOpened()
    {
        var log = string.Empty;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.OnResizeStart, EventCallback.Factory.Create<double>(this, p => log += $"start:{p};"));
            parameters.Add(p => p.OnResizeCancel, EventCallback.Factory.Create<double>(this, p => log += $"cancel:{p};"));
            parameters.Add(p => p.OnResizeEnd, EventCallback.Factory.Create<double>(this, p => log += $"end:{p};"));
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(40));
        await component.InvokeAsync(() => component.Instance.HandleResizeStart(40));
        await component.InvokeAsync(() => component.Instance.HandleResizeCancel(40));

        // Exactly one of the two closes a resize, so a page that put something aside for the duration of one
        // always has somewhere to pick it up again.
        Assert.AreEqual("start:40;cancel:40;", log);

        // The drag put the panels back where it found them, so there is nothing to assign either.
        Assert.AreEqual(40d, component.Instance.Percent);
    }

    [TestMethod]
    public void BitSplitterShouldOnlyOfferTheCollapseControlWhereThePanelMayBeFolded()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.ShowCollapseButton, true);
        });

        // Asking for the control on a splitter whose panel does not fold offers the reader nothing.
        Assert.AreEqual(0, component.FindAll(".bit-spl-cbt").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-spl-cbt").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, false);
        });

        Assert.AreEqual(0, component.FindAll(".bit-spl-cbt").Count);
    }

    [DataTestMethod,
     DataRow(true, true),
     DataRow(false, false)]
    public void BitSplitterASplitterNobodyMayResizeShouldNotOfferTheCollapseControlEither(bool readOnly, bool isEnabled)
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        Assert.AreEqual(0, component.FindAll(".bit-spl-cbt").Count);
    }

    [TestMethod]
    public void BitSplitterTheCollapseControlShouldStayOutOfTheTabOrderAndOutOfTheAccessibilityTree()
    {
        // The separator underneath it already answers to Enter and to Ctrl with an arrow key, so this is a
        // way in for the pointer rather than a control of its own: announcing it would read the same fold
        // twice and give the splitter a second tab stop that does nothing new.
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
        });

        var button = component.Find(".bit-spl-cbt");

        Assert.AreEqual("true", button.GetAttribute("aria-hidden"));
        Assert.IsFalse(button.HasAttribute("tabindex"));
        Assert.IsFalse(button.HasAttribute("role"));
    }

    [TestMethod]
    public void BitSplitterTheCollapseControlShouldBeDrawnOnTheGutter()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
        });

        // Where it is in the DOM is what lets the JavaScript side tell a press meant for it from a drag of
        // the gutter it covers.
        Assert.AreEqual(1, component.FindAll(".bit-spl-gtr > .bit-spl-cbt").Count);
    }

    [TestMethod]
    public void BitSplitterTheCollapseControlShouldSurviveAGutterTemplate()
    {
        // The template takes the place of what is drawn in the gutter, not of the fold offered on it.
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
            parameters.Add(p => p.GutterTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"custom-gutter\"></span>")));
        });

        Assert.AreEqual(1, component.FindAll(".bit-spl-gtr .custom-gutter").Count);
        Assert.AreEqual(1, component.FindAll(".bit-spl-cbt").Count);
    }

    [DataTestMethod,
     DataRow(false, false, false, "bit-icon--ChevronRight bit-ico-r180"),
     DataRow(false, true, false, "bit-icon--ChevronRight"),
     DataRow(false, false, true, "bit-icon--ChevronRight"),
     DataRow(false, true, true, "bit-icon--ChevronRight bit-ico-r180"),
     DataRow(true, false, false, "bit-icon--ChevronRight bit-ico--r90"),
     DataRow(true, true, false, "bit-icon--ChevronRight bit-ico-r90")]
    public void BitSplitterTheCollapseControlShouldPointAtWhatThePressWillDo(bool vertical, bool collapsed, bool rtl, string expected)
    {
        // The chevron makes the same turn the drag does: at the panel while it is there to be folded away,
        // at the room it is about to come back into once it is gone, and the other way round across a row
        // that is written right to left.
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
            parameters.Add(p => p.Vertical, vertical);
            parameters.Add(p => p.Collapsed, collapsed);
            parameters.Add(p => p.Dir, rtl ? BitDir.Rtl : BitDir.Ltr);
        });

        var icon = component.Find(".bit-spl-cbi");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));

        foreach (var expectedClass in expected.Split(' '))
        {
            Assert.IsTrue(icon.ClassList.Contains(expectedClass), $"{icon.ClassName} is missing {expectedClass}");
        }

        // The turn is the whole of the difference: nothing is left over from the other direction.
        Assert.AreEqual(expected.Split(' ').Length + 1, icon.ClassList.Count(c => c.StartsWith("bit-ic")));
    }

    [DataTestMethod,
     DataRow(false, "ClosePane"),
     DataRow(true, "OpenPane")]
    public void BitSplitterTheCollapseControlShouldTakeAnIconOfThePagesOwn(bool collapsed, string expected)
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
            parameters.Add(p => p.Collapsed, collapsed);
            parameters.Add(p => p.CollapseIconName, "ClosePane");
            parameters.Add(p => p.ExpandIconName, "OpenPane");
        });

        Assert.IsTrue(component.Find(".bit-spl-cbi").ClassList.Contains($"bit-icon--{expected}"));
    }

    [DataTestMethod,
     DataRow(false, "fa-solid"),
     DataRow(true, "bi")]
    public void BitSplitterTheCollapseControlShouldTakeAnExternalIcon(bool collapsed, string expected)
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
            parameters.Add(p => p.Collapsed, collapsed);
            parameters.Add(p => p.CollapseIcon, BitIconInfo.Css("fa-solid fa-angle-left"));
            parameters.Add(p => p.ExpandIcon, BitIconInfo.Bi("chevron-right"));
            // The BitIconInfo of each state takes precedence over the name beside it.
            parameters.Add(p => p.CollapseIconName, "ClosePane");
            parameters.Add(p => p.ExpandIconName, "OpenPane");
        });

        var icon = component.Find(".bit-spl-cbi");

        Assert.IsTrue(icon.ClassList.Contains(expected));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--ClosePane"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--OpenPane"));
    }

    [TestMethod]
    public void BitSplitterShouldRespectTheCollapseControlClassAndStyleSlots()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
            parameters.Add(p => p.Classes, new BitSplitterClassStyles { CollapseButton = "custom-button", CollapseButtonIcon = "custom-button-icon" });
            parameters.Add(p => p.Styles, new BitSplitterClassStyles { CollapseButton = "color:olive", CollapseButtonIcon = "color:maroon" });
        });

        var button = component.Find(".bit-spl-cbt");
        var icon = component.Find(".bit-spl-cbi");

        Assert.IsTrue(button.ClassList.Contains("custom-button"));
        Assert.AreEqual("color:olive", button.GetAttribute("style"));

        Assert.IsTrue(icon.ClassList.Contains("custom-button-icon"));
        Assert.AreEqual("color:maroon", icon.GetAttribute("style"));
    }

    [TestMethod]
    public void BitSplitterTheCollapseControlShouldFoldThePanelAwayAndBringItBack()
    {
        var reasons = new List<BitSplitterCollapseReason>();

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.ShowCollapseButton, true);
            parameters.Add(p => p.OnCollapsing, EventCallback.Factory.Create<BitSplitterCollapseArgs>(this, a => reasons.Add(a.Reason)));
        });

        component.Find(".bit-spl-cbt").Click();
        Assert.IsTrue(component.Instance.Collapsed);

        component.Find(".bit-spl-cbt").Click();
        Assert.IsFalse(component.Instance.Collapsed);

        // It is the gutter offering the fold, whichever of the two ways in the reader took.
        CollectionAssert.AreEqual(new[] { BitSplitterCollapseReason.Gutter, BitSplitterCollapseReason.Gutter }, reasons);
    }

    [TestMethod]
    public void BitSplitterShouldRenderTheGutterHitSizeAsACssVariable()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterHitSize, 36);
        });

        Assert.IsTrue(component.Find(".bit-spl").GetAttribute("style")!.Contains("--gutter-hit-size:36px"));
    }

    [TestMethod]
    public void BitSplitterShouldClampANegativeGutterHitSizeToZero()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.GutterHitSize, -36);
        });

        Assert.IsTrue(component.Find(".bit-spl").GetAttribute("style")!.Contains("--gutter-hit-size:0px"));
    }

    [TestMethod]
    public void BitSplitterWithNoGutterHitSizeShouldLeaveTheStylesheetToDecide()
    {
        var style = RenderComponent<BitSplitter>().Find(".bit-spl").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--gutter-hit-size"));
    }

    [TestMethod]
    public async Task BitSplitterAPanelThePageFoldsAwayItselfShouldStillComeBackWhereItWas()
    {
        double? percent = 40;
        var collapsed = false;

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Bind(p => p.Percent, percent, v => percent = v);
            parameters.Bind(p => p.Collapsed, collapsed, v => collapsed = v);
        });

        // The page folds the panel away by writing to Collapsed rather than by asking the component to.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Bind(p => p.Percent, percent, v => percent = v);
            parameters.Bind(p => p.Collapsed, true, v => collapsed = v);
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(80));
        await component.InvokeAsync(() => component.Instance.Expand());

        Assert.AreEqual(40d, component.Instance.Percent);
    }

    [TestMethod]
    public void BitSplitterShouldHandTheJavaScriptSideWhereThePositionIsRemembered()
    {
        RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Percent, 30d);
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.PersistKey, "a-key");
            parameters.Add(p => p.PersistInSessionStorage, true);
        });

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.setup");

        Assert.AreEqual(true, setup.Arguments[6]);
        Assert.AreEqual(30d, setup.Arguments[19]);
        Assert.AreEqual("a-key", setup.Arguments[20]);
        Assert.AreEqual(true, setup.Arguments[21]);
    }

    [TestMethod]
    public void BitSplitterShouldTellTheJavaScriptSideThatTheSplitterCannotBeResized()
    {
        RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.setup");

        // Read-only and disabled are the same thing to the drag engine; only the look of them differs.
        Assert.AreEqual(true, setup.Arguments[7]);
    }

    [TestMethod]
    public async Task BitSplitterShouldPutTheJavaScriptSideBackInStepWhenItRefusesADrag()
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Splitter.setup", _ => true).SetResult("spl-1");

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Percent, 40d);
        });

        await component.InvokeAsync(() => component.Instance.HandleResizeEnd(70, false));

        var sync = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.sync");

        // The inline properties the drag wrote are not something Blazor is tracking, so a position the
        // component would not take has to be undone by hand.
        Assert.AreEqual("spl-1", sync.Arguments[0]);
        Assert.AreEqual(40d, sync.Arguments[1]);
    }

    [TestMethod]
    public async Task BitSplitterResetSizeShouldHandTheJavaScriptSideBackToTheParametersToo()
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Splitter.setup", _ => true).SetResult("spl-1");

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.FirstPanelSize, 150);
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(70));
        await component.InvokeAsync(() => component.Instance.ResetSize());

        var sync = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Splitter.sync");

        Assert.IsNull(sync.Arguments[1]);
    }

    [TestMethod]
    public async Task BitSplitterShouldTakeItsListenersDownWithIt()
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Splitter.setup", _ => true).SetResult("spl-1");

        var component = RenderComponent<BitSplitter>();

        await component.Instance.DisposeAsync();

        var dispose = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.dispose");

        Assert.AreEqual("spl-1", dispose.Arguments[0]);
    }

    [TestMethod]
    public async Task BitSplitterShouldStopCallingIntoAJavaScriptSideThatIsGone()
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Splitter.setup", _ => true).SetResult("spl-1");

        var component = RenderComponent<BitSplitter>();

        await component.Instance.DisposeAsync();

        var before = Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Splitter.sync");

        await component.InvokeAsync(() => component.Instance.ResetSize());

        Assert.AreEqual(before, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Splitter.sync"));
    }

    [TestMethod]
    public void BitSplitterShouldSayInItsClassListWhichPanelFolds()
    {
        // Which of the two a fold takes away is a layout: one panel is held at its collapsed size and the
        // other one fills in, and only the class list can tell the stylesheet which is which.
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
        });

        Assert.IsFalse(component.Find(".bit-spl").ClassList.Contains("bit-spl-cse"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.CollapseSecondPanel, true);
        });

        Assert.IsTrue(component.Find(".bit-spl").ClassList.Contains("bit-spl-cse"));
    }

    [TestMethod]
    public void BitSplitterShouldTellTheJavaScriptSideWhichPanelFolds()
    {
        RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.CollapseSecondPanel, true);
        });

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.setup");

        // The snap that closes a panel and the shortcut that folds it both point at the panel that folds.
        Assert.AreEqual(true, setup.Arguments[8]);
        Assert.AreEqual(true, setup.Arguments[9]);
    }

    [TestMethod]
    public void BitSplitterChangingWhichPanelFoldsShouldReachTheJavaScriptSide()
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Splitter.setup", _ => true).SetResult("spl-1");

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.CollapseSecondPanel, true);
        });

        var update = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.update");

        Assert.AreEqual(true, update.Arguments[4]);
    }

    [DataTestMethod,
     DataRow(false, false, false, "bit-icon--ChevronRight"),
     DataRow(false, true, false, "bit-icon--ChevronRight bit-ico-r180"),
     DataRow(false, false, true, "bit-icon--ChevronRight bit-ico-r180"),
     DataRow(true, false, false, "bit-icon--ChevronRight bit-ico-r90"),
     DataRow(true, true, false, "bit-icon--ChevronRight bit-ico--r90")]
    public void BitSplitterTheCollapseControlShouldPointAtTheSecondPanelWhenThatIsTheOneThatFolds(bool vertical, bool collapsed, bool rtl, string expected)
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.CollapseSecondPanel, true);
            parameters.Add(p => p.ShowCollapseButton, true);
            parameters.Add(p => p.Vertical, vertical);
            parameters.Add(p => p.Collapsed, collapsed);
            parameters.Add(p => p.Dir, rtl ? BitDir.Rtl : BitDir.Ltr);
        });

        var icon = component.Find(".bit-spl-cbi");

        foreach (var expectedClass in expected.Split(' '))
        {
            Assert.IsTrue(icon.ClassList.Contains(expectedClass), $"{icon.ClassName} is missing {expectedClass}");
        }

        Assert.AreEqual(expected.Split(' ').Length + 1, icon.ClassList.Count(c => c.StartsWith("bit-ic")));
    }

    [TestMethod]
    public async Task BitSplitterFoldingTheSecondPanelShouldLeaveThePositionForItToComeBackTo()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.CollapseSecondPanel, true);
        });

        await component.InvokeAsync(() => component.Instance.SetPercent(35));

        // A drag that closed the second panel took the gutter all the way to the far end, and that is not
        // a position the splitter keeps: the split it had is what the panel comes back to.
        await component.InvokeAsync(() => component.Instance.HandleResizeEnd(98, true));

        Assert.IsTrue(component.Instance.Collapsed);
        Assert.AreEqual(35d, component.Instance.Percent);

        await component.InvokeAsync(() => component.Instance.Expand());

        Assert.IsFalse(component.Instance.Collapsed);
        Assert.AreEqual(35d, component.Instance.Percent);
    }

    [TestMethod]
    public void BitSplitterACollapsedSecondPanelShouldStillBeTheOneFoldedAway()
    {
        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.Collapsible, true);
            parameters.Add(p => p.CollapseSecondPanel, true);
            parameters.Add(p => p.Collapsed, true);
            parameters.Add(p => p.CollapsedSize, 8);
        });

        var root = component.Find(".bit-spl");

        // One collapsed state, one collapsed size; only the class list says which side of the gutter they
        // are about.
        Assert.IsTrue(root.ClassList.Contains("bit-spl-col"));
        Assert.IsTrue(root.ClassList.Contains("bit-spl-cse"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("--collapsed-size:8px"));
    }

    [TestMethod]
    public async Task BitSplitterShouldAskTheBrowserWhereTheSplitActuallyIs()
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Splitter.setup", _ => true).SetResult("spl-1");
        Context.JSInterop.Setup<double?>("BitBlazorUI.Splitter.getPercent", _ => true).SetResult(37.5);

        var component = RenderComponent<BitSplitter>(parameters =>
        {
            parameters.Add(p => p.FirstPanelSize, 150);
        });

        // Percent holds nothing until the gutter has been moved, so a split that is still whatever the
        // parameters and the content made of it can only be measured.
        Assert.IsNull(component.Instance.Percent);
        Assert.AreEqual(37.5, await component.Instance.GetPercent());

        var call = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Splitter.getPercent");

        Assert.AreEqual("spl-1", call.Arguments[0]);
    }

    [TestMethod]
    public async Task BitSplitterShouldHaveNothingToMeasureWithoutTheJavaScriptSide()
    {
        var component = RenderComponent<BitSplitter>();

        await component.Instance.DisposeAsync();

        Assert.IsNull(await component.Instance.GetPercent());
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Splitter.getPercent"));
    }
}
