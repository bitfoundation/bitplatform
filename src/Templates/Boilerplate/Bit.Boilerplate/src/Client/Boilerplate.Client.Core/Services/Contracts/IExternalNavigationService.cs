namespace Boilerplate.Client.Core.Services.Contracts;

// Check out Client.web/wwwroot/web-interop-app.html's comments.
public interface IExternalNavigationService
{
    Task NavigateToAsync(string url);
}
