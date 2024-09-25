﻿using Boilerplate.Client.Core.Services.Contracts;

namespace Boilerplate.Tests.Services;
public partial class TestStorageService : IStorageService
{
    private readonly Dictionary<string, string?> tempStorage = [];

    public async ValueTask<string?> GetItem(string key)
    {
        tempStorage.TryGetValue(key, out string? value);
        return value;
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        return false;
    }

    public async ValueTask RemoveItem(string key)
    {
        tempStorage.Remove(key);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (tempStorage.TryAdd(key, value) is false)
        {
            tempStorage[key] = value;
        }
    }
}
