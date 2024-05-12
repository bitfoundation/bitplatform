namespace Boilerplate.Client.Core.Services.Contracts;

public interface ILocalHttpServer
{
    Task<int?> Start() => Task.FromResult((int?)null);
}
