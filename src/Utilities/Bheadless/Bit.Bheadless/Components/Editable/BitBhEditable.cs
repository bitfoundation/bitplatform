using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// Headless inline editing (Ark / Reka Editable): text shown as a preview that turns into an input when activated
/// - a title, a cell, a label - committed with Enter or on blur, cancelled with Escape.
///
/// <code>
/// &lt;BitBhEditable @bind-Value="title" Placeholder="Untitled" class="editable"&gt;
///     &lt;BitBhEditablePreview class="editable-preview" /&gt;
///     &lt;BitBhEditableInput class="editable-input" aria-label="Title" /&gt;
///     &lt;BitBhEditableEditTrigger&gt;Rename&lt;/BitBhEditableEditTrigger&gt;
/// &lt;/BitBhEditable&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-editing</c>, <c>data-empty</c> (shows the placeholder), <c>data-disabled</c>,
/// <c>data-readonly</c> on every part. Focus moves into the input when editing starts and back to the preview (or the
/// edit trigger) when it ends.</para>
/// </summary>
public class BitBhEditable : BitBhComponentBase
{
    private string _value = string.Empty;
    private string _draft = string.Empty;
    private bool _editing;
    private BitBhEditableContext? _context;
    private string? _returnFocusId;
    private bool _ignoreNextFocus;

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>The value. Supplying it makes the component controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/> (called when an edit is committed).</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Initial value when uncontrolled.</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Fires when an edit is committed with a changed value.</summary>
    [Parameter]
    public EventCallback<string> OnValueCommit { get; set; }

    /// <summary>Fires when an edit is cancelled.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>Whether it is being edited. Supplying it makes the editing state controlled (use <c>@bind-Editing</c>).</summary>
    [Parameter]
    public bool Editing { get; set; }

    /// <summary>Two-way binding callback for <see cref="Editing"/>.</summary>
    [Parameter]
    public EventCallback<bool> EditingChanged { get; set; }

    /// <summary>What starts editing from the preview. Default: <see cref="BitBhEditableActivation.Focus"/>.</summary>
    [Parameter]
    public BitBhEditableActivation ActivationMode { get; set; }

    /// <summary>What commits an edit. Default: <see cref="BitBhEditableSubmitMode.Both"/>.</summary>
    [Parameter]
    public BitBhEditableSubmitMode SubmitMode { get; set; }

    /// <summary>Selects the text when editing starts. Default: true.</summary>
    [Parameter]
    public bool SelectOnFocus { get; set; } = true;

    /// <summary>Text the preview shows while the value is empty (and the input's placeholder).</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Decides whether a draft may be committed (e.g. not empty).</summary>
    [Parameter]
    public Func<string, bool>? Validate { get; set; }

    /// <summary>Disables editing.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the value without allowing edits.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>The parts.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The parts, rendered with the component's state.</summary>
    [Parameter]
    public RenderFragment<BitBhEditableContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The committed value.</summary>
    public string CurrentValue => IsProvided(nameof(Value)) ? Value ?? string.Empty : _value;

    /// <summary>Whether it is being edited.</summary>
    public bool IsEditing => IsProvided(nameof(Editing)) ? Editing : _editing;

    internal string Draft => _draft;
    internal bool IsDisabled => Disabled || (FieldsetContext?.Disabled ?? false);
    internal bool CanEdit => !IsDisabled && !ReadOnly;
    internal string InputId => $"{ComponentId}-input";
    internal string PreviewId => $"{ComponentId}-preview";

    protected override void OnInitialized() => _value = DefaultValue ?? string.Empty;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhEditableContext(this);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhEditableContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", BitBhContent.Pick(Template, ChildContent, _context));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        AddState(attrs);
        return attrs;
    }

    internal void AddState(Dictionary<string, object> attrs)
    {
        SetDataFlag(attrs, "editing", IsEditing);
        SetDataFlag(attrs, "empty", CurrentValue.Length == 0);
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", ReadOnly);
    }

    /// <summary>Starts editing (focuses the input). <paramref name="returnFocusId"/> gets the focus back when it ends.</summary>
    public async Task EditAsync(string? returnFocusId = null)
    {
        if (!CanEdit || IsEditing) return;
        _draft = CurrentValue;
        _returnFocusId = returnFocusId ?? PreviewId;
        await SetEditingAsync(true);
        await Interop.FocusAsync(InputId, select: SelectOnFocus);
    }

    /// <summary>Commits the draft (if <see cref="Validate"/> allows it) and ends editing.</summary>
    public async Task SubmitAsync()
    {
        if (!IsEditing) return;
        if (Validate?.Invoke(_draft) == false) return;
        var changed = _draft != CurrentValue;
        if (!IsProvided(nameof(Value))) _value = _draft;
        await SetEditingAsync(false);
        if (changed)
        {
            await ValueChanged.InvokeAsync(_draft);
            await OnValueCommit.InvokeAsync(_draft);
        }
        await RestoreFocusAsync();
    }

    /// <summary>Drops the draft and ends editing.</summary>
    public async Task CancelAsync()
    {
        if (!IsEditing) return;
        _draft = CurrentValue;
        await SetEditingAsync(false);
        await OnCancel.InvokeAsync();
        await RestoreFocusAsync();
    }

    private async Task RestoreFocusAsync()
    {
        if (_returnFocusId is not { } id) return;
        _returnFocusId = null;
        // Focus going back to a preview that edits on focus must not start editing again.
        _ignoreNextFocus = id == PreviewId && ActivationMode == BitBhEditableActivation.Focus;
        await Interop.FocusAsync(id);
    }

    internal Task HandlePreviewFocusAsync()
    {
        if (!_ignoreNextFocus) return EditAsync();
        _ignoreNextFocus = false;
        return Task.CompletedTask;
    }

    private async Task SetEditingAsync(bool editing)
    {
        if (!IsProvided(nameof(Editing))) _editing = editing;
        await EditingChanged.InvokeAsync(editing);
        StateHasChanged();
    }

    internal void SetDraft(string? text)
    {
        _draft = text ?? string.Empty;
        StateHasChanged();
    }

    internal Task HandleInputKeyDownAsync(KeyboardEventArgs e) => e.Key switch
    {
        "Enter" when SubmitMode is BitBhEditableSubmitMode.Both or BitBhEditableSubmitMode.Enter => SubmitAsync(),
        "Escape" => CancelAsync(),
        _ => Task.CompletedTask,
    };

    internal Task HandleInputBlurAsync() => SubmitMode switch
    {
        BitBhEditableSubmitMode.Both or BitBhEditableSubmitMode.Blur => SubmitAsync(),
        BitBhEditableSubmitMode.Enter => CancelAsync(),
        _ => Task.CompletedTask,
    };
}
