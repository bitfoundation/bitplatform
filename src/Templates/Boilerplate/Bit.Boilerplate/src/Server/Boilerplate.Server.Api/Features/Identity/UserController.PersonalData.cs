using Microsoft.AspNetCore.RateLimiting;
using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.Identity;

public partial class UserController
{
    [AutoInject] private PersonalDataExportService personalDataExportService = default!;

    /// <summary>
    /// Articles 15 and 20: everything the app holds about the signed-in account, as a zip.
    /// <para>
    /// Behind <see cref="AuthPolicies.ELEVATED_ACCESS"/>, the gate <c>Delete</c> uses - a full copy of an identity is
    /// worth as much to a stolen token as deleting it is. Rate limited for the reason Article 12(5) exists.
    /// </para>
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    [EnableRateLimiting(RateLimitOptionsExtensions.IDENTITY)]
    public async Task ExportPersonalData(CancellationToken cancellationToken)
    {
        await personalDataExportService.WriteTo(User.GetUserId(), Response, cancellationToken);
    }
}
