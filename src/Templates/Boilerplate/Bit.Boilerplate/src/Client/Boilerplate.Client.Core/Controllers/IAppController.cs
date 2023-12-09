namespace Boilerplate.Client.Core.Controllers;

public interface IAppController
{
    void AddQueryString(string key, object? value) { }
    void AddQueryString(Dictionary<string, object?> queryString) { }
}
