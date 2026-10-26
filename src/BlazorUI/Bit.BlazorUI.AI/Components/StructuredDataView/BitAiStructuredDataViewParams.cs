using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiStructuredDataView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every structured data view of an app or a section of it agrees on - the classes and
/// styles of its parts - rather than the part and the message a single view shows.
/// </remarks>
public class BitAiStructuredDataViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiStructuredDataView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiStructuredDataView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiStructuredDataView)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiStructuredDataViewParams))]
    public BitAiStructuredDataViewParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the structured data view.
    /// </summary>
    public BitAiStructuredDataViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the structured data view.
    /// </summary>
    public BitAiStructuredDataViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiStructuredDataView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiStructuredDataView"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiStructuredDataView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiStructuredDataView"/>.
    /// </remarks>
    /// <param name="bitAiStructuredDataView">
    /// The <see cref="BitAiStructuredDataView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiStructuredDataView bitAiStructuredDataView)
    {
        if (bitAiStructuredDataView is null) return;

        UpdateBaseParameters(bitAiStructuredDataView);

        if (Classes is not null)
        {
            bitAiStructuredDataView.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiStructuredDataView.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
