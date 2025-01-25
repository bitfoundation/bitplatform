using System.Runtime.CompilerServices;

namespace Boilerplate.Shared.Services.Contracts;

/// <summary>
/// The Client.Core codebase is designed to support various Blazor hosting models, including Hybrid and WebAssembly, 
/// which may or may not enable pre-rendering. To ensure consistent behavior across all scenarios, 
/// the `IPrerenderStateService` interface is introduced. This service provides the `GetValue` method for data retrieval, 
/// such as during the `OnInitAsync` method in components like SettingsPage.
///
/// The `WebPrerenderStateService` implementation of `IPrerenderStateService` facilitates pre-rendering by using 
/// PersistentComponentState to persist data across renders, simplifies the process of managing application state in pre-rendering scenarios outlined in 
/// the documentation: https://docs.microsoft.com/en-us/aspnet/core/blazor/components/prerendering-and-integration#persist-prerendered-state.
/// 
/// For cases where pre-rendering is unnecessary, such as in Blazor Hybrid, a `NoOpPrerenderStateService` is provided. This implementation simply executes the provided 
/// function and returns its result without persisting any state.
///
/// If pre-rendering is not required for your project, this service and its related dependencies can be safely removed.
/// </summary>
public interface IPrerenderStateService : IAsyncDisposable
{
    /// <summary>
    /// <inheritdoc cref="IPrerenderStateService"/>
    /// </summary>
    Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "");

    Task<T?> GetValue<T>(string key, Func<Task<T?>> factory);
}
