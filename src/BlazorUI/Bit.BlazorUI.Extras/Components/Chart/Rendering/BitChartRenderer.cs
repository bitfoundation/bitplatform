
namespace Bit.BlazorUI;

/// <summary>
/// Computes a <see cref="BitChartScene"/> (pure SVG primitives + interaction metadata) from a
/// <see cref="BitChartConfig"/>. This is the heart of the native Blazor renderer - no JavaScript or
/// canvas is involved.
/// </summary>
public sealed partial class BitChartRenderer
{
    private readonly BitChartConfig _config;
    private readonly BitChartData _data;
    private readonly BitChartOptions _options;
    private readonly BitChartRenderState _state;
    private readonly double _w;
    private readonly double _h;

    public BitChartRenderer(BitChartConfig config, BitChartRenderState state, double width, double height)
    {
        _config = config;
        _data = config.Data;
        _options = config.Options;
        _state = state;
        _w = width;
        _h = height;
    }

    public BitChartScene Render()
    {
        var scene = new BitChartScene { Width = _w, Height = _h };
        scene.Title = BuildTitle(_options.Plugins.Title);
        scene.Subtitle = BuildTitle(_options.Plugins.Subtitle);
        EnsureScales();

        switch (_config.Type)
        {
            case BitChartType.Pie:
            case BitChartType.Doughnut:
            case BitChartType.PolarArea:
                scene.IsRadialOrCircular = true;
                RenderCircular(scene);
                break;
            case BitChartType.Radar:
                scene.IsRadialOrCircular = true;
                RenderRadar(scene);
                break;
            default:
                RenderCartesian(scene);
                break;
        }

        BuildLegend(scene);
        return scene;
    }

    // ---- shared helpers ----

    private int _gradSeq;

    /// <summary>Registers a gradient on the scene and returns a <c>url(#id)</c> fill reference.</summary>
    private string RegisterGradient(BitChartScene scene, BitChartGradientBase grad)
    {
        string id = $"bcgrad{_gradSeq++}";
        scene.Defs.Add(new BitChartGradientDef(id, grad));
        return $"url(#{id})";
    }

    /// <summary>Registers a pattern on the scene and returns a <c>url(#id)</c> fill reference.</summary>
    private string RegisterPattern(BitChartScene scene, BitChartFillPattern pattern)
    {
        string id = $"bcpat{_gradSeq++}";
        scene.Patterns.Add(new BitChartPatternDef(id, pattern));
        return $"url(#{id})";
    }

    private static double EstimateTextWidth(string? text, double fontSize)
        => BitChartTextMeasure.Width(text, fontSize);

    private BitChartArea ContentArea()
    {
        var p = _options.Layout.Padding;
        return new BitChartArea(p.Left, p.Top, _w - p.Right, _h - p.Bottom);
    }

    /// <summary>Builds a scriptable-options context for a data element.</summary>
    private BitChartScriptableContext Ctx(BitChartDataset ds, int dsIndex, int dataIndex, double? value = null)
    {
        bool active = _state.Active == (dsIndex, dataIndex);
        double? v = value;
        double? vx = null, vr = null;
        if (v is null)
        {
            if (ds.Points is { } pts && dataIndex < pts.Count)
            {
                v = pts[dataIndex].Y; vx = pts[dataIndex].X; vr = pts[dataIndex].R;
            }
            else if (dataIndex < ds.Data.Count)
            {
                v = ds.Data[dataIndex];
            }
        }
        return new BitChartScriptableContext
        {
            DatasetIndex = dsIndex,
            DataIndex = dataIndex,
            Value = v,
            ValueX = vx,
            ValueR = vr,
            Label = dataIndex < _data.Labels.Count ? _data.Labels[dataIndex] : null,
            DatasetLabel = ds.Label,
            Active = active,
            Type = EffectiveType(ds)
        };
    }

    /// <summary>Resolves the effective color for a data element, honoring dataset palettes.</summary>
    private string ResolveBackground(BitChartDataset ds, int dsIndex, int dataIndex, bool perIndexPalette, double? value = null)
    {
        if (ds.BackgroundColorFn is { } fn && fn(Ctx(ds, dsIndex, dataIndex, value)) is { } c) return c;
        if (ds.BackgroundColors is { Count: > 0 } list)
            return list[dataIndex % list.Count];
        if (!string.IsNullOrEmpty(ds.BackgroundColor))
            return ds.BackgroundColor!;
        return perIndexPalette ? BitChartColorUtil.Palette(dataIndex) : BitChartColorUtil.Palette(dsIndex);
    }

    private string ResolveBorder(BitChartDataset ds, int dsIndex, int dataIndex, bool perIndexPalette, double? value = null)
    {
        if (ds.BorderColorFn is { } fn && fn(Ctx(ds, dsIndex, dataIndex, value)) is { } c) return c;
        if (ds.BorderColors is { Count: > 0 } list)
            return list[dataIndex % list.Count];
        if (!string.IsNullOrEmpty(ds.BorderColor))
            return ds.BorderColor!;
        return perIndexPalette ? BitChartColorUtil.Palette(dataIndex) : BitChartColorUtil.Palette(dsIndex);
    }

