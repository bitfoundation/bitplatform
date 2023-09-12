using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitToggle
{
    private string? _labelledById;
    private string? _stateText;
    private string? _buttonId;
    private string? _labelId;
    private string? _stateTextId;
    private string? _ariaChecked => CurrentValueAsString;


    /// <summary>
    /// Custom CSS classes for different parts of the BitToggle.
    /// </summary>
    [Parameter] public BitToggleClassStyles? Classes { get; set; }

    /// <summary>
    /// Default text of the toggle when it is neither ON or OFF.
    /// </summary>
    [Parameter] public string? DefaultText { get; set; }

    /// <summary>
    /// Whether the label (not the onText/offText) should be positioned inline with the toggle control.
    /// Left (right in RTL) side when on/off text provided VS right (left in RTL) side when there is no on/off text.
    /// </summary>
    [Parameter] public bool IsInlineLabel { get; set; }

    /// <summary>
    /// Label of the toggle.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom label of the toggle.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Text to display when toggle is ON.
    /// </summary>
    [Parameter] public string? OnText { get; set; }

    /// <summary>
    /// Text to display when toggle is OFF.
    /// </summary>
    [Parameter] public string? OffText { get; set; }

    /// <summary>
    /// Callback that is called when the checked value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Denotes role of the toggle, default is switch.        
    /// </summary>
    [Parameter] public string? Role { get; set; } = "switch";

    /// <summary>
    /// Custom CSS styles for different parts of the BitToggle.
    /// </summary>
    [Parameter] public BitToggleClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-tgl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => CurrentValue ? $"{RootElementClass}-chk {Classes?.Check}" : string.Empty);

        ClassBuilder.Register(() => IsInlineLabel ? $"{RootElementClass}-inl" : string.Empty);

        ClassBuilder.Register(() => OnText.HasNoValue() || OffText.HasNoValue() ? $"{RootElementClass}-noo" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => CurrentValue ? Styles?.Check : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _labelId = $"BitToggle-{UniqueId}-label";
        _buttonId = $"BitToggle-{UniqueId}-button";
        _stateTextId = $"BitToggle-{UniqueId}-state-text";

        SetTexts();

        OnValueChanged += HandleOnValueChanged;

        await base.OnInitializedAsync();
    }



    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        SetTexts();

        ClassBuilder.Reset();
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValue = !CurrentValue;

        await OnChange.InvokeAsync(CurrentValue);
    }

    private void SetTexts()
    {
        _stateText = (CurrentValue ? OnText : OffText) ?? DefaultText;

        if (AriaLabel.HasValue()) return;

        _labelledById = $"{(Label.HasValue() ? _labelId : "")} {(_stateText.HasValue() ? _stateTextId : "")}".Trim();
    }

    protected override string? FormatValueAsString(bool value) => value.ToString().ToLower(CultureInfo.CurrentUICulture);

    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnValueChanged -= HandleOnValueChanged;
        }

        base.Dispose(disposing);
    }
}
