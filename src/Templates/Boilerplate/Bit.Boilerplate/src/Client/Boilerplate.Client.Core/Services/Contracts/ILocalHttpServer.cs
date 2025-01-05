namespace Boilerplate.Client.Core.Services.Contracts;

public interface ILocalHttpServer
{
    int Start(CancellationToken cancellationToken);

    /// <summary>
    /// Social sign-in on the web version of the app uses simple redirects. However, for Android, iOS, Windows, and macOS, social sign-in requires an in-app or external browser.
    /// 
    /// # Navigating Back to the App After Social Sign-In
    /// 1. **Universal Deep Links**: Allow the app to directly handle specific web links (for iOS and Android apps).
    /// 2. **Local HTTP Server**: Works similarly to how `git.exe` manages sign-ins with services like GitHub (supported on iOS, Android, Windows, and macOS).
    ///
    /// - **iOS, Windows, and macOS**: Use local HTTP server implementations in MAUI and Windows projects.
    /// - **Android**: Use universal links.
    /// </summary>
    bool ShouldUseForSocialSignIn();
}
