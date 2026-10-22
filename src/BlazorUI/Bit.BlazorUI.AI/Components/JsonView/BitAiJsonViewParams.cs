namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiJsonView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every JSON tree of an app or a section of it agrees on - how much of a tree starts open,
/// and the classes and styles of its parts - rather than the value a single tree shows.
/// </remarks>
public class BitAiJsonViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiJsonView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiJsonView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiJsonView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the JSON view.
    /// </summary>
    public BitAiJsonViewClassStyles? Classes { get; set; }

    /// <summary>
    /// How many levels of the tree are expanded before the containers below them start collapsed.
    /// </summary>
    public int? DefaultExpandDepth { get; set; }

    /// <summary>
    /// The number of entries past which a container starts collapsed whatever its depth.
    /// </summary>
    public int? LargeContainerSize { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the JSON view.
    /// </summary>
    public BitAiJsonViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiJsonView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiJsonView"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiJsonView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiJsonView"/>.
    /// </remarks>
    /// <param name="bitAiJsonView">
    /// The <see cref="BitAiJsonView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiJsonView bitAiJsonView)
    {
        if (bitAiJsonView is null) return;

        UpdateBaseParameters(bitAiJsonView);

        if (Classes is not null)
        {
            bitAiJsonView.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (DefaultExpandDepth.HasValue)
        {
            bitAiJsonView.TakeFromCascade(nameof(DefaultExpandDepth), DefaultExpandDepth.Value, static c => c.DefaultExpandDepth, static (c, v) => c.DefaultExpandDepth = v);
        }

        if (LargeContainerSize.HasValue)
        {
            bitAiJsonView.TakeFromCascade(nameof(LargeContainerSize), LargeContainerSize.Value, static c => c.LargeContainerSize, static (c, v) => c.LargeContainerSize = v);
        }

        if (Styles is not null)
        {
            bitAiJsonView.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
