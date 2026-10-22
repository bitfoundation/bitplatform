using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BevTools;

public enum BevEventBindingKind
{
    /// <summary>A DOM event handler on an element: <c>@onclick="Save"</c>.</summary>
    DomEvent,

    /// <summary>The change handler <c>@bind</c> generates, which writes the element's value back into the component.</summary>
    Bind,

    /// <summary>An <c>EventCallback</c> or delegate the component passes to a child component as a parameter.</summary>
    ChildCallback,
}

/// <summary>One thing a component wired up in its latest render, read from the render tree the renderer holds for it.</summary>
/// <param name="Target">The element (<c>&lt;button#save&gt;</c>) or child component (<c>ProductRow</c>) it is attached to.</param>
/// <param name="Event">The DOM event (<c>onclick</c>), or the child's parameter (<c>OnSelected</c>).</param>
/// <param name="Handler">What runs: <c>Counter.IncrementCount</c>, or a lambda and the method that declares it.</param>
/// <param name="Kind">A DOM event handler, the handler of a <c>@bind</c>, or a callback passed to a child component.</param>
/// <param name="ChildInstanceId">BevTools' instance id of the child component, for <see cref="BevEventBindingKind.ChildCallback"/>.</param>
public sealed record BevEventBinding(string Target, string Event, string Handler, BevEventBindingKind Kind, long? ChildInstanceId = null);

/// <summary>What one component wired up in its latest render, and whether it could be read at all.</summary>
public sealed record BevComponentWiring(IReadOnlyList<BevEventBinding> Bindings, int TotalBindings, string? Unavailable)
{
    public static BevComponentWiring NotReadable(string reason) => new([], 0, reason);
}

/// <summary>
/// Reads which event handlers and callbacks a component bound, from the frames of its latest render. That is the
/// renderer's own record of the component's output, so nothing is inferred: a handler the component binds only under
/// a condition appears exactly while that condition holds.
/// </summary>
internal static class BevComponentWiringReader
{
    private const int MaxBindings = 200;

    public static BevComponentWiring Read(BevComponentRecord record, Func<int, BevComponentRecord?> childByComponentId)
    {
        if (!BlazorReflection.CanReadRenderTree)
        {
            return BevComponentWiring.NotReadable("The renderer's render tree could not be accessed on this framework version.");
        }

        if (record.Renderer is not { } renderer || record.Component is not { } component)
        {
            return BevComponentWiring.NotReadable(record.IsDisposed ? "The component was disposed." : "The component has not rendered yet.");
        }

        var state = BlazorReflection.ResolveState(renderer, component);
        if (state is null || BlazorReflection.CurrentFrames(state) is not { } frames)
        {
            return BevComponentWiring.NotReadable("The renderer does not hold a render tree for this component (not rendered yet, or disposed).");
        }

        var bindings = new List<BevEventBinding>();
        var total = 0;
        var ownerIndex = -1;
        for (var i = 0; i < frames.Length; i++)
        {
            ref readonly var frame = ref frames[i];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                case RenderTreeFrameType.Component:
                    ownerIndex = i;
                    continue;
                case RenderTreeFrameType.Attribute when ownerIndex >= 0:
                    break;
                default:
                    // Attributes only ever follow their element or component frame directly.
                    ownerIndex = -1;
                    continue;
            }

            ref readonly var owner = ref frames[ownerIndex];
            var binding = owner.FrameType == RenderTreeFrameType.Element
                ? ElementBinding(frames, ownerIndex, in frame)
                : ChildBinding(in owner, in frame, childByComponentId);
            if (binding is null)
            {
                continue;
            }

            total++;
            if (bindings.Count < MaxBindings)
            {
                bindings.Add(binding);
            }
        }

