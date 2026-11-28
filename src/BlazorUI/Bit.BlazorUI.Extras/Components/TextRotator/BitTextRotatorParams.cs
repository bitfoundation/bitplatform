using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTextRotator"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the rotators of an application, or of one part of it, agree on: their pace, the tag they
/// render, their look, and whether they move at all - a <see cref="Paused"/> set once over a region is how a single
/// "pause animations" switch reaches every rotator in it. The items are left out on purpose: they are what makes one
/// rotator the one it is.
/// </remarks>
public partial class BitTextRotatorParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTextRotator"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTextRotator value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTextRotator)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitTextRotatorParams))]
    public BitTextRotatorParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the text rotator.
    /// </summary>
    public BitTextRotatorClassStyles? Classes { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// The time each item is shown for before the next one comes in, in ms.
    /// </summary>
    public double? Interval { get; set; }

    /// <summary>
    /// Holds the rotation on the item that is shown.
    /// </summary>
    public bool? Paused { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the text rotator.
    /// </summary>
    public BitTextRotatorClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTextRotator"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTextRotator"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTextRotator"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTextRotator"/>.
    /// </remarks>
    /// <param name="bitTextRotator">
    /// The <see cref="BitTextRotator"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitTextRotator bitTextRotator)
    {
        if (bitTextRotator is null) return;

        UpdateBaseParameters(bitTextRotator);

        if (Classes is not null)
        {
            bitTextRotator.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (Element.HasValue())
        {
            bitTextRotator.TakeFromCascade(nameof(Element), Element, static t => t.Element, static (t, v) => t.Element = v);
        }

        if (Interval.HasValue)
        {
            bitTextRotator.TakeFromCascade(nameof(Interval), Interval.Value, static t => t.Interval, static (t, v) => t.Interval = v);
        }

        if (Paused.HasValue)
        {
            bitTextRotator.TakeFromCascade(nameof(Paused), Paused.Value, static t => t.Paused, static (t, v) => t.Paused = v);
        }

        if (Styles is not null)
        {
            bitTextRotator.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }
    }
}
