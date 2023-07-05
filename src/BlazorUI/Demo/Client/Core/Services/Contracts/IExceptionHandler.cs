namespace Bit.BlazorUI.Demo.Client.Core.Services.Contracts;

public interface IExceptionHandler
{
    void Handle(Exception exception, IDictionary<string, object?>? parameters = null);
}
