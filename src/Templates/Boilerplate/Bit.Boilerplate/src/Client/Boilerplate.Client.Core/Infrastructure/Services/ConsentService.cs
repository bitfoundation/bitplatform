//+:cnd:noEmit
namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// What the user has agreed to, per category, for what is not strictly necessary.
/// <para>
/// Categories rather than vendor names: a vendor list is only ever the list somebody noticed, which is how a
/// compiled-in SDK like Sentry gets missed. Anything on legitimate interests stays out - error reporting behind a
/// toggle would only ever show the crashes of users who agreed. Those are minimised instead (see appsettings.json).
/// </para>
/// </summary>
public partial class ConsentService
{
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private PubSubService pubSubService = default!;

    /// <summary>Public so a UI test can answer the banner before the app opens it - see <c>AppPageTest</c>.</summary>
    public const string StorageKey = "consent";

    /// <summary>Cached: every gate calls this, and the store behind it is a JS interop call on the web.</summary>
    private Dictionary<ConsentCategory, bool>? decisions;

    private readonly SemaphoreSlim writeLock = new(1, 1);

    /// <summary>
    /// The categories this deployment can ask about. Empty means nothing non-essential is wired up, so there is no
    /// question worth asking - see <c>AppConsentBanner</c>.
    /// </summary>
    public static readonly ConsentCategory[] AskableCategories =
    [
        //#if (appInsights == true)
        ConsentCategory.Analytics,
        //#endif
        //#if (ads == true)
        ConsentCategory.Advertising,
        //#endif
    ];

    /// <summary>Nothing is granted until it is asked for and answered: consent cannot be a default.</summary>
    public async Task<bool> IsGranted(ConsentCategory category)
    {
        return (await Read()).GetValueOrDefault(category);
    }

    /// <summary>True while the user has answered nothing at all, which is what opens the banner.</summary>
    public async Task<bool> IsPending()
    {
        return AskableCategories.Length > 0 && await storageService.GetItem(StorageKey) is null;
    }

    public async Task SetAll(bool granted)
    {
        await Set(AskableCategories.ToDictionary(category => category, _ => granted));
    }

    public async Task Set(ConsentCategory category, bool granted)
    {
        // Read-modify-write over one storage key: two toggles flipped together would otherwise each write a snapshot
        // taken before the other, and the loser's category would silently revert.
        await writeLock.WaitAsync();

        try
        {
            var current = new Dictionary<ConsentCategory, bool>(await Read())
            {
                [category] = granted
            };

            await Set(current);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private async Task Set(Dictionary<ConsentCategory, bool> values)
    {
        decisions = values;

        // A flat "name=0|1;" line rather than json, so it stays readable to someone inspecting their own storage.
        await storageService.SetItem(StorageKey, string.Join(';', values.Select(pair => $"{pair.Key}={(pair.Value ? 1 : 0)}")));

        // Whoever acted on the old answer has to hear that it changed. See AppClientCoordinator.
        pubSubService.Publish(ClientAppMessages.CONSENT_CHANGED);
    }

    private async Task<Dictionary<ConsentCategory, bool>> Read()
    {
        if (decisions is not null) return decisions;

        var stored = await storageService.GetItem(StorageKey);

        return decisions = string.IsNullOrWhiteSpace(stored)
            ? []
            : stored.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Split('='))
                    .Where(parts => parts.Length is 2 && Enum.TryParse<ConsentCategory>(parts[0], out _))
                    .ToDictionary(parts => Enum.Parse<ConsentCategory>(parts[0]), parts => parts[1] is "1");
    }
}

/// <summary>
/// A purpose, not a vendor: two analytics SDKs are one category, and one vendor doing two jobs is two.
/// </summary>
public enum ConsentCategory
{
    /// <summary>
    /// Telemetry that identifies the device across visits. Refusing it does not stop error reporting.
    /// </summary>
    Analytics,

    /// <summary>The advertising script, which sets identifiers of its own the moment it loads.</summary>
    Advertising
}
