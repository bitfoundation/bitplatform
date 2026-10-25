namespace Bit.Blex;

/// <summary>
/// A typed descriptor for a parameterless action of <typeparamref name="TStore"/>, generated into the
/// store's nested <c>Actions</c> class (<c>CounterStore.Actions.Increment</c>).
/// </summary>
/// <typeparam name="TStore">The store the action belongs to.</typeparam>
/// <example><code>
/// using var sub = manager.SubscribeToAction(CounterStore.Actions.Increment, counter => Log(counter.Count));
/// await CounterStore.Actions.Increment.InvokeAsync(counter);
/// </code></example>
public sealed class BlexAction<TStore> : BlexActionDescriptor
    where TStore : class, IBlexStore
{
    private readonly Func<TStore, Task> _invoke;

    /// <summary>Creates a descriptor. Called by generated code.</summary>
    /// <param name="name">The action name as dispatched (its display label when it has one).</param>
    /// <param name="invoke">Calls the generated wrapper.</param>
    public BlexAction(string name, Func<TStore, Task> invoke)
        : base(typeof(TStore), name, [])
    {
        ArgumentNullException.ThrowIfNull(invoke);
        _invoke = invoke;
    }

    /// <summary>Dispatches the action on <paramref name="store"/> (equivalent to calling its generated method).</summary>
    /// <returns>The action's task; already completed for a synchronous action.</returns>
    public Task InvokeAsync(TStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return _invoke(store);
    }

    /// <inheritdoc />
    public override Task InvokeAsync(IBlexStore store, IReadOnlyList<BlexActionArg>? args = null)
        => InvokeAsync(CastStore<TStore>(store));
}

/// <summary>
/// A typed descriptor for an action of <typeparamref name="TStore"/> that takes arguments, generated into
/// the store's nested <c>Actions</c> class (<c>CounterStore.Actions.Add</c>). Its payload is the
/// parameter's own type for a single-parameter action and a named tuple for several
/// (<c>(string Name, int Quantity)</c>).
/// </summary>
/// <typeparam name="TStore">The store the action belongs to.</typeparam>
/// <typeparam name="TPayload">The action's arguments, as one value.</typeparam>
/// <remarks>
/// A payload can only be read back from an action whose arguments were captured. The generated
/// wrappers capture them whenever anything observes the manager -- middleware, DevTools, a
/// subscriber or an <see cref="BlexManager.ActionFailed"/> handler -- which every place that reads a
/// payload already is.
/// </remarks>
/// <example><code>
/// options.UseFilter(CounterStore.Actions.Add, (counter, amount) => amount &lt;= 100);
///
/// using var sub = manager.SubscribeToAction(OrdersStore.Actions.Place,
///     (orders, order) => notifications.Push($"{order.Quantity} x {order.Name}"));
/// </code></example>
public sealed class BlexAction<TStore, TPayload> : BlexActionDescriptor
    where TStore : class, IBlexStore
{
    private readonly Func<TStore, TPayload, Task> _invoke;
    private readonly Func<IReadOnlyList<BlexActionArg>, TPayload> _read;

    /// <summary>Creates a descriptor. Called by generated code.</summary>
    /// <param name="name">The action name as dispatched (its display label when it has one).</param>
    /// <param name="parameterNames">The action's parameter names, in declaration order.</param>
    /// <param name="invoke">Calls the generated wrapper with the payload's values.</param>
    /// <param name="readPayload">Rebuilds the payload from an action's captured arguments.</param>
    public BlexAction(
        string name,
        string[] parameterNames,
        Func<TStore, TPayload, Task> invoke,
        Func<IReadOnlyList<BlexActionArg>, TPayload> readPayload)
        : base(typeof(TStore), name, parameterNames)
    {
        ArgumentNullException.ThrowIfNull(invoke);
        ArgumentNullException.ThrowIfNull(readPayload);
        _invoke = invoke;
        _read = readPayload;
    }

    /// <summary>Dispatches the action on <paramref name="store"/> (equivalent to calling its generated method).</summary>
    /// <returns>The action's task; already completed for a synchronous action.</returns>
    public Task InvokeAsync(TStore store, TPayload payload)
    {
        ArgumentNullException.ThrowIfNull(store);
        return _invoke(store, payload);
    }

    /// <inheritdoc />
    public override Task InvokeAsync(IBlexStore store, IReadOnlyList<BlexActionArg>? args = null)
    {
        var typed = CastStore<TStore>(store);
        return InvokeAsync(typed, ReadPayload(args ?? []));
    }

    /// <summary>
    /// Rebuilds the payload from a list of arguments -- an action's captured <c>Args</c>, or a payload
    /// assembled by hand. Throws when an argument is missing or cannot be converted.
    /// </summary>
    public TPayload ReadPayload(IReadOnlyList<BlexActionArg> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return _read(args);
    }

    /// <summary>Reads the payload of a dispatched action, when it is this action and its arguments were captured.</summary>
    public bool TryGetPayload(BlexActionContext context, out TPayload payload)
    {
        ArgumentNullException.ThrowIfNull(context);
        return TryRead(context.Store, context.ActionName, context.Args, out payload);
    }

    /// <summary>Reads the payload of an action about to run, when it is this action and its arguments were captured.</summary>
    public bool TryGetPayload(BlexPreActionContext context, out TPayload payload)
    {
        ArgumentNullException.ThrowIfNull(context);
        return TryRead(context.Store, context.ActionName, context.Args, out payload);
    }

    /// <summary>Reads the payload of an action that failed, when it is this action and its arguments were captured.</summary>
    public bool TryGetPayload(BlexActionErrorContext context, out TPayload payload)
    {
        ArgumentNullException.ThrowIfNull(context);
        return TryRead(context.Store, context.ActionName, context.Args, out payload);
    }

    /// <summary>
    /// Reads the payload of a dispatched action, throwing when it is another action or its arguments
    /// were not captured.
    /// </summary>
    public TPayload GetPayload(BlexActionContext context)
        => TryGetPayload(context, out var payload)
            ? payload
            : throw Unreadable(context.QualifiedName);

    /// <summary>
    /// Reads the payload of an action about to run, throwing when it is another action or its arguments
    /// were not captured.
    /// </summary>
    public TPayload GetPayload(BlexPreActionContext context)
        => TryGetPayload(context, out var payload)
            ? payload
            : throw Unreadable(context.QualifiedName);

    private InvalidOperationException Unreadable(string qualifiedName)
        => new($"'{qualifiedName}' is not {this} or its arguments were not captured, so its payload cannot be read.");

    private bool TryRead(IBlexStore store, string actionName, IReadOnlyList<BlexActionArg> args, out TPayload payload)
    {
        payload = default!;
        if (!Matches(store, actionName) || args.Count < ParameterNames.Count)
            return false;

        try
        {
            payload = _read(args);
            return true;
        }
        catch (Exception)
        {
            // A Try method over a conversion: a missing or unconvertible argument means "no payload".
            payload = default!;
            return false;
        }
    }
}
