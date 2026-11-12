using Microsoft.Maui.Storage;

namespace Bit.Blex.Maui;

/// <summary>
/// <see cref="IBlexStorage"/> backed by .NET MAUI <see cref="IPreferences"/>, so persisted store
/// state lives in the OS-native app preferences (SharedPreferences on Android, NSUserDefaults on
/// iOS/Mac Catalyst, the local settings store on Windows). All operations complete synchronously.
/// </summary>
/// <remarks>
/// A mobile OS stops a backgrounded app without disposing anything, so a save still waiting out
/// <see cref="BlexPersistenceOptions.DebounceInterval"/> would be lost with the process. Call
/// <see cref="NotifySuspending"/> where the app learns it is going to the background
/// (<c>Application.OnSleep</c>, <c>Window.Stopped</c>) and every persistor over this storage writes what it holds.
/// </remarks>
public sealed class BlexPreferencesStorage : IBlexStorage, IBlexStorageSuspension
{
    // Every instance a persistor watches; weak, so a Blazor Hybrid scope's storage is not kept alive by the list.
    private static readonly List<WeakReference<BlexPreferencesStorage>> Watched = [];

    private readonly IPreferences _preferences;
    private bool _watched;

    /// <summary>Creates a storage over <see cref="Preferences.Default"/>.</summary>
    public BlexPreferencesStorage()
        : this(Preferences.Default)
    {
    }

    /// <summary>Creates a storage over the supplied preferences implementation.</summary>
    public BlexPreferencesStorage(IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _preferences = preferences;
    }

    /// <inheritdoc />
    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => new(_preferences.Get<string?>(key, null));

    /// <inheritdoc />
    public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _preferences.Set(key, value);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _preferences.Remove(key);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public event Action? Suspending;

    /// <inheritdoc />
    public ValueTask StartWatchingSuspensionAsync(CancellationToken cancellationToken = default)
    {
        lock (Watched)
        {
            if (!_watched)
            {
                Watched.Add(new WeakReference<BlexPreferencesStorage>(this));
                _watched = true;
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Tells every persistor over a <see cref="BlexPreferencesStorage"/> that the app is going to the background, so
    /// it writes the debounced save it still holds before the OS may stop the process. Writes complete synchronously,
    /// so they are done when this returns.
    /// </summary>
    /// <exception cref="AggregateException">
    /// One or more <see cref="Suspending"/> handlers threw. Every handler of every storage is still called first, so
    /// one failing does not keep the others from writing.
    /// </exception>
    /// <example><code>
    /// public partial class App : Application
    /// {
    ///     protected override void OnSleep() => BlexPreferencesStorage.NotifySuspending();
    /// }
    /// </code></example>
    public static void NotifySuspending()
    {
        List<BlexPreferencesStorage> live = [];
        lock (Watched)
        {
            Watched.RemoveAll(reference => !reference.TryGetTarget(out _));
            foreach (var reference in Watched)
            {
                if (reference.TryGetTarget(out var storage))
                    live.Add(storage);
            }
        }

        List<Exception>? failures = null;
        foreach (var storage in live)
        {
            if (storage.Suspending is not { } suspending)
                continue;

            foreach (var handler in suspending.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception ex)
                {
                    (failures ??= []).Add(ex);
                }
            }
        }

        if (failures is not null)
            throw new AggregateException("One or more handlers failed while the app was going to the background.", failures);
    }
}
