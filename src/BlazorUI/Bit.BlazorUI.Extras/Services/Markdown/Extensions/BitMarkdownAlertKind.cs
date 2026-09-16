namespace Bit.BlazorUI;

/// <summary>The five GitHub alert kinds, in the order GitHub documents them.</summary>
public enum BitMarkdownAlertKind
{
    /// <summary>Useful information the reader should notice even when skimming.</summary>
    Note,

    /// <summary>Optional advice for doing something better.</summary>
    Tip,

    /// <summary>Key information the reader needs to succeed.</summary>
    Important,

    /// <summary>Urgent information that needs immediate attention to avoid a problem.</summary>
    Warning,

    /// <summary>Advice about the risks or negative outcomes of an action.</summary>
    Caution
}
