using System.Text.Json;
using System.Text.Json.Nodes;
using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.Tenants;

/// <summary>
/// Which organisations this account belongs to, and whether the invitation was accepted.
/// </summary>
public partial class TenantsPersonalDataSource : IPersonalDataSource
{
    [AutoInject] private AppDbContext dbContext = default!;

    public string Key => "tenants";

    public int Order => 50;

    public string Purpose => "Deciding which organisation's data you can see, and which you have been invited to.";

    public string Retention => "For as long as the membership exists. Leaving an organisation clears the acceptance; deleting your account removes the membership row.";

    public PersonalDataErasure Erasure => PersonalDataErasure.CascadeFromUser;

    public async Task<JsonNode?> Export(Guid userId, CancellationToken cancellationToken)
    {
        var memberships = await dbContext.TenantUsers
            .AsNoTracking()
            .Where(tenantUser => tenantUser.UserId == userId)
            .OrderBy(tenantUser => tenantUser.AcceptedOn)
            .Select(tenantUser => new
            {
                tenantUser.TenantId,
                TenantTitle = tenantUser.Tenant!.Title,
                // Null while the invitation is still open, or after you left the organisation.
                tenantUser.AcceptedOn
            })
            .ToArrayAsync(cancellationToken);

        return JsonSerializer.SerializeToNode(memberships, IPersonalDataSource.SerializerOptions);
    }
}
