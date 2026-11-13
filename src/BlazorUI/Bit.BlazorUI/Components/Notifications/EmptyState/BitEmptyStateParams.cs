using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitEmptyState"/> component.
/// </summary>
/// <remarks>
/// It carries what the empty states of an app agree on - their look, their icon, the level of their heading. The words
/// and the actions of each one stay on the empty state.
/// </remarks>
public class BitEmptyStateParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitEmptyState"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitEmptyState value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitEmptyState";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitEmptyStateParams))]
    public BitEmptyStateParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the empty state.
    /// </summary>
    public BitEmptyStateClassStyles? Classes { get; set; }

    /// <summary>
    /// Makes the title a heading of this level (1 to 6) in the outline of the page.
    /// </summary>
    public int? HeadingLevel { get; set; }

    /// <summary>
    /// The icon above the title, provided as custom CSS classes of an external icon library.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon above the title, from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The custom content above the title, which takes the place of the icon.
    /// </summary>
    public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the empty state.
    /// </summary>
    public BitEmptyStateClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitEmptyState"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitEmptyState"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitEmptyState"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitEmptyState"/>.
    /// </remarks>
    /// <param name="bitEmptyState">
    /// The <see cref="BitEmptyState"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitEmptyState bitEmptyState)
    {
        if (bitEmptyState is null) return;

        UpdateBaseParameters(bitEmptyState);

        if (Classes is not null)
        {
            bitEmptyState.TakeFromCascade(nameof(Classes), Classes, static e => e.Classes, static (e, v) => e.Classes = v);
        }

        if (HeadingLevel.HasValue)
        {
            bitEmptyState.TakeFromCascade(nameof(HeadingLevel), HeadingLevel.Value, static e => e.HeadingLevel, static (e, v) => e.HeadingLevel = v);
        }

        if (Icon is not null)
        {
            bitEmptyState.TakeFromCascade(nameof(Icon), Icon, static e => e.Icon, static (e, v) => e.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitEmptyState.TakeFromCascade(nameof(IconName), IconName, static e => e.IconName, static (e, v) => e.IconName = v);
        }

        if (IconTemplate is not null)
        {
            bitEmptyState.TakeFromCascade(nameof(IconTemplate), IconTemplate, static e => e.IconTemplate, static (e, v) => e.IconTemplate = v);
        }

        if (Styles is not null)
        {
            bitEmptyState.TakeFromCascade(nameof(Styles), Styles, static e => e.Styles, static (e, v) => e.Styles = v);
        }
    }
}
