using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitScrollToTop"/> component.
/// </summary>
/// <remarks>
/// It carries what the scroll to top buttons of an app agree on - where they float, when they are revealed, how they
/// scroll, their icon and their look. The text of each one stays on the button.
/// </remarks>
public partial class BitScrollToTopParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitScrollToTop"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitScrollToTop value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitScrollToTop";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitScrollToTopParams))]
    public BitScrollToTopParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the scroll to top button.
    /// </summary>
    public BitScrollToTopClassStyles? Classes { get; set; }

    /// <summary>
    /// Floats the button over the content, pinned to a corner or an edge of the viewport.
    /// </summary>
    public bool? Float { get; set; }

    /// <summary>
    /// Floats the button over the content, pinned to a corner or an edge of its nearest positioned ancestor.
    /// </summary>
    public bool? FloatAbsolute { get; set; }

    /// <summary>
    /// The inset of a floating button from the edge it is pinned to.
    /// </summary>
    public string? FloatOffset { get; set; }

    /// <summary>
    /// Where a floating button is pinned.
    /// </summary>
    public BitPosition? FloatPosition { get; set; }

    /// <summary>
    /// The icon of the button, provided as custom CSS classes of an external icon library.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon of the button, from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// How far (in pixels) the scroll has to travel from the top before the button is revealed.
    /// </summary>
    public int? RevealOffset { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling the button scrolls back to its top, and reveals itself by.
    /// </summary>
    public string? ScrollTarget { get; set; }

    /// <summary>
    /// Scrolls back to the top smoothly rather than in a single jump.
    /// </summary>
    public bool? Smooth { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the scroll to top button.
    /// </summary>
    public BitScrollToTopClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitScrollToTop"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitScrollToTop"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitScrollToTop"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitScrollToTop"/>.
    /// </remarks>
    /// <param name="bitScrollToTop">
    /// The <see cref="BitScrollToTop"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitScrollToTop bitScrollToTop)
    {
        if (bitScrollToTop is null) return;

        UpdateBaseParameters(bitScrollToTop);

        if (Classes is not null)
        {
            bitScrollToTop.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Float.HasValue)
        {
            bitScrollToTop.TakeFromCascade(nameof(Float), Float.Value, static s => s.Float, static (s, v) => s.Float = v);
        }

        if (FloatAbsolute.HasValue)
        {
            bitScrollToTop.TakeFromCascade(nameof(FloatAbsolute), FloatAbsolute.Value, static s => s.FloatAbsolute, static (s, v) => s.FloatAbsolute = v);
        }

        if (FloatOffset.HasValue())
        {
            bitScrollToTop.TakeFromCascade(nameof(FloatOffset), FloatOffset, static s => s.FloatOffset, static (s, v) => s.FloatOffset = v);
        }

        if (FloatPosition.HasValue)
        {
            bitScrollToTop.TakeFromCascade(nameof(FloatPosition), FloatPosition.Value, static s => s.FloatPosition, static (s, v) => s.FloatPosition = v);
        }

        if (Icon is not null)
        {
            bitScrollToTop.TakeFromCascade(nameof(Icon), Icon, static s => s.Icon, static (s, v) => s.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitScrollToTop.TakeFromCascade(nameof(IconName), IconName, static s => s.IconName, static (s, v) => s.IconName = v);
        }

        if (RevealOffset.HasValue)
        {
            bitScrollToTop.TakeFromCascade(nameof(RevealOffset), RevealOffset.Value, static s => s.RevealOffset, static (s, v) => s.RevealOffset = v);
        }

        if (ScrollTarget.HasValue())
        {
            bitScrollToTop.TakeFromCascade(nameof(ScrollTarget), ScrollTarget, static s => s.ScrollTarget, static (s, v) => s.ScrollTarget = v);
        }

        if (Smooth.HasValue)
        {
            bitScrollToTop.TakeFromCascade(nameof(Smooth), Smooth.Value, static s => s.Smooth, static (s, v) => s.Smooth = v);
        }

        if (Styles is not null)
        {
            bitScrollToTop.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }
    }
}
