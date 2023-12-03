namespace Bit.BlazorUI.Demo.Client.Core.Services.Contracts;

/// <summary>
/// This service simplifies the process of persisting application state in Pre-Rendering mode
/// (explained in this documentation: https://docs.microsoft.com/en-us/aspnet/core/blazor/components/prerendering-and-integration#persist-prerendered-state).
/// If your project does not require prerendering to be enabled, you can completely remove this service and its usages from your project.
/// </summary>
public interface IPrerenderStateService
{
    /// <summary>
    /// Instead of using ApplicationState.TryTakeFromJson, ApplicationState.RegisterOnPersisting, 
    /// and ApplicationState.PersistAsJson (explained here: https://docs.microsoft.com/en-us/aspnet/core/blazor/components/prerendering-and-integration#persist-prerendered-state),
    /// one can easily use the following method (<see cref="GetValue"/>) in the OnInit lifecycle method of the Blazor components or pages
    /// to retrieve everything that requires an async-await (like current user's info).
    /// </summary>
    Task<T?> GetValue<T>(string key, Func<Task<T?>> factory);
}
