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
/// </remarks>
public class BitTextFieldParams : BitComponentBaseParams, IBitComponentParams
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
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// Change the content of the input field when the user writes text (based on the 'oninput' HTML event).
    /// </summary>
    public bool? Immediate { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the input element.
    /// </summary>
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// Additional html attributes applied to the input element. They are merged into what the field already
    /// carries rather than replacing it, so a field keeps the attributes it wrote for itself.
    /// </summary>
    public Dictionary<string, object>? InputHtmlAttributes { get; set; }

    /// <summary>
    /// Label displayed above the text field and read by screen readers.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Where the label sits relative to the input.
    /// </summary>
    public BitLabelPosition? LabelPosition { get; set; }

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
    /// Disables the validation of the input.
    /// </summary>
    public bool? NoValidate { get; set; }

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
    /// Makes the input read-only.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Makes the input required.
    /// </summary>
    public bool? Required { get; set; }

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

        UpdateBaseParameters(bitTextField);

        if (Accent.HasValue && bitTextField.HasNotBeenSet(nameof(Accent)))
        {
            bitTextField.Accent = Accent.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (AriaDescription.HasValue() && bitTextField.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitTextField.AriaDescription = AriaDescription;
        }

        if (AutoCapitalize.HasValue() && bitTextField.HasNotBeenSet(nameof(AutoCapitalize)))
        {
            bitTextField.AutoCapitalize = AutoCapitalize;
        }

        // The parameters of the input base classes are not tracked by the generated HasNotBeenSet of the
        // component, which only knows the ones the component declares itself, so they are asked about through
        // the tier that does track them.
        if (AutoComplete.HasValue() && bitTextField.InheritedParameterHasNotBeenSet(nameof(AutoComplete)))
        {
            bitTextField.AutoComplete = AutoComplete;
        }

        if (AutoCorrect.HasValue && bitTextField.HasNotBeenSet(nameof(AutoCorrect)))
        {
            bitTextField.AutoCorrect = AutoCorrect.Value;
        }

        if (AutoHeight.HasValue && bitTextField.HasNotBeenSet(nameof(AutoHeight)))
        {
            bitTextField.AutoHeight = AutoHeight.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (Background.HasValue && bitTextField.HasNotBeenSet(nameof(Background)))
        {
            bitTextField.Background = Background.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (Border.HasValue && bitTextField.HasNotBeenSet(nameof(Border)))
        {
            bitTextField.Border = Border.Value;

            bitTextField.ClassBuilder.Reset();
        }

        bool elementTypeChanged = false;

        if (CanRevealPassword.HasValue && bitTextField.HasNotBeenSet(nameof(CanRevealPassword)))
        {
            bitTextField.CanRevealPassword = CanRevealPassword.Value;

            elementTypeChanged = true;
        }

        if (Classes is not null && bitTextField.HasNotBeenSet(nameof(Classes)))
        {
            bitTextField.Classes = Classes;

            bitTextField.ClassBuilder.Reset();
        }

        if (ClearButtonAriaLabel.HasValue() && bitTextField.HasNotBeenSet(nameof(ClearButtonAriaLabel)))
        {
            bitTextField.ClearButtonAriaLabel = ClearButtonAriaLabel;
        }

        if (ClearButtonIcon is not null && bitTextField.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitTextField.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitTextField.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitTextField.ClearButtonIconName = ClearButtonIconName;
        }

        if (ClearButtonTemplate is not null && bitTextField.HasNotBeenSet(nameof(ClearButtonTemplate)))
        {
            bitTextField.ClearButtonTemplate = ClearButtonTemplate;
        }

        if (CountStrategy is not null && bitTextField.HasNotBeenSet(nameof(CountStrategy)))
        {
            bitTextField.CountStrategy = CountStrategy;
        }

        if (CountTemplate is not null && bitTextField.HasNotBeenSet(nameof(CountTemplate)))
        {
            bitTextField.CountTemplate = CountTemplate;
        }

        if (DebounceTime.HasValue && bitTextField.InheritedParameterHasNotBeenSet(nameof(DebounceTime)))
        {
            bitTextField.DebounceTime = DebounceTime.Value;
        }

        if (Description.HasValue() && bitTextField.HasNotBeenSet(nameof(Description)))
        {
            bitTextField.Description = Description;
        }

        if (DescriptionTemplate is not null && bitTextField.HasNotBeenSet(nameof(DescriptionTemplate)))
        {
            bitTextField.DescriptionTemplate = DescriptionTemplate;
        }

        if (EnterKeyHint.HasValue() && bitTextField.HasNotBeenSet(nameof(EnterKeyHint)))
        {
            bitTextField.EnterKeyHint = EnterKeyHint;
        }

        if (FullWidth.HasValue && bitTextField.HasNotBeenSet(nameof(FullWidth)))
        {
            bitTextField.FullWidth = FullWidth.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (HidePasswordIcon is not null && bitTextField.HasNotBeenSet(nameof(HidePasswordIcon)))
        {
            bitTextField.HidePasswordIcon = HidePasswordIcon;
        }

        if (HidePasswordIconName.HasValue() && bitTextField.HasNotBeenSet(nameof(HidePasswordIconName)))
        {
            bitTextField.HidePasswordIconName = HidePasswordIconName;
        }

        if (Icon is not null && bitTextField.HasNotBeenSet(nameof(Icon)))
        {
            bitTextField.Icon = Icon;
        }

        if (IconAriaLabel.HasValue() && bitTextField.HasNotBeenSet(nameof(IconAriaLabel)))
        {
            bitTextField.IconAriaLabel = IconAriaLabel;
        }

        if (IconName.HasValue() && bitTextField.HasNotBeenSet(nameof(IconName)))
        {
            bitTextField.IconName = IconName;
        }

        if (IconPosition.HasValue && bitTextField.HasNotBeenSet(nameof(IconPosition)))
        {
            bitTextField.IconPosition = IconPosition.Value;
        }

        if (Immediate.HasValue && bitTextField.InheritedParameterHasNotBeenSet(nameof(Immediate)))
        {
            bitTextField.Immediate = Immediate.Value;
        }

        if (InputMode.HasValue && bitTextField.HasNotBeenSet(nameof(InputMode)))
        {
            bitTextField.InputMode = InputMode.Value;

            bitTextField.SetInputMode();
        }

        // Unlike every other parameter here, the attributes are merged rather than replaced: the two
        // dictionaries are a set of attributes each, and a field writing one of its own should not lose the
        // rest of the cascaded set along with the one it overrode.
        if (InputHtmlAttributes is not null)
        {
            if (bitTextField.InputHtmlAttributes is null)
            {
                bitTextField.InputHtmlAttributes = new Dictionary<string, object>(InputHtmlAttributes);
            }
            else
            {
                foreach (var attribute in InputHtmlAttributes)
                {
                    if (bitTextField.InputHtmlAttributes.ContainsKey(attribute.Key)) continue;

                    bitTextField.InputHtmlAttributes[attribute.Key] = attribute.Value;
                }
            }
        }

        if (Label.HasValue() && bitTextField.HasNotBeenSet(nameof(Label)))
        {
            bitTextField.Label = Label;

            bitTextField.ClassBuilder.Reset();
        }

        if (LabelPosition.HasValue && bitTextField.HasNotBeenSet(nameof(LabelPosition)))
        {
            bitTextField.LabelPosition = LabelPosition.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (LabelTemplate is not null && bitTextField.HasNotBeenSet(nameof(LabelTemplate)))
        {
            bitTextField.LabelTemplate = LabelTemplate;

            bitTextField.ClassBuilder.Reset();
        }

        if (LoadingAriaLabel.HasValue() && bitTextField.HasNotBeenSet(nameof(LoadingAriaLabel)))
        {
            bitTextField.LoadingAriaLabel = LoadingAriaLabel;
        }

        if (LoadingTemplate is not null && bitTextField.HasNotBeenSet(nameof(LoadingTemplate)))
        {
            bitTextField.LoadingTemplate = LoadingTemplate;
        }

        if (MaxLength.HasValue && bitTextField.HasNotBeenSet(nameof(MaxLength)))
        {
            bitTextField.MaxLength = MaxLength.Value;
        }

        if (MaxRows.HasValue && bitTextField.HasNotBeenSet(nameof(MaxRows)))
        {
            bitTextField.MaxRows = MaxRows.Value;
        }

        if (MinLength.HasValue && bitTextField.HasNotBeenSet(nameof(MinLength)))
        {
            bitTextField.MinLength = MinLength.Value;
        }

        if (Multiline.HasValue && bitTextField.HasNotBeenSet(nameof(Multiline)))
        {
            bitTextField.Multiline = Multiline.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (NoBorder.HasValue && bitTextField.HasNotBeenSet(nameof(NoBorder)))
        {
            bitTextField.NoBorder = NoBorder.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (NoValidate.HasValue && bitTextField.InheritedParameterHasNotBeenSet(nameof(NoValidate)))
        {
            bitTextField.NoValidate = NoValidate.Value;
        }

        if (Pattern.HasValue() && bitTextField.HasNotBeenSet(nameof(Pattern)))
        {
            bitTextField.Pattern = Pattern;
        }

        if (PermanentGhost.HasValue && bitTextField.HasNotBeenSet(nameof(PermanentGhost)))
        {
            bitTextField.PermanentGhost = PermanentGhost.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (Placeholder.HasValue() && bitTextField.HasNotBeenSet(nameof(Placeholder)))
        {
            bitTextField.Placeholder = Placeholder;
        }

        if (Prefix.HasValue() && bitTextField.HasNotBeenSet(nameof(Prefix)))
        {
            bitTextField.Prefix = Prefix;
        }

        if (PrefixTemplate is not null && bitTextField.HasNotBeenSet(nameof(PrefixTemplate)))
        {
            bitTextField.PrefixTemplate = PrefixTemplate;
        }

        if (PreventEnter.HasValue && bitTextField.HasNotBeenSet(nameof(PreventEnter)))
        {
            bitTextField.PreventEnter = PreventEnter.Value;
        }

        if (ReadOnly.HasValue && bitTextField.InheritedParameterHasNotBeenSet(nameof(ReadOnly)))
        {
            bitTextField.ReadOnly = ReadOnly.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (Required.HasValue && bitTextField.InheritedParameterHasNotBeenSet(nameof(Required)))
        {
            bitTextField.Required = Required.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (Resizable.HasValue && bitTextField.HasNotBeenSet(nameof(Resizable)))
        {
            bitTextField.Resizable = Resizable.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (RevealPasswordAriaLabel.HasValue() && bitTextField.HasNotBeenSet(nameof(RevealPasswordAriaLabel)))
        {
            bitTextField.RevealPasswordAriaLabel = RevealPasswordAriaLabel;
        }

        if (RevealPasswordIcon is not null && bitTextField.HasNotBeenSet(nameof(RevealPasswordIcon)))
        {
            bitTextField.RevealPasswordIcon = RevealPasswordIcon;
        }

        if (RevealPasswordIconName.HasValue() && bitTextField.HasNotBeenSet(nameof(RevealPasswordIconName)))
        {
            bitTextField.RevealPasswordIconName = RevealPasswordIconName;
        }

        if (RevealPasswordTemplate is not null && bitTextField.HasNotBeenSet(nameof(RevealPasswordTemplate)))
        {
            bitTextField.RevealPasswordTemplate = RevealPasswordTemplate;
        }

        if (Rows.HasValue && bitTextField.HasNotBeenSet(nameof(Rows)))
        {
            bitTextField.Rows = Rows.Value;
        }

        if (SelectOnFocus.HasValue && bitTextField.HasNotBeenSet(nameof(SelectOnFocus)))
        {
            bitTextField.SelectOnFocus = SelectOnFocus.Value;
        }

        if (ShowClearButton.HasValue && bitTextField.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitTextField.ShowClearButton = ShowClearButton.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (ShowCount.HasValue && bitTextField.HasNotBeenSet(nameof(ShowCount)))
        {
            bitTextField.ShowCount = ShowCount.Value;
        }

        if (Size.HasValue && bitTextField.HasNotBeenSet(nameof(Size)))
        {
            bitTextField.Size = Size.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (SpellCheck.HasValue && bitTextField.HasNotBeenSet(nameof(SpellCheck)))
        {
            bitTextField.SpellCheck = SpellCheck.Value;
        }

        if (Styles is not null && bitTextField.HasNotBeenSet(nameof(Styles)))
        {
            bitTextField.Styles = Styles;

            bitTextField.StyleBuilder.Reset();
        }

        if (Suffix.HasValue() && bitTextField.HasNotBeenSet(nameof(Suffix)))
        {
            bitTextField.Suffix = Suffix;
        }

        if (SuffixTemplate is not null && bitTextField.HasNotBeenSet(nameof(SuffixTemplate)))
        {
            bitTextField.SuffixTemplate = SuffixTemplate;
        }

        if (ThrottleTime.HasValue && bitTextField.InheritedParameterHasNotBeenSet(nameof(ThrottleTime)))
        {
            bitTextField.ThrottleTime = ThrottleTime.Value;
        }

        if (Title.HasValue() && bitTextField.HasNotBeenSet(nameof(Title)))
        {
            bitTextField.Title = Title;
        }

        if (Trim.HasValue && bitTextField.HasNotBeenSet(nameof(Trim)))
        {
            bitTextField.Trim = Trim.Value;
        }

        if (Type.HasValue && bitTextField.HasNotBeenSet(nameof(Type)))
        {
            bitTextField.Type = Type.Value;

            bitTextField.ClassBuilder.Reset();

            elementTypeChanged = true;
        }

        // The type of the rendered element follows both the Type and the reveal button, so it is resolved
        // once after either of them was filled in rather than twice.
        if (elementTypeChanged)
        {
            bitTextField.SetElementType();
        }

        if (Underlined.HasValue && bitTextField.HasNotBeenSet(nameof(Underlined)))
        {
            bitTextField.Underlined = Underlined.Value;

            bitTextField.ClassBuilder.Reset();
        }

        if (Wrap.HasValue() && bitTextField.HasNotBeenSet(nameof(Wrap)))
        {
            bitTextField.Wrap = Wrap;
        }
    }
}
