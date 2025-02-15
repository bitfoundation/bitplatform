using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// <inheritdoc cref="IPrerenderStateService"/>
/// </summary>
public class NoOpPrerenderStateService : IPrerenderStateService
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task<T?> GetValue<T>(Func<Task<T?>> factory, 
        [CallerLineNumber] int lineNumber = 0, 
        [CallerMemberName] string memberName = "", 
        [CallerFilePath] string filePath = "")
    {
        return factory();
    }

    public Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        return factory();
    }
}
