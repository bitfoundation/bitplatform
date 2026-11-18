namespace Bit.BevTools;

/// <summary>One thing a component wired up in its latest render, read from the render tree the renderer holds for it.</summary>
/// <param name="Target">The element (<c>&lt;button#save&gt;</c>) or child component (<c>ProductRow</c>) it is attached to.</param>
/// <param name="Event">The DOM event (<c>onclick</c>), or the child's parameter (<c>OnSelected</c>).</param>
/// <param name="Handler">What runs: <c>Counter.IncrementCount</c>, or a lambda and the method that declares it.</param>
/// <param name="Kind">A DOM event handler, the handler of a <c>@bind</c>, or a callback passed to a child component.</param>
/// <param name="ChildInstanceId">BevTools' instance id of the child component, for <see cref="BevToolsEventBindingKind.ChildCallback"/>.</param>
public sealed record BevToolsEventBinding(string Target, string Event, string Handler, BevToolsEventBindingKind Kind, long? ChildInstanceId = null)
{
    /// <summary>The method that runs, for finding where it is written; null for a <c>@bind</c> or an unreadable callback.</summary>
    public System.Reflection.MethodInfo? HandlerMethod { get; init; }
}
