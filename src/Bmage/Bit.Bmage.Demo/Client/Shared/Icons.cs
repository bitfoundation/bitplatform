namespace Bit.Bmage.Demo.Client.Shared;

/// <summary>
/// The site's icon set: the inner markup of a 24×24 icon, without the <c>&lt;svg&gt;</c> element
/// around it. <see cref="Icon"/> supplies that, along with the class the stylesheet paints them
/// through.
/// <para>
/// They are drawn as strokes rather than filled outlines, at the weight Fluent's regular icons are
/// drawn at, and they carry no colour of their own: <c>svg.icon</c> in app.css strokes them with
/// <c>currentColor</c>, so an icon is the colour of whatever it sits inside and follows that thing
/// through every theme, hover and disabled state without being re-authored.
/// </para>
/// <para>
/// They live in C# rather than in a sprite or an icon font because the site is prerendered: markup
/// that is part of the document costs no request, cannot arrive after the first paint, and cannot
/// go missing behind a failed download - and an icon that fails to load in a header is a button
/// with nothing in it.
/// </para>
/// </summary>
public static class Icons
{
    // ── Chrome ───────────────────────────────────────────────────────────────

    public const string Search = """<circle cx="11" cy="11" r="7" /><path d="m16.2 16.2 4.3 4.3" />""";

    public const string Menu = """<path d="M3.5 6.5h17M3.5 12h17M3.5 17.5h17" />""";

    public const string Close = """<path d="m6 6 12 12M18 6 6 18" />""";

    public const string Sun = """<circle cx="12" cy="12" r="4.25" /><path d="M12 2.75v2M12 19.25v2M2.75 12h2M19.25 12h2M5.4 5.4l1.4 1.4M17.2 17.2l1.4 1.4M18.6 5.4l-1.4 1.4M6.8 17.2l-1.4 1.4" />""";

    public const string Moon = """<path d="M20.4 14.3A8.6 8.6 0 0 1 9.7 3.6 8.6 8.6 0 1 0 20.4 14.3Z" />""";

    public const string ChevronRight = """<path d="m9.5 5.5 6.5 6.5-6.5 6.5" />""";

    public const string ArrowRight = """<path d="M3.75 12h16.5m-6.25-6.25L20.25 12l-6.25 6.25" />""";

    public const string ArrowLeft = """<path d="M20.25 12H3.75m6.25-6.25L3.75 12l6.25 6.25" />""";

    public const string ArrowUp = """<path d="M12 20.25V3.75M5.75 10 12 3.75 18.25 10" />""";

    public const string Copy = """<rect x="9" y="9" width="11.25" height="11.25" rx="2.25" /><path d="M5.25 15H4.5a1.5 1.5 0 0 1-1.5-1.5v-9A1.5 1.5 0 0 1 4.5 3h9A1.5 1.5 0 0 1 15 4.5v.75" />""";

    public const string Check = """<path d="m4.75 12.5 4.75 4.75L19.25 6.5" />""";

    public const string Link = """<path d="M10 14a4.25 4.25 0 0 0 6 0l2.75-2.75a4.25 4.25 0 0 0-6-6L11.5 6.5" /><path d="M14 10a4.25 4.25 0 0 0-6 0L5.25 12.75a4.25 4.25 0 0 0 6 6L12.5 17.5" />""";

    public const string Play = """<path d="M7.75 4.75 19 12 7.75 19.25Z" />""";

    public const string Download = """<path d="M12 3.75v11.5m-4.75-4.5L12 15.5l4.75-4.75" /><path d="M4.25 16.5v1.75a2 2 0 0 0 2 2h11.5a2 2 0 0 0 2-2V16.5" />""";

    // ── Capabilities ─────────────────────────────────────────────────────────

    /// <summary>An image: the frame, the sun and the hill every photo icon is drawn with.</summary>
    public const string Image = """<rect x="3.25" y="4.25" width="17.5" height="15.5" rx="2.25" /><circle cx="8.75" cy="9.5" r="1.75" /><path d="m20.75 15.5-5-5-10.5 9.25" />""";

    /// <summary>Cropping: two corner brackets crossing.</summary>
    public const string Crop = """<path d="M6.75 2.75v13.5a1 1 0 0 0 1 1h13.5" /><path d="M2.75 6.75h13.5a1 1 0 0 1 1 1v13.5" />""";

    /// <summary>Adjustments: a mixer's sliders.</summary>
    public const string Sliders = """<path d="M3.5 7.5h9M16.5 7.5h4M3.5 16.5h3.5M11 16.5h9.5" /><circle cx="14.5" cy="7.5" r="2.25" /><circle cx="9" cy="16.5" r="2.25" />""";

