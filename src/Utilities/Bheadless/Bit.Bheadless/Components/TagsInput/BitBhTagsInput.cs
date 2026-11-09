using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>Why a <see cref="BitBhTagsInput"/> refused a tag.</summary>
public enum BitBhTagRejectionReason
{
    /// <summary>The tag is already in the list (and <c>AllowDuplicates</c> is off).</summary>
    Duplicate,

    /// <summary>The list already holds <c>Max</c> tags.</summary>
    MaxReached,

    /// <summary>The <c>Validate</c> callback refused it.</summary>
    Invalid,
}

/// <summary>A tag a <see cref="BitBhTagsInput"/> refused, and why.</summary>
public sealed record BitBhTagRejection(string Tag, BitBhTagRejectionReason Reason);

/// <summary>
/// A headless tags input: a text input that turns what is typed into a list of removable tags (keywords,
/// e-mail recipients, labels), read and written as one list of strings.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Adding</b> — Enter or a <see cref="Delimiters"/> character adds the typed text; a pasted list is split into
///     tags; <see cref="AddOnBlur"/> adds what is left when the input is left.</item>
///   <item><b>Rules</b> — <see cref="Max"/>, duplicates (<see cref="AllowDuplicates"/>) and <see cref="Validate"/>; a refused tag
///     stays in the input and <see cref="OnTagRejected"/> says why.</item>
///   <item><b>Keyboard</b> (React Aria TagGroup model) — the tags are a grid of rows (<see cref="BitBhTagsInputList"/>): ← or
///     Backspace in the empty input moves to the last tag, ← / → / Home / End move between tags, Backspace / Delete remove the
///     focused tag and announce it, → past the last tag returns to the input.</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> wiring and one hidden input per tag (<see cref="Name"/>).</item>
/// </list>
///
/// <code>
/// &lt;BitBhTagsInput @bind-Values="tags" class="tags-input"&gt;
///     &lt;Template Context="t"&gt;
///         &lt;BitBhTagsInputList class="tags" aria-label="Keywords"&gt;
///             @for (var i = 0; i &lt; t.Values.Count; i++)
///             {
///                 &lt;BitBhTagsInputTag Index="i" class="tag"&gt;@t.Values[i] &lt;BitBhTagsInputTagRemove&gt;×&lt;/BitBhTagsInputTagRemove&gt;&lt;/BitBhTagsInputTag&gt;
///             }
///         &lt;/BitBhTagsInputList&gt;
///         &lt;BitBhTagsInputInput placeholder="Add a keyword" aria-label="Add a keyword" /&gt;
///     &lt;/Template&gt;
/// &lt;/BitBhTagsInput&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c>, <c>data-empty</c>,
/// <c>data-max-reached</c>, <c>data-focused</c> on the root.</para>
/// </summary>
public class BitBhTagsInput : BitBhComponentBase, IDisposable
{
    private BitBhFieldBinding? _binding;
    private IReadOnlyList<string> _values = [];
    private string _text = string.Empty;
    private BitBhTagsInputContext? _context;
    private bool _focusWithin;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>The tags. Supplying it makes the input controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IReadOnlyList<string>? Values { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> ValuesChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Values</c>).</summary>
    [Parameter]
    public Expression<Func<IReadOnlyList<string>?>>? ValuesExpression { get; set; }

    /// <summary>Initial tags when uncontrolled.</summary>
    [Parameter]
    public IEnumerable<string>? DefaultValues { get; set; }

    /// <summary>Fires whenever the tags change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> OnValuesChange { get; set; }

    /// <summary>Fires when a tag is refused (a duplicate, one too many, or one <see cref="Validate"/> refused).</summary>
    [Parameter]
    public EventCallback<BitBhTagRejection> OnTagRejected { get; set; }

    /// <summary>Characters that end a tag as they are typed or pasted. Default: <c>","</c>. A pasted line break always does.</summary>
    [Parameter]
    public string Delimiters { get; set; } = ",";

    /// <summary>The maximum number of tags.</summary>
    [Parameter]
    public int? Max { get; set; }

    /// <summary>Allows the same tag more than once. Default: false.</summary>
    [Parameter]
    public bool AllowDuplicates { get; set; }

    /// <summary>How tags are compared for duplicates. Default: <see cref="StringComparer.Ordinal"/>.</summary>
    [Parameter]
    public StringComparer? Comparer { get; set; }

    /// <summary>Decides whether a (trimmed) tag may be added, e.g. that it is an e-mail address.</summary>
    [Parameter]
    public Func<string, bool>? Validate { get; set; }

    /// <summary>Adds the typed text as a tag when the input loses focus. Default: false.</summary>
    [Parameter]
    public bool AddOnBlur { get; set; }

