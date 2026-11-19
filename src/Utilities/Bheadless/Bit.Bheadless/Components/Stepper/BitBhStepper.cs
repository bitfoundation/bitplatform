using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

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
