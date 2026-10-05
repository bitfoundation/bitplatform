using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
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
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so they are supplied
        // as raw component attributes, as real markup would write them.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitChart>(0);
            builder.AddAttribute(1, nameof(BitChart.Data), TwoSeries());
            builder.AddAttribute(2, "data-test", "chart");
            builder.CloseComponent();
        });

        Assert.AreEqual("chart", component.Find(".bit-cht").GetAttribute("data-test"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitChartShouldRespectForceAnimation(bool forceAnimation)
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.ForceAnimation, forceAnimation);
        });

        Assert.AreEqual(forceAnimation, component.Find(".bit-cht").ClassList.Contains("bit-fam"));
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
    public void BitChartShouldRenderAScreenReaderTableBesideTheChart()
    {
        var component = RenderChart();

        var table = component.Find("table");
        var svg = component.Find("svg");
        // The description is the how-to-navigate sentence alone: a table pointed at by aria-describedby would be read
        // as one flat string on every focus, so it is a real table a screen reader browses instead.
        var described = svg.GetAttribute("aria-describedby")!;
        Assert.IsFalse(described.Contains(table.Id!), "the table is read as one flat string when it is a description");
        Assert.IsNotNull(component.Find($"#{described}"));
        Assert.AreEqual(3, table.QuerySelectorAll("thead th").Length - 1);
        Assert.AreEqual(2, table.QuerySelectorAll("tbody tr").Length);
        StringAssert.Contains(table.QuerySelector("caption")!.TextContent, "Bar chart with 2 data series.");
    }

    [TestMethod]
    public void BitChartShouldTellAScreenReaderHowToWalkTheData()
    {
        var component = RenderChart();

        var hintId = component.Find("svg").GetAttribute("aria-describedby")!;
        StringAssert.Contains(component.Find($"#{hintId}").TextContent, "arrow keys");
        Assert.AreEqual("chart", component.Find("svg").GetAttribute("aria-roledescription"));
    }

    [TestMethod]
    public void TheNavigablePlotShouldBeAnApplicationAndAPictureOtherwise()
    {
        // An application is what makes a screen reader in browse mode hand the arrow keys to the chart.
        Assert.AreEqual("application", RenderChart().Find("svg").GetAttribute("role"));

        var empty = RenderComponent<BitChart>(p => p.Add(c => c.Data, new BitChartData()));
        Assert.AreEqual("img", empty.Find("svg").GetAttribute("role"));

        var disabled = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Disabled, true);
        });
        Assert.AreEqual("img", disabled.Find("svg").GetAttribute("role"));
        Assert.AreEqual("-1", disabled.Find("svg").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void TheDrawingShouldBeHiddenFromAssistiveTechnologies()
    {
        var component = RenderChart();

        // An application exposes its children, and the tick labels and shapes are not something to read one by one:
        // the live region and the table say what they show.
        foreach (var group in component.FindAll("svg.bit-cht-svg > g"))
            Assert.AreEqual("true", group.GetAttribute("aria-hidden"), $"{group.ClassName} is exposed");
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
        Assert.IsNull(silent.Find("svg").GetAttribute("aria-describedby"));
        Assert.AreEqual(1, silent.FindAll("table").Count);
    }

    [TestMethod]
    public void AnEmptyChartShouldNotPromiseKeyboardNavigation()
    {
        var component = RenderComponent<BitChart>(p => p.Add(c => c.Data, new BitChartData()));

        // Nothing to walk: the hint would be a lie.
        Assert.IsNull(component.Find("svg").GetAttribute("aria-describedby"));
        Assert.AreEqual(0, component.FindAll(".bit-cht-sr[id$='-hint']").Count);
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

        // With nothing to do on a click the entries are labels, not controls that do nothing.
        var item = component.FindAll(".bit-cht-lgd-itm")[0];
        Assert.AreEqual("span", item.TagName.ToLowerInvariant());
        Assert.IsNull(item.GetAttribute("aria-pressed"));
        Assert.AreEqual(0, component.FindAll(".bit-cht-lgd button").Count);
    }

    [TestMethod]
    public void ALegendWithOnlyAClickHandlerShouldBeButtonsWithoutAPressedState()
    {
        BitChartLegendItemModel? clicked = null;
        var options = new BitChartOptions { Plugins = { Legend = { OnClickToggle = false } } };
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Options, options);
            p.Add(c => c.OnLegendItemClick, (BitChartLegendItemModel i) => clicked = i);
        });

        var item = component.FindAll(".bit-cht-lgd-itm")[0];
        Assert.AreEqual("button", item.TagName.ToLowerInvariant());
        Assert.IsNull(item.GetAttribute("aria-pressed"), "a button that toggles nothing has no pressed state to report");

        item.Click();

        Assert.AreEqual("Alpha", clicked?.Text);
        Assert.IsFalse(component.FindAll(".bit-cht-lgd-itm")[0].ClassList.Contains("bit-cht-hdn"));
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
    public void ScatterCsvShouldCarryARadiusColumnOnlyForBubbleData()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Label = "P", Points = [new(1, 2), new(3, 4)] } }
        };
        var lines = RenderChart(BitChartType.Scatter, data).Replace_ToCsv().TrimEnd('\n').Split('\n');

        CollectionAssert.AreEqual(new[] { "Series,X,Y", "P,1,2", "P,3,4" }, lines, "the table shows no radius column here, and neither does the file");
    }

    [TestMethod]
    public void PointsOnDifferentAxesShouldNotBeHeadedByOneOfThemAlone()
    {
        var data = new BitChartData
        {
            Datasets =
            {
                new BitChartDataset { Label = "Revenue", Points = [new(1, 2)] },
                new BitChartDataset { Label = "Margin", Points = [new(3, 40)], YAxisID = "y2" }
            }
        };
        var options = new BitChartOptions
        {
            Scales =
            {
                ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Linear, Title = new() { Display = true, Text = "Month" } },
                ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Title = new() { Display = true, Text = "Revenue" } },
                ["y2"] = new BitChartScaleOptions { Id = "y2", Type = BitChartScaleType.Linear, Position = BitChartPosition.Right, Title = new() { Display = true, Text = "Margin %" } }
            }
        };
        var component = RenderChart(BitChartType.Scatter, data, options);

        // Both series share the x axis, so its title still names that column; the y values are on two axes.
        StringAssert.StartsWith(component.Instance.ToCsv(), "Series,Month,Y");
        CollectionAssert.AreEqual(new[] { "Series", "Month", "Y" }, component.FindAll("table thead th").Select(h => h.TextContent).ToList());
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

    [TestMethod]
    public void PlusAndMinusShouldZoomFromTheKeyboardAndZeroShouldReset()
    {
        var options = new BitChartOptions { Zoom = { Enabled = true } };
        var component = RenderChart(BitChartType.Line, options: options);
        var chart = component.Instance;
        var full = chart.GetAxisRange("x")!.Value;

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "+" });
        var zoomed = chart.GetAxisRange("x")!.Value;
        Assert.IsLessThan(full.Max - full.Min, zoomed.Max - zoomed.Min, "plus zooms in");

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "-" });
        var out1 = chart.GetAxisRange("x")!.Value;
        Assert.IsGreaterThan(zoomed.Max - zoomed.Min, out1.Max - out1.Min, "minus zooms back out");

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "+" });
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "0" });
        Assert.AreEqual(full, chart.GetAxisRange("x")!.Value, "0 resets");
    }

    [TestMethod]
    public void TheZoomKeysShouldLeaveTheBrowsersOwnZoomShortcutsAlone()
    {
        var options = new BitChartOptions { Zoom = { Enabled = true } };
        var component = RenderChart(BitChartType.Line, options: options);
        var chart = component.Instance;
        var full = chart.GetAxisRange("x")!.Value;

        // Ctrl/Meta with plus, minus or zero is the page zoom, which the chart must not join in.
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "+", CtrlKey = true });
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "=", MetaKey = true });
        Assert.AreEqual(full, chart.GetAxisRange("x")!.Value);

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "+" });
        var zoomed = chart.GetAxisRange("x")!.Value;
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "0", CtrlKey = true });
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "-", AltKey = true });
        Assert.AreEqual(zoomed, chart.GetAxisRange("x")!.Value, "a page-zoom reset must not discard the chart's own zoom");
    }

    [TestMethod]
    public void TheZoomKeysShouldDoNothingWithoutZoom()
    {
        var component = RenderChart(BitChartType.Line);
        var full = component.Instance.GetAxisRange("x")!.Value;

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "+" });

        Assert.AreEqual(full, component.Instance.GetAxisRange("x")!.Value);
    }

    [TestMethod]
    public void TheHintShouldNameTheZoomKeysOnlyWhileZoomIsEnabled()
    {
        var hintOf = (IRenderedComponent<BitChart> c) => c.Find($"#{c.Find("svg").GetAttribute("aria-describedby")!.Split(' ')[0]}").TextContent;

        Assert.DoesNotContain("zoom", hintOf(RenderChart(BitChartType.Line)));

        var zoomable = RenderChart(BitChartType.Line, options: new BitChartOptions { Zoom = { Enabled = true } });
        StringAssert.Contains(hintOf(zoomable), "Press plus or minus to zoom");

        var silenced = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.Line);
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Options, new BitChartOptions { Zoom = { Enabled = true } });
            p.Add(c => c.ZoomHint, null);
        });
        Assert.DoesNotContain("zoom", hintOf(silenced));
    }

    // ---- theming ----

    [TestMethod]
    public void TheDefaultPaletteShouldBeThePublicSeriesVariables()
    {
        var component = RenderChart();

        var bars = component.FindAll(".bit-cht-data rect");
        Assert.AreEqual("var(--bit-Chart-series-color-1, #36a2eb)", bars[0].GetAttribute("fill"));
        Assert.AreEqual("var(--bit-Chart-series-color-2, #ff6384)", bars[3].GetAttribute("fill"));
        StringAssert.Contains(component.FindAll(".bit-cht-lgd-box")[0].GetAttribute("style"), "--bit-Chart-series-color-1");
    }

    [TestMethod]
    public void AnAreaOfAThemedSeriesShouldStayTranslucent()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1, 2 }, Fill = BitChartFillMode.Origin } }
        };
        var component = RenderChart(BitChartType.Line, data);

        var area = component.FindAll(".bit-cht-series path").First(p => p.GetAttribute("fill") is { } f && f != "none");
        Assert.AreEqual("color-mix(in srgb, var(--bit-Chart-series-color-1, #36a2eb) 20%, transparent)", area.GetAttribute("fill"));
    }

    [TestMethod]
    public void TheTooltipShouldFollowTheThemeUnlessTheOptionsSayOtherwise()
    {
        var component = RenderChart();
        component.FindAll(".bit-cht-el")[0].MouseEnter();

        var style = component.Find(".bit-cht-tt").GetAttribute("style")!;
        StringAssert.Contains(style, "background:var(--bit-Chart-tooltip-background, var(--bit-clr-tooltip-bg))");
        Assert.DoesNotContain("border-radius", style, "the radius comes from the stylesheet's theme token");

        var options = new BitChartOptions { Plugins = { Tooltip = { CornerRadius = 2 } } };
        var custom = RenderChart(options: options);
        custom.FindAll(".bit-cht-el")[0].MouseEnter();

        StringAssert.Contains(custom.Find(".bit-cht-tt").GetAttribute("style"), "border-radius:2px");
    }

    [TestMethod]
    public void TheEasingShouldOnlyBeWrittenWhenTheOptionsNameOne()
    {
        Assert.DoesNotContain("--bit-cht-ease", RenderChart().Find("svg").GetAttribute("style")!);

        var options = new BitChartOptions { Animation = { Easing = "linear" } };
        StringAssert.Contains(RenderChart(options: options).Find("svg").GetAttribute("style"), "--bit-cht-ease:linear");
    }

    [TestMethod]
    public void ClassesAndStylesShouldReachEveryPart()
    {
        var options = new BitChartOptions
        {
            Plugins =
            {
                Title = { Display = true, Text = "T" },
                Subtitle = { Display = true, Text = "S" }
            }
        };
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.Bar);
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Options, options);
            p.Add(c => c.Classes, new BitChartClassStyles
            {
                Root = "c-root", Title = "c-title", Subtitle = "c-sub", Legend = "c-legend",
                LegendItem = "c-item", Plot = "c-plot", Tooltip = "c-tip"
            });
            p.Add(c => c.Styles, new BitChartClassStyles
            {
                Root = "margin:1px", Title = "margin:2px", Subtitle = "margin:3px", Legend = "margin:4px",
                LegendItem = "margin:5px", Plot = "margin:6px", Tooltip = "margin:7px"
            });
        });
        component.FindAll(".bit-cht-el")[0].MouseEnter();

        Assert.IsTrue(component.Find(".bit-cht").ClassList.Contains("c-root"));
        StringAssert.Contains(component.Find(".bit-cht").GetAttribute("style"), "margin:1px");
        StringAssert.Contains(component.Find(".bit-cht-ttl.c-title").GetAttribute("style"), "margin:2px");
        StringAssert.Contains(component.Find(".bit-cht-sub.c-sub").GetAttribute("style"), "margin:3px");
        StringAssert.Contains(component.Find(".bit-cht-lgd.c-legend").GetAttribute("style"), "margin:4px");
        StringAssert.Contains(component.FindAll(".bit-cht-lgd-itm.c-item")[1].GetAttribute("style"), "margin:5px");
        StringAssert.Contains(component.Find(".bit-cht-plot.c-plot").GetAttribute("style"), "margin:6px");
        StringAssert.Contains(component.Find(".bit-cht-tt.c-tip").GetAttribute("style"), "margin:7px");
    }

    [TestMethod]
    public void TheTitleStylesShouldWinOverTheGeneratedOnes()
    {
        var options = new BitChartOptions
        {
            Plugins =
            {
                Title = { Display = true, Text = "T" },
                Subtitle = { Display = true, Text = "S" }
            }
        };
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.Bar);
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Options, options);
            p.Add(c => c.Styles, new BitChartClassStyles { Title = "color:red", Subtitle = "font-size:24px" });
        });

        // An inline declaration later in the attribute wins, so the caller's has to come after the generated ones.
        StringAssert.EndsWith(component.Find(".bit-cht-ttl").GetAttribute("style")!.TrimEnd(';'), "color:red");
        StringAssert.EndsWith(component.Find(".bit-cht-sub").GetAttribute("style")!.TrimEnd(';'), "font-size:24px");
    }

    [TestMethod]
    public void TheEmptyStateShouldTakeItsClassAndStyle()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Classes, new BitChartClassStyles { NoData = "c-empty" });
            p.Add(c => c.Styles, new BitChartClassStyles { NoData = "margin:8px" });
        });

        StringAssert.Contains(component.Find(".bit-cht-nodata.c-empty").GetAttribute("style"), "margin:8px");
    }

    // ---- enabled state ----

    [TestMethod]
    public void ADisabledChartShouldTakeNoInput()
    {
        int clicks = 0;
        BitChartLegendItemModel? legendClicked = null;
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.Bar);
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Disabled, true);
            p.Add(c => c.OnElementClick, (_) => clicks++);
            p.Add(c => c.OnLegendItemClick, (BitChartLegendItemModel i) => legendClicked = i);
        });

        Assert.IsTrue(component.Find(".bit-cht").ClassList.Contains("bit-dis"));
        Assert.AreEqual("-1", component.Find("svg").GetAttribute("tabindex"), "a disabled chart is not a tab stop");
        Assert.IsTrue(component.FindAll(".bit-cht-lgd-itm").All(b => b.HasAttribute("disabled")));

        component.FindAll(".bit-cht-el")[0].MouseEnter();
        component.FindAll(".bit-cht-el")[0].Click();
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual(0, component.FindAll(".bit-cht-tt").Count);
        Assert.AreEqual(0, clicks);
        Assert.IsTrue(string.IsNullOrEmpty(component.Find("[role=status]").TextContent));
        StringAssert.Contains(component.FindAll(".bit-cht-el")[0].GetAttribute("style"), "cursor:default");
        Assert.IsNull(legendClicked);
    }

    [TestMethod]
    public void DisablingAChartShouldLetGoOfItsKeyboardPosition()
    {
        var component = RenderChart();
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual(1, component.FindAll(".bit-cht-tt").Count);

        component.Render(p => p.Add(c => c.Disabled, true));

        Assert.AreEqual(0, component.FindAll(".bit-cht-tt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-cht-focus-ring").Count);
    }

    [TestMethod]
    public void TabIndexShouldMoveThePlotsTabStop()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.TabIndex, "3");
        });

        Assert.AreEqual("3", component.Find("svg").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void TheCascadingDirectionShouldReachTheChart()
    {
        var component = RenderComponent<CascadingValue<BitDir?>>(p =>
        {
            p.Add(c => c.Value, BitDir.Rtl);
            p.AddChildContent<BitChart>(c => c.Add(x => x.Data, TwoSeries()));
        });

        var root = component.Find(".bit-cht");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void TheFocusRingShouldReadThePublicFocusVariable()
    {
        var component = RenderChart();
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("var(--bit-Chart-focus-color, var(--bit-clr-pri-focus))",
            component.Find(".bit-cht-focus-ring").GetAttribute("stroke"));
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

    // ---- texts ----

    private static readonly BitChartTexts PersianTexts = new()
    {
        DefaultAriaLabelFormat = "نمودار {0} با {1} سری",
        LegendAriaLabel = "راهنمای نمودار",
        PositionFormat = "{0} از {1}",
        SeriesPositionFormat = "سری {0} از {1}",
        DatasetLabelFormat = "مجموعه {0}",
        Series = "سری",
        RowsTruncatedFormat = "{0} از {1} ردیف"
    };

    [TestMethod]
    public void TextsShouldLocalizeWhatTheChartWritesForAssistiveTechnologies()
    {
        var data = new BitChartData
        {
            Labels = { "Jan", "Feb" },
            Datasets = { new BitChartDataset { Data = { 1, 2 } }, new BitChartDataset { Label = "Beta", Data = { 3, 4 } } }
        };
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, data);
            p.Add(c => c.Texts, PersianTexts);
            p.Add(c => c.MaxTableRows, 1);
        });

        Assert.AreEqual("نمودار Line با 2 سری", component.Find("svg").GetAttribute("aria-label"));
        Assert.AreEqual("راهنمای نمودار", component.Find(".bit-cht-lgd").GetAttribute("aria-label"));
        // An unlabeled dataset gets the same name in the legend, the table and the CSV.
        StringAssert.Contains(component.Find(".bit-cht-lgd").TextContent, "مجموعه 1");
        Assert.AreEqual("مجموعه 1", component.Find("table tbody th").TextContent);
        Assert.AreEqual("سری", component.Find("table thead th").TextContent);
        StringAssert.Contains(component.Find("table caption").TextContent, "1 از 2 ردیف");
        StringAssert.StartsWith(component.Instance.ToCsv(), "سری,Jan,Feb");

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        var live = component.Find(".bit-cht-sr[role=status]").TextContent;
        StringAssert.Contains(live, "1 از 2");
        StringAssert.Contains(live, "سری 1 از 2");
    }

    [TestMethod]
    public void TheEnglishTextsShouldBeTheDefault()
    {
        var data = new BitChartData { Labels = { "Jan" }, Datasets = { new BitChartDataset { Data = { 1 } } } };
        var component = RenderChart(BitChartType.Bar, data);

        Assert.AreEqual("Chart legend", component.Find(".bit-cht-lgd").GetAttribute("aria-label"));
        StringAssert.Contains(component.Find(".bit-cht-lgd").TextContent, "Dataset 1");
        Assert.AreEqual("Dataset 1", component.Find("table tbody th").TextContent);
    }

    [TestMethod]
    public void AMalformedTextFormatShouldNotBreakTheChart()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Texts, new BitChartTexts { PositionFormat = "{0} of {1" });
        });

        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        // A stray brace in a translation is a typo: the text is said as written rather than throwing out of the render.
        StringAssert.Contains(component.Find(".bit-cht-sr[role=status]").TextContent, "{0} of {1");
    }

    [TestMethod]
    public void TheBubbleTableShouldCarryTheRadiusAndTheAxisTitles()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Label = "Cities", Points = [new(1, 2, 5), new(3, 4, 8)] } }
        };
        var options = new BitChartOptions
        {
            Scales =
            {
                ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Linear, Title = new() { Display = true, Text = "GDP" } },
                ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Title = new() { Display = false, Text = "Hidden" } }
            }
        };
        var component = RenderChart(BitChartType.Bubble, data, options);

        // The axis title says what the values are; a title the axis does not show is not used.
        var headers = component.FindAll("table thead th").Select(h => h.TextContent).ToList();
        CollectionAssert.AreEqual(new[] { "Series", "GDP", "Y", "R" }, headers);
        CollectionAssert.AreEqual(new[] { "1", "2", "5" }, component.FindAll("table tbody tr")[0].QuerySelectorAll("td").Select(c => c.TextContent).ToList());
        StringAssert.StartsWith(component.Instance.ToCsv(), "Series,GDP,Y,R");
    }

    // ---- loading ----

    [TestMethod]
    public void TheLoadingStateShouldVeilThePlotAndHoldBackTheEmptyState()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, new BitChartData());
            p.Add(c => c.IsLoading, true);
            p.Add(c => c.LoadingLabel, "Fetching");
        });

        Assert.AreEqual("true", component.Find(".bit-cht").GetAttribute("aria-busy"));
        var loading = component.Find(".bit-cht-ldg");
        // Announced by the live region that is always there, since one appearing with its text is often not read.
        Assert.AreEqual("Fetching", component.Find(".bit-cht-sr[role=status]").TextContent.Trim());
        Assert.IsNull(loading.GetAttribute("role"));
        StringAssert.Contains(loading.TextContent, "Fetching");
        Assert.AreEqual(0, component.FindAll(".bit-cht-nodata").Count, "a chart still loading claimed it has no data");

        component.Render(p => p.Add(c => c.IsLoading, false));

        Assert.IsNull(component.Find(".bit-cht").GetAttribute("aria-busy"));
        Assert.AreEqual(0, component.FindAll(".bit-cht-ldg").Count);
        Assert.AreEqual(1, component.FindAll(".bit-cht-nodata").Count);
    }

    [TestMethod]
    public void ALoadingChartShouldTakeNoInput()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.LoadingTemplate, b => b.AddMarkupContent(0, "<i class=\"skeleton\"></i>"));
        });
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreNotEqual(string.Empty, component.Find(".bit-cht-sr[role=status]").TextContent.Trim());

        component.Render(p => p.Add(c => c.IsLoading, true));

        // The keyboard position is let go of - the live region says the loading label instead - the plot leaves the
        // tab order, and the template replaces the spinner.
        Assert.AreEqual("Loading", component.Find(".bit-cht-sr[role=status]").TextContent.Trim());
        Assert.AreEqual("-1", component.Find("svg").GetAttribute("tabindex"));
        Assert.AreEqual(1, component.FindAll(".bit-cht-ldg .skeleton").Count);
        Assert.AreEqual(0, component.FindAll(".bit-cht-spn").Count);

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();
        Assert.AreEqual(0, component.FindAll(".bit-cht-tt").Count);
    }

    // ---- dismissing a hover tooltip (WCAG 1.4.13) ----

    [TestMethod]
    public void EscapeShouldDismissATooltipThePointerOpened()
    {
        BitChartTooltipContext? reported = new();
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.OnElementHover, (BitChartTooltipContext? ctx) => reported = ctx);
        });
        component.FindAll(".bit-cht-data > g")[0].MouseEnter();
        Assert.AreEqual(1, component.FindAll(".bit-cht-tt").Count);

        component.InvokeAsync(component.Instance.OnDismissTooltip);

        Assert.AreEqual(0, component.FindAll(".bit-cht-tt").Count);
        Assert.IsNull(reported, "whoever tracks the active element was not told it is gone");
    }

    [TestMethod]
    public void EscapeOnThePageShouldLeaveTheKeyboardPositionToThePlot()
    {
        var component = RenderChart();
        component.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        component.InvokeAsync(component.Instance.OnDismissTooltip);

        Assert.AreEqual(1, component.FindAll(".bit-cht-focus-ring").Count);
    }

    // ---- legend highlight ----

    [TestMethod]
    public void HoveringALegendItemShouldFadeTheOtherSeries()
    {
        var component = RenderChart(BitChartType.Line);
        var items = component.FindAll(".bit-cht-lgd-itm");

        items[1].MouseEnter();

        // Beta is brought forward: Alpha's line and points fade, Beta's do not.
        var dimmedLines = component.FindAll(".bit-cht-series > g.bit-cht-dim");
        Assert.AreEqual(1, dimmedLines.Count);
        Assert.AreEqual(3, component.FindAll(".bit-cht-el.bit-cht-dim").Count);
        Assert.AreEqual(6, component.FindAll(".bit-cht-el").Count);

        component.FindAll(".bit-cht-lgd-itm")[1].MouseLeave();

        Assert.AreEqual(0, component.FindAll(".bit-cht-dim").Count);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void TheLegendHighlightShouldBeLetGoOfWhenTheChartStopsTakingInput(bool loading)
    {
        var component = RenderChart(BitChartType.Line);
        component.FindAll(".bit-cht-lgd-itm")[1].MouseEnter();
        Assert.AreNotEqual(0, component.FindAll(".bit-cht-dim").Count);

        // A disabled legend button need not fire mouseleave, so the fade cannot wait for one.
        component.Render(p =>
        {
            if (loading) p.Add(c => c.IsLoading, true);
            else p.Add(c => c.Disabled, true);
        });
        Assert.AreEqual(0, component.FindAll(".bit-cht-dim").Count);

        component.FindAll(".bit-cht-lgd-itm")[1].MouseEnter();
        Assert.AreEqual(0, component.FindAll(".bit-cht-dim").Count);
    }

    [TestMethod]
    public void ADataLabelShouldFadeWithItsSeries()
    {
        var component = RenderChart(options: new BitChartOptions
        {
            Plugins = new BitChartPluginOptions { DataLabels = new BitChartDataLabelOptions { Display = true } }
        });

        component.FindAll(".bit-cht-lgd-itm")[0].MouseEnter();

        // Beta's three labels fade with its bars; Alpha's stay.
        Assert.AreEqual(3, component.FindAll(".bit-cht-fg > g.bit-cht-dim text").Count);
        Assert.AreEqual(6, component.FindAll(".bit-cht-fg text").Count);
    }

    [TestMethod]
    public void FocusingALegendItemShouldHighlightLikeHovering()
    {
        var component = RenderChart();

        component.FindAll(".bit-cht-lgd-itm")[0].Focus();
        Assert.AreEqual(3, component.FindAll(".bit-cht-el.bit-cht-dim").Count);

        component.FindAll(".bit-cht-lgd-itm")[0].Blur();
        Assert.AreEqual(0, component.FindAll(".bit-cht-dim").Count);
    }

    [TestMethod]
    public void ASliceLegendShouldHighlightItsSlice()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 1, 2, 3 } } }
        };
        var component = RenderChart(BitChartType.Pie, data);

        component.FindAll(".bit-cht-lgd-itm")[2].MouseEnter();

        Assert.AreEqual(2, component.FindAll(".bit-cht-el.bit-cht-dim").Count);
    }

    [TestMethod]
    public void TheLegendHighlightCanBeTurnedOffAndSkipsAHiddenSeries()
    {
        var off = RenderChart(options: new BitChartOptions
        {
            Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { HighlightOnHover = false } }
        });
        off.FindAll(".bit-cht-lgd-itm")[0].MouseEnter();
        Assert.AreEqual(0, off.FindAll(".bit-cht-dim").Count);

        // A hidden series is not drawn, so bringing it forward would only fade everything else.
        var component = RenderChart();
        component.FindAll(".bit-cht-lgd-itm")[0].Click();
        component.FindAll(".bit-cht-lgd-itm")[0].MouseEnter();
        Assert.AreEqual(0, component.FindAll(".bit-cht-dim").Count);
    }

    // ---- descriptions ----

    [TestMethod]
    public void TheDescriptionShouldBeWhatThePlotIsDescribedByFirst()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Description, "Alpha falls while Beta rises.");
        });

        var ids = component.Find("svg").GetAttribute("aria-describedby")!.Split(' ');
        Assert.AreEqual(2, ids.Length, "the summary, then how to walk the data");
        Assert.AreEqual("Alpha falls while Beta rises.", component.Find($"#{ids[0]}").TextContent);
        StringAssert.Contains(component.Find($"#{ids[1]}").TextContent, "arrow keys");
        Assert.IsTrue(component.Find($"#{ids[0]}").ClassList.Contains("bit-cht-sr"), "it is visually hidden");
    }

    [TestMethod]
    public void AChartThatCannotBeWalkedShouldStillBeDescribed()
    {
        var component = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Description, "Flat.");
            p.Add(c => c.Disabled, true);
        });

        var svg = component.Find("svg");
        Assert.AreEqual("img", svg.GetAttribute("role"));
        Assert.AreEqual("Flat.", component.Find($"#{svg.GetAttribute("aria-describedby")}").TextContent);
    }

    [TestMethod]
    public void WhatThePluginsDrawShouldBeToldToAScreenReader()
    {
        var options = new BitChartOptions();
        options.Plugins.Custom.Add(new BitChartAnnotationPlugin(
            new BitChartAnnotation { Value = 2, Label = "Target" },
            new BitChartAnnotation { Value = 1 }));
        var component = RenderChart(BitChartType.Line, options: options);

        var ids = component.Find("svg").GetAttribute("aria-describedby")!.Split(' ');
        Assert.AreEqual("Marked on the chart: Target: 2.", component.Find($"#{ids[0]}").TextContent);

        var silent = RenderChart(BitChartType.Line);
        Assert.AreEqual(1, silent.Find("svg").GetAttribute("aria-describedby")!.Split(' ').Length, "no plugin, no note");
    }

    [TestMethod]
    public void TheDefaultNameShouldCallTheTypeByAReadableName()
    {
        var component = RenderChart(BitChartType.PolarArea);
        Assert.AreEqual("Polar area chart with 2 data series.", component.Find("svg").GetAttribute("aria-label"));

        var texts = new BitChartTexts { TypeNames = { [BitChartType.PolarArea] = "قطبی" }, DefaultAriaLabelFormat = "نمودار {0}", RoleDescription = "نمودار" };
        var localized = RenderComponent<BitChart>(p =>
        {
            p.Add(c => c.Type, BitChartType.PolarArea);
            p.Add(c => c.Data, TwoSeries());
            p.Add(c => c.Texts, texts);
        });
        Assert.AreEqual("نمودار قطبی", localized.Find("svg").GetAttribute("aria-label"));
        Assert.AreEqual("نمودار", localized.Find("svg").GetAttribute("aria-roledescription"));
    }

    [TestMethod]
    public void AStartAlignedTitleShouldFollowTheReadingDirection()
    {
        var component = RenderChart(options: new BitChartOptions
        {
            Plugins = new BitChartPluginOptions { Title = new BitChartTitleOptions { Display = true, Text = "T", Align = BitChartAlign.Start } }
        });

        StringAssert.Contains(component.Find(".bit-cht-ttl > div").GetAttribute("style"), "text-align:start");
    }
}

internal static class BitChartTestExtensions
{
    /// <summary>Reads the CSV off a rendered chart (kept short so the assertions stay readable).</summary>
    public static string Replace_ToCsv(this IRenderedComponent<BitChart> component)
        => component.Instance.ToCsv().Replace("\r\n", "\n");
}
