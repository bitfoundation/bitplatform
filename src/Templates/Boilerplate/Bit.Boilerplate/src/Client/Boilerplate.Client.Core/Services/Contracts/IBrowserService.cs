namespace Boilerplate.Client.Core.Services.Contracts;

public interface IBrowserService
{
    Task OpenUrl(string url);
}
