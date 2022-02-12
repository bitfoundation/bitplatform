namespace TodoTemplate.App.Services.Contracts;

public interface IStateService
{
    Task<T?> GetValue<T>(string key, Func<Task<T?>> factory);
}
