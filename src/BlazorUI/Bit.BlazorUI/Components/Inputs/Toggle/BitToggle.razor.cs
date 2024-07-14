using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitToggle : BitInputBase<bool>
{
    private string? _labelId;
    private string? _buttonId;
    private string? _stateText;
    private string? _stateTextId;
    private string? _labelledById;
    private string? _ariaChecked => CurrentValueAsString;



    /// <summary>
    /// Custom CSS classes for different parts of the BitToggle.
    /// </summary>
    [Parameter] public BitToggleClassStyles? Classes { get; set; }

    /// <summary>
    /// Default text used when the On or Off texts are null.
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
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => CurrentValue ? $"{RootElementClass}-chk {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => IsInlineLabel ? $"{RootElementClass}-inl" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => CurrentValue ? Styles?.Checked : string.Empty);
    }

    protected override void OnInitialized()
    {
        _labelId = $"BitToggle-{UniqueId}-label";
        _buttonId = $"BitToggle-{UniqueId}-button";
        _stateTextId = $"BitToggle-{UniqueId}-state-text";

        SetStateText();

        OnValueChanged += HandleOnValueChanged;

        base.OnInitialized();
    }

    protected override string? FormatValueAsString(bool value) => value.ToString().ToLower(CultureInfo.CurrentUICulture);

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out bool result, [NotNullWhen(false)] out string? parsingErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnValueChanged -= HandleOnValueChanged;
        }

        base.Dispose(disposing);
    }



    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        SetStateText();

        ClassBuilder.Reset();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValue = !CurrentValue;
    }

    private void SetStateText()
    {
        _stateText = (CurrentValue ? OnText : OffText) ?? DefaultText;

        if (AriaLabel.HasValue()) return;

        _labelledById = $"{(Label.HasValue() ? _labelId : "")} {(_stateText.HasValue() ? _stateTextId : "")}".Trim();
    }
}
