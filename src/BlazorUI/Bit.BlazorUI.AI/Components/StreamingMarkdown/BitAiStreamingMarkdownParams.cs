namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiStreamingMarkdown"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the streaming Markdown of an app, or of one part of it, agrees on - the look of its
/// parts. The text, whether it is still streaming and its citations are left out on purpose: they are what makes one
/// instance the one it is.
/// </remarks>
public class BitAiStreamingMarkdownParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiStreamingMarkdown"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiStreamingMarkdown value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiStreamingMarkdown)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the streaming Markdown.
    /// </summary>
    public BitAiStreamingMarkdownClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the streaming Markdown.
    /// </summary>
    public BitAiStreamingMarkdownClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiStreamingMarkdown"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiStreamingMarkdown"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiStreamingMarkdown"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiStreamingMarkdown"/>.
    /// </remarks>
    /// <param name="bitAiStreamingMarkdown">
    /// The <see cref="BitAiStreamingMarkdown"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiStreamingMarkdown bitAiStreamingMarkdown)
    {
        if (bitAiStreamingMarkdown is null) return;

        UpdateBaseParameters(bitAiStreamingMarkdown);

        if (Classes is not null)
        {
            bitAiStreamingMarkdown.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiStreamingMarkdown.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }
    }
}
