using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiImagePartView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the image parts of an app, or of one part of it, agree on - the look of their parts. The
/// part is left out on purpose: it is what makes one instance the one it is.
/// </remarks>
public class BitAiImagePartViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiImagePartView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiImagePartView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiImagePartView)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiImagePartViewParams))]
    public BitAiImagePartViewParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the image part view.
    /// </summary>
    public BitAiImagePartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the image part view.
    /// </summary>
    public BitAiImagePartViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiImagePartView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiImagePartView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiImagePartView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiImagePartView"/>.
    /// </remarks>
    /// <param name="bitAiImagePartView">
    /// The <see cref="BitAiImagePartView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiImagePartView bitAiImagePartView)
    {
        if (bitAiImagePartView is null) return;

        UpdateBaseParameters(bitAiImagePartView);

        if (Classes is not null)
        {
            bitAiImagePartView.TakeFromCascade(nameof(Classes), Classes, static i => i.Classes, static (i, v) => i.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiImagePartView.TakeFromCascade(nameof(Styles), Styles, static i => i.Styles, static (i, v) => i.Styles = v);
        }
    }
}