    private string FormatTooltipValue(BitChartDataset ds, double value)
    {
        var t = _options.Plugins.Tooltip;
        if (t.LabelFormatter is { } f) return f(ds.Label ?? "", value);
        string label = string.IsNullOrEmpty(ds.Label) ? "" : ds.Label + ": ";
        return label + value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Builds a tooltip item context for callbacks.</summary>
    private BitChartTooltipItemContext BuildTooltipItem(BitChartDataset ds, int dsIndex, int dataIndex, double value, string color)
    {
        double? vx = ds.Points is { } pts && dataIndex < pts.Count ? pts[dataIndex].X : null;
        return new BitChartTooltipItemContext
        {
            DatasetIndex = dsIndex,
            DataIndex = dataIndex,
            DatasetLabel = ds.Label,
            Label = dataIndex < _data.Labels.Count ? _data.Labels[dataIndex] : null,
            Value = value,
            ValueX = vx,
            Color = color,
            FormattedValue = value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    /// <summary>Builds the body text for one tooltip item, honoring the Label callback / formatter.</summary>
    private string BuildItemText(BitChartDataset ds, int dsIndex, int dataIndex, double value, string color)
    {
        var t = _options.Plugins.Tooltip;
        if (t.Callbacks.Label is { } cb && cb(BuildTooltipItem(ds, dsIndex, dataIndex, value, color)) is { } txt)
            return txt;
        return FormatTooltipValue(ds, value);
    }

    private void EnsureScales()
    {
        if (_config.Type is BitChartType.Pie or BitChartType.Doughnut)
            return;

        if (_config.Type is BitChartType.PolarArea or BitChartType.Radar)
        {
            _options.GetOrAddScale("r", BitChartScaleType.RadialLinear);
            return;
        }

        // Cartesian: ensure x and y exist with sensible defaults.
        var x = _options.GetOrAddScale("x", _config.Type is BitChartType.Scatter or BitChartType.Bubble
            ? BitChartScaleType.Linear : BitChartScaleType.Category);
        x.Position ??= BitChartPosition.Bottom;

        // Additional x axes referenced by datasets (default linear, bottom).
        foreach (var id in _data.Datasets.Select(d => d.XAxisID).Distinct())
        {
            if (id == "x" || string.IsNullOrEmpty(id)) continue;
            var x2 = _options.GetOrAddScale(id, BitChartScaleType.Linear);
            x2.Position ??= BitChartPosition.Bottom;
        }

        // Gather y axis ids referenced by datasets.
        var yIds = _data.Datasets.Select(d => d.YAxisID).Distinct().ToList();
        foreach (var id in yIds)
        {
            var y = _options.GetOrAddScale(id, BitChartScaleType.Linear);
            y.Position ??= BitChartPosition.Left;
        }
        if (!_options.Scales.ContainsKey("y"))
        {
            var y = _options.GetOrAddScale("y", BitChartScaleType.Linear);
            y.Position ??= BitChartPosition.Left;
        }
    }

    private void BuildLegend(BitChartScene scene)
    {
        var lo = _options.Plugins.Legend;
        if (!lo.Display) return;

        var legend = new BitChartLegendModel
        {
            Position = lo.Position,
            Align = lo.Align,
            Labels = lo.Labels,
            Title = lo.Title,
            OnClickToggle = lo.OnClickToggle
        };

        if (_config.Type is BitChartType.Pie or BitChartType.Doughnut or BitChartType.PolarArea)
        {
            // One legend entry per data index (label).
            var ds = _data.Datasets.FirstOrDefault();
            int n = _data.Labels.Count;
            for (int i = 0; i < n; i++)
            {
                legend.Items.Add(new BitChartLegendItemModel
                {
                    Text = _data.Labels[i],
                    Color = ds is null ? BitChartColorUtil.Palette(i) : ResolveBackground(ds, 0, i, true),
                    Hidden = _state.IsIndexHidden(i),
                    Index = i,
                    IsDataIndex = true,
                    UsePointStyle = lo.Labels.UsePointStyle,
                    PointStyle = lo.Labels.PointStyle
                });
            }
        }
        else
        {
            for (int i = 0; i < _data.Datasets.Count; i++)
            {
                var ds = _data.Datasets[i];
                legend.Items.Add(new BitChartLegendItemModel
                {
                    Text = ds.Label ?? $"Dataset {i + 1}",
                    Color = ResolveBackground(ds, i, 0, false),
                    StrokeColor = ResolveBorder(ds, i, 0, false),
                    Hidden = _state.IsDatasetHidden(i) || ds.Hidden,
                    Index = i,
                    UsePointStyle = lo.Labels.UsePointStyle,
                    PointStyle = ds.PointStyle
                });
            }
        }

        if (lo.Reverse) legend.Items.Reverse();
        scene.Legend = legend;
    }

    private BitChartTitleModel? BuildTitle(BitChartTitleOptions o)
    {
        if (!o.Display || string.IsNullOrEmpty(o.Text)) return null;
        return new BitChartTitleModel
        {
            Text = o.Text,
            Color = o.Color,
            Position = o.Position,
            Align = o.Align,
            Font = o.Font
        };
    }
}
