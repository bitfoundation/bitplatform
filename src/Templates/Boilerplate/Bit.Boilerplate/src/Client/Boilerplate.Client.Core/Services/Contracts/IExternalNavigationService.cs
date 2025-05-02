namespace Boilerplate.Client.Core.Services.Contracts;

// Checkout HybridAppWebInterop.razor's comments.
public interface IExternalNavigationService
{
    Task NavigateToAsync(string url);
}
