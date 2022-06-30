namespace TodoTemplate.App.Services.Contracts;

public interface IStateService
{
    /// <summary>
    /// ysm: Be jaye estefade az method haye ApplicationState.TryTakeFromJson, ApplicationState.RegisterOnPersisting and ApplicationState.PersistAsJson, ke dar inja tozih dade shode and, https://docs.microsoft.com/en-us/aspnet/core/blazor/components/prerendering-and-integration#persist-prerendered-state mitavanid be sadegi dar OnInit e component ha va page ha az StateService.GetValue baraye har an che ke async-await lazem darad (baraye mesal, gereftan e etelaate karbar jari) estefade konid.
    /// </summary>
    Task<T?> GetValue<T>(string key, Func<Task<T?>> factory);
}
