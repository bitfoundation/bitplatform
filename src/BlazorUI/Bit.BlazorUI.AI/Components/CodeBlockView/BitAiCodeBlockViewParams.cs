namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiCodeBlockView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the code blocks of an app, or of one part of it, agree on - whether they show their
/// header bar, and the look of their parts. The code, its language and file name and whether it is still streaming
/// are left out on purpose: they are what makes one code block the one it is.
/// </remarks>
public class BitAiCodeBlockViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiCodeBlockView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiCodeBlockView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiCodeBlockView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the code block.
    /// </summary>
    public BitAiCodeBlockViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Shows the header bar with the language and the copy button.
    /// </summary>
    public bool? ShowHeader { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the code block.
    /// </summary>
    public BitAiCodeBlockViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiCodeBlockView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiCodeBlockView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiCodeBlockView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiCodeBlockView"/>.
    /// </remarks>
    /// <param name="bitAiCodeBlockView">
    /// The <see cref="BitAiCodeBlockView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiCodeBlockView bitAiCodeBlockView)
    {
        if (bitAiCodeBlockView is null) return;

        UpdateBaseParameters(bitAiCodeBlockView);

        if (Classes is not null)
        {
            bitAiCodeBlockView.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (ShowHeader.HasValue)
        {
            bitAiCodeBlockView.TakeFromCascade(nameof(ShowHeader), ShowHeader.Value, static c => c.ShowHeader, static (c, v) => c.ShowHeader = v);
        }

        if (Styles is not null)
        {
            bitAiCodeBlockView.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
