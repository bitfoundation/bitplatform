namespace BlazorDual.Web.Services.Contracts;

public interface IStateService
{
    /// <summary>
    /// Instead of using ApplicationState.TryTakeFromJson, ApplicationState.RegisterOnPersisting, and ApplicationState.PersistAsJson 
    /// (explained here: https://docs.microsoft.com/en-us/aspnet/core/blazor/components/prerendering-and-integration#persist-prerendered-state), 
    /// one can easily use the following method (StateService.GetValue) in the OnInit lifecycle method of the Blazor components 
    /// or pages to retrieve everything that requires an async-await (like current user's info).
    /// </summary>
    Task<T?> GetValue<T>(string key, Func<Task<T?>> factory);
}
