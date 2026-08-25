namespace Boilerplate.Shared.Features.Tenants.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class TenantDto
{
    /// <summary>
    /// The tenant's name must match sub domain name restrictions, so the tenant
    /// can get resolved from the sub domain for anonymous requests (See TenantProvider).
    /// <para>
    /// SECURITY: this is a <b>shape</b> check only, and it also runs client-side. It deliberately does not know
    /// which labels are already taken by the deployment itself - the server rejects those separately
    /// (See <c>ReservedTenantNames</c>), because the name is the sub domain resolution key and creating a
    /// tenant is self-service. Do not treat matching this pattern as sufficient.
    /// </para>
    /// </summary>
    public const string NAME_REGEX_PATTERN = "^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$";

    /// <summary>
    /// A tenant's optional custom domain must be a valid host (FQDN with at least one dot, e.g. <c>myapp.com</c>),
    /// with no scheme, port or path, so the tenant can be resolved from the complete request host (See TenantProvider).
    /// Case-insensitive on input; it's stored lowercased by the server.
    /// </summary>
    public const string DOMAIN_REGEX_PATTERN = "^(?=.{1,253}$)([a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\\.)+[a-zA-Z]{2,}$";

    public Guid Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Name))]
    [MaxLength(63, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    [RegularExpression(NAME_REGEX_PATTERN, ErrorMessage = nameof(AppStrings.TenantNameRegexPatternError))]
    public string? Name { get; set; }

    [Display(Name = nameof(AppStrings.Title))]
    [MaxLength(64, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    public string? Title { get; set; }

    [Display(Name = nameof(AppStrings.Domain))]
    [MaxLength(253, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    [RegularExpression(DOMAIN_REGEX_PATTERN, ErrorMessage = nameof(AppStrings.TenantDomainRegexPatternError))]
    public string? Domain { get; set; }

    [Display(Name = nameof(AppStrings.Active))]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The current user's membership state for this tenant: <c>true</c> when she has accepted it, <c>false</c> when she
    /// has been invited but hasn't accepted yet (See <c>TenantUser.AcceptedOn</c>), and <c>null</c> when she has no
    /// membership row at all - which only happens for a global admin, who is listed every active tenant whether or not
    /// she belongs to it. Only populated by <c>UserController.GetTenants</c>.
    /// </summary>
    public bool? CurrentUserHasAcceptedThisTenantInvitation { get; set; }

    public long Version { get; set; }
}
