//+:cnd:noEmit
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The stand-in for the app's <see cref="IAttachmentController"/>: removing the profile picture. Uploading one goes to the
/// address the Workbench serves (See WorkbenchProfilePictures), as the app's page posts the file itself.
/// </summary>
public sealed class WorkbenchAttachmentController(WorkbenchScenarioState scenarioState, SettingsSampleAccount account)
    : WorkbenchStandIn(scenarioState, "settings"), IAttachmentController
{
    public Task DeleteUserProfilePicture(CancellationToken cancellationToken) => Save(account.RemovePicture, cancellationToken);

    //#if (module == "Sales" || module == "Admin")
    public Task DeleteProductPrimaryImage(Guid productId, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();
    //#endif
}
