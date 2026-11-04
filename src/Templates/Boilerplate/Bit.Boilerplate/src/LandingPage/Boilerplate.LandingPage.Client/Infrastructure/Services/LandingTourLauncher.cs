namespace Boilerplate.LandingPage.Client.Infrastructure.Services;

public sealed class LandingTourLauncher
{
    public event Action? Requested;

    public void Start() => Requested?.Invoke();
}
