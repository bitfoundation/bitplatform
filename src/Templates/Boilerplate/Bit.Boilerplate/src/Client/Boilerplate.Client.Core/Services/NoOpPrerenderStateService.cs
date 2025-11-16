using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// <inheritdoc cref="IPrerenderStateService"/>
/// </summary>
public class NoOpPrerenderStateService : IPrerenderStateService
{
    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            return await Task.Run(() => factory()); // This would improve responsiveness on Blazor Hybrid scenarios
        }
        return await factory();
    }

    public Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        return GetValue("", factory);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
