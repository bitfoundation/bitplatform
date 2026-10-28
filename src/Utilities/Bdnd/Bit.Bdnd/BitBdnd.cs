using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

namespace Bit.Bdnd;

/// <summary>
/// Extension methods to register Bit.Bdnd services in the DI container.
/// </summary>
public static class BitBdnd
{
    // One interop per browser connection for applications that never called AddBitBdndServices. Keyed weakly by the
    // connection's IJSRuntime, so a closed Blazor Server circuit takes its interop with it. Never disposed, and nothing
    // needs it to be: it lives as long as the connection, like a scope, and the one thing disposal would add, releasing
    // the module reference held in the browser, has no page left to do it in by then.
    private static readonly ConditionalWeakTable<IJSRuntime, BdndInterop> _ownInterop = new();

    /// <summary>
    /// Registers the Bit.Bdnd interop service. The <see cref="BdndDragDropManager"/> itself is
    /// provided per-subtree by the <see cref="BdndProvider"/> component, so it is not registered
    /// here; this only wires up the JS interop dependency. Calling it more than once is harmless.
    /// Call this in <c>Program.cs</c> before <c>builder.Build()</c>:
    /// <code>builder.Services.AddBitBdndServices();</code>
    /// A WebAssembly app that lazy loads Bit.Bdnd leaves it out: the draggables and drop targets then create the
    /// interop themselves, once per browser connection, and <see cref="GetBdndInterop"/> hands out the same one.
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

    /// <summary>
    /// The <see cref="BdndInterop"/> the draggables and drop targets of this circuit or WebAssembly runtime use: the one
    /// <see cref="AddBitBdndServices"/> registered, otherwise one Bdnd creates itself, once per browser connection.
    /// Inject <see cref="BdndInterop"/> instead wherever the services are registered; this is for an app that lazy loads
    /// Bit.Bdnd on WebAssembly and so cannot register them:
    /// <code>
    /// @inject IServiceProvider Services
    ///
    /// var rect = await Services.GetBdndInterop().GetBoundingClientRectAsync(element);
    /// </code>
    /// </summary>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    public static BdndInterop GetBdndInterop(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // The components reach the interop through here whether or not the app registered it, so the contracts stay
        // rooted in an app that never calls AddBitBdndServices.
        PreserveInteropContracts();

        if (services.GetService<BdndInterop>() is { } registered) return registered;

        var jsRuntime = services.GetService<IJSRuntime>() ?? throw new InvalidOperationException(
            "Bit.Bdnd's services are not registered: call builder.Services.AddBitBdndServices() in Program.cs. " +
            "Without them Bdnd creates its own interop for each browser connection, which needs the IJSRuntime of a Blazor app, and this container has none.");

        return _ownInterop.GetValue(jsRuntime, static js => new BdndInterop(js));
    }

    // Roots the public properties + constructors of every type that is (de)serialized across JS
    // interop so the trimmer preserves them. The [DynamicDependency] attributes take effect because
    // this method is reachable from AddBitBdndServices (an app entry point) and from GetBdndInterop,
    // which every draggable and drop target calls.
    // Each of these classes has a public parameterless constructor (the options records take init-only
    // properties, not positional parameters): without one System.Text.Json goes through a parameterized
    // constructor, even to serialize, and matches its parameters to the properties by name - names the
    // trimmer strips from a constructor it does not keep for reflection.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndDomRect))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndNativeDropPayload))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndNativeFile))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndDragSourceOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndDropTargetOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndKeyboardTarget))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BdndKeyboardCandidate))]
    private static void PreserveInteropContracts() { }
}
