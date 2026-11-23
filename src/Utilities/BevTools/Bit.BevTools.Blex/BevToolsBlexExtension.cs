using System.Runtime.CompilerServices;
using Bit.Blex;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>
/// bit Blex in the State panel as an extension (<see cref="BitBevToolsBlex.AddBlex"/>): what a session attached at runtime
/// can take, since the app's container was built before Bit.BevTools was loaded and holds no state provider of it.
/// </summary>
internal sealed class BevToolsBlexExtension : IBevToolsExtension
{
    // One provider per manager: a singleton manager (a host without scopes, or Blex registered as a singleton) serves
    // every session, and a provider per session would stay subscribed to it after its session ended. Lazy: two sessions
    // starting at once may both run GetValue's factory, and a provider it then drops would stay subscribed too.
    private static readonly ConditionalWeakTable<BlexManager, Lazy<BevToolsBlexStateProvider>> Providers = new();

    private readonly BevToolsOptions _options;

    internal BevToolsBlexExtension(BevToolsOptions options) => _options = options;

    public string Name => "bit Blex";

    public void Configure(IBevToolsExtensionBuilder builder) => builder.AddStateProvider(services =>
        // The one AddBevToolsBlex registered, where it did: the State panel shows a provider once.
        services.GetServices<IBevToolsStateProvider>().OfType<BevToolsBlexStateProvider>().FirstOrDefault()
        ?? Providers.GetValue(services.GetRequiredService<BlexManager>(), manager => new(() => new BevToolsBlexStateProvider(manager, _options.MaxStateChangesPerProvider))).Value);
}
