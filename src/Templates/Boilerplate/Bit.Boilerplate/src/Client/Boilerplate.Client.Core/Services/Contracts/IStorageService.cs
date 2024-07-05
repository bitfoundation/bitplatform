namespace Boilerplate.Client.Core.Services.Contracts;

public interface IStorageService
{
    ValueTask SetItem(string key, string? value, bool persistent = true);

    ValueTask<string?> GetItem(string key);

    ValueTask<bool> IsPersistent(string key);

    ValueTask RemoveItem(string key);
}
