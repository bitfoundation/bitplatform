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
}
