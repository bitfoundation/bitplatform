using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Microsoft.AspNetCore.Identity;

public static partial class UserManagerExtensions
{
    public static async Task<User?> FindUserAsync(this UserManager<User> userManager, IdentityRequestDto identity)
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

    public static Task<User?> FindByPhoneNumber(this UserManager<User> userManager, string phoneNumber)
    {
        return userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public static async Task CreateUserWithDemoRole(this UserManager<User> userManager, User userToAdd, string password)
    {
        var result = await userManager.CreateAsync(userToAdd, password);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        result = await userManager.AddToRoleAsync(userToAdd, AppRoles.Demo);
    }
}
