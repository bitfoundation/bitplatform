namespace Boilerplate.Client.Core.Services.Contracts;

/// <summary>
/// Contract for Publish/Subscribe pattern.
/// </summary>
public interface IPubSubService
{
    void Publish(string message, object? payload = null);
    Action Subscribe(string message, Func<object?, Task> handler);
}
