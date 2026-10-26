using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiToolResultView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is the look every tool result of a region shares. The result itself is left out on purpose: it
/// is what makes one view the one it is.
/// </remarks>
public class BitAiToolResultViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiToolResultView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiToolResultView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiToolResultView)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiToolResultViewParams))]
    public BitAiToolResultViewParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the tool result view.
    /// </summary>
    public BitAiToolResultViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tool result view.
    /// </summary>
    public BitAiToolResultViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiToolResultView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiToolResultView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiToolResultView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiToolResultView"/>.
    /// </remarks>
    /// <param name="bitAiToolResultView">
    /// The <see cref="BitAiToolResultView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiToolResultView bitAiToolResultView)
    {
        if (bitAiToolResultView is null) return;

        UpdateBaseParameters(bitAiToolResultView);

        if (Classes is not null)
        {
            bitAiToolResultView.TakeFromCascade(nameof(Classes), Classes, static r => r.Classes, static (r, v) => r.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiToolResultView.TakeFromCascade(nameof(Styles), Styles, static r => r.Styles, static (r, v) => r.Styles = v);
        }
    }
}
