//+:cnd:noEmit
using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.Identity;

public partial class UserManagementController
{
    [AutoInject] private ILogger<UserManagementController> logger = default!;
    [AutoInject] private PersonalDataExportService personalDataExportService = default!;

    /// <summary>
    /// The same export as <c>UserController.ExportPersonalData</c>, for a request that did not arrive through the app -
    /// by e-mail, through a lawyer, or from somebody who can no longer sign in. Article 12(3)'s month runs either way.
    /// The same <see cref="PersonalDataExportService"/> deliberately: an admin must not be able to send a different,
    /// and therefore incomplete, answer to the one the user can produce themselves.
    /// </summary>
    [HttpGet("{userId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task ExportPersonalData(Guid userId, CancellationToken cancellationToken)
    {
        //#if (multitenant == true)
        await EnsureUserIsInCurrentTenant(userId, cancellationToken);

        // Global admins only: the export spans every tenant the account belongs to, so handing it to a tenant admin
        // would tell them about tenants that are none of their business. Their answer is to escalate.
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
            throw new ForbiddenException().WithData("Reason", "Only global admins may export an account, because the export spans every tenant the account belongs to.");
        //#endif

        // Before the body streams, so a dropped connection still leaves the record. Interim: this belongs in a
        // PersonalDataAccessLog table that an erasure does not empty, which does not exist yet.
        logger.LogWarning("Personal data of user {SubjectUserId} was exported by user {ActorUserId}.", userId, User.GetUserId());

        await personalDataExportService.WriteTo(userId, Response, cancellationToken);
    }
}
