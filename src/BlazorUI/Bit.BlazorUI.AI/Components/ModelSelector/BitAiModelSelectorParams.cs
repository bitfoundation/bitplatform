namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiModelSelector"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the model selectors of an application, or of one part of it, agree on: the models they offer
/// and how they filter them, and their look. The selection and its callbacks are left out on purpose: they are what makes
/// one selector the one it is.
/// </remarks>
public class BitAiModelSelectorParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiModelSelector"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiModelSelector value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiModelSelector)}";



    public string Name => ParamName;



    /// <summary>
    /// The catalog of the models to offer.
    /// </summary>
    public IBitAiModelCatalog? Catalog { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the model selector.
    /// </summary>
    public BitAiModelSelectorClassStyles? Classes { get; set; }

    /// <summary>
    /// The models to offer, in place of those of the catalog.
    /// </summary>
    public IReadOnlyList<BitAiModel>? Models { get; set; }

    /// <summary>
    /// Offers only the models that declare every one of these capabilities.
    /// </summary>
    public BitAiModelCapabilities? RequiredCapabilities { get; set; }

    /// <summary>
    /// Offers the models marked deprecated too, labelled as such.
    /// </summary>
    public bool? ShowDeprecated { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the model selector.
    /// </summary>
    public BitAiModelSelectorClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiModelSelector"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiModelSelector"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiModelSelector"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiModelSelector"/>.
    /// </remarks>
    /// <param name="bitAiModelSelector">
    /// The <see cref="BitAiModelSelector"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiModelSelector bitAiModelSelector)
    {
        if (bitAiModelSelector is null) return;

        UpdateBaseParameters(bitAiModelSelector);

        if (Catalog is not null)
        {
            bitAiModelSelector.TakeFromCascade(nameof(Catalog), Catalog, static s => s.Catalog, static (s, v) => s.Catalog = v);
        }

        if (Classes is not null)
        {
            bitAiModelSelector.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Models is not null)
        {
            bitAiModelSelector.TakeFromCascade(nameof(Models), Models, static s => s.Models, static (s, v) => s.Models = v);
        }

        if (RequiredCapabilities.HasValue)
        {
            bitAiModelSelector.TakeFromCascade(nameof(RequiredCapabilities), RequiredCapabilities.Value, static s => s.RequiredCapabilities, static (s, v) => s.RequiredCapabilities = v);
        }

        if (ShowDeprecated.HasValue)
        {
            bitAiModelSelector.TakeFromCascade(nameof(ShowDeprecated), ShowDeprecated.Value, static s => s.ShowDeprecated, static (s, v) => s.ShowDeprecated = v);
        }

        if (Styles is not null)
        {
            bitAiModelSelector.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }
    }
}
