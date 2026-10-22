namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiArtifactPartView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every artifact part of an app or a section of it agrees on - the classes and styles of
/// its parts - rather than the part and the conversation a single view resolves.
/// </remarks>
public class BitAiArtifactPartViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiArtifactPartView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiArtifactPartView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiArtifactPartView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the artifact part view.
    /// </summary>
    public BitAiArtifactPartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the artifact part view.
    /// </summary>
    public BitAiArtifactPartViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiArtifactPartView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiArtifactPartView"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiArtifactPartView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiArtifactPartView"/>.
    /// </remarks>
    /// <param name="bitAiArtifactPartView">
    /// The <see cref="BitAiArtifactPartView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiArtifactPartView bitAiArtifactPartView)
    {
        if (bitAiArtifactPartView is null) return;

        UpdateBaseParameters(bitAiArtifactPartView);

        if (Classes is not null)
        {
            bitAiArtifactPartView.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiArtifactPartView.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
