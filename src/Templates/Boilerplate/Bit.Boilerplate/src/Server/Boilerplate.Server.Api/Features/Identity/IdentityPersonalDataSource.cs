using System.Text.Json;
using System.Text.Json.Nodes;
using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.Identity;

/// <summary>
/// The account row itself, plus the ways of signing into it.
/// </summary>
public partial class IdentityPersonalDataSource : IPersonalDataSource
{
    [AutoInject] private AppDbContext dbContext = default!;

    public string Key => "account";

    public int Order => 10;

    public string Purpose => "Identifying you, letting you sign in, and contacting you about your account.";

    public string Retention => "For as long as the account exists. An account that is never confirmed is deleted 48 hours after it was created.";

    /// <summary>
    /// The only source with nothing to delete: its data <em>is</em> the <c>Users</c> row, which
    /// <c>UserErasureService</c> removes itself once every other source has run, through
    /// <c>userManager.DeleteAsync</c> so the Identity cascades fire.
    /// </summary>
    public PersonalDataErasure Erasure => PersonalDataErasure.ErasureService;

    /// <summary>
    /// Everything the <c>Users</c> row holds, minus what proves they are that person: no password hash, security
    /// stamp, two-factor secret or passkey key. Handing those over weakens the account and tells the reader nothing
    /// about themselves - which is what Article 15(4) is for.
    /// </summary>
    public async Task<JsonNode?> Export(Guid userId, CancellationToken cancellationToken)
    {
        var account = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.PhoneNumberConfirmed,
                user.FullName,
                user.Gender,
                user.BirthDate,
                user.CreatedOn,
                user.TwoFactorEnabled,
                user.HasProfilePicture,
                user.LockoutEnd,
                user.AccessFailedCount,
                Roles = user.Roles.Select(userRole => userRole.Role!.Name).ToArray(),
                ExternalLogins = user.Logins.Select(login => new
                {
                    Provider = login.LoginProvider,
                    login.ProviderDisplayName,
                    // The identifier the provider knows you by. Yours, and worth having when asking them for a copy too.
                    login.ProviderKey
                }).ToArray(),
                Passkeys = user.WebAuthnCredentials.Select(credential => new
                {
                    RegisteredOn = credential.RegDate,
                    credential.AttestationFormat,
                    credential.IsBackedUp
                }).ToArray()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return account is null ? null : JsonSerializer.SerializeToNode(account, IPersonalDataSource.SerializerOptions);
    }
}
