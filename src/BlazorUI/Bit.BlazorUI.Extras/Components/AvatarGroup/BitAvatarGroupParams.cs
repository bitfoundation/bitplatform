using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAvatarGroup"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the avatar groups of an app agree on: their size, layout and look, how many avatars they
/// draw and how they name their overflow. The people and what a click on them does are the state of a single group.
/// </remarks>
public partial class BitAvatarGroupParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAvatarGroup"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAvatarGroup value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitAvatarGroup";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAvatarGroupParams))]
    public BitAvatarGroupParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the avatar group.
    /// </summary>
    public BitAvatarGroupClassStyles? Classes { get; set; }

    /// <summary>
    /// The number of avatars drawn before the rest are counted by the overflow coin.
    /// </summary>
    public int? MaxDisplayed { get; set; }

    /// <summary>
    /// The name of the overflow coin to the screen readers, as a composite format string.
    /// </summary>
    public string? OverflowAriaLabelFormat { get; set; }

    /// <summary>
    /// The size of the avatars.
    /// </summary>
    public BitPersonaSize? Size { get; set; }

    /// <summary>
    /// Lays the avatars out side by side instead of overlapping each other.
    /// </summary>
    public bool? Spread { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the avatar group.
    /// </summary>
    public BitAvatarGroupClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAvatarGroup"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAvatarGroup"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAvatarGroup"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAvatarGroup"/>.
    /// </remarks>
    /// <param name="bitAvatarGroup">
    /// The <see cref="BitAvatarGroup"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAvatarGroup bitAvatarGroup)
    {
        if (bitAvatarGroup is null) return;

        UpdateBaseParameters(bitAvatarGroup);

        if (Classes is not null)
        {
            bitAvatarGroup.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (MaxDisplayed.HasValue)
        {
            bitAvatarGroup.TakeFromCascade(nameof(MaxDisplayed), MaxDisplayed, static c => c.MaxDisplayed, static (c, v) => c.MaxDisplayed = v);
        }

        if (OverflowAriaLabelFormat is not null)
        {
            bitAvatarGroup.TakeFromCascade(nameof(OverflowAriaLabelFormat), OverflowAriaLabelFormat, static c => c.OverflowAriaLabelFormat, static (c, v) => c.OverflowAriaLabelFormat = v);
        }

        if (Size.HasValue)
        {
            bitAvatarGroup.TakeFromCascade(nameof(Size), Size.Value, static c => c.Size, static (c, v) => c.Size = v);
        }

        if (Spread.HasValue)
        {
            bitAvatarGroup.TakeFromCascade(nameof(Spread), Spread.Value, static c => c.Spread, static (c, v) => c.Spread = v);
        }

        if (Styles is not null)
        {
            bitAvatarGroup.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
