using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>Describes EventCallbacks and delegates by the method they run, which is what "EventCallback (bound)" never said.</summary>
public static class BevToolsEventCallbacks
{
    private static readonly ConcurrentDictionary<Type, System.Reflection.PropertyInfo?> HasDelegateProperties = new();

    /// <summary>"EventCallback → ProductList.OnSelected", "EventCallback (empty)", or "EventCallback (bound)" when the target cannot be read.</summary>
    public static string Describe(object callback)
    {
        var label = callback.GetType().IsGenericType ? "EventCallback<" + BevToolsTypeNames.Short(callback.GetType().GetGenericArguments()[0]) + ">" : "EventCallback";
        if (BevToolsBlazorReflection.EventCallbackDelegate(callback) is { } target)
        {
            // EventCallback.Empty is bound, to a no-op of the framework's own.
            return IsFrameworkNoOp(target) ? label + " (empty)" : label + " → " + Method(target);
        }

        var hasDelegate = HasDelegateProperties.GetOrAdd(callback.GetType(), static t => t.GetProperty(nameof(EventCallback.HasDelegate)));
        return hasDelegate?.GetValue(callback) is true ? label + " (bound)" : label + " (empty)";
    }

    private static bool IsFrameworkNoOp(Delegate handler)
    {
        var declaring = handler.Method.DeclaringType;
        while (declaring is not null && declaring.IsDefined(typeof(CompilerGeneratedAttribute), false) && declaring.DeclaringType is not null)
        {
            declaring = declaring.DeclaringType;
        }

        return declaring == typeof(EventCallback) || (declaring is { IsGenericType: true } && declaring.GetGenericTypeDefinition() == typeof(EventCallback<>));
    }

    /// <summary>
    /// "Counter.IncrementCount" for a method group; for a lambda, whose compiled name ("&lt;BuildRenderTree&gt;b__3_0")
    /// and closure class ("&lt;&gt;c__DisplayClass3_0") mean nothing to a reader, "Counter.Load (lambda)" when it calls
    /// one method of the component, else "lambda in Counter.BuildRenderTree".
    /// </summary>
    public static string Method(Delegate handler)
    {
        // @bind runs a lambda of the framework's binder; what the developer wrote is the setter it calls.
        if (BevToolsBlazorReflection.BindSetter(handler) is { } setter)
        {
            return Method(setter) + " (@bind)";
        }

        var method = handler.Method;
        var declaring = method.DeclaringType;
        while (declaring is not null && declaring.IsDefined(typeof(CompilerGeneratedAttribute), false) && declaring.DeclaringType is not null)
        {
            declaring = declaring.DeclaringType;
        }

        var owner = declaring is null ? "" : BevToolsTypeNames.Short(declaring) + ".";
        var name = method.Name;
        if (name.StartsWith('<'))
        {
            var close = name.IndexOf('>');
            var outer = close > 1 ? name[1..close] : null;
            return BevToolsLambdaTargets.CalledMethod(method) is { } target ? owner + target.Name + " (lambda)" : "lambda in " + owner + (outer ?? "?");
        }

        return owner + name;
    }
}
