namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiScrollToLatest"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the scroll buttons of an application agree on: their text and their classes and styles.
/// Whether one is shown and what pressing it does are left out on purpose: they are what makes one button the one it is.
/// </remarks>
public class BitAiScrollToLatestParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiScrollToLatest"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiScrollToLatest value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiScrollToLatest)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the scroll to latest.
    /// </summary>
    public BitAiScrollToLatestClassStyles? Classes { get; set; }

    /// <summary>
    /// The text of the button, in place of the one of the string table.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the scroll to latest.
    /// </summary>
    public BitAiScrollToLatestClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiScrollToLatest"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiScrollToLatest"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiScrollToLatest"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiScrollToLatest"/>.
    /// </remarks>
    /// <param name="bitAiScrollToLatest">
    /// The <see cref="BitAiScrollToLatest"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiScrollToLatest bitAiScrollToLatest)
    {
        if (bitAiScrollToLatest is null) return;

        UpdateBaseParameters(bitAiScrollToLatest);

        if (Classes is not null)
        {
            bitAiScrollToLatest.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Label.HasValue())
        {
            bitAiScrollToLatest.TakeFromCascade(nameof(Label), Label, static s => s.Label, static (s, v) => s.Label = v);
        }

        if (Styles is not null)
        {
            bitAiScrollToLatest.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }
    }
}
