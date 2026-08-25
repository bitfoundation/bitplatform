namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTag"/> component.
/// </summary>
public class BitTagParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTag"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTag value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTag)}";



    public string Name => ParamName;



    /// <summary>
    /// What a selected tag that is a link reports itself as through aria-current.
    /// </summary>
    public BitNavAriaCurrent? AriaCurrent { get; set; }

    /// <summary>
    /// The detailed description of the tag for the benefit of screen readers, rendered into a visually
    /// hidden element the tag points at with aria-describedby.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the tag.
    /// </summary>
    public BitTagClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the tag.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The icon to use for the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// The name of the icon to use for the dismiss button from the built-in Fluent UI icons.
    /// </summary>
    public string? DismissIconName { get; set; }

    /// <summary>
    /// The accessible name and the tooltip of the dismiss button.
    /// </summary>
    public string? DismissLabel { get; set; }

    /// <summary>
    /// The format the dismiss button is named by while it has no DismissLabel of its own, where {0} is the
    /// text of the tag. The default is "Remove {0}".
    /// </summary>
    public string? DismissLabelFormat { get; set; }

    /// <summary>
    /// Prompts the browser to download the Href of the tag rather than to navigate to it, using the value as
    /// the suggested file name.
    /// </summary>
    public string? Download { get; set; }

    /// <summary>
    /// Stretches the tag to fill the width of whatever holds it.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Hides the checkmark a selected tag shows in front of its content.
    /// </summary>
    public bool? HideSelectedIcon { get; set; }

    /// <summary>
    /// The icon to show inside the tag using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The text alternative of the IconUrl picture of the tag.
    /// </summary>
    public string? IconAlt { get; set; }

    /// <summary>
    /// The icon to show inside the tag.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The URL of a picture to show in place of the icon of the tag.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Keeps the content of the tag on a single line and ends it with an ellipsis where it does not fit.
    /// </summary>
    public bool? NoWrap { get; set; }

    /// <summary>
    /// The relationship between the current document and the one the Href of the tag leads to.
    /// </summary>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Reverses the direction flow of the content of the tag.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// The trailing icon of the tag, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SecondaryIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? SecondaryIcon { get; set; }

    /// <summary>
    /// The name of the trailing icon of the tag, from the built-in Fluent UI icons.
    /// </summary>
    public string? SecondaryIconName { get; set; }

    /// <summary>
    /// The secondary text of the tag, rendered under its text in a quieter type.
    /// </summary>
    public string? SecondaryText { get; set; }

    /// <summary>
    /// The icon of the checkmark a selected tag shows, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SelectedIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? SelectedIcon { get; set; }

    /// <summary>
    /// The name of the icon of the checkmark a selected tag shows, from the built-in Fluent UI icons.
    /// </summary>
    public string? SelectedIconName { get; set; }

    /// <summary>
    /// The corner shape of the tag.
    /// </summary>
    public BitTagShape? Shape { get; set; }

    /// <summary>
    /// The size of the tag.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Stops the click of the tag from bubbling any further up the DOM.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tag.
    /// </summary>
    public BitTagClassStyles? Styles { get; set; }

    /// <summary>
    /// The browsing context the Href of the tag is opened in, for example _blank.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The text of the tag.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the tag.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the tag.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTag"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTag"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTag"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTag"/>.
    /// </remarks>
    /// <param name="bitTag">
    /// The <see cref="BitTag"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitTag bitTag)
    {
        if (bitTag is null) return;

        UpdateBaseParameters(bitTag);

        if (AriaCurrent.HasValue && bitTag.HasNotBeenSet(nameof(AriaCurrent)))
        {
            bitTag.AriaCurrent = AriaCurrent.Value;
        }

        if (AriaDescription.HasValue() && bitTag.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitTag.AriaDescription = AriaDescription;
        }

        if (Classes is not null && bitTag.HasNotBeenSet(nameof(Classes)))
        {
            bitTag.Classes = Classes;

            bitTag.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitTag.HasNotBeenSet(nameof(Color)))
        {
            bitTag.Color = Color.Value;

            bitTag.ClassBuilder.Reset();
        }

        if (DismissIcon is not null && bitTag.HasNotBeenSet(nameof(DismissIcon)))
        {
            bitTag.DismissIcon = DismissIcon;
        }

        if (DismissIconName.HasValue() && bitTag.HasNotBeenSet(nameof(DismissIconName)))
        {
            bitTag.DismissIconName = DismissIconName;
        }

        if (DismissLabel.HasValue() && bitTag.HasNotBeenSet(nameof(DismissLabel)))
        {
            bitTag.DismissLabel = DismissLabel;
        }

        if (DismissLabelFormat.HasValue() && bitTag.HasNotBeenSet(nameof(DismissLabelFormat)))
        {
            bitTag.DismissLabelFormat = DismissLabelFormat;
        }

        if (Download is not null && bitTag.HasNotBeenSet(nameof(Download)))
        {
            bitTag.Download = Download;
        }

        if (FullWidth.HasValue && bitTag.HasNotBeenSet(nameof(FullWidth)))
        {
            bitTag.FullWidth = FullWidth.Value;

            bitTag.ClassBuilder.Reset();
        }

        if (HideSelectedIcon.HasValue && bitTag.HasNotBeenSet(nameof(HideSelectedIcon)))
        {
            bitTag.HideSelectedIcon = HideSelectedIcon.Value;
        }

        if (Icon is not null && bitTag.HasNotBeenSet(nameof(Icon)))
        {
            bitTag.Icon = Icon;
        }

        if (IconAlt.HasValue() && bitTag.HasNotBeenSet(nameof(IconAlt)))
        {
            bitTag.IconAlt = IconAlt;
        }

        if (IconName.HasValue() && bitTag.HasNotBeenSet(nameof(IconName)))
        {
            bitTag.IconName = IconName;
        }

        if (IconUrl.HasValue() && bitTag.HasNotBeenSet(nameof(IconUrl)))
        {
            bitTag.IconUrl = IconUrl;
        }

        if (NoWrap.HasValue && bitTag.HasNotBeenSet(nameof(NoWrap)))
        {
            bitTag.NoWrap = NoWrap.Value;

            bitTag.ClassBuilder.Reset();
        }

        var relWasSet = false;
        var targetWasSet = false;

        if (Rel.HasValue && bitTag.HasNotBeenSet(nameof(Rel)))
        {
            bitTag.Rel = Rel.Value;

            relWasSet = true;
        }

        if (Reversed.HasValue && bitTag.HasNotBeenSet(nameof(Reversed)))
        {
            bitTag.Reversed = Reversed.Value;

            bitTag.ClassBuilder.Reset();
        }

        if (SecondaryIcon is not null && bitTag.HasNotBeenSet(nameof(SecondaryIcon)))
        {
            bitTag.SecondaryIcon = SecondaryIcon;
        }

        if (SecondaryIconName.HasValue() && bitTag.HasNotBeenSet(nameof(SecondaryIconName)))
        {
            bitTag.SecondaryIconName = SecondaryIconName;
        }

        if (SecondaryText.HasValue() && bitTag.HasNotBeenSet(nameof(SecondaryText)))
        {
            bitTag.SecondaryText = SecondaryText;
        }

        if (SelectedIcon is not null && bitTag.HasNotBeenSet(nameof(SelectedIcon)))
        {
            bitTag.SelectedIcon = SelectedIcon;
        }

        if (SelectedIconName.HasValue() && bitTag.HasNotBeenSet(nameof(SelectedIconName)))
        {
            bitTag.SelectedIconName = SelectedIconName;
        }

        if (Shape.HasValue && bitTag.HasNotBeenSet(nameof(Shape)))
        {
            bitTag.Shape = Shape.Value;

            bitTag.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitTag.HasNotBeenSet(nameof(Size)))
        {
            bitTag.Size = Size.Value;

            bitTag.ClassBuilder.Reset();
        }

        if (StopPropagation.HasValue && bitTag.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitTag.StopPropagation = StopPropagation.Value;
        }

        if (Styles is not null && bitTag.HasNotBeenSet(nameof(Styles)))
        {
            bitTag.Styles = Styles;

            bitTag.StyleBuilder.Reset();
        }

        if (Target.HasValue() && bitTag.HasNotBeenSet(nameof(Target)))
        {
            bitTag.Target = Target;

            targetWasSet = true;
        }

        // the rel attribute is derived from Href, Rel and Target together, so it is recalculated
        // whenever one of the two that can be cascaded has just been filled in from here.
        if (relWasSet || targetWasSet)
        {
            bitTag.OnSetHrefAndRel();
        }

        if (Text.HasValue() && bitTag.HasNotBeenSet(nameof(Text)))
        {
            bitTag.Text = Text;
        }

        if (Title.HasValue() && bitTag.HasNotBeenSet(nameof(Title)))
        {
            bitTag.Title = Title;
        }

        if (Variant.HasValue && bitTag.HasNotBeenSet(nameof(Variant)))
        {
            bitTag.Variant = Variant.Value;

            bitTag.ClassBuilder.Reset();
        }
    }
}
