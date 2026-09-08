using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

/// <summary>Component-level tests: markup, accessibility, legend interaction and keyboard navigation.</summary>
[TestClass]
public class BitChartTests : BunitTestContext
{
    private static BitChartData TwoSeries() => new()
    {
        Labels = { "Jan", "Feb", "Mar" },
        Datasets =
        {
            new BitChartDataset { Label = "Alpha", Data = { 1, 2, 3 } },
            new BitChartDataset { Label = "Beta", Data = { 3, 2, 1 } }
        }
    };

    private IRenderedComponent<BitChart> RenderChart(BitChartType type = BitChartType.Bar, BitChartData? data = null,
        BitChartOptions? options = null)
        => RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, type);
            p.Add(c => c.Data, data ?? TwoSeries());
            if (options is not null) p.Add(c => c.Options, options);
        });

    // ---- markup ----

    [TestMethod]
    public void BitChartShouldRenderTheRootAndAnSvg()
    {
        var component = RenderChart();

        var root = component.Find(".bit-cht");
        Assert.IsNotNull(root);
        Assert.IsNotNull(component.Find("svg.bit-cht-svg"));
    }

    [TestMethod]
    public void BitChartShouldAppendTheCustomClassAndStyle()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Class, "my-chart");
            p.Add(c => c.Style, "opacity:0.5;");
            p.Add(c => c.Id, "chart-1");
        });

        var root = component.Find(".bit-cht");
        Assert.IsTrue(root.ClassList.Contains("my-chart"));
        Assert.AreEqual("chart-1", root.Id);
        StringAssert.Contains(root.GetAttribute("style"), "opacity:0.5");
    }

    [TestMethod]
    public void BitChartShouldSplatHtmlAttributes()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.HtmlAttributes, new Dictionary<string, object> { ["data-test"] = "chart" });
        });

        Assert.AreEqual("chart", component.Find(".bit-cht").GetAttribute("data-test"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitChartShouldOptIntoReducedMotionOnRequest(bool respect)
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.RespectReducedMotion, respect);
        });

        Assert.AreEqual(respect, component.Find(".bit-cht").ClassList.Contains("bit-cht-rm"));
    }

    [TestMethod]
    public void BitChartShouldRenderTheRequestedDirection()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", component.Find(".bit-cht").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitChartShouldNotEmitAPerInstanceStyleBlock()
    {
        var component = RenderChart();

        Assert.AreEqual(0, component.FindAll("style").Count,
            "the chart styles ship in the Extras stylesheet, not duplicated into every instance");
    }

    // ---- accessibility ----

    [TestMethod]
    public void BitChartShouldDescribeItselfWithTheGivenAriaLabel()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.AriaLabel, "Quarterly revenue");
        });

        Assert.AreEqual("Quarterly revenue", component.Find("svg").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitChartShouldFallBackToTheTitleForItsAccessibleName()
    {
        var options = new BitChartOptions { Plugins = { Title = { Display = true, Text = "Sales" } } };
        var component = RenderChart(options: options);

        Assert.AreEqual("Sales", component.Find("svg").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitChartShouldRenderAScreenReaderTableAndPointAtIt()
    {
        var component = RenderChart();

        var table = component.Find("table");
        var svg = component.Find("svg");
        // The description points at the how-to-navigate sentence and then the data itself.
        var described = svg.GetAttribute("aria-describedby")!.Split(' ');
        CollectionAssert.Contains(described, table.Id);
        Assert.AreEqual(2, described.Length);
        Assert.IsNotNull(component.Find($"#{described[0]}"));
        Assert.AreEqual(3, table.QuerySelectorAll("thead th").Length - 1);
        Assert.AreEqual(2, table.QuerySelectorAll("tbody tr").Length);
    }

    [TestMethod]
    public void BitChartShouldTellAScreenReaderHowToWalkTheData()
    {
        var component = RenderChart();

        var hintId = component.Find("svg").GetAttribute("aria-describedby")!.Split(' ')[0];
        StringAssert.Contains(component.Find($"#{hintId}").TextContent, "arrow keys");
        Assert.AreEqual("chart", component.Find("svg").GetAttribute("aria-roledescription"));
    }

    [TestMethod]
    public void TheNavigationHintCanBeReplacedOrTurnedOff()
    {
        var custom = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.NavigationHint, "Arrow keys walk the bars.");
        });
        StringAssert.Contains(custom.Find(".bit-cht").TextContent, "Arrow keys walk the bars.");

        var silent = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.NavigationHint, null);
        });
        var table = silent.Find("table");
        Assert.AreEqual(table.Id, silent.Find("svg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void AnEmptyChartShouldNotPromiseKeyboardNavigation()
    {
        var component = RenderComponent<BitChart>(p => p.Add(c => c.Data, new BitChartData()));

        // Nothing to walk: the hint would be a lie, so only the (empty) table is described.
        Assert.AreEqual(component.Find("table").Id, component.Find("svg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChartShouldSkipTheDataTableWhenAsked()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.GenerateTable, false);
            p.Add(c => c.NavigationHint, null);
        });

        Assert.AreEqual(0, component.FindAll("table").Count);
        Assert.IsNull(component.Find("svg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void DroppingTheTableShouldStillLeaveTheNavigationHintDescribing()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.GenerateTable, false);
        });

        var described = component.Find("svg").GetAttribute("aria-describedby");
        Assert.IsNotNull(described);
        Assert.IsFalse(described!.Contains(' '), "with no table there is only the hint to point at");
        StringAssert.Contains(component.Find($"#{described}").TextContent, "arrow keys");
    }

    [TestMethod]
    public void BitChartTableShouldListPointsForScatterData()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Label = "P", Points = [new(1, 2), new(3, 4)] } }
        };
        var component = RenderChart(BitChartType.Scatter, data);

        var headers = component.FindAll("table thead th").Select(h => h.TextContent).ToList();
        CollectionAssert.AreEqual(new[] { "Series", "X", "Y" }, headers);
        Assert.AreEqual(2, component.FindAll("table tbody tr").Count);
    }

    // ---- legend ----

    [TestMethod]
    public void LegendItemsShouldBeFocusableButtonsWithAPressedState()
    {
        var component = RenderChart();

        var items = component.FindAll(".bit-cht-lgd-itm");
        Assert.AreEqual(2, items.Count);
        foreach (var item in items)
        {
            Assert.AreEqual("button", item.TagName.ToLowerInvariant(),
                "legend entries are controls, so they have to be reachable by keyboard");
            Assert.AreEqual("true", item.GetAttribute("aria-pressed"));
            Assert.IsNull(item.GetAttribute("role"),
                "a role on the button would replace its native toggle-button semantics");
        }
        Assert.AreEqual("group", component.Find(".bit-cht-lgd").GetAttribute("role"));
    }

    [TestMethod]
    public void ClickingALegendItemShouldHideItsDataset()
    {
        var component = RenderChart();

        component.FindAll(".bit-cht-lgd-itm")[0].Click();

        var item = component.FindAll(".bit-cht-lgd-itm")[0];
        Assert.AreEqual("false", item.GetAttribute("aria-pressed"));
        Assert.IsTrue(item.ClassList.Contains("bit-cht-hdn"));
    }

    [TestMethod]
    public void ClickingALegendItemTwiceShouldBringTheDatasetBack()
    {
        var component = RenderChart();

        component.FindAll(".bit-cht-lgd-itm")[0].Click();
        component.FindAll(".bit-cht-lgd-itm")[0].Click();

        Assert.AreEqual("true", component.FindAll(".bit-cht-lgd-itm")[0].GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void LegendClickShouldRaiseTheCallback()
    {
        BitChartLegendItemModel? clicked = null;
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.OnLegendItemClick, (BitChartLegendItemModel i) => clicked = i);
        });

        component.FindAll(".bit-cht-lgd-itm")[1].Click();

        Assert.IsNotNull(clicked);
        Assert.AreEqual("Beta", clicked!.Text);
    }

    [TestMethod]
    public void LegendToggleCanBeTurnedOff()
    {
        var options = new BitChartOptions { Plugins = { Legend = { OnClickToggle = false } } };
        var component = RenderChart(options: options);

        component.FindAll(".bit-cht-lgd-itm")[0].Click();

        Assert.AreEqual("true", component.FindAll(".bit-cht-lgd-itm")[0].GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void CircularLegendShouldToggleSlices()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 1, 2, 3 } } }
        };
        var component = RenderChart(BitChartType.Pie, data);

        int before = component.FindAll(".bit-cht-data > g").Count;
        component.FindAll(".bit-cht-lgd-itm")[0].Click();

        Assert.AreEqual(before - 1, component.FindAll(".bit-cht-data > g").Count);
    }

    // ---- interaction ----

    [TestMethod]
    public void HoveringAnElementShouldShowATooltip()
    {
        var component = RenderChart();

        Assert.AreEqual(0, component.FindAll(".bit-cht-tt").Count);
        component.FindAll(".bit-cht-data > g")[0].MouseEnter();

        Assert.AreEqual(1, component.FindAll(".bit-cht-tt").Count);
        component.FindAll(".bit-cht-data > g")[0].MouseLeave();
        Assert.AreEqual(0, component.FindAll(".bit-cht-tt").Count);
    }

    [TestMethod]
    public void HoveringAHitBandShouldShowEverySeriesAtThatIndex()
    {
        var component = RenderChart(BitChartType.Line);

        var bands = component.FindAll(".bit-cht-band");
        Assert.AreEqual(3, bands.Count);
        bands[1].MouseEnter();

        var rows = component.FindAll(".bit-cht-tt .bit-cht-tt-itm");
        Assert.AreEqual(2, rows.Count, "a band covers the whole index, so both series are listed");
    }

    [TestMethod]
    public void TooltipCanBeDisabled()
    {
        var options = new BitChartOptions { Plugins = { Tooltip = { Enabled = false } } };
        var component = RenderChart(options: options);

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();

        Assert.AreEqual(0, component.FindAll(".bit-cht-tt").Count);
    }

    [TestMethod]
    public void ClickingAnElementShouldRaiseTheCallback()
    {
        (int ds, int di)? clicked = null;
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Type, BitChartType.Bar);
            p.Add(c => c.OnElementClick, (ValueTuple<int, int> e) => clicked = e);
        });

        component.FindAll(".bit-cht-data > g")[0].Click();

        Assert.IsNotNull(clicked);
        Assert.AreEqual(0, clicked!.Value.ds);
        Assert.AreEqual(0, clicked.Value.di);
    }

    [TestMethod]
    public void HoverShouldRaiseTheHoverCallbackWithAndWithoutAContext()
    {
        var seen = new List<BitChartTooltipContext?>();
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.OnElementHover, (BitChartTooltipContext? ctx) => seen.Add(ctx));
        });

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();
        component.FindAll(".bit-cht-data > g")[0].MouseLeave();

        Assert.AreEqual(2, seen.Count);
        Assert.IsNotNull(seen[0]);
        Assert.IsNull(seen[1]);
    }

    [TestMethod]
    public void CustomTooltipTemplateShouldReplaceTheDefaultBody()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.TooltipTemplate, (BitChartTooltipContext ctx) =>
                (builder) =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "my-tt");
                    builder.AddContent(2, ctx.Points.Count.ToString(CultureInfo.InvariantCulture));
                    builder.CloseElement();
                });
        });

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();

        Assert.AreEqual(1, component.FindAll(".bit-cht-tt-custom .my-tt").Count);
    }

    // ---- keyboard ----

    [TestMethod]
    public void ArrowKeysShouldWalkTheDataAndAnnounceIt()
    {
        var component = RenderChart();

        component.Find("svg").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });

        var live = component.Find("[role=status]");
        StringAssert.Contains(live.TextContent, "Jan");
        // Counted within the series the arrow keys walk (3 points), not across the whole scene.
        StringAssert.Contains(live.TextContent, "1 of 3");
        StringAssert.Contains(live.TextContent, "series 1 of 2");
        Assert.AreEqual(1, component.FindAll(".bit-cht-focus-ring").Count);
    }

    [TestMethod]
    public void EscapeShouldClearTheKeyboardSelection()
    {
        var component = RenderChart();
        var svg = component.Find("svg");

        svg.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });
        svg.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-cht-focus-ring").Count);
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void EndKeyShouldJumpToTheLastElement()
    {
        var component = RenderChart();

        component.Find("svg").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "End" });

        StringAssert.Contains(component.Find("[role=status]").TextContent, "Mar");
    }

    [TestMethod]
    public void EnterShouldActivateTheFocusedElement()
    {
        (int ds, int di)? clicked = null;
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.OnElementClick, (ValueTuple<int, int> e) => clicked = e);
        });
        var svg = component.Find("svg");

        svg.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Home" });
        svg.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        Assert.IsNotNull(clicked);
    }

    // ---- empty state ----

    [TestMethod]
    public void AnEmptyChartShouldExplainItself()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, new BitChartData());
            p.Add(c => c.NoDataText, "Nothing here");
        });

        Assert.AreEqual("Nothing here", component.Find(".bit-cht-nodata").TextContent.Trim());
    }

    [TestMethod]
    public void AnEmptyChartShouldRenderTheCustomTemplate()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, new BitChartData());
            p.Add(c => c.NoDataTemplate, (builder) =>
            {
                builder.OpenElement(0, "b");
                builder.AddAttribute(1, "class", "empty");
                builder.AddContent(2, "none");
                builder.CloseElement();
            });
        });

        Assert.AreEqual("none", component.Find(".bit-cht-nodata .empty").TextContent);
    }

    [TestMethod]
    public void AChartWithDataShouldNotShowTheEmptyState()
    {
        Assert.AreEqual(0, RenderChart().FindAll(".bit-cht-nodata").Count);
    }

    // ---- titles ----

    [TestMethod]
    public void TitleAndSubtitleShouldRenderAtTheRequestedPositions()
    {
        var options = new BitChartOptions
        {
            Plugins =
            {
                Title = { Display = true, Text = "Main" },
                Subtitle = { Display = true, Text = "Sub", Position = BitChartPosition.Bottom }
            }
        };
        var component = RenderChart(options: options);

        Assert.AreEqual("Main", component.Find(".bit-cht-ttl").TextContent.Trim());
        Assert.AreEqual("Sub", component.Find(".bit-cht-sub").TextContent.Trim());
    }

    [TestMethod]
    public void AMultiLineTitleShouldBreakOnNewlines()
    {
        var options = new BitChartOptions { Plugins = { Title = { Display = true, Text = "One\nTwo" } } };
        var component = RenderChart(options: options);

        Assert.AreEqual(1, component.FindAll(".bit-cht-ttl br").Count);
    }

    // ---- csv export ----

    [TestMethod]
    public void ToCsvShouldWriteOneRowPerSeries()
    {
        var csv = RenderChart().Instance.ToCsv().Replace("\r\n", "\n").TrimEnd('\n');

        var lines = csv.Split('\n');
        Assert.AreEqual("Series,Jan,Feb,Mar", lines[0]);
        Assert.AreEqual("Alpha,1,2,3", lines[1]);
        Assert.AreEqual("Beta,3,2,1", lines[2]);
    }

    [TestMethod]
    public void ToCsvShouldQuoteFieldsThatContainSeparators()
    {
        var data = new BitChartData
        {
            Labels = { "a,b" },
            Datasets = { new BitChartDataset { Label = "say \"hi\"", Data = { 1 } } }
        };
        var csv = RenderChart(BitChartType.Bar, data).Instance.ToCsv();

        StringAssert.Contains(csv, "\"a,b\"");
        StringAssert.Contains(csv, "\"say \"\"hi\"\"\"");
    }

    [TestMethod]
    public void ToCsvShouldWriteOneRowPerPointForScatterData()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Label = "P", Points = [new(1, 2), new(3, 4, 5)] } }
        };
        var csv = RenderChart(BitChartType.Bubble, data).Replace_ToCsv();

        StringAssert.StartsWith(csv, "Series,X,Y,R");
        StringAssert.Contains(csv, "P,3,4,5");
    }

    [TestMethod]
    public void ToCsvShouldFollowTheConfiguredCulture()
    {
        var options = new BitChartOptions { Culture = new CultureInfo("de-DE") };
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets = { new BitChartDataset { Label = "S", Data = { 1.5 } } }
        };
        var csv = RenderChart(BitChartType.Bar, data, options).Instance.ToCsv();

        StringAssert.Contains(csv, "\"1,5\"");
    }

    // ---- zoom api ----

    [TestMethod]
    public async Task ZoomToShouldNarrowTheVisibleRangeAndResetShouldRestoreIt()
    {
        var options = new BitChartOptions { Zoom = { Enabled = true } };
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 0, 50, 100 } } }
        };
        var component = RenderChart(BitChartType.Line, data, options);
        var chart = component.Instance;

        var full = chart.GetAxisRange("y")!.Value;
        await component.InvokeAsync(() => chart.ZoomTo("y", 20, 40));
        var zoomed = chart.GetAxisRange("y")!.Value;
        Assert.AreEqual(20, zoomed.Min, 1e-6);
        Assert.AreEqual(40, zoomed.Max, 1e-6);

        await component.InvokeAsync(chart.ResetZoom);
        Assert.AreEqual(full.Max, chart.GetAxisRange("y")!.Value.Max, 1e-6);
    }

    [TestMethod]
    public void ZoomShouldStayInsideTheDataRange()
    {
        var options = new BitChartOptions { Zoom = { Enabled = true, LimitToData = true } };
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 0, 100 } } }
        };
        var component = RenderChart(BitChartType.Line, data, options);
        var chart = component.Instance;
        var full = chart.GetAxisRange("y")!.Value;

        component.InvokeAsync(() => chart.ZoomTo("y", -500, 5000));
        var clamped = chart.GetAxisRange("y")!.Value;

        Assert.IsTrue(clamped.Min >= full.Min - 1e-6, $"{clamped.Min} < {full.Min}");
        Assert.IsTrue(clamped.Max <= full.Max + 1e-6, $"{clamped.Max} > {full.Max}");
    }

    [TestMethod]
    public void WheelZoomInModeXShouldMoveTheAxisThatRunsAcrossThePlot()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 0, 50, 100 } } }
        };
        var options = new BitChartOptions
        {
            IndexAxis = BitChartIndexAxis.Y,
            Zoom = { Enabled = true, Mode = BitChartZoomMode.X }
        };
        var component = RenderChart(BitChartType.Bar, data, options);
        var chart = component.Instance;

        var before = chart.GetAxisRange("y")!.Value;
        component.InvokeAsync(() => chart.OnWheelZoom(0.5, 0.5, -100));

        // Horizontal bars put the values across the plot, so an "X" gesture is about the value axis.
        var after = chart.GetAxisRange("y")!.Value;
        Assert.IsTrue(after.Max - after.Min < before.Max - before.Min,
            "the mode names a direction on screen, not the axis called x");
    }

    [TestMethod]
    public void WheelZoomInModeXShouldLeaveTheAxisRunningDownThePlotAlone()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 0, 50, 100 } } }
        };
        var options = new BitChartOptions
        {
            IndexAxis = BitChartIndexAxis.Y,
            Zoom = { Enabled = true, Mode = BitChartZoomMode.X }
        };
        var component = RenderChart(BitChartType.Bar, data, options);
        var chart = component.Instance;

        var before = chart.GetAxisRange("x")!.Value;
        component.InvokeAsync(() => chart.OnWheelZoom(0.5, 0.5, -100));

        Assert.AreEqual(before, chart.GetAxisRange("x")!.Value);
    }

    [TestMethod]
    public void WheelZoomShouldCenterOnThePointerOnAVerticalChart()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Points = [new(0, 0), new(100, 100)] } }
        };
        var options = new BitChartOptions { Zoom = { Enabled = true, Mode = BitChartZoomMode.X } };
        var component = RenderChart(BitChartType.Scatter, data, options);
        var chart = component.Instance;

        // Zooming at the left edge keeps the low end and pulls the high end in.
        var before = chart.GetAxisRange("x")!.Value;
        component.InvokeAsync(() => chart.OnWheelZoom(0.02, 0.5, -100));
        var after = chart.GetAxisRange("x")!.Value;

        Assert.IsTrue(after.Max < before.Max);
        Assert.IsTrue(after.Min - before.Min < before.Max - after.Max,
            "the edge nearest the pointer barely moves");
    }

    [TestMethod]
    public async Task ZoomChangeShouldRaiseTheCallback()
    {
        int raised = 0;
        var options = new BitChartOptions { Zoom = { Enabled = true } };
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.Line);
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Options, options);
            p.Add(c => c.OnZoomChange, () => raised++);
        });

        await component.InvokeAsync(() => component.Instance.ZoomTo("y", 1, 2));

        Assert.AreEqual(1, raised);
    }

    // ---- data table size ----

    [TestMethod]
    public void TheScreenReaderTableShouldNotRenderTensOfThousandsOfHiddenRows()
    {
        var points = Enumerable.Range(0, 3000).Select(i => new BitChartDataPoint(i, i)).ToList();
        var data = new BitChartData { Datasets = { new BitChartDataset { Label = "P", Points = points } } };

        var component = RenderChart(BitChartType.Line, data);

        Assert.AreEqual(500, component.FindAll("table tbody tr").Count);
        StringAssert.Contains(component.Find("table caption").TextContent, "3,000");
    }

    [TestMethod]
    public void TheTableShouldRenderEveryRowWhenItFitsTheLimit()
    {
        var points = Enumerable.Range(0, 10).Select(i => new BitChartDataPoint(i, i)).ToList();
        var data = new BitChartData { Datasets = { new BitChartDataset { Label = "P", Points = points } } };

        var component = RenderChart(BitChartType.Line, data);

        Assert.AreEqual(10, component.FindAll("table tbody tr").Count);
        Assert.IsFalse(component.Find("table caption").TextContent.Contains("Showing the first"));
    }

    [TestMethod]
    public void TheTableLimitShouldBeConfigurable()
    {
        var points = Enumerable.Range(0, 40).Select(i => new BitChartDataPoint(i, i)).ToList();
        var data = new BitChartData { Datasets = { new BitChartDataset { Label = "P", Points = points } } };

        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.Line);
            p.Add(c => c.Data, data);
            p.Add(c => c.MaxTableRows, 5);
        });

        Assert.AreEqual(5, component.FindAll("table tbody tr").Count);
    }

    // ---- title placement ----

    [TestMethod]
    public void ASideTitleShouldRenderBesideThePlot()
    {
        var options = new BitChartOptions
        {
            Plugins = { Title = { Display = true, Text = "Down the side", Position = BitChartPosition.Left } }
        };
        var component = RenderChart(options: options);

        var title = component.Find(".bit-cht-mid > .bit-cht-ttl");
        Assert.IsTrue(title.ClassList.Contains("bit-cht-ttl-v"));
        StringAssert.Contains(title.TextContent, "Down the side");
    }

    [TestMethod]
    public void ATitleWithNowhereToGoShouldRenderAtTheTop()
    {
        var options = new BitChartOptions
        {
            Plugins = { Title = { Display = true, Text = "Fallback", Position = BitChartPosition.Chart } }
        };
        var component = RenderChart(options: options);

        Assert.AreEqual(0, component.FindAll(".bit-cht-mid > .bit-cht-ttl").Count);
        StringAssert.Contains(component.Find(".bit-cht > .bit-cht-ttl").TextContent, "Fallback");
    }

    // ---- crosshair ----

    [TestMethod]
    public void HoveringAHitBandShouldNameTheCategoryOnTheAxis()
    {
        var component = RenderChart(BitChartType.Line);

        component.FindAll(".bit-cht-band")[1].MouseEnter();

        var chip = component.FindAll(".bit-cht-hover text").Select(t => t.TextContent).ToList();
        CollectionAssert.Contains(chip, "Feb", string.Join("|", chip));
    }

    [TestMethod]
    public void TheCrosshairCanBeTurnedOff()
    {
        var options = new BitChartOptions { Interaction = { Crosshair = false } };
        var component = RenderChart(BitChartType.Line, options: options);

        component.FindAll(".bit-cht-band")[1].MouseEnter();

        Assert.AreEqual(0, component.FindAll(".bit-cht-hover line").Count);
        Assert.AreEqual(0, component.FindAll(".bit-cht-hover text").Count);
    }

    [TestMethod]
    public void TheCrosshairLabelCanBeTurnedOffOnItsOwn()
    {
        var options = new BitChartOptions { Interaction = { CrosshairLabel = false } };
        var component = RenderChart(BitChartType.Line, options: options);

        component.FindAll(".bit-cht-band")[1].MouseEnter();

        Assert.AreEqual(1, component.FindAll(".bit-cht-hover line").Count);
        Assert.AreEqual(0, component.FindAll(".bit-cht-hover text").Count);
    }

    [TestMethod]
    public void AnEmptyChartShouldNotBeATabStop()
    {
        var component = RenderComponent<BitChart>(p => p.Add(c => c.Data, new BitChartData()));

        Assert.AreEqual("-1", component.Find("svg").GetAttribute("tabindex"));
    }

    // ---- keyboard navigation across series ----

    [TestMethod]
    public void LeftAndRightShouldWalkOneSeriesRatherThanTheWholeScene()
    {
        var component = RenderChart();
        var svg = component.Find("svg");

        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });   // Alpha / Jan
        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });   // Alpha / Feb

        var live = component.Find("[role=status]").TextContent;
        StringAssert.Contains(live, "Feb");
        StringAssert.Contains(live, "Alpha");
        StringAssert.Contains(live, "series 1 of 2");
    }

    [TestMethod]
    public void RightAtTheEndOfASeriesShouldWrapWithinIt()
    {
        var component = RenderChart();
        var svg = component.Find("svg");

        for (int i = 0; i < 4; i++) svg.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        // Three points: the fourth press comes back to the first, still inside the same series.
        var live = component.Find("[role=status]").TextContent;
        StringAssert.Contains(live, "Jan");
        StringAssert.Contains(live, "series 1 of 2");
    }

    [TestMethod]
    public void DownShouldStepToTheOtherSeriesAtTheSameCategory()
    {
        var component = RenderChart();
        var svg = component.Find("svg");

        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });   // Alpha / Jan
        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });   // Alpha / Feb
        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });    // Beta / Feb

        var live = component.Find("[role=status]").TextContent;
        StringAssert.Contains(live, "Feb", "stepping between series must stay on the same category");
        StringAssert.Contains(live, "Beta");
        StringAssert.Contains(live, "series 2 of 2");
    }

    [TestMethod]
    public void UpFromTheFirstSeriesShouldWrapToTheLastOne()
    {
        var component = RenderChart();
        var svg = component.Find("svg");

        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        StringAssert.Contains(component.Find("[role=status]").TextContent, "series 2 of 2");
    }

    [TestMethod]
    public void VerticalKeysShouldStillWalkAChartOfOneSeries()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Label = "Only", Data = { 1, 2, 3 } } }
        };
        var component = RenderChart(BitChartType.Pie, data);
        var svg = component.Find("svg");

        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        svg.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var live = component.Find("[role=status]").TextContent;
        StringAssert.Contains(live, "2 of 3", "with nothing to switch to, down has to keep walking the data");
        Assert.IsFalse(live.Contains("series"), "a single series is not worth announcing");
    }

    // ---- the interaction survives a re-render ----

    [TestMethod]
    public void AnOpenTooltipShouldSurviveARenderTheReaderDidNotAskFor()
    {
        var data = TwoSeries();
        var component = RenderChart(BitChartType.Bar, data);

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();
        Assert.AreEqual(1, component.FindAll(".bit-cht-tt").Count);

        // A parent re-render (same data) must not blink the tooltip out from under the pointer.
        component.Render(p => p.Add(c => c.Class, "re-rendered"));

        Assert.AreEqual(1, component.FindAll(".bit-cht-tt").Count);
    }

    [TestMethod]
    public void TheKeyboardPositionShouldSurviveARerender()
    {
        var component = RenderChart();
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        var before = component.Find("[role=status]").TextContent;

        component.Render(p => p.Add(c => c.Class, "re-rendered"));

        Assert.AreEqual(1, component.FindAll(".bit-cht-focus-ring").Count);
        Assert.AreEqual(before, component.Find("[role=status]").TextContent);
    }

    [TestMethod]
    public void HidingTheHoveredDatasetShouldDropTheTooltipRatherThanKeepAStaleOne()
    {
        var component = RenderChart();

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();
        Assert.AreEqual(1, component.FindAll(".bit-cht-tt").Count);

        component.InvokeAsync(() => component.Instance.ToggleDataset(0));

        Assert.AreEqual(0, component.FindAll(".bit-cht-tt").Count);
    }

    [TestMethod]
    public void LosingTheHoveredDataShouldReportThatNothingIsActive()
    {
        var contexts = new List<BitChartTooltipContext?>();
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.OnElementHover, (BitChartTooltipContext? ctx) => contexts.Add(ctx));
        });

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();
        Assert.IsNotNull(contexts[^1]);

        component.InvokeAsync(() => component.Instance.ToggleDataset(0));

        // Anyone driving a linked view off the callback has to be told the reading is gone.
        Assert.IsNull(contexts[^1], "hiding the hovered data must report that nothing is active any more");
    }

    // ---- touch ----

    [TestMethod]
    public void TappingAnElementShouldShowItsTooltipOnATouchScreen()
    {
        var component = RenderChart();

        component.FindAll(".bit-cht-data > g")[0]
                 .TriggerEvent("onpointerdown", new PointerEventArgs { PointerType = "touch" });

        Assert.AreEqual(1, component.FindAll(".bit-cht-tt").Count);
    }

    [TestMethod]
    public void TappingTheEmptyPlotShouldShowTheCategoryUnderTheFinger()
    {
        var component = RenderChart(BitChartType.Line);

        component.FindAll(".bit-cht-band")[1]
                 .TriggerEvent("onpointerdown", new PointerEventArgs { PointerType = "touch" });

        var tooltip = component.Find(".bit-cht-tt");
        StringAssert.Contains(tooltip.TextContent, "Feb");
    }

    [TestMethod]
    public void AMousePressShouldNotRebuildAHoverItAlreadyHas()
    {
        int hovers = 0;
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.OnElementHover, (BitChartTooltipContext? _) => hovers++);
        });

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();
        // Re-found after the hover render, so the handler id is the current one.
        component.FindAll(".bit-cht-data > g")[0]
                 .TriggerEvent("onpointerdown", new PointerEventArgs { PointerType = "mouse" });

        Assert.AreEqual(1, hovers, "a mouse has already hovered by the time it presses");
    }

    // ---- imperative API ----

    [TestMethod]
    public void RefreshShouldRedrawFromDataMutatedInPlace()
    {
        var data = TwoSeries();
        var component = RenderChart(BitChartType.Bar, data);
        Assert.AreEqual(6, component.FindAll(".bit-cht-data > g").Count);

        data.Labels.Add("Apr");
        data.Datasets[0].Data.Add(4);
        data.Datasets[1].Data.Add(0);
        component.InvokeAsync(component.Instance.Refresh);

        Assert.AreEqual(8, component.FindAll(".bit-cht-data > g").Count,
            "Refresh has to rebuild the scene from the data as it now stands");
    }

    [TestMethod]
    public void TheVisibilityApiShouldDriveTheSameStateAsTheLegend()
    {
        var component = RenderChart();
        var chart = component.Instance;

        Assert.IsTrue(chart.IsDatasetVisible(0));
        component.InvokeAsync(() => chart.SetDatasetVisible(0, false));

        Assert.IsFalse(chart.IsDatasetVisible(0));
        Assert.AreEqual("false", component.FindAll(".bit-cht-lgd-itm")[0].GetAttribute("aria-pressed"));
        Assert.AreEqual(3, component.FindAll(".bit-cht-data > g").Count, "only the second series is left");

        component.InvokeAsync(() => chart.ToggleDataset(0));
        Assert.IsTrue(chart.IsDatasetVisible(0));
        Assert.AreEqual(6, component.FindAll(".bit-cht-data > g").Count);
    }

    [TestMethod]
    public void ADatasetMarkedHiddenShouldStayHiddenThroughTheApi()
    {
        var data = TwoSeries();
        data.Datasets[0].Hidden = true;
        var component = RenderChart(BitChartType.Bar, data);

        Assert.IsFalse(component.Instance.IsDatasetVisible(0));
        component.InvokeAsync(() => component.Instance.SetDatasetVisible(0, true));

        Assert.IsFalse(component.Instance.IsDatasetVisible(0),
            "Hidden is the data's own answer; the visibility API only drives the chart's state");
    }

    [TestMethod]
    public void TheDataIndexApiShouldToggleOneSlice()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 1, 2, 3 } } }
        };
        var component = RenderChart(BitChartType.Doughnut, data);
        var chart = component.Instance;

        Assert.IsTrue(chart.IsDataIndexVisible(1));
        component.InvokeAsync(() => chart.ToggleDataIndex(1));

        Assert.IsFalse(chart.IsDataIndexVisible(1));
        Assert.AreEqual(2, component.FindAll(".bit-cht-data > g").Count);
    }

    [TestMethod]
    public void ResetVisibilityShouldBringEverythingBack()
    {
        var component = RenderChart();
        var chart = component.Instance;

        component.InvokeAsync(() => chart.SetDatasetVisible(0, false));
        component.InvokeAsync(() => chart.SetDatasetVisible(1, false));
        Assert.AreEqual(0, component.FindAll(".bit-cht-data > g").Count);

        component.InvokeAsync(chart.ResetVisibility);

        Assert.AreEqual(6, component.FindAll(".bit-cht-data > g").Count);
    }

    [TestMethod]
    public void TheVisibilityApiShouldIgnoreIndexesThatAreNotThere()
    {
        var component = RenderChart();

        component.InvokeAsync(() => component.Instance.SetDatasetVisible(9, false));

        Assert.IsFalse(component.Instance.IsDatasetVisible(9));
        Assert.AreEqual(6, component.FindAll(".bit-cht-data > g").Count);
    }

    // ---- the screen-reader table stays a table, not a wall of cells ----

    [TestMethod]
    public void TheTableShouldNotRenderThousandsOfColumnsForOneLongSeries()
    {
        var data = new BitChartData();
        for (int i = 0; i < 3000; i++) data.Labels.Add($"L{i}");
        data.Datasets.Add(new BitChartDataset { Label = "S", Data = Enumerable.Range(0, 3000).Select(i => (double?)i).ToList() });

        var component = RenderChart(BitChartType.Line, data);

        Assert.AreEqual(100, component.FindAll("table thead th").Count - 1);
        Assert.AreEqual(100, component.FindAll("table tbody td").Count);
        StringAssert.Contains(component.Find("table caption").TextContent, "3,000");
        StringAssert.Contains(component.Find("table caption").TextContent, "columns");
    }

    [TestMethod]
    public void TheColumnLimitShouldBeConfigurable()
    {
        var data = new BitChartData();
        for (int i = 0; i < 20; i++) data.Labels.Add($"L{i}");
        data.Datasets.Add(new BitChartDataset { Label = "S", Data = Enumerable.Range(0, 20).Select(i => (double?)i).ToList() });

        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, data);
            p.Add(c => c.MaxTableColumns, 5);
        });

        Assert.AreEqual(5, component.FindAll("table tbody td").Count);
    }

    [TestMethod]
    public void ATableThatFitsShouldNotClaimToBeTruncated()
    {
        var component = RenderChart();

        Assert.AreEqual(3, component.FindAll("table tbody tr td").Count / 2);
        Assert.IsFalse(component.Find("table caption").TextContent.Contains("columns"));
    }

    [TestMethod]
    public void TheTableShouldNameTheErrorIntervalBesideItsValue()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets =
            {
                new BitChartDataset { Label = "S", Data = { 10, 20, 30 }, ErrorData = [2, new BitChartErrorBar(1, 4), null] }
            }
        };
        var component = RenderChart(BitChartType.Bar, data);

        var cells = component.FindAll("table tbody td").Select(c => c.TextContent).ToList();
        Assert.AreEqual("10 ±2", cells[0]);
        Assert.AreEqual("20 +4/-1", cells[1]);
        Assert.AreEqual("30", cells[2], "a value without an interval reads exactly as it always did");
    }

    // ---- clicking the plate between the elements ----

    [TestMethod]
    public void ClickingTheEmptyPlotShouldReportTheIndexUnderThePointer()
    {
        (int ds, int di)? clicked = null;
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.Line);
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.OnElementClick, (ValueTuple<int, int> e) => clicked = e);
        });

        component.FindAll(".bit-cht-band")[1].Click();

        Assert.IsNotNull(clicked);
        Assert.AreEqual(1, clicked!.Value.di, "the click reports the category it landed in");
    }

    [TestMethod]
    public void TheEmptyPlotShouldOnlyLookClickableWhenItIs()
    {
        var plain = RenderChart(BitChartType.Line);
        StringAssert.Contains(plain.Find(".bit-cht-band").GetAttribute("style"), "cursor:default");

        var clickable = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.Line);
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.OnElementClick, (ValueTuple<int, int> _) => { });
        });
        StringAssert.Contains(clickable.Find(".bit-cht-band").GetAttribute("style"), "cursor:pointer");
    }

    // ---- the focus ring traces the element ----

    [TestMethod]
    public void TheFocusRingShouldOutlineARoundedBarRatherThanACircleInIt()
    {
        var data = TwoSeries();
        data.Datasets[0].BorderRadius = 6;
        var component = RenderChart(BitChartType.Bar, data);

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Home" });

        var ring = component.Find(".bit-cht-focus-ring");
        Assert.AreEqual("path", ring.TagName.ToLowerInvariant(),
            "a rounded bar is a path, so its ring has to be one too");
        Assert.AreEqual("none", ring.GetAttribute("fill"));
    }

    [TestMethod]
    public void TheFocusRingShouldOutlineAnArc()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 1, 2, 3 } } }
        };
        var component = RenderChart(BitChartType.Doughnut, data);

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Home" });

        Assert.AreEqual("path", component.Find(".bit-cht-focus-ring").TagName.ToLowerInvariant());
    }

    [TestMethod]
    public void TheFocusRingShouldStillBoxAPlainBar()
    {
        var component = RenderChart();

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Home" });

        Assert.AreEqual("rect", component.Find(".bit-cht-focus-ring").TagName.ToLowerInvariant());
    }

    // ---- pinch zoom ----

    [TestMethod]
    public void PinchingApartShouldZoomInAroundTheFingers()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Points = [new(0, 0), new(100, 100)] } }
        };
        var options = new BitChartOptions { Zoom = { Enabled = true, Mode = BitChartZoomMode.X } };
        var component = RenderChart(BitChartType.Scatter, data, options);
        var chart = component.Instance;

        var before = chart.GetAxisRange("x")!.Value;
        component.InvokeAsync(() => chart.OnPinchZoom(0.5, 0.5, 2));
        var after = chart.GetAxisRange("x")!.Value;

        Assert.IsTrue(after.Max - after.Min < before.Max - before.Min, "spreading the fingers zooms in");
    }

    [TestMethod]
    public void PinchingTogetherShouldZoomBackOut()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Points = [new(0, 0), new(100, 100)] } }
        };
        var options = new BitChartOptions { Zoom = { Enabled = true, Mode = BitChartZoomMode.X } };
        var component = RenderChart(BitChartType.Scatter, data, options);
        var chart = component.Instance;

        component.InvokeAsync(() => chart.OnPinchZoom(0.5, 0.5, 4));
        var zoomed = chart.GetAxisRange("x")!.Value;
        component.InvokeAsync(() => chart.OnPinchZoom(0.5, 0.5, 0.5));
        var after = chart.GetAxisRange("x")!.Value;

        Assert.IsTrue(after.Max - after.Min > zoomed.Max - zoomed.Min);
    }

    [TestMethod]
    [DataRow(0d)]
    [DataRow(-1d)]
    [DataRow(double.NaN)]
    public void AMeaninglessPinchScaleShouldBeIgnored(double scale)
    {
        var options = new BitChartOptions { Zoom = { Enabled = true } };
        var component = RenderChart(BitChartType.Line, options: options);
        var chart = component.Instance;

        var before = chart.GetAxisRange("y")!.Value;
        component.InvokeAsync(() => chart.OnPinchZoom(0.5, 0.5, scale));

        Assert.AreEqual(before, chart.GetAxisRange("y")!.Value);
    }

    // ---- sparkline ----

    [TestMethod]
    public void ASparklineShouldStillCarryItsScreenReaderTable()
    {
        var component = RenderChart(BitChartType.Line, options: new BitChartOptions { Sparkline = true });

        Assert.AreEqual(0, component.FindAll(".bit-cht-lgd").Count, "a sparkline has no legend");
        Assert.IsNotNull(component.Find("table"), "dropping the chrome must not drop the accessible data");
        Assert.AreEqual("0", component.Find("svg").GetAttribute("tabindex"), "it stays keyboard reachable");
    }
}

internal static class BitChartTestExtensions
{
    /// <summary>Reads the CSV off a rendered chart (kept short so the assertions stay readable).</summary>
    public static string Replace_ToCsv(this IRenderedComponent<BitChart> component)
        => component.Instance.ToCsv().Replace("\r\n", "\n");
}
