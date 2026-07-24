using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Server.Api.Features.Identity.Services;

/// <summary>
/// Unlike the base <see cref="RoleValidator{TRole}"/> that enforces a globally unique role name,
/// the role name uniqueness is scoped by the role's TenantId here, because each tenant has its own roles (like t-admin).
/// </summary>
public partial class AppRoleValidator : RoleValidator<Role>
{
    public override async Task<IdentityResult> ValidateAsync(RoleManager<Role> manager, Role role)
    {
        if (string.IsNullOrWhiteSpace(role.Name))
            return IdentityResult.Failed(manager.ErrorDescriber.InvalidRoleName(role.Name));

        var normalizedName = manager.KeyNormalizer.NormalizeName(role.Name);

        var duplicateRoleExists = await manager.Roles
            .AnyAsync(r => r.Id != role.Id && r.NormalizedName == normalizedName && r.TenantId == role.TenantId);

        return duplicateRoleExists
            ? IdentityResult.Failed(manager.ErrorDescriber.DuplicateRoleName(role.Name!))
            : IdentityResult.Success;
    }
}
