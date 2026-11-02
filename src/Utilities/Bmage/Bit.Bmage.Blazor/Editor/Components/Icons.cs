namespace Bit.Bmage.Editor.Components;

/// <summary>
/// Inline SVG icons for the default toolbar. They inherit <c>currentColor</c> and the surrounding font size, so a host
/// restyles them with CSS alone. Replace the toolbar entirely to use a different icon set.
/// </summary>
public static class Icons
{
    private const string Open = """<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private const string Close = "</svg>";

    /// <summary>Arrow cursor for the selection tool.</summary>
    public static string Select { get; } = $"""{Open}<path d="m4 3 7 17 2.5-6.5L20 11z"/>{Close}""";

    /// <summary>Hand for the pan tool.</summary>
    public static string Pan { get; } = $"""{Open}<path d="M9 11V5.5a1.5 1.5 0 0 1 3 0V11m0-1V4.5a1.5 1.5 0 0 1 3 0V11m0-.5V6.5a1.5 1.5 0 0 1 3 0V13a7 7 0 0 1-7 7h-1a6 6 0 0 1-6-6v-3.5a1.5 1.5 0 0 1 3 0V13"/>{Close}""";

    public static string Crop { get; } = $"""{Open}<path d="M6 2v14a2 2 0 0 0 2 2h14M2 6h14a2 2 0 0 1 2 2v14"/>{Close}""";

    public static string Pen { get; } = $"""{Open}<path d="M12 19l7-7 3 3-7 7-3-3zM18 13l-1.5-7.5L2 2l3.5 14.5L13 18l5-5zM2 2l7.586 7.586"/><circle cx="11" cy="11" r="2"/>{Close}""";

    public static string Arrow { get; } = $"""{Open}<path d="M5 19 19 5M11 5h8v8"/>{Close}""";

    public static string Line { get; } = $"""{Open}<path d="M5 19 19 5"/>{Close}""";

    public static string Rectangle { get; } = $"""{Open}<rect x="4" y="6" width="16" height="12" rx="1"/>{Close}""";

    public static string Ellipse { get; } = $"""{Open}<ellipse cx="12" cy="12" rx="8" ry="6"/>{Close}""";

    public static string Text { get; } = $"""{Open}<path d="M4 6V4h16v2M12 4v16M9 20h6"/>{Close}""";

    public static string Step { get; } = $"""{Open}<circle cx="12" cy="12" r="9"/><path d="M11 8h1v8M10 16h4"/>{Close}""";

    public static string Highlight { get; } = $"""{Open}<path d="m9 11-6 6v3h3l6-6M15 5l4 4-8 8-4-4z"/>{Close}""";

    public static string Redact { get; } = $"""{Open}<rect x="3" y="8" width="18" height="8" rx="1" fill="currentColor" stroke="none"/>{Close}""";

    public static string RotateLeft { get; } = $"""{Open}<path d="M3 3v6h6"/><path d="M3.5 9a9 9 0 1 1 .5 6"/>{Close}""";

    public static string RotateRight { get; } = $"""{Open}<path d="M21 3v6h-6"/><path d="M20.5 9a9 9 0 1 0-.5 6"/>{Close}""";

    public static string FlipHorizontal { get; } = $"""{Open}<path d="M12 3v18M7 8 3 12l4 4zM17 8l4 4-4 4z"/>{Close}""";

    public static string FlipVertical { get; } = $"""{Open}<path d="M3 12h18M8 7 12 3l4 4zM8 17l4 4 4-4z"/>{Close}""";

    public static string Undo { get; } = $"""{Open}<path d="M3 7v6h6"/><path d="M3.5 13a9 9 0 1 1 2.6 6.4"/>{Close}""";

    public static string Redo { get; } = $"""{Open}<path d="M21 7v6h-6"/><path d="M20.5 13a9 9 0 1 0-2.6 6.4"/>{Close}""";

    public static string Reset { get; } = $"""{Open}<path d="M3 12a9 9 0 1 0 9-9 9 9 0 0 0-6.4 2.6L3 8"/><path d="M3 3v5h5"/>{Close}""";

    public static string Adjust { get; } = $"""{Open}<path d="M4 21v-7M4 10V3M12 21v-9M12 8V3M20 21v-5M20 12V3M1 14h6M9 8h6M17 16h6"/>{Close}""";

    public static string Filters { get; } = $"""{Open}<circle cx="9" cy="9" r="6"/><circle cx="15" cy="15" r="6"/>{Close}""";

    public static string Layers { get; } = $"""{Open}<path d="m12 2 9 5-9 5-9-5zM3 12l9 5 9-5M3 17l9 5 9-5"/>{Close}""";

    public static string Download { get; } = $"""{Open}<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3"/>{Close}""";

    public static string Delete { get; } = $"""{Open}<path d="M3 6h18M8 6V4h8v2M6 6l1 14h10l1-14M10 11v6M14 11v6"/>{Close}""";

