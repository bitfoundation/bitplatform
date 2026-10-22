namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiCollapsible"/> component.
/// </summary>
/// <remarks>
/// The title, the header, the content and whether the panel is open are left out on purpose: they are what makes one
/// panel the one it is.
/// </remarks>
public class BitAiCollapsibleParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiCollapsible"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiCollapsible value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiCollapsible)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the collapsible.
    /// </summary>
    public BitAiCollapsibleClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the collapsible.
    /// </summary>
    public BitAiCollapsibleClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiCollapsible"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiCollapsible"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiCollapsible"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiCollapsible"/>.
    /// </remarks>
    /// <param name="bitAiCollapsible">
    /// The <see cref="BitAiCollapsible"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiCollapsible bitAiCollapsible)
    {
        if (bitAiCollapsible is null) return;

        UpdateBaseParameters(bitAiCollapsible);

        if (Classes is not null)
        {
            bitAiCollapsible.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiCollapsible.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
