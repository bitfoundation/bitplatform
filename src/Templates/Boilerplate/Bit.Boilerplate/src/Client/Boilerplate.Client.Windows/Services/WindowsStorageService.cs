namespace Boilerplate.Client.Windows.Services;

public partial class WindowsStorageService : IStorageService
{
    private readonly Dictionary<string, string?> tempStorage = [];

    public async ValueTask<string?> GetItem(string key)
    {
        if (tempStorage.TryGetValue(key, out string? value))
            return value;

        return Application.UserAppDataRegistry.GetValue(key) as string;
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        return string.IsNullOrEmpty(await GetItem(key)) is false;
    }

    public async ValueTask RemoveItem(string key)
    {
        Application.UserAppDataRegistry.DeleteValue(key);
        tempStorage.Remove(key);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (persistent)
        {
            Application.UserAppDataRegistry.SetValue(key, value ?? string.Empty);
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
