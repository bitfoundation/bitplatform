//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class RefreshTokenRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS" />
    /// </summary>
    public string? ElevatedAccessToken { get; set; }

    //#if (multitenant == true)
    /// <summary>
    /// The id of the tenant the user is trying to switch into.
    /// If it has a value and the user has such an active tenant, then that id will be stored as the <see cref="AppClaimTypes.TENANT_ID"/> claim,
    /// otherwise the user gets kicked out. When it's null, the tenant id of the passed refresh token remains unchanged.
    /// </summary>
    public Guid? RequestedTenantId { get; set; }
    //#endif
}