    /// <summary>An open eye, for a visible layer.</summary>
    public static string Visible { get; } = $"""{Open}<path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>{Close}""";

    /// <summary>A struck-through eye, for a hidden layer.</summary>
    public static string Hidden { get; } = $"""{Open}<path d="M2 12s3.5-7 10-7c2 0 3.7.6 5.1 1.5M22 12s-3.5 7-10 7c-2 0-3.7-.6-5.1-1.5M3 3l18 18"/>{Close}""";

    /// <summary>A round brush, for painting.</summary>
    public static string Brush { get; } = $"""{Open}<path d="M18.4 2.6a2 2 0 0 1 2.9 2.9L12 14.8 9.2 12z"/><path d="M8.5 13.5a3.5 3.5 0 0 0-3.5 3.5c0 1.5-1 2.5-2.5 3 1.5 1 6 1 7.6-.6a3.5 3.5 0 0 0-1.6-5.9z"/>{Close}""";

    /// <summary>An eraser.</summary>
    public static string Eraser { get; } = $"""{Open}<path d="M7 21h10M5 15l9.5-9.5a2 2 0 0 1 2.8 0l2.2 2.2a2 2 0 0 1 0 2.8L12 18H8z"/><path d="m9 11 5 5"/>{Close}""";

    /// <summary>An eyedropper, for picking a colour.</summary>
    public static string Eyedropper { get; } = $"""{Open}<path d="m2 22 1-1h3l9-9M3 21v-3l9-9"/><path d="m15 6 3.4-3.4a2.1 2.1 0 1 1 3 3L18 9l.4.4a2.1 2.1 0 1 1-3 3l-3.8-3.8a2.1 2.1 0 1 1 3-3z"/>{Close}""";

    /// <summary>A paint bucket, for filling a region.</summary>
    public static string Fill { get; } = $"""{Open}<path d="m19 11-8-8-8.6 8.6a2 2 0 0 0 0 2.8l5.2 5.2a2 2 0 0 0 2.8 0zM5 2l5 5M2 13h15"/><path d="M22 20a2 2 0 1 1-4 0c0-1.6 2-4 2-4s2 2.4 2 4z"/>{Close}""";

    /// <summary>A wand with sparkles, for the magic eraser.</summary>
    public static string MagicEraser { get; } = $"""{Open}<path d="m3 21 12-12M14 4l1-2 1 2 2 1-2 1-1 2-1-2-2-1zM19 11l.5-1 .5 1 1 .5-1 .5-.5 1-.5-1-1-.5z"/>{Close}""";

    /// <summary>A dashed rectangle, for selecting a rectangle.</summary>
    public static string SelectRectangle { get; } = $"""{Open}<rect x="4" y="5" width="16" height="14" rx="1" stroke-dasharray="3 2.5"/>{Close}""";

    /// <summary>A dashed ellipse, for selecting an ellipse.</summary>
    public static string SelectEllipse { get; } = $"""{Open}<ellipse cx="12" cy="12" rx="8.5" ry="6.5" stroke-dasharray="3 2.5"/>{Close}""";

    /// <summary>A lasso loop, for a freehand selection.</summary>
    public static string Lasso { get; } = $"""{Open}<path d="M7 17c-3-1-4-3.5-4-6 0-4 4-7 9-7s9 3 9 7-4 7-9 7c-1.5 0-3-.3-4-.8" stroke-dasharray="3 2.5"/><path d="M8 16.5c-1 1-1 3 1 4"/>{Close}""";

    /// <summary>A wand, for selecting by colour.</summary>
    public static string MagicWand { get; } = $"""{Open}<path d="m4 20 11-11M16 3v3M16 12v3M21 8h-3M12 8H9M19.5 4.5l-2 2M12.5 11.5l-2 2M19.5 11.5l-2-2"/>{Close}""";

    /// <summary>A split frame, for comparing the edit with the original.</summary>
    public static string Compare { get; } = $"""{Open}<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M12 2v20"/><path d="M3 15l5-5 4 4" />{Close}""";

    /// <summary>Two stacked frames, for duplicating.</summary>
    public static string Duplicate { get; } = $"""{Open}<rect x="8" y="8" width="13" height="13" rx="2"/><path d="M16 8V5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h3"/>{Close}""";

    /// <summary>An arrow up, for raising a layer.</summary>
    public static string MoveUp { get; } = $"""{Open}<path d="M12 19V5M5 12l7-7 7 7"/>{Close}""";

    /// <summary>An arrow down, for lowering a layer.</summary>
    public static string MoveDown { get; } = $"""{Open}<path d="M12 5v14M19 12l-7 7-7-7"/>{Close}""";

    /// <summary>A closed padlock, for a locked layer.</summary>
    public static string Locked { get; } = $"""{Open}<rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>{Close}""";

    /// <summary>An open padlock, for an unlocked layer.</summary>
    public static string Unlocked { get; } = $"""{Open}<rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 7.5-2"/>{Close}""";
}
