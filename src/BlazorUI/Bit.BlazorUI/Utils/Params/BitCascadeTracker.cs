namespace Bit.BlazorUI;

/// <summary>
/// What one component remembers of the params object it takes its defaults from: the object itself, and the value
/// each parameter that object supplies held before it first did.
/// </summary>
/// <remarks>
/// A value written by the params object is not one the markup passes again on the next render, so Blazor never
/// overwrites it: once the params object stops supplying it - the property cleared, the object removed, an isolated
/// BitParams put in between - it is put back from here. Applying what the params object supplies stays the job of
/// its own <c>UpdateParameters</c>; this only undoes it.
/// </remarks>
internal sealed class BitCascadeTracker
{
    private IBitComponentParams? _cascadedParams;
    private Dictionary<string, object?>? _cascadeOriginals;

    // The parameters the params object supplies while the markup sets them, so that there is no value of the
    // component's own to record for them yet: the moment the markup lets go of one, the value it leaves behind is.
    private HashSet<string>? _cascadeSetByMarkup;



    /// <summary>
    /// Puts back every parameter the params object supplied before and no longer does, and remembers the value each
    /// newly supplied one replaces. Called once every parameter is assigned and before the params object is applied,
    /// which is when a parameter still holds the value it had before the cascade.
    /// </summary>
    /// <param name="component">The component the params object is cascaded to.</param>
    /// <param name="map">The parameters the params object can supply to the component.</param>
    /// <param name="current">The params object cascaded to the component on this render, if any.</param>
    /// <param name="paramsChanged">Whether that is another params object than the one cascaded on the last render.</param>
    /// <returns>The parameters put back, or null when there were none.</returns>
    public List<BitCascadeMap.BitCascadeParameter>? RestoreDropped(IBitCascadeTarget component, BitCascadeMap map, IBitComponentParams? current, out bool paramsChanged)
    {
        paramsChanged = false;

        // Nothing supplied now and nothing before, which is where a component stays once a params object has gone.
        if (current is null && _cascadedParams is null) return null;

        List<BitCascadeMap.BitCascadeParameter>? restored = null;

        // BitParams hands down a new object whenever what it carries changes, so the same object supplies the same
        // parameters, and only what the markup took over or let go of since is left to keep track of.
        if (ReferenceEquals(current, _cascadedParams) is false)
        {
            paramsChanged = true;

            _cascadedParams = current;

            if (_cascadeOriginals is { Count: > 0 })
            {
                List<string>? dropped = null;

                foreach (var name in _cascadeOriginals.Keys)
                {
                    if (current is not null && map.Parameters[name].IsSuppliedBy(current)) continue;

                    (dropped ??= []).Add(name);
                }

                foreach (var name in dropped ?? [])
                {
                    var original = _cascadeOriginals[name];

                    _cascadeOriginals.Remove(name);

                    if (component.IsSetByMarkup(name)) continue;

                    var parameter = map.Parameters[name];

                    if (parameter.SetOnComponent(component, original) is false) continue;

                    (restored ??= []).Add(parameter);
                }
            }

            if (_cascadeSetByMarkup is { Count: > 0 })
            {
                _cascadeSetByMarkup.RemoveWhere(name => current is null || map.Parameters[name].IsSuppliedBy(current) is false);
            }

            if (current is not null)
            {
                foreach (var (name, parameter) in map.Parameters)
                {
                    if (parameter.IsSuppliedBy(current) is false) continue;
                    if (_cascadeOriginals?.ContainsKey(name) is true) continue;
                    if (_cascadeSetByMarkup?.Contains(name) is true) continue;

                    if (component.IsSetByMarkup(name))
                    {
                        (_cascadeSetByMarkup ??= []).Add(name);
                    }
                    else
                    {
                        (_cascadeOriginals ??= []).Add(name, parameter.GetFromComponent(component));
                    }
                }
            }
        }

        TrackMarkupChanges(component, map);

        return restored;
    }

    /// <summary>
    /// The value the named parameter held before the params object first supplied it, which stays recorded for as
    /// long as the params object supplies it and the markup does not set it.
    /// </summary>
    public bool TryGetOriginal(string name, out object? original)
    {
        original = null;

        return _cascadeOriginals?.TryGetValue(name, out original) is true;
    }

    /// <summary>
    /// Runs the setup hooks of the parameters <see cref="RestoreDropped"/> put back, which keep the state derived
    /// from a parameter in step with it exactly as they do when the markup or the params object sets one.
    /// </summary>
    /// <returns>The hooks that run asynchronously, or a completed task when there are none.</returns>
    public static Task RunSetupHooks(IBitCascadeTarget component, List<BitCascadeMap.BitCascadeParameter> restored)
    {
        List<Task>? pending = null;

        foreach (var parameter in restored)
        {
            var task = parameter.RunSetupHooks(component);

            if (task.IsCompletedSuccessfully) continue;

            (pending ??= []).Add(task);
        }

        return pending is null ? Task.CompletedTask : Task.WhenAll(pending);
    }



    /// <summary>
    /// Moves a supplied parameter the markup has started to set out of the recorded ones, since what the markup
    /// gives it is what it holds of its own now, and records one the markup has stopped setting with the value it
    /// left behind, which is what goes back once the params object stops supplying it too.
    /// </summary>
    private void TrackMarkupChanges(IBitCascadeTarget component, BitCascadeMap map)
    {
        if (_cascadeOriginals is { Count: > 0 })
        {
            List<string>? takenOver = null;

            foreach (var name in _cascadeOriginals.Keys)
            {
                if (component.IsSetByMarkup(name)) (takenOver ??= []).Add(name);
            }

            foreach (var name in takenOver ?? [])
            {
                _cascadeOriginals.Remove(name);

                (_cascadeSetByMarkup ??= []).Add(name);
            }
        }

        if (_cascadeSetByMarkup is { Count: > 0 })
        {
            List<string>? letGo = null;

            foreach (var name in _cascadeSetByMarkup)
            {
                if (component.IsSetByMarkup(name) is false) (letGo ??= []).Add(name);
            }

            foreach (var name in letGo ?? [])
            {
                _cascadeSetByMarkup.Remove(name);

                (_cascadeOriginals ??= []).Add(name, map.Parameters[name].GetFromComponent(component));
            }
        }
    }
}
