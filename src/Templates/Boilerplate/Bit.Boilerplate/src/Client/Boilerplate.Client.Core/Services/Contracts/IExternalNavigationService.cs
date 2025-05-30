namespace Boilerplate.Client.Core.Services.Contracts;

// Check out HybridAppWebInterop.razor's comments.
public interface IExternalNavigationService
{
    Task NavigateToAsync(string url);
}
