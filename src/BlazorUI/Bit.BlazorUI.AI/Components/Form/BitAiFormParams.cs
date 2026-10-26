using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiForm"/> component.
/// </summary>
/// <remarks>
/// The definition, the state of a form (busy, submitted) and its callbacks are left out on purpose: they are what makes
/// one form the one it is.
/// </remarks>
public class BitAiFormParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiForm"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiForm value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiForm)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiFormParams))]
    public BitAiFormParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the form.
    /// </summary>
    public BitAiFormClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the form.
    /// </summary>
    public BitAiFormClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiForm"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiForm"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiForm"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiForm"/>.
    /// </remarks>
    /// <param name="bitAiForm">
    /// The <see cref="BitAiForm"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiForm bitAiForm)
    {
        if (bitAiForm is null) return;

        UpdateBaseParameters(bitAiForm);

        if (Classes is not null)
        {
            bitAiForm.TakeFromCascade(nameof(Classes), Classes, static f => f.Classes, static (f, v) => f.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiForm.TakeFromCascade(nameof(Styles), Styles, static f => f.Styles, static (f, v) => f.Styles = v);
        }
    }
}
