using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCopyButton"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the copy buttons of an application, or of one part of it, agree on: their look, their texts
/// and icons and how long they show a copy. The value to copy and the callbacks are left out on purpose: they are what
/// makes one copy button the one it is.
/// </remarks>
public partial class BitCopyButtonParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCopyButton"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCopyButton value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCopyButton)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCopyButtonParams))]
    public BitCopyButtonParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the copy button.
    /// </summary>
    public BitCopyButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// How long the copied state stays on the button after a copy.
    /// </summary>
    public TimeSpan? CopiedDuration { get; set; }

    /// <summary>
    /// The icon the button shows while it is in the copied state, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? CopiedIcon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon the button shows while it is in the copied state.
    /// </summary>
    public string? CopiedIconName { get; set; }

    /// <summary>
    /// The text the button carries, and the live region announces, while it is in the copied state.
    /// </summary>
    public string? CopiedText { get; set; }

    /// <summary>
    /// The icon of the button, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon of the button.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The size of the button.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the copy button.
    /// </summary>
    public BitCopyButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the button.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The visual variant of the button.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCopyButton"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCopyButton"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCopyButton"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitCopyButton"/>.
    /// </remarks>
    /// <param name="bitCopyButton">
    /// The <see cref="BitCopyButton"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCopyButton bitCopyButton)
    {
        if (bitCopyButton is null) return;

        UpdateBaseParameters(bitCopyButton);

        if (Classes is not null)
        {
            bitCopyButton.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (Color.HasValue)
        {
            bitCopyButton.TakeFromCascade(nameof(Color), Color, static b => b.Color, static (b, v) => b.Color = v);
        }

        if (CopiedDuration.HasValue)
        {
            bitCopyButton.TakeFromCascade(nameof(CopiedDuration), CopiedDuration.Value, static b => b.CopiedDuration, static (b, v) => b.CopiedDuration = v);
        }

        if (CopiedIcon is not null)
        {
            bitCopyButton.TakeFromCascade(nameof(CopiedIcon), CopiedIcon, static b => b.CopiedIcon, static (b, v) => b.CopiedIcon = v);
        }

        if (CopiedIconName is not null)
        {
            bitCopyButton.TakeFromCascade(nameof(CopiedIconName), CopiedIconName, static b => b.CopiedIconName, static (b, v) => b.CopiedIconName = v);
        }

        if (CopiedText is not null)
        {
            bitCopyButton.TakeFromCascade(nameof(CopiedText), CopiedText, static b => b.CopiedText, static (b, v) => b.CopiedText = v);
        }

        if (Icon is not null)
        {
            bitCopyButton.TakeFromCascade(nameof(Icon), Icon, static b => b.Icon, static (b, v) => b.Icon = v);
        }

        if (IconName is not null)
        {
            bitCopyButton.TakeFromCascade(nameof(IconName), IconName, static b => b.IconName, static (b, v) => b.IconName = v);
        }

        if (Size.HasValue)
        {
            bitCopyButton.TakeFromCascade(nameof(Size), Size, static b => b.Size, static (b, v) => b.Size = v);
        }

        if (Styles is not null)
        {
            bitCopyButton.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }

        if (Text is not null)
        {
            bitCopyButton.TakeFromCascade(nameof(Text), Text, static b => b.Text, static (b, v) => b.Text = v);
        }

        if (Variant.HasValue)
        {
            bitCopyButton.TakeFromCascade(nameof(Variant), Variant, static b => b.Variant, static (b, v) => b.Variant = v);
        }
    }
}
