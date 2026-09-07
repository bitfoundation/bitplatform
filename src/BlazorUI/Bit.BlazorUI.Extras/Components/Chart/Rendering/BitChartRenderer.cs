using System.Globalization;

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
    private readonly string _uid;

    /// <summary>
    /// The scales actually used for this render. Seeded from <see cref="BitChartOptions.Scales"/> and
    /// completed with defaults here, so the caller's options object is never mutated and can safely be
    /// shared between charts of different types.
    /// </summary>
    private readonly Dictionary<string, BitChartScaleOptions> _scales = new();

    public BitChartRenderer(BitChartConfig config, BitChartRenderState state, double width, double height, string? uid = null)
    {
        _config = config;
        _data = config.Data;
        _options = config.Options;
        _state = state;
        _w = width;
        _h = height;
        _uid = uid ?? "bc";
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
        scene.IsEmpty = scene.Elements.Count == 0 && scene.Series.Count == 0;
        return scene;
    }

    // ---- shared helpers ----

    private int _defSeq;

    /// <summary>The culture every number/date is formatted with (invariant unless the caller opts in).</summary>
    private CultureInfo Culture => _options.Culture ?? CultureInfo.InvariantCulture;

    /// <summary>Formats a data value the way tooltips and data labels present it.</summary>
    private string FormatNumber(double value, string format = "0.###") => value.ToString(format, Culture);

    /// <summary>Registers a gradient on the scene and returns a <c>url(#id)</c> fill reference.
    /// Ids are namespaced per chart instance so several charts can share a page.</summary>
    private string RegisterGradient(BitChartScene scene, BitChartGradientBase grad)
    {
        string id = $"{_uid}-g{_defSeq++}";
        scene.Defs.Add(new BitChartGradientDef(id, grad));
        return $"url(#{id})";
    }

    /// <summary>Registers a pattern on the scene and returns a <c>url(#id)</c> fill reference.
    /// Ids are namespaced per chart instance so several charts can share a page.</summary>
    private string RegisterPattern(BitChartScene scene, BitChartFillPattern pattern)
    {
        string id = $"{_uid}-p{_defSeq++}";
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
    private BitChartScriptableContext Ctx(BitChartDataset ds, int dsIndex, int dataIndex, double? value = null, bool active = false)
    {
        double? v = value;
        double? vx = null, vr = null;
        if (ds.Points is { } pts && dataIndex < pts.Count)
        {
            vx = pts[dataIndex].X;
            vr = pts[dataIndex].R;
            v ??= pts[dataIndex].Y;
        }
        else if (v is null && dataIndex < ds.Data.Count)
        {
            v = ds.Data[dataIndex];
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
    private string ResolveBackground(BitChartDataset ds, int dsIndex, int dataIndex, bool perIndexPalette, double? value = null, bool active = false)
    {
        if (ds.BackgroundColorFn is { } fn && fn(Ctx(ds, dsIndex, dataIndex, value, active)) is { } c) return c;
        if (ds.BackgroundColors is { Count: > 0 } list)
            return list[((dataIndex % list.Count) + list.Count) % list.Count];
        if (!string.IsNullOrEmpty(ds.BackgroundColor))
            return ds.BackgroundColor!;
        return perIndexPalette ? BitChartColorUtil.Palette(dataIndex) : BitChartColorUtil.Palette(dsIndex);
    }

    /// <summary>
    /// Resolves the border color of an element. <paramref name="fallbackToBackground"/> is used by the
    /// filled element types (bars, arcs): with no explicit border color they take the fill color rather
    /// than an unrelated palette entry, which would otherwise outline them in a different hue.
    /// </summary>
    private string ResolveBorder(BitChartDataset ds, int dsIndex, int dataIndex, bool perIndexPalette,
        double? value = null, bool fallbackToBackground = false, bool active = false)
    {
        if (ds.BorderColorFn is { } fn && fn(Ctx(ds, dsIndex, dataIndex, value, active)) is { } c) return c;
        if (ds.BorderColors is { Count: > 0 } list)
            return list[((dataIndex % list.Count) + list.Count) % list.Count];
        if (!string.IsNullOrEmpty(ds.BorderColor))
            return ds.BorderColor!;
        if (fallbackToBackground)
            return ResolveBackground(ds, dsIndex, dataIndex, perIndexPalette, value, active);
        return perIndexPalette ? BitChartColorUtil.Palette(dataIndex) : BitChartColorUtil.Palette(dsIndex);
    }

    /// <summary>True when the dataset asks for a border color in any form.</summary>
    private static bool HasExplicitBorder(BitChartDataset ds)
        => ds.BorderColorFn is not null || ds.BorderColors is { Count: > 0 } || !string.IsNullOrEmpty(ds.BorderColor);

    /// <summary>
    /// Resolves the border/line width for a dataset, falling back to the per-type defaults on
    /// <see cref="BitChartElementOptions"/> when the dataset leaves <see cref="BitChartDataset.BorderWidth"/> unset.
    /// </summary>
    private double ResolveBorderWidth(BitChartDataset ds, BitChartType type, int dsIndex = 0, int dataIndex = 0, double? value = null, bool active = false)
    {
        if (active && ds.HoverBorderWidth is { } hbw) return hbw;
        if (ds.BorderWidthFn is { } fn && fn(Ctx(ds, dsIndex, dataIndex, value, active)) is { } w) return w;
        if (ds.BorderWidth is { } bw) return bw;
        var e = _options.Elements;
        return type switch
        {
            BitChartType.Bar => HasExplicitBorder(ds) ? Math.Max(e.BarBorderWidth, 1) : e.BarBorderWidth,
            BitChartType.Pie or BitChartType.Doughnut or BitChartType.PolarArea => e.ArcBorderWidth,
            _ => e.LineBorderWidth
        };
    }

    /// <summary>Marker radius for a dataset, falling back to the element default.</summary>
    private double ResolvePointRadius(BitChartDataset ds) => ds.PointRadius ?? _options.Elements.PointRadius;

    /// <summary>Line smoothing for a dataset, falling back to the element default.</summary>
    private double ResolveTension(BitChartDataset ds) => ds.Tension ?? _options.Elements.LineTension;

    private string FormatTooltipValue(BitChartDataset ds, double value)
    {
        var t = _options.Plugins.Tooltip;
        if (t.LabelFormatter is { } f) return f(ds.Label ?? "", value);
        string label = string.IsNullOrEmpty(ds.Label) ? "" : ds.Label + ": ";
        return label + FormatNumber(value);
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
            FormattedValue = FormatNumber(value)
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

    // ---- scales ----

    /// <summary>The scale options for an id within this render (never null once EnsureScales has run).</summary>
    private BitChartScaleOptions Scale(string id) => _scales[id];

    /// <summary>
    /// The radial scale a radar or polar-area chart reads. One radial scale serves the whole chart, so
    /// it is the first dataset's <see cref="BitChartDataset.RAxisID"/> that names it.
    /// </summary>
    private string RadialScaleId
    {
        get
        {
            var ds = _data.Datasets.FirstOrDefault(d => !string.IsNullOrEmpty(d.RAxisID));
            return ds?.RAxisID ?? "r";
        }
    }

    /// <summary>Effective position of a scale, without writing the default back onto the caller's object.</summary>
    private static BitChartPosition PositionOf(BitChartScaleOptions o, BitChartPosition fallback) => o.Position ?? fallback;

    /// <summary>Returns the caller's scale for an id, or a fresh default one - added to the local map only.</summary>
    private BitChartScaleOptions GetOrAddScale(string id, BitChartScaleType type)
    {
        if (_scales.TryGetValue(id, out var s)) return s;
        if (_options.Scales.TryGetValue(id, out var user))
        {
            _scales[id] = user;
            return user;
        }
        s = new BitChartScaleOptions { Id = id, Type = type };
        _scales[id] = s;
        return s;
    }

    private void EnsureScales()
    {
        _scales.Clear();
        foreach (var (id, so) in _options.Scales) _scales[id] = so;

        if (_config.Type is BitChartType.Pie or BitChartType.Doughnut)
            return;

        if (_config.Type is BitChartType.PolarArea or BitChartType.Radar)
        {
            GetOrAddScale(RadialScaleId, BitChartScaleType.RadialLinear);
            return;
        }

        // Cartesian: ensure x and y exist with sensible defaults.
        GetOrAddScale("x", _config.Type is BitChartType.Scatter or BitChartType.Bubble
            ? BitChartScaleType.Linear : BitChartScaleType.Category);

        // Additional x axes referenced by datasets (default linear).
        foreach (var id in _data.Datasets.Select(d => d.XAxisID).Distinct())
        {
            if (id == "x" || string.IsNullOrEmpty(id)) continue;
            GetOrAddScale(id, BitChartScaleType.Linear);
        }

        // Gather y axis ids referenced by datasets.
        foreach (var id in _data.Datasets.Select(d => d.YAxisID).Distinct())
        {
            if (string.IsNullOrEmpty(id)) continue;
            GetOrAddScale(id, BitChartScaleType.Linear);
        }
        GetOrAddScale("y", BitChartScaleType.Linear);
    }

    private void BuildLegend(BitChartScene scene)
    {
        var lo = _options.Plugins.Legend;
        if (!lo.Display) return;

        var legend = new BitChartLegendModel
        {
            // The legend only has four sides to live on; anything else would silently render nowhere.
            Position = lo.Position is BitChartPosition.Bottom or BitChartPosition.Left or BitChartPosition.Right
                ? lo.Position : BitChartPosition.Top,
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

        if (lo.Filter is { } filter)
            legend.Items.RemoveAll(it => !filter(it));
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
            Font = o.Font,
            Padding = o.Padding
        };
    }
}
