namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiEmptyState"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the empty states of an application agree on: the words and the icon they greet the user
/// with, and their classes and styles. The suggestions, the templates and the callback are left out on purpose: they
/// are what makes one empty state the one it is.
/// </remarks>
public class BitAiEmptyStateParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiEmptyState"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiEmptyState value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiEmptyState)}";



    public string Name => ParamName;



    /// <summary>
    /// The supporting line under the heading.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the empty state.
    /// </summary>
    public BitAiEmptyStateClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon of the empty state, provided as custom CSS classes of an external icon library.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon of the empty state, from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the empty state.
    /// </summary>
    public BitAiEmptyStateClassStyles? Styles { get; set; }

    /// <summary>
    /// The heading.
    /// </summary>
    public string? Title { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiEmptyState"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiEmptyState"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiEmptyState"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiEmptyState"/>.
    /// </remarks>
    /// <param name="bitAiEmptyState">
    /// The <see cref="BitAiEmptyState"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiEmptyState bitAiEmptyState)
    {
        if (bitAiEmptyState is null) return;

        UpdateBaseParameters(bitAiEmptyState);

        if (Body.HasValue())
        {
            bitAiEmptyState.TakeFromCascade(nameof(Body), Body, static e => e.Body, static (e, v) => e.Body = v);
        }

        if (Classes is not null)
        {
            bitAiEmptyState.TakeFromCascade(nameof(Classes), Classes, static e => e.Classes, static (e, v) => e.Classes = v);
        }

        if (Icon is not null)
        {
            bitAiEmptyState.TakeFromCascade(nameof(Icon), Icon, static e => e.Icon, static (e, v) => e.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitAiEmptyState.TakeFromCascade(nameof(IconName), IconName, static e => e.IconName, static (e, v) => e.IconName = v);
        }

        if (Styles is not null)
        {
            bitAiEmptyState.TakeFromCascade(nameof(Styles), Styles, static e => e.Styles, static (e, v) => e.Styles = v);
        }

        if (Title.HasValue())
        {
            bitAiEmptyState.TakeFromCascade(nameof(Title), Title, static e => e.Title, static (e, v) => e.Title = v);
        }
    }
}
