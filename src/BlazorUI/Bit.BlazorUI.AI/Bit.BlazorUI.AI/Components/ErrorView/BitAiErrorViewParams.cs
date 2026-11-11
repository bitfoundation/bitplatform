using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiErrorView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is the look every error view of a region shares. The error, whether a view is compact and the
/// retry handler are left out on purpose: they are what makes one error view the one it is.
/// </remarks>
public class BitAiErrorViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiErrorView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiErrorView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiErrorView)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiErrorViewParams))]
    public BitAiErrorViewParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the error view.
    /// </summary>
    public BitAiErrorViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the error view.
    /// </summary>
    public BitAiErrorViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiErrorView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiErrorView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiErrorView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiErrorView"/>.
    /// </remarks>
    /// <param name="bitAiErrorView">
    /// The <see cref="BitAiErrorView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiErrorView bitAiErrorView)
    {
        if (bitAiErrorView is null) return;

        UpdateBaseParameters(bitAiErrorView);

        if (Classes is not null)
        {
            bitAiErrorView.TakeFromCascade(nameof(Classes), Classes, static e => e.Classes, static (e, v) => e.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiErrorView.TakeFromCascade(nameof(Styles), Styles, static e => e.Styles, static (e, v) => e.Styles = v);
        }
    }
}
