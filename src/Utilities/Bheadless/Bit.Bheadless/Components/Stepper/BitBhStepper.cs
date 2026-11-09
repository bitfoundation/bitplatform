using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>Where a step of a <see cref="BitBhStepper"/> stands.</summary>
public enum BitBhStepState
{
    /// <summary>Not reached yet.</summary>
    Inactive,

    /// <summary>The current step.</summary>
    Active,

    /// <summary>Done (before the current one, or marked complete).</summary>
    Completed,
}

/// <summary>
/// A headless stepper for a multi-step flow (a checkout, a wizard): an ordered list of steps with their triggers,
/// the content of the current step, and next / previous buttons.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Binding</b> — <c>@bind-Step</c> (0-based), or uncontrolled with <see cref="DefaultStep"/>.</item>
///   <item><b>Linear</b> — with <see cref="Linear"/> a step can only be reached once the ones before it are complete
///     (<see cref="CanGoNext"/> or <see cref="CompletedSteps"/> decide).</item>
///   <item><b>ARIA</b> — the list is an <c>&lt;ol&gt;</c>, the current step's trigger has <c>aria-current="step"</c>;
///     content is labelled by its step's title.</item>
///   <item><b>Styling</b> — <c>data-state="active|completed|inactive"</c>, <c>data-orientation</c> on every part.</item>
/// </list>
///
/// <code>
/// &lt;BitBhStepper @bind-Step="step" Count="3"&gt;
///     &lt;BitBhStepperList&gt;
///         &lt;BitBhStepperItem Index="0"&gt;&lt;BitBhStepperTrigger&gt;Account&lt;/BitBhStepperTrigger&gt;&lt;/BitBhStepperItem&gt;
///         …
///     &lt;/BitBhStepperList&gt;
///     &lt;BitBhStepperContent Index="0"&gt;…&lt;/BitBhStepperContent&gt;
///     &lt;BitBhStepperPrevious&gt;Back&lt;/BitBhStepperPrevious&gt; &lt;BitBhStepperNext&gt;Next&lt;/BitBhStepperNext&gt;
/// &lt;/BitBhStepper&gt;
/// </code>
/// </summary>
public class BitBhStepper : BitBhComponentBase
{
    private readonly HashSet<int> _titles = new();
    private int _step;
    private BitBhStepperContext? _context;

    /// <summary>The current step (0-based). Supplying it makes the stepper controlled (use <c>@bind-Step</c>).</summary>
    [Parameter]
    public int Step { get; set; }

    /// <summary>Two-way binding callback for <see cref="Step"/>.</summary>
    [Parameter]
    public EventCallback<int> StepChanged { get; set; }

    /// <summary>Initial step when uncontrolled.</summary>
    [Parameter]
    public int DefaultStep { get; set; }

    /// <summary>Fires whenever the step changes.</summary>
    [Parameter]
    public EventCallback<int> OnStepChange { get; set; }

    /// <summary>
    /// Fires when every step is done: <see cref="BitBhStepperNext"/> pressed on the last step. The step stays on the
    /// last one; show a summary, submit …
    /// </summary>
    [Parameter]
    public EventCallback OnComplete { get; set; }

    /// <summary>The number of steps.</summary>
    [Parameter, EditorRequired]
    public int Count { get; set; }

    /// <summary>Steps can only be reached in order: a later step's trigger is disabled until the steps before it are complete.</summary>
    [Parameter]
    public bool Linear { get; set; }

    /// <summary>Decides whether the current step may be left forward (validate it here). Default: always.</summary>
    [Parameter]
    public Func<int, bool>? CanGoNext { get; set; }

    /// <summary>
    /// Steps marked complete regardless of the current step. By default every step before the current one is complete.
    /// </summary>
    [Parameter]
    public IReadOnlyCollection<int>? CompletedSteps { get; set; }

    /// <summary>Layout direction of the steps (<c>data-orientation</c>). Default: horizontal.</summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; }

    /// <summary>The parts, when they don't depend on the stepper's state.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The parts, rendered with the stepper's state.</summary>
    [Parameter]
    public RenderFragment<BitBhStepperContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current step.</summary>
    public int CurrentStep => Math.Clamp(IsProvided(nameof(Step)) ? Step : _step, 0, Math.Max(0, Count - 1));

    protected override void OnInitialized() => _step = DefaultStep;

    /// <summary>The state of step <paramref name="index"/>.</summary>
    public BitBhStepState StateOf(int index)
    {
        if (index == CurrentStep) return BitBhStepState.Active;
        if (CompletedSteps is not null) return CompletedSteps.Contains(index) ? BitBhStepState.Completed : BitBhStepState.Inactive;
        return index < CurrentStep ? BitBhStepState.Completed : BitBhStepState.Inactive;
    }

