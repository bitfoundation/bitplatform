namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTextField"/> component.
/// </summary>
/// <remarks>
/// What it carries is a default and not an override: a field that writes a parameter for itself keeps its own
/// value, and only what it left unset is filled in from the cascade.
/// <br />
/// Three groups of parameters are deliberately left out of it, because a value shared between fields would be
/// wrong rather than merely unused: what identifies a field and carries its value
/// (<c>Value</c>, <c>DefaultValue</c>, <c>Name</c>, <c>DisplayName</c>), its event callbacks, and what says
/// something about the value currently in one field alone (<c>Invalid</c>, <c>ErrorMessage</c>,
/// <c>GhostText</c>, <c>Loading</c>, <c>AutoFocus</c>).
/// <br />
/// <c>InputHtmlAttributes</c> and <c>NoValidate</c> are left out for the reasons
/// <see cref="BitInputBaseParams{TValue}"/> gives: the first is a dictionary the field writes into, and the second
/// is read while the parameters are still being set, before any cascade has been applied.
/// </remarks>
public class BitTextFieldParams : BitInputBaseParams<string?>, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTextField"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTextField value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTextField)}";



    public string Name => ParamName;



    /// <summary>
    /// The general color of the text field used when focused.
    /// </summary>
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Detailed description of the input for the benefit of screen readers, rendered into a visually hidden
    /// element the input references through its <c>aria-describedby</c> attribute.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// Sets the autocapitalize html attribute of the input element.
    /// </summary>
    public string? AutoCapitalize { get; set; }

    /// <summary>
    /// Specifies the value of the autocomplete attribute of the input element.
    /// </summary>
    public string? AutoComplete { get; set; }

    /// <summary>
    /// Sets the autocorrect html attribute of the input element.
    /// </summary>
    public bool? AutoCorrect { get; set; }

    /// <summary>
    /// Automatically adjust the height of the input in Multiline mode.
    /// </summary>
    public bool? AutoHeight { get; set; }

    /// <summary>
    /// The color kind of the text field background.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the text field border.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Whether to show the reveal password button for input type 'password'.
    /// </summary>
    public bool? CanRevealPassword { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTextField.
    /// </summary>
    public BitTextFieldClassStyles? Classes { get; set; }

    /// <summary>
    /// The aria-label of the clear button.
    /// </summary>
    public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon to display inside the clear button, from an external icon library.
    /// </summary>
    public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The name of the clear button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The custom content of the clear button, which replaces its icon.
    /// </summary>
    public RenderFragment? ClearButtonTemplate { get; set; }

    /// <summary>
    /// What a screen reader announces once the field has been emptied, in place of the default "Cleared".
    /// An empty string keeps the clearing from being announced at all.
    /// </summary>
    public string? ClearedAnnouncement { get; set; }

    /// <summary>
    /// Empties the field when the Escape key is pressed in it, the keyboard counterpart of the clear button.
    /// </summary>
    public bool? ClearOnEscape { get; set; }

    /// <summary>
    /// Decides how the characters of the value are counted for the counter rendered by <see cref="ShowCount"/>.
    /// </summary>
    public Func<string?, int>? CountStrategy { get; set; }

    /// <summary>
    /// The custom content of the character counter, which receives the current number of characters.
    /// </summary>
    public RenderFragment<int>? CountTemplate { get; set; }

    /// <summary>
    /// The debounce time in milliseconds.
    /// </summary>
    public int? DebounceTime { get; set; }

    /// <summary>
    /// Description displayed below the text field to provide additional details about what text to enter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Shows the custom description for text field.
    /// </summary>
    public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Sets the enterkeyhint html attribute of the input element.
    /// </summary>
    public string? EnterKeyHint { get; set; }

    /// <summary>
    /// Forces the text field fill 100% of its container width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The icon of the reveal password button when the password is shown, from an external icon library.
    /// </summary>
    public BitIconInfo? HidePasswordIcon { get; set; }

    /// <summary>
    /// The icon name of the reveal password button when the password is shown, from the built-in Fluent UI icons.
    /// </summary>
    public string? HidePasswordIconName { get; set; }

    /// <summary>
    /// The icon to display inside the field, from an external icon library.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The accessible name of the icon shown inside the text field.
    /// </summary>
    public string? IconAriaLabel { get; set; }

    /// <summary>
    /// The icon name for the icon shown inside the text field, from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Which end of the field the icon sits at, inside the frame.
    /// </summary>
    public BitPlacement? IconPlacement { get; set; }

    /// <summary>
    /// The html title of the icon, rendered while an OnIconClick handler makes the icon a button.
    /// </summary>
    public string? IconTitle { get; set; }

    /// <summary>
    /// Change the content of the input field when the user writes text (based on the 'oninput' HTML event).
    /// </summary>
    public bool? Immediate { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the input element.
    /// </summary>
    public BitInputMode? InputMode { get; set; }


    /// <summary>
    /// Label displayed above the text field and read by screen readers.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Where the label sits relative to the input.
    /// </summary>
    public BitPlacement? LabelPlacement { get; set; }

    /// <summary>
    /// Shows the custom label for text field.
    /// </summary>
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// What a screen reader announces while the field is loading, in place of the default "Loading".
    /// </summary>
    public string? LoadingAriaLabel { get; set; }

    /// <summary>
    /// The custom content of the busy indicator, which replaces the default spinner.
    /// </summary>
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Specifies the maximum number of characters allowed in the input.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// The maximum number of rows the input grows to in the Multiline mode while <see cref="AutoHeight"/> is enabled.
    /// </summary>
    public int? MaxRows { get; set; }

    /// <summary>
    /// Specifies the minimum number of characters the input accepts.
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Whether or not the text field is a Multiline text field.
    /// </summary>
    public bool? Multiline { get; set; }

    /// <summary>
    /// Removes the border of the text input.
    /// </summary>
    public bool? NoBorder { get; set; }


    /// <summary>
    /// Sets the pattern html attribute of the input element.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Enables permanent ghost mode that forces the scrollbar-gutter to always be present.
    /// </summary>
    public bool? PermanentGhost { get; set; }

    /// <summary>
    /// Input placeholder text.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Prefix displayed before the text field contents. This is not included in the value.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Shows the custom prefix for text field.
    /// </summary>
    public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// Prevents the enter key from adding a new line character to the input in the Multiline mode.
    /// </summary>
    public bool? PreventEnter { get; set; }



    /// <summary>
    /// For multiline text fields, whether or not the field is resizable.
    /// </summary>
    public bool? Resizable { get; set; }

    /// <summary>
    /// Aria label for the reveal password button.
    /// </summary>
    public string? RevealPasswordAriaLabel { get; set; }

    /// <summary>
    /// The icon of the reveal password button when the password is hidden, from an external icon library.
    /// </summary>
    public BitIconInfo? RevealPasswordIcon { get; set; }

    /// <summary>
    /// The icon name of the reveal password button when the password is hidden, from the built-in Fluent UI icons.
    /// </summary>
    public string? RevealPasswordIconName { get; set; }

    /// <summary>
    /// The custom content of the reveal password button, which receives whether the password is currently revealed.
    /// </summary>
    public RenderFragment<bool>? RevealPasswordTemplate { get; set; }

    /// <summary>
    /// For multiline text, the number of rows.
    /// </summary>
    public int? Rows { get; set; }

    /// <summary>
    /// Selects the whole value when the input receives focus.
    /// </summary>
    public bool? SelectOnFocus { get; set; }

    /// <summary>
    /// Whether to show the clear button when the text field has a value.
    /// </summary>
    public bool? ShowClearButton { get; set; }

    /// <summary>
    /// Shows the number of characters that were typed under the text field.
    /// </summary>
    public bool? ShowCount { get; set; }

    /// <summary>
    /// The size of the text field.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Sets the spellcheck html attribute of the input element.
    /// </summary>
    public bool? SpellCheck { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTextField.
    /// </summary>
    public BitTextFieldClassStyles? Styles { get; set; }

    /// <summary>
    /// Suffix displayed after the text field contents. This is not included in the value.
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// Shows the custom suffix for text field.
    /// </summary>
    public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// The throttle time in milliseconds.
    /// </summary>
    public int? ThrottleTime { get; set; }

    /// <summary>
    /// A more descriptive title of the text field, shown by the browser as its tooltip.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Specifies whether to remove any leading or trailing whitespace from the value.
    /// </summary>
    public bool? Trim { get; set; }

    /// <summary>
    /// Input type.
    /// </summary>
    public BitInputType? Type { get; set; }

    /// <summary>
    /// Whether or not the text field is underlined.
    /// </summary>
    public bool? Underlined { get; set; }

    /// <summary>
    /// Sets the wrap html attribute of the textarea rendered in the Multiline mode.
    /// </summary>
    public string? Wrap { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTextField"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTextField"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTextField"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTextField"/>.
    /// </remarks>
    /// <param name="bitTextField">
    /// The <see cref="BitTextField"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitTextField bitTextField)
    {
        if (bitTextField is null) return;

        UpdateInputBaseParameters(bitTextField);

        if (Accent.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Accent), Accent.Value, static t => t.Accent, static (t, v) => t.Accent = v);
        }

        if (AriaDescription.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(AriaDescription), AriaDescription, static t => t.AriaDescription, static (t, v) => t.AriaDescription = v);
        }

        if (AutoCapitalize.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(AutoCapitalize), AutoCapitalize, static t => t.AutoCapitalize, static (t, v) => t.AutoCapitalize = v);
        }

        if (AutoComplete.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(AutoComplete), AutoComplete, static t => t.AutoComplete, static (t, v) => t.AutoComplete = v);
        }

        if (AutoCorrect.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(AutoCorrect), AutoCorrect.Value, static t => t.AutoCorrect, static (t, v) => t.AutoCorrect = v);
        }

        if (AutoHeight.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(AutoHeight), AutoHeight.Value, static t => t.AutoHeight, static (t, v) => t.AutoHeight = v);
        }

        if (Background.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Background), Background.Value, static t => t.Background, static (t, v) => t.Background = v);
        }

        if (Border.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Border), Border.Value, static t => t.Border, static (t, v) => t.Border = v);
        }

        if (CanRevealPassword.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(CanRevealPassword), CanRevealPassword.Value, static t => t.CanRevealPassword, static (t, v) => t.CanRevealPassword = v);
        }

        if (Classes is not null)
        {
            bitTextField.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (ClearButtonAriaLabel.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(ClearButtonAriaLabel), ClearButtonAriaLabel, static t => t.ClearButtonAriaLabel, static (t, v) => t.ClearButtonAriaLabel = v);
        }

        if (ClearButtonIcon is not null)
        {
            bitTextField.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static t => t.ClearButtonIcon, static (t, v) => t.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static t => t.ClearButtonIconName, static (t, v) => t.ClearButtonIconName = v);
        }

        if (ClearButtonTemplate is not null)
        {
            bitTextField.TakeFromCascade(nameof(ClearButtonTemplate), ClearButtonTemplate, static t => t.ClearButtonTemplate, static (t, v) => t.ClearButtonTemplate = v);
        }

        if (ClearedAnnouncement is not null)
        {
            bitTextField.TakeFromCascade(nameof(ClearedAnnouncement), ClearedAnnouncement, static t => t.ClearedAnnouncement, static (t, v) => t.ClearedAnnouncement = v);
        }

        if (ClearOnEscape.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(ClearOnEscape), ClearOnEscape.Value, static t => t.ClearOnEscape, static (t, v) => t.ClearOnEscape = v);
        }

        if (CountStrategy is not null)
        {
            bitTextField.TakeFromCascade(nameof(CountStrategy), CountStrategy, static t => t.CountStrategy, static (t, v) => t.CountStrategy = v);
        }

        if (CountTemplate is not null)
        {
            bitTextField.TakeFromCascade(nameof(CountTemplate), CountTemplate, static t => t.CountTemplate, static (t, v) => t.CountTemplate = v);
        }

        if (DebounceTime.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(DebounceTime), DebounceTime.Value, static t => t.DebounceTime, static (t, v) => t.DebounceTime = v);
        }

        if (Description.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(Description), Description, static t => t.Description, static (t, v) => t.Description = v);
        }

        if (DescriptionTemplate is not null)
        {
            bitTextField.TakeFromCascade(nameof(DescriptionTemplate), DescriptionTemplate, static t => t.DescriptionTemplate, static (t, v) => t.DescriptionTemplate = v);
        }

        if (EnterKeyHint.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(EnterKeyHint), EnterKeyHint, static t => t.EnterKeyHint, static (t, v) => t.EnterKeyHint = v);
        }

        if (FullWidth.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static t => t.FullWidth, static (t, v) => t.FullWidth = v);
        }

        if (HidePasswordIcon is not null)
        {
            bitTextField.TakeFromCascade(nameof(HidePasswordIcon), HidePasswordIcon, static t => t.HidePasswordIcon, static (t, v) => t.HidePasswordIcon = v);
        }

        if (HidePasswordIconName.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(HidePasswordIconName), HidePasswordIconName, static t => t.HidePasswordIconName, static (t, v) => t.HidePasswordIconName = v);
        }

        if (Icon is not null)
        {
            bitTextField.TakeFromCascade(nameof(Icon), Icon, static t => t.Icon, static (t, v) => t.Icon = v);
        }

        if (IconAriaLabel.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(IconAriaLabel), IconAriaLabel, static t => t.IconAriaLabel, static (t, v) => t.IconAriaLabel = v);
        }

        if (IconName.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(IconName), IconName, static t => t.IconName, static (t, v) => t.IconName = v);
        }

        if (IconPlacement.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static t => t.IconPlacement, static (t, v) => t.IconPlacement = v);
        }

        if (IconTitle.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(IconTitle), IconTitle, static t => t.IconTitle, static (t, v) => t.IconTitle = v);
        }

        if (Immediate.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Immediate), Immediate.Value, static t => t.Immediate, static (t, v) => t.Immediate = v);
        }

        if (InputMode.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(InputMode), InputMode.Value, static t => t.InputMode, static (t, v) => t.InputMode = v);
        }

        if (Label.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(Label), Label, static t => t.Label, static (t, v) => t.Label = v);
        }

        if (LabelPlacement.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(LabelPlacement), LabelPlacement.Value, static t => t.LabelPlacement, static (t, v) => t.LabelPlacement = v);
        }

        if (LabelTemplate is not null)
        {
            bitTextField.TakeFromCascade(nameof(LabelTemplate), LabelTemplate, static t => t.LabelTemplate, static (t, v) => t.LabelTemplate = v);
        }

        if (LoadingAriaLabel.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(LoadingAriaLabel), LoadingAriaLabel, static t => t.LoadingAriaLabel, static (t, v) => t.LoadingAriaLabel = v);
        }

        if (LoadingTemplate is not null)
        {
            bitTextField.TakeFromCascade(nameof(LoadingTemplate), LoadingTemplate, static t => t.LoadingTemplate, static (t, v) => t.LoadingTemplate = v);
        }

        if (MaxLength.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(MaxLength), MaxLength.Value, static t => t.MaxLength, static (t, v) => t.MaxLength = v);
        }

        if (MaxRows.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(MaxRows), MaxRows.Value, static t => t.MaxRows, static (t, v) => t.MaxRows = v);
        }

        if (MinLength.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(MinLength), MinLength.Value, static t => t.MinLength, static (t, v) => t.MinLength = v);
        }

        if (Multiline.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Multiline), Multiline.Value, static t => t.Multiline, static (t, v) => t.Multiline = v);
        }

        if (NoBorder.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static t => t.NoBorder, static (t, v) => t.NoBorder = v);
        }

        if (Pattern.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(Pattern), Pattern, static t => t.Pattern, static (t, v) => t.Pattern = v);
        }

        if (PermanentGhost.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(PermanentGhost), PermanentGhost.Value, static t => t.PermanentGhost, static (t, v) => t.PermanentGhost = v);
        }

        if (Placeholder.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(Placeholder), Placeholder, static t => t.Placeholder, static (t, v) => t.Placeholder = v);
        }

        if (Prefix.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(Prefix), Prefix, static t => t.Prefix, static (t, v) => t.Prefix = v);
        }

        if (PrefixTemplate is not null)
        {
            bitTextField.TakeFromCascade(nameof(PrefixTemplate), PrefixTemplate, static t => t.PrefixTemplate, static (t, v) => t.PrefixTemplate = v);
        }

        if (PreventEnter.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(PreventEnter), PreventEnter.Value, static t => t.PreventEnter, static (t, v) => t.PreventEnter = v);
        }

        if (Resizable.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Resizable), Resizable.Value, static t => t.Resizable, static (t, v) => t.Resizable = v);
        }

        if (RevealPasswordAriaLabel.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(RevealPasswordAriaLabel), RevealPasswordAriaLabel, static t => t.RevealPasswordAriaLabel, static (t, v) => t.RevealPasswordAriaLabel = v);
        }

        if (RevealPasswordIcon is not null)
        {
            bitTextField.TakeFromCascade(nameof(RevealPasswordIcon), RevealPasswordIcon, static t => t.RevealPasswordIcon, static (t, v) => t.RevealPasswordIcon = v);
        }

        if (RevealPasswordIconName.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(RevealPasswordIconName), RevealPasswordIconName, static t => t.RevealPasswordIconName, static (t, v) => t.RevealPasswordIconName = v);
        }

        if (RevealPasswordTemplate is not null)
        {
            bitTextField.TakeFromCascade(nameof(RevealPasswordTemplate), RevealPasswordTemplate, static t => t.RevealPasswordTemplate, static (t, v) => t.RevealPasswordTemplate = v);
        }

        if (Rows.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Rows), Rows.Value, static t => t.Rows, static (t, v) => t.Rows = v);
        }

        if (SelectOnFocus.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(SelectOnFocus), SelectOnFocus.Value, static t => t.SelectOnFocus, static (t, v) => t.SelectOnFocus = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static t => t.ShowClearButton, static (t, v) => t.ShowClearButton = v);
        }

        if (ShowCount.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(ShowCount), ShowCount.Value, static t => t.ShowCount, static (t, v) => t.ShowCount = v);
        }

        if (Size.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Size), Size.Value, static t => t.Size, static (t, v) => t.Size = v);
        }

        if (SpellCheck.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(SpellCheck), SpellCheck.Value, static t => t.SpellCheck, static (t, v) => t.SpellCheck = v);
        }

        if (Styles is not null)
        {
            bitTextField.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }

        if (Suffix.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(Suffix), Suffix, static t => t.Suffix, static (t, v) => t.Suffix = v);
        }

        if (SuffixTemplate is not null)
        {
            bitTextField.TakeFromCascade(nameof(SuffixTemplate), SuffixTemplate, static t => t.SuffixTemplate, static (t, v) => t.SuffixTemplate = v);
        }

        if (ThrottleTime.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(ThrottleTime), ThrottleTime.Value, static t => t.ThrottleTime, static (t, v) => t.ThrottleTime = v);
        }

        if (Title.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(Title), Title, static t => t.Title, static (t, v) => t.Title = v);
        }

        if (Trim.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Trim), Trim.Value, static t => t.Trim, static (t, v) => t.Trim = v);
        }

        if (Type.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Type), Type.Value, static t => t.Type, static (t, v) => t.Type = v);
        }

        if (Underlined.HasValue)
        {
            bitTextField.TakeFromCascade(nameof(Underlined), Underlined.Value, static t => t.Underlined, static (t, v) => t.Underlined = v);
        }

        if (Wrap.HasValue())
        {
            bitTextField.TakeFromCascade(nameof(Wrap), Wrap, static t => t.Wrap, static (t, v) => t.Wrap = v);
        }
    }
}
