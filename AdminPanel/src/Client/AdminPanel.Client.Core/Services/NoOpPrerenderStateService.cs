using System.Runtime.CompilerServices;

namespace AdminPanel.Client.Core.Services;

/// <summary>
/// The Client.Core codebase is shared across different Blazor hosting models, such as Hybrid and WebAssembly, 
/// which may or may not support pre-rendering. Additionally, the pre-rendering configuration can vary, 
/// being either enabled or disabled. To ensure compatibility across all scenarios, regardless of pre-rendering state, 
/// we introduce the IPrerenderStateService interface. This interface provides a GetValue method for data retrieval 
/// (e.g., in UserMenu’s OnInitAsync method).
/// The WebPrerenderService implementation of IPrerenderStateService supports pre-rendering by leveraging 
/// PersistentComponentState to persist data across renders. However, for scenarios like Blazor Hybrid, where 
/// pre-rendering is not applicable, a 'Noop' (no operation) implementation is also provided. This Noop implementation 
/// simply executes the passed function and returns the result without persisting any data to PersistentComponentState.
/// </summary>
public class NoopPrerenderStateService : IPrerenderStateService
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