    /// <summary>Whether step <paramref name="index"/> can be gone to now.</summary>
    public bool CanGoTo(int index)
    {
        if (index < 0 || index >= Count) return false;
        if (!Linear || index <= CurrentStep) return true;
        // Forward in a linear stepper: every step before the target must be complete (the current one may be left).
        for (var i = 0; i < index; i++)
        {
            if (i == CurrentStep) { if (CanGoNext?.Invoke(i) == false) return false; continue; }
            if (StateOf(i) != BitBhStepState.Completed) return false;
        }
        return true;
    }

    /// <summary>Goes to step <paramref name="index"/> if it can be reached.</summary>
    public async Task GoToAsync(int index)
    {
        if (!CanGoTo(index) || index == CurrentStep) return;
        if (index > CurrentStep && CanGoNext?.Invoke(CurrentStep) == false) return;
        if (!IsProvided(nameof(Step))) _step = index;
        await StepChanged.InvokeAsync(index);
        await OnStepChange.InvokeAsync(index);
        StateHasChanged();
    }

    /// <summary>Goes to the next step, or raises <see cref="OnComplete"/> on the last one.</summary>
    public async Task NextAsync()
    {
        if (CanGoNext?.Invoke(CurrentStep) == false) return;
        if (CurrentStep >= Count - 1) await OnComplete.InvokeAsync();
        else await GoToAsync(CurrentStep + 1);
    }

    /// <summary>Goes to the previous step.</summary>
    public Task PreviousAsync() => GoToAsync(CurrentStep - 1);

    internal bool HasTitle(int index) => _titles.Contains(index);

    internal void RegisterTitle(int index, bool present)
    {
        if (present ? _titles.Add(index) : _titles.Remove(index)) StateHasChanged();
    }

    internal string TriggerId(int index) => $"{ComponentId}-trigger-{index.ToString(CultureInfo.InvariantCulture)}";

    internal string TitleId(int index) => $"{ComponentId}-title-{index.ToString(CultureInfo.InvariantCulture)}";

    internal string ContentId(int index) => $"{ComponentId}-content-{index.ToString(CultureInfo.InvariantCulture)}";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhStepperContext(this);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhStepperContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", BitBhContent.Pick(Template, ChildContent, _context));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataValue(attrs, "step", CurrentStep.ToString(CultureInfo.InvariantCulture));
        SetDataFlag(attrs, "linear", Linear);
        return attrs;
    }
}

/// <summary>The state of a <see cref="BitBhStepper"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhStepperContext
{
    internal BitBhStepperContext(BitBhStepper owner) => Owner = owner;

    internal BitBhStepper Owner { get; }

    /// <summary>The current step (0-based).</summary>
    public int Step => Owner.CurrentStep;

    /// <summary>The number of steps.</summary>
    public int Count => Owner.Count;

    /// <summary>Whether the current step is the first.</summary>
    public bool IsFirst => Owner.CurrentStep == 0;

    /// <summary>Whether the current step is the last.</summary>
    public bool IsLast => Owner.CurrentStep >= Owner.Count - 1;

    /// <summary>The state of a step.</summary>
    public BitBhStepState StateOf(int index) => Owner.StateOf(index);

    /// <summary>Goes to the next step (or completes on the last).</summary>
    public Task NextAsync() => Owner.NextAsync();

    /// <summary>Goes to the previous step.</summary>
    public Task PreviousAsync() => Owner.PreviousAsync();

    /// <summary>Goes to a step.</summary>
    public Task GoToAsync(int index) => Owner.GoToAsync(index);
}

/// <summary>Shared behaviour of the stepper parts tied to one step.</summary>
public abstract class BitBhStepperStepPartBase : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhStepperContext? Context { get; set; }

    [CascadingParameter]
    private BitBhStepperItem? Item { get; set; }

    /// <summary>The owning stepper.</summary>
    protected BitBhStepper? Stepper => Context?.Owner;

    /// <summary>The step (from the enclosing <see cref="BitBhStepperItem"/>, unless set).</summary>
    protected virtual int StepIndex => Item?.Index ?? -1;

    /// <summary>Adds the step's state hooks.</summary>
    protected void AddStepState(Dictionary<string, object> attrs)
    {
        if (Stepper is not { } stepper) return;
        SetDataValue(attrs, "state", stepper.StateOf(StepIndex) switch
        {
            BitBhStepState.Active => "active",
            BitBhStepState.Completed => "completed",
            _ => "inactive",
        });
        SetDataValue(attrs, "orientation", stepper.Orientation.ToAttribute());
        SetDataValue(attrs, "index", StepIndex.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>The ordered list of a <see cref="BitBhStepper"/>'s steps (<c>&lt;ol&gt;</c>).</summary>
public class BitBhStepperList : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhStepperContext? Context { get; set; }

    /// <summary>The <see cref="BitBhStepperItem"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ol";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-label"] = Strings.Steps;
        if (Context?.Owner is { } stepper) SetDataValue(attrs, "orientation", stepper.Orientation.ToAttribute());
        return attrs;
    }
}

