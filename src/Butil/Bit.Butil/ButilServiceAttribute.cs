using System;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Butil;

/// <summary>
/// Marks a Butil class as a DI service that <see cref="BitButil.AddBitButilServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>
/// registers as scoped. Applying it is the only step needed to add a Butil service - there is no
/// registration list to keep in sync.
/// </summary>
/// <remarks>
/// The type argument looks redundant next to the class it decorates, but it is what makes the library
/// trimmable, so it is required. The reasoning, in order:
/// <list type="number">
/// <item>Registration finds services by reflecting over this assembly instead of naming them in
/// <c>AddScoped&lt;T&gt;()</c> calls. A hard-coded call is a static reference that roots the class, so
/// one method naming every Butil class forced every consumer to carry all of them; reflection gives the
/// trimmer nothing to follow, and a class nobody injects is removed and never discovered at startup.</item>
/// <item>But a class that IS injected survives only through the reference the consumer creates, and
/// <c>@inject LocalStorage</c> references the <b>type</b>, never its constructor. The trimmer therefore
/// keeps the class with zero public constructors and DI activation fails at runtime.</item>
/// <item>Hence <c>serviceType</c>, annotated with <see cref="DynamicallyAccessedMemberTypes.PublicConstructors"/>:
/// the trimmer processes a custom attribute only once it has decided to keep the type the attribute sits
/// on, so <c>[ButilService(typeof(Clipboard))]</c> on <c>Clipboard</c> preserves the constructor of a
/// surviving class without rooting a class that would otherwise be trimmed.</item>
/// </list>
/// The argument must be the decorated type itself; anything else registers the wrong service.
/// <br/>
/// Only types inside the <c>Bit.Butil</c> assembly are scanned, so applying this in another assembly
/// has no effect.
/// </remarks>
/// <param name="serviceType">The decorated class itself, for example <c>[ButilService(typeof(Clipboard))]</c>.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ButilServiceAttribute(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType) : Attribute
{
    /// <summary>The service type to register. Carries the constructor annotation through to the registration call.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ServiceType { get; } = serviceType;
}
