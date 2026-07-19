//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Models;
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Server.Api.Infrastructure.Services;
using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Shared.Features.Tenants.Dtos;
//#if (signalR == true)
using Boilerplate.Shared.Features.Chatbot;
//#endif

namespace Boilerplate.Server.Api.Features.Tenants;

/// <summary>
/// Manages the tenant the user is currently signed into (See <see cref="TenantManagementController"/> for global admin's tenants management).
/// </summary>
[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]")]
public partial class TenantController : AppControllerBase, ITenantController
{
    [AutoInject] private PhoneService phoneService = default!;
    [AutoInject] private IFusionCache fusionCache = default!;
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private IdentityEmailService emailService = default!;

    [HttpGet]
    public async Task<TenantDto?> GetCurrentTenant(CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();

        if (tenantId is null)
            return null;

        return await DbContext.Tenants.Where(t => t.Id == tenantId).Project().FirstOrDefaultAsync(cancellationToken);
    }

    [HttpPost, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<TenantDto> Create(TenantDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var tenantToAdd = dto.Map();
        tenantToAdd.Id = Guid.CreateSequentialGuid();
        tenantToAdd.IsActive = true;

        await DbContext.Tenants.AddAsync(tenantToAdd, cancellationToken);

        await Validate(tenantToAdd, cancellationToken);

        // Each tenant gets its own roles, and the user that has created the tenant becomes its first tenant admin.
        var tenantAdminRole = new Role
        {
            Id = Guid.CreateSequentialGuid(),
            Name = AppRoles.TenantAdmin,
            NormalizedName = AppRoles.TenantAdmin.ToUpperInvariant(),
            TenantId = tenantToAdd.Id,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        await DbContext.Roles.AddAsync(tenantAdminRole, cancellationToken);

        // Unlimited privileged sessions for the tenant's admins (Just like the seeded default tenant).
        await DbContext.RoleClaims.AddAsync(new()
        {
            RoleId = tenantAdminRole.Id,
            ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
            ClaimValue = "-1"
        }, cancellationToken);

        await DbContext.UserRoles.AddAsync(new() { UserId = userId, RoleId = tenantAdminRole.Id, TenantId = tenantToAdd.Id }, cancellationToken);

        var demoRole = new Role
        {
            Id = Guid.CreateSequentialGuid(),
            Name = AppRoles.Demo,
            NormalizedName = AppRoles.Demo.ToUpperInvariant(),
            TenantId = tenantToAdd.Id,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        await DbContext.Roles.AddAsync(demoRole, cancellationToken);

        foreach (var feature in AppFeatures.GetDemoFeatures())
        {
            await DbContext.RoleClaims.AddAsync(new()
            {
                RoleId = demoRole.Id,
                ClaimType = AppClaimTypes.FEATURES,
                ClaimValue = feature.Value
            }, cancellationToken);
        }

        // The value of AcceptedOn is set upfront for the user who has created the tenant.
        await DbContext.TenantUsers.AddAsync(new()
        {
            TenantId = tenantToAdd.Id,
            UserId = userId,
            AcceptedOn = TimeProvider.GetUtcNow()
        }, cancellationToken);

        //#if (signalR == true)
        // Each new tenant gets the 3 default system prompts upon creation.
        await DbContext.SystemPrompts.AddRangeAsync([
            new() { PromptKind = PromptKind.Support, Markdown = SystemPromptConfiguration.GetInitialSystemPromptMarkdown(), TenantId = tenantToAdd.Id },
            new() { PromptKind = PromptKind.AnalyzeProductImage, Markdown = SystemPromptConfiguration.GetAnalyzeProductImageSystemPromptMarkdown(), TenantId = tenantToAdd.Id },
            new() { PromptKind = PromptKind.FollowUpSuggestion, Markdown = SystemPromptConfiguration.GetFollowUpSuggestionSystemPromptMarkdown(), TenantId = tenantToAdd.Id }
        ], cancellationToken);
        //#endif

        await DbContext.SaveChangesAsync(cancellationToken);

        await fusionCache.RemoveAsync(TenantProvider.TENANTS_CACHE_KEY, token: cancellationToken);

        return tenantToAdd.Map();
    }

    /// <summary>
    /// <inheritdoc cref="AppFeatures.Management.Tenant_Manage"/>
    /// </summary>
    [HttpPut]
    [Authorize(Policy = AppFeatures.Management.Tenant_Manage), Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<TenantDto> Update(TenantDto dto, CancellationToken cancellationToken)
    {
        if (dto.Id != User.GetTenantId())
            throw new ResourceNotFoundException().WithData("Reason", "Admins may only update the tenant they're currently signed into.");

        var entityToUpdate = await DbContext.Tenants.FindAsync([dto.Id], cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.TenantCouldNotBeFound)]);

        var isActive = entityToUpdate.IsActive;

        dto.Patch(entityToUpdate);

        entityToUpdate.IsActive = isActive; // Only global admins can toggle IsActive (See TenantManagementController.Update).

        await Validate(entityToUpdate, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        await fusionCache.RemoveAsync(TenantProvider.TENANTS_CACHE_KEY, token: cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpPost]
    [Authorize(Policy = AppFeatures.Management.Tenant_Manage), Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task InviteUser(InviteUserToTenantRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.PhoneNumber))
            throw new BadRequestException(Localizer[nameof(AppStrings.EitherProvideEmailOrPhoneNumber)]);

        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);

        var tenantId = User.GetTenantId() ?? throw new InvalidOperationException("TenantId is required for inviting a user.");

        var tenant = await DbContext.Tenants.FindAsync([tenantId], cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.TenantCouldNotBeFound)]);

        var user = await userManager.FindUser(new() { Email = request.Email, PhoneNumber = request.PhoneNumber })
            ?? await userManager.CreateUserWithDemoRole(new IdentityRequestDto { Email = request.Email, PhoneNumber = request.PhoneNumber }); // Creates the user with a random password. The user can reset it later.

        if (await DbContext.TenantUsers.AnyAsync(tu => tu.TenantId == tenantId && tu.UserId == user.Id, cancellationToken))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsAlreadyInTenantErrorMessage)]);

        // Grant the tenant's demo role so that once she accepts the invitation she actually gets the tenant's demo
        // features (e.g. the Dashboard). Freshly-created users already got it from CreateUserWithDemoRole above, but an
        // already-existing user (e.g. one who had signed in elsewhere first) would otherwise land in the tenant without
        // any role. AssignDemoRole is idempotent, so calling it for both is safe (See UserManagerExtensions.AssignDemoRole).
        await userManager.AssignDemoRole(user.Id, tenantId);

        await DbContext.TenantUsers.AddAsync(new()
        {
            TenantId = tenantId,
            UserId = user.Id,
            AcceptedOn = null // The invitation gets accepted the first time the user switches into the tenant (See IdentityController.Refresh).
        }, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        var inviterDisplayName = User.GetDisplayName();
        var tenantTitle = tenant.Title ?? tenant.Name!;
        var webAppUrl = HttpContext.Request.GetWebAppUrl();

        List<Task> sendMessagesTasks = [];

        if (string.IsNullOrEmpty(user.Email) is false)
        {
            sendMessagesTasks.Add(emailService.SendTenantInvitation(user, inviterDisplayName, tenantTitle, webAppUrl, cancellationToken));
        }

        if (string.IsNullOrEmpty(user.PhoneNumber) is false)
        {
            var smsMessage = Localizer[nameof(AppStrings.TenantInvitationShortText), inviterDisplayName, tenantTitle, webAppUrl.ToString()].ToString();
            sendMessagesTasks.Add(phoneService.SendSms(smsMessage, user.PhoneNumber!));
        }

        await Task.WhenAll(sendMessagesTasks);
    }

    private async Task Validate(Tenant tenant, CancellationToken cancellationToken)
    {
        var entry = DbContext.Entry(tenant);

        // The custom domain is matched against the request host (case-insensitive), so it's stored as a lowercase host; a blank one becomes null.
        // SECURITY: this endpoint is self-service (tenant admins), and a custom domain wins over the sub domain during resolution.
        // Enforcing uniqueness alone is NOT enough for production - verify domain ownership and add it to TrustedOrigins first.
        tenant.Domain = string.IsNullOrWhiteSpace(tenant.Domain) ? null : tenant.Domain.Trim().ToLowerInvariant();

        // Remote validation example: Any errors thrown here will be displayed in the client's edit form component.
        if ((entry.State is EntityState.Added || entry.Property(t => t.Name).IsModified)
            && await DbContext.Tenants.AnyAsync(t => t.Id != tenant.Id && t.Name == tenant.Name, cancellationToken))
            throw new ResourceValidationException((nameof(TenantDto.Name), [Localizer[nameof(AppStrings.DuplicateTenantName), tenant.Name!]]));

        if (tenant.Domain is not null
            && (entry.State is EntityState.Added || entry.Property(t => t.Domain).IsModified)
            && await DbContext.Tenants.AnyAsync(t => t.Id != tenant.Id && t.Domain == tenant.Domain, cancellationToken))
            throw new ResourceValidationException((nameof(TenantDto.Domain), [Localizer[nameof(AppStrings.DuplicateTenantDomain), tenant.Domain]]));
    }
}
