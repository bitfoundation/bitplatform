using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BevTools;

/// <summary>
/// Reads which event handlers and callbacks a component bound, from the frames of its latest render. That is the
/// renderer's own record of the component's output, so nothing is inferred: a handler the component binds only under
/// a condition appears exactly while that condition holds.
/// </summary>
internal static class BevToolsComponentWiringReader
{
    private const int MaxBindings = 200;

    public static BevToolsComponentWiring Read(BevToolsComponentRecord record, Func<int, BevToolsComponentRecord?> childByComponentId)
    {
        if (!BevToolsBlazorReflection.CanReadRenderTree)
        {
            return BevToolsComponentWiring.NotReadable("The renderer's render tree could not be accessed on this framework version.");
        }

        if (record.Renderer is not { } renderer || record.Component is not { } component)
        {
            return BevToolsComponentWiring.NotReadable(record.IsDisposed ? "The component was disposed." : "The component has not rendered yet.");
        }

        var state = BevToolsBlazorReflection.ResolveState(renderer, component);
        if (state is null || BevToolsBlazorReflection.CurrentFrames(state) is not { } frames)
        {
            return BevToolsComponentWiring.NotReadable("The renderer does not hold a render tree for this component (not rendered yet, or disposed).");
        }

        var bindings = new List<BevToolsEventBinding>();
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

        return new BevToolsComponentWiring(bindings, total, null);
    }

    private static BevToolsEventBinding? ElementBinding(RenderTreeFrame[] frames, int elementIndex, in RenderTreeFrame attribute)
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
            // @bind compiles to a framework binder around the setter, which names the binder, not the component's code. A
            // setter the component wrote as a method (@bind:set="OnQuery") is named; the assignment @bind compiles to is not.
            var handler = attribute.AttributeValue as Delegate ?? (IsCallable(attribute.AttributeValue) ? BevToolsBlazorReflection.EventCallbackDelegate(attribute.AttributeValue!) : null);
            var setter = handler is null ? null : BevToolsBlazorReflection.BindSetter(handler);
            return setter is not null && !setter.Method.Name.StartsWith('<')
                ? new BevToolsEventBinding(target, attribute.AttributeName, "@bind-" + updates + " → " + BevToolsEventCallbacks.Method(setter), BevToolsEventBindingKind.Bind) { HandlerMethod = setter.Method }
                : new BevToolsEventBinding(target, attribute.AttributeName, "@bind-" + updates + ": writes the element's " + updates + " back into the component", BevToolsEventBindingKind.Bind);
        }

        return new BevToolsEventBinding(target, attribute.AttributeName, DescribeHandler(attribute.AttributeValue), BevToolsEventBindingKind.DomEvent) { HandlerMethod = HandlerMethodOf(attribute.AttributeValue) };
    }

    private static BevToolsEventBinding? ChildBinding(in RenderTreeFrame component, in RenderTreeFrame attribute, Func<int, BevToolsComponentRecord?> childByComponentId)
    {
        var value = attribute.AttributeValue;
        if (value is null or RenderFragment || !IsCallable(value) || IsRenderFragment(value.GetType()))
        {
            return null;
        }

        var child = component.ComponentId > 0 ? childByComponentId(component.ComponentId) : null;
        var target = child?.DisplayName ?? BevToolsTypeNames.Short(component.ComponentType);
        return new BevToolsEventBinding(target, attribute.AttributeName, DescribeHandler(value), BevToolsEventBindingKind.ChildCallback, child?.InstanceId) { HandlerMethod = HandlerMethodOf(value) };
    }

    /// <summary>"&lt;button#save&gt;", "&lt;input.form-control&gt;", "&lt;li&gt;": enough to tell the elements of one component apart.</summary>
    internal static string DescribeElement(RenderTreeFrame[] frames, int elementIndex)
    {
        var name = frames[elementIndex].ElementName;
        string? id = null, firstClass = null;
        // Within the element's own subtree: a buffer read in place holds stale frames past the end of the tree.
        var end = Math.Min(frames.Length, elementIndex + frames[elementIndex].ElementSubtreeLength);
        for (var i = elementIndex + 1; i < end && frames[i].FrameType == RenderTreeFrameType.Attribute; i++)
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

    private static System.Reflection.MethodInfo? HandlerMethodOf(object? value) => value switch
    {
        Delegate d => d.Method,
        _ when IsCallable(value) => BevToolsBlazorReflection.EventCallbackDelegate(value!)?.Method,
        _ => null,
    };

    /// <summary>What a handler value runs, as the developer wrote it: a method group by name, a lambda by the method declaring it.</summary>
    public static string DescribeHandler(object? value) => value switch
    {
        null => "null",
        Delegate d => BevToolsEventCallbacks.Method(d),
        _ when IsCallable(value) => BevToolsBlazorReflection.EventCallbackDelegate(value) is { } target ? BevToolsEventCallbacks.Method(target) : BevToolsEventCallbacks.Describe(value),
        _ => BevToolsTypeNames.Short(value.GetType()),
    };
}
