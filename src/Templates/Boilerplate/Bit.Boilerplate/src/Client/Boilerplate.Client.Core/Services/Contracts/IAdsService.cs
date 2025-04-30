namespace Boilerplate.Client.Core.Services.Contracts;

public interface IAdsService
{
    ValueTask Init(string adUnitPath);

    ValueTask Watch();
}
