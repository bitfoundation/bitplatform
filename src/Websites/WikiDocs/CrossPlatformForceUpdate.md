# Cross-Platform Force Update Mechanism

---

This article outlines a robust, cross-platform force update mechanism designed to ensure users are running supported application versions. This system provides granular control over minimum supported versions across different platforms, addressing the unique challenges of each environment.

## How it Works

The core of this feature lies in its ability to define **minimum supported versions** for Android, iOS, Windows, macOS, and Web platforms. These versions are configured in `appsettings.json` or, for greater flexibility, as Environment variables, allowing modifications without requiring a new application publish.

The rationale behind platform-specific minimum values is to account for varying app store approval processes. For example, an app might be approved and live on Google Play but still awaiting approval on the Apple App Store. This system enables developers to apply different minimum supported levels tailored to each platform's deployment status.

It's crucial to understand that this mechanism is specifically for **mandatory updates**, not optional enhancements. When a client's application version falls below the defined minimum, the server API returns a `ClientNotSupportedException`, prompting the client to display a snackbar with an update button.

## Update Experience Across Platforms

The user experience for initiating an update differs slightly across platforms:

* **Windows and Web:** Tapping the update button will trigger the download and application of the new version, followed by an automatic refresh of the application.
* **iOS, macOS, and Android:** The update button will redirect the user to the application's page within the respective app store (e.g., Google Play Store, Apple App Store). The user must then manually tap "Update" again to complete the process. While this requires an extra step, it remains a straightforward user experience.

A significant advantage of this force update approach is its immediate impact. Even users who **keep the app open for extended periods** (especially on desktop PCs and laptops) will be prompted to update, ensuring they are not running critically outdated versions.

## Rationale for Web Platform Inclusion

While seemingly counterintuitive for web applications, including the web in this force update mechanism addresses a critical concern: **unnecessary automatic updates**.

Consider a scenario where your team is rapidly publishing new releases. Not all these releases necessitate a mandatory download for existing users, especially if they involve minor UI tweaks or small, non-critical changes.

Without this force update mechanism, a frequently updated Progressive Web App (PWA) that relies on automatic updates and reloads would present a less than ideal user experience. Imagine this:

1.  User opens the web app, experiencing a few seconds of loading time on lower-end devices.
2.  The browser automatically downloads the new version in the background.
3.  The web app refreshes itself, leading to another few seconds of loading time on lower-end devices.

This can result in a suboptimal user experience **if the updates between their last visit and the current one are not substantial.** To mitigate this, we recently disabled the automatic update feature in the web application boilerplate. Instead, developers can now explicitly set the minimum supported version for their web users.

## Example Scenario

Let's illustrate with a practical example:

* **Latest application version:** 10
* **Minimum supported version:** 8

In this scenario:

1.  A **new user** who has not previously accessed the web app will open it and automatically receive version 10.
2.  A user who last opened the web app a few days ago with **version 9** will still be able to continue using version 9, as it meets the minimum supported requirement.
3.  A user with a version **older than 8** (e.g., version 7) will be instructed to update the application to at least version 8.



## Configuration:

```json
"SupportedAppVersions": {
    "MinimumSupportedAndroidAppVersion": "1.0.0",
    "MinimumSupportedIosAppVersion": "1.0.0",
    "MinimumSupportedMacOSAppVersion": "1.0.0",
    "MinimumSupportedWindowsAppVersion": "1.0.0",
    "MinimumSupportedWebAppVersion": "1.0.0",
    "SupportedAppVersions__Comment": "Enabling `AutoReload` (Disabled by default) ensure the latest app version is always applied in Web & Windows apps. Refer to `Client.Web/Components/AppBswupProgressBar.razor`, `Client.Web/wwwroot/index.html` and `Client.Windows/appsettings.json` for details."
}
```
---