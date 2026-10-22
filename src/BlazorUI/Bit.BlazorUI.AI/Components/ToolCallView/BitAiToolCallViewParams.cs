namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiToolCallView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is the look every tool card of a region shares. The call, its message and the session are left
/// out on purpose: they are what makes one card the one it is. Whether the cards start collapsed is an option of the
/// whole conversation (<see cref="BitAiUiOptions.CollapseToolCalls"/>).
/// </remarks>
public class BitAiToolCallViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiToolCallView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiToolCallView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiToolCallView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the tool call view.
    /// </summary>
    public BitAiToolCallViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tool call view.
    /// </summary>
    public BitAiToolCallViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiToolCallView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiToolCallView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiToolCallView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiToolCallView"/>.
    /// </remarks>
    /// <param name="bitAiToolCallView">
    /// The <see cref="BitAiToolCallView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiToolCallView bitAiToolCallView)
    {
        if (bitAiToolCallView is null) return;

        UpdateBaseParameters(bitAiToolCallView);

        if (Classes is not null)
        {
            bitAiToolCallView.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiToolCallView.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }
    }
}
