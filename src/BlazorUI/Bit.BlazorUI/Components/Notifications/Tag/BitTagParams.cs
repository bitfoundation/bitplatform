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
    /// Keeps a disabled tag that is a control focusable and discoverable by assistive technologies, reporting
    /// the state through aria-disabled instead of the native disabled attribute.
    /// </summary>
    public bool? AllowDisabledFocus { get; set; }

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
    /// The text a link tag opening a new tab is announced with. The default is "(opens in a new tab)"; an
    /// empty value takes the announcement off.
    /// </summary>
    public string? NewTabHint { get; set; }

    /// <summary>
    /// Stops a link tag opening a new tab from announcing that it does.
    /// </summary>
    public bool? NoNewTabHint { get; set; }

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
    public BitShape? Shape { get; set; }

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

        if (AllowDisabledFocus.HasValue)
        {
            bitTag.TakeFromCascade(nameof(AllowDisabledFocus), AllowDisabledFocus.Value, static t => t.AllowDisabledFocus, static (t, v) => t.AllowDisabledFocus = v);
        }

        if (AriaCurrent.HasValue)
        {
            bitTag.TakeFromCascade(nameof(AriaCurrent), AriaCurrent.Value, static t => t.AriaCurrent, static (t, v) => t.AriaCurrent = v);
        }

        if (AriaDescription.HasValue())
        {
            bitTag.TakeFromCascade(nameof(AriaDescription), AriaDescription, static t => t.AriaDescription, static (t, v) => t.AriaDescription = v);
        }

        if (Classes is not null)
        {
            bitTag.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (Color.HasValue)
        {
            bitTag.TakeFromCascade(nameof(Color), Color.Value, static t => t.Color, static (t, v) => t.Color = v);
        }

        if (DismissIcon is not null)
        {
            bitTag.TakeFromCascade(nameof(DismissIcon), DismissIcon, static t => t.DismissIcon, static (t, v) => t.DismissIcon = v);
        }

        if (DismissIconName.HasValue())
        {
            bitTag.TakeFromCascade(nameof(DismissIconName), DismissIconName, static t => t.DismissIconName, static (t, v) => t.DismissIconName = v);
        }

        if (DismissLabel.HasValue())
        {
            bitTag.TakeFromCascade(nameof(DismissLabel), DismissLabel, static t => t.DismissLabel, static (t, v) => t.DismissLabel = v);
        }

        if (DismissLabelFormat.HasValue())
        {
            bitTag.TakeFromCascade(nameof(DismissLabelFormat), DismissLabelFormat, static t => t.DismissLabelFormat, static (t, v) => t.DismissLabelFormat = v);
        }

        if (Download is not null)
        {
            bitTag.TakeFromCascade(nameof(Download), Download, static t => t.Download, static (t, v) => t.Download = v);
        }

        if (FullWidth.HasValue)
        {
            bitTag.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static t => t.FullWidth, static (t, v) => t.FullWidth = v);
        }

        if (HideSelectedIcon.HasValue)
        {
            bitTag.TakeFromCascade(nameof(HideSelectedIcon), HideSelectedIcon.Value, static t => t.HideSelectedIcon, static (t, v) => t.HideSelectedIcon = v);
        }

        if (Icon is not null)
        {
            bitTag.TakeFromCascade(nameof(Icon), Icon, static t => t.Icon, static (t, v) => t.Icon = v);
        }

        if (IconAlt.HasValue())
        {
            bitTag.TakeFromCascade(nameof(IconAlt), IconAlt, static t => t.IconAlt, static (t, v) => t.IconAlt = v);
        }

        if (IconName.HasValue())
        {
            bitTag.TakeFromCascade(nameof(IconName), IconName, static t => t.IconName, static (t, v) => t.IconName = v);
        }

        if (IconUrl.HasValue())
        {
            bitTag.TakeFromCascade(nameof(IconUrl), IconUrl, static t => t.IconUrl, static (t, v) => t.IconUrl = v);
        }

        // an empty hint is a value of its own - the one that takes the announcement off - so only null is
        // what leaves the tag to its default.
        if (NewTabHint is not null)
        {
            bitTag.TakeFromCascade(nameof(NewTabHint), NewTabHint, static t => t.NewTabHint, static (t, v) => t.NewTabHint = v);
        }

        if (NoNewTabHint.HasValue)
        {
            bitTag.TakeFromCascade(nameof(NoNewTabHint), NoNewTabHint.Value, static t => t.NoNewTabHint, static (t, v) => t.NoNewTabHint = v);
        }

        if (NoWrap.HasValue)
        {
            bitTag.TakeFromCascade(nameof(NoWrap), NoWrap.Value, static t => t.NoWrap, static (t, v) => t.NoWrap = v);
        }

        if (Rel.HasValue)
        {
            bitTag.TakeFromCascade(nameof(Rel), Rel.Value, static t => t.Rel, static (t, v) => t.Rel = v);
        }

        if (Reversed.HasValue)
        {
            bitTag.TakeFromCascade(nameof(Reversed), Reversed.Value, static t => t.Reversed, static (t, v) => t.Reversed = v);
        }

        if (SecondaryIcon is not null)
        {
            bitTag.TakeFromCascade(nameof(SecondaryIcon), SecondaryIcon, static t => t.SecondaryIcon, static (t, v) => t.SecondaryIcon = v);
        }

        if (SecondaryIconName.HasValue())
        {
            bitTag.TakeFromCascade(nameof(SecondaryIconName), SecondaryIconName, static t => t.SecondaryIconName, static (t, v) => t.SecondaryIconName = v);
        }

        if (SecondaryText.HasValue())
        {
            bitTag.TakeFromCascade(nameof(SecondaryText), SecondaryText, static t => t.SecondaryText, static (t, v) => t.SecondaryText = v);
        }

        if (SelectedIcon is not null)
        {
            bitTag.TakeFromCascade(nameof(SelectedIcon), SelectedIcon, static t => t.SelectedIcon, static (t, v) => t.SelectedIcon = v);
        }

        if (SelectedIconName.HasValue())
        {
            bitTag.TakeFromCascade(nameof(SelectedIconName), SelectedIconName, static t => t.SelectedIconName, static (t, v) => t.SelectedIconName = v);
        }

        if (Shape.HasValue)
        {
            bitTag.TakeFromCascade(nameof(Shape), Shape.Value, static t => t.Shape, static (t, v) => t.Shape = v);
        }

        if (Size.HasValue)
        {
            bitTag.TakeFromCascade(nameof(Size), Size.Value, static t => t.Size, static (t, v) => t.Size = v);
        }

        if (StopPropagation.HasValue)
        {
            bitTag.TakeFromCascade(nameof(StopPropagation), StopPropagation.Value, static t => t.StopPropagation, static (t, v) => t.StopPropagation = v);
        }

        if (Styles is not null)
        {
            bitTag.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }

        if (Target.HasValue())
        {
            bitTag.TakeFromCascade(nameof(Target), Target, static t => t.Target, static (t, v) => t.Target = v);
        }

        if (Text.HasValue())
        {
            bitTag.TakeFromCascade(nameof(Text), Text, static t => t.Text, static (t, v) => t.Text = v);
        }

        if (Title.HasValue())
        {
            bitTag.TakeFromCascade(nameof(Title), Title, static t => t.Title, static (t, v) => t.Title = v);
        }

        if (Variant.HasValue)
        {
            bitTag.TakeFromCascade(nameof(Variant), Variant.Value, static t => t.Variant, static (t, v) => t.Variant = v);
        }
    }
}
