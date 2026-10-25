using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

[TestClass]
public class BitChartParamsTests : BunitTestContext
{
    [TestMethod]
    public void BitChartShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitChartCascadingParamsTest>();

        var charts = component.FindAll(".bit-cht");

        Assert.AreEqual(4, charts.Count);

        // The first chart takes everything from the cascading parameters.
        var first = charts[0];
        Assert.IsTrue(first.ClassList.Contains("cascaded"));
        Assert.IsTrue(first.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(first.GetAttribute("style"), "height:200px");
        StringAssert.Contains(first.GetAttribute("style"), "width:50%");
        StringAssert.Contains(first.GetAttribute("style"), "--bit-Chart-grid-color: red");
        Assert.AreEqual("Shared title", first.QuerySelector("svg")!.GetAttribute("aria-label"));
        Assert.IsNull(first.QuerySelector("table"), "GenerateTable=false is cascaded");
        Assert.IsNotNull(first.QuerySelector(".cascaded-legend"));
        Assert.AreEqual("Cascaded legend", first.QuerySelector(".bit-cht-lgd")!.GetAttribute("aria-label"));
        Assert.AreEqual(2, first.QuerySelectorAll(".bit-cht-data rect").Length, "the cascaded Bar type draws bars");

        // The second one sets its own type, height, table and classes, which the cascade must not overwrite.
        var second = charts[1];
        Assert.IsTrue(second.ClassList.Contains("second"));
        Assert.IsTrue(second.ClassList.Contains("own-root"));
        Assert.IsFalse(second.ClassList.Contains("cascaded"));
        Assert.IsFalse(second.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(second.GetAttribute("style"), "height:120px");
        Assert.AreEqual("Own legend", second.QuerySelector(".bit-cht-lgd")!.GetAttribute("aria-label"));
        Assert.IsNotNull(second.QuerySelector("table"));
        Assert.AreEqual(0, second.QuerySelectorAll(".bit-cht-data rect").Length, "its own Line type draws no bars");

        // The third one has no data, so the cascaded empty-state text shows.
        StringAssert.Contains(charts[2].QuerySelector(".bit-cht-nodata")!.TextContent, "Nothing yet");

        // The fourth one is loading, and says so with the cascaded label.
        StringAssert.Contains(charts[3].QuerySelector(".bit-cht-ldg")!.TextContent, "Cascaded loading");
    }

    // What makes one chart the one it is: its data and config, its description, whether it is loading, and its callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitChart.CascadingParameters),
        nameof(BitChart.Config),
        nameof(BitChart.Data),
        nameof(BitChart.Description),
        nameof(BitChart.IsLoading),
        nameof(BitChart.OnElementClick),
        nameof(BitChart.OnElementHover),
        nameof(BitChart.OnLegendItemClick),
        nameof(BitChart.OnZoomChange),
    ];

    private static BitChartData TwoSeries() => new()
    {
        Labels = { "Jan", "Feb", "Mar" },
        Datasets =
        {
            new BitChartDataset { Label = "Alpha", Data = { 1, 2, 3 } },
            new BitChartDataset { Label = "Beta", Data = { 3, 2, 1 } }
        }
    };

    private static BitChartData LongSeries(int count)
    {
        var data = new BitChartData();
        for (var i = 0; i < count; i++) data.Labels.Add($"L{i}");
        data.Datasets.Add(new BitChartDataset { Label = "S", Data = Enumerable.Range(0, count).Select(i => (double?)i).ToList() });
        return data;
    }

    private static RenderFragment Marker(string cssClass) => builder => builder.AddMarkupContent(0, $"<span class=\"{cssClass}\"></span>");

