using Bit.Blex;
using Bit.Blex.Blazor;
using Bit.Blex.Tests.Harness.Stores;

namespace Microsoft.Extensions.DependencyInjection;

public static class HarnessServiceCollectionExtensions
{
    /// <summary>The prefix of every localStorage key the harness persists under, which the suites read back.</summary>
    public const string StorageKeyPrefix = "blex-harness:";

    /// <summary>
    /// Registers Blex with every browser-facing feature switched on: localStorage persistence with cross-tab
    /// sync, undo/redo, and the DevTools bridge (which <c>&lt;BlexProvider&gt;</c> connects). Every host calls
    /// this, so the configuration under test is identical wherever the harness runs.
    /// </summary>
    public static IServiceCollection AddBlexHarness(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBitBlexServices(options => options.DevToolsName = "Blex harness");

        services.AddBitBlexStore<CounterStore>();
        services.AddBitBlexStore<TokenStore>();
        services.AddBitBlexStore<ProfileStore>();
        services.AddBitBlexStore<EffectStore>();

        services.AddBitBlexLocalStoragePersistence(options =>
        {
            options.KeyPrefix = StorageKeyPrefix;
            options.SyncAcrossTabs = true;
            // DebounceInterval stays unset, so every change is written straight away: a test reloads
            // right after it clicks.
        });

        services.AddBitBlexHistory();

        return services;
    }
}
