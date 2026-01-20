using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Server.Api.Features.Identity.Models;

namespace Microsoft.AspNetCore.Identity;

public static partial class UserManagerExtensions
{
    public static async Task<User?> FindUser(this UserManager<User> userManager, IdentityRequestDto identity)
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

    public static async Task<User> CreateUserWithDemoRole(this UserManager<User> userManager, IdentityRequestDto request, string? password = null)
    {
        return await userManager.CreateUserWithDemoRole(new User
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            UserName = request.UserName
        }, password);
    }

    public static async Task<User> CreateUserWithDemoRole(this UserManager<User> userManager, User userToAdd, string? password = null)
    {
        if (string.IsNullOrEmpty(password))
        {
            password = Guid.NewGuid().ToString("N"); // Users can reset their password later.
        }

        if (string.IsNullOrEmpty(userToAdd.UserName))
        {
            userToAdd.UserName = userToAdd.Email ?? userToAdd.PhoneNumber ?? Guid.NewGuid().ToString("N");
        }

        var result = await userManager.CreateAsync(userToAdd, password);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        result = await userManager.AddToRoleAsync(userToAdd, AppRoles.Demo);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        return userToAdd;
    }
}