    /// <summary>Annotation: a pen drawing a line.</summary>
    public const string Pen = """<path d="m14.75 5.25 4 4L8.5 19.5H4.5v-4Z" /><path d="m12.5 7.5 4 4" />""";

    /// <summary>A pipeline: steps joined in order.</summary>
    public const string Pipeline = """<rect x="2.75" y="9" width="5" height="6" rx="1.25" /><rect x="9.5" y="9" width="5" height="6" rx="1.25" /><rect x="16.25" y="9" width="5" height="6" rx="1.25" /><path d="M7.75 12h1.75M14.5 12h1.75" />""";

    /// <summary>Privacy: a shield.</summary>
    public const string Shield = """<path d="M12 3.25 19.25 6v5.5c0 4.5-3 7.75-7.25 9.25-4.25-1.5-7.25-4.75-7.25-9.25V6Z" /><path d="m9 12 2.25 2.25L15.25 10" />""";

    /// <summary>Performance: a gauge.</summary>
    public const string Gauge = """<path d="M4.25 17.5a8.25 8.25 0 1 1 15.5 0" /><path d="m12 13.5 4-4.25" /><circle cx="12" cy="13.5" r="1.25" />""";

    /// <summary>Batches: a stack of images.</summary>
    public const string Stack = """<rect x="6.75" y="6.75" width="14" height="13.5" rx="2" /><path d="M3.25 15.75V5.25a2 2 0 0 1 2-2h10.5" /><path d="m20.75 16.25-4-4-7 6.75" />""";

    /// <summary>Code without a UI: angle brackets.</summary>
    public const string Code = """<path d="m8.25 7.25-5 4.75 5 4.75M15.75 7.25l5 4.75-5 4.75M13.5 4.75l-3 14.5" />""";

    /// <summary>Capabilities: a chip being probed.</summary>
    public const string Chip = """<rect x="6.25" y="6.25" width="11.5" height="11.5" rx="1.75" /><path d="M9.5 3v3.25M14.5 3v3.25M9.5 17.75V21M14.5 17.75V21M3 9.5h3.25M3 14.5h3.25M17.75 9.5H21M17.75 14.5H21" />""";

    /// <summary>A profile picture.</summary>
    public const string Person = """<circle cx="12" cy="8.5" r="3.75" /><path d="M4.75 20a7.25 7.25 0 0 1 14.5 0" />""";

    /// <summary>A product label.</summary>
    public const string Tag = """<path d="M3.25 12.5V4.25a1 1 0 0 1 1-1h8.25l8.25 8.25-9.25 9.25Z" /><circle cx="8" cy="8" r="1.5" />""";

    /// <summary>A post, shared.</summary>
    public const string Share = """<circle cx="17.5" cy="5.75" r="2.5" /><circle cx="6.5" cy="12" r="2.5" /><circle cx="17.5" cy="18.25" r="2.5" /><path d="m8.75 10.75 6.5-3.75M8.75 13.25l6.5 3.75" />""";

    /// <summary>Hosting: a server and a window.</summary>
    public const string Server = """<rect x="3.25" y="3.75" width="17.5" height="7" rx="1.75" /><rect x="3.25" y="13.25" width="17.5" height="7" rx="1.75" /><path d="M7 7.25h.01M7 16.75h.01" />""";

    /// <summary>File formats: a document with a folded corner.</summary>
    public const string File = """<path d="M13.75 3.25H6.5a1.75 1.75 0 0 0-1.75 1.75v14a1.75 1.75 0 0 0 1.75 1.75h11a1.75 1.75 0 0 0 1.75-1.75V8.75Z" /><path d="M13.75 3.25v5.5h5.5" />""";

    /// <summary>Documentation.</summary>
    public const string Book = """<path d="M4 5.75A2.5 2.5 0 0 1 6.5 3.25h13.25V18H6.5A2.5 2.5 0 0 0 4 20.5Z" /><path d="M4 20.5A2.5 2.5 0 0 1 6.5 18h13.25v2.75H6.5A2.5 2.5 0 0 1 4 20.5Z" />""";

    /// <summary>Anything new, and anything generated.</summary>
    public const string Sparkle = """<path d="m11 3.5 1.9 5.6 5.6 1.9-5.6 1.9-1.9 5.6-1.9-5.6-5.6-1.9 5.6-1.9Z" /><path d="M18 15.5v4M20 17.5h-4" />""";
}