/// <summary>One step of a <see cref="BitBhStepper"/> (<c>&lt;li&gt;</c>): holds its trigger, indicator, title, description and separator.</summary>
public class BitBhStepperItem : BitBhStepperStepPartBase
{
    /// <summary>The step (0-based).</summary>
    [Parameter, EditorRequired]
    public int Index { get; set; }

    /// <summary>The step's parts.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The step's parts, rendered with its state.</summary>
    [Parameter]
    public RenderFragment<BitBhStepState>? Template { get; set; }

    protected override string DefaultTag => "li";

    protected override int StepIndex => Index;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhStepperItem>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "IsFixed", true);
        builder.AddComponentParameter(33, "ChildContent", BitBhContent.Pick(Template, ChildContent, Stepper?.StateOf(Index) ?? BitBhStepState.Inactive));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        AddStepState(attrs);
        return attrs;
    }
}

/// <summary>
/// The button that goes to its step (<c>aria-current="step"</c> on the current one; disabled in a linear stepper until the
/// step can be reached). Its text, or the step's <see cref="BitBhStepperTitle"/>, names the step.
/// </summary>
public class BitBhStepperTrigger : BitBhStepperStepPartBase
{
    /// <summary>The content (number, title, indicator).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Stepper?.TriggerId(StepIndex) ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => Stepper?.GoToAsync(StepIndex) ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Stepper is not { } stepper) return attrs;
        var disabled = !stepper.CanGoTo(StepIndex);
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        if (stepper.StateOf(StepIndex) == BitBhStepState.Active) attrs["aria-current"] = "step";
        attrs["data-bit-bh-interactive"] = "";
        AddStepState(attrs);
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}

/// <summary>The title of a step: names the step's content.</summary>
public class BitBhStepperTitle : BitBhStepperStepPartBase, IDisposable
{
    private int _registered = -1;

    protected override void OnParametersSet()
    {
        if (_registered == StepIndex || Stepper is null) return;
        if (_registered >= 0) Stepper.RegisterTitle(_registered, false);
        _registered = StepIndex;
        if (_registered >= 0) Stepper.RegisterTitle(_registered, true);
    }

    public void Dispose()
    {
        if (_registered >= 0) Stepper?.RegisterTitle(_registered, false);
    }

    /// <summary>The title.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Stepper?.TitleId(StepIndex) ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        AddStepState(attrs);
        return attrs;
    }
}

/// <summary>A decorative part of a step (its number or tick, its description, the line to the next step), with the step's state hooks.</summary>
public class BitBhStepperIndicator : BitBhStepperStepPartBase
{
    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The content, rendered with the step's state (a tick once completed).</summary>
    [Parameter]
    public RenderFragment<BitBhStepState>? Template { get; set; }

    /// <summary>Hides the part from assistive technology (a number or a line the trigger already says). Default: true.</summary>
    [Parameter]
    public bool Decorative { get; set; } = true;

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, BitBhContent.Pick(Template, ChildContent, Stepper?.StateOf(StepIndex) ?? BitBhStepState.Inactive));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Decorative) attrs["aria-hidden"] = "true";
        AddStepState(attrs);
        return attrs;
    }
}

/// <summary>The content of one step: shown while its step is current (hidden otherwise, or removed with <see cref="Unmount"/>).</summary>
public class BitBhStepperContent : BitBhStepperStepPartBase
{
    /// <summary>The step (0-based).</summary>
    [Parameter, EditorRequired]
    public int Index { get; set; }

    /// <summary>Removes the content while its step isn't current (default: kept, hidden, so its state survives).</summary>
    [Parameter]
    public bool Unmount { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override int StepIndex => Index;

    private bool IsActive => Stepper?.CurrentStep == Index;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Unmount && !IsActive) return;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Stepper?.ContentId(Index) ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // A region needs a name: the step's title, when there is one.
        if (Stepper is { } stepper && stepper.HasTitle(Index))
        {
            attrs["role"] = "region";
            attrs["aria-labelledby"] = stepper.TitleId(Index);
        }
        if (!IsActive) attrs["hidden"] = true;
        AddStepState(attrs);
        return attrs;
    }
}

/// <summary>Goes to the next step of a <see cref="BitBhStepper"/>; on the last step it completes the stepper (<c>OnComplete</c>).</summary>
public class BitBhStepperNext : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhStepperContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Context?.NextAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        if (Context is { } context) SetDataFlag(attrs, "last", context.IsLast);
        return attrs;
    }
}

/// <summary>Goes to the previous step of a <see cref="BitBhStepper"/> (disabled on the first).</summary>
public class BitBhStepperPrevious : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhStepperContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Context?.PreviousAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = Context is null || Context.IsFirst;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else if (disabled) attrs["aria-disabled"] = "true";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}
