using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitCheckbox is a component that permits the user to make a binary choice, a choice between one of two possible mutually exclusive options.
/// It supports an indeterminate state, three-state cycling, per-state icons, cancellable changes, read-only and required modes,
/// and is keyboard accessible through its underlying native checkbox input.
/// </summary>
public partial class BitCheckbox : BitInputBase<bool>
{
    private string _inputId = string.Empty;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Detailed description of the checkbox for the benefit of screen readers, rendered as a visually
    /// hidden element that the checkbox input points to via <c>aria-describedby</c>.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// ID for element that contains label information for the checkbox
    /// </summary>
    [Parameter] public string? AriaLabelledby { get; set; }

    /// <summary>
    /// The position in the parent set (if in a set) for aria-posinset
    /// </summary>
    [Parameter] public int? AriaPositionInSet { get; set; }

    /// <summary>
    /// The total size of the parent set (if in a set) for aria-setsize
    /// </summary>
    [Parameter] public int? AriaSetSize { get; set; }

    /// <summary>
    /// If true, the checkbox input automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Gets or sets the check icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CheckIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CheckIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: CheckIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: CheckIcon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: CheckIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? CheckIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon to render as the check mark inside the checkbox.
    /// </summary>
    [Parameter] public string? CheckIconName { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? CheckIconAriaLabel { get; set; }

    /// <summary>
    /// Used to customize the content of checkbox(Label and Box).
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitCheckbox.
    /// </summary>
    [Parameter] public BitCheckboxClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the checkbox.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default indeterminate visual state for checkbox
    /// </summary>
    [Parameter] public bool? DefaultIndeterminate { get; set; }

    /// <summary>
    /// An indeterminate visual state for checkbox.
    /// The indeterminate state takes visual precedence over the checked state but does not affect the Value.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    [CallOnSet(nameof(OnSetIndeterminate))]
    public bool Indeterminate { get; set; }

    /// <summary>
    /// Gets or sets the icon to render in the indeterminate state using custom CSS classes for external icon libraries,
    /// replacing the default filled square. Takes precedence over <see cref="IndeterminateIconName"/> when both are set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? IndeterminateIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon to render in the indeterminate state, replacing the default filled square.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? IndeterminateIconName { get; set; }

    /// <summary>
    /// Descriptive label for the checkbox.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The position of the label in regards to the checkbox box.
    /// Takes precedence over <see cref="Reversed"/> when both are set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Used to customize the label for the checkbox.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Callback invoked before the state of the checkbox changes, letting the change be cancelled.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitCheckboxChangeArgs"/> to keep the current state.
    /// Since the callback is awaited, it can also run asynchronous work like a confirmation prompt.
    /// </remarks>
    [Parameter] public EventCallback<BitCheckboxChangeArgs> OnChanging { get; set; }

    /// <summary>
    ///  Callback that is called when the check box is clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Reverses the label and checkbox location.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The size of the checkbox.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCheckbox.
    /// </summary>
    [Parameter] public BitCheckboxClassStyles? Styles { get; set; }

    /// <summary>
    /// Enables cycling through the unchecked, checked and indeterminate states on each click,
    /// instead of the indeterminate state being reachable only programmatically.
    /// </summary>
    [Parameter] public bool ThreeState { get; set; }

    /// <summary>
    /// Title text applied to the label container of the checkbox.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the icon to render in the unchecked state using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="UncheckedIconName"/> when both are set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? UncheckedIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon to render in the unchecked state.
    /// By default the unchecked box is empty and previews the check icon on hover.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? UncheckedIconName { get; set; }



    protected override async Task OnInitializedAsync()
    {
        _inputId = $"BitCheckbox-{UniqueId}-input";

        OnValueChanged += HandleOnValueChanged;

        SetDefaultValue();

        if (IndeterminateHasBeenSet is false && DefaultIndeterminate is not null)
        {
            await SetIndeterminate(DefaultIndeterminate.Value);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetIndeterminate();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override string RootElementClass => "bit-chb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-chb-pri",
            BitColor.Secondary => "bit-chb-sec",
            BitColor.Tertiary => "bit-chb-ter",
            BitColor.Info => "bit-chb-inf",
            BitColor.Success => "bit-chb-suc",
            BitColor.Warning => "bit-chb-wrn",
            BitColor.SevereWarning => "bit-chb-swr",
            BitColor.Error => "bit-chb-err",
            BitColor.PrimaryBackground => "bit-chb-pbg",
            BitColor.SecondaryBackground => "bit-chb-sbg",
            BitColor.TertiaryBackground => "bit-chb-tbg",
            BitColor.PrimaryForeground => "bit-chb-pfg",
            BitColor.SecondaryForeground => "bit-chb-sfg",
            BitColor.TertiaryForeground => "bit-chb-tfg",
            BitColor.PrimaryBorder => "bit-chb-pbr",
            BitColor.SecondaryBorder => "bit-chb-sbr",
            BitColor.TertiaryBorder => "bit-chb-tbr",
            _ => "bit-chb-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-chb-sm",
            BitSize.Medium => "bit-chb-md",
            BitSize.Large => "bit-chb-lg",
            _ => "bit-chb-md"
        });

        ClassBuilder.Register(() => CurrentValue ? $"bit-chb-ckd {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => Indeterminate ? $"bit-chb-ind {Classes?.Indeterminate}" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-chb-rvs" : string.Empty);

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Top => "bit-chb-ltp",
            BitLabelPosition.Bottom => "bit-chb-lbt",
            BitLabelPosition.Start => "bit-chb-lst",
            BitLabelPosition.End => "bit-chb-lnd",
            _ => string.Empty
        });

        ClassBuilder.Register(() => ReadOnly ? "bit-chb-rdl" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-chb-req" : string.Empty);

        ClassBuilder.Register(() => (UncheckedIcon is not null || UncheckedIconName.HasValue()) ? "bit-chb-uci" : string.Empty);

        ClassBuilder.Register(() => (IndeterminateIcon is not null || IndeterminateIconName.HasValue()) ? "bit-chb-cii" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => CurrentValue ? Styles?.Checked : string.Empty);

        StyleBuilder.Register(() => Indeterminate ? Styles?.Indeterminate : string.Empty);
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out bool result, [NotNullWhen(false)] out string? parsingErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");



    private BitIconInfo? GetStateIcon()
    {
        if (Indeterminate)
        {
            return BitIconInfo.From(IndeterminateIcon, IndeterminateIconName);
        }

        if (CurrentValue is false)
        {
            var uncheckedIcon = BitIconInfo.From(UncheckedIcon, UncheckedIconName);

            if (uncheckedIcon is not null) return uncheckedIcon;
        }

        return BitIconInfo.From(CheckIcon, CheckIconName ?? "Accept");
    }

    private (bool value, bool indeterminate) GetNextState()
    {
        if (ThreeState)
        {
            if (Indeterminate) return (false, false);

            if (CurrentValue) return (false, true);

            return (true, false);
        }

        return (CurrentValue is false, false);
    }

    private async Task SetIndeterminate()
    {
        await _js.BitUtilsSetProperty(InputElement, "indeterminate", Indeterminate);
    }

    private void OnSetIndeterminate()
    {
        _ = SetIndeterminate();
    }

    private async Task HandleOnCheckboxClick(MouseEventArgs args)
    {
        if (IsEnabled is false || ReadOnly) return;

        await OnClick.InvokeAsync(args);

        var oldValue = CurrentValue;
        var (newValue, newIndeterminate) = GetNextState();

        if (OnChanging.HasDelegate)
        {
            var changingArgs = new BitCheckboxChangeArgs(newValue, newIndeterminate);

            await OnChanging.InvokeAsync(changingArgs);

            if (changingArgs.Cancel)
            {
                await SyncInputCheckedProperty(oldValue);
                // the browser also clears the native indeterminate property on every click, so it gets put back too
                await SetIndeterminate();
                return;
            }
        }

        if (newIndeterminate != Indeterminate)
        {
            await SetIndeterminate(newIndeterminate);
        }

        CurrentValue = newValue;

        await SyncInputCheckedProperty(oldValue);
    }

    private async Task SyncInputCheckedProperty(bool oldValue)
    {
        // the browser toggles the native checked property on every click; when the committed value is not
        // that toggle result (a cancelled, one-way bound or three-state change), the property gets put back
        if (CurrentValue == (oldValue is false)) return;

        await _js.BitUtilsSetProperty(InputElement, "checked", CurrentValue);
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        ClassBuilder.Reset();
        StyleBuilder.Reset();
    }

    private async Task SetIndeterminate(bool value)
    {
        if (await AssignIndeterminate(value) is false) return;

        await _js.BitUtilsSetProperty(InputElement, "indeterminate", value);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        OnValueChanged -= HandleOnValueChanged;

        await base.DisposeAsync(disposing);
    }
}
