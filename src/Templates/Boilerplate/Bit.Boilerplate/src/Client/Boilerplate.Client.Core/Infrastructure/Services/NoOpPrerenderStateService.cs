using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// <inheritdoc cref="IPrerenderStateService"/>
/// </summary>
public class NoOpPrerenderStateService : IPrerenderStateService
{
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

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
