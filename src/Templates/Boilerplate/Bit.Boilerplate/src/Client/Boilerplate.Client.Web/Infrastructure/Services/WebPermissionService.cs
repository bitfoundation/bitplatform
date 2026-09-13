namespace Boilerplate.Client.Web.Infrastructure.Services;

/// <summary>
/// More info at <see cref="IPermissionService"/>
/// <para>
/// The browser owns its permissions: the web API that needs one raises the prompt itself and reports a refusal back
/// through its own error path, so there is nothing to ask for ahead of time.
/// </para>
/// </summary>
public partial class WebPermissionService : IPermissionService
{
    public Task<bool> RequestMicrophonePermission() => Task.FromResult(true);
}
