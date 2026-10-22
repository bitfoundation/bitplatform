using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// One action of a store, described as data: its name, its parameters, how to read its payload back
/// out of a dispatched action and how to dispatch it. The generator emits a typed descriptor for every
/// <c>[BlexActionAttribute]</c> and <c>[BlexEffectAttribute]</c> into the store's nested
/// <c>Actions</c> class -- <see cref="BlexAction{TStore}"/> for a parameterless action,
/// <see cref="BlexAction{TStore, TPayload}"/> otherwise.
/// </summary>
/// <remarks>
/// <para>
/// Descriptors are what make an action more than a method call. A subscriber, a filter or a test
/// matches <c>CounterStore.Actions.Add</c> instead of the string <c>"Add"</c> - a rename is a compile
/// error rather than a subscription that silently stops firing - and reads the payload as the
/// <c>int</c> the action takes rather than as an <c>object</c> looked up by parameter name.
/// </para>
/// <para>
/// The same descriptors let an action be dispatched by name with a JSON payload
/// (<see cref="BlexManager.DispatchAsync(string, JsonObject?)"/>), which is what the Redux DevTools
/// dispatcher and the testing package's action replay are built on.
/// </para>
/// </remarks>
public abstract class BlexActionDescriptor
{
    private protected BlexActionDescriptor(Type storeType, string name, string[] parameterNames)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(parameterNames);
        StoreType = storeType;
        Name = name;
        ParameterNames = parameterNames;
    }

    /// <summary>The store type the action belongs to.</summary>
    public Type StoreType { get; }

    /// <summary>
    /// The action name exactly as <see cref="BlexActionContext.ActionName"/> reports it -- the display
    /// label when the action sets one (<c>[BlexActionAttribute(Name = "Apply preset")]</c>).
    /// </summary>
    public string Name { get; }

    /// <summary>The action's parameter names, in declaration order (empty for a parameterless action).</summary>
    public IReadOnlyList<string> ParameterNames { get; }

    /// <summary>Whether <paramref name="actionName"/> dispatched by <paramref name="store"/> is this action.</summary>
    public bool Matches(IBlexStore store, string actionName)
        => store is not null && StoreType.IsInstanceOfType(store) && string.Equals(actionName, Name, StringComparison.Ordinal);

    /// <summary>Whether the dispatched action is this one.</summary>
    public bool Matches(BlexActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Matches(context.Store, context.ActionName);
    }

    /// <summary>Whether the action about to run is this one.</summary>
    public bool Matches(BlexPreActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Matches(context.Store, context.ActionName);
    }

    /// <summary>Whether the action that failed is this one.</summary>
    public bool Matches(BlexActionErrorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Matches(context.Store, context.ActionName);
    }

    /// <summary>
    /// Dispatches this action on <paramref name="store"/> through its generated wrapper, so it runs the
    /// whole pipeline (filters, middleware, DevTools, persistence) exactly as a direct call would.
    /// </summary>
    /// <param name="store">The store to dispatch on; must be a <see cref="StoreType"/>.</param>
    /// <param name="args">
    /// The arguments, matched to <see cref="ParameterNames"/> by name (case-insensitively as a fallback).
    /// A value may be the parameter's own type or a <see cref="JsonNode"/>/<see cref="JsonElement"/>,
    /// which is deserialized with <see cref="BlexJson.Options"/>. An optional parameter left out takes
    /// its declared default, as in a direct call.
    /// </param>
    /// <returns>The action's task; already completed for a synchronous action.</returns>
    public abstract Task InvokeAsync(IBlexStore store, IReadOnlyList<BlexActionArg>? args = null);

    /// <inheritdoc />
    public override string ToString() => $"{StoreType.Name}.{Name}";

    private protected TStore CastStore<TStore>(IBlexStore store) where TStore : class, IBlexStore
    {
        ArgumentNullException.ThrowIfNull(store);
        return store as TStore
            ?? throw new ArgumentException($"The action '{Name}' belongs to {typeof(TStore).Name}, not {store.GetType().Name}.", nameof(store));
    }

    /// <summary>
    /// Reads one argument out of an action's payload, converting it to the parameter's type. Used by
    /// generated descriptors; not intended to be called directly.
    /// </summary>
    /// <param name="args">The payload.</param>
    /// <param name="index">The parameter's position, where the generated wrappers record it.</param>
    /// <param name="name">The parameter's name, used when the payload is not in declaration order.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static T GetArg<T>(IReadOnlyList<BlexActionArg> args, int index, string name)
        => TryGetArg(args, index, name, out T value)
            ? value
            : throw new ArgumentException($"The payload has no argument named '{name}'.", nameof(args));

    /// <summary>
    /// Reads one optional argument out of an action's payload, falling back to the parameter's declared
    /// default when the payload leaves it out -- as a direct call that omits it would. Used by generated
    /// descriptors; not intended to be called directly.
    /// </summary>
    /// <param name="args">The payload.</param>
    /// <param name="index">The parameter's position, where the generated wrappers record it.</param>
    /// <param name="name">The parameter's name, used when the payload is not in declaration order.</param>
    /// <param name="defaultValue">The parameter's default value.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static T GetArgOrDefault<T>(IReadOnlyList<BlexActionArg> args, int index, string name, T defaultValue)
        => TryGetArg(args, index, name, out T value) ? value : defaultValue;

    private static bool TryGetArg<T>(IReadOnlyList<BlexActionArg> args, int index, string name, out T value)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(name);

        // Positional first: the generated wrappers record arguments in declaration order, so this is
        // the only lookup a replayed or observed action ever needs. A hand-built payload or a JSON
        // object from the DevTools dispatcher may list them in any order, or in another case.
        if (index >= 0 && index < args.Count && string.Equals(args[index].Name, name, StringComparison.Ordinal))
        {
            value = Convert<T>(args[index].Value, name);
            return true;
        }

        if (TryFind(args, name, StringComparison.Ordinal, out var exact)
            || TryFind(args, name, StringComparison.OrdinalIgnoreCase, out exact))
        {
            value = Convert<T>(exact, name);
            return true;
        }

        value = default!;
        return false;
    }

    private static bool TryFind(IReadOnlyList<BlexActionArg> args, string name, StringComparison comparison, out object? value)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (string.Equals(args[i].Name, name, comparison))
            {
                value = args[i].Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "BlexJson.Options is the app's trimming seam (BlexJson.Configure sets a source-generated TypeInfoResolver), the same one every generated SerializeState/DeserializeState goes through.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "BlexJson.Options is the app's trimming seam (BlexJson.Configure sets a source-generated TypeInfoResolver), the same one every generated SerializeState/DeserializeState goes through.")]
    private static T Convert<T>(object? value, string name)
    {
        switch (value)
        {
            // A JsonNode-typed parameter handed a node from a payload must own it: the node is still
            // parented to that payload, and storing it in state would fail on its next re-parenting.
            case JsonNode node when node is T:
                return (T)(object)node.DeepClone();

            case T typed:
                return typed;

            case null:
                if (default(T) is null)
                    return default!;
                throw new InvalidCastException($"The argument '{name}' is null, but the action takes a non-nullable {typeof(T).Name}.");

            case JsonNode node:
                return node.Deserialize<T>(BlexJson.Options)!;

            case JsonElement element:
                return element.Deserialize<T>(BlexJson.Options)!;

            default:
                // Another runtime type for the same value (a long for an int, a string for an enum):
                // round-trip it through JSON, which converts exactly the way a stored payload would.
                return JsonSerializer.SerializeToNode(value, value.GetType(), BlexJson.Options).Deserialize<T>(BlexJson.Options)!;
        }
    }
}
