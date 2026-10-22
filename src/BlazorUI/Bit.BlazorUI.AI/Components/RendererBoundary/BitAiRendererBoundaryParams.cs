namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiRendererBoundary"/> component.
/// </summary>
/// <remarks>
/// What belongs here is the look every boundary of a region shares. The content a boundary protects and the label of
/// its notice are left out on purpose: they are what makes one boundary the one it is.
/// </remarks>
public class BitAiRendererBoundaryParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiRendererBoundary"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiRendererBoundary value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiRendererBoundary)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the renderer boundary.
    /// </summary>
    public BitAiRendererBoundaryClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the renderer boundary.
    /// </summary>
    public BitAiRendererBoundaryClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiRendererBoundary"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiRendererBoundary"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiRendererBoundary"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiRendererBoundary"/>.
    /// </remarks>
    /// <param name="bitAiRendererBoundary">
    /// The <see cref="BitAiRendererBoundary"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiRendererBoundary bitAiRendererBoundary)
    {
        if (bitAiRendererBoundary is null) return;

        UpdateBaseParameters(bitAiRendererBoundary);

        if (Classes is not null)
        {
            bitAiRendererBoundary.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiRendererBoundary.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }
    }
}
