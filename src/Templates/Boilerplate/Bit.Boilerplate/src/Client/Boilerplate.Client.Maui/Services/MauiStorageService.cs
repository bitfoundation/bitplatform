namespace Boilerplate.Client.Maui.Services;

public partial class MauiStorageService : IStorageService
{
    private readonly Dictionary<string, string?> tempStorage = [];

    public async ValueTask<string?> GetItem(string key)
    {
        tempStorage.TryGetValue(key, out string? value);
        return Preferences.Get(key, value);
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        return Preferences.ContainsKey(key);
    }

    public async ValueTask RemoveItem(string key)
    {
        Preferences.Remove(key);
        tempStorage.Remove(key);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (persistent)
        {
            Preferences.Set(key, value);
        }
        else
        {
            if (tempStorage.TryAdd(key, value) is false)
            {
                tempStorage[key] = value;
            }
        }
    }
}
