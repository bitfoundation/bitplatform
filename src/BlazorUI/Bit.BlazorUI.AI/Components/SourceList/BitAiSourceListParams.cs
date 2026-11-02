using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiSourceList"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the source lists of an application, or of one part of it, agree on: the heading and the
/// look. The citations and the callback are left out on purpose: they are what makes one list the one it is.
/// </remarks>
public class BitAiSourceListParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiSourceList"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiSourceList value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiSourceList)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiSourceListParams))]
    public BitAiSourceListParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the source list.
    /// </summary>
    public BitAiSourceListClassStyles? Classes { get; set; }

    /// <summary>
    /// Shows the heading above the list.
    /// </summary>
    public bool? ShowHeading { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the source list.
    /// </summary>
    public BitAiSourceListClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiSourceList"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiSourceList"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiSourceList"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiSourceList"/>.
    /// </remarks>
    /// <param name="bitAiSourceList">
    /// The <see cref="BitAiSourceList"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiSourceList bitAiSourceList)
    {
        if (bitAiSourceList is null) return;

        UpdateBaseParameters(bitAiSourceList);

        if (Classes is not null)
        {
            bitAiSourceList.TakeFromCascade(nameof(Classes), Classes, static l => l.Classes, static (l, v) => l.Classes = v);
        }

        if (ShowHeading.HasValue)
        {
            bitAiSourceList.TakeFromCascade(nameof(ShowHeading), ShowHeading.Value, static l => l.ShowHeading, static (l, v) => l.ShowHeading = v);
        }

        if (Styles is not null)
        {
            bitAiSourceList.TakeFromCascade(nameof(Styles), Styles, static l => l.Styles, static (l, v) => l.Styles = v);
        }
    }
}
