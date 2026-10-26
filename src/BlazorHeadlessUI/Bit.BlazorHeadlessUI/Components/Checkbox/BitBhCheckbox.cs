using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless, accessible Checkbox (<c>role="checkbox"</c>) with tri-state support.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Binding</b> — <c>@bind-Checked</c> with <c>EditForm</c> validation, or uncontrolled with <see cref="BitBhCheckableBase.DefaultChecked"/>.</item>
///   <item><b>Indeterminate</b> — <see cref="Indeterminate"/> renders <c>aria-checked="mixed"</c> / <c>data-state="indeterminate"</c>; activating it checks the box.</item>
///   <item><b>Groups</b> — inside a <see cref="BitBhCheckboxGroup"/> the checkbox toggles its <see cref="BitBhCheckableBase.Value"/> in the group's values;
///     <see cref="SelectAll"/> turns it into the group's tri-state "select all" box.</item>
///   <item><b>Keyboard</b> — Space toggles; Enter doesn't (APG).</item>
///   <item><b>Forms</b> — <see cref="BitBhField"/> wiring and a hidden input for plain posts.</item>
/// </list>
///
/// <code>
/// &lt;BitBhField&gt;
///     &lt;BitBhCheckbox @bind-Checked="accepted"&gt;&lt;BitBhCheckboxIndicator&gt;✓&lt;/BitBhCheckboxIndicator&gt;&lt;/BitBhCheckbox&gt;
///     &lt;BitBhLabel&gt;I accept the terms&lt;/BitBhLabel&gt;
/// &lt;/BitBhField&gt;
/// </code>
/// </summary>
public class BitBhCheckbox : BitBhCheckableBase
{
    [CascadingParameter]
    private BitBhCheckboxGroupContext? Group { get; set; }

    /// <summary>Shows the mixed state (<c>aria-checked="mixed"</c>). Activating checks the box.</summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>
    /// Inside a <see cref="BitBhCheckboxGroup"/>: makes this the group's "select all" box. It is
    /// checked when every value is checked, mixed when some are, and toggling it checks or
    /// clears them all.
    /// </summary>
    [Parameter]
    public bool SelectAll { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhCheckboxRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhCheckboxRenderContext>? Template { get; set; }

    protected override string Role => "checkbox";

    private bool IsSelectAll => SelectAll && Group is not null;
    private bool InGroup => Group is not null && !SelectAll && IsProvided(nameof(Value));

    /// <summary>Whether the box is checked (from the group when inside a <see cref="BitBhCheckboxGroup"/>).</summary>
    public bool IsCheckedEffective => IsSelectAll ? Group!.AllChecked : InGroup ? Group!.IsChecked(Value) : IsChecked;

    private bool EffectiveIndeterminate => IsSelectAll ? Group!.SomeChecked : Indeterminate;

    /// <inheritdoc />
    protected override bool IsDisabled => base.IsDisabled || (Group?.Disabled ?? false);

    // In a group the group is the field's control: its checkboxes don't each claim the field's id and label.
    protected override bool JoinsField => Group is null;

    internal bool IsBoxDisabled => IsDisabled;

    internal string BoxId => ControlId;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // The group offers the values of the checkboxes it has now ("select all"), and focuses the first one
        // when its field label is clicked.
        Group?.Register(this, InGroup ? Value : null);
    }

    protected override bool NextChecked() => EffectiveIndeterminate || !IsCheckedEffective;

    // A native indeterminate checkbox posts only when it is checked as well.
    protected override bool ShouldSubmit => !IsSelectAll && IsCheckedEffective;

    protected override async Task SetCheckedAsync(bool value)
    {
        if (IsDisabled) return;
        if (IsSelectAll)
        {
            await Group!.SetAll(value);
            await OnCheckedChange.InvokeAsync(value);
            return;
        }
        if (InGroup)
        {
            await Group!.SetAsync(Value, value);
            await OnCheckedChange.InvokeAsync(value);
            return;
        }
        await base.SetCheckedAsync(value);
    }

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        var context = new BitBhCheckboxRenderContext
        {
            IsChecked = IsCheckedEffective,
            IsIndeterminate = EffectiveIndeterminate,
            Disabled = IsDisabled,
        };
        builder.OpenComponent<CascadingValue<BitBhCheckboxRenderContext>>(sequence);
        builder.AddComponentParameter(sequence + 1, "Value", context);
        builder.AddComponentParameter(sequence + 2, "ChildContent", BitBhContent.Pick(Template, ChildContent, context));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-checked"] = EffectiveIndeterminate ? "mixed" : IsCheckedEffective ? "true" : "false";
        if (EffectiveIndeterminate)
            SetDataValue(attrs, "state", "indeterminate");
        else
            SetDataState(attrs, IsCheckedEffective, "checked", "unchecked");
        return attrs;
    }

    public override void Dispose()
    {
        base.Dispose();
        Group?.Unregister(this);
    }
}

/// <summary>Render-fragment context exposed by <see cref="BitBhCheckbox"/> (and cascaded to <see cref="BitBhCheckboxIndicator"/>).</summary>
public sealed record BitBhCheckboxRenderContext
{
    /// <summary>Whether the box is checked.</summary>
    public required bool IsChecked { get; init; }

    /// <summary>Whether the box shows the mixed state.</summary>
    public required bool IsIndeterminate { get; init; }

    /// <summary>Whether the box is disabled.</summary>
    public required bool Disabled { get; init; }
}
