//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Server.Api.Features.Identity.Models;
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Identity.Services;
//#endif

namespace Microsoft.AspNetCore.Identity;

public static partial class UserManagerExtensions
{
    extension(UserManager<User> userManager)
    {
        public async Task<User?> FindUser(IdentityRequestDto identity)
        {
            User? user = default;

            var (userName, email, phoneNumber) = (identity.UserName, identity.Email, identity.PhoneNumber);

            if (userName is null && email is null && phoneNumber is null)
                throw new InvalidOperationException();

            if (string.IsNullOrEmpty(userName) is false)
            {
                user = await userManager.FindByNameAsync(userName!);
            }

            if (user is null && string.IsNullOrEmpty(email) is false)
            {
                user = await userManager.FindByEmailAsync(email!);
            }

            if (user is null && string.IsNullOrEmpty(phoneNumber) is false)
            {
                user = await userManager.FindByPhoneNumber(phoneNumber);
            }

            return user;
        }

        public Task<User?> FindByPhoneNumber(string phoneNumber)
        {
            return userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<User> CreateUserWithDemoRole(IdentityRequestDto request, string? password = null)
        {
            return await userManager.CreateUserWithDemoRole(new User
            {
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                UserName = request.UserName
            }, password);
        }

        public async Task<User> CreateUserWithDemoRole(User userToAdd, string? password = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                password = Guid.CreateVersion7().ToString("N"); // Users can reset their password later.
            }

            if (string.IsNullOrEmpty(userToAdd.UserName))
            {
                userToAdd.UserName = userToAdd.Email ?? userToAdd.PhoneNumber ?? Guid.CreateVersion7().ToString("N");
            }

            var result = await userManager.CreateAsync(userToAdd, password);

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

            //#if (multitenant == true)
            // The freshly-created user gets the current tenant's demo role (See AssignDemoRole).
            var tenantId = userManager.ServiceProvider.GetRequiredService<TenantProvider>().GetCurrentTenantId();
            await userManager.AssignDemoRole(userToAdd.Id, tenantId);
            //#endif
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
            //#if (multitenant != true)
            result = await userManager.AddToRoleAsync(userToAdd, AppRoles.Demo);

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
            //#endif
            //#if (IsInsideProjectTemplate == true)
            */
            //#endif

            return userToAdd;
        }

        //#if (multitenant == true)
        /// <summary>
        /// Grants the given tenant's demo role to the user, unless she already has it or that tenant has no demo role.
        /// Demo is a per-tenant role (See RoleConfiguration and TenantController.Create), so its name isn't globally
        /// unique and userManager.AddToRoleAsync (which resolves the role by its name) can't be used; the tenant's demo
        /// role is assigned directly instead (like RoleManagementController.ToggleUserRole manages UserRoles).
        /// Both freshly-created users (See CreateUserWithDemoRole) and already-existing invitees (See
        /// TenantController.InviteUser) go through here, so accepting an invitation actually unlocks the tenant's demo
        /// features (e.g. the Dashboard). If that tenant happens to have no demo role, the user is simply left without one.
        /// </summary>
        public async Task AssignDemoRole(Guid userId, Guid tenantId)
        {
            var dbContext = userManager.ServiceProvider.GetRequiredService<AppDbContext>();

            var demoRoleId = await dbContext.Roles
                .Where(role => role.Name == AppRoles.Demo && role.TenantId == tenantId)
                .Select(role => (Guid?)role.Id)
                .FirstOrDefaultAsync();

            if (demoRoleId is null)
                return;

            // Adding it twice would violate the UserRoles primary key, so skip if she already has it (e.g. she was just
            // created with it, then invited into the same tenant).
            if (await dbContext.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == demoRoleId.Value))
                return;

            await dbContext.UserRoles.AddAsync(new UserRole { UserId = userId, RoleId = demoRoleId.Value, TenantId = tenantId });
            await dbContext.SaveChangesAsync();
        }
        //#endif
    }
}
