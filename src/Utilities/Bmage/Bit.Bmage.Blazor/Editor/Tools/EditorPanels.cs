namespace Bit.Bmage.Editor.Tools;

/// <summary>The tabs of the built-in options panel, for <see cref="Components.BmageEditor.Panels"/>.</summary>
[Flags]
public enum EditorPanels
{
    /// <summary>No tabs: the panel is not shown.</summary>
    None = 0,
    /// <summary>Tone and colour adjustments, the histogram and the automatic corrections.</summary>
    Adjust = 1 << 0,
    /// <summary>Filter presets and effects.</summary>
    Filters = 1 << 1,
    /// <summary>Straighten, resize, canvas size and trim.</summary>
    Transform = 1 << 2,
    /// <summary>The drawing style of the tools and the selected annotation's properties.</summary>
    Tools = 1 << 3,
    /// <summary>Layers and their opacity, blend mode, order, groups and clipping.</summary>
    Layers = 1 << 4,
    /// <summary>The edit history.</summary>
    History = 1 << 5,
    /// <summary>Export format, quality, size and project files.</summary>
    Export = 1 << 6,
    /// <summary>Every tab.</summary>
    All = Adjust | Filters | Transform | Tools | Layers | History | Export,
}
