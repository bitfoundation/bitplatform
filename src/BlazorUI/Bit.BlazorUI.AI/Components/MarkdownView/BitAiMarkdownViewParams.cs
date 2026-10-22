namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiMarkdownView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the Markdown views of an app, or of one part of it, agree on - the look of their parts.
/// The document, its citations and the caret are left out on purpose: they are what makes one view the one it is.
/// </remarks>
public class BitAiMarkdownViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiMarkdownView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiMarkdownView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiMarkdownView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the Markdown view.
    /// </summary>
    public BitAiMarkdownViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the Markdown view.
    /// </summary>
    public BitAiMarkdownViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiMarkdownView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiMarkdownView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiMarkdownView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiMarkdownView"/>.
    /// </remarks>
    /// <param name="bitAiMarkdownView">
    /// The <see cref="BitAiMarkdownView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiMarkdownView bitAiMarkdownView)
    {
        if (bitAiMarkdownView is null) return;

        UpdateBaseParameters(bitAiMarkdownView);

        if (Classes is not null)
        {
            bitAiMarkdownView.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiMarkdownView.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }
    }
}