    /// <summary>The announcement made when a tag is removed. Default: <see cref="BitBhStrings.TagRemoved"/>.</summary>
    [Parameter]
    public Func<string, string>? RemovedMessage { get; set; }

    /// <summary>Disables the input.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the tags read-only: they can be read and focused, not added or removed.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the input invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the input required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Form field name: one hidden input per tag.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Reading direction (mirrors ← / →). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The parts, when they don't depend on the tags (a controlled list you loop over yourself).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The parts, rendered with the tags input's state: <c>&lt;Template Context="t"&gt;</c>.</summary>
    [Parameter]
    public RenderFragment<BitBhTagsInputContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current tags.</summary>
    public IReadOnlyList<string> CurrentValues => IsProvided(nameof(Values)) ? Values ?? [] : _values;

    internal string Text => _text;
    internal bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    internal bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    internal bool IsRequired => Required || (FieldContext?.Required ?? false);
    internal bool IsMaxReached => Max is { } max && CurrentValues.Count >= max;
    internal bool CanEdit => !IsDisabled && !ReadOnly;
    internal BitBhFieldContext? Field => FieldContext;
    internal BitBhDirection EffectiveDir => Dir ?? CascadedDir;
    private string? _inputId;

    internal string InputId => _inputId ?? FieldContext?.ControlId ?? $"{ComponentId}-input";

    internal void SetInputId(string? id)
    {
        if (_inputId == id) return;
        _inputId = id;
        StateHasChanged();
    }
    internal string TagId(int index) => $"{ComponentId}-tag-{index.ToString(CultureInfo.InvariantCulture)}";

    protected override void OnInitialized()
    {
        _values = DefaultValues?.ToArray() ?? [];
    }

