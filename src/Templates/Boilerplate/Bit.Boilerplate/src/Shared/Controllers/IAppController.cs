namespace Boilerplate.Shared.Controllers;

public interface IAppController
{
    void AddQueryString(string key, object? value) { }
    void AddQueryStrings(Dictionary<string, object?> queryString) { }
}
