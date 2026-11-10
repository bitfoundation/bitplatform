using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>Which characters a <see cref="BitBhPinInput"/> accepts.</summary>
public enum BitBhPinInputType
{
    /// <summary>Digits only (0–9). Phones show a numeric keypad.</summary>
    Numeric,

    /// <summary>Letters and digits.</summary>
    Alphanumeric,

    /// <summary>Letters only.</summary>
    Alphabetic,
}

/// <summary>
/// A headless PIN / one-time-code input: one single-character input per position (<see cref="BitBhPinInputSlot"/>),
/// read and written as one string.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Typing</b> — each character moves on to the next position; characters the <see cref="Type"/> doesn't
///     allow are refused as they are typed.</item>
///   <item><b>Paste and autofill</b> — a pasted code, or one the phone fills from a text message
///     (<c>autocomplete="one-time-code"</c>), spreads over the positions.</item>
///   <item><b>Keyboard</b> — ← / → / Home / End move between positions, Backspace on an empty position deletes the
///     one before it. The group is one tab stop: the first empty position.</item>
///   <item><b>Completion</b> — <see cref="OnComplete"/> fires when every position is filled, and
///     <see cref="AutoSubmit"/> submits the enclosing form.</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> wiring and a hidden input (<see cref="Name"/>).</item>
/// </list>
///
/// <code>
/// &lt;BitBhPinInput @bind-Value="code" Otp="true" OnComplete="VerifyAsync" class="pin"&gt;
///     @for (var i = 0; i &lt; 6; i++)
///     {
///         &lt;BitBhPinInputSlot class="pin-slot" /&gt;
///     }
/// &lt;/BitBhPinInput&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-complete</c>, <c>data-disabled</c>, <c>data-invalid</c> on the root and the slots;
/// <c>data-filled</c> and <c>data-index</c> on each slot.</para>
/// </summary>
public class BitBhPinInput : BitBhComponentBase, IDisposable
{
    private readonly List<BitBhPinInputSlot> _slots = new();
    private BitBhFieldBinding? _binding;
    private string _value = string.Empty;
    private BitBhPinInputContext? _context;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>The code. Supplying it makes the input controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>Initial code when uncontrolled.</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Fires whenever the code changes.</summary>
    [Parameter]
    public EventCallback<string> OnValueChange { get; set; }

    /// <summary>Fires when every position is filled, with the complete code.</summary>
    [Parameter]
    public EventCallback<string> OnComplete { get; set; }

    /// <summary>Which characters are accepted. Default: <see cref="BitBhPinInputType.Numeric"/>.</summary>
    [Parameter]
    public BitBhPinInputType Type { get; set; }

    /// <summary>
    /// A regular expression one character must match, replacing <see cref="Type"/> (e.g. <c>"[0-9A-F]"</c> for hex).
    /// </summary>
    [Parameter]
    public string? Pattern { get; set; }

    /// <summary>Converts typed characters to upper case. Default: false.</summary>
    [Parameter]
    public bool UpperCase { get; set; }

    /// <summary>Hides the characters (<c>type="password"</c>), for PINs.</summary>
    [Parameter]
    public bool Mask { get; set; }

    /// <summary>
    /// Marks the input as a one-time code: the browser and the phone offer a code received by text message
    /// (<c>autocomplete="one-time-code"</c>).
    /// </summary>
    [Parameter]
    public bool Otp { get; set; }

    /// <summary>Submits the enclosing form when the last position is filled.</summary>
    [Parameter]
    public bool AutoSubmit { get; set; }

    /// <summary>Selects a position's character when it is focused, so typing replaces it. Default: true.</summary>
    [Parameter]
    public bool SelectOnFocus { get; set; } = true;

    /// <summary>The placeholder of every empty position (e.g. "○").</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The accessible name of a position from its index (0-based) and the count. Default: "Character 2 of 6"
    /// (the first one takes the field's label inside a <see cref="BitBhField"/>).
    /// </summary>
    [Parameter]
    public Func<int, int, string>? SlotLabel { get; set; }

