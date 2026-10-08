namespace Bit.BlazorUI;

public abstract partial class BitComponentBase : IBitCascadeTarget
{
    // The map of this component's type, looked up once rather than on every render.
    private BitCascadeMap? _cascadeMap;
    private bool _isCascadeMapResolved;

    // What this component remembers of the params object it takes its defaults from, created with the first one.
    private BitCascadeTracker? _cascadeTracker;

    // Whether the params object cascaded on the last render is another one than the one before it.
    private bool _isCascadeRenewed;



    /// <summary>
    /// Whether the params object cascaded on the last render is another one than the one cascaded before it, which
    /// BitParams only hands down when something that params object carries has changed - possibly in place, inside
    /// an object the component already holds.
    /// </summary>
    internal bool IsCascadeRenewed => _isCascadeRenewed;



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
    /// <summary>
    /// Brings up to date what the component derives from a parameter the params object has just written: rebuilds
    /// the strings its [ResetClassBuilder] and [ResetStyleBuilder] name and runs its [CallOnSet] hook, exactly as
    /// the generated code does when the markup changes it, so that no params object has to remember to.
    /// </summary>
    internal void OnTakenFromCascade(string name)
    {
        var parameter = GetCascadeMap()?.Parameters.GetValueOrDefault(name);

        // A parameter the map does not know is one nothing is known about, so both strings are rebuilt.
        if (parameter?.ResetsClass ?? true) ClassBuilder.Reset();
        if (parameter?.ResetsStyle ?? true) StyleBuilder.Reset();

        parameter?.RunSetupHook(this);
    }

    /// <summary>
    /// Puts back the value the named parameter held before the params object wrote it, while the params object
    /// still supplies it but the component has set something that outranks it. The value stays recorded, so the
    /// parameter is restored again once the params object stops supplying it.
    /// </summary>
    internal void ReleaseCascadeParameter(string name)
    {
        if (HasNotBeenSet(name) is false) return;

        if (_cascadeTracker is null || _cascadeTracker.TryGetOriginal(name, out var original) is false) return;

        var parameter = GetCascadeMap()?.Parameters.GetValueOrDefault(name);

        if (parameter is null || parameter.SetOnComponent(this, original) is false) return;

        if (parameter.ResetsClass) ClassBuilder.Reset();
        if (parameter.ResetsStyle) StyleBuilder.Reset();

        parameter.RunSetupHook(this);
    }



    private Task RestoreDroppedCascadeParameters()
    {
        _isCascadeRenewed = false;

        var current = CascadedParams;

        // Nothing supplied now and nothing before, which is where every component outside a BitParams stays.
        if (current is null && _cascadeTracker is null) return Task.CompletedTask;

        var map = GetCascadeMap();

        if (map is null) return Task.CompletedTask;

        var restored = (_cascadeTracker ??= new()).RestoreDropped(this, map, current, out var paramsChanged);

        // A params object supplied again unchanged writes nothing (TakeFromCascade compares every value), so neither
        // does it rebuild anything. A new one - which BitParams only hands down when something that params object
        // carries has changed - writes every object it supplies again, since one changed in place reaches the
        // component as the very object it already holds.
        _isCascadeRenewed = paramsChanged;

        // A params object cascaded as it was given - through a CascadingValue of the app's own - is not handed down
        // anew when what it carries changes, so a nested object changed in place - a ClassStyles, an icon - cannot be
        // told apart from one left as it was. The strings are rebuilt on every render under it, as they always were.
        if (current is not null && BitParamsScope.IsCopy(current) is false)
        {
            ClassBuilder.Reset();
            StyleBuilder.Reset();
        }

        if (restored is null) return Task.CompletedTask;

        foreach (var parameter in restored)
        {
            if (parameter.ResetsClass) ClassBuilder.Reset();
            if (parameter.ResetsStyle) StyleBuilder.Reset();
        }

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