    private IRenderedComponent<BitParams> RenderWithParams(BitChartParams chartParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { chartParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitChart>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitChartParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitChart)}", BitChartParams.ParamName);
        Assert.AreEqual("BitParams.BitChart", BitChartParams.ParamName);
    }

    [TestMethod]
    public void BitChartParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitChartParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitChartParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitChartParamsShouldCarryEveryParameterThatIsNotPerChart()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitChart).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Select(p => p.Name)
                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitChartParams).GetProperty(name), $"BitChartParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitChartShouldTakeTheCascadedLoadingAndNoDataTemplates()
    {
        var chartParams = new BitChartParams
        {
            LoadingTemplate = Marker("cascaded-loading"),
            NoDataTemplate = Marker("cascaded-nodata"),
        };

        var loading = RenderWithParams(chartParams, builder => builder.AddAttribute(1, nameof(BitChart.IsLoading), true));
        Assert.AreEqual(1, loading.FindAll(".bit-cht-ldg .cascaded-loading").Count);

        var empty = RenderWithParams(chartParams);
        Assert.AreEqual(1, empty.FindAll(".bit-cht-nodata .cascaded-nodata").Count);
    }

    [TestMethod]
    public void BitChartShouldTakeTheCascadedTableLimits()
    {
        var component = RenderWithParams(new BitChartParams { MaxTableRows = 5, MaxTableColumns = 4 }, builder =>
        {
            builder.AddAttribute(1, nameof(BitChart.Data), new BitChartData
            {
                Datasets = { new BitChartDataset { Label = "P", Points = Enumerable.Range(0, 40).Select(i => new BitChartDataPoint(i, i)).ToList() } }
            });
        });

        Assert.AreEqual(5, component.FindAll("table tbody tr").Count);

        var columns = RenderWithParams(new BitChartParams { MaxTableColumns = 4 }, builder => builder.AddAttribute(1, nameof(BitChart.Data), LongSeries(20)));

        Assert.AreEqual(4, columns.FindAll("table tbody td").Count);
    }

    [TestMethod]
    public void BitChartShouldTakeTheCascadedHints()
    {
        var component = RenderWithParams(new BitChartParams
        {
            NavigationHint = "Cascaded navigation hint.",
            ZoomHint = "Cascaded zoom hint.",
            Options = new BitChartOptions { Zoom = { Enabled = true } },
        }, builder => builder.AddAttribute(1, nameof(BitChart.Data), TwoSeries()));

        var hint = component.Find($"#{component.Find("svg").GetAttribute("aria-describedby")!.Split(' ')[0]}").TextContent;

        StringAssert.Contains(hint, "Cascaded navigation hint.");
        StringAssert.Contains(hint, "Cascaded zoom hint.");
    }

    [TestMethod]
    public void BitChartShouldTakeTheCascadedTooltipTemplate()
    {
        var component = RenderWithParams(new BitChartParams
        {
            TooltipTemplate = ctx => builder => builder.AddMarkupContent(0, $"<span class=\"cascaded-tt\">{ctx.Points.Count}</span>"),
        }, builder => builder.AddAttribute(1, nameof(BitChart.Data), TwoSeries()));

        component.FindAll(".bit-cht-data > g")[0].MouseEnter();

        Assert.AreEqual(1, component.FindAll(".bit-cht-tt-custom .cascaded-tt").Count);
    }

    [TestMethod]
    public void BitChartShouldKeepItsOwnHintsAndLimitsOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitChartParams
        {
            MaxTableColumns = 4,
            NavigationHint = "Cascaded navigation hint.",
            NoDataTemplate = Marker("cascaded-nodata"),
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitChart.Data), LongSeries(20));
            builder.AddAttribute(2, nameof(BitChart.MaxTableColumns), 7);
            builder.AddAttribute(3, nameof(BitChart.NavigationHint), "Own hint.");
        });

        Assert.AreEqual(7, component.FindAll("table tbody td").Count);

        var hint = component.Find($"#{component.Find("svg").GetAttribute("aria-describedby")!.Split(' ')[0]}").TextContent;

        StringAssert.Contains(hint, "Own hint.");
        Assert.IsFalse(hint.Contains("Cascaded navigation hint."));

        // An own null is a value too: it turns the hint off against a cascaded one.
        var silent = RenderWithParams(new BitChartParams { NavigationHint = "Cascaded navigation hint." }, builder =>
        {
            builder.AddAttribute(1, nameof(BitChart.Data), TwoSeries());
            builder.AddAttribute(2, nameof(BitChart.NavigationHint), (string?)null);
        });

        Assert.IsNull(silent.Find("svg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChartShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitChartParams(), builder => builder.AddAttribute(1, nameof(BitChart.Data), TwoSeries()));

        var root = component.Find(".bit-cht");

        // The defaults of a chart on its own: a line chart, full width, with its table and its English hint.
        Assert.AreEqual(0, root.QuerySelectorAll(".bit-cht-data rect").Length);
        StringAssert.Contains(root.GetAttribute("style"), "width:100%");
        Assert.IsNotNull(root.QuerySelector("table"));
        StringAssert.Contains(root.TextContent, "arrow");
    }
}
