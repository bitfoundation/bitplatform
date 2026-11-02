using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiTextPartView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the text parts of an app, or of one part of it, agree on - the look of their parts. The
/// part, its message and the citation callback are left out on purpose: they are what makes one instance the one it is.
/// </remarks>
public class BitAiTextPartViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiTextPartView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiTextPartView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiTextPartView)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiTextPartViewParams))]
    public BitAiTextPartViewParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the text part view.
    /// </summary>
    public BitAiTextPartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the text part view.
    /// </summary>
    public BitAiTextPartViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiTextPartView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiTextPartView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiTextPartView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiTextPartView"/>.
    /// </remarks>
    /// <param name="bitAiTextPartView">
    /// The <see cref="BitAiTextPartView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiTextPartView bitAiTextPartView)
    {
        if (bitAiTextPartView is null) return;

        UpdateBaseParameters(bitAiTextPartView);

        if (Classes is not null)
        {
            bitAiTextPartView.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiTextPartView.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }
    }
}
