namespace AdminPanel.Shared.Controllers;

public interface IAppController
{
    void AddQueryString(string existingQueryString) { }
    void AddQueryString(string key, object? value) { }
    void AddQueryStrings(Dictionary<string, object?> queryString) { }
}
