namespace Boilerplate.Client.Core.Services.Contracts;

public interface IAdsService
{
    ValueTask Init<T>(string adUnitPath, DotNetObjectReference<T> dotnetObj) where T : class;

    ValueTask Watch();
}
