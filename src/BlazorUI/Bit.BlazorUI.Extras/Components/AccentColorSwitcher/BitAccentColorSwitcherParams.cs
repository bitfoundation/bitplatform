namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAccentColorSwitcher"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the accent color switchers of an application agree on: the look of their parts and the
/// app-wide <see cref="Config"/> - which the switcher's documentation asks to be stated once and handed to every
/// switcher alike, and which a BitParams around the layout does in one place. The <see cref="BitAccentColorSwitcher.OnChange"/>
/// callback is deliberately not here: it is what makes one switcher the one it is.
/// </remarks>
public class BitAccentColorSwitcherParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAccentColorSwitcher"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAccentColorSwitcher value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAccentColorSwitcher)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the switcher.
    /// </summary>
    public BitAccentColorSwitcherClassStyles? Classes { get; set; }

    /// <summary>
    /// The app-wide accent configuration: the accents offered as swatches, the stores the picked one is persisted to,
    /// and the first-paint strategy to maintain when applying it.
    /// </summary>
    public BitAccentColorConfig? Config { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the switcher.
    /// </summary>
    public BitAccentColorSwitcherClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAccentColorSwitcher"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAccentColorSwitcher"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAccentColorSwitcher"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAccentColorSwitcher"/>.
    /// </remarks>
    /// <param name="bitAccentColorSwitcher">
    /// The <see cref="BitAccentColorSwitcher"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAccentColorSwitcher bitAccentColorSwitcher)
    {
        if (bitAccentColorSwitcher is null) return;

        UpdateBaseParameters(bitAccentColorSwitcher);

        if (Classes is not null)
        {
            bitAccentColorSwitcher.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Config is not null)
        {
            bitAccentColorSwitcher.TakeFromCascade(nameof(Config), Config, static s => s.Config, static (s, v) => s.Config = v);
        }

        if (Styles is not null)
        {
            bitAccentColorSwitcher.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }
    }
}
