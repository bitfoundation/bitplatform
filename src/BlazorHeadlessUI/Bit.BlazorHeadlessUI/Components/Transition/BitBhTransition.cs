using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless Transition that animates its element in and out — with CSS class
/// lists (Tailwind style) and/or data attributes (plain CSS) — and keeps it
/// rendered until the leave transition has finished.
///
/// <para><b>Classes:</b> <c>Enter</c> + <c>EnterFrom</c> on the first frame → <c>EnterTo</c>
/// until the transition ends → <c>Entered</c>. Leave mirrors it.</para>
/// <para><b>Data attributes:</b> <c>data-transition</c>, <c>data-enter</c> /
/// <c>data-leave</c> while running and <c>data-closed</c> for the hidden state, so
/// <c>.panel { transition: opacity .2s } .panel[data-closed] { opacity: 0 }</c> just works.</para>
/// <para>Interrupting a transition (toggling <see cref="Show"/> mid-way) reverses it
/// smoothly; reduced-motion users get instant changes when your CSS disables the motion.</para>
///
/// <code>
/// &lt;BitBhTransition Show="@isOpen"
///              Enter="transition duration-200 ease-out" EnterFrom="opacity-0 scale-95" EnterTo="opacity-100 scale-100"
///              Leave="transition duration-150 ease-in" LeaveFrom="opacity-100 scale-100" LeaveTo="opacity-0 scale-95"&gt;
///     &lt;div class="panel"&gt;Content&lt;/div&gt;
/// &lt;/BitBhTransition&gt;
/// </code>
/// </summary>
public class BitBhTransition : BitBhComponentBase
{
    private enum Phase { Hidden, Entering, Shown, Leaving }

    private Phase _phase = Phase.Hidden;
    private bool _pendingJs;
    private int _run;

    /// <summary>Whether the content is shown. Drives the enter / leave lifecycle.</summary>
    [Parameter]
    public bool Show { get; set; }

    /// <summary>Run the enter transition on first render when <see cref="Show"/> is true. Default: false.</summary>
    [Parameter]
    public bool Appear { get; set; }

    /// <summary>Remove the element from the DOM when hidden (default). When false it stays with the <c>hidden</c> attribute.</summary>
    [Parameter]
    public bool Unmount { get; set; } = true;

    /// <summary>Classes applied during the whole enter phase.</summary>
    [Parameter] public string? Enter { get; set; }

    /// <summary>Classes applied on the first frame of enter.</summary>
    [Parameter] public string? EnterFrom { get; set; }

    /// <summary>Classes applied from the second frame of enter until it ends.</summary>
    [Parameter] public string? EnterTo { get; set; }

    /// <summary>Classes applied once entered, while shown.</summary>
    [Parameter] public string? Entered { get; set; }

    /// <summary>Classes applied during the whole leave phase.</summary>
    [Parameter] public string? Leave { get; set; }

    /// <summary>Classes applied on the first frame of leave.</summary>
    [Parameter] public string? LeaveFrom { get; set; }

    /// <summary>Classes applied from the second frame of leave until it ends.</summary>
    [Parameter] public string? LeaveTo { get; set; }

    /// <summary>Fires before the enter transition starts.</summary>
    [Parameter] public EventCallback BeforeEnter { get; set; }

    /// <summary>Fires after the enter transition completes.</summary>
    [Parameter] public EventCallback AfterEnter { get; set; }

    /// <summary>Fires before the leave transition starts.</summary>
    [Parameter] public EventCallback BeforeLeave { get; set; }

    /// <summary>Fires after the leave transition completes.</summary>
    [Parameter] public EventCallback AfterLeave { get; set; }

    /// <summary>The transitioned content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private BitBhTransitionClasses Classes => new()
    {
        Enter = Enter,
        EnterFrom = EnterFrom,
        EnterTo = EnterTo,
        Entered = Entered,
        Leave = Leave,
        LeaveFrom = LeaveFrom,
        LeaveTo = LeaveTo,
    };

    protected override void OnInitialized()
    {
        if (Show)
        {
            _phase = Appear ? Phase.Entering : Phase.Shown;
            _pendingJs = Appear;
        }
    }

    protected override void OnParametersSet()
    {
        if (Show && _phase is Phase.Hidden or Phase.Leaving)
        {
            _phase = Phase.Entering;
            _pendingJs = true;
        }
        else if (!Show && _phase is Phase.Shown or Phase.Entering)
        {
            _phase = Phase.Leaving;
            _pendingJs = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_pendingJs) return;
        _pendingJs = false;
        var run = ++_run;

        if (_phase == Phase.Entering)
        {
            await BeforeEnter.InvokeAsync();
            var completed = await Interop.TransitionEnterAsync(ComponentId, Classes);
            if (run != _run || _phase != Phase.Entering) return;
            _phase = Phase.Shown;
            if (completed) await AfterEnter.InvokeAsync();
            StateHasChanged();
        }
        else if (_phase == Phase.Leaving)
        {
            await BeforeLeave.InvokeAsync();
            var completed = await Interop.TransitionLeaveAsync(ComponentId, Classes);
            if (run != _run || _phase != Phase.Leaving) return;
            _phase = Phase.Hidden;
            if (completed) await AfterLeave.InvokeAsync();
            StateHasChanged();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_phase == Phase.Hidden && Unmount) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (_phase == Phase.Hidden)
            builder.AddAttribute(25, "hidden", true);
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, _phase is Phase.Shown or Phase.Entering);

        // First paint of an enter already shows the "from" state, so there is
        // no flash of the final state before the runtime takes over.
        switch (_phase)
        {
            case Phase.Entering:
                attrs["class"] = BitBhAttributeUtilities.JoinClasses(Enter, EnterFrom);
                attrs["data-closed"] = "";
                attrs["data-enter"] = "";
                attrs["data-transition"] = "";
                break;
            case Phase.Shown when !string.IsNullOrWhiteSpace(Entered):
                attrs["class"] = Entered!;
                break;
            case Phase.Hidden:
                attrs["data-closed"] = "";
                break;
        }
        return attrs;
    }
}
