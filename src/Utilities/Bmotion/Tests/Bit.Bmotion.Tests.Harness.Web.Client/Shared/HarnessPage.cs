using Bit.Bmotion;
using Microsoft.AspNetCore.Components;

namespace BmotionTests.Harness.Web.Client.Shared;

/// <summary>
/// Base of every harness page. Two things make the pages deterministic enough to drive from a browser:
/// <list type="bullet">
///   <item>
///     Events never re-render the page on their own (<see cref="IHandleEvent"/>). A Bmotion callback - a hover
///     start, an animation completing - only writes to the <see cref="EventLog"/>, which re-renders itself; the
///     animated elements are re-rendered only when a test asks for it through a control that calls
///     <see cref="Change(Action)"/>. Otherwise every logged event would re-render every element on the page and
///     the tests would be exercising re-render handling everywhere instead of the feature they name.
///   </item>
///   <item>
///     The page reports <c>ready</c> in <c>#status</c> only after its first interactive render (an after-render
///     callback never runs while prerendering), so nothing is clicked before the runtime has attached handlers.
///   </item>
/// </list>
/// </summary>
public abstract class HarnessPage : ComponentBase, IHandleEvent
{
    [Inject] protected BmotionCapabilities Caps { get; set; } = default!;

    /// <summary>The page's event log, bound with <c>@ref</c> by <see cref="HarnessHeader"/>.</summary>
    protected EventLog? Events { get; set; }

    /// <summary>Set after the first interactive render; what <c>#status</c> reports.</summary>
    protected bool Ready { get; private set; }

    /// <summary>The <c>OnLog</c> handler of <see cref="HarnessHeader"/>: hands the page its log.</summary>
    protected void BindLog(EventLog log) => Events = log;

    /// <summary>Appends an entry to <c>#log</c> as <c>&lt;li data-event="..."&gt;</c>, without re-rendering the page.</summary>
    protected void Log(string entry) => Events?.Add(entry);

    /// <summary>
    /// Logs <c>armed:&lt;name&gt;</c> the first time only - wired to an element's OnAnimationStart, which the library
    /// raises once the element's initialisation (listeners, observers, measurements) is done, it tells a test the
    /// element is ready to be driven.
    /// </summary>
    protected void Armed(string name)
    {
        if (_armed.Add(name)) Log($"armed:{name}");
    }

    private readonly HashSet<string> _armed = [];

    /// <summary>Applies a change of the page's state and re-renders the page - what every control on a page uses.</summary>
    protected void Change(Action change)
    {
        change();
        StateHasChanged();
    }

    /// <inheritdoc cref="Change(Action)"/>
    protected async Task ChangeAsync(Func<Task> change)
    {
        await change();
        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender is false) return;

        Ready = true;
        StateHasChanged();
    }

    // No automatic re-render after an event: see the remarks on the class.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
