namespace Boilerplate.Client.Core.Services.Contracts;

public interface ILocalHttpServer
{
    int Start(CancellationToken cancellationToken);
}
