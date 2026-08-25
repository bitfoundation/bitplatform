namespace Boilerplate.Client.Maui.Infrastructure.Services;

/// <summary>
/// More info at <see cref="IPermissionService"/>
/// <para>
/// The web view is inside a native app, so the platform refuses the capability to the page unless the app itself
/// holds it. Every permission asked for here has to be declared in the platform's manifest as well - Android's
/// AndroidManifest.xml, the Info.plist of iOS and macOS, and Package.appxmanifest on Windows - and a missing
/// declaration is a build-time configuration mistake that MAUI reports by throwing rather than by refusing.
/// </para>
/// </summary>
public partial class MauiPermissionService : IPermissionService
{
    public async Task<bool> RequestMicrophonePermission()
    {
        return await RequestPermission<Permissions.Microphone>();
    }

    /// <summary>
    /// The status is checked before it is requested because a request is only ever answered by the user once: after
    /// a refusal the platform returns Denied without showing anything, and on the platforms where it does show the
    /// dialog, asking for something already granted would put it in front of the user again.
    /// </summary>
    private static async Task<bool> RequestPermission<TPermission>()
        where TPermission : Permissions.BasePermission, new()
    {
        if (await Permissions.CheckStatusAsync<TPermission>() is PermissionStatus.Granted)
            return true;

        var status = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<TPermission>);

        return status is PermissionStatus.Granted;
    }
}
