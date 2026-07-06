namespace Bit.BlazorUI;

/// <summary>Provides localized labels and tooltips for the BitRichTextEditor's controls.</summary>
public interface IBitRichTextEditorLocalizer
{
    /// <summary>Returns the localized string for the given key, or null to use the default.</summary>
    string? this[string key] { get; }
}
