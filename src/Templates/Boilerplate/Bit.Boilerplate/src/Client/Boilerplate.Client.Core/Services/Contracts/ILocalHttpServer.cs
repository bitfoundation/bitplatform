namespace Boilerplate.Client.Core.Services.Contracts;

public interface ILocalHttpServer
{
    Task Start();

    int Port { get; }
}
