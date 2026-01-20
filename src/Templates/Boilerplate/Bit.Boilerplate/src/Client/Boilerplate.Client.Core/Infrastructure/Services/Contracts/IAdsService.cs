namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

public interface IAdsService
{
    Task Init(string adUnitPath);

    Task<AdWatchResult> Watch();
}

public enum AdWatchResult
{
    Rewarded,
    Failed
}
