//+:cnd:noEmit
using System.Text.Json;
using System.Text.Json.Nodes;
using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.Identity;

/// <summary>
/// Sign-ins, and what was recorded about the device and place each came from. The settings page already shows these;
/// the export is the same facts in a form the user can keep.
/// </summary>
public partial class UserSessionsPersonalDataSource : IPersonalDataSource
{
    [AutoInject] private AppDbContext dbContext = default!;
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;

    /// <summary>Read by <see cref="PrepareErase"/>, because after the delete there is no row to find them from.</summary>
    private string[] signalRConnectionIdsToRevoke = [];
    //#endif

    public string Key => "sessions";

    public int Order => 20;

    /// <summary>After the push subscriptions, whose foreign key to a session is <c>SetNull</c>.</summary>
    public int ErasureOrder => 30;

    public string Purpose => "Keeping you signed in on each device, showing you where your account is signed in, and letting you sign a device out remotely.";

    public string Retention => "14 days after a session was last renewed, then deleted by a daily job. Signing a device out deletes its session immediately.";

    //#if (cloudflare == true)
    public string[] Recipients => ["Cloudflare - sits in front of the application and supplies the country and city recorded below."];
    //#endif

    public PersonalDataErasure Erasure => PersonalDataErasure.ErasureService;

    public async Task<JsonNode?> Export(Guid userId, CancellationToken cancellationToken)
    {
        // Materialised before mapping: StartedOn and RenewedOn are unix seconds, which no provider translates.
        var sessions = await dbContext.UserSessions
            .AsNoTracking()
            .Where(userSession => userSession.UserId == userId)
            .OrderBy(userSession => userSession.StartedOn)
            .Select(userSession => new
            {
                userSession.Id,
                userSession.IP,
                userSession.Address,
                userSession.StartedOn,
                userSession.RenewedOn,
                userSession.DeviceInfo,
                userSession.PlatformType,
                userSession.CultureName,
                userSession.AppVersion,
                userSession.Privileged,
                //#if (multitenant == true)
                userSession.TenantId,
                //#endif
            })
            .ToArrayAsync(cancellationToken);

        var export = sessions.Select(session => new
        {
            session.Id,
            // The address the request came from, as the server saw it.
            session.IP,
            // Country and city, as reported by the CDN in front of the application. Rewritten on every token renewal.
            session.Address,
            StartedOn = DateTimeOffset.FromUnixTimeSeconds(session.StartedOn),
            RenewedOn = session.RenewedOn is null ? (DateTimeOffset?)null : DateTimeOffset.FromUnixTimeSeconds(session.RenewedOn.Value),
            session.DeviceInfo,
            Platform = session.PlatformType,
            session.CultureName,
            session.AppVersion,
            session.Privileged,
            //#if (multitenant == true)
            session.TenantId,
            //#endif
        });

        return JsonSerializer.SerializeToNode(export, IPersonalDataSource.SerializerOptions);
    }

    //#if (signalR == true)
    public async Task PrepareErase(PersonalDataErasureContext context, CancellationToken cancellationToken)
    {
        // WhereIf rather than an inline comparison: a null there is SQL NULL, which matches no rows.
        signalRConnectionIdsToRevoke = await dbContext.UserSessions
            .Where(userSession => userSession.UserId == context.UserId && userSession.SignalRConnectionId != null)
            .WhereIf(context.ExceptSessionId is not null, userSession => userSession.Id != context.ExceptSessionId)
            .Select(userSession => userSession.SignalRConnectionId!)
            .ToArrayAsync(cancellationToken);
    }
    //#endif

    public async Task Erase(PersonalDataErasureContext context, CancellationToken cancellationToken)
    {
        await dbContext.UserSessions
            .Where(userSession => userSession.UserId == context.UserId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    //#if (signalR == true)
    /// <summary>
    /// After the commit: the other devices are told the account is gone once it actually is. See <c>AppHub</c>'s comments.
    /// </summary>
    public async Task ErasePublished(PersonalDataErasureContext context, CancellationToken cancellationToken)
    {
        await appHubContext.Clients.Clients(signalRConnectionIdsToRevoke).Publish(SharedAppMessages.SESSION_REVOKED, null, cancellationToken);
    }
    //#endif
}
