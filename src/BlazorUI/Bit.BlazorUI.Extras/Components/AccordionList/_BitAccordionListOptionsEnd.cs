namespace Bit.BlazorUI;

/// <summary>
/// Rendered right behind the options of an accordion list, where it is initialized only once every option
/// before it has been initialized - and so has registered with the list. It renders nothing of its own.
/// </summary>
public sealed class _BitAccordionListOptionsEnd : IComponent
{
    private bool _reached;



    /// <summary>
    /// Invoked once, the first time the marker is handed its parameters.
    /// </summary>
    [Parameter] public Action? OnReached { get; set; }



    public void Attach(RenderHandle renderHandle) { }

    public Task SetParametersAsync(ParameterView parameters)
    {
        // Only the first pass marks the point the options of the first render have registered by: every later
        // one is the list rendering again, which tells nothing new.
        if (_reached) return Task.CompletedTask;

        _reached = true;

        if (parameters.TryGetValue<Action>(nameof(OnReached), out var onReached))
        {
            OnReached = onReached;
            onReached?.Invoke();
        }

        return Task.CompletedTask;
    }
}
