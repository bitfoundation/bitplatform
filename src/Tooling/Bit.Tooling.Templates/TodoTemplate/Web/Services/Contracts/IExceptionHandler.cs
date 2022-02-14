namespace TodoTemplate.App.Services.Contracts;

public interface IExceptionHandler
{
    void OnExceptionReceived(Exception exception, IDictionary<string, object?>? parameters = null);
}
