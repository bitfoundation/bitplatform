namespace Boilerplate.Client.Windows.Infrastructure.Services;

/// <summary>
/// More info at <see cref="IPermissionService"/>
/// <para>
/// The app is the web view: what the page asks for arrives as WebView2's PermissionRequested, which Program.cs
/// grants, and Windows itself gates the device behind its own privacy settings. There is nothing left to ask.
/// </para>
/// </summary>
public partial class WindowsPermissionService : IPermissionService
{
    public Task<bool> RequestMicrophonePermission() => Task.FromResult(true);
}
