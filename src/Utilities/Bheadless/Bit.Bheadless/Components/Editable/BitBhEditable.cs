using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>What starts editing a <see cref="BitBhEditable"/> from its preview.</summary>
public enum BitBhEditableActivation
{
    /// <summary>Focusing the preview (Tab or click).</summary>
    Focus,

    /// <summary>Clicking the preview, or Enter / Space on it.</summary>
    Click,

    /// <summary>Double-clicking the preview, or Enter on it.</summary>
    DoubleClick,

    /// <summary>Only a <see cref="BitBhEditableEditTrigger"/> (or code) does.</summary>
    None,
}

/// <summary>What commits an edit of a <see cref="BitBhEditable"/> (Escape always cancels).</summary>
public enum BitBhEditableSubmitMode
{
    /// <summary>Enter, or leaving the input.</summary>
    Both,

    /// <summary>Enter only (leaving the input cancels).</summary>
    Enter,

    /// <summary>Leaving the input only (Enter does nothing).</summary>
    Blur,

    /// <summary>Only a <see cref="BitBhEditableSubmitTrigger"/> (or code) does.</summary>
    None,
}

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

/// <summary>The state of a <see cref="BitBhEditable"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhEditableContext
{
    internal BitBhEditableContext(BitBhEditable owner) => Owner = owner;

    internal BitBhEditable Owner { get; }

    /// <summary>The committed value.</summary>
    public string Value => Owner.CurrentValue;

    /// <summary>What is being typed.</summary>
    public string Draft => Owner.Draft;

    /// <summary>Whether it is being edited.</summary>
    public bool IsEditing => Owner.IsEditing;

    /// <summary>Starts editing.</summary>
    public Task EditAsync() => Owner.EditAsync();

    /// <summary>Commits the draft.</summary>
    public Task SubmitAsync() => Owner.SubmitAsync();

    /// <summary>Cancels the edit.</summary>
    public Task CancelAsync() => Owner.CancelAsync();
}

/// <summary>
/// The value of a <see cref="BitBhEditable"/> while it isn't edited (its placeholder while empty). Activates editing as
/// the editable's <c>ActivationMode</c> says; hidden while editing.
/// </summary>
public class BitBhEditablePreview : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhEditableContext? Context { get; set; }

    /// <summary>Content replacing the value text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = Context?.Owner;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.PreviewId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
        {
            switch (owner.ActivationMode)
            {
                case BitBhEditableActivation.Focus:
                    builder.AddAttribute(30, "onfocus", On<FocusEventArgs>("onfocus", owner.HandlePreviewFocusAsync));
                    break;
                case BitBhEditableActivation.Click:
                    builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => owner.EditAsync()));
                    builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.Key is "Enter" or " " ? owner.EditAsync() : Task.CompletedTask));
                    break;
                case BitBhEditableActivation.DoubleClick:
                    builder.AddAttribute(30, "ondblclick", On<MouseEventArgs>("ondblclick", () => owner.EditAsync()));
                    builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.Key is "Enter" ? owner.EditAsync() : Task.CompletedTask));
                    break;
            }
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (ChildContent is not null) builder.AddContent(50, ChildContent);
        else if (owner is not null) builder.AddContent(50, owner.CurrentValue.Length > 0 ? owner.CurrentValue : owner.Placeholder);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        if (owner.IsEditing) attrs["hidden"] = true;
        if (owner.CanEdit && owner.ActivationMode != BitBhEditableActivation.None)
        {
            attrs["tabindex"] = "0";
            if (owner.ActivationMode != BitBhEditableActivation.Focus)
            {
                attrs["role"] = "button";
                attrs[BitBhKeyboard.KeysAttribute] = owner.ActivationMode == BitBhEditableActivation.Click ? "Enter Space" : "Enter";
            }
        }
        attrs["data-bit-bh-interactive"] = "";
        owner.AddState(attrs);
        return attrs;
    }
}

