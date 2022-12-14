using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitToggle
{
    private string? _labelledById;
    private string? _stateText;
    private string? _buttonId => $"{UniqueId}_button";
    private string? _labelId => $"{UniqueId}_label";
    private string? _stateTextId => $"{UniqueId}_state_text";
    private string? _ariaChecked => CurrentValueAsString;

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

    protected override string RootElementClass => "bit-tgl";

    protected override async Task OnInitializedAsync()
    {
        SetTexts();

        OnValueChanged += HandleOnValueChanged;

        await base.OnInitializedAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() =>
        {
            var isCheckedClass = Value ? "checked" : "unchecked";
            var isEnabledClass = IsEnabled ? "enabled" : "disabled";
            return $"{RootElementClass}-{isEnabledClass}-{isCheckedClass}-{VisualClassRegistrar()}";
        });

        ClassBuilder.Register(() => IsInlineLabel
                                    ? $"{RootElementClass}-inline-{VisualClassRegistrar()}" 
                                    : string.Empty);

        ClassBuilder.Register(() => OnText.HasNoValue() || OffText.HasNoValue()
                                    ? $"{RootElementClass}-noonoff-{VisualClassRegistrar()}" 
                                    : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true 
                                    ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" 
                                    : string.Empty);
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        SetTexts();
        ClassBuilder.Reset();
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false || ValueChanged.HasDelegate is false) return;
        CurrentValue = !CurrentValue;
        await OnChange.InvokeAsync(CurrentValue);
    }

    private void SetTexts()
    {
        _stateText = (CurrentValue ? OnText : OffText) ?? DefaultText;

        if (AriaLabel.HasNoValue())
        {
            if (Label.HasValue())
            {
                _labelledById = _labelId;
            }
            if (_stateText.HasValue())
            {
                _labelledById = _labelledById.HasValue() ? $"{_labelId} {_stateTextId}" : _stateTextId;
            }
        }
    }

    protected override string? FormatValueAsString(bool value) => value.ToString().ToLower(CultureInfo.CurrentUICulture);

    /// <inheritdoc />
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
