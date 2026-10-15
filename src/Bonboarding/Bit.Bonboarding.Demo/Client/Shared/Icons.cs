namespace Bit.Bonboarding.Demo.Client.Shared;

/// <summary>
/// The site's icon set: the inner markup of a 24×24 icon, without the <c>&lt;svg&gt;</c> element
/// around it. <see cref="Icon"/> supplies that, along with the class the stylesheet paints them
/// through.
/// <para>
/// They are drawn as strokes rather than filled outlines, at the weight Fluent's regular icons are
/// drawn at, and they carry no colour of their own: <c>svg.icon</c> in app.css strokes them with
/// <c>currentColor</c>, so an icon is the colour of whatever it sits inside and follows that thing
/// through every theme, hover and disabled state.
/// </para>
/// <para>
/// They live in C# rather than in a sprite or an icon font because the site is prerendered: markup
/// that is part of the document costs no request and cannot arrive after the first paint.
/// </para>
/// </summary>
public static class Icons
{
    // ── Chrome ───────────────────────────────────────────────────────────────

    public const string Menu = """<path d="M3.5 6.5h17M3.5 12h17M3.5 17.5h17" />""";

    public const string Sun = """<circle cx="12" cy="12" r="4.25" /><path d="M12 2.75v2M12 19.25v2M2.75 12h2M19.25 12h2M5.4 5.4l1.4 1.4M17.2 17.2l1.4 1.4M18.6 5.4l-1.4 1.4M6.8 17.2l-1.4 1.4" />""";

    public const string Moon = """<path d="M20.4 14.3A8.6 8.6 0 0 1 9.7 3.6 8.6 8.6 0 1 0 20.4 14.3Z" />""";

    public const string ChevronRight = """<path d="m9.5 5.5 6.5 6.5-6.5 6.5" />""";

    public const string ArrowRight = """<path d="M3.75 12h16.5m-6.25-6.25L20.25 12l-6.25 6.25" />""";

    public const string ArrowLeft = """<path d="M20.25 12H3.75m6.25-6.25L3.75 12l6.25 6.25" />""";

    public const string ArrowUp = """<path d="M12 20.25V3.75M5.75 10 12 3.75 18.25 10" />""";

    public const string Play = """<path d="M7.75 4.75 19 12 7.75 19.25Z" />""";

    /// <summary>A message bar's informational mark.</summary>
    public const string Info = """<circle cx="12" cy="12" r="8.75" /><path d="M12 11v5.25M12 7.75v.01" />""";

    /// <summary>A message bar's error mark.</summary>
    public const string Error = """<circle cx="12" cy="12" r="8.75" /><path d="M12 7.5v5.25M12 16.25v.01" />""";

    // ── Capabilities ─────────────────────────────────────────────────────────

    /// <summary>Placement and the spotlight: a target with a ring around it.</summary>
    public const string Target = """<circle cx="12" cy="12" r="8.75" /><circle cx="12" cy="12" r="4.75" /><circle cx="12" cy="12" r="1" />""";

    /// <summary>Dynamic targets: waiting for something to turn up.</summary>
    public const string Clock = """<circle cx="12" cy="12" r="8.75" /><path d="M12 7.25V12l3.25 2" />""";

    /// <summary>Conditional and branching flows: one path splitting in two.</summary>
    public const string Branch = """<circle cx="6.5" cy="5.5" r="2.25" /><circle cx="6.5" cy="18.5" r="2.25" /><circle cx="17.5" cy="7.5" r="2.25" /><path d="M6.5 7.75v8.5M17.5 9.75c0 4.25-11 2.75-11 6.5" />""";

    /// <summary>Multi-page tours: one document stacked on another.</summary>
    public const string Pages = """<path d="M9.25 3.25h6.5l3.5 3.5v10a1.5 1.5 0 0 1-1.5 1.5h-8.5a1.5 1.5 0 0 1-1.5-1.5v-12a1.5 1.5 0 0 1 1.5-1.5Z" /><path d="M5.25 7.25v12a1.5 1.5 0 0 0 1.5 1.5h8.5" />""";

    /// <summary>Theming: a palette.</summary>
    public const string Palette = """<path d="M12 3.25a8.75 8.75 0 1 0 0 17.5c1.1 0 1.75-.75 1.75-1.6 0-.95-.75-1.4-.75-2.4 0-1.1.9-1.75 2-1.75h2.25a3.5 3.5 0 0 0 3.5-3.5c0-4.6-3.9-8.25-8.75-8.25Z" /><circle cx="7.75" cy="11.25" r="1" /><circle cx="10.75" cy="7.5" r="1" /><circle cx="15.25" cy="7.75" r="1" />""";

    /// <summary>Headless UI: your own markup inside the library's frame.</summary>
    public const string Code = """<path d="m8.5 7.25-4.75 4.75 4.75 4.75M15.5 7.25l4.75 4.75-4.75 4.75" />""";

    /// <summary>Events: a trace of activity over time.</summary>
    public const string Pulse = """<path d="M2.75 12.5h3.5l2.25-6 3.75 12 2.75-9 1.75 3h4.5" />""";

    /// <summary>Accessibility.</summary>
    public const string Accessibility = """<circle cx="12" cy="4.75" r="1.75" /><path d="M4.75 8.75h14.5M12 8.75v5.5m0 0-3 6.25m3-6.25 3 6.25" />""";

    /// <summary>The guides, as a group: finding your way around.</summary>
    public const string Compass = """<circle cx="12" cy="12" r="8.75" /><path d="m15.25 8.75-1.9 4.6-4.6 1.9 1.9-4.6Z" />""";

    /// <summary>Anything generated, and the MCP server.</summary>
    public const string Sparkle = """<path d="m11 3.5 1.9 5.6 5.6 1.9-5.6 1.9-1.9 5.6-1.9-5.6-5.6-1.9 5.6-1.9Z" /><path d="M18 15.5v4M20 17.5h-4" />""";
}
