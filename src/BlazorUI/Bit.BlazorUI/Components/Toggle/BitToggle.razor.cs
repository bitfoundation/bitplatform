using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI;

public partial class BitToggle
{
    private Guid Id = Guid.NewGuid();
    private string? LabelledById;
    private string? StateText;
    private string LabelId => Id + "-label";
    private string StateTextId => Id + "-stateText";
    private string? AriaChecked => CurrentValueAsString;

    /// <summary>
    /// Default text of the toggle when it is neither ON or OFF.
    /// </summary>
    [Parameter] public string? DefaultText { get; set; }

    /// <summary>
    /// Label of the toggle.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom label of the toggle.
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    /// <summary>
    /// Denotes role of the toggle, default is switch.        
    /// </summary>
    [Parameter] public string? Role { get; set; } = "switch";

    /// <summary>
    /// Text to display when toggle is ON.
    /// </summary>
    [Parameter] public string? OnText { get; set; }

    /// <summary>
    /// Text to display when toggle is OFF.
    /// </summary>
    [Parameter] public string? OffText { get; set; }

    /// <summary>
    /// Whether the label (not the onText/offText) should be positioned inline with the toggle control. Left (right in RTL) side when on/off text provided VS right (left in RTL) side when there is no on/off text.
    /// </summary>
    [Parameter] public bool IsInlineLabel { get; set; }

    /// <summary>
    /// Callback that is called when the checked value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    protected override string RootElementClass => "bit-tgl";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() =>
        {
            var isCheckedClass = Value ? "checked" : "unchecked";
            var isEnabledClass = IsEnabled ? "enabled" : "disabled";
            return $"{RootElementClass}-{isEnabledClass}-{isCheckedClass}-{VisualClassRegistrar()}";
        });

        ClassBuilder.Register(() => IsInlineLabel ? $"{RootElementClass}-inline-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => OnText.HasNoValue() || OffText.HasNoValue()
                                        ? $"{RootElementClass}-noonoff-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false || ValueChanged.HasDelegate is false) return;
        CurrentValue = !CurrentValue;
        await OnChange.InvokeAsync(CurrentValue);
    }

    protected override async Task OnInitializedAsync()
    {
        SetTexts();

        OnCurrentValueChanged += HandleOnCurrentValueChanged;

        await base.OnInitializedAsync();
    }

    private void SetTexts()
    {
        StateText = (CurrentValue ? OnText : OffText) ?? DefaultText;

        if (AriaLabel.HasNoValue())
        {
            if (Label.HasValue())
            {
                LabelledById = LabelId;
            }
            if (StateText.HasValue())
            {
                LabelledById = LabelledById.HasValue() ? $"{LabelId} {StateTextId}" : StateTextId;
            }
        }
    }

    private void HandleOnCurrentValueChanged(object? sender, EventArgs args)
    {
        SetTexts();
        ClassBuilder.Reset();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");

    protected override string? FormatValueAsString(bool value) => value.ToString().ToLower(CultureInfo.CurrentUICulture);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnCurrentValueChanged -= HandleOnCurrentValueChanged;
        }

        base.Dispose(disposing);
    }
}