        return new BevComponentWiring(bindings, total, null);
    }

    private static BevEventBinding? ElementBinding(RenderTreeFrame[] frames, int elementIndex, in RenderTreeFrame attribute)
    {
        var isHandler = attribute.AttributeEventHandlerId != 0
            || (attribute.AttributeName.StartsWith("on", StringComparison.Ordinal) && IsCallable(attribute.AttributeValue));
        if (!isHandler)
        {
            return null;
        }

        var target = DescribeElement(frames, elementIndex);
        if (attribute.AttributeEventUpdatesAttributeName is { Length: > 0 } updates)
        {
            // @bind compiles to a framework binder around the setter, which names the binder, not the component's code.
            return new BevEventBinding(target, attribute.AttributeName, "@bind-" + updates + ": writes the element's " + updates + " back into the component", BevEventBindingKind.Bind);
        }

        return new BevEventBinding(target, attribute.AttributeName, DescribeHandler(attribute.AttributeValue), BevEventBindingKind.DomEvent);
    }

    private static BevEventBinding? ChildBinding(in RenderTreeFrame component, in RenderTreeFrame attribute, Func<int, BevComponentRecord?> childByComponentId)
    {
        var value = attribute.AttributeValue;
        if (value is null or RenderFragment || !IsCallable(value) || IsRenderFragment(value.GetType()))
        {
            return null;
        }

        var child = component.ComponentId > 0 ? childByComponentId(component.ComponentId) : null;
        var target = child?.DisplayName ?? TypeNames.Short(component.ComponentType);
        return new BevEventBinding(target, attribute.AttributeName, DescribeHandler(value), BevEventBindingKind.ChildCallback, child?.InstanceId);
    }

    /// <summary>"&lt;button#save&gt;", "&lt;input.form-control&gt;", "&lt;li&gt;": enough to tell the elements of one component apart.</summary>
    private static string DescribeElement(RenderTreeFrame[] frames, int elementIndex)
    {
        var name = frames[elementIndex].ElementName;
        string? id = null, firstClass = null;
        for (var i = elementIndex + 1; i < frames.Length && frames[i].FrameType == RenderTreeFrameType.Attribute; i++)
        {
            var attribute = frames[i];
            if (attribute.AttributeValue is not string text || text.Length == 0)
            {
                continue;
            }

            if (attribute.AttributeName == "id")
            {
                id = text;
            }
            else if (attribute.AttributeName == "class")
            {
                var space = text.IndexOf(' ');
                firstClass = space < 0 ? text : text[..space];
            }
        }

        return "<" + name + (id is not null ? "#" + id : firstClass is not null ? "." + firstClass : "") + ">";
    }

    private static bool IsCallable(object? value) =>
        value is Delegate || (value is not null && value.GetType() is var type && (type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>))));

    private static bool IsRenderFragment(Type type) =>
        type == typeof(RenderFragment) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>));

    /// <summary>What a handler value runs, as the developer wrote it: a method group by name, a lambda by the method declaring it.</summary>
    public static string DescribeHandler(object? value) => value switch
    {
        null => "null",
        Delegate d => BevEventCallbacks.Method(d),
        _ when IsCallable(value) => BlazorReflection.EventCallbackDelegate(value) is { } target ? BevEventCallbacks.Method(target) : BevEventCallbacks.Describe(value),
        _ => TypeNames.Short(value.GetType()),
    };
}

/// <summary>Describes EventCallbacks and delegates by the method they run, which is what "EventCallback (bound)" never said.</summary>
public static class BevEventCallbacks
{
    private static readonly ConcurrentDictionary<Type, System.Reflection.PropertyInfo?> HasDelegateProperties = new();

    /// <summary>"EventCallback → ProductList.OnSelected", "EventCallback (empty)", or "EventCallback (bound)" when the target cannot be read.</summary>
    public static string Describe(object callback)
    {
        var label = callback.GetType().IsGenericType ? "EventCallback<" + TypeNames.Short(callback.GetType().GetGenericArguments()[0]) + ">" : "EventCallback";
        if (BlazorReflection.EventCallbackDelegate(callback) is { } target)
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
    /// "Counter.IncrementCount" for a method group; "lambda in Counter.BuildRenderTree" for a lambda, whose compiled
    /// name ("&lt;BuildRenderTree&gt;b__3_0") and closure class ("&lt;&gt;c__DisplayClass3_0") mean nothing to a reader.
    /// </summary>
    public static string Method(Delegate handler)
    {
        var method = handler.Method;
        var declaring = method.DeclaringType;
        while (declaring is not null && declaring.IsDefined(typeof(CompilerGeneratedAttribute), false) && declaring.DeclaringType is not null)
        {
            declaring = declaring.DeclaringType;
        }

        var owner = declaring is null ? "" : TypeNames.Short(declaring) + ".";
        var name = method.Name;
        if (name.StartsWith('<'))
        {
            var close = name.IndexOf('>');
            var outer = close > 1 ? name[1..close] : null;
            return "lambda in " + owner + (outer ?? "?");
        }

        return owner + name;
    }
}

/// <summary>Names render modes the way they are written in markup.</summary>
public static class BevRenderModes
{
    /// <summary>"InteractiveServer", "InteractiveWebAssembly", "InteractiveAuto", with ", no prerendering" when it is off; null for no mode.</summary>
    public static string? Describe(IComponentRenderMode? mode) => mode switch
    {
        null => null,
        InteractiveServerRenderMode server => "InteractiveServer" + (server.Prerender ? "" : ", no prerendering"),
        InteractiveWebAssemblyRenderMode wasm => "InteractiveWebAssembly" + (wasm.Prerender ? "" : ", no prerendering"),
        InteractiveAutoRenderMode auto => "InteractiveAuto" + (auto.Prerender ? "" : ", no prerendering"),
        _ => TypeNames.Short(mode.GetType()).Replace("RenderMode", ""),
    };
}
