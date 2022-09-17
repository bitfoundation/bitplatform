namespace TodoTemplate.Client.Services.Contracts;

public interface IExceptionHandler
{
    void Handle(Exception exception, IDictionary<string, object?>? parameters = null);
}
