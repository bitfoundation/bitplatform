using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCodeBlock"/> component.
/// </summary>
/// <remarks>
/// It carries what the code blocks of an app agree on - their look, their copy button and its words. The code, its title
/// and its language stay on each code block.
/// </remarks>
public partial class BitCodeBlockParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCodeBlock"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCodeBlock value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitCodeBlock";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCodeBlockParams))]
    public BitCodeBlockParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the code block.
    /// </summary>
    public BitCodeBlockClassStyles? Classes { get; set; }

    /// <summary>
    /// The name of the copy button while what it copied is on the clipboard.
    /// </summary>
    public string? CopiedText { get; set; }

    /// <summary>
    /// Copies the code when the code itself is clicked, as well as through the copy button.
    /// </summary>
    public bool? CopyOnClick { get; set; }

    /// <summary>
    /// The name of the copy button.
    /// </summary>
    public string? CopyText { get; set; }

    /// <summary>
    /// The height (any CSS length) past which the code scrolls.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Shows the button that copies the code.
    /// </summary>
    public bool? ShowCopyButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the code block.
    /// </summary>
    public BitCodeBlockClassStyles? Styles { get; set; }

    /// <summary>
    /// Draws the code block as a terminal window.
    /// </summary>
    public bool? Terminal { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCodeBlock"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCodeBlock"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCodeBlock"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitCodeBlock"/>.
    /// </remarks>
    /// <param name="bitCodeBlock">
    /// The <see cref="BitCodeBlock"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCodeBlock bitCodeBlock)
    {
        if (bitCodeBlock is null) return;

        UpdateBaseParameters(bitCodeBlock);

        if (Classes is not null)
        {
            bitCodeBlock.TakeFromCascade(nameof(Classes), Classes, static e => e.Classes, static (e, v) => e.Classes = v);
        }

        if (CopiedText.HasValue())
        {
            bitCodeBlock.TakeFromCascade(nameof(CopiedText), CopiedText, static e => e.CopiedText, static (e, v) => e.CopiedText = v);
        }

        if (CopyOnClick.HasValue)
        {
            bitCodeBlock.TakeFromCascade(nameof(CopyOnClick), CopyOnClick.Value, static e => e.CopyOnClick, static (e, v) => e.CopyOnClick = v);
        }

        if (CopyText.HasValue())
        {
            bitCodeBlock.TakeFromCascade(nameof(CopyText), CopyText, static e => e.CopyText, static (e, v) => e.CopyText = v);
        }

        if (MaxHeight.HasValue())
        {
            bitCodeBlock.TakeFromCascade(nameof(MaxHeight), MaxHeight, static e => e.MaxHeight, static (e, v) => e.MaxHeight = v);
        }

        if (ShowCopyButton.HasValue)
        {
            bitCodeBlock.TakeFromCascade(nameof(ShowCopyButton), ShowCopyButton.Value, static e => e.ShowCopyButton, static (e, v) => e.ShowCopyButton = v);
        }

        if (Styles is not null)
        {
            bitCodeBlock.TakeFromCascade(nameof(Styles), Styles, static e => e.Styles, static (e, v) => e.Styles = v);
        }

        if (Terminal.HasValue)
        {
            bitCodeBlock.TakeFromCascade(nameof(Terminal), Terminal.Value, static e => e.Terminal, static (e, v) => e.Terminal = v);
        }
    }
}
