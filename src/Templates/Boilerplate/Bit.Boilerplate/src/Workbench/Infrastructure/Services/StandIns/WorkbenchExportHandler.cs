using System.Net;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// Answers the privacy section's personal data export in the view, from what the user stand-in answers the page with, as the
/// server's export holds what it has stored: a changed email, a removed session or a removed picture shows in the download.
/// As the server's, it needs the session to have confirmed it's the user lately (ELEVATED_ACCESS). Anything else the app's
/// code sends goes on to the Workbench's endpoints.
/// </summary>
public sealed class WorkbenchExportHandler(IUserController userController, WorkbenchScenarioState scenarioState, IWebHostEnvironment environment) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get || string.Equals(request.RequestUri?.AbsolutePath.TrimStart('/'), IUserController.ExportPersonalDataUri, StringComparison.OrdinalIgnoreCase) is false)
            return await base.SendAsync(request, cancellationToken);

        if (WorkbenchClaims.UserOf(request.Headers.Authorization?.ToString(), elevated: true) is not { } userId || WorkbenchClaims.UserOf(userId) is not { } demoUser)
            return new(HttpStatusCode.Forbidden) { RequestMessage = request };

        var user = await userController.GetCurrentUser(cancellationToken);
        var sessions = await userController.GetUserSessions(cancellationToken);

        // Both follow the view's sign-in, which can change while they load, as they take a while in the Slow state. An export
        // of another account than the token's, or of one account's profile with another's sessions, is refused as a token
        // that's no longer the view's.
        if (user.Id != userId || scenarioState.Scenario.Auth is WorkbenchAuthState.SignedOut || WorkbenchClaims.UserIdOf(scenarioState.Scenario.User) != userId)
            return new(HttpStatusCode.Unauthorized) { RequestMessage = request };

        var photo = WorkbenchPhotos.Find(demoUser.NameKey) is { } path ? environment.WebRootFileProvider.GetFileInfo(path).PhysicalPath : null;
        var exportedOn = DateTimeOffset.UtcNow;

        var content = new ByteArrayContent(SettingsExports.PersonalData(user, sessions, exportedOn, photo));
        content.Headers.ContentType = new("application/zip");
        content.Headers.ContentDisposition = new("attachment") { FileName = SettingsExports.FileNameOf(userId, exportedOn) };

        return new(HttpStatusCode.OK) { Content = content, RequestMessage = request };
    }
}
