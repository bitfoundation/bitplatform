using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitContainer"/> component.
/// </summary>
public partial class BitContainerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitContainer"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitContainer value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitContainer)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitContainerParams))]
    public BitContainerParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets the custom html element used for the root node. The default is "div".
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// The maximum width of the container, in which its content is then centered.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Removes the inline paddings (the gutters) of the container.
    /// </summary>
    public bool? NoGutter { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitContainer"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitContainer"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitContainer"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitContainer"/>.
    /// </remarks>
    /// <param name="bitContainer">
    /// The <see cref="BitContainer"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitContainer bitContainer)
    {
        if (bitContainer is null) return;

        UpdateBaseParameters(bitContainer);

        if (Element.HasValue())
        {
            bitContainer.TakeFromCascade(nameof(Element), Element, static c => c.Element, static (c, v) => c.Element = v);
        }

        if (MaxWidth.HasValue())
        {
            bitContainer.TakeFromCascade(nameof(MaxWidth), MaxWidth, static c => c.MaxWidth, static (c, v) => c.MaxWidth = v);
        }

        if (NoGutter.HasValue)
        {
            bitContainer.TakeFromCascade(nameof(NoGutter), NoGutter.Value, static c => c.NoGutter, static (c, v) => c.NoGutter = v);
        }
    }
}
