using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitRadioButtonGroup
{
    private bool _isComponentRendered;
    private bool _isRequired;
    private BitRadioButtonOption? _selectedOption;
    private List<BitRadioButtonOption> _allOptions = new();
    public string _labelId = default!;

    /// <summary>
    /// ID of an element to use as the aria label for this RadioButtonGroup.
    /// </summary>
    [Parameter] public string AriaLabelledBy { get; set; } = string.Empty;

    /// <summary>
    /// The content of RadioButtonGroup, common values are RadioButtonGroup component 
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Default value for RadioButtonGroup.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// If true, an option must be selected in the RadioButtonGroup.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => _isRequired;
        set
        {
            _isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Descriptive label for the RadioButtonGroup.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Used to customize the label for the RadioButtonGroup.
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    /// <summary>
    /// Name of RadioButtonGroup, this name is used to group each RadioButtonGroup into the same logical RadioButtonGroup
    /// </summary>
    [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Callback that is called when the value parameter is changed
    /// </summary>
    [Parameter] public EventCallback<string> OnValueChange { get; set; }

    protected override string RootElementClass => "bit-rbg";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled && IsRequired
                                   ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        CurrentValue ??= DefaultValue;
        _labelId = $"RadioButtonGroupLabel{UniqueId}";
        OnValueChanging += HandleOnValueChanging;
        OnValueChanged += HandleOnValueChanged;

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isComponentRendered = true;
            if (_allOptions.Any(option => option.Value == Value) is false)
            {
                ResetValue();
                if (ValueHasBeenSet && ValueChanged.HasDelegate)
                {
                    await ValueChanged.InvokeAsync(Value);
                }
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    internal async Task SelectOption(BitRadioButtonOption option)
    {
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        _selectedOption?.SetState(false);
        option.SetState(true);

        _selectedOption = option;
        CurrentValue = option.Value;

        await OnValueChange.InvokeAsync(CurrentValue);
    }

    internal void RegisterOption(BitRadioButtonOption option)
    {
        if (option.Value.HasNoValue())
        {
            option.Value = _allOptions.Count.ToString(CultureInfo.InvariantCulture);
        }

        if (CurrentValue == option.Value)
        {
            option.SetState(true);
            _selectedOption = option;
        }

        _allOptions.Add(option);
    }

    internal void UnregisterOption(BitRadioButtonOption option)
    {
        _allOptions.Remove(option);
    }

    private void SelectOptionByKey(string? value)
    {
        var newOption = _allOptions.FirstOrDefault(i => i.Value == value);

        if (newOption is null || newOption == _selectedOption || newOption.IsEnabled is false)
        {
            _ = ValueChanged.InvokeAsync(Value);
            return;
        }

        _ = SelectOption(newOption);
    }

    private string GetAriaLabelledBy() => Label.HasValue() || LabelFragment is not null ? _labelId : AriaLabelledBy;

    private void HandleOnValueChanging(object? sender, ValueChangingEventArgs<string?> args)
    {
        if (_isComponentRendered is false) return;

        var option = _allOptions.FirstOrDefault(i => i.Value == args.Value);
        if (option is not null)
        {
            _selectedOption?.SetState(false);
            option.SetState(true);
            _selectedOption = option;

            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            _ = OnValueChange.InvokeAsync(args.Value);
        }
        else
        {
            args.ShouldChange = false;

            if (Value.HasNoValue())
            {
                _selectedOption?.SetState(false);
                _selectedOption = null;
            }

            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
            if (Value == args.Value) return;

            _ = ValueChanged.InvokeAsync(Value);
        }
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        SelectOptionByKey(CurrentValue);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnValueChanging -= HandleOnValueChanging;
            OnValueChanged -= HandleOnValueChanged;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");
}
