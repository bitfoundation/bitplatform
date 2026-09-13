namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

/// <summary>
/// Asks the operating system for a capability the app is about to use.
/// <para>
/// Only the hosts that wrap the app in a native shell have anything to do here: a browser prompts for itself the
/// first time a web API needs the capability, whereas a web view inside a native app is refused by the platform
/// before its own prompt is ever reached, so the native permission has to be granted first.
/// </para>
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Whether the app may use the microphone, asking the user if the platform has not been answered already.
    /// </summary>
    Task<bool> RequestMicrophonePermission();
}