    protected override void OnParametersSet()
    {
        if (ValuesExpression is not null || _binding is not null)
        {
            _binding ??= new BitBhFieldBinding(() => InvokeAsync(OnValidationStateChanged));
            _binding.Update(EditContext, ValuesExpression);
            _binding.ReportTo(FieldContext);
        }
    }

    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhTagsInputContext(this);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(25, "onfocusin", On<Microsoft.AspNetCore.Components.Web.FocusEventArgs>("onfocusin", () => SetFocusWithin(true)));
        builder.AddAttribute(26, "onfocusout", On<Microsoft.AspNetCore.Components.Web.FocusEventArgs>("onfocusout", () => SetFocusWithin(false)));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhTagsInputContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", BitBhContent.Pick(Template, ChildContent, _context));
        builder.CloseComponent();
        builder.CloseElement();

        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenRegion(60);
            foreach (var value in CurrentValues)
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "hidden");
                builder.AddAttribute(2, "name", Name);
                builder.AddAttribute(3, "value", value);
                builder.CloseElement();
            }
            builder.CloseRegion();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", ReadOnly);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        SetDataFlag(attrs, "empty", CurrentValues.Count == 0);
        SetDataFlag(attrs, "max-reached", IsMaxReached);
        SetDataFlag(attrs, "focused", _focusWithin);
        return attrs;
    }

    private void SetFocusWithin(bool value)
    {
        if (_focusWithin == value) return;
        _focusWithin = value;
        StateHasChanged();
    }

    // ── Changes ──────────────────────────────────────────────────────────────

    private async Task SetValuesAsync(IReadOnlyList<string> values)
    {
        if (!IsProvided(nameof(Values))) _values = values;
        await ValuesChanged.InvokeAsync(values);
        await OnValuesChange.InvokeAsync(values);
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    /// <summary>Adds a tag. Returns false (and raises <see cref="OnTagRejected"/>) when it was refused.</summary>
    public async Task<bool> AddAsync(string tag)
    {
        var (added, values) = await TryAppendAsync(CurrentValues, tag);
        if (added) await SetValuesAsync(values);
        return added;
    }

    private async Task<(bool Added, IReadOnlyList<string> Values)> TryAppendAsync(IReadOnlyList<string> values, string raw)
    {
        var tag = raw.Trim();
        if (tag.Length == 0 || !CanEdit) return (false, values);
        BitBhTagRejectionReason? reason =
            Max is { } max && values.Count >= max ? BitBhTagRejectionReason.MaxReached
            : !AllowDuplicates && values.Contains(tag, Comparer ?? StringComparer.Ordinal) ? BitBhTagRejectionReason.Duplicate
            : Validate is not null && !Validate(tag) ? BitBhTagRejectionReason.Invalid
            : null;
        if (reason is { } r)
        {
            await OnTagRejected.InvokeAsync(new BitBhTagRejection(tag, r));
            return (false, values);
        }
        return (true, [.. values, tag]);
    }

    /// <summary>Removes the tag at <paramref name="index"/>.</summary>
    public async Task RemoveAtAsync(int index)
    {
        var values = CurrentValues;
        if (!CanEdit || index < 0 || index >= values.Count) return;
        var removed = values[index];
        await SetValuesAsync([.. values.Take(index), .. values.Skip(index + 1)]);
        if (GetService<IBitBhAnnouncer>() is { } announcer)
            await announcer.AnnounceAsync(RemovedMessage?.Invoke(removed) ?? Strings.TagRemoved(removed));
    }

    /// <summary>Removes every tag.</summary>
    public async Task ClearAsync()
    {
        if (!CanEdit || CurrentValues.Count == 0) return;
        await SetValuesAsync([]);
        await Interop.FocusAsync(InputId);
    }

    internal async Task OnTextInputAsync(string? text)
    {
        text ??= string.Empty;
        var separators = Delimiters.ToCharArray().Append('\n').Append('\r').ToArray();
        if (text.IndexOfAny(separators) < 0)
        {
            _text = text;
            StateHasChanged();
            return;
        }

        // Every complete piece becomes a tag; refused ones stay in the input with the unfinished rest.
        var parts = text.Split(separators);
        var values = CurrentValues;
        var kept = new List<string>();
        var changed = false;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Trim().Length == 0) continue;
            var (added, next) = await TryAppendAsync(values, parts[i]);
            if (added) { values = next; changed = true; }
            else kept.Add(parts[i].Trim());
        }
        kept.Add(parts[^1].TrimStart());
        _text = string.Join(Delimiters.Length > 0 ? Delimiters[0] + " " : " ", kept.Where(k => k.Length > 0));
        if (changed) await SetValuesAsync(values);
        else StateHasChanged();
    }

    internal async Task CommitTextAsync()
    {
        if (_text.Trim().Length == 0) return;
        if (await AddAsync(_text))
        {
            _text = string.Empty;
            StateHasChanged();
        }
    }

    internal async Task HandleInputBlurAsync()
    {
        if (AddOnBlur) await CommitTextAsync();
    }

    internal async Task HandleInputKeyDownAsync(string key)
    {
        if (key == "Enter")
        {
            await CommitTextAsync();
            return;
        }
        var back = EffectiveDir == BitBhDirection.Rtl ? "ArrowRight" : "ArrowLeft";
        // Leaving the empty input for the tags: ← or Backspace reach the last one.
        if (_text.Length == 0 && (key == back || key == "Backspace") && CurrentValues.Count > 0)
            await Interop.FocusAsync(TagId(CurrentValues.Count - 1));
    }

    internal async Task HandleTagKeyDownAsync(int index, string key)
    {
        var count = CurrentValues.Count;
        var rtl = EffectiveDir == BitBhDirection.Rtl;
        var previous = rtl ? "ArrowRight" : "ArrowLeft";
        var next = rtl ? "ArrowLeft" : "ArrowRight";
        if (key == previous) await FocusTagAsync(Math.Max(0, index - 1));
        else if (key == next) await FocusTagAsync(index + 1);
        else if (key == "Home") await FocusTagAsync(0);
        else if (key == "End") await FocusTagAsync(count - 1);
        else if (key is "Backspace" or "Delete" && CanEdit)
        {
            await RemoveAtAsync(index);
            // Backspace goes on with the tag before, Delete with the one after (now at this index).
            var target = key == "Backspace" ? index - 1 : index;
            if (target < 0 && CurrentValues.Count > 0) target = 0;
            await FocusTagAsync(target);
        }
    }

    private Task FocusTagAsync(int index)
        => index >= 0 && index < CurrentValues.Count ? Interop.FocusAsync(TagId(index)).AsTask() : Interop.FocusAsync(InputId).AsTask();

    public virtual void Dispose() => _binding?.Dispose();
}

/// <summary>The state of a <see cref="BitBhTagsInput"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhTagsInputContext
{
    internal BitBhTagsInputContext(BitBhTagsInput owner) => Owner = owner;

    internal BitBhTagsInput Owner { get; }

    /// <summary>The current tags.</summary>
    public IReadOnlyList<string> Values => Owner.CurrentValues;

    /// <summary>The text being typed.</summary>
    public string Text => Owner.Text;

    /// <summary>Whether no more tags can be added.</summary>
    public bool IsMaxReached => Owner.IsMaxReached;

    /// <summary>Whether the input is disabled.</summary>
    public bool Disabled => Owner.IsDisabled;

    /// <summary>Adds a tag.</summary>
    public Task<bool> AddAsync(string tag) => Owner.AddAsync(tag);

    /// <summary>Removes the tag at an index.</summary>
    public Task RemoveAtAsync(int index) => Owner.RemoveAtAsync(index);

    /// <summary>Removes every tag.</summary>
    public Task ClearAsync() => Owner.ClearAsync();
}
