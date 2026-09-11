namespace Bit.BlazorUI;

/// <summary>
/// Snapshot of the current selection's formatting, reported by the JS bridge and used to
/// highlight active toolbar buttons. Properties missing from the JS object default to inactive.
/// </summary>
public sealed class BitRichTextEditorSelectionState
{
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool StrikeThrough { get; set; }
    public bool OrderedList { get; set; }
    public bool UnorderedList { get; set; }
    public bool JustifyLeft { get; set; }
    public bool JustifyCenter { get; set; }
    public bool JustifyRight { get; set; }
    public bool JustifyFull { get; set; }

    /// <summary>The current block tag (e.g. "p", "h1", "blockquote", "pre"), lowercase, or empty when no active block is reported.</summary>
    public string Block { get; set; } = "";

    public bool Subscript { get; set; }
    public bool Superscript { get; set; }

    /// <summary>Active foreground color of the selection, or null when mixed/none.</summary>
    public string? ForeColor { get; set; }

    /// <summary>Active background/highlight color of the selection, or null when mixed/none.</summary>
    public string? BackColor { get; set; }

    /// <summary>Active font family, or null when the selection spans multiple families.</summary>
    public string? FontName { get; set; }

    /// <summary>Active font size, or null when the selection spans multiple sizes.</summary>
    public string? FontSize { get; set; }

    /// <summary>Text direction of the selected block ("ltr"/"rtl"), or null.</summary>
    public string? Direction { get; set; }

    /// <summary>True when the selection sits inside a single hyperlink.</summary>
    public bool InLink { get; set; }

    /// <summary>The href of the link under the selection, or null when none/multiple.</summary>
    public string? LinkHref { get; set; }

    /// <summary>True when the link under the selection opens in a new tab (target="_blank").</summary>
    public bool LinkNewTab { get; set; }

    /// <summary>True when the selection sits inside an inline code span (not a code block).</summary>
    public bool InlineCode { get; set; }

    /// <summary>True when the selection sits inside a checklist item.</summary>
    public bool TaskList { get; set; }

    /// <summary>True when the selection sits inside a table cell, enabling the table operations.</summary>
    public bool InTable { get; set; }

    /// <summary>True when a non-empty range inside the editor is selected.</summary>
    public bool HasSelection { get; set; }

    /// <summary>Top of the selection rectangle, in pixels relative to the component root.</summary>
    public double SelectionTop { get; set; }

    /// <summary>Left edge of the selection rectangle, in pixels relative to the component root.</summary>
    public double SelectionLeft { get; set; }

    /// <summary>Width of the selection rectangle in pixels.</summary>
    public double SelectionWidth { get; set; }

    /// <summary>Height of the selection rectangle in pixels.</summary>
    public double SelectionHeight { get; set; }

    /// <summary>True when an image inside the editor is selected.</summary>
    public bool ImageSelected { get; set; }

    /// <summary>Alignment of the selected image ("left", "center", "right"), or null when it flows inline.</summary>
    public string? ImageAlign { get; set; }

    /// <summary>Source of the selected image, or null when no image is selected.</summary>
    public string? ImageSrc { get; set; }

    /// <summary>Alternative text of the selected image (empty when it has none), or null when no image is selected.</summary>
    public string? ImageAlt { get; set; }
}
