using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A toggle represents a physical switch that allows someone to choose between two mutually exclusive options.  For example, “On/Off”, “Show/Hide”.
/// </summary>
public partial class BitToggle : BitInputBase<bool>
{
    private string? _labelId;
    private string? _buttonId;
    private string? _stateText;
    private string? _stateTextId;
    private string? _labelledById;
    private string? _ariaChecked => CurrentValueAsString;



    /// <summary>
    /// Custom CSS classes for different parts of the toggle.
    /// </summary>
    [Parameter] public BitToggleClassStyles? Classes { get; set; }

    /// <summary>
    /// The default value of the toggle when the value parameter has not been assigned.
    /// </summary>
    [Parameter] public bool? DefaultValue { get; set; }

    /// <summary>
    /// Renders the toggle in full width of its container while putting space between the label and the knob.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Renders the label and the knob in a single line together.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Inline { get; set; }

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
    /// Reverses the positions of the label and input of the toggle.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Denotes role of the toggle, default is switch.        
    /// </summary>
    [Parameter] public string? Role { get; set; } = "switch";

    /// <summary>
    /// Custom CSS styles for different parts of the toggle.
    /// </summary>
    [Parameter] public BitToggleClassStyles? Styles { get; set; }

    /// <summary>
    /// The default text used when the On or Off texts are null.
    /// </summary>
    [Parameter] public string? Text { get; set; }



    protected override string RootElementClass => "bit-tgl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => CurrentValue ? $"bit-tgl-chk {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-tgl-fwi" : string.Empty);

        ClassBuilder.Register(() => Inline ? "bit-tgl-inl" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-tgl-rvs" : string.Empty);
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

        if (ValueHasBeenSet is false && DefaultValue is not null)
        {
            Value = DefaultValue.Value;
        }

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
        StyleBuilder.Reset();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;

        CurrentValue = !CurrentValue;
    }

    private void SetStateText()
    {
        _stateText = (CurrentValue ? OnText : OffText) ?? Text;

        if (AriaLabel.HasValue()) return;

        _labelledById = $"{(Label.HasValue() ? _labelId : "")} {(_stateText.HasValue() ? _stateTextId : "")}".Trim();
    }
}
