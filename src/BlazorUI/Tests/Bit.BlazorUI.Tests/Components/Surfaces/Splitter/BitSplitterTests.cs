using System.Globalization;
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
            parameters.Add(p => p.FirstPanelSize, 128);
            parameters.Add(p => p.SecondPanelSize, 200);
        });

        var style = component.Find(".bit-spl").GetAttribute("style");

        Assert.IsNotNull(style);
        Assert.IsTrue(style.Contains("--first-panel-grow:0"));
        Assert.IsTrue(style.Contains("--second-panel-grow:0"));
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
            CultureInfo.DefaultThreadCurrentCulture = null;
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
                SecondPanel = "custom-second",
            });
            parameters.Add(p => p.Styles, new BitSplitterClassStyles
            {
                Root = "color:red",
                FirstPanel = "color:green",
                Gutter = "color:blue",
                GutterIndicator = "color:teal",
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

        await component.InvokeAsync(() => component.Instance.HandleResizeCancel());

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

        Assert.IsTrue(style.Contains(expectedStyle));
    }
}
