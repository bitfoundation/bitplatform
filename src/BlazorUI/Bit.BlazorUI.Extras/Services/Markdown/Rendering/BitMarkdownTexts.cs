namespace Bit.BlazorUI;

/// <summary>
/// The words the renderers write into a document themselves, rather than taking them from the
/// source: an alert's title, a footnote's back-link, the name of a table's scroll region. All
/// strings default to English; override the ones you need to localize a rendered document.
/// </summary>
/// <remarks>
/// These are a property of the pipeline, so they are chosen once where the flavors are - a
/// rendered document has no other place to learn what language it is in. Two of them
/// (<see cref="FootnoteBackReference"/> and <see cref="Task"/>) name a numbered thing and take the
/// number through <c>{0}</c>; <see cref="FootnoteBackReferenceOccurrence"/> takes the footnote's
/// number and the citation's through <c>{0}</c> and <c>{1}</c>.
/// </remarks>
public class BitMarkdownTexts
{
    /// <summary>
    /// The shared instance a renderer falls back to when it was handed none. It is process-wide, so
    /// setting a word on it translates every such renderer; a pipeline that needs its own wording
    /// is given it with <c>UseTexts</c> (or by setting the builder's <c>Texts</c>), which starts
    /// from an instance of that builder's own.
    /// </summary>
    public static BitMarkdownTexts Default { get; } = new();

    /// <summary>The title of a <c>&gt; [!NOTE]</c> alert.</summary>
    public string AlertNote { get; set; } = "Note";

    /// <summary>The title of a <c>&gt; [!TIP]</c> alert.</summary>
    public string AlertTip { get; set; } = "Tip";

    /// <summary>The title of a <c>&gt; [!IMPORTANT]</c> alert.</summary>
    public string AlertImportant { get; set; } = "Important";

    /// <summary>The title of a <c>&gt; [!WARNING]</c> alert.</summary>
    public string AlertWarning { get; set; } = "Warning";

    /// <summary>The title of a <c>&gt; [!CAUTION]</c> alert.</summary>
    public string AlertCaution { get; set; } = "Caution";

    /// <summary>The accessible name of the footnotes section.</summary>
    public string Footnotes { get; set; } = "Footnotes";

    /// <summary>The accessible name of a footnote's back-link, given the footnote's number.</summary>
    public string FootnoteBackReference { get; set; } = "Back to reference {0}";

    /// <summary>
    /// The accessible name of one of several back-links on the same footnote, given the footnote's
    /// number and the citation's.
    /// </summary>
    public string FootnoteBackReferenceOccurrence { get; set; } = "Back to reference {0}-{1}";

    /// <summary>The accessible name of the scrollable region a table sits in.</summary>
    public string Table { get; set; } = "Table";

    /// <summary>The accessible name of a heading's permalink, given the heading's text.</summary>
    public string PermalinkTo { get; set; } = "Permalink to {0}";

    /// <summary>The accessible name of a permalink whose heading has no text of its own.</summary>
    public string PermalinkToSection { get; set; } = "Permalink to this section";

    /// <summary>The accessible name of an interactive task-list checkbox, given its number.</summary>
    public string Task { get; set; } = "Task {0}";

    /// <summary>Returns the title of the given alert kind.</summary>
    public string GetAlertTitle(BitMarkdownAlertKind kind) => kind switch
    {
        BitMarkdownAlertKind.Tip => AlertTip,
        BitMarkdownAlertKind.Important => AlertImportant,
        BitMarkdownAlertKind.Warning => AlertWarning,
        BitMarkdownAlertKind.Caution => AlertCaution,
        _ => AlertNote
    };
}