    /// <summary>Disables the input.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the input read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the input invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the input required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Form field name: a hidden input carries the code.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Reading direction (mirrors ← / →). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The <see cref="BitBhPinInputSlot"/>s, and anything between them (a separator).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current code.</summary>
    public string CurrentValue => Clip(IsProvided(nameof(Value)) ? Value ?? string.Empty : _value);

    /// <summary>The number of positions.</summary>
    public int Length => _slots.Count;

    /// <summary>Whether every position is filled.</summary>
    public bool IsComplete => _slots.Count > 0 && CurrentValue.Length >= _slots.Count;

    internal bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    internal bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    internal bool IsRequired => Required || (FieldContext?.Required ?? false);
    internal BitBhFieldContext? Field => FieldContext;
    internal BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    internal string CharPattern => Pattern ?? Type switch
    {
        BitBhPinInputType.Alphanumeric => "[\\p{L}\\p{Nd}]",
        BitBhPinInputType.Alphabetic => "\\p{L}",
        _ => "[0-9]",
    };

    private string Clip(string value) => _slots.Count > 0 && value.Length > _slots.Count ? value[.._slots.Count] : value;

    protected override void OnInitialized()
    {
        _value = DefaultValue ?? string.Empty;
        // The label focuses the first empty position.
        FieldContext?.SetControlFocus(FocusAsync);
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

    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    /// <summary>Focuses the first empty position (the last one when the code is complete).</summary>
    public Task FocusAsync() => _slots.Count == 0 ? Task.CompletedTask : Interop.FocusAsync(SlotId(TabStopIndex), select: SelectOnFocus).AsTask();

    /// <summary>Clears the code and focuses the first position.</summary>
    public async Task ClearAsync()
    {
        await SetValueAsync(string.Empty);
        if (_slots.Count > 0) await Interop.FocusAsync(SlotId(0));
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhPinInputContext(this);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhPinInputContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenElement(60, "input");
            builder.AddAttribute(61, "type", "hidden");
            builder.AddAttribute(62, "name", Name);
            builder.AddAttribute(63, "value", CurrentValue);
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        if (FieldContext?.LabelledBy is { } label && GetAdditionalAttribute("aria-label") is null) attrs["aria-labelledby"] = label;
        if (FieldContext?.DescribedBy is { } description) attrs["aria-describedby"] = description;
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataFlag(attrs, "complete", IsComplete);
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", ReadOnly);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        return attrs;
    }

    // ── Slots ────────────────────────────────────────────────────────────────

    internal int Register(BitBhPinInputSlot slot)
    {
        if (!_slots.Contains(slot))
        {
            _slots.Add(slot);
            // A slot that joins changes every slot's "n of m" name and the tab stop.
            if (_slots.Count > 1) StateHasChanged();
        }
        return _slots.IndexOf(slot);
    }

    internal void Unregister(BitBhPinInputSlot slot)
    {
        if (_slots.Remove(slot)) StateHasChanged();
    }

    internal int IndexOf(BitBhPinInputSlot slot) => _slots.IndexOf(slot);

    internal string SlotId(int index) => $"{ComponentId}-slot-{index.ToString(CultureInfo.InvariantCulture)}";

    internal string CharAt(int index) => index >= 0 && index < CurrentValue.Length ? CurrentValue[index].ToString() : string.Empty;

    /// <summary>The position the group's tab stop is on: the first empty one, or the last.</summary>
    internal int TabStopIndex => Math.Min(CurrentValue.Length, Math.Max(0, _slots.Count - 1));

    internal string SlotName(int index) => SlotLabel?.Invoke(index, _slots.Count)
        ?? Strings.PinInputSlot(index, _slots.Count);

    internal bool IsAllowed(char c)
    {
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(c.ToString(), $"^(?:{CharPattern})$");
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private string Sanitize(string text)
    {
        var chars = text.Where(c => !char.IsWhiteSpace(c) && c != '-').Select(c => UpperCase ? char.ToUpperInvariant(c) : c);
        return string.Concat(chars.Where(IsAllowed));
    }

    /// <summary>A slot's input changed: one character typed, one deleted, or a code pasted / filled in.</summary>
    internal async Task HandleInputAsync(int index, string? text)
    {
        if (IsDisabled || ReadOnly || index < 0) return;
        var current = CurrentValue;
        var previous = CharAt(index);
        text ??= string.Empty;

        if (text.Length == 0)
        {
            // The position's character was deleted: the code closes up behind it.
            if (index < current.Length) await SetValueAsync(current.Remove(index, 1));
            else StateHasChanged();
            return;
        }

        // Typing into a filled position adds to its character: keep the new one.
        if (text.Length == 2 && previous.Length == 1 && (text[0].ToString() == previous || text[1].ToString() == previous))
            text = text[0].ToString() == previous ? text[1..] : text[..1];

        var incoming = Sanitize(text);
        if (incoming.Length == 0)
        {
            // Nothing usable (refused characters slipped through, e.g. pasted): the position shows what it had.
            StateHasChanged();
            return;
        }

        // There are no gaps: input at a position past the code goes to the first empty one.
        var at = Math.Min(index, current.Length);
        var prefix = current[..at];
        var suffix = at + incoming.Length < current.Length ? current[(at + incoming.Length)..] : string.Empty;
        var next = Clip(prefix + incoming + suffix);
        await SetValueAsync(next);

        var focus = Math.Min(at + incoming.Length, _slots.Count - 1);
        if (focus != index || at != index) await Interop.FocusAsync(SlotId(focus), select: SelectOnFocus);

        if (next.Length >= _slots.Count && _slots.Count > 0 && current.Length < _slots.Count)
        {
            await OnComplete.InvokeAsync(next);
            if (AutoSubmit) await Interop.RequestSubmitAsync(SlotId(0));
        }
    }

    internal async Task HandleKeyDownAsync(int index, string key)
    {
        if (IsDisabled) return;
        var rtl = EffectiveDir == BitBhDirection.Rtl;
        var target = key switch
        {
            "ArrowLeft" => rtl ? index + 1 : index - 1,
            "ArrowRight" => rtl ? index - 1 : index + 1,
            "Home" => 0,
            "End" => TabStopIndex,
            _ => -2,
        };
        if (key == "Backspace" && CharAt(index).Length == 0 && index > 0 && !ReadOnly)
        {
            // Backspace on an empty position deletes the character before it.
            var current = CurrentValue;
            if (index - 1 < current.Length) await SetValueAsync(current.Remove(index - 1, 1));
            target = index - 1;
        }
        if (target < 0 || target >= _slots.Count || target == index) return;
        // Positions past the code can't be reached: there is nothing there to change.
        target = Math.Min(target, TabStopIndex);
        await Interop.FocusAsync(SlotId(target), select: SelectOnFocus);
    }

    /// <summary>A position got focus: one past the code sends it back to the first empty one.</summary>
    internal async Task HandleFocusAsync(int index)
    {
        if (index > TabStopIndex) await Interop.FocusAsync(SlotId(TabStopIndex), select: SelectOnFocus);
    }

    private async Task SetValueAsync(string value)
    {
        value = Clip(value);
        if (value == CurrentValue)
        {
            StateHasChanged();
            return;
        }
        if (!IsProvided(nameof(Value))) _value = value;
        await ValueChanged.InvokeAsync(value);
        await OnValueChange.InvokeAsync(value);
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    public virtual void Dispose()
    {
        _binding?.Dispose();
        FieldContext?.SetControlFocus(null);
    }
}

/// <summary>The state a <see cref="BitBhPinInput"/> shares with its <see cref="BitBhPinInputSlot"/>s.</summary>
public sealed class BitBhPinInputContext
{
    internal BitBhPinInputContext(BitBhPinInput owner) => Owner = owner;

    internal BitBhPinInput Owner { get; }

    /// <summary>The current code.</summary>
    public string Value => Owner.CurrentValue;

    /// <summary>The number of positions.</summary>
    public int Length => Owner.Length;

    /// <summary>Whether every position is filled.</summary>
    public bool IsComplete => Owner.IsComplete;
}
