namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiCitationMarker"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the markers of an application, or of one part of it, agree on: their look. The citation
/// and the callback are left out on purpose: they are what makes one marker the one it is.
/// </remarks>
public class BitAiCitationMarkerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiCitationMarker"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiCitationMarker value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiCitationMarker)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the citation marker.
    /// </summary>
    public BitAiCitationMarkerClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the citation marker.
    /// </summary>
    public BitAiCitationMarkerClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiCitationMarker"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiCitationMarker"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiCitationMarker"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiCitationMarker"/>.
    /// </remarks>
    /// <param name="bitAiCitationMarker">
    /// The <see cref="BitAiCitationMarker"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiCitationMarker bitAiCitationMarker)
    {
        if (bitAiCitationMarker is null) return;

        UpdateBaseParameters(bitAiCitationMarker);

        if (Classes is not null)
        {
            bitAiCitationMarker.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiCitationMarker.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
