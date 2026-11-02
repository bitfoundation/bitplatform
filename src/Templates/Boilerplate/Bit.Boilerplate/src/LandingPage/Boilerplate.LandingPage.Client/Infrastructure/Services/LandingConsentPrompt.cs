namespace Boilerplate.LandingPage.Client.Infrastructure.Services;

public sealed class LandingConsentPrompt
{
    public event Action? Requested;

    public void Open() => Requested?.Invoke();
}
