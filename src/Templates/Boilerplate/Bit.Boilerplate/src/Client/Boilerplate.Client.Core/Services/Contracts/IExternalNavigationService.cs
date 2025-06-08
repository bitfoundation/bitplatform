namespace Boilerplate.Client.Core.Services.Contracts;

// Check out WebInteropApp.razor's comments.
public interface IExternalNavigationService
{
    Task NavigateToAsync(string url);
}
