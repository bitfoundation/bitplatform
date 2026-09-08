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

    // ---- percentage stacking ----

    private static BitChartConfig PercentStack(params double?[][] series)
    {
        var data = new BitChartData { Labels = { "A", "B", "C" } };
        foreach (var s in series)
            data.Datasets.Add(new BitChartDataset { Data = s.ToList(), Stack = "s" });
        var options = new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Stacked = true, Stacked100 = true } }
        };
        return new BitChartConfig(BitChartType.Bar, data, options);
    }

    [TestMethod]
    public void APercentageStackOfPositivesShouldSpanZeroToAHundred()
    {
        var scene = Render(PercentStack([1, 2, 3], [3, 2, 1]));

        var range = scene.AxisRanges["y"];
        Assert.AreEqual(0, range.Min, 1e-6);
        Assert.AreEqual(100, range.Max, 1e-6);
    }

    [TestMethod]
    public void APercentageStackWithNegativesShouldMakeRoomBelowTheBaseline()
    {
        // Every category is 50% up and 50% down, so the axis has to reach -50 for the bars to be drawn.
        var scene = Render(PercentStack([10, 10, 10], [-10, -10, -10]));

        var range = scene.AxisRanges["y"];
        Assert.IsTrue(range.Min <= -50, $"a negative share must stay inside the axis, but it stops at {range.Min}");
        Assert.IsTrue(range.Max >= 50, $"the positive share must stay inside the axis, but it stops at {range.Max}");
    }

    [TestMethod]
    public void EveryPercentageStackedBarShouldBeDrawnInsideThePlot()
    {
        var scene = Render(PercentStack([10, 10, 10], [-10, -10, -10]));
        var plot = scene.PlotArea!.Value;

        Assert.AreEqual(6, scene.Elements.Count);
        foreach (var el in scene.Elements)
        {
            var r = (BitChartSvgRect)el.Shape;
            Assert.IsTrue(r.Y >= plot.Top - 0.5 && r.Y + r.Height <= plot.Bottom + 0.5,
                $"a bar spanning {r.Y}..{r.Y + r.Height} is clipped out of the plot {plot.Top}..{plot.Bottom}");
        }
    }

    // ---- tick label rotation ----

    [TestMethod]
    public void MinRotationShouldSlantLabelsThatWouldHaveFitAnyway()
    {
        var options = new BitChartOptions();
        options.Scales["x"] = new BitChartScaleOptions
        {
            Id = "x", Type = BitChartScaleType.Category, Ticks = { MinRotation = 30 }
        };

        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3), options));

        // Short labels over a wide axis fit horizontally, but MinRotation is an instruction, not a fallback.
        Assert.IsTrue(scene.Background.OfType<BitChartSvgText>().Any(t => Math.Abs(t.Rotation - 30) < 1e-6),
            "MinRotation must be applied even when the labels would have fitted unrotated");
    }

    [TestMethod]
    public void LabelsThatFitShouldStayHorizontalWithoutAMinRotation()
    {
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3)));

        Assert.IsTrue(scene.Background.OfType<BitChartSvgText>().All(t => Math.Abs(t.Rotation) < 1e-6));
    }

    // ---- polar area ----

    [TestMethod]
    public void HidingThePolarAreaDatasetShouldEmptyTheChart()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets = { new BitChartDataset { Data = { 1, 2, 3 }, Hidden = true } }
        };

        var scene = Render(new BitChartConfig(BitChartType.PolarArea, data));

        Assert.AreEqual(0, scene.Elements.Count, "a hidden dataset must not still be drawn");
        Assert.IsTrue(scene.IsEmpty);
    }

    [TestMethod]
    public void PolarAreaShouldDrawTheFirstVisibleDataset()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C" },
            Datasets =
            {
                new BitChartDataset { Data = { 1, 2, 3 }, Hidden = true },
                new BitChartDataset { Data = { 4, 5, 6 } }
            }
        };

        var scene = Render(new BitChartConfig(BitChartType.PolarArea, data));

        Assert.AreEqual(3, scene.Elements.Count);
        Assert.IsTrue(scene.Elements.All(e => e.DatasetIndex == 1),
            "the visible dataset is the one that should be drawn, not the hidden first one");
    }

    [TestMethod]
    public void APolarWedgeBelowTheScaleMinimumShouldNotInvertThroughTheCenter()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 5, 10 } } }
        };
        var options = new BitChartOptions
        {
            Scales = { ["r"] = new BitChartScaleOptions { Id = "r", Type = BitChartScaleType.RadialLinear, Min = 8 } }
        };

        var scene = Render(new BitChartConfig(BitChartType.PolarArea, data, options));

        // The 5 sits below the axis minimum: it collapses to nothing rather than drawing a wedge
        // through the far side of the center.
        Assert.AreEqual(2, scene.Elements.Count);
        foreach (var el in scene.Elements)
            Assert.IsFalse(((BitChartSvgPath)el.Shape).D.Contains('-'),
                "no arc coordinate should come out negative from a clamped radius");
    }

    // ---- plugins on radial charts ----

    private sealed class ProbePlugin : IBitChartPlugin
    {
        public string Id => "probe";
        public int Before, After;
        public bool SawCartesian;
        public double OuterRadius;
        public void BeforeDatasetsDraw(BitChartPluginContext ctx) { Before++; SawCartesian = ctx.IsCartesian; OuterRadius = ctx.OuterRadius; }
        public void AfterDatasetsDraw(BitChartPluginContext ctx) => After++;
    }

    [TestMethod]
    [DataRow(BitChartType.Radar)]
    [DataRow(BitChartType.PolarArea)]
    [DataRow(BitChartType.Doughnut)]
    public void PluginsShouldRunOnEveryRadialChartType(BitChartType type)
    {
        var probe = new ProbePlugin();
        var options = new BitChartOptions();
        options.Plugins.Custom.Add(probe);

        Render(new BitChartConfig(type, Bars(1, 2, 3), options));

        Assert.AreEqual(1, probe.Before, $"{type} must give plugins their before-draw hook");
        Assert.AreEqual(1, probe.After, $"{type} must give plugins their after-draw hook");
        Assert.IsFalse(probe.SawCartesian);
        Assert.IsTrue(probe.OuterRadius > 0, "a radial context has to carry the geometry plugins draw against");
    }

    [TestMethod]
    public void TheCenterTextPluginShouldAlsoWorkOnARadarChart()
    {
        var options = new BitChartOptions();
        options.Plugins.Custom.Add(new BitChartCenterTextPlugin("87", "score"));

        var scene = Render(new BitChartConfig(BitChartType.Radar, Bars(1, 2, 3), options));

        var texts = scene.Foreground.OfType<BitChartSvgText>().Select(t => t.Text).ToList();
        CollectionAssert.Contains(texts, "87");
        CollectionAssert.Contains(texts, "score");
    }

    // ---- arcs: corner radius and ring weight ----

    [TestMethod]
    public void ArcBorderRadiusShouldRoundTheArcInsteadOfLeavingItSquare()
    {
        var square = Render(new BitChartConfig(BitChartType.Doughnut, Bars(1, 2, 3)));
        var data = Bars(1, 2, 3);
        data.Datasets[0].BorderRadius = 6;
        var rounded = Render(new BitChartConfig(BitChartType.Doughnut, data));

        string plain = ((BitChartSvgPath)square.Elements[0].Shape).D;
        string curved = ((BitChartSvgPath)rounded.Elements[0].Shape).D;

        Assert.IsFalse(plain.Contains('Q'), "a plain arc is made of straight edges and circular arcs");
        Assert.IsTrue(curved.Contains('Q'), "a rounded arc fillets its corners with quadratic curves");
    }

    [TestMethod]
    public void ARoundedPieWedgeShouldStillStartAtTheCenter()
    {
        var data = Bars(1, 2, 3);
        data.Datasets[0].BorderRadius = 40;   // deliberately larger than the wedge can take
        var scene = Render(new BitChartConfig(BitChartType.Pie, data));

        foreach (var el in scene.Elements)
            StringAssert.StartsWith(((BitChartSvgPath)el.Shape).D, "M ",
                "a pie wedge is still drawn from its point outwards");
    }

    [TestMethod]
    public void AFullCircleShouldIgnoreArcRounding()
    {
        var data = new BitChartData { Labels = { "Only" }, Datasets = { new BitChartDataset { Data = { 1 } } } };
        data.Datasets[0].BorderRadius = 8;
        var scene = Render(new BitChartConfig(BitChartType.Doughnut, data));

        // A single slice sweeps the whole circle, so it has no corners to round.
        Assert.IsFalse(((BitChartSvgPath)scene.Elements[0].Shape).D.Contains('Q'));
    }

    [TestMethod]
    public void RingWeightShouldShareTheRadiusOutInProportion()
    {
        // The radial midpoint of a ring is where its band sits, so growing one band moves its own
        // midpoint and, with it, the boundary between the two.
        static double MidRadius(int datasetIndex, double outerWeight, double innerWeight)
        {
            var data = new BitChartData
            {
                Labels = { "A", "B" },
                Datasets =
                {
                    new BitChartDataset { Data = { 1, 1 }, Weight = outerWeight },
                    new BitChartDataset { Data = { 1, 1 }, Weight = innerWeight }
                }
            };
            var scene = Render(new BitChartConfig(BitChartType.Doughnut, data));
            var el = scene.Elements.First(e => e.DatasetIndex == datasetIndex);
            double dx = el.CenterX - scene.Width / 2;
            double dy = el.CenterY - scene.Height / 2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        Assert.IsTrue(MidRadius(0, 2, 1) < MidRadius(0, 1, 1),
            "a heavier outer ring claims radius inwards, so its own midpoint moves towards the center");
        Assert.IsTrue(MidRadius(1, 1, 2) > MidRadius(1, 1, 1),
            "a heavier inner ring claims radius outwards, so its own midpoint moves away from the center");
    }

    [TestMethod]
    public void EqualRingWeightsShouldSplitTheRadiusEvenly()
    {
        static double MidRadius(BitChartScene scene, int datasetIndex)
        {
            var el = scene.Elements.First(e => e.DatasetIndex == datasetIndex);
            double dx = el.CenterX - scene.Width / 2;
            double dy = el.CenterY - scene.Height / 2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        var two = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1, 1 } }, new BitChartDataset { Data = { 1, 1 } } }
        };
        var one = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets = { new BitChartDataset { Data = { 1, 1 } } }
        };

        var split = Render(new BitChartConfig(BitChartType.Doughnut, two));
        var whole = Render(new BitChartConfig(BitChartType.Doughnut, one));

        // Default weight is 1 everywhere, so the two bands must straddle the single ring's midpoint.
        double outer = MidRadius(split, 0), inner = MidRadius(split, 1);
        Assert.AreEqual(MidRadius(whole, 0), (outer + inner) / 2, 0.01,
            "equal weights have to keep the even split the chart had before weights existed");
    }

    // ---- sparkline ----

    [TestMethod]
    public void ASparklineShouldDropEveryPieceOfChrome()
    {
        var options = new BitChartOptions
        {
            Sparkline = true,
            Plugins = new BitChartPluginOptions
            {
                Title = new BitChartTitleOptions { Display = true, Text = "Trend" },
                Legend = new BitChartLegendOptions { Display = true }
            }
        };

        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));

        Assert.IsNull(scene.Title, "a sparkline carries no title");
        Assert.IsNull(scene.Legend, "a sparkline carries no legend");
        Assert.AreEqual(0, scene.Background.Count, "a sparkline draws no axis, grid or tick label");
    }

    [TestMethod]
    public void ASparklineShouldGiveTheWholeBoxToTheSeries()
    {
        var plain = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3)));
        var spark = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), new BitChartOptions { Sparkline = true }));

        Assert.IsTrue(spark.PlotArea!.Value.Width > plain.PlotArea!.Value.Width);
        Assert.IsTrue(spark.PlotArea!.Value.Height > plain.PlotArea!.Value.Height);
    }

    [TestMethod]
    public void ASparklineShouldStillBeInteractiveAndDescribable()
    {
        var scene = Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), new BitChartOptions { Sparkline = true }));

        Assert.AreEqual(3, scene.Elements.Count, "dropping the chrome must not drop the data");
        Assert.IsTrue(scene.HitBands.Count > 0, "the plot stays hoverable");
    }

    [TestMethod]
    public void ASparklineOnARadarChartShouldDropItsPerimeterLabels()
    {
        var spark = Render(new BitChartConfig(BitChartType.Radar, Bars(1, 2, 3), new BitChartOptions { Sparkline = true }));

        Assert.IsFalse(spark.Background.OfType<BitChartSvgText>().Any(),
            "a sparkline radar draws no point labels or radial ticks");
    }

    // ---- trendlines ----

    private static BitChartScene Trend(BitChartTrendline trendline, BitChartType type = BitChartType.Line,
        BitChartData? data = null)
    {
        var options = new BitChartOptions();
        options.Plugins.Custom.Add(new BitChartTrendlinePlugin(trendline));
        return Render(new BitChartConfig(type, data ?? Bars(1, 2, 3), options));
    }

    [TestMethod]
    public void ALinearTrendlineShouldFollowTheSlopeOfItsDataset()
    {
        var rising = Trend(new BitChartTrendline { DatasetIndex = 0 }, data: Bars(1, 2, 3));
        var falling = Trend(new BitChartTrendline { DatasetIndex = 0 }, data: Bars(3, 2, 1));

        // A rising series maps to a falling pixel path (y grows downwards), and vice versa.
        Assert.IsTrue(EndY(rising) < StartY(rising), "a rising series must give a rising trend line");
        Assert.IsTrue(EndY(falling) > StartY(falling), "a falling series must give a falling trend line");
    }

    private static BitChartSvgPath TrendPath(BitChartScene scene)
        => scene.Foreground.OfType<BitChartSvgPath>().Last();

    private static double StartY(BitChartScene scene) => PathPoints(TrendPath(scene))[0].Y;
    private static double EndY(BitChartScene scene) => PathPoints(TrendPath(scene))[^1].Y;

    private static List<(double X, double Y)> PathPoints(BitChartSvgPath path)
    {
        var points = new List<(double, double)>();
        foreach (var token in path.D.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                                   .Where(t => t is not ("M" or "L")).Chunk(2))
            points.Add((double.Parse(token[0], CultureInfo.InvariantCulture),
                        double.Parse(token[1], CultureInfo.InvariantCulture)));
        return points;
    }

    [TestMethod]
    public void AnExtendedTrendlineShouldReachBothEdgesOfThePlot()
    {
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0, Extend = true });
        var plot = scene.PlotArea!.Value;
        var points = PathPoints(TrendPath(scene));

        Assert.AreEqual(plot.Left, points[0].X, 0.01);
        Assert.AreEqual(plot.Right, points[^1].X, 0.01);
    }

    [TestMethod]
    public void AnUnextendedTrendlineShouldStopAtTheData()
    {
        // Bars center their categories, so the first and last of them sit inside the plot - which is
        // exactly where an unextended fit has to begin and end.
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0 }, BitChartType.Bar);
        var plot = scene.PlotArea!.Value;
        var points = PathPoints(TrendPath(scene));

        Assert.AreEqual(scene.Elements.First(e => e.DataIndex == 0).CenterX, points[0].X, 0.5);
        Assert.AreEqual(scene.Elements.First(e => e.DataIndex == 2).CenterX, points[^1].X, 0.5);
        Assert.IsTrue(points[0].X > plot.Left, "the fit starts at the first point, not the edge");
        Assert.IsTrue(points[^1].X < plot.Right, "the fit ends at the last point, not the edge");
    }

    [TestMethod]
    public void AMovingAverageShouldHaveOneVertexPerDataPoint()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B", "C", "D", "E" },
            Datasets = { new BitChartDataset { Data = { 10, 0, 10, 0, 10 } } }
        };
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0, Kind = BitChartTrendlineKind.MovingAverage, Period = 2 },
            data: data);

        Assert.AreEqual(5, PathPoints(TrendPath(scene)).Count);
    }

    [TestMethod]
    public void AnAverageTrendlineShouldBeFlat()
    {
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0, Kind = BitChartTrendlineKind.Average },
            data: Bars(1, 5, 9));
        var points = PathPoints(TrendPath(scene));

        Assert.AreEqual(points[0].Y, points[^1].Y, 0.01, "the mean does not slope");
    }

    [TestMethod]
    public void ATrendlineShouldSitOnTheValuesItWasFittedTo()
    {
        // The mean of 1, 5, 9 is 5, which is exactly where the middle point of the series is drawn.
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0, Kind = BitChartTrendlineKind.Average },
            data: Bars(1, 5, 9));

        double middle = scene.Elements.Single(e => e.DataIndex == 1).CenterY;
        Assert.AreEqual(middle, PathPoints(TrendPath(scene))[0].Y, 0.5);
    }

    [TestMethod]
    public void ATrendlineOverBarsShouldLandOnTheBarCenters()
    {
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0, Kind = BitChartTrendlineKind.MovingAverage, Period = 1 },
            BitChartType.Bar);
        var points = PathPoints(TrendPath(scene));

        // Bars center their category in its band, so the fit has to be placed the same way.
        for (int i = 0; i < scene.Elements.Count; i++)
            Assert.AreEqual(scene.Elements[i].CenterX, points[i].X, 0.5,
                "a trend line over bars must follow the same band placement the bars use");
    }

    [TestMethod]
    public void AHiddenDatasetShouldTakeItsTrendlineWithIt()
    {
        var data = Bars(1, 2, 3);
        data.Datasets[0].Hidden = true;
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0 }, data: data);

        Assert.IsFalse(scene.Foreground.OfType<BitChartSvgPath>().Any());
    }

    [TestMethod]
    public void ATrendlineNamingADatasetThatIsNotThereShouldDrawNothing()
    {
        var scene = Trend(new BitChartTrendline { DatasetIndex = 7 });

        Assert.IsFalse(scene.Foreground.OfType<BitChartSvgPath>().Any());
    }

    [TestMethod]
    public void ATrendlineLabelShouldBeDrawnInAPillInsideThePlot()
    {
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0, Label = "trend" });
        var plot = scene.PlotArea!.Value;

        var pill = scene.Foreground.OfType<BitChartSvgRect>().Last();
        Assert.IsTrue(pill.X >= plot.Left - 0.01 && pill.X + pill.Width <= plot.Right + 0.01);
        Assert.IsTrue(pill.Y >= plot.Top - 0.01 && pill.Y + pill.Height <= plot.Bottom + 0.01);
        CollectionAssert.Contains(scene.Foreground.OfType<BitChartSvgText>().Select(t => t.Text).ToList(), "trend");
    }

    [TestMethod]
    public void ATrendlineShouldOnlyDrawOnCartesianCharts()
    {
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0 }, BitChartType.Doughnut);

        Assert.IsFalse(scene.Foreground.OfType<BitChartSvgPath>().Any(),
            "there is no plot to fit a line across on a circular chart");
    }

    [TestMethod]
    public void ATrendlineShouldBeAbleToDrawUnderTheDatasets()
    {
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0, DrawBehindDatasets = true });

        Assert.IsFalse(scene.Foreground.OfType<BitChartSvgPath>().Any());
        Assert.IsTrue(scene.Background.OfType<BitChartSvgPath>().Any());
    }

    [TestMethod]
    public void ATrendlineOfOnePointShouldDrawNothingRatherThanThrow()
    {
        var data = new BitChartData { Labels = { "A" }, Datasets = { new BitChartDataset { Data = { 5 } } } };
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0 }, data: data);

        Assert.IsFalse(scene.Foreground.OfType<BitChartSvgPath>().Any());
    }

    [TestMethod]
    public void ATrendlineOverAFlatSeriesShouldStillBeDrawn()
    {
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0 }, data: Bars(4, 4, 4));
        var points = PathPoints(TrendPath(scene));

        Assert.AreEqual(points[0].Y, points[^1].Y, 0.01);
    }

    [TestMethod]
    public void ATrendlineOverScatterPointsShouldUseTheirOwnXValues()
    {
        var data = new BitChartData
        {
            Datasets =
            {
                new BitChartDataset
                {
                    Points =
                    [
                        new BitChartDataPoint(0, 1),
                        new BitChartDataPoint(5, 3),
                        new BitChartDataPoint(10, 5)
                    ]
                }
            }
        };
        var scene = Trend(new BitChartTrendline { DatasetIndex = 0 }, BitChartType.Scatter, data);
        var points = PathPoints(TrendPath(scene));

        // Perfectly collinear points: the fit has to pass through the first and last of them.
        Assert.AreEqual(scene.Elements.First(e => e.DataIndex == 0).CenterY, points[0].Y, 0.5);
        Assert.AreEqual(scene.Elements.First(e => e.DataIndex == 2).CenterY, points[^1].Y, 0.5);
    }

    // ---- error bars ----

    private static BitChartData WithErrors(BitChartType _, params BitChartErrorBar?[] errors)
    {
        var data = Bars(10, 20, 30);
        data.Datasets[0].ErrorData = errors.ToList();
        return data;
    }

    [TestMethod]
    [DataRow(BitChartType.Bar)]
    [DataRow(BitChartType.Line)]
    public void AnErrorBarShouldSpanItsIntervalWithACapAtEachEnd(BitChartType type)
    {
        var scene = Render(new BitChartConfig(type, WithErrors(type, 5, 5, 5)));

        // One whisker plus two caps per point, on top of anything else the chart puts in the foreground.
        var lines = scene.Foreground.OfType<BitChartSvgLine>().ToList();
        Assert.AreEqual(9, lines.Count);
    }

    [TestMethod]
    public void AnErrorBarShouldReachTheValuesItWasGiven()
    {
        var options = new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Min = 0, Max = 40 } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, WithErrors(BitChartType.Bar, null, 5, null), options));
        var plot = scene.PlotArea!.Value;

        // The whisker is the tall line; its ends must land where 15 and 25 do on the axis.
        var whisker = scene.Foreground.OfType<BitChartSvgLine>()
            .OrderByDescending(l => Math.Abs(l.Y2 - l.Y1)).First();
        double Pixel(double v) => plot.Bottom - (v - 0) / 40 * plot.Height;

        Assert.AreEqual(Pixel(15), Math.Max(whisker.Y1, whisker.Y2), 0.5);
        Assert.AreEqual(Pixel(25), Math.Min(whisker.Y1, whisker.Y2), 0.5);
    }

    [TestMethod]
    public void AnAsymmetricErrorBarShouldUseBothArms()
    {
        var options = new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Min = 0, Max = 40 } }
        };
        var data = WithErrors(BitChartType.Bar, null, new BitChartErrorBar(2, 8), null);
        var scene = Render(new BitChartConfig(BitChartType.Bar, data, options));
        var plot = scene.PlotArea!.Value;

        var whisker = scene.Foreground.OfType<BitChartSvgLine>()
            .OrderByDescending(l => Math.Abs(l.Y2 - l.Y1)).First();
        double Pixel(double v) => plot.Bottom - v / 40 * plot.Height;

        Assert.AreEqual(Pixel(18), Math.Max(whisker.Y1, whisker.Y2), 0.5);
        Assert.AreEqual(Pixel(28), Math.Min(whisker.Y1, whisker.Y2), 0.5);
    }

    [TestMethod]
    public void ANullErrorEntryShouldLeaveThatPointWithoutAWhisker()
    {
        var scene = Render(new BitChartConfig(BitChartType.Bar, WithErrors(BitChartType.Bar, 3, null, 3)));

        Assert.AreEqual(6, scene.Foreground.OfType<BitChartSvgLine>().Count());
    }

    [TestMethod]
    public void AZeroCapWidthShouldDrawABareWhisker()
    {
        var data = WithErrors(BitChartType.Bar, 5, 5, 5);
        data.Datasets[0].ErrorBarCapWidth = 0;
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        Assert.AreEqual(3, scene.Foreground.OfType<BitChartSvgLine>().Count());
    }

    [TestMethod]
    public void HorizontalBarsShouldLayTheirErrorBarsAlongTheValueAxis()
    {
        var options = new BitChartOptions { IndexAxis = BitChartIndexAxis.Y };
        var scene = Render(new BitChartConfig(BitChartType.Bar, WithErrors(BitChartType.Bar, 5, 5, 5), options));

        var whiskers = scene.Foreground.OfType<BitChartSvgLine>()
            .Where(l => Math.Abs(l.X2 - l.X1) > Math.Abs(l.Y2 - l.Y1)).ToList();
        Assert.AreEqual(3, whiskers.Count, "the whisker follows the value axis, which now runs across the plot");
    }

    [TestMethod]
    public void AnErrorBarShouldBeNamedInTheTooltip()
    {
        var symmetric = Render(new BitChartConfig(BitChartType.Bar, WithErrors(BitChartType.Bar, 5, 5, 5)));
        StringAssert.Contains(symmetric.Elements[0].Tooltip.Items[0].Text, "±5");

        var data = WithErrors(BitChartType.Bar, new BitChartErrorBar(1, 3), null, null);
        var asymmetric = Render(new BitChartConfig(BitChartType.Bar, data));
        StringAssert.Contains(asymmetric.Elements[0].Tooltip.Items[0].Text, "+3/-1");
    }

    [TestMethod]
    public void ADatasetWithoutErrorDataShouldReadExactlyAsBefore()
    {
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(10, 20, 30)));

        Assert.IsFalse(scene.Elements[0].Tooltip.Items[0].Text.Contains('±'));
        Assert.AreEqual(0, scene.Foreground.OfType<BitChartSvgLine>().Count());
    }

    [TestMethod]
    public void ErrorBarsShouldFollowTheirOwnBarInAGroup()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 10, 10 }, ErrorData = [1, 1] },
                new BitChartDataset { Data = { 10, 10 }, ErrorData = [1, 1] }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Bar, data));

        // Each whisker sits over the bar it belongs to, not over the shared category center.
        var whiskerCenters = scene.Foreground.OfType<BitChartSvgLine>()
            .Where(l => Math.Abs(l.Y2 - l.Y1) > Math.Abs(l.X2 - l.X1))
            .Select(l => Math.Round(l.X1, 3)).OrderBy(x => x).ToList();
        var barCenters = scene.Elements.Select(e => Math.Round(e.CenterX, 3)).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(barCenters, whiskerCenters);
    }

    // ---- ellipse and polygon annotations ----

    private static BitChartScene Annotated(BitChartAnnotation annotation)
    {
        var options = new BitChartOptions();
        options.Plugins.Custom.Add(new BitChartAnnotationPlugin(annotation));
        return Render(new BitChartConfig(BitChartType.Line, Bars(1, 2, 3), options));
    }

    [TestMethod]
    public void AnEllipseAnnotationShouldBeInscribedInItsBounds()
    {
        var scene = Annotated(new BitChartAnnotation
        {
            Kind = BitChartAnnotationKind.Ellipse,
            XMin = 0, XMax = 2, YMin = 1, YMax = 3, XIsIndex = true
        });

        var path = scene.Foreground.OfType<BitChartSvgPath>().Single();
        StringAssert.Contains(path.D, "A ", "an ellipse is drawn from arcs");
        Assert.IsTrue(path.D.EndsWith("Z"), "and it closes");
    }

    [TestMethod]
    public void AnEllipseWithNoExtentShouldDrawNothing()
    {
        var scene = Annotated(new BitChartAnnotation
        {
            Kind = BitChartAnnotationKind.Ellipse,
            XMin = 1, XMax = 1, YMin = 2, YMax = 2, XIsIndex = true
        });

        Assert.AreEqual(0, scene.Foreground.OfType<BitChartSvgPath>().Count());
    }

    [TestMethod]
    [DataRow(3)]
    [DataRow(6)]
    public void APolygonAnnotationShouldHaveTheSidesItWasAskedFor(int sides)
    {
        var scene = Annotated(new BitChartAnnotation
        {
            Kind = BitChartAnnotationKind.Polygon,
            Sides = sides, Radius = 15, XMin = 1, Value = 2, XIsIndex = true
        });

        var poly = scene.Foreground.OfType<BitChartSvgPolygon>().Single();
        Assert.AreEqual(sides, poly.Points.Count);
        Assert.IsTrue(poly.Closed);
    }

    [TestMethod]
    public void APolygonAnnotationShouldPointUpwardsByDefault()
    {
        var scene = Annotated(new BitChartAnnotation
        {
            Kind = BitChartAnnotationKind.Polygon,
            Sides = 3, Radius = 15, XMin = 1, Value = 2, XIsIndex = true
        });

        var poly = scene.Foreground.OfType<BitChartSvgPolygon>().Single();
        double top = poly.Points.Min(p => p.Y);
        Assert.AreEqual(1, poly.Points.Count(p => Math.Abs(p.Y - top) < 0.01),
            "a triangle drawn from the top has exactly one vertex up there");
    }

    [TestMethod]
    public void APolygonAnnotationShouldRotate()
    {
        BitChartSvgPolygon Poly(double rotation) => Annotated(new BitChartAnnotation
        {
            Kind = BitChartAnnotationKind.Polygon,
            Sides = 3, Radius = 15, XMin = 1, Value = 2, XIsIndex = true, Rotation = rotation
        }).Foreground.OfType<BitChartSvgPolygon>().Single();

        Assert.AreNotEqual(Math.Round(Poly(0).Points[0].Y, 3), Math.Round(Poly(180).Points[0].Y, 3));
    }

    [TestMethod]
    public void APointAnnotationShouldTakeAnExplicitRadius()
    {
        var scene = Annotated(new BitChartAnnotation
        {
            Kind = BitChartAnnotationKind.Point, XMin = 1, Value = 2, XIsIndex = true, Radius = 9
        });

        Assert.AreEqual(9, scene.Foreground.OfType<BitChartSvgCircle>().Single().R, 1e-6);
    }

    // ---- legend and tooltip sizing ----

    [TestMethod]
    public void TheLegendShouldCarryItsHeightCapThroughToTheScene()
    {
        var options = new BitChartOptions { Plugins = { Legend = { MaxHeight = 60 } } };
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3), options));

        Assert.AreEqual(60, scene.Legend!.MaxHeight);
    }

    // ---- pointer gestures map onto the axes the chart actually drew ----

    [TestMethod]
    public void AVerticalChartShouldReportItsAxesAsItDrewThem()
    {
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3)));

        Assert.AreEqual((true, false), scene.AxisOrientations["x"], "the index axis runs across the plot");
        Assert.AreEqual((false, true), scene.AxisOrientations["y"], "the value axis runs up it, minimum at the bottom");
    }

    [TestMethod]
    public void AHorizontalChartShouldReportTheSwappedAxes()
    {
        var options = new BitChartOptions { IndexAxis = BitChartIndexAxis.Y };
        var scene = Render(new BitChartConfig(BitChartType.Bar, Bars(1, 2, 3), options));

        Assert.AreEqual((false, false), scene.AxisOrientations["x"], "the categories now run down the plot");
        Assert.AreEqual((true, false), scene.AxisOrientations["y"], "and the values run across it");
    }

    [TestMethod]
    public void ASecondaryXAxisShouldBeReportedAsHorizontalToo()
    {
        var data = new BitChartData
        {
            Datasets =
            {
                new BitChartDataset { Points = [new(0, 1), new(1, 2)] },
                new BitChartDataset { Points = [new(0, 3), new(1, 4)], XAxisID = "x2" }
            }
        };
        var scene = Render(new BitChartConfig(BitChartType.Scatter, data));

        Assert.AreEqual((true, false), scene.AxisOrientations["x2"]);
    }

    [TestMethod]
    public void ATimeAxisShouldPrintItsMonthsInTheChartsCulture()
    {
        var data = new BitChartData
        {
            Datasets =
            {
                new BitChartDataset
                {
                    Points =
                    [
                        new BitChartDataPoint(new DateTime(2026, 1, 15).ToOADate(), 1),
                        new BitChartDataPoint(new DateTime(2026, 6, 15).ToOADate(), 2)
                    ]
                }
            }
        };
        BitChartOptions Options(CultureInfo? culture) => new()
        {
            Culture = culture,
            Scales = { ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Time } }
        };

        var french = Render(new BitChartConfig(BitChartType.Line, data, Options(new CultureInfo("fr-FR"))));
        var invariant = Render(new BitChartConfig(BitChartType.Line, data, Options(null)));

        static List<string> Labels(BitChartScene scene) =>
            scene.Background.OfType<BitChartSvgText>().Select(t => t.Text).ToList();

        // The chart already formats its numbers with the culture; its dates have to follow. The axis
        // ticks on month boundaries, so March is the one both renders are certain to carry.
        CollectionAssert.Contains(Labels(french), "mars 2026",
            "a French chart must not print English month names, but drew: " + string.Join(" | ", Labels(french)));
        CollectionAssert.Contains(Labels(invariant), "Mar 2026");
    }

    [TestMethod]
    public void APercentageStackShouldNotDrawErrorBarsItCannotPlace()
    {
        var data = new BitChartData
        {
            Labels = { "A", "B" },
            Datasets =
            {
                new BitChartDataset { Data = { 10, 20 }, ErrorData = [2, 2], Stack = "s" },
                new BitChartDataset { Data = { 30, 40 }, Stack = "s" }
            }
        };
        var options = new BitChartOptions
        {
            Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Stacked = true, Stacked100 = true } }
        };

        var scene = Render(new BitChartConfig(BitChartType.Bar, data, options));

        // The values are rescaled to percentages; an interval still in the original units would land
        // somewhere meaningless, so it is left out rather than drawn wrong.
        Assert.AreEqual(0, scene.Foreground.OfType<BitChartSvgLine>().Count());
    }

    // ---- a numeric index axis reads value datasets too ----

    [TestMethod]
    public void ANumericIndexAxisShouldSpanAValueDatasetsIndexes()
    {
        var data = new BitChartData
        {
            Datasets = { new BitChartDataset { Data = { 5, 6, 7, 8, 9 } } }
        };
        var options = new BitChartOptions
        {
            Scales = { ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Linear } }
        };
        var scene = Render(new BitChartConfig(BitChartType.Line, data, options));

        Assert.AreEqual(0, scene.AxisRanges["x"].Min, 1e-6);
        Assert.AreEqual(4, scene.AxisRanges["x"].Max, 1e-6, "five values span indexes 0 to 4, not 0 to 1");
    }
}
