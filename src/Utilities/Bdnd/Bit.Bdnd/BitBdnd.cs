using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bdnd;

/// <summary>
/// Extension methods to register Bit.Bdnd services in the DI container.
/// </summary>
public static class BitBdnd
{
    /// <summary>
    /// Registers the Bit.Bdnd interop service. The <see cref="BdndDragDropManager"/> itself is
    /// provided per-subtree by the <see cref="BdndProvider"/> component, so it is not registered
    /// here; this only wires up the JS interop dependency. Calling it more than once is harmless.
    /// Call this in <c>Program.cs</c> before <c>builder.Build()</c>:
    /// <code>builder.Services.AddBitBdndServices();</code>
    /// </summary>
    public static IServiceCollection AddBitBdndServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Keep the JSON-marshaled interop DTOs trim/AOT-safe. These types cross the JS<->.NET
        // boundary via reflection-based System.Text.Json (InvokeAsync<T>, [JSInvokable] params),
        // so the trimmer can't see their members as used and would otherwise strip them - leading
        // to empty/failed (de)serialization in published WebAssembly builds.
        PreserveInteropContracts();

        services.TryAddScoped<BdndInterop>();

        return services;
    }

    // Roots the public properties + constructors of every type that is (de)serialized across JS
    // interop so the trimmer preserves them. The [DynamicDependency] attributes take effect because
    // this method is reachable from AddBitBdndServices (an app entry point).
    // The options records have no parameterless constructor: System.Text.Json goes through their
    // primary one - to serialize them too - and matches its parameters to the properties by name.
    // The trimmer strips those names from a constructor it does not keep for reflection, so the
    // records keep their public constructors (a sealed record's copy constructor is private).
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndDomRect))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndNativeDropPayload))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndNativeFile))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BdndDragSourceOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BdndDropTargetOptions))]
    private static void PreserveInteropContracts() { }
}
