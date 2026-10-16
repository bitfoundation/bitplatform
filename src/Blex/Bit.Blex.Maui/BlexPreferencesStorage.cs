using Microsoft.Maui.Storage;

namespace Bit.Blex.Maui;

/// <summary>
/// <see cref="IBlexStorage"/> backed by .NET MAUI <see cref="IPreferences"/>, so persisted store
/// state lives in the OS-native app preferences (SharedPreferences on Android, NSUserDefaults on
/// iOS/Mac Catalyst, the local settings store on Windows). All operations complete synchronously.
/// </summary>
public sealed class BlexPreferencesStorage : IBlexStorage
{
    private readonly IPreferences _preferences;

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
}
