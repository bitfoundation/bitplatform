namespace Boilerplate.Client.Windows.Services;

public class WindowsStorageService : IStorageService
{
    private readonly Dictionary<string, string?> tempStorage = [];

    public async ValueTask<string?> GetItem(string key)
    {
        if (tempStorage.TryGetValue(key, out string? value))
            return value;

        return App.Current.Properties[key]?.ToString();
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        return App.Current.Properties.Contains(key);
    }

    public async ValueTask RemoveItem(string key)
    {
        App.Current.Properties.Remove(key);
        tempStorage.Remove(key);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (persistent)
        {
            App.Current.Properties[key] = value;
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