/// <summary>
/// The input of a <see cref="BitBhEditable"/>, shown while editing. Enter commits and Escape cancels (as the
/// editable's <c>SubmitMode</c> says); give it a name with <c>aria-label</c>.
/// </summary>
public class BitBhEditableInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhEditableContext? Context { get; set; }

    protected override string DefaultTag => "input";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = Context?.Owner;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
        {
            builder.AddAttribute(30, "value", owner.Draft);
            builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => owner.SetDraft(e.Value?.ToString())));
            builder.SetUpdatesAttributeName("value");
            builder.AddAttribute(32, "onkeydown", On<KeyboardEventArgs>("onkeydown", owner.HandleInputKeyDownAsync));
            builder.AddAttribute(33, "onblur", On<FocusEventArgs>("onblur", owner.HandleInputBlurAsync));
            // Escape ends the edit only: a dialog around it stays open.
            builder.AddEventStopPropagationAttribute(34, "onkeydown", owner.IsEditing);
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "text";
        if (!owner.IsEditing) attrs["hidden"] = true;
        if (!string.IsNullOrEmpty(owner.Placeholder)) attrs["placeholder"] = owner.Placeholder;
        if (owner.IsDisabled) attrs["disabled"] = true;
        // Enter and Escape belong to the edit, not to a form or a dialog around it.
        attrs[BitBhKeyboard.KeysAttribute] = owner.SubmitMode is BitBhEditableSubmitMode.Both or BitBhEditableSubmitMode.Enter ? "Enter Escape" : "Escape";
        owner.AddState(attrs);
        return attrs;
    }
}

/// <summary>Shared behaviour of the <see cref="BitBhEditable"/> trigger buttons.</summary>
public abstract class BitBhEditableTriggerBase : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhEditableContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The owning editable.</summary>
    protected BitBhEditable? Owner => Context?.Owner;

    protected override string DefaultTag => "button";

    /// <summary>Whether the trigger is shown while editing (submit / cancel) or while not (edit).</summary>
    protected abstract bool ShownWhileEditing { get; }

    /// <summary>What the trigger does.</summary>
    protected abstract Task ActivateAsync(BitBhEditable owner);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => Owner is { } owner ? ActivateAsync(owner) : Task.CompletedTask));
        // The input's blur would commit or cancel before this click: the press keeps the focus in the input.
        if (ShownWhileEditing)
        {
            builder.AddAttribute(31, "onmousedown", On<MouseEventArgs>("onmousedown", static () => { }));
            builder.AddEventPreventDefaultAttribute(32, "onmousedown", true);
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Owner is not { } owner) return attrs;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (!owner.CanEdit) attrs["disabled"] = true;
        }
        if (owner.IsEditing != ShownWhileEditing) attrs["hidden"] = true;
        attrs["data-bit-bh-interactive"] = "";
        owner.AddState(attrs);
        return attrs;
    }
}

/// <summary>Starts editing a <see cref="BitBhEditable"/>; shown while it isn't edited, and gets the focus back after.</summary>
public class BitBhEditableEditTrigger : BitBhEditableTriggerBase
{
    protected override bool ShownWhileEditing => false;

    protected override Task ActivateAsync(BitBhEditable owner) => owner.EditAsync(ComponentId);
}

/// <summary>Commits the edit of a <see cref="BitBhEditable"/>; shown while editing.</summary>
public class BitBhEditableSubmitTrigger : BitBhEditableTriggerBase
{
    protected override bool ShownWhileEditing => true;

    protected override Task ActivateAsync(BitBhEditable owner) => owner.SubmitAsync();
}

/// <summary>Cancels the edit of a <see cref="BitBhEditable"/>; shown while editing.</summary>
public class BitBhEditableCancelTrigger : BitBhEditableTriggerBase
{
    protected override bool ShownWhileEditing => true;

    protected override Task ActivateAsync(BitBhEditable owner) => owner.CancelAsync();
}
