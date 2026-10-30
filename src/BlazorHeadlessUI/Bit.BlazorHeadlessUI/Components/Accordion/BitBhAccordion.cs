using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless Accordion: a vertically (or horizontally) stacked set of
/// expandable sections implementing the WAI-ARIA Accordion pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Single or multiple</b> open items (<see cref="Type"/>); <see cref="Collapsible"/> controls whether the last open item can be closed in single mode.</item>
///   <item><b>Binding</b> — <c>@bind-Value</c> (single) / <c>@bind-Values</c> (multiple), or uncontrolled defaults.</item>
///   <item><b>Keyboard</b> — ArrowDown / ArrowUp (ArrowRight / ArrowLeft when horizontal), Home and End move between triggers.</item>
///   <item><b>Semantics</b> — triggers live in headings (<see cref="BitBhAccordionHeader"/>), panels are <c>role="region"</c> labelled by their trigger.</item>
///   <item><b>Animations</b> — exit animations and <c>--bit-bh-content-height</c> on the content.</item>
/// </list>
///
/// <code>
/// &lt;BitBhAccordion DefaultValue="item-1"&gt;
///     &lt;BitBhAccordionItem Value="item-1"&gt;
///         &lt;BitBhAccordionHeader&gt;&lt;BitBhAccordionTrigger&gt;Section 1&lt;/BitBhAccordionTrigger&gt;&lt;/BitBhAccordionHeader&gt;
///         &lt;BitBhAccordionContent&gt;Content for section 1.&lt;/BitBhAccordionContent&gt;
///     &lt;/BitBhAccordionItem&gt;
/// &lt;/BitBhAccordion&gt;
/// </code>
/// </summary>
public class BitBhAccordion : BitBhComponentBase
{
    private readonly BitBhCollection<BitBhAccordionTrigger> _triggers;
    private HashSet<string> _open = new(StringComparer.Ordinal);

    public BitBhAccordion()
    {
        _triggers = new BitBhCollection<BitBhAccordionTrigger>(BitBhId.New("bit-bh-acc"));
    }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>Whether one or many items can be open. Default: <see cref="BitBhAccordionType.Single"/>.</summary>
    [Parameter]
    public BitBhAccordionType Type { get; set; } = BitBhAccordionType.Single;

    /// <summary>In single mode, whether the open item can be closed (leaving none open). Default: true.</summary>
    [Parameter]
    public bool Collapsible { get; set; } = true;

    /// <summary>Disables every item.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Orientation used for arrow-key navigation. Default: vertical.</summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; } = BitBhOrientation.Vertical;

    /// <summary>Reading direction (flips Left / Right when horizontal).</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Initially open item when uncontrolled (single mode).</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Initially open items when uncontrolled (multiple mode).</summary>
    [Parameter]
    public IEnumerable<string>? DefaultValues { get; set; }

    /// <summary>The open item (single mode). Supplying it makes the accordion controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>The open items (multiple mode). Supplying it makes the accordion controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IEnumerable<string>? Values { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ValuesChanged { get; set; }

    /// <summary>Fires when the open item changes (single mode).</summary>
    [Parameter]
    public EventCallback<string?> OnValueChange { get; set; }

    /// <summary>Fires when the open items change (multiple mode).</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> OnValuesChange { get; set; }

    /// <summary>The accordion items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    private ISet<string> OpenItems
    {
        get
        {
            if (Type == BitBhAccordionType.Single && IsProvided(nameof(Value)))
                return Value is null ? new HashSet<string>() : new HashSet<string>(StringComparer.Ordinal) { Value };
            if (Type == BitBhAccordionType.Multiple && IsProvided(nameof(Values)))
                return new HashSet<string>(Values ?? [], StringComparer.Ordinal);
            return _open;
        }
    }

    protected override void OnInitialized()
    {
        if (DefaultValue is not null) _open.Add(DefaultValue);
        if (DefaultValues is not null) _open.UnionWith(DefaultValues);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var open = OpenItems;
        var context = new BitBhAccordionContext(
            isOpen: open.Contains,
            toggle: v => _ = ToggleAsync(v),
            disabled: Disabled,
            type: Type)
        {
            Accordion = this,
            Orientation = Orientation,
            Collapsible = Collapsible,
            Triggers = _triggers,
        };

        builder.OpenComponent<CascadingValue<BitBhAccordionContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    internal async Task ToggleAsync(string value)
    {
        if (Disabled) return;
        var next = new HashSet<string>(OpenItems, StringComparer.Ordinal);

        if (next.Contains(value))
        {
            if (Type == BitBhAccordionType.Single && !Collapsible) return;
            next.Remove(value);
        }
        else
        {
            if (Type == BitBhAccordionType.Single) next.Clear();
            next.Add(value);
        }

        if (Type == BitBhAccordionType.Single)
        {
            var single = next.FirstOrDefault();
            if (!IsProvided(nameof(Value))) _open = next;
            await ValueChanged.InvokeAsync(single);
            await OnValueChange.InvokeAsync(single);
        }
        else
        {
            var values = next.ToArray();
            if (!IsProvided(nameof(Values))) _open = next;
            await ValuesChanged.InvokeAsync(values);
            await OnValuesChange.InvokeAsync(values);
        }
        StateHasChanged();
    }

    internal async Task HandleTriggerKeyDownAsync(BitBhAccordionTrigger trigger, KeyboardEventArgs e)
    {
        var next = BitBhKeyboard.NextKey(Orientation, EffectiveDir);
        var prev = BitBhKeyboard.PreviousKey(Orientation, EffectiveDir);
        await _triggers.EnsureOrderAsync(Interop);

        BitBhAccordionTrigger? target = null;
        if (e.Key == next) target = _triggers.Next(trigger);
        else if (e.Key == prev) target = _triggers.Previous(trigger);
        else if (e.Key == "Home") target = _triggers.First();
        else if (e.Key == "End") target = _triggers.Last();

        if (target is not null)
            await Interop.FocusAsync(target.ItemId);
    }
}
