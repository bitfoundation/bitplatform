namespace Bit.BlazorUI;

public abstract partial class BitComponentBase : IBitCascadeTarget
{
    // The map of this component's type, looked up once rather than on every render.
    private BitCascadeMap? _cascadeMap;
    private bool _isCascadeMapResolved;

    // What this component remembers of the params object it takes its defaults from, created with the first one.
    private BitCascadeTracker? _cascadeTracker;



    /// <summary>
    /// Whether the markup set the named parameter on this render. Each class that tracks its own parameters adds
    /// them, so the answer covers the whole hierarchy; <see cref="HasNotBeenSet"/> is its public negation.
    /// </summary>
    private protected virtual bool IsSetByMarkup(string name) => _assignedParameters.Contains(name);

    /// <summary>
    /// The params object cascaded to this component, if it reads one. The generated code of a component reads its
    /// cascading parameter directly; any other one is read through reflection.
    /// </summary>
    private protected virtual IBitComponentParams? CascadedParams => GetCascadeMap()?.GetParams(this);

    // Asked through HasNotBeenSet, not IsSetByMarkup: a class outside the assemblies that see IsSetByMarkup adds its
    // own parameters by overriding HasNotBeenSet, so only that answer covers them too.
    bool IBitCascadeTarget.IsSetByMarkup(string name) => HasNotBeenSet(name) is false;

    BitDir? IBitCascadeTarget.OwnDir
    {
        get => _dir;
        set => _dir = value;
    }



    /// <summary>
    /// Puts back every parameter the params object of this component supplied before and no longer does, and
    /// remembers the value each newly supplied one replaces. Runs once every parameter is assigned and before the
    /// params object is applied, which is when a parameter still holds the value it had before the cascade.
    /// </summary>
    /// <returns>
    /// The setup hooks of the restored parameters that run asynchronously, or a completed task when there are none.
    /// </returns>
    private Task RestoreDroppedCascadeParameters()
    {
        var current = CascadedParams;

        // Nothing supplied now and nothing before, which is where every component outside a BitParams stays.
        if (current is null && _cascadeTracker is null) return Task.CompletedTask;

        var map = GetCascadeMap();

        List<BitCascadeMap.BitCascadeParameter>? restored = null;
        var isAnotherParams = true;

        if (map is not null)
        {
            restored = (_cascadeTracker ??= new()).RestoreDropped(this, map, current, out isAnotherParams);
        }

        // The params objects only reset a builder for a value that differs from the one the component already holds,
        // which a nested object changed in place - a ClassStyles, an icon - is not: it is the very same object. A
        // BitParams hands down another copy of a params object whenever what it carries changes, down into such a
        // nested object, and the very same copy otherwise, so a copy not seen before is the one thing that tells it
        // here. A params object cascaded as it was given - through a CascadingValue of the app's own - has no such
        // copy to tell a change by, so the builders are reset on every render under it, as they always were.
        if (isAnotherParams || restored is not null || (current is not null && BitParamsScope.IsCopy(current) is false))
        {
            ClassBuilder.Reset();
            StyleBuilder.Reset();
        }

        if (restored is null) return Task.CompletedTask;

        return BitCascadeTracker.RunSetupHooks(this, restored);
    }

    private BitCascadeMap? GetCascadeMap()
    {
        if (_isCascadeMapResolved) return _cascadeMap;

        _cascadeMap = BitCascadeMap.For(GetType());
        _isCascadeMapResolved = true;

        return _cascadeMap;
    }
}
