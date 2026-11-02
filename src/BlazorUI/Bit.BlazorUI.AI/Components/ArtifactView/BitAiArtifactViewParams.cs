using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiArtifactView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every artifact panel of an app or a section of it agrees on - the classes and styles of
/// its parts - rather than the artifact and the version a single panel shows.
/// </remarks>
public class BitAiArtifactViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiArtifactView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiArtifactView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiArtifactView)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiArtifactViewParams))]
    public BitAiArtifactViewParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the artifact view.
    /// </summary>
    public BitAiArtifactViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the artifact view.
    /// </summary>
    public BitAiArtifactViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiArtifactView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiArtifactView"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiArtifactView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiArtifactView"/>.
    /// </remarks>
    /// <param name="bitAiArtifactView">
    /// The <see cref="BitAiArtifactView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiArtifactView bitAiArtifactView)
    {
        if (bitAiArtifactView is null) return;

        UpdateBaseParameters(bitAiArtifactView);

        if (Classes is not null)
        {
            bitAiArtifactView.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiArtifactView.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
