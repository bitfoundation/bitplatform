namespace Bit.Bmotion.Demo.Client.Shared;

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

    // ── Capabilities ─────────────────────────────────────────────────────────

    /// <summary>Springs, and physics in general: a trace that overshoots and settles.</summary>
    public const string Pulse = """<path d="M2.75 12.5h3.5l2.25-6 3.75 12 2.75-9 1.75 3h4.5" />""";

    /// <summary>Keyframes: values pinned along a timeline.</summary>
    public const string Keyframe = """<path d="M3 12h2.5M11 12h2M18.5 12H21" /><path d="M8.25 9.25 11 12l-2.75 2.75L5.5 12Z" /><path d="M15.75 9.25 18.5 12l-2.75 2.75L13 12Z" />""";

    /// <summary>Gestures: a pointer over the thing it is acting on.</summary>
    public const string Pointer = """<path d="m6.5 4.25 12 6.25-5.25 1.5-1.5 5.25Z" /><path d="m13.5 13.5 4.75 4.75" />""";

    /// <summary>Drag: free movement on both axes.</summary>
    public const string Drag = """<path d="M12 3.25v17.5M3.25 12h17.5" /><path d="m9.5 5.75 2.5-2.5 2.5 2.5M9.5 18.25l2.5 2.5 2.5-2.5M5.75 9.5l-2.5 2.5 2.5 2.5M18.25 9.5l2.5 2.5-2.5 2.5" />""";

    /// <summary>Variants: named states layered over one subtree.</summary>
    public const string Layers = """<path d="m12 3.25 8.75 4.5L12 12.25 3.25 7.75Z" /><path d="m3.25 12.5 8.75 4.5 8.75-4.5" /><path d="m3.25 16.75 8.75 4 8.75-4" />""";

    /// <summary>AnimatePresence: something appearing and leaving.</summary>
    public const string Presence = """<path d="M2.75 12S6.5 5.75 12 5.75 21.25 12 21.25 12 17.5 18.25 12 18.25 2.75 12 2.75 12Z" /><circle cx="12" cy="12" r="2.75" />""";

    /// <summary>Split text: type, broken into pieces.</summary>
    public const string Text = """<path d="M4.5 7V4.75h15V7M12 4.75V19.25M9.25 19.25h5.5" />""";

    /// <summary>Layout: a frame whose parts have been rearranged.</summary>
    public const string Layout = """<rect x="3.25" y="3.25" width="17.5" height="17.5" rx="2.25" /><path d="M3.25 9.5h17.5M9.5 20.75V9.5" />""";

    /// <summary>Scroll: a viewport being moved through.</summary>
    public const string Scroll = """<rect x="7.5" y="3.25" width="9" height="17.5" rx="4.5" /><path d="M12 7.25v3" />""";

    /// <summary>The programmatic API: controls driven from code.</summary>
    public const string Controls = """<path d="M3.5 7.5h9M16.5 7.5h4M3.5 16.5h3.5M11 16.5h9.5" /><circle cx="14.5" cy="7.5" r="2.25" /><circle cx="9" cy="16.5" r="2.25" />""";

    /// <summary>Motion path: an element sent along a curve.</summary>
    public const string Path = """<path d="M5 18.5c0-9 14-3 14-12" /><circle cx="5" cy="18.5" r="2.25" /><circle cx="19" cy="6.5" r="2.25" />""";

    /// <summary>Accessibility.</summary>
    public const string Accessibility = """<circle cx="12" cy="4.75" r="1.75" /><path d="M4.75 8.75h14.5M12 8.75v5.5m0 0-3 6.25m3-6.25 3 6.25" />""";

    /// <summary>Documentation.</summary>
    public const string Book = """<path d="M4 5.75A2.5 2.5 0 0 1 6.5 3.25h13.25V18H6.5A2.5 2.5 0 0 0 4 20.5Z" /><path d="M4 20.5A2.5 2.5 0 0 1 6.5 18h13.25v2.75H6.5A2.5 2.5 0 0 1 4 20.5Z" />""";

    /// <summary>Anything new, and anything generated.</summary>
    public const string Sparkle = """<path d="m11 3.5 1.9 5.6 5.6 1.9-5.6 1.9-1.9 5.6-1.9-5.6-5.6-1.9 5.6-1.9Z" /><path d="M18 15.5v4M20 17.5h-4" />""";
}
