using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

/// <summary>
/// Scene-level tests: they drive <see cref="BitChartRenderer"/> directly, which is where every layout,
/// scale and styling decision is made, so they pin the behavior without depending on the DOM.
/// </summary>
[TestClass]
public class BitChartRendererTests
{
    private static BitChartScene Render(BitChartConfig config, double w = 600, double h = 300, string uid = "u1",
        BitChartRenderState? state = null)
        => new BitChartRenderer(config, state ?? new BitChartRenderState(), w, h, uid).Render();

    private static BitChartData Bars(params double?[] values) => new()
    {
        Labels = { "A", "B", "C" },
        Datasets = { new BitChartDataset { Label = "S", Data = values.ToList() } }
    };

    // ---- scales are resolved without touching the caller's options ----

    [TestMethod]
    public void RendererShouldNotMutateTheCallersScaleDictionary()
    {
        var options = new BitChartOptions();
        Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        Assert.AreEqual(0, options.Scales.Count,
            "the renderer must complete the scales locally so one options instance can be shared between charts");
    }

    [TestMethod]
    public void RendererShouldNotWriteDefaultPositionsBackOntoUserScales()
    {
        var scale = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear };
        var options = new BitChartOptions { Scales = { ["y"] = scale } };
        Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3), options));

        Assert.IsNull(scale.Position, "an unset position must stay unset; the default is applied per render");
    }

    [TestMethod]
    public void SharedOptionsShouldRenderDifferentChartTypesIndependently()
    {
        var options = new BitChartOptions();
        var cartesian = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3), options));
        var pie = Render(new BitChartConfig(BitChartType.Pie, Bars(1, 2, 3), options));

        Assert.IsNotNull(cartesian.PlotArea);
        Assert.IsTrue(pie.IsRadialOrCircular);
        Assert.IsNull(pie.PlotArea);
    }

    // ---- def ids are namespaced per chart ----

    [TestMethod]
    public void GradientAndPatternIdsShouldBeNamespacedPerChartInstance()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset
                {
                    Data = { 1, 2 },
                    Fill = BitChartFillMode.Origin,
                    FillGradient = BitChartLinearGradient.Vertical2("#fff", "#000")
                },
                new BitChartDataset { Data = { 1, 2 }, Type = BitChartType.Bar, BackgroundPattern = new BitChartFillPattern() }
            }
        };

        var a = Render(new BitChartConfig(BitChartType.Line, data), uid: "chartA");
        var b = Render(new BitChartConfig(BitChartType.Line, data), uid: "chartB");

        Assert.IsTrue(a.Defs[0].Id.StartsWith("chartA"), a.Defs[0].Id);
        Assert.IsTrue(b.Defs[0].Id.StartsWith("chartB"), b.Defs[0].Id);
        Assert.AreNotEqual(a.Defs[0].Id, b.Defs[0].Id, "two charts on one page must not share a defs id");
        Assert.IsTrue(a.Patterns[0].Id.StartsWith("chartA"), a.Patterns[0].Id);
    }

    // ---- bar styling ----

    [TestMethod]
    public void BarWithoutBorderColorShouldNotBeOutlined()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets = { new BitChartDataset { Data = { 10 }, BackgroundColor = "#ff0000" } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        var rect = (BitChartSvgRect)scene.Elements[0].Shape;
        Assert.AreEqual(0, rect.StrokeWidth, "bars default to no border, like Chart.js");
        Assert.IsNull(scene.Elements[0].BorderShape);
    }

    [TestMethod]
    public void BarBorderShouldFallBackToTheFillColorInsteadOfThePalette()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets =
            {
                new BitChartDataset { Data = { 1 }, BackgroundColor = "#111111" },
                new BitChartDataset { Data = { 1 }, BackgroundColor = "#222222", BorderWidth = 2 }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        var border = (BitChartSvgPath)scene.Elements[1].BorderShape!;
        Assert.AreEqual("#222222", border.Stroke,
            "with no explicit border color a bar takes its own fill, not an unrelated palette entry");
    }

    [TestMethod]
    public void ExplicitBorderColorShouldGiveABarAOnePixelBorderByDefault()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets = { new BitChartDataset { Data = { 1 }, BackgroundColor = "#111", BorderColor = "#999" } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        var border = (BitChartSvgPath)scene.Elements[0].BorderShape!;
        Assert.AreEqual("#999", border.Stroke);
        Assert.AreEqual(1, border.StrokeWidth);
    }

    [TestMethod]
    public void UniformBorderRadiusShouldOnlyRoundTheCornersAwayFromTheBaseline()
    {
        BitChartScene RenderWithSkip(BitChartBorderSkipped skip) => Render(new BitChartConfig(BitChartType.Bar, new BitChartData
        {
            Labels = { "A" },
            Datasets = { new BitChartDataset { Data = { 10 }, BorderRadius = 8, BorderSkipped = skip } }
        }));

        var skipped = (BitChartSvgPath)RenderWithSkip(BitChartBorderSkipped.Start).Elements[0].Shape;
        var all = (BitChartSvgPath)RenderWithSkip(BitChartBorderSkipped.None).Elements[0].Shape;

        Assert.AreNotEqual(skipped.D, all.D,
            "the default skips the baseline edge, so only the tip corners round; BorderSkipped.None rounds all four");
    }

    [TestMethod]
    public void MinBarLengthShouldKeepTinyValuesVisible()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 100, 0.0001 }, MinBarLength = 12 } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        var tiny = (BitChartSvgRect)scene.Elements[1].Shape;
        Assert.IsTrue(tiny.Height >= 12, $"expected at least 12px, got {tiny.Height}");
    }

    [TestMethod]
    public void BarBaseShouldMoveWhereBarsStartFrom()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets = { new BitChartDataset { Data = { 100 }, Base = 50 } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data, new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Min = 0, Max = 100 } }
        }));

        var rect = (BitChartSvgRect)scene.Elements[0].Shape;
        var plot = scene.PlotArea!.Value;
        // Half the axis: the bar covers 50..100, i.e. the upper half of the plot.
        Assert.AreEqual(plot.Height / 2, rect.Height, plot.Height * 0.02);
    }

    [TestMethod]
    public void UngroupedBarDatasetShouldSpanTheWholeCategory()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 5, 6 } },
                new BitChartDataset { Data = { 7, 8 } },
                new BitChartDataset { Data = { 3, 4 }, Grouped = false }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        // The two grouped datasets share the category; the ungrouped one sits behind them at full width.
        var grouped = (BitChartSvgRect)scene.Elements.First(e => e.DatasetIndex == 0).Shape;
        var ungrouped = (BitChartSvgRect)scene.Elements.First(e => e.DatasetIndex == 2).Shape;
        Assert.AreEqual(grouped.Width * 2, ungrouped.Width, 0.01,
            $"an ungrouped bar keeps the whole band ({ungrouped.Width} vs {grouped.Width})");
    }

    [TestMethod]
    public void SkipNullShouldWidenTheRemainingBarsOfACategory()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 5, null }, SkipNull = true },
                new BitChartDataset { Data = { 3, 4 }, SkipNull = true }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        var shared = (BitChartSvgRect)scene.Elements.First(e => e.DatasetIndex == 1 && e.DataIndex == 0).Shape;
        var alone = (BitChartSvgRect)scene.Elements.First(e => e.DatasetIndex == 1 && e.DataIndex == 1).Shape;
        Assert.IsTrue(alone.Width > shared.Width * 1.8,
            $"the surviving bar fills the category ({alone.Width} vs {shared.Width})");
    }

    [TestMethod]
    public void BarBaselineShouldBeTheAxisZeroLineNotTheLastBarDrawn()
    {
        // Two independent stacks: the last bar drawn sits well above zero, so a baseline taken from it
        // would make the entry animation scale out of the wrong place.
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets =
            {
                new BitChartDataset { Data = { 10 }, Stack = "s" },
                new BitChartDataset { Data = { 10 }, Stack = "s" }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data, new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Stacked = true } }
        }));

        Assert.AreEqual(scene.PlotArea!.Value.Bottom, scene.BarBaseline, 0.5);
    }

    // ---- stacking ----

    [TestMethod]
    public void IndependentStacksShouldNotAddUpIntoOneAxisRange()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets =
            {
                new BitChartDataset { Data = { 10 }, Stack = "left" },
                new BitChartDataset { Data = { 10 }, Stack = "left" },
                new BitChartDataset { Data = { 10 }, Stack = "right" },
                new BitChartDataset { Data = { 10 }, Stack = "right" }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data, new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Stacked = true } }
        }));

        Assert.AreEqual(20, scene.DataRanges["y"].Max, 1e-6,
            "the axis must fit the tallest stack (20), not the sum of every dataset (40)");
    }

    [TestMethod]
    public void StackedLineAreasShouldNormalizeWhenStacked100IsSet()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 30, 10 }, Fill = BitChartFillMode.Origin },
                new BitChartDataset { Data = { 10, 30 }, Fill = BitChartFillMode.Origin }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data, new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Stacked = true, Stacked100 = true } }
        }));

        Assert.AreEqual(0, scene.DataRanges["y"].Min, 1e-6);
        Assert.AreEqual(100, scene.DataRanges["y"].Max, 1e-6);
        Assert.IsTrue(scene.Series.Count > 0);
    }

    // ---- line / area ----

    [TestMethod]
    public void AreaFillShouldUseTheDatasetBackgroundColor()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 1, 2 }, Fill = BitChartFillMode.Origin, BackgroundColor = "#00ff00", BorderColor = "#f00" }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data));

        var fill = (BitChartSvgPath)scene.Series[0];
        Assert.AreEqual("#00ff00", fill.Fill);
    }

    [TestMethod]
    public void ExplicitFillColorShouldWinOverBackgroundColor()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 1, 2 }, Fill = BitChartFillMode.Origin, BackgroundColor = "#00ff00", FillColor = "#0000ff" }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data));

        Assert.AreEqual("#0000ff", ((BitChartSvgPath)scene.Series[0]).Fill);
    }

    [TestMethod]
    public void LineBorderWidthShouldBeHonoredExactlyWhenSet()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1, 2 }, BorderWidth = 1 } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data));

        var line = scene.Series.OfType<BitChartSvgPath>().First(p => p.Stroke is not null);
        Assert.AreEqual(1, line.StrokeWidth, "a dataset asking for a 1px line must get a 1px line");
    }

    [TestMethod]
    public void LineBorderWidthShouldFallBackToTheElementDefault()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1, 2 } } }
        };
        var options = new BitChartOptions { Elements = { LineBorderWidth = 5 } };
        var scene = Render(new BitChartConfig(BitChartType.Line, data, options));

        var line = scene.Series.OfType<BitChartSvgPath>().First(p => p.Stroke is not null);
        Assert.AreEqual(5, line.StrokeWidth);
    }

    [TestMethod]
    public void ABrokenSeriesShouldRegisterItsFillPaintOnlyOnce()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C", "D", "E" },
            Datasets =
            {
                new BitChartDataset
                {
                    Data = { 1, 2, null, 4, 5 },
                    Fill = BitChartFillMode.Origin,
                    FillGradient = BitChartLinearGradient.Vertical2("#fff", "#000")
                }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data));

        Assert.AreEqual(1, scene.Defs.Count, "a gap must not add a duplicate gradient definition");
    }

    [TestMethod]
    public void MarkerlessLineShouldStillProduceHoverableElements()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 1, 2, 3 }, PointRadius = 0 } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data));

        Assert.AreEqual(3, scene.Elements.Count,
            "a line drawn without markers still needs hit targets, otherwise it can never show a tooltip");
    }

    [TestMethod]
    public void PointStyleNoneShouldSuppressMarkersEntirely()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1, 2 }, PointStyle = BitChartPointStyle.None } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data));

        Assert.AreEqual(0, scene.Elements.Count);
    }

    // ---- hit bands ----

    [TestMethod]
    public void HitBandsShouldCoverEveryCategoryByDefault()
    {
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3)));

        Assert.AreEqual(3, scene.HitBands.Count);
        var plot = scene.PlotArea!.Value;
        Assert.AreEqual(plot.Left, scene.HitBands.First().X, 0.5);
        Assert.AreEqual(plot.Right, scene.HitBands.Last().X + scene.HitBands.Last().Width, 0.5);
    }

    [TestMethod]
    public void HitBandsShouldBeSkippedWhenTheInteractionRequiresAnIntersection()
    {
        var options = new BitChartOptions { Interaction = { Intersect = true } };
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        Assert.AreEqual(0, scene.HitBands.Count);
    }

    [TestMethod]
    public void HitBandsShouldBeSkippedForScatterCharts()
    {
        var data = new BitChartData
        {
            Datasets =
            {
                new BitChartDataset { Points = [new(1, 1), new(2, 4), new(3, 9)] }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Scatter, data));

        Assert.AreEqual(0, scene.HitBands.Count, "scatter points are placed by value, so index bands make no sense");
        Assert.AreEqual(3, scene.Elements.Count);
    }

    [TestMethod]
    public void HorizontalBarHitBandsShouldRunAcrossThePlot()
    {
        var options = new BitChartOptions { IndexAxis = BitChartIndexAxis.Y };
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3), options));

        Assert.AreEqual(3, scene.HitBands.Count);
        var plot = scene.PlotArea!.Value;
        Assert.AreEqual(plot.Width, scene.HitBands[0].Width, 0.5);
    }

    // ---- hover shapes ----

    [TestMethod]
    [DataRow(BitChartType.Bar)]
    [DataRow(BitChartType.Line)]
    [DataRow(BitChartType.Pie)]
    [DataRow(BitChartType.Doughnut)]
    [DataRow(BitChartType.PolarArea)]
    [DataRow(BitChartType.Radar)]
    public void EveryElementShouldCarryAPrecomputedHoverShape(BitChartType type)
    {
        var scene = Render(new BitChartConfig(type, Bars(1, 2, 3)));

        Assert.IsTrue(scene.Elements.Count > 0);
        foreach (var el in scene.Elements)
            Assert.IsNotNull(el.HoverShape, $"{type} elements must know how they look when hovered");
    }

    [TestMethod]
    public void HoveredArcShouldBePushedOutFromTheCenter()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1, 1 }, HoverOffset = 20 } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Pie, data));

        var normal = (BitChartSvgPath)scene.Elements[0].Shape;
        var hover = (BitChartSvgPath)scene.Elements[0].HoverShape!;
        Assert.AreNotEqual(normal.D, hover.D);
    }

    [TestMethod]
    public void HoverBackgroundColorShouldBeUsedByTheHoverShape()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets = { new BitChartDataset { Data = { 5 }, BackgroundColor = "#111", HoverBackgroundColor = "#abcdef" } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        Assert.AreEqual("#abcdef", ((BitChartSvgRect)scene.Elements[0].HoverShape!).Fill);
    }

    [TestMethod]
    public void ScriptableColorsShouldSeeActiveTrueWhileBuildingTheHoverShape()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets =
            {
                new BitChartDataset
                {
                    Data = { 5 },
                    BackgroundColorFn = ctx => ctx.Active ? "#00ff00" : "#ff0000"
                }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        Assert.AreEqual("#ff0000", ((BitChartSvgRect)scene.Elements[0].Shape).Fill);
        Assert.AreEqual("#00ff00", ((BitChartSvgRect)scene.Elements[0].HoverShape!).Fill);
    }

    // ---- data labels ----

    [TestMethod]
    public void HorizontalBarDataLabelsShouldSitBesideTheBarNotOnTheDiagonal()
    {
        var options = new BitChartOptions
        {
            IndexAxis = BitChartIndexAxis.Y,
            Plugins = { DataLabels = { Display = true } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(10, 20, 30), options));

        var labels = scene.Foreground.OfType<BitChartSvgText>().ToList();
        Assert.AreEqual(3, labels.Count);
        for (int i = 0; i < labels.Count; i++)
            Assert.AreEqual(scene.Elements[i].CenterY, labels[i].Y, 0.01,
                "a horizontal bar's label must be vertically centered on its bar");
    }

    [TestMethod]
    public void DataLabelsShouldBeDrawnForLinePoints()
    {
        var options = new BitChartOptions { Plugins = { DataLabels = { Display = true } } };
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        Assert.AreEqual(3, scene.Foreground.OfType<BitChartSvgText>().Count());
    }

    [TestMethod]
    public void ShowOnPointsFalseShouldKeepLinePointsClean()
    {
        var options = new BitChartOptions { Plugins = { DataLabels = { Display = true, ShowOnPoints = false } } };
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        Assert.AreEqual(0, scene.Foreground.OfType<BitChartSvgText>().Count());
    }

    [TestMethod]
    public void DataLabelAnchorShouldMoveTheLabelBetweenTipAndBaseline()
    {
        double LabelY(BitChartAlign anchor)
        {
            var options = new BitChartOptions { Plugins = { DataLabels = { Display = true, Anchor = anchor, Align = BitChartAlign.Center } } };
            var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(10, 20, 30), options));
            return scene.Foreground.OfType<BitChartSvgText>().First().Y;
        }

        double tip = LabelY(BitChartAlign.End);
        double middle = LabelY(BitChartAlign.Center);
        double baseline = LabelY(BitChartAlign.Start);

        Assert.IsTrue(tip < middle && middle < baseline, $"tip {tip}, middle {middle}, baseline {baseline}");
    }

    // ---- culture ----

    [TestMethod]
    public void TickLabelsShouldFollowTheConfiguredCulture()
    {
        var options = new BitChartOptions { Culture = new CultureInfo("de-DE") };
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1000, 5000 } } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data, options));

        var labels = scene.Background.OfType<BitChartSvgText>().Select(t => t.Text).ToList();
        Assert.IsTrue(labels.Any(l => l.Contains('.')), string.Join("|", labels));
        Assert.IsFalse(labels.Any(l => l.Contains(',')), string.Join("|", labels));
    }

    [TestMethod]
    public void TicksShouldUseAnExplicitNumericFormatWhenGiven()
    {
        var options = new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Ticks = { Format = "0.0" } } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        var labels = scene.Background.OfType<BitChartSvgText>().Select(t => t.Text).ToList();
        Assert.IsTrue(labels.Any(l => l.Contains('.')), string.Join("|", labels));
    }

    // ---- axes ----

    [TestMethod]
    public void AShortChartShouldNotCrowdItsValueAxisWithLabels()
    {
        int TickCount(double height)
        {
            var scene = Render(new BitChartConfig(BitChartType.Line, Bars(0, 50, 100)), h: height);
            return scene.Background.OfType<BitChartSvgText>().Count();
        }

        Assert.IsTrue(TickCount(120) < TickCount(700),
            "the tick count has to follow the space actually available along the axis");
    }

    [TestMethod]
    public void MirroredTicksShouldNotReserveSpaceOutsideThePlot()
    {
        BitChartArea Plot(bool mirror)
        {
            var options = new BitChartOptions
            {
                Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Ticks = { Mirror = mirror } } }
            };
            return Render(new BitChartConfig(BitChartType.Line, Bars(1000, 2000, 3000), options)).PlotArea!.Value;
        }

        Assert.IsTrue(Plot(true).Left < Plot(false).Left);
    }

    [TestMethod]
    public void HiddenAxisShouldReserveNoSpace()
    {
        BitChartArea Plot(bool display)
        {
            var options = new BitChartOptions
            {
                Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Display = display } }
            };
            return Render(new BitChartConfig(BitChartType.Line, Bars(1000, 2000, 3000), options)).PlotArea!.Value;
        }

        Assert.IsTrue(Plot(false).Left < Plot(true).Left);
    }

    [TestMethod]
    public void DataRangesShouldRecordTheUnzoomedExtentForEveryAxis()
    {
        var state = new BitChartRenderState();
        state.AxisRanges["y"] = (0, 5);
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(0, 50, 100)), state: state);

        Assert.AreEqual(100, scene.DataRanges["y"].Max, 1e-6, "the full range must survive a zoom");
        Assert.AreEqual(5, scene.AxisRanges["y"].Max, 1e-6, "the visible range is the zoomed one");
    }

    // ---- legend ----

    [TestMethod]
    public void LegendFilterShouldRemoveItems()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets =
            {
                new BitChartDataset { Label = "keep", Data = { 1 } },
                new BitChartDataset { Label = "drop", Data = { 2 } }
            }
        };
        var options = new BitChartOptions { Plugins = { Legend = { Filter = i => i.Text != "drop" } } };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data, options));

        Assert.AreEqual(1, scene.Legend!.Items.Count);
        Assert.AreEqual("keep", scene.Legend.Items[0].Text);
    }

    [TestMethod]
    public void CircularLegendShouldListTheLabelsNotTheDatasets()
    {
        var scene = Render(new BitChartConfig(BitChartType.Doughnut, Bars(1, 2, 3)));

        Assert.AreEqual(3, scene.Legend!.Items.Count);
        Assert.IsTrue(scene.Legend.Items.All(i => i.IsDataIndex));
    }

    // ---- empty ----

    [TestMethod]
    public void ASceneWithNothingToDrawShouldReportItself()
    {
        Assert.IsTrue(Render(new BitChartConfig(BitChartType.Line, new BitChartData())).IsEmpty);
        Assert.IsTrue(Render(new BitChartConfig(BitChartType.Pie, new BitChartData())).IsEmpty);
        Assert.IsFalse(Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3))).IsEmpty);
    }

    [TestMethod]
    public void HidingEveryDatasetShouldProduceAnEmptyScene()
    {
        var state = new BitChartRenderState();
        state.HiddenDatasets.Add(0);
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3)), state: state);

        Assert.IsTrue(scene.IsEmpty);
    }

    // ---- arcs ----

    [TestMethod]
    public void ArcSpacingShouldLeaveAGapBetweenSlices()
    {
        BitChartSvgPath First(double spacing) =>
            (BitChartSvgPath)Render(new BitChartConfig(BitChartType.Pie, new BitChartData
            {
                Labels = { "A", "B", "C" },
                Datasets = { new BitChartDataset { Data = { 1, 1, 1 }, SpacingArc = spacing } }
            })).Elements[0].Shape;

        Assert.AreNotEqual(First(0).D, First(10).D);
    }

    [TestMethod]
    public void DoughnutCutoutShouldProduceARingPath()
    {
        var options = new BitChartOptions { CutoutPercentage = 60 };
        var scene = Render(new BitChartConfig(BitChartType.Doughnut, Bars(1, 2, 3), options));

        // A ring path traces two arcs; a solid pie wedge traces one.
        var d = ((BitChartSvgPath)scene.Elements[0].Shape).D;
        Assert.AreEqual(2, d.Split('A').Length - 1, d);
    }

    // ---- mixed / multi axis ----

    [TestMethod]
    public void PerDatasetTypeShouldProduceAMixedChart()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 1, 2 }, Type = BitChartType.Bar },
                new BitChartDataset { Data = { 3, 4 }, Type = BitChartType.Line }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        Assert.IsTrue(scene.HasBars);
        Assert.IsTrue(scene.Series.Count > 0, "the line dataset must contribute a series path");
        Assert.IsTrue(scene.Elements.Any(e => e.Shape is BitChartSvgRect));
        Assert.IsTrue(scene.Elements.Any(e => e.Shape is BitChartSvgCircle));
    }

    [TestMethod]
    public void ASecondValueAxisShouldGetItsOwnRange()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 1, 2 }, YAxisID = "y" },
                new BitChartDataset { Data = { 1000, 2000 }, YAxisID = "y2" }
            }
        };
        var options = new BitChartOptions
        {
            Scales = { ["y2"] = new BitChartScaleOptions { Id = "y2", Position = BitChartPosition.Right } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data, options));

        Assert.IsTrue(scene.AxisRanges["y"].Max < 100);
        Assert.IsTrue(scene.AxisRanges["y2"].Max >= 2000);
    }

    // ---- plugins ----

    [TestMethod]
    public void AnnotationPluginShouldDrawInFrontOrBehindAsAsked()
    {
        var options = new BitChartOptions();
        options.Plugins.Custom.Add(new BitChartAnnotationPlugin(
            new BitChartAnnotation { Value = 2, Label = "target" },
            new BitChartAnnotation { Value = 1, DrawBehindDatasets = true }));

        var before = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3))).Foreground.Count;
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        Assert.IsTrue(scene.Foreground.Count > before);
        Assert.IsTrue(scene.Background.OfType<BitChartSvgLine>().Any());
    }

    [TestMethod]
    public void AnnotationLabelWidthShouldFollowTheMeasuredText()
    {
        double Width(string label)
        {
            var options = new BitChartOptions();
            options.Plugins.Custom.Add(new BitChartAnnotationPlugin(new BitChartAnnotation { Value = 2, Label = label }));
            var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));
            return scene.Foreground.OfType<BitChartSvgRect>().First().Width;
        }

        Assert.IsTrue(Width("iiii") < Width("WWWW"), "the label pill is measured, not counted");
    }

    [TestMethod]
    public void CenterTextPluginShouldDrawInsideTheDoughnut()
    {
        var options = new BitChartOptions { CutoutPercentage = 70 };
        options.Plugins.Custom.Add(new BitChartCenterTextPlugin("120", "total"));
        var scene = Render(new BitChartConfig(BitChartType.Doughnut, Bars(1, 2, 3), options));

        var texts = scene.Foreground.OfType<BitChartSvgText>().Select(t => t.Text).ToList();
        CollectionAssert.Contains(texts, "120");
        CollectionAssert.Contains(texts, "total");
    }

    // ---- tooltips ----

    [TestMethod]
    public void TooltipLabelCallbackShouldReplaceTheDefaultRow()
    {
        var options = new BitChartOptions
        {
            Plugins = { Tooltip = { Callbacks = { Label = i => $"<{i.Value}>" } } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(7), options));

        Assert.AreEqual("<7>", scene.Elements[0].Tooltip.Items[0].Text);
    }

    [TestMethod]
    public void TooltipValuesShouldBeFormattedWithTheConfiguredCulture()
    {
        var options = new BitChartOptions { Culture = new CultureInfo("de-DE") };
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1.5), options));

        StringAssert.Contains(scene.Elements[0].Tooltip.Items[0].Text, "1,5");
    }

    // ---- horizontal (index axis = y) layout ----

    [TestMethod]
    public void HorizontalChartShouldReserveTheLeftEdgeForItsCategoryLabels()
    {
        BitChartArea Plot(params string[] labels)
        {
            var data = new BitChartData
            {
                Labels = labels.ToList(),
                Datasets = { new BitChartDataset { Data = labels.Select(_ => (double?)10).ToList() } }
            };
            var options = new BitChartOptions { IndexAxis = BitChartIndexAxis.Y };
            return Render(new BitChartConfig(BitChartType.Bar, data, options)).PlotArea!.Value;
        }

        double narrow = Plot("A", "B", "C").Left;
        double wide = Plot("A very long category name", "B", "C").Left;

        Assert.IsTrue(wide > narrow + 40,
            $"the categories run down the left edge, so their width is what must be reserved ({wide} vs {narrow})");
    }

    [TestMethod]
    public void HorizontalChartShouldReserveTheBottomForItsValueLabels()
    {
        var options = new BitChartOptions { IndexAxis = BitChartIndexAxis.Y };
        var plot = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3), options)).PlotArea!.Value;

        Assert.IsTrue(plot.Bottom < 300 - 10, $"the value axis under the plot needs room: bottom was {plot.Bottom}");
        Assert.IsTrue(plot.Right > 580, $"nothing is drawn on the right, so nothing should be reserved: right was {plot.Right}");
    }

    [TestMethod]
    public void EveryHorizontalBarLabelShouldStayInsideTheChart()
    {
        var data = new BitChartData
        {
            Labels = { "Netherlands", "Switzerland", "New Zealand" },
            Datasets = { new BitChartDataset { Data = { 10, 20, 30 } } }
        };
        var options = new BitChartOptions { IndexAxis = BitChartIndexAxis.Y };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data, options));

        foreach (var text in scene.Background.OfType<BitChartSvgText>().Where(t => t.Anchor == "end"))
        {
            double left = text.X - BitChartTextMeasure.Width(text.Text, text.FontSize);
            Assert.IsTrue(left >= -0.5, $"'{text.Text}' starts at {left}, outside the chart");
        }
    }

    // ---- stack grouping ----

    [TestMethod]
    public void BarsAndLinesOnOneStackedAxisShouldNotShareAStack()
    {
        var data = new BitChartData
        {
            Labels = { "A" },
            Datasets =
            {
                new BitChartDataset { Data = { 10 }, Type = BitChartType.Bar },
                new BitChartDataset { Data = { 10 }, Type = BitChartType.Bar },
                new BitChartDataset { Data = { 10 }, Type = BitChartType.Line }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data, new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Stacked = true } }
        }));

        // The two bars stack to 20; the line stacks on its own. The axis must fit 20, not 30.
        Assert.AreEqual(20, scene.DataRanges["y"].Max, 1e-6);
    }

    // ---- element defaults ----

    [TestMethod]
    public void PointRadiusShouldFallBackToTheElementDefault()
    {
        var options = new BitChartOptions { Elements = { PointRadius = 9 } };
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        Assert.AreEqual(9, ((BitChartSvgCircle)scene.Elements[0].Shape).R, 1e-9);
    }

    [TestMethod]
    public void AnExplicitZeroPointRadiusShouldStillWinOverTheElementDefault()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1, 2 }, PointRadius = 0 } }
        };
        var options = new BitChartOptions { Elements = { PointRadius = 9 } };
        var scene = Render(new BitChartConfig(BitChartType.Line, data, options));

        // The marker is gone, but an invisible hit target keeps the point hoverable.
        var shape = (BitChartSvgCircle)scene.Elements[0].Shape;
        Assert.AreEqual("transparent", shape.Fill);
    }

    [TestMethod]
    public void TensionShouldFallBackToTheElementDefault()
    {
        string Path(BitChartOptions options)
            => ((BitChartSvgPath)Render(new BitChartConfig(BitChartType.Line, Bars(1, 5, 2), options))
                .Series.First(n => n is BitChartSvgPath { Stroke: not null })).D;

        string straight = Path(new BitChartOptions());
        string curved = Path(new BitChartOptions { Elements = { LineTension = 0.5 } });

        Assert.IsFalse(straight.Contains('C'), straight);
        Assert.IsTrue(curved.Contains('C'), curved);
    }

    // ---- legend position ----

    [TestMethod]
    public void ALegendPositionWithNowhereToGoShouldFallBackToTheTop()
    {
        var options = new BitChartOptions { Plugins = { Legend = { Position = BitChartPosition.Center } } };
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3), options));

        Assert.AreEqual(BitChartPosition.Top, scene.Legend!.Position,
            "the legend renders on one of four sides; anything else would silently disappear");
    }

    [TestMethod]
    public void ReversedAxesShouldBeReportedOnTheScene()
    {
        var options = new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Reverse = true } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        Assert.IsTrue(scene.ReversedAxes.Contains("y"));
        Assert.IsFalse(scene.ReversedAxes.Contains("x"));
    }

    // ---- radial label space ----

    [TestMethod]
    public void RadarShouldShrinkItsWebToFitLongPointLabels()
    {
        double Radius(params string[] labels)
        {
            var data = new BitChartData
            {
                Labels = labels.ToList(),
                Datasets = { new BitChartDataset { Data = labels.Select(_ => (double?)10).ToList() } }
            };
            var scene = Render(new BitChartConfig(BitChartType.Radar, data));
            // The outermost grid ring is the web's radius.
            var poly = scene.Background.OfType<BitChartSvgPolygon>().Last();
            return poly.Points.Max(pt => Math.Abs(pt.X - 300));
        }

        // Index 1 of a four-spoke web points straight to the right, where a long label reaches out
        // sideways by its whole width - the case that actually costs radius.
        double shortLabels = Radius("A", "B", "C", "D");
        double longLabels = Radius("A", "Operational excellence", "C", "D");

        Assert.IsTrue(longLabels < shortLabels - 20,
            $"a long category name has to be measured, not assumed ({longLabels} vs {shortLabels})");
    }

    [TestMethod]
    public void RadarPointLabelsShouldStayInsideTheChart()
    {
        var data = new BitChartData
        {
            Labels = { "Operational excellence", "Speed", "Cost", "Reliability" },
            Datasets = { new BitChartDataset { Data = { 10, 20, 30, 40 } } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Radar, data));

        foreach (var text in scene.Background.OfType<BitChartSvgText>())
        {
            double w = BitChartTextMeasure.Width(text.Text, text.FontSize, text.FontWeight);
            double left = text.Anchor switch { "end" => text.X - w, "middle" => text.X - w / 2, _ => text.X };
            Assert.IsTrue(left >= -1, $"'{text.Text}' starts at {left}");
            Assert.IsTrue(left + w <= 601, $"'{text.Text}' ends at {left + w}");
        }
    }

    [TestMethod]
    public void PolarAreaShouldAlsoMeasureItsPerimeterLabels()
    {
        double Radius(params string[] labels)
        {
            var data = new BitChartData
            {
                Labels = labels.ToList(),
                Datasets = { new BitChartDataset { Data = labels.Select(_ => (double?)10).ToList() } }
            };
            var options = new BitChartOptions
            {
                Scales = { ["r"] = new BitChartScaleOptions { Id = "r", Type = BitChartScaleType.RadialLinear } }
            };
            var scene = Render(new BitChartConfig(BitChartType.PolarArea, data, options));
            return scene.Background.OfType<BitChartSvgCircle>().Max(c => c.R);
        }

        Assert.IsTrue(Radius("A distinctly long slice name", "B", "C") < Radius("A", "B", "C") - 10);
    }

    // ---- axes at the zero line ----

    [TestMethod]
    public void ACenteredAxisShouldBeDrawnAtTheOtherAxisZero()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Points = [new(-10, -10), new(10, 10)] } }
        };
        var options = new BitChartOptions
        {
            Scales =
            {
                ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Linear, Position = BitChartPosition.Center },
                ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Position = BitChartPosition.Center }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Scatter, data, options));
        var plot = scene.PlotArea!.Value;

        // Both axis lines run through the middle of a symmetric plot rather than along its edges.
        var lines = scene.Background.OfType<BitChartSvgLine>().ToList();
        Assert.IsTrue(lines.Any(l => Math.Abs(l.X1 - l.X2) < 0.01 && Math.Abs(l.X1 - plot.CenterX) < plot.Width * 0.1),
            "expected a vertical axis line near the horizontal center");
        Assert.IsTrue(lines.Any(l => Math.Abs(l.Y1 - l.Y2) < 0.01 && Math.Abs(l.Y1 - plot.CenterY) < plot.Height * 0.1),
            "expected a horizontal axis line near the vertical center");
    }

    [TestMethod]
    public void ACenteredAxisShouldReserveNoLayoutSpace()
    {
        BitChartArea Plot(BitChartPosition position)
        {
            var options = new BitChartOptions
            {
                Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Position = position } }
            };
            return Render(new BitChartConfig(BitChartType.Line, Bars(1000, 2000, 3000), options)).PlotArea!.Value;
        }

        Assert.IsTrue(Plot(BitChartPosition.Center).Left < Plot(BitChartPosition.Left).Left,
            "a centered axis lives inside the plot, so it must not push the plot inwards");
    }

    // ---- reversed value axis ----

    [TestMethod]
    public void BarsOnAReversedAxisShouldStillGrowAwayFromTheirBaseline()
    {
        BitChartSvgRect Bar(bool reverse)
        {
            var options = new BitChartOptions
            {
                Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Reverse = reverse, Min = 0, Max = 100 } }
            };
            var data = new BitChartData
            {
                Labels = { "A" },
                Datasets = { new BitChartDataset { Data = { 60 } } }
            };
            return (BitChartSvgRect)Render(new BitChartConfig(BitChartType.Bar, data, options)).Elements[0].Shape;
        }

        var normal = Bar(false);
        var reversed = Bar(true);

        // Normally the bar hangs down to the axis at the bottom; reversed, zero is at the top and the
        // bar hangs down from it. Either way it starts at the baseline and is the same length.
        Assert.AreEqual(normal.Height, reversed.Height, 0.5);
        Assert.IsTrue(reversed.Y < normal.Y, $"reversed bar top {reversed.Y}, normal {normal.Y}");
    }

    [TestMethod]
    public void ABarOnAReversedAxisShouldSkipTheEdgeTouchingItsBaseline()
    {
        string Corners(bool reverse)
        {
            var options = new BitChartOptions
            {
                Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Reverse = reverse, Min = 0, Max = 100 } }
            };
            var data = new BitChartData
            {
                Labels = { "A" },
                Datasets = { new BitChartDataset { Data = { 60 }, BorderRadius = 8 } }
            };
            return ((BitChartSvgPath)Render(new BitChartConfig(BitChartType.Bar, data, options)).Elements[0].Shape).D;
        }

        Assert.AreNotEqual(Corners(false), Corners(true),
            "the rounded end follows the tip of the bar, which a reversed axis moves to the other side");
    }

    [TestMethod]
    public void NegativeBarsShouldPointTheirTooltipAtTheirOwnTip()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 10, -10 } } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        var positive = (BitChartSvgRect)scene.Elements[0].Shape;
        var negative = (BitChartSvgRect)scene.Elements[1].Shape;
        Assert.AreEqual(positive.Y, scene.Elements[0].Tooltip.AnchorY, 0.01);
        Assert.AreEqual(negative.Y + negative.Height, scene.Elements[1].Tooltip.AnchorY, 0.01);
    }

    [TestMethod]
    public void ADoughnutCutoutOfOneHundredPercentShouldNotInvertItsArcs()
    {
        var options = new BitChartOptions { CutoutPercentage = 100 };
        var scene = Render(new BitChartConfig(BitChartType.Doughnut, Bars(1, 2, 3), options));

        Assert.AreEqual(3, scene.Elements.Count);
        foreach (var el in scene.Elements)
            Assert.IsFalse(((BitChartSvgPath)el.Shape).D.Contains("NaN"));
    }

    [TestMethod]
    public void AChartOfMalformedColorsShouldStillRender()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 1, 2 }, BackgroundColor = "#nothex", BorderColor = "not a color", Fill = BitChartFillMode.Origin }
            }
        };

        var scene = Render(new BitChartConfig(BitChartType.Line, data));

        Assert.AreEqual(2, scene.Elements.Count);
    }

    [TestMethod]
    public void DataLabelsShouldStayInsideTheChartBox()
    {
        var options = new BitChartOptions
        {
            Plugins = { DataLabels = { Display = true, Anchor = BitChartAlign.End, Align = BitChartAlign.End, Offset = 40 } },
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Display = false } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(100, 100, 100), options));

        foreach (var t in scene.Foreground.OfType<BitChartSvgText>())
            Assert.IsTrue(t.Y >= 0 && t.Y <= 300, $"label at y={t.Y} is outside the 300px chart");
    }

    [TestMethod]
    public void ARadarChartShouldReadTheRadialScaleItsDatasetsName()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 1, 2, 3 }, RAxisID = "radial" } }
        };
        var options = new BitChartOptions
        {
            Scales =
            {
                ["radial"] = new BitChartScaleOptions
                {
                    Id = "radial", Type = BitChartScaleType.RadialLinear, Grid = { Circular = true }
                }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Radar, data, options));

        // Circular rings only appear when the named scale is the one actually being read.
        Assert.IsTrue(scene.Background.OfType<BitChartSvgCircle>().Any(),
            "the scale named by RAxisID must be the one the chart uses");
    }
}
