using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless, accessible Radio Group implementing the WAI-ARIA Radio Group pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Roving tabindex</b> — the checked radio (or the first enabled one) is the group's single tab stop.</item>
///   <item><b>Keyboard</b> — every arrow key moves and checks (Down / Right next, Up / Left previous, mirrored in RTL), wrapping; Space checks the focused radio; disabled radios are skipped.</item>
///   <item><b>Forms</b> — <c>@bind-Value</c> with <c>EditForm</c> validation, <see cref="BitBhField"/> labels / descriptions, a hidden input for plain posts (<see cref="Name"/>).</item>
///   <item><b>Data attributes</b> — <c>data-orientation</c>, <c>data-disabled</c>; radios emit <c>data-state="checked|unchecked"</c>.</item>
/// </list>
///
/// <code>
/// &lt;BitBhRadioGroup @bind-Value="size" aria-label="Size"&gt;
///     &lt;BitBhRadio Value="s"&gt;Small&lt;/BitBhRadio&gt;
///     &lt;BitBhRadio Value="m"&gt;Medium&lt;/BitBhRadio&gt;
/// &lt;/BitBhRadioGroup&gt;
/// </code>
/// </summary>
public class BitBhRadioGroup : BitBhComponentBase, IDisposable
{
    private readonly BitBhCollection<BitBhRadio> _radios = new(BitBhId.New("bit-bh-radios"));
    private BitBhFieldBinding? _binding;
    private string? _value;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The checked value. Supplying it makes the group controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Initial value when uncontrolled.</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>Fires whenever the value changes.</summary>
    [Parameter]
    public EventCallback<string?> OnValueChange { get; set; }

    /// <summary>Disables every radio.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the group required (<c>aria-required</c>).</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Marks the group invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Vertical layout (default). Only affects <c>aria-orientation</c> / <c>data-orientation</c> — all arrows work.</summary>
    [Parameter]
    public bool Vertical { get; set; } = true;

    /// <summary>Whether arrow keys wrap. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction (mirrors Left / Right). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Form field name: renders a hidden input carrying the value.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The radios.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private string? SelectedValue => IsProvided(nameof(Value)) ? Value : _value;
    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    private bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    private bool IsRequired => Required || (FieldContext?.Required ?? false);
    private BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    protected override void OnInitialized()
    {
        _value = DefaultValue;
        FieldContext?.SetControlFocus(FocusAsync);
    }

    /// <summary>Focuses the checked radio, or the first enabled one.</summary>
    public async Task FocusAsync()
    {
        await _radios.EnsureOrderAsync(Interop);
        if (_radios.Items.FirstOrDefault(IsTabStop) is { } radio)
            await Interop.FocusAsync(radio.ItemId);
    }

    protected override void OnParametersSet()
    {
        if (ValueExpression is not null || _binding is not null)
        {
            _binding ??= new BitBhFieldBinding(() => InvokeAsync(OnValidationStateChanged));
            _binding.Update(EditContext, ValueExpression);
            _binding.ReportTo(FieldContext);
        }
    }

    // Validation also changes without this control changing (a submit, an async validator): the field's
    // error message and data-invalid follow it as well as the group's aria-invalid.
    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    protected override Task OnAfterRenderAsync(bool firstRender) => _radios.EnsureOrderAsync(Interop);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhRadioGroupContext(
            selectedValue: SelectedValue,
            disabled: IsDisabled,
            required: IsRequired,
            orientation: Vertical ? BitBhRadioGroupOrientation.Vertical : BitBhRadioGroupOrientation.Horizontal,
            group: this,
            radios: _radios);

        builder.OpenComponent<CascadingValue<BitBhRadioGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", Id is null && FieldContext is not null ? FieldContext.ControlId : ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            // Like native disabled radios, a disabled group posts nothing.
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(SelectedValue) && !IsDisabled)
            {
                inner.OpenElement(50, "input");
                inner.AddAttribute(51, "type", "hidden");
                inner.AddAttribute(52, "name", Name);
                inner.AddAttribute(53, "value", SelectedValue);
                inner.CloseElement();
            }
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "radiogroup";
        attrs["aria-orientation"] = Vertical ? "vertical" : "horizontal";
        if (FieldContext?.LabelledBy is { } label) attrs["aria-labelledby"] = label;
        if (FieldContext?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        if (IsRequired) attrs["aria-required"] = "true";
        if (IsInvalid) attrs["aria-invalid"] = "true";
        if (IsDisabled) attrs["aria-disabled"] = "true";
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataValue(attrs, "orientation", Vertical ? "vertical" : "horizontal");
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);
        return attrs;
    }

    internal bool IsTabStop(BitBhRadio radio)
    {
        var checkedRadio = _radios.Items.FirstOrDefault(r => r.Value == SelectedValue && !r.IsItemDisabled);
        return checkedRadio is not null ? checkedRadio == radio : _radios.First() == radio;
    }

    internal async Task SelectAsync(string value)
    {
        if (IsDisabled || value == SelectedValue) return;
        if (_radios.Items.FirstOrDefault(r => r.Value == value) is { IsItemDisabled: true }) return;

        if (!IsProvided(nameof(Value)))
            _value = value;
        await ValueChanged.InvokeAsync(value);
        await OnValueChange.InvokeAsync(value);
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    internal async Task HandleKeyDownAsync(BitBhRadio radio, KeyboardEventArgs e)
    {
        if (IsDisabled) return;
        var rtl = EffectiveDir == BitBhDirection.Rtl;

        BitBhRadio? target = e.Key switch
        {
            "ArrowDown" => _radios.Next(radio, Loop),
            "ArrowUp" => _radios.Previous(radio, Loop),
            "ArrowRight" => rtl ? _radios.Previous(radio, Loop) : _radios.Next(radio, Loop),
            "ArrowLeft" => rtl ? _radios.Next(radio, Loop) : _radios.Previous(radio, Loop),
            " " => radio,
            _ => null,
        };

        if (target is null) return;
        if (target != radio) await Interop.FocusAsync(target.ItemId);
        await SelectAsync(target.Value);
    }

    public void Dispose()
    {
        _binding?.Dispose();
        FieldContext?.SetControlFocus(null);
    }
}
